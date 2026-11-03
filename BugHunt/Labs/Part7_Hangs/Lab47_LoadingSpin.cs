namespace BugHunt;

// 로딩 스레드가 에셋을 읽는 동안 메인 스레드는 "로딩 중" 표시를 돌리며 기다린다.
// Debug 에서는 멀쩡하다. Release 로 빌드한 출시판에서만 로딩 화면이 끝나지 않는다는 제보가 왔다.
public static class Lab47
{
    class Loader
    {
        public bool Done;
        public int AssetsLoaded;

        public void LoadAll(int assets)
        {
            for (int i = 0; i < assets; i++)
            {
                Thread.Sleep(5);   // 디스크에서 읽는 척
                AssetsLoaded++;
            }
            Done = true;
        }
    }

    // 로딩이 끝날 때까지 기다린다. 기다리는 동안 몇 번 확인했는지 센다(로딩 아이콘 회전에 쓴다).
    static long WaitForLoading(Loader loader)
    {
        long spins = 0;
        while (!loader.Done)
            spins++;
        return spins;
    }

    public static void Run()
    {
        int assets = Pick(20, 5, 40);
        var loader = new Loader();
        var loadingThread = new Thread(() => loader.LoadAll(assets)) { IsBackground = true, Name = "로딩 스레드" };
        loadingThread.Start();

        long spins = 0;
        if (!FinishesWithin("로딩 대기", () => spins = WaitForLoading(loader)))
        {
            Console.WriteLine($"  (멈춘 순간 loader.Done = {loader.Done}, 읽은 에셋 {loader.AssetsLoaded}개)");
            return;
        }

        Console.WriteLine($"  로딩 끝: 에셋 {loader.AssetsLoaded}개 (기다리며 {spins:N0}번 확인)");
        Expect("읽은 에셋 수", assets, loader.AssetsLoaded);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// 이 랩은 Release 에서 해야 재현됩니다. 구성을 Release 로 바꾸고 F5, 또는 BugHunt\bin\Release\net10.0\BugHunt.exe 47.
// 멈추면 모두 중단(디버거 없이 실행했다면 디버그 → 프로세스에 연결). 스레드 창에서 "로딩 스레드"는 아직 있나요?
// 멈춘 순간 loader.Done 은 무엇인가요? 그런데 왜 while 은 계속 돌까요?
#endregion

#region 힌트 2
// "랩 본문" 스레드의 WaitForLoading 에서 디버그 → 창 → 디스어셈블리(Ctrl+Alt+D)를 열어 보세요.
// 루프 안에 loader.Done 을 메모리에서 읽는 명령이 있나요? (Release 에서는 지역 창 값도 "최적화되었습니다"로 보일 수 있습니다)
#endregion

#region 힌트 3
// JIT 최적화는 "이 루프 안에서 아무도 Done 을 바꾸지 않는다"고 보고, 읽기를 루프 밖으로 한 번만 꺼냅니다. 다른 스레드의 쓰기는 영영 안 보입니다.
// Debug 는 최적화를 끄기 때문에 매번 메모리에서 읽어서 우연히 됩니다.
// 다른 스레드가 바꾸는 플래그는 volatile 이나 Volatile.Read 로 읽으세요. 기다리는 게 목적이라면 바쁜 대기 대신
// ManualResetEventSlim, Task 처럼 기다림을 위한 도구를 쓰는 게 더 좋습니다.
#endregion
