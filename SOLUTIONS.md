# 해답 (스포일러!)

# 1부: 예외와 스택 트레이스

## Lab 1 — null 역참조, `!`로 눌러 둔 nullable 경고
```
멈춘 함수:  PrintPlayer           이상한 값: player == null
값의 출처:  ShowProfile ← FindPlayer가 못 찾아서 null 반환
진짜 원인:  OpenFriendList에서 "Doran"을 "Dorna"로 오타
```
**수정:** 오타 수정. 그리고 `!`를 지우고 방어한다.
```csharp
Player? player = FindPlayer(name);
if (player is null)
{
    Console.WriteLine($"  {name}: 없는 플레이어");
    return;
}
PrintPlayer(player);
```
**교훈:** 죽은 곳(`PrintPlayer`)은 잘못이 없다. `!`는 검사가 아니라 **경고를 끄는 스위치**다. 컴파일러는 `FindPlayer`가 `Player?`를 돌려준다는 것을 알고 경고(CS8600)를 내려고 했다.

## Lab 2 — 대소문자만 다른 Dictionary 키
```
멈춘 함수:  [외부 코드] Dictionary.get_Item → 첫 내 코드: PriceOf(itemId = "Mana_Potion")
값의 출처:  CartTotal ← LoadCart ← SavedCart (1.0 클라이언트가 저장한 데이터)
진짜 원인:  Prices는 기본 비교자(대소문자 구분)인데, 저장된 키는 "Mana_Potion"
```
**수정:** `new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)`. 또는 불러올 때 `ToLowerInvariant()`로 정규화.
없는 아이템까지 대비하려면 `TryGetValue`로 찾고 경고를 남긴다.
**교훈:** `KeyNotFoundException` 메시지에는 못 찾은 **키가 그대로** 나온다. 메시지를 끝까지 읽자. 이미 배포된 데이터는 못 고치니 받는 쪽이 관대해야 한다.

## Lab 3 — 열거 중 컬렉션 수정
```
멈춘 곳:    CastExplosion의 foreach (외부 코드: List<T>.Enumerator.MoveNext)
이상한 값:  _enemies가 순회 도중 줄어듦
진짜 범인:  Despawn의 _enemies.Remove — 예외가 날 때는 이미 반환해서 호출 스택에 없음
```
**수정 (택1):**
- `foreach (Enemy enemy in _enemies.ToList())` — 사본을 순회
- 순회 중에는 죽은 적을 모아 두고, 루프가 끝난 뒤 `_enemies.RemoveAll(e => e.Hp <= 0)`
- 뒤에서부터 `for (int i = _enemies.Count - 1; i >= 0; i--)`

**교훈:** 예외는 수정한 순간이 아니라 **다음 `MoveNext`**에서 난다. 호출 스택은 "지금"만 보여 준다. 범인을 잡으려면 수정하는 줄에 중단점을 건다.

## Lab 4 — 박싱된 long을 (int)로 박싱 해제
```
멈춘 함수:  CreateParty
이상한 값:  config["maxPartySize"].GetType() == System.Int64
진짜 원인:  ParseConfig가 숫자를 long으로 박싱해 담았고, (int)obj 는 "박싱 해제"라 정확히 int일 때만 됨
```
**수정:** `Convert.ToInt32(config["maxPartySize"])`, 또는 `(int)(long)config["maxPartySize"]`. 더 좋게는 설정을 처음부터 형식이 있는 클래스로 파싱한다.
**교훈:** `(int)` 캐스트는 상황에 따라 **숫자 변환**일 수도 **박싱 해제**일 수도 있다. `object`에서 꺼낼 때는 후자다.

## Lab 5 — 자기 자신을 부르는 속성 setter
```
증상:       Stack overflow. Repeat N times: at BugHunt.Lab05+Wallet.set_Gold(Int32)
호출 스택:  set_Gold 가 수천 개
진짜 원인:  set => Gold = ...   (백킹 필드 _gold가 아니라 속성 자신에 대입)
```
**수정:** `set => _gold = Math.Clamp(value, 0, MaxGold);`
**교훈:** StackOverflow는 **잡을 수 없고** 프로세스가 끝난다. 디버거 없이 실행하면 런타임이 반복되는 프레임을 `Repeat N times`로 접어서 보여 준다. 스택이 한 함수로 가득 차면 재귀가 끝나지 않는 것.

