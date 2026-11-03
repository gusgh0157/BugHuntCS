namespace BugHunt;

// 서버에서 받은 설정 문자열을 파싱해 파티 초대 기능을 켠다.
// [JA] サーバーから受け取った設定文字列を解析して、パーティー招待機能をオンにします。
public static class Lab04
{
    const string ServerConfig = """
        maxPartySize=7
        pvpEnabled=true
        motd=Welcome to season 3
        """;

    // JSON 파서처럼, 숫자는 전부 가장 큰 정수형(long)으로 담는다.
    static Dictionary<string, object> ParseConfig(string text)
    {
        var config = new Dictionary<string, object>();
        foreach (string line in text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            string[] kv = line.Split('=', 2);
            object value;
            if (long.TryParse(kv[1], out long number)) value = number;
            else if (bool.TryParse(kv[1], out bool flag)) value = flag;
            else value = kv[1];
            config[kv[0]] = value;
        }
        return config;
    }

    class Party(int maxSize)
    {
        public List<string> Members { get; } = [];

        public bool Invite(string name)
        {
            if (Members.Count >= maxSize) return false;
            Members.Add(name);
            return true;
        }
    }

    static Party CreateParty(Dictionary<string, object> config)
    {
        int maxSize = (int)config["maxPartySize"];
        return new Party(maxSize);
    }

    public static void Run()
    {
        Dictionary<string, object> config = ParseConfig(ServerConfig);
        Console.WriteLine($"  공지: {config["motd"]}");
        Party party = CreateParty(config);

        string[] friends = ["Kael", "Mira", "Doran", "Selene", "Bram", "Ivy", "Rook", "Tess"];
        foreach (string friend in friends)
            party.Invite(friend);

        Expect("파티 인원 (최대 7명)", 7, party.Members.Count);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 예외 메시지를 읽어 보세요. 어떤 형식을 어떤 형식으로 바꾸지 못했다고 하나요?
// 멈춘 줄에서 조사식에 config["maxPartySize"] 와 config["maxPartySize"].GetType() 을 넣어 보세요.
//
// [JA] 例外メッセージを読んでみてください。どの型をどの型に変換できなかったと言っていますか?
// [JA] 止まった行で、ウォッチに config["maxPartySize"] と config["maxPartySize"].GetType() を入れてみてください。
#endregion

#region 힌트 2 / ヒント 2
// long 7 은 int 로 바꿀 수 있습니다. (int)7L 은 잘 됩니다.
// 그런데 object 안에 들어 있는(박싱된) long 을 (int)로 꺼내는 것은 다른 연산입니다.
// 박싱 해제(unboxing)는 "정확히 같은 형식"으로만 할 수 있습니다.
//
// [JA] long の 7 は int に変換できます。(int)7L はちゃんと動きます。
// [JA] しかし object の中に入っている(ボックス化された)long を (int) で取り出すのは別の演算です。
// [JA] ボックス化解除(unboxing)は「まったく同じ型」でしかできません。
#endregion
