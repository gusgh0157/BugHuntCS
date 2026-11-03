namespace BugHunt;

// 레이드 보스가 쓰러지면 파티원별 기여도(%)를 계산해 보상 순서를 정한다.
public static class Lab14
{
    record Attacker(string Name, int Damage);

    static int ContributionPercent(int damage, int totalDamage) => damage * 100 / totalDamage;

    public static void Run()
    {
        // 월드 보스 HP 50,000,000
        Attacker[] party =
        [
            new("Kael", 30_000_000),
            new("Mira", 15_000_000),
            new("Doran", 5_000_000),
        ];
        int total = party.Sum(a => a.Damage);

        var percents = new List<int>();
        foreach (Attacker a in party)
        {
            int percent = ContributionPercent(a.Damage, total);
            Console.WriteLine($"  {a.Name}: 피해 {a.Damage:N0}, 기여도 {percent}%");
            percents.Add(percent);
        }

        Expect("기여도 (Kael, Mira, Doran)", "60, 30, 10", string.Join(", ", percents));
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// ContributionPercent 에 조건부 중단점을 거세요. 조건: damage == 30000000
// 조사식에 damage * 100 을 넣어 보세요. 3,000,000,000 이 나오나요?
#endregion

#region 힌트 2
// 조사식에 int.MaxValue 도 넣어 보세요. 그리고 checked(damage * 100) 을 넣어 보세요.
// C# 의 int 연산은 기본적으로 넘쳐도 예외 없이 조용히 한 바퀴 돕니다(unchecked).
#endregion

#region 힌트 3
// 프로젝트 속성 → 빌드 → 고급 → "산술 오버플로 검사"(CheckForOverflowUnderflow)를 켜고 다시 실행해 보세요.
// 이번에는 어디서 멈추나요? 고칠 때는 계산 중간값이 int 를 넘지 않게 해야 합니다.
#endregion
