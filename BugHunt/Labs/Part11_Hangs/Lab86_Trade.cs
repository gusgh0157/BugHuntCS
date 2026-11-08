namespace BugHunt;

// 두 플레이어가 서로에게 동시에 골드를 보낸다. 거래 중에는 두 사람의 지갑을 모두 잠근다.
// [JA] 二人のプレイヤーがお互いに同時にゴールドを送ります。取引中は二人の財布を両方ロックします。
public static class Lab86
{
    class Player(int id, string name, int gold)
    {
        public int Id { get; } = id;
        public string Name { get; } = name;
        public int Gold { get; set; } = gold;
        public object Sync { get; } = new();
    }

    // 거래 기록을 서버에 남긴다(느리다). 이 함수에는 버그가 없다.
    static void WriteTradeLog(Player from, Player to, int gold) => Thread.Sleep(100);

    static void Transfer(Player from, Player to, int gold)
    {
        lock (from.Sync)
        {
            WriteTradeLog(from, to, gold);
            lock (to.Sync)
            {
                from.Gold -= gold;
                to.Gold += gold;
            }
        }
    }

    public static void Run()
    {
        var kael = new Player(1, "Kael", Pick(500, 100, 900));
        var mira = new Player(2, "Mira", Pick(300, 100, 900));
        int kaelGives = Pick(120, 1, 100), miraGives = Pick(80, 1, 100);
        int total = kael.Gold + mira.Gold;
        int expectedKael = kael.Gold - kaelGives + miraGives;

        bool done = FinishesWithin("동시 거래", () =>
        {
            using var go = new ManualResetEventSlim();
            var a = new Thread(() => { go.Wait(); Transfer(kael, mira, kaelGives); }) { IsBackground = true, Name = Text.T("거래: Kael → Mira") };
            var b = new Thread(() => { go.Wait(); Transfer(mira, kael, miraGives); }) { IsBackground = true, Name = Text.T("거래: Mira → Kael") };
            a.Start();
            b.Start();
            go.Set();
            a.Join();
            b.Join();
        });
        if (!done)
            return;

        Console.WriteLine($"  거래 끝: Kael {kael.Gold}, Mira {mira.Gold}");
        Expect("Kael 골드", expectedKael, kael.Gold);
        Expect("두 사람 골드 합", total, kael.Gold + mira.Gold);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 멈추면 모두 중단 → 디버그 → 창 → 병렬 스택(Ctrl+Shift+D, S). "거래: Kael → Mira" 와 "거래: Mira → Kael" 스레드는 각각 어디서 멈췄나요?
// 두 스레드의 Transfer 프레임을 하나씩 더블클릭해서 지역 창의 from, to 를 보세요.
//
// [JA] 止まったら すべて中断 → デバッグ → ウィンドウ → 並列スタック(Ctrl+Shift+D, S)。「거래: Kael → Mira」と「거래: Mira → Kael」のスレッドは、それぞれどこで止まっていますか?
// [JA] 二つのスレッドの Transfer フレームを一つずつダブルクリックして、ローカル ウィンドウの from、to を見てください。
#endregion

#region 힌트 2 / ヒント 2
// 스레드 창(Ctrl+Alt+H)에서 두 스레드의 "위치"를 보세요. 둘 다 Monitor.Enter(= lock) 에서 기다리고 있다면, 각자 무엇을 이미 쥐고 있나요?
// 명령줄로는: dotnet-stack report -p <PID>, 또는 dotnet-dump analyze 에서 syncblk 명령이 누가 어떤 잠금을 쥐었는지 보여 줍니다.
//
// [JA] スレッド ウィンドウ(Ctrl+Alt+H)で二つのスレッドの「場所」を見てください。どちらも Monitor.Enter(= lock)で待っているなら、それぞれ何をすでに握っていますか?
// [JA] コマンド ラインでは: dotnet-stack report -p <PID>、または dotnet-dump analyze の syncblk コマンドが、誰がどのロックを握っているかを見せてくれます。
#endregion

#region 힌트 3 / ヒント 3
// Kael→Mira 는 Kael 을 잠그고 Mira 를 기다리고, Mira→Kael 은 Mira 를 잠그고 Kael 을 기다립니다. 고전적인 교착 상태입니다.
// 잠금 여러 개를 잡을 때는 모든 곳에서 "같은 순서"로 잡으세요. (예: Id 가 작은 플레이어부터)
//
// [JA] Kael→Mira は Kael をロックして Mira を待ち、Mira→Kael は Mira をロックして Kael を待ちます。典型的なデッドロックです。
// [JA] 複数のロックを取るときは、すべての場所で「同じ順番」で取ってください。(例: Id の小さいプレイヤーから)
#endregion
