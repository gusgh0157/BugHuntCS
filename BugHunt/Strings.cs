using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace BugHunt;

// 실행 출력의 언어 (한국어 / 일본어).
// 화면에 나오는 문구는 쓰는 자리에서 두 언어를 함께 적는다: Text.L("보석", "宝石"), Text.L($"보석 {n}", $"宝石 {n}").
// 그래서 소스를 읽는 사람은 어느 언어로든 그 줄이 무엇을 찍는지 바로 알 수 있다. 따로 떨어진 번역 표는 없다.
// 고르는 순서: --lang ja|ko > 환경 변수 BUGHUNT_LANG > CurrentUICulture > 한국어.
// CurrentCulture는 읽지도 바꾸지도 않는다. 문화권 랩(16, B-2)의 판정이 화면 언어에 따라 달라지면 안 되기 때문이다.
// 채점기(BugHunt.Grader)도 이 파일을 같이 컴파일해서 쓴다.
// [JA] 実行出力の言語(韓国語 / 日本語)。
// [JA] 画面に出る文言は、使う場所で二つの言語を一緒に書きます: Text.L("보석", "宝石")、Text.L($"보석 {n}", $"宝石 {n}")。
// [JA] なのでソースを読む人は、どちらの言語でもその行が何を出力するかがすぐ分かります。別に離れた翻訳表はありません。
// [JA] 選ぶ順番: --lang ja|ko > 環境変数 BUGHUNT_LANG > CurrentUICulture > 韓国語。
// [JA] CurrentCulture は読みも変えもしません。カルチャのラボ(16, B-2)の判定が表示言語で変わってはいけないからです。
// [JA] 採点器(BugHunt.Grader)もこのファイルを一緒にコンパイルして使います。
public static class Text
{
    public const string EnvName = "BUGHUNT_LANG";

    public static string Lang { get; private set; } = Detect(null);
    public static bool IsJa => Lang == "ja";

    // args에서 --lang X 를 빼고 언어를 정한다. 잘못된 값이면 null을 돌려준다
    public static string[]? TakeLangArg(string[] args)
    {
        int at = Array.IndexOf(args, "--lang");
        if (at < 0)
            return args;
        if (at + 1 >= args.Length || Normalize(args[at + 1]) == null)
            return null;
        Lang = Normalize(args[at + 1])!;
        return [.. args[..at], .. args[(at + 2)..]];
    }

    public static void SetLang(string lang) => Lang = Normalize(lang) ?? "ko";