## Lab 6 — AggregateException 안의 FormatException
```
멈춘 곳:    Run의 Parallel.ForEach (처리되지 않은 System.AggregateException)
내부 예외:  FormatException: The input string '??' was not in a correct format. ← Parse("swamp.loot")
진짜 원인:  catch (FormatException)는 AggregateException과 형식이 달라서 안 잡힘.
           게다가 실패 후 남은 파일을 처리하지 않을 수도 있어서 "파일 하나만 건너뛰기"가 안 됨
```
**수정:** 파일 하나 단위로 잡는다.
```csharp
Parallel.ForEach(Files, file =>
{
    try { loaded.Add(Parse(file.Key, file.Value)); }
    catch (FormatException ex)
    {
        Console.WriteLine($"  고장 난 파일 건너뜀: {file.Key} ({ex.Message})");
        Interlocked.Increment(ref skipped);
    }
});
```
(`skipped++`를 그대로 두면 스레드 경쟁이 생긴다. 랩 23 참고.) 더 나아가 `int.TryParse`로 예외 없이 처리할 수도 있다.
**교훈:** 병렬 API(`Parallel`, `Task.Wait`, `Task.Result`)는 예외를 **AggregateException으로 감싼다**. 예외 설정 창의 first chance 중단으로 진짜 throw 지점(작업 스레드)을 바로 볼 수 있다.

## Lab 7 — `throw ex;`가 스택 트레이스를 덮어씀
```
리포트 맨 위:   at BugHunt.Lab07.LoadSlot(Int32 index)   ← throw ex; 줄
실제 throw:     int.Parse ← ParseItem("potion:two") ← ParseInventory ← ParseSlot ← LoadSlot
진짜 원인:      throw ex; 는 예외를 "새로 던진" 것처럼 스택 트레이스를 이 줄에서 다시 시작함
숨긴 경고:      BugHunt.csproj 의 <NoWarn>CA2200</NoWarn>
```
**수정:** `throw;` 그리고 `NoWarn`에서 CA2200을 지운다. 정보를 덧붙이고 싶다면 `throw new SaveLoadException($"슬롯 {index + 1}", ex);`처럼 **내부 예외로 감싼다**.
**교훈:** 스택 트레이스는 예외가 **마지막으로 throw된 곳**부터 기록된다. 다시 던질 때는 `throw;`. 경고를 끄면 그 경고가 지키던 버그도 함께 숨는다.

---

# 2부: 흐름 추적

## Lab 8 — 루프 변수 캡처
```
기대: CastSlot(1), (2), (3)        실제: CastSlot(4) 세 번
처음 어긋난 곳: 람다는 slot의 "값"이 아니라 "변수"를 캡처함.
              for의 slot은 루프 전체에서 변수 하나(DisplayClass 객체 하나)이고, 루프가 끝나면 4
```
**수정:** 루프 안에서 새 변수에 복사한다.
```csharp
int thisSlot = slot;
hotkeys[key] = () => CastSlot(thisSlot);
```
**교훈:** C#의 람다는 변수를 **참조로** 캡처한다. `foreach` 변수는 C# 5부터 바퀴마다 새 변수라 안전하지만, `for`의 변수는 아니다. 지역 창에서 람다의 `this`를 펼쳐 보면 `<>c__DisplayClass`가 보이고, 개체 ID로 세 람다가 같은 캡처 객체를 공유하는 것을 확인할 수 있다.

## Lab 9 — LINQ 지연 실행이 두 번 열거됨
```
기대: Send 3번          실제: 6번
처음 어긋난 곳: rewards.Count() 에서 3번, foreach 에서 또 3번
진짜 원인:     Select(...)는 결과 목록이 아니라 "쿼리"를 돌려준다. 열거할 때마다 Send가 다시 실행됨
```
**수정:** 부수 효과가 있는 쿼리는 즉시 실체화한다. `.Select(...).ToList()`. 더 좋게는 Select 안에서 부수 효과를 내지 않고, 대상 목록을 만든 뒤 `foreach`로 보낸다.
**교훈:** `IEnumerable<T>` 변수는 **목록이 아니라 만드는 방법**일 수 있다. 디버거의 "결과 보기"도 열거를 실행해서 상태를 바꾼다.

## Lab 10 — readonly struct 필드의 방어적 복사
```
TryUse 안:   this.Current 100 → 70 (기대대로)
Sprint 로 나와서: _stamina.Current 100
진짜 원인:   _stamina는 readonly 필드이고 Stamina는 변경 가능한 struct.
            컴파일러는 readonly 필드를 지키려고 메서드 호출 때마다 복사본을 만들어 그 복사본을 바꿈
```
**수정 (택1):** `readonly`를 지운다 / `Stamina`를 `class`로 바꾼다 / struct를 불변으로 만들고 `_stamina = _stamina.Use(30)`처럼 새 값을 받는다.
**교훈:** "변경 가능한 struct"는 복사의 함정이 많다. 처음부터 `readonly struct`로 선언하면, 필드를 바꾸는 메서드 자체가 **컴파일 오류**가 되어 이런 실수를 막는다.

