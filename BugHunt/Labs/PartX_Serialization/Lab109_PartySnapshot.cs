using System.Text.Encodings.Web;
using System.Text.Json;

namespace BugHunt;

// 던전에 들어갈 때 파티 상태를 JSON 스냅샷으로 저장하고, 재접속하면 스냅샷에서 복원한다.
// 복원한 뒤 리더가 피해를 입었는데 파티 창의 리더 체력은 그대로다.
// [JA] ダンジョンに入るときパーティーの状態を JSON のスナップショットで保存し、再接続するとスナップショットから復元します。
// [JA] 復元したあとリーダーがダメージを受けたのに、パーティー画面のリーダーのHPはそのままです。
public static partial class Lab109
{
    sealed class Member
    {
        public string Name { get; set; } = "";
        public int Hp { get; set; }
    }

    sealed class Party
    {
        public List<Member> Members { get; set; } = [];
        // 리더와 메인 탱커는 파티원 중 한 사람을 가리킨다.
        public Member? Leader { get; set; }
        public Member? MainTank { get; set; }
    }

    // 한글을 이스케이프하지 않고 그대로 쓴다(콘솔에서 읽기 쉽게).
    static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    static Party Restore(Party party)
    {
        string json = JsonSerializer.Serialize(party, Options);
        Console.WriteLine($"  스냅샷: {json}");
        return JsonSerializer.Deserialize<Party>(json, Options) ?? throw new FormatException("빈 스냅샷");
    }

    public static void Run()
    {
        string[] names = Pick(Rosters[0], Rosters[1..]);
        var party = new Party { Members = [.. names.Select((n, i) => new Member { Name = n, Hp = 100 + 10 * i })] };
        int leader = Pick(0, 0, names.Length - 1);
        int tank = Pick(1, 0, names.Length - 1);
        party.Leader = party.Members[leader];
        party.MainTank = party.Members[tank];

        Party restored = Restore(party);

        // 복원 뒤 전투: 보스가 리더와 메인 탱커를 때린다.
        int damage = Pick(30, 5, 90);
        restored.Leader!.Hp -= damage;
        restored.MainTank!.Hp -= damage;
        int leaderHits = leader == tank ? 2 : 1;

        Member leaderInList = restored.Members[leader];
        Console.WriteLine($"  파티 창: {string.Join(", ", restored.Members.Select(m => $"{m.Name} {m.Hp}"))}");

        Expect("리더가 파티원 목록의 그 사람", true, ReferenceEquals(restored.Leader, leaderInList));
        Expect("메인 탱커가 파티원 목록의 그 사람", true, ReferenceEquals(restored.MainTank, restored.Members[tank]));
        Expect("파티 창의 리더 체력", party.Members[leader].Hp - damage * leaderHits, leaderInList.Hp);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// Restore 에 들어가기 전에 party.Leader 와 party.Members[leader] 에 각각 개체 ID 를 만들어 보세요($1, $2). 같은 번호인가요?
// Restore 뒤에 restored.Leader 와 restored.Members[leader] 에도 개체 ID 를 만들어 보세요. 이번에는요?
//
// [JA] Restore に入る前に party.Leader と party.Members[leader] にそれぞれオブジェクト ID を作ってみてください($1, $2)。同じ番号ですか?
// [JA] Restore のあと restored.Leader と restored.Members[leader] にもオブジェクト ID を作ってみてください。今度はどうですか?
#endregion

#region 힌트 2 / ヒント 2
// JSON 에는 "같은 객체"라는 개념이 없습니다. 콘솔의 스냅샷을 보면 리더의 이름과 체력이 members 안에 한 번, leader 에 또 한 번 그대로 적혀 있습니다.
// 읽을 때는 적힌 곳마다 새 객체를 만듭니다. 그래서 원래 한 사람이던 리더가 복원 뒤에는 두 사람(목록 안의 리더, Leader 속성의 리더)이 됩니다. 한쪽을 고쳐도 다른 쪽은 그대로입니다.
//
// [JA] JSON には「同じオブジェクト」という概念がありません。コンソールのスナップショットを見ると、リーダーの名前とHPが members の中に一度、leader にもう一度そのまま書かれています。
// [JA] 読むときは書かれた場所ごとに新しいオブジェクトを作ります。そのため元は一人だったリーダーが、復元後には二人(リストの中のリーダー、Leader プロパティのリーダー)になります。片方を直してももう片方はそのままです。
#endregion

#region 힌트 3 / ヒント 3
// 방법 1: 참조를 보존하는 옵션. ReferenceHandler = ReferenceHandler.Preserve 를 쓰면 처음 나온 객체에 "$id", 다시 나올 때 "$ref" 를 적고, 읽을 때 같은 객체로 이어 줍니다.
//        (저장하는 쪽과 읽는 쪽 모두 같은 옵션이어야 합니다. JSON 모양이 바뀌므로 다른 프로그램과 주고받는 형식이면 상대도 이 규칙을 알아야 합니다.)
// 방법 2: 형식을 바꾼다. 객체 대신 "목록의 몇 번째" 또는 고유 Id 를 저장하고(LeaderIndex, MainTankId), 읽은 뒤에 다시 찾아 잇습니다. 세이브 형식으로는 이쪽이 더 단단합니다.
// 리더만 이름으로 다시 찾아 잇는 수정은 메인 탱커에서 걸립니다. 공유되는 참조가 몇 개인지 먼저 세어 보세요.
//
// [JA] 方法1: 参照を保持するオプション。ReferenceHandler = ReferenceHandler.Preserve を使うと、最初に出たオブジェクトに "$id"、再び出たときに "$ref" を書き、読むときに同じオブジェクトとしてつなぎます。
// [JA]        (保存する側と読む側の両方が同じオプションでなければなりません。JSON の形が変わるので、他のプログラムとやり取りする形式なら相手もこの規則を知っている必要があります。)
// [JA] 方法2: 形式を変える。オブジェクトの代わりに「リストの何番目」または固有 Id を保存し(LeaderIndex, MainTankId)、読んだあとに探し直してつなぎます。セーブ形式としてはこちらのほうが堅牢です。
// [JA] リーダーだけ名前で探し直してつなぐ修正はメインタンクで引っかかります。共有される参照がいくつあるか、先に数えてみてください。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 다른 파티 구성, 리더·메인 탱커 위치(같은 사람일 수도 있음), 피해량으로 검사한다.
// [JA] 隠しケースは別のパーティー構成、リーダー・メインタンクの位置(同じ人のこともある)、ダメージ量で検査します。
public static partial class Lab109
{
    static readonly string[][] Rosters =
    [
        ["아린", "케인", "미라"],
        ["도란", "리라", "오렌", "케인"],
        ["미라", "아린"],
        ["케인", "도란", "리라", "아린", "오렌"],
    ];
}
#endregion
