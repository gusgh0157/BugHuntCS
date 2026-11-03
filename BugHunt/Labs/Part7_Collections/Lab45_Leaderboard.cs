namespace BugHunt;

// 시즌 리더보드. 점수 높은 순으로 정렬하고, 점수가 같으면 먼저 달성한 사람이 위다.
// 기록은 달성한 순서대로 들어온다.
public static class Lab45
{
    record Entry(string Name, int Score);

    static void SortLeaderboard(List<Entry> board) => board.Sort((a, b) => b.Score.CompareTo(a.Score));

    public static void Run()
    {
        int count = Pick(24, 20, 60);
        var board = new List<Entry>();
        for (int i = 0; i < count; i++)
            board.Add(new Entry($"P{i + 1:00}", Pick(i % 4, 0, 4) * 1000));

        // 기대 순위: 점수 내림차순, 같으면 들어온 순서 (OrderByDescending 은 안정 정렬이다)
        string expected = string.Join(" ", board.OrderByDescending(e => e.Score).Select(e => e.Name));

        SortLeaderboard(board);
        string actual = string.Join(" ", board.Select(e => e.Name));
        Console.WriteLine($"  1~5위: {string.Join(", ", board.Take(5).Select(e => $"{e.Name}({e.Score})"))}");

        Expect("1위", expected.Split(' ')[0], board[0].Name);
        Expect("전체 순위", expected, actual);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// 점수는 맞게 내림차순입니다. 그런데 같은 점수끼리의 순서를 보세요. 들어온 순서(P01, P05, P09...)대로인가요?
// SortLeaderboard 호출 전후에 조사식으로 board 를 펼쳐 같은 점수인 사람들의 순서를 비교해 보세요.
#endregion

#region 힌트 2
// 기록이 16개 이하일 때도 틀리나요? 숨은 케이스처럼 개수를 바꿔 보세요. (List.Sort 는 작은 구간에서는 삽입 정렬을 씁니다)
// 비교자는 점수가 같으면 무엇을 돌려주나요? 0 이면 정렬은 "아무 순서나 괜찮다"고 받아들입니다.
#endregion

#region 힌트 3
// List.Sort / Array.Sort 는 불안정 정렬(introsort)이라 같은 값끼리의 원래 순서를 지키지 않습니다. LINQ 의 OrderBy 는 안정 정렬입니다.
// 같을 때의 규칙이 있다면 비교자에 넣으세요(달성 시각, 들어온 순서). 비교자는 일관되어야 합니다:
// a<b 이면 b>a, a==a 는 0. (a, b) => a.Score > b.Score ? -1 : 1 처럼 0 을 안 돌려주거나 a.Score - b.Score 처럼 넘칠 수 있는 비교자는
// 결과가 흔들리거나 "IComparer.Compare() method returns inconsistent results" 예외를 냅니다.
#endregion
