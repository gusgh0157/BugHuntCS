namespace BugHunt;

// 길드마다 창고를 하나씩 둔다. 길드 객체를 키로 Dictionary 에서 창고를 찾는다.
public static class Lab26
{
    class Guild(int id, string tag)
    {
        public int Id { get; } = id;
        public string Tag { get; set; } = tag;

        // 같은 태그면 같은 길드로 본다.
        public override bool Equals(object? obj) => obj is Guild g && g.Tag == Tag;
        public override int GetHashCode() => Tag.GetHashCode();
    }

    public static void Run()
    {
        string[] tags = ["DAWN", "EMBR", "FRST", "GALE", "HAZE"];
        int count = Pick(3, 2, tags.Length);
        Guild[] guilds = [.. tags.Take(count).Select(t => new Guild(Array.IndexOf(tags, t) + 1, t))];

        var storage = new Dictionary<Guild, int>();
        for (int i = 0; i < guilds.Length; i++)
            storage[guilds[i]] = Pick(100 * (i + 1), 1, 999);

        // 길드장이 태그를 바꿨다.
        int renamed = Pick(0, 0, count - 1);
        int expectedGold = storage[guilds[renamed]];
        guilds[renamed].Tag = Pick("DWN2", "NEW1", "ZZZZ", "QWER");
        Console.WriteLine($"  길드 태그 변경: {tags[renamed]} → {guilds[renamed].Tag}");

        bool found = storage.TryGetValue(guilds[renamed], out int gold);
        Console.WriteLine($"  창고 찾기: {(found ? $"{gold}G" : "없음")} (창고 개수 {storage.Count})");

        Expect("태그를 바꾼 길드의 창고를 찾음", true, found);
        Expect("창고 골드", expectedGold, gold);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// TryGetValue 줄에서 멈추고 조사식에 storage 를 펼쳐 보세요. 이름이 바뀐 길드가 키 목록에 보이나요?
// 그런데 왜 못 찾을까요? 조사식에 storage.Keys.Contains(guilds[renamed]) 와 storage.ContainsKey(guilds[renamed]) 를 둘 다 넣어 보세요.
#endregion

#region 힌트 2
// GetHashCode 에 중단점을 거세요. 넣을 때와 찾을 때 각각 몇이 나오나요?
// Dictionary 는 넣을 때 계산한 해시로 "어느 칸"에 둘지 정해 두고, 찾을 때도 해시로 그 칸만 봅니다.
#endregion

#region 힌트 3
// 키로 쓰는 동안 해시에 들어가는 값이 바뀌면, 그 키는 엉뚱한 칸에 남아 영영 못 찾습니다. (랩 18은 Equals/GetHashCode가 서로 어긋난 경우였습니다)
// 키는 바뀌지 않는 값으로: 길드의 고유 Id(태그가 아니라)를 키로 쓰거나, 태그를 바꿀 때 딕셔너리에서 빼고 다시 넣으세요.
#endregion