## Lab 11 — 박싱된 값의 `!=`는 참조 비교
```
조건부 중단점 (key == "fullscreen"):
    before[key]          true (object)
    after[key]           true (object)
    before[key] != after[key]                 true  ← 둘 다 true인데 "다르다"
    before[key].Equals(after[key])            true
    ReferenceEquals(before[key], after[key])  false
진짜 원인:   변수 형식이 object라서 ==/!= 는 값이 아니라 "같은 상자인가"를 비교. Snapshot마다 새로 박싱됨
```
**수정:** `if (!Equals(before[key], after[key]))` (정적 `object.Equals`는 null도 안전하게 처리하고 값의 `Equals`를 부른다).
**language가 맞게 나온 이유:** 두 스냅샷이 **같은 string 객체**(`settings.Language`)를 담았기 때문에 참조 비교도 우연히 같았다. 설정을 파일에서 다시 읽었다면 내용이 같은 다른 string 객체가 되어 "바뀌었다"고 나왔을 것이다.
**교훈:** `==`의 의미는 **변수의 컴파일 시점 형식**으로 정해진다. `object`로 들고 있으면 int도 bool도 참조 비교다.

## Lab 12 — float 동등 비교
```
조건부 중단점 (XpBar > 0.95f):
    XpBar              1.0000001
    XpBar == 1.0f      false
    (double)XpBar      1.0000001192092896
진짜 원인:  0.1f 는 2진수로 정확하지 않아 열 번 더하면 1.0f 가 되지 않음. 화면의 P0 서식은 반올림해서 "100%"
```
**수정:** 최소한 `if (XpBar >= 1.0f)`(또는 작은 허용 오차). 근본적으로는 경험치를 **정수**로 들고(`int xp`, `int xpToNext`), 바의 비율은 표시할 때만 계산한다.
**교훈:** 부동소수점은 `==`로 비교하지 않는다. 화면에 보이는 숫자는 서식이 반올림한 값일 수 있다.

## Lab 13 — 중간 계산의 int 오버플로
```
조건부 중단점 (damage == 30000000):
    damage * 100           -1294967296
    checked(damage * 100)  System.OverflowException
    int.MaxValue           2147483647
진짜 원인:  30,000,000 × 100 = 3,000,000,000 > int.MaxValue. C#은 기본이 unchecked라 조용히 한 바퀴 돎
```
**수정:** `(int)((long)damage * 100 / totalDamage)`. 기여도를 `double`로 계산하는 것도 방법이다.
**교훈:** 결과는 int에 들어가도 **중간값이 넘칠 수 있다.** Debug 빌드에서 `<CheckForOverflowUnderflow>true</CheckForOverflowUnderflow>`를 켜 두면 이런 곳에서 바로 예외로 멈춘다.

## Lab 14 — string 불변성, Replace 결과를 버림
```
Replace 줄을 F10:  nickname 그대로. 자동 창에 "String.Replace 반환  "  ***Slayer "" 가 보였다가 사라짐
진짜 원인:  string은 불변. Replace는 새 문자열을 돌려줄 뿐이고, 그 반환값을 아무 데도 저장하지 않음
           (Trim은 return nickname.Trim(); 으로 반환값을 썼기 때문에 잘 동작함)
```
**수정:** `nickname = nickname.Replace(word, "***", StringComparison.OrdinalIgnoreCase);`
**교훈:** `string`, `DateTime`, `ImmutableList` 같은 불변 형식의 메서드는 **새 값을 돌려준다.** CA1806을 켜면 반환값을 버리는 호출을 경고로 잡는다.

## Lab 15 — 현재 문화권으로 쓰고 InvariantCulture로 읽음
```
de-DE 일 때 file:  "moveSpeed=1,5\nmouseSensitivity=0,75"
NumberDecimalSeparator (de-DE)  ","
double.Parse("1,5", InvariantCulture)   15   ← Invariant에서 ","는 천 단위 구분 기호
진짜 원인:  $"{double}" 보간은 CurrentCulture로 숫자를 씀. 읽는 쪽만 InvariantCulture
```
**수정:** 쓰는 쪽도 고정한다. `string.Create(CultureInfo.InvariantCulture, $"moveSpeed={o.MoveSpeed}\n...")`, 또는 `FormattableString.Invariant(...)`, `o.MoveSpeed.ToString(CultureInfo.InvariantCulture)`.
**교훈:** 파일·네트워크·세이브처럼 **기계가 읽는 텍스트**는 쓰기와 읽기 모두 InvariantCulture로. 화면에 보여 줄 때만 CurrentCulture. 한국어·영어 PC에서는 재현되지 않아서 "내 PC에서는 되는" 버그가 된다. CA1305가 잡아 준다.

