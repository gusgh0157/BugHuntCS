namespace BugHunt;

// 패킷이 알려 준 크기만큼 임시 버퍼를 스택에 잡아 내용을 담고 체크섬을 낸다.
// 큰 패킷이 오면 프로그램이 통째로 사라진다(예외 메시지도, [FAIL]도 없다).
// [JA] パケットが知らせたサイズ分だけ一時バッファをスタックに取り、内容を入れてチェックサムを出します。
// [JA] 大きいパケットが来るとプログラムが丸ごと消えます(例外メッセージも [FAIL] も出ません)。
public static partial class Lab81
{
    // 패킷 내용을 임시 버퍼에 복사하고 바이트 합(체크섬)을 돌려준다.
    static long Checksum(byte[] packet)
    {
        Span<byte> scratch = stackalloc byte[packet.Length];
        packet.CopyTo(scratch);
        long sum = 0;
        foreach (byte b in scratch) sum += b;
        return sum;
    }

    public static void Run()
    {
        int size = Pick(Sizes[0], Sizes[1..]);
        byte[] packet = new byte[size];
        for (int i = 0; i < size; i++) packet[i] = (byte)(i * 31 + 7);

        long expected = 0;
        foreach (byte b in packet) expected += b;

        long sum = Checksum(packet);
        Console.WriteLine(Text.L($"  패킷 {size} 바이트, 체크섬 {sum}", $"  パケット {size} バイト、チェックサム {sum}"));
        Expect(Text.L("체크섬", "チェックサム"), expected, sum);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// Checksum 의 첫 줄에 중단점을 걸고 조사식에 packet.Length 를 넣어 보세요. 몇 바이트인가요?
// 그 줄을 F10 으로 넘기는 순간 프로그램이 사라집니다. 예외 창도 뜨지 않습니다. 어떤 예외도 잡히지 않는 죽음이 있습니다.
//
// [JA] Checksum の最初の行にブレークポイントを置いて、ウォッチに packet.Length を入れてみてください。何バイトですか?
// [JA] その行を F10 で越えた瞬間にプログラムが消えます。例外ウィンドウも出ません。どんな例外も捕まえられない死があります。
#endregion

#region 힌트 2 / ヒント 2
// stackalloc byte[n] 은 스택에 n 바이트를 잡습니다. 스택은 스레드마다 크기가 고정(보통 1 MB)입니다.
// n 이 입력에서 오는데 수백만~수천만이면 스택을 넘어서고, 이것은 StackOverflowException 입니다.
// 이 예외는 try/catch 로 잡을 수 없고 프로세스를 즉시 끝냅니다(그래서 [FAIL]도 안 찍힙니다). 채점기는 이를 CRASH 로 봅니다.
//
// [JA] stackalloc byte[n] はスタックに n バイトを取ります。スタックはスレッドごとにサイズが固定(ふつう 1 MB)です。
// [JA] n が入力から来て数百万〜数千万だとスタックを越え、これは StackOverflowException です。
// [JA] この例外は try/catch で捕まえられず、プロセスを即座に終わらせます(だから [FAIL] も出ません)。採点ツールはこれを CRASH と見ます。
#endregion

#region 힌트 3 / ヒント 3
// stackalloc 의 크기는 반드시 작고 고정된 상한 안에 두세요. 입력에서 온 크기를 그대로 stackalloc 에 넘기면 안 됩니다.
//   const int StackCap = 1024;
//   byte[]? rented = packet.Length <= StackCap ? null : System.Buffers.ArrayPool<byte>.Shared.Rent(packet.Length);
//   Span<byte> scratch = rented is null ? stackalloc byte[StackCap] : rented;
//   scratch = scratch[..packet.Length];   // ... try { 작업 } finally { 빌린 것은 반납 }
// 작은 패킷만 스택에, 큰 패킷은 힙(ArrayPool)으로. 또는 그냥 new byte[n] 으로 힙에 잡아도 됩니다(OOM 은 잡히는 예외).
//
// [JA] stackalloc のサイズは必ず小さく固定された上限の中に収めてください。入力から来たサイズをそのまま stackalloc に渡してはいけません。
// [JA]   const int StackCap = 1024;
// [JA]   byte[]? rented = packet.Length <= StackCap ? null : System.Buffers.ArrayPool<byte>.Shared.Rent(packet.Length);
// [JA]   Span<byte> scratch = rented is null ? stackalloc byte[StackCap] : rented;   // ... try { 処理 } finally { 借りたものは返す }
// [JA] 小さいパケットだけスタックに、大きいパケットはヒープ(ArrayPool)へ。または単に new byte[n] でヒープに取ってもよいです(OOM は捕まえられる例外)。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 다른 큰 크기로 검사한다. 보이는 크기만 특별 처리(힙으로)하면 다른 큰 크기에서 그대로 CRASH 한다.
// [JA] 隠しケースは別の大きいサイズで検査します。見えているサイズだけ特別扱い(ヒープに)すると、別の大きいサイズでそのまま CRASH します。
public static partial class Lab81
{
    static readonly int[] Sizes = [20_000_000, 30_000_000, 40_000_000, 50_000_000];
}
#endregion
