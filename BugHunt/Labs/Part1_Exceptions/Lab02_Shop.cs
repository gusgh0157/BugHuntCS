namespace BugHunt;

// 세이브 파일에서 장바구니를 불러와 합계를 계산한다.
// [JA] セーブファイルからカートを読み込んで合計を計算します。
public static class Lab02
{
    static readonly Dictionary<string, int> Prices = new()
    {
        ["hp_potion"] = 50,
        ["mana_potion"] = 70,
        ["iron_sword"] = 1200,
        ["leather_boots"] = 300,
    };

    // 1.0 버전 클라이언트가 저장한 장바구니. "아이템 x 개수"가 ; 로 구분된다.
    const string SavedCart = "hp_potion x 3; Mana_Potion x 2; iron_sword x 1";

    record CartLine(string ItemId, int Count);

    static List<CartLine> LoadCart(string saved)
    {
        var lines = new List<CartLine>();
        foreach (string entry in saved.Split(';', StringSplitOptions.TrimEntries))
        {
            string[] parts = entry.Split(" x ");
            lines.Add(new CartLine(parts[0], int.Parse(parts[1])));
        }
        return lines;
    }

    static int PriceOf(string itemId) => Prices[itemId];

    static int CartTotal(List<CartLine> cart)
    {
        int total = 0;
        foreach (CartLine line in cart)
        {
            int subtotal = PriceOf(line.ItemId) * line.Count;
            Console.WriteLine($"  {line.ItemId} x {line.Count} = {subtotal}");
            total += subtotal;
        }
        return total;
    }

    public static void Run()
    {
        List<CartLine> cart = LoadCart(SavedCart);
        int total = CartTotal(cart);
        Expect("장바구니 합계 (50x3 + 70x2 + 1200)", 1490, total);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 예외 도우미의 메시지를 끝까지 읽으세요. 어떤 키를 못 찾았다고 하나요?
// 호출 스택에서 PriceOf 프레임을 고르고, 조사식에 itemId 와 Prices.Keys 를 넣어 비교해 보세요.
//
// [JA] 例外ヘルパーのメッセージを最後まで読んでください。どのキーが見つからないと言っていますか?
// [JA] 呼び出し履歴で PriceOf のフレームを選び、ウォッチに itemId と Prices.Keys を入れて比べてみてください。
#endregion

#region 힌트 2 / ヒント 2
// 키가 "있는 것처럼" 보인다면 글자 하나하나를 보세요. 대문자와 소문자는 다른 글자입니다.
// 그 키는 어디서 왔나요? 호출 스택을 한 칸씩 내려가 보세요.
//
// [JA] キーが「ある」ように見えるなら、一文字ずつ見てください。大文字と小文字は別の文字です。
// [JA] そのキーはどこから来ましたか? 呼び出し履歴を一段ずつ下りてみてください。
#endregion

#region 힌트 3 / ヒント 3
// 세이브 파일은 이미 사용자 PC에 있으니 고칠 수 없습니다. 코드 쪽에서 받아 주어야 합니다.
// Dictionary 생성자가 받을 수 있는 인자를 찾아보세요. (StringComparer)
//
// [JA] セーブファイルはすでにユーザーの PC にあるので直せません。コードの側で受け止める必要があります。
// [JA] Dictionary のコンストラクターが受け取れる引数を調べてみてください。(StringComparer)
#endregion
