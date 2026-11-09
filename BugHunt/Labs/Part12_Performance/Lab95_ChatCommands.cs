namespace BugHunt;

// 채팅 명령 "/roll 숫자"를 처리한다. 숫자가 아니면 기본값 100. 결과는 맞는데, 장난으로 이상한 명령을 도배하면 서버가 느려진다.
// [JA] チャット コマンド「/roll 数字」を処理します。数字でなければ既定値 100。結果は合っていますが、いたずらで変なコマンドを連投されるとサーバーが遅くなります。
public static class Lab95
{
    static int ParseRollMax(string command)
    {
        string arg = command.Length > 6 ? command[6..] : "";
        try
        {
            return int.Parse(arg);
        }
        catch (FormatException)
        {
            return 100;
        }
    }

    public static void Run()
    {
        string[] commands = ["/roll 20", Text.L("/roll ㅋㅋㅋ", "/roll www"), "/roll", "/roll 6", Text.L("/roll 백", "/roll 百"), "/roll 1d6", "/roll 100"];
        int count = Pick(20_000, 10_000, 30_000);
        int offset = Pick(0, 0, commands.Length - 1);
        ParseRollMax("/roll x");   // 처음 한 번만 드는 비용 빼기

        long exceptions = 0;
        EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs> counter = (_, _) => Interlocked.Increment(ref exceptions);
        AppDomain.CurrentDomain.FirstChanceException += counter;
        long sum = 0;
        (_, long ms) = Measure(() =>
        {
            for (int i = 0; i < count; i++)
                sum += ParseRollMax(commands[(i + offset) % commands.Length]);
        });
        AppDomain.CurrentDomain.FirstChanceException -= counter;
        Console.WriteLine(Text.L($"  명령 {count:N0}개 처리, 최댓값 합 {sum:N0}. 그동안 던져진 예외 {exceptions:N0}개, {ms}ms", $"  コマンド {count:N0} 個を処理、最大値の合計 {sum:N0}。その間に投げられた例外 {exceptions:N0} 個、{ms}ms"));

        long expected = 0;
        for (int i = 0; i < count; i++)
        {
            string c = commands[(i + offset) % commands.Length];
            expected += c.Length > 6 && c[6..].All(char.IsAsciiDigit) ? int.Parse(c[6..]) : 100;
        }
        Expect(Text.L("최댓값 합", "最大値の合計"), expected, sum);
        ExpectAtMost(Text.L("처리 중 던져진 예외 수", "処理中に投げられた例外の数"), 0, exceptions);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// dotnet-counters monitor -n BugHunt --counters System.Runtime 에서 dotnet.exceptions(예외 수, .NET 9 이상)를 보세요.
// VS 라면 예외 설정(Ctrl+Alt+E)에서 FormatException 을 체크하고 실행해 보세요. 몇 번이나 멈추나요?
//
// [JA] dotnet-counters monitor -n BugHunt --counters System.Runtime で dotnet.exceptions(例外の数、.NET 9 以上)を見てください。
// [JA] VS なら例外設定(Ctrl+Alt+E)で FormatException にチェックを入れて実行してみてください。何回止まりますか?
#endregion

#region 힌트 2 / ヒント 2
// 예외는 던질 때 스택 트레이스를 모으고, catch 를 찾아 스택을 거슬러 올라갑니다. 일반 if 문보다 수천 배 비쌉니다.
// 성능 프로파일러의 CPU 사용량 핫 경로에 예외 처리 관련 내부 함수가 얼마나 보이나요?
//
// [JA] 例外は投げるときにスタック トレースを集め、catch を探してスタックをさかのぼります。普通の if 文より何千倍も高くつきます。
// [JA] パフォーマンス プロファイラーの CPU 使用率のホット パスに、例外処理関連の内部関数がどれくらい見えますか?
#endregion

#region 힌트 3 / ヒント 3
// "흔히 일어나는 실패"(사용자 입력이 숫자가 아님)는 예외가 아니라 값으로 다루세요: int.TryParse.
// 예외는 정말 예외적인 상황용입니다. 출력 창에 "예외 발생: System.FormatException"이 줄줄이 찍힌다면 그 자체가 단서입니다.
//
// [JA] 「よく起きる失敗」(ユーザー入力が数字でない)は、例外ではなく値として扱ってください: int.TryParse。
// [JA] 例外は本当に例外的な状況のためのものです。出力ウィンドウに「例外がスローされました: 'System.FormatException'」がずらずら出るなら、それ自体が手がかりです。
#endregion
