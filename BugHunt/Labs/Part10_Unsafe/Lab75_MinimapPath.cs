using System.Runtime.CompilerServices;

namespace BugHunt;

// 미니맵에 길찾기 경로를 그린다. 경로는 매 프레임 새로 구하므로 힙 할당을 줄이려고 stackalloc 버퍼를 쓴다.
// [JA] ミニマップに経路探索の経路を描きます。経路は毎フレーム新しく求めるので、ヒープ割り当てを減らすために stackalloc バッファを使います。
public static unsafe class Lab75
{
    const int MaxPathLength = 16;

    // 처음에는 Span<int>를 돌려주게 짰는데 컴파일러가 막았다(CS8352: 스택 메모리를 밖으로 내보낼 수 없음).
    // 그래서 포인터로 바꿨더니 빌드가 된다.
    // [JA] 最初は Span<int> を返すように書いたのですが、コンパイラーに止められました(CS8352: スタックのメモリを外に出せない)。
    // [JA] それでポインターに変えたらビルドが通ります。
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int* FindPath(int startTile, int goalTile, out int length)
    {
        int* path = stackalloc int[MaxPathLength];
        int step = startTile < goalTile ? 1 : -1;
        length = 0;
        for (int tile = startTile; tile != goalTile + step; tile += step)
            path[length++] = tile;
        return path;
    }

    // 미니맵 배경을 그린다. 이 함수에는 버그가 없다.
    // [JA] ミニマップの背景を描きます。この関数にバグはありません。
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void DrawMinimapBackground()
    {
        Span<int> pixels = stackalloc int[256];
        pixels.Fill(-1);
        Console.WriteLine(Text.L($"  미니맵 배경 그림 ({pixels.Length}픽셀)", $"  ミニマップの背景を描画 ({pixels.Length}ピクセル)"));
    }

    public static void Run()
    {
        int start = Pick(3, 0, 40);
        int goal = Pick(9, Math.Max(0, start - MaxPathLength + 1), start + MaxPathLength - 1);

        int* path = FindPath(start, goal, out int length);
        DrawMinimapBackground();

        var tiles = new List<int>();
        for (int i = 0; i < length; i++)
            tiles.Add(path[i]);
        Console.WriteLine(Text.L($"  경로: {string.Join(" → ", tiles)}", $"  経路: {string.Join(" → ", tiles)}"));

        var expected = new List<int>();
        for (int tile = start; tile != goal; tile += start < goal ? 1 : -1)
            expected.Add(tile);
        expected.Add(goal);

        Expect(Text.L("경로 타일 수", "経路のタイル数"), expected.Count, length);
        Expect(Text.L("경로", "経路"), string.Join(" → ", expected), string.Join(" → ", tiles));
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// FindPath 의 return path; 줄에 중단점을 거세요. 조사식에 path[0], path[1] 을 넣어 보세요. 맞는 값인가요?
// 그다음 Run 으로 나와서(Shift+F11) DrawMinimapBackground() 를 F10 으로 넘긴 뒤 다시 path[0] 을 보세요.
//
// [JA] FindPath の return path; の行にブレークポイントを置いてください。ウォッチに path[0]、path[1] を入れてみてください。正しい値ですか?
// [JA] 次に Run に戻って(Shift+F11)DrawMinimapBackground() を F10 で越えたあと、もう一度 path[0] を見てください。
#endregion

#region 힌트 2 / ヒント 2
// 조사식에 (long)path 를 넣고, DrawMinimapBackground 안에서 &pixels[0] 도 넣어 보세요. (조사식은 unsafe 식도 됩니다)
// 디버그 → 창 → 메모리 → 메모리 1 에 path 를 넣으면 그 주소의 바이트가 바뀌는 순간을 볼 수 있습니다.
//
// [JA] ウォッチに (long)path を入れ、DrawMinimapBackground の中で &pixels[0] も入れてみてください。(ウォッチは unsafe の式も使えます)
// [JA] デバッグ → ウィンドウ → メモリ → メモリ 1 に path を入れると、そのアドレスのバイトが変わる瞬間を見られます。
#endregion

#region 힌트 3 / ヒント 3
// stackalloc 메모리는 그 메서드의 스택 프레임 안에 있습니다. 메서드가 돌아가면 그 자리는 "빈 자리"가 되고,
// 다음에 부르는 메서드가 그대로 다시 씁니다. 컴파일러가 Span 을 막은 데에는 이유가 있었습니다.
// 버퍼는 누가 만들어야 할까요? 오래 살아야 하는 쪽(호출자)이 만들어서 Span 으로 넘겨 주세요.
//
// [JA] stackalloc のメモリはそのメソッドのスタック フレームの中にあります。メソッドが戻るとその場所は「空き」になり、
// [JA] 次に呼ぶメソッドがそのまま上書きします。コンパイラーが Span を止めたのには理由がありました。
// [JA] バッファは誰が作るべきでしょう? 長く生きる側(呼び出し元)が作って Span で渡してください。
#endregion
