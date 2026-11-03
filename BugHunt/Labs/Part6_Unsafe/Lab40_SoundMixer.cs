namespace BugHunt;

// 효과음 볼륨을 샘플 버퍼에 써 두면, 오디오 장치(네이티브 쪽)가 매 프레임 그 버퍼를 읽어 섞는다.
public static unsafe class Lab40
{
    // "네이티브" 오디오 장치 흉내. 실제로는 C++ 드라이버라서 버퍼의 주소만 받아 두고 나중에 읽는다고 치자.
    // 이 클래스에는 버그가 없다.
    static class AudioDevice
    {
        static short* _buffer;
        static int _length;

        public static void Attach(short* buffer, int length)
        {
            _buffer = buffer;
            _length = length;
        }

        public static int MixFrame()
        {
            int sum = 0;
            for (int i = 0; i < _length; i++)
                sum += _buffer[i];
            return sum;
        }
    }

    class Mixer
    {
        public short[] Samples { get; } = new short[32];

        public void Start()
        {
            fixed (short* p = Samples)
            {
                AudioDevice.Attach(p, Samples.Length);
            }
            Console.WriteLine("  오디오 장치에 버퍼 연결");
        }

        public void SetVolume(short volume) => Array.Fill(Samples, volume);
    }

    // 레벨 데이터를 읽고, 다 읽으면 임시 데이터를 버리고 힙을 정리한다. 이 함수에는 버그가 없다.
    static void LoadLevel(string name)
    {
        var temp = new List<byte[]>();
        for (int i = 0; i < 2000; i++)
            temp.Add(new byte[64]);
        temp.Clear();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        Console.WriteLine($"  {name} 로드 완료, 힙 정리");
    }

    public static void Run()
    {
        // 레벨 하나를 미리 읽어 두고(임시 데이터가 힙에 남아 있다) 오디오를 켠다.
        var preload = new List<byte[]>();
        for (int i = 0; i < 2000; i++)
            preload.Add(new byte[64]);

        var mixer = new Mixer();
        mixer.Start();
        preload.Clear();

        short[] volumes = [Pick<short>(100, 37, 250, 999, 1234), Pick<short>(40, 3, 77, 500, 811)];
        int[] mixed = new int[volumes.Length];
        for (int frame = 0; frame < volumes.Length; frame++)
        {
            LoadLevel(frame == 0 ? "Forest" : "Cave");
            mixer.SetVolume(volumes[frame]);
            mixed[frame] = AudioDevice.MixFrame();
            Console.WriteLine($"  프레임 {frame}: 볼륨 {volumes[frame]} → 장치가 섞은 값 {mixed[frame]}");
        }

        for (int frame = 0; frame < volumes.Length; frame++)
            Expect($"프레임 {frame}에 장치가 읽은 합", volumes[frame] * mixer.Samples.Length, mixed[frame]);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// Mixer.Start 의 Attach 줄과 MixFrame 의 for 줄에 중단점을 거세요.
// 조사식에 (long)p 와 (long)_buffer 를 넣고, MixFrame 에서는 &mixer.Samples[0] 대신 즉시 창에서
// (long)System.Runtime.CompilerServices.Unsafe.AsPointer(ref mixer.Samples[0]) 을 계산해 비교해 보세요. (Run 프레임에서)
#endregion

#region 힌트 2
// LoadLevel 을 지나기 전과 지난 뒤에 Samples 배열의 주소가 같은가요?
// fixed 는 "이 블록 안에서만" GC 에게 배열을 옮기지 말라고 부탁합니다. 블록이 끝나면 부탁도 끝납니다.
#endregion

#region 힌트 3
// GC 는 힙을 압축(compacting)하면서 살아 있는 객체를 옮깁니다. 관리 코드의 참조는 GC 가 같이 고쳐 주지만,
// 포인터(short*)는 GC 가 모르는 숫자일 뿐이라 옛 주소를 그대로 가리킵니다.
// 네이티브 쪽이 오래 들고 있을 버퍼라면: 옮겨지지 않는 메모리가 필요합니다.
// (GC.AllocateArray(pinned: true), GCHandle.Alloc(..., GCHandleType.Pinned), NativeMemory.Alloc)
#endregion
