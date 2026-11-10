using System.Buffers;

namespace BugHunt;

// 채팅 메시지를 "[이름] 내용" 형태로 만들어 기록에 쌓는다. 메시지마다 string 을 만들지 않으려고 ArrayPool 버퍼를 빌려 쓴다.
// [JA] チャット メッセージを「[名前] 内容」の形にして履歴に積みます。メッセージごとに string を作らないように ArrayPool のバッファを借りて使います。
public static class Lab79
{
    static ReadOnlyMemory<char> FormatMessage(string sender, string text)
    {
        char[] buffer = ArrayPool<char>.Shared.Rent(128);
        int length = 0;
        foreach (string part in new[] { "[", sender, "] ", text })
        {
            part.CopyTo(buffer.AsSpan(length));
            length += part.Length;
        }
        ArrayPool<char>.Shared.Return(buffer);   // 빌린 것은 돌려준다 / 借りたものは返す
        return buffer.AsMemory(0, length);
    }

    public static void Run()
    {
        string[] senders = ["Kael", "Mira", "Doran", "Lia", "Sven"];
        string[] texts = [Text.L("안녕", "やあ"), Text.L("보스 어디야?", "ボスどこ?"), Text.L("2시 방향", "2時の方向"), Text.L("포션 남는 사람", "ポーション余ってる人"), Text.L("ㄱㄱ", "いこいこ"), Text.L("잠깐만 기다려 줘", "ちょっと待って"), Text.L("길드전 8시", "ギルド戦は8時")];

        int count = Pick(4, 3, 8);
        var expected = new List<string>();
        var history = new List<ReadOnlyMemory<char>>();
        for (int i = 0; i < count; i++)
        {
            string sender = Seed == 0 ? senders[i] : Pick(senders[0], senders);
            string text = Seed == 0 ? texts[i] : Pick(texts[0], texts);
            history.Add(FormatMessage(sender, text));
            expected.Add($"[{sender}] {text}");
        }

        Console.WriteLine(Text.L("  채팅 기록:", "  チャット履歴:"));
        foreach (ReadOnlyMemory<char> line in history)
            Console.WriteLine($"    {line}");

        Expect(Text.L("첫 메시지", "最初のメッセージ"), expected[0], history[0].ToString());
        Expect(Text.L("전체 기록", "履歴全体"), string.Join(" / ", expected), string.Join(" / ", history.Select(m => m.ToString())));
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// history.Add 줄에 중단점을 걸고, 매번 조사식에 history[0].ToString() 을 넣어 보세요. 언제 바뀌나요?
// 조사식의 history[0] 을 우클릭 → 개체 ID 만들기. 그리고 FormatMessage 안의 buffer 도 개체 ID 를 만들어 보세요.
//
// [JA] history.Add の行にブレークポイントを置き、毎回ウォッチに history[0].ToString() を入れてみてください。いつ変わりますか?
// [JA] ウォッチの history[0] を右クリック → オブジェクト ID の作成。そして FormatMessage の中の buffer にもオブジェクト ID を作ってみてください。
#endregion

#region 힌트 2 / ヒント 2
// FormatMessage 의 return 줄에서 buffer 에 개체 ID($1)를 만들고, 다음 메시지 때 다시 멈춰서 buffer 를 보세요. 또 $1 인가요?
// 조사식에 ArrayPool<char>.Shared 를 넣고 Rent 가 돌려준 배열과 비교해 봐도 됩니다.
// ReadOnlyMemory 와 Span 은 배열의 "창문"입니다. 내용을 복사하지 않습니다.
//
// [JA] FormatMessage の return の行で buffer にオブジェクト ID($1)を作り、次のメッセージのときにまた止まって buffer を見てください。また $1 ですか?
// [JA] ウォッチに ArrayPool<char>.Shared を入れて、Rent が返した配列と比べてみてもかまいません。
// [JA] ReadOnlyMemory と Span は配列の「窓」です。内容をコピーしません。
#endregion

#region 힌트 3 / ヒント 3
// ArrayPool 에 돌려준 배열은 다음에 Rent 하는 사람에게 그대로 다시 나갑니다. (같은 스레드라면 바로 그 배열이)
// 돌려준 뒤에도 그 배열을 가리키는 창문(Memory)을 들고 있으면, 남의 데이터를 보게 됩니다.
// 기록처럼 오래 둘 것은 반납하기 전에 복사(new string(span))하거나, 다 쓸 때까지 반납하지 마세요.
//
// [JA] ArrayPool に返した配列は、次に Rent する人にそのまま再び渡されます。(同じスレッドなら、まさにその配列が)
// [JA] 返したあとも、その配列を指す窓(Memory)を持っていると、他人のデータを見ることになります。
// [JA] 履歴のように長く置くものは、返す前にコピー(new string(span))するか、使い終わるまで返さないでください。
#endregion
