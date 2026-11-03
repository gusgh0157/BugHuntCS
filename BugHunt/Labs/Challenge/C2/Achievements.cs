namespace BugHunt.Challenge.C2;

// 서버에 있는 업적 진행도 (진짜 기록). 읽어 오는 데 한 프레임 걸린다.
public sealed class ProgressServer(GameLoop loop)
{
    readonly Dictionary<string, int> _saved = [];
    public int Reads { get; private set; }

    public async Task<Dictionary<string, int>> ReadAllAsync()
    {
        Reads++;
        await loop.NextFrame();   // 네트워크 왕복
        return new Dictionary<string, int>(_saved);
    }

    public void Write(string questId, int progress) => _saved[questId] = progress;
}

// 클라이언트 쪽 진행도 캐시. 처음 쓸 때 서버에서 한 번 읽어 오고, 그 뒤로는 캐시를 쓴다.
// 채널을 옮기면 캐시를 버리고 다시 읽는다.
public sealed class AchievementStore(ProgressServer server)
{
    Dictionary<string, int>? _progress;
    Task? _loading;
    readonly HashSet<string> _unlocked = [];

    public bool Loaded => _progress != null;

    public Task LoadAsync() => _loading ??= LoadCoreAsync();

    async Task LoadCoreAsync()
    {
        try
        {
            _progress = await server.ReadAllAsync();
        }
        finally
        {
            _loading = null;
        }
    }

    public void Invalidate() => _progress = null;

    public int Add(string questId, int amount)
    {
        if (_progress == null)
            throw new InvalidOperationException("진행도를 아직 불러오지 않았다");
        int now = _progress.GetValueOrDefault(questId) + amount;
        _progress[questId] = now;
        server.Write(questId, now);
        return now;
    }

    public int Get(string questId) => _progress?.GetValueOrDefault(questId) ?? 0;

    public bool Unlock(string questId) => _unlocked.Add(questId);

    public IReadOnlyCollection<string> Unlocked => _unlocked;
}

// 퀘스트 진행 알림을 받아 업적 진행도를 올리고, 목표에 닿으면 업적을 연다.
public sealed class AchievementTracker(AchievementStore store, IReadOnlyDictionary<string, int> goals)
{
    int _handled;
    public int Handled => _handled;

    public void Attach(QuestEventBus bus) => bus.Progress += OnProgressAsync;

    async Task OnProgressAsync(QuestProgress e)
    {
        if (!store.Loaded)
            await store.LoadAsync();

        int now = store.Add(e.QuestId, e.Amount);
        _handled += 1;
        if (goals.TryGetValue(e.QuestId, out int goal) && now >= goal && store.Unlock(e.QuestId))
            Console.WriteLine($"  [업적] {e.QuestId} 달성! ({now}/{goal})");
    }
}
