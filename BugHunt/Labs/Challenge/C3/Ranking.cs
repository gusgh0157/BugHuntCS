namespace BugHunt.Challenge.C3;

public sealed class RankEntry(string name, string guild, int score)
{
    public string Name { get; } = name;
    public string Guild { get; } = guild;
    public int Score { get; } = score;
    public int Rank { get; set; }

    public override string ToString() => $"{Rank}위 {Name} ({Score})";
}

// 랭킹 DB. 질의할 때마다 새 결과를 만든다. 느리다고 가정한다.
public sealed class RankingDatabase(IReadOnlyList<(string Name, string Guild, int Score)> players)
{
    public int Queries { get; private set; }

    public List<RankEntry> QuerySeason(string region)
    {
        Queries++;
        return players.Select(p => new RankEntry(p.Name, p.Guild, p.Score)).ToList();
    }
}

// 랭킹 서비스. DB 질의가 비싸서 지역별 결과를 정렬해 캐시해 둔다. 시즌이 바뀌면 Invalidate.
public sealed class RankingService(RankingDatabase db)
{
    readonly Dictionary<string, List<RankEntry>> _cache = [];

    public List<RankEntry> Top(string region)
    {
        if (!_cache.TryGetValue(region, out List<RankEntry>? ranks))
        {
            ranks = db.QuerySeason(region);
            ranks.Sort((a, b) => b.Score != a.Score ? b.Score.CompareTo(a.Score) : string.CompareOrdinal(a.Name, b.Name));
            for (int i = 0; i < ranks.Count; i++)
                ranks[i].Rank = i + 1;
            _cache[region] = ranks;
        }
        return ranks;
    }

    public void Invalidate(string region) => _cache.Remove(region);
}

// 여러 화면이 같이 쓰는 랭킹 거르기 도우미. 이어 쓰기 편하게 받은 목록을 그대로 돌려준다.
public static class RankFilters
{
    public static List<RankEntry> Where(List<RankEntry> ranks, Predicate<RankEntry> keep)
    {
        ranks.RemoveAll(r => !keep(r));
        return ranks;
    }

    // 걸러진 목록 안에서 다시 1위부터 매긴다 (친구 중 몇 위인지 보여 줄 때)
    public static List<RankEntry> Renumber(List<RankEntry> ranks)
    {
        for (int i = 0; i < ranks.Count; i++)
            ranks[i].Rank = i + 1;
        return ranks;
    }
}
