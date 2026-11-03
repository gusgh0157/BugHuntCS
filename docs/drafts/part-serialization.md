# 초안: 직렬화 JSON/XML (새 부)

> 이 파일은 README·SOLUTIONS·커버리지 표에 합칠 **초안**입니다. 번호는 임시 번호(`Lab106`~`Lab113`, 지금 메뉴 번호 74~81)이고, 재배치 때 원래 세션이 최종 번호로 바꿉니다. 최종 순서에서는 6부(… 5 파일 입출력 → **6 직렬화** → 7 비동기 …)입니다.
>
> 소스 위치: `BugHunt/Labs/PartX_Serialization/`. 숨은 케이스는 랩마다 3개. 쓰는 API는 `System.Text.Json`, `XmlSerializer`, `XmlDocument`, `XDocument`/`XElement`뿐입니다(외부 패키지 없음). 모든 랩이 메모리(문자열)에서만 직렬화하므로 디스크에 쓰지 않습니다.

| 임시 번호 | 지금 메뉴 번호 | 파일 |
|---|---|---|
| Lab106 | 74 | `Lab106_GuildNotice.cs` |
| Lab107 | 75 | `Lab107_CharacterSave.cs` |
| Lab108 | 76 | `Lab108_MailRewards.cs` |
| Lab109 | 77 | `Lab109_PartySnapshot.cs` |
| Lab110 | 78 | `Lab110_RewardPacket.cs` |
| Lab111 | 79 | `Lab111_OptionsXml.cs` |
| Lab112 | 80 | `Lab112_QuestXml.cs` |
| Lab113 | 81 | `Lab113_GuildInvite.cs` |

---

## README에 들어갈 부분 (한국어)

# N부: 직렬화 (JSON/XML)

직렬화 버그의 대부분은 **예외 없이 조용히** 일어납니다. 이름이 안 맞으면 건너뛰고, 못 쓰는 멤버는 빼고, 형식 정보가 없으면 기반 형식으로 만들고, 없는 값은 기본값으로 채웁니다. 이 부에서는 **직렬화된 문자열 자체를 눈으로 확인**하는 것부터 시작합니다. 콘솔에 찍힌 JSON/XML과 C# 객체를 나란히 놓고, 어느 쪽에서 무엇이 사라졌는지 찾으세요.

## N부에서 쓰는 도구

| 도구 | 하는 법 | 언제 |
|---|---|---|
| JSON/XML 시각화 도우미 | 문자열 변수 옆 돋보기 → **JSON 시각화 도우미** / **XML 시각화 도우미** | 직렬화 결과를 들여쓰기해서 봄. 무엇이 저장됐고 무엇이 빠졌는지 (랩 107, 108, 109) |
| 속성 접근자에 중단점 | `set` 쪽에 커서를 두고 F9 | 역직렬화가 그 속성을 쓰기는 하는지 (랩 106, 107) |
| 조사식의 `GetType()` | `received.Attachments[0].GetType()` | 파생 형식으로 돌아왔는지 (랩 108) |
| 개체 ID | 변수 우클릭 → 개체 ID 만들기 | 저장 전에는 같은 객체였던 것이 복원 뒤에도 같은지 (랩 109) |
| 내부 예외 보기 | 예외 도우미 → **내부 예외 보기**, 조사식 `$exception.InnerException` | `XmlSerializer`의 "There was an error reflecting type"의 진짜 이유 (랩 111) |
| 조사식에서 XPath 시험 | `doc.SelectSingleNode("/quests")`, `doc.DocumentElement.NamespaceURI` | 어느 단계에서 못 찾는지 (랩 112) |
| 예외 설정 (first chance) | Ctrl+Alt+E → `System.Text.Json.JsonException`, `System.Xml.XmlException` 체크 | `catch`에 삼켜지는 파싱 오류 (랩 110, 113) |

## 따라 해 보기: 랩 109 (파티 스냅샷)

1. 명령 인수에 번호를 넣고 **F5**. `[FAIL] 리더가 파티원 목록의 그 사람 (기대: true, 실제: false)`.
2. `Party restored = Restore(party);` 줄에 **F9**, 다시 F5. 지역 창에서 `party.Leader`를 우클릭 → **개체 ID 만들기**(`$1`), `party.Members[0]`에도(`$2`가 아니라 `$1`이 붙습니다. 같은 객체입니다).
3. F10으로 `Restore`를 넘깁니다. 콘솔의 "스냅샷" 줄을 **JSON 시각화 도우미**로 보면 리더의 이름과 체력이 `members` 안과 `leader`에 **두 번** 적혀 있습니다.
4. 이번에는 `restored.Leader`와 `restored.Members[0]`에 개체 ID를 만듭니다. 번호가 **다릅니다**. → *처음 어긋난 줄*
5. JSON이 "같은 객체"를 어떻게 표현할 수 있을지, 혹은 객체 대신 무엇을 저장하면 될지 생각합니다. `MainTank`도 같은 문제를 가지고 있습니다.
6. 고친 뒤 채점기로 다른 파티 구성(숨은 케이스, 리더와 탱커가 같은 사람인 경우 포함)도 통과하는지 확인합니다.

## N부 랩 목록

