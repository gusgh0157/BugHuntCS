namespace BugHunt;

// 새 캐릭터의 가방을 만든다. 시작 무기를 첫 칸에 넣고, 튜토리얼에서 받은 물약을 빈 칸에 넣는다.
// 물약을 넣는 순간 ArrayTypeMismatchException 이 난다.
// [JA] 新しいキャラクターのバッグを作ります。初期武器を最初のスロットに入れ、チュートリアルでもらったポーションを空きスロットに入れます。
// [JA] ポーションを入れた瞬間に ArrayTypeMismatchException が発生します。
public static partial class Lab97
{
    abstract class Item(string name)
    {
        public string Name { get; } = name;
    }

    sealed class Weapon(string name, int damage) : Item(name)
    {
        public int Damage { get; } = damage;
    }

    sealed class Potion(string name, int heal) : Item(name)
    {
        public int Heal { get; } = heal;
    }

    // 가방: 시작 무기가 들어간 size 칸짜리 배열
    static Item[] CreateBag(Weapon starter, int size)
    {
        var bag = new Weapon[size];
        bag[0] = starter;
        return bag;
    }

    static void Put(Item[] bag, Item item)
    {
        int slot = Array.FindIndex(bag, s => s is null);
        if (slot < 0) throw new InvalidOperationException("가방이 꽉 찼다");
        bag[slot] = item;
        Console.WriteLine($"  {slot}번 칸에 {item.Name} 넣음");
    }

    public static void Run()
    {
        int size = Pick(6, 3, 10);
        string weaponName = Pick("나무 검", "녹슨 단검", "견습생 지팡이", "사냥용 활");
        int potions = Pick(2, 1, 3);

        Item[] bag = CreateBag(new Weapon(weaponName, 5), size);
        Console.WriteLine($"  가방 {size}칸, 첫 칸: {bag[0].Name}");

        for (int i = 0; i < potions; i++)
            Put(bag, new Potion("회복 물약", 30));

        int filled = bag.Count(slot => slot != null);
        Console.WriteLine($"  채워진 칸: {filled}");

        Expect("채워진 칸 수 (무기 1 + 물약)", 1 + potions, filled);
        Expect("1번 칸은 물약", true, bag[1] is Potion);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 예외 도우미가 bag[slot] = item 줄에서 멈춥니다. 조사식에 bag.GetType() 을 넣어 보세요. Item[] 인가요?
// bag 변수의 선언 형식은 Item[] 인데 실제 객체는 무엇인가요?
//
// [JA] 例外ヘルパーが bag[slot] = item の行で止まります。ウォッチに bag.GetType() を入れてみてください。Item[] ですか?
// [JA] bag 変数の宣言の型は Item[] なのに、実際のオブジェクトは何でしょう?
#endregion

#region 힌트 2 / ヒント 2
// CreateBag 안에서 new Weapon[size] 를 만들어 Item[] 로 돌려줍니다. 배열은 공변(covariant)이라 Weapon[] 을 Item[] 변수에 넣는 것이 컴파일됩니다.
// 하지만 실제 객체는 여전히 Weapon[] 이고, 런타임은 칸에 넣는 순간마다 "이 값이 Weapon 인가"를 검사합니다. Potion 을 넣으니 ArrayTypeMismatchException 입니다.
// 배열 쓰기가 안전하지 않은 이유이고, 참조 형식 배열의 대입마다 형식 검사 비용이 드는 이유이기도 합니다.
//
// [JA] CreateBag の中で new Weapon[size] を作り Item[] として返しています。配列は共変(covariant)なので、Weapon[] を Item[] の変数に入れるのはコンパイルが通ります。
// [JA] しかし実際のオブジェクトは依然として Weapon[] で、ランタイムは要素に入れるたびに「この値は Weapon か」を検査します。Potion を入れたので ArrayTypeMismatchException です。
// [JA] 配列への書き込みが安全でない理由であり、参照型配列の代入ごとに型チェックのコストがかかる理由でもあります。
#endregion

#region 힌트 3 / ヒント 3
// 담을 요소의 형식으로 배열을 만드세요: new Item[size]. 배열 공변성은 읽기 전용으로 쓸 때만 안전합니다.
// List<T> 는 공변이 아니라서(List<Weapon> 을 List<Item> 에 넣을 수 없음) 애초에 이 실수가 컴파일되지 않습니다. 읽기만 한다면 IEnumerable<out T> / IReadOnlyList<out T> 가 안전한 공변성입니다.
// try/catch 로 예외를 삼키고 건너뛰면 물약이 사라집니다. 숨은 테스트는 채워진 칸 수를 셉니다.
//
// [JA] 入れる要素の型で配列を作ってください: new Item[size]。配列の共変性は読み取り専用で使うときだけ安全です。
// [JA] List<T> は共変ではない(List<Weapon> を List<Item> に入れられない)ので、そもそもこの間違いはコンパイルが通りません。読むだけなら IEnumerable<out T> / IReadOnlyList<out T> が安全な共変性です。
// [JA] try/catch で例外を飲み込んで飛ばすとポーションが消えます。隠しテストは埋まったスロット数を数えます。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 가방 크기와 물약 수를 바꿔 검사한다. 예외를 삼키는 수정은 채워진 칸 수에서 걸린다.
// [JA] 隠しケースはバッグの大きさとポーション数を変えて検査します。例外を飲み込む修正は埋まったスロット数で引っかかります。
#endregion
