namespace BugHunt;

// 레이드 보스가 쓰러지면 파티원별 기여도(%)를 계산해 보상 순서를 정한다.
// [JA] レイドボスが倒れたら、パーティーメンバーごとの貢献度(%)を計算して報酬の順番を決めます。
public static class Lab14
{
    record Attacker(string Name, int Damage);

    static int ContributionPercent(int damage, int totalDamage) => damage * 100 / totalDamage;

    public static void Run()
    {
        // 월드 보스 HP 50,000,000
        Attacker[] party =
        [
            new("Kael", 30_000_000),
            new("Mira", 15_000_000),
            new("Doran", 5_000_000),
        ];
        int total = party.Sum(a => a.Damage);

        var percents = new List<int>();
        foreach (Attacker a in party)
        {
            int percent = ContributionPercent(a.Damage, total);
            Console.WriteLine(Text.L($"  {a.Name}: 피해 {a.Damage:N0}, 기여도 {percent}%", $"  {a.Name}: ダメージ {a.Damage:N0}、貢献度 {percent}%"));
            percents.Add(percent);
        }

        Expect(Text.L("기여도 (Kael, Mira, Doran)", "貢献度 (Kael, Mira, Doran)"), "60, 30, 10", string.Join(", ", percents));
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// ContributionPercent 에 조건부 중단점을 거세요. 조건: damage == 30000000
// 조사식에 damage * 100 을 넣어 보세요. 3,000,000,000 이 나오나요?
//
// [JA] ContributionPercent に条件付きブレークポイントを置いてください。条件: damage == 30000000
// [JA] ウォッチに damage * 100 を入れてみてください。3,000,000,000 になりますか?
#endregion

#region 힌트 2 / ヒント 2
// 조사식에 int.MaxValue 도 넣어 보세요. 그리고 checked(damage * 100) 을 넣어 보세요.
// C# 의 int 연산은 기본적으로 넘쳐도 예외 없이 조용히 한 바퀴 돕니다(unchecked).
//
// [JA] ウォッチに int.MaxValue も入れてみてください。そして checked(damage * 100) を入れてみてください。
// [JA] C# の int 演算は既定ではあふれても例外なしに黙って一周します(unchecked)。
#endregion

#region 힌트 3 / ヒント 3
// 프로젝트 속성 → 빌드 → 고급 → "산술 오버플로 검사"(CheckForOverflowUnderflow)를 켜고 다시 실행해 보세요.
// 이번에는 어디서 멈추나요? 고칠 때는 계산 중간값이 int 를 넘지 않게 해야 합니다.
//
// [JA] プロジェクトのプロパティ → ビルド → 詳細設定 → 「算術オーバーフローのチェック」(CheckForOverflowUnderflow)をオンにしてもう一度実行してみてください。
// [JA] 今度はどこで止まりますか? 直すときは、計算の途中の値が int を超えないようにする必要があります。
#endregion