| # | 이름 | 증상 | 새로 배우는 것 | 난이도 |
|---|---|---|---|---|
| 106 | 길드 공지 | 예외도 없는데 공지가 전부 빈 칸이다 | **JSON 이름 대소문자**: 기본은 정확히 일치, 모르는 속성은 조용히 무시, `JsonSerializerDefaults.Web`, `UnmappedMemberHandling` | ★ |
| 107 | 캐릭터 세이브 | 불러오면 골드는 0, 레벨은 1이 된다 | **필드와 `private set`은 저장·복원되지 않음**, `[JsonInclude]`, `IncludeFields`, 왕복 테스트 | ★ |
| 108 | 우편 보상 | 첨부한 보상이 전부 '알 수 없는 보상'이 된다 | **다형성**: 선언 형식으로 직렬화, `[JsonPolymorphic]`/`[JsonDerivedType]`, 등록 안 한 파생 형식 | ★★ |
| 109 | 파티 스냅샷 | 복원 뒤 리더가 맞아도 파티 창 체력은 그대로다 | **공유 참조가 왕복 뒤 두 사본**, 개체 ID, `ReferenceHandler.Preserve` 또는 Id로 저장 | ★★ |
| 110 | 보상 패킷 | 이상한 패킷을 거절하지 않고 x0 보상이 들어온다 | **없음(missing) vs null vs 0**, `required`, `RespectNullableAnnotations` | ★★ |
| 111 | 옵션 XML | 그래픽 옵션을 추가하자 저장하는 순간 죽는다 | **`XmlSerializer`의 규칙**: 매개변수 없는 생성자, `Dictionary` 미지원, 내부 예외 사슬 읽기 | ★★ |
| 112 | 퀘스트 XML | XML에 스키마를 붙인 뒤로 보상이 전부 0G다 | **XML 네임스페이스와 XPath**: `XmlNamespaceManager`, `XNamespace`, "못 찾으면 0"의 위험 | ★★ |
| 113 | 길드 초대장 | 어떤 길드의 초대장은 보내도 도착하지 않는다 | **문자열로 이어 붙인 XML의 이스케이프 누락**, `XElement`/`XmlWriter`로 만들기 | ★ |

(번호 열은 재배치 뒤의 최종 번호로 바꿉니다.)

---

## SOLUTIONS.md에 들어갈 부분 (한국어)

# N부: 직렬화 (JSON/XML)

## Lab 106 — JSON 이름의 대소문자가 안 맞아 조용히 기본값
```
받은 JSON:     {"title":"...","author":"...","priority":3,"pinned":true}
Title set 중단점:   Deserialize 중에 안 불림
진짜 원인:  System.Text.Json 기본 옵션은 이름을 대소문자까지 정확히 비교한다. "title" ≠ Title.
           모르는 속성은 기본으로 무시하므로 예외 없이 모든 속성이 초기값으로 남는다.
```
**수정:** `new JsonSerializerOptions(JsonSerializerDefaults.Web)` (또는 `PropertyNameCaseInsensitive = true`, `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`, 속성마다 `[JsonPropertyName]`).
**교훈:** 직렬화 옵션은 **상대와 맞추는 계약**입니다. 형식을 맞추는 동안에는 `UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow`로 "모르는 속성이면 예외"를 켜 두면 이런 버그가 바로 드러납니다. (참고: Newtonsoft.Json은 기본이 대소문자 무시라서, 옮겨 온 코드에서 이 버그가 자주 생깁니다.)

## Lab 107 — 필드와 `private set`은 왕복되지 않음
```
세이브 파일:   {"name":"아린","level":5}      ← gold 가 없음
Level private set 중단점:   Load 중에 안 불림
진짜 원인:  Gold 는 필드. System.Text.Json 은 기본으로 public 속성만 다룬다 → 저장부터 빠짐.
           Level 은 public get 이라 저장은 되지만 set 이 private 이라 읽을 때 건너뜀 → 초기값 1.
```
**수정:** `[JsonInclude] public int Gold;`, `[JsonInclude] public int Level { get; private set; } = 1;` (또는 `IncludeFields = true` + Level에 `[JsonInclude]`, 또는 `[JsonConstructor]` 생성자).
**교훈:** "저장은 되는데 못 읽는 멤버"는 경고가 없습니다. 세이브 형식에는 저장 → 불러오기 → 비교하는 왕복 테스트를 둡니다.

## Lab 108 — 다형성 정보가 없어 파생 형식이 기반 형식으로 돌아옴
```
서버 저장:    {"to":"아린","attachments":[{"source":"주간 퀘스트"},{"source":"주간 퀘스트"}]}   ← amount, itemName 없음
조사식 received.Attachments[0].GetType():   Lab108+Reward
진짜 원인:  List<Reward> 의 항목을 선언 형식 Reward 로 직렬화해서 파생 형식의 속성이 빠지고,
           JSON 에 형식 구분자가 없으니 읽을 때도 전부 Reward 로 만든다.
```
**수정:**
```csharp
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(GoldReward), "gold")]
[JsonDerivedType(typeof(ItemReward), "item")]
[JsonDerivedType(typeof(ExpReward), "exp")]
class Reward { ... }
```
**교훈:** System.Text.Json은 보안상 "JSON에 적힌 형식 이름으로 아무 형식이나 만드는" 기능이 없고, 허용할 파생 형식을 직접 등록합니다. 하나라도 빠뜨리면 그 형식을 직렬화할 때 `NotSupportedException`이 납니다(숨은 케이스의 경험치 보상). 기반 형식을 `abstract`로 만들어 두면 등록을 잊었을 때 역직렬화가 조용히 넘어가지 않고 예외를 내 줍니다.

## Lab 109 — 공유 참조가 왕복 뒤 두 사본이 됨
```
Restore 전:   party.Leader 와 party.Members[0] 의 개체 ID 가 같음 ($1)
스냅샷:       리더의 name/hp 가 members 안과 leader 에 두 번 적힘
Restore 뒤:   restored.Leader 와 restored.Members[0] 의 개체 ID 가 다름
진짜 원인:  JSON 에는 "같은 객체" 개념이 없다. 적힌 곳마다 새 객체를 만든다. MainTank 도 마찬가지.
```
**수정 (택1):**
- 저장·복원 양쪽 옵션에 `ReferenceHandler = ReferenceHandler.Preserve` (`$id`/`$ref`가 붙음).
- 형식을 바꾼다: `Leader` 대신 `LeaderIndex`(또는 고유 Id)를 저장하고 읽은 뒤 다시 잇는다.

