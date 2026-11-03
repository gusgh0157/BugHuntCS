namespace BugHunt.Challenge.C3;

// 친구 랭킹 창: 친구들만 골라서 "친구 중 몇 위"로 보여 준다.
public sealed class FriendRankingScreen(RankingService service)
{
    public List<string> Show(string region, IReadOnlySet<string> friends)
    {
        List<RankEntry> ranks = RankFilters.Renumber(RankFilters.Where(service.Top(region), r => friends.Contains(r.Name)));
        return ranks.Select(r => $"{r.Rank}. {r.Name}").ToList();
    }
}

// 길드 랭킹 창: 우리 길드원이 전체에서 몇 위인지 보여 준다.
public sealed class GuildRankingScreen(RankingService service)
{
    public List<string> Show(string region, string guild) =>
        service.Top(region).Where(r => r.Guild == guild).Select(r => $"{r.Rank}. {r.Name}").ToList();
}

// 주간 보상 우편: 상위 N명에게 순위별 보상을 보낸다.
public sealed class WeeklyRewardMailer(RankingService service)
{
    public const int Winners = 10;

    public List<string> Send(string region)
    {
        var outbox = new List<string>();
        foreach (RankEntry entry in service.Top(region).Take(Winners))
            outbox.Add($"{entry.Name}: {RewardFor(entry.Rank)}");
        return outbox;
    }

    static string RewardFor(int rank) => rank switch
    {
        1 => "황금 왕관",
        <= 3 => "은 트로피",
        _ => "동 메달",
    };
}
