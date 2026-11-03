using System.Globalization;

namespace BugHunt;

// 인벤토리를 세이브 파일에 저장한다. 저장 도중 아이템 하나가 손상되어 저장이 실패했다. 실패한 저장은 무시하고 지난번 세이브를 쓰면 되는데,
// 다시 불러오니 지난번 세이브까지 망가져 있다.
// 이 랩은 %TEMP%\BugHunt\Lab52_SafeSave 아래에만 쓰고, 끝나면 지운다.
// [JA] インベントリをセーブファイルに保存します。保存の途中でアイテムが一つ壊れていて保存に失敗しました。失敗した保存は無視して前回のセーブを使えばいいはずですが、
// [JA] ロードし直すと前回のセーブまで壊れています。
// [JA] このラボは %TEMP%\BugHunt\Lab52_SafeSave の下にだけ書き込み、終わったら削除します。
public static partial class Lab52
{
    static readonly string Root = Path.Combine(Path.GetTempPath(), "BugHunt", "Lab52_SafeSave");

    sealed record Item(string Name, int Count);

    static void Save(string path, IReadOnlyList<Item> items)
    {
        using var writer = new StreamWriter(path);
        writer.WriteLine(items.Count.ToString(CultureInfo.InvariantCulture));
        foreach (Item item in items)
        {
            if (item.Count < 0)
                throw new InvalidDataException($"손상된 아이템: {item.Name} x{item.Count}");
            writer.WriteLine($"{item.Name}|{item.Count.ToString(CultureInfo.InvariantCulture)}");
        }
    }

    // 세이브가 손상됐으면 null
    static List<Item>? Load(string path)
    {
        string[] lines = File.ReadAllLines(path);
        if (lines.Length == 0 || !int.TryParse(lines[0], NumberStyles.None, CultureInfo.InvariantCulture, out int count) || lines.Length != count + 1)
            return null;
        return [.. lines.Skip(1).Select(l => l.Split('|')).Select(p => new Item(p[0], int.Parse(p[1], CultureInfo.InvariantCulture)))];
    }

    static string Show(IEnumerable<Item>? items) => items == null ? "(손상됨)" : string.Join(", ", items.Select(i => $"{i.Name} x{i.Count}"));