**교훈:** 객체 그래프를 그대로 직렬화하면 공유가 사라지고, 순환이 있으면 `JsonException`(깊이 초과)이 납니다. 세이브 형식으로는 Id로 참조하는 쪽이 더 단단합니다(`$ref`는 다른 프로그램이 이해하지 못할 수 있음). 리더만 이름으로 다시 잇는 수정은 메인 탱커에서 걸립니다.

## Lab 110 — 없는 값(missing), null, 0을 구별하지 못함
```
{"item":"마나 물약"}              → 받음 "마나 물약 x0"     (count 없음 → int 기본값 0, 예외 없음)
{"item":"화살","count":null}      → 거절                    (int 에 null → JsonException)
{"item":null,"count":5}  (숨은)   → 받음 "(null) x5"        (string 은 null 을 받을 수 있음)
진짜 원인:  System.Text.Json 은 JSON 에 없는 속성을 건드리지 않는다. nullable 주석(string 은 null 아님)도 기본으로는 검사하지 않는다.
```
**수정:**
```csharp
public required string Item { get; set; } = "";
public required int Count { get; set; }
static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { RespectNullableAnnotations = true };
```
**교훈:** `required`(또는 `[JsonRequired]`)는 "있는가"만 보고, null 여부는 `RespectNullableAnnotations`(.NET 9+)가 봅니다. 둘은 다른 질문입니다. `required`만 붙이면 숨은 케이스(`"item": null`)에서 걸립니다. 받은 뒤 `Count == 0`을 거르는 수정은 진짜 0개를 구별하지 못하고 이름 없는 보상도 못 거릅니다.

## Lab 111 — `XmlSerializer`: 매개변수 없는 생성자와 `Dictionary`
```
예외:   InvalidOperationException: There was an error reflecting type 'BugHunt.Lab111.GameOptions'.
 ---> InvalidOperationException: Cannot serialize member 'BugHunt.Lab111+GameOptions.Graphics' of type 'BugHunt.Lab111+GraphicsOptions', see inner exception for more details.
 ---> InvalidOperationException: BugHunt.Lab111.GraphicsOptions cannot be serialized because it does not have a parameterless constructor.
멈춘 곳:  new XmlSerializer(typeof(GameOptions))   (데이터와 상관없이 생성자에서)
그것을 고치면 다음:  Cannot serialize member ...KeyBindings of type Dictionary`2 ... because it implements IDictionary.
```
**수정:** `GraphicsOptions`를 매개변수 없는 생성자와 public set이 있는 class로. `KeyBindings`는 `[XmlIgnore]`로 두고 XML에는 배열 대리 속성으로:
```csharp
[XmlIgnore] public Dictionary<string, string> KeyBindings { get; set; } = [];
[XmlArray("KeyBindings")]
public KeyBinding[] KeyBindingList
{
    get => [.. KeyBindings.Select(p => new KeyBinding { Action = p.Key, Key = p.Value })];
    set => KeyBindings = value.ToDictionary(b => b.Action, b => b.Key);
}
public sealed class KeyBinding { [XmlAttribute] public string Action { get; set; } = ""; [XmlAttribute] public string Key { get; set; } = ""; }
```
또는 이 파일을 System.Text.Json으로 저장한다(record와 Dictionary를 그대로 다룸).
**교훈:** `XmlSerializer`는 public 형식, 매개변수 없는 생성자, 읽고 쓸 수 있는 public 멤버만 다루고 `IDictionary`는 못 다룹니다. 형식 전체를 생성자에서 검사하므로 "There was an error reflecting type"이 나오면 **내부 예외를 끝까지** 읽습니다(랩 6의 `AggregateException`과 같은 읽는 법).

## Lab 112 — 기본 네임스페이스가 붙은 XML을 접두어 없는 XPath로 찾음
```
조사식 doc.SelectSingleNode("/quests"):            null
조사식 doc.DocumentElement.NamespaceURI:           "urn:bughunt:quests"
조사식 doc.SelectSingleNode("/*[local-name()='quests']"):   찾음
진짜 원인:  xmlns="urn:bughunt:quests" 로 모든 요소가 그 네임스페이스에 속하게 됐는데, XPath 1.0 의 접두어 없는 이름은 "네임스페이스 없음"만 찾는다.
           RewardGold 가 못 찾은 것(null)을 0G 로 바꿔 조용히 넘어갔다.
```
**수정:**
```csharp
var ns = new XmlNamespaceManager(doc.NameTable);
ns.AddNamespace("q", "urn:bughunt:quests");
XmlNode? reward = doc.SelectSingleNode($"/q:quests/q:quest[@id='{questId}']/q:reward", ns);
```
LINQ to XML이면 `XNamespace q = "urn:bughunt:quests";` 그리고 `root.Elements(q + "quest")`.
**교훈:** 속성은 접두어가 없으면 네임스페이스에 속하지 않으므로 `@id`는 그대로 씁니다. 필수 데이터를 못 찾았을 때 기본값(0)을 돌려주면 이런 버그가 숨습니다. 예외를 내세요. (공백 텍스트 노드: `PreserveWhitespace = true`로 읽으면 들여쓰기도 노드가 되어 `ChildNodes`/`FirstChild`가 엉뚱한 노드를 가리킵니다. 요소만 볼 때는 `SelectNodes`, `Elements()`를 씁니다.)

## Lab 113 — 문자열로 이어 붙인 XML의 이스케이프 누락
```
(받는 쪽) 읽을 수 없는 메시지를 버림: Name cannot begin with the '3' character, hexadecimal value 0x33. Line 1, position N.
보낸 XML:   <invite guild="불꽃 연대" from="케인"><message>레이드 같이 가요 <3</message></invite>
진짜 원인:  BuildInvite 가 사용자 입력을 그대로 XML 문법 안에 넣는다. < 와 & 는 텍스트에서, " 는 속성 값에서 문서를 깨뜨린다.
           받는 쪽은 XmlException 을 삼켜 "도착하지 않음"으로 만들었다.
