namespace BugHunt;

// 채팅 로그는 게임이 켜져 있는 동안 계속 열어 두고 쓴다. 신고 버튼을 누르면 업로더가 그 로그 파일을 읽어 서버로 보낸다.
// 업로더가 "다른 프로세스가 사용 중"이라며 아무것도 못 읽는다.
// 이 랩은 %TEMP%\BugHunt\Lab100_ChatUpload 아래에만 쓰고, 끝나면 지운다.
// [JA] チャットログはゲームが起動している間ずっと開いたまま書き込みます。通報ボタンを押すとアップローダーがそのログファイルを読んでサーバーに送ります。
// [JA] アップローダーが「別のプロセスが使用中」と言って何も読めません。
// [JA] このラボは %TEMP%\BugHunt\Lab100_ChatUpload の下にだけ書き込み、終わったら削除します。
public static partial class Lab100
{
    static readonly string Root = Path.Combine(Path.GetTempPath(), "BugHunt", "Lab100_ChatUpload");

    // 게임이 켜져 있는 동안 열어 두는 로그
    sealed class ChatLogger(string path) : IDisposable
    {
        readonly StreamWriter _writer = new(path, append: true) { AutoFlush = true };

        public void Write(string sender, string message) => _writer.WriteLine($"[{sender}] {message}");

        public void Dispose() => _writer.Dispose();
    }

    // 신고: 지금까지의 로그를 읽어 서버로 보낸다.
    static string[] Upload(string path)
    {
        try
        {
            string[] lines = File.ReadAllLines(path);
            Console.WriteLine($"  업로드: {lines.Length}줄");
            return lines;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"  업로드 실패: {ex.Message}");
            return [];
        }
    }

    public static void Run()
    {
        // FileShare 는 Windows 에서만 운영 체제가 강제한다. Linux/macOS 는 공유 모드를 대부분 무시해서 판정할 수 없다.
        if (!OperatingSystem.IsWindows())
        {
            Skip("FileShare 충돌은 Windows 에서만 재현됩니다");
            return;
        }

        Directory.CreateDirectory(Root);
        string path = Path.Combine(Root, "chat.log");
        try
        {
            int before = Pick(5, 2, 30);
            int after = Pick(3, 1, 10);
            string[] senders = ["Kael", "Mira", "Doran"];

            using var logger = new ChatLogger(path);
            for (int i = 1; i <= before; i++)
                logger.Write(senders[i % senders.Length], $"메시지 {i}");

            string[] uploaded = Upload(path);

            // 신고 뒤에도 채팅은 계속된다.
            for (int i = 1; i <= after; i++)
                logger.Write(senders[i % senders.Length], $"신고 뒤 메시지 {i}");

            Expect("업로드한 줄 수", before, uploaded.Length);
            Expect("업로드한 마지막 줄", $"[{senders[before % senders.Length]}] 메시지 {before}", uploaded.Length > 0 ? uploaded[^1] : "");
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
// 예외 설정 창(Ctrl+Alt+E)에서 System.IO.IOException 을 체크하고 실행하세요. File.ReadAllLines 안에서 멈춥니다.
// 메시지의 "다른 프로세스"는 사실 이 프로세스입니다. 지금 이 파일을 열어 둔 객체가 무엇인지 호출 스택과 지역 창에서 찾아보세요.
//
// [JA] 例外設定ウィンドウ(Ctrl+Alt+E)で System.IO.IOException にチェックを入れて実行してください。File.ReadAllLines の中で止まります。
// [JA] メッセージの「別のプロセス」は実はこのプロセスです。今このファイルを開いたままにしているオブジェクトが何か、呼び出し履歴とローカルウィンドウで探してください。
#endregion

#region 힌트 2 / ヒント 2
// 파일을 열 때는 "나는 어떻게 쓸 것인가(FileAccess)"와 "남은 어떻게 쓰게 허용할 것인가(FileShare)"를 같이 정합니다. 두 핸들이 공존하려면 서로의 FileAccess 가 상대의 FileShare 에 들어 있어야 합니다.
// new StreamWriter(path, append: true) 는 FileAccess.Write + FileShare.Read 로 엽니다. 즉 "남이 읽는 것은 허용".
// File.ReadAllLines 는 FileAccess.Read + FileShare.Read 로 엽니다. 즉 "남이 쓰고 있으면 거부". 이미 쓰고 있는 로거가 있으니 여기서 막힙니다.
//
// [JA] ファイルを開くときは「自分はどう使うか(FileAccess)」と「他人にどう使わせるか(FileShare)」を一緒に決めます。二つのハンドルが共存するには、互いの FileAccess が相手の FileShare に含まれている必要があります。
// [JA] new StreamWriter(path, append: true) は FileAccess.Write + FileShare.Read で開きます。つまり「他人が読むのは許可」。
// [JA] File.ReadAllLines は FileAccess.Read + FileShare.Read で開きます。つまり「他人が書いていたら拒否」。すでに書いているロガーがいるのでここで弾かれます。
#endregion

#region 힌트 3 / ヒント 3
// 쓰는 중인 파일을 읽는 쪽은 FileShare.ReadWrite 로 열어야 합니다:
//   using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
//   using var reader = new StreamReader(stream);
// 쓰는 쪽이 버퍼에 들고 있는 내용은 읽을 수 없으니 AutoFlush 나 Flush 도 필요합니다(이 로거는 이미 AutoFlush 입니다).
// 업로드 전에 로거를 닫는 수정도 동작은 하지만, 로그를 열고 닫기를 반복하는 설계가 되고 신고 중에 온 채팅을 놓칠 수 있습니다.
// 반대 상황(내가 읽는 동안 남이 못 쓰게 막고 싶다)이면 FileShare.None 이 맞습니다. 어느 쪽이든 "의도한 공유 모드"를 명시하세요.
//
// [JA] 書き込み中のファイルを読む側は FileShare.ReadWrite で開く必要があります:
// [JA]   using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
// [JA]   using var reader = new StreamReader(stream);
// [JA] 書く側がバッファーに持っている内容は読めないので AutoFlush か Flush も必要です(このロガーはすでに AutoFlush です)。
// [JA] アップロード前にロガーを閉じる修正も動きはしますが、ログを開閉し続ける設計になり、通報中に来たチャットを取りこぼすことがあります。
// [JA] 逆の状況(自分が読んでいる間に他人に書かせたくない)なら FileShare.None が正解です。どちらにせよ「意図した共有モード」を明示してください。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 신고 전후의 줄 수를 바꿔 검사한다. 업로드 줄 수를 박아 넣거나 메모리의 목록을 돌려주는 수정은 여기서 걸린다.
// [JA] 隠しケースは通報前後の行数を変えて検査します。アップロード行数を埋め込んだり、メモリ上のリストを返したりする修正はここで引っかかります。
#endregion
