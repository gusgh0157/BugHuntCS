namespace BugHunt;

// 파티원들이 던전에 입장한다. 플레이어가 취소를 누르면 조용히 로비로 돌아가고,
// 던전 서버가 응답하지 않으면(시간 초과) 예비 채널로 한 번 더 들어가 본다.
// [JA] パーティーメンバーがダンジョンに入場します。プレイヤーがキャンセルを押したら静かにロビーに戻り、
// [JA] ダンジョンサーバーが応答しなければ(タイムアウト)予備チャンネルでもう一度入ってみます。
public static class Lab70
{
    const int LoadTimeoutMs = 300;

    enum Kind { Normal, UserCancels, ServerHangs }

    static async Task LoadDungeonAsync(Kind kind, CancellationToken token)
    {
        if (kind == Kind.Normal)
            return;   // 서버가 바로 응답한다
        await Task.Delay(Timeout.Infinite, token);   // 응답 없는 서버, 또는 플레이어가 취소할 때까지 로딩 중
    }

    static async Task<string> TryEnterAsync(Kind kind, CancellationToken userCancel)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(userCancel);
        timeout.CancelAfter(LoadTimeoutMs);
        try
        {
            await LoadDungeonAsync(kind, timeout.Token);
            return "입장";
        }
        catch (OperationCanceledException)
        {
            return "취소됨";   // 플레이어가 취소했다
        }
    }

    static async Task<string> EnterAsync(string name, Kind kind)
    {
        using var userCancel = new CancellationTokenSource();
        Task<string> entering = TryEnterAsync(kind, userCancel.Token);
        if (kind == Kind.UserCancels)
            userCancel.Cancel();   // 로딩 중에 취소 버튼을 누른다

        string result = await entering;
        if (result == "시간 초과")
        {
            Console.WriteLine($"  {name}: 던전 서버가 대답하지 않습니다. 예비 채널로 다시 시도");
            result = await TryEnterAsync(Kind.Normal, userCancel.Token) + " (예비 채널)";
        }
        return result;
    }

    public static void Run()
    {
        var party = new List<(string Name, Kind Kind)>
        {
            ("Kael", Kind.Normal), ("Mira", Kind.ServerHangs), ("Doran", Kind.UserCancels), ("Lia", Kind.Normal),
        };
        if (Seed != 0)
        {
            Kind[] kinds = [Kind.Normal, Kind.UserCancels, Kind.ServerHangs];
            for (int i = 0; i < party.Count; i++)
                party[i] = (party[i].Name, Pick(Kind.Normal, kinds));
            party.Add(("Sera", Kind.ServerHangs));   // 숨은 케이스에도 응답 없는 서버는 꼭 하나 있다
            party.Add(("Bran", Kind.UserCancels));
        }

        string[] results = Task.WhenAll(party.Select(p => EnterAsync(p.Name, p.Kind))).GetAwaiter().GetResult();
        for (int i = 0; i < party.Count; i++)
            Console.WriteLine($"  {party[i].Name}: {results[i]}");

        string Expected(Kind kind) => kind switch
        {
            Kind.Normal => "입장",
            Kind.UserCancels => "취소됨",
            _ => "입장 (예비 채널)",
        };
        Expect("파티원별 입장 결과",
               string.Join(", ", party.Select(p => $"{p.Name} {Expected(p.Kind)}")),
               string.Join(", ", party.Select((p, i) => $"{p.Name} {results[i]}")));
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// "취소됨" 을 돌려주는 줄에 중단점을 거세요. 멈출 때마다 조사식에 userCancel.IsCancellationRequested 와 timeout.Token.IsCancellationRequested 를 넣어 보세요.
// kind 가 ServerHangs 인, 취소를 누르지 않은 플레이어인데도 여기서 멈추나요?
//
// [JA] "취소됨"(キャンセル済み)を返す行にブレークポイントを置いてください。止まるたびにウォッチに userCancel.IsCancellationRequested と timeout.Token.IsCancellationRequested を入れてみてください。
// [JA] kind が ServerHangs の、キャンセルを押していないプレイヤーなのに、ここで止まりますか?
#endregion

#region 힌트 2 / ヒント 2
// 예외 설정(Ctrl+Alt+E)에서 System.Threading.Tasks.TaskCanceledException 을 체크하면 던져지는 순간에 멈춥니다.
// 그때 예외의 CancellationToken 속성은 어느 토큰인가요? userCancel 인가요, 연결된(linked) 토큰인가요?
//
// [JA] 例外設定(Ctrl+Alt+E)で System.Threading.Tasks.TaskCanceledException をチェックすると、スローされた瞬間に止まります。
// [JA] そのとき例外の CancellationToken プロパティはどのトークンですか? userCancel ですか、リンクされた(linked)トークンですか?
#endregion

#region 힌트 3 / ヒント 3
// OperationCanceledException 은 "누가" 취소했는지 말해 주지 않습니다. 사용자 취소도, CancelAfter 시간 초과도 같은 예외입니다.
// catch 에 조건을 붙일 수 있습니다: catch (...) when (조건). 조건이 거짓이면 그 catch 는 없는 것처럼 지나갑니다.
// 어느 토큰을 물어봐야 "플레이어가 눌렀다"를 알 수 있을까요? (ex.CancellationToken 은 연결된 토큰이라는 점에 주의)
//
// [JA] OperationCanceledException は「誰が」キャンセルしたかを教えてくれません。ユーザーのキャンセルも、CancelAfter のタイムアウトも同じ例外です。
// [JA] catch に条件を付けられます: catch (...) when (条件)。条件が偽なら、その catch はないものとして素通りします。
// [JA] どのトークンに尋ねれば「プレイヤーが押した」とわかるでしょう? (ex.CancellationToken はリンクされたトークンである点に注意)
#endregion
