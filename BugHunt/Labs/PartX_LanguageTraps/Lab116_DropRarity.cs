namespace BugHunt;

// 서버가 보낸 아이템 등급(정수)으로 드롭 가중치를 정한다. 새 등급(신화)이 추가된 서버가 섞이면서
// 클라이언트가 모르는 등급의 아이템이 가장 흔한 등급(일반)으로 취급된다.
// [JA] サーバーが送ったアイテム等級(整数)でドロップ重みを決めます。新しい等級(神話)が追加されたサーバーが混ざり、
// [JA] クライアントが知らない等級のアイテムが一番ありふれた等級(ノーマル)として扱われます。
public static partial class Lab116
{
    enum Rarity { Common, Rare, Epic, Legendary }

    // 등급별 드롭 가중치. 모르는 등급은 "넣지 않음"(0)이어야 한다.
    static int Weight(Rarity r) => r switch
    {
        Rarity.Common => 100,
        Rarity.Rare => 30,
        Rarity.Epic => 5,
        Rarity.Legendary => 1,
        _ => 100,
    };

    public static void Run()
    {
        int[] raw = Pick(Feeds[0], Feeds[1..]);

        long totalWeight = 0;
        int known = 0;
        foreach (int value in raw)
        {
            var rarity = (Rarity)value;
            int w = Weight(rarity);
            totalWeight += w;
            if (Enum.IsDefined(rarity))
                known++;
            Console.WriteLine($"  등급 코드 {value} → {rarity} (가중치 {w})");
        }

        // 기대값: 클라이언트가 아는 등급만 가중치를 더한다.
        long expectedWeight = raw.Where(v => Enum.IsDefined((Rarity)v)).Sum(v => (long)Weight((Rarity)v));
        int expectedKnown = raw.Count(v => Enum.IsDefined((Rarity)v));

        Console.WriteLine($"  아는 등급 수: {known} / {raw.Length}, 가중치 합: {totalWeight}");
        Expect("아는 등급의 가중치만 합산", expectedWeight, totalWeight);
        Expect("모르는 등급은 드롭 표에 넣지 않음", expectedKnown, known);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 콘솔을 보세요. 등급 코드 9 같은 큰 값이 어떤 Rarity 로 찍히나요? (Rarity)9 에 중단점을 걸고 조사식에 넣어 보세요.
// 조사식에 Enum.IsDefined((Rarity)9) 와 Weight((Rarity)9) 를 넣어 비교하세요. 하나는 false 인데, 가중치는 왜 100 일까요?
//
// [JA] コンソールを見てください。等級コード 9 のような大きな値がどの Rarity として出ますか? (Rarity)9 にブレークポイントを置いてウォッチに入れてみてください。
// [JA] ウォッチに Enum.IsDefined((Rarity)9) と Weight((Rarity)9) を入れて比べてください。一方は false なのに、重みはなぜ 100 なのでしょう?
#endregion

#region 힌트 2 / ヒント 2
// enum 은 사실 정수입니다. (Rarity)9 같은 범위 밖 캐스트는 예외를 내지 않고 "이름 없는 Rarity 값"을 만듭니다.
// switch 의 _ => 100 (또는 default) 가 그 모든 미지의 값을 받아 일반 등급의 가중치를 줍니다. "모른다"와 "일반"을 구별하지 못합니다.
// 비슷한 함정: default(Rarity) 는 0 = Common 입니다. 등급을 지정하지 않은 아이템이 조용히 일반이 됩니다. [Flags] enum 이라면 x.HasFlag(None) 은 항상 true 입니다(0 은 모든 값의 부분집합).
//
// [JA] enum は実は整数です。(Rarity)9 のような範囲外のキャストは例外を出さず、「名前のない Rarity 値」を作ります。
// [JA] switch の _ => 100 (または default)がそのすべての未知の値を受け取り、ノーマル等級の重みを与えます。「知らない」と「ノーマル」を区別できません。
// [JA] 似た罠: default(Rarity) は 0 = Common です。等級を指定していないアイテムが黙ってノーマルになります。[Flags] enum なら x.HasFlag(None) は常に true です(0 はすべての値の部分集合)。
#endregion

#region 힌트 3 / ヒント 3
// 바깥에서 온 정수를 enum 으로 받을 때는 범위를 검사하세요:
//   if (!Enum.IsDefined((Rarity)value)) { /* 모르는 등급: 건너뛰거나 로그 */ continue; }
// switch 에서 _ => 는 "그 밖의 아는 값"이 아니라 "예상 못 한 값"입니다. 기본값을 주지 말고 0 을 돌려주거나 예외를 내세요: _ => throw new ArgumentOutOfRangeException(...).
// 모르는 등급을 일반으로 세는 수정(_ => 100 유지)은 숨은 케이스의 다른 범위 밖 코드에서 걸립니다. [Flags] 라면 None 체크는 HasFlag 대신 (flags & X) != 0 로 하세요.
//
// [JA] 外から来た整数を enum として受け取るときは範囲を検査してください:
// [JA]   if (!Enum.IsDefined((Rarity)value)) { /* 知らない等級: スキップするかログ */ continue; }
// [JA] switch の _ => は「その他の既知の値」ではなく「想定外の値」です。既定値を与えず、0 を返すか例外を出してください: _ => throw new ArgumentOutOfRangeException(...)。
// [JA] 知らない等級をノーマルとして数える修正(_ => 100 のまま)は、隠しケースの別の範囲外コードで引っかかります。[Flags] なら None のチェックは HasFlag ではなく (flags & X) != 0 で行ってください。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 다른 범위 밖 등급 코드(5, 7, 음수 등)로 검사한다. 보이는 코드 9만 특별 처리하면 걸린다.
// [JA] 隠しケースは別の範囲外の等級コード(5、7、負の数など)で検査します。見えているコード 9 だけ特別扱いすると引っかかります。
public static partial class Lab116
{
    static readonly int[][] Feeds =
    [
        [0, 1, 2, 3, 9],
        [1, 1, 5, 2],
        [3, 7, 0, 10, 2],
        [2, 2, 2, -1, 1],
    ];
}
#endregion
