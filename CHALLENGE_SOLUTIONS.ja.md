[한국어](CHALLENGE_SOLUTIONS.md)

# BugHunt C# チャレンジ解答

> **ネタバレ注意。** チャレンジはヒントなしで解くのが規則です。採点ツールで `PASS` が出たあとに答え合わせをしてください。
> 隠しケースでだけ失敗する(`症状は消えたが原因が残っている`)なら、まだ原因を直していません。

---

# 深い追跡 (C)

## チャレンジ C-1 — 逆シリアル化で消えたパケット番号

**広がる経路:**
```
1. RewardPacket.Id は { get; private set; }。System.Text.Json は公開 setter のないプロパティを逆シリアル化するとき黙って飛ばす
2. クライアントが受けたパケットはコンストラクターの = Guid.NewGuid() で「新しい番号」を付ける。回線上の JSON には元の番号がちゃんとある
3. クライアントは新しい番号で ack を送る → サーバーの _unacked から何も消えない → サーバーは再送し続ける
4. 再送されたパケットもまた新しい番号を付ける → RewardInbox の _seen の重複チェックを通過する
5. RewardArrived → InventoryView に同じアイテムが再送回数ぶん積み上がる (サーバーが諦めるまで)
```

**偽の容疑者:**
- **InventoryView で重複排除** (`Distinct`、`Contains` なら飛ばす): 基本ケースは通ります。しかし一人が同じアイテムを本当に二回受け取る隠しケースでは報酬が消えます。
- **サーバーの再送を切る** (`MaxAttempts = 1`): 基本ケースは通ります。しかしパケットが実際に消える隠しケースでは報酬が来ません。
- **受信箱で (人、アイテム) で重複チェック**: 正常な重複まで捨てます。

**追跡の方法:**
- サーバー `Transmit` と受信箱 `Receive` にトレースポイント(tracepoint)をかけます: `{outgoing.Packet.Id}` / `{packet.Id}`。送った番号と受けた番号が一度も同じになりません。
- `Wire.Decode` に `json.Contains("Yuna")` の条件付きブレークポイントをかけます(アイテム名は JSON で `\uXXXX` にエスケープされるので、ASCII のメンバー名で絞ります)。ローカルウィンドウで `json` の `"id"` と結果 `packet.Id` を比べると、回線はちゃんとしていて逆シリアル化で番号が変わります。
- サーバーのパケットにオブジェクト ID(`$1`)を作っておくと、再送は常に同じオブジェクト(`$1`)を送ります。したがって番号はサーバーでは変わりません。

**修正:** 逆シリアル化できるように開く。`C1/RewardPacket.cs` の `RewardPacket` の `public Guid Id { get; private set; } = Guid.NewGuid();` の行を次の一行に置き換える。ほかのファイルはそのまま。
```csharp
    public Guid Id { get; init; } = Guid.NewGuid();
```
`private set` のまま `[JsonInclude]` を付けるか、コンストラクターの引数で受け取ってもよい。

**教訓:** シリアル化の往復(`Deserialize(Serialize(x))`)が元の値を返すか試すテストを一つ置いてください。System.Text.Json は private setter、読み取り専用プロパティ、フィールドを既定で**黙って**飛ばします。直すときは `[JsonInclude]` を付けるか `init` アクセサーに変えます。(`JsonUnmappedMemberHandling.Disallow` は JSON にあって型にないメンバーを防ぐオプションなので、型にあるのに飛ばされる `private set` は捕まえられません。)症状が現れた所(バッグ)と原因(逆シリアル化)の間に ack、再送、重複チェックが挟まっていると、症状の近くを塞ぐ修正は別の正常な動作(正当な重複、本当の再送)を壊します。

**カテゴリ:** シリアル化の往復 + キャッシュ・重複チェック (7部シリアル化 + 2部フロー追跡 + 3部コンテナの重複チェック)

## チャレンジ C-2 — await の向こうで読んだ再利用オブジェクト

**広がる経路:**
```
1. QuestEventBus は割り当てを減らそうと QuestProgress オブジェクト一つ(_current)を使い回す
2. AchievementTracker.OnProgressAsync は進捗をまだ読めていなければ await store.LoadAsync() で1フレーム待つ
3. その間に同じフレームの次の Publish が _current の QuestId/Amount を上書きする
4. 次のフレームに続けて実行されたハンドラーがすべて同じ _current を読む → 最後の通知の内容で何度も足す
5. 間違ったクエストに進捗がたまり、見当違いの実績が開く
```

