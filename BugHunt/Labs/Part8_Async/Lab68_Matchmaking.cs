using System.Diagnostics;

namespace BugHunt;

// 매칭을 찾는 동안 플레이어가 "취소"를 누르면 바로 멈추고 로비로 돌아간다.
// [JA] マッチングを探している間にプレイヤーが「キャンセル」を押したら、すぐに止まってロビーに戻ります。
public static class Lab68
{
    const int Regions = 20;

    // 지역 서버에 보낸 매칭 요청 수와, 플레이어가 취소를 누른 순간의 요청 수
    static int requestsSent;
    static int requestsAtCancel;

    static async Task SearchRegionAsync(int region, CancellationToken token = default)
    {
        Interlocked.Increment(ref requestsSent);
        await Task.Delay(50, token);   // 지역 서버에 매칭 요청
    }

    static async Task<string> FindMatchAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        for (int region = 1; region <= Regions; region++)
        {
            await SearchRegionAsync(region);
        }
        return Text.L("매칭 성공", "マッチング成功");
    }

    static async Task<string> MatchmakingScreenAsync(TimeSpan cancelAfter)
    {
        using var cancel = new CancellationTokenSource();
        cancel.CancelAfter(cancelAfter);   // 플레이어가 취소 버튼을 누른다
        cancel.Token.Register(() => Volatile.Write(ref requestsAtCancel, Volatile.Read(ref requestsSent)));
        try
        {
            return await FindMatchAsync(cancel.Token);
        }
        catch (OperationCanceledException)
        {
            return Text.L("취소됨", "キャンセル済み");
        }
    }

    public static void Run()
    {
        requestsSent = 0;
        requestsAtCancel = -1;
        var watch = Stopwatch.StartNew();
        string result = MatchmakingScreenAsync(TimeSpan.FromMilliseconds(150)).GetAwaiter().GetResult();
        watch.Stop();

        int afterCancel = requestsAtCancel < 0 ? 0 : requestsSent - requestsAtCancel;
        Console.WriteLine(Text.L($"  결과: {result}, 걸린 시간 {watch.ElapsedMilliseconds}ms, 매칭 요청 {requestsSent}번 (취소 뒤 {afterCancel}번)", $"  結果: {result}、所要時間 {watch.ElapsedMilliseconds}ms、マッチング要求 {requestsSent} 回 (キャンセル後 {afterCancel} 回)"));
        Expect(Text.L("매칭 결과", "マッチング結果"), Text.L("취소됨", "キャンセル済み"), result);
        // 시간이 아니라 요청 수로 본다: PC가 바빠 느려져도 판정이 같다. 취소와 거의 동시에 나간 요청 하나는 봐준다
        ExpectAtMost(Text.L("취소 뒤 새로 보낸 매칭 요청 수", "キャンセル後に新しく送ったマッチング要求の数"), 1, afterCancel);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// SearchRegionAsync 안에 조건부 중단점을 거세요. 조건: region == 10  (이미 취소된 뒤입니다)
// 조사식에 token.IsCancellationRequested 와 token.CanBeCanceled 를 넣어 보세요.
//
// [JA] SearchRegionAsync の中に条件付きブレークポイントを置いてください。条件: region == 10  (すでにキャンセルされたあとです)
// [JA] ウォッチに token.IsCancellationRequested と token.CanBeCanceled を入れてみてください。
#endregion

#region 힌트 2 / ヒント 2
// 같은 시점에 호출 스택에서 FindMatchAsync 프레임을 더블클릭하고, 그 프레임의 token 도 조사식에 넣어 보세요.
// 두 token 은 같은 것인가요?
//
// [JA] 同じ時点で呼び出し履歴の FindMatchAsync フレームをダブルクリックし、そのフレームの token もウォッチに入れてみてください。
// [JA] 二つの token は同じものですか?
#endregion

#region 힌트 3 / ヒント 3
// CancellationToken 은 호출 사슬의 모든 단계에 직접 넘겨야 합니다. 한 군데라도 빠지면 거기서 끊깁니다.
// "= default" 기본값은 편하지만, 넘기는 것을 잊어도 컴파일러가 아무 말도 하지 않게 만듭니다. (분석기 규칙 CA2016 참고)
//
// [JA] CancellationToken は呼び出しの連鎖のすべての段階に直接渡さなければなりません。一か所でも抜けると、そこで途切れます。
// [JA] "= default" の既定値は便利ですが、渡し忘れてもコンパイラが何も言わないようにしてしまいます。(アナライザー規則 CA2016 を参照)
#endregion
