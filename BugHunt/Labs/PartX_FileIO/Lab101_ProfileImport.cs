namespace BugHunt;

// 플레이어 프로필마다 세이브 폴더를 하나씩 둔다. 옛 설치에서 가져온 프로필은 폴더 이름 대신 옛 경로를 들고 있다.
// 모든 프로필을 저장한 뒤 세이브 루트를 통째로 백업했는데, 백업에 프로필 하나가 빠져 있다.
// 이 랩은 %TEMP%\BugHunt\Lab101_ProfileImport 아래에만 쓰고, 끝나면 지운다.
// [JA] プレイヤープロフィールごとにセーブフォルダーを一つずつ置きます。旧インストールから取り込んだプロフィールはフォルダー名の代わりに旧パスを持っています。
// [JA] すべてのプロフィールを保存したあとセーブルートを丸ごとバックアップしたのに、バックアップにプロフィールが一つ欠けています。
// [JA] このラボは %TEMP%\BugHunt\Lab101_ProfileImport の下にだけ書き込み、終わったら削除します。
public static partial class Lab101
{
    static readonly string Root = Path.Combine(Path.GetTempPath(), "BugHunt", "Lab101_ProfileImport");

    sealed record Profile(string Name, string Folder);

    // 프로필의 세이브 폴더: 세이브 루트 아래 프로필 폴더
    static string SaveDir(string saveRoot, Profile profile) => Path.Combine(saveRoot, profile.Folder);

    static void Save(string saveRoot, Profile profile)
    {
        string dir = SaveDir(saveRoot, profile);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "slot1.sav"), $"{profile.Name}의 세이브");
        Console.WriteLine($"  {profile.Name} 저장 → {dir}");
    }

    // 세이브 루트 아래의 모든 .sav 를 백업 폴더로 복사
    static int Backup(string saveRoot, string backupDir)
    {
        int copied = 0;
        foreach (string file in Directory.GetFiles(saveRoot, "*.sav", SearchOption.AllDirectories))
        {
            string target = Path.Combine(backupDir, Path.GetRelativePath(saveRoot, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
            copied++;
        }
        return copied;
    }

    public static void Run()
    {
        string saveRoot = Path.Combine(Root, "saves");
        string oldInstall = Path.Combine(Root, "old-install", "saves");
        string backupDir = Path.Combine(Root, "backup");
        Directory.CreateDirectory(saveRoot);
        try
        {
            string[] names = [.. Names.Take(Pick(3, 2, Names.Length))];
            int imported = Pick(1, 0, names.Length - 1);
            Profile[] profiles = [.. names.Select((n, i) => new Profile(n, i == imported ? Path.Combine(oldInstall, n) : n))];
            Console.WriteLine($"  가져온 프로필: {profiles[imported].Name} (폴더 값: {profiles[imported].Folder})");

            foreach (Profile p in profiles)
                Save(saveRoot, p);

            int copied = Backup(saveRoot, backupDir);
            Console.WriteLine($"  백업: {copied}개 파일 (세이브 루트: {saveRoot})");

            Expect("백업된 세이브 수 = 프로필 수", profiles.Length, copied);
            Expect("가져온 프로필의 세이브가 세이브 루트 안에 있음", true, File.Exists(Path.Combine(saveRoot, profiles[imported].Name, "slot1.sav")));
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
// 콘솔의 "저장 →" 줄 세 개를 비교하세요. 하나만 세이브 루트 밖입니다.
// SaveDir 에 중단점을 걸고 조사식에 saveRoot, profile.Folder, Path.Combine(saveRoot, profile.Folder) 를 넣어 보세요. 첫 인자가 통째로 사라집니다.
//
// [JA] コンソールの「저장 →」(保存 →)の3行を比べてください。一つだけセーブルートの外です。
// [JA] SaveDir にブレークポイントを置き、ウォッチに saveRoot、profile.Folder、Path.Combine(saveRoot, profile.Folder) を入れてみてください。最初の引数が丸ごと消えます。
#endregion

#region 힌트 2 / ヒント 2
// Path.Combine 은 뒤 인자가 루트 경로(C:\..., \..., /...)이면 그 앞의 인자들을 전부 버립니다. 문서에 적힌 동작이고, 예외도 없습니다.
// 그래서 사용자 입력이나 외부 데이터를 Path.Combine 의 두 번째 인자로 넣으면, 절대 경로 하나로 세이브 루트 밖 어디에나 쓰게 됩니다(경로 탈출).
// 조사식에 Path.IsPathRooted(profile.Folder) 를 넣어 보세요.
//
// [JA] Path.Combine は後ろの引数がルートパス(C:\...、\...、/...)なら、その前の引数をすべて捨てます。ドキュメントに書かれた動作で、例外も出ません。
// [JA] そのためユーザー入力や外部データを Path.Combine の2番目の引数に入れると、絶対パス一つでセーブルートの外のどこにでも書けてしまいます(パストラバーサル)。
// [JA] ウォッチに Path.IsPathRooted(profile.Folder) を入れてみてください。
#endregion

#region 힌트 3 / ヒント 3
// 바깥에서 온 값은 "폴더 이름"으로만 받으세요: Path.GetFileName(profile.Folder) 로 마지막 조각만 쓰거나, Path.IsPathRooted / ".." 포함이면 거부하세요.
// 합친 뒤에 확인하는 방법도 있습니다: Path.GetFullPath(합친 경로)가 Path.GetFullPath(saveRoot) + 구분자로 시작하는지.
// Path.Join 은 앞 인자를 버리지는 않지만 "saves\C:\old\..." 같은 엉뚱한 경로를 만듭니다. 검증을 대신해 주지 않습니다.
// 가져온 프로필을 그냥 건너뛰는 수정은 백업 수에서 걸립니다. 그 프로필도 세이브 루트 안에 저장되어야 합니다.
//
// [JA] 外から来た値は「フォルダー名」としてだけ受け取ってください: Path.GetFileName(profile.Folder) で最後の部分だけ使うか、Path.IsPathRooted / ".." を含むなら拒否してください。
// [JA] 結合したあとに確認する方法もあります: Path.GetFullPath(結合したパス)が Path.GetFullPath(saveRoot) + 区切り文字で始まるかどうか。
// [JA] Path.Join は前の引数を捨てはしませんが "saves\C:\old\..." のようなおかしなパスを作ります。検証の代わりにはなりません。
// [JA] 取り込んだプロフィールをただ飛ばす修正はバックアップ数で引っかかります。そのプロフィールもセーブルートの中に保存されなければなりません。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 프로필 수와 어느 프로필이 옛 경로를 들고 있는지를 바꿔 검사한다.
// [JA] 隠しケースはプロフィール数と、どのプロフィールが旧パスを持っているかを変えて検査します。
public static partial class Lab101
{
    static readonly string[] Names = ["kael", "mira", "doran", "lyra", "oren"];
}
#endregion
