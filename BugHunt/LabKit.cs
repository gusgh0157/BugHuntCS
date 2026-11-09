using System.Diagnostics;
using System.Globalization;

namespace BugHunt;

// 각 랩은 "기대값 vs 실제값"을 출력한다.
// [FAIL]이 나오거나 프로그램이 멈추면, 그때부터 버그 추적 시작.
// what, message, reason 은 이미 고른 언어의 문구다 (랩에서 Text.L("…", "…") 로 넘긴다).
public static class LabKit
{
    public static int PassCount { get; private set; }
    public static int FailCount { get; private set; }

    public static bool Expect<T>(string what, T expected, T actual)
    {
        bool ok = EqualityComparer<T>.Default.Equals(expected, actual);
        if (ok) PassCount++; else FailCount++;
        Console.WriteLine("  " + (ok ? "[PASS] " : "[FAIL] ") + Text.L($"{what}  (기대: {Show(expected)}, 실제: {Show(actual)})", $"{what}  (期待: {Show(expected)}, 実際: {Show(actual)})"));
        return ok;
    }

    // 12부: 측정값이 한도 이하인가 (할당 바이트, 비교 횟수 등)
    public static bool ExpectAtMost(string what, long limit, long actual)
    {
        bool ok = actual <= limit;
        if (ok) PassCount++; else FailCount++;
        Console.WriteLine("  " + (ok ? "[PASS] " : "[FAIL] ") + Text.L($"{what}  (기대: {Show(limit)} 이하, 실제: {Show(actual)})", $"{what}  (期待: {Show(limit)} 以下, 実際: {Show(actual)})"));
        return ok;
    }

    // 12부: body 를 실행하는 동안 이 스레드가 할당한 바이트와 걸린 시간(ms)
    public static (long Bytes, long Ms) Measure(Action body)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        var watch = Stopwatch.StartNew();
        body();
        watch.Stop();
        return (GC.GetAllocatedBytesForCurrentThread() - before, watch.ElapsedMilliseconds);
    }

    // 채점기(BugHunt.Grader)가 실행한 프로세스인가
    public static bool IsGrading => Environment.GetEnvironmentVariable("BUGHUNT_GRADER") == "1";

    // 숨은 테스트 케이스. 0이면 화면에 보이는 기본 입력, 0이 아니면 그 값을 시드로 입력을 무작위로 만든다.
    // 채점기가 BUGHUNT_SEED로 넘기고, 직접 재현할 때는 BugHunt.exe 84 --seed 1234 처럼 쓴다.
    public static int Seed { get; internal set; } =
        int.TryParse(Environment.GetEnvironmentVariable("BUGHUNT_SEED"), out int seed) ? seed : 0;

    static Random? _rng;
    static Random Rng => _rng ??= new Random(Seed);

    // 기본 케이스에서는 visible, 숨은 케이스에서는 min~max(포함) 중 하나
    public static int Pick(int visible, int min, int max) => Seed == 0 ? visible : Rng.Next(min, max + 1);

    public static T Pick<T>(T visible, params T[] pool) => Seed == 0 ? visible : pool[Rng.Next(pool.Length)];

    // 11부: 멈출 수 있는 코드를 감시 타이머 아래에서 실행한다.
    // 본문 스레드는 우선순위를 높여 둔다. PC가 바빠도 정상 코드가 감시 시간 안에 끝나게 하려는 것뿐이다.
    // timeoutMs 안에 끝나지 않으면 [FAIL] 응답 없음을 출력하고 false를 돌려준다. (랩이 정말 영원히 멈추지는 않게)
    // 디버거가 붙어 있으면 실패로 끝내지 않고 계속 기다린다. 그때 디버그 → 모두 중단으로 멈춘 곳을 보면 된다.
    public static bool FinishesWithin(string what, Action body, int timeoutMs = 2000)
    {
        var worker = new Thread(() => body()) { IsBackground = true, Name = Text.L($"랩 본문 ({what})", $"ラボ本体 ({what})"), Priority = ThreadPriority.Highest };
        worker.Start();
        if (worker.Join(timeoutMs))
            return true;

        if (Debugger.IsAttached && !IsGrading)
        {
            Console.WriteLine("  >> " + Text.L($"{what}: {timeoutMs} ms가 지나도 안 끝난다. 지금 디버그 → 모두 중단(Ctrl+Alt+Break)을 눌러 보세요.", $"{what}: {timeoutMs} ms たっても終わりません。今 デバッグ → すべて中断(Ctrl+Alt+Break)を押してみてください。"));
            worker.Join();
            return true;
        }

        FailCount++;
        Console.WriteLine("  " + Text.L($"[FAIL] 응답 없음 ({timeoutMs} ms 초과): {what}", $"[FAIL] 応答なし ({timeoutMs} ms 超過): {what}"));
        PauseForTools(Text.L($"멈춘 스레드는 아직 그대로입니다. 다른 터미널에서 dotnet-stack report -p {Environment.ProcessId} 로 스택을 보거나, VS에서 디버그 → 프로세스에 연결 → 모두 중단 해 보세요.", $"止まったスレッドはまだそのままです。別のターミナルで dotnet-stack report -p {Environment.ProcessId} でスタックを見るか、VS で デバッグ → プロセスにアタッチ → すべて中断 をしてみてください。"));
        return false;
    }

    // 이 환경에서는 판정할 수 없는 랩. 채점기는 SKIP으로 센다.
    public static void Skip(string reason) => Console.WriteLine("  [SKIP] " + reason);

    // 사람이 실행할 때만 잠깐 멈춰서, 그동안 진단 도구(스냅샷, dotnet-dump 등)를 쓸 수 있게 한다.
    public static void PauseForTools(string message)
    {
        if (IsGrading || Console.IsInputRedirected)
            return;
        Console.WriteLine("  >> " + message);
        Console.Write("  >> " + Text.L($"(PID {Environment.ProcessId}) 계속하려면 Enter:", $"(PID {Environment.ProcessId}) 続けるには Enter:") + " ");
        Console.ReadLine();
    }

    // 랩이 CurrentCulture를 바꿔도 판정 출력은 항상 같은 모양으로 나오게 한다.
    static string Show<T>(T value) => value switch
    {
        null => "null",
        bool b => b ? "true" : "false",
        string s => $"\"{s}\"",
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "null",
    };
}