**偽の容疑者:**
- **ログイン時に先読みする** (`store.LoadAsync()` のあと1フレーム回す): 基本ケースは通ります。しかしチャンネルを移ってキャッシュを捨てた直後にモンスターを倒す隠しケースで同じことがまた起きます。
- **`_handled += 1` の競合状態**: すべての処理がゲームループの一スレッドで回るので競合がありません。`Interlocked` に変えてもそのままです。
- **`.Wait()`/`.Result` で同期ロード**: ゲームループが止まります(ラボ 63)。ロードを終わらせる次のフレームが来ません。

**追跡の方法:**
- `store.Add` の行にブレークポイントをかけ、毎回 `e` にオブジェクト ID を作ってみてください。別の通知のはずの `e` が全部同じ `$1` です。
- デバッグ → ウィンドウ → **タスク(Tasks)** / **並列スタック**のタスク表示: 最初のフレームのあと `OnProgressAsync` のタスクがいくつも同じ `LoadAsync` を待っています。
- `Publish` に `{questId} {amount}` のトレースポイントを、`store.Add` に `{e}` のトレースポイントをかけます。出力ウィンドウで順序を比べると、送った通知と処理した通知が違います。

**修正:** ハンドラーで `await` の**前に**必要な値をローカル変数にコピーします。`C2/Achievements.cs` の `AchievementTracker.OnProgressAsync` を次のものに置き換えます。`await` のあとでは `e` を読み直しません。ほかのクラスはそのままです。
```csharp
    async Task OnProgressAsync(QuestProgress e)
    {
        // e はバスが使い回し続けるオブジェクト。await の前に必要な値をコピーしておく
        string questId = e.QuestId;
        int amount = e.Amount;

        if (!store.Loaded)
            await store.LoadAsync();

        int now = store.Add(questId, amount);
        _handled += 1;
        if (goals.TryGetValue(questId, out int goal) && now >= goal && store.Unlock(questId))
            Console.WriteLine(Text.L($"  [업적] {questId} 달성! ({now}/{goal})", $"  [実績] {questId} 達成! ({now}/{goal})"));
    }
```
別の方法: バスが通知ごとに新しいオブジェクトを作ります(`QuestEventBus.Publish` が `_current` を書き換える代わりに `new QuestProgress { ... }` を渡します。不変の `record` にすればもっとよいです)。

使い回したいなら、そのオブジェクトがハンドラーの外に漏れないという契約(同期ハンドラーだけ)が必要です。

**教訓:** `await` はメソッドを二つに切ります。後ろの半分は**あとで**実行されるので、参照で受けた引数がそのときも同じ値だという保証はありません。割り当てを減らす最適化(12部)をすると「いつまでこのオブジェクトを使ってよいか」という寿命の規則が生まれますが、その規則は `async` とうまく噛み合いません。`ArrayPool`(ラボ 79)と同じ種類のバグです。

**カテゴリ:** 非同期の境界 + オブジェクトの再利用 (8部非同期、12部パフォーマンス最適化の副作用)

## チャレンジ C-3 — キャッシュが返したリストを書き換えた画面

**広がる経路:**
```
1. RankingService.Top はキャッシュした List<RankEntry> をそのまま(同じインスタンス)返す
2. FriendRankingScreen が RankFilters.Where でフレンドだけ残す。Where は RemoveAll で受けたリストを直接直す
3. RankFilters.Renumber が残った項目の Rank を 1 から付け直す。RankEntry オブジェクトもキャッシュと共有される
4. 次に Top を呼ぶ画面は、フレンドだけ残り、順位が変わったリストを受け取る
5. 週間報酬のメールはフレンドにだけ、見当違いの順位の報酬で出る。ギルドランキング画面もギルドメンバーの一部が消え、順位が間違う
```

**偽の容疑者:**
- **メールを送る前にキャッシュを空にする**(`Invalidate`)、またはメールだけ DB から直接読む: 基本ケース(フレンド画面 → メール)は通ります。しかしフレンド画面の次にギルド画面を開く隠しケースで失敗します。
- **`Take(Winners)` と `RewardFor` の境界値**: 境界は合っています。
- **`Top` でリストだけコピー**(`return [.. ranks];`): 項目が消えるのは防ぎますが、共有された `RankEntry` の `Rank` が変わるので報酬の等級が依然として間違います。

