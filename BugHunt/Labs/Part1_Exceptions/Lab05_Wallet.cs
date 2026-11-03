namespace BugHunt;

// 플레이어 지갑. 골드와 보석은 0 ~ 상한 사이로 유지된다.
public static class Lab05
{
    class Wallet
    {
        public const int MaxGold = 9999;
        public const int MaxGems = 500;

        int _gold;
        int _gems;

        public int Gems
        {
            get => _gems;
            set => _gems = Math.Clamp(value, 0, MaxGems);
        }

        public int Gold
        {
            get => _gold;
            set => Gold = Math.Clamp(value, 0, MaxGold);
        }

        public void AddGold(int amount) => Gold += amount;
        public void AddGems(int amount) => Gems += amount;

        // 새 시즌이 시작되면 지갑을 비운다.
        public void ResetSeason()
        {
            _gold = 0;
            _gems = 0;
        }
    }

    public static void Run()
    {
        var wallet = new Wallet();
        wallet.AddGems(30);
        Console.WriteLine($"  보석 {wallet.Gems}");

        wallet.AddGold(100);
        wallet.AddGold(200);
        Console.WriteLine($"  골드 {wallet.Gold}");
        Expect("골드 (100 + 200)", 300, wallet.Gold);

        wallet.AddGold(1_000_000);
        Expect("골드 상한", Wallet.MaxGold, wallet.Gold);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// 이번에는 Ctrl+F5(디버깅하지 않고 시작)로 한 번 실행해서 콘솔에 남는 글을 읽어 보세요.
// "Stack overflow." 아래 "Repeat N times:" 사이에 반복되는 줄이 무엇인가요?
// 그다음 F5로 실행해서 호출 스택 창을 보세요. 같은 줄이 몇 개나 쌓여 있나요?
#endregion

#region 힌트 2
// set_Gold 는 Gold 속성의 set 접근자가 컴파일된 이름입니다.
// set 접근자 안에서 "값을 저장하는 곳"이 어디인지 보세요. Gems 와 비교해 보세요.
// (ResetSeason 이 없었다면 컴파일러가 "_gold 에는 아무것도 대입되지 않는다(CS0649)"고 알려 줬을 겁니다.)
#endregion

#region 참고
// StackOverflowException 은 try/catch 로 잡을 수 없고, .NET 은 프로세스를 바로 끝냅니다.
// 그래서 다른 1부 랩처럼 "처리되지 않은 예외"를 보여 주고 기다리는 게 아니라 프로세스가 통째로 사라집니다.
// 디버거를 붙여 실행하면 VS 가 끝나기 직전에 멈춰서 호출 스택을 보여 주지만, 계속(F5)하면 그대로 종료됩니다.
#endregion
