namespace BugHunt;

// 자동 저장과 업적 집계는 서로 다른 시스템이다. 그런데 자동 저장이 도는 동안 업적 집계가 멈춰 버린다(둘 다 응답 없음).
// [JA] オートセーブと実績集計は別々のシステムです。ところがオートセーブが動いている間、実績集計が止まってしまいます(両方とも応答なし)。
public static partial class Lab118
{
    // 자동 저장 시스템
    sealed class AutoSave
    {
        // 저장 중에는 세이브 데이터를 잠근다.
        readonly object _lock = "bughunt-save-lock";

        public void Save(ManualResetEventSlim saveStarted, ManualResetEventSlim achievementsDone)
        {
            lock (_lock)
            {
                saveStarted.Set();
                // 저장 직전 업적 집계가 끝나길 기다린다(최종 수치를 함께 저장하려고).
                achievementsDone.Wait();
            }
        }
    }

    // 업적 집계 시스템 (자동 저장과는 아무 관계가 없다)
    sealed class Achievements
    {
        readonly object _lock = "bughunt-save-lock";

        public void Tally(ManualResetEventSlim saveStarted, ManualResetEventSlim achievementsDone)
        {
            saveStarted.Wait();
            lock (_lock)
            {
                achievementsDone.Set();
            }
        }
    }

    public static void Run()
    {
        int repeats = Pick(1, 2, 3, 4);
        int completed = 0;
        if (!FinishesWithin("자동 저장과 업적 집계가 둘 다 끝남", () =>
        {
            for (int i = 0; i < repeats; i++)
            {
                var save = new AutoSave();
                var achievements = new Achievements();
                using var saveStarted = new ManualResetEventSlim(false);
                using var achievementsDone = new ManualResetEventSlim(false);

                var t1 = new Thread(() => save.Save(saveStarted, achievementsDone)) { IsBackground = true };
                var t2 = new Thread(() => achievements.Tally(saveStarted, achievementsDone)) { IsBackground = true };
                t1.Start();
                t2.Start();
                t1.Join();
                t2.Join();
                completed++;
            }
        }, timeoutMs: 3000))
            return;

        Console.WriteLine($"  저장+집계 {completed}회 완료");
        Expect("저장과 집계가 모두 끝난 횟수", repeats, completed);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 멈추면 디버그 → 모두 중단. 스레드 창(Ctrl+Alt+H)과 병렬 스택에서 두 스레드가 각각 어디서 멈췄는지 보세요.
// AutoSave.Save 는 _lock 을 쥔 채 achievementsDone 을 기다립니다. Achievements.Tally 는 _lock 을 잡으려고 합니다. 두 _lock 은 같은 객체일까요?
// Run 의 프레임에서 조사식에 object.ReferenceEquals(save._lock, achievements._lock) 를 넣어 보세요. 두 클래스의 _lock 초기값도 비교해 보세요.
//
// [JA] 止まったらデバッグ → すべて中断。スレッドウィンドウ(Ctrl+Alt+H)と並列スタックで、二つのスレッドがそれぞれどこで止まったか見てください。
// [JA] AutoSave.Save は _lock を握ったまま achievementsDone を待ちます。Achievements.Tally は _lock を取ろうとします。二つの _lock は同じオブジェクトでしょうか?
// [JA] Run のフレームでウォッチに object.ReferenceEquals(save._lock, achievements._lock) を入れてみてください。二つのクラスの _lock の初期値も比べてください。
#endregion

#region 힌트 2 / ヒント 2
// 두 클래스는 각자 readonly object _lock = "bughunt-save-lock"; 로 "자기만의" 잠금을 둔 줄 알지만, 둘 다 같은 문자열 리터럴입니다.
// 문자열 리터럴은 프로세스 전체에서 인터닝(interning)되어 하나의 객체로 공유됩니다. 그래서 두 _lock 은 같은 모니터입니다.
// AutoSave 가 그 모니터를 쥔 채 업적이 끝나길 기다리는데, 업적은 같은 모니터를 잡지 못해 영영 못 끝냅니다. 서로를 기다리는 데드락입니다.
//
// [JA] 二つのクラスはそれぞれ readonly object _lock = "bughunt-save-lock"; で「自分専用の」ロックを持ったつもりですが、どちらも同じ文字列リテラルです。
// [JA] 文字列リテラルはプロセス全体でインターン(interning)され、一つのオブジェクトとして共有されます。そのため二つの _lock は同じモニターです。
// [JA] AutoSave がそのモニターを握ったまま実績が終わるのを待ち、実績は同じモニターを取れず永遠に終われません。互いを待つデッドロックです。
#endregion

#region 힌트 3 / ヒント 3
// 절대 공유될 수 있는 것에 lock 하지 마세요: 문자열, 인터닝된 값, typeof(X), this, 그리고 다른 코드가 손댈 수 있는 모든 객체.
// 잠금 대상은 오직 그 목적으로 만든 private 객체여야 합니다: private readonly object _lock = new();
// 그러면 두 시스템의 잠금이 서로 다른 객체가 되어, 자동 저장이 자기 잠금을 쥐어도 업적은 자기 잠금으로 진행됩니다.
// lock(this) 도 같은 위험입니다: 바깥 코드가 lock(myObject) 하면 내 메서드를 막을 수 있습니다. 애초에 이 설계는 "잠근 채로 다른 작업을 기다리는" 모양이라, 잠금 범위도 짧게 줄이세요.
//
// [JA] 絶対に共有されうるものに lock しないでください: 文字列、インターンされた値、typeof(X)、this、そして他のコードが触れうるすべてのオブジェクト。
// [JA] ロックの対象はその目的だけのために作った private オブジェクトであるべきです: private readonly object _lock = new();
// [JA] そうすれば二つのシステムのロックが別々のオブジェクトになり、オートセーブが自分のロックを握っても実績は自分のロックで進めます。
// [JA] lock(this) も同じ危険です: 外のコードが lock(myObject) すると自分のメソッドを止められます。そもそもこの設計は「ロックしたまま別の作業を待つ」形なので、ロックの範囲も短くしてください。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 반복 횟수를 늘려 검사한다. 각 시스템이 자기만의 private 잠금 객체를 쓰면 통과한다.
// [JA] 隠しケースは繰り返し回数を増やして検査します。各システムが自分専用の private ロックオブジェクトを使えば通ります。
#endregion
