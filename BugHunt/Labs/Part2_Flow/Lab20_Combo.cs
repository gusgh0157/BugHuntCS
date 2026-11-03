namespace BugHunt;

// 리듬 액션의 콤보. 박자에 맞춰 치면 콤보가 1씩 오르고, 놓치면 0으로 돌아간다.
// 판이 끝나면 최고 콤보로 등급을 매긴다.
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
        Console.WriteLine($"  플레이: {play}");
        Console.WriteLine($"  최고 콤보 {counter.Best}, 등급 {Grade(counter.Best, play.Length)}");
        Expect("최고 콤보", expected, counter.Best);
        Expect("등급", Grade(expected, play.Length), Grade(counter.Best, play.Length));
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// OnHit 첫 줄에 중단점을 걸고 조사식에 _combo 를 넣으세요. F10 을 한 번 누르기 전과 후의 값은?
// 몇 번을 쳐도 그대로라면, 그 한 줄 안에서 무슨 일이 일어나는지 순서대로 적어 보세요.
#endregion

#region 힌트 2
// _combo++ 라는 식의 "값"은 무엇인가요? 증가하기 전의 값인가요, 뒤의 값인가요?
// 조사식 창에 i 같은 지역 변수를 두고 i++ 를 직접 넣어 보면(조사식도 식을 실행합니다) 돌려주는 값과 i 가 어떻게 바뀌는지 볼 수 있습니다.
#endregion

#region 힌트 3
// C# 은 x = x++; 를 이렇게 정확히 정해 둡니다: (1) x 의 값 v 를 읽는다 (2) x 를 v+1 로 바꾼다 (3) 식의 값 v 를 x 에 대입한다.
// 그래서 결과는 언제나 "그대로"입니다. 컴파일러마다 다르지 않습니다. 증가만 하고 싶다면 대입하지 마세요.
#endregion
