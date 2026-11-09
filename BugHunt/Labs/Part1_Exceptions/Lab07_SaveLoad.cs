namespace BugHunt;

// 세이브 슬롯 세 개를 불러온다. 손상된 슬롯은 건너뛰고, 원인 추적용 크래시 리포트를 남긴다.
// [JA] セーブスロットを三つ読み込みます。破損したスロットは飛ばし、原因追跡用のクラッシュレポートを残します。
public static class Lab07
{
    static readonly string[] Slots =
    [
        "Kael|12|sword:1,potion:5",
        "Mira|7|staff:1,potion:two",
        "Doran|15|axe:1,shield:1",
    ];

    record Item(string Id, int Count);
    record SaveData(string Name, int Level, List<Item> Items);

    static Item ParseItem(string text)
    {
        string[] parts = text.Split(':');
        return new Item(parts[0], int.Parse(parts[1]));
    }

    static List<Item> ParseInventory(string text) => text.Split(',').Select(ParseItem).ToList();

    static SaveData ParseSlot(string text)
    {
        string[] fields = text.Split('|');
        return new SaveData(fields[0], int.Parse(fields[1]), ParseInventory(fields[2]));
    }

    static SaveData LoadSlot(int index)
    {
        try
        {
            return ParseSlot(Slots[index]);
        }
        catch (Exception ex)
        {
            Console.WriteLine(Text.L($"  슬롯 {index + 1} 손상: {ex.GetType().Name}", $"  スロット {index + 1} 破損: {ex.GetType().Name}"));
            throw ex;
        }
    }

    public static void Run()
    {
        var loaded = new List<SaveData>();
        var crashReports = new List<string>();

        for (int i = 0; i < Slots.Length; i++)
        {
            try
            {
                loaded.Add(LoadSlot(i));
            }
            catch (Exception ex)
            {
                crashReports.Add(ex.ToString());
            }
        }

        foreach (SaveData save in loaded)
            Console.WriteLine(Text.L($"  Lv.{save.Level} {save.Name} - 아이템 {save.Items.Count}종", $"  Lv.{save.Level} {save.Name} - アイテム {save.Items.Count} 種類"));
        foreach (string report in crashReports)
            Console.WriteLine(Text.L($"  --- 크래시 리포트 ---\n{report}\n  -------------------", $"  --- クラッシュレポート ---\n{report}\n  -------------------"));

        Expect(Text.L("불러온 슬롯 (Kael, Doran)", "読み込んだスロット (Kael, Doran)"), 2, loaded.Count);
        Expect(Text.L("크래시 리포트가 실제로 실패한 함수(ParseItem)를 가리킨다", "クラッシュレポートが実際に失敗した関数(ParseItem)を指している"), true,
               crashReports.Count == 1 && crashReports[0].Contains(nameof(ParseItem)));
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 콘솔에 찍힌 크래시 리포트의 "at ..." 줄을 위에서부터 읽어 보세요.
// 맨 위 줄이 예외가 던져진 곳입니다. 그 줄은 슬롯 데이터를 파싱하는 코드인가요?
//
// [JA] コンソールに出力されたクラッシュレポートの "at ..." の行を上から読んでみてください。
// [JA] 一番上の行が例外がスローされた場所です。その行はスロットのデータを解析するコードですか?
#endregion

#region 힌트 2 / ヒント 2
// 예외 설정 창(Ctrl+Alt+E)에서 System.FormatException 을 체크하고 다시 실행하세요.
// 진짜로 처음 던져진 곳의 호출 스택과, 리포트에 남은 스택 트레이스를 비교해 보세요.
//
// [JA] 例外設定ウィンドウ(Ctrl+Alt+E)で System.FormatException にチェックを入れてもう一度実行してください。
// [JA] 本当に最初にスローされた場所の呼び出し履歴と、レポートに残ったスタック トレースを比べてみてください。
#endregion

#region 힌트 3 / ヒント 3
// catch 블록에서 다시 던지는 방법은 두 가지입니다. throw ex; 와 throw;
// 둘 중 하나는 스택 트레이스를 "지금 이 줄"에서 새로 시작합니다.
// 컴파일러도 알고 있었습니다. BugHunt.csproj 의 NoWarn 에 꺼 둔 경고 번호가 있습니다.
//
// [JA] catch ブロックでスローし直す方法は二つあります。throw ex; と throw;
// [JA] そのうち一つはスタック トレースを「今のこの行」から新しく始めます。
// [JA] コンパイラーも知っていました。BugHunt.csproj の NoWarn でオフにしてある警告番号があります。
#endregion
