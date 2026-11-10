# 해답 (스포일러!)

# 1부: 예외와 스택 트레이스

## Lab 1 — null 역참조, `!`로 눌러 둔 nullable 경고
```
멈춘 함수:  PrintPlayer           이상한 값: player == null
값의 출처:  ShowProfile ← FindPlayer가 못 찾아서 null 반환
진짜 원인:  OpenFriendList에서 "Doran"을 "Dorna"로 오타
```
**수정:** 오타를 고치고, `!`를 지워 null을 막는다. `Lab01_Profile.cs`의 `ShowProfile`과 `OpenFriendList`를(둘이 이어져 있다) 아래로 바꾼다. `OpenFriendList`는 `"Dorna"`를 `"Doran"`으로 고친 것뿐이다. `FindPlayer`, `PrintPlayer`, `Run`은 그대로 둔다.
```csharp
    static void ShowProfile(string name)
    {
        Player? player = FindPlayer(name);
        if (player is null)
        {
            Console.WriteLine(Text.L($"  {name}: 없는 플레이어", $"  {name}: 存在しないプレイヤー"));
            return;
        }
        PrintPlayer(player);
    }

    static void OpenFriendList()
    {
        ShowProfile("Kael");
        ShowProfile("Mira");
        ShowProfile("Doran");
    }
```
**교훈:** 죽은 곳(`PrintPlayer`)은 잘못이 없다. `!`는 검사가 아니라 **경고를 끄는 스위치**다. 컴파일러는 `FindPlayer`가 `Player?`를 돌려준다는 것을 알고 경고(CS8600)를 내려고 했다.

## Lab 2 — 대소문자만 다른 Dictionary 키
```
멈춘 함수:  [외부 코드] Dictionary.get_Item → 첫 내 코드: PriceOf(itemId = "Mana_Potion")
값의 출처:  CartTotal ← LoadCart ← SavedCart (1.0 클라이언트가 저장한 데이터)
진짜 원인:  Prices는 기본 비교자(대소문자 구분)인데, 저장된 키는 "Mana_Potion"
```
**수정:** 가격표를 대소문자를 가리지 않는 비교자로 만든다. `Lab02_Shop.cs`의 `Prices` 필드 선언(이니셜라이저의 `};`까지)을 아래로 바꾼다. 다른 메서드는 그대로 둔다.
```csharp
    static readonly Dictionary<string, int> Prices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["hp_potion"] = 50,
        ["mana_potion"] = 70,
        ["iron_sword"] = 1200,
        ["leather_boots"] = 300,
    };
```
다른 방법: 불러올 때 `ToLowerInvariant()`로 키를 정규화한다. 없는 아이템까지 대비하려면 `PriceOf`에서 `TryGetValue`로 찾고 경고를 남긴다.
**교훈:** `KeyNotFoundException` 메시지에는 못 찾은 **키가 그대로** 나온다. 메시지를 끝까지 읽자. 이미 배포된 데이터는 못 고치니 받는 쪽이 관대해야 한다.

## Lab 3 — 열거 중 컬렉션 수정
```
멈춘 곳:    CastExplosion의 foreach (외부 코드: List<T>.Enumerator.MoveNext)
이상한 값:  _enemies가 순회 도중 줄어듦
진짜 범인:  Despawn의 _enemies.Remove — 예외가 날 때는 이미 반환해서 호출 스택에 없음
```
**수정:** 사본을 순회한다. `Lab03_Enemies.cs`의 `CastExplosion`에서 `foreach (Enemy enemy in _enemies)` 줄을 아래 한 줄로 바꾼다. 루프 본문은 그대로 둔다.
```csharp
            foreach (Enemy enemy in _enemies.ToList())
```
다른 방법:
- 순회 중에는 죽은 적을 모아 두고, 루프가 끝난 뒤 `_enemies.RemoveAll(e => e.Hp <= 0)`
- 뒤에서부터 `for (int i = _enemies.Count - 1; i >= 0; i--)`

**교훈:** 예외는 수정한 순간이 아니라 **다음 `MoveNext`**에서 난다. 호출 스택은 "지금"만 보여 준다. 범인을 잡으려면 수정하는 줄에 중단점을 건다.

## Lab 4 — 박싱된 long을 (int)로 박싱 해제
```
멈춘 함수:  CreateParty
이상한 값:  config["maxPartySize"].GetType() == System.Int64
진짜 원인:  ParseConfig가 숫자를 long으로 박싱해 담았고, (int)obj 는 "박싱 해제"라 정확히 int일 때만 됨
```
**수정:** 박싱 해제가 아니라 숫자 변환을 한다. `Lab04_Config.cs`의 `CreateParty`를 아래로 바꾼다. `ParseConfig`와 `Run`은 그대로 둔다.
```csharp
    static Party CreateParty(Dictionary<string, object> config)
    {
        int maxSize = Convert.ToInt32(config["maxPartySize"]);
        return new Party(maxSize);
    }
```
`(int)(long)config["maxPartySize"]`도 된다. 더 좋게는 설정을 처음부터 형식이 있는 클래스로 파싱한다.
**교훈:** `(int)` 캐스트는 상황에 따라 **숫자 변환**일 수도 **박싱 해제**일 수도 있다. `object`에서 꺼낼 때는 후자다.

## Lab 5 — 자기 자신을 부르는 속성 setter
```
증상:       Stack overflow. Repeat N times: at BugHunt.Lab05+Wallet.set_Gold(Int32)
호출 스택:  set_Gold 가 수천 개
진짜 원인:  set => Gold = ...   (백킹 필드 _gold가 아니라 속성 자신에 대입)
```
**수정:** 백킹 필드에 대입한다. `Lab05_Wallet.cs`의 `Gold` 속성에서 `set => Gold = ...` 줄을 아래 한 줄로 바꾼다.
```csharp
            set => _gold = Math.Clamp(value, 0, MaxGold);
```
**교훈:** StackOverflow는 **잡을 수 없고** 프로세스가 끝난다. 디버거 없이 실행하면 런타임이 반복되는 프레임을 `Repeat N times`로 접어서 보여 준다. 스택이 한 함수로 가득 차면 재귀가 끝나지 않는 것.

## Lab 6 — AggregateException 안의 FormatException
```
멈춘 곳:    Run의 Parallel.ForEach (처리되지 않은 System.AggregateException)
내부 예외:  FormatException: The input string '??' was not in a correct format. ← Parse("swamp.loot")
진짜 원인:  catch (FormatException)는 AggregateException과 형식이 달라서 안 잡힘.
           게다가 실패 후 남은 파일을 처리하지 않을 수도 있어서 "파일 하나만 건너뛰기"가 안 됨
```
**수정:** 파일 하나 단위로 잡는다. `Lab06_LootFiles.cs`의 `Run`에서 `Parallel.ForEach`를 감싼 `try` 줄부터 `catch (FormatException ex)` 블록의 닫는 괄호 `}`까지(바깥 `try`/`catch` 전체. 이제 아무것도 잡지 못한다)를 아래로 바꾼다. 람다 안에서 `skipped++`를 그대로 쓰면 스레드 경쟁이 생기므로 `Interlocked.Increment`로 센다(랩 66 참고). `Run`의 나머지 줄은 그대로 둔다.
```csharp
        Parallel.ForEach(Files, file =>
        {
            try { loaded.Add(Parse(file.Key, file.Value)); }
            catch (FormatException ex)
            {
                Console.WriteLine(Text.L($"  고장 난 파일 건너뜀: {file.Key} ({ex.Message})", $"  壊れたファイルを飛ばしました: {file.Key} ({ex.Message})"));
                Interlocked.Increment(ref skipped);
            }
        });
```
더 나아가 `Parse`에서 `int.TryParse`로 예외 없이 처리할 수도 있다.
**교훈:** 병렬 API(`Parallel`, `Task.Wait`, `Task.Result`)는 예외를 **AggregateException으로 감싼다**. 예외 설정 창의 first chance 중단으로 진짜 throw 지점(작업 스레드)을 바로 볼 수 있다.

## Lab 7 — `throw ex;`가 스택 트레이스를 덮어씀
```
리포트 맨 위:   at BugHunt.Lab07.LoadSlot(Int32 index)   ← throw ex; 줄
실제 throw:     int.Parse ← ParseItem("potion:two") ← ParseInventory ← ParseSlot ← LoadSlot
진짜 원인:      throw ex; 는 예외를 "새로 던진" 것처럼 스택 트레이스를 이 줄에서 다시 시작함
숨긴 경고:      BugHunt.csproj 의 <NoWarn>CA2200</NoWarn>
```
**수정:** `throw ex;`를 `throw;`로 바꾼다. `Lab07_SaveLoad.cs`의 `LoadSlot`에서 `catch (Exception ex)` 블록(닫는 괄호까지)을 아래로 바꾼다. 그다음 `BugHunt.csproj`의 `<NoWarn>$(NoWarn);CA2200</NoWarn>` 줄을 지운다. 순서가 중요하다: 이 저장소는 경고를 오류로 다루므로(`TreatWarningsAsErrors`) `throw ex;`가 남아 있는 채로 `NoWarn`을 먼저 지우면 빌드가 실패한다. `throw ex;`를 쓰는 랩은 이 랩뿐이라, 고친 뒤에는 `NoWarn`을 지워도 빌드가 깨끗하다. (`NoWarn`을 그대로 두어도 채점은 통과하지만, 그러면 다음에 누가 또 `throw ex;`를 써도 컴파일러가 알려 주지 않는다.)
```csharp
        catch (Exception ex)
        {
            Console.WriteLine(Text.L($"  슬롯 {index + 1} 손상: {ex.GetType().Name}", $"  スロット {index + 1} 破損: {ex.GetType().Name}"));
            throw;   // 원래 스택 트레이스를 그대로 둔 채 다시 던진다
        }
```
정보를 덧붙이고 싶다면 `throw new SaveLoadException(Text.L($"슬롯 {index + 1}", $"スロット {index + 1}"), ex);`처럼 **내부 예외로 감싼다**(이 랩에는 그런 예외 형식이 없으므로 직접 만들어야 한다).
**교훈:** 스택 트레이스는 예외가 **마지막으로 throw된 곳**부터 기록된다. 다시 던질 때는 `throw;`. 경고를 끄면 그 경고가 지키던 버그도 함께 숨는다.

---

## Lab 8 — `ToString`이 `$"{this}"`로 자기를 불러 무한 재귀
```
호출 스택:  DamageLabel.ToString → DamageLabel.ToString → … (수천 단계)
진짜 원인:  $"{this} {Text.L("피해", "ダメージ")}"의 {this}는 this.ToString()을 부른다. 그게 지금 실행 중인 ToString이다.
           무한 재귀로 스택을 다 쓰면 StackOverflowException으로 프로세스가 즉시 죽는다.
```
**수정:** `ToString`에서 그 객체 자신을 보간하지 말고 필드를 직접 씁니다. `Lab08_DamageLabel.cs`의 `DamageLabel`에서 `ToString` 줄을 아래 한 줄로 바꿉니다.
```csharp
        public override string ToString() => $"{Amount} {Text.L("피해", "ダメージ")}";
```
**교훈:** `ToString`, `GetHashCode`, `Equals`, 연산자 오버로드, 속성 `get` 안에서 **자기 자신을 다시 부르지** 않도록 조심합니다. 흔한 형태: `ToString`에서 `$"{this}"`, `Equals`에서 `this == other`(→ `==` 연산자가 다시 `Equals`), `get`에서 같은 속성 대입. 랩 5(지갑)의 `set => Gold = ...`도 같은 뿌리입니다. 증상(프로그램이 조용히 사라짐)이 같으면 호출 스택부터 보세요.

---

# 2부: 흐름 추적

## Lab 9 — 루프 변수 캡처
```
기대: CastSlot(1), (2), (3)        실제: CastSlot(4) 세 번
처음 어긋난 곳: 람다는 slot의 "값"이 아니라 "변수"를 캡처함.
              for의 slot은 루프 전체에서 변수 하나(DisplayClass 객체 하나)이고, 루프가 끝나면 4
```
**수정:** 루프 안에서 새 변수에 복사한다. `Lab09_Hotkeys.cs`의 `BindHotkeys`에서 `for` 루프 안의 `hotkeys[key] = () => CastSlot(slot);` 한 줄을 아래 두 줄로 바꾼다.
```csharp
            int thisSlot = slot;
            hotkeys[key] = () => CastSlot(thisSlot);
```
**교훈:** C#의 람다는 변수를 **참조로** 캡처한다. `foreach` 변수는 C# 5부터 바퀴마다 새 변수라 안전하지만, `for`의 변수는 아니다. 지역 창에서 람다의 `this`를 펼쳐 보면 `<>c__DisplayClass`가 보이고, 개체 ID로 세 람다가 같은 캡처 객체를 공유하는 것을 확인할 수 있다.

## Lab 10 — LINQ 지연 실행이 두 번 열거됨
```
기대: Send 3번          실제: 6번
처음 어긋난 곳: rewards.Count() 에서 3번, foreach 에서 또 3번
진짜 원인:     Select(...)는 결과 목록이 아니라 "쿼리"를 돌려준다. 열거할 때마다 Send가 다시 실행됨
```
**수정:** 부수 효과가 있는 쿼리는 즉시 실체화한다. `Lab10_Ranking.cs`의 `Run`에서 `IEnumerable<Mail> rewards = Ranking`부터 `.Select(e => mailbox.Send(e.Name, 500));`까지의 문장(4줄)을 아래로 바꾼다. `Run`의 나머지 줄은 그대로 둔다.
```csharp
        IEnumerable<Mail> rewards = Ranking
            .OrderByDescending(e => e.Score)
            .Take(3)
            .Select(e => mailbox.Send(e.Name, 500))
            .ToList();
```
더 좋게는 Select 안에서 부수 효과를 내지 않고, 대상 목록을 만든 뒤 `foreach`로 보낸다.
**교훈:** `IEnumerable<T>` 변수는 **목록이 아니라 만드는 방법**일 수 있다. 디버거의 "결과 보기"도 열거를 실행해서 상태를 바꾼다.

## Lab 11 — readonly struct 필드의 방어적 복사
```
TryUse 안:   this.Current 100 → 70 (기대대로)
Sprint 로 나와서: _stamina.Current 100
진짜 원인:   _stamina는 readonly 필드이고 Stamina는 변경 가능한 struct.
            컴파일러는 readonly 필드를 지키려고 메서드 호출 때마다 복사본을 만들어 그 복사본을 바꿈
```
**수정:** `readonly`를 지워서 사본이 아니라 필드 자체를 바꾸게 한다. `Lab11_Stamina.cs`의 `Runner`에서 `readonly Stamina _stamina = new(100);` 줄을 아래 한 줄로 바꾼다.
```csharp
        Stamina _stamina = new(100);
```
다른 방법: `Stamina`를 `class`로 바꾼다 / struct를 불변으로 만들고 `_stamina = _stamina.Use(30)`처럼 새 값을 받는다.
**교훈:** "변경 가능한 struct"는 복사의 함정이 많다. 처음부터 `readonly struct`로 선언하면, 필드를 바꾸는 메서드 자체가 **컴파일 오류**가 되어 이런 실수를 막는다.

## Lab 12 — 박싱된 값의 `!=`는 참조 비교
```
조건부 중단점 (key == "fullscreen"):
    before[key]          true (object)
    after[key]           true (object)
    before[key] != after[key]                 true  ← 둘 다 true인데 "다르다"
    before[key].Equals(after[key])            true
    ReferenceEquals(before[key], after[key])  false
진짜 원인:   변수 형식이 object라서 ==/!= 는 값이 아니라 "같은 상자인가"를 비교. Snapshot마다 새로 박싱됨
```
**수정:** 값으로 비교한다. `Lab12_SettingsDiff.cs`의 `ChangedKeys`에서 `if (before[key] != after[key])` 줄을 아래 한 줄로 바꾼다 (정적 `object.Equals`는 null도 안전하게 처리하고 값의 `Equals`를 부른다).
```csharp
            if (!Equals(before[key], after[key]))
```
**language가 맞게 나온 이유:** 두 스냅샷이 **같은 string 객체**(`settings.Language`)를 담았기 때문에 참조 비교도 우연히 같았다. 설정을 파일에서 다시 읽었다면 내용이 같은 다른 string 객체가 되어 "바뀌었다"고 나왔을 것이다.
**교훈:** `==`의 의미는 **변수의 컴파일 시점 형식**으로 정해진다. `object`로 들고 있으면 int도 bool도 참조 비교다.

## Lab 13 — float 동등 비교
```
조건부 중단점 (XpBar > 0.95f):
    XpBar              1.0000001
    XpBar == 1.0f      false
    (double)XpBar      1.0000001192092896
진짜 원인:  0.1f 는 2진수로 정확하지 않아 열 번 더하면 1.0f 가 되지 않음. 화면의 P0 서식은 반올림해서 "100%"
```
**수정:** 최소한 범위로 비교한다. `Lab13_XpBar.cs`의 `GainXp`에서 `if (XpBar == 1.0f)` 줄을 아래 한 줄로 바꾼다.
```csharp
            if (XpBar >= 1.0f)
```
근본적으로는 경험치를 **정수**로 들고(`int xp`, `int xpToNext`), 바의 비율은 표시할 때만 계산한다.
**교훈:** 부동소수점은 `==`로 비교하지 않는다. 화면에 보이는 숫자는 서식이 반올림한 값일 수 있다.

## Lab 14 — 중간 계산의 int 오버플로
```
조건부 중단점 (damage == 30000000):
    damage * 100           -1294967296
    checked(damage * 100)  System.OverflowException
    int.MaxValue           2147483647
진짜 원인:  30,000,000 × 100 = 3,000,000,000 > int.MaxValue. C#은 기본이 unchecked라 조용히 한 바퀴 돎
```
**수정:** 중간값을 `long`으로 계산한다. `Lab14_BossContribution.cs`의 `ContributionPercent` 줄을 아래 한 줄로 바꾼다.
```csharp
    static int ContributionPercent(int damage, int totalDamage) => (int)((long)damage * 100 / totalDamage);
```
기여도를 `double`로 계산하는 것도 방법이다.
**교훈:** 결과는 int에 들어가도 **중간값이 넘칠 수 있다.** Debug 빌드에서 `<CheckForOverflowUnderflow>true</CheckForOverflowUnderflow>`를 켜 두면 이런 곳에서 바로 예외로 멈춘다.

## Lab 15 — string 불변성, Replace 결과를 버림
```
Replace 줄을 F10:  nickname 그대로. 자동 창에 "String.Replace 반환  "  ***Slayer "" 가 보였다가 사라짐
진짜 원인:  string은 불변. Replace는 새 문자열을 돌려줄 뿐이고, 그 반환값을 아무 데도 저장하지 않음
           (Trim은 return nickname.Trim(); 으로 반환값을 썼기 때문에 잘 동작함)
```
**수정:** 반환값을 저장한다. `Lab15_NicknameFilter.cs`의 `Sanitize`에서 `nickname.Replace(word, "***", StringComparison.OrdinalIgnoreCase);` 줄을 아래 한 줄로 바꾼다.
```csharp
                nickname = nickname.Replace(word, "***", StringComparison.OrdinalIgnoreCase);
```
**교훈:** `string`, `DateTime`, `ImmutableList` 같은 불변 형식의 메서드는 **새 값을 돌려준다.** CA1806을 켜면 반환값을 버리는 호출을 경고로 잡는다.

## Lab 16 — 현재 문화권으로 쓰고 InvariantCulture로 읽음
```
de-DE 일 때 file:  "moveSpeed=1,5\nmouseSensitivity=0,75"
NumberDecimalSeparator (de-DE)  ","
double.Parse("1,5", InvariantCulture)   15   ← Invariant에서 ","는 천 단위 구분 기호
진짜 원인:  $"{double}" 보간은 CurrentCulture로 숫자를 씀. 읽는 쪽만 InvariantCulture
```
**수정:** 쓰는 쪽도 고정한다. `Lab16_SettingsFile.cs`의 `Save`를 아래로 바꾼다. `using System.Globalization;`은 파일에 이미 있고, `Load`와 `SaveAndReload`는 그대로 둔다.
```csharp
    static string Save(Options o) =>
        string.Create(CultureInfo.InvariantCulture, $"moveSpeed={o.MoveSpeed}\nmouseSensitivity={o.MouseSensitivity}");
```
`FormattableString.Invariant($"...")`로 감싸거나 값마다 `o.MoveSpeed.ToString(CultureInfo.InvariantCulture)`를 써도 같다.
**교훈:** 파일·네트워크·세이브처럼 **기계가 읽는 텍스트**는 쓰기와 읽기 모두 InvariantCulture로. 화면에 보여 줄 때만 CurrentCulture. 한국어·영어 PC에서는 재현되지 않아서 "내 PC에서는 되는" 버그가 된다. CA1305가 잡아 준다.

