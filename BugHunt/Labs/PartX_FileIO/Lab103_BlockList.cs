namespace BugHunt;

// 차단 목록 파일(한 줄에 이름 하나)을 읽어 그 플레이어의 귓속말을 막는다. 운영자가 Windows 메모장으로 고친 뒤부터 아무도 차단되지 않는다.
// 이 랩은 %TEMP%\BugHunt\Lab103_BlockList 아래에만 쓰고, 끝나면 지운다.
// [JA] ブロックリストファイル(1行に名前一つ)を読み、そのプレイヤーのささやきをブロックします。運営者が Windows のメモ帳で編集してから、誰もブロックされなくなりました。
// [JA] このラボは %TEMP%\BugHunt\Lab103_BlockList の下にだけ書き込み、終わったら削除します。
public static partial class Lab103
{
    static readonly string Root = Path.Combine(Path.GetTempPath(), "BugHunt", "Lab103_BlockList");

    static HashSet<string> LoadBlockList(string path)
    {
        string text = File.ReadAllText(path);
        return [.. text.Split('\n')];
    }

    public static void Run()
    {
        Directory.CreateDirectory(Root);
        string path = Path.Combine(Root, "blocklist.txt");
        try
        {
            string[] blocked = Pick(Lists[0], Lists[1..]);
            string target = blocked[Pick(0, 0, blocked.Length - 1)];
            foreach ((string newline, bool trailing) in Layouts)
            {
                // 운영자가 메모장(CRLF) 또는 다른 편집기(LF)로 저장한 파일
                File.WriteAllText(path, string.Join(newline, blocked) + (trailing ? newline : ""));
                string layout = $"{(newline == "\r\n" ? "CRLF" : "LF")}, 끝 줄바꿈 {(trailing ? "있음" : "없음")}";
                Console.WriteLine($"  차단 목록 파일: {new FileInfo(path).Length}바이트, {layout}");

                HashSet<string> list = LoadBlockList(path);
                Console.WriteLine($"  읽은 항목: {string.Join(", ", list.Select(n => $"\"{n}\""))}");

                Expect($"[{layout}] 차단 목록 항목 수", blocked.Length, list.Count);
                Expect($"[{layout}] {target} 의 귓속말 차단", true, list.Contains(target));
            }
        }
        finally
        {
            if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 콘솔에 찍힌 "읽은 항목"을 보세요. 따옴표 안이 이상합니다. 닫는 따옴표가 줄 맨 앞으로 갔거나, 빈 항목 "" 이 있습니다.
// LoadBlockList 의 return 에서 멈추고 text 를 텍스트 시각화 도우미(돋보기)로 보세요. 조사식에 text.Contains('\r') 도 넣어 보세요.
//
// [JA] コンソールに出た「읽은 항목」(読んだ項目)を見てください。引用符の中が変です。閉じ引用符が行頭に行っていたり、空の項目 "" があったりします。
// [JA] LoadBlockList の return で止めて text をテキストビジュアライザー(虫眼鏡)で見てください。ウォッチに text.Contains('\r') も入れてみてください。
#endregion

#region 힌트 2 / ヒント 2
// Windows 메모장은 줄바꿈을 "\r\n"(CRLF)으로 저장합니다. '\n' 으로만 자르면 각 항목 끝에 '\r' 이 남아 "Kael\r" 이 됩니다. "Kael" 과는 다른 문자열입니다.
// 그리고 파일이 줄바꿈으로 끝나면 마지막 '\n' 뒤에 빈 문자열 하나가 더 생깁니다. 빈 이름이 차단 목록에 들어갑니다.
//
// [JA] Windows のメモ帳は改行を "\r\n"(CRLF)で保存します。'\n' だけで区切ると各項目の末尾に '\r' が残り "Kael\r" になります。"Kael" とは別の文字列です。
// [JA] さらにファイルが改行で終わると、最後の '\n' の後ろに空文字列がもう一つできます。空の名前がブロックリストに入ります。
#endregion

#region 힌트 3 / ヒント 3
// 줄 단위로 읽을 때는 File.ReadAllLines / File.ReadLines / StreamReader.ReadLine 을 쓰세요. "\r\n", "\n", "\r" 을 모두 줄 끝으로 처리하고, 끝의 줄바꿈 뒤에 빈 줄을 만들지 않습니다.
// 직접 자른다면 text.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) 처럼 두 줄 끝을 모두 다루고, 빈 줄과 앞뒤 공백을 버리세요.
// TrimEnd('\r') 만 붙이는 수정은 끝의 빈 항목을 못 없앱니다. 숨은 테스트는 LF 파일, 끝에 줄바꿈이 없는 파일도 검사합니다.
// 같은 문제는 Git 의 autocrlf, 서버(Linux)와 클라이언트(Windows) 사이에 오가는 텍스트에서도 생깁니다.
//
// [JA] 行単位で読むときは File.ReadAllLines / File.ReadLines / StreamReader.ReadLine を使ってください。"\r\n"、"\n"、"\r" をすべて行末として扱い、末尾の改行の後ろに空行を作りません。
// [JA] 自分で区切るなら text.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) のように両方の行末を扱い、空行と前後の空白を捨ててください。
// [JA] TrimEnd('\r') だけ付ける修正では末尾の空の項目が消えません。隠しテストは LF のファイル、末尾に改行のないファイルも検査します。
// [JA] 同じ問題は Git の autocrlf や、サーバー(Linux)とクライアント(Windows)の間でやり取りするテキストでも起きます。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 다른 이름 목록, LF 줄바꿈, 끝에 줄바꿈이 없는 파일로 검사한다. '\r' 만 지우거나 개수를 박아 넣으면 여기서 걸린다.
// [JA] 隠しケースは別の名前リスト、LF の改行、末尾に改行のないファイルで検査します。'\r' だけ消したり件数を埋め込んだりするとここで引っかかります。
public static partial class Lab103
{
    // 기본 케이스는 메모장 파일 하나, 숨은 케이스는 LF·끝 줄바꿈 없음 파일도 함께
    static (string NewLine, bool Trailing)[] Layouts =>
        Seed == 0 ? [("\r\n", true)] : [("\r\n", true), ("\n", false), ("\r\n", false), ("\n", true)];

    static readonly string[][] Lists =
    [
        ["Kael", "Mira", "Doran"],
        ["spam_bot", "Gold4Sale", "Troll99", "Nyx"],
        ["Lyra", "Oren"],
        ["AFK_King", "Ragequit", "Botter01", "Scammer", "Flame"],
    ];
}
#endregion
