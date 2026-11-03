using System.Text.Encodings.Web;
using System.Text.Json;

namespace BugHunt;

// 우편에 보상을 첨부해 보낸다. 보상은 골드·아이템 등 여러 종류이고 모두 Reward 를 상속한다.
// 서버가 우편을 JSON 으로 저장했다가 받는 사람에게 돌려주면, 첨부가 전부 "알 수 없는 보상"이 된다.
// [JA] メールに報酬を添付して送ります。報酬はゴールド・アイテムなど何種類もあり、すべて Reward を継承しています。
// [JA] サーバーがメールを JSON で保存してから受取人に返すと、添付がすべて「不明な報酬」になります。
public static partial class Lab108
{
    class Reward
    {
        public string Source { get; set; } = "";
        public virtual string Describe() => "알 수 없는 보상";
    }

    sealed class GoldReward : Reward
    {
        public int Amount { get; set; }
        public override string Describe() => $"{Amount}G";
    }

    sealed class ItemReward : Reward
    {
        public string ItemName { get; set; } = "";
        public int Count { get; set; }
        public override string Describe() => $"{ItemName} x{Count}";
    }

    sealed class ExpReward : Reward
    {
        public int Points { get; set; }
        public override string Describe() => $"경험치 {Points}";
    }

    sealed class Mail
    {
        public string To { get; set; } = "";
        public List<Reward> Attachments { get; set; } = [];
    }

    // 한글을 이스케이프하지 않고 그대로 쓴다(콘솔에서 읽기 쉽게).
    static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    static Mail RoundTrip(Mail mail)
    {
        string json = JsonSerializer.Serialize(mail, Options);
        Console.WriteLine($"  서버 저장: {json}");
        return JsonSerializer.Deserialize<Mail>(json, Options) ?? throw new FormatException("빈 우편");
    }

    public static void Run()
    {
        Reward[] attachments = Pick(Bundles[0], Bundles[1..]);
        var mail = new Mail { To = "아린", Attachments = [.. attachments] };
        Console.WriteLine($"  보낸 첨부: {string.Join(", ", mail.Attachments.Select(r => r.Describe()))}");

        Mail received = RoundTrip(mail);
        Console.WriteLine($"  받은 첨부: {string.Join(", ", received.Attachments.Select(r => r.Describe()))}");

        Expect("첨부 개수", mail.Attachments.Count, received.Attachments.Count);
        Expect("첨부 내용", string.Join(", ", mail.Attachments.Select(r => r.Describe())), string.Join(", ", received.Attachments.Select(r => r.Describe())));
        Expect("보상 출처", string.Join(",", mail.Attachments.Select(r => r.Source)), string.Join(",", received.Attachments.Select(r => r.Source)));
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 콘솔의 "서버 저장" 줄을 보세요. 골드 보상의 amount 가 JSON 에 있나요? 첨부마다 무엇이 저장됐나요?
// RoundTrip 이 끝난 뒤 조사식에 received.Attachments[0].GetType() 을 넣어 보세요.
//
// [JA] コンソールの「서버 저장」(サーバー保存)の行を見てください。ゴールド報酬の amount は JSON にありますか? 添付ごとに何が保存されましたか?
// [JA] RoundTrip が終わったあと、ウォッチに received.Attachments[0].GetType() を入れてみてください。
#endregion

#region 힌트 2 / ヒント 2
// System.Text.Json 은 "선언된 형식"으로 직렬화합니다. Attachments 는 List<Reward> 라서 각 항목을 Reward 로 보고, Reward 에 있는 Source 만 씁니다. 파생 형식의 Amount, ItemName 은 빠집니다.
// 읽을 때도 JSON 에 "어느 파생 형식인지"가 없으니 전부 Reward 로 만듭니다. 그래서 Describe 가 기반의 "알 수 없는 보상"을 돌려줍니다.
// (Newtonsoft.Json 의 TypeNameHandling 과 달리, 형식 이름을 JSON 에 넣어 아무 형식이나 만들게 하는 기능은 보안상 일부러 없습니다.)
//
// [JA] System.Text.Json は「宣言された型」でシリアル化します。Attachments は List<Reward> なので各項目を Reward と見なし、Reward にある Source だけを書きます。派生型の Amount や ItemName は抜けます。
// [JA] 読むときも JSON に「どの派生型か」がないので、すべて Reward として作ります。そのため Describe が基底の「알 수 없는 보상」(不明な報酬)を返します。
// [JA] (Newtonsoft.Json の TypeNameHandling と違い、型名を JSON に入れて任意の型を作らせる機能はセキュリティ上わざと用意されていません。)
#endregion

#region 힌트 3 / ヒント 3
// 기반 형식에 다형성을 선언하세요. 허용할 파생 형식을 하나씩 등록합니다:
//   [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
//   [JsonDerivedType(typeof(GoldReward), "gold")]
//   [JsonDerivedType(typeof(ItemReward), "item")]
//   [JsonDerivedType(typeof(ExpReward), "exp")]
//   class Reward { ... }
// 그러면 JSON 에 "$type":"gold" 가 들어가고, 읽을 때 그 값으로 파생 형식을 고릅니다.
// 기본 케이스에 보이는 두 종류만 등록하면, 등록하지 않은 파생 형식(숨은 케이스의 경험치 보상)을 직렬화할 때 NotSupportedException 이 납니다. 보상 종류를 전부 등록하세요.
//
// [JA] 基底型にポリモーフィズムを宣言してください。許可する派生型を一つずつ登録します:
// [JA]   [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
// [JA]   [JsonDerivedType(typeof(GoldReward), "gold")]
// [JA]   [JsonDerivedType(typeof(ItemReward), "item")]
// [JA]   [JsonDerivedType(typeof(ExpReward), "exp")]
// [JA]   class Reward { ... }
// [JA] すると JSON に "$type":"gold" が入り、読むときにその値で派生型を選びます。
// [JA] 基本ケースで見える二種類だけ登録すると、登録していない派生型(隠しケースの経験値報酬)をシリアル化するときに NotSupportedException が出ます。報酬の種類をすべて登録してください。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 경험치 보상이 섞인 다른 첨부 묶음으로 검사한다. 보이는 두 종류만 등록하거나 첨부를 따로 전달하면 여기서 걸린다.
// [JA] 隠しケースは経験値報酬が混ざった別の添付の組み合わせで検査します。見えている二種類だけ登録したり、添付を別に渡したりするとここで引っかかります。
public static partial class Lab108
{
    static readonly Reward[][] Bundles =
    [
        [new GoldReward { Amount = 500, Source = "주간 퀘스트" }, new ItemReward { ItemName = "회복 물약", Count = 3, Source = "주간 퀘스트" }],
        [new ExpReward { Points = 1200, Source = "이벤트" }, new GoldReward { Amount = 50, Source = "출석" }],
        [new ItemReward { ItemName = "용의 비늘", Count = 1, Source = "레이드" }, new ExpReward { Points = 300, Source = "레이드" }, new GoldReward { Amount = 999, Source = "레이드" }],
        [new ExpReward { Points = 75, Source = "튜토리얼" }],
    ];
}
#endregion
