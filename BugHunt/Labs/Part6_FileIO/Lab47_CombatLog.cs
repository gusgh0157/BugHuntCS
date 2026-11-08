namespace BugHunt;

// 전투가 끝나면 전투 기록을 파일로 저장한다. 파일 스트림은 using 으로 닫았는데, 저장된 파일의 마지막 몇 줄이 빠져 있다.
// 이 랩은 %TEMP%\BugHunt\Lab47_CombatLog_<프로세스 ID> 아래에만 쓰고, 끝나면 지운다.
// [JA] 戦闘が終わると戦闘ログをファイルに保存します。ファイルストリームは using で閉じたのに、保存されたファイルの最後の数行が欠けています。
// [JA] このラボは %TEMP%\BugHunt\Lab47_CombatLog_<プロセス ID> の下にだけ書き込み、終わったら削除します。
public static partial class Lab47
{
    static readonly string Root = Path.Combine(Path.GetTempPath(), "BugHunt", $"Lab47_CombatLog_{Environment.ProcessId}");

    static void SaveLog(string path, IReadOnlyList<string> lines)
    {
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        var writer = new StreamWriter(stream);
        foreach (string line in lines)
            writer.WriteLine(line);
        Console.WriteLine($"  전투 기록 {lines.Count}줄 저장: {path}");
    }

    public static void Run()
    {
        Directory.CreateDirectory(Root);
        string path = Path.Combine(Root, "combat.log");
        try
        {
            int count = Pick(120, 60, 400);
            string[] attackers = [Text.T("기사"), Text.T("궁수"), Text.T("마법사"), Text.T("도적")];
            var lines = new List<string>();
            for (int i = 1; i <= count; i++)
                lines.Add($"[{i:0000}] " + Text.F("{0}의 공격: 피해 {1}", attackers[i % attackers.Length], Pick(17 * i % 100, 1, 999)));
            // 줄 길이 합이 우연히 버퍼 크기의 배수가 되지 않게 한 줄 더
            if (lines.Sum(l => l.Length + 2) % 1024 == 0)
                lines.Add(Text.T("[끝] 전투 종료"));

            SaveLog(path, lines);
            string[] read = File.ReadAllLines(path);
            Console.WriteLine($"  파일에서 읽은 줄 수: {read.Length}");

            Expect("저장된 줄 수", lines.Count, read.Length);
            Expect("마지막 줄", lines[^1], read.Length > 0 ? read[^1] : "");
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
// SaveLog 의 마지막 줄(Console.WriteLine)에 중단점을 걸고 조사식에 넣어 보세요:
//   stream.Position              (파일에 실제로 나간 바이트)
//   writer.BaseStream.Position   (같은 값)
//   lines.Sum(l => l.Length + 2) (써야 할 글자 수. 한글은 UTF-8 로 3바이트라 정확히 같지는 않음)
// 아직 나가지 않은 데이터는 어디에 있을까요? writer 를 펼쳐 [Raw View] 의 _charPos 를 보세요.
//
// [JA] SaveLog の最後の行(Console.WriteLine)にブレークポイントを置き、ウォッチに入れてみてください:
// [JA]   stream.Position              (ファイルに実際に出たバイト数)
// [JA]   writer.BaseStream.Position   (同じ値)
// [JA]   lines.Sum(l => l.Length + 2) (書くべき文字数。日本語・韓国語は UTF-8 で3バイトなので正確には一致しない)
// [JA] まだ出ていないデータはどこにあるのでしょう? writer を展開して [Raw View] の _charPos を見てください。
#endregion

#region 힌트 2 / ヒント 2
// StreamWriter 는 글자를 자기 버퍼(기본 1024자)에 모았다가 꽉 차면 스트림으로 보냅니다. 마지막에 남은 부분은 Flush 나 Dispose 때 나갑니다.
// 이 코드는 stream 만 using 으로 닫습니다. 안쪽 스트림을 닫아도 바깥 writer 의 버퍼는 비워지지 않습니다. 그 글자들은 메서드가 끝나며 그냥 사라집니다.
// 랩 72(리플레이)은 writer 를 아예 안 닫아 파일이 비고 핸들이 남는 경우였고, 이번에는 "안쪽만 닫아서" 꼬리가 잘리는 경우입니다.
//
// [JA] StreamWriter は文字を自分のバッファー(既定 1024 文字)にためて、いっぱいになるとストリームに送ります。最後に残った分は Flush か Dispose のときに出ます。
// [JA] このコードは stream だけを using で閉じています。内側のストリームを閉じても外側の writer のバッファーは空になりません。その文字はメソッドが終わると共にただ消えます。
// [JA] ラボ 72(リプレイ)は writer をまったく閉じずファイルが空になりハンドルが残るケースで、今回は「内側だけ閉じて」末尾が切れるケースです。
#endregion

#region 힌트 3 / ヒント 3
// 가장 바깥 래퍼를 using 으로 닫으세요. StreamWriter 를 Dispose 하면 자기 버퍼를 비우고 안쪽 스트림까지 닫습니다:
//   using var writer = new StreamWriter(path);                       (FileStream 을 직접 만들 필요가 없다)
//   using var stream = ...; using var writer = new StreamWriter(stream);   (선언 역순으로 닫히므로 writer 가 먼저 닫힌다)
// 스트림을 계속 써야 해서 writer 만 닫고 싶으면 new StreamWriter(stream, leaveOpen: true) 입니다.
// 래퍼가 겹칠 때(GZipStream, CryptoStream, BinaryWriter...)마다 같은 규칙: 바깥부터 닫는다.
//
// [JA] 一番外側のラッパーを using で閉じてください。StreamWriter を Dispose すると自分のバッファーを空にして内側のストリームまで閉じます:
// [JA]   using var writer = new StreamWriter(path);                       (FileStream を直接作る必要はない)
// [JA]   using var stream = ...; using var writer = new StreamWriter(stream);   (宣言と逆順に閉じられるので writer が先に閉じる)
// [JA] ストリームを使い続けたくて writer だけ閉じたいなら new StreamWriter(stream, leaveOpen: true) です。
// [JA] ラッパーが重なるとき(GZipStream, CryptoStream, BinaryWriter...)も同じ規則: 外側から閉じる。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 줄 수와 내용을 바꿔 검사한다. 마지막 줄을 따로 한 번 더 쓰거나 읽은 쪽에서 줄 수를 맞추는 수정은 여기서 걸린다.
// [JA] 隠しケースは行数と内容を変えて検査します。最後の行を別に書き足したり、読む側で行数を合わせたりする修正はここで引っかかります。
#endregion
