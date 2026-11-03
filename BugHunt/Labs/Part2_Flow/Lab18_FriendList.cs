namespace BugHunt;

// 친구 요청을 받으면 친구 목록(HashSet)에 넣는다. 이름은 대소문자를 구분하지 않는다.
public static class Lab18
{
    sealed class PlayerName(string value) : IEquatable<PlayerName>
    {
        public string Value { get; } = value;

        public bool Equals(PlayerName? other) =>
            other is not null && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object? obj) => Equals(obj as PlayerName);

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => Value;
    }

    public static void Run()
    {
        var friends = new HashSet<PlayerName>();

        string[] requests = ["Kael", "Mira", "kael", "MIRA", "Doran"];
        foreach (string from in requests)
        {
            bool added = friends.Add(new PlayerName(from));
            Console.WriteLine($"  {from} 의 요청: {(added ? "친구 추가" : "이미 친구")}");
        }

        Expect("친구 수 (Kael, Mira, Doran)", 3, friends.Count);
        Expect("\"mira\" 가 친구인가", true, friends.Contains(new PlayerName("mira")));
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// Equals(PlayerName? other) 안에 중단점을 거세요. "kael" 을 넣을 때 멈추나요?
// 멈추지 않는다면, HashSet 은 Equals 를 부르기도 전에 "다르다"고 판단한 것입니다.
#endregion

#region 힌트 2
// 조사식에 아래를 넣어 보세요.
//   new PlayerName("Kael").Equals(new PlayerName("kael"))
//   new PlayerName("Kael").GetHashCode()
//   new PlayerName("kael").GetHashCode()
#endregion

#region 힌트 3
// HashSet 과 Dictionary 는 GetHashCode 로 "어느 칸"을 찾은 다음, 그 칸 안에서만 Equals 로 비교합니다.
// 규칙: Equals 가 true 인 두 객체는 반드시 GetHashCode 가 같아야 합니다.
#endregion
