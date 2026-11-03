namespace BugHunt;

// 레이드에 들어가는 순간의 참가자 명단을 남겨 두고, 레이드가 끝나면 그 명단대로 보상을 준다.
// 레이드 도중에 길드원이 들어오고 나가도, 보상은 "입장할 때 명단"이 기준이다.
public static class Lab25
{
    class Member(string name, bool online)
    {
        public string Name { get; } = name;
        public bool Online { get; set; } = online;
    }

    class Raid
    {
        IEnumerable<Member> _roster = [];

        public void Enter(List<Member> guild)
        {
            _roster = guild.Where(m => m.Online);
            Console.WriteLine($"  레이드 입장: {string.Join(", ", _roster.Select(m => m.Name))}");
        }

        public List<string> RewardRecipients() => [.. _roster.Select(m => m.Name)];
    }

    public static void Run()
    {
        string[] names = ["Kael", "Mira", "Doran", "Lia", "Sven", "Yuna", "Bram"];
        int size = Pick(5, 4, names.Length);
        var guild = new List<Member>();
        for (int i = 0; i < size; i++)
            guild.Add(new Member(names[i], Seed == 0 ? i != 3 : Pick(0, 0, 2) != 0));
        guild[0].Online = true;

        var raid = new Raid();
        raid.Enter(guild);
        string expected = string.Join(", ", guild.Where(m => m.Online).Select(m => m.Name));

        // 레이드 도중: 한 명이 접속을 끊고, 접속 안 했던 사람이 들어오고, 새 길드원이 가입한다.
        guild[0].Online = false;
        foreach (Member m in guild.Where(m => !m.Online).Skip(1).Take(1))
            m.Online = true;
        guild.Add(new Member("Newbie", true));
        Console.WriteLine($"  레이드 도중 접속 중: {string.Join(", ", guild.Where(m => m.Online).Select(m => m.Name))}");

        List<string> recipients = raid.RewardRecipients();
        Console.WriteLine($"  보상 받는 사람: {string.Join(", ", recipients)}");
        Expect("보상 받는 사람 (입장할 때 명단)", expected, string.Join(", ", recipients));
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// Enter 끝에서 조사식에 _roster 를 넣고 펼쳐 보세요. "결과 보기"를 눌러야 내용이 나온다면, 그것은 명단일까요 조건일까요?
// RewardRecipients 에서 다시 _roster 의 결과 보기를 눌러 비교해 보세요.
#endregion

#region 힌트 2
// m => m.Online 람다에 중단점을 거세요. Enter 에서 몇 번 걸리고, RewardRecipients 에서 또 몇 번 걸리나요?
// 랩 9는 "두 번 열거하면 부수 효과가 두 번 일어난다"였습니다. 여기서는 "나중에 열거하면 나중의 데이터를 본다"입니다.
#endregion

#region 힌트 3
// Where 가 돌려주는 것은 사진(스냅샷)이 아니라 창문(질의)입니다. 원본 List 가 바뀌면 다음에 열거할 때 바뀐 결과가 나옵니다.
// 그 순간의 명단이 필요하다면 그 순간에 ToList()/ToArray() 로 찍어 두세요. 다만 Member 객체 자체는 여전히 공유됩니다(얕은 복사, 랩 18).
#endregion
