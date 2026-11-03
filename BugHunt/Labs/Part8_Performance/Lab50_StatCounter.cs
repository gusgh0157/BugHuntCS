namespace BugHunt;

// 전투 통계(타격 수, 치명타 수 등)를 이벤트마다 센다. 통계는 맞는데, 통계를 켜면 GC 횟수가 확 늘어난다.
public static class Lab50
{
    class Stats
    {
        // 숫자도 문자열도 담을 수 있게 object 로 만들었다. (지금은 숫자만 쓴다)
        readonly Dictionary<string, object> _values = [];

        public void Add(string key, int amount) => _values[key] = (int)_values.GetValueOrDefault(key, 0) + amount;

        public int Get(string key) => (int)_values.GetValueOrDefault(key, 0);
    }

    public static void Run()
    {
        string[] keys = ["hit", "crit", "miss", "block", "heal"];
        int events = Pick(100_000, 50_000, 200_000);
        int step = Pick(1, 1, 7);
        var stats = new Stats();
        stats.Add("hit", 0);   // 처음 한 번만 드는 비용 빼기

        (long bytes, long ms) = Measure(() =>
        {
            for (int i = 0; i < events; i++)
                stats.Add(keys[(i * step) % keys.Length], 1 + i % 3);
        });
        Console.WriteLine($"  이벤트 {events:N0}개: 할당 {bytes:N0}바이트 (이벤트당 {(double)bytes / events:0.0}), {ms}ms");

        var expected = new int[keys.Length];
        for (int i = 0; i < events; i++)
            expected[(i * step) % keys.Length] += 1 + i % 3;
        Expect("통계", string.Join(", ", expected), string.Join(", ", keys.Select(stats.Get)));
        ExpectAtMost("이벤트 1000개당 할당 (바이트)", 64, bytes * 1000 / events);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// 성능 프로파일러 → ".NET 개체 할당 추적". 가장 많이 만들어진 형식이 System.Int32 입니다. int 는 값 형식인데 왜 힙에 만들어질까요?
#endregion

#region 힌트 2
// object 자리에 int 를 넣으면 박싱(랩 4, 11)이 일어나 힙에 상자가 하나 생깁니다. Add 가 한 번 불릴 때마다 상자 하나.
// 디스어셈블리(Ctrl+Alt+D)에서 Add 를 보면 박싱 도우미 호출이 보입니다.
#endregion

#region 힌트 3
// 담을 값의 형식을 알면 제네릭으로: Dictionary<string, int>. 숨은 박싱은 비제네릭 컬렉션(ArrayList, Hashtable),
// object 매개변수, IEquatable<T> 를 구현하지 않은 struct 의 Equals, 인터페이스로 넘긴 struct 에서도 생깁니다.
// 한 번 찾아서 바로 고치려면 CollectionsMarshal.GetValueRefOrAddDefault 를 쓸 수도 있습니다.
#endregion
