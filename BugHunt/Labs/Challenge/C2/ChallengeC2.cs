using BugHunt.Challenge.C2;

namespace BugHunt;

public static class ChallengeC2
{
    // 프레임마다 일어나는 일. ChannelMove 면 그 프레임 처음에 채널을 옮긴다.
    record FrameScript(bool ChannelMove, (string Quest, int Amount)[] Kills);

    static List<FrameScript> Script()
    {
        if (Seed == 0)
        {
            return
            [
                new(false, [("slime", 1), ("slime", 1), ("wolf", 2), ("boss", 1)]),   // 로그인 직후 광역기 한 방
                new(false, [("slime", 1)]),
                new(false, [("wolf", 1)]),
            ];
        }

        var rng = new Random(Seed);
        string[] quests = ["slime", "wolf", "boss", "bat"];
        var frames = new List<FrameScript>();
        int count = rng.Next(8, 15);
        int forcedMove = rng.Next(3, count);
        for (int f = 0; f < count; f++)
        {
            if (f == forcedMove)
            {
                // 채널을 옮긴 직후 서로 다른 몬스터를 한꺼번에 잡는 프레임
                string a = quests[rng.Next(quests.Length)];
                string b = quests.First(q => q != a);
                frames.Add(new(true, [(a, 2), (b, 1)]));
                continue;
            }
            bool move = f > 0 && rng.Next(6) == 0;
            var kills = new (string, int)[rng.Next(0, 5)];
            for (int k = 0; k < kills.Length; k++)
                kills[k] = (quests[rng.Next(quests.Length)], rng.Next(1, 4));
            frames.Add(new(move, kills));
        }
        return frames;
    }

    public static void Run()
    {
        // 이 랩의 코드는 모두 게임 루프 스레드에서 돈다 (await 뒤도 게임 루프로 돌아온다)
        var loop = new GameLoop();
        SynchronizationContext? previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(loop);
        try
        {
            Play(loop);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    static void Play(GameLoop loop)
    {
        var goals = new Dictionary<string, int> { ["slime"] = 3, ["wolf"] = 3, ["boss"] = 3, ["bat"] = 5 };
        var server = new ProgressServer(loop);
        var store = new AchievementStore(server);
        var bus = new QuestEventBus(loop);
        var tracker = new AchievementTracker(store, goals);
        tracker.Attach(bus);

        List<FrameScript> script = Script();
        foreach (FrameScript frame in script)
        {
            loop.RunFrame(() =>
            {
                if (frame.ChannelMove)
                {
                    Console.WriteLine($"  (프레임 {loop.Frame}: 채널 이동)");
                    store.Invalidate();
                }
                foreach ((string quest, int amount) in frame.Kills)
                    bus.Publish(quest, amount);
            });
        }
        for (int i = 0; i < 5 && bus.Pending > 0; i++)
            loop.RunFrame(() => { });
        Task reload = store.LoadAsync();   // 마지막에 채널을 옮겼으면 다시 읽어 와서 확인한다
        for (int i = 0; i < 5 && !reload.IsCompleted; i++)
            loop.RunFrame(() => { });

        foreach (Exception ex in bus.Faults)
            Console.WriteLine($"  처리기 예외: {ex.GetType().Name}: {ex.Message}");

        var total = new Dictionary<string, int>();
        foreach (FrameScript frame in script)
            foreach ((string quest, int amount) in frame.Kills)
                total[quest] = total.GetValueOrDefault(quest) + amount;

        Console.WriteLine($"  프레임 {loop.Frame}개, 처리한 알림 {tracker.Handled}개, 서버 읽기 {server.Reads}번");
        string Show(Func<string, int> progress) => string.Join(", ", goals.Keys.Select(q => $"{q} {progress(q)}"));
        Expect("업적 진행도", Show(q => total.GetValueOrDefault(q)), Show(store.Get));
        Expect("달성한 업적",
            string.Join(", ", goals.Keys.Where(q => total.GetValueOrDefault(q) >= goals[q])),
            string.Join(", ", goals.Keys.Where(store.Unlocked.Contains)));
    }
}
