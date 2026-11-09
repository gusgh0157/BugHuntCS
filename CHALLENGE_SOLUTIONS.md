# BugHunt C# 챌린지 해답

> **스포일러 주의.** 챌린지는 힌트 없이 푸는 것이 규칙입니다. 채점기에서 `PASS`가 나온 뒤에 맞춰 보세요.
> 숨은 케이스에서만 실패하면(`증상은 사라졌지만 원인이 남아 있음`) 아직 원인을 고치지 않은 것입니다.

---

# 깊은 추적 (C)

## 챌린지 C-1 — 역직렬화에서 사라진 패킷 번호

**퍼지는 길:**
```
1. RewardPacket.Id 는 { get; private set; }. System.Text.Json 은 공개 setter 가 없는 속성을 역직렬화할 때 조용히 건너뛴다
2. 클라이언트가 받은 패킷은 생성자의 = Guid.NewGuid() 로 "새 번호"를 단다. 회선 위의 JSON 에는 원래 번호가 멀쩡히 있다
3. 클라이언트는 새 번호로 ack 를 보낸다 → 서버의 _unacked 에서 아무것도 지워지지 않는다 → 서버는 계속 재전송한다
4. 재전송된 패킷도 또 새 번호를 단다 → RewardInbox 의 _seen 중복 검사를 통과한다
5. RewardArrived → InventoryView 에 같은 아이템이 재전송 횟수만큼 쌓인다 (서버가 포기할 때까지)
```

**가짜 용의자:**
- **InventoryView에서 중복 제거** (`Distinct`, `Contains`면 건너뛰기): 기본 케이스는 통과합니다. 하지만 한 사람이 같은 아이템을 정말로 두 번 받는 숨은 케이스에서는 보상이 사라집니다.
- **서버의 재전송 끄기** (`MaxAttempts = 1`): 기본 케이스는 통과합니다. 하지만 패킷이 실제로 사라지는 숨은 케이스에서는 보상이 오지 않습니다.
- **수신함에서 (사람, 아이템)으로 중복 검사**: 정상적인 중복까지 버립니다.

**추적 방법:**
- 서버 `Transmit`과 수신함 `Receive`에 추적점(tracepoint)을 겁니다: `{outgoing.Packet.Id}` / `{packet.Id}`. 보낸 번호와 받은 번호가 한 번도 같지 않습니다.
- `Wire.Decode`에 `json.Contains("Yuna")` 조건부 중단점을 겁니다(아이템 이름은 JSON 에서 `\uXXXX` 로 이스케이프되므로 ASCII 인 멤버 이름으로 거릅니다). 지역 창에서 `json`의 `"id"`와 결과 `packet.Id`를 비교하면, 회선은 멀쩡하고 역직렬화에서 번호가 바뀝니다.
- 서버 패킷에 개체 ID(`$1`)를 만들어 두면, 재전송은 항상 같은 개체(`$1`)를 보냅니다. 따라서 번호는 서버에서 바뀌지 않습니다.

**수정:** 역직렬화할 수 있게 연다: `public Guid Id { get; init; } = Guid.NewGuid();` (또는 `[JsonInclude]`, 생성자 매개변수)

**교훈:** 직렬화 왕복(`Deserialize(Serialize(x))`)이 원래 값을 돌려주는지 시험을 하나 두세요. System.Text.Json은 private setter, 읽기 전용 속성, 필드를 기본으로 **조용히** 건너뜁니다. 고칠 때는 `[JsonInclude]`를 붙이거나 `init` 접근자로 바꿉니다. (`JsonUnmappedMemberHandling.Disallow`는 JSON에만 있고 형식에 없는 멤버를 막는 옵션이라, 형식에 있는데 건너뛰는 `private set`은 잡지 못합니다.) 증상이 나타난 곳(가방)과 원인(역직렬화) 사이에 ack, 재전송, 중복 검사가 끼어 있으면, 증상 근처를 막는 수정은 다른 정상 동작(정당한 중복, 진짜 재전송)을 망가뜨립니다.

**범주:** 직렬화 왕복 + 캐시·중복 검사 (7부 직렬화 + 2부 흐름 추적 + 3부 컨테이너의 중복 검사)

## 챌린지 C-2 — await 너머에서 읽은 재사용 객체

