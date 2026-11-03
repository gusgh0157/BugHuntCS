namespace BugHunt;

// 광역 공격이 파티 전원에게 피해를 준다. 파티원 정보는 할당을 줄이려고 struct 로 들고 있다.
public static class Lab42
{
    struct Member(string name, int hp)
    {
        public string Name = name;
        public int Hp = hp;

        public void TakeDamage(int damage) => Hp = Math.Max(0, Hp - damage);
    }

    static void AreaAttack(List<Member> party, int damage)
    {
        // party[i].Hp -= damage; 라고 쓰면 컴파일 오류(CS1612)가 나서 메서드로 바꿨다.
        for (int i = 0; i < party.Count; i++)
            party[i].TakeDamage(damage);
    }

    public static void Run()
    {
        string[] names = ["Kael", "Mira", "Doran", "Lia", "Sven"];
        int size = Pick(3, 2, names.Length);
        var party = new List<Member>();
        for (int i = 0; i < size; i++)
            party.Add(new Member(names[i], Pick(100 + i * 20, 50, 300)));
        int[] before = [.. party.Select(m => m.Hp)];

        int damage = Pick(30, 5, 120);
        AreaAttack(party, damage);
        Console.WriteLine($"  광역 공격 {damage}: {string.Join(", ", party.Select(m => $"{m.Name} {m.Hp}"))}");

        Expect("공격 뒤 파티 HP", string.Join(", ", before.Select(hp => Math.Max(0, hp - damage))), string.Join(", ", party.Select(m => m.Hp)));
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// TakeDamage 안에 중단점을 거세요. F10 으로 Hp 가 줄어드는 것을 확인하고, Shift+F11 로 AreaAttack 에 나와 조사식에 party[i].Hp 를 넣어 보세요.
#endregion

#region 힌트 2
// TakeDamage 안에서 조사식 this 에 개체 ID 를 만들 수 있나요? (struct 는 값이라 만들 수 없습니다)
// List<T> 의 인덱서 party[i] 는 무엇을 돌려주나요? 배열의 arr[i] 와 무엇이 다를까요? (배열은 그 칸 자체, List 는 복사본)
#endregion

#region 힌트 3
// party[i].Hp -= damage 가 CS1612 로 막힌 이유가 바로 그것입니다. 메서드 호출로 바꾸면 컴파일러는 막지 않지만, 고치는 것은 여전히 복사본입니다.
// 꺼내서 고치고 다시 넣거나(var m = party[i]; m.TakeDamage(d); party[i] = m;), CollectionsMarshal.AsSpan(party) 로 칸 자체를 고치세요.
// 또는 바뀌는 데이터라면 class 가 더 맞을 수도 있습니다.
#endregion
