namespace BugHunt;

// 스테이지를 깨면 새로 딴 업적을 서버로 보내고 결과 화면으로 넘어간다.
public static class Lab28
{
    class AchievementServer
    {
        int _received;
        public int Received => Volatile.Read(ref _received);

        public async Task SubmitAsync(string achievementId)
        {
            await Task.Delay(100);   // 네트워크 왕복
            Interlocked.Increment(ref _received);
        }
    }

    static void SubmitUnlocked(AchievementServer server, IEnumerable<string> unlocked)
    {
        foreach (string id in unlocked)
        {
            _ = server.SubmitAsync(id);
            Console.WriteLine($"  업적 전송: {id}");
        }
    }

    public static void Run()
    {
        var server = new AchievementServer();
        SubmitUnlocked(server, ["first_blood", "no_damage", "speed_run"]);

        Console.WriteLine("  결과 화면으로 이동");
        Expect("서버가 받은 업적 수", 3, server.Received);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// Expect 줄에 중단점을 걸고, 디버그 → 창 → 작업 을 여세요.
// SubmitAsync 작업이 몇 개 보이나요? 상태는 무엇인가요?
#endregion

#region 힌트 2
// Interlocked.Increment 줄에 중단점을 걸어 보세요. Expect 줄보다 먼저 멈추나요, 나중에 멈추나요?
// SubmitAsync 는 Task 를 돌려줍니다. 그 Task 는 누가 받아서 기다리나요?
#endregion

#region 힌트 3 (컴파일러 경고)
// _ = 를 지우고 빌드해 보세요. 어떤 경고(CS4014)가 나오나요?
// _ = 는 "이 Task 는 일부러 버린다"는 선언입니다. 경고를 없애려고 붙이면, 경고가 지키던 버그를 숨기게 됩니다.
#endregion
