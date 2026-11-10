namespace BugHunt;

// 퀘스트 중간 정산. 지금까지 끝낸 단계마다 단계 보상과 빨리 깬 보너스를 준다.
// [JA] クエストの途中精算です。これまでに終えた段階ごとに、段階報酬と早期クリアボーナスを与えます。
public static class Lab24
{
    record Stage(string Name, int Gold, int ParSeconds);

    static int TimeBonus(Stage stage, int seconds) => seconds <= stage.ParSeconds ? stage.Gold / 2 : 0;

    static int Settle(Stage[] stages, List<int> clearSeconds)
    {
        int total = 0;
        for (int i = 0; i < stages.Length; i++)
            total += stages[i].Gold + TimeBonus(stages[i], clearSeconds[i]);
        return total;
    }

    public static void Run()
    {
        int count = Pick(4, 3, 9);
        Stage[] stages = [.. Enumerable.Range(1, count).Select(i => new Stage(Text.L($"{i}단계", $"ステージ{i}"), Pick(i * 100, 10, 500), 60))];

        // 플레이어는 아직 퀘스트를 다 깨지 않았다. 지금까지 깬 단계의 클리어 시간(초)
        // [JA] プレイヤーはまだクエストを全部クリアしていません。これまでにクリアしたステージのクリア時間(秒)
        int cleared = Pick(3, 1, count - 1);
        var clearSeconds = new List<int>();
        for (int i = 0; i < cleared; i++)
            clearSeconds.Add(Pick(45 + i * 10, 30, 90));

        Console.WriteLine(Text.L($"  퀘스트 {stages.Length}단계 중 {clearSeconds.Count}단계 클리어: {string.Join(", ", clearSeconds.Select(s => $"{s}초"))}", $"  クエスト {stages.Length} 段階中 {clearSeconds.Count} 段階クリア: {string.Join(", ", clearSeconds.Select(s => $"{s}秒"))}"));
        int total = Settle(stages, clearSeconds);
        Console.WriteLine(Text.L($"  중간 정산: {total}G", $"  途中精算: {total}G"));

        int expected = 0;
        for (int i = 0; i < cleared; i++)
            expected += stages[i].Gold + (clearSeconds[i] <= 60 ? stages[i].Gold / 2 : 0);
        Expect(Text.L("중간 정산 골드", "途中精算のゴールド"), expected, total);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 예외 도우미의 메시지를 끝까지 읽으세요. "Index was out of range. Must be non-negative and less than the size of the collection."
// 호출 스택 맨 위의 내 코드 프레임에서 지역 창의 i 를 보세요. 어느 컬렉션의 몇 번째 칸을 읽으려 했나요?
//
// [JA] 例外ヘルパーのメッセージを最後まで読んでください。"Index was out of range. Must be non-negative and less than the size of the collection."
// [JA] 呼び出し履歴の一番上にある自分のコードのフレームで、ローカル ウィンドウの i を見てください。どのコレクションの何番目の要素を読もうとしましたか?
#endregion

#region 힌트 2 / ヒント 2
// 조사식에 stages.Length, clearSeconds.Count, i 를 넣어 보세요. 루프는 어느 쪽의 크기를 기준으로 돌고 있나요?
// 배열은 Length, List 는 Count. 이름이 다른 만큼 "무엇의 개수"인지도 다를 수 있습니다.
//
// [JA] ウォッチに stages.Length、clearSeconds.Count、i を入れてみてください。ループはどちらのサイズを基準に回っていますか?
// [JA] 配列は Length、List は Count。名前が違うぶん、「何の個数」なのかも違うことがあります。
#endregion

#region 힌트 3 / ヒント 3
// 두 컬렉션의 크기가 다를 수 있다면, 루프는 "실제로 읽을 컬렉션"의 크기로 돌아야 합니다.
// 다 깬 퀘스트로만 시험하면 두 크기가 같아서 이 버그는 숨어 있습니다. 경계(0개, 일부, 전부)를 모두 시험하세요.
//
// [JA] 二つのコレクションのサイズが違いうるなら、ループは「実際に読むコレクション」のサイズで回すべきです。
// [JA] 全部クリアしたクエストだけで試すと二つのサイズが同じなので、このバグは隠れたままです。境界(0 個、一部、全部)をすべて試してください。
#endregion
