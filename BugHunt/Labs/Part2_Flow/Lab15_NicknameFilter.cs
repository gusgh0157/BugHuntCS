namespace BugHunt;

// 닉네임을 등록하기 전에 앞뒤 공백을 없애고, 금칙어는 *** 로 가린다.
public static class Lab15
{
    static readonly string[] BannedWords = ["noob", "hack"];

    static string Sanitize(string nickname)
    {
        foreach (string word in BannedWords)
        {
            if (nickname.Contains(word, StringComparison.OrdinalIgnoreCase))
            {
                nickname.Replace(word, "***", StringComparison.OrdinalIgnoreCase);
                Console.WriteLine($"  금칙어 \"{word}\" 가림");
            }
        }
        return nickname.Trim();
    }

    public static void Run()
    {
        string[] requested = ["  NoobSlayer ", "Kael", "xXHackerXx"];
        var registered = new List<string>();
        foreach (string name in requested)
        {
            string clean = Sanitize(name);
            Console.WriteLine($"  \"{name}\" → \"{clean}\"");
            registered.Add(clean);
        }

        Expect("등록된 닉네임", "***Slayer, Kael, xX***erXx", string.Join(", ", registered));
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// 콘솔에는 "금칙어 가림"이 찍혔습니다. 그럼 가리는 줄은 실행됐습니다.
// 그 줄에 중단점을 걸고 F10 으로 넘긴 뒤, 지역 창에서 nickname 이 바뀌었는지 보세요.
#endregion

#region 힌트 2
// 공백은 잘 지워졌습니다. Trim() 과 Replace() 를 쓴 방식은 무엇이 다른가요?
// F10 으로 Replace 줄을 넘기면 자동 창(Ctrl+Alt+V, A)에 "String.Replace 반환" 값이 잠깐 보입니다.
// 그 값은 어디로 갔나요?
#endregion

#region 힌트 3 (분석기)
// string 은 불변입니다. Replace 는 원본을 바꾸지 않고 새 문자열을 돌려줍니다.
// 이런 실수는 분석기가 잡아 줄 수 있습니다. BugHunt 폴더에 .editorconfig 를 만들고 아래 두 줄을 넣은 뒤 빌드해 보세요.
//   [*.cs]
//   dotnet_diagnostic.CA1806.severity = warning
#endregion
