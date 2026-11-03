namespace BugHunt;

// 가방(List<ItemStack>)과 즐겨찾기(HashSet<IItem>). 같은 종류(Id)의 아이템은 품질이 달라도 같은 아이템으로 본다.
// 새로 주운 아이템이 가방에서는 "이미 있음"으로 잡히는데, 즐겨찾기에서는 같은 종류인데도 별표가 안 붙는다.
// [JA] バッグ(List<ItemStack>)とお気に入り(HashSet<IItem>)。同じ種類(Id)のアイテムは品質が違っても同じアイテムと見なします。
// [JA] 新しく拾ったアイテムがバッグでは「すでにある」と判定されるのに、お気に入りでは同じ種類なのに星が付きません。
public static partial class Lab95
{
    interface IItem
    {
        int Id { get; }
        string Name { get; }
    }

    sealed class ItemStack(int id, string name, int quality) : IItem, IEquatable<ItemStack>
    {
        public int Id => id;
        public string Name => name;
        public int Quality => quality;

        // 같은 종류(Id)면 같은 아이템. 품질은 상관없다.
        public bool Equals(ItemStack? other) => other is not null && other.Id == Id;

        // 옛 코드: 품질까지 같아야 같은 아이템으로 봤다.
        public override bool Equals(object? obj) => obj is ItemStack s && s.Id == Id && s.Quality == Quality;

        public override int GetHashCode() => Id;

        public override string ToString() => $"{Name}(품질 {Quality})";
    }

    public static void Run()
    {
        int quality = Pick(1, 1, 5);
        int lootQuality = Pick(3, 1, 5);
        if (lootQuality == quality) lootQuality = lootQuality % 5 + 1;
        (int Id, string Name) kind = Pick(Kinds[0], Kinds[1..]);

        var bag = new List<ItemStack> { new(kind.Id, kind.Name, quality), new(99, "횃불", 1) };
        var favorites = new HashSet<IItem> { bag[0] };

        var loot = new ItemStack(kind.Id, kind.Name, lootQuality);
        Console.WriteLine($"  주운 아이템: {loot}  (가방에는 {bag[0]})");

        bool inBag = bag.Contains(loot);
        bool starred = favorites.Contains(loot);
        Console.WriteLine($"  가방에 같은 종류 있음: {inBag}, 즐겨찾기 별표: {starred}");

        Expect("가방에 같은 종류가 있다고 판정", true, inBag);
        Expect("즐겨찾기에도 같은 종류로 판정", true, starred);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// ItemStack 에는 Equals 가 두 개 있습니다. 둘 다에 중단점을 거세요.
// bag.Contains 는 어느 Equals 에서 멈추고, favorites.Contains 는 어느 Equals 에서 멈추나요?
//
// [JA] ItemStack には Equals が二つあります。両方にブレークポイントを置いてください。
// [JA] bag.Contains はどちらの Equals で止まり、favorites.Contains はどちらの Equals で止まりますか?
#endregion

#region 힌트 2 / ヒント 2
// 제네릭 컬렉션은 EqualityComparer<T>.Default 로 비교합니다. T 가 IEquatable<T> 를 구현하면 Equals(T) 를, 아니면 Equals(object) 를 부릅니다.
// bag 은 List<ItemStack> 이라 T = ItemStack → Equals(ItemStack). favorites 는 HashSet<IItem> 이라 T = IItem → IItem 은 IEquatable<IItem> 이 아니므로 Equals(object).
// 조사식에 favorites.Comparer 와 bag.Contains(loot), favorites.Contains(loot) 를 넣어 비교해 보세요.
//
// [JA] ジェネリックコレクションは EqualityComparer<T>.Default で比較します。T が IEquatable<T> を実装していれば Equals(T) を、そうでなければ Equals(object) を呼びます。
// [JA] bag は List<ItemStack> なので T = ItemStack → Equals(ItemStack)。favorites は HashSet<IItem> なので T = IItem → IItem は IEquatable<IItem> ではないため Equals(object)。
// [JA] ウォッチに favorites.Comparer と bag.Contains(loot)、favorites.Contains(loot) を入れて比べてみてください。
#endregion

#region 힌트 3 / ヒント 3
// Equals(object) 와 Equals(T) 는 반드시 같은 규칙이어야 합니다. 보통 Equals(object) 는 한 줄로 Equals(T) 에 위임합니다:
//   public override bool Equals(object? obj) => Equals(obj as ItemStack);
// GetHashCode 도 같은 규칙(Id)이어야 하고요(랩 17). 세 개 중 하나라도 어긋나면 컬렉션 종류에 따라 다른 답이 나옵니다.
// 같은 형식이 인터페이스·object·제네릭 세 경로로 비교될 수 있다는 점을 기억하세요. (== 연산자를 재정의했다면 그것도 네 번째 경로입니다)
//
// [JA] Equals(object) と Equals(T) は必ず同じ規則でなければなりません。普通 Equals(object) は一行で Equals(T) に委譲します:
// [JA]   public override bool Equals(object? obj) => Equals(obj as ItemStack);
// [JA] GetHashCode も同じ規則(Id)でなければなりません(ラボ17)。三つのうち一つでもずれると、コレクションの種類によって違う答えが出ます。
// [JA] 同じ型がインターフェイス・object・ジェネリックの三つの経路で比較されうることを覚えておいてください。(== 演算子を再定義していればそれも四つ目の経路です)
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 다른 아이템 종류와 품질 조합으로 검사한다. favorites 를 HashSet<ItemStack> 으로 바꾸는 수정은 통과하지만 다른 IItem 구현이 들어오면 다시 깨지므로 권하지 않는다.
// [JA] 隠しケースは別のアイテム種類と品質の組み合わせで検査します。favorites を HashSet<ItemStack> に変える修正は通りますが、別の IItem 実装が入るとまた壊れるので勧めません。
public static partial class Lab95
{
    static readonly (int Id, string Name)[] Kinds = [(7, "강철 검"), (12, "회복 물약"), (31, "마나 결정"), (45, "가죽 장갑")];
}
#endregion
