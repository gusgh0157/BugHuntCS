namespace BugHunt;

// 서버의 전투 스레드 여러 개가 처치 기록을 하나의 Dictionary 에 남긴다. (몬스터 Id → 처치한 전투 스레드 번호)
// [JA] サーバーの戦闘スレッド複数が、討伐記録を一つの Dictionary に残します。(モンスター Id → 討伐した戦闘スレッドの番号)
public static class Lab27
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

        bool finished = FinishesWithin(Text.L("처치 기록 집계", "討伐記録の集計"), () =>
        {
            // (전투 스레드 우선순위를 높여 두는 것은 PC가 바빠도 스레드들이 정말 동시에 돌게 하려는 것뿐이다)
            // [JA] (戦闘スレッドの優先度を上げておくのは、PC が忙しくてもスレッドが本当に同時に動くようにするためだけです)
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
                        // [JA] 戦闘スレッドごとに担当するモンスター Id の範囲が違うので、キーは重なりません。
                        for (int i = 0; i < killsPerWorker; i++)
                            log.Record(worker * killsPerWorker + i, worker);
                    }
                    catch (Exception ex)
                    {
                        lock (errors)
                            errors.Add(Text.L($"전투 스레드 {worker}: {ex.GetType().Name}", $"戦闘スレッド {worker}: {ex.GetType().Name}"));
                    }
                }) { IsBackground = true, Name = Text.L($"전투 스레드 {w}", $"戦闘スレッド {w}"), Priority = ThreadPriority.Highest };
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
            Console.WriteLine(Text.L($"  예외: {error}", $"  例外: {error}"));
        Console.WriteLine(Text.L($"  처치 기록 {log.Count:N0}건 (전투 스레드 {workers}개 × {killsPerWorker:N0})", $"  討伐記録 {log.Count:N0} 件 (戦闘スレッド {workers} 個 × {killsPerWorker:N0})"));

        Expect(Text.L("집계 중 예외", "集計中の例外"), 0, errors.Count);
        Expect(Text.L("처치 기록 수", "討伐記録の数"), workers * killsPerWorker, log.Count);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 몇 번 실행해 보세요. 기록 수가 매번 다르고, 가끔은 예외가 나고, (.NET Framework 였다면 아주 가끔 멈추기도 했습니다.)
// 예외 설정(Ctrl+Alt+E)에서 Common Language Runtime Exceptions 를 모두 체크하고 F5. 어디서 멈추나요? 스레드 창에서 다른 전투 스레드들은 어디에 있나요?
//
// [JA] 何回か実行してみてください。記録数が毎回違い、ときどき例外が出て、(.NET Framework だったら、ごくまれに止まることもありました。)
// [JA] 例外設定(Ctrl+Alt+E)で Common Language Runtime Exceptions をすべてチェックして F5。どこで止まりますか? スレッド ウィンドウで、ほかの戦闘スレッドはどこにいますか?
#endregion

#region 힌트 2 / ヒント 2
// 병렬 스택 창에서 여러 스레드가 동시에 Dictionary.TryInsert 안에 있는 모습을 찾아보세요.
// Dictionary 는 칸이 모자라면 Resize 로 내부 배열을 새로 만들어 옮깁니다. 그 도중에 다른 스레드가 넣으면 어떻게 될까요?
//
// [JA] 並列スタック ウィンドウで、複数のスレッドが同時に Dictionary.TryInsert の中にいる様子を探してみてください。
// [JA] Dictionary は空きが足りなくなると Resize で内部配列を作り直して移します。その途中で別のスレッドが追加したらどうなるでしょう?
#endregion

#region 힌트 3 / ヒント 3
// Dictionary, List, HashSet 은 "한 번에 한 스레드만 고친다"는 가정으로 만들어졌습니다. 동시에 고치면 항목이 사라지고,
// 내부 연결이 꼬여 예외가 나거나 (옛 .NET Framework 에서는) 무한루프에 빠집니다. 키가 겹치지 않아도 마찬가지입니다.
// ConcurrentDictionary 를 쓰거나, 고치는 곳을 lock 으로 감싸세요. 스레드마다 따로 모았다가 마지막에 합치는 것도 좋습니다.
//
// [JA] Dictionary、List、HashSet は「一度に一つのスレッドだけが変更する」という前提で作られています。同時に変更すると項目が消え、
// [JA] 内部のつながりがもつれて例外が出たり、(古い .NET Framework では)無限ループに陥ったりします。キーが重ならなくても同じです。
// [JA] ConcurrentDictionary を使うか、変更する箇所を lock で囲んでください。スレッドごとに別々に集めて最後に合わせるのも良い方法です。
#endregion
