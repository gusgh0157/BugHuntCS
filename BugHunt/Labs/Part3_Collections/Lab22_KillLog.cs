namespace BugHunt;

// 서버의 전투 스레드 여러 개가 처치 기록을 하나의 Dictionary 에 남긴다. (몬스터 Id → 처치한 전투 스레드 번호)
public static class Lab22
{
    class KillLog
    {
        readonly Dictionary<int, int> _kills = [];

        public void Record(int monsterId, int worker) => _kills[monsterId] = worker;

        public int Count => _kills.Count;
    }

    public static void Run()
    {
        int workers = Pick(4, 3, 6);
        int killsPerWorker = Pick(50_000, 30_000, 80_000);
        var log = new KillLog();
        var errors = new List<string>();

        bool finished = FinishesWithin("처치 기록 집계", () =>
        {
            using var go = new ManualResetEventSlim();
            var threads = new List<Thread>();
            for (int w = 0; w < workers; w++)
            {
                int worker = w;
                var t = new Thread(() =>
                {
                    go.Wait();
                    try
                    {
                        // 전투 스레드마다 맡은 몬스터 Id 구간이 달라서 키가 겹치지 않는다.
                        for (int i = 0; i < killsPerWorker; i++)
                            log.Record(worker * killsPerWorker + i, worker);
                    }
                    catch (Exception ex)
                    {
                        lock (errors)
                            errors.Add($"전투 스레드 {worker}: {ex.GetType().Name}");
                    }
                }) { IsBackground = true, Name = $"전투 스레드 {w}" };
                threads.Add(t);
                t.Start();
            }
            go.Set();
            foreach (Thread t in threads)
                t.Join();
        });
        if (!finished)
            return;

        foreach (string error in errors.Distinct())
            Console.WriteLine($"  예외: {error}");
        Console.WriteLine($"  처치 기록 {log.Count:N0}건 (전투 스레드 {workers}개 × {killsPerWorker:N0})");

        Expect("집계 중 예외", 0, errors.Count);
        Expect("처치 기록 수", workers * killsPerWorker, log.Count);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// 몇 번 실행해 보세요. 기록 수가 매번 다르고, 가끔은 예외가 나고, 아주 가끔은 멈추기도 합니다.
// 예외 설정(Ctrl+Alt+E)에서 Common Language Runtime Exceptions 를 모두 체크하고 F5. 어디서 멈추나요? 스레드 창에서 다른 전투 스레드들은 어디에 있나요?
#endregion

#region 힌트 2
// 병렬 스택 창에서 여러 스레드가 동시에 Dictionary.TryInsert 안에 있는 모습을 찾아보세요.
// Dictionary 는 칸이 모자라면 Resize 로 내부 배열을 새로 만들어 옮깁니다. 그 도중에 다른 스레드가 넣으면 어떻게 될까요?
#endregion

#region 힌트 3
// Dictionary, List, HashSet 은 "한 번에 한 스레드만 고친다"는 가정으로 만들어졌습니다. 동시에 고치면 항목이 사라지고,
// 내부 연결이 꼬여 예외가 나거나 (옛 .NET Framework 에서는) 무한루프에 빠집니다. 키가 겹치지 않아도 마찬가지입니다.
// ConcurrentDictionary 를 쓰거나, 고치는 곳을 lock 으로 감싸세요. 스레드마다 따로 모았다가 마지막에 합치는 것도 좋습니다.
#endregion