```
**수정:** `new XElement("invite", new XAttribute("guild", guild), new XAttribute("from", from), new XElement("message", message)).ToString(SaveOptions.DisableFormatting)` (또는 `XmlWriter`). 꼭 문자열이어야 하면 값마다 `SecurityElement.Escape`.
**교훈:** `<`만 바꾸는 수정은 숨은 케이스의 `&`, `"`에서 걸립니다. 같은 실수가 SQL 주입, HTML(XSS), 셸 명령, JSON 문자열 이어 붙이기에도 있습니다. 구조를 아는 API가 만들게 하세요.

### 참고: XML과 보안 (랩 없음)

- **XXE(외부 엔터티)**: .NET Core 이후 `XmlDocument`, `XDocument`, `XmlReader.Create`, `XmlSerializer`는 기본으로 외부 엔터티를 해석하지 않습니다(`XmlResolver`가 null, `DtdProcessing.Prohibit`). 신뢰할 수 없는 XML을 받을 때 `XmlReaderSettings { DtdProcessing = DtdProcessing.Parse, XmlResolver = new XmlUrlResolver() }`처럼 **기본값을 풀지 마세요**. .NET Framework 4.5.2 이전에서 옮겨 온 코드는 확인이 필요합니다.
- **엔터티 확장(Billion laughs)**: DTD를 허용해야 한다면 `MaxCharactersFromEntities`로 제한합니다.
- **형식 이름으로 객체 만들기**: `BinaryFormatter`(.NET 9에서 제거), Newtonsoft.Json의 `TypeNameHandling.All`, `XmlSerializer`에 바깥에서 받은 형식을 넘기는 코드는 원격 코드 실행으로 이어질 수 있습니다. 랩 108의 `JsonDerivedType`처럼 **허용 목록**을 쓰세요.

---

## 커버리지 표에 들어갈 줄

새 범주 **직렬화**를 추가합니다. 챌린지 C-1의 범주 표("직렬화 왕복 + 캐시·중복 검사")는 그대로 둡니다.

| 범주 | 대표 케이스 | 랩 |
|---|---|---|
| **직렬화** | JSON 이름 대소문자, 모르는 속성은 조용히 무시 | 106 |
| | 필드·`private set` 미저장 (`[JsonInclude]`, `IncludeFields`) | 107 |
| | 다형성: 파생 형식이 기반으로 저장 (`JsonDerivedType`) | 108 |
| | 공유 참조가 왕복 뒤 두 사본 (`ReferenceHandler.Preserve`) | 109 |
| | 없는 값 vs null vs 0 (`required`, `RespectNullableAnnotations`) | 110 |
| | `XmlSerializer`: 매개변수 없는 생성자, `Dictionary` 미지원 | 111 |
| | XML 네임스페이스와 XPath (`SelectSingleNode`가 null) | 112 |
| | 문자열로 이어 붙인 XML의 이스케이프 누락 | 113 |
| | 공백 텍스트 노드(`PreserveWhitespace`) | 미포함 (랩 112 해답에서 언급) |
| | `DateTime` Kind 손실 | 미포함 (랩 16과 겹침. 판정을 고정하려면 PC의 시간대에 기대야 함) |
| | XXE·엔터티 확장, 형식 이름으로 객체 만들기(`BinaryFormatter`, `TypeNameHandling`) | 미포함 (보안 주제라 랩 113 해답의 참고로만) |
| | 세이브 형식 버전 호환, 문화권에 따른 숫자·날짜 문자열, 순환 참조(`JsonException` 깊이 초과), `JsonSerializerOptions`를 매번 새로 만드는 성능 문제, 소스 생성기(`JsonSerializerContext`)에 형식 등록 누락 | 미포함 |

미포함 사유: 세이브 호환성과 문화권 파싱은 사용자 결정으로 뺐습니다(문화권은 랩 15가 다룸). 순환 참조는 랩 109 해답에서, 옵션 재사용은 성능 부의 범주로 남깁니다. 소스 생성기는 트리밍·AOT 설정이 필요해 이 프로젝트 설정과 맞지 않습니다.

---

## 일본어판: 부 소개와 도구

# N部: シリアル化 (JSON/XML)

シリアル化のバグのほとんどは**例外なしに黙って**起きます。名前が合わなければ読み飛ばし、書けないメンバーは外し、型情報がなければ基底型で作り、ない値は既定値で埋めます。この部では**シリアル化された文字列そのものを目で確認する**ことから始めます。コンソールに出た JSON/XML と C# のオブジェクトを並べ、どちら側で何が消えたのかを探してください。

