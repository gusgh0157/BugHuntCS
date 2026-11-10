namespace BugHunt.Challenge.C2;

// 한 스레드에서 도는 게임 루프. await 뒤의 이어서 실행(continuation)은 이 루프에 올라가 다음 프레임에 돈다.
// (유니티의 메인 스레드, WinForms/WPF 의 UI 스레드와 같은 모양)
// [JA] 一つのスレッドで回るゲーム ループ。await のあとの続きの実行(continuation)はこのループに載り、次のフレームで回ります。
// [JA] (Unity のメイン スレッド、WinForms/WPF の UI スレッドと同じ形)
public sealed class GameLoop : SynchronizationContext
{
    readonly Queue<(SendOrPostCallback Callback, object? State)> _posted = new();
    List<TaskCompletionSource> _waitingForNextFrame = [];

    public int Frame { get; private set; }

    public override void Post(SendOrPostCallback d, object? state) => _posted.Enqueue((d, state));

    public override void Send(SendOrPostCallback d, object? state) => d(state);

    // 다음 프레임이 시작될 때 끝나는 Task
    // [JA] 次のフレームが始まるときに完了する Task
    public Task NextFrame()
    {
        var tcs = new TaskCompletionSource();
        _waitingForNextFrame.Add(tcs);
        return tcs.Task;
    }

    // 한 프레임: 다음 프레임을 기다리던 것들을 깨우고, 쌓인 이어서 실행을 돌린 뒤, 게임 로직(update)을 부른다.
    // [JA] 1 フレーム: 次のフレームを待っていたものを起こし、たまった続きの実行を回してから、ゲーム ロジック(update)を呼びます。
    public void RunFrame(Action update)
    {
        SynchronizationContext? previous = Current;
        SetSynchronizationContext(this);
        try
        {
            Frame++;
            List<TaskCompletionSource> waiting = _waitingForNextFrame;
            _waitingForNextFrame = [];
            foreach (TaskCompletionSource tcs in waiting)
                tcs.SetResult();
            while (_posted.Count > 0)
            {
                (SendOrPostCallback callback, object? state) = _posted.Dequeue();
                callback(state);
            }
            update();
        }
        finally
        {
            SetSynchronizationContext(previous);
        }
    }
}
