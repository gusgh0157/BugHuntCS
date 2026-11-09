using System.Text.Json;

namespace BugHunt;

// 길드 공지를 서버(웹 API)에서 JSON 으로 받아 화면에 띄운다. 예외도 경고도 없는데 공지가 전부 빈 칸이다.
// [JA] ギルドのお知らせをサーバー(Web API)から JSON で受け取って画面に表示します。例外も警告もないのに、お知らせがすべて空欄です。
public static partial class Lab54
{
    sealed class GuildNotice
    {
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public int Priority { get; set; }
        public bool Pinned { get; set; }
    }

    // 클라이언트 공용 JSON 설정
    static readonly JsonSerializerOptions Options = new() { WriteIndented = false };

    static GuildNotice Receive(string json) =>
        JsonSerializer.Deserialize<GuildNotice>(json, Options) ?? throw new FormatException(Text.L("빈 공지", "空のお知らせ"));

    public static void Run()
    {
        (string Title, string Author, int Priority, bool Pinned) n = Pick(Notices[0], Notices[1..]);

        // 서버(웹 API)가 보낸 응답. 웹 API 관례대로 camelCase 다.
        string json = $$"""{"title":"{{n.Title}}","author":"{{n.Author}}","priority":{{n.Priority}},"pinned":{{(n.Pinned ? "true" : "false")}}}""";
        Console.WriteLine(Text.L($"  받은 JSON: {json}", $"  受け取った JSON: {json}"));

        GuildNotice notice = Receive(json);
        Console.WriteLine(Text.L($"  화면: [{(notice.Pinned ? "고정" : "일반")}] {notice.Title} - {notice.Author} (중요도 {notice.Priority})", $"  画面: [{(notice.Pinned ? "固定" : "通常")}] {notice.Title} - {notice.Author} (重要度 {notice.Priority})"));

        Expect(Text.L("공지 제목", "お知らせのタイトル"), n.Title, notice.Title);
        Expect(Text.L("작성자", "作成者"), n.Author, notice.Author);
        Expect(Text.L("중요도", "重要度"), n.Priority, notice.Priority);
        Expect(Text.L("고정 공지", "固定のお知らせ"), n.Pinned, notice.Pinned);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// GuildNotice 의 Title 속성 set 접근자에 중단점을 거세요(set 쪽에 커서를 두고 F9). Deserialize 중에 불리나요?
// 받은 JSON 의 이름 "title" 과 C# 속성 이름 Title 을 글자 하나씩 비교해 보세요.
//
// [JA] GuildNotice の Title プロパティの set アクセサーにブレークポイントを置いてください(set 側にカーソルを置いて F9)。Deserialize の途中で呼ばれますか?
// [JA] 受け取った JSON の名前 "title" と C# のプロパティ名 Title を一文字ずつ比べてみてください。
#endregion

#region 힌트 2 / ヒント 2
// System.Text.Json 은 기본적으로 이름을 대소문자까지 정확히 비교합니다. "title" 은 Title 과 다른 이름으로 보고, 모르는 이름은 조용히 건너뜁니다.
// 그래서 모든 속성이 초기값("" 과 0, false)으로 남습니다. 예외는 나지 않습니다. "모르는 속성은 무시"가 기본값이기 때문입니다.
// 반대로 JsonSerializerDefaults.Web 으로 만든 옵션(ASP.NET Core 의 기본)은 camelCase 와 대소문자 무시를 켭니다. 랩 C-1 의 Wire 클래스가 그렇게 씁니다.
//
// [JA] System.Text.Json は既定で名前を大文字小文字まで正確に比較します。"title" は Title と別の名前と見なし、知らない名前は黙って読み飛ばします。
// [JA] そのためすべてのプロパティが初期値("" と 0、false)のまま残ります。例外は出ません。「知らないプロパティは無視」が既定だからです。
// [JA] 逆に JsonSerializerDefaults.Web で作ったオプション(ASP.NET Core の既定)は camelCase と大文字小文字の無視を有効にします。チャレンジ C-1 の Wire クラスがそう使っています。
#endregion

#region 힌트 3 / ヒント 3
// 옵션을 상대와 맞추세요(택1):
//   new JsonSerializerOptions(JsonSerializerDefaults.Web)          (camelCase + 대소문자 무시 + 숫자 문자열 허용)
//   new JsonSerializerOptions { PropertyNameCaseInsensitive = true } 또는 PropertyNamingPolicy = JsonNamingPolicy.CamelCase
//   속성마다 [JsonPropertyName("title")]
// 이름이 안 맞아도 조용히 넘어가는 것이 더 큰 문제입니다. .NET 8 부터는 UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow 로
// "모르는 속성이 오면 JsonException" 을 켤 수 있습니다. 서버와 형식을 맞추는 동안 켜 두면 이런 버그가 바로 예외로 드러납니다.
//
// [JA] オプションを相手に合わせてください(いずれか):
// [JA]   new JsonSerializerOptions(JsonSerializerDefaults.Web)          (camelCase + 大文字小文字の無視 + 数値の文字列を許可)
// [JA]   new JsonSerializerOptions { PropertyNameCaseInsensitive = true } または PropertyNamingPolicy = JsonNamingPolicy.CamelCase
// [JA]   プロパティごとに [JsonPropertyName("title")]
// [JA] 名前が合わなくても黙って通ってしまうことのほうが大きな問題です。.NET 8 からは UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow で
// [JA] 「知らないプロパティが来たら JsonException」を有効にできます。サーバーと形式を合わせている間有効にしておくと、この種のバグがすぐ例外として表れます。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 다른 공지(제목·작성자·중요도·고정 여부)로 검사한다. 보이는 값을 박아 넣거나 Title 만 직접 꺼내면 여기서 걸린다.
// [JA] 隠しケースは別のお知らせ(タイトル・作成者・重要度・固定の有無)で検査します。見えている値を埋め込んだり Title だけ直接取り出したりするとここで引っかかります。
public static partial class Lab54
{
    static readonly (string, string, int, bool)[] Notices =
    [
        (Text.L("이번 주 레이드는 토요일 9시", "今週のレイドは土曜21時"), Text.L("길드장 아린", "ギルドマスター アリン"), 3, true),
        (Text.L("창고 정리 부탁드립니다", "倉庫の整理をお願いします"), Text.L("부길드장 케인", "サブマスター ケイン"), 1, false),
        (Text.L("신규 길드원 환영회", "新メンバー歓迎会"), Text.L("총무 미라", "会計係 ミラ"), 2, true),
        (Text.L("길드 전쟁 참가 신청", "ギルド戦の参加申し込み"), Text.L("길드장 아린", "ギルドマスター アリン"), 5, false),
    ];
}
#endregion