## Lab 17 — DateTime 빼기는 Kind를 무시한다
```
EndsAtUtc       2026-11-03 15:00  Kind=Utc
clock.Now       2026-11-03 21:00  Kind=Unspecified (서울 시각)
clock.UtcNow    2026-11-03 12:00  Kind=Utc
EndsAtUtc - clock.Now     -06:00:00
EndsAtUtc - clock.UtcNow  03:00:00
진짜 원인:  보여 주기용 현지 시각으로 계산함. DateTime 끼리 빼기는 Kind와 시간대를 보지 않고 숫자만 뺌
```
**수정:** UTC끼리 계산한다. `Lab17_EventTimer.cs`의 `LimitedEvent`에서 `Remaining` 줄을 아래 한 줄로 바꾼다.
```csharp
        public TimeSpan Remaining(PlayerClock clock) => EndsAtUtc - clock.UtcNow;
```
시간대가 섞이는 코드라면 `DateTimeOffset`을 쓰면 빼기가 오프셋을 반영한다.
**교훈:** 계산은 UTC끼리, 변환은 **보여 주기 직전에만**. 조사식에서 `Kind`를 펼쳐 보면 바로 보인다.

## Lab 18 — Equals와 GetHashCode가 서로 다른 규칙
```
Equals 중단점:  "kael" 을 넣을 때 걸리지 않음 (HashSet이 Equals까지 가지도 않음)
조사식:
    new PlayerName("Kael").Equals(new PlayerName("kael"))   true
    new PlayerName("Kael").GetHashCode()   12345...
    new PlayerName("kael").GetHashCode()   -6789...   ← 다름
진짜 원인:  Equals는 대소문자 무시, GetHashCode는 대소문자 구분(string.GetHashCode)
```
**수정:** 해시도 대소문자를 무시한다. `Lab18_FriendList.cs`의 `PlayerName`에서 `GetHashCode` 줄을 아래 한 줄로 바꾼다.
```csharp
        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
```
**교훈:** `Equals`가 true면 **GetHashCode도 반드시 같아야 한다.** HashSet/Dictionary는 해시로 칸을 먼저 고르고 그 칸 안에서만 Equals를 부른다. **걸리지 않는 중단점**도 중요한 단서다.

## Lab 19 — record `with`는 얕은 복사
```
개체 ID:   basePreset.Skills {$1}, custom.Skills {$1}  ← 같은 List
ReferenceEquals(basePreset, custom)                false (레코드는 새로 만들어짐)
ReferenceEquals(basePreset.Skills, custom.Skills)  true  (List는 주소만 복사됨)
```
**수정:** 바꿀 컬렉션은 함께 복사한다. `Lab19_Presets.cs`의 `Customize`에서 `CharacterPreset custom = basePreset with { Name = name };` 줄을 아래 한 줄로 바꾼다.
```csharp
        CharacterPreset custom = basePreset with { Name = name, Skills = [.. basePreset.Skills] };
```
근본적으로는 레코드에 `List<T>` 대신 `IReadOnlyList<T>`나 `ImmutableList<T>`를 두어, 복사본을 바꾸려면 새 컬렉션을 만들 수밖에 없게 한다.
**교훈:** `with`는 "새 레코드 + 속성 값 그대로 복사"다. 참조 형식 속성은 **공유**된다.

## Lab 20 — `x = x++;`는 x를 그대로 둔다
```
OnHit 첫 줄 전후:   _combo 0 → 0, 0 → 0 ...   Best 0
진짜 원인:  _combo = _combo++;  (1) 값 0을 읽고 (2) _combo를 1로 올린 뒤 (3) 식의 값 0을 다시 대입
```
**수정:** 대입하지 않는다. `Lab20_Combo.cs`의 `OnHit`에서 `_combo = _combo++;` 줄을 아래 한 줄로 바꾼다 (`_combo += 1;`, `++_combo;`도 된다).
```csharp
            _combo++;
```
**교훈:** C#은 피연산자를 **왼쪽에서 오른쪽으로** 평가하고 후위 `++`의 값은 "올리기 전 값"이라고 정해 두었다. 그래서 `x = x++;`는 언제나 x를 그대로 둔다. C++처럼 컴파일러마다 달라지는 미지정 동작이 아니라, 정해진 대로 틀린 코드다.

## Lab 21 — 후위 `++`가 올리기 전 값을 돌려줌
```
불러온 명단:  1 Kael, 2 Mira, 5 Doran      _lastId = 5 ("지금까지 쓴 가장 큰 번호")
Recruit("Lia"):  NextId() → 5 (돌려준 뒤 _lastId = 6)   ← Doran과 겹침
Whisper(5) → 목록에서 먼저 찾은 Doran에게 감
진짜 원인:  NextId() => _lastId++;  후위 ++ 는 올리기 전 값을 돌려준다
```
**수정:** 전위 `++`로 올린 뒤의 값을 돌려준다. `Lab21_MemberId.cs`의 `NextId` 줄을 아래 한 줄로 바꾼다.
```csharp
        int NextId() => ++_lastId;
```
`{ _lastId += 1; return _lastId; }`도 된다. 생성자에서 `_lastId = Max + 1`로 바꾸려면 변수의 뜻도 "다음에 쓸 번호"로 바꾸고 이름도 맞춘다.
**가짜 수정:** 귓속말을 `Last(m => m.Id == id)`로 찾게 하면 환영 인사는 맞게 가지만 번호는 여전히 겹친다. 기본 케이스의 번호(6, 7)를 박아 넣으면 숨은 케이스(다른 번호, 다른 인원)에서 틀린다.
**교훈:** 후위 `x++`의 값은 올리기 전, 전위 `++x`의 값은 올린 뒤다. `if (hits++ == 3)`도 같은 함정으로, 네 번째가 아니라 다섯 번째 타격에서야 참이 된다. 값을 쓰는 곳에서는 증감을 따로 한 줄로 빼면 헷갈릴 일이 없다.

## Lab 22 — 조건 안의 `--`와 byte 넘침
```
TryDrink (Potions == 0):  if (Potions-- == 0)  → 비교는 참, 그런데 감소도 일어남: 0 - 1 = 255 (byte)
다음 누름부터:  255, 254, ... 마심   ← 물약이 252개 생겼다
진짜 원인:  검사와 감소를 한 식에 섞음. byte 는 unchecked 라 0 아래로 내려가면 조용히 255
```
**수정:** 검사와 감소를 나눈다. `Lab22_Potions.cs`의 `Bag.TryDrink`를 아래로 바꾼다.
```csharp
        public bool TryDrink()
        {
            if (Potions == 0)
                return false;
            Potions--;
            return true;
        }
```
**교훈:** 조건식 안에 `++`/`--`를 넣으면 조건이 참이든 거짓이든 값이 바뀐다. 검사와 변경은 나눈다. 정수 넘침은 기본이 unchecked라 조용히 한 바퀴 돈다. 넘치면 안 되는 계산은 `checked(...)`로 감싸거나 프로젝트에 `CheckForOverflowUnderflow`를 켜서 `OverflowException`으로 드러나게 한다 (랩 14의 int 곱셈과 같은 규칙).

## Lab 23 — `||` 대신 `|`: 단락 평가가 사라짐
```
LoadFromDisk 중단점:  캐시에 이미 있는 "potion" 인데도 멈춤 (7칸에 7번)
Get:  if (_cache.TryGetValue(id, out icon) | LoadFromDisk(id, out icon))
진짜 원인:  bool 에 쓴 | 는 컴파일되지만 양쪽을 언제나 계산한다. 캐시에 있어도 디스크를 읽는다
```
**수정:** 단락 평가하는 `||`를 쓴다. `Lab23_IconCache.cs`의 `Get`에서 `if (_cache.TryGetValue(id, out string? icon) | LoadFromDisk(id, out icon))` 줄을 아래 한 줄로 바꾼다.
```csharp
            if (_cache.TryGetValue(id, out string? icon) || LoadFromDisk(id, out icon))
```
**교훈:** `||`/`&&`는 왼쪽만으로 답이 정해지면 오른쪽을 계산하지 않는다(단락 평가). `|`/`&`는 `bool`에도 쓸 수 있지만 양쪽을 다 계산한다. 오른쪽에 부작용(디스크 읽기, 카운터, 로그)이 있으면 매번 일어나고, `if (target == null | target.Hp <= 0)`처럼 왼쪽이 오른쪽을 지켜 주는 식이면 `NullReferenceException`이 난다. 정수 플래그에 `&&` 대신 `&`를 쓰는 C++식 실수는 C#에서는 `int`가 `bool`로 바뀌지 않아 컴파일러가 막아 준다.

---

# 3부: 컨테이너와 컬렉션

## Lab 24 — Length와 Count를 섞어 쓴 범위 밖 인덱스
```
예외:        ArgumentOutOfRangeException: Index was out of range. Must be non-negative and less than the size of the collection. (Parameter 'index')
멈춘 곳:     Settle의 clearSeconds[i]   (i == 3)
조사식:      stages.Length == 4, clearSeconds.Count == 3
진짜 원인:  루프는 퀘스트 전체 단계 수(stages.Length)만큼 도는데, 읽는 것은 지금까지 깬 단계의 목록(clearSeconds)
```
**수정:** 루프는 **실제로 읽는 컬렉션**의 크기로 돈다. `Lab24_QuestRewards.cs`의 `Settle`에서 `for (int i = 0; i < stages.Length; i++)` 줄을 아래 한 줄로 바꾼다.
```csharp
        for (int i = 0; i < clearSeconds.Count; i++)
```
**교훈:** 배열은 `IndexOutOfRangeException`, `List`는 `ArgumentOutOfRangeException`을 낸다. 메시지에 인덱스 값은 없으니 지역 창에서 `i`와 크기를 직접 비교한다. 이런 버그는 "다 깬 퀘스트"처럼 두 크기가 우연히 같은 데이터로만 시험하면 숨는다. 경계(0개, 일부, 전부)를 시험하자. (`<=`로 한 칸 넘어가는 off-by-one도 같은 모습이다.)

## Lab 25 — `List<struct>` 인덱서가 돌려준 복사본을 고침
```
TakeDamage 안:   Hp 100 → 70   (줄어든다)
AreaAttack로 나와서 party[i].Hp:   100   (그대로)
진짜 원인:  List<T>의 인덱서는 get 메서드라서 struct의 "복사본"을 돌려준다. TakeDamage는 그 임시 복사본을 고치고 버린다.
           party[i].Hp -= damage 는 컴파일러가 CS1612로 막지만, 메서드 호출은 막지 않는다.
```
**수정:** 꺼내서 고치고 다시 넣는다. `Lab25_PartyHp.cs`의 `AreaAttack`을 아래로 바꾼다(루프 본문이 세 줄이 되므로 중괄호로 감싼다). `Member`와 `Run`은 그대로 둔다.
```csharp
    static void AreaAttack(List<Member> party, int damage)
    {
        for (int i = 0; i < party.Count; i++)
        {
            Member member = party[i];   // 사본을 꺼내
            member.TakeDamage(damage);  // 고치고
            party[i] = member;          // 다시 넣는다
        }
    }
```
다른 방법:
- `CollectionsMarshal.AsSpan(party)[i].TakeDamage(damage);` — `Span`의 인덱서는 **칸 자체(ref)**를 돌려준다. (그 사이에 List에 넣거나 빼면 안 된다. `using System.Runtime.InteropServices;`가 필요하다)
- 바뀌는 데이터라면 `class`로 만든다. 또는 `readonly struct`로 만들어 바꾸는 메서드 자체를 없앤다.

**교훈:** 배열의 `arr[i]`는 칸 자체(ref)지만 `List`, `Dictionary`의 인덱서와 속성은 **값을 돌려주는 메서드**다. 바뀌는 struct는 복사될 때마다 버그의 씨앗이 된다 (랩 11의 `readonly` 필드 방어적 복사와 같은 뿌리).

## Lab 26 — Dictionary 키로 쓰는 동안 해시가 바뀜 (가변 키)
```
TryGetValue:   false
조사식 storage:  키 목록에 이름이 바뀐 길드가 그대로 보인다
storage.Keys.Contains(g):   true   (하나씩 Equals로 비교)
storage.ContainsKey(g):     false  (해시로 칸을 찾음)
GetHashCode 중단점:  넣을 때와 찾을 때 값이 다름
진짜 원인:  GetHashCode가 Tag로 계산되는데, 키로 넣은 뒤에 Tag를 바꿈. 객체는 옛 해시의 칸에 남아 있다.
```
**수정:** 같은 길드인지는 **바뀌지 않는 값**으로 판단한다. `Lab26_GuildRename.cs`의 `Guild`에서 `Equals`와 `GetHashCode` 두 줄(이어져 있다)을 아래로 바꾼다.
```csharp
        public override bool Equals(object? obj) => obj is Guild g && g.Id == Id;
        public override int GetHashCode() => Id.GetHashCode();
```
태그를 키로 써야 한다면, 바꿀 때 딕셔너리에서 `Remove`하고 바꾼 뒤 다시 넣는다.
**교훈:** 해시 기반 컬렉션(`Dictionary`, `HashSet`)의 키는 넣은 뒤에 **해시에 쓰이는 값이 바뀌면 안 된다.** 랩 18(Equals와 GetHashCode가 서로 어긋남)과 짝이다. `record`는 모든 속성으로 해시를 만들기 때문에, 속성을 바꿀 수 있는 record를 키로 쓰면 이 버그가 된다.

## Lab 27 — Dictionary를 여러 스레드에서 동시에 고침
```
실행할 때마다:   기록 수가 199,8xx처럼 모자라거나, IndexOutOfRangeException / InvalidOperationException
                ("Operations that change non-concurrent collections must have exclusive access...")
병렬 스택:      여러 전투 스레드가 동시에 Dictionary.TryInsert, Resize 안에 있음
진짜 원인:  Dictionary는 한 번에 한 스레드만 고친다는 가정으로 만들어졌다. 내부 배열을 늘리는(Resize) 도중에 다른 스레드가 넣으면
           항목이 사라지거나 내부 연결이 꼬인다. 키가 겹치지 않아도 같다.
```
**수정:** `Lab27_KillLog.cs`의 `KillLog` 클래스를 아래로 바꾼다. 파일 맨 위(`namespace BugHunt;` 위)에 `using System.Collections.Concurrent;`를 넣는다. `Run`은 그대로 둔다.
```csharp
using System.Collections.Concurrent;

    class KillLog
    {
        // 여러 스레드가 동시에 고쳐도 되는 사전
        readonly ConcurrentDictionary<int, int> _kills = new();

        public void Record(int monsterId, int worker) => _kills[monsterId] = worker;

        public int Count => _kills.Count;
    }
```
다른 방법: `Dictionary`를 그대로 두고 고치는 곳을 `lock (_kills) _kills[monsterId] = worker;`로 감싸거나(그러면 `Count`도 같은 `lock` 안에서 읽는다), 전투 스레드마다 따로 모았다가 끝난 뒤 한 스레드에서 합친다(가장 빠르다).

**교훈:** .NET Framework의 `Dictionary`는 이렇게 꼬이면 **무한루프**(CPU 100%로 멈춤)에 빠지기도 했다. 지금의 .NET은 꼬임을 일부 감지해 예외를 내지만, 항목이 조용히 사라지는 것은 막지 못한다. 결과가 실행마다 다르면 랩 66처럼 경쟁 조건부터 의심한다.

## Lab 28 — 불안정 정렬 (List.Sort)
```
점수 순서:      맞다 (3000, 3000, ..., 2000, ...)
같은 점수끼리:  P04 P20 P08 ... 처럼 들어온 순서가 아님
16개 이하로 줄이면:  맞게 나온다 (작은 구간은 삽입 정렬이라 우연히 안정)
진짜 원인:  List.Sort는 불안정 정렬(introsort). 비교자가 0을 돌려준 원소들의 원래 순서를 지키지 않는다.
```
**수정:** 안정 정렬인 LINQ를 쓴다. `Lab28_Leaderboard.cs`의 `SortLeaderboard`를 아래로 바꾼다. `Run`은 받은 목록이 그 자리에서 정렬되기를 기대하므로, 정렬한 결과를 같은 목록에 다시 채운다. `Run`은 그대로 둔다.
```csharp
    static void SortLeaderboard(List<Entry> board)
    {
        // OrderByDescending 은 안정 정렬: 점수가 같으면 들어온 순서(먼저 달성한 순서)를 지킨다
        List<Entry> sorted = [.. board.OrderByDescending(e => e.Score)];
        board.Clear();
        board.AddRange(sorted);
    }
```
다른 방법: 같을 때의 규칙을 비교자에 넣는다. 달성 시각이나 들어온 순서를 `Entry`에 담아 두 번째 키로 비교한다.

**교훈:** "같으면 먼저 온 사람"처럼 같은 값의 순서에 의미가 있으면, 그 규칙을 **명시**한다. 비교자는 일관되어야 한다. `(a, b) => a.Score > b.Score ? -1 : 1`(같을 때 0을 안 줌)이나 `a.Score - b.Score`(넘침, 랩 14)처럼 어긋난 비교자는 결과가 실행·크기마다 흔들리거나 `InvalidOperationException: IComparer.Compare() method returns inconsistent results`를 낸다.

## Lab 29 — yield 메서드의 예외는 열거할 때 난다
```
예외:        FormatException: 드롭 테이블 줄이 이상하다: "sword=?"
호출 스택:   <ParseDropTable>d__1.MoveNext() ← Enumerable.Sum ← Lab29.Run   (LoadDropTable은 없다)
콘솔:        "고장 난 드롭 테이블, 기본 테이블 사용"이 한 번도 안 찍힘
진짜 원인:  yield return이 있는 메서드는 부르는 순간 본문을 하나도 실행하지 않는다. LoadDropTable의 try는 "열거할 객체"만 받아서 바로 끝나고,
           실제 파싱(과 예외)은 나중에 Sum이 열거할 때 일어난다.
```
**수정:** 잡고 싶은 곳에서 끝까지 열거한다. `Lab29_DropTable.cs`의 `LoadDropTable` 메서드를 통째로 아래로 바꾼다(`try` 안의 `return`에 `.ToList()`가 붙는다). 파일 아래쪽 힌트 주석에도 `return ParseDropTable(text);`가 인용돼 있으므로, 일괄 찾아 바꾸기가 아니라 이 메서드만 고쳐 쓴다. `ParseDropTable`과 `Run`은 그대로 둔다.
```csharp
    static IEnumerable<Drop> LoadDropTable(string text)
    {
        try
        {
            return ParseDropTable(text).ToList();   // 여기서 끝까지 열거하므로 예외도 이 try 안에서 난다
        }
        catch (FormatException ex)
        {
            Console.WriteLine(Text.L($"  고장 난 드롭 테이블, 기본 테이블 사용: {ex.Message}", $"  壊れたドロップテーブル、既定のテーブルを使用: {ex.Message}"));
            return DefaultTable;
        }
    }
```
**교훈:** 지연 실행되는 것은 LINQ(랩 10, 30)만이 아니다. `yield` 메서드의 **예외, 로그, 파일 열기 같은 부수 효과**는 모두 "열거하는 순간, 열거하는 곳"에서 일어난다. 인자 검사처럼 즉시 실패해야 하는 일은 `yield` 없는 바깥 메서드에서 먼저 하고, 열거는 안쪽 지역 함수에 맡긴다.

