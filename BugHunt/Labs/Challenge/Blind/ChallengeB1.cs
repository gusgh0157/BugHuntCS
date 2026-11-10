namespace BugHunt;

// 로딩 스레드가 지역 데이터를 읽는 동안 메인 스레드는 화면을 준비하고, 다 되면 결과를 받아 간다.
// [JA] ローディング スレッドが地域データを読む間、メイン スレッドは画面を準備し、終わったら結果を受け取ります。
public static class ChallengeB1
{
    sealed class RegionLoader
    {
        readonly object _gate = new();
        int _tiles;

        public void Start(string region, int tileCount)
        {
            var worker = new Thread(() =>
            {
                int tiles = 0;
                for (int i = 0; i < tileCount; i++)
                    tiles++;
                lock (_gate)
                {
                    _tiles = tiles;
                    Monitor.Pulse(_gate);
                }
            }) { IsBackground = true, Name = Text.L($"로더 ({region})", $"ローダー ({region})"), Priority = ThreadPriority.Highest };
            worker.Start();
        }

        public int WaitForTiles()
        {
            lock (_gate)
            {
                Monitor.Wait(_gate);
                return _tiles;
            }
        }
    }

    static void PrepareScreen() => Thread.Sleep(100);   // 화면 준비 (UI 배치, 셰이더 예열) / 画面の準備 (UI 配置、シェーダーのウォームアップ)

    public static void Run()
    {
        string[] all = [Text.L("초원", "草原"), Text.L("사막", "砂漠"), Text.L("설원", "雪原"), Text.L("화산", "火山")];
        string[] regions = all[..Pick(1, 3, 4)];
        int total = 0;
        bool finished = FinishesWithin(Text.L("지역 불러오기", "地域の読み込み"), () =>
        {
            foreach (string region in regions)
            {
                var loader = new RegionLoader();
                loader.Start(region, 1000 + region.Length);
                PrepareScreen();
                total += loader.WaitForTiles();
                Console.WriteLine(Text.L($"  {region} 불러옴", $"  {region} 読み込み完了"));
            }
        });
        if (finished)
            Expect(Text.L("불러온 타일 수", "読み込んだタイル数"), regions.Sum(r => 1000 + r.Length), total);
    }
}
