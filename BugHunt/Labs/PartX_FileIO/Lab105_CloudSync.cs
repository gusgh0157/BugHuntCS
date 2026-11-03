using System.Globalization;

namespace BugHunt;

// 옵션 창에서 설정을 바꾸면 설정 파일에 저장하고, 곧바로 클라우드 동기화가 그 파일을 읽어 서버에 올린다.
// 서버에는 늘 "한 번 전의" 설정이 올라간다.
// 이 랩은 %TEMP%\BugHunt\Lab105_CloudSync 아래에만 쓰고, 끝나면 지운다.
// [JA] オプション画面で設定を変えると設定ファイルに保存し、すぐにクラウド同期がそのファイルを読んでサーバーにアップロードします。
// [JA] サーバーにはいつも「一つ前の」設定がアップロードされます。
// [JA] このラボは %TEMP%\BugHunt\Lab105_CloudSync の下にだけ書き込み、終わったら削除します。
public static partial class Lab105
{
    static readonly string Root = Path.Combine(Path.GetTempPath(), "BugHunt", "Lab105_CloudSync");

    // 저장 대기열: 저장은 한 번에 하나씩, 들어온 순서대로. 게임을 끌 때 마지막 저장까지 기다린다.
    static Task _saveQueue = Task.CompletedTask;

    static string Format(int volume, int sensitivity) =>
        $"volume={volume.ToString(CultureInfo.InvariantCulture)}\nsensitivity={sensitivity.ToString(CultureInfo.InvariantCulture)}\n";

    static string OneLine(string s) => s.Replace('\n', ' ').Trim();

    // 설정 저장. 앞의 저장이 끝난 뒤에 시작하고, 압축과 체크섬 계산 때문에 시간이 조금 걸린다.
    static async Task SaveSettingsAsync(Task previous, string path, int volume, int sensitivity)
    {
        await previous;
        await Task.Delay(200);   // 압축·체크섬
        await File.WriteAllTextAsync(path, Format(volume, sensitivity));
    }

    // 옵션 창의 "적용" 버튼
    static async Task<string> ApplyAsync(string path, int volume, int sensitivity)
    {
        Task save = _saveQueue = SaveSettingsAsync(_saveQueue, path, volume, sensitivity);

        // 클라우드 동기화: 저장된 설정 파일을 그대로 올린다
        string uploaded = await File.ReadAllTextAsync(path);
        Console.WriteLine($"  클라우드에 올림: {OneLine(uploaded)}");
        return uploaded;
    }