## Lab 30 — ToList() 없이 남긴 "스냅샷"
```
Enter의 _roster:      WhereListIterator (조사식에서 "결과 보기"를 눌러야 내용이 나온다)
m => m.Online 중단점:  Enter에서도, RewardRecipients에서도 걸림
진짜 원인:  _roster는 입장할 때의 명단이 아니라 "접속 중인 사람"이라는 조건이다. 보상 줄 때 열거하면 그때의 길드를 다시 걸러서,
           나간 사람은 빠지고 나중에 들어온 사람(Newbie 포함)이 들어간다.
```
**수정:** 그 순간의 명단을 찍어 둔다. `Lab30_RaidRoster.cs`의 `Enter`에서 `_roster = guild.Where(m => m.Online);` 줄을 아래 한 줄로 바꾼다.
```csharp
            _roster = guild.Where(m => m.Online).ToList();
```
**교훈:** `Where`, `Select`, `Dictionary.Keys`/`Values`, `List.AsReadOnly()`는 원본을 비추는 **창문**이다. 원본이 바뀌면 보이는 것도 바뀐다. 그 순간의 사진이 필요하면 `ToList()`/`ToArray()`로 복사한다. 단 복사되는 것은 목록뿐이고 `Member` 객체는 여전히 공유된다 (얕은 복사, 랩 19).

---

# 4부: C# 언어 함정

## Lab 31 — `string.GetHashCode`를 저장했다가 다음 실행에서 비교
```
조사식 "화염구".GetHashCode():   실행할 때마다 다른 값 (예: 1182412346 → -503617214)
savedHashes:                     지난 세션 값(이 랩에서는 고정된 안정 해시로 대신함)
진짜 원인:  .NET Core 이후 문자열 해시는 프로세스마다 무작위 시드를 쓴다(해시 충돌 공격 방지).
           해시는 같은 프로세스 안의 Dictionary/HashSet을 위한 값이지, 저장하거나 보내는 식별자가 아니다.
```
**수정:** 해시가 아니라 **스킬 이름(또는 고정 Id)** 자체를 저장하고 비교합니다. 이 랩에서는 세이브 쪽이 이미 안정 해시(`OldSessionHash`)로 저장돼 있으므로, 비교하는 쪽도 같은 함수를 쓰면 됩니다. `Run`에서 `starred`를 만드는 줄 하나만 바뀝니다.
```csharp
        string[] starred = [.. allSkills.Where(s => savedHashes.Contains(OldSessionHash(s)))];
```
이름을 저장하는 쪽으로 고치려면 `savedHashes` 대신 이름 집합을 씁니다(그러면 `OldSessionHash`는 쓰이지 않으므로 지워도 됩니다).
```csharp
        HashSet<string> savedNames = [.. favorites];
        string[] starred = [.. allSkills.Where(savedNames.Contains)];
```
해시가 꼭 필요하면 FNV-1a, `XxHash64`(`System.IO.Hashing`), SHA-256 같은 **정의가 고정된 해시**를 직접 씁니다.
**교훈:** `GetHashCode`의 계약은 "같은 프로세스에서, 같은 값이면 같은 해시"뿐입니다. 프로세스, 머신, .NET 버전을 넘어가는 순간 의미가 없습니다. `HashCode.Combine`도 같은 이유로 실행마다 다릅니다.

## Lab 32 — `a + b ?? 0`은 `(a + b) ?? 0`
```
조사식 baseDamage + comboBonus:           null   (형식 int?)
조사식 (baseDamage + comboBonus) ?? 0:     0
조사식 baseDamage + (comboBonus ?? 0):     80
진짜 원인:  ??는 +보다 우선순위가 낮다. int + int? 는 "리프트 연산자"로 int? 가 되고,
           한쪽이 null이면 결과도 null이다. 그 null에 ?? 0 이 붙어 기본 피해까지 0이 된다.
```
**수정:** 괄호로 `??`를 먼저 계산한다. `Lab32_ComboBonus.cs`의 `TotalDamage`에서 `int total = baseDamage + comboBonus ?? 0;` 줄을 아래 한 줄로 바꾼다.
```csharp
        int total = baseDamage + (comboBonus ?? 0);
```
`baseDamage + comboBonus.GetValueOrDefault()`도 된다.
**교훈:** `??`, `?:`, `=>`처럼 우선순위가 낮은 연산자를 산술식 안에 쓸 때는 항상 괄호를 칩니다. nullable끼리의 `+ - * /`, `< >`는 null이 있으면 각각 null, `false`가 됩니다(`<`와 `>=`가 둘 다 `false`일 수 있음).

## Lab 33 — 범위 밖 enum 값이 `_ =>`로 기본 등급이 됨
```
등급 코드 9 → (Rarity)9 → 콘솔에 "9"
조사식 Enum.IsDefined((Rarity)9):  false
조사식 Weight((Rarity)9):           100 (Common과 같은 값)
진짜 원인:  enum은 이름 붙은 정수일 뿐이라 어떤 정수든 캐스트된다(예외 없음).
           switch의 _ => 100 이 "모르는 값"까지 Common 취급한다.
```
**수정:** 모르는 값에는 가중치를 주지 않습니다. `Lab33_DropRarity.cs`의 `Weight` switch 식에서 마지막 갈래 `_ => 100,` 줄을 아래 한 줄로 바꿉니다.
```csharp
        _ => 0,
```
또는 받을 때 `Enum.IsDefined`로 먼저 걸러 건너뜁니다. `_ => throw new ArgumentOutOfRangeException(...)`은 이렇게 먼저 거를 때만 씁니다 (지금 `Run`은 `Weight`를 먼저 부르므로 throw만 바꾸면 코드 9에서 죽습니다).
**교훈:** 바깥에서 들어온 정수(패킷, 세이브, 설정)를 enum으로 바꿀 때는 범위 검사가 필요합니다. 비슷한 함정: `default(Rarity)`는 첫 멤버(0)라서 "지정 안 함"이 조용히 `Common`이 되므로, 0에는 `None`/`Unknown`을 두는 것이 안전합니다. `[Flags]` enum에서 `x.HasFlag(None)`은 항상 `true`입니다.

## Lab 34 — 값으로 넘긴 struct 커서는 바깥에서 진행되지 않음
```
TakeTurn 안 cursor._index:  매번 -1 → 0
Run 의 cursor._index:       계속 -1
진짜 원인:  TurnCursor는 struct라 TakeTurn(TurnCursor cursor)가 호출마다 복사본을 받는다.
           MoveNext는 복사본만 진행시키고 버려진다.
```
**수정:** 커서를 `ref`로 넘긴다. `Lab34_TurnOrder.cs`에서 두 곳을 바꿉니다.

1. `TakeTurn`을 아래로 바꿉니다.
```csharp
    static string TakeTurn(ref TurnCursor cursor)
    {
        cursor.MoveNext();
        return cursor.Current;
    }
```
2. `Run`의 `for` 루프 안의 `order.Add(TakeTurn(cursor));` 줄을 아래 한 줄로 바꿉니다.
```csharp
            order.Add(TakeTurn(ref cursor));
```
또는 `TurnCursor`를 `class`로 바꿉니다.
**교훈:** 상태를 가진 struct(열거자, 커서, 빌더)는 대입·매개변수·`readonly` 필드·`foreach` 반복 변수에서 복사됩니다. `List<T>.Enumerator`를 변수에 받아 다른 메서드로 넘기거나, `readonly` 필드에 둔 struct의 메서드를 부르는 경우(방어적 복사)도 같은 증상입니다.

