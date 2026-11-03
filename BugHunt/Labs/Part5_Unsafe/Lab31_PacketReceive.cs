using System.Runtime.InteropServices;

namespace BugHunt;

// 네트워크 라이브러리(네이티브)에 바이트 배열을 고정해서 넘기면, 받은 패킷을 그 배열에 채워 준다.
public static class Lab31
{
    // "네이티브" 수신 함수 흉내. 주소에 패킷 바이트를 채운다. 마지막 바이트는 체크섬이다. 이 클래스에는 버그가 없다.
    static class NativeNet
    {
        public static void Receive(IntPtr buffer, int length, int packetId, bool corrupt)
        {
            byte sum = 0;
            for (int i = 0; i < length - 1; i++)
            {
                byte b = (byte)(packetId * 31 + i);
                Marshal.WriteByte(buffer, i, b);
                sum += b;
            }
            Marshal.WriteByte(buffer, length - 1, corrupt ? (byte)(sum + 1) : sum);
        }
    }

    // 메모리 진단용: 받은 버퍼가 GC 에 수거됐는지 지켜본다. 이 클래스에는 버그가 없다.
    static readonly List<WeakReference> Tracked = [];

    static byte[] ReceivePacket(int packetId, bool corrupt)
    {
        var buffer = new byte[256];
        Tracked.Add(new WeakReference(buffer));

        GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        NativeNet.Receive(handle.AddrOfPinnedObject(), buffer.Length, packetId, corrupt);

        byte sum = 0;
        for (int i = 0; i < buffer.Length - 1; i++)
            sum += buffer[i];
        if (sum != buffer[^1])
            throw new InvalidDataException($"패킷 {packetId} 체크섬 불일치");

        handle.Free();
        return buffer;
    }

    static int ReceiveAll(int count, HashSet<int> corruptIds)
    {
        int ok = 0;
        for (int id = 1; id <= count; id++)
        {
            try
            {
                byte[] packet = ReceivePacket(id, corruptIds.Contains(id));
                if (packet[0] == (byte)(id * 31))
                    ok++;
            }
            catch (InvalidDataException ex)
            {
                Console.WriteLine($"  손상된 패킷 버림: {ex.Message}");
            }
        }
        return ok;
    }

    public static void Run()
    {
        int count = Pick(10, 6, 30);
        HashSet<int> corrupt = Seed == 0 ? [3, 7] : [.. Enumerable.Range(1, count).Where(_ => Pick(0, 0, 3) == 0), count];

        int ok = ReceiveAll(count, corrupt);
        Console.WriteLine($"  패킷 {count}개 중 {ok}개 처리");

        // 처리가 끝난 패킷 버퍼는 이제 아무도 쓰지 않는다. GC 를 돌리면 전부 수거되어야 한다.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        int alive = Tracked.Count(w => w.IsAlive);
        Console.WriteLine($"  GC 뒤에도 살아 있는 패킷 버퍼: {alive}개, 고정된 객체: {GC.GetGCMemoryInfo().PinnedObjectsCount}개");

        Expect("처리한 패킷 수", count - corrupt.Count, ok);
        Expect("GC 뒤에 살아 있는 패킷 버퍼", 0, alive);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// 살아 남은 버퍼 수와 손상된 패킷 수를 비교해 보세요. 우연일까요?
// 진단 도구(Ctrl+Alt+F2) → 메모리 사용량 → 스냅샷 생성 → Byte[] 를 찾아 루트 경로를 보세요. 사슬 끝에 무엇이 있나요?
#endregion

#region 힌트 2
// ReceivePacket 의 throw 줄에 중단점을 걸고 F10 으로 한 줄씩 가 보세요. handle.Free() 는 실행되나요?
// GCHandle 은 GC 바깥의 "강한 참조"입니다. Free 하기 전까지 그 객체는 절대 수거되지 않고, Pinned 라면 옮겨지지도 않습니다.
#endregion

#region 힌트 3
// 예외가 나도 반드시 실행되어야 하는 정리 코드는 어디에 둬야 할까요? (try/finally)
// 더 쉬운 방법도 있습니다. 짧게 고정하는 거라면 fixed 문, 아니면 GC.AllocateArray(..., pinned: true).
#endregion
