namespace BugHunt;

// 캐시 상점에서 장바구니 합계와 세금을 계산한다. 합계가 1원씩 어긋나고, 반올림이 가끔 아래로 내려간다.
// [JA] キャッシュショップでカートの合計と税を計算します。合計が 1 ずつずれ、四捨五入が時々下に下がります。
public static partial class Lab37
{
    // 장바구니 합계. 가격은 "원" 단위 소수.
    // [JA] カートの合計。価格は小数。
    static double Total(decimal[] prices)
    {
        double sum = 0;
        foreach (decimal p in prices)
            sum += (double)p;
        return sum;
    }

    // 세금을 정수로 반올림한다.
    // [JA] 税を整数に丸める。
    static decimal RoundTax(decimal tax) => Math.Round(tax);

    public static void Run()
    {
        decimal[] prices = Pick(Carts[0], Carts[1..]);
        decimal taxRaw = Pick(2.5m, 0.5m, 4.5m, 6.5m);

        double total = Total(prices);
        double expectedTotal = (double)prices.Sum();
        Console.WriteLine(Text.L(FormattableString.Invariant($"  장바구니 {prices.Length}개 합계: {total} (기대 {expectedTotal})"), FormattableString.Invariant($"  カート {prices.Length}個の合計: {total} (期待 {expectedTotal})")));

        decimal tax = RoundTax(taxRaw);
        decimal expectedTax = Math.Round(taxRaw, MidpointRounding.AwayFromZero);
        Console.WriteLine(Text.L(FormattableString.Invariant($"  세금 {taxRaw} → {tax} (기대 {expectedTax})"), FormattableString.Invariant($"  税 {taxRaw} → {tax} (期待 {expectedTax})")));

        Expect(Text.L("장바구니 합계", "カートの合計"), expectedTotal, total);
        Expect(Text.L($"세금 반올림 ({taxRaw})", $"税の四捨五入 ({taxRaw})"), expectedTax, tax);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// Total 의 return 에 중단점을 걸고 조사식에 sum 을 넣어 보세요. 0.3 이 아니라 0.30000000000000004 같은 값입니다.
// RoundTax(2.5) 는 왜 3 이 아니라 2 일까요? 조사식에 Math.Round(2.5m), Math.Round(3.5m), Math.Round(2.5m, MidpointRounding.AwayFromZero) 를 넣어 비교하세요.
//
// [JA] Total の return にブレークポイントを置いてウォッチに sum を入れてみてください。0.3 ではなく 0.30000000000000004 のような値です。
// [JA] RoundTax(2.5) はなぜ 3 ではなく 2 なのでしょう? ウォッチに Math.Round(2.5m)、Math.Round(3.5m)、Math.Round(2.5m, MidpointRounding.AwayFromZero) を入れて比べてください。
#endregion

#region 힌트 2 / ヒント 2
// double 과 float 는 2진 부동소수점이라 0.1, 0.2 같은 10진 소수를 정확히 담지 못합니다. 더하면 작은 오차가 쌓여 0.1+0.2 != 0.3 이 됩니다.
// Math.Round 의 기본 규칙은 "은행가 반올림(MidpointRounding.ToEven)"입니다: 정확히 중간(2.5)이면 짝수 쪽(2)으로 갑니다. 3.5 는 4 로, 2.5 는 2 로 갑니다.
// 둘 다 "틀린" 게 아니라 "그렇게 정의된" 동작입니다. 화면 표시(0.3, 올림)에 대한 기대와 다를 뿐입니다.
//
// [JA] double と float は2進浮動小数点なので、0.1、0.2 のような10進の小数を正確に表せません。足すと小さな誤差がたまり 0.1+0.2 != 0.3 になります。
// [JA] Math.Round の既定の規則は「銀行家の丸め(MidpointRounding.ToEven)」です: ちょうど中間(2.5)なら偶数側(2)に行きます。3.5 は 4 に、2.5 は 2 に行きます。
// [JA] どちらも「間違い」ではなく「そう定義された」動作です。画面表示(0.3、切り上げ)への期待と違うだけです。
#endregion

#region 힌트 3 / ヒント 3
// 돈 계산에는 double/float 를 쓰지 말고 decimal 을 쓰세요. decimal 은 10진 소수를 정확히 담고 29자리 유효숫자를 가집니다. 합계는 처음부터 끝까지 decimal 로:
//   decimal sum = 0; foreach (var p in prices) sum += p;   (더하는 동안 double 로 바꾸지 않는다)
// 반올림 규칙은 상황에 맞게 명시하세요: 세금·요금은 보통 MidpointRounding.AwayFromZero(2.5 → 3). Math.Round(tax, MidpointRounding.AwayFromZero).
// 화면 표시만 올림하고 실제 값은 double 로 두는 수정은 숨은 케이스(다른 가격·세금)에서 걸립니다. 저장·전송·비교에 쓰는 값 자체를 decimal 로 다루세요.
//
// [JA] お金の計算には double/float を使わず decimal を使ってください。decimal は10進の小数を正確に表し、29桁の有効数字を持ちます。合計は最初から最後まで decimal で:
// [JA]   decimal sum = 0; foreach (var p in prices) sum += p;   (足している間は double に変換しない)
// [JA] 丸めの規則は状況に合わせて明示してください: 税・料金はふつう MidpointRounding.AwayFromZero(2.5 → 3)。Math.Round(tax, MidpointRounding.AwayFromZero)。
// [JA] 画面表示だけ切り上げて実際の値は double のままにする修正は、隠しケース(別の価格・税)で引っかかります。保存・送信・比較に使う値そのものを decimal で扱ってください。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 double 로 더하면 오차가 나는 다른 가격 조합과, 중간값(0.5, 4.5, 6.5) 세금으로 검사한다.
// [JA] 隠しケースは double で足すと誤差が出る別の価格の組み合わせと、中間値(0.5、4.5、6.5)の税で検査します。
public static partial class Lab37
{
    static readonly decimal[][] Carts =
    [
        [0.1m, 0.2m],
        [0.1m, 0.1m, 0.1m],
        [1.1m, 2.2m, 3.3m],
        [0.7m, 0.1m, 0.3m, 0.2m],
    ];
}
#endregion
