namespace BugHunt;

// 설정 창을 닫을 때, 바뀐 항목만 서버에 다시 보낸다.
// [JA] 設定画面を閉じるとき、変わった項目だけをサーバーに送り直します。
public static class Lab12
{
    class Settings
    {
        public int Volume { get; set; } = 50;
        public bool Fullscreen { get; set; } = true;
        public string Language { get; set; } = "ko";
        public double MouseSpeed { get; set; } = 1.25;
    }

    static Dictionary<string, object> Snapshot(Settings s) => new()
    {
        ["volume"] = s.Volume,
        ["fullscreen"] = s.Fullscreen,
        ["language"] = s.Language,
        ["mouseSpeed"] = s.MouseSpeed,
    };

    static List<string> ChangedKeys(Dictionary<string, object> before, Dictionary<string, object> after)
    {
        var changed = new List<string>();
        foreach (string key in before.Keys)
        {
            if (before[key] != after[key])
                changed.Add(key);
        }
        return changed;
    }

    public static void Run()
    {
        var settings = new Settings();
        Dictionary<string, object> before = Snapshot(settings);

        // 플레이어가 설정 창에서 볼륨만 70 으로 바꿨다.
        settings.Volume = 70;

        Dictionary<string, object> after = Snapshot(settings);
        List<string> changed = ChangedKeys(before, after);
        Console.WriteLine($"  서버로 보낼 항목: {string.Join(", ", changed)}");

        Expect("바뀐 항목", "volume", string.Join(", ", changed));
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// ChangedKeys 의 if 줄에 조건부 중단점을 거세요. 조건: key == "fullscreen"
// 멈추면 조사식에 before[key], after[key], before[key] != after[key] 를 넣어 보세요.
// 값은 둘 다 true 인데 왜 다르다고 할까요?
//
// [JA] ChangedKeys の if の行に条件付きブレークポイントを置いてください。条件: key == "fullscreen"
// [JA] 止まったら、ウォッチに before[key]、after[key]、before[key] != after[key] を入れてみてください。
// [JA] 値はどちらも true なのに、なぜ違うと言うのでしょう?
#endregion

#region 힌트 2 / ヒント 2
// 조사식에 before[key].Equals(after[key]) 와 object.ReferenceEquals(before[key], after[key]) 도 넣어 보세요.
// 변수의 형식(조사식 창의 "형식" 열)이 object 일 때 == 와 != 는 무엇을 비교할까요?
//
// [JA] ウォッチに before[key].Equals(after[key]) と object.ReferenceEquals(before[key], after[key]) も入れてみてください。
// [JA] 変数の型(ウォッチ ウィンドウの「型」列)が object のとき、== と != は何を比べるのでしょう?
#endregion

#region 힌트 3 / ヒント 3 (생각해 볼 것 / 考えてみること)
// language 는 왜 "안 바뀌었다"고 맞게 나올까요? 같은 키로 조사식에 ReferenceEquals 를 넣어 보세요.
// 맞게 나오는 이유가 "우연"이라면, 그것도 같은 버그입니다.
//
// [JA] language はなぜ「変わっていない」と正しく出るのでしょう? 同じキーでウォッチに ReferenceEquals を入れてみてください。
// [JA] 正しく出る理由が「偶然」なら、それも同じバグです。
#endregion
