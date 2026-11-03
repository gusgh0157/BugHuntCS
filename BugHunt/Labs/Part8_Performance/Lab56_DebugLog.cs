namespace BugHunt;

// 개발 중에 넣어 둔 디버그 로그. 출시판에서는 로그를 꺼 두었는데도 이동 처리 때 GC 가 자주 돈다.
public static class Lab56
{
    static class Log
    {
        public static bool DebugEnabled;   // 출시판에서는 false

        public static void Debug(string format, params object?[] args)
        {
            if (!DebugEnabled)
                return;
            Console.WriteLine(format, args);
        }
    }

    struct Unit(int id, float x, float y)
    {
        public int Id = id;
        public float X = x, Y = y;
    }

    static void MoveAll(Unit[] units, float dx, float dy, int frame)
    {
        for (int i = 0; i < units.Length; i++)
        {
            units[i].X += dx;
            units[i].Y += dy;
            Log.Debug("프레임 {0}: 유닛 {1} 이동 → ({2}, {3})", frame, units[i].Id, units[i].X, units[i].Y);
        }
    }

    public static void Run()
    {
        Log.DebugEnabled = false;
        int count = Pick(200, 100, 400);
        int frames = Pick(200, 100, 300);
        var units = new Unit[count];
        for (int i = 0; i < count; i++)
            units[i] = new Unit(i, i, -i);
        MoveAll(units, 0, 0, -1);   // 처음 한 번만 드는 비용 빼기

        (long bytes, long ms) = Measure(() =>
        {
            for (int frame = 0; frame < frames; frame++)
                MoveAll(units, 0.5f, 0.25f, frame);
        });
        long calls = (long)count * frames;
        Console.WriteLine($"  유닛 {count} × {frames}프레임 이동. 로그는 꺼져 있음. 할당 {bytes:N0}바이트 (이동 1회당 {bytes / calls}), {ms}ms");

        Expect("마지막 유닛 위치", $"{count - 1 + frames * 0.5f}, {-(count - 1) + frames * 0.25f}", $"{units[^1].X}, {units[^1].Y}");
        ExpectAtMost("이동 1회당 할당 (바이트)", 1, bytes / calls);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// 로그는 꺼져 있습니다. 그런데 .NET 개체 할당 추적에는 Object[], Int32, Single 이 잔뜩 보입니다. 누가 만들었을까요?
#endregion

#region 힌트 2
// Log.Debug 의 첫 줄(if (!DebugEnabled))에 중단점을 걸고, 멈췄을 때 지역 창에서 args 를 펼쳐 보세요. 이미 무엇이 들어 있나요?
// params 배열과 그 안의 상자(박싱된 int, float)는 Log.Debug 에 "들어가기 전에", 부르는 쪽에서 만들어집니다.
#endregion

#region 힌트 3
// 로그를 끄는 검사가 함수 안에 있으면, 인자를 준비하는 비용은 이미 치른 뒤입니다. 부르는 쪽에서 if (Log.DebugEnabled) 로 감싸거나,
// 제네릭 오버로드(Debug<T1, T2>(...))나 [Conditional("DEBUG")] 로 호출 자체를 없애세요.
// 큰 struct 를 값으로 여러 번 넘기는 복사도 비슷하게 "안 보이는 비용"입니다(in, ref readonly 매개변수).
#endregion
