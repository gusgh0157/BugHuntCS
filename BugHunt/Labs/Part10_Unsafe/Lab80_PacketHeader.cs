using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace BugHunt;

// 서버가 보낸 채팅 패킷을 읽는다. 서버 문서의 패킷 형식(리틀 엔디언, 빈틈 없이 붙어 있음):
//   [Type: 1바이트][Length: 4바이트 int][Seq: 2바이트 ushort][본문: Length 바이트 UTF-8]
// [JA] サーバーが送ったチャット パケットを読みます。サーバー ドキュメントのパケット形式(リトル エンディアン、隙間なく詰まっている):
// [JA]   [Type: 1バイト][Length: 4バイト int][Seq: 2バイト ushort][本文: Length バイト UTF-8]
public static class Lab80
{
    [StructLayout(LayoutKind.Sequential)]
    struct PacketHeader
    {
        public byte Type;
        public int Length;
        public ushort Seq;
    }

    const byte ChatPacket = 7;

    static string? ReadChat(ReadOnlySpan<byte> packet)
    {
        PacketHeader header = MemoryMarshal.Read<PacketHeader>(packet);
        if (header.Type != ChatPacket)
            return null;
        int headerSize = Unsafe.SizeOf<PacketHeader>();
        return $"#{header.Seq} {Encoding.UTF8.GetString(packet.Slice(headerSize, header.Length))}";
    }

    // 서버 쪽 코드 흉내: 문서대로 바이트를 하나씩 쓴다. 이 함수에는 버그가 없다.
    // [JA] サーバー側のコードのまね: 文書どおりにバイトを一つずつ書きます。この関数にバグはありません。
    static byte[] ServerSend(ushort seq, string text)
    {
        byte[] body = Encoding.UTF8.GetBytes(text);
        var packet = new byte[7 + body.Length];
        packet[0] = ChatPacket;
        BitConverter.TryWriteBytes(packet.AsSpan(1, 4), body.Length);
        BitConverter.TryWriteBytes(packet.AsSpan(5, 2), seq);
        body.CopyTo(packet, 7);
        return packet;
    }

    public static void Run()
    {
        string[] pool = [Text.L("안녕하세요", "こんにちは"), "GG", Text.L("보스 잡으러 갈 사람", "ボス狩りに行く人"), Text.L("ㅋㅋㅋ", "www"), "Need healer!", Text.L("내일 8시 레이드", "明日8時にレイド")];
        int count = Pick(3, 2, 6);
        ushort firstSeq = (ushort)Pick(41, 1, 60000);

        var expected = new List<string>();
        var shown = new List<string>();
        for (int i = 0; i < count; i++)
        {
            string text = Seed == 0 ? pool[i] : Pick(pool[0], pool);
            ushort seq = (ushort)(firstSeq + i);
            byte[] packet = ServerSend(seq, text);
            expected.Add($"#{seq} {text}");

            try
            {
                string? chat = ReadChat(packet);
                if (chat != null)
                    shown.Add(chat);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(Text.L($"  손상된 패킷 버림 ({packet.Length}바이트): {ex.GetType().Name}", $"  破損したパケットを破棄 ({packet.Length}バイト): {ex.GetType().Name}"));
            }
        }

        foreach (string chat in shown)
            Console.WriteLine(Text.L($"  채팅 {chat}", $"  チャット {chat}"));

        Expect(Text.L("화면에 나온 채팅 수", "画面に表示されたチャット数"), count, shown.Count);
        Expect(Text.L("채팅 내용", "チャット内容"), string.Join(" / ", expected), string.Join(" / ", shown));
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 예외 설정(Ctrl+Alt+E)에서 System.ArgumentOutOfRangeException 을 체크하고 F5. ReadChat 에서 멈추면 header 를 펼쳐 보세요.
// Type, Length, Seq 가 서버가 보낸 값과 같나요? 조사식에 packet 도 넣어 바이트를 하나씩 보세요.
//
// [JA] 例外設定(Ctrl+Alt+E)で System.ArgumentOutOfRangeException にチェックを入れて F5。ReadChat で止まったら header を展開してみてください。
// [JA] Type、Length、Seq はサーバーが送った値と同じですか? ウォッチに packet も入れてバイトを一つずつ見てください。
#endregion

#region 힌트 2 / ヒント 2
// 조사식에 Unsafe.SizeOf<PacketHeader>() 와 Marshal.SizeOf<PacketHeader>() 를 넣어 보세요. 1 + 4 + 2 = 7 인가요?
// 디버그 → 창 → 메모리 1 에서 &header 를 보면 필드 사이에 무엇이 끼어 있는지 보입니다.
//
// [JA] ウォッチに Unsafe.SizeOf<PacketHeader>() と Marshal.SizeOf<PacketHeader>() を入れてみてください。1 + 4 + 2 = 7 ですか?
// [JA] デバッグ → ウィンドウ → メモリ 1 で &header を見ると、フィールドの間に何が挟まっているかが見えます。
#endregion

#region 힌트 3 / ヒント 3
// 런타임은 int 를 4의 배수 주소에 두려고 필드 사이에 빈 바이트(padding)를 넣습니다. 그래서 Length 는 1번이 아니라 4번 바이트부터 읽힙니다.
// 바이트 형식이 정해진 데이터를 구조체로 그대로 읽으려면 [StructLayout(LayoutKind.Sequential, Pack = 1)].
// 또는 구조체 없이 BinaryPrimitives.ReadInt32LittleEndian 으로 위치를 직접 정해서 읽는 편이 더 안전합니다.
//
// [JA] ランタイムは int を 4 の倍数のアドレスに置くために、フィールドの間に空きバイト(padding)を入れます。だから Length は 1 番ではなく 4 番のバイトから読まれます。
// [JA] バイト形式が決まっているデータを構造体でそのまま読むなら [StructLayout(LayoutKind.Sequential, Pack = 1)]。
// [JA] または構造体を使わず BinaryPrimitives.ReadInt32LittleEndian で位置を直接決めて読むほうが、より安全です。
#endregion
