namespace BugHunt;

// 길드 명단을 불러온 뒤 새 길드원을 받는다. 길드원마다 겹치지 않는 번호를 주고,
// 귓속말은 번호로 찾아서 보낸다.
public static class Lab21
{
    record Member(int Id, string Name);

    class GuildRoster
    {
        readonly List<Member> _members;
        int _lastId;   // 지금까지 쓴 가장 큰 번호

        public GuildRoster(List<Member> saved)
        {
            _members = saved;
            _lastId = saved.Max(m => m.Id);
        }

        int NextId() => _lastId++;

        public int Recruit(string name)
        {
            var member = new Member(NextId(), name);
            _members.Add(member);
            return member.Id;
        }

        public string Whisper(int id, string text) => $"{_members.First(m => m.Id == id).Name}에게: {text}";

        public IEnumerable<int> Ids => _members.Select(m => m.Id);
    }

    public static void Run()
    {
        var saved = new List<Member> { new(1, "Kael"), new(2, "Mira"), new(5, "Doran") };
        string[] recruits = ["Lia", "Sera"];
        if (Seed != 0)
        {
            int count = Pick(3, 2, 6);
            saved = Enumerable.Range(0, count).Select(i => new Member(Pick(1, 1, 90) + i * 100, $"기존{i}")).Reverse().ToList();
            recruits = Enumerable.Range(0, Pick(2, 1, 5)).Select(i => $"신입{i}").ToArray();
        }

        var roster = new GuildRoster(saved);
        var replies = new List<string>();
        foreach (string name in recruits)
        {
            int id = roster.Recruit(name);
            string reply = roster.Whisper(id, "어서 오세요!");
            Console.WriteLine($"  {name} 가입, 번호 {id} → {reply}");
            replies.Add(reply);
        }

        Expect("환영 귓속말을 받은 사람", string.Join(", ", recruits.Select(n => $"{n}에게: 어서 오세요!")), string.Join(", ", replies));
        Expect("번호가 겹치지 않음", true, roster.Ids.Distinct().Count() == roster.Ids.Count());
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// 귓속말이 엉뚱한 사람에게 갔다면, 그 사람과 새 길드원의 번호를 비교하세요.
// Recruit 에 중단점을 걸고 조사식에 _lastId 와 NextId() 의 결과(member.Id)를 넣어 보세요.
#endregion

#region 힌트 2
// _lastId 의 주석은 "지금까지 쓴 가장 큰 번호"입니다. 그렇다면 다음 번호는 무엇이어야 하나요?
// NextId() 가 돌려주는 값은 _lastId 를 올리기 전의 값인가요, 뒤의 값인가요?
#endregion

#region 힌트 3
// 후위 ++ 는 "올리기 전의 값"을 돌려주고, 전위 ++ 는 "올린 뒤의 값"을 돌려줍니다.
// 같은 종류: if (hits++ == 3) 는 네 번째가 아니라 다섯 번째 타격에서야 참이 됩니다.
#endregion
