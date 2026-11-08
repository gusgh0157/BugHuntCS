using BugHunt.Challenge.C1;

namespace BugHunt;

public static class ChallengeC1
{
    public static void Run()
    {
        string[] party = ["Kael", "Mira", "Doran", "Yuna"];
        string[] loot = [Text.T("전설 망토"), Text.T("용의 비늘"), Text.T("고대 금화"), Text.T("회복 물약"), Text.T("마나 결정"), Text.T("흑요석 반지")];

        // 드롭 목록: 기본 케이스는 화면에 보이는 그대로. 숨은 케이스는 무작위(같은 사람에게 같은 아이템이 두 번 떨어지기도 한다)
        var drops = new List<(string Member, string Item)>
        {
            ("Kael", Text.T("전설 망토")), ("Mira", Text.T("용의 비늘")), ("Doran", Text.T("고대 금화")),
            ("Yuna", Text.T("회복 물약")), ("Mira", Text.T("마나 결정")), ("Kael", Text.T("흑요석 반지")),
        };
        if (Seed != 0)
        {
            var rng = new Random(Seed);
            drops.Clear();
            int n = rng.Next(6, 13);
            for (int i = 0; i < n; i++)
                drops.Add((party[rng.Next(party.Length)], loot[rng.Next(3)]));   // 종류를 줄여서 같은 보상이 겹치게
            drops.Add(drops[0]);   // 같은 사람이 같은 아이템을 정말로 두 번 받는 경우
        }

        // 회선 상태: 기본 케이스는 패킷이 사라지지 않는다. 숨은 케이스는 패킷이나 ack 가 가끔 사라진다
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

        Console.WriteLine($"  보낸 패킷 {link.PacketsSent}개 (재전송 {server.Resends}개), 받은 패킷 {inbox.Received}개, 중복으로 버림 {inbox.Duplicates}개");
        foreach (string member in party)
            Console.WriteLine($"  {member,-6} 가방: {string.Join(", ", view.LinesOf(member))}");

        foreach (string member in party)
        {
            string expected = string.Join(", ", drops.Where(d => d.Member == member).Select(d => d.Item).Order());
            string actual = string.Join(", ", view.LinesOf(member).Order());
            Expect($"{member}의 보상", expected, actual);
        }
        Expect("ack 를 못 받은 패킷", 0, server.Unacked);
    }
}