**追跡の方法:**
- `Top` の `return ranks;` と各画面で受けたリストにオブジェクト ID を作ると、すべて同じ `$1` です。
- ウォッチに `$1.Count` を入れてフレンド画面を通ってみてください。24 が 5 に変わります。誰が変えたかは `List<T>.RemoveAll` に関数ブレークポイントをかければすぐ出ます(条件: `this == $1`)。
- データブレークポイント: ある `RankEntry` の `Rank` プロパティの後ろのフィールドをウォッチで選び、「値が変わったら中断」をかけます(.NET Core 3 以上、VS 2019 16.x 以上)。

**修正:** キャッシュの結果を誰も直せなくする。三つを一緒に適用しないとコンパイルできない(`Rank` を `init` に変えた瞬間に `RankFilters` がコンパイルできなくなるので消す必要があり、そうするとフレンド画面も書き直すことになる)。
1. `C3/Ranking.cs`: `RankEntry.Rank` を `init` にし、`Top` は `IReadOnlyList<RankEntry>` を返して、順位はソートした順に**新しい項目を作りながら**決める。`RankEntry` クラスを丸ごと次のものに置き換える。
```csharp
public sealed class RankEntry(string name, string guild, int score)
{
    public string Name { get; } = name;
    public string Guild { get; } = guild;
    public int Score { get; } = score;
    public int Rank { get; init; }   // 作るときだけ決める

    public override string ToString() => $"{Rank}{Text.L("위", "位")} {Name} ({Score})";
}
```
   続けて、同じファイルの `RankingService` クラスを丸ごと次のものに置き換える。間にある `RankingDatabase` はそのまま。
```csharp
public sealed class RankingService(RankingDatabase db)
{
    readonly Dictionary<string, IReadOnlyList<RankEntry>> _cache = [];

    public IReadOnlyList<RankEntry> Top(string region)
    {
        if (!_cache.TryGetValue(region, out IReadOnlyList<RankEntry>? ranks))
        {
            List<RankEntry> rows = db.QuerySeason(region);
            rows.Sort((a, b) => b.Score != a.Score ? b.Score.CompareTo(a.Score) : string.CompareOrdinal(a.Name, b.Name));
            ranks = rows.Select((r, i) => new RankEntry(r.Name, r.Guild, r.Score) { Rank = i + 1 }).ToList();
            _cache[region] = ranks;
        }
        return ranks;
    }

    public void Invalidate(string region) => _cache.Remove(region);
}
```
2. `C3/Ranking.cs`: `RankFilters` クラスをそのすぐ上の説明コメント(二行)ごと丸ごと消す。「受けたものを直してそのまま返す」ヘルパーは名前が嘘をつく(LINQ の `Where` と同じ名前なのに動作が逆だ)。
3. `C3/Screens.cs`: フレンド画面はキャッシュに触れずに新しいリストを作る。`FriendRankingScreen` クラスを丸ごと次のものに置き換える。`GuildRankingScreen` と `WeeklyRewardMailer` は `IReadOnlyList` でもそのままコンパイルできる。
```csharp
public sealed class FriendRankingScreen(RankingService service)
{
    public List<string> Show(string region, IReadOnlySet<string> friends) =>
        service.Top(region).Where(r => friends.Contains(r.Name)).Select((r, i) => $"{i + 1}. {r.Name}").ToList();
}
```
**教訓:** キャッシュは同じオブジェクトを複数の人に配る仕組みです。配ったオブジェクトが変更可能なら、一つの画面のミスが時間と場所を越えて別の画面の症状として現れます。症状が出た画面でキャッシュを空にするのはその画面だけを直します。キャッシュが返すものは不変にするか、コピーで渡します (ラボ 19 の浅いコピー、30 のクエリとスナップショット)。

**カテゴリ:** キャッシュ + 共有された変更可能な状態 (2部の浅いコピー、3部のコンテナ)

---

# ブラインド (B)