| ツール | 使い方 | いつ |
|---|---|---|
| JSON/XML ビジュアライザー | 文字列変数の横の虫眼鏡 → **JSON ビジュアライザー** / **XML ビジュアライザー** | シリアル化の結果をインデントして見る。何が保存され何が抜けたか (ラボ107、108、109) |
| プロパティのアクセサーにブレークポイント | `set` 側にカーソルを置いて F9 | 逆シリアル化がそのプロパティに書き込んでいるか (ラボ106、107) |
| ウォッチの `GetType()` | `received.Attachments[0].GetType()` | 派生型として戻ってきたか (ラボ108) |
| オブジェクト ID | 変数を右クリック → オブジェクト ID の作成 | 保存前に同じオブジェクトだったものが復元後も同じか (ラボ109) |
| 内部例外の表示 | 例外ヘルパー → **内部例外の表示**、ウォッチ `$exception.InnerException` | `XmlSerializer` の "There was an error reflecting type" の本当の理由 (ラボ111) |
| ウォッチで XPath を試す | `doc.SelectSingleNode("/quests")`、`doc.DocumentElement.NamespaceURI` | どの段階で見つからないか (ラボ112) |
| 例外設定 (初回例外) | Ctrl+Alt+E → `System.Text.Json.JsonException`、`System.Xml.XmlException` にチェック | `catch` に飲み込まれるパースエラー (ラボ110、113) |

## 일본어판: 따라 해 보기 (ラボ109 パーティースナップショット)

1. コマンドライン引数に番号を入れて **F5**。`[FAIL] 리더가 파티원 목록의 그 사람 (기대: true, 실제: false)`(リーダーがパーティーメンバー一覧のその人)。
2. `Party restored = Restore(party);` の行に **F9** を置き、もう一度 F5。ローカルウィンドウで `party.Leader` を右クリック → **オブジェクト ID の作成**(`$1`)、`party.Members[0]` にも(`$2` ではなく `$1` が付きます。同じオブジェクトです)。
3. F10 で `Restore` を進めます。コンソールの「스냅샷」(スナップショット)の行を **JSON ビジュアライザー**で見ると、リーダーの名前とHPが `members` の中と `leader` に**二回**書かれています。
4. 今度は `restored.Leader` と `restored.Members[0]` にオブジェクト ID を作ります。番号が**違います**。→ *最初にずれた行*
5. JSON が「同じオブジェクト」をどう表現できるか、あるいはオブジェクトの代わりに何を保存すればよいか考えます。`MainTank` も同じ問題を抱えています。
6. 直したら採点ツールで別のパーティー構成(隠しケース、リーダーとタンクが同じ人の場合を含む)も通るか確認します。

## 일본어판: 랩 표

| # | 名前 | 症状 | 新しく学ぶこと | 難易度 |
|---|---|---|---|---|
| 106 | ギルドのお知らせ | 例外もないのにお知らせがすべて空欄 | **JSON の名前の大文字小文字**: 既定は完全一致、知らないプロパティは黙って無視、`JsonSerializerDefaults.Web`、`UnmappedMemberHandling` | ★ |
| 107 | キャラクターのセーブ | ロードするとゴールドは 0、レベルは 1 になる | **フィールドと `private set` は保存・復元されない**、`[JsonInclude]`、`IncludeFields`、往復テスト | ★ |
| 108 | メール報酬 | 添付した報酬がすべて「不明な報酬」になる | **ポリモーフィズム**: 宣言の型でシリアル化、`[JsonPolymorphic]`/`[JsonDerivedType]`、未登録の派生型 | ★★ |
| 109 | パーティースナップショット | 復元後にリーダーが殴られてもパーティー画面のHPはそのまま | **共有参照が往復後に二つのコピー**、オブジェクト ID、`ReferenceHandler.Preserve` または Id で保存 | ★★ |
| 110 | 報酬パケット | おかしなパケットを拒否せず x0 の報酬が入ってくる | **欠落(missing)と null と 0**、`required`、`RespectNullableAnnotations` | ★★ |
| 111 | オプション XML | グラフィックオプションを追加したら保存した瞬間に落ちる | **`XmlSerializer` の規則**: 引数なしのコンストラクター、`Dictionary` 非対応、内部例外の連鎖を読む | ★★ |
| 112 | クエスト XML | XML にスキーマを付けてから報酬がすべて 0G | **XML の名前空間と XPath**: `XmlNamespaceManager`、`XNamespace`、「見つからなければ 0」の危険 | ★★ |
| 113 | ギルド招待状 | あるギルドの招待状は送っても届かない | **文字列連結で作った XML のエスケープ漏れ**、`XElement`/`XmlWriter` で作る | ★ |

## 일본어판: 기록표 (SOLUTIONS)

### Lab 106 — JSON の名前の大文字小文字が合わず黙って既定値になる
```
受け取った JSON:  {"title":"...","author":"...","priority":3,"pinned":true}
Title の set のブレークポイント:  Deserialize の間に呼ばれない
本当の原因:  System.Text.Json の既定オプションは名前を大文字小文字まで正確に比較する。"title" ≠ Title。
            知らないプロパティは既定で無視するので、例外なしにすべてのプロパティが初期値のまま残る。
```
**修正:** `new JsonSerializerOptions(JsonSerializerDefaults.Web)`(または `PropertyNameCaseInsensitive = true`、`PropertyNamingPolicy = JsonNamingPolicy.CamelCase`、プロパティごとに `[JsonPropertyName]`)。
**教訓:** シリアル化のオプションは**相手と合わせる契約**です。形式を合わせている間は `UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow` を有効にしておくと、この種のバグがすぐに表れます。

### Lab 107 — フィールドと `private set` は往復しない
```
セーブファイル:  {"name":"아린","level":5}      ← gold がない
Level の private set のブレークポイント:  Load の間に呼ばれない
本当の原因:  Gold はフィールド。System.Text.Json は既定で public プロパティだけを扱う → 保存から抜ける。
            Level は public get なので保存はされるが、set が private なので読むときに飛ばす → 初期値 1。
```
**修正:** `[JsonInclude] public int Gold;`、`[JsonInclude] public int Level { get; private set; } = 1;`
**教訓:** 「保存はされるのに読めないメンバー」には警告がありません。セーブ形式には往復テストを置きます。

