namespace BugHunt;

// 로딩 스레드가 지역 데이터를 읽는 동안 메인 스레드는 화면을 준비하고, 다 되면 결과를 받아 간다.
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
            }) { IsBackground = true, Name = $"로더 ({region})" };
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

    static void PrepareScreen() => Thread.Sleep(300);   // 화면 준비 (UI 배치, 셰이더 예열)

    public static void Run()
    {
        string[] all = ["초원", "사막", "설원", "화산"];
        string[] regions = all[..Pick(1, 3, 4)];
        int total = 0;
        bool finished = FinishesWithin("지역 불러오기", () =>
        {
            foreach (string region in regions)
            {
                var loader = new RegionLoader();
                loader.Start(region, 1000 + region.Length);
                PrepareScreen();
                total += loader.WaitForTiles();
                Console.WriteLine($"  {region} 불러옴");
            }
        });
        if (finished)
            Expect("불러온 타일 수", regions.Sum(r => 1000 + r.Length), total);
    }
}
