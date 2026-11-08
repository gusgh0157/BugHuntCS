namespace BugHunt;

// 몬스터 드롭 테이블 파일을 읽는다. 고장 난 파일은 건너뛰고 기본 테이블을 쓴다.
// [JA] モンスターのドロップテーブルファイルを読みます。壊れたファイルは飛ばして既定のテーブルを使います。
public static class Lab29
{
    record Drop(string Item, int Weight);

    static IEnumerable<Drop> ParseDropTable(string text)
    {
        foreach (string line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = line.Split('=');
            if (parts.Length != 2 || !int.TryParse(parts[1], out int weight))
                throw new FormatException(Text.F("드롭 테이블 줄이 이상하다: \"{0}\"", line));
            yield return new Drop(parts[0].Trim(), weight);
        }
    }

    static readonly Drop[] DefaultTable = [new("gold", 1)];

    static IEnumerable<Drop> LoadDropTable(string text)
    {
        try
        {
            return ParseDropTable(text);
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"  고장 난 드롭 테이블, 기본 테이블 사용: {ex.Message}");
            return DefaultTable;
        }
    }

    public static void Run()
    {
        string[] good = ["potion=30\nsword=5\ngem=1", "arrow=50\nbow=3", "herb=40\nscroll=10\nring=2"];
        string broken = Pick("potion=30\nsword=?\ngem=1", "bow=three", "arrow==50", "herb=40\nscroll");
        string[] files = Seed == 0 ? [good[0], broken, good[1]] : [.. good.Take(Pick(0, 0, 2)), broken, .. good.Skip(1)];

        var tables = new List<IEnumerable<Drop>>();
        foreach (string file in files)
            tables.Add(LoadDropTable(file));
        Console.WriteLine($"  드롭 테이블 {tables.Count}개 읽음");

        // 몬스터를 잡을 때마다 테이블을 본다.
        int totalWeight = 0;
        foreach (IEnumerable<Drop> table in tables)
            totalWeight += table.Sum(d => d.Weight);
        Console.WriteLine($"  전체 가중치 합: {totalWeight}");

        int expected = files.Sum(f => f == broken ? 1 : f.Split('\n').Sum(l => int.Parse(l.Split('=')[1])));
        Expect("전체 가중치 합 (고장 난 파일은 기본 테이블 1)", expected, totalWeight);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 콘솔에 "고장 난 드롭 테이블"이 찍혔나요? 예외는 LoadDropTable 의 try 안이 아니라 어디서 났나요? 호출 스택을 보세요.
// 호출 스택에 <ParseDropTable>d__1.MoveNext() 와 Sum 이 보입니다. LoadDropTable 은 어디에 있나요?
//
// [JA] コンソールに「壊れたドロップテーブル」と出力されましたか? 例外は LoadDropTable の try の中ではなく、どこで発生しましたか? 呼び出し履歴を見てください。
// [JA] 呼び出し履歴に <ParseDropTable>d__1.MoveNext() と Sum が見えます。LoadDropTable はどこにありますか?
#endregion

#region 힌트 2 / ヒント 2
// LoadDropTable 의 return ParseDropTable(text); 줄에서 F11 로 들어가 보세요. ParseDropTable 의 첫 줄로 들어가나요?
// yield return 이 있는 메서드는 부르는 순간 아무것도 실행하지 않고 "열거하면 실행될 객체"만 돌려줍니다.
//
// [JA] LoadDropTable の return ParseDropTable(text); の行で F11 を押して中に入ってみてください。ParseDropTable の最初の行に入りますか?
// [JA] yield return があるメソッドは、呼んだ瞬間には何も実行せず、「列挙すると実行されるオブジェクト」だけを返します。
#endregion

#region 힌트 3 / ヒント 3
// 예외도 부수 효과(로그, 파일 읽기)도 열거하는 순간, 열거하는 곳에서 일어납니다. 그래서 try/catch 가 이미 끝난 뒤에 터집니다.
// 잡고 싶은 곳에서 끝까지 열거하세요(ParseDropTable(text).ToList()). 인자 검사처럼 즉시 실패해야 하는 것은
// yield 메서드 밖의 일반 메서드에서 먼저 하고, 열거는 안쪽 지역 함수에 맡기는 것이 관례입니다.
//
// [JA] 例外も副作用(ログ、ファイル読み込み)も、列挙する瞬間に列挙する場所で起こります。だから try/catch がすでに終わったあとで発生します。
// [JA] 捕まえたい場所で最後まで列挙してください(ParseDropTable(text).ToList())。引数チェックのようにすぐ失敗すべきものは、
// [JA] yield メソッドの外の通常のメソッドで先に行い、列挙は内側のローカル関数に任せるのが慣例です。
#endregion