### Lab 108 — ポリモーフィズムの情報がなく派生型が基底型で戻ってくる
```
サーバー保存:  {"to":"아린","attachments":[{"source":"주간 퀘스트"},...]}   ← amount、itemName がない
ウォッチ received.Attachments[0].GetType():  Lab108+Reward
本当の原因:  List<Reward> の項目を宣言の型 Reward でシリアル化したので派生型のプロパティが抜け、
            JSON に型の識別子がないので読むときもすべて Reward で作る。
```
**修正:** 基底型に `[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]` と、派生型ごとに `[JsonDerivedType(typeof(GoldReward), "gold")]` などをすべて登録します。
**教訓:** System.Text.Json はセキュリティ上「JSON に書かれた型名で任意の型を作る」機能を持たず、許可する派生型を自分で登録します。一つでも漏れるとその型をシリアル化するときに `NotSupportedException` が出ます(隠しケースの経験値報酬)。

### Lab 109 — 共有参照が往復後に二つのコピーになる
```
Restore 前:  party.Leader と party.Members[0] のオブジェクト ID が同じ ($1)
スナップショット:  リーダーの name/hp が members の中と leader に二回書かれる
Restore 後:  restored.Leader と restored.Members[0] のオブジェクト ID が違う
本当の原因:  JSON には「同じオブジェクト」の概念がない。書かれた場所ごとに新しいオブジェクトを作る。MainTank も同じ。
```
**修正(いずれか):** 保存・復元の両方のオプションに `ReferenceHandler = ReferenceHandler.Preserve`、または `Leader` の代わりに `LeaderIndex`(または固有 Id)を保存して読んだあとにつなぎ直す。
**教訓:** オブジェクトグラフをそのままシリアル化すると共有が消え、循環があれば `JsonException`(深さ超過)になります。セーブ形式としては Id で参照するほうが堅牢です。

### Lab 110 — 欠落(missing)、null、0 を区別できない
```
{"item":"마나 물약"}              → 受信 "마나 물약 x0"     (count なし → int の既定値 0、例外なし)
{"item":"화살","count":null}      → 拒否                    (int に null → JsonException)
{"item":null,"count":5}  (隠し)   → 受信 "(null) x5"        (string は null を受け取れる)
本当の原因:  System.Text.Json は JSON にないプロパティに触れない。nullable 注釈も既定では検査しない。
```
**修正:** `public required string Item`、`public required int Count`、オプションに `RespectNullableAnnotations = true`。
**教訓:** `required` は「あるか」だけを、null かどうかは `RespectNullableAnnotations`(.NET 9+)が見ます。二つは別の問いです。

### Lab 111 — `XmlSerializer`: 引数なしのコンストラクターと `Dictionary`
```
例外:   InvalidOperationException: There was an error reflecting type 'BugHunt.Lab111.GameOptions'.
 ---> ... Cannot serialize member 'BugHunt.Lab111+GameOptions.Graphics' ...
 ---> ... BugHunt.Lab111.GraphicsOptions cannot be serialized because it does not have a parameterless constructor.
止まった所:  new XmlSerializer(typeof(GameOptions))   (データと無関係にコンストラクターで)
それを直すと次:  Cannot serialize member ...KeyBindings of type Dictionary`2 ... because it implements IDictionary.
```
**修正:** `GraphicsOptions` を引数なしのコンストラクターと public set を持つ class に。`KeyBindings` は `[XmlIgnore]` にして、XML には配列の代理プロパティ(`KeyBinding[] KeyBindingList`)で。または System.Text.Json で保存する。
**教訓:** "There was an error reflecting type" が出たら**内部例外を最後まで**読みます(ラボ6の `AggregateException` と同じ読み方)。

### Lab 112 — 既定の名前空間が付いた XML を接頭辞なしの XPath で探す
```
ウォッチ doc.SelectSingleNode("/quests"):            null
ウォッチ doc.DocumentElement.NamespaceURI:           "urn:bughunt:quests"
本当の原因:  xmlns="urn:bughunt:quests" ですべての要素がその名前空間に属したが、XPath 1.0 の接頭辞なしの名前は「名前空間なし」だけを探す。
            RewardGold が見つからなかったもの(null)を 0G に変えて黙って通した。
```
**修正:** `XmlNamespaceManager` で `q` を `urn:bughunt:quests` に割り当て、`/q:quests/q:quest[@id='...']/q:reward`。LINQ to XML なら `XNamespace`。
**教訓:** 必須データが見つからないときに既定値(0)を返すとこの種のバグが隠れます。例外を出してください。

### Lab 113 — 文字列連結で作った XML のエスケープ漏れ
```
(受信側) 読めないメッセージを破棄: Name cannot begin with the '3' character ... Line 1, position N.
送った XML:  <invite guild="불꽃 연대" from="케인"><message>레이드 같이 가요 <3</message></invite>
本当の原因:  BuildInvite がユーザー入力をそのまま XML の文法の中に入れる。< と & はテキストで、" は属性値で文書を壊す。
```
**修正:** `new XElement("invite", new XAttribute("guild", guild), ...)` で作る(または `XmlWriter`)。どうしても文字列なら値ごとに `SecurityElement.Escape`。
**教訓:** `<` だけ置き換える修正は隠しケースの `&`、`"` で引っかかります。同じ間違いは SQL インジェクション、XSS、シェルコマンド、JSON 文字列の連結にもあります。

### 参考: XML とセキュリティ (ラボなし)

- **XXE(外部エンティティ)**: .NET Core 以降の `XmlDocument`、`XDocument`、`XmlReader.Create`、`XmlSerializer` は既定で外部エンティティを解決しません。信頼できない XML を受け取るときに**既定値を緩めないでください**。
- **エンティティ展開(Billion laughs)**: DTD を許可する必要があるなら `MaxCharactersFromEntities` で制限します。
- **型名でオブジェクトを作る**: `BinaryFormatter`(.NET 9 で削除)、Newtonsoft.Json の `TypeNameHandling.All` などはリモートコード実行につながりえます。ラボ108の `JsonDerivedType` のように**許可リスト**を使ってください。

