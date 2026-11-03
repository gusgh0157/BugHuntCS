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

    // 채점기(BugHunt.exe grade)가 실행한 프로세스인가
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
