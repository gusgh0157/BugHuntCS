namespace BugHunt;

// 물약 단축키. 물약이 있으면 하나 마시고, 없으면 "물약이 없습니다"를 띄운다.
// 가방 한 칸에는 255개까지 들어가서 개수를 byte 로 둔다.
// [JA] ポーションのショートカット。ポーションがあれば一つ飲み、なければ「ポーションがありません」を表示します。
// [JA] バッグの 1 枠には 255 個まで入るので、個数を byte で持ちます。
public static class Lab22
{
    class Bag
    {
        public byte Potions;

        public bool TryDrink()
        {
            if (Potions-- == 0)
                return false;
            return true;
        }
    }

    public static void Run()
    {
        // 숨은 케이스는 칸이 거의 꽉 찬 가방
        var bag = new Bag { Potions = (byte)Pick(3, 251, 255) };
        int start = bag.Potions;
        int presses = start + Pick(4, 1, 6);   // 물약보다 많이 누른다

        int drunk = 0, refused = 0;
        for (int i = 0; i < presses; i++)
        {
            if (bag.TryDrink()) drunk++;
            else refused++;
        }

        Console.WriteLine($"  물약 {start}개로 시작, 단축키 {presses}번: 마심 {drunk}, 거절 {refused}, 남은 물약 {bag.Potions}");
        Expect("마신 물약", start, drunk);
        Expect("남은 물약", 0, (int)bag.Potions);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// TryDrink 에 조건부 중단점을 거세요. 조건: Potions == 0
// 거기서 F10 을 한 번 누르고 Potions 를 보세요. 마시지 않았는데 개수가 바뀌었나요?
//
// [JA] TryDrink に条件付きブレークポイントを置いてください。条件: Potions == 0
// [JA] そこで F10 を一回押して Potions を見てください。飲んでいないのに個数が変わりましたか?
#endregion

#region 힌트 2 / ヒント 2
// if (Potions-- == 0) 은 비교와 감소를 한 줄에서 합니다. 비교가 거짓이든 참이든 감소는 일어납니다.
// byte 0 에서 1을 빼면 무엇이 되나요? 조사식에 (byte)(0 - 1) 과 checked((byte)(0 - 1)) 을 넣어 비교해 보세요.
//
// [JA] if (Potions-- == 0) は比較と減少を一行で行います。比較が偽でも真でも減少は起きます。
// [JA] byte の 0 から 1 を引くと何になりますか? ウォッチに (byte)(0 - 1) と checked((byte)(0 - 1)) を入れて比べてみてください。
#endregion

#region 힌트 3 / ヒント 3
// 조건 검사와 값 바꾸기를 한 식에 섞지 마세요. 먼저 검사하고, 통과했을 때만 줄이세요.
// 프로젝트 속성 → 빌드 → 고급 → "산술 오버플로 검사"(CheckForOverflowUnderflow)를 켜면 이런 넘침이 조용히 지나가지 않고 OverflowException 으로 드러납니다.
//
// [JA] 条件のチェックと値の変更を一つの式に混ぜないでください。先にチェックし、通ったときだけ減らしてください。
// [JA] プロジェクトのプロパティ → ビルド → 詳細設定 → 「算術オーバーフローのチェック」(CheckForOverflowUnderflow)をオンにすると、こうしたあふれが黙って通り過ぎず OverflowException として表に出ます。
#endregion
