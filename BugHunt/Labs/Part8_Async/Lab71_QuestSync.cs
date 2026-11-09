using System.Text.RegularExpressions;

namespace BugHunt;

// 퀘스트 데이터를 서버에서 받아 파싱한다. 네트워크가 흔들릴 수 있으니 세 번까지 다시 시도한다.
// 끝내 실패하면 오류 보고서에 "무슨 예외가 어느 함수에서" 났는지 남긴다. 담당자는 그 함수부터 본다.
// [JA] クエストデータをサーバーから受け取ってパースします。ネットワークが不安定なことがあるので、三回まで再試行します。
// [JA] 最後まで失敗したら、エラーレポートに「どんな例外がどの関数で」起きたかを残します。担当者はその関数から見ます。
public static class Lab71
{
    const int Attempts = 3;

    record Quest(string Id, string Title, int Reward);

    static async Task<T> RetryAsync<T>(Func<Task<T>> body)
    {
        Exception? last = null;
        for (int attempt = 1; attempt <= Attempts; attempt++)
        {
            try
            {
                return await body();
            }
            catch (Exception ex)
            {
                last = ex;
                Console.WriteLine(Text.L($"  [재시도 {attempt}/{Attempts}] {ex.GetType().Name}: {ex.Message}", $"  [再試行 {attempt}/{Attempts}] {ex.GetType().Name}: {ex.Message}"));
                await Task.Delay(5);
            }
        }
        throw last!;
    }

    static async Task<string> DownloadAsync(Dictionary<string, string?> server, string id)
    {
        await Task.Delay(1);
        return server[id] ?? throw new IOException(Text.L($"{id}: 서버 연결이 끊겼습니다", $"{id}: サーバーとの接続が切れました"));
    }

    static async Task<string> ParseHeaderAsync(string text)
    {
        await Task.Yield();
        int bar = text.IndexOf('|');
        if (bar < 0)
            throw new InvalidDataException(Text.L($"헤더가 없습니다: \"{text}\"", $"ヘッダーがありません: \"{text}\""));
        return text[..bar];
    }

    static async Task<int> ParseRewardAsync(string field)
    {
        await Task.Yield();
        return int.Parse(field);
    }

    static async Task<Quest> ParseQuestAsync(string id, string text)
    {
        string title = await ParseHeaderAsync(text);
        int reward = await ParseRewardAsync(text[(text.IndexOf('|') + 1)..]);
        return new Quest(id, title, reward);
    }

    static async Task<Quest> LoadQuestAsync(Dictionary<string, string?> server, string id) =>
        await ParseQuestAsync(id, await DownloadAsync(server, id));

    // 오류 보고서: 예외 형식과, 스택 트레이스에서 이 랩의 코드 중 맨 위에 있는 함수
    static string Report(Exception ex)
    {
        Match top = Regex.Match(ex.StackTrace ?? "", @"at BugHunt\.Lab71\.(\w+)");
        return $"{ex.GetType().Name} @ {(top.Success ? top.Groups[1].Value : "?")}";
    }