## Lab 35 — 같은 문자열 리터럴에 lock 해서 서로 다른 시스템이 교착
```
병렬 스택:  스레드 A  AutoSave.Save → ManualResetEventSlim.Wait  (잠금을 쥔 채)
           스레드 B  Achievements.Tally → Monitor.Enter          (잠금을 기다림)
조사식 object.ReferenceEquals(save._lock, achievements._lock):  true
진짜 원인:  두 클래스가 각자 "bughunt-save-lock" 문자열을 잠금 객체로 썼다. 문자열 리터럴은
           인터닝되어 프로세스 전체에서 같은 객체이므로 두 잠금은 같은 모니터다.
```
**수정:** 클래스마다 자기만의 잠금 객체를 둔다. `Lab35_SharedLock.cs`의 `AutoSave` 클래스와 `Achievements` 클래스에는 각각 클래스의 첫 멤버로 `readonly object _lock = "bughunt-save-lock";` 줄이 있습니다. 그 두 줄을 모두 아래 한 줄로 바꿉니다 (C# 13 이상이면 `System.Threading.Lock`도 됩니다). 파일 아래쪽 힌트 주석도 같은 코드를 인용하므로, 일괄 찾아 바꾸기는 쓰지 말고 두 클래스 안의 선언만 고칩니다.
```csharp
        private readonly object _lock = new();
```
**교훈:** 잠금 대상은 **그 목적만을 위해 만든 private 객체**여야 합니다. 문자열, `typeof(X)`, `this`, 박싱된 값, 바깥에 노출된 객체는 모르는 코드와 모니터를 공유할 수 있습니다. 또 "잠금을 쥔 채로 다른 작업을 기다리기"는 그 자체로 교착의 씨앗이므로 잠금 범위를 짧게 유지합니다.

## Lab 36 — `for` 변수를 캡처한 `Task.Run`
```
적중 시 로그 {i} (downloaded.Add(i)):  5, 5, 5, 5, 5
gate.Set() 줄에서 i:                   for를 다 돈 뒤라 이미 5(= pieces)
진짜 원인:  for의 i는 루프 전체에 하나뿐인 변수이고, 람다는 그 변수를 참조로 캡처한다.
           작업은 gate 때문에 루프가 끝난 뒤에 돌므로 모두 마지막 값(범위 밖)을 본다.
```
**수정:** 반복마다 복사본을 만듭니다. `Lab36_PatchQueue.cs`의 `Run`에서 `for (int i = 0; i < pieces; i++)` 루프 전체(`tasks.Add(...)`를 포함해 닫는 괄호 `}`까지)를 아래로 바꿉니다. `Run`의 나머지 줄은 그대로 둡니다.
```csharp
        for (int i = 0; i < pieces; i++)
        {
            int piece = i;   // 반복마다 새 변수. 람다는 이것을 캡처한다
            tasks.Add(Task.Run(() =>
            {
                gate.Wait();
                downloaded.Add(piece);
            }));
        }
```
또는 `foreach (int piece in Enumerable.Range(0, pieces))`, `Parallel.For`.
**교훈:** C# 5부터 `foreach`의 반복 변수는 반복마다 새로 만들어지지만, `for`의 변수는 아닙니다. 이 랩의 `gate`는 "작업이 늦게 시작하는" 상황을 결정적으로 만든 것이며, 실제로는 스레드 풀 상태에 따라 가끔만 틀립니다. 같은 뿌리의 다른 함정으로 `list.ForEach(async x => ...)`가 있습니다. `ForEach`는 `Action<T>`만 받으므로 람다가 `async void`가 되어 기다릴 수 없고 예외도 잡히지 않습니다(8부 비동기 예외 처리 참고).

## Lab 37 — 돈을 `double`로 더하고 `Math.Round`의 기본 반올림을 씀
```
조사식 sum (Total의 return):   0.30000000000000004
조사식 Math.Round(2.5m):        2
조사식 Math.Round(3.5m):        4
조사식 Math.Round(2.5m, MidpointRounding.AwayFromZero):  3
진짜 원인:  double은 0.1, 0.2를 정확히 담지 못해 합에 오차가 쌓인다.
           Math.Round의 기본은 MidpointRounding.ToEven(은행가 반올림)이라 2.5 → 2, 0.5 → 0.
```
**수정:** 합계를 `decimal`로 더하고, 세금은 반올림 규칙을 명시합니다. `Lab37_CashShop.cs`의 `Total`과 `RoundTax`를 아래로 바꿉니다. `Run`이 합계를 `double`로 비교하므로 `Total`의 반환 형식은 그대로 두고, 다 더한 뒤 한 번만 바꿉니다. `Run`은 그대로 둡니다.
```csharp
    // 장바구니 합계. 가격은 "원" 단위 소수.
    static double Total(decimal[] prices)
    {
        decimal sum = 0;
        foreach (decimal p in prices)
            sum += p;
        return (double)sum;   // 다 더한 뒤에 한 번만 바꾼다
    }

    // 세금을 정수로 반올림한다. .5 는 0 에서 먼 쪽으로.
    static decimal RoundTax(decimal tax) => Math.Round(tax, MidpointRounding.AwayFromZero);
```
**교훈:** 돈, 수량, 점수처럼 10진수로 정확해야 하는 값은 `decimal` 또는 정수(원·센트 단위)로 다룹니다. 반올림 규칙은 도메인 결정이므로 항상 인자로 적습니다. `double` 합계를 `Math.Round(sum, 2)`로 감추는 수정은 이 랩의 숨은 케이스는 통과할 수 있지만, 값이 커지거나 연산이 길어지면 다시 어긋납니다. (참고: `(decimal)someDouble`은 유효숫자 15자리로 반올림해 변환하므로 오차를 가려 버립니다. 이 랩이 합계를 `double`로 비교하는 이유입니다.)

---

# 5부: 인터페이스와 상속

## Lab 38 — `new`로 숨긴 메서드는 기반 형식으로 부르면 안 불린다
```
FireSkill.Damage 중단점:   TotalDamage 에서는 안 멈춤 (Run 의 기대값 계산에서만 멈춤)
F11 (s.Damage(attack)):    Skill.Damage 로 들어감
조사식 s.GetType():        FireSkill   (정적 형식은 Skill)
진짜 원인:  FireSkill·IceSkill 의 Damage 가 override 가 아니라 new. 기반 메서드를 덮어쓰지 않고 같은 이름의 새 메서드를 만들었다.
           Skill 형식의 변수로 부르면 Skill.Damage 가 불린다. new 를 지우면 CS0114 경고가 나서 누군가 new 로 눌러 둔 것.
```
**수정:** `new`를 `override`로 바꾼다. `Lab38_SkillDamage.cs`에서 두 곳, `FireSkill`의 `Damage` 줄과 `IceSkill`의 `Damage` 줄을 각각 아래 한 줄로 바꾼다.

1. `FireSkill`:
```csharp
        public override int Damage(int attack) => (attack + Power) * 3 / 2;
```
2. `IceSkill`:
```csharp
        public override int Damage(int attack) => attack + Power + 20;
```
**교훈:** CS0114("상속된 멤버를 숨깁니다. 재정의하려면 override 를, 아니면 new 를 추가하세요")는 대개 `override`를 빠뜨렸다는 뜻입니다. `new`는 기반 클래스를 바꿀 수 없는데 같은 이름이 꼭 필요한 드문 경우에만 씁니다. 불 스킬만 고치면 숨은 케이스(얼음 스킬)에서 걸립니다.

## Lab 39 — 시그니처가 다른 메서드 대신 인터페이스 기본 구현이 불림
```
Boss.DropCount 중단점:     RollDrops 에서는 안 멈춤
F11 (monster.DropCount):   IDropSource.DropCount 의 기본 구현 (=> 1) 으로 들어감
진짜 원인:  Boss.DropCount(int, bool = false), Elite.DropCount(long) 은 인터페이스의 int DropCount(int) 와 시그니처가 달라서
           구현으로 인정되지 않는다. 인터페이스에 기본 구현이 있으니 컴파일 오류도 경고도 없다.
```
**수정:** 시그니처를 인터페이스의 `int DropCount(int luck)`와 똑같이 맞춘다. `Boss`는 선택적 매개변수 `bool elite = false`와 그것을 쓰던 `(elite ? 1 : 0)`을 지우고, `Elite`는 `long`을 `int`로 바꾼다. 두 클래스 위의 주석("정예 보스는 1개 더", "운은 long으로 받는다")도 바뀐 코드에 맞게 고친다.
```csharp
    sealed class Boss(string name, int baseDrops) : IDropSource
    {
        public string Name => name;
        public int DropCount(int luck) => baseDrops + luck / 10;
    }

    sealed class Elite(string name) : IDropSource
    {
        public string Name => name;
        public int DropCount(int luck) => 2 + luck / 20;
    }
```
정예 보스 같은 변형은 생성자 인자(`bool elite`)나 별도 오버로드로 받고, 서버의 `long` 운 수치는 인터페이스를 부르기 전에 `int`로 바꾼다.
**교훈:** 선택적 매개변수를 붙이거나 매개변수 형식을 넓히면(`int` → `long`) 호출하는 쪽 코드는 똑같아 보여도 **다른 메서드**입니다. 기본 구현(DIM)이 있는 인터페이스는 "구현을 빠뜨려도 컴파일된다"는 위험을 같이 들고 옵니다. 기본 구현을 잠깐 지워 보면 구현이 빠진 클래스가 컴파일 오류로 드러납니다.

## Lab 40 — 명시적 인터페이스 구현이 다른 값을 돌려줌
```
조사식 r.Gold:            600     (IReward 로 봄 → 명시적 구현)
조사식 ((Quest)r).Gold:   1200    (Quest 로 봄 → public 구현)
진짜 원인:  Quest 에 Gold 가 둘. public int Gold(배율 적용)와 int IReward.Gold(배율 없음).
           퀘스트 창은 Quest 로, 정산은 IReward 로 읽어서 서로 다른 구현이 불린다.
```
**수정:** 명시적 구현을 public 구현에 위임한다. `Lab40_QuestReward.cs`의 `Quest`에서 `int IReward.Gold => baseGold;` 줄을 아래 한 줄로 바꾼다.
```csharp
        int IReward.Gold => Gold;
```
명시적 구현 줄을 지워도 된다(public `Gold`가 그대로 인터페이스를 구현한다).
**교훈:** 명시적 구현은 이름 충돌을 피하거나 멤버를 인터페이스 쪽으로만 보이게 할 때 씁니다. 두 번째 로직을 두는 자리가 아닙니다. 정산에서 2배를 하는 수정은 숨은 케이스(다른 배율)에서 걸립니다.

## Lab 41 — struct를 인터페이스로 다뤄서 박싱 사본만 바뀜
```
Buff.Tick 중단점:     불림. this.Turns 가 줄어드는 것도 보임
EndTurn 뒤 buffs[0].Turns:   그대로
Tick 안에서 this 에 개체 ID 만들기:   불가 (값 형식)
진짜 원인:  foreach (ITickable buff in buffs) 가 칸마다 Buff 를 박싱해 힙 사본을 만들고, Tick 은 그 사본을 고친다.
```
**수정:** 배열의 칸 자체에 대해 `Tick`을 부른다. `Lab41_BuffTick.cs`의 `EndTurn`을 아래로 바꾼다. `Buff`와 `Run`은 그대로 둔다.
```csharp
    static void EndTurn(Buff[] buffs)
    {
        for (int i = 0; i < buffs.Length; i++)
            buffs[i].Tick();   // buffs[i]는 배열의 칸 자체. 박싱도 복사도 없다
    }
```
다른 방법:
- `foreach (ref Buff buff in buffs.AsSpan()) buff.Tick();`
- 제네릭 제약: `static void TickAll<T>(Span<T> items) where T : ITickable { foreach (ref T it in items) it.Tick(); }`
- 바뀌는 데이터면 `class`로.

**교훈:** 값 형식을 인터페이스, `object`로 바꾸는 순간 복사본이 생깁니다(랩 4 박싱 해제, 랩 12 박싱된 `==`, 랩 25 `List<struct>` 인덱서와 같은 뿌리). 바뀌는 struct에 인터페이스를 붙이면 이 버그가 숨어 들어오기 쉽습니다.

## Lab 42 — 생성자 안의 가상 호출
```
Knight.MaxHp 중단점 (처음 멈춤):   armor == 0, baseHp == 100
호출 스택:   Unit..ctor → Knight.MaxHp   (Knight..ctor 본문은 아직)
진짜 원인:  생성 순서는 파생 필드 이니셜라이저 → 기반 생성자 → 파생 생성자 본문.
           기반 생성자가 부른 가상 메서드는 이미 Knight.MaxHp 로 가지만, armor 는 파생 생성자 본문에서야 들어간다.
```
**수정:** 기반 생성자가 최대 체력을 인자로 받고, 가상 호출 `Hp = MaxHp();`를 없앤다. 파생 생성자는 자기 필드 값으로 계산한 최대 체력을 `base(...)`에 넘긴다. `Lab42_UnitSpawn.cs`에서 세 곳을 바꾼다. `MaxHp()` 재정의와 필드 이니셜라이저는 그대로 둔다.

1. `Unit` 클래스를 통째로 아래로 바꾼다.
```csharp
    abstract class Unit
    {
        public string Name { get; }
        public int Hp { get; protected set; }

        protected Unit(string name, int maxHp)
        {
            Name = name;
            Hp = maxHp;   // 파생 클래스가 계산해서 넘긴 값. 가상 호출이 없다
        }

        public abstract int MaxHp();
    }
```
2. `Knight`의 생성자를 아래로 바꾼다.
```csharp
        public Knight(string name, int armor) : base(name, 100 + armor * 10)
        {
            this.armor = armor;
        }
```
3. `Mage`의 생성자를 아래로 바꾼다.
```csharp
        public Mage(string name, int wisdom) : base(name, 60 + wisdom * 5)
        {
            this.wisdom = wisdom;
        }
```
또는 체력 채우기를 생성이 끝난 뒤의 `Spawn()` 단계로 옮긴다.
**교훈:** 생성자에서 가상 메서드를 부르면 파생 클래스가 아직 준비되지 않은 객체를 보게 됩니다(CA2214). 필드 이니셜라이저(`baseHp = 100`)는 기반 생성자보다 **먼저**, 생성자 본문은 **나중에** 실행된다는 차이가 증상을 헷갈리게 만듭니다. `Knight` 생성자 끝에서 `Hp`를 다시 채우는 수정은 숨은 케이스(마법사)에서 걸립니다.

## Lab 43 — `Equals(object)`와 `IEquatable<T>.Equals`가 다른 규칙
```
bag.Contains(loot):         true    → Equals(ItemStack) (Id 만 비교)
favorites.Contains(loot):   false   → Equals(object)    (Id + 품질 비교)
조사식 favorites.Comparer:  ObjectEqualityComparer<IItem>
진짜 원인:  EqualityComparer<T>.Default 는 T 가 IEquatable<T> 이면 Equals(T), 아니면 Equals(object) 를 부른다.
           List<ItemStack> 은 T = ItemStack, HashSet<IItem> 은 T = IItem 이라 서로 다른 Equals 를 탄다.
```
**수정:** 한 규칙에 위임한다. `Lab43_Favorites.cs`의 `ItemStack`에서 `public override bool Equals(object? obj)` 줄을 아래 한 줄로 바꾼다.
```csharp
        public override bool Equals(object? obj) => Equals(obj as ItemStack);
```
**교훈:** 같은 형식이 `IEquatable<T>`, `object`, 인터페이스 형식의 컬렉션, `==` 연산자 등 여러 경로로 비교됩니다. 모든 경로가 같은 규칙이어야 하고, `GetHashCode`도 같은 규칙이어야 합니다(랩 18은 해시가 어긋난 경우). 즐겨찾기를 `HashSet<ItemStack>`으로 바꾸는 수정도 통과하지만, 다른 `IItem` 구현이 들어오는 순간 다시 깨집니다.

## Lab 44 — 기반 형식을 먼저 검사해서 파생 분기에 닿지 않음
```
return 500; 중단점:   안 멈춤
조사식 killed is Enemy:       true   (보스도 Enemy)
조사식 killed is BossEnemy:   true
진짜 원인:  if (killed is Enemy) 가 첫 분기라서 보스·정예도 모두 10G. 뒤의 else if 두 개는 닿을 수 없다.
           같은 순서를 switch 식으로 쓰면 CS8510(switch 문이면 CS8120, "이미 앞에서 처리")으로 컴파일이 안 되지만, if/else if 에는 그런 검사가 없다.
```
**수정:** 가장 파생된 형식부터 검사한다 (보스 → 정예 → 일반 적). `Lab44_KillReward.cs`의 `Reward`를 아래로 바꾼다. `Spawn`, `ExpectedReward`, `Run`은 그대로 둔다.
```csharp
    static int Reward(Unit killed)
    {
        if (killed is BossEnemy)
            return 500;
        else if (killed is EliteEnemy)
            return 50;
        else if (killed is Enemy)
            return 10;
        else
            return 0;
    }
```
더 나은 방법은 `switch` 식으로 써서 컴파일러가 순서를 검사하게 하거나, `Unit`에 `virtual int KillReward`를 둬서 형식 검사 자체를 없애는 것.
**교훈:** 보스 분기만 위로 올리면 숨은 케이스(정예가 섞인 웨이브)에서 걸립니다.

## Lab 45 — 배열 공변성
```
예외:      ArrayTypeMismatchException: Attempted to access an element as a type incompatible with the array.
멈춘 곳:   Put 의 bag[slot] = item
조사식 bag.GetType():   Lab45+Weapon[]   (선언 형식은 Item[])
진짜 원인:  CreateBag 이 new Weapon[size] 를 만들어 Item[] 로 돌려줬다. 배열 공변성 덕에 컴파일은 되지만 실제 배열은 Weapon 만 받는다.
```
**수정:** 실제 배열을 `Item[]`로 만든다. `Lab45_StarterBag.cs`의 `CreateBag`에서 `var bag = new Weapon[size];` 줄을 아래 한 줄로 바꾼다.
```csharp
        var bag = new Item[size];
```
**교훈:** 참조 형식 배열은 공변이라 `Weapon[]`을 `Item[]` 변수에 넣을 수 있지만, 쓰기는 런타임 검사로 막힙니다. `List<T>`는 공변이 아니어서 이 실수가 컴파일되지 않고, 읽기 전용 공변성은 `IEnumerable<out T>`, `IReadOnlyList<out T>`로 얻습니다. 예외를 삼키고 건너뛰면 물약이 사라져 채워진 칸 수에서 걸립니다. (블라인드 챌린지 B-4도 같은 범주입니다)

---

# 6부: 파일 입출력

## Lab 46 — 상대 경로는 작업 디렉터리 기준
```
예외:        DirectoryNotFoundException: Could not find a part of the path '...\Lab46_SaveFolder\mods\saves\slot1.sav'.
저장할 때 경로:  ...\Lab46_SaveFolder\game\saves\slot1.sav
조사식:      Environment.CurrentDirectory == ...\mods,  Path.GetFullPath("saves") == ...\mods\saves
진짜 원인:  SavePath 가 "saves\slot1.sav" 라는 상대 경로를 만든다. 상대 경로는 그때그때의 작업 디렉터리 기준인데,
           폴더 열기(파일 대화상자)가 작업 디렉터리를 바꿨다. SaveSystem 은 GameDir 을 받아 두고도 쓰지 않았다.
```
**수정:** 경로를 `GameDir` 기준으로 만들고, 폴더도 같은 기준으로 만든다. `Lab46_SaveFolder.cs`의 `SaveSystem`에서 `SavePath`와 `Save`를(둘이 이어져 있다) 아래로 바꾼다. `Load`와 `Run`은 그대로 둔다.
```csharp
        string SavePath(string slot) => Path.Combine(GameDir, "saves", slot + ".sav");

        public void Save(string slot, int gold)
        {
            Directory.CreateDirectory(Path.Combine(GameDir, "saves"));
            File.WriteAllText(SavePath(slot), gold.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine(Text.L($"  저장: {slot} ({gold}G) → {Path.GetFullPath(SavePath(slot))}", $"  セーブ: {slot} ({gold}G) → {Path.GetFullPath(SavePath(slot))}"));
        }
```
**교훈:** 작업 디렉터리는 프로세스 전체가 공유하는 바뀌는 상태입니다(런처, 바로 가기의 시작 위치, 파일 대화상자, 다른 폴더에서 친 명령줄). 실행 파일 폴더는 `AppContext.BaseDirectory`, 사용자 데이터는 `Environment.GetFolderPath(SpecialFolder.LocalApplicationData)` 아래가 맞습니다. 불러오기 전에 작업 디렉터리를 되돌리는 수정은 다음에 작업 디렉터리를 바꾸는 코드가 생기면 다시 깨집니다.

## Lab 47 — 안쪽 스트림만 닫아서 `StreamWriter` 버퍼가 버려짐
```
SaveLog 끝:   stream.Position 이 써야 할 양보다 작음
writer [Raw View] _charPos:   0 이 아님 (버퍼에 남은 글자)
진짜 원인:  using 은 FileStream 에만 붙어 있다. StreamWriter 는 자기 버퍼(1024자)가 찰 때마다 스트림으로 보내는데,
           마지막에 남은 글자는 writer 의 Flush/Dispose 때만 나간다. 안쪽 스트림을 닫아도 바깥 writer 의 버퍼는 비워지지 않는다.
```
**수정:** writer에도 `using`을 붙인다. `Lab47_CombatLog.cs`의 `SaveLog`에서 `var writer = new StreamWriter(stream);` 줄을 아래 한 줄로 바꾼다 (선언 역순으로 닫혀 writer가 먼저 닫힘).
```csharp
        using var writer = new StreamWriter(stream);
```
`FileStream`을 만들지 않고 `using var writer = new StreamWriter(path);` 하나로 해도 된다.
**교훈:** 래퍼가 겹치면(`StreamWriter`, `BinaryWriter`, `GZipStream`, `CryptoStream`) **가장 바깥부터** 닫습니다. 바깥만 닫으면 안쪽까지 닫히고, 안쪽을 계속 쓰려면 `leaveOpen: true`. 랩 72는 아예 닫지 않아 파일이 비고 핸들이 남은 경우, 이번은 안쪽만 닫아 꼬리가 잘린 경우입니다.

## Lab 48 — `FileShare`가 맞지 않아 쓰는 중인 파일을 못 읽음
```
first chance:  IOException: The process cannot access the file '...chat.log' because it is being used by another process.
멈춘 곳:      File.ReadAllLines (Upload 의 catch 가 삼켜서 0줄 업로드)
진짜 원인:  ChatLogger 의 StreamWriter(append) 는 FileAccess.Write + FileShare.Read 로 열려 있다.
           File.ReadAllLines 는 FileAccess.Read + FileShare.Read 로 연다. 즉 "남이 쓰고 있으면 거부". "다른 프로세스"는 사실 이 프로세스.
```
**수정:** 읽는 쪽이 쓰기 공유를 허용한다. `Upload`의 `File.ReadAllLines(path)`는 공유 모드를 고를 수 없으므로, `FileStream`을 직접 열고 `StreamReader`로 줄을 읽는다. `Lab48_ChatUpload.cs`의 `Upload`를 통째로 아래로 바꾼다. `ChatLogger`와 `Run`은 그대로 둔다.
```csharp
    static string[] Upload(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            var lines = new List<string>();
            while (reader.ReadLine() is { } line)
                lines.Add(line);
            Console.WriteLine(Text.L($"  업로드: {lines.Count}줄", $"  アップロード: {lines.Count}行"));
            return [.. lines];
        }
        catch (IOException ex)
        {
            Console.WriteLine(Text.L($"  업로드 실패: {ex.Message}", $"  アップロード失敗: {ex.Message}"));
            return [];
        }
    }
```
**교훈:** 두 핸들이 공존하려면 서로의 `FileAccess`가 상대의 `FileShare`에 들어 있어야 합니다. 쓰는 쪽은 `AutoFlush`나 `Flush`도 해야 읽는 쪽이 최신 내용을 봅니다. 업로드 전에 로거를 닫는 수정도 동작하지만 신고 중에 온 채팅을 놓칠 수 있습니다. `catch (IOException)`이 실패를 "0줄"로 바꾼 것도 랩 69(예외를 삼키고 성공을 돌려줌)과 같은 냄새입니다. Linux/macOS는 공유 모드를 강제하지 않아 이 랩은 `SKIP`합니다.

## Lab 49 — `Path.Combine`의 두 번째 인자가 절대 경로
```
콘솔:      "저장 →" 세 줄 중 하나만 ...\old-install\saves\mira
조사식:    Path.Combine(saveRoot, profile.Folder) == profile.Folder,  Path.IsPathRooted(profile.Folder) == true
진짜 원인:  Path.Combine 은 뒤 인자가 루트 경로면 앞의 인자를 전부 버린다(문서에 적힌 동작, 예외 없음).
           옛 설치에서 가져온 프로필의 Folder 가 절대 경로라 세이브 루트 밖에 저장됐고, 백업이 세이브 루트만 훑어 빠졌다.
```
**수정:** 바깥에서 온 값은 폴더 이름으로만 쓴다. `Lab49_ProfileImport.cs`의 `SaveDir` 줄을 아래 한 줄로 바꾼다.
```csharp
    static string SaveDir(string saveRoot, Profile profile) => Path.Combine(saveRoot, Path.GetFileName(profile.Folder));
```
합친 뒤 `Path.GetFullPath(결과)`가 세이브 루트 안인지 한 번 더 확인하면 안전합니다. (`Path.IsPathRooted`, `..` 포함이면 거부하는 방법은 이 게임에서는 맞지 않습니다: 가져온 프로필의 세이브가 빠져 판정에서 걸립니다.)
**교훈:** 사용자 입력, 세이브 파일, 서버 응답, 압축 파일 안의 이름을 `Path.Combine`에 그대로 넣으면 경로 탈출(path traversal)이 됩니다. `Path.Join`은 앞을 버리지 않지만 엉뚱한 경로를 만들 뿐 검증을 대신하지 않습니다.

## Lab 50 — 바이트를 UTF-8로만 디코딩: BOM과 CP949
```
모드 A (UTF-8 BOM):  table.Keys.First().Length == firstKey.Length + 1,  첫 글자 '﻿'
모드 B (CP949):      bytes 의 한글이 2바이트씩. UTF8.GetString 결과에 '�'(�)
모드 C (UTF-8):      정상
진짜 원인:  Encoding.UTF8.GetString 은 BOM 을 지우지 않고(U+FEFF 로 남음), 잘못된 바이트는 예외 없이 �로 바꾼다.
           File.ReadAllText/StreamReader 는 BOM 을 처리하지만 바이트를 직접 디코딩하면 그렇지 않다.
```
**수정:** BOM을 떼고, UTF-8로 올바르지 않으면 CP949로 다시 읽는다. `Lab50_DialogueMod.cs`의 `LoadDialogue`를 통째로 아래로 바꾼다(`string text = Encoding.UTF8.GetString(bytes);` 한 줄이 아래의 `if`/`else`가 된다). CP949에 필요한 `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)`는 `Run`이 이미 부르고, `using System.Text;`도 파일에 있다. `Lookup`과 `Run`은 그대로 둔다.
```csharp
    static Dictionary<string, string> LoadDialogue(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        string text;
        if (bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble))
            text = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);   // BOM 3바이트를 건너뛴다
        else
        {
            try { text = new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes); }
            catch (DecoderFallbackException) { text = Encoding.GetEncoding(949).GetString(bytes); }   // UTF-8이 아니면 CP949
        }

        var table = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            int eq = line.IndexOf('=');
            if (eq > 0)
                table[line[..eq]] = line[(eq + 1)..].TrimEnd('\r');
        }
        return table;
    }
```
**교훈:** .NET Core 이후는 기본으로 UTF-8, UTF-16, ASCII, Latin-1 정도만 들어 있고 CP949·Shift_JIS(932) 같은 코드 페이지는 `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)`를 해야 씁니다. 키에서 `﻿`만 지우는 수정은 모드 B를 못 고칩니다. 저장하는 쪽이라면 BOM 없는 UTF-8로 통일합니다(`File.WriteAllText`의 기본값).

## Lab 51 — `\n`으로만 잘라서 `\r`과 빈 항목이 남음
```
콘솔 "읽은 항목":   "Kael\r", "Mira\r", "Doran\r", ""
조사식:            text.Contains('\r') == true
진짜 원인:  메모장은 CRLF 로 저장한다. Split('\n') 은 각 항목 끝에 '\r' 을 남기고, 끝 줄바꿈 뒤에 빈 문자열을 하나 더 만든다.
```
**수정:** 줄 단위로 읽는 API를 쓴다. `Lab51_BlockList.cs`의 `LoadBlockList`를 아래 한 줄로 바꾼다(`File.ReadAllText`와 `Split('\n')`는 지운다). `Run`은 그대로 둔다.
```csharp
    static HashSet<string> LoadBlockList(string path) => [.. File.ReadAllLines(path)];
```
`File.ReadLines`나 `StreamReader.ReadLine`도 같다. 직접 자른다면 `text.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)`.
**교훈:** `ReadLine` 계열은 `\r\n`, `\n`, `\r`을 모두 줄 끝으로 처리하고 끝 줄바꿈 뒤에 빈 줄을 만들지 않습니다. `TrimEnd('\r')`만 붙이면 빈 항목이 남고, `Split("\r\n")`만 쓰면 숨은 케이스(LF 파일)에서 한 줄로 붙어 걸립니다.

## Lab 52 — 원본을 직접 덮어쓰다 실패해서 반쯤 쓴 파일
```
Save(path, broken) 직전:  inventory.sav 정상
Save 의 첫 줄(new StreamWriter(path)) 직후:  0바이트
예외 뒤:  개수 줄 + 앞쪽 아이템 몇 줄만 (Load 가 손상으로 판정)
진짜 원인:  new StreamWriter(path) 는 FileMode.Create 라서 여는 순간 기존 파일을 자른다. 예외가 나도 using 이 그때까지 쓴 앞부분을 내보낸다.
```
**수정:** 다른 파일에 끝까지 쓰고, 다 쓰고 닫은 뒤 한 번에 바꿔치기. `Lab52_SafeSave.cs`의 `Save`를 통째로 아래로 바꾼다. 실패하면 임시 파일을 지우고 예외를 그대로 다시 던진다(`Run`이 그 예외를 잡아 지난번 세이브를 쓴다). `using System.Globalization;`은 파일에 이미 있고, `Load`와 `Run`은 그대로 둔다.
```csharp
    static void Save(string path, IReadOnlyList<Item> items)
    {
        string tmp = path + ".tmp";
        try
        {
            using (var writer = new StreamWriter(tmp))
            {
                writer.WriteLine(items.Count.ToString(CultureInfo.InvariantCulture));
                foreach (Item item in items)
                {
                    if (item.Count < 0)
                        throw new InvalidDataException(Text.L($"손상된 아이템: {item.Name} x{item.Count}", $"破損したアイテム: {item.Name} x{item.Count}"));
                    writer.WriteLine($"{item.Name}|{item.Count.ToString(CultureInfo.InvariantCulture)}");
                }
            }   // 바꿔치기 전에 닫아서 버퍼를 다 내보낸다
        }
        catch
        {
            File.Delete(tmp);   // 실패한 저장의 임시 파일은 남기지 않는다
            throw;
        }
        File.Move(tmp, path, overwrite: true);   // 같은 볼륨 안에서는 이름만 바뀐다
    }
```
지난번 세이브를 백업으로도 남기려면, 원본이 있을 때 마지막 줄 대신 `File.Replace(tmp, path, path + ".bak");`를 쓴다.
**교훈:** 정전, 강제 종료, 디스크 꽉 참도 같은 결과입니다. 같은 볼륨 안의 `File.Replace`/`File.Move(…, overwrite: true)`는 이름만 바꾸므로 중간 상태가 남지 않습니다. 먼저 메모리에서 전부 검사한 뒤에 여는 수정은 이 예외는 피하지만 쓰는 도중의 중단에는 여전히 약합니다. 실패한 저장의 임시 파일은 지웁니다(세 번째 판정).

## Lab 53 — 비동기 쓰기를 기다리지 않고 그 파일을 읽음
```
중단점 순서:   ReadAllTextAsync 가 WriteAllTextAsync 보다 먼저 멈춤
조사식:        save.IsCompleted == false, save.Status == WaitingForActivation
작업 창:       SaveSettingsAsync 가 Task.Delay 에서 대기 중
진짜 원인:  ApplyAsync 가 저장 Task 를 대기열에 넣기만 하고 await 하지 않은 채 파일을 읽는다. 읽는 것은 이전 설정.
           Task 를 변수로 받았기 때문에 CS4014 경고도 나지 않는다.
```
**수정:** 파일을 읽기 전에 저장이 끝나기를 기다린다. `Lab53_CloudSync.cs`의 `ApplyAsync`를 아래로 바꾼다. `await save;`는 `await FileLock.WaitAsync();`보다 **앞**에 둔다. `SaveSettingsAsync`도 쓸 때 같은 `FileLock`을 잡으므로, 잠금을 쥔 채 `await save;`를 하면 저장은 잠금을 기다리고 이쪽은 저장을 기다려 교착 상태가 된다. `SaveSettingsAsync`와 `Run`은 그대로 둔다.
```csharp
    static async Task<string> ApplyAsync(string path, int volume, int sensitivity)
    {
        Task save = _saveQueue = SaveSettingsAsync(_saveQueue, path, volume, sensitivity);

        await save;   // 저장이 끝난 뒤에 읽는다. FileLock을 잡기 전에 기다린다
        string uploaded;
        await FileLock.WaitAsync();
        try { uploaded = await File.ReadAllTextAsync(path); }
        finally { FileLock.Release(); }
        Console.WriteLine(Text.L($"  클라우드에 올림: {OneLine(uploaded)}", $"  クラウドにアップロード: {OneLine(uploaded)}"));
        return uploaded;
    }
```
**교훈:** 순서는 시간(`Task.Delay`)이 아니라 작업 완료로 맞춥니다. 저장한 내용을 업로드에 직접 넘겨 파일을 다시 읽지 않는 것도 방법입니다. 랩 64는 기다리지 않은 작업이 아예 안 끝나는 경우, 이번은 끝나기는 하지만 다른 코드가 너무 일찍 그 결과(파일)를 보는 경우입니다.

---

# 7부: 직렬화 (JSON/XML)

## Lab 54 — JSON 이름의 대소문자가 안 맞아 조용히 기본값
```
받은 JSON:     {"title":"...","author":"...","priority":3,"pinned":true}
Title set 중단점:   Deserialize 중에 안 불림
진짜 원인:  System.Text.Json 기본 옵션은 이름을 대소문자까지 정확히 비교한다. "title" ≠ Title.
           모르는 속성은 기본으로 무시하므로 예외 없이 모든 속성이 초기값으로 남는다.
```
**수정:** Web 기본값(camelCase, 대소문자 무시)을 쓴다. `Lab54_GuildNotice.cs`의 `Options` 필드 줄을 아래 한 줄로 바꾼다.
```csharp
    static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = false };
```
`PropertyNameCaseInsensitive = true`, `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`, 속성마다 `[JsonPropertyName]`도 된다.
**교훈:** 직렬화 옵션은 **상대와 맞추는 계약**입니다. 형식을 맞추는 동안에는 `UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow`로 "모르는 속성이면 예외"를 켜 두면 이런 버그가 바로 드러납니다. (참고: Newtonsoft.Json은 기본이 대소문자 무시라서, 옮겨 온 코드에서 이 버그가 자주 생깁니다.)

## Lab 55 — 필드와 `private set`은 왕복되지 않음
```
세이브 파일:   {"name":"아린","level":5}      ← gold 가 없음
Level private set 중단점:   Load 중에 안 불림
진짜 원인:  Gold 는 필드. System.Text.Json 은 기본으로 public 속성만 다룬다 → 저장부터 빠짐.
           Level 은 public get 이라 저장은 되지만 set 이 private 이라 읽을 때 건너뜀 → 초기값 1.
```
**수정:** 두 멤버에 `[JsonInclude]`를 붙인다. `Lab55_CharacterSave.cs`의 `Character` 클래스를 아래로 바꾸고, 파일 맨 위의 `using` 줄들에 `using System.Text.Json.Serialization;`을 더한다(`[JsonInclude]`가 있는 네임스페이스). `Run`은 그대로 둔다.
```csharp
using System.Text.Json.Serialization;

    sealed class Character
    {
        public string Name { get; set; } = "";

        // 골드는 자주 바뀌어서 필드로 둔다. 필드는 [JsonInclude] 가 있어야 저장·복원된다.
        [JsonInclude] public int Gold;

        // 레벨은 LevelUp 으로만 올린다. private set 은 [JsonInclude] 가 있어야 읽을 때 쓰인다.
        [JsonInclude] public int Level { get; private set; } = 1;

        public void LevelUp() => Level++;
    }
```
다른 방법: `Options`에 `IncludeFields = true`를 주고 `Level`에만 `[JsonInclude]`를 붙이거나, `[JsonConstructor]` 생성자로 받는다.
**교훈:** "저장은 되는데 못 읽는 멤버"는 경고가 없습니다. 세이브 형식에는 저장 → 불러오기 → 비교하는 왕복 테스트를 둡니다.

## Lab 56 — 다형성 정보가 없어 파생 형식이 기반 형식으로 돌아옴
```
서버 저장:    {"to":"아린","attachments":[{"source":"주간 퀘스트"},{"source":"주간 퀘스트"}]}   ← amount, itemName 없음
조사식 received.Attachments[0].GetType():   Lab56+Reward
진짜 원인:  List<Reward> 의 항목을 선언 형식 Reward 로 직렬화해서 파생 형식의 속성이 빠지고,
           JSON 에 형식 구분자가 없으니 읽을 때도 전부 Reward 로 만든다.
```
**수정:** 기반 형식에 파생 형식을 등록한다. `Lab56_MailRewards.cs`의 `Reward` 클래스를 아래로 바꾸고(본문은 그대로, 특성 네 줄이 늘어난다), 파일 맨 위의 `using` 줄들에 `using System.Text.Json.Serialization;`을 더한다. 파생 형식 세 개를 모두 등록해야 한다. 파생 클래스와 `Run`은 그대로 둔다.
```csharp
using System.Text.Json.Serialization;

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(GoldReward), "gold")]
    [JsonDerivedType(typeof(ItemReward), "item")]
    [JsonDerivedType(typeof(ExpReward), "exp")]
    class Reward
    {
        public string Source { get; set; } = "";
        public virtual string Describe() => Text.L("알 수 없는 보상", "不明な報酬");
    }
```
**교훈:** System.Text.Json은 보안상 "JSON에 적힌 형식 이름으로 아무 형식이나 만드는" 기능이 없고, 허용할 파생 형식을 직접 등록합니다. 하나라도 빠뜨리면 그 형식을 직렬화할 때 `NotSupportedException`이 납니다(숨은 케이스의 경험치 보상). 기반 형식을 `abstract`로 만들어 두면 등록을 잊었을 때 역직렬화가 조용히 넘어가지 않고 예외를 내 줍니다.

## Lab 57 — 공유 참조가 왕복 뒤 두 사본이 됨
```
Restore 전:   party.Leader 와 party.Members[0] 의 개체 ID 가 같음 ($1)
스냅샷:       리더의 name/hp 가 members 안과 leader 에 두 번 적힘
Restore 뒤:   restored.Leader 와 restored.Members[0] 의 개체 ID 가 다름
진짜 원인:  JSON 에는 "같은 객체" 개념이 없다. 적힌 곳마다 새 객체를 만든다. MainTank 도 마찬가지.
```
**수정:** 저장·복원 양쪽 옵션에 `ReferenceHandler = ReferenceHandler.Preserve`를 준다(`$id`/`$ref`가 붙는다). `Lab57_PartySnapshot.cs`에서는 `Serialize`와 `Deserialize`가 같은 `Options` 필드를 쓰므로 그 필드 하나만 아래로 바꾸면 양쪽에 적용된다. 파일 맨 위의 `using` 줄들에 `using System.Text.Json.Serialization;`을 더한다(`ReferenceHandler`가 있는 네임스페이스). `Restore`와 `Run`은 그대로 둔다.
```csharp
using System.Text.Json.Serialization;

    // 한글을 이스케이프하지 않고 그대로 쓴다(콘솔에서 읽기 쉽게). 같은 객체는 $id/$ref 로 한 번만 적는다.
    static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        ReferenceHandler = ReferenceHandler.Preserve,
    };
```
다른 방법: 형식을 바꾼다. `Leader` 대신 `LeaderIndex`(또는 고유 Id)를 저장하고 읽은 뒤 다시 잇는다.

**교훈:** 객체 그래프를 그대로 직렬화하면 공유가 사라지고, 순환이 있으면 `JsonException`(깊이 초과)이 납니다. 세이브 형식으로는 Id로 참조하는 쪽이 더 단단합니다(`$ref`는 다른 프로그램이 이해하지 못할 수 있음). 리더만 이름으로 다시 잇는 수정은 메인 탱커에서 걸립니다.

## Lab 58 — 없는 값(missing), null, 0을 구별하지 못함
```
{"item":"마나 물약"}              → 받음 "마나 물약 x0"     (count 없음 → int 기본값 0, 예외 없음)
{"item":"화살","count":null}      → 거절                    (int 에 null → JsonException)
{"item":null,"count":5}  (숨은)   → 받음 "(null) x5"        (string 은 null 을 받을 수 있음)
진짜 원인:  System.Text.Json 은 JSON 에 없는 속성을 건드리지 않는다. nullable 주석(string 은 null 아님)도 기본으로는 검사하지 않는다.
```
**수정:** "있는가"는 `required`로, "null인가"는 `RespectNullableAnnotations`로 검사한다. `Lab58_RewardPacket.cs`의 `MailPacket` 클래스와 바로 아래의 `Options` 필드를(둘이 이어져 있다) 아래로 바꾼다. `Decode`와 `Run`은 그대로 둔다.
```csharp
    sealed class MailPacket
    {
        public required string Item { get; set; } = "";
        public required int Count { get; set; }
    }

    static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { RespectNullableAnnotations = true };
```
**교훈:** `required`(또는 `[JsonRequired]`)는 "있는가"만 보고, null 여부는 `RespectNullableAnnotations`(.NET 9+)가 봅니다. 둘은 다른 질문입니다. `required`만 붙이면 숨은 케이스(`"item": null`)에서 걸립니다. 받은 뒤 `Count == 0`을 거르는 수정은 진짜 0개를 구별하지 못하고 이름 없는 보상도 못 거릅니다.

## Lab 59 — `XmlSerializer`: 매개변수 없는 생성자와 `Dictionary`
```
예외:   InvalidOperationException: There was an error reflecting type 'BugHunt.Lab59.GameOptions'.
 ---> InvalidOperationException: Cannot serialize member 'BugHunt.Lab59+GameOptions.Graphics' of type 'BugHunt.Lab59+GraphicsOptions', see inner exception for more details.
 ---> InvalidOperationException: BugHunt.Lab59.GraphicsOptions cannot be serialized because it does not have a parameterless constructor.
멈춘 곳:  new XmlSerializer(typeof(GameOptions))   (데이터와 상관없이 생성자에서)
그것을 고치면 다음:  Cannot serialize member ...KeyBindings of type Dictionary`2 ... because it implements IDictionary.
```
**수정:** `GraphicsOptions`를 매개변수 없는 생성자와 public set이 있는 class로. `KeyBindings`는 `[XmlIgnore]`로 두고 XML에는 배열 대리 속성으로 적는다. `Lab59_OptionsXml.cs`의 `GraphicsOptions` record와 `GameOptions` 클래스를 지우고 그 자리에 아래 두 클래스를 넣는다. `Run`이 `new GraphicsOptions(w, h, fullscreen)`으로 만들므로 3인자 생성자도 남긴다. `using System.Xml.Serialization;`은 파일에 이미 있고, `Save`, `Load`, `Run`은 그대로 둔다.
```csharp
    // XmlSerializer 가 다루는 형식은 public 이어야 하고, 매개변수 없는 생성자와 public set 이 있어야 한다.
    public sealed class GraphicsOptions
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public bool Fullscreen { get; set; }

        public GraphicsOptions() { }   // XmlSerializer 가 읽을 때 쓴다
        public GraphicsOptions(int width, int height, bool fullscreen) => (Width, Height, Fullscreen) = (width, height, fullscreen);
    }

    public sealed class GameOptions
    {
        public int Volume { get; set; } = 80;
        public GraphicsOptions Graphics { get; set; } = new(1280, 720, false);

        // XmlSerializer 는 IDictionary 를 못 다루므로 XML 에서는 빼고, 아래 배열로 대신 적는다
        [XmlIgnore] public Dictionary<string, string> KeyBindings { get; set; } = [];

        [XmlArray("KeyBindings")]
        public KeyBinding[] KeyBindingList
        {
            get => [.. KeyBindings.Select(p => new KeyBinding { Action = p.Key, Key = p.Value })];
            set => KeyBindings = value.ToDictionary(b => b.Action, b => b.Key);
        }

        public sealed class KeyBinding
        {
            [XmlAttribute] public string Action { get; set; } = "";
            [XmlAttribute] public string Key { get; set; } = "";
        }
    }