## チャレンジ B-1 — 待つ前に鳴った Pulse
```
症状:   「地域の読み込み」応答なし。すべて中断するとメインスレッドが Monitor.Wait に、ローダースレッドはすでに終わっている
原因:   ローダーが 300ms 以内に終わって Pulse を先に鳴らす。Monitor はシグナルを覚えないので、あとで Wait した側は永遠に待つ
```
**修正:** 条件変数と一緒にループで待ちます: ローダーが `_done = true` を書いて `Pulse`、待つ側は `while (!_done) Monitor.Wait(_gate);`。`Blind/ChallengeB1.cs` の `RegionLoader` クラスを丸ごと次のものに置き換えます。`PrepareScreen` と `Run` はそのままです。
```csharp
    sealed class RegionLoader
    {
        readonly object _gate = new();
        int _tiles;
        bool _done;   // Pulse は覚えてもらえないので「終わった」を状態として残す

        public void Start(string region, int tileCount)
        {
            var worker = new Thread(() =>
            {
                int tiles = 0;
                for (int i = 0; i < tileCount; i++)
                    tiles++;
                lock (_gate)
                {
                    _tiles = tiles;
                    _done = true;
                    Monitor.Pulse(_gate);
                }
            }) { IsBackground = true, Name = Text.L($"로더 ({region})", $"ローダー ({region})"), Priority = ThreadPriority.Highest };
            worker.Start();
        }

        public int WaitForTiles()
        {
            lock (_gate)
            {
                while (!_done)   // もう終わっていれば待たない
                    Monitor.Wait(_gate);
                return _tiles;
            }
        }
    }
```
もっと簡単には `Task<int>` を返して待つか、`ManualResetEventSlim` を使います。

**偽の修正:** `Monitor.Wait(_gate, 1000)` のように制限時間を与えると、地域一つは1秒で終わって通ります。しかし地域を3〜4個読み込む隠しケースでは監視時間を超えます。

**カテゴリ:** 11部ハング (`Monitor.Wait`/`Pulse` のシグナル取りこぼし)

## チャレンジ B-2 — トルコ語の i
```
症状:   tr-TR、az-Latn-AZ で "/quit".ToUpper() == "/QUİT" (点のある大文字 İ、U+0130)。コマンド表の "/QUIT" と違う
        i のないコマンド(/map、/help)はちゃんと動く
```
**修正:** プログラムが定めた識別子はカルチャと無関係に比較する。`Blind/ChallengeB2.cs` の `Handle` の `string key = input.Trim().Split(' ')[0].ToUpper();` の行を次の一行に置き換える。
```csharp
        string key = input.Trim().Split(' ')[0].ToUpperInvariant();
```
もっとよいのはコマンド表を `new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)` にして大文字小文字を変えないこと。

**カテゴリ:** 2部フロー追跡 (カルチャ依存の文字列処理。ラボ 16 の数値パースと同じ種類)

## チャレンジ B-3 — 衝突するハッシュ
```
測定:   マス一つあたり Equals 70回以上 (期待: 4 以下)
原因:   GetHashCode() => X ^ Y。(1,2) と (2,1)、(3,3) と (0,0) のような数多くのマスが同じハッシュを持つので HashSet が一つのバケットをずらずら比較する。
        X、Y が 0〜127 なら、異なるハッシュは 128 個だけだ
```
**修正:** X と Y をよく混ぜるハッシュを使う。`Blind/ChallengeB3.cs` の `Cell` の `GetHashCode` の行を次の一行に置き換える。
```csharp
        public override int GetHashCode() => HashCode.Combine(X, Y);
```
`Cell` を `readonly record struct` にして、コンパイラが作るハッシュを使ってもよい。

**カテゴリ:** 12部パフォーマンス (`GetHashCode` の品質、ハッシュコレクションの最悪の場合)

## チャレンジ B-4 — 配列の共変性
```
例外:   ArrayTypeMismatchException at Refill
原因:   Recruit は Unit[] を返すが実際には Archer[] だ(配列の共変性)。その配列に Knight を入れた瞬間にランタイムの型チェックが失敗する
```
**修正:** 実際の配列を `Unit[]` にします。`Blind/ChallengeB4.cs` の `Recruit` を次のものに置き換えます(三つの分岐の `.ToArray()` がすべて `.ToArray<Unit>()` になります)。`Refill` と `Run` はそのままです。
```csharp
    static Unit[] Recruit(string kind, int size) => kind switch
    {
        "archer" => Enumerable.Range(1, size).Select(n => new Archer(n)).ToArray<Unit>(),
        "knight" => Enumerable.Range(1, size).Select(n => new Knight(n)).ToArray<Unit>(),
        _ => Enumerable.Range(1, size).Select(n => new Healer(n)).ToArray<Unit>(),
    };
```
`List<Unit>` を使ってもかまいません。`IReadOnlyList<Unit>` のような共変インターフェースで返すと、入れること自体をコンパイラが防ぎます。

