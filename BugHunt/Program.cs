using System.Diagnostics;
using System.Text;

namespace BugHunt;

record LabEntry(string Title, string Symptom, Action Run);

static class Program
{
    static readonly LabEntry[] Labs =
    [
        // 1부: 예외와 스택 트레이스
        new("친구 프로필",      "세 번째 프로필에서 NullReferenceException",      Lab01.Run),
        new("상점 장바구니",    "세이브에서 불러온 장바구니 계산 중 죽는다",       Lab02.Run),
        new("죽은 적 정리",     "적이 쓰러지는 순간 컬렉션 예외가 난다",          Lab03.Run),
        new("서버 설정",        "숫자 7을 int로 꺼내는데 InvalidCastException",   Lab04.Run),
        new("골드 지갑",        "골드를 넣자마자 프로그램이 통째로 사라진다",      Lab05.Run),
        new("전리품 파일",      "고장 난 파일 하나를 건너뛰게 했는데도 죽는다",    Lab06.Run),
        new("세이브 불러오기",  "크래시 리포트가 엉뚱한 곳을 가리킨다",           Lab07.Run),
        // 2부: 흐름 추적
        new("스킬 단축키",      "단축키 1, 2, 3이 전부 같은 스킬을 쓴다",          Lab08.Run),
        new("주간 랭킹 보상",   "1~3위가 보상을 두 번씩 받는다",                  Lab09.Run),
        new("스태미나",         "달려도 스태미나가 줄지 않는다",                  Lab10.Run),
        new("설정 변경 감지",   "아무것도 안 바꿨는데 전부 바뀌었다고 한다",       Lab11.Run),
        new("경험치 바",        "바가 꽉 찼는데 레벨 업을 안 한다",               Lab12.Run),
        new("보스 기여도",      "제일 많이 때린 사람의 기여도가 음수다",           Lab13.Run),
        new("닉네임 필터",      "금칙어가 그대로 통과한다",                       Lab14.Run),
        new("설정 파일",        "독일 플레이어만 이동 속도가 10배가 된다",           Lab15.Run),
        new("이벤트 남은 시간", "남은 시간이 9시간 어긋난다",                     Lab16.Run),
        new("친구 목록",        "대소문자만 다른 이름이 친구 목록에 두 번 들어간다", Lab17.Run),
        new("캐릭터 프리셋",    "복사본에 스킬을 넣었는데 원본도 바뀐다",          Lab18.Run),
        // 3부: 비동기와 동시성
        new("자동 저장",        "try/catch로 감쌌는데 저장 실패로 죽는다",         Lab19.Run),
        new("로딩 화면",        "로딩 바가 100%에서 멈춘다",                      Lab20.Run),
        new("업적 전송",        "업적을 세 개 보냈는데 서버는 0개 받았다",         Lab21.Run),
        new("패치 다운로드",    "두 개가 실패했는데 로그에는 하나만 남는다",       Lab22.Run),
        new("파티 피해 집계",   "총 피해량이 매번 다르다",                        Lab23.Run),
        new("HUD 갱신",         "로딩 후 HUD가 엉뚱한 스레드에서 갱신된다",        Lab24.Run),
        new("매칭 취소",        "취소를 눌렀는데 매칭이 계속된다",                Lab25.Run),
    ];

    const int Part2Start = 8, Part3Start = 19, Part4Start = 26;

    static string BuildName =>
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    static void PrintMenu()
    {
        Console.WriteLine($"=== BugHunt: C# 버그 추적 실습 [{BuildName}] ===");
        for (int i = 0; i < Labs.Length; i++)
        {
            int n = i + 1;
            if (n == 1) Console.WriteLine("\n[1부] 예외와 스택 트레이스 - VS에서 F5로 실행하세요");
            else if (n == Part2Start) Console.WriteLine("\n[2부] 흐름 추적 - 크래시 없음. 중단점을 걸고 F10/F11로 따라가세요");
            else if (n == Part3Start) Console.WriteLine("\n[3부] 비동기와 동시성 - 병렬 스택/작업 창");
            else if (n == Part4Start) Console.WriteLine("\n[4부] 리소스와 메모리 (선택) - 진단 도구, 메모리 스냅샷");
            Console.WriteLine($"  {n,2}. {Labs[i].Title}  - {Labs[i].Symptom}");
        }
        Console.WriteLine("  (명령줄 인자로 번호를 넘겨도 된다: BugHunt.exe 3)");
        Console.WriteLine("  (채점: BugHunt.exe grade  또는  BugHunt.exe grade 8-18)");
    }