**퍼지는 길:**
```
1. QuestEventBus 는 할당을 줄이려고 QuestProgress 객체 하나(_current)를 계속 재사용한다
2. AchievementTracker.OnProgressAsync 는 진행도를 아직 못 읽었으면 await store.LoadAsync() 로 한 프레임 기다린다
3. 그 사이 같은 프레임의 다음 Publish 가 _current 의 QuestId/Amount 를 덮어쓴다
4. 다음 프레임에 이어서 실행된 처리기들이 모두 같은 _current 를 읽는다 → 마지막 알림 내용으로 여러 번 더한다
5. 잘못된 퀘스트에 진행도가 쌓이고, 엉뚱한 업적이 열린다
```

**가짜 용의자:**
- **로그인할 때 미리 불러오기** (`store.LoadAsync()` 후 한 프레임 돌리기): 기본 케이스는 통과합니다. 하지만 채널을 옮겨 캐시를 버린 직후 몬스터를 잡는 숨은 케이스에서 같은 일이 다시 납니다.
- **`_handled += 1`의 경쟁 조건**: 모든 처리가 게임 루프 한 스레드에서 돌아서 경쟁이 없습니다. `Interlocked`로 바꿔도 그대로입니다.
- **`.Wait()`/`.Result`로 동기 로드**: 게임 루프가 멈춥니다(랩 63). 로드를 끝내 줄 다음 프레임이 오지 않습니다.

**추적 방법:**
- `store.Add` 줄에 중단점을 걸고 매번 `e`에 개체 ID를 만들어 보세요. 다른 알림이어야 할 `e`가 전부 같은 `$1`입니다.
- 디버그 → 창 → **작업(Tasks)** / **병렬 스택**의 작업 보기: 첫 프레임 뒤에 `OnProgressAsync` 작업 여러 개가 같은 `LoadAsync`를 기다리고 있습니다.
- `Publish`에 `{questId} {amount}` 추적점을, `store.Add`에 `{e}` 추적점을 겁니다. 출력 창에서 순서를 비교하면, 보낸 알림과 처리한 알림이 다릅니다.

**수정:** 처리기에서 `await` **전에** 필요한 값을 지역 변수로 복사한다. `C2/Achievements.cs`의 `AchievementTracker.OnProgressAsync`를 아래로 바꾼다. `await` 뒤에서는 `e`를 다시 읽지 않는다. 다른 클래스는 그대로 둔다.
```csharp
    async Task OnProgressAsync(QuestProgress e)
    {
        // e 는 버스가 계속 다시 쓰는 객체다. await 전에 필요한 값을 복사해 둔다
        string questId = e.QuestId;
        int amount = e.Amount;

        if (!store.Loaded)
            await store.LoadAsync();

        int now = store.Add(questId, amount);
        _handled += 1;
        if (goals.TryGetValue(questId, out int goal) && now >= goal && store.Unlock(questId))
            Console.WriteLine(Text.L($"  [업적] {questId} 달성! ({now}/{goal})", $"  [実績] {questId} 達成! ({now}/{goal})"));
    }
```
다른 방법: 버스가 알림마다 새 객체를 만든다(`QuestEventBus.Publish`가 `_current`를 고쳐 쓰는 대신 `new QuestProgress { ... }`를 넘긴다. 불변 `record`로 만들면 더 좋다).

재사용하고 싶다면 그 객체가 처리기 밖으로 새지 않는다는 계약(동기 처리기만)이 있어야 합니다.

**교훈:** `await`은 메서드를 둘로 자릅니다. 뒤쪽 절반은 **나중에** 실행되므로, 참조로 받은 인자가 그때도 같은 값이라는 보장이 없습니다. 할당을 줄이는 최적화(12부)를 하면 "언제까지 이 객체를 써도 되는가"라는 수명 규칙이 생기는데, 그 규칙은 `async`와 잘 어울리지 않습니다. `ArrayPool`(랩 79)와 같은 종류의 버그입니다.

**범주:** 비동기 경계 + 객체 재사용 (8부 비동기, 12부 성능 최적화의 부작용)

## 챌린지 C-3 — 캐시가 돌려준 목록을 고쳐 쓴 화면

