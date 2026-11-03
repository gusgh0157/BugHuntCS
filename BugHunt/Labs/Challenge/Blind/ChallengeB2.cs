using System.Globalization;

namespace BugHunt;

// 채팅 명령 처리. 대소문자를 가리지 않게 대문자로 바꿔서 명령표에서 찾는다.
// [JA] チャット コマンドの処理。大文字・小文字を区別しないように大文字に変えてからコマンド表で探します。
public static class ChallengeB2
{
    static readonly Dictionary<string, string> Commands = new()
    {
        ["/QUIT"] = "종료",
        ["/INVITE"] = "초대",
        ["/WHISPER"] = "귓속말",
        ["/LIST"] = "목록",
        ["/HELP"] = "도움말",
        ["/MAP"] = "지도",
    };

    static string Handle(string input)
    {
        string key = input.Trim().Split(' ')[0].ToUpper();
        return Commands.TryGetValue(key, out string? action) ? action : "알 수 없는 명령";
    }

    public static void Run()
    {
        CultureInfo saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(Pick("tr-TR", "tr-TR", "az-Latn-AZ"));
            Console.WriteLine($"  플레이어 문화권: {CultureInfo.CurrentCulture.Name}");

            string[] typed = Seed == 0
                ? ["/quit", "/map", "/Invite Kael", "/help"]
                : [.. Enumerable.Range(0, 8).Select(_ => Pick("/quit", "/QUIT", "/list", "/whisper Mira hi", "/Help", "/map", "/invite Doran", "/LIST"))];

            foreach (string input in typed)
            {
                string expected = Commands.First(c => c.Key.Equals(input.Split(' ')[0], StringComparison.OrdinalIgnoreCase)).Value;
                Expect($"{input} →", expected, Handle(input));
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }
}
