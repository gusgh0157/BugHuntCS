namespace BugHunt;

// 몬스터를 잡을 때마다 경험치 바가 10% 씩 찬다. 바가 꽉 차면 레벨 업.
// [JA] モンスターを倒すたびに経験値バーが 10% ずつたまります。バーが満タンになったらレベルアップ。
public static class Lab13
{
    class Hero
    {
        public int Level { get; private set; } = 1;
        public float XpBar { get; private set; }

        public void GainXp(float fraction)
        {
            XpBar += fraction;
            if (XpBar == 1.0f)
            {
                Level++;
                XpBar = 0f;
                Console.WriteLine(Text.L($"  레벨 업! Lv.{Level}", $"  レベルアップ! Lv.{Level}"));
            }
        }
    }

    public static void Run()
    {
        var hero = new Hero();
        for (int kill = 1; kill <= 10; kill++)
        {
            hero.GainXp(0.1f);
            Console.WriteLine(Text.L($"  처치 {kill,2}: 경험치 {hero.XpBar:P0}", $"  撃破 {kill,2}: 経験値 {hero.XpBar:P0}"));
        }

        Expect(Text.L("슬라임 10마리 처치 후 레벨", "スライム 10 匹撃破後のレベル"), 2, hero.Level);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 콘솔에는 "100%"가 찍힙니다. 그런데 레벨 업은 안 했습니다. 화면에 보이는 숫자를 믿어도 될까요?
// GainXp 의 if 줄에 조건부 중단점을 거세요. 조건: XpBar > 0.95f
//
// [JA] コンソールには "100%" と出ます。なのにレベルアップしていません。画面に見える数字を信じてよいのでしょうか?
// [JA] GainXp の if の行に条件付きブレークポイントを置いてください。条件: XpBar > 0.95f
#endregion

#region 힌트 2 / ヒント 2
// 멈추면 조사식에 XpBar, XpBar == 1.0f, XpBar - 1.0f 를 넣어 보세요.
// 조사식에 (double)XpBar 를 넣으면 float 에 실제로 저장된 값을 더 자세히 볼 수 있습니다.
//
// [JA] 止まったら、ウォッチに XpBar、XpBar == 1.0f、XpBar - 1.0f を入れてみてください。
// [JA] ウォッチに (double)XpBar を入れると、float に実際に保存されている値をもっと詳しく見られます。
#endregion

#region 힌트 3 / ヒント 3
// 0.1 은 2진수로 정확히 나타낼 수 없습니다. 0.1f 를 열 번 더하면 오차가 쌓입니다.
// float 를 == 로 비교하지 마세요. 경험치처럼 "정확해야 하는" 값은 무엇으로 들고 있는 게 좋을까요?
//
// [JA] 0.1 は 2 進数で正確に表せません。0.1f を 10 回足すと誤差がたまります。
// [JA] float を == で比べないでください。経験値のように「正確でなければならない」値は、何で持つのがよいでしょう?
#endregion