**偽の修正:** 例外を捕まえて飛ばすと、部隊が埋まらず戦闘力が足りません。

**カテゴリ:** 1部例外 / 3部コンテナ (配列の共変性)

## チャレンジ B-5 — GC が回収したタイマー
```
症状:   最初の数回は保存するが、最初の GC のあと止まる
原因:   StartAutoSave のローカル変数 timer 以外に Timer をつかむものがない。メソッドが終わると回収の対象になり、
        GC が回るとファイナライザーがタイマーを止める。(System.Threading.Timer はコールバックが生きていても Timer オブジェクトがないと止まる)
```
**修正:** タイマーをローカル変数ではなくフィールドに保存し、終わるとき `Dispose` する。`StartAutoSave` の中で `using var timer` に変えるのは逆方向だ(メソッドが終わった瞬間にタイマーが止まる)。`Blind/ChallengeB5.cs` で二か所を変える。`PlayFrame` はそのまま。

1. `static int _saves;` の行と `StartAutoSave` を(二つは隣り合っている)次のものに置き換える。
```csharp
    static int _saves;
    static Timer? _autoSave;   // フィールドがつかんでいる間は GC の対象にならない

    static void StartAutoSave(int intervalMs)
    {
        _autoSave = new Timer(_ => Interlocked.Increment(ref _saves), null, intervalMs, intervalMs);
        Console.WriteLine(Text.L($"  자동 저장 시작 ({intervalMs}ms마다)", $"  オートセーブ開始 ({intervalMs}ms ごと)"));
    }
```
2. `Run` を丸ごと次のものに置き換える(最後の二行でタイマーを止めて手放す以外は元と同じ)。
```csharp
    public static void Run()
    {
        _saves = 0;
        int interval = Pick(20, 15, 40);
        int wanted = Pick(10, 8, 12);

        StartAutoSave(interval);

        int frames = 0;
        if (FinishesWithin(Text.L($"자동 저장 {wanted}번", $"オートセーブ {wanted} 回"), () =>
            {
                while (Volatile.Read(ref _saves) < wanted)
                    PlayFrame(++frames);
            }))
        {
            int saves = Volatile.Read(ref _saves);
            Console.WriteLine(Text.L($"  {frames}프레임 동안 저장 {saves}번", $"  {frames} フレームの間に保存 {saves} 回"));
            Expect(Text.L($"저장 횟수가 {wanted}번 이상", $"保存回数が {wanted} 回以上"), true, saves >= wanted);
        }
        else
            Console.WriteLine(Text.L($"  (멈춘 순간 {frames}프레임, 저장 {Volatile.Read(ref _saves)}번)", $"  (止まった時点で {frames} フレーム、保存 {Volatile.Read(ref _saves)} 回)"));
        _autoSave?.Dispose();   // 終わったら止めて手放す
        _autoSave = null;
    }
```
(`PeriodicTimer` と `async` ループもよい。)

**カテゴリ:** 9部リソースとメモリ (GC とオブジェクトの寿命)

## チャレンジ B-6 — 静的初期化の循環
```
値:     ハードモードなのに開始ゴールド 300(ノーマル 100 × 3)、ポーション 10 ゴールド(ノーマルの値)
原因:   Balance の静的初期化が StartingGold を計算しようと Economy を初期化する。Economy は Balance.Mode を読むが、
        Balance はまだ初期化中なので Mode は既定値 Normal(0) だ。循環があるとランタイムは例外なしに「まだ途中」の値を見せる
```
**修正:** 循環を断つ。`Blind/ChallengeB6.cs` の `Balance` の `public static readonly int StartingGold = Economy.BaseGold * 3;` の行を次の一行に置き換える(フィールドの初期化ではなく、読むたびに計算するプロパティにする)。`Economy` と `Run` はそのまま。
```csharp
        public static int StartingGold => Economy.BaseGold * 3;
```
難易度を別の設定オブジェクトに出してコンストラクターで渡す方法もある。

