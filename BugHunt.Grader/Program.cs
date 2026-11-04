using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using BugHunt;

namespace BugHunt.Grader;

// BugHunt 채점기.
// 랩마다 BugHunt.exe를 새 프로세스로 띄워서 크래시와 멈춤을 격리하고, 숨은 케이스(무작위 시드)로도 다시 돌린다.
// 결과는 콘솔 표와 grade-report.json(다른 BugHunt 판과 같은 형식)으로 낸다.
static class Program
{
    const string Edition = "cs";
    const string ReportVersion = "1";
    const int ChallengePart = 0;   // 챌린지 랩의 part 번호(번호 붙은 부가 아님). id는 C-n = 100+n, B-n = 200+n

    static string PartName(int part) => part == ChallengePart ? Text.T("챌린지") : Text.F("{0}부", part);

    record LabInfo(int Id, string Title, int Part, int Hidden, bool Release);

    record LabResult(int Id, string Title, int Part, string Result, long Ms, string Detail);

    record Report(string Edition, string Version, int Total, int Passed, List<LabResult> Labs);

    // 한 번 실행할 조건: 빌드 구성과 시드(0 = 화면에 보이는 기본 케이스)
    record Case(string Config, int Seed);

    class Options
    {
        public HashSet<int>? Only;
        public string? Config;          // null이면 Debug + (Release 표시된 랩은) Release
        public bool Build = true;
        public bool Hidden = true;
        public int? Seed;
        public int TimeoutMs = 20_000;
        public string? Out;
    }

    static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        // 출력 언어: --lang ja|ko > BUGHUNT_LANG > 화면 언어 > 한국어. 랩 프로세스에도 같은 언어를 넘긴다
        string[]? rest = Text.TakeLangArg(args);
        if (rest == null)
        {
            Console.WriteLine("--lang 뒤에는 ko 또는 ja / --lang の後には ko か ja を指定してください");
            return 2;
        }
        args = rest;
        Text.InstallConsole();

        Options options;
        try
        {
            options = ParseArgs(args);
        }
        catch (FormatException ex)
        {
            Console.WriteLine(ex.Message);
            PrintUsage();
            return 2;
        }

        string? root = FindRepoRoot();
        if (root == null)
        {
            Console.WriteLine(Text.T("BugHunt.slnx가 있는 폴더를 찾지 못했습니다. 저장소 안에서 실행하세요."));
            return 2;
        }

        string[] configs = options.Config != null ? [options.Config] : ["Debug", "Release"];
        foreach (string config in configs)
        {
            if (options.Build && !Build(root, config))
                return 2;
        }

        string listExe = FindLabExe(root, configs[0])!;
        List<LabInfo> labs = ListLabs(listExe);
        if (options.Only != null)
            labs = [.. labs.Where(l => options.Only.Contains(l.Id))];

        int baseSeed = options.Seed ?? Random.Shared.Next(1, 1_000_000);
        Console.WriteLine("\n" + Text.F("=== BugHunt 채점 (랩 {0}개, 숨은 케이스 기준 시드 {1}) ===", labs.Count, baseSeed) + "\n");
        Console.WriteLine($"  {Text.T("랩"),3}  {Text.T("부"),2}  {Text.T("결과"),-8}{Text.T("시간"),8}  {Text.T("제목")}");

        var results = new List<LabResult>();
        foreach (LabInfo lab in labs)
        {
            LabResult result = GradeLab(root, lab, options, baseSeed);
            results.Add(result);
            string detail = result.Detail.Length > 0 ? $"\n{"",26}└ {result.Detail}" : "";
            // 제목과 상세는 이미 고른 언어다. 상세에 섞인 랩 데이터(한국어 값)를 빠진 번역으로 세지 않게 바로 쓴다
            Text.WriteLineRaw($"  {lab.Id,3}  {(lab.Part == ChallengePart ? "C" : lab.Part.ToString()),2}  {result.Result,-8}{result.Ms,6}ms  {lab.Title}{detail}");
        }

