namespace BugHunt;

// 인벤토리를 세이브 파일에 슬롯 순서대로 쓴다. 확장 가방을 산 플레이어는 슬롯이 255개를 넘는다.
// [JA] インベントリをセーブ ファイルにスロット順に書きます。拡張バッグを買ったプレイヤーはスロットが 255 個を超えます。
public static class Lab84
{
    record Item(string Name, int Count);

    static int SaveInventory(List<Item> items, string[] saveFile)
    {
        int written = 0;
        // 옛 세이브 형식에서는 슬롯 번호를 1바이트로 썼다. 그때 짠 루프
        // [JA] 昔のセーブ形式ではスロット番号を 1 バイトで書いていました。そのときに書いたループ
        for (byte slot = 0; slot < items.Count; slot++)
        {
            saveFile[slot] = $"{slot}:{items[slot].Name}x{items[slot].Count}";
            written++;
        }
        return written;
    }

    public static void Run()
    {
        int slots = Pick(300, 256, 700);
        var items = new List<Item>();
        for (int i = 0; i < slots; i++)
            items.Add(new Item($"item{i}", i % 99 + 1));
        Console.WriteLine(Text.L($"  인벤토리 슬롯 {items.Count}개 저장 시작", $"  インベントリ スロット {items.Count} 個の保存を開始"));

        var saveFile = new string[items.Count];
        int written = 0;
        if (!FinishesWithin(Text.L("인벤토리 저장", "インベントリの保存"), () => written = SaveInventory(items, saveFile)))
            return;

        Console.WriteLine(Text.L($"  저장 끝: {written}슬롯, 마지막 슬롯 \"{saveFile[^1]}\"", $"  保存終了: {written} スロット、最後のスロット \"{saveFile[^1]}\""));
        Expect(Text.L("저장한 슬롯 수", "保存したスロット数"), items.Count, written);
        Expect(Text.L("마지막 슬롯", "最後のスロット"), $"{items.Count - 1}:{items[^1].Name}x{items[^1].Count}", saveFile[^1]);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 멈추면 모두 중단 → 스레드 창에서 "랩 본문" 스레드로. 지역 창의 slot 과 written 을 보세요.
// F5 → 모두 중단을 몇 번 반복해 보세요. written 은 계속 커지는데 slot 은 어떤가요?
//
// [JA] 止まったら すべて中断 → スレッド ウィンドウで「ラボ本体」スレッドへ。ローカル ウィンドウの slot と written を見てください。
// [JA] F5 → すべて中断 を何回か繰り返してみてください。written はどんどん増えますが、slot はどうですか?
#endregion

#region 힌트 2 / ヒント 2
// for 줄에 조건부 중단점: slot == 255. 멈추면 F10 으로 한 바퀴 돌려 보세요. slot++ 다음 slot 은 몇인가요?
// 조사식에 (byte)(255 + 1) 과 byte.MaxValue 를 넣어 보세요.
//
// [JA] for の行に条件付きブレークポイント: slot == 255。止まったら F10 で一周回してみてください。slot++ のあとの slot はいくつですか?
// [JA] ウォッチに (byte)(255 + 1) と byte.MaxValue を入れてみてください。
#endregion

#region 힌트 3 / ヒント 3
// byte 는 255 다음에 0 이 됩니다(unchecked). items.Count 가 256 이상이면 slot < items.Count 는 영원히 참입니다.
// 루프 변수는 비교 대상과 같은 형식(int)으로. 저장 형식이 정말 1바이트라면 그건 쓸 때 따로 검사해야 합니다.
// (uint 로 거꾸로 세는 for (uint i = n - 1; i >= 0; i--) 도 같은 이유로 끝나지 않습니다)
//
// [JA] byte は 255 の次に 0 になります(unchecked)。items.Count が 256 以上なら slot < items.Count は永遠に真です。
// [JA] ループ変数は比較対象と同じ型(int)に。保存形式が本当に 1 バイトなら、それは書くときに別途チェックすべきです。
// [JA] (uint で逆向きに数える for (uint i = n - 1; i >= 0; i--) も同じ理由で終わりません)
#endregion
