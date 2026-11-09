namespace BugHunt;

// 패치 파일을 한꺼번에 받되, 서버 부담을 줄이려고 동시에 2개까지만 받는다. 실패한 파일은 나중에 다시 받는다.
// [JA] パッチ ファイルをまとめてダウンロードしますが、サーバーの負担を減らすため同時に 2 個までしか受け取りません。失敗したファイルは後でもう一度受け取ります。
public static class Lab87
{
    class Downloader(int maxParallel, HashSet<string> broken)
    {
        readonly SemaphoreSlim _slots = new(maxParallel);
        public int Downloaded;

        public int FreeSlots => _slots.CurrentCount;

        // 서버에서 파일 하나를 받는다. 이 함수에는 버그가 없다.
        async Task<byte[]> FetchAsync(string file)
        {
            await Task.Yield();   // 서버 응답은 다른 스레드에서 온다
            if (broken.Contains(file))
                throw new HttpRequestException($"{file}: 503 Service Unavailable");
            return new byte[1024];
        }

        public async Task<bool> DownloadAsync(string file)
        {
            await _slots.WaitAsync();
            byte[] data = await FetchAsync(file);
            Interlocked.Increment(ref Downloaded);
            _slots.Release();
            return data.Length > 0;
        }
    }

    static async Task<List<string>> DownloadAll(Downloader downloader, string[] files)
    {
        var failed = new List<string>();
        await Task.WhenAll(files.Select(async file =>
        {
            try
            {
                await downloader.DownloadAsync(file);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(Text.L($"  실패, 나중에 다시: {ex.Message}", $"  失敗、後で再試行: {ex.Message}"));
                lock (failed)
                    failed.Add(file);
            }
        }));
        return failed;
    }

    public static void Run()
    {
        int count = Pick(8, 6, 20);
        string[] files = [.. Enumerable.Range(1, count).Select(i => $"patch_{i:00}.pak")];
        int maxParallel = Pick(2, 1, 3);
        HashSet<string> broken = Seed == 0
            ? ["patch_02.pak", "patch_03.pak"]
            : [.. files.Where(_ => Pick(0, 0, 2) == 0), .. files.Take(maxParallel)];

        var downloader = new Downloader(maxParallel, broken);
        List<string> failed = [];
        if (!FinishesWithin(Text.L("패치 다운로드", "パッチ ダウンロード"), () => failed = DownloadAll(downloader, files).GetAwaiter().GetResult()))
        {
            Console.WriteLine(Text.L($"  (멈춘 순간 받은 파일 {downloader.Downloaded}개, 빈 슬롯 {downloader.FreeSlots}개)", $"  (止まった時点で受信したファイル {downloader.Downloaded} 個、空きスロット {downloader.FreeSlots} 個)"));
            return;
        }

        Console.WriteLine(Text.L($"  받은 파일 {downloader.Downloaded}개, 실패 {failed.Count}개, 빈 슬롯 {downloader.FreeSlots}개", $"  受信したファイル {downloader.Downloaded} 個、失敗 {failed.Count} 個、空きスロット {downloader.FreeSlots} 個"));
        Expect(Text.L("받은 파일 수", "受信したファイル数"), count - broken.Count, downloader.Downloaded);
        Expect(Text.L("실패한 파일 수", "失敗したファイル数"), broken.Count, failed.Count);
        Expect(Text.L("다 끝난 뒤 빈 슬롯", "すべて終わった後の空きスロット"), maxParallel, downloader.FreeSlots);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 멈추면 모두 중단 → 디버그 → 창 → 작업(Ctrl+Shift+D, K). 끝나지 않은 작업들은 무엇을 기다리고 있나요?
// 병렬 스택 창을 "작업" 보기로 바꾸면 같은 곳에서 기다리는 작업들이 한 상자로 묶여 보입니다.
//
// [JA] 止まったら すべて中断 → デバッグ → ウィンドウ → タスク(Ctrl+Shift+D, K)。終わっていないタスクは何を待っていますか?
// [JA] 並列スタック ウィンドウを「タスク」ビューに切り替えると、同じ場所で待っているタスクが一つの箱にまとめて見えます。
#endregion

#region 힌트 2 / ヒント 2
// 조사식에 downloader.FreeSlots 를 넣어 보세요. 0 이라면 슬롯은 누가 들고 있나요? 아직 다운로드 중인 파일이 있나요?
// DownloadAsync 의 _slots.Release() 줄에 중단점을 걸고, 실패한 파일일 때 그 줄에 오는지 보세요.
//
// [JA] ウォッチに downloader.FreeSlots を入れてみてください。0 なら、スロットは誰が持っていますか? まだダウンロード中のファイルはありますか?
// [JA] DownloadAsync の _slots.Release() の行にブレークポイントを置き、失敗したファイルのときにその行に来るかを見てください。
#endregion

#region 힌트 3 / ヒント 3
// FetchAsync 가 예외를 던지면 그 아래 줄들은 실행되지 않습니다. 슬롯을 못 돌려받은 채로 실패가 maxParallel 개 쌓이면
// 그 뒤의 모든 다운로드가 WaitAsync 에서 영원히 기다립니다. 잠금과 같습니다: 얻었으면 finally 에서 돌려주세요.
//
// [JA] FetchAsync が例外を投げると、その下の行は実行されません。スロットを返してもらえないまま失敗が maxParallel 個たまると、
// [JA] その後のすべてのダウンロードが WaitAsync で永遠に待ちます。ロックと同じです: 取得したら finally で返してください。
#endregion
