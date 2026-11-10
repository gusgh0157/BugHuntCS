namespace BugHunt;

// 전투 통계(타격 수, 치명타 수 등)를 이벤트마다 센다. 통계는 맞는데, 통계를 켜면 GC 횟수가 확 늘어난다.
// [JA] 戦闘統計(ヒット数、クリティカル数など)をイベントごとに数えます。統計は合っていますが、統計をオンにすると GC の回数がぐっと増えます。
public static class Lab91
{
    class Stats
    {
        // 숫자도 문자열도 담을 수 있게 object 로 만들었다. (지금은 숫자만 쓴다)
        // [JA] 数値も文字列も入れられるように object にしました。(今は数値だけ使います)
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
        stats.Add("hit", 0);   // 처음 한 번만 드는 비용 빼기 / 最初の一回だけかかるコストを除く

        (long bytes, long ms) = Measure(() =>
        {
            for (int i = 0; i < events; i++)
                stats.Add(keys[(i * step) % keys.Length], 1 + i % 3);
        });
        Console.WriteLine(Text.L($"  이벤트 {events:N0}개: 할당 {bytes:N0}바이트 (이벤트당 {(double)bytes / events:0.0}), {ms}ms", $"  イベント {events:N0} 個: 割り当て {bytes:N0} バイト (イベントあたり {(double)bytes / events:0.0})、{ms}ms"));

        var expected = new int[keys.Length];
        for (int i = 0; i < events; i++)
            expected[(i * step) % keys.Length] += 1 + i % 3;
        Expect(Text.L("통계", "統計"), string.Join(", ", expected), string.Join(", ", keys.Select(stats.Get)));
        ExpectAtMost(Text.L("이벤트 1000개당 할당 (바이트)", "イベント 1000 個あたりの割り当て (バイト)"), 64, bytes * 1000 / events);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 성능 프로파일러 → ".NET 개체 할당 추적". 가장 많이 만들어진 형식이 System.Int32 입니다. int 는 값 형식인데 왜 힙에 만들어질까요?
//
// [JA] パフォーマンス プロファイラー → 「.NET オブジェクトの割り当ての追跡」。一番多く作られた型が System.Int32 です。int は値型なのに、なぜヒープに作られるのでしょう?
#endregion

#region 힌트 2 / ヒント 2
// object 자리에 int 를 넣으면 박싱(랩 4, 12)이 일어나 힙에 상자가 하나 생깁니다. Add 가 한 번 불릴 때마다 상자 하나.
// 디스어셈블리(Ctrl+Alt+D)에서 Add 를 보면 박싱 도우미 호출이 보입니다.
//
// [JA] object の場所に int を入れるとボックス化(ラボ 4、12)が起き、ヒープに箱が一つできます。Add が一回呼ばれるたびに箱が一つ。
// [JA] 逆アセンブル(Ctrl+Alt+D)で Add を見ると、ボックス化ヘルパーの呼び出しが見えます。
#endregion

#region 힌트 3 / ヒント 3
// 담을 값의 형식을 알면 제네릭으로: Dictionary<string, int>. 숨은 박싱은 비제네릭 컬렉션(ArrayList, Hashtable),
// object 매개변수, IEquatable<T> 를 구현하지 않은 struct 의 Equals, 인터페이스로 넘긴 struct 에서도 생깁니다.
// 한 번 찾아서 바로 고치려면 CollectionsMarshal.GetValueRefOrAddDefault 를 쓸 수도 있습니다.
//
// [JA] 入れる値の型がわかっているならジェネリックで: Dictionary<string, int>。隠れたボックス化は非ジェネリック コレクション(ArrayList、Hashtable)、
// [JA] object 引数、IEquatable<T> を実装していない struct の Equals、インターフェイスとして渡した struct でも起きます。
// [JA] 一回の検索ですぐ書き換えたいなら CollectionsMarshal.GetValueRefOrAddDefault を使うこともできます。
#endregion
