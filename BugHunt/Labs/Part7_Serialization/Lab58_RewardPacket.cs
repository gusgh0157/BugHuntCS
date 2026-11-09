using System.Text.Json;

namespace BugHunt;

// 보상 서버가 보낸 우편 패킷(JSON)을 받아 가방에 넣는다. 형식이 틀린 패킷은 거절하고 다시 보내 달라고 한다.
// 옛 버전 서버가 섞여 있어서 가끔 이상한 패킷이 오는데, 거절은 하나도 안 되고 "x0" 짜리 보상이 들어온다.
// [JA] 報酬サーバーが送ったメールパケット(JSON)を受け取ってバッグに入れます。形式が間違っているパケットは拒否して再送を頼みます。
// [JA] 旧バージョンのサーバーが混ざっていて時々おかしなパケットが来るのに、一つも拒否されず「x0」の報酬が入ってきます。
public static partial class Lab58
{
    sealed class MailPacket
    {
        public string Item { get; set; } = "";
        public int Count { get; set; }
    }

    static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    // 형식이 틀리면 null (거절하고 재전송 요청)
    static MailPacket? Decode(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<MailPacket>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static void Run()
    {
        (string Json, bool Valid)[] packets = Pick(Batches[0], Batches[1..]);

        var bag = new List<string>();
        int rejected = 0;
        foreach ((string json, _) in packets)
        {
            MailPacket? p = Decode(json);
            if (p == null)
            {
                rejected++;
                Console.WriteLine(Text.L($"  거절: {json}", $"  拒否: {json}"));
                continue;
            }
            bag.Add($"{p.Item ?? "(null)"} x{p.Count}");
            Console.WriteLine(Text.L($"  받음: {json} → {bag[^1]}", $"  受信: {json} → {bag[^1]}"));
        }

        int invalid = packets.Count(p => !p.Valid);
        Expect(Text.L("거절한 패킷 수", "拒否したパケット数"), invalid, rejected);
        Expect(Text.L("가방에 들어온 보상 수", "バッグに入った報酬の数"), packets.Length - invalid, bag.Count);
        Expect(Text.L("수량 0 이나 이름 없는 보상이 들어오지 않음", "数量 0 や名前のない報酬が入ってこない"), false, bag.Any(b => b.EndsWith(" x0") || b.StartsWith("(null)") || b.StartsWith(" x")));
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 콘솔의 "받음"/"거절" 줄을 패킷 JSON 과 나란히 보세요. "count": null 인 패킷은 거절됐습니다. 그런데 "count" 가 아예 없는 패킷은요?
// Decode 의 return 에 중단점을 걸고, count 가 없는 패킷일 때 반환값을 펼쳐 보세요.
//
// [JA] コンソールの「받음」(受信)/「거절」(拒否)の行をパケットの JSON と並べて見てください。"count": null のパケットは拒否されました。では "count" がまったくないパケットは?
// [JA] Decode の return にブレークポイントを置き、count がないパケットのときに戻り値を展開してみてください。
#endregion

#region 힌트 2 / ヒント 2
// System.Text.Json 은 JSON 에 없는 속성을 "그냥 안 건드립니다". 그래서 Count 는 int 의 기본값 0, Item 은 초기값 "" 로 남고 예외는 없습니다. "없음(missing)"과 "0"을 구별하지 못합니다.
// 반면 "count": null 은 int 에 null 을 넣을 수 없어서 JsonException 이 납니다. 같은 "값이 없다"인데 JSON 의 모양에 따라 결과가 다릅니다.
// 그리고 "item": null 은? string 은 null 을 받을 수 있는 형식이라, nullable 경고로 null 이 아니라고 약속한 속성에도 null 이 그대로 들어옵니다.
//
// [JA] System.Text.Json は JSON にないプロパティを「何も触りません」。そのため Count は int の既定値 0、Item は初期値 "" のまま残り、例外は出ません。「欠落(missing)」と「0」を区別できません。
// [JA] 一方 "count": null は int に null を入れられないので JsonException になります。同じ「値がない」なのに JSON の形によって結果が違います。
// [JA] では "item": null は? string は null を受け取れる型なので、nullable の警告で null ではないと約束したプロパティにも null がそのまま入ってきます。
#endregion

#region 힌트 3 / ヒント 3
// 1) 꼭 있어야 하는 속성에 C# 의 required 를 붙이세요: public required string Item { get; set; }, public required int Count { get; set; }
//    System.Text.Json 은 required 멤버가 JSON 에 없으면 JsonException 을 냅니다. ([JsonRequired] 특성도 같은 뜻입니다.)
// 2) required 는 "있는가"만 봅니다. "item": null 은 있으니 통과합니다. null 까지 막으려면 옵션에 RespectNullableAnnotations = true (.NET 9+) 를 켜세요.
//    그러면 null 이 아니라고 선언한 string 에 null 이 오면 JsonException 입니다.
// required 만 붙이면 숨은 케이스("item": null, item 이 없는 패킷)에서 걸립니다. 받은 뒤에 Count == 0 을 거르는 수정은 "정말 0개" 보상을 구별 못 하고, 이름 없는 보상도 못 거릅니다.
//
// [JA] 1) 必ずあるべきプロパティに C# の required を付けてください: public required string Item { get; set; }, public required int Count { get; set; }
// [JA]    System.Text.Json は required メンバーが JSON にないと JsonException を出します。([JsonRequired] 属性も同じ意味です。)
// [JA] 2) required は「あるか」だけを見ます。"item": null はあるので通ります。null まで防ぐにはオプションで RespectNullableAnnotations = true (.NET 9+) を有効にしてください。
// [JA]    すると null ではないと宣言した string に null が来たら JsonException です。
// [JA] required だけ付けると隠しケース("item": null、item がないパケット)で引っかかります。受け取ったあとで Count == 0 を除外する修正は「本当に0個」の報酬を区別できず、名前のない報酬も除外できません。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 item 이 null 이거나 빠진 패킷, 다른 아이템과 수량으로 검사한다.
// [JA] 隠しケースは item が null または欠落したパケット、別のアイテムと数量で検査します。
public static partial class Lab58
{
    static readonly (string, bool)[][] Batches =
    [
        [
            ($$"""{"item":"{{Text.L("회복 물약", "回復ポーション")}}","count":3}""", true),
            ($$"""{"item":"{{Text.L("마나 물약", "マナポーション")}}"}""", false),
            ($$"""{"item":"{{Text.L("화살", "矢")}}","count":null}""", false),
            ($$"""{"item":"{{Text.L("철광석", "鉄鉱石")}}","count":12}""", true),
        ],
        [
            ($$"""{"item":"{{Text.L("용의 비늘", "竜のうろこ")}}","count":1}""", true),
            ("""{"item":null,"count":5}""", false),
            ("""{"count":2}""", false),
            ($$"""{"item":"{{Text.L("빵", "パン")}}"}""", false),
        ],
        [
            ($$"""{"item":"{{Text.L("화살", "矢")}}","count":99}""", true),
            ("""{"count":7}""", false),
            ($$"""{"item":"{{Text.L("마나 물약", "マナポーション")}}","count":4}""", true),
        ],
        [
            ("""{"item":null,"count":1}""", false),
            ($$"""{"item":"{{Text.L("회복 물약", "回復ポーション")}}","count":2}""", true),
            ($$"""{"item":"{{Text.L("철광석", "鉄鉱石")}}","count":null}""", false),
            ($$"""{"item":"{{Text.L("빵", "パン")}}","count":6}""", true),
            ($$"""{"item":"{{Text.L("영웅의 증표", "英雄の証")}}"}""", false),
        ],
    ];
}
#endregion
