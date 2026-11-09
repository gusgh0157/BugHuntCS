namespace BugHunt;

// 콤보 점수를 고정 크기 버퍼(4칸)에 포인터로 담는다. 콤보가 4개를 넘으면 옆 칸(가드 값)이 저절로 바뀐다.
// [JA] コンボスコアを固定サイズのバッファ(4マス)にポインターで入れます。コンボが 4 個を超えると隣のマス(ガード値)が勝手に変わります。
public static partial class Lab82
{
    const int Guard = unchecked((int)0xC0FFEE);

    // 들어온 콤보 점수를 버퍼에 담고, (담은 개수, 가드 칸의 값)을 돌려준다.
    static unsafe (int Stored, int GuardAfter) Fill(int[] incoming)
    {
        // 한 덩어리를 잡아 앞 4칸은 점수 버퍼, 바로 뒤 1칸은 가드(경계 표시)로 쓴다.
        // 가드 뒤의 여유 칸은 넘쳐 쓴 값이 이 덩어리 밖(스택의 다른 값)까지 가지 않게 하는 실습용 안전장치다.
        Span<int> block = stackalloc int[4 + 1 + 8];
        Span<int> scores = block[..4];
        block[4] = Guard;

        int stored = 0;
        fixed (int* p = scores)
        {
            for (int i = 0; i < incoming.Length; i++)
            {
                p[i] = incoming[i];
                stored++;
            }
        }

        return (stored, block[4]);
    }

    public static void Run()
    {
        int[] incoming = Pick(Feeds[0], Feeds[1..]);
        var (stored, guardAfter) = Fill(incoming);

        Console.WriteLine(Text.L($"  들어온 콤보 {incoming.Length}개, 버퍼에 담긴 수 {stored}, 가드 0x{guardAfter:X}", $"  入ってきたコンボ {incoming.Length}個、バッファに入った数 {stored}、ガード 0x{guardAfter:X}"));
        Expect(Text.L("옆 칸(가드) 값이 그대로", "隣のマス(ガード)の値がそのまま"), Guard, guardAfter);
        Expect(Text.L("버퍼 용량(4)을 넘겨 담지 않음", "バッファ容量(4)を超えて入れない"), Math.Min(incoming.Length, 4), stored);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// Fill 의 fixed 블록 안 p[i] = incoming[i]; 에 중단점을 걸고, 조사식에 i 와 incoming.Length 를 넣으세요.
// i 가 4 가 되는 순간을 보세요. scores 는 4칸(0~3)인데 p[4] 에 쓰고 있습니다. 그 직후 조사식에 block[4] 를 넣어 보세요. 가드 값이 바뀌었습니다.
//
// [JA] Fill の fixed ブロック内の p[i] = incoming[i]; にブレークポイントを置き、ウォッチに i と incoming.Length を入れてください。
// [JA] i が 4 になる瞬間を見てください。scores は 4 マス(0〜3)なのに p[4] に書いています。その直後にウォッチで block[4] を見てください。ガード値が変わっています。
#endregion

#region 힌트 2 / ヒント 2
// p 는 scores(4칸)의 첫 칸을 가리킵니다. 포인터 p[i] 는 범위 검사를 하지 않습니다. p[4] 는 버퍼 바로 뒤, 즉 가드 칸을 덮어씁니다.
// 같은 자리를 안전한 인덱서로 쓰면(scores[4] = ...) IndexOutOfRangeException 이 나서 바로 알 수 있습니다. 포인터는 조용히 옆 메모리를 망가뜨립니다.
// 실제 코드라면 그 "옆 칸"이 다른 변수, 반환 주소, 다른 객체일 수 있어, 전혀 엉뚱한 곳에서 버그가 터집니다. 이것이 버퍼 오버런입니다.
//
// [JA] p は scores(4 マス)の先頭を指します。ポインター p[i] は範囲検査をしません。p[4] はバッファのすぐ後ろ、つまりガードのマスを上書きします。
// [JA] 同じ場所を安全なインデクサーで書くと(scores[4] = ...)IndexOutOfRangeException が出てすぐ分かります。ポインターは黙って隣のメモリを壊します。
// [JA] 実際のコードならその「隣のマス」が別の変数、戻りアドレス、別のオブジェクトでありえて、まったく見当違いの所でバグが出ます。これがバッファオーバーランです。
#endregion

#region 힌트 3 / ヒント 3
// 포인터로 쓸 때는 언제나 버퍼 용량을 직접 지켜야 합니다. 들어온 개수가 아니라 버퍼 크기로 루프를 제한하세요:
//   for (int i = 0; i < incoming.Length && i < scores.Length; i++) { p[i] = incoming[i]; stored++; }
// 또는 포인터를 쓰지 말고 안전한 Span 인덱서(scores[i])를 쓰면, 넘칠 때 예외로 바로 드러납니다. 넘치는 입력은 자르거나 거절하는 정책을 명시하세요.
//
// [JA] ポインターで書くときは常にバッファの容量を自分で守らねばなりません。入ってきた個数ではなくバッファのサイズでループを制限してください:
// [JA]   for (int i = 0; i < incoming.Length && i < scores.Length; i++) { p[i] = incoming[i]; stored++; }
// [JA] またはポインターを使わず安全な Span のインデクサー(scores[i])を使えば、あふれたときに例外ですぐ分かります。あふれる入力は切り捨てるか拒否する方針を明示してください。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 용량을 넘기는 다른 콤보 수로 검사한다. 보이는 입력만 자르면 더 긴 입력에서 가드가 깨진다.
// [JA] 隠しケースは容量を超える別のコンボ数で検査します。見えている入力だけ切り捨てると、もっと長い入力でガードが壊れます。
public static partial class Lab82
{
    static readonly int[][] Feeds =
    [
        [10, 20, 30, 40, 50],
        [3, 1, 4, 1, 5, 9],
        [7, 7, 7, 7, 7],
        [2, 4, 6, 8, 10, 12, 14],
    ];
}
#endregion