    public static void Run()
    {
        var server = new Dictionary<string, string?>
        {
            ["q1"] = Text.L("늑대 사냥", "オオカミ狩り") + "|120",
            ["q2"] = Text.L("약초 채집", "薬草採取") + "|80",
            ["q3"] = Text.L("광산 정찰", "鉱山の偵察") + "|" + Text.L("두백", "二百"),
        };
        string broken = "q3";
        string expected = "FormatException @ ParseRewardAsync";
        if (Seed != 0)
        {
            server["q3"] = Text.L("광산 정찰", "鉱山の偵察") + "|200";
            broken = Pick("q1", "q1", "q2", "q3");
            (server[broken], expected) = Pick(1, 1, 2) switch   // 숨은 케이스는 기본 케이스와 다른 곳에서 실패한다
            {
                0 => ((string?)(Text.L("고장 난 데이터", "壊れたデータ") + "|??"), "FormatException @ ParseRewardAsync"),
                1 => (Text.L("헤더 없는 데이터", "ヘッダーのないデータ"), "InvalidDataException @ ParseHeaderAsync"),
                _ => (null, "IOException @ DownloadAsync"),
            };
        }

        var loaded = new List<Quest>();
        string report = "";
        foreach (string id in server.Keys)
        {
            try
            {
                loaded.Add(RetryAsync(() => LoadQuestAsync(server, id)).GetAwaiter().GetResult());
            }
            catch (Exception ex)
            {
                report = Report(ex);
                Console.WriteLine(Text.L($"  --- {id} 오류 보고서: {report} ---\n{ex.StackTrace}\n  ---------------------------", $"  --- {id} エラーレポート: {report} ---\n{ex.StackTrace}\n  ---------------------------"));
            }
        }

        Console.WriteLine(Text.L($"  불러온 퀘스트: {string.Join(", ", loaded.Select(q => q.Title))}", $"  読み込んだクエスト: {string.Join(", ", loaded.Select(q => q.Title))}"));
        Expect(Text.L($"불러온 퀘스트 수 ({broken} 빼고)", $"読み込んだクエスト数 ({broken} を除く)"), server.Count - 1, loaded.Count);
        Expect(Text.L("오류 보고서 (예외 @ 실패한 함수)", "エラーレポート (例外 @ 失敗した関数)"), expected, report);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 콘솔에 찍힌 스택 트레이스를 위에서부터 읽어 보세요. 맨 위의 BugHunt 줄은 어느 함수인가요?
// 재시도 로그에는 진짜 원인(예외 형식과 메시지)이 제대로 찍혀 있습니다. 그런데 보고서는 왜 다른 곳을 가리킬까요?
//
// [JA] コンソールに出力されたスタック トレースを上から読んでみてください。一番上の BugHunt の行はどの関数ですか?
// [JA] 再試行ログには本当の原因(例外の型とメッセージ)が正しく出力されています。それなのに、レポートはなぜ別の場所を指すのでしょう?
#endregion

#region 힌트 2 / ヒント 2
// 예외 설정 창(Ctrl+Alt+E)에서 System.FormatException 을 체크하고 다시 실행하세요. 처음 던져진 곳에서 멈춥니다.
// 그때의 호출 스택과, 보고서에 남은 스택 트레이스를 비교해 보세요. 예외 개체는 같은데(개체 ID를 만들어 확인) 스택 트레이스는 언제 바뀌나요?
//
// [JA] 例外設定ウィンドウ(Ctrl+Alt+E)で System.FormatException をチェックして、もう一度実行してください。最初にスローされた場所で止まります。
// [JA] そのときの呼び出し履歴と、レポートに残ったスタック トレースを比べてみてください。例外オブジェクトは同じなのに(オブジェクト ID を作成して確認)、スタック トレースはいつ変わりますか?
#endregion

#region 힌트 3 / ヒント 3
// 랩 7 과 같은 종류지만, 여기서는 catch 블록 "밖에서" 다시 던집니다. 그래서 throw; 를 쓸 수 없습니다.
// 예외를 잡아 두었다가 나중에(다른 곳에서, await 뒤에서) 원래 스택 트레이스 그대로 다시 던지는 도구가 있습니다.
// System.Runtime.ExceptionServices 네임스페이스를 보세요. 또는 마지막 시도에서는 아예 잡지 않는 방법도 있습니다(catch ... when).
//
// [JA] ラボ 7 と同じ種類ですが、ここでは catch ブロックの「外で」再スローします。だから throw; は使えません。
// [JA] 例外を捕まえておいて、あとで(別の場所で、await のあとで)元のスタック トレースのまま再スローする道具があります。
// [JA] System.Runtime.ExceptionServices 名前空間を見てください。または、最後の試行ではそもそも捕まえない方法もあります(catch ... when)。
#endregion
