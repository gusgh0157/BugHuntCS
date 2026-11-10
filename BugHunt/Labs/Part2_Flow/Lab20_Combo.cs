namespace BugHunt;

// 리듬 액션의 콤보. 박자에 맞춰 치면 콤보가 1씩 오르고, 놓치면 0으로 돌아간다.
// 판이 끝나면 최고 콤보로 등급을 매긴다.
// [JA] リズムアクションのコンボ。拍子に合わせて叩くとコンボが 1 ずつ上がり、外すと 0 に戻ります。
// [JA] プレイが終わったら最高コンボで評価を付けます。
public static class Lab20
{
    class ComboCounter
    {
        int _combo;
        public int Best { get; private set; }

        public void OnHit()
        {
            _combo = _combo++;
            if (_combo > Best)
                Best = _combo;
        }

        public void OnMiss() => _combo = 0;
    }

    static string Grade(int bestCombo, int notes) =>
        bestCombo == notes ? "S" : bestCombo * 2 >= notes ? "A" : bestCombo * 4 >= notes ? "B" : "C";

    public static void Run()
    {
        // H = 박자에 맞춤, M = 놓침
        // [JA] H = 拍子に合わせて叩いた、M = 外した
        string play = "HHHHMHHHHHHHMHH";
        if (Seed != 0)
            play = new string('H', Pick(8, 8, 14)) + "M" + new string(Enumerable.Range(0, Pick(15, 10, 30)).Select(_ => Pick('H', 'H', 'H', 'H', 'M')).ToArray());

        var counter = new ComboCounter();
        foreach (char note in play)
        {
            if (note == 'H') counter.OnHit();
            else counter.OnMiss();
        }

        int expected = play.Split('M').Max(run => run.Length);
        Console.WriteLine(Text.L($"  플레이: {play}", $"  プレイ: {play}"));
        Console.WriteLine(Text.L($"  최고 콤보 {counter.Best}, 등급 {Grade(counter.Best, play.Length)}", $"  最高コンボ {counter.Best}、評価 {Grade(counter.Best, play.Length)}"));
        Expect(Text.L("최고 콤보", "最高コンボ"), expected, counter.Best);
        Expect(Text.L("등급", "評価"), Grade(expected, play.Length), Grade(counter.Best, play.Length));
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// OnHit 첫 줄에 중단점을 걸고 조사식에 _combo 를 넣으세요. F10 을 한 번 누르기 전과 후의 값은?
// 몇 번을 쳐도 그대로라면, 그 한 줄 안에서 무슨 일이 일어나는지 순서대로 적어 보세요.
//
// [JA] OnHit の最初の行にブレークポイントを置き、ウォッチに _combo を入れてください。F10 を一回押す前と後の値は?
// [JA] 何回叩いても変わらないなら、その一行の中で何が起きているかを順番に書き出してみてください。
#endregion

#region 힌트 2 / ヒント 2
// _combo++ 라는 식의 "값"은 무엇인가요? 증가하기 전의 값인가요, 뒤의 값인가요?
// 조사식 창에 i 같은 지역 변수를 두고 i++ 를 직접 넣어 보면(조사식도 식을 실행합니다) 돌려주는 값과 i 가 어떻게 바뀌는지 볼 수 있습니다.
//
// [JA] _combo++ という式の「値」は何ですか? 増える前の値ですか、後の値ですか?
// [JA] ウォッチ ウィンドウに i のようなローカル変数を置いて i++ を直接入れてみると(ウォッチも式を実行します)、返す値と i がどう変わるかを見られます。
#endregion

#region 힌트 3 / ヒント 3
// C# 은 x = x++; 를 이렇게 정확히 정해 둡니다: (1) x 의 값 v 를 읽는다 (2) x 를 v+1 로 바꾼다 (3) 식의 값 v 를 x 에 대입한다.
// 그래서 결과는 언제나 "그대로"입니다. 컴파일러마다 다르지 않습니다. 증가만 하고 싶다면 대입하지 마세요.
//
// [JA] C# は x = x++; をこう厳密に決めています: (1) x の値 v を読む (2) x を v+1 に変える (3) 式の値 v を x に代入する。
// [JA] だから結果はいつも「そのまま」です。コンパイラーによって違うわけではありません。増やしたいだけなら代入しないでください。
#endregion
