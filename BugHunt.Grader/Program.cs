using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace BugHunt.Grader;

// BugHunt 채점기.
// 랩마다 BugHunt.exe를 새 프로세스로 띄워서 크래시와 멈춤을 격리하고, 숨은 케이스(무작위 시드)로도 다시 돌린다.
// 결과는 콘솔 표와 grade-report.json(다른 BugHunt 판과 같은 형식)으로 낸다.
static class Program
{
    const string Edition = "cs";
    const string ReportVersion = "1";

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
            Console.WriteLine("BugHunt.slnx가 있는 폴더를 찾지 못했습니다. 저장소 안에서 실행하세요.");
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
        Console.WriteLine($"\n=== BugHunt 채점 (랩 {labs.Count}개, 숨은 케이스 기준 시드 {baseSeed}) ===\n");
        Console.WriteLine($"  {"랩",3}  {"부",2}  {"결과",-8}{"시간",8}  제목");

        var results = new List<LabResult>();
        foreach (LabInfo lab in labs)
        {
            LabResult result = GradeLab(root, lab, options, baseSeed);
            results.Add(result);
            string detail = result.Detail.Length > 0 ? $"\n{"",26}└ {result.Detail}" : "";
            Console.WriteLine($"  {lab.Id,3}  {lab.Part,2}  {result.Result,-8}{result.Ms,6}ms  {lab.Title}{detail}");
        }

        int passed = results.Count(r => r.Result == "PASS");
        int skipped = results.Count(r => r.Result == "SKIP");
        int graded = results.Count - skipped;
        Console.WriteLine();
        foreach (IGrouping<int, LabResult> part in results.GroupBy(r => r.Part))
            Console.WriteLine($"  {part.Key}부: {part.Count(r => r.Result == "PASS")} / {part.Count(r => r.Result != "SKIP")}");
        string counts = string.Join(", ", results.GroupBy(r => r.Result).OrderBy(g => g.Key).Select(g => $"{g.Key} {g.Count()}"));
        Console.WriteLine($"\n  총점: {passed} / {graded}" + (graded > 0 ? $" ({passed * 100 / graded}%)" : "") + $"   [{counts}]");

        string outPath = options.Out ?? Path.Combine(root, "grade-report.json");
        WriteReport(outPath, new Report(Edition, ReportVersion, results.Count, passed, results));
        Console.WriteLine($"  보고서: {outPath}");
        if (passed < graded)
            Console.WriteLine($"  숨은 케이스를 그대로 다시 돌리려면: --seed {baseSeed}");

        return passed == graded ? 0 : 1;
    }

    static void PrintUsage()
    {
        Console.WriteLine("""
            사용법: grade.bat [랩] [옵션]
                    dotnet run --project BugHunt.Grader -- [랩] [옵션]

              랩            채점할 랩 번호. 예: 29  29-40  1,5,29-34   (없으면 전부)
              --config C    Debug 또는 Release만 채점 (기본: 전부 Debug, Release 표시된 랩은 Release도)
              --seed N      숨은 케이스 시드를 고정 (실패를 그대로 재현할 때)
              --no-hidden   숨은 케이스 없이 기본 케이스만
              --no-build    빌드를 건너뛰고 이미 빌드된 BugHunt를 채점
              --timeout MS  실행 한 번의 제한 시간 (기본 20000)
              --out 경로    보고서 위치 (기본: 저장소 루트의 grade-report.json)
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

    static string? FindRepoRoot()
    {
        foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
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
        Console.Write($"BugHunt 빌드 중 ({config})... ");
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
            Console.WriteLine("완료");
            return true;
        }

        Console.WriteLine("실패\n");
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
            ?? throw new InvalidOperationException("BugHunt --list 결과를 읽지 못했습니다.");
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
                return new LabResult(lab.Id, lab.Title, lab.Part, "SKIP", totalMs, $"{c.Config} 빌드가 없음 (--no-build 없이 실행하세요)");

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
                where.Add($"숨은 케이스 seed {c.Seed}");
            string prefix = where.Count > 0 ? $"[{string.Join(", ", where)}] " : "";
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
        p.StandardInput.Close();
        Task<string> stdout = p.StandardOutput.ReadToEndAsync();
        Task<string> stderr = p.StandardError.ReadToEndAsync();
        if (!p.WaitForExit(timeoutMs))
        {
            p.Kill(entireProcessTree: true);
            p.WaitForExit();
            return ("TIMEOUT", $"{timeoutMs} ms 안에 끝나지 않아 강제 종료", watch.ElapsedMilliseconds);
        }
        p.WaitForExit();
        long elapsed = watch.ElapsedMilliseconds;

        string output = stdout.Result;
        string[] lines = output.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        string? hang = lines.FirstOrDefault(l => l.StartsWith("[FAIL] 응답 없음"));
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
            return ("FAIL", firstFail?["[FAIL] ".Length..] ?? "판정 출력이 없음", elapsed);
        }

        // 처리되지 않은 예외, 스택 오버플로, FailFast, 접근 위반 등
        string[] err = stderr.Result.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        string reason = string.Join(" ", err.Where(l => !l.StartsWith("at ") && !l.StartsWith("---")).Take(2));
        return ("CRASH", $"0x{p.ExitCode:X8} {reason}".TrimEnd(), elapsed);
    }

    static void WriteReport(string path, Report report)
    {
        string json = JsonSerializer.Serialize(report, Json);
        File.WriteAllText(path, json + "\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
