namespace BugHunt;

// 파티원 네 명의 전투 기록을 동시에 처리해서 총 피해량을 집계한다.
// [JA] パーティーメンバー四人の戦闘記録を同時に処理して、総ダメージを集計します。
public static class Lab66
{
    class DamageMeter
    {
        public long TotalDamage;
        public int Hits;

        public void Record(int damage)
        {
            TotalDamage += damage;
            Hits++;
        }
    }

    public static void Run()
    {
        const int HitsPerMember = 2_000_000;
        string[] party = ["Kael", "Mira", "Doran", "Selene"];
        var meter = new DamageMeter();
        if (Environment.ProcessorCount < 2)
        {
            Skip("CPU 코어가 하나라 스레드가 동시에 돌지 않는다");
            return;
        }

        // 파티원마다 전투 기록 처리 스레드를 하나씩 두고, 다 준비되면 동시에 시작한다.
        // (우선순위를 높여 두는 것은 PC가 바빠도 네 스레드가 정말 동시에 돌게 하려는 것뿐이다)
        using var startTogether = new Barrier(party.Length);
        Thread[] workers = party.Select(member => new Thread(() =>
        {
            startTogether.SignalAndWait();
            for (int i = 0; i < HitsPerMember; i++)
                meter.Record(10);
        }) { Name = $"Combat log: {member}", Priority = ThreadPriority.Highest }).ToArray();

        foreach (Thread worker in workers) worker.Start();
        foreach (Thread worker in workers) worker.Join();

        Console.WriteLine($"  타격 {meter.Hits:N0}회, 총 피해 {meter.TotalDamage:N0}");
        Expect("타격 횟수", party.Length * HitsPerMember, meter.Hits);
        Expect("총 피해량", party.Length * HitsPerMember * 10L, meter.TotalDamage);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 여러 번 실행해 보세요(Ctrl+F5). 결과가 매번 다르면 무엇을 의심해야 할까요?
//
// [JA] 何回か実行してみてください(Ctrl+F5)。結果が毎回違うなら、何を疑うべきでしょう?
#endregion

#region 힌트 2 / ヒント 2
// Record 안에 중단점을 걸고 디버그 → 창 → 병렬 스택 을 여세요. Record 안에 동시에 몇 개의 스레드가 있나요?
// 스레드 창(Ctrl+Alt+H)에서 스레드를 바꿔 가며 각 스레드가 어느 줄에 있는지 보세요.
// 중단점 우클릭 → 조건 → "필터"에 ThreadId = 숫자 를 넣으면 한 스레드에서만 멈추게 할 수 있습니다.
//
// [JA] Record の中にブレークポイントを置き、デバッグ → ウィンドウ → 並列スタック を開いてください。Record の中に同時にいくつのスレッドがいますか?
// [JA] スレッド ウィンドウ(Ctrl+Alt+H)でスレッドを切り替えながら、各スレッドがどの行にいるか見てください。
// [JA] ブレークポイントを右クリック → 条件 → 「フィルター」に ThreadId = 数値 を入れると、一つのスレッドだけで止まるようにできます。
#endregion

#region 힌트 3 / ヒント 3
// TotalDamage += damage 는 한 줄이지만 "읽기 → 더하기 → 쓰기" 세 단계입니다.
// 두 스레드가 같은 값을 읽고 각자 더해서 쓰면, 한쪽의 더하기는 사라집니다.
//
// [JA] TotalDamage += damage は一行ですが、「読む → 足す → 書く」の三段階です。
// [JA] 二つのスレッドが同じ値を読んでそれぞれ足して書くと、片方の足し算は消えます。
#endregion