```
또는 이 파일을 System.Text.Json으로 저장한다(record와 Dictionary를 그대로 다룸).
**교훈:** `XmlSerializer`는 public 형식, 매개변수 없는 생성자, 읽고 쓸 수 있는 public 멤버만 다루고 `IDictionary`는 못 다룹니다. 형식 전체를 생성자에서 검사하므로 "There was an error reflecting type"이 나오면 **내부 예외를 끝까지** 읽습니다(랩 6의 `AggregateException`과 같은 읽는 법).

## Lab 60 — 기본 네임스페이스가 붙은 XML을 접두어 없는 XPath로 찾음
```
조사식 doc.SelectSingleNode("/quests"):            null
조사식 doc.DocumentElement.NamespaceURI:           "urn:bughunt:quests"
조사식 doc.SelectSingleNode("/*[local-name()='quests']"):   찾음
진짜 원인:  xmlns="urn:bughunt:quests" 로 모든 요소가 그 네임스페이스에 속하게 됐는데, XPath 1.0 의 접두어 없는 이름은 "네임스페이스 없음"만 찾는다.
           RewardGold 가 못 찾은 것(null)을 0G 로 바꿔 조용히 넘어갔다.
```
**수정:** 네임스페이스에 접두어를 붙여 XPath로 찾는다. `Lab60_QuestXml.cs`의 `RewardGold`를 아래로 바꾼다. `using System.Xml;`은 파일에 이미 있고, `Run`은 그대로 둔다.
```csharp
    static int RewardGold(XmlDocument doc, string questId)
    {
        var ns = new XmlNamespaceManager(doc.NameTable);
        ns.AddNamespace("q", "urn:bughunt:quests");
        XmlNode? reward = doc.SelectSingleNode($"/q:quests/q:quest[@id='{questId}']/q:reward", ns);
        string? gold = reward?.Attributes?["gold"]?.Value;
        return gold == null ? 0 : int.Parse(gold, CultureInfo.InvariantCulture);
    }
```
LINQ to XML이면 `XNamespace q = "urn:bughunt:quests";` 그리고 `root.Elements(q + "quest")`.
**교훈:** 속성은 접두어가 없으면 네임스페이스에 속하지 않으므로 `@id`는 그대로 씁니다. 필수 데이터를 못 찾았을 때 기본값(0)을 돌려주면 이런 버그가 숨습니다. 예외를 내세요. (공백 텍스트 노드: `PreserveWhitespace = true`로 읽으면 들여쓰기도 노드가 되어 `ChildNodes`/`FirstChild`가 엉뚱한 노드를 가리킵니다. 요소만 볼 때는 `SelectNodes`, `Elements()`를 씁니다.)

## Lab 61 — 문자열로 이어 붙인 XML의 이스케이프 누락
```
(받는 쪽) 읽을 수 없는 메시지를 버림: Name cannot begin with the '3' character, hexadecimal value 0x33. Line 1, position N.
보낸 XML:   <invite guild="불꽃 연대" from="케인"><message>레이드 같이 가요 <3</message></invite>
진짜 원인:  BuildInvite 가 사용자 입력을 그대로 XML 문법 안에 넣는다. < 와 & 는 텍스트에서, " 는 속성 값에서 문서를 깨뜨린다.
           받는 쪽은 XmlException 을 삼켜 "도착하지 않음"으로 만들었다.
```
**수정:** 구조를 아는 API가 XML을 만들게 한다. `Lab61_GuildInvite.cs`의 `BuildInvite`(두 줄짜리 식 본문 메서드)를 아래로 바꾼다. `using System.Xml.Linq;`는 파일에 이미 있고, `Receive`와 `Run`은 그대로 둔다.
```csharp
    static string BuildInvite(string guild, string from, string message) =>
        new XElement("invite", new XAttribute("guild", guild), new XAttribute("from", from), new XElement("message", message)).ToString(SaveOptions.DisableFormatting);
```
`XmlWriter`도 된다. 꼭 문자열이어야 하면 값마다 `SecurityElement.Escape`.
**교훈:** `<`만 바꾸는 수정은 숨은 케이스의 `&`, `"`에서 걸립니다. 같은 실수가 SQL 주입, HTML(XSS), 셸 명령, JSON 문자열 이어 붙이기에도 있습니다. 구조를 아는 API가 만들게 하세요.

### 참고: XML과 보안 (랩 없음)

- **XXE(외부 엔터티)**: .NET Core 이후 `XmlDocument`, `XDocument`, `XmlReader.Create`, `XmlSerializer`는 기본으로 외부 엔터티를 해석하지 않습니다(`XmlResolver`가 null, `DtdProcessing.Prohibit`). 신뢰할 수 없는 XML을 받을 때 `XmlReaderSettings { DtdProcessing = DtdProcessing.Parse, XmlResolver = new XmlUrlResolver() }`처럼 **기본값을 풀지 마세요**. .NET Framework 4.5.2 이전에서 옮겨 온 코드는 확인이 필요합니다.
- **엔터티 확장(Billion laughs)**: DTD를 허용해야 한다면 `MaxCharactersFromEntities`로 제한합니다.
- **형식 이름으로 객체 만들기**: `BinaryFormatter`(.NET 9에서 제거), Newtonsoft.Json의 `TypeNameHandling.All`, `XmlSerializer`에 바깥에서 받은 형식을 넘기는 코드는 원격 코드 실행으로 이어질 수 있습니다. 랩 56의 `JsonDerivedType`처럼 **허용 목록**을 쓰세요.

---

# 8부: 비동기와 동시성

## Lab 62 — async void의 예외는 catch에 오지 않는다
```
멈춘 곳:    SaveAsync의 throw (스레드 창: .NET TP Worker, 호출 스택에 AutoSave/Run 없음)
진짜 원인:  SaveAsync는 첫 await에서 바로 반환됨 → try 블록은 이미 끝남.
           async void라 실패를 전달할 Task가 없어서, 예외가 스레드 풀에서 다시 던져지고 프로세스가 죽음
```
**수정:** `async Task`로 바꾸고 기다린다. `Lab62_AutoSave.cs`에서 세 곳을 바꾼다.

1. `SaveSystem`의 `SaveAsync`를 아래로 바꾼다(`async void` → `async Task`).
```csharp
        public async Task SaveAsync(string slot)
        {
            await Task.Delay(50);
            if (slot == "checkpoint_3")
                throw new IOException(Text.L($"{slot}: 디스크 공간이 부족합니다", $"{slot}: ディスクの空き容量が不足しています"));
            Interlocked.Increment(ref SavedCount);
            Console.WriteLine(Text.L($"  {slot} 저장 완료", $"  {slot} セーブ完了"));
        }
```
2. `AutoSave`를 아래의 `AutoSaveAsync`로 바꾼다(`await`하므로 `async Task<int>`가 된다).
```csharp
    static async Task<int> AutoSaveAsync(SaveSystem saves, string[] checkpoints)
    {
        int failures = 0;
        foreach (string checkpoint in checkpoints)
        {
            try
            {
                await saves.SaveAsync(checkpoint);   // 실패는 이 await에서 예외로 나온다
            }
            catch (IOException ex)
            {
                failures++;
                Console.WriteLine(Text.L($"  [알림] 저장 실패: {ex.Message}", $"  [通知] セーブ失敗: {ex.Message}"));
            }
        }
        return failures;
    }
```
3. `Run`의 `int failures = AutoSave(...);` 줄을 아래 한 줄로 바꾼다(콘솔의 최상위이므로 동기로 기다려도 괜찮다).
```csharp
        int failures = AutoSaveAsync(saves, ["checkpoint_1", "checkpoint_2", "checkpoint_3"]).GetAwaiter().GetResult();
```
**교훈:** `async void`는 호출자가 **끝났는지도, 실패했는지도 알 수 없다.** 이벤트 처리기처럼 시그니처가 정해진 경우에만 쓴다.

## Lab 63 — `.Result` 데드락
```
모두 중단 → 병렬 스택:
    Main Thread (게임 루프):  Task.Result → ... → Lab63.OnEnterLoadingScreen  (막혀 있음)
작업 창:     LoadAssetsAsync  대기 중 (첫 await 이후 계속할 차례를 기다림)
main.PendingCount:  1  ← LoadAssetsAsync의 나머지가 메인 스레드 큐에 줄 서 있음
진짜 원인:  메인 스레드가 .Result로 막혀 있는데, await의 나머지는 그 메인 스레드에서만 실행될 수 있음
```
**수정:** 끝까지 async로 만든다(권장). `Lab63_LoadingScreen.cs`에서 두 곳을 바꾼다.

