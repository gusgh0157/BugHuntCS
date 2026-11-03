namespace BugHunt;

// 주간 랭킹 1~3위에게 우편으로 보상 골드를 보낸다.
public static class Lab10
{
    record Entry(string Name, int Score);
    record Mail(string To, int Gold);

    class Mailbox
    {
        public List<Mail> Sent { get; } = [];

        public Mail Send(string to, int gold)
        {
            var mail = new Mail(to, gold);
            Sent.Add(mail);
            return mail;
        }
    }

    static readonly List<Entry> Ranking =
    [
        new("Mira", 8200),
        new("Kael", 9100),
        new("Bram", 4300),
        new("Doran", 7600),
        new("Ivy", 6900),
    ];

    public static void Run()
    {
        var mailbox = new Mailbox();

        IEnumerable<Mail> rewards = Ranking
            .OrderByDescending(e => e.Score)
            .Take(3)
            .Select(e => mailbox.Send(e.Name, 500));

        Console.WriteLine($"  보상 대상 {rewards.Count()}명");
        foreach (Mail mail in rewards)
            Console.WriteLine($"  {mail.To}: {mail.Gold}G 발송");

        Expect("보낸 우편 수", 3, mailbox.Sent.Count);
        Expect("보낸 골드 합계", 1500, mailbox.Sent.Sum(m => m.Gold));
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// mailbox.Send 를 부르는 람다 안에 중단점을 거세요. (그 줄에서 F9 를 누르면 람다만 고를 수 있습니다)
// 몇 번 멈추나요? 기대는 3번입니다. 멈출 때마다 호출 스택에서 누가 불렀는지 보세요.
#endregion

#region 힌트 2
// rewards = ... 줄을 F10 으로 넘겼을 때 Send 는 몇 번 불렸나요? (mailbox.Sent.Count 를 조사식에)
// LINQ 의 Select 는 "목록"이 아니라 "목록을 만드는 방법"을 돌려줍니다. 실제 계산은 열거할 때마다 일어납니다.
#endregion

#region 주의 (디버거도 열거합니다)
// 조사식이나 지역 창에서 rewards 를 펼쳐 "결과 보기"를 누르면, 디버거가 그 자리에서 열거를 실행합니다.
// 그러면 Send 가 또 불려서 우편이 더 늘어납니다. 부수 효과가 있는 LINQ 는 펼쳐 보기만 해도 상태가 바뀝니다.
#endregion
