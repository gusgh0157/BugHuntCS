using BugHunt.Challenge.C3;

namespace BugHunt;

public static class ChallengeC3
{
    enum Screen { Friends, Guild, Mail }

    public static void Run()
    {
        string[] names = ["Aria", "Bram", "Cleo", "Dax", "Elin", "Finn", "Gwen", "Hugo", "Iris", "Jude", "Kael", "Lena",
                          "Milo", "Nora", "Owen", "Pia", "Quin", "Rhea", "Sven", "Tess", "Ugo", "Vera", "Wynn", "Yuna"];
        string[] guilds = [Text.L("새벽", "暁"), Text.L("늑대", "オオカミ"), Text.L("별빛", "星明かり")];
        var rng = new Random(Seed == 0 ? 1234 : Seed);
        var players = names.Select(n => (n, guilds[rng.Next(guilds.Length)], rng.Next(10, 100) * 10)).ToList();
        const string region = "KR";

        // 기본 케이스: 친구 랭킹을 보고 나서 주간 보상을 보낸다. 숨은 케이스: 여러 창을 무작위 순서로 연다
        // [JA] 基本ケース: フレンド ランキングを見てから週間報酬を送ります。隠しケース: いくつものウィンドウをランダムな順番で開きます
        var friends = new HashSet<string>(Seed == 0 ? ["Cleo", "Kael", "Nora", "Tess", "Yuna"] : names.Where(_ => rng.Next(4) == 0));
        string myGuild = guilds[rng.Next(guilds.Length)];
        List<Screen> actions = [Screen.Friends, Screen.Mail];
        if (Seed != 0)
        {
            // 친구 창 바로 다음에 길드 창. 길드에는 친구가 아닌 사람이 적어도 하나 있다
            // [JA] フレンド ウィンドウのすぐ次にギルド ウィンドウ。ギルドにはフレンドでない人が少なくとも一人います
            string outsider = players.First(p => p.Item2 == myGuild).n;
            friends.Remove(outsider);
            friends.Add(names.First(n => n != outsider));
            actions = [];
            for (int i = rng.Next(0, 3); i > 0; i--)
                actions.Add((Screen)rng.Next(3));
            actions.AddRange([Screen.Friends, Screen.Guild]);
            for (int i = rng.Next(1, 3); i > 0; i--)
                actions.Add((Screen)rng.Next(3));
        }

        var db = new RankingDatabase(players);
        var service = new RankingService(db);
        var friendScreen = new FriendRankingScreen(service);
        var guildScreen = new GuildRankingScreen(service);
        var mailer = new WeeklyRewardMailer(service);

        // 기대값은 DB 원본으로 따로 계산한다
        // [JA] 期待値は DB の元データで別に計算します
        var truth = players.OrderByDescending(p => p.Item3).ThenBy(p => p.Item1, StringComparer.Ordinal)
                           .Select((p, i) => (Name: p.Item1, Guild: p.Item2, Rank: i + 1)).ToList();

        foreach (Screen screen in actions)
        {
            switch (screen)
            {
                case Screen.Friends:
                {
                    List<string> shown = friendScreen.Show(region, friends);
                    Console.WriteLine(Text.L($"  [친구 랭킹] {string.Join(", ", shown)}", $"  [フレンド ランキング] {string.Join(", ", shown)}"));
                    var expected = truth.Where(t => friends.Contains(t.Name)).Select((t, i) => $"{i + 1}. {t.Name}");
                    Expect(Text.L("친구 랭킹 창", "フレンド ランキング ウィンドウ"), string.Join(", ", expected), string.Join(", ", shown));
                    break;
                }
                case Screen.Guild:
                {
                    List<string> shown = guildScreen.Show(region, myGuild);
                    Console.WriteLine(Text.L($"  [길드 랭킹: {myGuild}] {string.Join(", ", shown)}", $"  [ギルド ランキング: {myGuild}] {string.Join(", ", shown)}"));
                    var expected = truth.Where(t => t.Guild == myGuild).Select(t => $"{t.Rank}. {t.Name}");
                    Expect(Text.L("길드 랭킹 창", "ギルド ランキング ウィンドウ"), string.Join(", ", expected), string.Join(", ", shown));
                    break;
                }
                case Screen.Mail:
                {
                    List<string> outbox = mailer.Send(region);
                    foreach (string mail in outbox)
                        Console.WriteLine(Text.L($"  [우편] {mail}", $"  [郵便] {mail}"));
                    var expected = truth.Take(WeeklyRewardMailer.Winners).Select(t => $"{t.Name}: {(t.Rank == 1 ? Text.L("황금 왕관", "黄金の王冠") : t.Rank <= 3 ? Text.L("은 트로피", "銀のトロフィー") : Text.L("동 메달", "銅メダル"))}");
                    Expect(Text.L("주간 보상 우편", "週間報酬の郵便"), string.Join(" / ", expected), string.Join(" / ", outbox));
                    break;
                }
            }
        }
        Console.WriteLine(Text.L($"  DB 질의 {db.Queries}번", $"  DB クエリ {db.Queries} 回"));
    }
}