    public static void Run()
    {
        Directory.CreateDirectory(Root);
        string path = Path.Combine(Root, "settings.ini");
        try
        {
            // 지난번에 저장해 둔 설정
            File.WriteAllText(path, Format(50, 5));

            (int Volume, int Sensitivity)[] changes = Pick(Changes[0], Changes[1..]);
            foreach ((int volume, int sensitivity) in changes)
            {
                Console.WriteLine($"  설정 변경: 볼륨 {volume}, 감도 {sensitivity}");
                string uploaded = ApplyAsync(path, volume, sensitivity).GetAwaiter().GetResult();
                Expect($"클라우드에 올라간 설정 (볼륨 {volume}, 감도 {sensitivity})", OneLine(Format(volume, sensitivity)), OneLine(uploaded));
            }
        }
        finally
        {
            // 게임 종료: 진행 중인 저장을 기다린다
            _saveQueue.GetAwaiter().GetResult();
            if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// File.WriteAllTextAsync 줄과 File.ReadAllTextAsync 줄에 중단점을 거세요. 어느 쪽이 먼저 멈추나요?
// 읽기에서 멈췄을 때 조사식에 save.IsCompleted 와 save.Status 를 넣어 보세요.
//
// [JA] File.WriteAllTextAsync の行と File.ReadAllTextAsync の行にブレークポイントを置いてください。どちらが先に止まりますか?
// [JA] 読み込みで止まったとき、ウォッチに save.IsCompleted と save.Status を入れてみてください。
#endregion

#region 힌트 2 / ヒント 2
// SaveSettingsAsync 를 부르면 첫 await(Task.Delay)에서 바로 Task 를 돌려받습니다. 파일 쓰기는 아직 시작도 안 했습니다.
// ApplyAsync 는 그 Task 를 저장 대기열에 넣기만 하고 기다리지 않은 채 파일을 읽습니다. 그래서 읽는 것은 이전 설정입니다. 다음 "적용" 때는 그 전 설정이 덮여 있고요.
// 경고가 없는 이유: Task 를 변수에 받으면 "await 하지 않은 호출" 경고(CS4014)가 나지 않습니다. 대기열에 넣은 것도 기다린 것이 아닙니다.
// 병렬 작업 창(디버그 → 창 → 작업)에서 읽기 시점에 SaveSettingsAsync 가 "대기 중"인 것을 볼 수 있습니다.
//
// [JA] SaveSettingsAsync を呼ぶと最初の await(Task.Delay)ですぐに Task が返ってきます。ファイルの書き込みはまだ始まってもいません。
// [JA] ApplyAsync はその Task を保存キューに入れるだけで、待たずにファイルを読みます。だから読むのは前の設定です。次の「適用」のときにはその前の設定で上書きされています。
// [JA] 警告が出ない理由: Task を変数で受け取ると「await していない呼び出し」の警告(CS4014)は出ません。キューに入れたのも待ったことにはなりません。
// [JA] 並列タスクウィンドウ(デバッグ → ウィンドウ → タスク)で、読み込み時点に SaveSettingsAsync が「待機中」なのが見えます。
#endregion

#region 힌트 3 / ヒント 3
// 파일을 읽기 전에 그 파일을 쓰는 작업을 await 하세요: await save; 그다음 읽기.
// 읽기 전에 Task.Delay 를 넣어 "기다리는" 수정은 PC 가 느리거나 저장이 오래 걸리면 다시 깨집니다. 시간으로 순서를 맞추지 말고 작업 완료로 맞추세요.
// 저장과 업로드를 같이 해야 한다면, 저장한 내용(문자열)을 업로드에 직접 넘겨 파일을 다시 읽지 않는 것도 방법입니다.
// 랩 28(업적 전송)은 기다리지 않은 작업이 "아예 안 끝나는" 경우였고, 이번에는 끝나기는 하는데 "다른 코드가 너무 일찍 그 결과(파일)를 보는" 경우입니다.
//
// [JA] ファイルを読む前に、そのファイルを書くタスクを await してください: await save; そのあとで読み込み。
// [JA] 読み込みの前に Task.Delay を入れて「待つ」修正は、PC が遅かったり保存に時間がかかったりするとまた壊れます。時間で順番を合わせず、タスクの完了で合わせてください。
// [JA] 保存とアップロードを一緒にする必要があるなら、保存した内容(文字列)をアップロードに直接渡してファイルを読み直さないのも一つの方法です。
// [JA] ラボ28(実績送信)は待たなかったタスクが「まったく終わらない」ケースで、今回は終わりはするものの「別のコードが早すぎるタイミングでその結果(ファイル)を見る」ケースです。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 다른 설정 값과 변경 횟수로 검사한다. 기대값을 박아 넣거나 업로드 문자열을 직접 만들어 돌려주면 여기서 걸린다.
// [JA] 隠しケースは別の設定値と変更回数で検査します。期待値を埋め込んだり、アップロード文字列を直接作って返したりするとここで引っかかります。
public static partial class Lab105
{
    static readonly (int, int)[][] Changes =
    [
        [(80, 7)],
        [(30, 2), (90, 9)],
        [(10, 1), (20, 2), (75, 6)],
        [(65, 4)],
    ];
}
#endregion
