using System.Text.RegularExpressions;

namespace BugHunt;

// 채팅 메시지마다 보낸 사람 이름이 규칙에 맞는지 검사한다. 결과는 맞는데, 채팅이 몰리면 서버 CPU 가 치솟는다.
// [JA] チャット メッセージごとに、送信者の名前が規則に合っているか検査します。結果は合っていますが、チャットが集中するとサーバーの CPU が跳ね上がります。
public static class Lab93
{
    // 이름 규칙: 영문자로 시작, 영문자·숫자·밑줄 3~16자
    static bool IsValidName(string name) => new Regex("^[A-Za-z][A-Za-z0-9_]{2,15}$").IsMatch(name);

    public static void Run()
    {
        string[] names = ["Kael", "Mira_02", "x", "9lives", "Doran", "dark_knight_99", "toolongname_123456", "Lia", "S v e n", "Yuna"];
        int messages = Pick(20_000, 10_000, 40_000);
        int offset = Pick(0, 0, names.Length - 1);
        IsValidName("warmup");   // 처음 한 번만 드는 비용 빼기

        int valid = 0;
        (long bytes, long ms) = Measure(() =>
        {
            for (int i = 0; i < messages; i++)
                if (IsValidName(names[(i + offset) % names.Length]))
                    valid++;
        });
        Console.WriteLine(Text.L($"  메시지 {messages:N0}개 중 이름이 올바른 것 {valid:N0}개. 할당 {bytes:N0}바이트 (메시지당 {bytes / messages}), {ms}ms", $"  メッセージ {messages:N0} 個中、名前が正しいもの {valid:N0} 個。割り当て {bytes:N0} バイト (メッセージあたり {bytes / messages})、{ms}ms"));

        int expected = 0;
        for (int i = 0; i < messages; i++)
        {
            string n = names[(i + offset) % names.Length];
            if (n.Length is >= 3 and <= 16 && char.IsAsciiLetter(n[0]) && n.All(c => char.IsAsciiLetterOrDigit(c) || c == '_'))
                expected++;
        }
        Expect(Text.L("올바른 이름 수", "正しい名前の数"), expected, valid);
        ExpectAtMost(Text.L("메시지당 할당 (바이트)", "メッセージあたりの割り当て (バイト)"), 64, bytes / messages);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 성능 프로파일러 → CPU 사용량. 핫 경로에 RegexParser, RegexWriter, RegexCompiler 같은 이름이 보이나요? 그것들은 무엇을 하는 코드일까요?
//
// [JA] パフォーマンス プロファイラー → CPU 使用率。ホット パスに RegexParser、RegexWriter、RegexCompiler のような名前が見えますか? それらは何をするコードでしょう?
#endregion

#region 힌트 2 / ヒント 2
// new Regex(...) 는 패턴 문자열을 해석해서 실행 가능한 형태로 "컴파일"합니다. 그 일을 메시지마다 다시 하고 있습니다.
// .NET 개체 할당 추적으로 보면 RegexNode, RegexCharClass 같은 객체가 메시지 수만큼 만들어집니다.
//
// [JA] new Regex(...) はパターン文字列を解析して実行可能な形に「コンパイル」します。その作業をメッセージごとにやり直しています。
// [JA] .NET オブジェクトの割り当ての追跡で見ると、RegexNode、RegexCharClass のようなオブジェクトがメッセージ数分作られています。
#endregion

#region 힌트 3 / ヒント 3
// 패턴이 바뀌지 않으면 한 번만 만드세요: static readonly Regex NameRule = new(...).
// 더 좋게는 [GeneratedRegex("...")] 소스 생성기로 빌드할 때 만들어 두는 방법도 있습니다.
// 같은 종류: 루프 안의 new JsonSerializerOptions(), new HttpClient(), CultureInfo.GetCultureInfo 반복 생성.
//
// [JA] パターンが変わらないなら一度だけ作ってください: static readonly Regex NameRule = new(...)。
// [JA] さらに良いのは、[GeneratedRegex("...")] ソース ジェネレーターでビルド時に作っておく方法です。
// [JA] 同じ種類: ループの中での new JsonSerializerOptions()、new HttpClient()、CultureInfo.GetCultureInfo の繰り返し生成。
#endregion
