using System.Runtime.CompilerServices;

namespace BugHunt;

// 전투 기록 창은 열려 있는 동안 피해 이벤트를 받아 줄을 쌓는다. 창을 닫으면 버려진다.
public static class Lab37
{
    static class GameEvents
    {
        public static event Action<string, int>? DamageDealt;

        public static void RaiseDamage(string target, int amount) => DamageDealt?.Invoke(target, amount);
    }

    class CombatLogWindow
    {
        readonly List<string> _lines = [];
        readonly byte[] _textureCache = new byte[256 * 1024];   // 창 그림을 캐시해 둔 텍스처라고 치자

        public bool IsOpen { get; private set; }
        public int LineCount => _lines.Count;

        public void Open()
        {
            IsOpen = true;
            GameEvents.DamageDealt += OnDamageDealt;
        }

        public void Close()
        {
            IsOpen = false;
            _lines.Clear();
        }

        void OnDamageDealt(string target, int amount) => _lines.Add($"{target} -{amount}");
    }

    // 창 50개를 열고 닫는다. 창에 대한 강한 참조는 이 메서드가 끝나면 모두 사라진다.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static List<WeakReference> OpenAndCloseWindows(int count)
    {
        var refs = new List<WeakReference>();
        for (int i = 0; i < count; i++)
        {
            var window = new CombatLogWindow();
            window.Open();
            GameEvents.RaiseDamage("Slime", 10 + i);
            window.Close();
            refs.Add(new WeakReference(window));
        }
        return refs;
    }

    public static void Run()
    {
        List<WeakReference> windows = OpenAndCloseWindows(50);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        int alive = windows.Count(w => w.IsAlive);
        Console.WriteLine($"  닫은 창 50개 중 메모리에 남은 창: {alive}");
        PauseForTools("지금 진단 도구에서 메모리 스냅샷을 찍거나 dotnet-dump collect 를 해 보세요.");

        Expect("GC 후 남은 전투 기록 창", 0, alive);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// F5 로 실행하고, "계속하려면 Enter" 가 나오면 VS 의 진단 도구 창(Ctrl+Alt+F2) → 메모리 사용량 → 스냅샷 생성.
// 스냅샷을 열고 CombatLogWindow 를 검색하세요. 몇 개인가요?
#endregion

#region 힌트 2
// 스냅샷에서 CombatLogWindow 를 고르고 아래쪽 "루트 경로"(Paths to Root)를 펼치세요.
// 누가 이 창을 붙잡고 있나요? 맨 끝(루트)은 무엇인가요?
// dotnet-dump 로 할 때는: dumpheap -type CombatLogWindow → 주소 하나 골라서 gcroot <주소>
#endregion

#region 힌트 3
// 이벤트에 구독하면 이벤트 쪽이 구독자(대리자 → 대상 객체)를 참조합니다. 반대 방향이 아닙니다.
// 이벤트가 static 이면 그 참조는 프로그램이 끝날 때까지 살아 있는 GC 루트입니다.
#endregion
