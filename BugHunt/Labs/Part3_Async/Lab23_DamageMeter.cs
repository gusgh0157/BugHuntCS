namespace BugHunt;

// 파티원 네 명의 전투 기록을 동시에 처리해서 총 피해량을 집계한다.
public static class Lab23
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
        const int HitsPerMember = 200_000;
        string[] party = ["Kael", "Mira", "Doran", "Selene"];
        var meter = new DamageMeter();

        // 파티원마다 전투 기록을 따로 처리한다.
        Parallel.ForEach(party, new ParallelOptions { MaxDegreeOfParallelism = party.Length }, member =>
        {
            for (int i = 0; i < HitsPerMember; i++)
                meter.Record(10);
        });

        Console.WriteLine($"  타격 {meter.Hits:N0}회, 총 피해 {meter.TotalDamage:N0}");
        Expect("타격 횟수", party.Length * HitsPerMember, meter.Hits);
        Expect("총 피해량", party.Length * HitsPerMember * 10L, meter.TotalDamage);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// 여러 번 실행해 보세요(Ctrl+F5). 결과가 매번 다르면 무엇을 의심해야 할까요?
#endregion

#region 힌트 2
// Record 안에 중단점을 걸고 디버그 → 창 → 병렬 스택 을 여세요. Record 안에 동시에 몇 개의 스레드가 있나요?
// 스레드 창(Ctrl+Alt+H)에서 스레드를 바꿔 가며 각 스레드가 어느 줄에 있는지 보세요.
// 중단점 우클릭 → 조건 → "필터"에 ThreadId = 숫자 를 넣으면 한 스레드에서만 멈추게 할 수 있습니다.
#endregion

#region 힌트 3
// TotalDamage += damage 는 한 줄이지만 "읽기 → 더하기 → 쓰기" 세 단계입니다.
// 두 스레드가 같은 값을 읽고 각자 더해서 쓰면, 한쪽의 더하기는 사라집니다.
#endregion
