using System.Text;

namespace BugHunt;

// 모드 제작자가 만든 NPC 대사 파일(한 줄에 "키=대사")을 읽는다. 모드 제작자마다 다른 편집기로 저장해서 인코딩이 제각각이다.
// 어떤 모드는 첫 대사가 "없음"이 되고, 어떤 모드는 한글이 전부 깨진다.
// 이 랩은 %TEMP%\BugHunt\Lab50_DialogueMod_<프로세스 ID> 아래에만 쓰고, 끝나면 지운다.
// [JA] MOD 製作者が作った NPC のセリフファイル(1行に「キー=セリフ」)を読みます。製作者ごとに別のエディターで保存したので、エンコーディングがばらばらです。
// [JA] ある MOD は最初のセリフが「なし」になり、ある MOD はセリフがすべて文字化けします。
// [JA] このラボは %TEMP%\BugHunt\Lab50_DialogueMod_<プロセス ID> の下にだけ書き込み、終わったら削除します。
public static partial class Lab50
{
    static readonly string Root = Path.Combine(Path.GetTempPath(), "BugHunt", $"Lab50_DialogueMod_{Environment.ProcessId}");

    // 대사 파일을 읽어 키 → 대사 표를 만든다. (모드 무결성 검사 때문에 바이트로 먼저 읽는다)
    static Dictionary<string, string> LoadDialogue(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        string text = Encoding.UTF8.GetString(bytes);

        var table = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            int eq = line.IndexOf('=');
            if (eq > 0)
                table[line[..eq]] = line[(eq + 1)..].TrimEnd('\r');
        }
        return table;
    }

    static string Lookup(Dictionary<string, string> table, string key) => table.TryGetValue(key, out string? s) ? s : Text.T("(없음)");

    public static void Run()
    {
        // CP949(한국어 Windows 의 옛 "ANSI") 인코딩을 쓰려면 코드 페이지 공급자를 등록해야 한다. (모드 파일을 만드는 쪽 흉내)
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding cp949 = Encoding.GetEncoding(949);

        Directory.CreateDirectory(Root);
        try
        {
            (string Key, string Line)[] lines = Pick(Scripts[0], Scripts[1..]);
            string content = string.Join("\n", lines.Select(l => $"{l.Key}={l.Line}")) + "\n";

            // 모드 A: 메모장에서 "UTF-8 (BOM)"으로 저장
            string modA = Path.Combine(Root, "mod_a.txt");
            File.WriteAllText(modA, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            // 모드 B: 옛 한국어 Windows 메모장에서 "ANSI"로 저장
            string modB = Path.Combine(Root, "mod_b.txt");
            File.WriteAllBytes(modB, cp949.GetBytes(content));
            // 모드 C: BOM 없는 UTF-8 (요즘 편집기 기본값)
            string modC = Path.Combine(Root, "mod_c.txt");
            File.WriteAllText(modC, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            string firstKey = lines[0].Key, lastKey = lines[^1].Key;
            foreach ((string name, string path) in new[] { (Text.T("모드 A (UTF-8 BOM)"), modA), (Text.T("모드 B (CP949)"), modB), (Text.T("모드 C (UTF-8)"), modC) })
            {
                Dictionary<string, string> table = LoadDialogue(path);
                Console.WriteLine($"  {name}: {firstKey} = {Lookup(table, firstKey)} / {lastKey} = {Lookup(table, lastKey)}");
                Expect($"{name}의 첫 대사", lines[0].Line, Lookup(table, firstKey));
                Expect($"{name}의 마지막 대사", lines[^1].Line, Lookup(table, lastKey));
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
// 모드 A: LoadDialogue 의 return 에서 멈추고 table 을 펼쳐 보세요. 첫 키가 보이는데 왜 못 찾을까요? 조사식에 table.Keys.First().Length 와 firstKey.Length 를 넣어 보세요.
// 모드 B: 조사식에 bytes 를 넣고 처음 몇 바이트를 보세요(16진수 표시: 조사식 창 우클릭 → 16진수 표시). 같은 글자 "안"이 모드 C 에서는 몇 바이트인가요?
//
// [JA] MOD A: LoadDialogue の return で止めて table を展開してください。最初のキーが見えるのに、なぜ見つからないのでしょう? ウォッチに table.Keys.First().Length と firstKey.Length を入れてみてください。
// [JA] MOD B: ウォッチに bytes を入れて最初の数バイトを見てください(16進表示: ウォッチウィンドウを右クリック → 16 進数で表示)。同じ文字が MOD C では何バイトですか?
#endregion

#region 힌트 2 / ヒント 2
// 모드 A: UTF-8 BOM(EF BB BF)은 Encoding.UTF8.GetString 이 지워 주지 않습니다. 글자 U+FEFF 로 남아 첫 키 앞에 붙습니다. 텍스트 시각화 도우미로는 보이지 않는 글자입니다.
//         (File.ReadAllText 와 StreamReader 는 BOM 을 알아서 처리하지만, 바이트를 직접 디코딩하면 그렇지 않습니다.)
// 모드 B: CP949 는 한글 한 글자가 2바이트, UTF-8 은 3바이트입니다. CP949 바이트를 UTF-8 로 읽으면 잘못된 바이트열이 되어 U+FFFD(�)로 바뀝니다. 예외는 나지 않습니다.
//
// [JA] MOD A: UTF-8 の BOM(EF BB BF)は Encoding.UTF8.GetString が消してくれません。文字 U+FEFF として残り、最初のキーの前に付きます。テキストビジュアライザーでは見えない文字です。
// [JA]         (File.ReadAllText と StreamReader は BOM を自動で処理しますが、バイトを直接デコードするとそうはなりません。)
// [JA] MOD B: CP949 はハングルもかなも1文字が2バイト、UTF-8 は3バイトです。CP949 のバイトを UTF-8 として読むと不正なバイト列になり U+FFFD(�)に置き換わります。例外は出ません。
#endregion

#region 힌트 3 / ヒント 3
// 바이트를 디코딩할 때 인코딩을 판단하세요:
//   1) UTF-8 BOM 으로 시작하면 그 3바이트를 건너뛰고 UTF-8 로 (Encoding.UTF8.Preamble 과 비교)
//   2) 아니면 잘못된 바이트에서 예외를 던지는 UTF-8(new UTF8Encoding(false, throwOnInvalidBytes: true))로 시도하고
//   3) DecoderFallbackException 이면 CP949(Encoding.GetEncoding(949), 코드 페이지 공급자 등록 필요)로 읽습니다.
// "모든 파일은 UTF-8"이라고 가정하는 대신, 지원하는 인코딩을 정해 두고 판단 규칙을 코드로 쓰는 것이 핵심입니다. 키에서 '﻿' 만 지우는 수정은 모드 B 를 못 고칩니다.
// 저장하는 쪽이라면 BOM 없는 UTF-8 로 통일하세요(File.WriteAllText 의 기본값).
//
// [JA] バイトをデコードするときにエンコーディングを判定してください:
// [JA]   1) UTF-8 BOM で始まるならその3バイトを飛ばして UTF-8 で (Encoding.UTF8.Preamble と比較)
// [JA]   2) そうでなければ不正なバイトで例外を投げる UTF-8(new UTF8Encoding(false, throwOnInvalidBytes: true))で試し
// [JA]   3) DecoderFallbackException なら CP949(Encoding.GetEncoding(949)、コードページプロバイダーの登録が必要)で読みます。
// [JA] 「すべてのファイルは UTF-8」と仮定する代わりに、対応するエンコーディングを決めて判定規則をコードで書くことが核心です。キーから '﻿' だけ消す修正では MOD B が直りません。
// [JA] 保存する側なら BOM なしの UTF-8 に統一してください(File.WriteAllText の既定値)。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 다른 키와 대사로 검사한다. 보이는 대사를 박아 넣거나 첫 키 이름만 특별 처리하면 여기서 걸린다.
// [JA] 隠しケースは別のキーとセリフで検査します。見えているセリフを埋め込んだり、最初のキー名だけ特別扱いしたりするとここで引っかかります。
public static partial class Lab50
{
    static readonly (string Key, string Line)[][] Scripts =
    [
        [("greet", Text.T("어서 오게, 모험가여.")), ("quest", Text.T("북쪽 숲의 늑대를 잡아 주게.")), ("bye", Text.T("조심해서 가게."))],
        [("hello", Text.T("길드에 온 걸 환영하네.")), ("rank", Text.T("자네 등급은 아직 청동이군.")), ("farewell", Text.T("다음 의뢰도 부탁하네."))],
        [("open", Text.T("물건 보고 가세요!")), ("sale", Text.T("오늘만 물약 반값이에요.")), ("close", Text.T("또 오세요."))],
        [("intro", Text.T("이 탑은 백 층이다.")), ("warn", Text.T("위로 갈수록 몬스터가 강해진다.")), ("hint", Text.T("열쇠는 오십 층에 있다.")), ("end", Text.T("행운을 빈다."))],
    ];
}
#endregion
