namespace BugHunt;

// 패치 파일 여러 개를 동시에 내려받는다. 실패한 파일은 전부 로그에 남겨서 다시 받게 한다.
public static class Lab65
{
    static readonly string[] Patches = ["maps.pak", "sounds.pak", "textures.pak", "locale.pak", "scripts.pak"];
    static readonly HashSet<string> BrokenOnServer = ["sounds.pak", "locale.pak"];

    static async Task<string> DownloadAsync(string file)
    {
        await Task.Delay(file.Length * 10);
        if (BrokenOnServer.Contains(file))
            throw new IOException($"{file} 다운로드 실패 (체크섬 불일치)");
        return file;
    }

    static async Task<List<string>> DownloadAllAsync()
    {
        var failed = new List<string>();
        List<Task<string>> downloads = Patches.Select(DownloadAsync).ToList();
        try
        {
            await Task.WhenAll(downloads);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [로그] {ex.Message}");
            failed.Add(ex.Message.Split(' ')[0]);
        }
        return failed;
    }

    public static void Run()
    {
        List<string> failed = DownloadAllAsync().GetAwaiter().GetResult();
        failed.Sort();
        Expect("다시 받아야 할 파일", "locale.pak, sounds.pak", string.Join(", ", failed));
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// throw 줄에 중단점을 거세요. 몇 번 멈추나요? 두 번 멈춘다면 예외는 두 개 던져진 것입니다.
// catch 블록 안에 중단점을 걸고 조사식에 ex 를 넣어 보세요. 형식은 무엇이고, 메시지는 몇 개인가요?
#endregion

#region 힌트 2
// catch 안에서 조사식에 downloads 를 넣고 각 항목의 Status 와 Exception 을 펼쳐 보세요.
// Task.WhenAll 이 만든 Task 자체의 Exception 속성에는 무엇이 들어 있을까요?
#endregion

#region 힌트 3
// await 는 실패한 Task 의 AggregateException 을 풀어서 "첫 번째 예외 하나만" 다시 던집니다.
// 나머지 예외는 사라진 것이 아니라 Task 안에 그대로 있습니다.
#endregion
