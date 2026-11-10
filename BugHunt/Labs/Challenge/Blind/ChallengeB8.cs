using System.Runtime.CompilerServices;

namespace BugHunt;

// 텍스처 스트리밍. 텍스처 픽셀은 네이티브 메모리 풀에 있고, 텍스처 객체가 사라지면(종료자) 블록을 풀에 돌려준다.
// [JA] テクスチャ ストリーミング。テクスチャのピクセルはネイティブ メモリ プールにあり、テクスチャ オブジェクトが消えると(ファイナライザー)ブロックをプールに返します。
public static class ChallengeB8
{
    const int Rows = 64, RowBytes = 64;

    static class NativePool
    {
        static readonly byte[][] Blocks = [.. Enumerable.Range(0, 8).Select(_ => GC.AllocateArray<byte>(Rows * RowBytes, pinned: true))];
        static readonly Stack<int> Free = new(Enumerable.Range(0, 8).Reverse());
        static readonly object Gate = new();

        public static int Allocate() { lock (Gate) return Free.Pop(); }
        public static void Release(int block) { lock (Gate) Free.Push(block); }
        public static Span<byte> Pixels(int block) => Blocks[block];
    }

    sealed class Texture
    {
        readonly int _block = NativePool.Allocate();

        ~Texture() => NativePool.Release(_block);

        // 핫 경로라서 처음부터 최적화해서 컴파일한다
        // [JA] ホットパスなので最初から最適化してコンパイルする
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public uint FillAndChecksum(byte color, Action everyFewRows)
        {
            Span<byte> pixels = NativePool.Pixels(_block);
            for (int row = 0; row < Rows; row++)
            {
                pixels.Slice(row * RowBytes, RowBytes).Fill(color);
                if (row % 8 == 7)
                    everyFewRows();
            }
            uint sum = 0;
            foreach (byte b in pixels)
                sum = sum * 31 + b;
            return sum;
        }
    }

    static readonly List<Texture> Streamed = [];

    // 다른 텍스처를 스트리밍해 온다. 그 사이 GC 가 돈다.
    // [JA] 別のテクスチャをストリーミングしてきます。その間に GC が走ります。
    static void StreamOne()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        if (Streamed.Count < 6)
        {
            var other = new Texture();
            other.FillAndChecksum(0xEE, () => { });
            Streamed.Add(other);
        }
    }

    static uint Expected(byte color)
    {
        uint sum = 0;
        for (int i = 0; i < Rows * RowBytes; i++)
            sum = sum * 31 + color;
        return sum;
    }

    // 미리보기용 체크섬: 임시 텍스처에 색을 채우고 체크섬만 받아 간다. 매 프레임 불린다
    // [JA] プレビュー用チェックサム: 一時テクスチャに色を塗り、チェックサムだけ受け取ります。毎フレーム呼ばれます
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    static uint PreviewChecksum(byte color) => new Texture().FillAndChecksum(color, StreamOne);

    public static void Run()
    {
        byte color = (byte)Pick(0x40, 0x10, 0x90);
        uint actual = PreviewChecksum(color);
        Expect(Text.L("미리보기 체크섬", "プレビューのチェックサム"), Expected(color), actual);
    }
}
