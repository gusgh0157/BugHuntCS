namespace BugHunt;

// 레이드 전리품 목록에서 중복을 빼고, 처음 나온 순서대로 도감에 등록한다. 결과는 맞는데 큰 레이드에서는 몇 초씩 멈춘다.
public static class Lab92
{
    // 아이템 Id. 비교 횟수를 세어 둔다(진단용). 이 형식에는 버그가 없다.
    readonly struct ItemId(int value) : IEquatable<ItemId>
    {
        public static long Comparisons;
        public int Value { get; } = value;
        public bool Equals(ItemId other) { Comparisons++; return Value == other.Value; }
        public override bool Equals(object? obj) => obj is ItemId other && Equals(other);
        public override int GetHashCode() => Value;
    }

    static List<ItemId> UniqueInOrder(List<ItemId> drops)
    {
        var unique = new List<ItemId>();
        foreach (ItemId id in drops)
            if (!unique.Contains(id))
                unique.Add(id);
        return unique;
    }

    public static void Run()
    {
        var rng = new Random(Pick(7, 1, 1000));
        int count = Pick(8_000, 5_000, 12_000);
        int kinds = Pick(3_000, 1_500, 4_000);
        var drops = new List<ItemId>();
        for (int i = 0; i < count; i++)
            drops.Add(new ItemId(rng.Next(kinds) * 13 + 1000));

        ItemId.Comparisons = 0;
        List<ItemId> unique = [];
        (_, long ms) = Measure(() => unique = UniqueInOrder(drops));
        Console.WriteLine($"  전리품 {count:N0}개 → 도감 {unique.Count:N0}종. 비교 {ItemId.Comparisons:N0}번, {ms}ms");

        var seen = new HashSet<int>();
        int[] expected = [.. drops.Where(d => seen.Add(d.Value)).Select(d => d.Value)];
        Expect("도감 종류 수", expected.Length, unique.Count);
        Expect("처음 나온 순서대로 등록", true, expected.SequenceEqual(unique.Select(u => u.Value)));
        ExpectAtMost("전리품 하나당 비교 횟수", 3, ItemId.Comparisons / count);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// 성능 프로파일러 → "CPU 사용량". 핫 경로(Hot Path)를 펼쳐 보세요. 시간의 대부분은 어느 함수에 있나요?
// 전리품 수를 두 배로 늘리면(숨은 케이스처럼) 시간과 비교 횟수는 몇 배가 되나요?
#endregion

#region 힌트 2
// List.Contains 는 처음부터 끝까지 하나씩 비교합니다(선형 탐색). 그것을 전리품마다 하면 비교 횟수는 대략 전리품 수 × 도감 크기.
// ItemId.Equals 에 조건부 중단점(히트 수 >= 1000000)을 걸어 보세요. 언제 멈추나요?
#endregion

#region 힌트 3
// "이미 있나?"를 자주 물어보는 컬렉션은 해시 기반으로: HashSet<ItemId>. 순서는 List 에 따로 남기면 됩니다.
// 같은 함정: 루프 안의 List.IndexOf, List.Remove(item), Insert(0, ...), RemoveAt(0) (큐가 필요하면 Queue<T>).
#endregion