## Lab 16 — DateTime 빼기는 Kind를 무시한다
```
EndsAtUtc       2026-11-03 15:00  Kind=Utc
clock.Now       2026-11-03 21:00  Kind=Unspecified (서울 시각)
clock.UtcNow    2026-11-03 12:00  Kind=Utc
EndsAtUtc - clock.Now     -06:00:00
EndsAtUtc - clock.UtcNow  03:00:00
진짜 원인:  보여 주기용 현지 시각으로 계산함. DateTime 끼리 빼기는 Kind와 시간대를 보지 않고 숫자만 뺌
```
**수정:** `EndsAtUtc - clock.UtcNow`. 시간대가 섞이는 코드라면 `DateTimeOffset`을 쓰면 빼기가 오프셋을 반영한다.
**교훈:** 계산은 UTC끼리, 변환은 **보여 주기 직전에만**. 조사식에서 `Kind`를 펼쳐 보면 바로 보인다.

## Lab 17 — Equals와 GetHashCode가 서로 다른 규칙
```
Equals 중단점:  "kael" 을 넣을 때 걸리지 않음 (HashSet이 Equals까지 가지도 않음)
조사식:
    new PlayerName("Kael").Equals(new PlayerName("kael"))   true
    new PlayerName("Kael").GetHashCode()   12345...
    new PlayerName("kael").GetHashCode()   -6789...   ← 다름
진짜 원인:  Equals는 대소문자 무시, GetHashCode는 대소문자 구분(string.GetHashCode)
```
**수정:** `public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);`
**교훈:** `Equals`가 true면 **GetHashCode도 반드시 같아야 한다.** HashSet/Dictionary는 해시로 칸을 먼저 고르고 그 칸 안에서만 Equals를 부른다. **걸리지 않는 중단점**도 중요한 단서다.

## Lab 18 — record `with`는 얕은 복사
```
개체 ID:   basePreset.Skills {$1}, custom.Skills {$1}  ← 같은 List
ReferenceEquals(basePreset, custom)                false (레코드는 새로 만들어짐)
ReferenceEquals(basePreset.Skills, custom.Skills)  true  (List는 주소만 복사됨)
```
**수정:** 바꿀 컬렉션은 함께 복사한다. `basePreset with { Name = name, Skills = [.. basePreset.Skills] }`. 근본적으로는 레코드에 `List<T>` 대신 `IReadOnlyList<T>`나 `ImmutableList<T>`를 두어, 복사본을 바꾸려면 새 컬렉션을 만들 수밖에 없게 한다.
**교훈:** `with`는 "새 레코드 + 속성 값 그대로 복사"다. 참조 형식 속성은 **공유**된다.

---

# 3부: 비동기와 동시성

## Lab 19 — async void의 예외는 catch에 오지 않는다
```
멈춘 곳:    SaveAsync의 throw (스레드 창: .NET TP Worker, 호출 스택에 AutoSave/Run 없음)
진짜 원인:  SaveAsync는 첫 await에서 바로 반환됨 → try 블록은 이미 끝남.
           async void라 실패를 전달할 Task가 없어서, 예외가 스레드 풀에서 다시 던져지고 프로세스가 죽음
```
**수정:** `async Task`로 바꾸고 기다린다.
```csharp
public async Task SaveAsync(string slot) { ... }

static async Task<int> AutoSaveAsync(SaveSystem saves, string[] checkpoints)
{
    ...
    try { await saves.SaveAsync(checkpoint); }
    catch (IOException ex) { failures++; ... }
}
```
`Run`에서는 `AutoSaveAsync(...).GetAwaiter().GetResult()` (콘솔의 최상위이므로 괜찮다).
**교훈:** `async void`는 호출자가 **끝났는지도, 실패했는지도 알 수 없다.** 이벤트 처리기처럼 시그니처가 정해진 경우에만 쓴다.

## Lab 20 — `.Result` 데드락
```
모두 중단 → 병렬 스택:
    Main Thread (게임 루프):  Task.Result → ... → Lab20.OnEnterLoadingScreen  (막혀 있음)
작업 창:     LoadAssetsAsync  대기 중 (첫 await 이후 계속할 차례를 기다림)
main.PendingCount:  1  ← LoadAssetsAsync의 나머지가 메인 스레드 큐에 줄 서 있음
진짜 원인:  메인 스레드가 .Result로 막혀 있는데, await의 나머지는 그 메인 스레드에서만 실행될 수 있음
```
**수정 (택1):**
- 끝까지 async (권장): `static async Task OnEnterLoadingScreenAsync(...) { ... int loaded = await LoadAssetsAsync(); ... }` 그리고 `main.Run(() => OnEnterLoadingScreenAsync(screen))`
- 라이브러리 쪽 `LoadAssetsAsync` 안의 `await Task.Delay(20).ConfigureAwait(false)` — 메인 스레드로 돌아오지 않으므로 데드락은 풀린다. 단, 그 뒤에서 메인 스레드 전용 객체를 만지면 랩 24의 버그가 된다.

**교훈:** SynchronizationContext가 있는 스레드에서 비동기 작업을 **동기로 기다리면** 데드락이 된다. 콘솔 기본 스레드에는 컨텍스트가 없어서 재현되지 않으므로, 콘솔에서 시험한 코드가 UI/게임에서 멈춘다.

