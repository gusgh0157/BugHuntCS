# 초안: 파일 입출력 (새 부)

> 이 파일은 README·SOLUTIONS·커버리지 표에 합칠 **초안**입니다. 번호는 임시 번호(`Lab98`~`Lab105`, 지금 메뉴 번호 66~73)이고, 재배치 때 원래 세션이 최종 번호로 바꿉니다. 최종 순서에서는 5부(… 4 인터페이스와 상속 → **5 파일 입출력** → 6 직렬화 → 7 비동기 …)입니다.
>
> 소스 위치: `BugHunt/Labs/PartX_FileIO/`. 숨은 케이스는 랩마다 3개.
>
> 모든 랩은 `%TEMP%\BugHunt\<랩 이름>`(`Path.Combine(Path.GetTempPath(), "BugHunt", "Lab98_SaveFolder")` 식) 아래에서만 읽고 쓰고, `finally`에서 그 폴더를 지웁니다. 랩마다 폴더 이름이 달라 채점기가 랩을 따로 돌려도 겹치지 않습니다. 숫자는 `CultureInfo.InvariantCulture`로 쓰고 읽어서 화면 언어나 문화권을 바꿔도 판정이 같습니다.

| 임시 번호 | 지금 메뉴 번호 | 파일 |
|---|---|---|
| Lab98 | 66 | `Lab98_SaveFolder.cs` |
| Lab99 | 67 | `Lab99_CombatLog.cs` |
| Lab100 | 68 | `Lab100_ChatUpload.cs` |
| Lab101 | 69 | `Lab101_ProfileImport.cs` |
| Lab102 | 70 | `Lab102_DialogueMod.cs` |
| Lab103 | 71 | `Lab103_BlockList.cs` |
| Lab104 | 72 | `Lab104_SafeSave.cs` |
| Lab105 | 73 | `Lab105_CloudSync.cs` |

---

## README에 들어갈 부분 (한국어)

# N부: 파일 입출력

파일 버그는 코드만 봐서는 잘 안 보입니다. 같은 코드가 **어느 폴더에서 실행했는지, 파일을 어떤 편집기로 저장했는지, 다른 누가 그 파일을 열고 있는지, 저장 도중에 무슨 일이 있었는지**에 따라 다르게 동작하기 때문입니다. 이 부에서는 조사식으로 **디스크에 실제로 무엇이 있는지**(전체 경로, 바이트, 길이)를 확인하는 습관을 들입니다.

## N부에서 쓰는 도구

| 도구 | 하는 법 | 언제 |
|---|---|---|
| 조사식의 `Path.GetFullPath(...)` | `Path.GetFullPath("saves")`, `Environment.CurrentDirectory`, `AppContext.BaseDirectory` | 상대 경로가 **지금** 어디를 가리키는지 (랩 98, 101) |
| 조사식의 `new FileInfo(path).Length`, `File.ReadAllText(path)` | 중단점에서 바로 디스크 내용을 읽어 봄 | 썼다고 생각한 것과 실제로 쓰인 것 비교 (랩 99, 104) |
| `[Raw View]` | `writer` 펼치기 → `_charPos` | `StreamWriter` 버퍼에 아직 남은 글자 (랩 99) |
| 16진수 표시 | 조사식 창 우클릭 → **16진수 표시**, `bytes` 펼치기 | BOM(`EF BB BF`), CP949와 UTF-8의 바이트 차이 (랩 102) |
| 텍스트 시각화 도우미 / 조사식의 `s.Length` | 문자열 옆 돋보기, `key.Length`, `text.Contains('\r')` | 눈에 안 보이는 글자: `﻿`, `\r` (랩 102, 103) |
| 예외 설정 (first chance) | Ctrl+Alt+E → `System.IO.IOException` 체크 | `catch`에 삼켜지는 파일 공유 예외 (랩 100) |
| 작업 창 | 디버그 → 창 → 작업 | 아직 안 끝난 쓰기 작업 (랩 105) |
| 탐색기 | 중단점에서 멈춘 채로 파일 열어 보기 | 저장 "도중"의 파일 상태 (랩 104) |

## 따라 해 보기: 랩 99 (전투 기록 파일)

1. 명령 인수에 번호를 넣고 **F5**. `[FAIL] 저장된 줄 수 (기대: 120, 실제: 93)`.
2. `SaveLog`의 마지막 줄(`Console.WriteLine`)에 **F9**, 다시 F5.
3. 조사식에 `stream.Position`을 넣습니다. 파일로 실제로 나간 바이트입니다. 써야 할 양보다 적습니다.
4. 조사식에 `writer`를 넣고 펼쳐 `[Raw View]`의 `_charPos`를 봅니다. 아직 `writer`의 버퍼에 남아 있는 글자 수입니다.
5. F10으로 메서드를 빠져나가면서 `using var stream`이 닫힐 때 무엇이 닫히고 무엇이 안 닫히는지 생각합니다. 다시 `new FileInfo(path).Length`를 보면 값이 그대로입니다. → *처음 어긋난 줄*
6. 버퍼를 가진 쪽(`writer`)을 닫도록 고치고 채점기로 다른 줄 수(숨은 케이스)도 통과하는지 확인합니다. 랩 36(리플레이)과 무엇이 다른지 비교해 보세요.

## N부 랩 목록