    static string Detect(string? arg) =>
        Normalize(arg) ?? Normalize(Environment.GetEnvironmentVariable(EnvName))
        ?? (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ja" ? "ja" : "ko");

    static string? Normalize(string? lang) => lang?.Trim().ToLowerInvariant() switch
    {
        "ja" or "jp" or "ja-jp" => "ja",
        "ko" or "kr" or "ko-kr" => "ko",
        _ => null,
    };

    // 한국어와 일본어 중 지금 언어의 문구를 고른다.
    // [JA] 韓国語と日本語のうち、今の言語の文言を選びます。
    [DebuggerStepThrough]
    public static string L(string ko, string ja) => IsJa ? ja : ko;

    // 보간 문자열 두 개를 넘기면 고른 언어 쪽만 만든다. {} 안의 식도 그쪽에서 한 번만 계산한다.
    // (일반 $"..." 와 같이 CurrentCulture로 서식을 맞춘다.) F11로 들어가도 이 안으로는 들어오지 않는다.
    // [JA] 補間文字列を二つ渡すと、選んだ言語の方だけを作ります。{} の中の式もその方で一度だけ計算します。
    // [JA] (普通の $"..." と同じく CurrentCulture で書式を整えます。)F11 でもこの中には入りません。
    [DebuggerStepThrough]
    public static string L(ref KoText ko, ref JaText ja) => IsJa ? ja.ToStringAndClear() : ko.ToStringAndClear();

    // ---- 번역 빠짐 검사 ----

    public static bool HasHangul(string s)
    {
        foreach (char c in s)
            if (c is >= '가' and <= '힣' or >= 'ㄱ' and <= 'ㆎ')
                return true;
        return false;
    }

    // 이 표시가 붙은 줄의 한글 문자열은 두 언어를 이미 한 문자열에 같이 적은 것이라 검사하지 않는다.
    public const string BothLangMark = "// ko+ja";

    // 소스(.cs)에서 Text.L(...) 밖에 있는 한글 문자열 리터럴을 찾는다. 주석과 BothLangMark 줄은 건너뛴다.
    // 돌려주는 값: "파일:줄: 리터럴"
    // [JA] ソース(.cs)で Text.L(...) の外にあるハングルの文字列リテラルを探します。コメントと BothLangMark の行は飛ばします。
    public static List<string> FindUnpairedHangul(string root)
    {
        var found = new List<string>();
        foreach (string dir in new[] { "BugHunt", "BugHunt.Grader" })
        {
            string path = Path.Combine(root, dir);
            if (!Directory.Exists(path))
                continue;
            foreach (string file in Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
            {
                string rel = Path.GetRelativePath(root, file);
                if (rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(p => p is "bin" or "obj"))
                    continue;
                string src = File.ReadAllText(file);
                string[] lines = src.Split('\n');
                foreach ((int at, int length) in new LiteralScanner(src).UnpairedHangul())
                {
                    int line = src.AsSpan(0, at).Count('\n');
                    if (lines[line].Contains(BothLangMark))
                        continue;
                    found.Add($"{rel}:{line + 1}: {src.Substring(at, Math.Min(length, 80)).ReplaceLineEndings(" ")}");
                }
            }
        }
        return found;
    }

    // C# 소스를 훑으며 문자열 리터럴이 Text.L( ... ) 인자 안에 있는지 본다. (주석, 문자 리터럴, 보간 구멍, 원시 문자열 처리)
    sealed class LiteralScanner(string src)
    {
        int _i;
        readonly Stack<bool> _parens = new();   // 여는 괄호마다: 그 괄호가 Text.L( 인가
        readonly List<(int, int)> _found = [];

        public List<(int At, int Length)> UnpairedHangul()
        {
            Code(endAtBrace: false);
            return _found;
        }

        bool InsidePair => _parens.Contains(true);

        void Code(bool endAtBrace)
        {
            int depth = 0;
            while (_i < src.Length)
            {
                char c = src[_i];
                if (Starts("//") || (c == '#' && LineStartBefore(_i)))
                {
                    int nl = src.IndexOf('\n', _i);
                    _i = nl < 0 ? src.Length : nl;
                }
                else if (Starts("/*"))
                {
                    int end = src.IndexOf("*/", _i + 2, StringComparison.Ordinal);
                    _i = end < 0 ? src.Length : end + 2;
                }
                else if (c == '\'')
                {
                    _i++;
                    while (_i < src.Length && src[_i] != '\'')
                        _i += src[_i] == '\\' ? 2 : 1;
                    _i++;
                }
                else if (c == '"' || ((c == '$' || c == '@') && StringPrefixAt(_i)))
                {
                    Literal();
                }
                else if (c == '(')
                {
                    _parens.Push(src.AsSpan(0, _i).TrimEnd().EndsWith("Text.L", StringComparison.Ordinal));
                    _i++;
                }
                else if (c == ')')
                {
                    if (_parens.Count > 0) _parens.Pop();
                    _i++;
                }
                else if (c == '{')
                {
                    depth++;
                    _i++;
                }
                else if (c == '}')
                {
                    if (depth == 0 && endAtBrace)
                        return;
                    depth--;
                    _i++;
                }
                else
                {
                    _i++;
                }
            }
        }

        void Literal()
        {
            int start = _i, dollars = 0;
            bool verbatim = false;
            while (src[_i] is '$' or '@')
            {
                if (src[_i] == '$') dollars++; else verbatim = true;
                _i++;
            }
            int quotes = 0;
            while (_i + quotes < src.Length && src[_i + quotes] == '"')
                quotes++;
            bool hangul = false;
            if (quotes >= 3)
            {
                string close = new('"', quotes), open = new('{', Math.Max(dollars, 1));
                _i += quotes;
                while (_i < src.Length && !Starts(close))
                {
                    if (dollars > 0 && Starts(open))
                    {
                        _i += dollars;
                        Hole(dollars);
                        continue;
                    }
                    hangul |= IsHangul(src[_i]);
                    _i++;
                }
                _i += quotes;
            }
            else
            {
                _i++;
                while (_i < src.Length)
                {
                    char c = src[_i];
                    if (c == '\\' && !verbatim) { _i += 2; continue; }
                    if (c == '"')
                    {
                        if (verbatim && Starts("\"\"")) { _i += 2; continue; }
                        _i++;
                        break;
                    }
                    if (dollars > 0 && c == '{')
                    {
                        if (Starts("{{")) { _i += 2; continue; }
                        _i++;
                        Hole(1);
                        continue;
                    }
                    hangul |= IsHangul(c);
                    _i++;
                }
            }
            if (hangul && !InsidePair)
                _found.Add((start, _i - start));
        }

        // 보간 구멍 { ... } 은 코드다. 닫는 } 를 braces 개 건너뛴다
        void Hole(int braces)
        {
            Code(endAtBrace: true);
            _i += braces;
        }

        bool Starts(string s) => string.CompareOrdinal(src, _i, s, 0, s.Length) == 0;

        bool StringPrefixAt(int i)
        {
            while (i < src.Length && src[i] is '$' or '@')
                i++;
            return i < src.Length && src[i] == '"';
        }

        bool LineStartBefore(int i)
        {
            int nl = src.LastIndexOf('\n', Math.Max(i - 1, 0));
            return src.AsSpan(nl + 1, i - nl - 1).IsWhiteSpace();
        }

        static bool IsHangul(char c) => c is >= '가' and <= '힣' or >= 'ㄱ' and <= 'ㆎ';
    }

    // ---- 보간 문자열 핸들러 (Text.L 전용) ----
    // 고른 언어가 아니면 shouldAppend = false 라서 컴파일러가 그쪽 구멍의 식을 아예 계산하지 않는다.

    [InterpolatedStringHandler, DebuggerStepThrough]
    public ref struct KoText
    {
        DefaultInterpolatedStringHandler _inner;

        public KoText(int literalLength, int formattedCount, out bool shouldAppend)
        {
            shouldAppend = !IsJa;
            // 고르지 않은 쪽은 버퍼도 빌리지 않는다 (Append* 와 ToStringAndClear 가 불리지 않는다)
            _inner = shouldAppend ? new DefaultInterpolatedStringHandler(literalLength, formattedCount) : default;
        }

        public void AppendLiteral(string value) => _inner.AppendLiteral(value);
        public void AppendFormatted<T>(T value) => _inner.AppendFormatted(value);
        public void AppendFormatted<T>(T value, string? format) => _inner.AppendFormatted(value, format);
        public void AppendFormatted<T>(T value, int alignment) => _inner.AppendFormatted(value, alignment);
        public void AppendFormatted<T>(T value, int alignment, string? format) => _inner.AppendFormatted(value, alignment, format);
        public void AppendFormatted(string? value) => _inner.AppendFormatted(value);
        public void AppendFormatted(string? value, int alignment = 0, string? format = null) => _inner.AppendFormatted(value, alignment, format);
        public string ToStringAndClear() => _inner.ToStringAndClear();
    }

    [InterpolatedStringHandler, DebuggerStepThrough]
    public ref struct JaText
    {
        DefaultInterpolatedStringHandler _inner;

        public JaText(int literalLength, int formattedCount, out bool shouldAppend)
        {
            shouldAppend = IsJa;
            // 고르지 않은 쪽은 버퍼도 빌리지 않는다 (Append* 와 ToStringAndClear 가 불리지 않는다)
            _inner = shouldAppend ? new DefaultInterpolatedStringHandler(literalLength, formattedCount) : default;
        }

        public void AppendLiteral(string value) => _inner.AppendLiteral(value);
        public void AppendFormatted<T>(T value) => _inner.AppendFormatted(value);
        public void AppendFormatted<T>(T value, string? format) => _inner.AppendFormatted(value, format);
        public void AppendFormatted<T>(T value, int alignment) => _inner.AppendFormatted(value, alignment);
        public void AppendFormatted<T>(T value, int alignment, string? format) => _inner.AppendFormatted(value, alignment, format);
        public void AppendFormatted(string? value) => _inner.AppendFormatted(value);
        public void AppendFormatted(string? value, int alignment = 0, string? format = null) => _inner.AppendFormatted(value, alignment, format);
        public string ToStringAndClear() => _inner.ToStringAndClear();
    }
}
