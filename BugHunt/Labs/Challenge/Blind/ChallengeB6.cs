namespace BugHunt;

// 난이도와 경제 수치. 시작 골드는 난이도에 따라 정해진다.
public static class ChallengeB6
{
    enum Difficulty { Normal, Hard, Nightmare }

    static class Balance
    {
        public static readonly int StartingGold = Economy.BaseGold * 3;
        public static readonly Difficulty Mode = Pick(Difficulty.Hard, Difficulty.Nightmare);
        public static readonly int EnemyHpPercent = Mode switch { Difficulty.Hard => 150, Difficulty.Nightmare => 250, _ => 100 };
    }

    static class Economy
    {
        public static readonly int BaseGold = Balance.Mode switch { Difficulty.Hard => 60, Difficulty.Nightmare => 30, _ => 100 };
        public static readonly int PotionPrice = Balance.Mode == Difficulty.Normal ? 10 : 25;
    }

    public static void Run()
    {
        Console.WriteLine($"  난이도 {Balance.Mode}, 적 HP {Balance.EnemyHpPercent}%");
        Console.WriteLine($"  시작 골드 {Balance.StartingGold}, 물약 {Economy.PotionPrice}골드");

        int baseGold = Balance.Mode switch { Difficulty.Hard => 60, Difficulty.Nightmare => 30, _ => 100 };
        Expect("시작 골드", baseGold * 3, Balance.StartingGold);
        Expect("물약 값", 25, Economy.PotionPrice);
    }
}