| # | 이름 | 증상 | 새로 배우는 것 | 난이도 |
|---|---|---|---|---|
| 98 | 세이브 폴더 | 폴더를 한 번 열고 나면 세이브가 없다고 한다 | **상대 경로는 작업 디렉터리 기준**, `Environment.CurrentDirectory` vs `AppContext.BaseDirectory` | ★ |
| 99 | 전투 기록 파일 | 저장한 전투 기록의 마지막 몇 줄이 빠져 있다 | **`StreamWriter` 버퍼**: 안쪽 스트림만 닫으면 꼬리가 잘림, 바깥 래퍼부터 `Dispose` | ★★ |
| 100 | 채팅 신고 | 신고하면 채팅 로그를 '사용 중'이라 못 읽는다 | **`FileShare`**: 열려 있는 쪽과 여는 쪽의 공유 모드, 같은 프로세스 안의 "다른 프로세스" (Windows만, 다른 OS는 `SKIP`) | ★★ |
| 101 | 프로필 가져오기 | 가져온 프로필의 세이브만 백업에서 빠진다 | **`Path.Combine`의 두 번째 인자가 절대 경로**면 앞을 버림, 경로 탈출 막기 | ★ |
| 102 | NPC 대사 모드 | 모드에 따라 첫 대사가 없거나 한글이 깨진다 | **인코딩**: UTF-8 BOM, CP949(`CodePagesEncodingProvider`), 바이트를 직접 디코딩할 때 | ★★ |
| 103 | 차단 목록 | 메모장으로 고친 뒤 아무도 차단되지 않는다 | **줄바꿈 `\r\n` vs `\n`**, 끝 줄바꿈 뒤의 빈 항목, `File.ReadAllLines` | ★ |
| 104 | 안전한 저장 | 저장이 실패하자 지난번 세이브까지 망가졌다 | **반쯤 쓴 파일**: `FileMode.Create`는 여는 순간 자른다, 임시 파일 + `File.Replace` | ★★ |
| 105 | 클라우드 동기화 | 서버에는 늘 한 번 전의 설정이 올라간다 | **비동기 쓰기를 기다리지 않고 그 파일을 읽음**, 작업 창, 시간 대신 완료로 순서 맞추기 | ★★ |

(번호 열은 재배치 뒤의 최종 번호로 바꿉니다.)

---

## SOLUTIONS.md에 들어갈 부분 (한국어)

# N부: 파일 입출력

## Lab 98 — 상대 경로는 작업 디렉터리 기준
```
예외:        DirectoryNotFoundException: Could not find a part of the path '...\Lab98_SaveFolder\mods\saves\slot1.sav'.
저장할 때 경로:  ...\Lab98_SaveFolder\game\saves\slot1.sav
조사식:      Environment.CurrentDirectory == ...\mods,  Path.GetFullPath("saves") == ...\mods\saves
진짜 원인:  SavePath 가 "saves\slot1.sav" 라는 상대 경로를 만든다. 상대 경로는 그때그때의 작업 디렉터리 기준인데,
           폴더 열기(파일 대화상자)가 작업 디렉터리를 바꿨다. SaveSystem 은 GameDir 을 받아 두고도 쓰지 않았다.
```
**수정:** `Path.Combine(GameDir, "saves", slot + ".sav")`, `Directory.CreateDirectory`도 같은 기준으로.
**교훈:** 작업 디렉터리는 프로세스 전체가 공유하는 바뀌는 상태입니다(런처, 바로 가기의 시작 위치, 파일 대화상자, 다른 폴더에서 친 명령줄). 실행 파일 폴더는 `AppContext.BaseDirectory`, 사용자 데이터는 `Environment.GetFolderPath(SpecialFolder.LocalApplicationData)` 아래가 맞습니다. 불러오기 전에 작업 디렉터리를 되돌리는 수정은 다음에 작업 디렉터리를 바꾸는 코드가 생기면 다시 깨집니다.

## Lab 99 — 안쪽 스트림만 닫아서 `StreamWriter` 버퍼가 버려짐
```
SaveLog 끝:   stream.Position 이 써야 할 양보다 작음
writer [Raw View] _charPos:   0 이 아님 (버퍼에 남은 글자)
진짜 원인:  using 은 FileStream 에만 붙어 있다. StreamWriter 는 자기 버퍼(1024자)가 찰 때마다 스트림으로 보내는데,
           마지막에 남은 글자는 writer 의 Flush/Dispose 때만 나간다. 안쪽 스트림을 닫아도 바깥 writer 의 버퍼는 비워지지 않는다.
```
**수정:** `using var writer = new StreamWriter(stream);` (선언 역순으로 닫혀 writer가 먼저 닫힘) 또는 `using var writer = new StreamWriter(path);`
**교훈:** 래퍼가 겹치면(`StreamWriter`, `BinaryWriter`, `GZipStream`, `CryptoStream`) **가장 바깥부터** 닫습니다. 바깥만 닫으면 안쪽까지 닫히고, 안쪽을 계속 쓰려면 `leaveOpen: true`. 랩 36은 아예 닫지 않아 파일이 비고 핸들이 남은 경우, 이번은 안쪽만 닫아 꼬리가 잘린 경우입니다.