1. `OnEnterLoadingScreen`을 아래의 `OnEnterLoadingScreenAsync`로 바꾼다(`.Result` 대신 `await`).
```csharp
    static async Task OnEnterLoadingScreenAsync(LoadingScreen screen)
    {
        screen.Show();
        int loaded = await LoadAssetsAsync();   // 스레드를 막지 않고 기다린다. 나머지는 메인 스레드에서 실행된다
        screen.Close(loaded);
    }
```
2. `Run`의 `Task frame = main.Run(() => OnEnterLoadingScreen(screen));` 줄을 아래 한 줄로 바꾼다.
```csharp
        Task frame = main.Run(() => OnEnterLoadingScreenAsync(screen));
```
다른 방법: 라이브러리 쪽 `LoadAssetsAsync` 안을 `await Task.Delay(20).ConfigureAwait(false);`로 만든다. 메인 스레드로 돌아오지 않으므로 데드락은 풀린다. 단, 그 뒤에서 메인 스레드 전용 객체를 만지면 랩 67의 버그가 된다.

**교훈:** SynchronizationContext가 있는 스레드에서 비동기 작업을 **동기로 기다리면** 데드락이 된다. 콘솔 기본 스레드에는 컨텍스트가 없어서 재현되지 않으므로, 콘솔에서 시험한 코드가 UI/게임에서 멈춘다.

## Lab 64 — await 누락 (fire-and-forget)
```
Expect 줄의 작업 창:  SubmitAsync 3개, 모두 "대기 중"
Interlocked.Increment 중단점:  Expect 보다 나중에 걸림 (또는 프로세스가 먼저 끝나서 안 걸림)
진짜 원인:  _ = server.SubmitAsync(id); 로 Task를 버림. 아무도 끝나기를 기다리지 않음
```
**수정:** 보낸 Task를 기다린다. `Lab64_Achievements.cs`에서 두 곳을 바꾼다.

1. `SubmitUnlocked`를 아래의 `SubmitUnlockedAsync`로 바꾼다(`_ =` 대신 `await`. 로그 줄은 그대로 남긴다).
```csharp
    static async Task SubmitUnlockedAsync(AchievementServer server, IEnumerable<string> unlocked)
    {
        foreach (string id in unlocked)
        {
            await server.SubmitAsync(id);
            Console.WriteLine(Text.L($"  업적 전송: {id}", $"  実績送信: {id}"));
        }
    }
```
2. `Run`의 `SubmitUnlocked(server, [...]);` 줄을 아래 한 줄로 바꾼다.
```csharp
        SubmitUnlockedAsync(server, ["first_blood", "no_damage", "speed_run"]).GetAwaiter().GetResult();
```
동시에 보내도 된다면 1.의 루프 대신 `await Task.WhenAll(unlocked.Select(server.SubmitAsync));`.
**교훈:** Task를 돌려받으면 **누군가는 기다려야 한다.** `_ =`는 그 책임을 일부러 버린다는 표시다. CS4014를 끄려고 붙이면 안 된다.

## Lab 65 — `await Task.WhenAll`은 첫 예외만 던진다
```
throw 중단점:  두 번 걸림 (sounds.pak, locale.pak)
catch 안의 ex:  IOException "sounds.pak 다운로드 실패" 하나
downloads[3].Status  Faulted, downloads[3].Exception  locale.pak
진짜 원인:  WhenAll의 Task는 예외 둘을 AggregateException으로 갖고 있지만, await는 첫 번째 것만 꺼내 던짐
```
**수정:** catch 안에서 각 Task를 확인한다. `Lab65_PatchDownload.cs`의 `DownloadAllAsync`에서 `catch (Exception ex)` 블록(닫는 괄호까지)을 아래로 바꾼다.
```csharp
        catch (Exception)
        {
            foreach (Exception ex in downloads.Where(d => d.IsFaulted).Select(d => d.Exception!.InnerException!))
            {
                Console.WriteLine(Text.L($"  [로그] {ex.Message}", $"  [ログ] {ex.Message}"));
                failed.Add(ex.Message.Split(' ')[0]);
            }
        }
```
또는 `Task all = Task.WhenAll(downloads); try { await all; } catch { foreach (var ex in all.Exception!.InnerExceptions) ... }`.
**교훈:** 예외는 사라지지 않고 **Task 안에** 남아 있다. 여러 작업의 실패를 모두 다루려면 Task를 직접 본다.

## Lab 66 — lock 없는 경쟁 조건
```
결과:     매 실행마다 다른 값 (예: 타격 1,0xx,xxx / 기대 2,000,000)
병렬 스택:  Record 안에 스레드 4개가 동시에 있음
진짜 원인:  TotalDamage += damage 와 Hits++ 는 읽기-수정-쓰기 세 단계라, 두 스레드가 겹치면 한쪽 더하기가 사라짐
```
**수정:** 읽기-수정-쓰기를 나눌 수 없게 만든다. `Lab66_DamageMeter.cs`의 `DamageMeter.Record`를 아래로 바꾼다.
```csharp
        public void Record(int damage)
        {
            Interlocked.Add(ref TotalDamage, damage);
            Interlocked.Increment(ref Hits);
        }
```
다른 방법:
- `lock (_gate) { TotalDamage += damage; Hits++; }` (두 값을 **함께** 일관되게 바꿔야 한다면 lock. `DamageMeter`에 `readonly object _gate = new();`를 더한다)
- 파티원마다 지역 합계를 내고 마지막에 한 번만 더한다 (`Parallel.ForEach`의 localInit/localFinally 오버로드). 가장 빠르다.

**교훈:** 결과가 실행마다 다르면 경쟁 조건부터 의심한다. 중단점을 걸면 타이밍이 바뀌어 오히려 안 보일 수 있다.

## Lab 67 — ConfigureAwait(false) 뒤에서 메인 스레드 전용 객체를 만짐
```
RefreshHudAsync 첫 줄:  스레드 = Main Thread (게임 루프), SynchronizationContext.Current = MainThread
hud.SetGold 줄:         스레드 = .NET TP Worker, SynchronizationContext.Current = null
진짜 원인:  ConfigureAwait(false) 때문에 await 뒤가 메인 스레드로 돌아오지 않음
```
**수정:** 이 메서드에서는 `ConfigureAwait(false)`를 지운다. `Lab67_HudRefresh.cs`의 `RefreshHudAsync`에서 `Profile profile = await FetchProfileAsync().ConfigureAwait(false);` 줄을 아래 한 줄로 바꾼다. 메인 스레드로 돌아와야 하는 코드(UI, 게임 오브젝트)는 기본 동작(컨텍스트 캡처)을 써야 한다.
```csharp
        Profile profile = await FetchProfileAsync();
```
**교훈:** `ConfigureAwait(false)`는 **컨텍스트가 필요 없는 라이브러리 코드**용이다. 랩 63에서는 데드락을 푸는 도구였지만, 그 뒤에서 메인 스레드 전용 객체를 만지면 버그가 된다. 실제 WinForms/WPF/유니티에서는 여기서 예외가 나거나 화면이 가끔 깨진다.

## Lab 68 — CancellationToken을 넘기지 않음
```
SearchRegionAsync (region == 10):
    token.IsCancellationRequested  false
    token.CanBeCanceled            false   ← default(CancellationToken). 절대 취소되지 않는 토큰
FindMatchAsync 프레임의 token:   IsCancellationRequested true
진짜 원인:  FindMatchAsync가 SearchRegionAsync(region) 으로 부르면서 token을 빠뜨림. 기본값 = default 라 컴파일러도 조용함
```
**수정:** 토큰을 넘긴다. `Lab68_Matchmaking.cs`의 `FindMatchAsync`에서 `await SearchRegionAsync(region);` 줄을 아래 한 줄로 바꾼다.
```csharp
            await SearchRegionAsync(region, token);
```
가능하면 `CancellationToken` 매개변수에 기본값을 두지 않아서 빠뜨리면 컴파일 오류가 나게 한다.
**교훈:** 취소는 **사슬의 모든 고리**에 토큰을 넘겨야 전달된다. `CanBeCanceled == false`는 "누가 토큰을 안 넘겼다"는 신호다. CA2016이 이것을 잡는다.

## Lab 69 — 예외를 삼키고 성공을 돌려줌
```
1단계 콘솔:  [로그] 창고 업로드 경고: 영웅의 증표: 귀속 아이템은 창고에 넣을 수 없습니다
             영웅의 증표 보관 완료        ← 실패했는데 "완료"
첫 예외 중단(InvalidOperationException) → F10:  catch (Exception) 로 들어가 로그만 찍고 return true
진짜 원인:  UploadToStorageAsync가 모든 예외를 잡고 무조건 true. 호출자는 "맡겼다"고 믿고 가방에서 뺌.
           증상(아이템 소실)은 2단계에서 가방이 저장되고 3단계에서 다시 읽을 때 처음 보인다
```
**수정:** 처리 방법을 아는 예외만 잡고, 실패를 호출자에게 돌려준다. `Lab69_Storage.cs`의 `UploadToStorageAsync`를 통째로 아래로 바꾼다. 메서드 끝에 있던 `return true;`는 사라진다(`try`와 `catch`가 각각 값을 돌려주므로, 남겨 두면 도달할 수 없는 코드 경고 CS0162가 오류가 된다). `DepositAllAsync`와 `Run`은 그대로 둔다.
```csharp
    static async Task<bool> UploadToStorageAsync(StorageServer server, string item, int count)
    {
        try
        {
            await server.UploadAsync(item, count);
            return true;
        }
        catch (InvalidOperationException ex)   // 서버가 거절함: 가방에 그대로 둔다
        {
            Console.WriteLine(Text.L($"  [로그] 창고 업로드 거절: {ex.Message}", $"  [ログ] 倉庫アップロード拒否: {ex.Message}"));
            return false;
        }
    }
```
다른 예외(네트워크 끊김, 코드 버그)는 잡지 않고 위로 올려 보낸다. 거기서 잡을지는 그것을 처리할 줄 아는 쪽이 정한다.
**가짜 수정:** `if (item == Text.L("영웅의 증표", "英雄の証")) return false;`처럼 보이는 아이템만 막으면 기본 케이스는 통과하지만, 숨은 케이스는 다른 아이템이 귀속이라 실패한다. `catch (Exception)`을 그대로 두고 `return false`로만 바꾸면 이 랩은 통과하지만, 코드 버그까지 "보관 실패"로 바꿔 버리는 같은 냄새가 남는다.
**교훈:** `catch (Exception) { 로그 }` 뒤에 성공 값을 돌려주면, 실패가 **다른 단계의 다른 증상**으로 바뀌어 나타난다. 로그 한 줄은 처리가 아니다. 잡을 예외는 좁히고, 실패는 반환값이나 예외로 호출자에게 알린다.

## Lab 70 — 시간 초과를 "사용자가 취소함"으로 처리
```
첫 예외 중단(TaskCanceledException), Mira(서버 응답 없음):
    userCancel.IsCancellationRequested       false
    timeout.Token.IsCancellationRequested    true    ← CancelAfter(300) 이 취소함
    ex.CancellationToken                     연결된 토큰 (userCancel 이 아님)
진짜 원인:  catch (OperationCanceledException) 하나가 사용자 취소와 시간 초과를 둘 다 "취소됨"으로 바꿈.
           그래서 EnterAsync 의 "시간 초과면 예비 채널" 분기가 한 번도 실행되지 않음
```
**수정:** 예외 필터로 "누가 취소했나"를 구분한다. `Lab70_DungeonEntry.cs`의 `TryEnterAsync`에서 `catch (OperationCanceledException)` 블록(닫는 괄호까지)을 아래 두 catch로 바꾼다. `EnterAsync`와 `Run`은 그대로 둔다.
```csharp
        catch (OperationCanceledException) when (userCancel.IsCancellationRequested)
        {
            return Text.L("취소됨", "キャンセル済み");      // 플레이어가 눌렀다
        }
        catch (OperationCanceledException)
        {
            return "timeout";   // 우리가 건 CancelAfter. 화면에 나오지 않는 내부 표시라 번역하지 않는다 (EnterAsync 가 이 값을 보고 예비 채널로 다시 시도)
        }
```
시간 초과를 `throw new TimeoutException(...)`으로 바꿔 던지고 `EnterAsync`에서 `catch (TimeoutException)`으로 받아도 된다.
`when (ex.CancellationToken == userCancel)`은 **틀린** 수정이다. 예외에 실린 토큰은 연결된 토큰이라 이 조건은 늘 거짓이고, 진짜 사용자 취소가 시간 초과로 바뀐다.
**가짜 수정:** 시간 제한을 늘리거나 없애면 응답 없는 서버를 끝없이 기다린다. 이름으로 예외를 두면(`Mira`) 숨은 케이스에서 다른 플레이어가 걸린다.
**교훈:** `OperationCanceledException`은 "취소됐다"만 말하고 **누가** 취소했는지는 말하지 않는다. 토큰을 여러 개 엮었다면 `catch ... when (내 토큰.IsCancellationRequested)`로 구분한다. 예외 필터는 조건이 거짓이면 그 catch를 건너뛰고, 스택을 풀기 전에 평가되므로 첫 예외 중단과 같이 쓰기 좋다.

## Lab 71 — 잡아 둔 예외를 나중에 `throw ex;`로 다시 던짐
```
재시도 로그:  [재시도 3/3] FormatException: The input string '두백' was not in a correct format.
보고서:       FormatException @ RetryAsync          ← 형식은 맞는데 위치가 재시도 함수
첫 예외 중단(FormatException):  호출 스택 맨 위의 BugHunt 프레임은 ParseRewardAsync
개체 ID로 같은 예외임을 확인하고 throw last! 줄을 F10:  StackTrace 가 RetryAsync 에서 새로 시작
진짜 원인:  throw last; 는 스택 트레이스를 "지금 이 줄"부터 다시 쓴다. catch 블록 밖이라 throw; 도 못 쓴다
```
**수정:** 원래 스택 트레이스를 그대로 다시 던진다. `Lab71_QuestSync.cs`의 `RetryAsync`를 통째로 아래로 바꾼다(`Exception? last = null;`과 `throw last!;`는 사라진다). 파일 맨 위의 `using System.Text.RegularExpressions;` 옆에 `using` 두 줄을 더한다. `Run`과 `Report`는 그대로 둔다.
```csharp
using System.Diagnostics;                 // UnreachableException
using System.Runtime.ExceptionServices;   // ExceptionDispatchInfo

    static async Task<T> RetryAsync<T>(Func<Task<T>> body)
    {
        ExceptionDispatchInfo? last = null;
        for (int attempt = 1; attempt <= Attempts; attempt++)
        {
            try
            {
                return await body();
            }
            catch (Exception ex)
            {
                last = ExceptionDispatchInfo.Capture(ex);   // 이 순간의 스택 트레이스까지 함께 담아 둔다
                Console.WriteLine(Text.L($"  [재시도 {attempt}/{Attempts}] {ex.GetType().Name}: {ex.Message}", $"  [再試行 {attempt}/{Attempts}] {ex.GetType().Name}: {ex.Message}"));
                await Task.Delay(5);
            }
        }
        last!.Throw();
        throw new UnreachableException();   // 컴파일러용. Throw()는 돌아오지 않는다
    }
```
`ExceptionDispatchInfo.Throw(lastException)`(정적 메서드)도 같다. 또는 마지막 시도에서는 잡지 않는다: `catch (Exception ex) when (attempt < Attempts)`. 그러면 마지막 예외는 손대지 않은 채 그대로 올라간다.
**가짜 수정:** `throw new Exception(last.Message, last)`로 감싸면 예외 형식이 바뀌고 위치는 여전히 `RetryAsync`다. 보고서에 함수 이름을 박아 넣으면 숨은 케이스(헤더 손상, 다운로드 실패)에서 틀린다.
**교훈:** 예외를 저장했다가 **나중에, 다른 곳에서** 던질 때(재시도, 큐, `await` 경계를 넘긴 콜백)는 `ExceptionDispatchInfo`를 쓴다. `await`가 실패한 Task의 예외를 다시 던질 때 원래 스택이 살아 있는 것도 같은 장치 덕분이다. 랩 7의 `throw ex;`와 같은 뿌리지만 `throw;`로는 고칠 수 없다는 점이 다르다.

---

# 9부: 리소스와 메모리

## Lab 72 — Dispose하지 않은 StreamWriter
```
SaveReplay 마지막 줄:
    new FileInfo(path).Length    0
    writer.BaseStream.Position   1024 (StreamWriter가 FileStream 버퍼로 넘긴 양. 디스크에는 아직 0)
진짜 원인:  StreamWriter/FileStream은 버퍼에 모았다가 Flush/Dispose 때 씀. writer를 Dispose하지 않아서 버퍼가 디스크로 나가지 않음.
           핸들도 프로세스가 끝날 때까지 열려 있음
```
**수정:** writer를 반드시 Dispose한다. `Lab72_Replay.cs`의 `SaveReplay`에서 `var writer = new StreamWriter(path);` 줄을 아래 한 줄로 바꾼다.
```csharp
        using var writer = new StreamWriter(path);
```
`using (...) { }` 블록이나, 루프째 `File.WriteAllLines(path, frames)`로 바꿔도 된다.
**교훈:** `IDisposable`을 구현한 객체는 **반드시 Dispose**한다. GC는 메모리를 회수할 뿐, 버퍼를 플러시하거나 파일을 닫아 주는 시점은 보장하지 않는다. .NET은 종료할 때 종료자도 돌리지 않는다.

## Lab 73 — static 이벤트가 닫은 창을 붙잡음
```
메모리 스냅샷:  CombatLogWindow 50개, 각각 256KB 텍스처 캐시
루트 경로:     CombatLogWindow ← Action<string,int> (대리자의 Target) ← Lab73.GameEvents.DamageDealt (static 필드)
진짜 원인:     Open에서 += 로 구독하고 Close에서 -= 로 해지하지 않음.
              이벤트(대리자)가 구독자를 참조하고, static 필드는 프로그램이 끝날 때까지 GC 루트
```
**수정:** 닫을 때 구독을 해지한다. `Lab73_CombatLogWindow.cs`의 `CombatLogWindow.Close`를 아래로 바꾼다.
```csharp
        public void Close()
        {
            IsOpen = false;
            _lines.Clear();
            GameEvents.DamageDealt -= OnDamageDealt;   // static 이벤트가 창을 붙잡지 않게
        }
```
창의 수명이 명확하다면 `IDisposable`로 만들어 `Dispose`에서 해지한다.
**교훈:** "참조를 다 버렸는데 메모리가 안 줄어든다"면 **누가 아직 붙잡고 있는지**를 찾는다. 메모리 스냅샷의 루트 경로나 `dotnet-dump`의 `gcroot`가 그 사슬을 보여 준다. 오래 사는 객체(static, 싱글턴)의 이벤트에 짧게 사는 객체가 구독하는 구조가 가장 흔한 누수다.

## Lab 74 — 종료자에 자원 반납을 맡김
```
~Texture() 중단점:   Exit() 동안 안 걸림. 프로그램이 끝날 때까지도 안 걸림
GC.CollectionCount(0)  0   ← GC가 한 번도 안 돌았음
(조사식으로 GC.Collect() 실행 후) ~Texture() 가 "종료자 스레드"(.NET Finalizer)에서 걸림
진짜 원인:  종료자는 C++ 소멸자가 아님. 객체가 쓸모없어진 순간이 아니라, GC가 발견한 뒤 종료자 스레드가 짬이 날 때 불림
```
**수정:** `IDisposable`을 구현하고, 레벨을 나갈 때 명시적으로 Dispose한다. `Lab74_TextureCache.cs`에서 두 곳을 바꾼다. 원래의 종료자(`~Texture() { GpuMemory.Free(Bytes); }`)는 지우고 아래의 안전망 종료자로 대신한다. `GpuMemory`, `Level.Enter`, `Run`은 그대로 둔다.

