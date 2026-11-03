using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BugHunt;

// 실행 출력의 언어 (한국어 / 일본어). 번역은 모두 Strings.Table.cs 한곳에 있다.
// 고르는 순서: --lang ja|ko > 환경 변수 BUGHUNT_LANG > CurrentUICulture > 한국어.
// CurrentCulture는 읽지도 바꾸지도 않는다. 문화권 랩(16, B-2)의 판정이 화면 언어에 따라 달라지면 안 되기 때문이다.
// 채점기(BugHunt.Grader)도 이 파일을 같이 컴파일해서 쓴다.
// [JA] 実行出力の言語(韓国語 / 日本語)。翻訳はすべて Strings.Table.cs の一か所にあります。
// [JA] 選ぶ順番: --lang ja|ko > 環境変数 BUGHUNT_LANG > CurrentUICulture > 韓国語。
// [JA] CurrentCulture は読みも変えもしません。カルチャのラボ(16, B-2)の判定が表示言語で変わってはいけないからです。
// [JA] 採点器(BugHunt.Grader)もこのファイルを一緒にコンパイルして使います。
public static partial class Text
{
    public const string EnvName = "BUGHUNT_LANG";

    // 번역이 없을 때 표준 오류에 찍는 표시. 채점기가 이 줄을 모아 "빠진 번역"으로 보고한다
    public const string MissingMark = "[BUGHUNT-MISSING-JA] ";

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

    // 한국어 원문 → 선택한 언어. 표에 없으면 원문을 그대로 돌려주고 빠진 번역으로 기록한다
    public static string T(string ko) => IsJa ? Lookup(ko) ?? (HasHangul(ko) ? Missing(ko) : ko) : ko;

    // 자리 표시({0}, {1} ...)가 있는 원문. 번역한 틀에 값을 넣는다 (숫자는 문화권과 상관없이 같은 모양)
    public static string F(string koFormat, params object?[] args) =>
        string.Format(CultureInfo.InvariantCulture, T(koFormat), args);

    // 일본어 번역이 있으면 그것, 없으면 null (언어 설정과 상관없이)
    public static string? JaOf(string ko) => Lookup(ko);

    // 표(Entries)는 다른 파일에 있어 정적 초기화 순서가 정해지지 않으므로 처음 쓸 때 만든다
    static Dictionary<string, string>? _exact;
    static Dictionary<string, string> Exact => _exact ??= BuildExact();
    static List<(Regex Pattern, string Ja, int Literal)>? _templates;

