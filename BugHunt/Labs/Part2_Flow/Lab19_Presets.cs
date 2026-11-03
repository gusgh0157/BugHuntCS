namespace BugHunt;

// 기본 직업 프리셋을 복사해서 플레이어만의 프리셋을 만든다.
public static class Lab19
{
    record CharacterPreset(string Name, int Level, List<string> Skills);

    static readonly CharacterPreset Warrior = new("Warrior", 1, ["Slash", "Guard"]);

    static CharacterPreset Customize(CharacterPreset basePreset, string name, string extraSkill)
    {
        CharacterPreset custom = basePreset with { Name = name };
        custom.Skills.Add(extraSkill);
        return custom;
    }

    public static void Run()
    {
        CharacterPreset kael = Customize(Warrior, "Kael의 전사", "Whirlwind");
        CharacterPreset mira = Customize(Warrior, "Mira의 전사", "Taunt");

        Console.WriteLine($"  {Warrior.Name}: {string.Join(", ", Warrior.Skills)}");
        Console.WriteLine($"  {kael.Name}: {string.Join(", ", kael.Skills)}");
        Console.WriteLine($"  {mira.Name}: {string.Join(", ", mira.Skills)}");

        Expect("기본 프리셋 스킬", "Slash, Guard", string.Join(", ", Warrior.Skills));
        Expect("Mira 프리셋 스킬", "Slash, Guard, Taunt", string.Join(", ", mira.Skills));
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// Customize 의 custom.Skills.Add 줄에 중단점을 거세요.
// 지역 창에서 basePreset.Skills 를 우클릭 → "개체 ID 만들기" 하면 {$1} 같은 표시가 붙습니다.
// 이제 custom.Skills 를 펼쳐 보세요. 무엇이 붙어 있나요?
#endregion

#region 힌트 2
// 조사식에 object.ReferenceEquals(basePreset, custom) 와
// object.ReferenceEquals(basePreset.Skills, custom.Skills) 를 넣어 보세요.
#endregion

#region 힌트 3
// with 는 새 레코드를 만들지만, 각 속성은 "그대로 복사"합니다. List 같은 참조 형식은 주소만 복사됩니다(얕은 복사).
#endregion
