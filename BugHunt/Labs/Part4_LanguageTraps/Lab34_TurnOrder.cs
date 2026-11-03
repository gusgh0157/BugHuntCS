namespace BugHunt;

// 전투 턴 순서를 커서(struct)로 들고 다니며, 한 유닛씩 턴을 넘긴다. 그런데 모든 턴을 첫 번째 유닛만 가져간다.
// [JA] 戦闘のターン順をカーソル(struct)で持ち回り、1ユニットずつターンを進めます。ところがすべてのターンを最初のユニットだけが取っていきます。
public static partial class Lab34
{
    // 턴 커서: 다음 유닛으로 넘어간다. 가볍게 쓰려고 struct 로 만들었다.
    struct TurnCursor(string[] units)
    {
        int _index = -1;
        readonly string[] _units = units;

        public bool MoveNext()
        {
            _index++;
            return _index < _units.Length;
        }

        public readonly string Current => _units[_index];
    }

    // 한 턴 진행: 커서를 다음으로 옮기고 그 유닛을 돌려준다.
    static string TakeTurn(TurnCursor cursor)
    {
        cursor.MoveNext();
        return cursor.Current;
    }

    public static void Run()
    {
        string[] units = Pick(Teams[0], Teams[1..]);
        var cursor = new TurnCursor(units);

        var order = new List<string>();
        for (int i = 0; i < units.Length; i++)
            order.Add(TakeTurn(cursor));

        Console.WriteLine($"  유닛: {string.Join(", ", units)}");
        Console.WriteLine($"  실제 턴 순서: {string.Join(", ", order)}");

        Expect("턴 순서", string.Join(",", units), string.Join(",", order));
        Expect("서로 다른 유닛이 턴을 가짐", units.Length, order.Distinct().Count());
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// TakeTurn 안의 cursor.MoveNext() 에 중단점을 걸고 _index 를 보세요. 매번 -1 에서 0 으로 올라갑니다. 왜 이전 턴의 1, 2 가 남아 있지 않을까요?
// TakeTurn 에서 나온 뒤 Run 의 cursor 에 조사식을 걸어 _index 를 보세요. for 를 몇 번 돌아도 그대로입니다.
//
// [JA] TakeTurn の中の cursor.MoveNext() にブレークポイントを置いて _index を見てください。毎回 -1 から 0 に上がります。なぜ前のターンの 1、2 が残っていないのでしょう?
// [JA] TakeTurn から戻ったあと、Run の cursor にウォッチをかけて _index を見てください。for を何回回してもそのままです。
#endregion

#region 힌트 2 / ヒント 2
// TurnCursor 는 struct(값 형식)입니다. TakeTurn(TurnCursor cursor) 는 커서를 "값으로" 받습니다. 호출할 때마다 Run 의 cursor 가 복사되어 넘어갑니다.
// TakeTurn 은 그 복사본의 _index 를 0 으로 올리고 Current 를 돌려준 뒤 복사본을 버립니다. Run 의 cursor 는 전혀 바뀌지 않아 늘 -1 에서 시작합니다.
// 이것이 List<T>.Enumerator, Span 열거자 같은 struct 열거자를 "변수에 담아 메서드에 넘기면" 진행되지 않는 이유와 같은 뿌리입니다.
//
// [JA] TurnCursor は struct(値型)です。TakeTurn(TurnCursor cursor) はカーソルを「値で」受け取ります。呼ぶたびに Run の cursor がコピーされて渡ります。
// [JA] TakeTurn はそのコピーの _index を 0 に上げて Current を返し、コピーを捨てます。Run の cursor はまったく変わらず、いつも -1 から始まります。
// [JA] これは List<T>.Enumerator や Span の列挙子のような struct の列挙子を「変数に入れてメソッドに渡すと」進まないのと同じ根です。
#endregion

#region 힌트 3 / ヒント 3
// struct 상태를 메서드가 바꾸게 하려면 참조로 넘기세요: static string TakeTurn(ref TurnCursor cursor).
// 또는 커서를 class 로 만들면(참조 형식) 복사되지 않습니다. 열거라면 foreach 를 그냥 쓰세요(foreach 는 열거자를 바깥으로 복사하지 않습니다).
// 주의: List<T>.Enumerator 를 var e = list.GetEnumerator(); 로 받아 두 곳에서 쓰면 두 복사본이 따로 진행됩니다. 이럴 때도 ref 로 넘기거나 foreach 를 쓰세요.
//
// [JA] struct の状態をメソッドに変えさせたいなら参照で渡してください: static string TakeTurn(ref TurnCursor cursor)。
// [JA] または カーソルを class(参照型)にすればコピーされません。列挙なら foreach をそのまま使ってください(foreach は列挙子を外にコピーしません)。
// [JA] 注意: List<T>.Enumerator を var e = list.GetEnumerator(); で受けて二箇所で使うと、二つのコピーが別々に進みます。このときも ref で渡すか foreach を使ってください。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 다른 유닛 구성으로 검사한다. ref/class 로 고치면 통과하고, Run 에서 직접 순서를 만드는 땜질은 다른 구성에서 걸린다.
// [JA] 隠しケースは別のユニット構成で検査します。ref/class で直せば通り、Run で直接順番を作る応急修正は別の構成で引っかかります。
public static partial class Lab34
{
    static readonly string[][] Teams =
    [
        ["기사", "궁수", "마법사"],
        ["전사", "도적"],
        ["성기사", "흑마법사", "사냥꾼", "음유시인"],
        ["광전사", "주술사", "암살자"],
    ];
}
#endregion
