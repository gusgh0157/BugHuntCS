namespace BugHunt;

// 친구 요청을 받으면 친구 목록(HashSet)에 넣는다. 이름은 대소문자를 구분하지 않는다.
// [JA] フレンド申請を受けたらフレンドリスト(HashSet)に入れます。名前は大文字と小文字を区別しません。
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
            Console.WriteLine(Text.L($"  {from} 의 요청: {(added ? "친구 추가" : "이미 친구")}", $"  {from} からの申請: {(added ? "フレンド追加" : "すでにフレンド")}"));
        }

        Expect(Text.L("친구 수 (Kael, Mira, Doran)", "フレンド数 (Kael, Mira, Doran)"), 3, friends.Count);
        Expect(Text.L("\"mira\" 가 친구인가", "\"mira\" はフレンドか"), true, friends.Contains(new PlayerName("mira")));
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// Equals(PlayerName? other) 안에 중단점을 거세요. "kael" 을 넣을 때 멈추나요?
// 멈추지 않는다면, HashSet 은 Equals 를 부르기도 전에 "다르다"고 판단한 것입니다.
//
// [JA] Equals(PlayerName? other) の中にブレークポイントを置いてください。"kael" を入れるときに止まりますか?
// [JA] 止まらないなら、HashSet は Equals を呼ぶ前に「違う」と判断したということです。
#endregion

#region 힌트 2 / ヒント 2
// 조사식에 아래를 넣어 보세요.
//   new PlayerName("Kael").Equals(new PlayerName("kael"))
//   new PlayerName("Kael").GetHashCode()
//   new PlayerName("kael").GetHashCode()
//
// [JA] ウォッチに次を入れてみてください。
// [JA]   new PlayerName("Kael").Equals(new PlayerName("kael"))
// [JA]   new PlayerName("Kael").GetHashCode()
// [JA]   new PlayerName("kael").GetHashCode()
#endregion

#region 힌트 3 / ヒント 3
// HashSet 과 Dictionary 는 GetHashCode 로 "어느 칸"을 찾은 다음, 그 칸 안에서만 Equals 로 비교합니다.
// 규칙: Equals 가 true 인 두 객체는 반드시 GetHashCode 가 같아야 합니다.
//
// [JA] HashSet と Dictionary は GetHashCode で「どの枠」かを見つけてから、その枠の中だけで Equals で比べます。
// [JA] ルール: Equals が true になる二つのオブジェクトは、必ず GetHashCode が同じでなければなりません。
#endregion