1. `Texture` 클래스를 통째로 아래로 바꾼다.
```csharp
    class Texture : IDisposable
    {
        bool _disposed;

        public string Name { get; }
        public long Bytes { get; }

        public Texture(string name, long bytes)
        {
            Name = name;
            Bytes = bytes;
            GpuMemory.Allocate(bytes);
        }

        // 레벨을 나갈 때 바로 GPU 메모리를 돌려준다. 두 번 불려도 한 번만 돌려준다.
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            GpuMemory.Free(Bytes);
            GC.SuppressFinalize(this);
        }

        ~Texture() => Dispose();   // 안전망: Dispose를 잊었을 때만, 언젠가
    }
```
2. `Level` 클래스의 `Exit`를 아래로 바꾼다.
```csharp
        public void Exit()
        {
            foreach (Texture texture in _textures)
                texture.Dispose();
            _textures.Clear();
            Console.WriteLine(Text.L($"  {name} 퇴장: GPU {GpuMemory.BytesInUse / (1024 * 1024)}MB", $"  {name} 退場: GPU {GpuMemory.BytesInUse / (1024 * 1024)}MB"));
        }
```
(실무에서는 비관리 핸들을 직접 종료자로 다루기보다 `SafeHandle`을 쓴다.)
**교훈:** "정해진 순간"에 돌려줘야 하는 자원은 **Dispose**로. 종료자는 잊었을 때를 위한 안전망일 뿐이고, 언제 불릴지(불리기는 할지) 보장되지 않는다. `GC.Collect()`로 억지로 돌리는 것도 해결책이 아니다.

---

# 10부: unsafe와 네이티브 메모리

## Lab 75 — 메서드가 끝난 stackalloc 메모리를 가리키는 포인터
```
FindPath의 return 줄:          path[0] == 3, path[1] == 4   ← 여기까지는 맞다
DrawMinimapBackground 다음:    path[0] == -1 또는 쓰레기 값
(long)path 와 &pixels[0]:      같은 스택 영역(몇십 바이트 차이)
진짜 원인:  stackalloc 메모리는 FindPath의 스택 프레임 안에 있다. FindPath가 돌아가면 그 자리는 다음 호출이 다시 쓴다.
           Span<int>로 돌려주려다 CS8352로 막히자 포인터로 바꿔서 컴파일러의 검사를 피했다.
```
**수정:** 오래 살아야 하는 쪽(호출자)이 버퍼를 만들어 `Span`으로 넘긴다. `Lab75_MinimapPath.cs`에서 두 곳을 바꾼다.

1. `FindPath`를 아래로 바꾼다(위의 `[MethodImpl(MethodImplOptions.NoInlining)]` 줄은 그대로 둔다).
```csharp
    static int FindPath(int startTile, int goalTile, Span<int> path)
    {
        int step = startTile < goalTile ? 1 : -1;
        int length = 0;
        for (int tile = startTile; tile != goalTile + step; tile += step)
            path[length++] = tile;
        return length;
    }
```
2. `Run`의 `int* path = FindPath(start, goal, out int length);` 줄을 아래 두 줄로 바꾼다. `path[i]`로 읽는 다른 줄은 그대로 동작한다.
```csharp
        Span<int> path = stackalloc int[MaxPathLength];   // Run의 스택 프레임에 있으므로 Run이 끝날 때까지 유효
        int length = FindPath(start, goal, path);
```
(크기가 크거나 정해지지 않았다면 `int[]`나 `ArrayPool`을 쓴다.)
**교훈:** 컴파일러가 `Span`을 막는 것은 **수명 검사**다. 포인터, `Unsafe.AsRef`, `MemoryMarshal.CreateSpan`으로 그 검사를 우회하면 C++의 "지역 변수 주소 반환"과 똑같은 버그가 된다. 값을 망가뜨리는 것은 읽는 순간이 아니라 **그 사이의 다른 호출**이라서, 중단점을 거는 위치에 따라 멀쩡해 보이기도 한다.

## Lab 76 — fixed 블록 밖으로 빼낸 포인터를 GC 압축 뒤에 사용
```
Attach 줄의 (long)p:              처음 주소
LoadLevel 뒤 Samples의 주소:       다른 주소   ← 배열이 옮겨졌다
MixFrame의 (long)_buffer:          처음 주소 그대로
진짜 원인:  fixed는 "블록 안에서만" 배열을 고정한다. 블록이 끝난 뒤 GC.Collect(compacting)가 배열을 옮겼고,
           오디오 장치는 옛 주소(이제 비어 있는 곳)를 계속 읽는다.
```
**수정:** 처음부터 옮겨지지 않는 힙(POH)에 만든다. `Lab76_SoundMixer.cs`의 `Mixer`에서 `Samples` 속성 줄을 아래 한 줄로 바꾼다.
```csharp
        public short[] Samples { get; } = GC.AllocateArray<short>(32, pinned: true);
```
다른 방법:
- `GCHandle.Alloc(Samples, GCHandleType.Pinned)`로 고정하고, 장치에서 떼어 낼 때 `Free` (랩 77 참고)
- 네이티브가 오래 쓸 버퍼는 아예 `NativeMemory.Alloc`으로 GC 밖에 만들고 `NativeMemory.Free`로 돌려준다

**교훈:** 관리 참조는 GC가 같이 고쳐 주지만 **포인터는 GC가 모르는 숫자**다. 네이티브 쪽이 "주소를 기억해 두는" API라면 `fixed`로는 안 된다. 실제로는 GC가 언제 압축하느냐에 따라 오랫동안 멀쩡하다가 터지는 버그인데, 이 랩은 `GC.Collect(..., compacting: true)`로 그 순간을 고정해 두었다.

## Lab 77 — 예외 경로에서 GCHandle.Free 누락
```
살아 남은 버퍼 수:     2  == 손상된 패킷 수
throw 줄에서 F10:      handle.Free()로 가지 않고 catch로 빠짐
메모리 스냅샷 루트 경로: Byte[] ← 고정 핸들(Pinned Handle)
GC.GetGCMemoryInfo().PinnedObjectsCount:  손상된 패킷 수만큼 늘어 있음
진짜 원인:  체크섬이 틀리면 throw로 빠져나가면서 handle.Free()를 건너뜀
```
**수정:** 고정은 네이티브 호출 동안만 하고, `finally`에서 반드시 푼다. `Lab77_PacketReceive.cs`의 `ReceivePacket`을 통째로 아래로 바꾼다. 원래 `return buffer;` 바로 앞에 있던 `handle.Free();`는 지운다(두 번 풀면 `InvalidOperationException`이 난다). `using System.Runtime.InteropServices;`는 파일에 이미 있고, `ReceiveAll`과 `Run`은 그대로 둔다.
```csharp
    static byte[] ReceivePacket(int packetId, bool corrupt)
    {
        var buffer = new byte[256];
        Tracked.Add(new WeakReference(buffer));

        GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            NativeNet.Receive(handle.AddrOfPinnedObject(), buffer.Length, packetId, corrupt);
        }
        finally
        {
            handle.Free();   // 예외가 나도 반드시 푼다
        }

        byte sum = 0;
        for (int i = 0; i < buffer.Length - 1; i++)
            sum += buffer[i];
        if (sum != buffer[^1])
            throw new InvalidDataException(Text.L($"패킷 {packetId} 체크섬 불일치", $"パケット {packetId} チェックサム不一致"));

        return buffer;
    }
```
짧게만 고정한다면 `fixed (byte* p = buffer) { ... }`가 더 간단하다 (블록을 어떻게 빠져나가든 풀린다).
**교훈:** `GCHandle`은 GC 바깥의 강한 참조라서, `Free`를 잊으면 **GC가 있어도 누수**다. Pinned 핸들은 게다가 힙 압축도 방해한다. 얻었으면 `finally`에서 돌려준다. `lock`, 세마포(랩 87)과 같은 규칙이다.

## Lab 78 — 네이티브에 넘긴 콜백 대리자가 GC에 수거됨
```
콘솔(디버거 없이):  Process terminated.
                    A callback was made on a garbage collected delegate of type 'BugHunt!BugHunt.Lab78+BeatCallback::Invoke'.
F5:                 PlayBeat의 _onBeat(beat) 줄에서 멈춤
진짜 원인:  GetFunctionPointerForDelegate(OnBeat)에 넘긴 대리자는 그 자리에서 만들어진 임시 객체다.
           네이티브 쪽은 함수 포인터(숫자)만 가지고 있으므로 GC는 대리자를 쓰레기로 보고, LoadSong의 GC.Collect가 수거한다.
           대리자와 함께 네이티브 진입점(thunk)도 막혀서, 다음 박자에 런타임이 프로세스를 끝낸다.
```
**수정:** 네이티브가 콜백을 들고 있는 동안 대리자를 필드에 붙잡아 둔다. `Lab78_BeatCallback.cs`의 `RhythmGame.Start`를 아래(필드 하나 + `Start`)로 바꾼다. `OnBeat`와 `Run`은 그대로 둔다.
```csharp
        BeatCallback? _beatCallback;   // 네이티브가 들고 있는 동안 살아 있게

        public void Start()
        {
            _beatCallback = OnBeat;
            NativeAudio.SetBeatCallback(Marshal.GetFunctionPointerForDelegate(_beatCallback));
            Console.WriteLine(Text.L("  오디오 엔진에 박자 콜백 등록", "  オーディオ エンジンにビート コールバックを登録"));
        }
```
다른 방법: 대리자를 아예 쓰지 않는다. `[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])] static int OnBeat(int beat)`를 만들고 `&OnBeat`를 넘긴다. (static이라 게임 객체는 따로 찾아야 한다. `GCHandle.ToIntPtr`로 만든 "사용자 데이터" 포인터를 같이 넘기는 방식이 흔하다.)

`GC.KeepAlive(callback)`은 **그 줄까지만** 살려 두므로, 나중에 불리는 콜백에는 소용이 없다.
**교훈:** .NET Framework에서 MDA `CallbackOnCollectedDelegate`로 유명했던 버그다. 지금의 .NET은 수거된 대리자의 진입점을 막아 두기 때문에, 엉뚱한 곳으로 뛰지 않고 **이유를 적고 즉시 끝난다.** 그 메시지의 형식 이름이 범인이다.

## Lab 79 — ArrayPool에 반납한 배열을 Memory로 계속 사용
```
history.Add 중단점마다 history[0]:  "[Kael] 안녕" → 다음 메시지로 덮어써진 값 → ...   ← 매번 바뀐다
개체 ID:   FormatMessage의 buffer가 매번 같은 $1
진짜 원인:  Return(buffer) 뒤에도 buffer.AsMemory(...)를 돌려줘서 기록에 남김.
           같은 스레드에서 다시 Rent하면 방금 돌려준 그 배열이 나오고, 새 메시지가 옛 기록 위에 덮어써진다.
           ReadOnlyMemory/Span은 배열의 "창문"일 뿐 복사본이 아니다.
```
**수정:** 오래 둘 데이터는 반납하기 전에 복사한다. `Lab79_ChatHistory.cs`의 `FormatMessage`를 아래로 바꾼다. `Run`은 그대로 둔다.
```csharp
    static ReadOnlyMemory<char> FormatMessage(string sender, string text)
    {
        char[] buffer = ArrayPool<char>.Shared.Rent(128);
        int length = 0;
        foreach (string part in new[] { "[", sender, "] ", text })
        {
            part.CopyTo(buffer.AsSpan(length));
            length += part.Length;
        }
        string message = new(buffer, 0, length);   // 반납하기 전에 복사한다
        ArrayPool<char>.Shared.Return(buffer);
        return message.AsMemory();
    }
```
(더 간단히는 `string.Concat`으로 string을 바로 만든다. 풀은 **그 메서드 안에서만** 쓰는 임시 버퍼에 쓴다.)
**교훈:** `Return`은 "이제 다른 사람이 써도 된다"는 약속이다. 반납한 배열을 가리키는 `Span`, `Memory`, 슬라이스가 남아 있으면 해제 후 사용(use-after-free)과 같다. 크래시는 안 나고 **남의 데이터가 보일 뿐**이라 더 찾기 어렵다.

## Lab 80 — 구조체 레이아웃의 패딩
```
header:   Type 7, Length 엄청 큰 수(또는 음수), Seq 엉뚱한 값
Unsafe.SizeOf<PacketHeader>():   12   (1 + 4 + 2 = 7이 아님)
Marshal.SizeOf<PacketHeader>():  12
메모리 창(&header):  Type 다음에 빈 3바이트, Seq 뒤에 빈 2바이트
진짜 원인:  Sequential 레이아웃은 int를 4바이트 경계에 맞추려고 Type 뒤에 3바이트를 끼워 넣는다.
           그래서 Length를 1번이 아니라 4번 바이트부터 읽고, 본문도 7이 아니라 12번 바이트부터 자른다.
```
**수정:** 빈틈 없이 붙인다(크기 7). `Lab80_PacketHeader.cs`의 `PacketHeader` 위의 `[StructLayout(LayoutKind.Sequential)]` 줄을 아래 한 줄로 바꾼다.
```csharp
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
```
다른 방법: 구조체를 쓰지 않고 위치를 직접 정해서 읽는다 (엔디언도 명시되어 더 안전. `using System.Buffers.Binary;`가 필요하다). 읽는 부분은 아래와 같다.
  ```csharp
  byte type = packet[0];
  int length = BinaryPrimitives.ReadInt32LittleEndian(packet[1..]);
  ushort seq = BinaryPrimitives.ReadUInt16LittleEndian(packet[5..]);
  string text = Encoding.UTF8.GetString(packet.Slice(7, length));
  ```

**교훈:** 바이트 형식이 정해진 데이터(네트워크, 파일, 네이티브 구조체)를 구조체로 그대로 읽을 때는 **크기와 오프셋을 먼저 확인**한다. `Marshal.SizeOf`(마샬링 크기)와 `Unsafe.SizeOf`/`sizeof`(메모리 크기)는 다를 수도 있다. 예를 들어 `bool` 필드는 메모리에서는 1바이트지만 마샬링하면 4바이트(Win32 `BOOL`)다.


## Lab 81 — 입력 크기로 `stackalloc` 해서 스택 오버플로
```
조사식 packet.Length:  20000000 (입력에서 온 값)
Checksum 첫 줄 F10:    프로그램이 사라짐. [FAIL]도 예외 창도 없음. 채점기 결과 CRASH (0xC00000FD)
진짜 원인:  stackalloc byte[n]은 스택에 n 바이트를 잡는다. 스택은 보통 1 MB로 고정이라
           수백만 바이트를 잡으면 StackOverflowException. 이 예외는 catch로 못 잡고 프로세스를 즉시 끝낸다.
```
**수정:** `stackalloc` 크기는 작고 고정된 상한(`StackCap`) 안에서만 쓰고, 큰 입력은 힙(`ArrayPool<byte>`)에서 빌립니다. `Lab81_PacketBuffer.cs`의 `Checksum`을 아래(상수 `StackCap` + `Checksum`)로 통째로 바꾸고, 파일 맨 위(`namespace BugHunt;` 위)에 `using System.Buffers;`를 추가합니다. `Run`은 그대로 둡니다.
```csharp
using System.Buffers;

    const int StackCap = 1024;   // 스택에 잡아도 되는 최대 크기

    static long Checksum(byte[] packet)
    {
        byte[]? rented = packet.Length <= StackCap ? null : ArrayPool<byte>.Shared.Rent(packet.Length);
        try
        {
            Span<byte> scratch = rented is null ? stackalloc byte[StackCap] : rented;
            scratch = scratch[..packet.Length];
            packet.CopyTo(scratch);
            long sum = 0;
            foreach (byte b in scratch) sum += b;
            return sum;
        }
        finally
        {
            if (rented is not null) ArrayPool<byte>.Shared.Return(rented);
        }
    }
```
(그냥 `Span<byte> scratch = packet.Length <= StackCap ? stackalloc byte[StackCap] : new byte[packet.Length];`로 힙에 잡아도 됩니다. `OutOfMemoryException`은 잡히는 예외입니다.)
**교훈:** `stackalloc`의 크기는 **반드시 컴파일 시점에 아는 작은 상한** 안에 두세요. 입력·반복 횟수·재귀 깊이 중 어느 것이든 스택을 무제한으로 쓰면 오버플로입니다. 힙의 `OutOfMemoryException`은 그래도 잡을 수 있지만, 스택 오버플로는 잡을 수 없습니다.

## Lab 82 — `unsafe` 포인터 쓰기가 버퍼를 넘겨 옆 메모리를 덮어씀
```
중단점 p[i] = incoming[i], i == 4:  scores는 4칸(0~3)인데 p[4]에 씀
조사식 block[4] (가드):             쓰기 직후 0xC0FFEE → 50 으로 바뀜
진짜 원인:  포인터 p[i]는 경계 검사를 하지 않는다. p[4]는 버퍼 바로 뒤(가드 칸)를 덮어쓴다.
           같은 자리를 안전한 인덱서 scores[4]로 쓰면 IndexOutOfRangeException이 나서 바로 드러난다.
```
**수정:** 포인터로 쓸 때는 버퍼 용량으로 루프를 제한합니다. `Lab82_ComboBuffer.cs`의 `Fill`에서 `fixed` 블록 안의 `for (int i = 0; i < incoming.Length; i++)` 줄을 아래 한 줄로 바꿉니다.
```csharp
            for (int i = 0; i < incoming.Length && i < scores.Length; i++)
```
또는 포인터를 쓰지 말고 안전한 `Span` 인덱서를 쓰고, 넘치는 입력은 자르거나 거절하는 정책을 명시합니다.
**교훈:** `unsafe` 포인터, `Marshal`, `Unsafe.Add`로 메모리를 다룰 때는 경계 검사가 사라집니다. 한 칸만 넘겨도 예외가 아니라 **조용한 손상**이 되고, 그 피해는 전혀 다른 곳에서 나타나 추적이 어렵습니다. 성능을 위해 `unsafe`가 꼭 필요한 게 아니라면 `Span<T>`/`stackalloc`의 안전한 인덱서를 쓰세요.

### 참고: 네이티브 힙 손상 (랩 없음)
- `Marshal.AllocHGlobal` + `Marshal.Copy`로 네이티브 힙을 넘겨 쓰면 CLR 힙이나 다른 할당을 망가뜨릴 수 있습니다. 다만 그 결과(크래시인지, 조용한 손상인지, 어디가 깨지는지)가 런타임과 힙 상태에 따라 달라 **결정적이지 않아** 랩으로 넣지 않았습니다.
- `AllocHGlobal` 후 `FreeHGlobal`을 빼먹으면 네이티브 메모리 누수입니다. 이는 9부(리소스·메모리)의 누수 랩과 주제가 겹칩니다.

---

# 11부: 멈춤 - 무한루프와 데드락

## Lab 83 — 실수 누적과 `!=` 종료 조건
```
모두 중단 → 랩 본문 스레드:  ZoomTo의 for 줄
zoom:    목표(2.0)를 한참 지난 값 (계속 커지다가, 어느 크기부터는 0.1을 더해도 안 바뀜)
(double)zoom (2.0 근처):  1.9999998807907104 다음 2.0999999046325684
진짜 원인:  0.1f를 누적한 값은 2.0f를 정확히 밟지 않는다. zoom != target은 영원히 참.
```
**수정:** 반복 횟수를 정수로 정하고, 값은 그 정수로 계산한다. `Lab83_CameraZoom.cs`의 `ZoomTo`를 아래로 바꾼다. `Run`은 그대로 둔다.
```csharp
    static void ZoomTo(Camera camera, float target, float step)
    {
        float start = camera.Zoom;
        int frames = (int)MathF.Round((target - start) / step);   // 몇 프레임인지 정수로 한 번만 정한다
        for (int frame = 0; frame < frames; frame++)
        {
            camera.Zoom = start + frame * step;
            camera.Render();
        }
        camera.Zoom = target;
    }
```
`zoom < target`으로만 바꾸면 멈추지는 않지만, 누적 오차 때문에 프레임이 하나 더 그려질 수 있다 (`1.9999998 < 2.0`).
**교훈:** 랩 13과 같은 원인(실수 동등 비교)이 **종료 조건**에 들어가면 "틀린 값"이 아니라 "안 끝남"이 된다. 실수는 범위(`<`)로 비교하거나, 반복 자체는 정수로 센다.

