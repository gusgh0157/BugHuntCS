namespace BugHunt;

// 콤보를 이으면 보너스 피해가 붙는다. 보너스는 콤보가 없을 때 null(아직 값 없음)이다.
// 콤보가 끊긴 공격에서 총 피해가 0이 된다. 기본 피해까지 사라진다.
// [JA] コンボをつなぐとボーナスダメージが付きます。ボーナスはコンボがないとき null(まだ値なし)です。
// [JA] コンボが途切れた攻撃で合計ダメージが 0 になります。基本ダメージまで消えます。
public static partial class Lab115
{
    static int TotalDamage(int baseDamage, int? comboBonus)
    {
        // 콤보 보너스가 없으면 0을 더한 셈 치고 싶다.
        int total = baseDamage + comboBonus ?? 0;
        return total;
    }

    public static void Run()
    {
        (int Base, int? Combo)[] attacks = Pick(Attacks[0], Attacks[1..]);

        foreach ((int baseDamage, int? combo) in attacks)
        {
            int total = TotalDamage(baseDamage, combo);
            int expected = baseDamage + (combo ?? 0);
            Console.WriteLine($"  기본 {baseDamage}, 콤보 {(combo?.ToString() ?? "없음")} → 총 {total} (기대 {expected})");
            Expect($"총 피해 (기본 {baseDamage}, 콤보 {(combo?.ToString() ?? "없음")})", expected, total);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 콤보가 null 인 공격에서만 틀립니다. TotalDamage 의 total 줄에 중단점을 걸고, 조사식에 식의 일부를 따로 넣어 보세요:
//   baseDamage + comboBonus        (형식이 int? 입니다)
//   (baseDamage + comboBonus) ?? 0
//   baseDamage + (comboBonus ?? 0)
// 셋 중 어느 것이 지금 코드와 같은 값인가요?
//
// [JA] コンボが null の攻撃でだけ間違えます。TotalDamage の total 行にブレークポイントを置き、ウォッチに式の一部を別々に入れてみてください:
// [JA]   baseDamage + comboBonus        (型は int? です)
// [JA]   (baseDamage + comboBonus) ?? 0
// [JA]   baseDamage + (comboBonus ?? 0)
// [JA] この三つのうち、どれが今のコードと同じ値ですか?
#endregion

#region 힌트 2 / ヒント 2
// ?? 는 + 보다 우선순위가 낮습니다. 그래서 baseDamage + comboBonus ?? 0 은 (baseDamage + comboBonus) ?? 0 으로 묶입니다.
// comboBonus 가 null 이면 int? 인 (baseDamage + comboBonus) 전체가 null 이 되고(한쪽이 null 이면 결과가 null), ?? 0 이 그걸 0 으로 바꿉니다. baseDamage 가 통째로 사라집니다.
// 이것을 "lifted(들어 올린) 연산자"라고 합니다: int 연산자가 int? 에도 쓰이도록 확장되며, 피연산자 중 하나라도 null 이면 결과가 null 입니다.
//
// [JA] ?? は + より優先順位が低いです。そのため baseDamage + comboBonus ?? 0 は (baseDamage + comboBonus) ?? 0 とまとめられます。
// [JA] comboBonus が null なら int? である (baseDamage + comboBonus) 全体が null になり(一方が null なら結果は null)、?? 0 がそれを 0 に変えます。baseDamage がまるごと消えます。
// [JA] これを「リフトされた(lifted)演算子」と呼びます: int の演算子が int? にも使えるよう拡張され、オペランドのどれかが null なら結果は null です。
#endregion

#region 힌트 3 / ヒント 3
// 의도한 괄호를 직접 넣으세요: baseDamage + (comboBonus ?? 0). "null 이면 0으로 바꾼 뒤 더한다"는 뜻이 분명해집니다.
// 같은 함정의 다른 모습도 조심하세요:
//   int? x = null; if (x >= 0) ...   → false 입니다 (null 과의 비교는 >, <, >=, <= 모두 false. == 과 != 만 예외).
//   if (x != 5)                       → x 가 null 이면 true 입니다.
//   a ?? b + c                        → a ?? (b + c) 입니다.
// nullable 이 섞인 식에서는 "이 식의 정적 형식이 int 인가 int? 인가"를 조사식으로 꼭 확인하세요.
//
// [JA] 意図した括弧を自分で入れてください: baseDamage + (comboBonus ?? 0)。「null なら 0 に変えてから足す」という意味がはっきりします。
// [JA] 同じ罠の別の姿にも注意してください:
// [JA]   int? x = null; if (x >= 0) ...   → false です(null との比較は >, <, >=, <= すべて false。== と != だけ例外)。
// [JA]   if (x != 5)                       → x が null なら true です。
// [JA]   a ?? b + c                        → a ?? (b + c) です。
// [JA] nullable が混じった式では「この式の静的な型が int か int? か」をウォッチで必ず確認してください。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 다른 기본 피해와 콤보 조합(null 포함)으로 검사한다. null 일 때만 baseDamage 를 더하는 식의 수정은 괄호를 제대로 넣어야 통과한다.
// [JA] 隠しケースは別の基本ダメージとコンボの組み合わせ(null を含む)で検査します。null のときだけ baseDamage を足すような修正は、括弧を正しく入れないと通りません。
public static partial class Lab115
{
    static readonly (int, int?)[][] Attacks =
    [
        [(100, 50), (80, null), (120, 30)],
        [(60, null), (200, 100)],
        [(45, 15), (90, null), (300, null), (150, 75)],
        [(250, null)],
    ];
}
#endregion