    static Dictionary<string, string> BuildExact()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach ((string ko, string ja) in Entries)
            map.TryAdd(ko, ja);
        return map;
    }

    static List<(Regex, string, int)> Templates => _templates ??= Entries
        .Where(e => e.Ko.Contains("{0}"))
        .Select(e => (new Regex("^" + Regex.Replace(Regex.Escape(e.Ko), @"\\\{(\d+)}", m => $"(?<p{m.Groups[1].Value}>.*?)") + "$",
                                RegexOptions.Singleline | RegexOptions.CultureInvariant), e.Ja,
                      Regex.Replace(e.Ko, @"\{\d+}", "").Length))
        .OrderByDescending(t => t.Item3)
        .ToList();

    static string? Lookup(string ko)
    {
        if (Exact.TryGetValue(ko, out string? ja))
            return ja;
        foreach ((Regex pattern, string jaFormat, _) in Templates)
        {
            Match m = pattern.Match(ko);
            if (!m.Success)
                continue;
            // 자리에 들어간 값이 그 자체로 번역할 문구면(예: Expect 설명) 그것도 바꾼다
            return Regex.Replace(jaFormat, @"\{(\d+)}", p =>
            {
                Group g = m.Groups["p" + p.Groups[1].Value];
                if (!g.Success)
                    return p.Value;
                return Exact.TryGetValue(g.Value, out string? inner) ? inner : g.Value;
            });
        }
        return null;
    }

    static readonly HashSet<string> Reported = new(StringComparer.Ordinal);

    public static int MissingCount { get { lock (Reported) return Reported.Count; } }

    static string Missing(string ko)
    {
        lock (Reported)
        {
            if (Reported.Add(ko))
                Console.Error.WriteLine(MissingMark + ko);
        }
        return ko;
    }

    // 표 자체 검사: 일본어가 비었거나, 한국어와 일본어의 자리 표시({0} ...)가 다르면 문제로 돌려준다
    public static List<string> ValidateTable()
    {
        var problems = new List<string>();
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach ((string ko, string ja) in Entries)
        {
            if (string.IsNullOrWhiteSpace(ja))
                problems.Add($"일본어 없음: {ko}");
            static string Holes(string s) => string.Join(",", Regex.Matches(s, @"\{\d+}").Select(m => m.Value).Order());
            if (Holes(ko) != Holes(ja))
                problems.Add($"자리 표시 다름: {ko} / {ja}");
            if (seen.TryGetValue(ko, out string? other) && other != ja)
                problems.Add($"같은 원문에 다른 번역: {ko}");
            seen.TryAdd(ko, ja);
        }
        return problems;
    }

    public static bool HasHangul(string s)
    {
        foreach (char c in s)
            if (c is >= '가' and <= '힣' or >= '㄰' and <= '㆏')
                return true;
        return false;
    }

    // ---- 콘솔 출력 바꾸기 ----
    // 랩 코드는 한국어로 Console.WriteLine 한다. 일본어일 때는 Console.Out을 줄 단위로 번역하는 writer로 바꿔 끼운다.
    // LabKit의 판정 줄처럼 이미 번역한 줄은 Raw로 바로 쓴다.

    static readonly object Gate = new();
    static TextWriter _raw = Console.Out;
    static LineTranslator? _translator;

    public static void InstallConsole()
    {
        _raw = Console.Out;
        if (!IsJa)
            return;
        _translator = new LineTranslator(_raw);
        Console.SetOut(_translator);
        AppDomain.CurrentDomain.ProcessExit += (_, _) => _translator.FlushPartial();
    }

    // 번역을 거치지 않고 쓴다 (먼저 랩이 쓰다 만 줄을 내보낸다)
    public static void WriteRaw(string text)
    {
        lock (Gate)
        {
            _translator?.FlushPartial();
            _raw.Write(text);
            _raw.Flush();
        }
    }

    public static void WriteLineRaw(string line) => WriteRaw(line + Environment.NewLine);

    sealed class LineTranslator(TextWriter inner) : TextWriter
    {
        readonly StringBuilder _line = new();

        public override Encoding Encoding => inner.Encoding;

        public override IFormatProvider FormatProvider => inner.FormatProvider;

        public override void Write(char value)
        {
            lock (Gate)
            {
                if (value == '\n')
                    EmitLine(newline: true);
                else
                    _line.Append(value);
            }
        }

        public override void Write(string? value)
        {
            if (value == null)
                return;
            lock (Gate)
                foreach (char c in value)
                    Write(c);
        }

        public override void WriteLine(string? value)
        {
            lock (Gate)
            {
                Write(value);
                Write('\n');
            }
        }

        public override void Flush() => inner.Flush();

        public void FlushPartial()
        {
            lock (Gate)
                if (_line.Length > 0)
                    EmitLine(newline: false);
        }

        void EmitLine(bool newline)
        {
            string line = _line.ToString().TrimEnd('\r');
            _line.Clear();
            inner.Write(TranslateLine(line));
            if (newline)
                inner.WriteLine();
            inner.Flush();
        }
    }

    // 앞쪽 공백은 그대로 두고 내용만 바꾼다
    public static string TranslateLine(string line)
    {
        string content = line.TrimStart();
        if (content.Length == 0 || !HasHangul(content))
            return line;
        string indent = line[..(line.Length - content.Length)];
        string trimmed = content.TrimEnd();
        return indent + T(trimmed) + content[trimmed.Length..];
    }
}
