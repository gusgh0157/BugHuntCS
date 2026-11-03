namespace BugHunt;

// 숫자 키 1, 2, 3 에 스킬 바의 슬롯을 연결한다.
public static class Lab09
{
    static readonly Dictionary<int, string> SkillBar = new()
    {
        [1] = "Fireball",
        [2] = "Ice Lance",
        [3] = "Heal",
    };

    static string CastSlot(int slot) =>
        SkillBar.TryGetValue(slot, out string? skill) ? skill : "(빈 슬롯)";

    static Dictionary<ConsoleKey, Func<string>> BindHotkeys()
    {
        var hotkeys = new Dictionary<ConsoleKey, Func<string>>();
        for (int slot = 1; slot <= SkillBar.Count; slot++)
        {
            ConsoleKey key = ConsoleKey.D0 + slot;
            hotkeys[key] = () => CastSlot(slot);
        }
        return hotkeys;
    }

    public static void Run()
    {
        Dictionary<ConsoleKey, Func<string>> hotkeys = BindHotkeys();

        var cast = new List<string>();
        foreach (ConsoleKey pressed in new[] { ConsoleKey.D1, ConsoleKey.D2, ConsoleKey.D3 })
        {
            string skill = hotkeys[pressed]();
            Console.WriteLine($"  {pressed} → {skill}");
            cast.Add(skill);
        }

        Expect("1, 2, 3 을 눌러 쓴 스킬", "Fireball, Ice Lance, Heal", string.Join(", ", cast));
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// CastSlot 의 첫 줄에 중단점을 걸고 실행하세요. 첫 번째로 멈췄을 때 slot 은 몇인가요?
// 기대: 1 키를 눌렀으니 1.
#endregion

#region 힌트 2
// 호출 스택에서 한 칸 아래 프레임(람다)을 더블클릭하세요. 이름에 <BindHotkeys>b__ 같은 것이 붙어 있습니다.
// 지역 창에서 this 를 펼쳐 보세요. 람다가 들고 있는 slot 은 "값"인가요, 아니면 어떤 객체의 필드인가요?
#endregion

#region 힌트 3
// BindHotkeys 의 람다 줄에 중단점을 걸고 개체 ID 를 만들어(지역 창에서 우클릭 → 개체 ID 만들기) 보세요.
// 세 번 만들어진 람다가 들고 있는 캡처 객체는 몇 개인가요? 루프가 끝났을 때 그 객체의 slot 은?
#endregion
