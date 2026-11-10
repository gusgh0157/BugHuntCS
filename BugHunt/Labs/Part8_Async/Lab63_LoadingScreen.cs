namespace BugHunt;

// 로딩 화면에서 에셋을 비동기로 읽고, 다 읽으면 로딩 화면을 닫는다.
// 화면(LoadingScreen)은 게임 루프의 메인 스레드에서만 만져야 한다. (MainThread.cs 참고)
// [JA] ロード画面でアセットを非同期に読み込み、すべて読み終えたらロード画面を閉じます。
// [JA] 画面(LoadingScreen)はゲームループのメインスレッドでしか触ってはいけません。(MainThread.cs を参照)
public static class Lab63
{
    class LoadingScreen
    {
        public bool Visible { get; private set; }
        public int AssetsLoaded { get; private set; }

        public void Show() => Visible = true;

        public void Close(int assetsLoaded)
        {
            AssetsLoaded = assetsLoaded;
            Visible = false;
        }
    }

    static readonly string[] Assets = ["terrain", "hero", "monsters", "music", "ui"];

    static async Task<int> LoadAssetsAsync()
    {
        int loaded = 0;
        foreach (string asset in Assets)
        {
            await Task.Delay(20);   // 디스크에서 읽는 척 / ディスクから読むふり
            loaded++;
            Console.WriteLine(Text.L($"  {asset} 로드 ({loaded * 100 / Assets.Length}%)", $"  {asset} ロード ({loaded * 100 / Assets.Length}%)"));
        }
        return loaded;
    }

    static void OnEnterLoadingScreen(LoadingScreen screen)
    {
        screen.Show();
        int loaded = LoadAssetsAsync().Result;
        screen.Close(loaded);
    }

    public static void Run()
    {
        var main = new MainThread();
        var screen = new LoadingScreen();

        Task frame = main.Run(() => OnEnterLoadingScreen(screen));

        TimeSpan limit = TimeSpan.FromSeconds(IsGrading ? 3 : 30);
        if (!frame.Wait(TimeSpan.FromSeconds(2)))
        {
            Console.WriteLine(Text.L($"  ...로딩이 끝나지 않는다. 멈췄다면 지금 디버그 → 모두 중단(Ctrl+Alt+Break)으로 확인해 보세요.", $"  ...ロードが終わりません。止まっているなら、今 デバッグ → すべて中断(Ctrl+Alt+Break) で確認してみてください。"));
            Console.WriteLine(Text.L($"  ({limit.TotalSeconds:0}초가 지나면 실패로 판정합니다)", $"  ({limit.TotalSeconds:0} 秒が過ぎたら失敗と判定します)"));
            frame.Wait(limit);
        }

        Expect(Text.L("로딩 완료", "ロード完了"), true, frame.IsCompletedSuccessfully);
        Expect(Text.L("로드한 에셋 수", "ロードしたアセット数"), Assets.Length, screen.AssetsLoaded);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 로딩 바가 멈추면 디버그 → 모두 중단(Ctrl+Alt+Break). 그다음 디버그 → 창 → 병렬 스택(Ctrl+Shift+D, S).
// "Main Thread (게임 루프)" 스레드는 지금 무엇을 하고 있나요? 맨 위의 내 코드 프레임을 더블클릭하세요.
//
// [JA] ロードバーが止まったら デバッグ → すべて中断(Ctrl+Alt+Break)。次に デバッグ → ウィンドウ → 並列スタック(Ctrl+Shift+D, S)。
// [JA] "Main Thread (게임 루프)" スレッドは今何をしていますか? 一番上の自分のコードのフレームをダブルクリックしてください。
#endregion

#region 힌트 2 / ヒント 2
// 병렬 스택 창 위쪽의 보기를 "스레드"에서 "작업"으로 바꿔 보세요. (또는 디버그 → 창 → 작업)
// LoadAssetsAsync 는 어떤 상태이고, 무엇을 기다리고 있나요?
// 조사식에 main.PendingCount 를 넣어 보세요. 메인 스레드 큐에 누가 줄을 서 있나요?
//
// [JA] 並列スタック ウィンドウ上部の表示を「スレッド」から「タスク」に切り替えてみてください。(または デバッグ → ウィンドウ → タスク)
// [JA] LoadAssetsAsync はどんな状態で、何を待っていますか?
// [JA] ウォッチに main.PendingCount を入れてみてください。メインスレッドのキューに誰が並んでいますか?
#endregion

#region 힌트 3 / ヒント 3
// await 는 끝난 뒤 "시작한 곳의 SynchronizationContext"로 돌아가 나머지를 실행하려고 합니다.
// 그런데 그 스레드는 .Result 에서 결과가 나오기를 기다리며 막혀 있습니다. 서로가 서로를 기다립니다.
// 콘솔 앱의 기본 스레드에서는 SynchronizationContext 가 없어서 같은 코드가 멀쩡히 돕니다. 그래서 "내 PC 에서는 되던" 코드가 됩니다.
//
// [JA] await は終わったあと「始めた場所の SynchronizationContext」に戻って残りを実行しようとします。
// [JA] ところがそのスレッドは .Result で結果が出るのを待ってブロックされています。互いが互いを待っています。
// [JA] コンソール アプリの既定のスレッドには SynchronizationContext がないので、同じコードが問題なく動きます。だから「自分の PC では動いていた」コードになります。
#endregion