## Lab 100 — `FileShare`가 맞지 않아 쓰는 중인 파일을 못 읽음
```
first chance:  IOException: The process cannot access the file '...chat.log' because it is being used by another process.
멈춘 곳:      File.ReadAllLines (Upload 의 catch 가 삼켜서 0줄 업로드)
진짜 원인:  ChatLogger 의 StreamWriter(append) 는 FileAccess.Write + FileShare.Read 로 열려 있다.
           File.ReadAllLines 는 FileAccess.Read + FileShare.Read 로 연다. 즉 "남이 쓰고 있으면 거부". "다른 프로세스"는 사실 이 프로세스.
```
**수정:** 읽는 쪽이 쓰기 공유를 허용한다.
```csharp
using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
using var reader = new StreamReader(stream);
```
**교훈:** 두 핸들이 공존하려면 서로의 `FileAccess`가 상대의 `FileShare`에 들어 있어야 합니다. 쓰는 쪽은 `AutoFlush`나 `Flush`도 해야 읽는 쪽이 최신 내용을 봅니다. 업로드 전에 로거를 닫는 수정도 동작하지만 신고 중에 온 채팅을 놓칠 수 있습니다. `catch (IOException)`이 실패를 "0줄"로 바꾼 것도 랩 33(예외를 삼키고 성공을 돌려줌)과 같은 냄새입니다. Linux/macOS는 공유 모드를 강제하지 않아 이 랩은 `SKIP`합니다.

## Lab 101 — `Path.Combine`의 두 번째 인자가 절대 경로
```
콘솔:      "저장 →" 세 줄 중 하나만 ...\old-install\saves\mira
조사식:    Path.Combine(saveRoot, profile.Folder) == profile.Folder,  Path.IsPathRooted(profile.Folder) == true
진짜 원인:  Path.Combine 은 뒤 인자가 루트 경로면 앞의 인자를 전부 버린다(문서에 적힌 동작, 예외 없음).
           옛 설치에서 가져온 프로필의 Folder 가 절대 경로라 세이브 루트 밖에 저장됐고, 백업이 세이브 루트만 훑어 빠졌다.
```
**수정:** 바깥에서 온 값은 폴더 이름으로만 쓴다: `Path.Combine(saveRoot, Path.GetFileName(profile.Folder))`. 또는 `Path.IsPathRooted`, `..` 포함이면 거부하고, 합친 뒤 `Path.GetFullPath(결과)`가 세이브 루트 안인지 확인.
**교훈:** 사용자 입력, 세이브 파일, 서버 응답, 압축 파일 안의 이름을 `Path.Combine`에 그대로 넣으면 경로 탈출(path traversal)이 됩니다. `Path.Join`은 앞을 버리지 않지만 엉뚱한 경로를 만들 뿐 검증을 대신하지 않습니다.

## Lab 102 — 바이트를 UTF-8로만 디코딩: BOM과 CP949
```
모드 A (UTF-8 BOM):  table.Keys.First().Length == firstKey.Length + 1,  첫 글자 '﻿'
모드 B (CP949):      bytes 의 한글이 2바이트씩. UTF8.GetString 결과에 '�'(�)
모드 C (UTF-8):      정상
진짜 원인:  Encoding.UTF8.GetString 은 BOM 을 지우지 않고(U+FEFF 로 남음), 잘못된 바이트는 예외 없이 �로 바꾼다.
           File.ReadAllText/StreamReader 는 BOM 을 처리하지만 바이트를 직접 디코딩하면 그렇지 않다.
```
**수정:**
```csharp
string text;
if (bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble))
    text = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
else
{
    try { text = new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes); }
    catch (DecoderFallbackException) { text = Encoding.GetEncoding(949).GetString(bytes); }   // CodePagesEncodingProvider 등록 필요
}
```
**교훈:** .NET Core 이후는 기본으로 UTF-8, UTF-16, ASCII, Latin-1 정도만 들어 있고 CP949·Shift_JIS(932) 같은 코드 페이지는 `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)`를 해야 씁니다. 키에서 `﻿`만 지우는 수정은 모드 B를 못 고칩니다. 저장하는 쪽이라면 BOM 없는 UTF-8로 통일합니다(`File.WriteAllText`의 기본값).

## Lab 103 — `\n`으로만 잘라서 `\r`과 빈 항목이 남음
```
콘솔 "읽은 항목":   "Kael\r", "Mira\r", "Doran\r", ""
조사식:            text.Contains('\r') == true
진짜 원인:  메모장은 CRLF 로 저장한다. Split('\n') 은 각 항목 끝에 '\r' 을 남기고, 끝 줄바꿈 뒤에 빈 문자열을 하나 더 만든다.
```
**수정:** `File.ReadAllLines(path)`(또는 `File.ReadLines`, `StreamReader.ReadLine`). 직접 자른다면 `text.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)`.
**교훈:** `ReadLine` 계열은 `\r\n`, `\n`, `\r`을 모두 줄 끝으로 처리하고 끝 줄바꿈 뒤에 빈 줄을 만들지 않습니다. `TrimEnd('\r')`만 붙이면 빈 항목이 남고, `Split("\r\n")`만 쓰면 숨은 케이스(LF 파일)에서 한 줄로 붙어 걸립니다.