**퍼지는 길:**
```
1. RankingService.Top 은 캐시해 둔 List<RankEntry> 를 그대로(같은 인스턴스) 돌려준다
2. FriendRankingScreen 이 RankFilters.Where 로 친구만 남긴다. Where 는 RemoveAll 로 받은 목록을 직접 고친다
3. RankFilters.Renumber 가 남은 항목의 Rank 를 1부터 다시 매긴다. RankEntry 객체도 캐시와 공유된다
4. 다음에 Top 을 부르는 화면은 친구만 남은, 순위가 바뀐 목록을 받는다
5. 주간 보상 우편은 친구들에게만, 엉뚱한 순위 보상으로 나간다. 길드 랭킹 창도 길드원 일부가 사라지고 순위가 틀린다
```

**가짜 용의자:**
- **우편 보내기 전에 캐시 비우기**(`Invalidate`) 또는 우편만 DB에서 직접 읽기: 기본 케이스(친구 창 → 우편)는 통과합니다. 하지만 친구 창 다음에 길드 창을 여는 숨은 케이스에서 실패합니다.
- **`Take(Winners)`와 `RewardFor`의 경계값**: 경계는 맞습니다.
- **`Top`에서 목록만 복사**(`return [.. ranks];`): 항목이 사라지는 것은 막지만, 공유된 `RankEntry`의 `Rank`가 바뀌어서 보상 등급이 여전히 틀립니다.

**추적 방법:**
- `Top`의 `return ranks;`와 각 화면에서 받은 목록에 개체 ID를 만들면, 모두 같은 `$1`입니다.
- 조사식에 `$1.Count`를 넣고 친구 창을 지나가 보세요. 24가 5로 바뀝니다. 누가 바꿨는지는 `List<T>.RemoveAll`에 함수 중단점을 걸면 바로 나옵니다(조건: `this == $1`).
- 데이터 중단점: 어느 `RankEntry`의 `Rank` 속성 뒤의 필드를 조사식에서 고르고 "값이 바뀔 때 중단"을 겁니다(.NET Core 3 이상, VS 2019 16.x 이상).

**수정:** 캐시 결과를 아무도 고치지 못하게 한다. 세 가지를 함께 적용해야 컴파일된다(`Rank`를 `init`으로 바꾸는 순간 `RankFilters`가 컴파일되지 않으므로 지워야 하고, 그러면 친구 창도 새로 써야 한다).
1. `Ranking.cs`: `RankEntry.Rank`를 `init`으로 만들고, `Top`은 `IReadOnlyList<RankEntry>`를 돌려주며 순위는 정렬된 순서대로 **새 항목을 만들면서** 정한다.
```csharp
public sealed class RankEntry(string name, string guild, int score)
{
    public string Name { get; } = name;
    public string Guild { get; } = guild;
    public int Score { get; } = score;
    public int Rank { get; init; }   // 만들 때만 정한다

    public override string ToString() => $"{Rank}{Text.L("위", "位")} {Name} ({Score})";
}

public sealed class RankingService(RankingDatabase db)
{
    readonly Dictionary<string, IReadOnlyList<RankEntry>> _cache = [];

    public IReadOnlyList<RankEntry> Top(string region)
    {
        if (!_cache.TryGetValue(region, out IReadOnlyList<RankEntry>? ranks))
        {
            List<RankEntry> rows = db.QuerySeason(region);
            rows.Sort((a, b) => b.Score != a.Score ? b.Score.CompareTo(a.Score) : string.CompareOrdinal(a.Name, b.Name));
            ranks = rows.Select((r, i) => new RankEntry(r.Name, r.Guild, r.Score) { Rank = i + 1 }).ToList();
            _cache[region] = ranks;
        }
        return ranks;
    }

    public void Invalidate(string region) => _cache.Remove(region);
}
```
2. `Ranking.cs`: `RankFilters` 클래스를 바로 위의 설명 주석과 함께 통째로 지운다. "받은 것을 고쳐서 그대로 돌려주는" 도우미는 이름이 거짓말을 한다(LINQ `Where`와 같은 이름인데 동작이 반대다).
3. `Screens.cs`: 친구 창은 캐시를 건드리지 않고 새 목록을 만든다. `GuildRankingScreen`과 `WeeklyRewardMailer`는 `IReadOnlyList`로도 그대로 컴파일된다.
```csharp
public sealed class FriendRankingScreen(RankingService service)
{
    public List<string> Show(string region, IReadOnlySet<string> friends) =>
        service.Top(region).Where(r => friends.Contains(r.Name)).Select((r, i) => $"{i + 1}. {r.Name}").ToList();
}
```
**교훈:** 캐시는 같은 객체를 여러 사람에게 나눠 주는 장치입니다. 나눠 준 객체가 변경 가능하면, 한 화면의 실수가 시간과 장소를 건너 다른 화면의 증상으로 나타납니다. 증상이 난 화면에서 캐시를 비우는 것은 그 화면만 고칩니다. 캐시가 돌려주는 것은 불변으로 만들거나 복사본으로 줍니다 (랩 19 얕은 복사, 30 질의와 스냅샷).

