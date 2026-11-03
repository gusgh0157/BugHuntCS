using System.Diagnostics;
using System.Globalization;

namespace BugHunt;

// 각 랩은 "기대값 vs 실제값"을 출력한다.
// [FAIL]이 나오거나 프로그램이 멈추면, 그때부터 버그 추적 시작.
public static class LabKit
{
    public static int PassCount { get; private set; }
    public static int FailCount { get; private set; }

    public static bool Expect<T>(string what, T expected, T actual)
    {
        bool ok = EqualityComparer<T>.Default.Equals(expected, actual);
        if (ok) PassCount++; else FailCount++;
        Console.WriteLine($"  {(ok ? "[PASS]" : "[FAIL]")} {what}  (기대: {Show(expected)}, 실제: {Show(actual)})");
        return ok;
    }

    // 채점기(BugHunt.Grader)가 실행한 프로세스인가
    public static bool IsGrading => Environment.GetEnvironmentVariable("BUGHUNT_GRADER") == "1";

    // 숨은 테스트 케이스. 0이면 화면에 보이는 기본 입력, 0이 아니면 그 값을 시드로 입력을 무작위로 만든다.
    // 채점기가 BUGHUNT_SEED로 넘기고, 직접 재현할 때는 BugHunt.exe 36 --seed 1234 처럼 쓴다.
    public static int Seed { get; internal set; } =
        int.TryParse(Environment.GetEnvironmentVariable("BUGHUNT_SEED"), out int seed) ? seed : 0;

    static Random? _rng;
    static Random Rng => _rng ??= new Random(Seed);

    // 기본 케이스에서는 visible, 숨은 케이스에서는 min~max(포함) 중 하나
    public static int Pick(int visible, int min, int max) => Seed == 0 ? visible : Rng.Next(min, max + 1);

    public static T Pick<T>(T visible, params T[] pool) => Seed == 0 ? visible : pool[Rng.Next(pool.Length)];

    // 6부: 멈출 수 있는 코드를 감시 타이머 아래에서 실행한다.
    // timeoutMs 안에 끝나지 않으면 [FAIL] 응답 없음을 출력하고 false를 돌려준다. (랩이 정말 영원히 멈추지는 않게)
    // 디버거가 붙어 있으면 실패로 끝내지 않고 계속 기다린다. 그때 디버그 → 모두 중단으로 멈춘 곳을 보면 된다.
    public static bool FinishesWithin(string what, Action body, int timeoutMs = 2000)
    {
        var worker = new Thread(() => body()) { IsBackground = true, Name = $"랩 본문 ({what})" };
        worker.Start();
        if (worker.Join(timeoutMs))
            return true;

        if (Debugger.IsAttached && !IsGrading)
        {
            Console.WriteLine($"  >> {what}: {timeoutMs} ms가 지나도 안 끝난다. 지금 디버그 → 모두 중단(Ctrl+Alt+Break)을 눌러 보세요.");
            worker.Join();
            return true;
        }

        FailCount++;
        Console.WriteLine($"  [FAIL] 응답 없음 ({timeoutMs} ms 초과): {what}");
        PauseForTools($"멈춘 스레드는 아직 그대로입니다. 다른 터미널에서 dotnet-stack report -p {Environment.ProcessId} 로 스택을 보거나, VS에서 디버그 → 프로세스에 연결 → 모두 중단 해 보세요.");
        return false;
    }

    // 이 환경에서는 판정할 수 없는 랩. 채점기는 SKIP으로 센다.
    public static void Skip(string reason) => Console.WriteLine($"  [SKIP] {reason}");

    // 사람이 실행할 때만 잠깐 멈춰서, 그동안 진단 도구(스냅샷, dotnet-dump 등)를 쓸 수 있게 한다.
    public static void PauseForTools(string message)
    {
        if (IsGrading || Console.IsInputRedirected)
            return;
        Console.WriteLine($"  >> {message}");
        Console.Write($"  >> (PID {Environment.ProcessId}) 계속하려면 Enter: ");
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
