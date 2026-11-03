namespace BugHunt;

// 퀘스트 보상 골드. 이벤트 기간에는 배율이 붙는다. 퀘스트 창(Quest 형식)에는 배율이 적용된 골드가 보이는데,
// 정산 파이프라인(IReward 인터페이스)으로 받은 골드는 그보다 적다.
// [JA] クエスト報酬ゴールド。イベント期間中は倍率が付きます。クエスト画面(Quest 型)には倍率を適用したゴールドが表示されますが、
// [JA] 精算パイプライン(IReward インターフェイス)で受け取るゴールドはそれより少ないです。
public static partial class Lab92
{
    interface IReward
    {
        string Title { get; }
        int Gold { get; }
    }

    sealed class Quest(string title, int baseGold) : IReward
    {
        public string Title => title;

        // 이벤트 배율 (1.0 = 평상시)
        public double EventRate { get; set; } = 1.0;

        // 퀘스트 창에 보여 주는 보상: 배율 적용
        public int Gold => (int)Math.Round(baseGold * EventRate);

        // 정산 파이프라인용 보상
        int IReward.Gold => baseGold;
    }

    // 정산: 완료한 퀘스트의 보상을 모두 더해 지급한다.
    static int Payout(IEnumerable<IReward> completed) => completed.Sum(r => r.Gold);

    public static void Run()
    {
        double rate = Pick(2.0, Rates);
        int count = Pick(3, 2, 5);
        Quest[] quests = [.. Enumerable.Range(0, count).Select(i => new Quest(Titles[i], Pick(100 * (i + 1), 50, 900)) { EventRate = rate })];

        int shown = quests.Sum(q => q.Gold);
        foreach (Quest q in quests)
            Console.WriteLine($"  {q.Title}: 퀘스트 창 {q.Gold}G");
        Console.WriteLine($"  이벤트 배율 x{rate}: 퀘스트 창 합계 {shown}G");

        int paid = Payout(quests);
        Console.WriteLine($"  정산 지급: {paid}G");

        Expect("정산 지급 골드 = 퀘스트 창에 보인 합계", shown, paid);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// Payout 의 람다 r => r.Gold 에 중단점을 걸고(람다 본문에 커서를 두고 F9) 멈추면 조사식에 r.Gold 와 ((Quest)r).Gold 를 나란히 넣어 보세요.
// 같은 객체인데 값이 다릅니다. 어떻게 이런 일이 가능할까요?
//
// [JA] Payout のラムダ r => r.Gold にブレークポイントを置き(ラムダ本体にカーソルを置いて F9)、止まったらウォッチに r.Gold と ((Quest)r).Gold を並べて入れてください。
// [JA] 同じオブジェクトなのに値が違います。どうしてこんなことが起こるのでしょう?
#endregion

#region 힌트 2 / ヒント 2
// Quest 에는 Gold 가 두 개 있습니다. public int Gold 와 int IReward.Gold (명시적 인터페이스 구현).
// 명시적 구현은 변수의 형식이 그 인터페이스일 때만 불립니다. Quest 형식으로 부르면 public 쪽이, IReward 로 부르면 명시적 쪽이 불립니다.
// 두 구현에 각각 중단점을 걸고 퀘스트 창(shown)과 정산(Payout)이 각각 어느 쪽에서 멈추는지 확인하세요.
//
// [JA] Quest には Gold が二つあります。public int Gold と int IReward.Gold (明示的なインターフェイス実装)です。
// [JA] 明示的な実装は、変数の型がそのインターフェイスのときだけ呼ばれます。Quest 型で呼べば public 側が、IReward で呼べば明示的な側が呼ばれます。
// [JA] 二つの実装にそれぞれブレークポイントを置き、クエスト画面(shown)と精算(Payout)がそれぞれどちらで止まるか確認してください。
#endregion

#region 힌트 3 / ヒント 3
// 같은 이름의 public 구현과 명시적 구현이 서로 다른 값을 돌려주면, 호출 경로에 따라 결과가 달라지는 버그가 됩니다.
// 명시적 구현은 "이름 충돌을 피하거나 인터페이스로만 쓰이게 숨기고 싶을 때" 쓰는 것이지, 두 번째 로직을 두는 자리가 아닙니다.
// 고치는 법: 명시적 구현을 지우거나(public Gold 가 인터페이스를 구현하게 됨), 명시적 구현이 public Gold 를 그대로 돌려주게 하세요.
//
// [JA] 同じ名前の public 実装と明示的な実装が違う値を返すと、呼び出し経路によって結果が変わるバグになります。
// [JA] 明示的な実装は「名前の衝突を避けたい、またはインターフェイス経由でしか使えないよう隠したいとき」に使うもので、二つ目のロジックを置く場所ではありません。
// [JA] 直し方: 明示的な実装を消す(public Gold がインターフェイスを実装するようになる)か、明示的な実装が public Gold をそのまま返すようにしてください。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 다른 배율과 퀘스트 수로 검사한다. 배율 2.0 에 맞춰 Payout 결과를 두 배로 하거나 기대값을 박아 넣으면 여기서 걸린다.
// [JA] 隠しケースは別の倍率とクエスト数で検査します。倍率 2.0 に合わせて Payout の結果を2倍にしたり、期待値を埋め込んだりするとここで引っかかります。
public static partial class Lab92
{
    static readonly double[] Rates = [1.5, 2.0, 3.0, 1.25];
    static readonly string[] Titles = ["늑대 사냥", "약초 수집", "고블린 소탕", "잃어버린 반지", "유적 탐사"];
}
#endregion
