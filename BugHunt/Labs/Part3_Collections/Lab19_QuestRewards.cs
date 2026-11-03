namespace BugHunt;

// 퀘스트 중간 정산. 지금까지 끝낸 단계마다 단계 보상과 빨리 깬 보너스를 준다.
public static class Lab19
{
    record Stage(string Name, int Gold, int ParSeconds);

    static int TimeBonus(Stage stage, int seconds) => seconds <= stage.ParSeconds ? stage.Gold / 2 : 0;

    static int Settle(Stage[] stages, List<int> clearSeconds)
    {
        int total = 0;
        for (int i = 0; i < stages.Length; i++)
            total += stages[i].Gold + TimeBonus(stages[i], clearSeconds[i]);
        return total;
    }

    public static void Run()
    {
        int count = Pick(4, 3, 9);
        Stage[] stages = [.. Enumerable.Range(1, count).Select(i => new Stage($"{i}단계", Pick(i * 100, 10, 500), 60))];

        // 플레이어는 아직 퀘스트를 다 깨지 않았다. 지금까지 깬 단계의 클리어 시간(초)
        int cleared = Pick(3, 1, count - 1);
        var clearSeconds = new List<int>();
        for (int i = 0; i < cleared; i++)
            clearSeconds.Add(Pick(45 + i * 10, 30, 90));

        Console.WriteLine($"  퀘스트 {stages.Length}단계 중 {clearSeconds.Count}단계 클리어: {string.Join(", ", clearSeconds.Select(s => $"{s}초"))}");
        int total = Settle(stages, clearSeconds);
        Console.WriteLine($"  중간 정산: {total}G");

        int expected = 0;
        for (int i = 0; i < cleared; i++)
            expected += stages[i].Gold + (clearSeconds[i] <= 60 ? stages[i].Gold / 2 : 0);
        Expect("중간 정산 골드", expected, total);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// 예외 도우미의 메시지를 끝까지 읽으세요. "Index was out of range. Must be non-negative and less than the size of the collection."
// 호출 스택 맨 위의 내 코드 프레임에서 지역 창의 i 를 보세요. 어느 컬렉션의 몇 번째 칸을 읽으려 했나요?
#endregion

#region 힌트 2
// 조사식에 stages.Length, clearSeconds.Count, i 를 넣어 보세요. 루프는 어느 쪽의 크기를 기준으로 돌고 있나요?
// 배열은 Length, List 는 Count. 이름이 다른 만큼 "무엇의 개수"인지도 다를 수 있습니다.
#endregion

#region 힌트 3
// 두 컬렉션의 크기가 다를 수 있다면, 루프는 "실제로 읽을 컬렉션"의 크기로 돌아야 합니다.
// 다 깬 퀘스트로만 시험하면 두 크기가 같아서 이 버그는 숨어 있습니다. 경계(0개, 일부, 전부)를 모두 시험하세요.
#endregion
