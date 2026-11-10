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
    // [JA] args から --lang X を取り除いて言語を決めます。不正な値なら null を返します
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
    // 숫자 서식은 일반 $"..." 와 똑같이 CurrentCulture 를 따른다(이 클래스가 CurrentCulture 를 바꾸지는 않는다).
    // 문화권 랩의 출력이 Text.L 을 쓰기 전과 같아야 하므로 InvariantCulture 로 바꾸지 말 것. 고정 서식이 필요한 값은 그 자리에서 ToString(CultureInfo.InvariantCulture) 로.
    // F11로 들어가도 이 안으로는 들어오지 않는다.
    // [JA] 補間文字列を二つ渡すと、選んだ言語の方だけを作ります。{} の中の式もその方で一度だけ計算します。
    // [JA] 数値の書式は普通の $"..." とまったく同じく CurrentCulture に従います(このクラスが CurrentCulture を変えることはありません)。
    // [JA] カルチャのラボの出力が Text.L を使う前と同じである必要があるので、InvariantCulture に変えないでください。固定の書式が必要な値は、その場で ToString(CultureInfo.InvariantCulture) で。
    // [JA] F11 でもこの中には入りません。
    [DebuggerStepThrough]
    public static string L(ref KoText ko, ref JaText ja) => IsJa ? ja.ToStringAndClear() : ko.ToStringAndClear();

    // ---- 번역 빠짐 검사 ----
    // [JA] ---- 翻訳漏れチェック ----

    public static bool HasHangul(string s)
    {
        foreach (char c in s)
            if (c is >= '가' and <= '힣' or >= 'ㄱ' and <= 'ㆎ')
                return true;
        return false;
    }

    // 이 표시가 붙은 줄의 한글 문자열은 두 언어를 이미 한 문자열에 같이 적은 것이라 검사하지 않는다.
    // [JA] この印が付いた行のハングル文字列は、二つの言語をすでに一つの文字列に一緒に書いたものなのでチェックしません。
    public const string BothLangMark = "// ko+ja";

    // 소스(.cs)에서 Text.L(...) 밖에 있거나 일본어 쪽(둘째 인자)에 들어간 한글 문자열 리터럴을 찾는다. 주석과 BothLangMark 줄은 건너뛴다.
    // 일본어 짝이 없는 한글 주석도 찾는다 (UnpairedComments).
    // 돌려주는 값: "파일:줄: 리터럴" (일본어 쪽이면 앞에 "[ja] ", 주석이면 앞에 CommentTag)
    // [JA] ソース(.cs)で Text.L(...) の外にある、または日本語側(2 番目の引数)に入ったハングルの文字列リテラルを探します。コメントと BothLangMark の行は飛ばします。
    // [JA] 日本語のペアがないハングルのコメントも探します (UnpairedComments)。
    // [JA] 戻り値: "ファイル:行: リテラル" (日本語側なら前に "[ja] "、コメントなら前に CommentTag)
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
                var scanner = new LiteralScanner(src);
                foreach ((int at, int length, bool inJa) in scanner.UnpairedHangul())
                {
                    int line = src.AsSpan(0, at).Count('\n');
                    if (lines[line].Contains(BothLangMark))
                        continue;
                    found.Add($"{rel}:{line + 1}: {(inJa ? "[ja] " : "")}{src.Substring(at, Math.Min(length, 80)).ReplaceLineEndings(" ")}");
                }
                foreach ((int line, string text) in UnpairedComments(src, scanner.Comments))
                    found.Add($"{rel}:{line + 1}: {CommentTag}{text[..Math.Min(text.Length, 80)].ReplaceLineEndings(" ")}");
            }
        }
        return found;
    }

    // 찾은 것 가운데 주석에서 나온 것 앞에 붙는 표시
    // [JA] 見つかったもののうち、コメントから出たものの前に付く印
    public const string CommentTag = "[comment] ";

    static bool HasJapanese(string s) =>
        s.Contains("[JA]", StringComparison.Ordinal) || s.Any(c => c is >= '぀' and <= 'ヿ' or >= 'ｦ' and <= 'ﾟ');

    static bool IsKoreanOnly(string s) => HasHangul(s) && !HasJapanese(s);

    // 주석 짝 검사: 한글만 있는 주석에는 일본어 짝이 있어야 한다.
    // 줄 전체가 // 주석인 줄이 이어진 묶음은, 일본어 줄([JA] 또는 가나가 있는 줄)이 한글만 있는 줄보다 적으면 그 묶음의 한글 줄을 돌려준다.
    // 코드 뒤에 붙은 // 주석, /* */ 주석, #region 같은 지시문 줄은 그 주석 하나 안에 일본어가 같이 있어야 한다.
    // [JA] コメントのペア チェック: ハングルだけのコメントには日本語のペアが必要です。
    // [JA] 行全体が // コメントの行が続くまとまりでは、日本語の行([JA] かかなのある行)がハングルだけの行より少なければ、そのまとまりのハングルの行を返します。
    // [JA] コードの後ろに付いた // コメント、/* */ コメント、#region のようなディレクティブ行は、そのコメント一つの中に日本語も一緒に必要です。
    static List<(int Line, string Text)> UnpairedComments(string src, List<(int At, int Length)> comments)
    {
        var result = new List<(int Line, string Text)>();
        var block = new List<(int Line, string Text)>();
        int korean = 0, japanese = 0, lastLine = -2;
        void Flush()
        {
            if (korean > japanese)
                result.AddRange(block.Where(b => IsKoreanOnly(b.Text)));
            block.Clear();
            korean = japanese = 0;
        }
        foreach ((int at, int length) in comments)
        {
            string text = src.Substring(at, length).TrimEnd('\r');
            int line = src.AsSpan(0, at).Count('\n');
            int lineStart = at == 0 ? 0 : src.LastIndexOf('\n', at - 1) + 1;
            bool wholeLine = text.StartsWith("//", StringComparison.Ordinal) && src.AsSpan(lineStart, at - lineStart).IsWhiteSpace();
            if (!wholeLine)
            {
                // 한 주석 안에 한글과 한자가 같이 있으면 "참고 / 参考" 처럼 두 언어를 적은 것으로 본다
                // [JA] 一つのコメントにハングルと漢字が一緒にあれば、「참고 / 参考」のように二つの言語で書いたものと見なします
                if (IsKoreanOnly(text) && !text.Any(c => c is >= '一' and <= '鿿'))
                    result.Add((line, text));
                continue;
            }
            if (line != lastLine + 1)
                Flush();
            lastLine = line;
            block.Add((line, text));
            if (IsKoreanOnly(text))
                korean++;
            else if (HasJapanese(text))
                japanese++;
        }
        Flush();
        return [.. result.OrderBy(r => r.Line)];
    }

    // C# 소스를 훑으며 한글 문자열 리터럴이 Text.L( 한국어, 일본어 ) 의 첫 인자 안에 있는지 본다.
    // Text.L 밖이거나 일본어 쪽(둘째 인자) 안에 있으면 찾은 것으로 친다. (주석, 문자 리터럴, 보간 구멍과 그 서식, 원시 문자열 처리)
    // [JA] C# ソースを読み進めながら、ハングルの文字列リテラルが Text.L( 韓国語, 日本語 ) の最初の引数の中にあるかを見ます。
    // [JA] Text.L の外か日本語側(2 番目の引数)の中にあれば、見つかったものとして数えます。(コメント、文字リテラル、補間の穴とその書式、生文字列を処理)
    sealed class LiteralScanner(string src)
    {
        // 여는 괄호 ( [ { 마다 하나. Pair: 그 괄호가 Text.L( 인가, Arg: 그 괄호 안에서 지금 몇 번째 인자인가(0부터)
        // [JA] 開き括弧 ( [ { ごとに一つ。Pair: その括弧が Text.L( かどうか、Arg: その括弧の中で今何番目の引数か(0 から)
        sealed class Frame(bool pair)
        {
            public bool Pair { get; } = pair;
            public int Arg { get; set; }
        }

        int _i;
        readonly List<Frame> _frames = [];
        readonly List<(int, int, bool)> _found = [];

        // 지나가며 본 주석과 지시문 줄의 (시작, 길이). UnpairedHangul() 을 부른 뒤에 채워져 있다
        // [JA] 通りがかりに見たコメントとディレクティブ行の(開始, 長さ)。UnpairedHangul() を呼んだ後に埋まっています
        public List<(int At, int Length)> Comments { get; } = [];

        public List<(int At, int Length, bool InJa)> UnpairedHangul()
        {
            Code(inHole: false);
            return _found;
        }

        // 가장 안쪽 Text.L 이 없으면 Text.L 밖, 있으면 그 인자 번호로 판단한다
        // [JA] 一番内側の Text.L がなければ Text.L の外、あればその引数の番号で判断します
        (bool Outside, bool InJa) Where()
        {
            for (int k = _frames.Count - 1; k >= 0; k--)
                if (_frames[k].Pair)
                    return (false, _frames[k].Arg >= 1);
            return (true, false);
        }

        // inHole: 보간 구멍 안. 맨 바깥에서 } 를 만나거나 서식(:)·정렬(,)이 시작되면 돌아간다
        // [JA] inHole: 補間の穴の中。一番外側で } に出会うか、書式(:)・配置(,)が始まったら戻ります
        void Code(bool inHole)
        {
            int bottom = _frames.Count;
            while (_i < src.Length)
            {
                char c = src[_i];
                bool top = _frames.Count == bottom;
                if (inHole && top && c is '}' or ':' or ',')
                    return;
                if (!inHole && (Starts("//") || (c == '#' && LineStartBefore(_i))))
                {
                    int nl = src.IndexOf('\n', _i);
                    int end = nl < 0 ? src.Length : nl;
                    Comments.Add((_i, end - _i));
                    _i = end;
                }
                else if (Starts("/*"))
                {
                    int end = src.IndexOf("*/", _i + 2, StringComparison.Ordinal);
                    end = end < 0 ? src.Length : end + 2;
                    Comments.Add((_i, end - _i));
                    _i = end;
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
                else if (c is '(' or '[' or '{')
                {
                    _frames.Add(new Frame(c == '(' && src.AsSpan(0, _i).TrimEnd().EndsWith("Text.L", StringComparison.Ordinal)));
                    _i++;
                }
                else if (c is ')' or ']' or '}')
                {
                    if (_frames.Count > bottom) _frames.RemoveAt(_frames.Count - 1);
                    _i++;
                }
                else if (c == ',' && _frames.Count > 0)
                {
                    _frames[^1].Arg++;
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
            (bool outside, bool inJa) = Where();
            if (hangul && (outside || inJa))
                _found.Add((start, _i - start, inJa));
        }

        // 보간 구멍 { 식 , 정렬 : 서식 } 에서 식만 코드로 읽고, 정렬·서식은 닫는 } 까지 건너뛴다 (} 는 braces 개)
        // [JA] 補間の穴 { 式 , 配置 : 書式 } では式だけをコードとして読み、配置・書式は閉じる } まで飛ばします (} は braces 個)
        void Hole(int braces)
        {
            Code(inHole: true);
            while (_i < src.Length && src[_i] != '}')
                _i++;
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
    // [JA] ---- 補間文字列ハンドラー (Text.L 専用) ----
    // [JA] 選んだ言語でなければ shouldAppend = false なので、コンパイラーはそちらの穴の式をまったく計算しません。

    [InterpolatedStringHandler, DebuggerStepThrough]
    public ref struct KoText
    {
        DefaultInterpolatedStringHandler _inner;

        public KoText(int literalLength, int formattedCount, out bool shouldAppend)
        {
            shouldAppend = !IsJa;
            // 고르지 않은 쪽은 버퍼도 빌리지 않는다 (Append* 와 ToStringAndClear 가 불리지 않는다)
            // [JA] 選ばなかった側はバッファーも借りません (Append* と ToStringAndClear が呼ばれません)
            _inner = shouldAppend ? new DefaultInterpolatedStringHandler(literalLength, formattedCount) : default;
        }

        public void AppendLiteral(string value) => _inner.AppendLiteral(value);
        public void AppendFormatted<T>(T value) => _inner.AppendFormatted(value);
        public void AppendFormatted<T>(T value, string? format) => _inner.AppendFormatted(value, format);
        public void AppendFormatted<T>(T value, int alignment) => _inner.AppendFormatted(value, alignment);
        public void AppendFormatted<T>(T value, int alignment, string? format) => _inner.AppendFormatted(value, alignment, format);
        public void AppendFormatted(string? value) => _inner.AppendFormatted(value);
        public void AppendFormatted(string? value, int alignment = 0, string? format = null) => _inner.AppendFormatted(value, alignment, format);
        public void AppendFormatted(object? value, int alignment = 0, string? format = null) => _inner.AppendFormatted(value, alignment, format);
        public void AppendFormatted(scoped ReadOnlySpan<char> value) => _inner.AppendFormatted(value);
        public void AppendFormatted(scoped ReadOnlySpan<char> value, int alignment = 0, string? format = null) => _inner.AppendFormatted(value, alignment, format);
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
            // [JA] 選ばなかった側はバッファーも借りません (Append* と ToStringAndClear が呼ばれません)
            _inner = shouldAppend ? new DefaultInterpolatedStringHandler(literalLength, formattedCount) : default;
        }

        public void AppendLiteral(string value) => _inner.AppendLiteral(value);
        public void AppendFormatted<T>(T value) => _inner.AppendFormatted(value);
        public void AppendFormatted<T>(T value, string? format) => _inner.AppendFormatted(value, format);
        public void AppendFormatted<T>(T value, int alignment) => _inner.AppendFormatted(value, alignment);
        public void AppendFormatted<T>(T value, int alignment, string? format) => _inner.AppendFormatted(value, alignment, format);
        public void AppendFormatted(string? value) => _inner.AppendFormatted(value);
        public void AppendFormatted(string? value, int alignment = 0, string? format = null) => _inner.AppendFormatted(value, alignment, format);
        public void AppendFormatted(object? value, int alignment = 0, string? format = null) => _inner.AppendFormatted(value, alignment, format);
        public void AppendFormatted(scoped ReadOnlySpan<char> value) => _inner.AppendFormatted(value);
        public void AppendFormatted(scoped ReadOnlySpan<char> value, int alignment = 0, string? format = null) => _inner.AppendFormatted(value, alignment, format);
        public string ToStringAndClear() => _inner.ToStringAndClear();
    }
}