    static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        if (args.Length > 0 && args[0] == "grade")
            return Grade(args.Length > 1 ? args[1] : null);

        string? input;
        if (args.Length > 0)
        {
            input = args[0];
        }
        else
        {
            PrintMenu();
            Console.Write("\n실행할 랩 번호: ");
            input = Console.ReadLine();
        }

        if (!int.TryParse(input, out int choice) || choice < 1 || choice > Labs.Length)
        {
            Console.WriteLine($"1 ~ {Labs.Length} 사이의 번호를 입력하세요.");
            return 2;
        }

        LabEntry lab = Labs[choice - 1];
        Console.WriteLine($"\n--- Lab {choice}: {lab.Title} [{BuildName}] ---");
        lab.Run();
        Console.WriteLine("--- 종료 ---");
        return FailCount == 0 && PassCount > 0 ? 0 : 1;
    }

    // 각 랩을 별도 프로세스로 실행해서 판정한다. (StackOverflow와 데드락도 판정할 수 있게)
    // PASS: 종료 코드 0, [FAIL] 없음, [PASS] 1개 이상, 제한 시간 안에 끝남
    static int Grade(string? range)
    {
        int from = 1, to = Labs.Length;
        if (range != null)
        {
            string[] parts = range.Split('-');
            from = int.Parse(parts[0]);
            to = parts.Length > 1 ? int.Parse(parts[1]) : from;
        }

        // BugHunt.exe 로 실행했으면 그대로, "dotnet BugHunt.dll" 로 실행했으면 dll 경로를 앞에 붙인다.
        string exe = Environment.ProcessPath!;
        string dllArg = Path.GetFileNameWithoutExtension(exe) == "dotnet"
            ? $"\"{typeof(Program).Assembly.Location}\" "
            : "";
        int passed = 0;
        for (int n = from; n <= to; n++)
        {
            var psi = new ProcessStartInfo(exe, dllArg + n)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                UseShellExecute = false,
            };
            psi.Environment["BUGHUNT_GRADER"] = "1";
            using Process p = Process.Start(psi)!;
            Task<string> stdout = p.StandardOutput.ReadToEndAsync();
            Task<string> stderr = p.StandardError.ReadToEndAsync();

            string verdict, detail = "";
            if (!p.WaitForExit(TimeSpan.FromSeconds(30)))
            {
                p.Kill(entireProcessTree: true);
                verdict = "TIMEOUT";
            }
            else
            {
                p.WaitForExit();
                string output = stdout.Result;
                string[] lines = output.Split('\n', StringSplitOptions.TrimEntries);
                if (p.ExitCode == 0 && output.Contains("[PASS]") && !output.Contains("[FAIL]"))
                {
                    verdict = "PASS";
                }
                else if (p.ExitCode is 0 or 1)
                {
                    verdict = "FAIL";
                    detail = lines.FirstOrDefault(l => l.StartsWith("[FAIL]")) ?? "";
                }
                else
                {
                    verdict = $"CRASH (0x{p.ExitCode:X8})";
                    detail = stderr.Result.Split('\n', StringSplitOptions.TrimEntries)
                        .FirstOrDefault(l => l.Length > 0) ?? "";
                }
            }

            if (verdict == "PASS") passed++;
            Console.WriteLine($"  Lab {n,2} {verdict,-20} {Labs[n - 1].Title}  {detail}");
        }
        Console.WriteLine($"\n  {passed} / {to - from + 1} 통과");
        return passed == to - from + 1 ? 0 : 1;
    }
}
