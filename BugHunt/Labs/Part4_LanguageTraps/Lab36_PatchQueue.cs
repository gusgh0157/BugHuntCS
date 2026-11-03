namespace BugHunt;

using System.Collections.Concurrent;

// 패치 파일 여러 개를 병렬로 내려받는다. 각 작업에 조각 번호를 넘기는데, 받은 조각 번호가 뒤죽박죽이고 범위 밖 번호까지 섞인다.
// [JA] パッチファイルを複数、並列でダウンロードします。各タスクに断片番号を渡すのに、受け取った断片番号がばらばらで、範囲外の番号まで混ざります。
public static partial class Lab36
{
    public static void Run()
    {
        int pieces = Pick(5, 3, 8);
        var downloaded = new ConcurrentBag<int>();
        var gate = new ManualResetEventSlim(false);
        var tasks = new List<Task>();

        // 조각마다 다운로드 작업을 하나씩 띄운다.
        for (int i = 0; i < pieces; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                gate.Wait();          // 모든 작업이 만들어진 뒤 동시에 시작하도록
                downloaded.Add(i);    // 이 조각 번호를 받았다고 기록
            }));
        }

        gate.Set();
        Task.WhenAll(tasks).GetAwaiter().GetResult();

        int[] got = [.. downloaded.OrderBy(x => x)];
        int[] expected = [.. Enumerable.Range(0, pieces)];
        Console.WriteLine($"  조각 {pieces}개, 받은 번호: {string.Join(", ", got)}");

        Expect("받은 조각 번호(정렬)", string.Join(",", expected), string.Join(",", got));
        Expect("모든 조각을 정확히 한 번씩", true, got.SequenceEqual(expected));
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// downloaded.Add(i) 에 중단점을 걸고, 멈출 때마다 조사식에서 i 를 보세요. 작업이 실제로 도는 시점의 i 는 몇인가요?
// gate.Set() 줄에 중단점을 걸고, 거기서 i 는 이미 몇인가요? (for 를 다 돈 뒤입니다.)
//
// [JA] downloaded.Add(i) にブレークポイントを置き、止まるたびにウォッチで i を見てください。タスクが実際に動く時点の i はいくつですか?
// [JA] gate.Set() の行にブレークポイントを置くと、そこで i はもういくつですか?(for を回し終えたあとです。)
#endregion

#region 힌트 2 / ヒント 2
// for 의 i 는 반복마다 새로 만들어지는 변수가 아니라 "하나의 변수"입니다. 람다 () => downloaded.Add(i) 는 그 변수를 값이 아니라 참조로 캡처합니다.
// gate.Wait() 때문에 작업은 for 가 전부 끝난 뒤에 실행됩니다. 그때 i 는 이미 pieces(끝 값)이거나 그 근처라, 대부분의 작업이 같은 번호(또는 범위 밖 번호)를 기록합니다.
// 중요한 차이: foreach 의 반복 변수는 C# 9부터 반복마다 새로 만들어져 캡처해도 안전하지만, for 의 변수는 그렇지 않습니다.
//
// [JA] for の i は反復ごとに新しく作られる変数ではなく「一つの変数」です。ラムダ () => downloaded.Add(i) はその変数を値ではなく参照でキャプチャします。
// [JA] gate.Wait() のため、タスクは for がすべて終わったあとに実行されます。そのとき i はすでに pieces(終わりの値)かその近くで、ほとんどのタスクが同じ番号(または範囲外の番号)を記録します。
// [JA] 重要な違い: foreach の反復変数は C# 5 から反復ごとに新しく作られるのでキャプチャしても安全ですが、for の変数はそうではありません。
#endregion

#region 힌트 3 / ヒント 3
// 반복마다 그 시점의 값을 고정하세요:
//   for (int i = 0; i < pieces; i++) { int piece = i; tasks.Add(Task.Run(() => downloaded.Add(piece))); }
// 또는 foreach 로 바꾸세요: foreach (int piece in Enumerable.Range(0, pieces)) tasks.Add(Task.Run(() => downloaded.Add(piece)));
// 또는 Task.Run(() => ...) 에 상태를 인자로 넘기세요.
// 핵심: 람다가 "변수를 캡처"하는지 "값을 캡처"하는지 구별하세요. 루프 변수는 공유되는 변경 가능 상태입니다.
//
// [JA] 反復ごとにその時点の値を固定してください:
// [JA]   for (int i = 0; i < pieces; i++) { int piece = i; tasks.Add(Task.Run(() => downloaded.Add(piece))); }
// [JA] または foreach に変えてください: foreach (int piece in Enumerable.Range(0, pieces)) tasks.Add(Task.Run(() => downloaded.Add(piece)));
// [JA] または Task.Run(() => ...) に状態を引数で渡してください。
// [JA] 核心: ラムダが「変数をキャプチャする」のか「値をキャプチャする」のかを区別してください。ループ変数は共有される変更可能な状態です。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 조각 수를 바꿔 검사한다. 루프 변수를 반복마다 복사하거나 foreach 로 고치면 통과한다.
// [JA] 隠しケースは断片数を変えて検査します。ループ変数を反復ごとにコピーするか foreach に直せば通ります。
#endregion