## Lab 21 — await 누락 (fire-and-forget)
```
Expect 줄의 작업 창:  SubmitAsync 3개, 모두 "대기 중"
Interlocked.Increment 중단점:  Expect 보다 나중에 걸림 (또는 프로세스가 먼저 끝나서 안 걸림)
진짜 원인:  _ = server.SubmitAsync(id); 로 Task를 버림. 아무도 끝나기를 기다리지 않음
```
**수정:**
```csharp
static async Task SubmitUnlockedAsync(AchievementServer server, IEnumerable<string> unlocked)
{
    foreach (string id in unlocked)
        await server.SubmitAsync(id);
}
```
동시에 보내도 된다면 `await Task.WhenAll(unlocked.Select(server.SubmitAsync));`. `Run`에서는 `.GetAwaiter().GetResult()`.
**교훈:** Task를 돌려받으면 **누군가는 기다려야 한다.** `_ =`는 그 책임을 일부러 버린다는 표시다. CS4014를 끄려고 붙이면 안 된다.

## Lab 22 — `await Task.WhenAll`은 첫 예외만 던진다
```
throw 중단점:  두 번 걸림 (sounds.pak, locale.pak)
catch 안의 ex:  IOException "sounds.pak 다운로드 실패" 하나
downloads[3].Status  Faulted, downloads[3].Exception  locale.pak
진짜 원인:  WhenAll의 Task는 예외 둘을 AggregateException으로 갖고 있지만, await는 첫 번째 것만 꺼내 던짐
```
**수정:** catch 안에서 각 Task를 확인한다.
```csharp
catch (Exception)
{
    foreach (Exception ex in downloads.Where(d => d.IsFaulted).Select(d => d.Exception!.InnerException!))
    {
        Console.WriteLine($"  [로그] {ex.Message}");
        failed.Add(ex.Message.Split(' ')[0]);
    }
}
```
또는 `Task all = Task.WhenAll(downloads); try { await all; } catch { foreach (var ex in all.Exception!.InnerExceptions) ... }`.
**교훈:** 예외는 사라지지 않고 **Task 안에** 남아 있다. 여러 작업의 실패를 모두 다루려면 Task를 직접 본다.

## Lab 23 — lock 없는 경쟁 조건
```
결과:     매 실행마다 다른 값 (예: 타격 1,0xx,xxx / 기대 2,000,000)
병렬 스택:  Record 안에 스레드 4개가 동시에 있음
진짜 원인:  TotalDamage += damage 와 Hits++ 는 읽기-수정-쓰기 세 단계라, 두 스레드가 겹치면 한쪽 더하기가 사라짐
```
**수정 (택1):**
- `Interlocked.Add(ref TotalDamage, damage); Interlocked.Increment(ref Hits);`
- `lock (_gate) { TotalDamage += damage; Hits++; }` (두 값을 **함께** 일관되게 바꿔야 한다면 lock)
- 파티원마다 지역 합계를 내고 마지막에 한 번만 더한다 (`Parallel.ForEach`의 localInit/localFinally 오버로드). 가장 빠르다.

**교훈:** 결과가 실행마다 다르면 경쟁 조건부터 의심한다. 중단점을 걸면 타이밍이 바뀌어 오히려 안 보일 수 있다.

## Lab 24 — ConfigureAwait(false) 뒤에서 메인 스레드 전용 객체를 만짐
```
RefreshHudAsync 첫 줄:  스레드 = Main Thread (게임 루프), SynchronizationContext.Current = MainThread
hud.SetGold 줄:         스레드 = .NET TP Worker, SynchronizationContext.Current = null
진짜 원인:  ConfigureAwait(false) 때문에 await 뒤가 메인 스레드로 돌아오지 않음
```
**수정:** 이 메서드에서는 `ConfigureAwait(false)`를 지운다. 메인 스레드로 돌아와야 하는 코드(UI, 게임 오브젝트)는 기본 동작(컨텍스트 캡처)을 써야 한다.
**교훈:** `ConfigureAwait(false)`는 **컨텍스트가 필요 없는 라이브러리 코드**용이다. 랩 20에서는 데드락을 푸는 도구였지만, 그 뒤에서 메인 스레드 전용 객체를 만지면 버그가 된다. 실제 WinForms/WPF/유니티에서는 여기서 예외가 나거나 화면이 가끔 깨진다.