## 일본어판: 커버리지 표 줄

| カテゴリ | 代表的なケース | ラボ |
|---|---|---|
| **シリアル化** | JSON の名前の大文字小文字、知らないプロパティは黙って無視 | 106 |
| | フィールド・`private set` が保存されない (`[JsonInclude]`、`IncludeFields`) | 107 |
| | ポリモーフィズム: 派生型が基底型で保存される (`JsonDerivedType`) | 108 |
| | 共有参照が往復後に二つのコピー (`ReferenceHandler.Preserve`) | 109 |
| | 欠落と null と 0 (`required`、`RespectNullableAnnotations`) | 110 |
| | `XmlSerializer`: 引数なしのコンストラクター、`Dictionary` 非対応 | 111 |
| | XML の名前空間と XPath (`SelectSingleNode` が null) | 112 |
| | 文字列連結で作った XML のエスケープ漏れ | 113 |
| | 空白のテキストノード(`PreserveWhitespace`) | 未収録 (ラボ112の解答で言及) |
| | `DateTime` の Kind の消失 | 未収録 (ラボ16と重複) |
| | XXE・エンティティ展開、型名でオブジェクトを作る | 未収録 (セキュリティの話題なのでラボ113の解答の参考のみ) |
| | セーブ形式のバージョン互換、カルチャによる数値・日付の文字列、循環参照、`JsonSerializerOptions` の毎回生成、ソースジェネレーターへの型の登録漏れ | 未収録 |

---

## 문자열 표 (실행 출력, 한국어 → 일본어)

### 랩 제목과 증상 (`Program.cs`)

| 한국어 | 日本語 |
|---|---|
| 길드 공지 | ギルドのお知らせ |
| 예외도 없는데 공지가 전부 빈 칸이다 | 例外もないのにお知らせがすべて空欄 |
| 캐릭터 세이브 | キャラクターのセーブ |
| 불러오면 골드는 0, 레벨은 1이 된다 | ロードするとゴールドは 0、レベルは 1 になる |
| 우편 보상 | メール報酬 |
| 첨부한 보상이 전부 '알 수 없는 보상'이 된다 | 添付した報酬がすべて「不明な報酬」になる |
| 파티 스냅샷 | パーティースナップショット |
| 복원 뒤 리더가 맞아도 파티 창 체력은 그대로다 | 復元後にリーダーが殴られてもパーティー画面のHPはそのまま |
| 보상 패킷 | 報酬パケット |
| 이상한 패킷을 거절하지 않고 x0 보상이 들어온다 | おかしなパケットを拒否せず x0 の報酬が入ってくる |
| 옵션 XML | オプション XML |
| 그래픽 옵션을 추가하자 저장하는 순간 죽는다 | グラフィックオプションを追加したら保存した瞬間に落ちる |
| 퀘스트 XML | クエスト XML |
| XML에 스키마를 붙인 뒤로 보상이 전부 0G다 | XML にスキーマを付けてから報酬がすべて 0G |
| 길드 초대장 | ギルド招待状 |
| 어떤 길드의 초대장은 보내도 도착하지 않는다 | あるギルドの招待状は送っても届かない |

### `Expect` 설명과 콘솔 출력

