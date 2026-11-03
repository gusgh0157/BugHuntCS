using System.Collections.Concurrent;

namespace BugHunt;

// 지역별 전리품 테이블 파일을 병렬로 읽는다. 고장 난 파일은 건너뛰고 경고만 남긴다.
public static class Lab06
{
    static readonly Dictionary<string, string> Files = new()
    {
        ["forest.loot"] = "gold=120\nherb=3",
        ["cave.loot"] = "gold=80\nore=5",
        ["swamp.loot"] = "gold=??\nslime=2",
        ["castle.loot"] = "gold=300\ngem=1",
    };

    record LootTable(string Region, int Gold);

    static LootTable Parse(string fileName, string text)
    {
        int gold = 0;
        foreach (string line in text.Split('\n'))
        {
            string[] kv = line.Split('=');
            if (kv[0] == "gold")
                gold = int.Parse(kv[1]);
        }
        return new LootTable(Path.GetFileNameWithoutExtension(fileName), gold);
    }

    public static void Run()
    {
        var loaded = new ConcurrentBag<LootTable>();
        int skipped = 0;

        try
        {
            Parallel.ForEach(Files, file =>
            {
                loaded.Add(Parse(file.Key, file.Value));
            });
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"  고장 난 파일 건너뜀: {ex.Message}");
            skipped++;
        }

        foreach (LootTable table in loaded.OrderBy(t => t.Region))
            Console.WriteLine($"  {table.Region}: {table.Gold}G");

        Expect("읽은 테이블 수", 3, loaded.Count);
        Expect("건너뛴 파일 수", 1, skipped);
        Expect("골드 합계 (120 + 80 + 300)", 500, loaded.Sum(t => t.Gold));
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// 예외 도우미 맨 위의 예외 형식을 보세요. catch 가 기다리는 형식과 같은가요?
// 예외 도우미에서 "내부 예외 보기"를 누르거나, 조사식에 $exception.InnerExceptions 를 넣어 보세요.
#endregion

#region 힌트 2
// 진짜 예외가 "처음 던져진 순간"에 멈추게 할 수 있습니다.
// 예외 설정 창(Ctrl+Alt+E) → Common Language Runtime Exceptions → System.FormatException 체크.
// 다시 실행하면 어디서 멈추나요? 그때 스레드 창(Ctrl+Alt+H)을 보면 어느 스레드인가요?
#endregion

#region 힌트 3
// Parallel.ForEach 는 작업 스레드들에서 난 예외를 모아 AggregateException 하나로 다시 던집니다.
// 게다가 하나라도 실패하면 아직 시작하지 않은 항목은 처리하지 않을 수 있습니다.
// "파일 하나를 건너뛴다"는 규칙은 파일 하나 단위에서 지켜야 합니다.
#endregion