## Lab 25 — CancellationToken을 넘기지 않음
```
SearchRegionAsync (region == 10):
    token.IsCancellationRequested  false
    token.CanBeCanceled            false   ← default(CancellationToken). 절대 취소되지 않는 토큰
FindMatchAsync 프레임의 token:   IsCancellationRequested true
진짜 원인:  FindMatchAsync가 SearchRegionAsync(region) 으로 부르면서 token을 빠뜨림. 기본값 = default 라 컴파일러도 조용함
```
**수정:** `await SearchRegionAsync(region, token);`. 가능하면 `CancellationToken` 매개변수에 기본값을 두지 않아서 빠뜨리면 컴파일 오류가 나게 한다.
**교훈:** 취소는 **사슬의 모든 고리**에 토큰을 넘겨야 전달된다. `CanBeCanceled == false`는 "누가 토큰을 안 넘겼다"는 신호다. CA2016이 이것을 잡는다.

---

# 4부: 리소스와 메모리

## Lab 26 — Dispose하지 않은 StreamWriter
```
SaveReplay 마지막 줄:
    new FileInfo(path).Length    0
    writer.BaseStream.Position   1024 (StreamWriter가 FileStream 버퍼로 넘긴 양. 디스크에는 아직 0)
진짜 원인:  StreamWriter/FileStream은 버퍼에 모았다가 Flush/Dispose 때 씀. writer를 Dispose하지 않아서 버퍼가 디스크로 나가지 않음.
           핸들도 프로세스가 끝날 때까지 열려 있음
```
**수정:** `using var writer = new StreamWriter(path);` (또는 `using (...) { }` 블록, 또는 `File.WriteAllLines(path, frames)`).
**교훈:** `IDisposable`을 구현한 객체는 **반드시 Dispose**한다. GC는 메모리를 회수할 뿐, 버퍼를 플러시하거나 파일을 닫아 주는 시점은 보장하지 않는다. .NET은 종료할 때 종료자도 돌리지 않는다.

## Lab 27 — static 이벤트가 닫은 창을 붙잡음
```
메모리 스냅샷:  CombatLogWindow 50개, 각각 256KB 텍스처 캐시
루트 경로:     CombatLogWindow ← Action<string,int> (대리자의 Target) ← Lab27.GameEvents.DamageDealt (static 필드)
진짜 원인:     Open에서 += 로 구독하고 Close에서 -= 로 해지하지 않음.
              이벤트(대리자)가 구독자를 참조하고, static 필드는 프로그램이 끝날 때까지 GC 루트
```
**수정:** `Close()`에 `GameEvents.DamageDealt -= OnDamageDealt;`. 창의 수명이 명확하다면 `IDisposable`로 만들어 `Dispose`에서 해지한다.
**교훈:** "참조를 다 버렸는데 메모리가 안 줄어든다"면 **누가 아직 붙잡고 있는지**를 찾는다. 메모리 스냅샷의 루트 경로나 `dotnet-dump`의 `gcroot`가 그 사슬을 보여 준다. 오래 사는 객체(static, 싱글턴)의 이벤트에 짧게 사는 객체가 구독하는 구조가 가장 흔한 누수다.

## Lab 28 — 종료자에 자원 반납을 맡김
```
~Texture() 중단점:   Exit() 동안 안 걸림. 프로그램이 끝날 때까지도 안 걸림
GC.CollectionCount(0)  0   ← GC가 한 번도 안 돌았음
(조사식으로 GC.Collect() 실행 후) ~Texture() 가 "종료자 스레드"(.NET Finalizer)에서 걸림
진짜 원인:  종료자는 C++ 소멸자가 아님. 객체가 쓸모없어진 순간이 아니라, GC가 발견한 뒤 종료자 스레드가 짬이 날 때 불림
```
**수정:** `IDisposable`을 구현하고, 레벨을 나갈 때 명시적으로 Dispose한다.
```csharp
class Texture : IDisposable
{
    bool _disposed;
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GpuMemory.Free(Bytes);
        GC.SuppressFinalize(this);
    }
    ~Texture() => Dispose();   // 안전망: Dispose를 잊었을 때만, 언젠가
}

public void Exit()
{
    foreach (Texture texture in _textures)
        texture.Dispose();
    _textures.Clear();
}
```
(실무에서는 비관리 핸들을 직접 종료자로 다루기보다 `SafeHandle`을 쓴다.)
**교훈:** "정해진 순간"에 돌려줘야 하는 자원은 **Dispose**로. 종료자는 잊었을 때를 위한 안전망일 뿐이고, 언제 불릴지(불리기는 할지) 보장되지 않는다. `GC.Collect()`로 억지로 돌리는 것도 해결책이 아니다.

---

# 5부: unsafe와 네이티브 메모리