        int passed = results.Count(r => r.Result == "PASS");
        int skipped = results.Count(r => r.Result == "SKIP");
        int graded = results.Count - skipped;
        Console.WriteLine();
        foreach (IGrouping<int, LabResult> part in results.GroupBy(r => r.Part))
            Console.WriteLine($"  {PartName(part.Key)}: {part.Count(r => r.Result == "PASS")} / {part.Count(r => r.Result != "SKIP")}");
        string counts = string.Join(", ", results.GroupBy(r => r.Result).OrderBy(g => g.Key).Select(g => $"{g.Key} {g.Count()}"));
        Console.WriteLine("\n  " + Text.F("총점: {0} / {1}", passed, graded) + (graded > 0 ? $" ({passed * 100 / graded}%)" : "") + $"   [{counts}]");

        string outPath = options.Out ?? Path.Combine(root, "grade-report.json");
        WriteReport(outPath, new Report(Edition, ReportVersion, results.Count, passed, results));
        Console.WriteLine("  " + Text.F("보고서: {0}", outPath));
        if (passed < graded)
            Console.WriteLine("  " + Text.F("숨은 케이스를 그대로 다시 돌리려면: --seed {0}", baseSeed));

        // 일본어로 채점하면 번역이 빠진 문구를 모아 보여 준다 (판정과는 별개. 종료 코드 3)
        if (Text.IsJa)
        {
            CheckLang(listExe);
            Console.WriteLine("  " + Text.F("번역이 빠진 문구: {0}개", MissingJa.Count));
            foreach (string m in MissingJa.Order(StringComparer.Ordinal).Take(30))
                Text.WriteLineRaw($"    {m}");
            if (MissingJa.Count > 0)
                return 3;
        }

