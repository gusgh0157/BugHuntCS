using System.Diagnostics;

namespace BugHunt;

// 매칭을 찾는 동안 플레이어가 "취소"를 누르면 바로 멈추고 로비로 돌아간다.
public static class Lab25
{
    const int Regions = 20;

    static async Task SearchRegionAsync(int region, CancellationToken token = default)
    {
        await Task.Delay(50, token);   // 지역 서버에 매칭 요청
    }

    static async Task<string> FindMatchAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        for (int region = 1; region <= Regions; region++)
        {
            await SearchRegionAsync(region);
        }
        return "매칭 성공";
    }

    static async Task<string> MatchmakingScreenAsync(TimeSpan cancelAfter)
    {
        using var cancel = new CancellationTokenSource();
        cancel.CancelAfter(cancelAfter);   // 플레이어가 취소 버튼을 누른다
        try
        {
            return await FindMatchAsync(cancel.Token);
        }
        catch (OperationCanceledException)
        {
            return "취소됨";
        }
    }

    public static void Run()
    {
        var watch = Stopwatch.StartNew();
        string result = MatchmakingScreenAsync(TimeSpan.FromMilliseconds(150)).GetAwaiter().GetResult();
        watch.Stop();

        Console.WriteLine($"  결과: {result}, 걸린 시간 {watch.ElapsedMilliseconds}ms");
        Expect("매칭 결과", "취소됨", result);
        Expect("취소 후 0.5초 안에 로비로 돌아옴", true, watch.ElapsedMilliseconds < 650);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// SearchRegionAsync 안에 조건부 중단점을 거세요. 조건: region == 10  (이미 취소된 뒤입니다)
// 조사식에 token.IsCancellationRequested 와 token.CanBeCanceled 를 넣어 보세요.
#endregion

#region 힌트 2
// 같은 시점에 호출 스택에서 FindMatchAsync 프레임을 더블클릭하고, 그 프레임의 token 도 조사식에 넣어 보세요.
// 두 token 은 같은 것인가요?
#endregion

#region 힌트 3
// CancellationToken 은 호출 사슬의 모든 단계에 직접 넘겨야 합니다. 한 군데라도 빠지면 거기서 끊깁니다.
// "= default" 기본값은 편하지만, 넘기는 것을 잊어도 컴파일러가 아무 말도 하지 않게 만듭니다. (분석기 규칙 CA2016 참고)
#endregion