## Lab 29 — 메서드가 끝난 stackalloc 메모리를 가리키는 포인터
```
FindPath의 return 줄:          path[0] == 3, path[1] == 4   ← 여기까지는 맞다
DrawMinimapBackground 다음:    path[0] == -1 또는 쓰레기 값
(long)path 와 &pixels[0]:      같은 스택 영역(몇십 바이트 차이)
진짜 원인:  stackalloc 메모리는 FindPath의 스택 프레임 안에 있다. FindPath가 돌아가면 그 자리는 다음 호출이 다시 쓴다.
           Span<int>로 돌려주려다 CS8352로 막히자 포인터로 바꿔서 컴파일러의 검사를 피했다.
```
**수정:** 오래 살아야 하는 쪽(호출자)이 버퍼를 만들어 `Span`으로 넘긴다.
```csharp
static int FindPath(int startTile, int goalTile, Span<int> path)
{
    int step = startTile < goalTile ? 1 : -1;
    int length = 0;
    for (int tile = startTile; tile != goalTile + step; tile += step)
        path[length++] = tile;
    return length;
}

Span<int> path = stackalloc int[MaxPathLength];
int length = FindPath(start, goal, path);
```
(크기가 크거나 정해지지 않았다면 `int[]`나 `ArrayPool`을 쓴다.)
**교훈:** 컴파일러가 `Span`을 막는 것은 **수명 검사**다. 포인터, `Unsafe.AsRef`, `MemoryMarshal.CreateSpan`으로 그 검사를 우회하면 C++의 "지역 변수 주소 반환"과 똑같은 버그가 된다. 값을 망가뜨리는 것은 읽는 순간이 아니라 **그 사이의 다른 호출**이라서, 중단점을 거는 위치에 따라 멀쩡해 보이기도 한다.

## Lab 30 — fixed 블록 밖으로 빼낸 포인터를 GC 압축 뒤에 사용
```
Attach 줄의 (long)p:              처음 주소
LoadLevel 뒤 Samples의 주소:       다른 주소   ← 배열이 옮겨졌다
MixFrame의 (long)_buffer:          처음 주소 그대로
진짜 원인:  fixed는 "블록 안에서만" 배열을 고정한다. 블록이 끝난 뒤 GC.Collect(compacting)가 배열을 옮겼고,
           오디오 장치는 옛 주소(이제 비어 있는 곳)를 계속 읽는다.
```
**수정 (택1):**
- 처음부터 옮겨지지 않는 힙(POH)에 만든다: `public short[] Samples { get; } = GC.AllocateArray<short>(32, pinned: true);`
- `GCHandle.Alloc(Samples, GCHandleType.Pinned)`로 고정하고, 장치에서 떼어 낼 때 `Free` (랩 31 참고)
- 네이티브가 오래 쓸 버퍼는 아예 `NativeMemory.Alloc`으로 GC 밖에 만들고 `NativeMemory.Free`로 돌려준다

**교훈:** 관리 참조는 GC가 같이 고쳐 주지만 **포인터는 GC가 모르는 숫자**다. 네이티브 쪽이 "주소를 기억해 두는" API라면 `fixed`로는 안 된다. 실제로는 GC가 언제 압축하느냐에 따라 오랫동안 멀쩡하다가 터지는 버그인데, 이 랩은 `GC.Collect(..., compacting: true)`로 그 순간을 고정해 두었다.

## Lab 31 — 예외 경로에서 GCHandle.Free 누락
```
살아 남은 버퍼 수:     2  == 손상된 패킷 수
throw 줄에서 F10:      handle.Free()로 가지 않고 catch로 빠짐
메모리 스냅샷 루트 경로: Byte[] ← 고정 핸들(Pinned Handle)
GC.GetGCMemoryInfo().PinnedObjectsCount:  손상된 패킷 수만큼 늘어 있음
진짜 원인:  체크섬이 틀리면 throw로 빠져나가면서 handle.Free()를 건너뜀
```
**수정:** 고정은 네이티브 호출 동안만 하고, `finally`에서 반드시 푼다.
```csharp
GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
try
{
    NativeNet.Receive(handle.AddrOfPinnedObject(), buffer.Length, packetId, corrupt);
}
finally
{
    handle.Free();
}
```
짧게만 고정한다면 `fixed (byte* p = buffer) { ... }`가 더 간단하다 (블록을 어떻게 빠져나가든 풀린다).
**교훈:** `GCHandle`은 GC 바깥의 강한 참조라서, `Free`를 잊으면 **GC가 있어도 누수**다. Pinned 핸들은 게다가 힙 압축도 방해한다. 얻었으면 `finally`에서 돌려준다. `lock`, 세마포(랩 39)와 같은 규칙이다.

