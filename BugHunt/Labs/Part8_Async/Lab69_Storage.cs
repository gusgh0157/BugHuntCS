namespace BugHunt;

// 가방의 아이템을 개인 창고에 맡긴다. 창고 서버에 올린 것만 가방에서 빼고, 못 맡긴 것은 알려 준다.
// 접속을 끊었다가 다시 들어오면 가방과 창고를 서버에서 다시 읽는다.
// [JA] バッグのアイテムを個人倉庫に預けます。倉庫サーバーにアップロードしたものだけバッグから取り除き、預けられなかったものは知らせます。
// [JA] 接続を切ってから入り直すと、バッグと倉庫をサーバーから読み直します。
public static class Lab69
{
    class StorageServer(HashSet<string> boundItems)
    {
        public Dictionary<string, int> Stored { get; } = [];
        public Dictionary<string, int> SavedBag { get; private set; } = [];

        public async Task UploadAsync(string item, int count)
        {
            await Task.Delay(1);   // 네트워크
            if (boundItems.Contains(item))
                throw new InvalidOperationException(Text.L($"{item}: 귀속 아이템은 창고에 넣을 수 없습니다", $"{item}: 帰属アイテムは倉庫に入れられません"));
            Stored[item] = Stored.GetValueOrDefault(item) + count;
        }

        public async Task SaveBagAsync(Dictionary<string, int> bag)
        {
            await Task.Delay(1);
            SavedBag = new Dictionary<string, int>(bag);
        }
    }

    static async Task<bool> UploadToStorageAsync(StorageServer server, string item, int count)
    {
        try
        {
            await server.UploadAsync(item, count);
        }
        catch (Exception ex)
        {
            Console.WriteLine(Text.L($"  [로그] 창고 업로드 경고: {ex.Message}", $"  [ログ] 倉庫アップロード警告: {ex.Message}"));
        }
        return true;
    }

    // 1단계: 맡기기. 맡겼다고 판단한 아이템만 가방에서 뺀다.
    static async Task<List<string>> DepositAllAsync(StorageServer server, Dictionary<string, int> bag, string[] items)
    {
        var notStored = new List<string>();
        foreach (string item in items)
        {
            bool stored = await UploadToStorageAsync(server, item, bag[item]);
            if (stored)
            {
                bag.Remove(item);
                Console.WriteLine(Text.L($"  {item} 보관 완료", $"  {item} 保管完了"));
            }
            else
            {
                notStored.Add(item);
                Console.WriteLine(Text.L($"  {item} 보관 실패 - 가방에 그대로 둡니다", $"  {item} 保管失敗 - バッグにそのまま残します"));
            }
        }
        return notStored;
    }

    static int Total(Dictionary<string, int> a, Dictionary<string, int> b) => a.Values.Sum() + b.Values.Sum();

    public static void Run()
    {
        string[] items = [Text.L("철광석", "鉄鉱石"), Text.L("회복 물약", "回復ポーション"), Text.L("영웅의 증표", "英雄の証"), Text.L("용의 비늘", "竜のうろこ")];
        string bound = Pick(Text.L("영웅의 증표", "英雄の証"), Text.L("철광석", "鉄鉱石"), Text.L("회복 물약", "回復ポーション"), Text.L("용의 비늘", "竜のうろこ"));
        var bag = new Dictionary<string, int>();
        foreach (string item in items)
            bag[item] = Pick(3, 1, 20);
        var server = new StorageServer([bound]);
        int before = bag.Values.Sum();

        List<string> notStored = DepositAllAsync(server, bag, items).GetAwaiter().GetResult();

        // 2단계: 접속 종료. 가방을 서버에 저장한다.
        server.SaveBagAsync(bag).GetAwaiter().GetResult();
        Console.WriteLine(Text.L("  (접속 종료 ... 다시 접속)", "  (切断 ... 再接続)"));

        // 3단계: 다시 접속. 서버에 있는 것이 전부다.
        Dictionary<string, int> bagAfter = server.SavedBag;
        Console.WriteLine(Text.L($"  가방: {string.Join(", ", bagAfter.Select(p => $"{p.Key} {p.Value}"))}", $"  バッグ: {string.Join(", ", bagAfter.Select(p => $"{p.Key} {p.Value}"))}"));
        Console.WriteLine(Text.L($"  창고: {string.Join(", ", server.Stored.Select(p => $"{p.Key} {p.Value}"))}", $"  倉庫: {string.Join(", ", server.Stored.Select(p => $"{p.Key} {p.Value}"))}"));

        Expect(Text.L("다시 접속한 뒤 가방 + 창고의 아이템 수", "再接続後のバッグ + 倉庫のアイテム数"), before, Total(bagAfter, server.Stored));
        Expect(Text.L("못 맡겼다고 알려 준 아이템", "預けられなかったと知らせたアイテム"), bound, string.Join(", ", notStored));
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 아이템이 사라진 것은 3단계(다시 접속)에서 보이지만, 사라진 순간은 그보다 앞입니다.
// 콘솔의 "[로그]" 줄을 보세요. 경고가 찍힌 아이템에 대해 바로 다음 줄은 무엇이라고 하나요?
//
// [JA] アイテムが消えたことは 3 段階目(再接続)で見えますが、消えた瞬間はそれより前です。
// [JA] コンソールの「[ログ]」の行を見てください。警告が出力されたアイテムについて、すぐ次の行は何と言っていますか?
#endregion

#region 힌트 2 / ヒント 2
// 예외 설정 창(Ctrl+Alt+E)에서 Common Language Runtime Exceptions 를 전부 체크하고 다시 실행하세요.
// "처음 던져진(first chance)" 예외에서 멈춥니다. 그 예외는 그 뒤에 어디로 가나요? F10 으로 따라가 보세요.
//
// [JA] 例外設定ウィンドウ(Ctrl+Alt+E)で Common Language Runtime Exceptions をすべてチェックして、もう一度実行してください。
// [JA] 「最初にスローされた(first chance)」例外で止まります。その例外はそのあとどこへ行きますか? F10 でたどってみてください。
#endregion

#region 힌트 3 / ヒント 3
// catch (Exception) 는 "무엇이 실패했든 성공한 것으로 친다"는 뜻이 되기 쉽습니다.
// 잡을 예외는 내가 처리 방법을 아는 것으로 좁히고, 실패했다는 사실은 호출자에게 돌려주세요.
// 로그 한 줄은 처리가 아닙니다.
//
// [JA] catch (Exception) は「何が失敗しても成功したことにする」という意味になりがちです。
// [JA] 捕まえる例外は自分が処理方法を知っているものに絞り、失敗したという事実は呼び出し元に返してください。
// [JA] ログ一行は処理ではありません。
#endregion
