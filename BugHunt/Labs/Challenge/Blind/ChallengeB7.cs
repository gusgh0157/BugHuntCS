namespace BugHunt;

// 점검이 끝나면 접속했던 플레이어 모두에게 보상 우편을 보낸다. 여러 통을 한꺼번에 보내서 빨리 끝낸다.
public static class ChallengeB7
{
    sealed class MailServer(int latencyMs)
    {
        int _delivered;
        public int Delivered => Volatile.Read(ref _delivered);

        public async Task SendAsync(string player)
        {
            await Task.Delay(latencyMs);   // 우편 서버 왕복
            Interlocked.Increment(ref _delivered);
        }
    }

    static void SendCompensation(MailServer server, IEnumerable<string> players)
    {
        Parallel.ForEach(players, async player =>
        {
            await server.SendAsync(player);
        });
        Console.WriteLine("  보상 우편 발송 완료");
    }

    public static void Run()
    {
        int count = Pick(12, 8, 40);
        string[] players = [.. Enumerable.Range(1, count).Select(i => $"player{i:00}")];
        var server = new MailServer(Pick(100, 600, 1000));

        SendCompensation(server, players);
        Expect("'발송 완료' 시점에 도착한 우편", count, server.Delivered);
    }
}
