using System.Collections.Concurrent;

namespace BugHunt;

// 게임 엔진의 "메인 스레드"를 흉내 낸다. (WinForms/WPF 의 UI 스레드, 유니티의 메인 스레드와 같은 역할)
// 콘솔 앱에는 SynchronizationContext 가 없어서 await 뒤의 코드가 아무 스레드에서나 실행된다.
// 이 클래스는 일을 큐에 받아 스레드 하나에서만 차례로 실행하고, 자신을 SynchronizationContext 로 등록한다.
// 그래서 이 스레드에서 시작한 await 는 끝난 뒤 다시 이 큐로 돌아온다.
// 랩 63, 67 에서 쓴다. 이 파일에는 버그가 없다.
// [JA] ゲームエンジンの「メインスレッド」をまねます。(WinForms/WPF の UI スレッド、Unity のメインスレッドと同じ役割)
// [JA] コンソール アプリには SynchronizationContext がないので、await のあとのコードは任意のスレッドで実行されます。
// [JA] このクラスは仕事をキューで受け取り、一つのスレッドだけで順に実行し、自分を SynchronizationContext として登録します。
// [JA] そのため、このスレッドで始めた await は、終わったあと再びこのキューに戻ってきます。
// [JA] ラボ 63、67 で使います。このファイルにはバグはありません。
public sealed class MainThread : SynchronizationContext
{
    readonly BlockingCollection<Action> _queue = [];
    readonly Thread _thread;

    public MainThread()
    {
        _thread = new Thread(Loop) { Name = "Main Thread (게임 루프)", IsBackground = true };
        _thread.Start();
    }

    public int ThreadId => _thread.ManagedThreadId;

    // 실행을 기다리는 일의 개수. 조사식에 넣어 보면 큐에 무엇이 쌓였는지 알 수 있다.
    public int PendingCount => _queue.Count;

    void Loop()
    {
        SetSynchronizationContext(this);
        foreach (Action work in _queue.GetConsumingEnumerable())
            work();
    }

    public override void Post(SendOrPostCallback d, object? state) => _queue.Add(() => d(state));

    public override void Send(SendOrPostCallback d, object? state) =>
        throw new NotSupportedException("이 예제에서는 Send 를 쓰지 않는다.");

    public override SynchronizationContext CreateCopy() => this;

    // 메인 스레드에서 work 를 실행한다. 돌려준 Task 는 work 가 끝나면 완료된다.
    public Task Run(Action work)
    {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _queue.Add(() =>
        {
            try { work(); done.SetResult(); }
            catch (Exception ex) { done.SetException(ex); }
        });
        return done.Task;
    }

    // 메인 스레드에서 비동기 work 를 시작한다. 돌려준 Task 는 work 의 Task 가 끝나면 완료된다.
    public Task Run(Func<Task> work)
    {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _queue.Add(() =>
        {
            Task task;
            try { task = work(); }
            catch (Exception ex) { done.SetException(ex); return; }
            task.ContinueWith(t =>
            {
                if (t.IsFaulted) done.SetException(t.Exception!.InnerExceptions);
                else if (t.IsCanceled) done.SetCanceled();
                else done.SetResult();
            }, TaskScheduler.Default);
        });
        return done.Task;
    }
}
