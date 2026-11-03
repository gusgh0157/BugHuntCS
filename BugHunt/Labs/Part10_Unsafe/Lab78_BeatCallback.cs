using System.Runtime.InteropServices;

namespace BugHunt;

// 리듬 게임. 오디오 엔진(네이티브)이 박자마다 콜백을 불러 주면, 그때 노트 판정을 한다.
public static unsafe class Lab78
{
    // 네이티브 쪽이 부를 함수의 모양
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate int BeatCallback(int beat);

    // "네이티브" 오디오 엔진 흉내. 실제로는 C++ DLL 이라서 함수 포인터(숫자)만 받아 두고, 나중에 박자마다 부른다고 치자.
    // 이 클래스에는 버그가 없다.
    static class NativeAudio
    {
        static delegate* unmanaged[Cdecl]<int, int> _onBeat;

        public static void SetBeatCallback(IntPtr callback) => _onBeat = (delegate* unmanaged[Cdecl]<int, int>)callback;

        public static int PlayBeat(int beat) => _onBeat(beat);
    }

    class RhythmGame
    {
        public int Score { get; private set; }
        public int Combo { get; private set; }

        public void Start()
        {
            NativeAudio.SetBeatCallback(Marshal.GetFunctionPointerForDelegate<BeatCallback>(OnBeat));
            Console.WriteLine("  오디오 엔진에 박자 콜백 등록");
        }

        int OnBeat(int beat)
        {
            Combo++;
            Score += 100 * Combo;
            return Combo;
        }
    }

    // 곡 데이터를 읽고 임시 데이터를 정리한다. 이 함수에는 버그가 없다.
    static void LoadSong(string name)
    {
        var notes = new List<int[]>();
        for (int i = 0; i < 1000; i++)
            notes.Add(new int[16]);
        notes.Clear();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Console.WriteLine($"  곡 \"{name}\" 로드 완료");
    }

    public static void Run()
    {
        var game = new RhythmGame();
        game.Start();
        LoadSong(Pick("Starlight", "Neon Rain", "Paper Moon", "Overdrive"));

        int beats = Pick(8, 3, 20);
        for (int beat = 1; beat <= beats; beat++)
            NativeAudio.PlayBeat(beat);
        Console.WriteLine($"  {beats}박자 연주 끝: 점수 {game.Score}, 콤보 {game.Combo}");

        Expect("콤보", beats, game.Combo);
        Expect("점수", 100 * beats * (beats + 1) / 2, game.Score);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// 디버거 없이 실행하면 콘솔에 "Process terminated." 다음 한 줄이 남습니다. 끝까지 읽어 보세요. 어떤 형식(type) 이름이 나오나요?
// F5 로 실행하면 VS 는 어디서 멈추나요? 호출 스택에서 내 코드 프레임을 찾아보세요.
#endregion

#region 힌트 2
// GetFunctionPointerForDelegate 에 넘긴 대리자(delegate)는 누가 들고 있나요? 메서드 그룹 OnBeat 를 넘기면
// 그 자리에서 new BeatCallback(OnBeat) 가 만들어집니다. 이 대리자를 가리키는 관리 참조는 몇 개인가요?
// 네이티브 쪽이 가진 것은 함수 포인터, 즉 숫자일 뿐입니다. GC 는 그 숫자를 참조로 보지 않습니다.
#endregion

#region 힌트 3
// LoadSong 의 GC.Collect() 가 아무도 참조하지 않는 대리자를 수거하고, 그 대리자용으로 만든 네이티브 진입점(thunk)도 같이 사라집니다.
// 네이티브가 콜백을 들고 있는 동안에는 대리자도 살아 있어야 합니다. 어디에 붙잡아 둘까요? (필드)
// 또는 대리자 없이 [UnmanagedCallersOnly] static 메서드의 주소(&OnBeat)를 넘기는 방법도 있습니다.
#endregion
