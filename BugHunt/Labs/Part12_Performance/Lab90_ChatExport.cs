namespace BugHunt;

// 길드 채팅 기록을 텍스트로 내보낸다. 내용은 맞는데, 기록이 길면 내보내기 버튼을 누르고 한참을 기다려야 한다.
// [JA] ギルド チャットの履歴をテキストに書き出します。内容は合っていますが、履歴が長いと書き出しボタンを押してからかなり待たされます。
public static class Lab90
{
    record ChatLine(int Minute, string Sender, string Text);

    static string Export(List<ChatLine> lines)
    {
        string text = "";
        foreach (ChatLine line in lines)
            text += $"[{line.Minute / 60:00}:{line.Minute % 60:00}] {line.Sender}: {line.Text}\n";
        return text;
    }

    public static void Run()
    {
        string[] senders = ["Kael", "Mira", "Doran", "Lia"];
        string[] texts = [Text.T("ㅎㅇ"), Text.T("보스 가자"), Text.T("포션 있음?"), Text.T("2시 방향 집결"), Text.T("ㄱㄱ"), Text.T("수고하셨습니다")];
        int count = Pick(3_000, 2_000, 5_000);
        var lines = new List<ChatLine>();
        for (int i = 0; i < count; i++)
            lines.Add(new ChatLine(i / 3, senders[i % senders.Length], texts[(i * 7) % texts.Length]));

        Export(lines.GetRange(0, 10));   // 처음 한 번만 드는 비용 빼기

        string exported = "";
        (long bytes, long ms) = Measure(() => exported = Export(lines));
        long resultBytes = exported.Length * 2L;
        Console.WriteLine($"  {count}줄 → {exported.Length:N0}글자. 할당 {bytes:N0}바이트 (결과 문자열의 {bytes / resultBytes}배), {ms}ms");

        Expect("내보낸 줄 수", count, exported.Count(c => c == '\n'));
        Expect("마지막 줄", $"[{lines[^1].Minute / 60:00}:{lines[^1].Minute % 60:00}] {lines[^1].Sender}: {lines[^1].Text}", exported.TrimEnd('\n').Split('\n')[^1]);
        ExpectAtMost("할당 / 결과 문자열 크기 (배)", 8, bytes / resultBytes);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 성능 프로파일러(Alt+F2) → "CPU 사용량"과 ".NET 개체 할당 추적". 가장 많은 바이트를 차지한 형식은 무엇이고, 개수는 몇 개인가요?
// 결과 문자열은 하나인데, string 이 줄 수만큼 만들어졌다면 어디서 만들어졌을까요?
//
// [JA] パフォーマンス プロファイラー(Alt+F2) → 「CPU 使用率」と「.NET オブジェクトの割り当ての追跡」。一番多くのバイトを占めた型は何で、個数はいくつですか?
// [JA] 結果の文字列は一つなのに、string が行数分作られているなら、どこで作られたのでしょう?
#endregion

#region 힌트 2 / ヒント 2
// text += ... 줄에 중단점, 조사식에 text.Length. string 은 불변(랩 15)이라 += 는 기존 내용 전체를 복사한 새 문자열을 만듭니다.
// 줄이 n 개면 복사량은 대략 1 + 2 + ... + n, 즉 n² 에 비례합니다. 줄 수를 두 배로 하면 시간은 몇 배가 될까요?
//
// [JA] text += ... の行にブレークポイント、ウォッチに text.Length。string は不変(ラボ 15)なので、+= は既存の内容全体をコピーした新しい文字列を作ります。
// [JA] 行が n 個ならコピー量はおよそ 1 + 2 + ... + n、つまり n² に比例します。行数を 2 倍にすると時間は何倍になるでしょう?
#endregion

#region 힌트 3 / ヒント 3
// 반복해서 이어 붙일 때는 StringBuilder (또는 string.Join, string.Concat 으로 한 번에).
// sb.Append($"...") 처럼 보간 문자열을 바로 넘기면 중간 문자열도 만들지 않습니다.
//
// [JA] 繰り返しつなげるときは StringBuilder(または string.Join、string.Concat で一度に)。
// [JA] sb.Append($"...") のように補間文字列を直接渡すと、途中の文字列も作りません。
#endregion