## Lab 32 — 네이티브에 넘긴 콜백 대리자가 GC에 수거됨
```
콘솔(디버거 없이):  Process terminated.
                    A callback was made on a garbage collected delegate of type 'BugHunt!BugHunt.Lab32+BeatCallback::Invoke'.
F5:                 PlayBeat의 _onBeat(beat) 줄에서 멈춤
진짜 원인:  GetFunctionPointerForDelegate(OnBeat)에 넘긴 대리자는 그 자리에서 만들어진 임시 객체다.
           네이티브 쪽은 함수 포인터(숫자)만 가지고 있으므로 GC는 대리자를 쓰레기로 보고, LoadSong의 GC.Collect가 수거한다.
           대리자와 함께 네이티브 진입점(thunk)도 막혀서, 다음 박자에 런타임이 프로세스를 끝낸다.
```
**수정 (택1):**
- 네이티브가 콜백을 들고 있는 동안 대리자를 필드에 붙잡아 둔다.
  ```csharp
  BeatCallback? _beatCallback;   // 네이티브가 들고 있는 동안 살아 있게

  public void Start()
  {
      _beatCallback = OnBeat;
      NativeAudio.SetBeatCallback(Marshal.GetFunctionPointerForDelegate(_beatCallback));
  }
  ```
- 대리자를 아예 쓰지 않는다: `[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])] static int OnBeat(int beat)`를 만들고 `&OnBeat`를 넘긴다. (static이라 게임 객체는 따로 찾아야 한다. `GCHandle.ToIntPtr`로 만든 "사용자 데이터" 포인터를 같이 넘기는 방식이 흔하다.)

`GC.KeepAlive(callback)`은 **그 줄까지만** 살려 두므로, 나중에 불리는 콜백에는 소용이 없다.
**교훈:** .NET Framework에서 MDA `CallbackOnCollectedDelegate`로 유명했던 버그다. 지금의 .NET은 수거된 대리자의 진입점을 막아 두기 때문에, 엉뚱한 곳으로 뛰지 않고 **이유를 적고 즉시 끝난다.** 그 메시지의 형식 이름이 범인이다.

## Lab 33 — ArrayPool에 반납한 배열을 Memory로 계속 사용
```
history.Add 중단점마다 history[0]:  "[Kael] 안녕" → 다음 메시지로 덮어써진 값 → ...   ← 매번 바뀐다
개체 ID:   FormatMessage의 buffer가 매번 같은 $1
진짜 원인:  Return(buffer) 뒤에도 buffer.AsMemory(...)를 돌려줘서 기록에 남김.
           같은 스레드에서 다시 Rent하면 방금 돌려준 그 배열이 나오고, 새 메시지가 옛 기록 위에 덮어써진다.
           ReadOnlyMemory/Span은 배열의 "창문"일 뿐 복사본이 아니다.
```
**수정:** 오래 둘 데이터는 반납하기 전에 복사한다.
```csharp
string message = new(buffer, 0, length);
ArrayPool<char>.Shared.Return(buffer);
return message.AsMemory();
```
(더 간단히는 `string.Concat`으로 string을 바로 만든다. 풀은 **그 메서드 안에서만** 쓰는 임시 버퍼에 쓴다.)
**교훈:** `Return`은 "이제 다른 사람이 써도 된다"는 약속이다. 반납한 배열을 가리키는 `Span`, `Memory`, 슬라이스가 남아 있으면 해제 후 사용(use-after-free)과 같다. 크래시는 안 나고 **남의 데이터가 보일 뿐**이라 더 찾기 어렵다.

## Lab 34 — 구조체 레이아웃의 패딩
```
header:   Type 7, Length 엄청 큰 수(또는 음수), Seq 엉뚱한 값
Unsafe.SizeOf<PacketHeader>():   12   (1 + 4 + 2 = 7이 아님)
Marshal.SizeOf<PacketHeader>():  12
메모리 창(&header):  Type 다음에 빈 3바이트, Seq 뒤에 빈 2바이트
진짜 원인:  Sequential 레이아웃은 int를 4바이트 경계에 맞추려고 Type 뒤에 3바이트를 끼워 넣는다.
           그래서 Length를 1번이 아니라 4번 바이트부터 읽고, 본문도 7이 아니라 12번 바이트부터 자른다.
```
**수정 (택1):**
- `[StructLayout(LayoutKind.Sequential, Pack = 1)]` — 빈틈 없이 붙인다. 크기 7.
- 구조체를 쓰지 않고 위치를 직접 정해서 읽는다 (엔디언도 명시되어 더 안전):
  ```csharp
  byte type = packet[0];
  int length = BinaryPrimitives.ReadInt32LittleEndian(packet[1..]);
  ushort seq = BinaryPrimitives.ReadUInt16LittleEndian(packet[5..]);
  string text = Encoding.UTF8.GetString(packet.Slice(7, length));
  ```

**교훈:** 바이트 형식이 정해진 데이터(네트워크, 파일, 네이티브 구조체)를 구조체로 그대로 읽을 때는 **크기와 오프셋을 먼저 확인**한다. `Marshal.SizeOf`(마샬링 크기)와 `Unsafe.SizeOf`/`sizeof`(메모리 크기)는 다를 수도 있다. 예를 들어 `bool` 필드는 메모리에서는 1바이트지만 마샬링하면 4바이트(Win32 `BOOL`)다.

