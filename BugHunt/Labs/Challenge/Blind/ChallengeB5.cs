namespace BugHunt;

// 자동 저장. 게임을 시작할 때 타이머를 걸어 두면 일정 간격으로 저장한다.
// [JA] オートセーブ。ゲーム開始時にタイマーを仕掛けておくと、一定間隔で保存します。
public static class ChallengeB5
{
    static int _saves;

    static void StartAutoSave(int intervalMs)
    {
        var timer = new Timer(_ => Interlocked.Increment(ref _saves), null, intervalMs, intervalMs);
        Console.WriteLine($"  자동 저장 시작 ({intervalMs}ms마다)");
    }

    // 게임 한 프레임: 이것저것 만들고 버린다. 가끔 GC 가 돈다.
    static void PlayFrame(int frame)
    {
        _ = new byte[64 * 1024];
        if (frame % 10 == 1)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        Thread.Sleep(5);
    }

    public static void Run()
    {
        _saves = 0;
        int interval = Pick(20, 15, 40);
        int wanted = Pick(10, 8, 12);

        StartAutoSave(interval);

        // 자동 저장이 wanted 번 될 때까지 게임을 계속한다.
        int frames = 0;
        if (FinishesWithin($"자동 저장 {wanted}번", () =>
            {
                while (Volatile.Read(ref _saves) < wanted)
                    PlayFrame(++frames);
            }))
        {
            int saves = Volatile.Read(ref _saves);
            Console.WriteLine($"  {frames}프레임 동안 저장 {saves}번");
            Expect($"저장 횟수가 {wanted}번 이상", true, saves >= wanted);
        }
        else
            Console.WriteLine($"  (멈춘 순간 {frames}프레임, 저장 {Volatile.Read(ref _saves)}번)");
    }
}
