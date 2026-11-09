namespace BugHunt;

// 기간 한정 이벤트의 남은 시간을 플레이어 화면에 보여 준다.
// [JA] 期間限定イベントの残り時間をプレイヤーの画面に表示します。
public static class Lab17
{
    // 플레이어의 시계. 화면에는 플레이어 지역의 시각을 보여 준다.
    class PlayerClock(DateTime utcNow, TimeZoneInfo zone)
    {
        public DateTime UtcNow { get; } = utcNow;
        public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(UtcNow, zone);
    }

    class LimitedEvent(string name, DateTime endsAtUtc)
    {
        public string Name { get; } = name;
        public DateTime EndsAtUtc { get; } = endsAtUtc;

        public TimeSpan Remaining(PlayerClock clock) => EndsAtUtc - clock.Now;
    }

    public static void Run()
    {
        TimeZoneInfo seoul = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");

        // 서버 기준(UTC) 11월 3일 15:00 에 끝난다. 서울로는 11월 4일 자정.
        var halloween = new LimitedEvent(Text.L("할로윈 사냥", "ハロウィン狩り"), new DateTime(2026, 11, 3, 15, 0, 0, DateTimeKind.Utc));

        // 지금은 UTC 12:00, 서울로는 밤 9시.
        var clock = new PlayerClock(new DateTime(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc), seoul);

        TimeSpan remaining = halloween.Remaining(clock);
        Console.WriteLine(Text.L($"  지금 {clock.Now:yyyy-MM-dd HH:mm} (서울)", $"  現在 {clock.Now:yyyy-MM-dd HH:mm} (ソウル)"));
        Console.WriteLine(remaining > TimeSpan.Zero
            ? Text.L($"  {halloween.Name}: {remaining.TotalHours:0}시간 남음", $"  {halloween.Name}: 残り {remaining.TotalHours:0} 時間")
            : Text.L($"  {halloween.Name}: 이미 끝남 ({remaining.TotalHours:0}시간)", $"  {halloween.Name}: すでに終了 ({remaining.TotalHours:0} 時間)"));

        Expect(Text.L("남은 시간 (시간)", "残り時間 (時間)"), 3.0, remaining.TotalHours);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// Remaining 에 중단점을 걸고, 조사식에 EndsAtUtc, clock.Now, clock.UtcNow 를 넣어 보세요.
// 각각을 펼치면 Kind 라는 속성이 있습니다. 세 값의 Kind 는 무엇인가요?
//
// [JA] Remaining にブレークポイントを置き、ウォッチに EndsAtUtc、clock.Now、clock.UtcNow を入れてみてください。
// [JA] それぞれを展開すると Kind というプロパティがあります。三つの値の Kind は何ですか?
#endregion

#region 힌트 2 / ヒント 2
// 조사식에 EndsAtUtc - clock.UtcNow 와 EndsAtUtc - clock.Now 를 나란히 넣어 보세요.
// DateTime 끼리 빼기는 Kind 를 보지 않습니다. 시계 바늘의 숫자만 뺍니다.
//
// [JA] ウォッチに EndsAtUtc - clock.UtcNow と EndsAtUtc - clock.Now を並べて入れてみてください。
// [JA] DateTime 同士の引き算は Kind を見ません。時計の針の数字だけを引きます。
#endregion

#region 힌트 3 / ヒント 3
// "보여 주기용" 현지 시각을 계산에 쓰면 안 됩니다. 계산은 같은 기준끼리 하세요.
// 기준이 섞이지 않게 하는 방법으로 DateTimeOffset 도 찾아보세요.
//
// [JA] 「表示用」の現地時刻を計算に使ってはいけません。計算は同じ基準同士でしてください。
// [JA] 基準が混ざらないようにする方法として DateTimeOffset も調べてみてください。
#endregion
