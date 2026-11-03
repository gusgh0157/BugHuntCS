namespace BugHunt;

// 레벨에 들어가면 텍스처를 GPU 메모리에 올리고, 레벨을 나가면 내린다.
public static class Lab38
{
    // GPU 메모리 사용량. (실제로는 그래픽 드라이버가 관리하는 비관리 자원이라고 치자)
    static class GpuMemory
    {
        static long _bytesInUse;
        public static long BytesInUse => Interlocked.Read(ref _bytesInUse);
        public static void Allocate(long bytes) => Interlocked.Add(ref _bytesInUse, bytes);
        public static void Free(long bytes) => Interlocked.Add(ref _bytesInUse, -bytes);
    }

    class Texture
    {
        public string Name { get; }
        public long Bytes { get; }

        public Texture(string name, long bytes)
        {
            Name = name;
            Bytes = bytes;
            GpuMemory.Allocate(bytes);
        }

        // C++ 소멸자처럼, 텍스처를 다 쓰면 GPU 메모리를 돌려준다.
        ~Texture()
        {
            GpuMemory.Free(Bytes);
        }
    }

    class Level(string name)
    {
        readonly List<Texture> _textures = [];

        public void Enter()
        {
            foreach (string tex in new[] { "ground", "trees", "water", "sky" })
                _textures.Add(new Texture($"{name}/{tex}", 16L * 1024 * 1024));
            Console.WriteLine($"  {name} 입장: GPU {GpuMemory.BytesInUse / (1024 * 1024)}MB");
        }

        public void Exit()
        {
            _textures.Clear();
            Console.WriteLine($"  {name} 퇴장: GPU {GpuMemory.BytesInUse / (1024 * 1024)}MB");
        }
    }

    public static void Run()
    {
        var forest = new Level("Forest");
        forest.Enter();
        forest.Exit();

        var cave = new Level("Cave");
        cave.Enter();
        long duringCave = GpuMemory.BytesInUse;
        cave.Exit();

        Console.WriteLine($"  지금까지 GC 횟수(0세대): {GC.CollectionCount(0)}");
        Expect("동굴에 있을 때 GPU 메모리 (MB)", 64L, duringCave / (1024 * 1024));
        Expect("레벨을 다 나온 뒤 GPU 메모리 (MB)", 0L, GpuMemory.BytesInUse / (1024 * 1024));
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// ~Texture() 안에 중단점을 거세요. Exit() 를 지나는 동안 멈추나요? 프로그램이 끝날 때까지는요?
#endregion

#region 힌트 2
// 종료자(finalizer)는 언제 불릴까요? 조사식에 GC.CollectionCount(0) 을 넣어 보세요.
// _textures.Clear() 다음 줄에서 직접 조사식에 GC.Collect() 를 넣어 실행해 보고, 다시 ~Texture() 중단점을 보세요.
// 멈춘다면 호출 스택과 스레드 창을 보세요. 어느 스레드인가요?
#endregion

#region 힌트 3
// C# 의 종료자는 C++ 소멸자가 아닙니다. 객체가 "쓸모없어진 순간"이 아니라
// "GC 가 언젠가 그 객체를 발견한 다음, 종료자 스레드가 짬이 날 때" 불립니다. 그리고 .NET 은 프로그램이 끝날 때 종료자를 부르지 않습니다.
// 정해진 순간에 자원을 돌려줘야 한다면 무엇을 써야 할까요? (IDisposable)
#endregion
