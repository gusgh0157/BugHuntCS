using System.Globalization;

namespace BugHunt;

// 세이브는 게임 폴더 아래 saves 에 둔다. 저장 뒤 모드 폴더를 열었다가(파일 대화상자처럼 작업 디렉터리가 바뀐다) 다시 불러오면 세이브가 없다고 한다.
// 이 랩은 %TEMP%\BugHunt\Lab46_SaveFolder 아래에 가짜 게임 폴더를 만들어 쓰고, 끝나면 지운다.
// [JA] セーブはゲームフォルダー直下の saves に置きます。保存後に MOD フォルダーを開き(ファイルダイアログのように作業ディレクトリが変わります)、もう一度ロードするとセーブがないと言われます。
// [JA] このラボは %TEMP%\BugHunt\Lab46_SaveFolder の下に仮のゲームフォルダーを作って使い、終わったら削除します。
public static partial class Lab46
{
    static readonly string Root = Path.Combine(Path.GetTempPath(), "BugHunt", "Lab46_SaveFolder");

    sealed class SaveSystem(string gameDir)
    {
        // 게임이 설치된 폴더. 실제 게임에서는 AppContext.BaseDirectory 가 들어온다.
        public string GameDir { get; } = gameDir;

        string SavePath(string slot) => Path.Combine("saves", slot + ".sav");

        public void Save(string slot, int gold)
        {
            Directory.CreateDirectory("saves");
            File.WriteAllText(SavePath(slot), gold.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine($"  저장: {slot} ({gold}G) → {Path.GetFullPath(SavePath(slot))}");
        }

        public int Load(string slot) =>
            int.Parse(File.ReadAllText(SavePath(slot)), CultureInfo.InvariantCulture);
    }

    public static void Run()
    {
        string gameDir = Path.Combine(Root, "game");
        string otherDir = Path.Combine(Root, Pick("mods", "mods", "screenshots", "logs"));
        Directory.CreateDirectory(gameDir);
        Directory.CreateDirectory(otherDir);

        string slot = Pick("slot1", "slot1", "slot2", "autosave");
        int gold = Pick(1250, 1, 99_999);
        string originalCwd = Environment.CurrentDirectory;
        try
        {
            // 게임 시작: 런처가 게임 폴더에서 실행해 준다.
            Environment.CurrentDirectory = gameDir;
            var saves = new SaveSystem(gameDir);
            saves.Save(slot, gold);

            // 플레이어가 "모드 폴더 열기"를 눌렀다. 파일 대화상자가 작업 디렉터리를 바꾼다.
            Environment.CurrentDirectory = otherDir;
            Console.WriteLine($"  (폴더 열기 → 작업 디렉터리: {Environment.CurrentDirectory})");

            int loaded = saves.Load(slot);
            Console.WriteLine($"  불러오기: {slot} → {loaded}G");

            Expect("불러온 골드", gold, loaded);
        }
        finally
        {
            Environment.CurrentDirectory = originalCwd;
            if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 예외(DirectoryNotFoundException) 메시지에 나온 전체 경로를 저장할 때 출력된 경로와 비교하세요. 폴더가 다릅니다.
// Load 에서 멈추고 조사식에 Path.GetFullPath("saves") 와 Environment.CurrentDirectory 를 넣어 보세요.
//
// [JA] 例外(DirectoryNotFoundException)のメッセージに出たフルパスを、保存時に出力されたパスと比べてください。フォルダーが違います。
// [JA] Load で止めてウォッチに Path.GetFullPath("saves") と Environment.CurrentDirectory を入れてみてください。
#endregion

#region 힌트 2 / ヒント 2
// "saves/slot1.sav" 같은 상대 경로는 "지금의 작업 디렉터리(CurrentDirectory) 기준"입니다. 작업 디렉터리는 프로세스 전체의 변하는 상태입니다.
// 파일 열기 대화상자, 런처, 바로 가기의 "시작 위치", 다른 폴더에서 친 명령줄 모두 이 값을 바꿉니다. 게임 폴더와는 아무 상관이 없습니다.
// 실행 파일이 있는 폴더는 AppContext.BaseDirectory 로 얻습니다. 이 값은 바뀌지 않습니다.
//
// [JA] "saves/slot1.sav" のような相対パスは「今の作業ディレクトリ(CurrentDirectory)基準」です。作業ディレクトリはプロセス全体の、変わりうる状態です。
// [JA] ファイルを開くダイアログ、ランチャー、ショートカットの「作業フォルダー」、別のフォルダーから打ったコマンドライン、すべてがこの値を変えます。ゲームフォルダーとは何の関係もありません。
// [JA] 実行ファイルのあるフォルダーは AppContext.BaseDirectory で取得します。この値は変わりません。
#endregion

#region 힌트 3 / ヒント 3
// 파일 경로는 항상 "변하지 않는 기준 폴더"와 Path.Combine 으로 만드세요. SaveSystem 은 이미 GameDir 을 받아 두고도 쓰지 않았습니다.
//   string SavePath(string slot) => Path.Combine(GameDir, "saves", slot + ".sav");   (CreateDirectory 도 같은 기준으로)
// 실제 게임에서 세이브는 설치 폴더가 아니라 Environment.GetFolderPath(SpecialFolder.LocalApplicationData) 아래가 맞습니다(설치 폴더는 쓰기 권한이 없을 수 있음).
// 불러오기 전에 작업 디렉터리를 되돌리는 수정은 다음에 작업 디렉터리를 바꾸는 코드가 생기면 또 깨집니다.
//
// [JA] ファイルパスは常に「変わらない基準フォルダー」と Path.Combine で作ってください。SaveSystem はすでに GameDir を受け取っているのに使っていませんでした。
// [JA]   string SavePath(string slot) => Path.Combine(GameDir, "saves", slot + ".sav");   (CreateDirectory も同じ基準で)
// [JA] 実際のゲームでは、セーブはインストールフォルダーではなく Environment.GetFolderPath(SpecialFolder.LocalApplicationData) の下が正解です(インストールフォルダーには書き込み権限がないことがあります)。
// [JA] ロード前に作業ディレクトリを戻す修正は、次に作業ディレクトリを変えるコードができるとまた壊れます。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 다른 슬롯 이름, 골드, 작업 디렉터리를 옮길 폴더로 검사한다. 기대값을 박아 넣으면 여기서 걸린다.
// [JA] 隠しケースは別のスロット名、ゴールド、作業ディレクトリの移動先フォルダーで検査します。期待値を埋め込むとここで引っかかります。
#endregion