        return passed == graded ? 0 : 1;
    }

    // 랩 프로세스가 표준 오류에 남긴 "번역 없음" 표시 (일본어로 채점할 때만 생긴다)
    static readonly HashSet<string> MissingJa = new(StringComparer.Ordinal);

    static void CollectMissing(string stderr)
    {
        string mark = Text.MissingMark.Trim();
        foreach (string line in stderr.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            if (line.StartsWith(mark))
                lock (MissingJa)
                    MissingJa.Add(line[mark.Length..].Trim());
    }

    // 메뉴·랩 제목·증상과 번역 표 자체 검사 (BugHunt --check-lang)
    static void CheckLang(string dll)
    {
        using Process p = Process.Start(LabProcess(dll, "--check-lang"))!;
        p.StandardInput.Close();
        Task<string> err = p.StandardError.ReadToEndAsync();
        string output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        CollectMissing(err.Result);
        foreach (string line in output.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Where(l => !l.StartsWith("check-lang:")))
            lock (MissingJa)
                MissingJa.Add(line);
    }

    static void PrintUsage()
    {
        Console.WriteLine("""
            사용법: grade.bat [랩] [옵션]
                    dotnet run --project BugHunt.Grader -- [랩] [옵션]

              랩            채점할 랩 번호. 예: 24  24-30  1,5,75-82  C  B  C-1,B-3   (없으면 전부. C = 깊은 추적 챌린지, B = 블라인드 챌린지)
              --config C    Debug 또는 Release만 채점 (기본: 전부 Debug, Release 표시된 랩은 Release도)
              --seed N      숨은 케이스 시드를 고정 (실패를 그대로 재현할 때)
              --no-hidden   숨은 케이스 없이 기본 케이스만
              --no-build    빌드를 건너뛰고 이미 빌드된 BugHunt를 채점
              --timeout MS  실행 한 번의 제한 시간 (기본 20000)
              --out 경로    보고서 위치 (기본: 저장소 루트의 grade-report.json)
              --lang L      출력 언어 ko 또는 ja (기본: BUGHUNT_LANG, 없으면 화면 언어)
            """);
    }

    static Options ParseArgs(string[] args)
    {
        var o = new Options();
        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            string Next() => i + 1 < args.Length ? args[++i] : throw new FormatException($"{a} 뒤에 값이 필요합니다.");
            switch (a)
            {
                case "-h" or "--help" or "/?":
                    PrintUsage();
                    Environment.Exit(0);
                    break;
                case "--config" or "-c":
                    string c = Next();
                    o.Config = c.Equals("release", StringComparison.OrdinalIgnoreCase) ? "Release"
                             : c.Equals("debug", StringComparison.OrdinalIgnoreCase) ? "Debug"
                             : throw new FormatException($"구성은 Debug 또는 Release: {c}");
                    break;
                case "--seed":
                    o.Seed = int.TryParse(Next(), out int seed) && seed != 0 ? seed : throw new FormatException("시드는 0이 아닌 정수");
                    break;
                case "--no-hidden": o.Hidden = false; break;
                case "--no-build": o.Build = false; break;
                case "--timeout":
                    o.TimeoutMs = int.TryParse(Next(), out int ms) && ms > 0 ? ms : throw new FormatException("제한 시간은 양의 정수(ms)");
                    break;
                case "--out": o.Out = Path.GetFullPath(Next()); break;
                default:
                    o.Only ??= [];
                    foreach (string part in a.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        // 챌린지: C, B (전부) 또는 C-2, B-5
                        if (part.Length >= 1 && (char.ToUpperInvariant(part[0]) is 'C' or 'B') && (part.Length == 1 || part[1] == '-'))
                        {
                            int baseId = char.ToUpperInvariant(part[0]) == 'C' ? 100 : 200;
                            if (part.Length == 1)
                                for (int n = 1; n < 100; n++) o.Only.Add(baseId + n);
                            else if (int.TryParse(part[2..], out int k)) o.Only.Add(baseId + k);
                            else throw new FormatException($"알 수 없는 인자: {a}");
                            continue;
                        }
                        string[] range = part.Split('-');
                        if (!int.TryParse(range[0], out int from) || !int.TryParse(range[^1], out int to) || range.Length > 2)
                            throw new FormatException($"알 수 없는 인자: {a}");
                        for (int n = from; n <= to; n++)
                            o.Only.Add(n);
                    }
                    break;
            }
        }
        return o;
    }

    // 채점기 자신이 들어 있는 저장소를 먼저 찾는다. 현재 폴더를 먼저 보면, 다른 폴더에서 grade.bat 을 부를 때
    // 엉뚱한 체크아웃(다른 worktree)을 채점한다. 채점기 exe 를 저장소 밖으로 복사해 쓸 때만 현재 폴더로 찾는다.
    static string? FindRepoRoot()
    {
        foreach (string start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            for (DirectoryInfo? dir = new(start); dir != null; dir = dir.Parent)
            {
                if (File.Exists(Path.Combine(dir.FullName, "BugHunt", "BugHunt.csproj")) &&
                    File.Exists(Path.Combine(dir.FullName, "BugHunt.slnx")))
                    return dir.FullName;
            }
        }
        return null;
    }

    static bool Build(string root, string config)
    {
        Text.WriteRaw(Text.F("BugHunt 빌드 중 ({0})...", config) + " ");
        var psi = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (string arg in new[] { "build", Path.Combine(root, "BugHunt", "BugHunt.csproj"), "-c", config, "-nologo", "-v", "q", "-clp:NoSummary" })
            psi.ArgumentList.Add(arg);
        // 빌드 메시지를 영어로 받아서 "error"/"warning" 줄을 고를 수 있게 한다.
        psi.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en";

        using Process p = Process.Start(psi)!;
        Task<string> stdout = p.StandardOutput.ReadToEndAsync();
        Task<string> stderr = p.StandardError.ReadToEndAsync();
        p.WaitForExit();
        if (p.ExitCode == 0)
        {
            Text.WriteLineRaw(Text.T("완료"));
            return true;
        }

        Text.WriteLineRaw(Text.T("실패") + Environment.NewLine);
        string[] lines = (stdout.Result + stderr.Result).Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines.Where(l => l.Contains(": error ") || l.Contains(": warning ")).Distinct().Take(20))
            Console.WriteLine($"  {line}");
        Console.WriteLine("\n빌드가 실패해서 채점하지 못했습니다. (이 프로젝트는 경고도 오류로 취급합니다)");
        return false;
    }

    // bin/<구성>/<대상 프레임워크>/ 아래의 BugHunt 실행 파일. Windows가 아니면 apphost 대신 dotnet BugHunt.dll로 실행한다.
    static string? FindLabExe(string root, string config)
    {
        string bin = Path.Combine(root, "BugHunt", "bin", config);
        if (!Directory.Exists(bin))
            return null;
        return Directory.GetFiles(bin, "BugHunt.dll", SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    static ProcessStartInfo LabProcess(string dll, params string[] args)
    {
        string apphost = Path.ChangeExtension(dll, OperatingSystem.IsWindows() ? ".exe" : null);
        var psi = File.Exists(apphost) ? new ProcessStartInfo(apphost) : new ProcessStartInfo("dotnet") { ArgumentList = { dll } };
        foreach (string arg in args)
            psi.ArgumentList.Add(arg);
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.RedirectStandardInput = true;
        psi.StandardOutputEncoding = Encoding.UTF8;
        psi.StandardErrorEncoding = Encoding.UTF8;
        psi.UseShellExecute = false;
        psi.Environment["BUGHUNT_GRADER"] = "1";
        psi.Environment[Text.EnvName] = Text.Lang;
        return psi;
    }

    static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    static List<LabInfo> ListLabs(string dll)
    {
        using Process p = Process.Start(LabProcess(dll, "--list"))!;
        string output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        return JsonSerializer.Deserialize<List<LabInfo>>(output, Json)
            ?? throw new InvalidOperationException(Text.T("BugHunt --list 결과를 읽지 못했습니다."));
    }

    static LabResult GradeLab(string root, LabInfo lab, Options options, int baseSeed)
    {
        var cases = new List<Case>();
        string[] configs = options.Config != null ? [options.Config] : lab.Release ? ["Debug", "Release"] : ["Debug"];
        foreach (string config in configs)
        {
            cases.Add(new Case(config, 0));
            if (options.Hidden)
                for (int k = 1; k <= lab.Hidden; k++)
                    cases.Add(new Case(config, HiddenSeed(baseSeed, lab.Id, k)));
        }

        long totalMs = 0;
        bool sawPass = false;
        foreach (Case c in cases)
        {
            string? dll = FindLabExe(root, c.Config);
            if (dll == null)
                return new LabResult(lab.Id, lab.Title, lab.Part, "SKIP", totalMs, Text.F("{0} 빌드가 없음 (--no-build 없이 실행하세요)", c.Config));

            (string result, string detail, long ms) = RunOnce(dll, lab.Id, c.Seed, options.TimeoutMs);
            totalMs += ms;
            if (result == "PASS")
            {
                sawPass = true;
                continue;
            }
            if (result == "SKIP" && !sawPass)
                return new LabResult(lab.Id, lab.Title, lab.Part, "SKIP", totalMs, detail);

            var where = new List<string>();
            if (configs.Length > 1 || options.Config != null)
                where.Add(c.Config);
            if (c.Seed != 0)
                where.Add(Text.F("숨은 케이스 seed {0}", c.Seed));
            string prefix = where.Count > 0 ? $"[{string.Join(", ", where)}] " : "";
            // 기본 케이스는 통과했는데 숨은 케이스에서 실패: 보이는 증상만 막고 원인은 그대로 둔 수정일 가능성이 크다
            if (c.Seed != 0)
                prefix = Text.T("증상은 사라졌지만 원인이 남아 있음") + " " + prefix;
            return new LabResult(lab.Id, lab.Title, lab.Part, result, totalMs, prefix + detail);
        }
        return new LabResult(lab.Id, lab.Title, lab.Part, "PASS", totalMs, "");
    }

    // 랩 번호와 순번이 같아도 기준 시드가 다르면 다른 입력이 나온다. 0은 기본 케이스라서 피한다.
    static int HiddenSeed(int baseSeed, int lab, int k)
    {
        int seed = (int)((uint)(baseSeed * 7919 + lab * 104_729 + k * 15_485_863) % 1_000_000_000);
        return seed == 0 ? 1 : seed;
    }

    // PASS: 종료 코드 0, [FAIL] 없음, [PASS] 1개 이상, 제한 시간 안에 끝남
    static (string Result, string Detail, long Ms) RunOnce(string dll, int lab, int seed, int timeoutMs)
    {
        ProcessStartInfo psi = LabProcess(dll, lab.ToString());
        if (seed != 0)
            psi.Environment["BUGHUNT_SEED"] = seed.ToString();

        var watch = Stopwatch.StartNew();
        using Process p = Process.Start(psi)!;
        // 랩 프로세스는 우선순위를 한 단계 높여 돌린다. PC가 바빠도 결과(특히 스택 오버플로 같은 크래시 처리)가 제한 시간 안에 나오게 하려는 것뿐이다
        try { p.PriorityClass = ProcessPriorityClass.AboveNormal; } catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception) { }
        p.StandardInput.Close();
        Task<string> stdout = p.StandardOutput.ReadToEndAsync();
        Task<string> stderr = p.StandardError.ReadToEndAsync();
        if (!p.WaitForExit(timeoutMs))
        {
            p.Kill(entireProcessTree: true);
            p.WaitForExit();
            CollectMissing(stderr.Result);
            // 스택 오버플로는 이미 넘친 뒤 런타임이 스택을 찍는 데 PC가 바쁘면 오래 걸릴 수 있다. 넘쳤다면 멈춤이 아니라 크래시다
            if (stderr.Result.StartsWith("Stack overflow.", StringComparison.Ordinal))
                return ("CRASH", "Stack overflow.", watch.ElapsedMilliseconds);
            return ("TIMEOUT", Text.F("{0} ms 안에 끝나지 않아 강제 종료", timeoutMs), watch.ElapsedMilliseconds);
        }
        p.WaitForExit();
        long elapsed = watch.ElapsedMilliseconds;

        string output = stdout.Result;
        CollectMissing(stderr.Result);
        string[] lines = output.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        // 응답 없음 표시는 랩의 출력 언어로 나온다
        string hangKo = "[FAIL] 응답 없음", hangJa = Text.JaOf(hangKo) ?? hangKo;
        string? hang = lines.FirstOrDefault(l => l.StartsWith(hangKo) || l.StartsWith(hangJa));
        string? firstFail = lines.FirstOrDefault(l => l.StartsWith("[FAIL]"));
        string? skip = lines.FirstOrDefault(l => l.StartsWith("[SKIP]"));

        if (p.ExitCode is 0 or 1)
        {
            if (hang != null)
                return ("TIMEOUT", hang["[FAIL] ".Length..], elapsed);
            if (p.ExitCode == 0 && firstFail == null && lines.Any(l => l.StartsWith("[PASS]")))
                return ("PASS", "", elapsed);
            if (skip != null && firstFail == null)
                return ("SKIP", skip["[SKIP] ".Length..], elapsed);
            return ("FAIL", firstFail?["[FAIL] ".Length..] ?? Text.T("판정 출력이 없음"), elapsed);
        }

        // 처리되지 않은 예외, 스택 오버플로, FailFast, 접근 위반 등
        string[] err = stderr.Result.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        string reason = string.Join(" ", err.Where(l => !l.StartsWith("at ") && !l.StartsWith("---") && !l.StartsWith(Text.MissingMark.Trim())).Take(2));
        if (Text.IsJa)
            reason = TranslateExceptionMessage(reason);
        return ("CRASH", $"0x{p.ExitCode:X8} {reason}".TrimEnd(), elapsed);
    }

    // 런타임이 찍는 "...Exception: 메시지"에서 랩이 던진 한국어 메시지만 표로 바꾼다 (AggregateException이면 괄호 안의 안쪽 메시지).
    // 표에 없으면 그대로 둔다. 런타임의 영어 문구는 건드리지 않는다
    static string TranslateExceptionMessage(string reason)
    {
        const string Key = "Exception: ";
        int at = reason.IndexOf(Key, StringComparison.Ordinal);
        if (at < 0)
            return reason;
        string head = reason[..(at + Key.Length)], message = reason[(at + Key.Length)..];
        if (Text.JaOf(message) is string ja)
            return head + ja;
        int open = message.IndexOf(" (", StringComparison.Ordinal);
        if (open >= 0 && message.EndsWith(')') && Text.JaOf(message[(open + 2)..^1]) is string inner)
            return head + message[..(open + 2)] + inner + ")";
        return reason;
    }

    static void WriteReport(string path, Report report)
    {
        string json = JsonSerializer.Serialize(report, Json);
        File.WriteAllText(path, json + "\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
