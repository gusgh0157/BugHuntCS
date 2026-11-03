namespace BugHunt.Challenge.C2;

public sealed class QuestProgress
{
    public string QuestId = "";
    public int Amount;
    public int Frame;

    public override string ToString() => $"{QuestId} +{Amount} (프레임 {Frame})";
}

// 퀘스트 진행 알림. 몬스터가 한꺼번에 수백 마리 잡히는 프레임도 있어서,
// 알림 객체는 매번 만들지 않고 하나를 다시 쓴다 (8부: 핫 경로에서 할당 줄이기).
public sealed class QuestEventBus(GameLoop loop)
{
    readonly QuestProgress _current = new();
    readonly List<Task> _inFlight = [];

    public event Func<QuestProgress, Task>? Progress;

    // 아직 끝나지 않은 처리기 수
    public int Pending
    {
        get
        {
            _inFlight.RemoveAll(t => t.IsCompleted);
            return _inFlight.Count;
        }
    }

    public IEnumerable<Exception> Faults => _inFlight.Where(t => t.IsFaulted).SelectMany(t => t.Exception!.InnerExceptions);

    public void Publish(string questId, int amount)
    {
        _current.QuestId = questId;
        _current.Amount = amount;
        _current.Frame = loop.Frame;

        if (Progress == null)
            return;
        foreach (Func<QuestProgress, Task> handler in Progress.GetInvocationList().Cast<Func<QuestProgress, Task>>())
        {
            Task task = handler(_current);
            if (task.IsFaulted)
                throw task.Exception!.InnerException!;
            if (!task.IsCompleted)
                _inFlight.Add(task);
        }
    }
}
