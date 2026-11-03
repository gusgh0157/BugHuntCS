namespace BugHunt;

// 친구 목록에서 플레이어를 골라 프로필을 보여 준다.
public static class Lab01
{
    class Player(string name, int level, int hp)
    {
        public string Name { get; } = name;
        public int Level { get; } = level;
        public int Hp { get; } = hp;
    }

    static readonly List<Player> Players =
    [
        new("Kael", 12, 100),
        new("Mira", 5, 80),
        new("Doran", 15, 120),
    ];

    static int _shown;

    static Player? FindPlayer(string name)
    {
        foreach (Player p in Players)
        {
            if (p.Name == name)
                return p;
        }
        return null;
    }

    static void PrintPlayer(Player player)
    {
        Console.WriteLine($"  Lv.{player.Level} {player.Name} (HP {player.Hp})");
        _shown++;
    }

    static void ShowProfile(string name)
    {
        Player player = FindPlayer(name)!;
        PrintPlayer(player);
    }

    static void OpenFriendList()
    {
        ShowProfile("Kael");
        ShowProfile("Mira");
        ShowProfile("Dorna");
    }

    public static void Run()
    {
        OpenFriendList();
        Expect("보여 준 프로필 수", 3, _shown);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (README의 "따라 해 보기"를 먼저 하세요)
// 예외 도우미 창에 "player was null"(player이(가) null)이라는 설명이 붙어 있습니다.
// 호출 스택 창에서 ShowProfile 프레임을 더블클릭하고, 지역 창에서 name 값을 보세요.
#endregion

#region 힌트 2
// 그 name을 Players에 있는 이름들과 한 글자씩 비교해 보세요.
// 그리고 OpenFriendList 프레임에서 초록 화살표가 가리키는 줄을 보세요.
#endregion

#region 힌트 3 (컴파일러는 미리 알려 주려고 했습니다)
// ShowProfile에서 FindPlayer(name) 뒤의 ! 를 지우고 빌드해 보세요. 무슨 경고가 나오나요?
// ! 는 "여기는 절대 null이 아니야"라고 컴파일러의 입을 막는 연산자일 뿐, 검사가 아닙니다.
#endregion
