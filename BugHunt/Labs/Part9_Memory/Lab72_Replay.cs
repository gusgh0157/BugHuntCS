namespace BugHunt;

// 경기가 끝나면 리플레이(프레임별 입력)를 파일로 저장하고, 리플레이 뷰어가 그 파일을 읽는다.
public static class Lab72
{
    static readonly string ReplayPath = Path.Combine(Path.GetTempPath(), "BugHunt", $"replay_{Environment.ProcessId}.txt");

    static void SaveReplay(string path, IEnumerable<string> frames)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var writer = new StreamWriter(path);
        foreach (string frame in frames)
            writer.WriteLine(frame);
        Console.WriteLine($"  리플레이 저장: {path}");
    }

    // 리플레이 뷰어. 게임이 파일을 쓰는 중이어도 읽을 수 있게 FileShare.ReadWrite 로 연다.
    static int CountFrames(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        int count = 0;
        while (reader.ReadLine() != null)
            count++;
        return count;
    }

    public static void Run()
    {
        string[] frames = Enumerable.Range(1, 60)
            .Select(i => $"frame {i:000}: move={(i % 4) switch { 0 => "L", 1 => "R", 2 => "J", _ => "-" }}")
            .ToArray();

        SaveReplay(ReplayPath, frames);
        int read = CountFrames(ReplayPath);
        Console.WriteLine($"  뷰어가 읽은 프레임: {read}");

        Expect("리플레이 프레임 수", frames.Length, read);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// SaveReplay 의 마지막 줄에 중단점을 걸고 조사식에 아래를 넣어 보세요.
//   new FileInfo(path).Length     (디스크에 실제로 쓰인 바이트)
//   writer.BaseStream.Position    (writer 가 썼다고 생각하는 바이트)
// 둘이 같나요? 탐색기에서 그 파일을 열어 보는 것도 방법입니다.
#endregion

#region 힌트 2
// StreamWriter 와 FileStream 은 쓰기 성능을 위해 데이터를 메모리 버퍼에 모아 두었다가 한꺼번에 씁니다.
// 버퍼는 언제 디스크로 나갈까요? writer 변수는 메서드가 끝나면 어떻게 되나요?
#endregion

#region 힌트 3
// 이 파일을 다른 프로그램(예: 메모장 저장)으로 덮어쓰려고 하면 "다른 프로세스가 사용 중"이라고 나옵니다.
// 핸들이 프로세스가 끝날 때까지 열려 있기 때문입니다. 진단 도구 창에서는 이런 것이 잘 보이지 않습니다.
// 분석기 규칙 CA2000(범위를 벗어나기 전에 개체를 삭제하십시오)을 .editorconfig 에서 켜 보세요.
#endregion