## Lab 104 — 원본을 직접 덮어쓰다 실패해서 반쯤 쓴 파일
```
Save(path, broken) 직전:  inventory.sav 정상
Save 의 첫 줄(new StreamWriter(path)) 직후:  0바이트
예외 뒤:  개수 줄 + 앞쪽 아이템 몇 줄만 (Load 가 손상으로 판정)
진짜 원인:  new StreamWriter(path) 는 FileMode.Create 라서 여는 순간 기존 파일을 자른다. 예외가 나도 using 이 그때까지 쓴 앞부분을 내보낸다.
```
**수정:** 다른 파일에 끝까지 쓰고, 다 쓴 뒤 한 번에 바꿔치기.
```csharp
string tmp = path + ".tmp";
try
{
    using var writer = new StreamWriter(tmp);
    ... (같은 내용)
}
catch
{
    File.Delete(tmp);
    throw;
}
if (File.Exists(path)) File.Replace(tmp, path, null);   // 백업을 남기려면 세 번째 인자에 경로
else File.Move(tmp, path);
```
**교훈:** 정전, 강제 종료, 디스크 꽉 참도 같은 결과입니다. 같은 볼륨 안의 `File.Replace`/`File.Move(…, overwrite: true)`는 이름만 바꾸므로 중간 상태가 남지 않습니다. 먼저 메모리에서 전부 검사한 뒤에 여는 수정은 이 예외는 피하지만 쓰는 도중의 중단에는 여전히 약합니다. 실패한 저장의 임시 파일은 지웁니다(세 번째 판정).

## Lab 105 — 비동기 쓰기를 기다리지 않고 그 파일을 읽음
```
중단점 순서:   ReadAllTextAsync 가 WriteAllTextAsync 보다 먼저 멈춤
조사식:        save.IsCompleted == false, save.Status == WaitingForActivation
작업 창:       SaveSettingsAsync 가 Task.Delay 에서 대기 중
진짜 원인:  ApplyAsync 가 저장 Task 를 대기열에 넣기만 하고 await 하지 않은 채 파일을 읽는다. 읽는 것은 이전 설정.
           Task 를 변수로 받았기 때문에 CS4014 경고도 나지 않는다.
```
**수정:** 읽기 전에 `await save;`.
**교훈:** 순서는 시간(`Task.Delay`)이 아니라 작업 완료로 맞춥니다. 저장한 내용을 업로드에 직접 넘겨 파일을 다시 읽지 않는 것도 방법입니다. 랩 28은 기다리지 않은 작업이 아예 안 끝나는 경우, 이번은 끝나기는 하지만 다른 코드가 너무 일찍 그 결과(파일)를 보는 경우입니다.

---

## 커버리지 표에 들어갈 줄

새 범주 **파일 입출력**을 추가합니다. 기존 **리소스·메모리** 범주의 "`Dispose` 누락 (버퍼 플러시) | 36"은 그대로 두고 "안쪽 스트림만 닫음 | 99"를 파일 입출력 쪽에 둡니다.

| 범주 | 대표 케이스 | 랩 |
|---|---|---|
| **파일 입출력** | 상대 경로와 작업 디렉터리 (`CurrentDirectory` vs `AppContext.BaseDirectory`) | 98 |
| | `StreamWriter` 버퍼: `Dispose` 누락 / 안쪽 스트림만 닫음 | 36 / 99 |
| | `FileShare` 충돌 (`IOException`, Windows) | 100 |
| | `Path.Combine` 두 번째 인자가 절대 경로, 경로 탈출 | 101 |
| | 인코딩: UTF-8 BOM, CP949(`CodePagesEncodingProvider`) | 102 |
| | 줄바꿈 `\r\n`/`\n`, 끝 줄바꿈 뒤 빈 항목 | 103 |
| | 저장 도중 실패로 반쯤 쓴 파일 (임시 파일 + `File.Replace`) | 104 |
| | 비동기 쓰기를 기다리지 않고 읽음 | 105 (랩 28은 끝나지 않는 쪽) |
| | 경로 대소문자 구분(Windows vs Linux), `MAX_PATH`·잘못된 파일 이름 문자, `FileSystemWatcher` 이벤트 중복·유실, 바이러스 백신 등 외부 프로세스의 잠금, `File.Exists` 뒤 열기 사이의 경쟁(TOCTOU), 세이브 형식 버전 호환 | 미포함 |

미포함 사유: 대소문자 구분과 `MAX_PATH`, `FileSystemWatcher`, 외부 잠금은 운영 체제·파일 시스템·실행 환경마다 결과가 달라 판정을 고정할 수 없습니다(이 저장소는 런타임마다 결과가 달라지는 케이스를 뺍니다). TOCTOU는 경쟁 조건이라 매번 같은 결과를 내기 어렵습니다. 세이브 호환성은 사용자 결정으로 뺐습니다.

---

## 일본어판: 부 소개와 도구

# N部: ファイル入出力

ファイルのバグはコードを見ただけではなかなか見えません。同じコードが**どのフォルダーから実行したか、ファイルをどのエディターで保存したか、ほかの誰がそのファイルを開いているか、保存の途中で何が起きたか**によって違う動きをするからです。この部では、ウォッチで**ディスクに実際に何があるか**(フルパス、バイト、長さ)を確かめる習慣を身につけます。

