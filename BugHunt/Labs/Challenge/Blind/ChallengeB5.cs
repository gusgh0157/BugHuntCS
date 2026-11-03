namespace BugHunt;

// 자동 저장. 게임을 시작할 때 타이머를 걸어 두면 일정 간격으로 저장한다.
public static class ChallengeB5
{
    static int _saves;

    static void StartAutoSave(int intervalMs)
    {
        var timer = new Timer(_ => Interlocked.Increment(ref _saves), null, intervalMs, intervalMs);
        Console.WriteLine($"  자동 저장 시작 ({intervalMs}ms마다)");
    }

    // 게임 진행: 매 프레임 이것저것 만들고 버린다. 가끔 GC 가 돈다.
    static void PlayFor(int ms)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        int frame = 0;
        while (watch.ElapsedMilliseconds < ms)
        {
            _ = new byte[64 * 1024];
            if (++frame % 10 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Thread.Sleep(5);
        }
    }

    public static void Run()
    {
        _saves = 0;
        int interval = Pick(20, 15, 40);
        int playMs = Pick(800, 600, 1200);

        StartAutoSave(interval);
        PlayFor(playMs);

        int saves = Volatile.Read(ref _saves);
        int atLeast = playMs / interval / 2;
        Console.WriteLine($"  {playMs}ms 동안 저장 {saves}번");
        Expect($"저장 횟수가 {atLeast}번 이상", true, saves >= atLeast);
    }
}