**범주:** 캐시 + 공유된 변경 가능 상태 (2부 얕은 복사, 3부 컨테이너)

---

# 블라인드 (B)

## 챌린지 B-1 — 기다리기 전에 울린 Pulse
```
증상:   "지역 불러오기" 응답 없음. 모두 중단하면 메인 스레드가 Monitor.Wait 에, 로더 스레드는 이미 끝나 있다
원인:   로더가 300ms 안에 끝나서 Pulse 를 먼저 울린다. Monitor 는 신호를 기억하지 않으므로 나중에 Wait 한 쪽은 영원히 기다린다
```
**수정:** 조건 변수와 함께 루프로 기다린다: 로더가 `_done = true`를 쓰고 `Pulse`, 기다리는 쪽은 `while (!_done) Monitor.Wait(_gate);`. `Blind/ChallengeB1.cs`의 `RegionLoader` 클래스를 통째로 아래로 바꾼다. `PrepareScreen`과 `Run`은 그대로 둔다.
```csharp
    sealed class RegionLoader
    {
        readonly object _gate = new();
        int _tiles;
        bool _done;   // Pulse 는 기억되지 않으니 "끝났다"를 상태로 남긴다

        public void Start(string region, int tileCount)
        {
            var worker = new Thread(() =>
            {
                int tiles = 0;
                for (int i = 0; i < tileCount; i++)
                    tiles++;
                lock (_gate)
                {
                    _tiles = tiles;
                    _done = true;
                    Monitor.Pulse(_gate);
                }
            }) { IsBackground = true, Name = Text.L($"로더 ({region})", $"ローダー ({region})"), Priority = ThreadPriority.Highest };
            worker.Start();
        }

        public int WaitForTiles()
        {
            lock (_gate)
            {
                while (!_done)   // 이미 끝났으면 기다리지 않는다
                    Monitor.Wait(_gate);
                return _tiles;
            }
        }
    }
```
더 간단하게는 `Task<int>`를 돌려주고 기다리거나 `ManualResetEventSlim`을 쓴다.

**가짜 수정:** `Monitor.Wait(_gate, 1000)`처럼 제한 시간을 주면, 지역 하나는 1초 만에 끝나서 통과합니다. 하지만 지역 3~4개를 불러오는 숨은 케이스에서는 감시 시간을 넘깁니다.

**범주:** 11부 멈춤 (`Monitor.Wait`/`Pulse` 신호 유실)

## 챌린지 B-2 — 터키어의 i
```
증상:   tr-TR, az-Latn-AZ 에서 "/quit".ToUpper() == "/QUİT" (점 있는 대문자 İ, U+0130). 명령표의 "/QUIT" 과 다르다
        i 가 없는 명령(/map, /help)은 멀쩡하다
```
**수정:** 프로그램이 정한 식별자는 문화권과 무관하게 비교한다: `ToUpperInvariant()`. 더 좋게는 명령표를 `new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)`로 만들고 대소문자를 바꾸지 않는다.

**범주:** 2부 흐름 추적 (문화권 의존 문자열 처리. 랩 16의 숫자 파싱과 같은 종류)

## 챌린지 B-3 — 충돌하는 해시
```
측정:   칸 하나당 Equals 70번 이상 (기대: 4 이하)
원인:   GetHashCode() => X ^ Y. (1,2)와 (2,1), (3,3)과 (0,0) 같은 수많은 칸이 같은 해시를 가져서 HashSet 이 한 버킷을 줄줄이 비교한다.
        X, Y 가 0~127 이면 서로 다른 해시는 128개뿐이다
```
**수정:** `HashCode.Combine(X, Y)` (또는 `record struct`가 만들어 주는 해시)

