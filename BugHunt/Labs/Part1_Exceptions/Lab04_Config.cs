namespace BugHunt;

// 서버에서 받은 설정 문자열을 파싱해 파티 초대 기능을 켠다.
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
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// 예외 메시지를 읽어 보세요. 어떤 형식을 어떤 형식으로 바꾸지 못했다고 하나요?
// 멈춘 줄에서 조사식에 config["maxPartySize"] 와 config["maxPartySize"].GetType() 을 넣어 보세요.
#endregion

#region 힌트 2
// long 7 은 int 로 바꿀 수 있습니다. (int)7L 은 잘 됩니다.
// 그런데 object 안에 들어 있는(박싱된) long 을 (int)로 꺼내는 것은 다른 연산입니다.
// 박싱 해제(unboxing)는 "정확히 같은 형식"으로만 할 수 있습니다.
#endregion