    public static void Run()
    {
        Directory.CreateDirectory(Root);
        string path = Path.Combine(Root, "inventory.sav");
        try
        {
            string[] names = ["철광석", "회복 물약", "마나 물약", "용의 비늘", "화살", "빵"];
            int n = Pick(4, 3, names.Length);
            List<Item> good = [.. names.Take(n).Select((name, i) => new Item(name, Pick(5 * (i + 1), 1, 99)))];

            // 1) 정상 저장
            Save(path, good);
            Console.WriteLine($"  지난번 세이브: {Show(Load(path))}");

            // 2) 다음 저장: 아이템 하나가 손상됐다 (수량 음수)
            List<Item> broken = [.. good.Select(i => i with { Count = i.Count + 1 })];
            int bad = Pick(2, 1, n - 1);
            broken[bad] = broken[bad] with { Count = -1 };
            try
            {
                Save(path, broken);
            }
            catch (InvalidDataException ex)
            {
                Console.WriteLine($"  저장 실패: {ex.Message} → 지난번 세이브를 그대로 쓴다");
            }

            List<Item>? afterFail = Load(path);
            Console.WriteLine($"  불러오기: {Show(afterFail)}");
            Expect("저장이 실패해도 지난번 세이브는 그대로", Show(good), Show(afterFail));

            // 3) 손상을 고치고 다시 저장하면 새 내용이 저장된다
            broken[bad] = broken[bad] with { Count = 7 };
            Save(path, broken);
            Expect("고친 뒤 다시 저장", Show(broken), Show(Load(path)));
            Expect("세이브 폴더에 남은 임시 파일 없음", 0, Directory.GetFiles(Root, "*.tmp").Length);
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
// 2) 의 Save(path, broken) 에 중단점을 걸고, F10 으로 넘기기 직전과 직후에 탐색기에서 inventory.sav 를 열어 보세요(또는 조사식에 File.ReadAllText(path)).
// 저장이 "실패"했는데 파일은 바뀌었습니다. 언제 바뀌었을까요? Save 안의 첫 줄에서 F10 을 한 번 누르고 다시 파일 크기를 보세요.
//
// [JA] 2) の Save(path, broken) にブレークポイントを置き、F10 で進める直前と直後にエクスプローラーで inventory.sav を開いてみてください(またはウォッチに File.ReadAllText(path))。
// [JA] 保存は「失敗」したのにファイルは変わっています。いつ変わったのでしょう? Save の中の最初の行で F10 を一度押して、もう一度ファイルサイズを見てください。
#endregion

#region 힌트 2 / ヒント 2
// new StreamWriter(path) 는 FileMode.Create 로 엽니다. 여는 순간 기존 파일은 0바이트로 잘립니다. 지난번 세이브는 그때 이미 사라졌습니다.
// 그 뒤 예외가 나도 using 이 writer 를 닫으면서 그때까지 쓴 앞부분을 디스크에 내보냅니다. 결과는 "새 세이브의 앞부분"만 담긴 반쯤 쓴 파일입니다.
// 정전, 강제 종료, 디스크 꽉 참도 같은 결과를 냅니다. 이 랩의 예외는 그것을 재현하는 장치일 뿐입니다.
//
// [JA] new StreamWriter(path) は FileMode.Create で開きます。開いた瞬間に既存のファイルは0バイトに切り詰められます。前回のセーブはその時点ですでに消えています。
// [JA] その後に例外が出ても、using が writer を閉じるときにそれまで書いた前半をディスクに書き出します。結果は「新しいセーブの前半」だけが入った書きかけのファイルです。
// [JA] 停電、強制終了、ディスク容量不足も同じ結果になります。このラボの例外はそれを再現する仕掛けにすぎません。
#endregion

#region 힌트 3 / ヒント 3
// 원본을 건드리기 전에 다른 파일에 끝까지 쓰고, 다 쓴 뒤에 한 번에 바꿔치기하세요:
//   string tmp = path + ".tmp";
//   try { (tmp 에 저장) } catch { File.Delete(tmp); throw; }
//   if (File.Exists(path)) File.Replace(tmp, path, path + ".bak"); else File.Move(tmp, path);   (또는 File.Move(tmp, path, overwrite: true))
// 같은 볼륨 안의 File.Replace/File.Move 는 이름만 바꾸는 작업이라 중간 상태가 남지 않습니다. 실패한 저장의 임시 파일은 지우세요(세 번째 판정).
// 먼저 메모리에서 전부 검사한 뒤에 파일을 여는 수정도 이 예외는 피하지만, 쓰는 도중의 강제 종료·디스크 꽉 참에는 여전히 약합니다.
//
// [JA] 元のファイルに触れる前に別のファイルに最後まで書き、書き終えてから一度に差し替えてください:
// [JA]   string tmp = path + ".tmp";
// [JA]   try { (tmp に保存) } catch { File.Delete(tmp); throw; }
// [JA]   if (File.Exists(path)) File.Replace(tmp, path, path + ".bak"); else File.Move(tmp, path);   (または File.Move(tmp, path, overwrite: true))
// [JA] 同じボリューム内の File.Replace/File.Move は名前を変えるだけの操作なので、中途半端な状態が残りません。失敗した保存の一時ファイルは消してください(3つ目の判定)。
// [JA] 先にメモリ上ですべて検査してからファイルを開く修正もこの例外は避けられますが、書いている途中の強制終了・ディスク容量不足には依然として弱いです。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 아이템 수, 수량, 손상된 아이템의 위치를 바꿔 검사한다. 실패하면 메모리의 good 으로 다시 쓰는 식의 수정은 통과하지만 "지난번 세이브"를 들고 있지 않은 실제 게임에서는 쓸 수 없다.
// [JA] 隠しケースはアイテム数、数量、壊れたアイテムの位置を変えて検査します。失敗したらメモリ上の good で書き直す修正は通りますが、「前回のセーブ」を持っていない実際のゲームでは使えません。
#endregion
