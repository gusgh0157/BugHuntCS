namespace BugHunt;

// 달리기는 스태미나를 30 쓴다. 스태미나가 모자라면 달릴 수 없다.
public static class Lab10
{
    struct Stamina(int max)
    {
        public int Current = max;
        public int Max = max;

        public bool TryUse(int amount)
        {
            if (Current < amount)
                return false;
            Current -= amount;
            return true;
        }
    }

    class Runner
    {
        public const int SprintCost = 30;

        readonly Stamina _stamina = new(100);

        public int StaminaLeft => _stamina.Current;

        public bool Sprint() => _stamina.TryUse(SprintCost);
    }

    public static void Run()
    {
        var runner = new Runner();
        int sprints = 0;
        for (int i = 0; i < 4; i++)
        {
            bool ok = runner.Sprint();
            Console.WriteLine($"  달리기 {i + 1}: {(ok ? "성공" : "스태미나 부족")}, 남은 스태미나 {runner.StaminaLeft}");
            if (ok) sprints++;
        }

        Expect("성공한 달리기 (100 / 30)", 3, sprints);
        Expect("남은 스태미나", 10, runner.StaminaLeft);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// TryUse 안의 Current -= amount; 줄에 중단점을 걸고, F10 으로 넘긴 뒤 this.Current 를 보세요. 줄었나요?
// 그다음 Shift+F11 로 Sprint 로 나와서 조사식에 _stamina.Current 를 넣어 보세요.
#endregion

#region 힌트 2
// 방금 TryUse 가 바꾼 this 는 _stamina 그 자체였을까요?
// Stamina 는 struct(값 형식)이고, _stamina 필드에는 어떤 한정자가 붙어 있나요?
#endregion

#region 힌트 3
// 컴파일러는 readonly 필드가 바뀌지 않도록, 그 필드의 메서드를 부를 때 "복사본"을 만들어서 부릅니다(방어적 복사).
// 그래서 경고도 오류도 없이 복사본만 줄어듭니다.
#endregion
