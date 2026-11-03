namespace BugHunt;

// 매 프레임 플레이어 주변의 살아 있는 적 수를 센다(위협도 표시). 결과는 맞는데, 전투가 길어지면 GC 때문에 프레임이 툭툭 끊긴다.
public static class Lab89
{
    record struct Vec(float X, float Y)
    {
        public readonly float DistanceSq(Vec o) => (X - o.X) * (X - o.X) + (Y - o.Y) * (Y - o.Y);
    }

    class Enemy(Vec pos, bool alive)
    {
        public Vec Pos = pos;
        public bool Alive = alive;
    }

    static int CountNearby(List<Enemy> enemies, Vec player, float radius) =>
        enemies.Count(e => e.Alive && e.Pos.DistanceSq(player) <= radius * radius);

    public static void Run()
    {
        var rng = new Random(Pick(1, 1, 1000));
        int count = Pick(300, 100, 600);
        var enemies = new List<Enemy>();
        for (int i = 0; i < count; i++)
            enemies.Add(new Enemy(new Vec(rng.Next(-100, 100), rng.Next(-100, 100)), rng.Next(4) != 0));
        float radius = Pick(30, 10, 60);
        int frames = 2_000;

        Vec PlayerAt(int frame) => new(frame % 50 - 25, frame % 30 - 15);
        CountNearby(enemies, PlayerAt(0), radius);   // 한 번 돌려서 JIT 컴파일 등 처음 한 번만 드는 비용을 뺀다

        long total = 0;
        (long bytes, long ms) = Measure(() =>
        {
            for (int frame = 0; frame < frames; frame++)
                total += CountNearby(enemies, PlayerAt(frame), radius);
        });
        Console.WriteLine($"  {frames}프레임, 위협도 합 {total}, 할당 {bytes:N0}바이트 (프레임당 {bytes / frames}), {ms}ms");

        long expected = 0;
        for (int frame = 0; frame < frames; frame++)
            foreach (Enemy e in enemies)
                if (e.Alive && e.Pos.DistanceSq(PlayerAt(frame)) <= radius * radius)
                    expected++;
        Expect("위협도 합", expected, total);
        ExpectAtMost("프레임당 할당 (바이트)", 8, bytes / frames);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// 디버그 → 성능 프로파일러(Alt+F2) → ".NET 개체 할당 추적"을 체크하고 시작. 끝나면 할당 탭에서 가장 많이 만들어진 형식을 보세요.
// 명령줄이라면: dotnet-counters monitor -n BugHunt --counters System.Runtime 으로 gen0 GC 횟수가 올라가는 속도를 보세요.
#endregion

#region 힌트 2
// 할당된 형식 이름에 <>c__DisplayClass, Func<Enemy, bool> 이 보이나요? 그 할당의 호출 스택(역방향)을 따라가 보세요.
// 람다가 바깥 변수(player, radius)를 쓰면, 부를 때마다 그 변수를 담을 객체와 대리자가 새로 만들어집니다.
#endregion

#region 힌트 3
// 1초에 수십 번 도는 코드에서는 LINQ 와 캡처하는 람다가 매번 쓰레기를 만듭니다. 결과는 같아도 GC 가 자주 돌고, 그때마다 프레임이 멈춥니다.
// 핫 루프는 평범한 for/foreach 로 쓰세요. (static 람다는 캡처를 못 하게 막아 주는 안전장치입니다)
#endregion