## Lab 84 — byte 루프 변수의 넘침
```
모두 중단 여러 번:  written은 계속 커지는데 slot은 항상 0~255
조건부 중단점 slot == 255 → F10:  slot == 0
진짜 원인:  byte는 255 다음에 0이 된다(unchecked). items.Count가 256 이상이면 slot < items.Count가 영원히 참.
```
**수정:** 루프 변수를 비교 대상과 같은 `int`로 만든다. `Lab84_InventorySave.cs`의 `SaveInventory`에서 `for (byte slot = 0; slot < items.Count; slot++)` 줄을 아래 한 줄로 바꾼다.
```csharp
        for (int slot = 0; slot < items.Count; slot++)
```
세이브 형식이 정말 슬롯 번호를 1바이트로만 쓸 수 있다면, 저장하기 전에 개수를 검사해서 형식을 올리거나 오류를 낸다.
**교훈:** 루프 변수는 **비교 대상과 같은 형식**으로. 같은 함정: `for (uint i = n - 1; i >= 0; i--)` (uint는 0 아래로 가지 않는다). `checked` 블록이나 "산술 오버플로 검사" 옵션을 켜면 255 다음에서 `OverflowException`으로 바로 보인다.

## Lab 85 — 고리가 생긴 부모 사슬
```
가입 신청:  "승인"
모두 중단 → Leader()의 g:  Dawn → Ember → Frost → Gale → Dawn → ...   (개체 ID가 다시 나옴)
진짜 원인:  JoinUnder는 parent.Parent == this (A↔B, 길이 2인 고리)만 막는다.
           Dawn ← Ember ← Frost ← Gale 사슬에서 Dawn이 Gale 밑으로 들어가면 길이 4인 고리가 생기고, Leader()가 영원히 돈다.
```
**수정:** 새 부모에서 위로 끝까지 올라가며 자기 자신을 만나는지 본다. `Lab85_GuildAlliance.cs`의 `Guild.JoinUnder`를 아래로 바꾼다.
```csharp
        public bool JoinUnder(Guild parent)
        {
            for (Guild? g = parent; g != null; g = g.Parent)
                if (g == this)
                    return false;   // 고리가 생기는 가입은 거절
            Parent = parent;
            return true;
        }
```
데이터가 이미 망가졌을 때를 대비해 `Leader()`에 방문 집합(`HashSet<Guild>`)이나 최대 단계를 두면, 무한루프 대신 오류로 드러난다.
**교훈:** 그래프나 연결 리스트를 "끝(null)이 나올 때까지" 따라가는 코드는 **고리가 없다는 가정**에 기대고 있다. 그 가정은 데이터를 **바꾸는 곳**에서 지켜야 한다. 순회 쪽에서는 플로이드의 토끼와 거북이 알고리즘으로 고리를 찾을 수도 있다.

## Lab 86 — 잠금 순서가 엇갈린 데드락
```
병렬 스택:
    거래: Kael → Mira   Monitor.Enter ← Transfer(from=Kael, to=Mira)   lock (to.Sync) 줄에서 대기
    거래: Mira → Kael   Monitor.Enter ← Transfer(from=Mira, to=Kael)   lock (to.Sync) 줄에서 대기
진짜 원인:  한 스레드는 Kael을 쥐고 Mira를, 다른 스레드는 Mira를 쥐고 Kael을 기다린다.
```
**수정:** 모든 곳에서 같은 순서로 잠근다. `Lab86_Trade.cs`의 `Transfer`를 아래로 바꾼다. `WriteTradeLog`와 `Run`은 그대로 둔다.
```csharp
    static void Transfer(Player from, Player to, int gold)
    {
        Player first = from.Id < to.Id ? from : to;   // Id가 작은 쪽부터 먼저 잠근다
        Player second = first == from ? to : from;
        lock (first.Sync)
        {
            WriteTradeLog(from, to, gold);
            lock (second.Sync)
            {
                from.Gold -= gold;
                to.Gold += gold;
            }
        }
    }
```
느린 일(`WriteTradeLog`)은 잠금 밖으로 빼는 것도 좋다. 잠금을 쥔 시간이 짧을수록 교착 상태와 경합이 줄어든다.
**교훈:** 데드락은 **잠금 둘 + 엇갈린 순서**로 생긴다. 모두 중단 → 병렬 스택에서 `Monitor.Enter`로 기다리는 스레드들을 찾고, 각자 **이미 쥔 것**(바깥 lock)과 **기다리는 것**(안쪽 lock)을 적어 보면 고리가 보인다. 명령줄에서는 `dotnet-dump analyze`의 `syncblk`가 잠금 주인 스레드를 보여 준다.

## Lab 87 — 예외 경로에서 SemaphoreSlim.Release 누락
```
작업 창:               DownloadAsync 여러 개가 "대기 중" — SemaphoreSlim.WaitAsync를 기다림
downloader.FreeSlots:  0
받은 파일:             1개, 실패 2개 → 그 뒤로 아무도 슬롯을 못 얻음
진짜 원인:  FetchAsync가 예외를 던지면 _slots.Release()를 건너뜀. 실패가 maxParallel번 쌓이면 슬롯이 0개가 된다.
```
**수정:** 얻은 슬롯은 `finally`에서 돌려준다. `Lab87_DownloadSlots.cs`의 `Downloader.DownloadAsync`를 아래로 바꾼다. `FetchAsync`, `DownloadAll`, `Run`은 그대로 둔다.
```csharp
        public async Task<bool> DownloadAsync(string file)
        {
            await _slots.WaitAsync();
            try
            {
                byte[] data = await FetchAsync(file);
                Interlocked.Increment(ref Downloaded);
                return data.Length > 0;
            }
            finally
            {
                _slots.Release();   // 예외가 나도 반드시 돌려준다
            }
        }
```
**교훈:** 세마포는 잠금과 같다. **얻었으면 `finally`에서 돌려준다.** `Monitor.Wait`/`Pulse`도 같은 종류의 멈춤을 만든다. 조건을 `if`로 한 번만 검사하거나, 기다리기 전에 온 `Pulse`를 놓치면 영원히 기다린다 (`while (조건) Monitor.Wait(gate);`가 기본형).

## Lab 88 — volatile 없는 바쁜 대기 (Release에서만)
```
Debug:     정상 (기다리며 수억 번 확인)
Release:   응답 없음. 멈춘 순간 loader.Done == True, 읽은 에셋 20개 (로딩은 이미 끝났다!)
디스어셈블리 (Release, WaitForLoading의 루프):
    Done을 루프에 들어가기 전에 한 번만 읽고, 루프 안에서는 spins++ 와 점프만 반복한다
진짜 원인:  JIT 최적화가 "루프 안에서 아무도 Done을 쓰지 않는다"고 보고 읽기를 루프 밖으로 꺼냈다.
           다른 스레드가 Done = true를 써도 이 루프는 영영 보지 못한다. Debug는 최적화를 끄므로 매번 메모리에서 읽는다.
```
**수정:** 다른 스레드가 쓰는 값은 매번 메모리에서 읽게 한다. `Lab88_LoadingSpin.cs`의 `Loader`에서 `public bool Done;` 줄을 아래 한 줄로 바꾼다.
```csharp
        public volatile bool Done;
```
다른 방법:
- `Done`은 그대로 두고 `WaitForLoading`의 루프를 `while (!Volatile.Read(ref loader.Done))`로 만든다
- 바쁜 대기를 없앤다: `ManualResetEventSlim`을 `Set`/`Wait`하거나, 로딩을 `Task`로 만들어 기다린다. CPU를 태우지 않는다.

**교훈:** "Debug에서는 되는데 Release에서만 멈춘다"면 **최적화가 바꿀 수 있는 가정**(다른 스레드와 공유하는 메모리)을 의심한다. 다른 스레드가 바꾸는 값은 `volatile`/`Volatile`/`Interlocked`/`lock` 중 하나를 거쳐서 읽고 쓴다.

---

# 12부: 성능 - 틀리지는 않는데 느리다

12부의 랩은 결과가 모두 맞습니다. `[FAIL]`은 측정값(할당 바이트, 비교 횟수, 예외 수)이 한도를 넘었을 때 납니다. 측정값은 실행 시간과 달리 PC 부하에 흔들리지 않아서, 고친 뒤에는 한도보다 훨씬 작게(대부분 0) 나옵니다.

## Lab 89 — 핫 루프 안의 LINQ와 캡처하는 람다
```
할당 추적:   Func<Enemy, bool>, <>c__DisplayClass..가 프레임 수만큼
버그 상태:   프레임당 약 96바이트 (기대: 8 이하)
진짜 원인:  enemies.Count(e => ... player ... radius)의 람다가 바깥 변수 player, radius를 캡처한다.
           부를 때마다 캡처를 담을 객체 하나와 대리자 하나가 새로 만들어진다. 2000프레임이면 4000개.
```
**수정:** 평범한 `foreach`로 센다. `radius * radius`도 루프 밖에서 한 번만 계산한다. `Lab89_ThreatMeter.cs`의 `CountNearby`를 아래로 바꾼다. 람다가 없으니 캡처 객체도 대리자도 만들어지지 않는다. `Run`은 그대로 둔다.
```csharp
    static int CountNearby(List<Enemy> enemies, Vec player, float radius)
    {
        float radiusSq = radius * radius;
        int count = 0;
        foreach (Enemy e in enemies)
            if (e.Alive && e.Pos.DistanceSq(player) <= radiusSq)
                count++;
        return count;
    }
```

**교훈:** 1초에 수십 번 도는 코드(Update, 렌더, 서버 틱)에서 LINQ와 캡처하는 람다는 "보이지 않는 `new`"다. 결과가 같아도 GC가 자주 돌고 그때마다 모든 스레드가 잠깐 멈춘다. 캡처하지 않는 람다는 캐시되므로 `static` 람다로 써 두면 실수로 캡처하는 것을 컴파일러가 막아 준다.

## Lab 90 — 루프 안의 문자열 `+=`
```
버그 상태:   할당이 결과 문자열 크기의 약 1500배 (기대: 8배 이하)
조사식:      text.Length가 줄마다 커진다. += 할 때마다 그 길이만큼 통째로 복사
진짜 원인:  string은 불변이라 a += b는 a 전체를 복사한 새 문자열을 만든다. n줄이면 복사량이 n²에 비례한다.
```
**수정:** `StringBuilder`에 `Append`하고 마지막에 한 번 `ToString()`. `Lab90_ChatExport.cs`의 `Export`를 아래로 바꾸고, 파일 맨 위(`namespace BugHunt;` 위)에 `using System.Text;`를 넣는다. `Run`은 그대로 둔다.
```csharp
using System.Text;

    static string Export(List<ChatLine> lines)
    {
        var text = new StringBuilder();
        foreach (ChatLine line in lines)
            text.Append($"[{line.Minute / 60:00}:{line.Minute % 60:00}] {line.Sender}: {line.Text}\n");
        return text.ToString();
    }
```
(줄 수를 알면 `string.Join`, 파일로 쓸 거면 `StreamWriter`로 바로 쓰기)

**교훈:** 몇 개를 이어 붙이는 것은 괜찮다(컴파일러가 `string.Concat` 하나로 만든다). **루프** 안의 `+=`가 문제다. 데이터가 2배가 되면 시간이 4배가 되는 코드는 작은 시험 데이터로는 절대 안 드러난다. 숨은 케이스처럼 크기를 바꿔 재 보면 보인다.

## Lab 91 — `object`에 넣은 int (박싱)
```
할당 추적:   System.Int32가 이벤트 수만큼
버그 상태:   이벤트 1000개당 약 48000바이트 (기대: 64 이하)
진짜 원인:  Dictionary<string, object>에 int를 넣을 때마다 박싱으로 힙에 상자가 생긴다 (랩 4, 12).
```
**수정:** 값을 `int` 그대로 담는다. `Lab91_StatCounter.cs`의 `Stats` 클래스를 통째로 아래로 바꾼다(`object` 캐스트도 사라진다). `Run`은 그대로 둔다.
```csharp
    class Stats
    {
        // 값의 형식을 int로 고정한다. 넣어도 꺼내도 박싱이 일어나지 않는다
        readonly Dictionary<string, int> _values = [];

        public void Add(string key, int amount) => _values[key] = _values.GetValueOrDefault(key) + amount;

        public int Get(string key) => _values.GetValueOrDefault(key);
    }
```
값을 꺼내고 다시 넣는 조회 두 번이 아깝다면 `Add`를 `CollectionsMarshal.GetValueRefOrAddDefault(_values, key, out _) += amount;`로 만든다(`using System.Runtime.InteropServices;`가 필요하다).

**교훈:** "나중에 다른 형식도 담을지 몰라서" 만든 `object` 자리는 지금 당장 비용을 낸다. 숨은 박싱은 비제네릭 컬렉션, `object`/인터페이스 매개변수로 넘긴 struct, `IEquatable<T>` 없는 struct의 `Equals`, `string.Format`의 값 형식 인자(랩 94)에서도 생긴다.

## Lab 92 — `List.Contains`로 중복 제거 (O(n²))
```
버그 상태:   전리품 하나당 비교 약 1200번 (기대: 3 이하)
CPU 사용량:  시간 대부분이 List.Contains → ItemId.Equals
진짜 원인:  List.Contains는 처음부터 하나씩 비교한다. 전리품마다 도감 전체를 훑으니 비교는 전리품 수 × 도감 크기.
```
**수정:** "이미 있나?"는 `HashSet<ItemId>`에 묻고, 순서는 `List`에 따로 남긴다. `Lab92_LootDedupe.cs`의 `UniqueInOrder`를 아래로 바꾼다. `ItemId`와 `Run`은 그대로 둔다.
```csharp
    static List<ItemId> UniqueInOrder(List<ItemId> drops)
    {
        var seen = new HashSet<ItemId>();
        var unique = new List<ItemId>();
        foreach (ItemId id in drops)
            if (seen.Add(id))   // 처음 본 Id 일 때만 true
                unique.Add(id);
        return unique;
    }
```

**교훈:** 컬렉션은 "무엇을 자주 묻는가"로 고른다. 포함 여부·키로 찾기는 `HashSet`/`Dictionary`, 앞에서 빼기는 `Queue`, 정렬 유지는 `SortedSet`. 루프 안의 `Contains`, `IndexOf`, `Remove(item)`, `Insert(0, ...)`는 모두 한 번에 O(n)이다. 해시 컬렉션에 넣는 struct는 `IEquatable<T>`와 `GetHashCode`를 제대로 구현해야 박싱 없이 빠르다.

## Lab 93 — 부를 때마다 `new Regex`
```
CPU 사용량:  RegexParser, RegexWriter 등 패턴 해석 코드가 핫 경로에
버그 상태:   메시지당 약 2700바이트 (기대: 64 이하)
진짜 원인:  new Regex(pattern)는 패턴을 해석해서 실행할 수 있는 형태로 만든다. 그 일을 메시지마다 다시 한다.
```
**수정:** 한 번만 만든다. `Lab93_NameCheck.cs`의 `IsValidName` 한 줄(과 그 위 주석)을 아래로 바꾼다. `using System.Text.RegularExpressions;`는 파일에 이미 있고, `Run`은 그대로 둔다.
```csharp
    // 이름 규칙: 영문자로 시작, 영문자·숫자·밑줄 3~16자. 패턴은 한 번만 해석해 두고 재사용한다.
    static readonly Regex NameRule = new("^[A-Za-z][A-Za-z0-9_]{2,15}$");

    static bool IsValidName(string name) => NameRule.IsMatch(name);
```
소스 생성기를 써도 된다(빌드할 때 만들어 둔다). 그때는 클래스 선언을 `public static partial class Lab93`으로 바꾸고, `NameRule` 필드 대신 `[GeneratedRegex("^[A-Za-z][A-Za-z0-9_]{2,15}$")] private static partial Regex NameRule();`를 선언한 뒤 `NameRule().IsMatch(name)`으로 부른다.

정적 메서드 `Regex.IsMatch(name, pattern)`은 내부 캐시(기본 15개)를 쓰므로 이 랩에서는 통과하지만, 패턴이 많으면 캐시에서 밀려나 같은 일이 생긴다.

**교훈:** 만들 때 비싼 객체(`Regex`, `JsonSerializerOptions`, `HttpClient`)는 만들어 두고 재사용한다. 할당 추적에서 "내 코드에 없는 내부 형식"(`RegexNode`, `RegexCharClass`)이 잔뜩 보이면, 그것을 만들게 한 내 코드의 `new`를 호출 스택에서 찾는다.

## Lab 94 — 꺼진 로그의 `params` 배열과 박싱
```
지역 창(Log.Debug 첫 줄):  args = object[4] { -1, 0, 0f, 0f }  ← 이미 만들어져 있음
버그 상태:   이동 1회당 152바이트 (기대: 1 이하)
진짜 원인:  params object?[] 배열과 그 안의 상자(int, float 박싱)는 Log.Debug에 들어가기 "전에", 부르는 쪽에서 만들어진다.
           함수 안의 if (!DebugEnabled) return;은 이미 비용을 치른 뒤다.
```
**수정:** 부르는 쪽에서 감싼다. `Lab94_DebugLog.cs`의 `MoveAll`을 아래로 바꾼다. 로그가 꺼져 있으면 `Log.Debug`의 인자를 아예 계산하지 않으므로 `params` 배열도 박싱 상자도 만들어지지 않는다. `Log` 클래스와 `Run`은 그대로 둔다.
```csharp
    static void MoveAll(Unit[] units, float dx, float dy, int frame)
    {
        for (int i = 0; i < units.Length; i++)
        {
            units[i].X += dx;
            units[i].Y += dy;
            if (Log.DebugEnabled)   // 꺼져 있으면 인자를 만들지 않는다
                Log.Debug(Text.L("프레임 {0}: 유닛 {1} 이동 → ({2}, {3})", "フレーム {0}: ユニット {1} 移動 → ({2}, {3})"), frame, units[i].Id, units[i].X, units[i].Y);
        }
    }
```
다른 방법:
- 제네릭 오버로드 `Debug<T1, T2, T3, T4>(string format, T1 a, ...)`를 만들어 배열과 박싱을 없앤다
- 보간 문자열 처리기(`[InterpolatedStringHandler]`)로 꺼져 있으면 아무것도 만들지 않게 한다 (`ILogger`가 쓰는 방식)

참고: `[Conditional("DEBUG")]`는 **Release 빌드에서** 호출과 인자 계산을 통째로 지운다. Debug 빌드에서는 `DEBUG`가 정의되어 있어 호출과 `params` 배열·박싱이 그대로 남으므로, Debug로 채점하는 이 랩은 통과하지 못한다. 출시판의 비용을 없애는 별도 수단으로 기억해 두자.

**교훈:** 인자는 함수가 실행되기 전에 계산된다. `params`, 박싱, 보간 문자열(`$"..."`), 인자 안의 메서드 호출은 모두 "꺼진 기능"에서도 비용을 낸다. 비슷하게 안 보이는 비용으로 큰 struct를 값으로 넘기는 복사가 있다 (`in`, `ref readonly` 매개변수).

## Lab 95 — 예외로 흐름 제어
```
예외 설정에서 FormatException 체크:  거의 매번 멈춘다
출력 창:      "예외 발생: 'System.FormatException'"이 줄줄이
버그 상태:   처리 중 던져진 예외 약 11000개 (기대: 0)
진짜 원인:  "숫자가 아님"은 흔한 입력인데 int.Parse의 예외로 처리한다. 예외는 던질 때 스택 트레이스를 모으고 catch를 찾아 스택을 거슬러 올라가서,
           if 문보다 수천 배 비싸다.
```
**수정:** 예상되는 실패는 값으로 다룬다. `Lab95_ChatCommands.cs`의 `ParseRollMax`를 아래로 바꾼다. `Run`은 그대로 둔다.
```csharp
    static int ParseRollMax(string command)
    {
        string arg = command.Length > 6 ? command[6..] : "";
        return int.TryParse(arg, out int max) ? max : 100;   // 숫자가 아니면 예외 없이 기본값
    }
```

**교훈:** 예상되는 실패(사용자 입력, 파일 없음, 키 없음)는 값으로 다룬다: `TryParse`, `TryGetValue`, `File.Exists`. 예외는 정말 예외적인 상황용이다. `dotnet-counters`의 예외 수가 초당 수백 개를 넘거나 출력 창에 첫 예외 메시지가 쏟아지면 그 자체가 성능 버그의 단서다. 공격자가 예외 경로를 일부러 두드리면 서비스 거부가 되기도 한다.

---

