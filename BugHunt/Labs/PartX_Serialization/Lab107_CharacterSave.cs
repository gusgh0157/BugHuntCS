using System.Text.Encodings.Web;
using System.Text.Json;

namespace BugHunt;

// 캐릭터를 JSON 세이브로 저장했다가 다시 불러온다. 이름은 돌아오는데 골드는 0, 레벨은 1이 된다.
// [JA] キャラクターを JSON のセーブに保存してからロードし直します。名前は戻るのに、ゴールドは 0、レベルは 1 になります。
public static partial class Lab107
{
    sealed class Character
    {
        public string Name { get; set; } = "";

        // 골드는 자주 바뀌어서 필드로 둔다.
        public int Gold;

        // 레벨은 LevelUp 으로만 올린다.
        public int Level { get; private set; } = 1;

        public void LevelUp() => Level++;
    }

    // 한글을 이스케이프하지 않고 그대로 쓴다(콘솔에서 읽기 쉽게).
    static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    static string Save(Character c) => JsonSerializer.Serialize(c, Options);

    static Character Load(string json) => JsonSerializer.Deserialize<Character>(json, Options) ?? throw new FormatException("빈 세이브");

    public static void Run()
    {
        var hero = new Character { Name = Pick("아린", "케인", "미라", "도란"), Gold = Pick(1500, 1, 99_999) };
        int levels = Pick(4, 1, 30);
        for (int i = 0; i < levels; i++)
            hero.LevelUp();
        Console.WriteLine($"  저장 전: {hero.Name}, 레벨 {hero.Level}, {hero.Gold}G");

        string json = Save(hero);
        Console.WriteLine($"  세이브 파일: {json}");

        Character loaded = Load(json);
        Console.WriteLine($"  불러온 뒤: {loaded.Name}, 레벨 {loaded.Level}, {loaded.Gold}G");

        Expect("이름", hero.Name, loaded.Name);
        Expect("골드", hero.Gold, loaded.Gold);
        Expect("레벨", hero.Level, loaded.Level);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 콘솔의 "세이브 파일" 줄을 보세요. 골드가 들어 있나요? 레벨은 들어 있나요?
// 저장 쪽에서 빠진 것과, 저장은 됐는데 불러올 때 빠진 것을 나누어 보세요. 둘의 원인은 다릅니다.
//
// [JA] コンソールの「세이브 파일」(セーブファイル)の行を見てください。ゴールドは入っていますか? レベルは入っていますか?
// [JA] 保存側で抜けたものと、保存はされたのにロード時に抜けたものを分けてみてください。二つの原因は別です。
#endregion

#region 힌트 2 / ヒント 2
// Gold 는 속성이 아니라 필드입니다. System.Text.Json 은 기본적으로 public 속성만 다루고 필드는 무시합니다. 그래서 세이브에 아예 없습니다.
// Level 은 세이브에 있습니다(public get). 하지만 set 이 private 이라 불러올 때 쓸 수 없어서 건너뜁니다. 초기값 1 이 남습니다.
// Level 의 private set 에 중단점을 걸어 보세요. Load 중에 한 번도 안 불립니다.
//
// [JA] Gold はプロパティではなくフィールドです。System.Text.Json は既定で public プロパティだけを扱い、フィールドは無視します。だからセーブにまったく入っていません。
// [JA] Level はセーブに入っています(public get)。しかし set が private なのでロード時に書き込めず読み飛ばします。初期値 1 が残ります。
// [JA] Level の private set にブレークポイントを置いてみてください。Load の間に一度も呼ばれません。
#endregion

#region 힌트 3 / ヒント 3
// 직렬화에 넣을 멤버를 명시하세요:
//   [JsonInclude] public int Gold;                              (또는 옵션의 IncludeFields = true)
//   [JsonInclude] public int Level { get; private set; } = 1;  (private set 도 [JsonInclude] 가 있으면 씁니다)
// 생성자로 받는 방법도 있습니다: [JsonConstructor] public Character(string name, int gold, int level).
// 골드만 고치면 레벨에서, 레벨만 고치면 골드에서 걸립니다. 숨은 테스트는 다른 값으로 둘 다 검사합니다.
// "저장은 됐는데 못 읽는 멤버"는 경고가 없습니다. 세이브 형식에는 왕복(저장 → 불러오기 → 비교) 테스트를 두세요.
//
// [JA] シリアル化に含めるメンバーを明示してください:
// [JA]   [JsonInclude] public int Gold;                              (またはオプションの IncludeFields = true)
// [JA]   [JsonInclude] public int Level { get; private set; } = 1;  (private set でも [JsonInclude] があれば書き込みます)
// [JA] コンストラクターで受け取る方法もあります: [JsonConstructor] public Character(string name, int gold, int level)。
// [JA] ゴールドだけ直すとレベルで、レベルだけ直すとゴールドで引っかかります。隠しテストは別の値で両方を検査します。
// [JA] 「保存はされたのに読めないメンバー」には警告がありません。セーブ形式には往復(保存 → ロード → 比較)テストを置いてください。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 다른 이름·골드·레벨로 검사한다. 보이는 값을 박아 넣으면 여기서 걸린다.
// [JA] 隠しケースは別の名前・ゴールド・レベルで検査します。見えている値を埋め込むとここで引っかかります。
#endregion
