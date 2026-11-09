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

    static string PartName(int part) => part == ChallengePart ? Text.L("챌린지", "チャレンジ") : Text.L($"{part}부", $"第{part}部");

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
            Console.WriteLine("--lang 뒤에는 ko 또는 ja / --lang の後には ko か ja を指定してください"); // ko+ja (언어를 아직 못 골랐다 / まだ言語が決まっていない)
            return 2;
        }
        args = rest;

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
            Console.WriteLine(Text.L("BugHunt.slnx가 있는 폴더를 찾지 못했습니다. 저장소 안에서 실행하세요.", "BugHunt.slnx があるフォルダーが見つかりませんでした。リポジトリの中で実行してください。"));
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
        Console.WriteLine("\n" + Text.L($"=== BugHunt 채점 (랩 {labs.Count}개, 숨은 케이스 기준 시드 {baseSeed}) ===", $"=== BugHunt 採点 (ラボ {labs.Count} 個、隠しケースの基準シード {baseSeed}) ===") + "\n");
        Console.WriteLine($"  {Text.L("랩", "ラボ"),3}  {Text.L("부", "部"),2}  {Text.L("결과", "結果"),-8}{Text.L("시간", "時間"),8}  {Text.L("제목", "タイトル")}");

        var results = new List<LabResult>();
        foreach (LabInfo lab in labs)
        {
            LabResult result = GradeLab(root, lab, options, baseSeed);
            results.Add(result);
            string detail = result.Detail.Length > 0 ? $"\n{"",26}└ {result.Detail}" : "";
            // 제목과 상세는 랩 프로세스가 이미 고른 언어로 찍은 것이다
            Console.WriteLine($"  {lab.Id,3}  {(lab.Part == ChallengePart ? "C" : lab.Part.ToString()),2}  {result.Result,-8}{result.Ms,6}ms  {lab.Title}{detail}");
        }

        int passed = results.Count(r => r.Result == "PASS");
        int skipped = results.Count(r => r.Result == "SKIP");
        int graded = results.Count - skipped;
        Console.WriteLine();
        foreach (IGrouping<int, LabResult> part in results.GroupBy(r => r.Part))
            Console.WriteLine($"  {PartName(part.Key)}: {part.Count(r => r.Result == "PASS")} / {part.Count(r => r.Result != "SKIP")}");
        string counts = string.Join(", ", results.GroupBy(r => r.Result).OrderBy(g => g.Key).Select(g => $"{g.Key} {g.Count()}"));
        Console.WriteLine("\n  " + Text.L($"총점: {passed} / {graded}", $"総合点: {passed} / {graded}") + (graded > 0 ? $" ({passed * 100 / graded}%)" : "") + $"   [{counts}]");

        string outPath = options.Out ?? Path.Combine(root, "grade-report.json");
        WriteReport(outPath, new Report(Edition, ReportVersion, results.Count, passed, results));
        Console.WriteLine("  " + Text.L($"보고서: {outPath}", $"レポート: {outPath}"));
        if (passed < graded)
            Console.WriteLine("  " + Text.L($"숨은 케이스를 그대로 다시 돌리려면: --seed {baseSeed}", $"隠しケースをそのまま再実行するには: --seed {baseSeed}"));

        // 일본어로 채점하면 번역이 빠진 문구를 모아 보여 준다 (판정과는 별개. 종료 코드 3)
        if (Text.IsJa)
        {
            foreach (string u in Text.FindUnpairedHangul(root))
                MissingJa.Add(u);
            Console.WriteLine("  " + Text.L($"번역이 빠진 문구: {MissingJa.Count}개", $"翻訳が抜けている文言: {MissingJa.Count} 個"));
            foreach (string m in MissingJa.Order(StringComparer.Ordinal).Take(30))
                Console.WriteLine($"    {m}");
            if (MissingJa.Count > 0)
                return 3;
        }

        return passed == graded ? 0 : 1;
    }

    // 번역이 빠진 곳: 소스에서 Text.L 밖이나 일본어 쪽에 남은 한글 문자열과, 일본어로 돌린 랩의 출력(표준 출력, 표준 오류)에 섞여 나온 한글 줄
    // [JA] 翻訳が抜けている所: ソースで Text.L の外か日本語側に残ったハングルの文字列と、日本語で実行したラボの出力(標準出力、標準エラー)に混じったハングルの行
    static readonly HashSet<string> MissingJa = new(StringComparer.Ordinal);

    // 출력에 한글이 나와도 되는 랩. 50: 모드 B(CP949)를 UTF-8로 잘못 읽어 깨진 글자(�) 사이에 우연히 한글 음절이 섞인다
    // [JA] 出力にハングルが出てもよいラボ。50: MOD B(CP949)を UTF-8 として誤って読み、化けた文字(�)の間に偶然ハングルの音節が混じる
    static readonly HashSet<int> HangulAllowed = [50];

    static void CollectHangul(int lab, string output)
    {
        if (!Text.IsJa)
            return;
        foreach (string line in output.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (!Text.HasHangul(line) || (HangulAllowed.Contains(lab) && line.Contains('�')))
                continue;
            lock (MissingJa)
                MissingJa.Add($"Lab {LabCode(lab)}: {line}");
        }
    }

    static string LabCode(int id) => id > 200 ? $"B-{id - 200}" : id > 100 ? $"C-{id - 100}" : id.ToString();

    static void PrintUsage()
    {
        Console.WriteLine(Text.L("""
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
            """, """
            使い方: grade.bat [ラボ] [オプション]
                    dotnet run --project BugHunt.Grader -- [ラボ] [オプション]

              ラボ          採点するラボ番号。例: 24  24-30  1,5,75-82  C  B  C-1,B-3   (省略するとすべて。C = 深い追跡チャレンジ、B = ブラインド チャレンジ)
              --config C    Debug か Release だけを採点 (既定: すべて Debug、Release 指定のラボは Release も)
              --seed N      隠しケースのシードを固定 (失敗をそのまま再現するとき)
              --no-hidden   隠しケースなしで基本ケースだけ
              --no-build    ビルドを飛ばして、ビルド済みの BugHunt を採点
              --timeout MS  1 回の実行の制限時間 (既定 20000)
              --out パス    レポートの場所 (既定: リポジトリ ルートの grade-report.json)
              --lang L      出力言語 ko または ja (既定: BUGHUNT_LANG、なければ表示言語)
            """));
    }

    static Options ParseArgs(string[] args)
    {
        var o = new Options();
        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            string Next() => i + 1 < args.Length ? args[++i] : throw new FormatException(Text.L($"{a} 뒤에 값이 필요합니다.", $"{a} の後に値が必要です。"));
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
                             : throw new FormatException(Text.L($"구성은 Debug 또는 Release: {c}", $"構成は Debug または Release: {c}"));
                    break;
                case "--seed":
                    o.Seed = int.TryParse(Next(), out int seed) && seed != 0 ? seed : throw new FormatException(Text.L("시드는 0이 아닌 정수", "シードは 0 以外の整数"));
                    break;
                case "--no-hidden": o.Hidden = false; break;
                case "--no-build": o.Build = false; break;
                case "--timeout":
                    o.TimeoutMs = int.TryParse(Next(), out int ms) && ms > 0 ? ms : throw new FormatException(Text.L("제한 시간은 양의 정수(ms)", "制限時間は正の整数(ms)"));
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
                            else throw new FormatException(Text.L($"알 수 없는 인자: {a}", $"不明な引数: {a}"));
                            continue;
                        }
                        string[] range = part.Split('-');
                        if (!int.TryParse(range[0], out int from) || !int.TryParse(range[^1], out int to) || range.Length > 2)
                            throw new FormatException(Text.L($"알 수 없는 인자: {a}", $"不明な引数: {a}"));
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
        Console.Write(Text.L($"BugHunt 빌드 중 ({config})... ", $"BugHunt をビルド中 ({config})... "));
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
            Console.WriteLine(Text.L("완료", "完了"));
            return true;
        }

        Console.WriteLine(Text.L("실패", "失敗") + Environment.NewLine);
        string[] lines = (stdout.Result + stderr.Result).Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines.Where(l => l.Contains(": error ") || l.Contains(": warning ")).Distinct().Take(20))
            Console.WriteLine($"  {line}");
        Console.WriteLine(Text.L("\n빌드가 실패해서 채점하지 못했습니다. (이 프로젝트는 경고도 오류로 취급합니다)", "\nビルドが失敗したため採点できませんでした。(このプロジェクトは警告もエラーとして扱います)"));
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
            ?? throw new InvalidOperationException(Text.L("BugHunt --list 결과를 읽지 못했습니다.", "BugHunt --list の結果を読めませんでした。"));
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
                return new LabResult(lab.Id, lab.Title, lab.Part, "SKIP", totalMs, Text.L($"{c.Config} 빌드가 없음 (--no-build 없이 실행하세요)", $"{c.Config} のビルドがありません (--no-build なしで実行してください)"));

            // 랩 8(스택 오버플로)은 PC가 바쁘면 런타임이 넘친 스택을 찍는 데만 20초 가까이 걸린다. 이 랩만 제한 시간을 60초 이상으로
            int timeoutMs = lab.Id == 8 ? Math.Max(options.TimeoutMs, 60_000) : options.TimeoutMs;
            (string result, string detail, long ms) = RunOnce(dll, lab.Id, c.Seed, timeoutMs);
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
                where.Add(Text.L($"숨은 케이스 seed {c.Seed}", $"隠しケース seed {c.Seed}"));
            string prefix = where.Count > 0 ? $"[{string.Join(", ", where)}] " : "";
            // 기본 케이스는 통과했는데 숨은 케이스에서 실패: 보이는 증상만 막고 원인은 그대로 둔 수정일 가능성이 크다
            if (c.Seed != 0)
                prefix = Text.L("증상은 사라졌지만 원인이 남아 있음", "症状は消えたが原因が残っている") + " " + prefix;
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
            CollectHangul(lab, stdout.Result);
            CollectHangul(lab, stderr.Result);
            // 스택 오버플로는 이미 넘친 뒤 런타임이 스택을 찍는 데 PC가 바쁘면 오래 걸릴 수 있다. 넘쳤다면 멈춤이 아니라 크래시다
            if (stderr.Result.StartsWith("Stack overflow.", StringComparison.Ordinal))
                return ("CRASH", "Stack overflow.", watch.ElapsedMilliseconds);
            return ("TIMEOUT", Text.L($"{timeoutMs} ms 안에 끝나지 않아 강제 종료", $"{timeoutMs} ms 以内に終わらなかったため強制終了"), watch.ElapsedMilliseconds);
        }
        p.WaitForExit();
        long elapsed = watch.ElapsedMilliseconds;

        string output = stdout.Result;
        CollectHangul(lab, output);
        CollectHangul(lab, stderr.Result);
        string[] lines = output.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        // 응답 없음 표시는 랩의 출력 언어로 나온다
        string hangMark = Text.L("[FAIL] 응답 없음", "[FAIL] 応答なし");
        string? hang = lines.FirstOrDefault(l => l.StartsWith(hangMark));
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
            return ("FAIL", firstFail?["[FAIL] ".Length..] ?? Text.L("판정 출력이 없음", "判定の出力がない"), elapsed);
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
