using System.Text;
using System.Text.Json;

namespace BugHunt;

// Hidden: 채점기가 돌릴 숨은 케이스(무작위 시드) 수. Release: Release 빌드에서도 판정해야 하는 랩
record LabEntry(string Title, string Symptom, Action Run, int Hidden = 0, bool Release = false);

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
        // 3부: 컨테이너와 컬렉션
        new("퀘스트 정산",      "퀘스트를 다 깨기 전에 정산하면 죽는다",            Lab19.Run, Hidden: 3),
        new("광역 공격",        "광역 공격을 맞아도 파티 HP가 그대로다",            Lab20.Run, Hidden: 3),
        new("길드 창고",        "길드 태그를 바꾸자 창고가 사라졌다",               Lab21.Run, Hidden: 3),
        new("처치 기록",        "처치 기록 수가 매번 모자라고 가끔 예외가 난다",     Lab22.Run, Hidden: 2),
        new("시즌 리더보드",    "점수가 같으면 먼저 달성한 사람이 밀려난다",         Lab23.Run, Hidden: 3),
        new("드롭 테이블",      "고장 난 파일을 건너뛰게 했는데 다른 곳에서 죽는다", Lab24.Run, Hidden: 3),
        new("레이드 보상",      "레이드에 없던 사람이 보상을 받는다",               Lab25.Run, Hidden: 3),
        // 4부: 비동기와 동시성
        new("자동 저장",        "try/catch로 감쌌는데 저장 실패로 죽는다",         Lab26.Run),
        new("로딩 화면",        "로딩 바가 100%에서 멈춘다",                      Lab27.Run),
        new("업적 전송",        "업적을 세 개 보냈는데 서버는 0개 받았다",         Lab28.Run),
        new("패치 다운로드",    "두 개가 실패했는데 로그에는 하나만 남는다",       Lab29.Run),
        new("파티 피해 집계",   "총 피해량이 매번 다르다",                        Lab30.Run),
        new("HUD 갱신",         "로딩 후 HUD가 엉뚱한 스레드에서 갱신된다",        Lab31.Run),
        new("매칭 취소",        "취소를 눌렀는데 매칭이 계속된다",                Lab32.Run),
        // 5부: 리소스와 메모리
        new("리플레이 저장",    "저장한 리플레이 파일이 비어 있다",               Lab33.Run),
        new("전투 기록 창",     "닫은 창들이 메모리에서 사라지지 않는다",          Lab34.Run),
        new("텍스처 캐시",      "레벨을 나갔는데 텍스처 메모리가 그대로다",        Lab35.Run),
        // 6부: unsafe와 네이티브 메모리
        new("미니맵 경로",      "경로를 구했는데 미니맵에는 엉뚱한 타일이 찍힌다",  Lab36.Run, Hidden: 3, Release: true),
        new("효과음 믹서",      "레벨을 넘어가면 효과음 볼륨이 엉망이 된다",       Lab37.Run, Hidden: 3, Release: true),
        new("패킷 수신",        "손상된 패킷을 버렸는데 메모리가 안 줄어든다",      Lab38.Run, Hidden: 3),
        new("박자 콜백",        "곡을 고르면 게임이 통째로 사라진다",              Lab39.Run, Hidden: 2, Release: true),
        new("채팅 기록",        "채팅 기록이 전부 마지막 메시지로 바뀐다",          Lab40.Run, Hidden: 3),
        new("패킷 헤더",        "서버가 보낸 채팅이 하나도 안 보인다",              Lab41.Run, Hidden: 3),
        // 7부: 멈춤 - 무한루프와 데드락
        new("보스 등장 연출",   "보스가 나오면 카메라 줌이 끝나지 않는다",          Lab42.Run, Hidden: 3),
        new("인벤토리 저장",    "확장 가방을 산 플레이어만 저장하다 멈춘다",        Lab43.Run, Hidden: 3),
        new("길드 동맹",        "동맹 가입 뒤 맹주 찾기가 끝나지 않는다",           Lab44.Run, Hidden: 3),
        new("동시 거래",        "두 사람이 서로 골드를 보내면 둘 다 멈춘다",         Lab45.Run, Hidden: 2),
        new("패치 슬롯",        "몇 개 실패하더니 다운로드가 통째로 멈춘다",         Lab46.Run, Hidden: 3),
        new("로딩 대기",        "Release 출시판에서만 로딩 화면이 끝나지 않는다",   Lab47.Run, Hidden: 1, Release: true),
        // 8부: 성능 - 틀리지는 않는데 느리다
        new("위협도 표시",      "전투가 길어지면 프레임이 툭툭 끊긴다",               Lab48.Run, Hidden: 2),
        new("채팅 내보내기",    "기록이 길면 내보내기가 한참 걸린다",                 Lab49.Run, Hidden: 2),
        new("전투 통계",        "통계를 켜면 GC 횟수가 확 늘어난다",                  Lab50.Run, Hidden: 2),
        new("전리품 도감",      "큰 레이드가 끝나면 몇 초씩 멈춘다",                  Lab51.Run, Hidden: 2),
        new("이름 검사",        "채팅이 몰리면 서버 CPU가 치솟는다",                  Lab52.Run, Hidden: 2),
        new("디버그 로그",      "로그를 꺼 뒀는데도 이동 처리 때 GC가 돈다",          Lab53.Run, Hidden: 2),
        new("주사위 명령",      "이상한 명령을 도배하면 서버가 느려진다",             Lab54.Run, Hidden: 2),
    ];

    const int Part2Start = 8, Part3Start = 19, Part4Start = 26, Part5Start = 33, Part6Start = 36, Part7Start = 42, Part8Start = 48;

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
            else if (n == Part3Start) Console.WriteLine("\n[3부] 컨테이너와 컬렉션 - 예외 도우미, 조사식, 개체 ID");
            else if (n == Part4Start) Console.WriteLine("\n[4부] 비동기와 동시성 - 병렬 스택/작업 창");
            else if (n == Part5Start) Console.WriteLine("\n[5부] 리소스와 메모리 (선택) - 진단 도구, 메모리 스냅샷");
            else if (n == Part6Start) Console.WriteLine("\n[6부] unsafe와 네이티브 메모리 (선택) - 메모리 창, 조사식의 포인터 식");
            else if (n == Part7Start) Console.WriteLine("\n[7부] 멈춤: 무한루프와 데드락 - 모두 중단, 병렬 스택, 스레드 창, dotnet-stack");
            else if (n == Part8Start) Console.WriteLine("\n[8부] 성능 - 틀리지는 않는데 느리다. 성능 프로파일러, dotnet-counters, dotnet-trace");
            Console.WriteLine($"  {n,2}. {Labs[i].Title}  - {Labs[i].Symptom}");
        }
        Console.WriteLine("  (명령줄 인자로 번호를 넘겨도 된다: BugHunt.exe 3)");
        Console.WriteLine("  (채점: 저장소 폴더에서 grade.bat  또는  grade.bat 19-25)");
    }

    static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        if (args.Length > 0 && args[0] == "--list")
            return ListLabs();
        if (args.Length > 0 && args[0] == "grade")
        {
            Console.WriteLine("채점기는 별도 프로젝트로 옮겼습니다: grade.bat 또는 dotnet run --project BugHunt.Grader (README의 \"채점기\" 참고)");
            return 2;
        }

        // 숨은 케이스 재현: BugHunt.exe 36 --seed 1234
        int seedAt = Array.IndexOf(args, "--seed");
        if (seedAt >= 0 && seedAt + 1 < args.Length && int.TryParse(args[seedAt + 1], out int seed))
        {
            Seed = seed;
            args = [.. args[..seedAt], .. args[(seedAt + 2)..]];
        }

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
        string hiddenCase = Seed != 0 ? $" (숨은 케이스 seed {Seed})" : "";
        Console.WriteLine($"\n--- Lab {choice}: {lab.Title} [{BuildName}]{hiddenCase} ---");
        lab.Run();
        Console.WriteLine("--- 종료 ---");
        return FailCount == 0 && PassCount > 0 ? 0 : 1;
    }

    // 채점기(BugHunt.Grader)가 랩 목록을 읽어 가는 출력
    static int ListLabs()
    {
        var labs = Labs.Select((lab, i) => new
        {
            Id = i + 1,
            lab.Title,
            Part = PartOf(i + 1),
            lab.Hidden,
            lab.Release,
        });
        Console.WriteLine(JsonSerializer.Serialize(labs, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        return 0;
    }

    static int PartOf(int n) =>
        n >= Part8Start ? 8 : n >= Part7Start ? 7 : n >= Part6Start ? 6 : n >= Part5Start ? 5 : n >= Part4Start ? 4 : n >= Part3Start ? 3 : n >= Part2Start ? 2 : 1;
}