**범주:** 12부 성능 (`GetHashCode` 품질, 해시 컬렉션의 최악의 경우)

## 챌린지 B-4 — 배열 공변성
```
예외:   ArrayTypeMismatchException at Refill
원인:   Recruit 는 Unit[] 를 돌려주지만 실제로는 Archer[] 다(배열 공변성). 그 배열에 Knight 를 넣는 순간 런타임 형식 검사가 실패한다
```
**수정:** 실제 배열을 `Unit[]`로 만든다. `Blind/ChallengeB4.cs`의 `Recruit`를 아래로 바꾼다(세 갈래의 `.ToArray()`가 모두 `.ToArray<Unit>()`이 된다). `Refill`과 `Run`은 그대로 둔다.
```csharp
    static Unit[] Recruit(string kind, int size) => kind switch
    {
        "archer" => Enumerable.Range(1, size).Select(n => new Archer(n)).ToArray<Unit>(),
        "knight" => Enumerable.Range(1, size).Select(n => new Knight(n)).ToArray<Unit>(),
        _ => Enumerable.Range(1, size).Select(n => new Healer(n)).ToArray<Unit>(),
    };
```
`List<Unit>`을 써도 된다. `IReadOnlyList<Unit>`처럼 공변 인터페이스로 돌려주면 넣기 자체를 컴파일러가 막는다.

**가짜 수정:** 예외를 잡고 넘어가면, 부대가 채워지지 않아 전투력이 모자랍니다.

**범주:** 1부 예외 / 3부 컨테이너 (배열 공변성)

## 챌린지 B-5 — GC가 수거한 타이머
```
증상:   처음 몇 번 저장하다가 첫 GC 이후 멈춘다
원인:   StartAutoSave 의 지역 변수 timer 말고는 Timer 를 붙잡는 것이 없다. 메서드가 끝나면 수거 대상이 되고,
        GC 가 돌면 종료자가 타이머를 멈춘다. (System.Threading.Timer 는 콜백이 살아 있어도 Timer 객체가 없으면 멈춘다)
```
**수정:** 타이머를 지역 변수가 아니라 필드에 저장하고, 끝낼 때 `Dispose`한다. `StartAutoSave` 안에서 `using var timer`로 바꾸는 것은 반대 방향이다(메서드가 끝나는 순간 타이머가 멈춘다).
```csharp
    static int _saves;
    static Timer? _autoSave;   // 필드가 붙잡고 있는 동안은 GC 대상이 아니다

    static void StartAutoSave(int intervalMs)
    {
        _autoSave = new Timer(_ => Interlocked.Increment(ref _saves), null, intervalMs, intervalMs);
        Console.WriteLine(Text.L($"  자동 저장 시작 ({intervalMs}ms마다)", $"  オートセーブ開始 ({intervalMs}ms ごと)"));
    }

    // Run의 마지막 줄 (if/else 뒤)
        _autoSave?.Dispose();
        _autoSave = null;
```
(`PeriodicTimer`와 `async` 루프도 좋다.)

**범주:** 9부 리소스와 메모리 (GC와 객체 수명)

## 챌린지 B-6 — 정적 초기화 순환
```
값:     하드 모드인데 시작 골드 300(노멀 100 × 3), 물약 10골드(노멀 값)
원인:   Balance 의 정적 초기화가 StartingGold 를 계산하려고 Economy 를 초기화한다. Economy 는 Balance.Mode 를 읽는데,
        Balance 는 아직 초기화 중이라 Mode 는 기본값 Normal(0)이다. 순환이 있으면 런타임은 예외 없이 "덜 된" 값을 보여 준다
```
**수정:** 순환을 끊는다. 예: `StartingGold`를 속성(`=> Economy.BaseGold * 3`)으로 바꾸거나, 난이도를 별도 설정 객체로 빼서 생성자로 넘긴다.

**가짜 수정:** 하드 모드 숫자(60, 25)를 박아 넣으면, 나이트메어 숨은 케이스에서 틀립니다.

**범주:** 2부 흐름 추적 (정적 필드 초기화 순서)