| ツール | 使い方 | いつ |
|---|---|---|
| ウォッチの `Path.GetFullPath(...)` | `Path.GetFullPath("saves")`、`Environment.CurrentDirectory`、`AppContext.BaseDirectory` | 相対パスが**今**どこを指しているか (ラボ98、101) |
| ウォッチの `new FileInfo(path).Length`、`File.ReadAllText(path)` | ブレークポイントでその場でディスクの内容を読む | 書いたつもりの内容と実際に書かれた内容を比べる (ラボ99、104) |
| `[Raw View]` | `writer` を展開 → `_charPos` | `StreamWriter` のバッファーにまだ残っている文字 (ラボ99) |
| 16 進数で表示 | ウォッチウィンドウを右クリック → **16 進数で表示**、`bytes` を展開 | BOM(`EF BB BF`)、CP949 と UTF-8 のバイトの違い (ラボ102) |
| テキストビジュアライザー / ウォッチの `s.Length` | 文字列の横の虫眼鏡、`key.Length`、`text.Contains('\r')` | 目に見えない文字: `﻿`、`\r` (ラボ102、103) |
| 例外設定 (初回例外) | Ctrl+Alt+E → `System.IO.IOException` にチェック | `catch` に飲み込まれるファイル共有の例外 (ラボ100) |
| タスクウィンドウ | デバッグ → ウィンドウ → タスク | まだ終わっていない書き込みタスク (ラボ105) |
| エクスプローラー | ブレークポイントで止めたままファイルを開いてみる | 保存「途中」のファイルの状態 (ラボ104) |

## 일본어판: 따라 해 보기 (ラボ99 戦闘ログファイル)

1. コマンドライン引数に番号を入れて **F5**。`[FAIL] 저장된 줄 수 (기대: 120, 실제: 93)`(保存された行数)。
2. `SaveLog` の最後の行(`Console.WriteLine`)に **F9** を置き、もう一度 F5。
3. ウォッチに `stream.Position` を入れます。ファイルに実際に出たバイト数です。書くべき量より少ないです。
4. ウォッチに `writer` を入れて展開し、`[Raw View]` の `_charPos` を見ます。まだ `writer` のバッファーに残っている文字数です。
5. F10 でメソッドを抜けながら、`using var stream` が閉じるときに何が閉じて何が閉じないか考えます。もう一度 `new FileInfo(path).Length` を見ると値はそのままです。→ *最初にずれた行*
6. バッファーを持っている側(`writer`)を閉じるように直し、採点ツールで別の行数(隠しケース)も通るか確認します。ラボ36(リプレイ)と何が違うか比べてみてください。

## 일본어판: 랩 표

| # | 名前 | 症状 | 新しく学ぶこと | 難易度 |
|---|---|---|---|---|
| 98 | セーブフォルダー | フォルダーを一度開くとセーブがないと言われる | **相対パスは作業ディレクトリ基準**、`Environment.CurrentDirectory` と `AppContext.BaseDirectory` | ★ |
| 99 | 戦闘ログファイル | 保存した戦闘ログの最後の数行が欠けている | **`StreamWriter` のバッファー**: 内側のストリームだけ閉じると末尾が切れる、外側のラッパーから `Dispose` | ★★ |
| 100 | チャット通報 | 通報するとチャットログが「使用中」で読めない | **`FileShare`**: 開いている側と開く側の共有モード、同じプロセス内の「別のプロセス」(Windows のみ、他の OS は `SKIP`) | ★★ |
| 101 | プロフィール取り込み | 取り込んだプロフィールのセーブだけバックアップから漏れる | **`Path.Combine` の2番目の引数が絶対パス**なら前を捨てる、パストラバーサルを防ぐ | ★ |
| 102 | NPC セリフ MOD | MOD によって最初のセリフがなかったりハングルが文字化けしたりする | **エンコーディング**: UTF-8 BOM、CP949(`CodePagesEncodingProvider`)、バイトを直接デコードするとき | ★★ |
| 103 | ブロックリスト | メモ帳で編集してから誰もブロックされない | **改行 `\r\n` と `\n`**、末尾の改行の後ろの空の項目、`File.ReadAllLines` | ★ |
| 104 | 安全なセーブ | 保存に失敗したら前回のセーブまで壊れた | **書きかけのファイル**: `FileMode.Create` は開いた瞬間に切り詰める、一時ファイル + `File.Replace` | ★★ |
| 105 | クラウド同期 | サーバーにはいつも一つ前の設定がアップロードされる | **非同期の書き込みを待たずにそのファイルを読む**、タスクウィンドウ、時間ではなく完了で順番を合わせる | ★★ |

## 일본어판: 기록표 (SOLUTIONS)

### Lab 98 — 相対パスは作業ディレクトリ基準
```
例外:        DirectoryNotFoundException: Could not find a part of the path '...\Lab98_SaveFolder\mods\saves\slot1.sav'.
保存時のパス:  ...\Lab98_SaveFolder\game\saves\slot1.sav
ウォッチ:    Environment.CurrentDirectory == ...\mods,  Path.GetFullPath("saves") == ...\mods\saves
本当の原因:  SavePath が "saves\slot1.sav" という相対パスを作る。相対パスはその時々の作業ディレクトリ基準で、
            フォルダーを開く操作(ファイルダイアログ)が作業ディレクトリを変えた。SaveSystem は GameDir を受け取っているのに使っていない。
```
**修正:** `Path.Combine(GameDir, "saves", slot + ".sav")`、`Directory.CreateDirectory` も同じ基準で。
**教訓:** 作業ディレクトリはプロセス全体で共有される変わりうる状態です。実行ファイルのフォルダーは `AppContext.BaseDirectory`、ユーザーデータは `Environment.GetFolderPath(SpecialFolder.LocalApplicationData)` の下が正解です。

