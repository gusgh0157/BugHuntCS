namespace BugHunt;

// 부대 편성. 전투가 끝나면 쓰러진 자리를 예비 병력으로 채운다.
public static class ChallengeB4
{
    abstract class Unit(string name, int power)
    {
        public string Name { get; } = name;
        public int Power { get; } = power;
        public bool Down { get; set; }
    }

    sealed class Archer(int n) : Unit($"궁수{n}", 3);
    sealed class Knight(int n) : Unit($"기사{n}", 5);
    sealed class Healer(int n) : Unit($"치유사{n}", 2);

    static Unit[] Recruit(string kind, int size) => kind switch
    {
        "archer" => Enumerable.Range(1, size).Select(n => new Archer(n)).ToArray(),
        "knight" => Enumerable.Range(1, size).Select(n => new Knight(n)).ToArray(),
        _ => Enumerable.Range(1, size).Select(n => new Healer(n)).ToArray(),
    };

    static void Refill(Unit[] squad, Queue<Unit> reserve)
    {
        for (int i = 0; i < squad.Length && reserve.Count > 0; i++)
            if (squad[i].Down)
                squad[i] = reserve.Dequeue();
    }

    public static void Run()
    {
        string kind = Pick("archer", "archer", "healer", "knight");
        int size = Pick(6, 4, 9);
        Unit[] squad = Recruit(kind, size);
        var reserve = new Queue<Unit>(kind == "knight" ? [new Archer(91), new Healer(92), new Knight(93)] : [new Knight(91), new Healer(92), new Archer(93)]);

        int fallen = Pick(2, 1, 3);
        for (int i = 0; i < fallen; i++)
            squad[i * 2 % size].Down = true;

        int expected = squad.Where(u => !u.Down).Sum(u => u.Power) + reserve.Take(squad.Count(u => u.Down)).Sum(u => u.Power);
        Refill(squad, reserve);
        Console.WriteLine($"  부대: {string.Join(", ", squad.Select(u => u.Name))}");
        Expect("부대 전투력", expected, squad.Where(u => !u.Down).Sum(u => u.Power));
    }
}
