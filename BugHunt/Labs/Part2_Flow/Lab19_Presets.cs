namespace BugHunt;

// 기본 직업 프리셋을 복사해서 플레이어만의 프리셋을 만든다.
// [JA] 基本職のプリセットをコピーして、プレイヤー専用のプリセットを作ります。
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
        CharacterPreset kael = Customize(Warrior, Text.L("Kael의 전사", "Kaelの戦士"), "Whirlwind");
        CharacterPreset mira = Customize(Warrior, Text.L("Mira의 전사", "Miraの戦士"), "Taunt");

        Console.WriteLine($"  {Warrior.Name}: {string.Join(", ", Warrior.Skills)}");
        Console.WriteLine($"  {kael.Name}: {string.Join(", ", kael.Skills)}");
        Console.WriteLine($"  {mira.Name}: {string.Join(", ", mira.Skills)}");

        Expect(Text.L("기본 프리셋 스킬", "基本プリセットのスキル"), "Slash, Guard", string.Join(", ", Warrior.Skills));
        Expect(Text.L("Mira 프리셋 스킬", "Mira のプリセットのスキル"), "Slash, Guard, Taunt", string.Join(", ", mira.Skills));
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// Customize 의 custom.Skills.Add 줄에 중단점을 거세요.
// 지역 창에서 basePreset.Skills 를 우클릭 → "개체 ID 만들기" 하면 {$1} 같은 표시가 붙습니다.
// 이제 custom.Skills 를 펼쳐 보세요. 무엇이 붙어 있나요?
//
// [JA] Customize の custom.Skills.Add の行にブレークポイントを置いてください。
// [JA] ローカル ウィンドウで basePreset.Skills を右クリック → 「オブジェクト ID の作成」をすると {$1} のような表示が付きます。
// [JA] では custom.Skills を展開してみてください。何が付いていますか?
#endregion

#region 힌트 2 / ヒント 2
// 조사식에 object.ReferenceEquals(basePreset, custom) 와
// object.ReferenceEquals(basePreset.Skills, custom.Skills) 를 넣어 보세요.
//
// [JA] ウォッチに object.ReferenceEquals(basePreset, custom) と
// [JA] object.ReferenceEquals(basePreset.Skills, custom.Skills) を入れてみてください。
#endregion

#region 힌트 3 / ヒント 3
// with 는 새 레코드를 만들지만, 각 속성은 "그대로 복사"합니다. List 같은 참조 형식은 주소만 복사됩니다(얕은 복사).
//
// [JA] with は新しいレコードを作りますが、各プロパティは「そのままコピー」します。List のような参照型はアドレスだけがコピーされます(浅いコピー)。
#endregion
