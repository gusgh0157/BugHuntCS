namespace BugHunt;

// 서버에서 프로필을 받아 와 HUD 의 골드 표시를 갱신한다.
// HUD 는 게임 루프의 메인 스레드에서만 만져야 한다. (MainThread.cs 참고)
public static class Lab31
{
    class Hud(MainThread main)
    {
        public int Gold { get; private set; }
        public int WrongThreadUpdates { get; private set; }

        public void SetGold(int gold)
        {
            // 실제 엔진이었다면 여기서 바로 크래시가 나거나, 화면이 가끔 깨진다.
            if (Environment.CurrentManagedThreadId != main.ThreadId)
                WrongThreadUpdates++;
            Gold = gold;
        }
    }

    record Profile(string Name, int Gold);

    static async Task<Profile> FetchProfileAsync()
    {
        await Task.Delay(50);   // 네트워크
        return new Profile("Kael", 1500);
    }

    static async Task RefreshHudAsync(Hud hud)
    {
        Profile profile = await FetchProfileAsync().ConfigureAwait(false);
        hud.SetGold(profile.Gold);
        Console.WriteLine($"  HUD 갱신: {profile.Name} {profile.Gold}G");
    }

    public static void Run()
    {
        var main = new MainThread();
        var hud = new Hud(main);

        bool finished = main.Run(() => RefreshHudAsync(hud)).Wait(TimeSpan.FromSeconds(10));

        Expect("HUD 갱신 완료", true, finished);
        Expect("골드 표시", 1500, hud.Gold);
        Expect("메인 스레드가 아닌 곳에서 HUD 를 만진 횟수", 0, hud.WrongThreadUpdates);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// RefreshHudAsync 의 첫 줄과 hud.SetGold 줄에 중단점을 거세요.
// 각각 멈췄을 때 스레드 창(Ctrl+Alt+H)에서 노란 화살표가 있는 스레드의 이름을 보세요.
// 조사식에 System.Threading.SynchronizationContext.Current 를 넣어 비교해 보세요.
#endregion

#region 힌트 2
// await 한 줄을 사이에 두고 스레드가 바뀌었습니다. 무엇이 "돌아올 곳"을 정할까요?
#endregion

#region 힌트 3
// ConfigureAwait(false) 는 "끝나면 아무 스레드에서나 계속해도 된다"는 뜻입니다.
// 라이브러리 코드에서는 좋은 습관이지만, 그 뒤에서 UI/메인 스레드 전용 객체를 만지면 안 됩니다.
// 랩 27 과 비교해 보세요. 같은 도구가 한쪽에서는 해결책, 한쪽에서는 버그입니다.
#endregion