## 챌린지 B-7 — Parallel.ForEach에 async 람다
```
값:     '발송 완료' 시점에 도착한 우편 0
원인:   Parallel.ForEach 는 Action<T> 를 받는다. async 람다는 async void 가 되어, 첫 await 에서 바로 "끝났다"고 돌아온다.
        ForEach 는 아무것도 기다리지 않고 끝나고, 우편은 그 뒤에 도착한다. 예외가 나면 프로세스가 죽는다(랩 62)
```
**수정:** `Action<T>`가 아니라 기다릴 수 있는 API로 보낸다. `SendCompensation`을 `async Task`를 돌려주는 `SendCompensationAsync`로 바꾸고(`Run`의 호출도 함께 고친다) `Parallel.ForEachAsync`를 `await`한다. `Run`은 채점기가 부르는 동기 메서드라서 끝까지 기다려야 한다(콘솔 앱에는 `SynchronizationContext`가 없으므로 여기서 `GetAwaiter().GetResult()`로 막아도 랩 63 같은 데드락은 생기지 않는다).
```csharp
    static async Task SendCompensationAsync(MailServer server, IEnumerable<string> players)
    {
        await Parallel.ForEachAsync(players, async (player, ct) => await server.SendAsync(player));
        Console.WriteLine(Text.L("  보상 우편 발송 완료", "  補償の郵便 発送完了"));
    }

    // Run
        SendCompensationAsync(server, players).GetAwaiter().GetResult();
        Expect(Text.L("'발송 완료' 시점에 도착한 우편", "「発送完了」の時点で届いた郵便"), count, server.Delivered);
```
또는 `await Task.WhenAll(players.Select(server.SendAsync));`로 전부 띄우고 한꺼번에 기다린다.

**가짜 수정:** `Thread.Sleep(500)`을 넣으면, 왕복이 더 오래 걸리는 숨은 케이스에서 다시 모자랍니다.

**범주:** 8부 비동기 (`async void`, 기다리지 않은 작업)

## 챌린지 B-8 — 메서드 중간에 수거된 this
```
증상:   Release 에서만 체크섬이 틀린다. Debug 는 통과
원인:   FillAndChecksum 은 처음에 _block 으로 Span 을 얻고 나면 this 를 더 쓰지 않는다. 최적화된 코드에서 JIT 는 this 를
        "이미 죽은 변수"로 보고, 호출한 쪽도 임시 객체(new Texture())를 붙잡고 있지 않다. 그래서 루프 도중 GC 가 Texture 를 수거하고,
        종료자가 블록을 풀에 돌려주고, 스트리밍이 그 블록을 받아 0xEE 로 칠한다
```
**수정:** 네이티브 자원을 쓰는 동안 소유자를 살려 둔다. `Blind/ChallengeB8.cs`의 `Texture.FillAndChecksum`을 아래로 바꾼다. 체크섬을 다 센 뒤 `GC.KeepAlive(this);`를 부르면, JIT는 그 줄까지 `this`를 살아 있는 것으로 본다. 특성(`[MethodImpl]`)과 나머지 코드는 그대로 둔다.
```csharp
        // 핫 경로라서 처음부터 최적화해서 컴파일한다
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public uint FillAndChecksum(byte color, Action everyFewRows)
        {
            Span<byte> pixels = NativePool.Pixels(_block);
            for (int row = 0; row < Rows; row++)
            {
                pixels.Slice(row * RowBytes, RowBytes).Fill(color);
                if (row % 8 == 7)
                    everyFewRows();
            }
            uint sum = 0;
            foreach (byte b in pixels)
                sum = sum * 31 + b;
            GC.KeepAlive(this);   // 여기까지 this(블록의 주인)를 살려 둔다. 종료자가 블록을 먼저 돌려주지 못한다
            return sum;
        }
```
다른 방법:
- 더 근본적으로는 종료자에 기대지 않는다. `IDisposable`로 만들어 `using var tex = new Texture();`처럼 명시적으로 반납한다. 네이티브 핸들은 `SafeHandle`로 감싼다 (랩 74)

**범주:** 10부 unsafe와 네이티브 메모리 (`GC.KeepAlive`, 종료자와 네이티브 자원). Release에서만 나는 점은 랩 88과 같다