**偽の修正:** ハードモードの数字(60、25)を埋め込むと、ナイトメアの隠しケースで間違います。

**カテゴリ:** 2部フロー追跡 (静的フィールドの初期化順)

## チャレンジ B-7 — Parallel.ForEach に async ラムダ
```
値:     「発送完了」の時点で届いたメール 0
原因:   Parallel.ForEach は Action<T> を受け取る。async ラムダは async void になり、最初の await ですぐ「終わった」と戻る。
        ForEach は何も待たずに終わり、メールはそのあと届く。例外が出るとプロセスが死ぬ(ラボ 62)
```
**修正:** `Action<T>` ではなく待てる API で送る。`Run` は採点ツールが呼ぶ同期メソッドなので最後まで待つ必要がある(コンソールアプリには `SynchronizationContext` がないので、ここで `GetAwaiter().GetResult()` でブロックしてもラボ 63 のようなデッドロックは起きない)。`Blind/ChallengeB7.cs` で二か所を変える。`MailServer` はそのまま。

1. `SendCompensation` を、`async Task` を返す次の `SendCompensationAsync` に置き換える(`Parallel.ForEachAsync` を `await` する)。
```csharp
    static async Task SendCompensationAsync(MailServer server, IEnumerable<string> players)
    {
        await Parallel.ForEachAsync(players, async (player, ct) => await server.SendAsync(player));
        Console.WriteLine(Text.L("  보상 우편 발송 완료", "  補償の郵便 発送完了"));
    }
```
2. `Run` を丸ごと次のものに置き換える(呼び出しの行だけが変わる)。
```csharp
    public static void Run()
    {
        int count = Pick(12, 8, 40);
        string[] players = [.. Enumerable.Range(1, count).Select(i => $"player{i:00}")];
        var server = new MailServer(Pick(100, 600, 1000));

        SendCompensationAsync(server, players).GetAwaiter().GetResult();
        Expect(Text.L("'발송 완료' 시점에 도착한 우편", "「発送完了」の時点で届いた郵便"), count, server.Delivered);
    }
```
または 1. で `await Task.WhenAll(players.Select(server.SendAsync));` で全部起動して一度に待つ。

**偽の修正:** `Thread.Sleep(500)` を入れると、往復がもっと長くかかる隠しケースでまた足りません。

**カテゴリ:** 8部非同期 (`async void`、待たなかった作業)

## チャレンジ B-8 — メソッドの途中で回収された this
```
症状:   Release でだけチェックサムが間違う。Debug は通る
原因:   FillAndChecksum は最初に _block で Span を得たら this をそれ以上使わない。最適化されたコードで JIT は this を
        「すでに死んだ変数」と見なし、呼んだ側も一時オブジェクト(new Texture())をつかんでいない。だからループの途中で GC が Texture を回収し、
        ファイナライザーがブロックをプールに返し、ストリーミングがそのブロックを受けて 0xEE で塗る
```
**修正:** ネイティブ資源を使っている間、所有者を生かしておきます。`Blind/ChallengeB8.cs` の `Texture.FillAndChecksum` を次のものに置き換えます。チェックサムを数え終えたあとに `GC.KeepAlive(this);` を呼ぶと、JIT はその行まで `this` が生きているものと見なします。属性(`[MethodImpl]`)と残りのコードはそのままです。
```csharp
        // ホットパスなので最初から最適化してコンパイルする
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public uint FillAndChecksum(byte color, Action everyFewRows)
        {
            Span<byte> pixels = NativePool.Pixels(_block);
            for (int row = 0; row < Rows; row++)
            {
                pixels.Slice(row * RowBytes, RowBytes).Fill(color);
                if (row % 8 == 7)
                    everyFewRows();
            }
            uint sum = 0;
            foreach (byte b in pixels)
                sum = sum * 31 + b;
            GC.KeepAlive(this);   // ここまで this(ブロックの持ち主)を生かしておく。ファイナライザーが先にブロックを返せない
            return sum;
        }
```
別の方法:
- もっと根本的にはファイナライザーに頼りません。`IDisposable` にして `using var tex = new Texture();` のように明示的に返します。ネイティブハンドルは `SafeHandle` で包みます (ラボ 74)

**カテゴリ:** 10部 unsafe とネイティブメモリ (`GC.KeepAlive`、ファイナライザーとネイティブ資源)。Release でだけ出る点はラボ 88 と同じだ