### Lab 99 — 内側のストリームだけ閉じて `StreamWriter` のバッファーが捨てられる
```
SaveLog の最後:   stream.Position が書くべき量より小さい
writer [Raw View] _charPos:   0 ではない (バッファーに残った文字)
本当の原因:  using は FileStream にだけ付いている。最後に残った文字は writer の Flush/Dispose のときだけ出る。
```
**修正:** `using var writer = new StreamWriter(stream);`(宣言と逆順に閉じるので writer が先)、または `using var writer = new StreamWriter(path);`
**教訓:** ラッパーが重なったら**一番外側から**閉じます。内側を使い続けたいなら `leaveOpen: true`。ラボ36はまったく閉じなかったケース、今回は内側だけ閉じたケースです。

### Lab 100 — `FileShare` が合わず書き込み中のファイルを読めない
```
初回例外:  IOException: The process cannot access the file '...chat.log' because it is being used by another process.
止まった所:  File.ReadAllLines (Upload の catch が飲み込んで 0 行アップロード)
本当の原因:  ChatLogger は FileAccess.Write + FileShare.Read、File.ReadAllLines は FileAccess.Read + FileShare.Read で開く。
            つまり「他人が書いていたら拒否」。「別のプロセス」は実はこのプロセス。
```
**修正:** 読む側が書き込みの共有を許可します(`new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)` + `StreamReader`)。
**教訓:** 二つのハンドルが共存するには、互いの `FileAccess` が相手の `FileShare` に含まれている必要があります。Linux/macOS は共有モードを強制しないので、このラボは `SKIP` します。

### Lab 101 — `Path.Combine` の2番目の引数が絶対パス
```
コンソール:  「저장 →」の3行のうち一つだけ ...\old-install\saves\mira
ウォッチ:    Path.Combine(saveRoot, profile.Folder) == profile.Folder,  Path.IsPathRooted(profile.Folder) == true
本当の原因:  Path.Combine は後ろの引数がルートパスなら前の引数をすべて捨てる(ドキュメント通りの動作、例外なし)。
```
**修正:** 外から来た値はフォルダー名としてだけ使います: `Path.Combine(saveRoot, Path.GetFileName(profile.Folder))`。または結合後に `Path.GetFullPath(結果)` がセーブルート内か確認します。
**教訓:** ユーザー入力、セーブファイル、サーバー応答、圧縮ファイル内の名前をそのまま `Path.Combine` に入れるとパストラバーサルになります。

### Lab 102 — バイトを UTF-8 だけでデコード: BOM と CP949
```
MOD A (UTF-8 BOM):  table.Keys.First().Length == firstKey.Length + 1、先頭の文字 '﻿'
MOD B (CP949):      bytes のハングルが2バイトずつ。UTF8.GetString の結果に '�'(�)
本当の原因:  Encoding.UTF8.GetString は BOM を消さず(U+FEFF として残る)、不正なバイトは例外なしに � に置き換える。
```
**修正:** BOM で始まるなら3バイト飛ばして UTF-8、そうでなければ例外を投げる UTF-8 で試し、`DecoderFallbackException` なら CP949(`Encoding.GetEncoding(949)`、コードページプロバイダーの登録が必要)。
**教訓:** .NET Core 以降、CP949・Shift_JIS(932) のようなコードページは `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)` をしないと使えません。保存する側なら BOM なしの UTF-8 に統一します。

### Lab 103 — `\n` だけで区切って `\r` と空の項目が残る
```
コンソール「읽은 항목」:  "Kael\r", "Mira\r", "Doran\r", ""
本当の原因:  メモ帳は CRLF で保存する。Split('\n') は各項目の末尾に '\r' を残し、末尾の改行の後ろに空文字列を作る。
```
**修正:** `File.ReadAllLines(path)`。自分で区切るなら `text.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)`。
**教訓:** `TrimEnd('\r')` だけでは空の項目が残り、`Split("\r\n")` だけでは隠しケース(LF のファイル)で一行につながって引っかかります。

### Lab 104 — 元のファイルを直接上書きして失敗し、書きかけのファイルになる
```
new StreamWriter(path) の直後:  0 バイト
例外のあと:  件数の行 + 前半のアイテム数行だけ (Load が破損と判定)
本当の原因:  new StreamWriter(path) は FileMode.Create なので開いた瞬間に既存のファイルを切り詰める。
```
**修正:** 一時ファイル(`path + ".tmp"`)に最後まで書き、失敗したら一時ファイルを消して再スロー、成功したら `File.Replace(tmp, path, null)`(初回は `File.Move`)。
**教訓:** 停電、強制終了、ディスク容量不足も同じ結果になります。同じボリューム内の `File.Replace`/`File.Move` は名前を変えるだけなので中途半端な状態が残りません。

### Lab 105 — 非同期の書き込みを待たずにそのファイルを読む
```
ブレークポイントの順番:  ReadAllTextAsync が WriteAllTextAsync より先に止まる
ウォッチ:    save.IsCompleted == false
本当の原因:  ApplyAsync が保存 Task をキューに入れるだけで await せずにファイルを読む。読むのは前の設定。
```
**修正:** 読む前に `await save;`。
**教訓:** 順番は時間(`Task.Delay`)ではなくタスクの完了で合わせます。ラボ28は待たなかったタスクがまったく終わらないケース、今回は終わりはするが別のコードが早すぎるタイミングで結果(ファイル)を見るケースです。

