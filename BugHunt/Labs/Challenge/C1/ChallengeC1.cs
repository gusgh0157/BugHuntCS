using BugHunt.Challenge.C1;

namespace BugHunt;

public static class ChallengeC1
{
    public static void Run()
    {
        string[] party = ["Kael", "Mira", "Doran", "Yuna"];
        string[] loot = [Text.L("전설 망토", "伝説のマント"), Text.L("용의 비늘", "竜のうろこ"), Text.L("고대 금화", "古代の金貨"), Text.L("회복 물약", "回復ポーション"), Text.L("마나 결정", "マナの結晶"), Text.L("흑요석 반지", "黒曜石の指輪")];

        // 드롭 목록: 기본 케이스는 화면에 보이는 그대로. 숨은 케이스는 무작위(같은 사람에게 같은 아이템이 두 번 떨어지기도 한다)
        // [JA] ドロップ一覧: 基本ケースは画面に見えるとおりです。隠しケースはランダムです(同じ人に同じアイテムが二度落ちることもあります)
        var drops = new List<(string Member, string Item)>
        {
            ("Kael", Text.L("전설 망토", "伝説のマント")), ("Mira", Text.L("용의 비늘", "竜のうろこ")), ("Doran", Text.L("고대 금화", "古代の金貨")),
            ("Yuna", Text.L("회복 물약", "回復ポーション")), ("Mira", Text.L("마나 결정", "マナの結晶")), ("Kael", Text.L("흑요석 반지", "黒曜石の指輪")),
        };
        if (Seed != 0)
        {
            var rng = new Random(Seed);
            drops.Clear();
            int n = rng.Next(6, 13);
            for (int i = 0; i < n; i++)
                drops.Add((party[rng.Next(party.Length)], loot[rng.Next(3)]));   // 종류를 줄여서 같은 보상이 겹치게 / 種類を減らして同じ報酬が重なるように
            drops.Add(drops[0]);   // 같은 사람이 같은 아이템을 정말로 두 번 받는 경우 / 同じ人が同じアイテムを本当に二度受け取るケース
        }

        // 회선 상태: 기본 케이스는 패킷이 사라지지 않는다. 숨은 케이스는 패킷이나 ack 가 가끔 사라진다
        // [JA] 回線の状態: 基本ケースはパケットが消えません。隠しケースはパケットや ack がときどき消えます
        var lossRng = new Random(Seed);
        bool lossy = Seed != 0;
        var link = new UnreliableLink(
            dropPacket: n => lossy && (n == 0 || lossRng.Next(5) == 0),
            dropAck: n => lossy && lossRng.Next(5) == 0);

        var server = new RewardServer(link);
        var inbox = new RewardInbox(link);
        var view = new InventoryView();
        view.Attach(inbox);

        server.Distribute(raidId: 7, drops);
        for (int tick = 0; tick < 40; tick++)
        {
            inbox.Tick();
            server.Tick();
        }

        Console.WriteLine(Text.L($"  보낸 패킷 {link.PacketsSent}개 (재전송 {server.Resends}개), 받은 패킷 {inbox.Received}개, 중복으로 버림 {inbox.Duplicates}개", $"  送信したパケット {link.PacketsSent} 個 (再送 {server.Resends} 個)、受信したパケット {inbox.Received} 個、重複として破棄 {inbox.Duplicates} 個"));
        foreach (string member in party)
            Console.WriteLine(Text.L($"  {member,-6} 가방: {string.Join(", ", view.LinesOf(member))}", $"  {member,-6} バッグ: {string.Join(", ", view.LinesOf(member))}"));

        foreach (string member in party)
        {
            string expected = string.Join(", ", drops.Where(d => d.Member == member).Select(d => d.Item).Order());
            string actual = string.Join(", ", view.LinesOf(member).Order());
            Expect(Text.L($"{member}의 보상", $"{member} の報酬"), expected, actual);
        }
        Expect(Text.L("ack 를 못 받은 패킷", "ack を受け取れなかったパケット"), 0, server.Unacked);
    }
}