| 랩 | 한국어 | 日本語 |
|---|---|---|
| 106 | `공지 제목` / `작성자` / `중요도` / `고정 공지` | `お知らせのタイトル` / `作成者` / `重要度` / `固定のお知らせ` |
| 106 | `  받은 JSON: {json}` | `  受け取った JSON: {json}` |
| 106 | `  화면: [{고정/일반}] {제목} - {작성자} (중요도 {n})` | `  画面: [{固定/通常}] {タイトル} - {作成者} (重要度 {n})` |
| 106 | `빈 공지` (예외 메시지) | `空のお知らせ` |
| 106 | 이번 주 레이드는 토요일 9시 / 창고 정리 부탁드립니다 / 신규 길드원 환영회 / 길드 전쟁 참가 신청, 길드장 아린 / 부길드장 케인 / 총무 미라 | 今週のレイドは土曜9時 / 倉庫の整理をお願いします / 新規ギルドメンバー歓迎会 / ギルド戦争の参加申請、ギルドマスター アリン / サブマスター ケイン / 会計 ミラ |
| 107 | `이름` / `골드` / `레벨` | `名前` / `ゴールド` / `レベル` |
| 107 | `  저장 전: {이름}, 레벨 {n}, {g}G` | `  保存前: {名前}、レベル {n}、{g}G` |
| 107 | `  세이브 파일: {json}` | `  セーブファイル: {json}` |
| 107 | `  불러온 뒤: {이름}, 레벨 {n}, {g}G` | `  ロード後: {名前}、レベル {n}、{g}G` |
| 107 | `빈 세이브` (예외 메시지) | `空のセーブ` |
| 107 | 아린, 케인, 미라, 도란 | アリン、ケイン、ミラ、ドラン |
| 108 | `첨부 개수` / `첨부 내용` / `보상 출처` | `添付の数` / `添付の内容` / `報酬の出どころ` |
| 108 | `알 수 없는 보상` / `{n}G` / `{이름} x{n}` / `경험치 {n}` | `不明な報酬` / `{n}G` / `{名前} x{n}` / `経験値 {n}` |
| 108 | `  보낸 첨부: {목록}` / `  서버 저장: {json}` / `  받은 첨부: {목록}` | `  送った添付: {リスト}` / `  サーバー保存: {json}` / `  受け取った添付: {リスト}` |
| 108 | `빈 우편` (예외 메시지) | `空のメール` |
| 108 | 주간 퀘스트, 이벤트, 출석, 레이드, 튜토리얼 / 회복 물약, 용의 비늘 | ウィークリークエスト、イベント、ログインボーナス、レイド、チュートリアル / 回復ポーション、竜の鱗 |
| 109 | `리더가 파티원 목록의 그 사람` | `リーダーがパーティーメンバー一覧のその人` |
| 109 | `메인 탱커가 파티원 목록의 그 사람` | `メインタンクがパーティーメンバー一覧のその人` |
| 109 | `파티 창의 리더 체력` | `パーティー画面のリーダーのHP` |
| 109 | `  스냅샷: {json}` / `  파티 창: {목록}` | `  スナップショット: {json}` / `  パーティー画面: {リスト}` |
| 109 | `빈 스냅샷` (예외 메시지) | `空のスナップショット` |
| 109 | 아린, 케인, 미라, 도란, 리라, 오렌 | アリン、ケイン、ミラ、ドラン、リラ、オレン |
| 110 | `거절한 패킷 수` | `拒否したパケット数` |
| 110 | `가방에 들어온 보상 수` | `バッグに入った報酬の数` |
| 110 | `수량 0 이나 이름 없는 보상이 들어오지 않음` | `数量 0 や名前のない報酬が入ってこない` |
| 110 | `  거절: {json}` / `  받음: {json} → {보상}` | `  拒否: {json}` / `  受信: {json} → {報酬}` |
| 110 | 회복 물약, 마나 물약, 화살, 철광석, 용의 비늘, 빵, 영웅의 증표 | 回復ポーション、マナポーション、矢、鉄鉱石、竜の鱗、パン、英雄の証 |
| 111 | `볼륨` / `해상도` / `전체 화면` / `키 설정` | `音量` / `解像度` / `フルスクリーン` / `キー設定` |
| 111 | `  저장한 옵션 XML: {n}자` | `  保存したオプション XML: {n}文字` |
| 111 | `  불러온 옵션: 볼륨 {v}, {w}x{h}, 키 {n}개` | `  ロードしたオプション: 音量 {v}、{w}x{h}、キー {n}個` |
| 111 | `빈 옵션` (예외 메시지) | `空のオプション` |
| 112 | `{id} 보상 골드` | `{id} の報酬ゴールド` |
| 112 | `  {id} {제목}: 보상 {n}G` | `  {id} {タイトル}: 報酬 {n}G` |
| 112 | 늑대 사냥, 약초 수집, 고블린 소탕, 잃어버린 반지, 유적 탐사, 일일 출석, 할로윈 호박 모으기, 유령 퇴치 | オオカミ狩り、薬草採集、ゴブリン掃討、失われた指輪、遺跡探索、デイリーログイン、ハロウィンのカボチャ集め、ゴースト退治 |
| 113 | `{길드} 초대장 내용` | `{ギルド} の招待状の内容` |
| 113 | `도착한 초대장 수` | `届いた招待状の数` |
| 113 | `  보냄: {xml}` | `  送信: {xml}` |
| 113 | `  (받는 쪽) 읽을 수 없는 메시지를 버림: {메시지}` | `  (受信側) 読めないメッセージを破棄: {メッセージ}` |
| 113 | 길드 이름: 새벽의 검, 불꽃 연대, 검 & 방패, 고요한 숲, "전설" 길드, 별빛, 늑대 무리, 평화 | 暁の剣、炎の連隊、剣 & 盾、静かな森、"伝説" ギルド、星明かり、狼の群れ、平和 |
| 113 | 메시지: 같이 레이드 가요! / 레이드 같이 가요 <3 / 주말 공성전 함께해요 / 환영합니다 / 초보 환영 & 친목 / A<B 이면 가입! / 1 < 2 & 3 > 2 / 안녕하세요 | 一緒にレイド行きましょう! / レイド一緒に行こう <3 / 週末の攻城戦ご一緒に / ようこそ / 初心者歓迎 & 交流 / A<B なら加入! / 1 < 2 & 3 > 2 / こんにちは |

데이터 문자열(이름, 아이템, 대사)을 일본어로 바꿀 때는 숨은 테스트가 같은 성질을 유지하는지 확인해야 합니다. 특히 랩 113은 `<`, `&`, `"`가 들어간 항목을 그대로 두어야 하고, 랩 110은 패킷 JSON의 모양(키가 있는지, null인지)만 지키면 아이템 이름은 바꿔도 됩니다.

---

## 검증 기록

채점기(`dotnet run --project BugHunt.Grader -- 74-81`)로 확인했습니다 (Windows 11, .NET SDK 10.0.401, Debug, 숨은 케이스 3개씩).

- 버그 상태: 0 / 8 (`FAIL` 7, `CRASH` 1: 랩 111 `InvalidOperationException`)
- 정답 수정을 모두 적용: 8 / 8 (기본 + 숨은 케이스)
- 되돌림: 다시 0 / 8
- 증상만 가린 수정 8가지(랩 106 보이는 공지만 박기, 107 `아린`일 때 값 박기, 108 보이는 두 파생 형식만 등록, 109 리더만 이름으로 잇고 탱커는 3인 파티만, 110 `required`만, 111 생성자만 고치고 키 설정은 `[XmlIgnore]`, 112 `q1`/`q2`만 박기, 113 `<`만 바꾸기): 랩 106~110, 112, 113은 기본 케이스는 통과하고 숨은 케이스에서 실패(`증상은 사라졌지만 원인이 남아 있음`, 108은 `NotSupportedException`으로 `CRASH`), 랩 111은 기본 케이스부터 `FAIL`(키 설정)