## 일본어판: 커버리지 표 줄

| カテゴリ | 代表的なケース | ラボ |
|---|---|---|
| **ファイル入出力** | 相対パスと作業ディレクトリ (`CurrentDirectory` と `AppContext.BaseDirectory`) | 98 |
| | `StreamWriter` のバッファー: `Dispose` 漏れ / 内側のストリームだけ閉じる | 36 / 99 |
| | `FileShare` の衝突 (`IOException`、Windows) | 100 |
| | `Path.Combine` の2番目の引数が絶対パス、パストラバーサル | 101 |
| | エンコーディング: UTF-8 BOM、CP949(`CodePagesEncodingProvider`) | 102 |
| | 改行 `\r\n`/`\n`、末尾の改行の後ろの空の項目 | 103 |
| | 保存途中の失敗で書きかけのファイル (一時ファイル + `File.Replace`) | 104 |
| | 非同期の書き込みを待たずに読む | 105 (ラボ28は終わらない側) |
| | パスの大文字小文字(Windows と Linux)、`MAX_PATH`・使えないファイル名文字、`FileSystemWatcher` のイベント重複・欠落、ウイルス対策ソフトなど外部プロセスのロック、`File.Exists` と開く操作の間の競合(TOCTOU)、セーブ形式のバージョン互換 | 未収録 |

---

## 문자열 표 (실행 출력, 한국어 → 일본어)

### 랩 제목과 증상 (`Program.cs`)

| 한국어 | 日本語 |
|---|---|
| 세이브 폴더 | セーブフォルダー |
| 폴더를 한 번 열고 나면 세이브가 없다고 한다 | フォルダーを一度開くとセーブがないと言われる |
| 전투 기록 파일 | 戦闘ログファイル |
| 저장한 전투 기록의 마지막 몇 줄이 빠져 있다 | 保存した戦闘ログの最後の数行が欠けている |
| 채팅 신고 | チャット通報 |
| 신고하면 채팅 로그를 '사용 중'이라 못 읽는다 | 通報するとチャットログが「使用中」で読めない |
| 프로필 가져오기 | プロフィール取り込み |
| 가져온 프로필의 세이브만 백업에서 빠진다 | 取り込んだプロフィールのセーブだけバックアップから漏れる |
| NPC 대사 모드 | NPC セリフ MOD |
| 모드에 따라 첫 대사가 없거나 한글이 깨진다 | MOD によって最初のセリフがなかったりハングルが文字化けしたりする |
| 차단 목록 | ブロックリスト |
| 메모장으로 고친 뒤 아무도 차단되지 않는다 | メモ帳で編集してから誰もブロックされない |
| 안전한 저장 | 安全なセーブ |
| 저장이 실패하자 지난번 세이브까지 망가졌다 | 保存に失敗したら前回のセーブまで壊れた |
| 클라우드 동기화 | クラウド同期 |
| 서버에는 늘 한 번 전의 설정이 올라간다 | サーバーにはいつも一つ前の設定がアップロードされる |

### `Expect` 설명과 콘솔 출력

