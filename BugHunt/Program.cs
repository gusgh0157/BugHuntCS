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
        new("피해 라벨",        "피해 라벨을 만드는 순간 프로그램이 사라진다",         Lab08.Run, Hidden: 3),
        // 2부: 흐름 추적
        new("스킬 단축키",      "단축키 1, 2, 3이 전부 같은 스킬을 쓴다",          Lab09.Run),
        new("주간 랭킹 보상",   "1~3위가 보상을 두 번씩 받는다",                  Lab10.Run),
        new("스태미나",         "달려도 스태미나가 줄지 않는다",                  Lab11.Run),
        new("설정 변경 감지",   "아무것도 안 바꿨는데 전부 바뀌었다고 한다",       Lab12.Run),
        new("경험치 바",        "바가 꽉 찼는데 레벨 업을 안 한다",               Lab13.Run),
        new("보스 기여도",      "제일 많이 때린 사람의 기여도가 음수다",           Lab14.Run),
        new("닉네임 필터",      "금칙어가 그대로 통과한다",                       Lab15.Run),
        new("설정 파일",        "독일 플레이어만 이동 속도가 10배가 된다",           Lab16.Run),
        new("이벤트 남은 시간", "남은 시간이 9시간 어긋난다",                     Lab17.Run),
        new("친구 목록",        "대소문자만 다른 이름이 친구 목록에 두 번 들어간다", Lab18.Run),
        new("캐릭터 프리셋",    "복사본에 스킬을 넣었는데 원본도 바뀐다",          Lab19.Run),
        new("콤보",           "몇 번을 맞춰도 콤보가 0이다",                         Lab20.Run, Hidden: 3),
        new("길드원 번호",    "새 길드원에게 보낸 환영 인사가 다른 사람에게 간다",   Lab21.Run, Hidden: 3),
        new("물약 단축키",    "물약이 다 떨어졌는데 계속 마실 수 있다",              Lab22.Run, Hidden: 3),
        new("아이콘 캐시",    "아이콘 캐시를 켰는데 디스크 읽기가 그대로다",         Lab23.Run, Hidden: 3),
        // 3부: 컨테이너와 컬렉션
        new("퀘스트 정산",      "퀘스트를 다 깨기 전에 정산하면 죽는다",            Lab24.Run, Hidden: 3),
        new("광역 공격",        "광역 공격을 맞아도 파티 HP가 그대로다",            Lab25.Run, Hidden: 3),
        new("길드 창고",        "길드 태그를 바꾸자 창고가 사라졌다",               Lab26.Run, Hidden: 3),
        new("처치 기록",        "처치 기록 수가 매번 모자라고 가끔 예외가 난다",     Lab27.Run, Hidden: 2),
        new("시즌 리더보드",    "점수가 같으면 먼저 달성한 사람이 밀려난다",         Lab28.Run, Hidden: 3),
        new("드롭 테이블",      "고장 난 파일을 건너뛰게 했는데 다른 곳에서 죽는다", Lab29.Run, Hidden: 3),
        new("레이드 보상",      "레이드에 없던 사람이 보상을 받는다",               Lab30.Run, Hidden: 3),
        // 4부: C# 언어 함정
        new("즐겨찾기 스킬",    "재접속하면 즐겨찾기 별표가 전부 사라진다",            Lab31.Run, Hidden: 3),
        new("콤보 보너스",      "콤보가 없는 공격은 피해가 0으로 나온다",              Lab32.Run, Hidden: 3),
        new("드롭 등급",        "모르는 등급의 아이템이 일반 등급처럼 쏟아진다",       Lab33.Run, Hidden: 3),
        new("턴 순서",          "모든 턴을 첫 번째 유닛만 가져간다",                   Lab34.Run, Hidden: 3),
        new("자동 저장 잠금",   "자동 저장이 도는 동안 업적 집계가 멈춘다",            Lab35.Run, Hidden: 3),
        new("패치 다운로드",    "받은 조각 번호가 뒤죽박죽이고 범위 밖까지 섞인다",    Lab36.Run, Hidden: 3),
        new("캐시 상점",        "합계가 조금씩 어긋나고 세금 반올림이 내려간다",       Lab37.Run, Hidden: 3),
        // 5부: 인터페이스와 상속
        new("스킬 피해",        "불 스킬을 써도 화상 보너스가 안 붙는다",             Lab38.Run, Hidden: 3),
        new("드롭 개수",        "보스를 잡아도 아이템이 하나만 떨어진다",             Lab39.Run, Hidden: 3),
        new("퀘스트 보상",      "이벤트 배율이 정산에서는 빠진다",                    Lab40.Run, Hidden: 3),
        new("버프 지속 시간",   "턴을 넘겨도 버프가 사라지지 않는다",                 Lab41.Run, Hidden: 3),
        new("유닛 소환",        "기사가 갑옷 보정 없는 체력으로 소환된다",            Lab42.Run, Hidden: 3),
        new("즐겨찾기",         "가방에는 있다는 아이템이 즐겨찾기에서는 없다",       Lab43.Run, Hidden: 3),
        new("처치 보상",        "보스를 잡았는데 일반 적만큼만 준다",                 Lab44.Run, Hidden: 3),
        new("시작 가방",        "물약을 넣는 순간 ArrayTypeMismatchException",        Lab45.Run, Hidden: 3),
        // 6부: 파일 입출력
        new("세이브 폴더",      "폴더를 한 번 열고 나면 세이브가 없다고 한다",         Lab46.Run, Hidden: 3),
        new("전투 기록 파일",   "저장한 전투 기록의 마지막 몇 줄이 빠져 있다",         Lab47.Run, Hidden: 3),
        new("채팅 신고",        "신고하면 채팅 로그를 '사용 중'이라 못 읽는다",        Lab48.Run, Hidden: 3),
        new("프로필 가져오기",  "가져온 프로필의 세이브만 백업에서 빠진다",            Lab49.Run, Hidden: 3),
        new("NPC 대사 모드",    "모드에 따라 첫 대사가 없거나 한글이 깨진다",          Lab50.Run, Hidden: 3),
        new("차단 목록",        "메모장으로 고친 뒤 아무도 차단되지 않는다",            Lab51.Run, Hidden: 3),
        new("안전한 저장",      "저장이 실패하자 지난번 세이브까지 망가졌다",          Lab52.Run, Hidden: 3),
        new("클라우드 동기화",  "서버에는 늘 한 번 전의 설정이 올라간다",              Lab53.Run, Hidden: 3),
        // 7부: 직렬화 (JSON/XML)
        new("길드 공지",        "예외도 없는데 공지가 전부 빈 칸이다",                 Lab54.Run, Hidden: 3),
        new("캐릭터 세이브",    "불러오면 골드는 0, 레벨은 1이 된다",                  Lab55.Run, Hidden: 3),
        new("우편 보상",        "첨부한 보상이 전부 '알 수 없는 보상'이 된다",         Lab56.Run, Hidden: 3),
        new("파티 스냅샷",      "복원 뒤 리더가 맞아도 파티 창 체력은 그대로다",       Lab57.Run, Hidden: 3),
        new("보상 패킷",        "이상한 패킷을 거절하지 않고 x0 보상이 들어온다",      Lab58.Run, Hidden: 3),
        new("옵션 XML",         "그래픽 옵션을 추가하자 저장하는 순간 죽는다",         Lab59.Run, Hidden: 3),
        new("퀘스트 XML",       "XML에 스키마를 붙인 뒤로 보상이 전부 0G다",            Lab60.Run, Hidden: 3),
        new("길드 초대장",      "어떤 길드의 초대장은 보내도 도착하지 않는다",         Lab61.Run, Hidden: 3),
        // 8부: 비동기와 동시성
        new("자동 저장",        "try/catch로 감쌌는데 저장 실패로 죽는다",         Lab62.Run),
        new("로딩 화면",        "로딩 바가 100%에서 멈춘다",                      Lab63.Run),
        new("업적 전송",        "업적을 세 개 보냈는데 서버는 0개 받았다",         Lab64.Run),
        new("패치 다운로드",    "두 개가 실패했는데 로그에는 하나만 남는다",       Lab65.Run),
        new("파티 피해 집계",   "총 피해량이 매번 다르다",                        Lab66.Run),
        new("HUD 갱신",         "로딩 후 HUD가 엉뚱한 스레드에서 갱신된다",        Lab67.Run),
        new("매칭 취소",        "취소를 눌렀는데 매칭이 계속된다",                Lab68.Run),
        new("개인 창고",      "창고에 맡긴 아이템이 다시 접속하면 사라진다",     Lab69.Run, Hidden: 3),
        new("던전 입장",      "취소를 누르지 않았는데 '취소됨'이 뜬다",          Lab70.Run, Hidden: 3),
        new("퀘스트 동기화",  "오류 보고서가 늘 재시도 함수만 가리킨다",          Lab71.Run, Hidden: 3),
        // 9부: 리소스와 메모리
        new("리플레이 저장",    "저장한 리플레이 파일이 비어 있다",               Lab72.Run),
        new("전투 기록 창",     "닫은 창들이 메모리에서 사라지지 않는다",          Lab73.Run),
        new("텍스처 캐시",      "레벨을 나갔는데 텍스처 메모리가 그대로다",        Lab74.Run),
        // 10부: unsafe와 네이티브 메모리
        new("미니맵 경로",      "경로를 구했는데 미니맵에는 엉뚱한 타일이 찍힌다",  Lab75.Run, Hidden: 3, Release: true),
        new("효과음 믹서",      "레벨을 넘어가면 효과음 볼륨이 엉망이 된다",       Lab76.Run, Hidden: 3, Release: true),
        new("패킷 수신",        "손상된 패킷을 버렸는데 메모리가 안 줄어든다",      Lab77.Run, Hidden: 3),
        new("박자 콜백",        "곡을 고르면 게임이 통째로 사라진다",              Lab78.Run, Hidden: 2, Release: true),
        new("채팅 기록",        "채팅 기록이 전부 마지막 메시지로 바뀐다",          Lab79.Run, Hidden: 3),
        new("패킷 헤더",        "서버가 보낸 채팅이 하나도 안 보인다",              Lab80.Run, Hidden: 3),
        new("패킷 버퍼",        "큰 패킷이 오면 프로그램이 통째로 사라진다",            Lab81.Run, Hidden: 3),
        new("콤보 버퍼",        "콤보가 넘치면 옆 칸 값이 저절로 바뀐다",              Lab82.Run, Hidden: 3),
        // 11부: 멈춤 - 무한루프와 데드락
        new("보스 등장 연출",   "보스가 나오면 카메라 줌이 끝나지 않는다",          Lab83.Run, Hidden: 3),
        new("인벤토리 저장",    "확장 가방을 산 플레이어만 저장하다 멈춘다",        Lab84.Run, Hidden: 3),
        new("길드 동맹",        "동맹 가입 뒤 맹주 찾기가 끝나지 않는다",           Lab85.Run, Hidden: 3),
        new("동시 거래",        "두 사람이 서로 골드를 보내면 둘 다 멈춘다",         Lab86.Run, Hidden: 2),
        new("패치 슬롯",        "몇 개 실패하더니 다운로드가 통째로 멈춘다",         Lab87.Run, Hidden: 3),
        new("로딩 대기",        "Release 출시판에서만 로딩 화면이 끝나지 않는다",   Lab88.Run, Hidden: 1, Release: true),
        // 12부: 성능 - 틀리지는 않는데 느리다
        new("위협도 표시",      "전투가 길어지면 프레임이 툭툭 끊긴다",               Lab89.Run, Hidden: 2),
        new("채팅 내보내기",    "기록이 길면 내보내기가 한참 걸린다",                 Lab90.Run, Hidden: 2),
        new("전투 통계",        "통계를 켜면 GC 횟수가 확 늘어난다",                  Lab91.Run, Hidden: 2),
        new("전리품 도감",      "큰 레이드가 끝나면 몇 초씩 멈춘다",                  Lab92.Run, Hidden: 2),
        new("이름 검사",        "채팅이 몰리면 서버 CPU가 치솟는다",                  Lab93.Run, Hidden: 2),
        new("디버그 로그",      "로그를 꺼 뒀는데도 이동 처리 때 GC가 돈다",          Lab94.Run, Hidden: 2),
        new("주사위 명령",      "이상한 명령을 도배하면 서버가 느려진다",             Lab95.Run, Hidden: 2),
    ];

    // 챌린지: 힌트 없음. 기본 랩 번호와 따로 센다. C-n = 깊은 추적, B-n = 블라인드
    record ChallengeEntry(string Code, string Symptom, Action Run, int Hidden, bool Release = false)
    {
        // --list 와 채점 보고서의 id. 정수를 유지하려고 C-n은 100+n, B-n은 200+n
        public int Id => (Code[0] == 'C' ? 100 : 200) + int.Parse(Code[2..]);
        public string Title => $"챌린지 {Code}";
    }

    static readonly ChallengeEntry[] Challenges =
    [
        new("C-1", "레이드 보상이 가방에 여러 번 들어온다",                 ChallengeC1.Run, Hidden: 4),
        new("C-2", "로그인하자마자 잡은 몬스터가 엉뚱한 업적에 올라간다",   ChallengeC2.Run, Hidden: 4),
        new("C-3", "친구 랭킹을 본 뒤에 주간 보상 우편이 엉뚱하게 나간다",  ChallengeC3.Run, Hidden: 4),
        new("B-1", "지역을 불러오다가 멈춘다",                              ChallengeB1.Run, Hidden: 2),
        new("B-2", "터키 서버에서만 일부 채팅 명령이 안 먹는다",            ChallengeB2.Run, Hidden: 3),
        new("B-3", "맵이 커질수록 길찾기가 급격히 느려진다",                ChallengeB3.Run, Hidden: 2),
        new("B-4", "전투가 끝나고 부대를 채우면 죽는다",                    ChallengeB4.Run, Hidden: 3),
        new("B-5", "자동 저장이 처음 몇 번만 되고 멈춘다",                  ChallengeB5.Run, Hidden: 2),
        new("B-6", "하드 모드인데 시작 골드가 이상하다",                    ChallengeB6.Run, Hidden: 2),
        new("B-7", "보상 우편이 '발송 완료' 뒤에 도착한다",                 ChallengeB7.Run, Hidden: 2),
        new("B-8", "출시판에서만 텍스처 미리보기가 가끔 깨진다",            ChallengeB8.Run, Hidden: 3, Release: true),
    ];

    const int ChallengePart = 0;   // --list 의 part 값. 번호 붙은 부가 아니라는 뜻. 채점기는 "챌린지"로 표시한다

    const int Part2Start = 9, Part3Start = 24, Part4Start = 31, Part5Start = 38, Part6Start = 46, Part7Start = 54, Part8Start = 62, Part9Start = 72, Part10Start = 75, Part11Start = 83, Part12Start = 89;

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
            else if (n == Part4Start) Console.WriteLine("\n[4부] C# 언어 함정 - 식을 쪼개 조사식에 하나씩 넣어 보세요");
            else if (n == Part5Start) Console.WriteLine("\n[5부] 인터페이스와 상속 - 조사식의 실제 형식, 가상 호출 따라가기");
            else if (n == Part6Start) Console.WriteLine("\n[6부] 파일 입출력 - 경로, 공유 모드, 인코딩, 반쯤 쓴 파일");
            else if (n == Part7Start) Console.WriteLine("\n[7부] 직렬화 (JSON/XML) - 저장한 것과 불러온 것을 나란히 비교하세요");
            else if (n == Part8Start) Console.WriteLine("\n[8부] 비동기와 동시성 - 병렬 스택/작업 창");
            else if (n == Part9Start) Console.WriteLine("\n[9부] 리소스와 메모리 (선택) - 진단 도구, 메모리 스냅샷");
            else if (n == Part10Start) Console.WriteLine("\n[10부] unsafe와 네이티브 메모리 (선택) - 메모리 창, 조사식의 포인터 식");
            else if (n == Part11Start) Console.WriteLine("\n[11부] 멈춤: 무한루프와 데드락 - 모두 중단, 병렬 스택, 스레드 창, dotnet-stack");
            else if (n == Part12Start) Console.WriteLine("\n[12부] 성능 - 틀리지는 않는데 느리다. 성능 프로파일러, dotnet-counters, dotnet-trace");
            Console.WriteLine($"  {n,2}. {Labs[i].Title}  - {Labs[i].Symptom}");
        }
        Console.WriteLine("\n[챌린지] 힌트 없음. 1~12부를 먼저 끝내세요. C: 여러 파일을 거치는 깊은 추적, B: 범주를 알려 주지 않는 블라인드");
        foreach (ChallengeEntry c in Challenges)
            Console.WriteLine($"  {c.Code}. {c.Symptom}");
        Console.WriteLine("  (명령줄 인자로 번호를 넘겨도 된다: BugHunt.exe 3, BugHunt.exe C-1)");
        Console.WriteLine("  (채점: 저장소 폴더에서 grade.bat  또는  grade.bat 24-30,  grade.bat C)");
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

        // 숨은 케이스 재현: BugHunt.exe 84 --seed 1234
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

        input = input?.Trim().ToUpperInvariant();
        ChallengeEntry? challenge = Challenges.FirstOrDefault(c => c.Code == input || c.Id.ToString() == input);
        string heading;
        Action run;
        if (challenge != null)
        {
            heading = challenge.Title;
            run = challenge.Run;
        }
        else if (int.TryParse(input, out int choice) && choice >= 1 && choice <= Labs.Length)
        {
            heading = $"Lab {choice}: {Labs[choice - 1].Title}";
            run = Labs[choice - 1].Run;
        }
        else
        {
            Console.WriteLine($"1 ~ {Labs.Length} 사이의 번호나 챌린지 코드(C-1, B-1 ...)를 입력하세요.");
            return 2;
        }

        string hiddenCase = Seed != 0 ? $" (숨은 케이스 seed {Seed})" : "";
        Console.WriteLine($"\n--- {heading} [{BuildName}]{hiddenCase} ---");
        run();
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
        }).Concat(Challenges.Select(c => new { c.Id, c.Title, Part = ChallengePart, c.Hidden, c.Release }));
        Console.WriteLine(JsonSerializer.Serialize(labs, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        return 0;
    }

    static int PartOf(int n) =>
        n >= Part12Start ? 12 : n >= Part11Start ? 11 : n >= Part10Start ? 10 : n >= Part9Start ? 9 : n >= Part8Start ? 8 : n >= Part7Start ? 7 : n >= Part6Start ? 6 : n >= Part5Start ? 5 : n >= Part4Start ? 4 : n >= Part3Start ? 3 : n >= Part2Start ? 2 : 1;
}