| 랩 | 한국어 | 日本語 |
|---|---|---|
| 98 | `불러온 골드` | `ロードしたゴールド` |
| 98 | `  저장: {슬롯} ({n}G) → {경로}` | `  セーブ: {スロット} ({n}G) → {パス}` |
| 98 | `  (폴더 열기 → 작업 디렉터리: {경로})` | `  (フォルダーを開く → 作業ディレクトリ: {パス})` |
| 98 | `  불러오기: {슬롯} → {n}G` | `  ロード: {スロット} → {n}G` |
| 99 | `저장된 줄 수` | `保存された行数` |
| 99 | `마지막 줄` | `最後の行` |
| 99 | `  전투 기록 {n}줄 저장: {경로}` | `  戦闘ログ {n}行を保存: {パス}` |
| 99 | `  파일에서 읽은 줄 수: {n}` | `  ファイルから読んだ行数: {n}` |
| 99 | `[{n}] {직업}의 공격: 피해 {d}` / `[끝] 전투 종료` (파일 내용) | `[{n}] {職業}の攻撃: ダメージ {d}` / `[終] 戦闘終了` |
| 99 | 기사, 궁수, 마법사, 도적 | 騎士、アーチャー、魔法使い、盗賊 |
| 100 | `FileShare 충돌은 Windows 에서만 재현됩니다` (SKIP 사유) | `FileShare の衝突は Windows でのみ再現します` |
| 100 | `업로드한 줄 수` | `アップロードした行数` |
| 100 | `업로드한 마지막 줄` | `アップロードした最後の行` |
| 100 | `  업로드: {n}줄` / `  업로드 실패: {메시지}` | `  アップロード: {n}行` / `  アップロード失敗: {メッセージ}` |
| 100 | `메시지 {n}` / `신고 뒤 메시지 {n}` (파일 내용) | `メッセージ {n}` / `通報後のメッセージ {n}` |
| 101 | `백업된 세이브 수 = 프로필 수` | `バックアップされたセーブ数 = プロフィール数` |
| 101 | `가져온 프로필의 세이브가 세이브 루트 안에 있음` | `取り込んだプロフィールのセーブがセーブルートの中にある` |
| 101 | `  가져온 프로필: {이름} (폴더 값: {경로})` | `  取り込んだプロフィール: {名前} (フォルダーの値: {パス})` |
| 101 | `  {이름} 저장 → {경로}` | `  {名前} を保存 → {パス}` |
| 101 | `  백업: {n}개 파일 (세이브 루트: {경로})` | `  バックアップ: {n}個のファイル (セーブルート: {パス})` |
| 101 | `{이름}의 세이브` (파일 내용) | `{名前}のセーブ` |
| 102 | `{모드}의 첫 대사` / `{모드}의 마지막 대사` | `{MOD}の最初のセリフ` / `{MOD}の最後のセリフ` |
| 102 | `모드 A (UTF-8 BOM)`, `모드 B (CP949)`, `모드 C (UTF-8)` | `MOD A (UTF-8 BOM)`、`MOD B (CP949)`、`MOD C (UTF-8)` |
| 102 | `(없음)` | `(なし)` |
| 102 | 대사 데이터 (CP949 시험용이라 **한국어 그대로 둠**. 일본어판에서 CP932로 바꾼다면 `Encoding.GetEncoding(932)`와 함께 아래 대사로) | いらっしゃい、冒険者よ。/ 北の森のオオカミを倒してくれ。/ 気をつけて行きなさい。/ ギルドへようこそ。/ 君のランクはまだブロンズだな。/ 次の依頼も頼むよ。/ 見ていってください!/ 今日だけポーション半額です。/ またどうぞ。/ この塔は百階ある。/ 上に行くほどモンスターが強くなる。/ 鍵は五十階にある。/ 幸運を祈る。 |
| 103 | `[{형식}] 차단 목록 항목 수` | `[{形式}] ブロックリストの項目数` |
| 103 | `[{형식}] {이름} 의 귓속말 차단` | `[{形式}] {名前} のささやきをブロック` |
| 103 | `{CRLF/LF}, 끝 줄바꿈 {있음/없음}` | `{CRLF/LF}、末尾の改行 {あり/なし}` |
| 103 | `  차단 목록 파일: {n}바이트, {형식}` | `  ブロックリストファイル: {n}バイト、{形式}` |
| 103 | `  읽은 항목: {목록}` | `  読んだ項目: {リスト}` |
| 104 | `저장이 실패해도 지난번 세이브는 그대로` | `保存に失敗しても前回のセーブはそのまま` |
| 104 | `고친 뒤 다시 저장` | `直したあと再保存` |
| 104 | `세이브 폴더에 남은 임시 파일 없음` | `セーブフォルダーに一時ファイルが残っていない` |
| 104 | `  지난번 세이브: {목록}` / `  불러오기: {목록}` / `(손상됨)` | `  前回のセーブ: {リスト}` / `  ロード: {リスト}` / `(破損)` |
| 104 | `  저장 실패: {메시지} → 지난번 세이브를 그대로 쓴다` | `  保存失敗: {メッセージ} → 前回のセーブをそのまま使います` |
| 104 | `손상된 아이템: {이름} x{n}` (예외 메시지) | `壊れたアイテム: {名前} x{n}` |
| 104 | 철광석, 회복 물약, 마나 물약, 용의 비늘, 화살, 빵 | 鉄鉱石、回復ポーション、マナポーション、竜の鱗、矢、パン |
| 105 | `클라우드에 올라간 설정 (볼륨 {v}, 감도 {s})` | `クラウドにアップロードされた設定 (音量 {v}、感度 {s})` |
| 105 | `  설정 변경: 볼륨 {v}, 감도 {s}` | `  設定変更: 音量 {v}、感度 {s}` |
| 105 | `  클라우드에 올림: {내용}` | `  クラウドにアップロード: {内容}` |

---

## 검증 기록

채점기(`dotnet run --project BugHunt.Grader -- 66-73`)로 확인했습니다 (Windows 11, .NET SDK 10.0.401, Debug, 숨은 케이스 3개씩).

- 버그 상태: 0 / 8 (`FAIL` 7, `CRASH` 1: 랩 98 `DirectoryNotFoundException`)
- 정답 수정을 모두 적용: 8 / 8 (기본 + 숨은 케이스)
- 되돌림: 다시 0 / 8
- 증상만 가린 수정 8가지(랩 98 `slot1`만 1250 돌려주기, 99 줄 수 120일 때 메모리 목록 쓰기, 100 0줄이면 5로 치기, 101 `mira`만 세이브 루트로, 102 `﻿`만 지우기, 103 `"\r\n"`으로만 자르기, 104 3번째 아이템만 미리 검사, 105 볼륨 80일 때 값 박기): 랩 98, 99, 101, 103, 104, 105는 기본 케이스는 통과하고 숨은 케이스에서 실패(`증상은 사라졌지만 원인이 남아 있음`), 랩 100, 102는 기본 케이스부터 `FAIL`
- 모든 랩이 끝난 뒤 `%TEMP%\BugHunt\Lab*` 폴더가 남지 않음
- 확인하지 못한 것: Linux/macOS에서의 실행(랩 100은 `SKIP`하도록 해 둠), VS의 16진수 표시·작업 창 화면
