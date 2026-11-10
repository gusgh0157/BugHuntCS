[한국어](SOLUTIONS.md)

# 解答 (ネタバレ!)

# 1部: 例外とスタックトレース

## Lab 1 — null 参照解除、`!` で抑え込んだ nullable 警告
```
止まった関数:  PrintPlayer           おかしな値: player == null
値の出どころ:  ShowProfile ← FindPlayer が見つけられず null を返す
本当の原因:  OpenFriendList で "Doran" を "Dorna" と打ち間違い
```
**修正:** 打ち間違いを直し、`!` を消して null を守る。`Lab01_Profile.cs` の `ShowProfile` と `OpenFriendList` を(二つは隣り合っている)次のものに置き換える。`OpenFriendList` は `"Dorna"` を `"Doran"` に直しただけだ。`FindPlayer`、`PrintPlayer`、`Run` はそのまま。
```csharp
    static void ShowProfile(string name)
    {
        Player? player = FindPlayer(name);
        if (player is null)
        {
            Console.WriteLine(Text.L($"  {name}: 없는 플레이어", $"  {name}: 存在しないプレイヤー"));
            return;
        }
        PrintPlayer(player);
    }

    static void OpenFriendList()
    {
        ShowProfile("Kael");
        ShowProfile("Mira");
        ShowProfile("Doran");
    }
```
**教訓:** 死んだ所(`PrintPlayer`)に間違いはない。`!` はチェックではなく**警告を切るスイッチ**だ。コンパイラは `FindPlayer` が `Player?` を返すことを知っていて警告(CS8600)を出そうとしていた。

## Lab 2 — 大文字小文字だけ違う Dictionary のキー
```
止まった関数:  [外部コード] Dictionary.get_Item → 最初のマイコード: PriceOf(itemId = "Mana_Potion")
値の出どころ:  CartTotal ← LoadCart ← SavedCart (1.0 クライアントが保存したデータ)
本当の原因:  Prices は既定の比較子(大文字小文字を区別)なのに、保存されたキーは "Mana_Potion"
```
**修正:** 価格表を大文字小文字を区別しない比較子で作る。`Lab02_Shop.cs` の `Prices` フィールドの宣言(初期化子の `};` まで)を次のものに置き換える。ほかのメソッドはそのまま。
```csharp
    static readonly Dictionary<string, int> Prices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["hp_potion"] = 50,
        ["mana_potion"] = 70,
        ["iron_sword"] = 1200,
        ["leather_boots"] = 300,
    };
```
別の方法: ロードするときに `ToLowerInvariant()` でキーを正規化する。ないアイテムまで備えるなら、`PriceOf` で `TryGetValue` を使って探し、警告を残す。
**教訓:** `KeyNotFoundException` のメッセージには見つからなかった**キーがそのまま**出る。メッセージを最後まで読もう。すでに配布されたデータは直せないので、受け取る側が寛容でなければならない。

## Lab 3 — 列挙中のコレクション変更
```
止まった所:    CastExplosion の foreach (外部コード: List<T>.Enumerator.MoveNext)
おかしな値:  _enemies が巡回の途中で減る
本当の犯人:  Despawn の _enemies.Remove — 例外が出るときにはすでに戻っていて呼び出し履歴にない
```
**修正:** コピーを巡回する。`Lab03_Enemies.cs` の `CastExplosion` の `foreach (Enemy enemy in _enemies)` の行を次の一行に置き換える。ループの本体はそのまま。
```csharp
            foreach (Enemy enemy in _enemies.ToList())
```
別の方法:
- 巡回中は死んだ敵を集めておき、ループが終わったあとで `_enemies.RemoveAll(e => e.Hp <= 0)`
- 後ろから `for (int i = _enemies.Count - 1; i >= 0; i--)`

**教訓:** 例外は変更した瞬間ではなく**次の `MoveNext`** で出る。呼び出し履歴は「今」だけを見せる。犯人を捕まえるには変更する行にブレークポイントを置く。

## Lab 4 — ボックス化された long を (int) でボックス化解除
```
止まった関数:  CreateParty
おかしな値:  config["maxPartySize"].GetType() == System.Int64
本当の原因:  ParseConfig が数字を long でボックス化して入れ、(int)obj は「ボックス化解除」なので正確に int のときだけ成功する
```
**修正:** ボックス化解除ではなく数値変換をする。`Lab04_Config.cs` の `CreateParty` を次のものに置き換える。`ParseConfig` と `Run` はそのまま。
```csharp
    static Party CreateParty(Dictionary<string, object> config)
    {
        int maxSize = Convert.ToInt32(config["maxPartySize"]);
        return new Party(maxSize);
    }
```
`(int)(long)config["maxPartySize"]` でもよい。もっとよいのは設定を最初から型のあるクラスにパースすること。
**教訓:** `(int)` のキャストは状況によって**数値変換**にも**ボックス化解除**にもなる。`object` から取り出すときは後者だ。

## Lab 5 — 自分自身を呼ぶプロパティの setter
```
症状:       Stack overflow. Repeat N times: at BugHunt.Lab05+Wallet.set_Gold(Int32)
呼び出し履歴:  set_Gold が数千個
本当の原因:  set => Gold = ...   (バッキングフィールド _gold ではなくプロパティ自身に代入)
```
**修正:** バッキングフィールドに代入する。`Lab05_Wallet.cs` の `Gold` プロパティの `set => Gold = ...` の行を次の一行に置き換える。
```csharp
            set => _gold = Math.Clamp(value, 0, MaxGold);
```
**教訓:** StackOverflow は**捕まえられず**プロセスが終わる。デバッガーなしで実行すると、ランタイムが繰り返されるフレームを `Repeat N times` で畳んで見せる。スタックが一つの関数で埋まれば、再帰が終わっていないということ。

## Lab 6 — AggregateException の中の FormatException
```
止まった所:    Run の Parallel.ForEach (処理されない System.AggregateException)
内部例外:  FormatException: The input string '??' was not in a correct format. ← Parse("swamp.loot")
本当の原因:  catch (FormatException) は AggregateException と型が違うので捕まらない。
           その上、失敗後に残ったファイルを処理しないこともあるので「ファイル一つだけ飛ばす」ができていない
```
**修正:** ファイル一つ単位で捕まえる。`Lab06_LootFiles.cs` の `Run` の中で、`Parallel.ForEach` を囲んでいる `try` の行から `catch (FormatException ex)` ブロックの閉じ括弧 `}` まで(外側の `try`/`catch` 全体。もう何も捕まえない)を次のものに置き換える。ラムダの中で `skipped++` をそのまま使うとスレッド競合が起きるので `Interlocked.Increment` で数える(ラボ 66 参照)。`Run` のほかの行はそのまま。
```csharp
        Parallel.ForEach(Files, file =>
        {
            try { loaded.Add(Parse(file.Key, file.Value)); }
            catch (FormatException ex)
            {
                Console.WriteLine(Text.L($"  고장 난 파일 건너뜀: {file.Key} ({ex.Message})", $"  壊れたファイルを飛ばしました: {file.Key} ({ex.Message})"));
                Interlocked.Increment(ref skipped);
            }
        });
```
さらに `Parse` で `int.TryParse` を使って例外なしに処理することもできる。
**教訓:** 並列 API(`Parallel`、`Task.Wait`、`Task.Result`)は例外を **AggregateException で包む**。例外設定ウィンドウの first chance 中断で、本当の throw 地点(作業スレッド)をすぐ見られる。

## Lab 7 — `throw ex;` がスタックトレースを上書きする
```
レポートの一番上:   at BugHunt.Lab07.LoadSlot(Int32 index)   ← throw ex; の行
実際の throw:     int.Parse ← ParseItem("potion:two") ← ParseInventory ← ParseSlot ← LoadSlot
本当の原因:      throw ex; は例外を「新しく投げた」ようにスタックトレースをこの行から始め直す
隠した警告:      BugHunt.csproj の <NoWarn>CA2200</NoWarn>
```
**修正:** `throw ex;` を `throw;` に変える。`Lab07_SaveLoad.cs` の `LoadSlot` の `catch (Exception ex)` ブロック(閉じ括弧まで)を次のものに置き換える。そのあと `BugHunt.csproj` の `<NoWarn>$(NoWarn);CA2200</NoWarn>` の行を消す。順序が大事だ: このリポジトリは警告をエラーとして扱うので(`TreatWarningsAsErrors`)、`throw ex;` が残ったまま先に `NoWarn` を消すとビルドが失敗する。`throw ex;` を使うラボはこのラボだけなので、直したあとなら `NoWarn` を消してもビルドはきれいなままだ。(`NoWarn` をそのままにしても採点は通るが、それでは次に誰かがまた `throw ex;` を書いてもコンパイラが知らせてくれない。)
```csharp
        catch (Exception ex)
        {
            Console.WriteLine(Text.L($"  슬롯 {index + 1} 손상: {ex.GetType().Name}", $"  スロット {index + 1} 破損: {ex.GetType().Name}"));
            throw;   // 元のスタックトレースをそのままにして投げ直す
        }
```
情報を足したいなら `throw new SaveLoadException(Text.L($"슬롯 {index + 1}", $"スロット {index + 1}"), ex);` のように**内部例外で包む**(このラボにはそのような例外型がないので自分で作る必要がある)。
**教訓:** スタックトレースは例外が**最後に throw された所**から記録される。投げ直すときは `throw;`。警告を切ると、その警告が守っていたバグも一緒に隠れる。

---

## Lab 8 — `ToString` が `$"{this}"` で自分を呼び無限再帰
```
呼び出し履歴:  DamageLabel.ToString → DamageLabel.ToString → … (数千段)
本当の原因:  $"{this} {Text.L("피해", "ダメージ")}" の {this} は this.ToString() を呼ぶ。それが今実行中の ToString。
           無限再帰でスタックを使い切ると StackOverflowException でプロセスが即死する。
```
**修正:** `ToString` でそのオブジェクト自身を補間せず、フィールドを直接書きます。`Lab08_DamageLabel.cs` の `DamageLabel` の `ToString` の行を次の一行に置き換えます。
```csharp
        public override string ToString() => $"{Amount} {Text.L("피해", "ダメージ")}";
```
**教訓:** `ToString`、`GetHashCode`、`Equals`、演算子オーバーロード、プロパティの `get` の中で**自分自身を再び呼ばない**よう気をつけます。よくある形: `ToString` で `$"{this}"`、`Equals` で `this == other`(→ `==` 演算子がまた `Equals` を呼ぶ)、`get` で同じプロパティへの代入。ラボ 5(財布)の `set => Gold = ...` も同じ根です。症状(プログラムが静かに消える)が同じなら呼び出し履歴から見てください。

---

# 2部: フロー追跡

## Lab 9 — ループ変数のキャプチャ
```
期待: CastSlot(1)、(2)、(3)        実際: CastSlot(4) を三回
最初にずれた所: ラムダは slot の「値」ではなく「変数」をキャプチャする。
              for の slot はループ全体で一つの変数(DisplayClass オブジェクト一つ)で、ループが終わると 4
```
**修正:** ループの中で新しい変数にコピーする。`Lab09_Hotkeys.cs` の `BindHotkeys` の `for` ループの中の `hotkeys[key] = () => CastSlot(slot);` の一行を次の二行に置き換える。
```csharp
            int thisSlot = slot;
            hotkeys[key] = () => CastSlot(thisSlot);
```
**教訓:** C# のラムダは変数を**参照で**キャプチャする。`foreach` の変数は C# 5 から周回ごとに新しい変数で安全だが、`for` の変数は違う。ローカルウィンドウでラムダの `this` を展開すると `<>c__DisplayClass` が見え、オブジェクト ID で三つのラムダが同じキャプチャオブジェクトを共有しているのを確認できる。

## Lab 10 — LINQ の遅延実行が二回列挙される
```
期待: Send 3回          実際: 6回
最初にずれた所: rewards.Count() で3回、foreach でまた3回
本当の原因:     Select(...) は結果のリストではなく「クエリ」を返す。列挙するたびに Send がまた実行される
```
**修正:** 副作用のあるクエリは即座に実体化する。`Lab10_Ranking.cs` の `Run` の `IEnumerable<Mail> rewards = Ranking` から `.Select(e => mailbox.Send(e.Name, 500));` までの文(4行)を次のものに置き換える。`Run` のほかの行はそのまま。
```csharp
        IEnumerable<Mail> rewards = Ranking
            .OrderByDescending(e => e.Score)
            .Take(3)
            .Select(e => mailbox.Send(e.Name, 500))
            .ToList();
```
もっとよいのは Select の中で副作用を出さず、対象のリストを作ってから `foreach` で送ること。
**教訓:** `IEnumerable<T>` の変数は**リストではなく作り方**かもしれない。デバッガーの「結果ビュー」も列挙を実行して状態を変える。

## Lab 11 — readonly struct フィールドの防御的コピー
```
TryUse の中:   this.Current 100 → 70 (期待どおり)
Sprint に出ると: _stamina.Current 100
本当の原因:   _stamina は readonly フィールドで Stamina は変更可能な struct。
            コンパイラは readonly フィールドを守るためメソッド呼び出しのたびにコピーを作り、そのコピーを変える
```
**修正:** `readonly` を消して、コピーではなくフィールドそのものを変えさせる。`Lab11_Stamina.cs` の `Runner` の `readonly Stamina _stamina = new(100);` の行を次の一行に置き換える。
```csharp
        Stamina _stamina = new(100);
```
別の方法: `Stamina` を `class` に変える / struct を不変にして `_stamina = _stamina.Use(30)` のように新しい値を受け取る。
**教訓:** 「変更可能な struct」はコピーの罠が多い。最初から `readonly struct` で宣言すれば、フィールドを変えるメソッドそのものが**コンパイルエラー**になってこの手のミスを防ぐ。

## Lab 12 — ボックス化された値の `!=` は参照比較
```
条件付きブレークポイント (key == "fullscreen"):
    before[key]          true (object)
    after[key]           true (object)
    before[key] != after[key]                 true  ← どちらも true なのに「違う」
    before[key].Equals(after[key])            true
    ReferenceEquals(before[key], after[key])  false
本当の原因:   変数の型が object なので ==/!= は値ではなく「同じ箱か」を比較する。Snapshot ごとに新しくボックス化される
```
**修正:** 値で比べる。`Lab12_SettingsDiff.cs` の `ChangedKeys` の `if (before[key] != after[key])` の行を次の一行に置き換える(静的な `object.Equals` は null も安全に扱い、値の `Equals` を呼ぶ)。
```csharp
            if (!Equals(before[key], after[key]))
```
**language が合っていた理由:** 二つのスナップショットが**同じ string オブジェクト**(`settings.Language`)を入れていたので、参照比較もたまたま同じになった。設定をファイルから読み直していれば、内容が同じ別の string オブジェクトになって「変わった」と出ただろう。
**教訓:** `==` の意味は**変数のコンパイル時の型**で決まる。`object` で持っていれば int も bool も参照比較だ。

## Lab 13 — float の等値比較
```
条件付きブレークポイント (XpBar > 0.95f):
    XpBar              1.0000001
    XpBar == 1.0f      false
    (double)XpBar      1.0000001192092896
本当の原因:  0.1f は2進数で正確でないので十回足しても 1.0f にならない。画面の P0 書式は丸めて「100%」
```
**修正:** 最低でも範囲で比べる。`Lab13_XpBar.cs` の `GainXp` の `if (XpBar == 1.0f)` の行を次の一行に置き換える。
```csharp
            if (XpBar >= 1.0f)
```
根本的には経験値を**整数**で持ち(`int xp`、`int xpToNext`)、バーの比率は表示するときだけ計算する。
**教訓:** 浮動小数点は `==` で比較しない。画面に見える数字は書式が丸めた値かもしれない。

## Lab 14 — 中間計算の int オーバーフロー
```
条件付きブレークポイント (damage == 30000000):
    damage * 100           -1294967296
    checked(damage * 100)  System.OverflowException
    int.MaxValue           2147483647
本当の原因:  30,000,000 × 100 = 3,000,000,000 > int.MaxValue。C# は既定が unchecked なので黙って一周する
```
**修正:** 中間値を `long` で計算する。`Lab14_BossContribution.cs` の `ContributionPercent` の行を次の一行に置き換える。
```csharp
    static int ContributionPercent(int damage, int totalDamage) => (int)((long)damage * 100 / totalDamage);
```
貢献度を `double` で計算するのも手だ。
**教訓:** 結果は int に入っても**中間値があふれうる。** Debug ビルドで `<CheckForOverflowUnderflow>true</CheckForOverflowUnderflow>` を入れておくと、こういう所ですぐ例外で止まる。

## Lab 15 — string の不変性、Replace の結果を捨てる
```
Replace の行を F10:  nickname がそのまま。自動変数ウィンドウに "String.Replace の戻り値  ***Slayer "" が見えて消えた
本当の原因:  string は不変。Replace は新しい文字列を返すだけで、その戻り値をどこにも保存していない
           (Trim は return nickname.Trim(); で戻り値を使ったのでちゃんと動く)
```
**修正:** 戻り値を保存する。`Lab15_NicknameFilter.cs` の `Sanitize` の `nickname.Replace(word, "***", StringComparison.OrdinalIgnoreCase);` の行を次の一行に置き換える。
```csharp
                nickname = nickname.Replace(word, "***", StringComparison.OrdinalIgnoreCase);
```
**教訓:** `string`、`DateTime`、`ImmutableList` のような不変型のメソッドは**新しい値を返す。** CA1806 を有効にすると戻り値を捨てる呼び出しを警告で捕まえる。

## Lab 16 — 現在のカルチャで書き InvariantCulture で読む
```
de-DE のとき file:  "moveSpeed=1,5\nmouseSensitivity=0,75"
NumberDecimalSeparator (de-DE)  ","
double.Parse("1,5", InvariantCulture)   15   ← Invariant では "," は桁区切り記号
本当の原因:  $"{double}" の補間は CurrentCulture で数字を書く。読む側だけ InvariantCulture
```
**修正:** 書く側も固定します。`Lab16_SettingsFile.cs` の `Save` を次のものに置き換えます。`using System.Globalization;` はファイルにすでにあり、`Load` と `SaveAndReload` はそのままです。
```csharp
    static string Save(Options o) =>
        string.Create(CultureInfo.InvariantCulture, $"moveSpeed={o.MoveSpeed}\nmouseSensitivity={o.MouseSensitivity}");
```
`FormattableString.Invariant($"...")` で包んだり、値ごとに `o.MoveSpeed.ToString(CultureInfo.InvariantCulture)` を使ったりしても同じです。
**教訓:** ファイル・ネットワーク・セーブのように**機械が読むテキスト**は書きも読みも InvariantCulture で。画面に見せるときだけ CurrentCulture。韓国語・英語の PC では再現されないので「自分の PC では動く」バグになる。CA1305 が捕まえてくれる。

## Lab 17 — DateTime の引き算は Kind を無視する
```
EndsAtUtc       2026-11-03 15:00  Kind=Utc
clock.Now       2026-11-03 21:00  Kind=Unspecified (ソウル時刻)
clock.UtcNow    2026-11-03 12:00  Kind=Utc
EndsAtUtc - clock.Now     -06:00:00
EndsAtUtc - clock.UtcNow  03:00:00
本当の原因:  見せる用の現地時刻で計算している。DateTime どうしの引き算は Kind とタイムゾーンを見ず、数字だけを引く
```
**修正:** UTC どうしで計算する。`Lab17_EventTimer.cs` の `LimitedEvent` の `Remaining` の行を次の一行に置き換える。
```csharp
        public TimeSpan Remaining(PlayerClock clock) => EndsAtUtc - clock.UtcNow;
```
タイムゾーンが混ざるコードなら `DateTimeOffset` を使えば引き算がオフセットを反映する。
**教訓:** 計算は UTC どうしで、変換は**見せる直前にだけ**。ウォッチで `Kind` を展開するとすぐ見える。

## Lab 18 — Equals と GetHashCode が互いに違う規則
```
Equals のブレークポイント:  "kael" を入れるとき引っかからない (HashSet が Equals まで行かない)
ウォッチ:
    new PlayerName("Kael").Equals(new PlayerName("kael"))   true
    new PlayerName("Kael").GetHashCode()   12345...
    new PlayerName("kael").GetHashCode()   -6789...   ← 違う
本当の原因:  Equals は大文字小文字を無視、GetHashCode は大文字小文字を区別(string.GetHashCode)
```
**修正:** ハッシュも大文字小文字を無視する。`Lab18_FriendList.cs` の `PlayerName` の `GetHashCode` の行を次の一行に置き換える。
```csharp
        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
```
**教訓:** `Equals` が true なら **GetHashCode も必ず同じでなければならない。** HashSet/Dictionary はハッシュでマスをまず選び、そのマスの中だけで Equals を呼ぶ。**引っかからないブレークポイント**も重要な手がかりだ。

## Lab 19 — record `with` は浅いコピー
```
オブジェクト ID:   basePreset.Skills {$1}、custom.Skills {$1}  ← 同じ List
ReferenceEquals(basePreset, custom)                false (レコードは新しく作られる)
ReferenceEquals(basePreset.Skills, custom.Skills)  true  (List はアドレスだけコピーされる)
```
**修正:** 変えるコレクションは一緒にコピーする。`Lab19_Presets.cs` の `Customize` の `CharacterPreset custom = basePreset with { Name = name };` の行を次の一行に置き換える。
```csharp
        CharacterPreset custom = basePreset with { Name = name, Skills = [.. basePreset.Skills] };
```
根本的にはレコードに `List<T>` ではなく `IReadOnlyList<T>` や `ImmutableList<T>` を置き、コピーを変えるには新しいコレクションを作るしかないようにする。
**教訓:** `with` は「新しいレコード + プロパティの値をそのままコピー」だ。参照型のプロパティは**共有**される。

## Lab 20 — `x = x++;` は x をそのままにする
```
OnHit の最初の行の前後:   _combo 0 → 0、0 → 0 ...   Best 0
本当の原因:  _combo = _combo++;  (1) 値 0 を読み (2) _combo を 1 に上げたあと (3) 式の値 0 を再び代入
```
**修正:** 代入しない。`Lab20_Combo.cs` の `OnHit` の `_combo = _combo++;` の行を次の一行に置き換える(`_combo += 1;`、`++_combo;` でもよい)。
```csharp
            _combo++;
```
**教訓:** C# は被演算子を**左から右へ**評価し、後置 `++` の値は「上げる前の値」と定めてある。だから `x = x++;` はいつも x をそのままにする。C++ のようにコンパイラごとに変わる未規定動作ではなく、定義どおりに間違ったコードだ。

## Lab 21 — 後置 `++` が上げる前の値を返す
```
読み込んだ名簿:  1 Kael、2 Mira、5 Doran      _lastId = 5 (「今まで使った一番大きい番号」)
Recruit("Lia"):  NextId() → 5 (返したあと _lastId = 6)   ← Doran と重なる
Whisper(5) → リストで先に見つかった Doran に行く
本当の原因:  NextId() => _lastId++;  後置 ++ は上げる前の値を返す
```
**修正:** 前置 `++` で上げたあとの値を返す。`Lab21_MemberId.cs` の `NextId` の行を次の一行に置き換える。
```csharp
        int NextId() => ++_lastId;
```
`{ _lastId += 1; return _lastId; }` でもよい。コンストラクターで `_lastId = Max + 1` に変えるなら、変数の意味も「次に使う番号」に変え、名前もそれに合わせる。
**偽の修正:** ささやきを `Last(m => m.Id == id)` で探させると歓迎のあいさつは正しく行くが、番号は依然として重なる。基本ケースの番号(6、7)を埋め込むと隠しケース(別の番号、別の人数)で間違う。
**教訓:** 後置 `x++` の値は上げる前、前置 `++x` の値は上げたあと。`if (hits++ == 3)` も同じ罠で、四回目ではなく五回目の打撃でやっと真になる。値を使う所では増減を別の一行に分ければ迷うことがない。

## Lab 22 — 条件の中の `--` と byte のあふれ
```
TryDrink (Potions == 0):  if (Potions-- == 0)  → 比較は真、しかし減少も起きる: 0 - 1 = 255 (byte)
次の押下から:  255、254、... 飲む   ← ポーションが252個生まれた
本当の原因:  チェックと減少を一つの式に混ぜた。byte は unchecked なので 0 の下に下がると黙って 255
```
**修正:** チェックと減少を分ける。`Lab22_Potions.cs` の `Bag.TryDrink` を次のものに置き換える。
```csharp
        public bool TryDrink()
        {
            if (Potions == 0)
                return false;
            Potions--;
            return true;
        }
```
**教訓:** 条件式の中に `++`/`--` を入れると、条件が真でも偽でも値が変わる。チェックと変更は分ける。整数のあふれは既定が unchecked なので黙って一周する。あふれてはいけない計算は `checked(...)` で包むか、プロジェクトに `CheckForOverflowUnderflow` を入れて `OverflowException` で表に出す (ラボ 14 の int の掛け算と同じ規則)。

## Lab 23 — `||` の代わりに `|`: 短絡評価が消える
```
LoadFromDisk のブレークポイント:  キャッシュにすでにある "potion" なのに止まる (7マスに7回)
Get:  if (_cache.TryGetValue(id, out icon) | LoadFromDisk(id, out icon))
本当の原因:  bool に使った | はコンパイルされるが両側をいつも計算する。キャッシュにあってもディスクを読む
```
**修正:** 短絡評価の `||` を使う。`Lab23_IconCache.cs` の `Get` の `if (_cache.TryGetValue(id, out string? icon) | LoadFromDisk(id, out icon))` の行を次の一行に置き換える。
```csharp
            if (_cache.TryGetValue(id, out string? icon) || LoadFromDisk(id, out icon))
```
**教訓:** `||`/`&&` は左側だけで答えが決まれば右側を計算しない(短絡評価)。`|`/`&` は `bool` にも使えるが両側を計算する。右側に副作用(ディスク読み込み、カウンター、ログ)があれば毎回起き、`if (target == null | target.Hp <= 0)` のように左側が右側を守る式なら `NullReferenceException` が出る。整数フラグに `&&` の代わりに `&` を使う C++ 風のミスは、C# では `int` が `bool` に変わらずコンパイラが防いでくれる。

---

# 3部: コンテナとコレクション

## Lab 24 — Length と Count を混ぜて使った範囲外インデックス
```
例外:        ArgumentOutOfRangeException: Index was out of range. Must be non-negative and less than the size of the collection. (Parameter 'index')
止まった所:     Settle の clearSeconds[i]   (i == 3)
ウォッチ:      stages.Length == 4, clearSeconds.Count == 3
本当の原因:  ループはクエストの全段階数(stages.Length)だけ回るのに、読むのは今までクリアした段階のリスト(clearSeconds)
```
**修正:** ループは**実際に読むコレクション**のサイズで回す。`Lab24_QuestRewards.cs` の `Settle` の `for (int i = 0; i < stages.Length; i++)` の行を次の一行に置き換える。
```csharp
        for (int i = 0; i < clearSeconds.Count; i++)
```
**教訓:** 配列は `IndexOutOfRangeException`、`List` は `ArgumentOutOfRangeException` を出す。メッセージにインデックスの値はないので、ローカルウィンドウで `i` とサイズを直接比べる。この手のバグは「全部クリアしたクエスト」のように二つのサイズがたまたま同じデータでだけ試すと隠れる。境界(0個、一部、全部)を試そう。(`<=` で一マス越える off-by-one も同じ見た目だ。)

## Lab 25 — `List<struct>` のインデクサーが返したコピーを直す
```
TakeDamage の中:   Hp 100 → 70   (減る)
AreaAttack に出て party[i].Hp:   100   (そのまま)
本当の原因:  List<T> のインデクサーは get メソッドなので struct の「コピー」を返す。TakeDamage はその一時コピーを直して捨てる。
           party[i].Hp -= damage はコンパイラが CS1612 で防ぐが、メソッド呼び出しは防がない。
```
**修正:** 取り出して直して入れ直す。`Lab25_PartyHp.cs` の `AreaAttack` を次のものに置き換える(ループの本体が三行になるので波括弧で囲む)。`Member` と `Run` はそのまま。
```csharp
    static void AreaAttack(List<Member> party, int damage)
    {
        for (int i = 0; i < party.Count; i++)
        {
            Member member = party[i];   // コピーを取り出し
            member.TakeDamage(damage);  // 直して
            party[i] = member;          // 入れ直す
        }
    }
```
別の方法:
- `CollectionsMarshal.AsSpan(party)[i].TakeDamage(damage);` — `Span` のインデクサーは**マスそのもの(ref)**を返す。(その間に List に入れたり抜いたりしてはいけない。`using System.Runtime.InteropServices;` が必要)
- 変わるデータなら `class` にする。または `readonly struct` にして、変えるメソッド自体をなくす。

**教訓:** 配列の `arr[i]` はマスそのもの(ref)だが、`List`、`Dictionary` のインデクサーとプロパティは**値を返すメソッド**だ。変わる struct はコピーされるたびにバグの種になる (ラボ 11 の `readonly` フィールドの防御的コピーと同じ根)。

## Lab 26 — Dictionary のキーとして使う間にハッシュが変わる (可変キー)
```
TryGetValue:   false
ウォッチ storage:  キーの一覧に名前が変わったギルドがそのまま見える
storage.Keys.Contains(g):   true   (一つずつ Equals で比較)
storage.ContainsKey(g):     false  (ハッシュでマスを探す)
GetHashCode のブレークポイント:  入れるときと探すときで値が違う
本当の原因:  GetHashCode が Tag で計算されるのに、キーとして入れたあとに Tag を変えた。オブジェクトは古いハッシュのマスに残っている。
```
**修正:** 同じギルドかどうかは**変わらない値**で判断する。`Lab26_GuildRename.cs` の `Guild` の `Equals` と `GetHashCode` の二行(続いている)を次のものに置き換える。
```csharp
        public override bool Equals(object? obj) => obj is Guild g && g.Id == Id;
        public override int GetHashCode() => Id.GetHashCode();
```
タグをキーに使うなら、変えるときにディクショナリから `Remove` し、変えたあとで入れ直す。
**教訓:** ハッシュベースのコレクション(`Dictionary`、`HashSet`)のキーは、入れたあとに**ハッシュに使う値が変わってはいけない。** ラボ 18(Equals と GetHashCode が互いにずれる)と対だ。`record` はすべてのプロパティでハッシュを作るので、プロパティを変えられる record をキーに使うとこのバグになる。

## Lab 27 — Dictionary を複数のスレッドから同時に直す
```
実行するたびに:   記録数が 199,8xx のように足りないか、IndexOutOfRangeException / InvalidOperationException
                ("Operations that change non-concurrent collections must have exclusive access...")
並列スタック:      複数の戦闘スレッドが同時に Dictionary.TryInsert、Resize の中にいる
本当の原因:  Dictionary は一度に一つのスレッドだけが直すという前提で作られている。内部配列を増やす(Resize)途中に別のスレッドが入れると
           項目が消えたり内部のつながりが絡まったりする。キーが重ならなくても同じ。
```
**修正:** `Lab27_KillLog.cs` の `KillLog` クラスを次のものに置き換えます。ファイルの先頭(`namespace BugHunt;` の上)に `using System.Collections.Concurrent;` を追加します。`Run` はそのままです。
```csharp
using System.Collections.Concurrent;

    class KillLog
    {
        // 複数のスレッドが同時に変更してもよい辞書
        readonly ConcurrentDictionary<int, int> _kills = new();

        public void Record(int monsterId, int worker) => _kills[monsterId] = worker;

        public int Count => _kills.Count;
    }
```
別の方法: `Dictionary` のまま、変更する所を `lock (_kills) _kills[monsterId] = worker;` で包むか(その場合は `Count` も同じ `lock` の中で読みます)、戦闘スレッドごとに別々に集めて、終わったあと一つのスレッドで合わせます(一番速い方法です)。

**教訓:** .NET Framework の `Dictionary` はこう絡まると**無限ループ**(CPU 100% で止まる)に陥ることもあった。今の .NET は絡まりを一部検知して例外を出すが、項目が黙って消えるのは防げない。結果が実行ごとに違うならラボ 66 のように競合状態から疑う。

## Lab 28 — 不安定ソート (List.Sort)
```
スコアの順序:      合っている (3000、3000、...、2000、...)
同じスコアどうし:  P04 P20 P08 ... のように入ってきた順ではない
16個以下に減らすと:  合って出る (小さい区間は挿入ソートなのでたまたま安定)
本当の原因:  List.Sort は不安定ソート(introsort)。比較子が 0 を返した要素の元の順序を守らない。
```
**修正:** 安定ソートの LINQ を使います。`Lab28_Leaderboard.cs` の `SortLeaderboard` を次のものに置き換えます。`Run` は受け取ったリストがその場でソートされることを期待しているので、ソートした結果を同じリストに詰め直します。`Run` はそのままです。
```csharp
    static void SortLeaderboard(List<Entry> board)
    {
        // OrderByDescending は安定ソート: スコアが同じなら入ってきた順(先に達成した順)を守る
        List<Entry> sorted = [.. board.OrderByDescending(e => e.Score)];
        board.Clear();
        board.AddRange(sorted);
    }
```
別の方法: 同じときの規則を比較子に入れます。達成時刻や入ってきた順を `Entry` に持たせ、二番目のキーで比較します。

**教訓:** 「同じなら先に来た人」のように同じ値の順序に意味があるなら、その規則を**明示**する。比較子は一貫していなければならない。`(a, b) => a.Score > b.Score ? -1 : 1`(同じとき 0 を返さない)や `a.Score - b.Score`(あふれ、ラボ 14)のようにずれた比較子は、結果が実行・サイズごとにぶれたり `InvalidOperationException: IComparer.Compare() method returns inconsistent results` を出したりする。

## Lab 29 — yield メソッドの例外は列挙するときに出る
```
例外:        FormatException: ドロップテーブルの行がおかしい: "sword=?"
呼び出し履歴:   <ParseDropTable>d__1.MoveNext() ← Enumerable.Sum ← Lab29.Run   (LoadDropTable はない)
コンソール:        "壊れたドロップテーブル、既定のテーブルを使用" が一度も出ない
本当の原因:  yield return があるメソッドは呼ぶ瞬間に本体を一つも実行しない。LoadDropTable の try は「列挙するオブジェクト」だけ受けてすぐ終わり、
           実際のパース(と例外)はあとで Sum が列挙するときに起きる。
```
**修正:** 捕まえたい所で最後まで列挙する。`Lab29_DropTable.cs` の `LoadDropTable` メソッドを丸ごと次のものに置き換える(`try` の中の `return` に `.ToList()` が付く)。ファイル下部のヒントのコメントにも `return ParseDropTable(text);` が引用されているので、一括の検索置換ではなくこのメソッドだけを書き換える。`ParseDropTable` と `Run` はそのまま。
```csharp
    static IEnumerable<Drop> LoadDropTable(string text)
    {
        try
        {
            return ParseDropTable(text).ToList();   // ここで最後まで列挙するので、例外もこの try の中で出る
        }
        catch (FormatException ex)
        {
            Console.WriteLine(Text.L($"  고장 난 드롭 테이블, 기본 테이블 사용: {ex.Message}", $"  壊れたドロップテーブル、既定のテーブルを使用: {ex.Message}"));
            return DefaultTable;
        }
    }
```
**教訓:** 遅延実行されるのは LINQ(ラボ 10、30)だけではない。`yield` メソッドの**例外、ログ、ファイルを開くような副作用**はすべて「列挙する瞬間、列挙する所」で起きる。引数チェックのように即座に失敗すべきことは `yield` のない外側のメソッドで先にやり、列挙は内側のローカル関数に任せる。

## Lab 30 — ToList() なしで残した「スナップショット」
```
Enter の _roster:      WhereListIterator (ウォッチで「結果ビュー」を押さないと中身が出ない)
m => m.Online のブレークポイント:  Enter でも、RewardRecipients でも引っかかる
本当の原因:  _roster は入場したときの名簿ではなく「接続中の人」という条件だ。報酬を渡すとき列挙すると、そのときのギルドを改めて絞り、
           出た人は外れ、あとで入った人(Newbie を含む)が入る。
```
**修正:** その瞬間の名簿を写し取る。`Lab30_RaidRoster.cs` の `Enter` の `_roster = guild.Where(m => m.Online);` の行を次の一行に置き換える。
```csharp
            _roster = guild.Where(m => m.Online).ToList();
```
**教訓:** `Where`、`Select`、`Dictionary.Keys`/`Values`、`List.AsReadOnly()` は元を映す**窓**だ。元が変われば見えるものも変わる。その瞬間の写真が必要なら `ToList()`/`ToArray()` でコピーする。ただしコピーされるのはリストだけで、`Member` オブジェクトは依然として共有される (浅いコピー、ラボ 19)。

---

# 4部: C# 言語の罠

## Lab 31 — `string.GetHashCode` を保存して次の実行で比べる
```
ウォッチ "ファイアボール".GetHashCode():   実行ごとに違う値 (例: 1182412346 → -503617214)
savedHashes:                     前回のセッションの値(このラボでは固定の安定ハッシュで代用)
本当の原因:  .NET Core 以降、文字列のハッシュはプロセスごとにランダムなシードを使う(ハッシュ衝突攻撃の防止)。
           ハッシュは同じプロセス内の Dictionary/HashSet のための値で、保存したり送ったりする識別子ではない。
```
**修正:** ハッシュではなく**スキル名(または固定の Id)**そのものを保存して比べます。このラボではセーブ側がすでに安定ハッシュ(`OldSessionHash`)で保存されているので、比べる側も同じ関数を使えば通ります。`Run` で `starred` を作る行一つだけが変わります。
```csharp
        string[] starred = [.. allSkills.Where(s => savedHashes.Contains(OldSessionHash(s)))];
```
名前を保存する方向に直すなら、`savedHashes` の代わりに名前の集合を使います(そうすると `OldSessionHash` は使われなくなるので消してもかまいません)。
```csharp
        HashSet<string> savedNames = [.. favorites];
        string[] starred = [.. allSkills.Where(savedNames.Contains)];
```
ハッシュがどうしても必要なら FNV-1a、`XxHash64`(`System.IO.Hashing`)、SHA-256 のような**定義が固定されたハッシュ**を自分で使います。
**教訓:** `GetHashCode` の契約は「同じプロセスで、同じ値なら同じハッシュ」だけです。プロセス、マシン、.NET のバージョンを越えた瞬間に意味がなくなります。`HashCode.Combine` も同じ理由で実行ごとに違います。

## Lab 32 — `a + b ?? 0` は `(a + b) ?? 0`
```
ウォッチ baseDamage + comboBonus:          null   (型 int?)
ウォッチ (baseDamage + comboBonus) ?? 0:    0
ウォッチ baseDamage + (comboBonus ?? 0):    80
本当の原因:  ?? は + より優先順位が低い。int + int? は「リフト演算子」で int? になり、
           片方が null なら結果も null。その null に ?? 0 が付き、基本ダメージまで 0 になる。
```
**修正:** 括弧で `??` を先に計算する。`Lab32_ComboBonus.cs` の `TotalDamage` の `int total = baseDamage + comboBonus ?? 0;` の行を次の一行に置き換える。
```csharp
        int total = baseDamage + (comboBonus ?? 0);
```
`baseDamage + comboBonus.GetValueOrDefault()` でもよい。
**教訓:** `??`、`?:`、`=>` のように優先順位の低い演算子を算術式の中で使うときは、必ず括弧を付けます。null 許容どうしの `+ - * /`、`< >` は null があるとそれぞれ null、`false` になります(`<` と `>=` が両方 `false` になりうる)。

## Lab 33 — 範囲外の enum 値が `_ =>` で既定の等級になる
```
等級コード 9 → (Rarity)9 → コンソールに "9"
ウォッチ Enum.IsDefined((Rarity)9):  false
ウォッチ Weight((Rarity)9):           100 (Common と同じ値)
本当の原因:  enum は名前の付いた整数にすぎず、どんな整数でもキャストできる(例外なし)。
           switch の _ => 100 が「知らない値」まで Common として扱う。
```
**修正:** 知らない値には重みを与えません。`Lab33_DropRarity.cs` の `Weight` の switch 式の最後の腕 `_ => 100,` の行を次の一行に置き換えます。
```csharp
        _ => 0,
```
または受け取るときに `Enum.IsDefined` で先に弾いてスキップします。`_ => throw new ArgumentOutOfRangeException(...)` はこうして先に弾くときだけ使います(今の `Run` は `Weight` を先に呼ぶので、throw だけに変えるとコード 9 で落ちます)。
**教訓:** 外から来た整数(パケット、セーブ、設定)を enum に変えるときは範囲の検査が必要です。似た罠: `default(Rarity)` は最初のメンバー(0)なので「未指定」が黙って `Common` になります。0 には `None`/`Unknown` を置くのが安全です。`[Flags]` enum で `x.HasFlag(None)` は常に `true` です。

## Lab 34 — 値で渡した struct のカーソルは外で進まない
```
TakeTurn の中の cursor._index:  毎回 -1 → 0
Run の cursor._index:           ずっと -1
本当の原因:  TurnCursor は struct なので TakeTurn(TurnCursor cursor) は呼び出しごとにコピーを受け取る。
           MoveNext はコピーだけを進め、コピーは捨てられる。
```
**修正:** カーソルを `ref` で渡す。`Lab34_TurnOrder.cs` で二か所を変えます。

1. `TakeTurn` を次のものに置き換えます。
```csharp
    static string TakeTurn(ref TurnCursor cursor)
    {
        cursor.MoveNext();
        return cursor.Current;
    }
```
2. `Run` の `for` ループの中の `order.Add(TakeTurn(cursor));` の行を次の一行に置き換えます。
```csharp
            order.Add(TakeTurn(ref cursor));
```
または `TurnCursor` を `class` に変えます。
**教訓:** 状態を持つ struct(列挙子、カーソル、ビルダー)は代入・引数・`readonly` フィールド・`foreach` の反復変数でコピーされます。`List<T>.Enumerator` を変数に受けて別のメソッドに渡す場合や、`readonly` フィールドに置いた struct のメソッドを呼ぶ場合(防御的コピー)も同じ症状です。

## Lab 35 — 同じ文字列リテラルに lock して別々のシステムがデッドロック
```
並列スタック:  スレッド A  AutoSave.Save → ManualResetEventSlim.Wait  (ロックを握ったまま)
           スレッド B  Achievements.Tally → Monitor.Enter          (ロックを待つ)
ウォッチ object.ReferenceEquals(save._lock, achievements._lock):  true
本当の原因:  二つのクラスがそれぞれ "bughunt-save-lock" という文字列をロックオブジェクトにした。文字列リテラルは
           インターンされプロセス全体で同じオブジェクトなので、二つのロックは同じモニター。
```
**修正:** 各クラスが自分専用のロックオブジェクトを持つ。`Lab35_SharedLock.cs` の `AutoSave` クラスと `Achievements` クラスには、それぞれクラスの最初のメンバーとして `readonly object _lock = "bughunt-save-lock";` の行があります。その二行をどちらも次の一行に置き換えます(C# 13 以降なら `System.Threading.Lock` でもよい)。ファイル下部のヒントのコメントも同じコードを引用しているので、一括の検索置換は使わず、二つのクラスの中の宣言だけを直します。
```csharp
        private readonly object _lock = new();
```
**教訓:** ロックの対象は**その目的だけのために作った private オブジェクト**であるべきです。文字列、`typeof(X)`、`this`、ボックス化された値、外に公開されたオブジェクトは、知らないコードとモニターを共有しうります。また「ロックを握ったまま別の作業を待つ」こと自体がデッドロックの種なので、ロックの範囲を短く保ちます。

## Lab 36 — `for` 変数をキャプチャした `Task.Run`
```
ヒット時にログ {i} (downloaded.Add(i)):  5, 5, 5, 5, 5
gate.Set() の行での i:                  for を回し終えたあとなので、もう 5(= pieces)
本当の原因:  for の i はループ全体で一つだけの変数で、ラムダはその変数を参照でキャプチャする。
           タスクは gate のためループが終わったあとに動くので、すべて最後の値(範囲外)を見る。
```
**修正:** 反復ごとにコピーを作ります。`Lab36_PatchQueue.cs` の `Run` の中の `for (int i = 0; i < pieces; i++)` ループ全体(`tasks.Add(...)` を含む閉じ括弧 `}` まで)を次のものに置き換えます。`Run` のほかの行はそのままです。
```csharp
        for (int i = 0; i < pieces; i++)
        {
            int piece = i;   // 反復ごとに新しい変数。ラムダはこれをキャプチャする
            tasks.Add(Task.Run(() =>
            {
                gate.Wait();
                downloaded.Add(piece);
            }));
        }
```
または `foreach (int piece in Enumerable.Range(0, pieces))`、`Parallel.For`。
**教訓:** C# 5 から `foreach` の反復変数は反復ごとに新しく作られますが、`for` の変数は違います。このラボの `gate` は「タスクが遅れて始まる」状況を決定的にしたもので、実際にはスレッドプールの状態によって時々だけ間違います。同じ根の別の罠に `list.ForEach(async x => ...)` があります。`ForEach` は `Action<T>` しか受け取らないのでラムダが `async void` になり、待つことができず例外も捕まりません(8部の非同期の例外処理を参照)。

## Lab 37 — お金を `double` で足し、`Math.Round` の既定の丸めを使う
```
ウォッチ sum (Total の return):   0.30000000000000004
ウォッチ Math.Round(2.5m):        2
ウォッチ Math.Round(3.5m):        4
ウォッチ Math.Round(2.5m, MidpointRounding.AwayFromZero):  3
本当の原因:  double は 0.1、0.2 を正確に表せず、合計に誤差がたまる。
           Math.Round の既定は MidpointRounding.ToEven(銀行家の丸め)なので 2.5 → 2、0.5 → 0。
```
**修正:** 合計を `decimal` で足し、税は丸めの規則を明示します。`Lab37_CashShop.cs` の `Total` と `RoundTax` を次のものに置き換えます。`Run` が合計を `double` で比べるので `Total` の戻り値の型はそのままにし、足し終えたあとで一度だけ変換します。`Run` はそのままです。
```csharp
    // カートの合計。価格は小数。
    static double Total(decimal[] prices)
    {
        decimal sum = 0;
        foreach (decimal p in prices)
            sum += p;
        return (double)sum;   // 足し終えたあとで一度だけ変換する
    }

    // 税を整数に丸める。.5 は 0 から遠い方へ。
    static decimal RoundTax(decimal tax) => Math.Round(tax, MidpointRounding.AwayFromZero);
```
**教訓:** お金、数量、スコアのように10進で正確であるべき値は `decimal` または整数(円・セント単位)で扱います。丸めの規則はドメインの決定なので、常に引数で書きます。`double` の合計を `Math.Round(sum, 2)` で隠す修正は、このラボの隠しケースは通るかもしれませんが、値が大きくなったり演算が長くなったりするとまたずれます。(参考: `(decimal)someDouble` は有効数字15桁に丸めて変換するので誤差を隠してしまいます。このラボが合計を `double` で比べる理由です。)

---

# 5部: インターフェースと継承

## Lab 38 — `new` で隠したメソッドは基底型で呼ぶと呼ばれない
```
FireSkill.Damage のブレークポイント:   TotalDamage では止まらない (Run の期待値計算でだけ止まる)
F11 (s.Damage(attack)):    Skill.Damage に入る
ウォッチ s.GetType():        FireSkill   (静的な型は Skill)
本当の原因:  FireSkill・IceSkill の Damage が override ではなく new。基底メソッドを上書きせず、同じ名前の新しいメソッドを作った。
           Skill 型の変数で呼ぶと Skill.Damage が呼ばれる。new を消すと CS0114 警告が出るので、誰かが new で抑え込んだもの。
```
**修正:** `new` を `override` に変える。`Lab38_SkillDamage.cs` で二か所、`FireSkill` の `Damage` の行と `IceSkill` の `Damage` の行を、それぞれ次の一行に置き換える。

1. `FireSkill`:
```csharp
        public override int Damage(int attack) => (attack + Power) * 3 / 2;
```
2. `IceSkill`:
```csharp
        public override int Damage(int attack) => attack + Power + 20;
```
**教訓:** CS0114(「継承されたメンバーを隠します。オーバーライドするなら override を、そうでなければ new を追加してください」)はたいてい `override` を抜かしたという意味です。`new` は基底クラスを変えられないのに同じ名前がどうしても必要な、まれな場合にだけ使います。炎スキルだけ直すと隠しケース(氷スキル)で引っかかります。

## Lab 39 — シグネチャの違うメソッドの代わりにインターフェースの既定実装が呼ばれる
```
Boss.DropCount のブレークポイント:     RollDrops では止まらない
F11 (monster.DropCount):   IDropSource.DropCount の既定実装 (=> 1) に入る
本当の原因:  Boss.DropCount(int, bool = false)、Elite.DropCount(long) はインターフェースの int DropCount(int) とシグネチャが違うので
           実装と認められない。インターフェースに既定実装があるのでコンパイルエラーも警告もない。
```
**修正:** シグネチャをインターフェースの `int DropCount(int luck)` とまったく同じにする。`Boss` は省略可能な引数 `bool elite = false` とそれを使っていた `(elite ? 1 : 0)` を消し、`Elite` は `long` を `int` に変える。二つのクラスの上のコメント(「精鋭ボスは1個多い」「運は long で受け取る」)も変わったコードに合わせて直す。
```csharp
    sealed class Boss(string name, int baseDrops) : IDropSource
    {
        public string Name => name;
        public int DropCount(int luck) => baseDrops + luck / 10;
    }

    sealed class Elite(string name) : IDropSource
    {
        public string Name => name;
        public int DropCount(int luck) => 2 + luck / 20;
    }
```
精鋭ボスのような変形はコンストラクターの引数(`bool elite`)か別のオーバーロードで受け取り、サーバーの `long` の運の値はインターフェースを呼ぶ前に `int` に変換する。
**教訓:** 省略可能な引数を付けたり引数の型を広げたり(`int` → `long`)すると、呼ぶ側のコードはそっくりでも**別のメソッド**です。既定実装(DIM)のあるインターフェースは「実装を抜かしてもコンパイルできる」という危険を一緒に持ち込みます。既定実装を一時的に消してみると、実装が抜けたクラスがコンパイルエラーで表に出ます。

## Lab 40 — 明示的インターフェース実装が違う値を返す
```
ウォッチ r.Gold:            600     (IReward で見る → 明示的実装)
ウォッチ ((Quest)r).Gold:   1200    (Quest で見る → public 実装)
本当の原因:  Quest に Gold が二つ。public int Gold(倍率適用)と int IReward.Gold(倍率なし)。
           クエスト画面は Quest で、精算は IReward で読むので別々の実装が呼ばれる。
```
**修正:** 明示的実装を public 実装に委譲する。`Lab40_QuestReward.cs` の `Quest` の `int IReward.Gold => baseGold;` の行を次の一行に置き換える。
```csharp
        int IReward.Gold => Gold;
```
明示的実装の行を消してもよい(public `Gold` がそのままインターフェースを実装する)。
**教訓:** 明示的実装は名前の衝突を避けたり、メンバーをインターフェース側にだけ見せたりするときに使います。二つ目のロジックを置く場所ではありません。精算で2倍する修正は隠しケース(別の倍率)で引っかかります。

## Lab 41 — struct をインターフェースとして扱い、ボックス化コピーだけが変わる
```
Buff.Tick のブレークポイント:     呼ばれる。this.Turns が減るのも見える
EndTurn のあと buffs[0].Turns:   そのまま
Tick の中で this にオブジェクト ID を作成:   不可 (値型)
本当の原因:  foreach (ITickable buff in buffs) がマスごとに Buff をボックス化してヒープのコピーを作り、Tick はそのコピーを直す。
```
**修正:** 配列のマスそのものに対して `Tick` を呼ぶ。`Lab41_BuffTick.cs` の `EndTurn` を次のものに置き換える。`Buff` と `Run` はそのまま。
```csharp
    static void EndTurn(Buff[] buffs)
    {
        for (int i = 0; i < buffs.Length; i++)
            buffs[i].Tick();   // buffs[i] は配列のマスそのもの。ボックス化もコピーもない
    }
```
別の方法:
- `foreach (ref Buff buff in buffs.AsSpan()) buff.Tick();`
- ジェネリック制約: `static void TickAll<T>(Span<T> items) where T : ITickable { foreach (ref T it in items) it.Tick(); }`
- 変わるデータなら `class` に。

**教訓:** 値型をインターフェース、`object` に変える瞬間にコピーが生まれます(ラボ 4 のボックス化解除、ラボ 12 のボックス化された `==`、ラボ 25 の `List<struct>` インデクサーと同じ根)。変わる struct にインターフェースを付けると、このバグが紛れ込みやすいです。

## Lab 42 — コンストラクター内の仮想呼び出し
```
Knight.MaxHp のブレークポイント (最初に止まる):   armor == 0、baseHp == 100
呼び出し履歴:   Unit..ctor → Knight.MaxHp   (Knight..ctor 本体はまだ)
本当の原因:  生成の順序は派生フィールドの初期化子 → 基底コンストラクター → 派生コンストラクター本体。
           基底コンストラクターが呼んだ仮想メソッドはすでに Knight.MaxHp に行くが、armor は派生コンストラクター本体でやっと入る。
```
**修正:** 基底コンストラクターが最大 HP を引数で受け取り、仮想呼び出し `Hp = MaxHp();` をなくす。派生コンストラクターは自分のフィールドの値で計算した最大 HP を `base(...)` に渡す。`Lab42_UnitSpawn.cs` で三か所を変える。`MaxHp()` のオーバーライドとフィールド初期化子はそのまま残す。

1. `Unit` クラスを丸ごと次のものに置き換える。
```csharp
    abstract class Unit
    {
        public string Name { get; }
        public int Hp { get; protected set; }

        protected Unit(string name, int maxHp)
        {
            Name = name;
            Hp = maxHp;   // 派生クラスが計算して渡した値。仮想呼び出しがない
        }

        public abstract int MaxHp();
    }
```
2. `Knight` のコンストラクターを次のものに置き換える。
```csharp
        public Knight(string name, int armor) : base(name, 100 + armor * 10)
        {
            this.armor = armor;
        }
```
3. `Mage` のコンストラクターを次のものに置き換える。
```csharp
        public Mage(string name, int wisdom) : base(name, 60 + wisdom * 5)
        {
            this.wisdom = wisdom;
        }
```
または HP を満たすのを生成が終わったあとの `Spawn()` の段階に移す。
**教訓:** コンストラクターで仮想メソッドを呼ぶと、派生クラスがまだ準備できていないオブジェクトを見ます(CA2214)。フィールド初期化子(`baseHp = 100`)は基底コンストラクターより**先に**、コンストラクター本体は**あとに**実行されるという違いが症状を紛らわしくします。`Knight` コンストラクターの終わりで `Hp` を入れ直す修正は隠しケース(魔法使い)で引っかかります。

## Lab 43 — `Equals(object)` と `IEquatable<T>.Equals` が違う規則
```
bag.Contains(loot):         true    → Equals(ItemStack) (Id だけ比較)
favorites.Contains(loot):   false   → Equals(object)    (Id + 品質を比較)
ウォッチ favorites.Comparer:  ObjectEqualityComparer<IItem>
本当の原因:  EqualityComparer<T>.Default は T が IEquatable<T> なら Equals(T)、そうでなければ Equals(object) を呼ぶ。
           List<ItemStack> は T = ItemStack、HashSet<IItem> は T = IItem なので別々の Equals に乗る。
```
**修正:** 一つの規則に委譲する。`Lab43_Favorites.cs` の `ItemStack` の `public override bool Equals(object? obj)` の行を次の一行に置き換える。
```csharp
        public override bool Equals(object? obj) => Equals(obj as ItemStack);
```
**教訓:** 同じ型が `IEquatable<T>`、`object`、インターフェース型のコレクション、`==` 演算子などいくつもの経路で比較されます。すべての経路が同じ規則でなければならず、`GetHashCode` も同じ規則でなければなりません(ラボ 18 はハッシュがずれた場合)。お気に入りを `HashSet<ItemStack>` に変える修正も通りますが、別の `IItem` 実装が入ってきた瞬間にまた壊れます。

## Lab 44 — 基底型を先にチェックして派生の分岐に届かない
```
return 500; のブレークポイント:   止まらない
ウォッチ killed is Enemy:       true   (ボスも Enemy)
ウォッチ killed is BossEnemy:   true
本当の原因:  if (killed is Enemy) が最初の分岐なのでボス・精鋭もすべて 10G。あとの else if 二つには届かない。
           同じ順序を switch 式で書くと CS8510(switch 文なら CS8120、「すでに前で処理」)でコンパイルできないが、if/else if にはそのチェックがない。
```
**修正:** 一番派生した型から検査する (ボス → 精鋭 → 一般の敵)。`Lab44_KillReward.cs` の `Reward` を次のものに置き換える。`Spawn`、`ExpectedReward`、`Run` はそのまま。
```csharp
    static int Reward(Unit killed)
    {
        if (killed is BossEnemy)
            return 500;
        else if (killed is EliteEnemy)
            return 50;
        else if (killed is Enemy)
            return 10;
        else
            return 0;
    }
```
もっとよい方法は `switch` 式で書いてコンパイラに順序を検査させるか、`Unit` に `virtual int KillReward` を置いて型チェック自体をなくすこと。
**教訓:** ボスの分岐だけ上に上げると隠しケース(精鋭が混ざったウェーブ)で引っかかります。

## Lab 45 — 配列の共変性
```
例外:      ArrayTypeMismatchException: Attempted to access an element as a type incompatible with the array.
止まった所:   Put の bag[slot] = item
ウォッチ bag.GetType():   Lab45+Weapon[]   (宣言型は Item[])
本当の原因:  CreateBag が new Weapon[size] を作って Item[] で返した。配列の共変性のおかげでコンパイルは通るが、実際の配列は Weapon しか受けない。
```
**修正:** 実際の配列を `Item[]` で作る。`Lab45_StarterBag.cs` の `CreateBag` の `var bag = new Weapon[size];` の行を次の一行に置き換える。
```csharp
        var bag = new Item[size];
```
**教訓:** 参照型の配列は共変なので `Weapon[]` を `Item[]` 変数に入れられますが、書き込みはランタイムのチェックで防がれます。`List<T>` は共変でないのでこのミスはコンパイルされず、読み取り専用の共変性は `IEnumerable<out T>`、`IReadOnlyList<out T>` で得ます。例外を飲み込んで飛ばすとポーションが消え、埋まったマスの数で引っかかります。(ブラインドチャレンジ B-4 も同じカテゴリです)

---

# 6部: ファイル入出力

## Lab 46 — 相対パスは作業ディレクトリ基準
```
例外:        DirectoryNotFoundException: Could not find a part of the path '...\Lab46_SaveFolder\mods\saves\slot1.sav'.
保存するときのパス:  ...\Lab46_SaveFolder\game\saves\slot1.sav
ウォッチ:      Environment.CurrentDirectory == ...\mods,  Path.GetFullPath("saves") == ...\mods\saves
本当の原因:  SavePath が "saves\slot1.sav" という相対パスを作る。相対パスはそのときどきの作業ディレクトリ基準なのに、
           フォルダーを開く(ファイルダイアログ)が作業ディレクトリを変えた。SaveSystem は GameDir を受け取っておきながら使わなかった。
```
**修正:** パスを `GameDir` 基準で作り、フォルダーも同じ基準で作る。`Lab46_SaveFolder.cs` の `SaveSystem` の `SavePath` と `Save` を(二つは隣り合っている)次のものに置き換える。`Load` と `Run` はそのまま。
```csharp
        string SavePath(string slot) => Path.Combine(GameDir, "saves", slot + ".sav");

        public void Save(string slot, int gold)
        {
            Directory.CreateDirectory(Path.Combine(GameDir, "saves"));
            File.WriteAllText(SavePath(slot), gold.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine(Text.L($"  저장: {slot} ({gold}G) → {Path.GetFullPath(SavePath(slot))}", $"  セーブ: {slot} ({gold}G) → {Path.GetFullPath(SavePath(slot))}"));
        }
```
**教訓:** 作業ディレクトリはプロセス全体が共有する変わる状態です(ランチャー、ショートカットの作業フォルダー、ファイルダイアログ、別のフォルダーで打ったコマンドライン)。実行ファイルのフォルダーは `AppContext.BaseDirectory`、ユーザーデータは `Environment.GetFolderPath(SpecialFolder.LocalApplicationData)` の下が正解です。ロードの前に作業ディレクトリを戻す修正は、次に作業ディレクトリを変えるコードができるとまた壊れます。

## Lab 47 — 内側のストリームだけ閉じて `StreamWriter` のバッファが捨てられる
```
SaveLog の終わり:   stream.Position が書くべき量より小さい
writer [Raw View] _charPos:   0 でない (バッファに残った文字)
本当の原因:  using は FileStream にだけ付いている。StreamWriter は自分のバッファ(1024文字)がいっぱいになるたびにストリームへ送るが、
           最後に残った文字は writer の Flush/Dispose のときだけ出る。内側のストリームを閉じても外側の writer のバッファは空にならない。
```
**修正:** writer にも `using` を付ける。`Lab47_CombatLog.cs` の `SaveLog` の `var writer = new StreamWriter(stream);` の行を次の一行に置き換える(宣言の逆順で閉じられ、writer が先に閉じる)。
```csharp
        using var writer = new StreamWriter(stream);
```
`FileStream` を作らず `using var writer = new StreamWriter(path);` 一つにしてもよい。
**教訓:** ラッパーが重なると(`StreamWriter`、`BinaryWriter`、`GZipStream`、`CryptoStream`)**一番外から**閉じます。外だけ閉じると内側まで閉じ、内側を使い続けるなら `leaveOpen: true`。ラボ 72 はそもそも閉じずにファイルが空になり、ハンドルが残った場合、今回は内側だけ閉じて末尾が切れた場合です。

## Lab 48 — `FileShare` が合わず書き込み中のファイルを読めない
```
first chance:  IOException: The process cannot access the file '...chat.log' because it is being used by another process.
止まった所:      File.ReadAllLines (Upload の catch が飲み込んで 0 行アップロード)
本当の原因:  ChatLogger の StreamWriter(append) は FileAccess.Write + FileShare.Read で開いている。
           File.ReadAllLines は FileAccess.Read + FileShare.Read で開く。つまり「他人が書いていれば拒否」。「別のプロセス」は実はこのプロセス。
```
**修正:** 読む側が書き込みの共有を許す。`Upload` の `File.ReadAllLines(path)` は共有モードを選べないので、`FileStream` を自分で開いて `StreamReader` で行を読む。`Lab48_ChatUpload.cs` の `Upload` を丸ごと次のものに置き換える。`ChatLogger` と `Run` はそのまま。
```csharp
    static string[] Upload(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            var lines = new List<string>();
            while (reader.ReadLine() is { } line)
                lines.Add(line);
            Console.WriteLine(Text.L($"  업로드: {lines.Count}줄", $"  アップロード: {lines.Count}行"));
            return [.. lines];
        }
        catch (IOException ex)
        {
            Console.WriteLine(Text.L($"  업로드 실패: {ex.Message}", $"  アップロード失敗: {ex.Message}"));
            return [];
        }
    }
```
**教訓:** 二つのハンドルが共存するには、互いの `FileAccess` が相手の `FileShare` に入っていなければなりません。書く側は `AutoFlush` や `Flush` もしないと、読む側が最新の内容を見られません。アップロードの前にロガーを閉じる修正も動きますが、通報中に来たチャットを逃すことがあります。`catch (IOException)` が失敗を「0 行」に変えたのもラボ 69(例外を飲み込んで成功を返す)と同じ匂いです。Linux/macOS は共有モードを強制しないので、このラボは `SKIP` します。

## Lab 49 — `Path.Combine` の二番目の引数が絶対パス
```
コンソール:      "を保存 →" の三行のうち一つだけ ...\old-install\saves\mira
ウォッチ:    Path.Combine(saveRoot, profile.Folder) == profile.Folder,  Path.IsPathRooted(profile.Folder) == true
本当の原因:  Path.Combine は後ろの引数がルートパスなら前の引数をすべて捨てる(ドキュメントに書かれた動作、例外なし)。
           古いインストールから持ってきたプロフィールの Folder が絶対パスなので、セーブルートの外に保存され、バックアップがセーブルートだけを走査して抜けた。
```
**修正:** 外から来た値はフォルダー名としてだけ使う。`Lab49_ProfileImport.cs` の `SaveDir` の行を次の一行に置き換える。
```csharp
    static string SaveDir(string saveRoot, Profile profile) => Path.Combine(saveRoot, Path.GetFileName(profile.Folder));
```
合わせたあと `Path.GetFullPath(結果)` がセーブルートの中かもう一度確認すると安全。(`Path.IsPathRooted`、`..` を含めば拒否するやり方はこのゲームには合わない: 取り込んだプロフィールのセーブが抜けて判定で引っかかる。)
**教訓:** ユーザー入力、セーブファイル、サーバーの応答、圧縮ファイル内の名前を `Path.Combine` にそのまま入れるとパスの脱出(path traversal)になります。`Path.Join` は前を捨てませんが、見当違いのパスを作るだけで検証の代わりにはなりません。

## Lab 50 — バイトを UTF-8 でだけデコード: BOM と CP949
```
MOD A (UTF-8 BOM):  table.Keys.First().Length == firstKey.Length + 1,  最初の文字 '﻿'
MOD B (CP949):      bytes のかな(韓国語で実行したならハングル)が2バイトずつ。UTF8.GetString の結果に '�'(�)
MOD C (UTF-8):      正常
本当の原因:  Encoding.UTF8.GetString は BOM を消さず(U+FEFF として残る)、不正なバイトは例外なしに � に変える。
           File.ReadAllText/StreamReader は BOM を処理するが、バイトを直接デコードするとそうではない。
```
**修正:** BOM を外し、UTF-8 として正しくなければ CP949 で読み直す。`Lab50_DialogueMod.cs` の `LoadDialogue` を丸ごと次のものに置き換える(`string text = Encoding.UTF8.GetString(bytes);` の一行が下の `if`/`else` になる)。CP949 に必要な `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)` は `Run` がすでに呼んでおり、`using System.Text;` もファイルにある。`Lookup` と `Run` はそのまま。
```csharp
    static Dictionary<string, string> LoadDialogue(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        string text;
        if (bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble))
            text = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);   // BOM の3バイトを飛ばす
        else
        {
            try { text = new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes); }
            catch (DecoderFallbackException) { text = Encoding.GetEncoding(949).GetString(bytes); }   // UTF-8 でなければ CP949
        }

        var table = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            int eq = line.IndexOf('=');
            if (eq > 0)
                table[line[..eq]] = line[(eq + 1)..].TrimEnd('\r');
        }
        return table;
    }
```
**教訓:** .NET Core 以降は既定で UTF-8、UTF-16、ASCII、Latin-1 くらいしか入っておらず、CP949・Shift_JIS(932) のようなコードページは `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)` をしないと使えません。キーから `﻿` だけ消す修正はモード B を直せません。保存する側ならば BOM なしの UTF-8 で統一します(`File.WriteAllText` の既定値)。

## Lab 51 — `\n` でだけ切って `\r` と空項目が残る
```
コンソール "読んだ項目":   "Kael\r"、"Mira\r"、"Doran\r"、""
ウォッチ:            text.Contains('\r') == true
本当の原因:  メモ帳は CRLF で保存する。Split('\n') は各項目の末尾に '\r' を残し、末尾の改行のあとに空文字列をもう一つ作る。
```
**修正:** 行単位で読む API を使います。`Lab51_BlockList.cs` の `LoadBlockList` を次の一行に置き換えます(`File.ReadAllText` と `Split('\n')` は消します)。`Run` はそのままです。
```csharp
    static HashSet<string> LoadBlockList(string path) => [.. File.ReadAllLines(path)];
```
`File.ReadLines` や `StreamReader.ReadLine` でも同じです。自分で切るなら `text.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)` です。
**教訓:** `ReadLine` 系は `\r\n`、`\n`、`\r` をすべて行末として扱い、末尾の改行のあとに空の行を作りません。`TrimEnd('\r')` だけ付けると空項目が残り、`Split("\r\n")` だけ使うと隠しケース(LF ファイル)で一行につながって引っかかります。

## Lab 52 — 元を直接上書きしていて失敗し、書きかけのファイルになる
```
Save(path, broken) の直前:  inventory.sav 正常
Save の最初の行(new StreamWriter(path))の直後:  0 バイト
例外のあと:  個数の行 + 前の方のアイテム数行だけ (Load が破損と判定)
本当の原因:  new StreamWriter(path) は FileMode.Create なので開いた瞬間に既存のファイルを切り詰める。例外が出ても using がそれまでに書いた前半を出す。
```
**修正:** 別のファイルに最後まで書き、書き終えて閉じたあと一度に入れ替えます。`Lab52_SafeSave.cs` の `Save` を丸ごと次のものに置き換えます。失敗したら一時ファイルを消し、例外をそのまま投げ直します(`Run` がその例外を捕まえて前回のセーブを使います)。`using System.Globalization;` はファイルにすでにあり、`Load` と `Run` はそのままです。
```csharp
    static void Save(string path, IReadOnlyList<Item> items)
    {
        string tmp = path + ".tmp";
        try
        {
            using (var writer = new StreamWriter(tmp))
            {
                writer.WriteLine(items.Count.ToString(CultureInfo.InvariantCulture));
                foreach (Item item in items)
                {
                    if (item.Count < 0)
                        throw new InvalidDataException(Text.L($"손상된 아이템: {item.Name} x{item.Count}", $"破損したアイテム: {item.Name} x{item.Count}"));
                    writer.WriteLine($"{item.Name}|{item.Count.ToString(CultureInfo.InvariantCulture)}");
                }
            }   // 入れ替える前に閉じて、バッファをすべて書き出す
        }
        catch
        {
            File.Delete(tmp);   // 失敗した保存の一時ファイルは残さない
            throw;
        }
        File.Move(tmp, path, overwrite: true);   // 同じボリューム内では名前が変わるだけ
    }
```
前回のセーブをバックアップとしても残したいなら、元のファイルがあるときに最後の行の代わりに `File.Replace(tmp, path, path + ".bak");` を使います。
**教訓:** 停電、強制終了、ディスクいっぱいも同じ結果です。同じボリューム内の `File.Replace`/`File.Move(…, overwrite: true)` は名前を変えるだけなので中間の状態が残りません。先にメモリで全部検査してから開く修正はこの例外は避けられますが、書いている途中の中断には依然として弱いです。失敗した保存の一時ファイルは消します(三つ目の判定)。

## Lab 53 — 非同期書き込みを待たずにそのファイルを読む
```
ブレークポイントの順序:   ReadAllTextAsync が WriteAllTextAsync より先に止まる
ウォッチ:        save.IsCompleted == false, save.Status == WaitingForActivation
タスクウィンドウ:       SaveSettingsAsync が Task.Delay で待機中
本当の原因:  ApplyAsync が保存の Task をキューに入れるだけで await せずにファイルを読む。読むのは前の設定。
           Task を変数で受けたので CS4014 の警告も出ない。
```
**修正:** ファイルを読む前に保存の完了を待つ。`Lab53_CloudSync.cs` の `ApplyAsync` を次のものに置き換える。`await save;` は `await FileLock.WaitAsync();` より**前**に置く。`SaveSettingsAsync` も書き込むときに同じ `FileLock` を取るので、ロックを握ったまま `await save;` すると、保存はロックを待ち、こちらは保存を待ってデッドロックになる。`SaveSettingsAsync` と `Run` はそのまま。
```csharp
    static async Task<string> ApplyAsync(string path, int volume, int sensitivity)
    {
        Task save = _saveQueue = SaveSettingsAsync(_saveQueue, path, volume, sensitivity);

        await save;   // 保存が終わってから読む。FileLock を取る前に待つ
        string uploaded;
        await FileLock.WaitAsync();
        try { uploaded = await File.ReadAllTextAsync(path); }
        finally { FileLock.Release(); }
        Console.WriteLine(Text.L($"  클라우드에 올림: {OneLine(uploaded)}", $"  クラウドにアップロード: {OneLine(uploaded)}"));
        return uploaded;
    }
```
**教訓:** 順序は時間(`Task.Delay`)ではなく作業の完了で合わせます。保存した内容をアップロードに直接渡してファイルを読み直さないのも手です。ラボ 64 は待たなかった作業がそもそも終わらない場合、今回は終わりはするが別のコードが早すぎてその結果(ファイル)を見る場合です。

---

# 7部: シリアル化 (JSON/XML)

## Lab 54 — JSON の名前の大文字小文字が合わず黙って既定値になる
```
受け取った JSON:  {"title":"...","author":"...","priority":3,"pinned":true}
Title の set のブレークポイント:  Deserialize の間に呼ばれない
本当の原因:  System.Text.Json の既定オプションは名前を大文字小文字まで正確に比較する。"title" ≠ Title。
           知らないプロパティは既定で無視するので、例外なしにすべてのプロパティが初期値のまま残る。
```
**修正:** Web の既定値(camelCase、大文字小文字を無視)を使う。`Lab54_GuildNotice.cs` の `Options` フィールドの行を次の一行に置き換える。
```csharp
    static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = false };
```
`PropertyNameCaseInsensitive = true`、`PropertyNamingPolicy = JsonNamingPolicy.CamelCase`、プロパティごとの `[JsonPropertyName]` でもよい。
**教訓:** シリアル化のオプションは**相手と合わせる契約**です。型を合わせている間は `UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow` で「知らないプロパティなら例外」を入れておくと、この手のバグがすぐ表に出ます。(参考: Newtonsoft.Json は既定が大文字小文字を無視するので、移ってきたコードでこのバグがよく起きます。)

## Lab 55 — フィールドと `private set` は往復しない
```
セーブファイル:   {"name":"アリン","level":5}    ← gold がない
Level の private set のブレークポイント:   Load の間に呼ばれない
本当の原因:  Gold はフィールド。System.Text.Json は既定で public プロパティだけ扱う → 保存から抜ける。
           Level は public get なので保存はされるが set が private なので読むとき飛ばす → 初期値 1。
```
**修正:** 二つのメンバーに `[JsonInclude]` を付けます。`Lab55_CharacterSave.cs` の `Character` クラスを次のものに置き換え、ファイル先頭の `using` 行に `using System.Text.Json.Serialization;` を追加します(`[JsonInclude]` がある名前空間です)。`Run` はそのままです。
```csharp
using System.Text.Json.Serialization;

    sealed class Character
    {
        public string Name { get; set; } = "";

        // ゴールドは頻繁に変わるのでフィールドにしている。フィールドは [JsonInclude] がないと保存・復元されない。
        [JsonInclude] public int Gold;

        // レベルは LevelUp でだけ上げる。private set は [JsonInclude] がないと読むときに使われない。
        [JsonInclude] public int Level { get; private set; } = 1;

        public void LevelUp() => Level++;
    }
```
別の方法: `Options` に `IncludeFields = true` を指定して `Level` にだけ `[JsonInclude]` を付けるか、`[JsonConstructor]` のコンストラクターで受け取ります。
**教訓:** 「保存はされるのに読めないメンバー」には警告がありません。セーブ形式には保存 → ロード → 比較する往復テストを置きます。

## Lab 56 — ポリモーフィズムの情報がなく派生型が基底型で戻ってくる
```
サーバー保存:    {"to":"アリン","attachments":[{"source":"ウィークリークエスト"},{"source":"ウィークリークエスト"}]}   ← amount、itemName がない
ウォッチ received.Attachments[0].GetType():   Lab56+Reward
本当の原因:  List<Reward> の項目を宣言型 Reward でシリアル化して派生型のプロパティが抜け、
           JSON に型の識別子がないので読むときもすべて Reward で作る。
```
**修正:** 基底型に派生型を登録します。`Lab56_MailRewards.cs` の `Reward` クラスを次のものに置き換え(本体はそのままで、属性が四行増えます)、ファイル先頭の `using` 行に `using System.Text.Json.Serialization;` を追加します。派生型は三つすべて登録する必要があります。派生クラスと `Run` はそのままです。
```csharp
using System.Text.Json.Serialization;

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(GoldReward), "gold")]
    [JsonDerivedType(typeof(ItemReward), "item")]
    [JsonDerivedType(typeof(ExpReward), "exp")]
    class Reward
    {
        public string Source { get; set; } = "";
        public virtual string Describe() => Text.L("알 수 없는 보상", "不明な報酬");
    }
```
**教訓:** System.Text.Json はセキュリティ上「JSON に書かれた型名で任意の型を作る」機能がなく、許す派生型を自分で登録します。一つでも抜かすと、その型をシリアル化するとき `NotSupportedException` が出ます(隠しケースの経験値報酬)。基底型を `abstract` にしておくと、登録を忘れたとき逆シリアル化が黙って通らず例外を出してくれます。

## Lab 57 — 共有参照が往復後に二つのコピーになる
```
Restore の前:   party.Leader と party.Members[0] のオブジェクト ID が同じ ($1)
スナップショット:       リーダーの name/hp が members の中と leader に二回書かれる
Restore のあと:   restored.Leader と restored.Members[0] のオブジェクト ID が違う
本当の原因:  JSON には「同じオブジェクト」の概念がない。書かれた所ごとに新しいオブジェクトを作る。MainTank も同じ。
```
**修正:** 保存・復元の両方のオプションに `ReferenceHandler = ReferenceHandler.Preserve` を指定します(`$id`/`$ref` が付きます)。`Lab57_PartySnapshot.cs` では `Serialize` と `Deserialize` が同じ `Options` フィールドを使うので、そのフィールド一つを次のものに置き換えれば両方に効きます。ファイル先頭の `using` 行に `using System.Text.Json.Serialization;` を追加します(`ReferenceHandler` がある名前空間です)。`Restore` と `Run` はそのままです。
```csharp
using System.Text.Json.Serialization;

    // ハングルをエスケープせずそのまま書く(コンソールで読みやすく)。同じオブジェクトは $id/$ref で一度だけ書く。
    static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        ReferenceHandler = ReferenceHandler.Preserve,
    };
```
別の方法: 型を変えます。`Leader` の代わりに `LeaderIndex`(または固有の Id)を保存し、読んだあとでつなぎ直します。

**教訓:** オブジェクトグラフをそのままシリアル化すると共有が消え、循環があると `JsonException`(深さ超過)が出ます。セーブ形式としては Id で参照する方が丈夫です(`$ref` は別のプログラムが理解できないことがある)。リーダーだけ名前でつなぎ直す修正はメインタンクで引っかかります。

## Lab 58 — 欠落(missing)、null、0 を区別できない
```
{"item":"マナポーション"}          → 受信 "マナポーション x0" (count なし → int 既定値 0、例外なし)
{"item":"矢","count":null}        → 拒否                    (int に null → JsonException)
{"item":null,"count":5}  (隠し)   → 受信 "(null) x5"        (string は null を受けられる)
本当の原因:  System.Text.Json は JSON にないプロパティを触らない。nullable 注釈(string は null でない)も既定では検査しない。
```
**修正:** 「あるか」は `required` で、「null か」は `RespectNullableAnnotations` で検査する。`Lab58_RewardPacket.cs` の `MailPacket` クラスと、そのすぐ下の `Options` フィールドを(二つは隣り合っている)次のものに置き換える。`Decode` と `Run` はそのまま。
```csharp
    sealed class MailPacket
    {
        public required string Item { get; set; } = "";
        public required int Count { get; set; }
    }

    static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { RespectNullableAnnotations = true };
```
**教訓:** `required`(または `[JsonRequired]`)は「あるか」だけを見て、null かどうかは `RespectNullableAnnotations`(.NET 9+)が見ます。二つは別の問いです。`required` だけ付けると隠しケース(`"item": null`)で引っかかります。受けたあと `Count == 0` を弾く修正は本当の0個を区別できず、名前のない報酬も弾けません。

## Lab 59 — `XmlSerializer`: 引数なしのコンストラクターと `Dictionary`
```
例外:   InvalidOperationException: There was an error reflecting type 'BugHunt.Lab59.GameOptions'.
 ---> InvalidOperationException: Cannot serialize member 'BugHunt.Lab59+GameOptions.Graphics' of type 'BugHunt.Lab59+GraphicsOptions', see inner exception for more details.
 ---> InvalidOperationException: BugHunt.Lab59.GraphicsOptions cannot be serialized because it does not have a parameterless constructor.
止まった所:  new XmlSerializer(typeof(GameOptions))   (データと関係なくコンストラクターで)
それを直すと次:  Cannot serialize member ...KeyBindings of type Dictionary`2 ... because it implements IDictionary.
```
**修正:** `GraphicsOptions` を引数なしのコンストラクターと public set のある class にします。`KeyBindings` は `[XmlIgnore]` にして、XML には配列の代理プロパティで書きます。`Lab59_OptionsXml.cs` の `GraphicsOptions` レコードと `GameOptions` クラスを消し、その場所に次の二つのクラスを入れます。`Run` が `new GraphicsOptions(w, h, fullscreen)` で作るので、引数三つのコンストラクターも残します。`using System.Xml.Serialization;` はファイルにすでにあり、`Save`、`Load`、`Run` はそのままです。
```csharp
    // XmlSerializer が扱う型は public で、引数なしのコンストラクターと public set が必要。
    public sealed class GraphicsOptions
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public bool Fullscreen { get; set; }

        public GraphicsOptions() { }   // XmlSerializer が読むときに使う
        public GraphicsOptions(int width, int height, bool fullscreen) => (Width, Height, Fullscreen) = (width, height, fullscreen);
    }

    public sealed class GameOptions
    {
        public int Volume { get; set; } = 80;
        public GraphicsOptions Graphics { get; set; } = new(1280, 720, false);

        // XmlSerializer は IDictionary を扱えないので XML からは外し、下の配列で代わりに書く
        [XmlIgnore] public Dictionary<string, string> KeyBindings { get; set; } = [];

        [XmlArray("KeyBindings")]
        public KeyBinding[] KeyBindingList
        {
            get => [.. KeyBindings.Select(p => new KeyBinding { Action = p.Key, Key = p.Value })];
            set => KeyBindings = value.ToDictionary(b => b.Action, b => b.Key);
        }

        public sealed class KeyBinding
        {
            [XmlAttribute] public string Action { get; set; } = "";
            [XmlAttribute] public string Key { get; set; } = "";
        }
    }
```
またはこのファイルを System.Text.Json で保存します(record と Dictionary をそのまま扱えます)。
**教訓:** `XmlSerializer` は public な型、引数なしのコンストラクター、読み書きできる public メンバーだけを扱い、`IDictionary` は扱えません。型全体をコンストラクターで検査するので、「There was an error reflecting type」が出たら**内部例外を最後まで**読みます(ラボ 6 の `AggregateException` と同じ読み方)。

## Lab 60 — 既定の名前空間が付いた XML を接頭辞なしの XPath で探す
```
ウォッチ doc.SelectSingleNode("/quests"):            null
ウォッチ doc.DocumentElement.NamespaceURI:           "urn:bughunt:quests"
ウォッチ doc.SelectSingleNode("/*[local-name()='quests']"):   見つかる
本当の原因:  xmlns="urn:bughunt:quests" ですべての要素がその名前空間に属するようになったのに、XPath 1.0 の接頭辞なしの名前は「名前空間なし」だけを探す。
           RewardGold が見つからなかったの(null)を 0G に変えて黙って通した。
```
**修正:** 名前空間に接頭辞を付けて XPath で探す。`Lab60_QuestXml.cs` の `RewardGold` を次のものに置き換える。`using System.Xml;` はファイルにすでにあり、`Run` はそのまま。
```csharp
    static int RewardGold(XmlDocument doc, string questId)
    {
        var ns = new XmlNamespaceManager(doc.NameTable);
        ns.AddNamespace("q", "urn:bughunt:quests");
        XmlNode? reward = doc.SelectSingleNode($"/q:quests/q:quest[@id='{questId}']/q:reward", ns);
        string? gold = reward?.Attributes?["gold"]?.Value;
        return gold == null ? 0 : int.Parse(gold, CultureInfo.InvariantCulture);
    }
```
LINQ to XML なら `XNamespace q = "urn:bughunt:quests";` そして `root.Elements(q + "quest")`。
**教訓:** 属性は接頭辞がなければ名前空間に属さないので `@id` はそのまま使います。必須のデータが見つからなかったとき既定値(0)を返すと、この手のバグが隠れます。例外を出してください。(空白のテキストノード: `PreserveWhitespace = true` で読むとインデントもノードになり、`ChildNodes`/`FirstChild` が見当違いのノードを指します。要素だけ見るときは `SelectNodes`、`Elements()` を使います。)

## Lab 61 — 文字列連結で作った XML のエスケープ漏れ
```
(受信側) 読めないメッセージを破棄: Name cannot begin with the '3' character, hexadecimal value 0x33. Line 1, position N.
送った XML:   <invite guild="炎の連隊" from="ケイン"><message>レイド一緒に行こう <3</message></invite>
本当の原因:  BuildInvite がユーザー入力をそのまま XML の文法の中に入れる。< と & はテキストで、" は属性値で文書を壊す。
           受け取る側は XmlException を飲み込んで「届かない」にした。
```
**修正:** 構造を知る API に XML を作らせる。`Lab61_GuildInvite.cs` の `BuildInvite`(二行の式本体メソッド)を次のものに置き換える。`using System.Xml.Linq;` はファイルにすでにあり、`Receive` と `Run` はそのまま。
```csharp
    static string BuildInvite(string guild, string from, string message) =>
        new XElement("invite", new XAttribute("guild", guild), new XAttribute("from", from), new XElement("message", message)).ToString(SaveOptions.DisableFormatting);
```
`XmlWriter` でもよい。どうしても文字列にするなら値ごとに `SecurityElement.Escape`。
**教訓:** `<` だけ変える修正は隠しケースの `&`、`"` で引っかかります。同じミスが SQL インジェクション、HTML(XSS)、シェルコマンド、JSON 文字列の連結にもあります。構造を知る API に作らせてください。

### 参考: XML とセキュリティ (ラボなし)

- **XXE(外部エンティティ)**: .NET Core 以降 `XmlDocument`、`XDocument`、`XmlReader.Create`、`XmlSerializer` は既定で外部エンティティを解釈しません(`XmlResolver` が null、`DtdProcessing.Prohibit`)。信頼できない XML を受け取るとき `XmlReaderSettings { DtdProcessing = DtdProcessing.Parse, XmlResolver = new XmlUrlResolver() }` のように**既定値を緩めないでください**。.NET Framework 4.5.2 より前から移ってきたコードは確認が必要です。
- **エンティティ展開(Billion laughs)**: DTD を許さなければならないなら `MaxCharactersFromEntities` で制限します。
- **型名でオブジェクトを作る**: `BinaryFormatter`(.NET 9 で削除)、Newtonsoft.Json の `TypeNameHandling.All`、`XmlSerializer` に外から受けた型を渡すコードはリモートコード実行につながりえます。ラボ 56 の `JsonDerivedType` のように**許可リスト**を使ってください。

---

# 8部: 非同期と並行性

## Lab 62 — async void の例外は catch に来ない
```
止まった所:    SaveAsync の throw (スレッドウィンドウ: .NET TP Worker、呼び出し履歴に AutoSave/Run がない)
本当の原因:  SaveAsync は最初の await ですぐ戻る → try ブロックはすでに終わっている。
           async void なので失敗を伝える Task がなく、例外がスレッドプールで投げ直されてプロセスが死ぬ
```
**修正:** `async Task` に変えて待つ。`Lab62_AutoSave.cs` で三か所を変える。

1. `SaveSystem` の `SaveAsync` を次のものに置き換える(`async void` → `async Task`)。
```csharp
        public async Task SaveAsync(string slot)
        {
            await Task.Delay(50);
            if (slot == "checkpoint_3")
                throw new IOException(Text.L($"{slot}: 디스크 공간이 부족합니다", $"{slot}: ディスクの空き容量が不足しています"));
            Interlocked.Increment(ref SavedCount);
            Console.WriteLine(Text.L($"  {slot} 저장 완료", $"  {slot} セーブ完了"));
        }
```
2. `AutoSave` を、次の `AutoSaveAsync` に置き換える(`await` するので `async Task<int>` になる)。
```csharp
    static async Task<int> AutoSaveAsync(SaveSystem saves, string[] checkpoints)
    {
        int failures = 0;
        foreach (string checkpoint in checkpoints)
        {
            try
            {
                await saves.SaveAsync(checkpoint);   // 失敗はこの await で例外として出る
            }
            catch (IOException ex)
            {
                failures++;
                Console.WriteLine(Text.L($"  [알림] 저장 실패: {ex.Message}", $"  [通知] セーブ失敗: {ex.Message}"));
            }
        }
        return failures;
    }
```
3. `Run` の `int failures = AutoSave(...);` の行を次の一行に置き換える(コンソールの最上位なので同期で待っても問題ない)。
```csharp
        int failures = AutoSaveAsync(saves, ["checkpoint_1", "checkpoint_2", "checkpoint_3"]).GetAwaiter().GetResult();
```
**教訓:** `async void` は呼び出し元が**終わったかも、失敗したかも分からない。** イベントハンドラーのようにシグネチャが決まっている場合にだけ使う。

## Lab 63 — `.Result` デッドロック
```
すべて中断 → 並列スタック:
    Main Thread (ゲームループ):  Task.Result → ... → Lab63.OnEnterLoadingScreen  (詰まっている)
タスクウィンドウ:     LoadAssetsAsync  待機中 (最初の await のあと続ける順番を待っている)
main.PendingCount:  1  ← LoadAssetsAsync の残りがメインスレッドのキューに並んでいる
本当の原因:  メインスレッドが .Result で詰まっているのに、await の残りはそのメインスレッドでしか実行できない
```
**修正:** 最後まで async にする(推奨)。`Lab63_LoadingScreen.cs` で二か所を変える。

1. `OnEnterLoadingScreen` を、次の `OnEnterLoadingScreenAsync` に置き換える(`.Result` の代わりに `await`)。
```csharp
    static async Task OnEnterLoadingScreenAsync(LoadingScreen screen)
    {
        screen.Show();
        int loaded = await LoadAssetsAsync();   // スレッドを塞がずに待つ。続きはメインスレッドで実行される
        screen.Close(loaded);
    }
```
2. `Run` の `Task frame = main.Run(() => OnEnterLoadingScreen(screen));` の行を次の一行に置き換える。
```csharp
        Task frame = main.Run(() => OnEnterLoadingScreenAsync(screen));
```
別の方法: ライブラリ側の `LoadAssetsAsync` の中を `await Task.Delay(20).ConfigureAwait(false);` にする。メインスレッドに戻らないのでデッドロックは解ける。ただし、そのあとでメインスレッド専用のオブジェクトを触るとラボ 67 のバグになる。

**教訓:** SynchronizationContext のあるスレッドで非同期の作業を**同期で待つと**デッドロックになる。コンソールの既定スレッドにはコンテキストがないので再現されず、コンソールで試したコードが UI/ゲームで止まる。

## Lab 64 — await 漏れ (fire-and-forget)
```
Expect の行のタスクウィンドウ:  SubmitAsync 3個、すべて「待機中」
Interlocked.Increment のブレークポイント:  Expect より後に引っかかる (またはプロセスが先に終わって引っかからない)
本当の原因:  _ = server.SubmitAsync(id); で Task を捨てている。誰も終わるのを待っていない
```
**修正:** 送った Task を待つ。`Lab64_Achievements.cs` で二か所を変える。

1. `SubmitUnlocked` を、次の `SubmitUnlockedAsync` に置き換える(`_ =` の代わりに `await`。ログの行はそのまま残す)。
```csharp
    static async Task SubmitUnlockedAsync(AchievementServer server, IEnumerable<string> unlocked)
    {
        foreach (string id in unlocked)
        {
            await server.SubmitAsync(id);
            Console.WriteLine(Text.L($"  업적 전송: {id}", $"  実績送信: {id}"));
        }
    }
```
2. `Run` の `SubmitUnlocked(server, [...]);` の行を次の一行に置き換える。
```csharp
        SubmitUnlockedAsync(server, ["first_blood", "no_damage", "speed_run"]).GetAwaiter().GetResult();
```
同時に送ってよいなら、1. のループの代わりに `await Task.WhenAll(unlocked.Select(server.SubmitAsync));`(ログの行はそのあと別に出す)。
**教訓:** Task を返されたら**誰かは待たなければならない。** `_ =` はその責任をわざと捨てるという印だ。CS4014 を切るために付けてはいけない。

## Lab 65 — `await Task.WhenAll` は最初の例外だけ投げる
```
throw のブレークポイント:  二回引っかかる (sounds.pak、locale.pak)
catch の中の ex:  IOException "sounds.pak ダウンロード失敗" 一つ
downloads[3].Status  Faulted、downloads[3].Exception  locale.pak
本当の原因:  WhenAll の Task は例外二つを AggregateException で持っているが、await は最初のものだけ取り出して投げる
```
**修正:** catch の中で各 Task を確認する。`Lab65_PatchDownload.cs` の `DownloadAllAsync` の `catch (Exception ex)` ブロック(閉じ括弧まで)を次のものに置き換える。
```csharp
        catch (Exception)
        {
            foreach (Exception ex in downloads.Where(d => d.IsFaulted).Select(d => d.Exception!.InnerException!))
            {
                Console.WriteLine(Text.L($"  [로그] {ex.Message}", $"  [ログ] {ex.Message}"));
                failed.Add(ex.Message.Split(' ')[0]);
            }
        }
```
または `Task all = Task.WhenAll(downloads); try { await all; } catch { foreach (var ex in all.Exception!.InnerExceptions) ... }`。
**教訓:** 例外は消えず**Task の中に**残っている。複数の作業の失敗をすべて扱うには Task を直接見る。

## Lab 66 — lock なしの競合状態
```
結果:     実行するたびに違う値 (例: 打撃 1,0xx,xxx / 期待 2,000,000)
並列スタック:  Record の中にスレッド4個が同時にいる
本当の原因:  TotalDamage += damage と Hits++ は読み-変更-書きの三段階なので、二つのスレッドが重なると片方の足し算が消える
```
**修正:** 読み-変更-書きを不可分にする。`Lab66_DamageMeter.cs` の `DamageMeter.Record` を次のものに置き換える。
```csharp
        public void Record(int damage)
        {
            Interlocked.Add(ref TotalDamage, damage);
            Interlocked.Increment(ref Hits);
        }
```
別の方法:
- `lock (_gate) { TotalDamage += damage; Hits++; }` (二つの値を**一緒に**一貫して変えなければならないなら lock。`readonly object _gate = new();` を `DamageMeter` に足す)
- パーティーメンバーごとにローカルの合計を出し、最後に一度だけ足す (`Parallel.ForEach` の localInit/localFinally のオーバーロード)。一番速い。

**教訓:** 結果が実行ごとに違うなら競合状態からまず疑う。ブレークポイントを置くとタイミングが変わってかえって見えなくなることがある。

## Lab 67 — ConfigureAwait(false) のあとでメインスレッド専用のオブジェクトを触る
```
RefreshHudAsync の最初の行:  スレッド = Main Thread (ゲームループ)、SynchronizationContext.Current = MainThread
hud.SetGold の行:         スレッド = .NET TP Worker、SynchronizationContext.Current = null
本当の原因:  ConfigureAwait(false) のせいで await のあとがメインスレッドに戻らない
```
**修正:** このメソッドでは `ConfigureAwait(false)` を消す。`Lab67_HudRefresh.cs` の `RefreshHudAsync` の `Profile profile = await FetchProfileAsync().ConfigureAwait(false);` の行を次の一行に置き換える。メインスレッドに戻らなければならないコード(UI、ゲームオブジェクト)は既定の動作(コンテキストのキャプチャ)を使う。
```csharp
        Profile profile = await FetchProfileAsync();
```
**教訓:** `ConfigureAwait(false)` は**コンテキストが要らないライブラリコード**用だ。ラボ 63 ではデッドロックを解く道具だったが、そのあとでメインスレッド専用のオブジェクトを触るとバグになる。実際の WinForms/WPF/Unity ではここで例外が出たり画面が時々壊れたりする。

## Lab 68 — CancellationToken を渡さない
```
SearchRegionAsync (region == 10):
    token.IsCancellationRequested  false
    token.CanBeCanceled            false   ← default(CancellationToken)。絶対にキャンセルされないトークン
FindMatchAsync フレームの token:   IsCancellationRequested true
本当の原因:  FindMatchAsync が SearchRegionAsync(region) で呼ぶとき token を抜かした。既定値 = default なのでコンパイラも黙っている
```
**修正:** トークンを渡す。`Lab68_Matchmaking.cs` の `FindMatchAsync` の `await SearchRegionAsync(region);` の行を次の一行に置き換える。
```csharp
            await SearchRegionAsync(region, token);
```
できれば `CancellationToken` の引数に既定値を置かず、抜かしたらコンパイルエラーになるようにする。
**教訓:** キャンセルは**鎖のすべての輪**にトークンを渡して初めて伝わる。`CanBeCanceled == false` は「誰かがトークンを渡さなかった」という合図だ。CA2016 がこれを捕まえる。

## Lab 69 — 例外を飲み込んで成功を返す
```
段階1のコンソール:  [ログ] 倉庫アップロード警告: 英雄の証: 帰属アイテムは倉庫に入れられません
             英雄の証 保管完了          ← 失敗したのに「完了」
最初の例外で中断(InvalidOperationException) → F10:  catch (Exception) に入ってログだけ出して return true
本当の原因:  UploadToStorageAsync がすべての例外を捕まえて無条件に true。呼び出し元は「預けた」と信じてバッグから外す。
           症状(アイテムの消失)は段階2でバッグが保存され、段階3で読み直すときに初めて見える
```
**修正:** 対処の仕方を知っている例外だけ捕まえ、失敗を呼び出し元に返す。`Lab69_Storage.cs` の `UploadToStorageAsync` を丸ごと次のものに置き換える。メソッドの最後にあった `return true;` はなくなる(`try` と `catch` がそれぞれ値を返すので、残すと到達できないコードの警告 CS0162 がエラーになる)。`DepositAllAsync` と `Run` はそのまま。
```csharp
    static async Task<bool> UploadToStorageAsync(StorageServer server, string item, int count)
    {
        try
        {
            await server.UploadAsync(item, count);
            return true;
        }
        catch (InvalidOperationException ex)   // サーバーが拒否した: バッグにそのまま残す
        {
            Console.WriteLine(Text.L($"  [로그] 창고 업로드 거절: {ex.Message}", $"  [ログ] 倉庫アップロード拒否: {ex.Message}"));
            return false;
        }
    }
```
他の例外(ネットワーク切断、コードのバグ)は捕まえず上に送る。そこで捕まえるかは、それを処理できる側が決める。
**偽の修正:** `if (item == Text.L("영웅의 증표", "英雄の証")) return false;` のように見えているアイテムだけ塞ぐと基本ケースは通るが、隠しケースは別のアイテムが帰属アイテムなので失敗する。`catch (Exception)` をそのままにして `return false` にだけ変えると、このラボは通るが、コードのバグまで「保管失敗」に変えてしまう同じ匂いが残る。
**教訓:** `catch (Exception) { ログ }` のあとに成功値を返すと、失敗が**別の段階の別の症状**に変わって現れる。ログ一行は処理ではない。捕まえる例外は絞り、失敗は戻り値か例外で呼び出し元に知らせる。

## Lab 70 — タイムアウトを「ユーザーがキャンセルした」として処理
```
最初の例外で中断(TaskCanceledException)、Mira(サーバーの応答なし):
    userCancel.IsCancellationRequested       false
    timeout.Token.IsCancellationRequested    true    ← CancelAfter(300) がキャンセルした
    ex.CancellationToken                     連結したトークン (userCancel ではない)
本当の原因:  catch (OperationCanceledException) 一つがユーザーのキャンセルとタイムアウトを両方「キャンセルされた」に変える。
           だから EnterAsync の「タイムアウトなら予備チャンネル」の分岐が一度も実行されない
```
**修正:** 例外フィルターで「誰がキャンセルしたか」を区別する。`Lab70_DungeonEntry.cs` の `TryEnterAsync` の `catch (OperationCanceledException)` ブロック(閉じ括弧まで)を、次の二つの catch に置き換える。`EnterAsync` と `Run` はそのまま。
```csharp
        catch (OperationCanceledException) when (userCancel.IsCancellationRequested)
        {
            return Text.L("취소됨", "キャンセル済み");      // プレイヤーが押した
        }
        catch (OperationCanceledException)
        {
            return "timeout";   // 自分がかけた CancelAfter。画面に出ない内部の目印なので翻訳しない (EnterAsync がこの値を見て予備チャンネルで再試行する)
        }
```
タイムアウトを `throw new TimeoutException(...)` に変えて投げ、`EnterAsync` で `catch (TimeoutException)` で受けてもよい。
`when (ex.CancellationToken == userCancel)` は**間違った**修正だ。例外に乗ったトークンは連結したトークンなのでこの条件は常に偽になり、本当のユーザーのキャンセルがタイムアウトに変わる。
**偽の修正:** 時間制限を延ばしたりなくしたりすると、応答のないサーバーを延々と待つ。名前で例外を置くと(`Mira`)隠しケースで別のプレイヤーが引っかかる。
**教訓:** `OperationCanceledException` は「キャンセルされた」だけを言い、**誰が**キャンセルしたかは言わない。トークンを複数つないだなら `catch ... when (自分のトークン.IsCancellationRequested)` で区別する。例外フィルターは条件が偽ならその catch を飛ばし、スタックを巻き戻す前に評価されるので、最初の例外で中断と一緒に使いやすい。

## Lab 71 — 捕まえた例外をあとで `throw ex;` で投げ直す
```
リトライのログ:  [再試行 3/3] FormatException: The input string '二百' was not in a correct format.
レポート:       FormatException @ RetryAsync          ← 型は合っているのに位置がリトライの関数
最初の例外で中断(FormatException):  呼び出し履歴の一番上の BugHunt フレームは ParseRewardAsync
オブジェクト ID で同じ例外だと確認し throw last! の行を F10:  StackTrace が RetryAsync から新しく始まる
本当の原因:  throw last; はスタックトレースを「今この行」から書き直す。catch ブロックの外なので throw; も使えない
```
**修正:** 元のスタックトレースのまま投げ直します。`Lab71_QuestSync.cs` の `RetryAsync` を丸ごと次のものに置き換えます(`Exception? last = null;` と `throw last!;` はなくなります)。ファイル先頭の `using System.Text.RegularExpressions;` の隣に `using` を二行追加します。`Run` と `Report` はそのままです。
```csharp
using System.Diagnostics;                 // UnreachableException
using System.Runtime.ExceptionServices;   // ExceptionDispatchInfo

    static async Task<T> RetryAsync<T>(Func<Task<T>> body)
    {
        ExceptionDispatchInfo? last = null;
        for (int attempt = 1; attempt <= Attempts; attempt++)
        {
            try
            {
                return await body();
            }
            catch (Exception ex)
            {
                last = ExceptionDispatchInfo.Capture(ex);   // この瞬間のスタックトレースも一緒に持っておく
                Console.WriteLine(Text.L($"  [재시도 {attempt}/{Attempts}] {ex.GetType().Name}: {ex.Message}", $"  [再試行 {attempt}/{Attempts}] {ex.GetType().Name}: {ex.Message}"));
                await Task.Delay(5);
            }
        }
        last!.Throw();
        throw new UnreachableException();   // コンパイラ用。Throw() は戻ってこない
    }
```
`ExceptionDispatchInfo.Throw(lastException)`(静的メソッド)も同じです。または最後の試行では捕まえません: `catch (Exception ex) when (attempt < Attempts)`。そうすれば最後の例外は手をつけずにそのまま上がります。
**偽の修正:** `throw new Exception(last.Message, last)` で包むと例外の型が変わり、位置は依然として `RetryAsync` だ。レポートに関数名を埋め込むと隠しケース(ヘッダー破損、ダウンロード失敗)で間違う。
**教訓:** 例外を保存して**あとで、別の所で**投げるとき(リトライ、キュー、`await` の境界を越えたコールバック)は `ExceptionDispatchInfo` を使う。`await` が失敗した Task の例外を投げ直すとき元のスタックが生きているのも同じ仕組みのおかげだ。ラボ 7 の `throw ex;` と同じ根だが、`throw;` では直せない点が違う。

---

# 9部: リソースとメモリ

## Lab 72 — Dispose しなかった StreamWriter
```
SaveReplay の最後の行:
    new FileInfo(path).Length    0
    writer.BaseStream.Position   1024 (StreamWriter が FileStream のバッファに渡した量。ディスクにはまだ 0)
本当の原因:  StreamWriter/FileStream はバッファに溜めて Flush/Dispose のときに書く。writer を Dispose しないのでバッファがディスクに出ない。
           ハンドルもプロセスが終わるまで開いたまま
```
**修正:** writer を必ず Dispose する。`Lab72_Replay.cs` の `SaveReplay` の `var writer = new StreamWriter(path);` の行を次の一行に置き換える。
```csharp
        using var writer = new StreamWriter(path);
```
`using (...) { }` ブロックや、ループごと `File.WriteAllLines(path, frames)` にしてもよい。
**教訓:** `IDisposable` を実装したオブジェクトは**必ず Dispose** する。GC はメモリを回収するだけで、バッファをフラッシュしたりファイルを閉じたりする時点は保証しない。.NET は終了するときファイナライザーも回さない。

## Lab 73 — static イベントが閉じたウィンドウをつかむ
```
メモリスナップショット:  CombatLogWindow 50個、それぞれ 256KB のテクスチャキャッシュ
ルートへのパス:     CombatLogWindow ← Action<string,int> (デリゲートの Target) ← Lab73.GameEvents.DamageDealt (static フィールド)
本当の原因:     Open で += で購読し、Close で -= で解除していない。
              イベント(デリゲート)が購読者を参照し、static フィールドはプログラムが終わるまで GC ルート
```
**修正:** 閉じるときに購読を解除する。`Lab73_CombatLogWindow.cs` の `CombatLogWindow.Close` を次のものに置き換える。
```csharp
        public void Close()
        {
            IsOpen = false;
            _lines.Clear();
            GameEvents.DamageDealt -= OnDamageDealt;   // static イベントがウィンドウをつかまないように
        }
```
ウィンドウの寿命が明確なら `IDisposable` にして `Dispose` で解除する。
**教訓:** 「参照を全部捨てたのにメモリが減らない」なら**誰がまだつかんでいるか**を探す。メモリスナップショットのルートへのパスや `dotnet-dump` の `gcroot` がその鎖を見せる。長く生きるオブジェクト(static、シングルトン)のイベントに短く生きるオブジェクトが購読する構造が、一番よくある漏れだ。

## Lab 74 — ファイナライザーに資源の返却を任せる
```
~Texture() のブレークポイント:   Exit() の間に引っかからない。プログラムが終わるまでも引っかからない
GC.CollectionCount(0)  0   ← GC が一度も回っていない
(ウォッチで GC.Collect() を実行後) ~Texture() が「ファイナライザースレッド」(.NET Finalizer)で引っかかる
本当の原因:  ファイナライザーは C++ のデストラクターではない。オブジェクトが要らなくなった瞬間ではなく、GC が見つけたあとファイナライザースレッドの手が空いたときに呼ばれる
```
**修正:** `IDisposable` を実装し、レベルを出るとき明示的に Dispose します。`Lab74_TextureCache.cs` で二か所を変えます。元のファイナライザー(`~Texture() { GpuMemory.Free(Bytes); }`)は消し、下の安全網のファイナライザーに置き換えます。`GpuMemory`、`Level.Enter`、`Run` はそのままです。

1. `Texture` クラスを丸ごと次のものに置き換えます。
```csharp
    class Texture : IDisposable
    {
        bool _disposed;

        public string Name { get; }
        public long Bytes { get; }

        public Texture(string name, long bytes)
        {
            Name = name;
            Bytes = bytes;
            GpuMemory.Allocate(bytes);
        }

        // レベルを出るときすぐに GPU メモリを返す。二回呼ばれても一回だけ返す。
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            GpuMemory.Free(Bytes);
            GC.SuppressFinalize(this);
        }

        ~Texture() => Dispose();   // 安全網: Dispose を忘れたときだけ、いつか
    }
```
2. `Level` クラスの `Exit` を次のものに置き換えます。
```csharp
        public void Exit()
        {
            foreach (Texture texture in _textures)
                texture.Dispose();
            _textures.Clear();
            Console.WriteLine(Text.L($"  {name} 퇴장: GPU {GpuMemory.BytesInUse / (1024 * 1024)}MB", $"  {name} 退場: GPU {GpuMemory.BytesInUse / (1024 * 1024)}MB"));
        }
```
(実務では非管理ハンドルを直接ファイナライザーで扱うより `SafeHandle` を使う。)
**教訓:** 「決まった瞬間」に返さなければならない資源は**Dispose**で。ファイナライザーは忘れたときのための安全網にすぎず、いつ呼ばれるか(そもそも呼ばれるか)保証されない。`GC.Collect()` で無理に回すのも解決策ではない。

---

# 10部: unsafe とネイティブメモリ

## Lab 75 — メソッドが終わった stackalloc メモリを指すポインター
```
FindPath の return の行:          path[0] == 3, path[1] == 4   ← ここまでは合っている
DrawMinimapBackground のあと:    path[0] == -1 またはゴミの値
(long)path と &pixels[0]:      同じスタック領域(数十バイトの差)
本当の原因:  stackalloc メモリは FindPath のスタックフレームの中にある。FindPath が戻ると、その場所は次の呼び出しが上書きする。
           Span<int> で返そうとして CS8352 で防がれたので、ポインターに変えてコンパイラのチェックを避けた。
```
**修正:** 長く生きるべき側(呼び出し元)がバッファを作って `Span` で渡す。`Lab75_MinimapPath.cs` で二か所を変える。

1. `FindPath` を次のものに置き換える(上の `[MethodImpl(MethodImplOptions.NoInlining)]` の行はそのまま)。
```csharp
    static int FindPath(int startTile, int goalTile, Span<int> path)
    {
        int step = startTile < goalTile ? 1 : -1;
        int length = 0;
        for (int tile = startTile; tile != goalTile + step; tile += step)
            path[length++] = tile;
        return length;
    }
```
2. `Run` の `int* path = FindPath(start, goal, out int length);` の行を次の二行に置き換える。`path[i]` で読むほかの行はそのまま動く。
```csharp
        Span<int> path = stackalloc int[MaxPathLength];   // Run のスタックフレームにあるので Run が終わるまで有効
        int length = FindPath(start, goal, path);
```
(サイズが大きいか決まっていないなら `int[]` や `ArrayPool` を使う。)
**教訓:** コンパイラが `Span` を防ぐのは**寿命のチェック**だ。ポインター、`Unsafe.AsRef`、`MemoryMarshal.CreateSpan` でそのチェックを回避すると、C++ の「ローカル変数のアドレスを返す」とまったく同じバグになる。値を壊すのは読む瞬間ではなく**その間の別の呼び出し**なので、ブレークポイントを置く位置によっては無事に見えることもある。

## Lab 76 — fixed ブロックの外に持ち出したポインターを GC の圧縮のあとで使う
```
Attach の行の (long)p:              最初のアドレス
LoadLevel のあとの Samples のアドレス:       別のアドレス   ← 配列が移された
MixFrame の (long)_buffer:          最初のアドレスのまま
本当の原因:  fixed は「ブロックの中だけ」配列を固定する。ブロックが終わったあと GC.Collect(compacting) が配列を移し、
           オーディオ装置は古いアドレス(今は空の所)を読み続ける。
```
**修正:** 最初から移されないヒープ(POH)に作る。`Lab76_SoundMixer.cs` の `Mixer` の `Samples` プロパティの行を次の一行に置き換える。
```csharp
        public short[] Samples { get; } = GC.AllocateArray<short>(32, pinned: true);
```
別の方法:
- `GCHandle.Alloc(Samples, GCHandleType.Pinned)` で固定し、装置から外すとき `Free` (ラボ 77 参照)
- ネイティブが長く使うバッファはいっそ `NativeMemory.Alloc` で GC の外に作り `NativeMemory.Free` で返す

**教訓:** 管理参照は GC が一緒に直してくれるが、**ポインターは GC が知らない数字**だ。ネイティブ側が「アドレスを覚えておく」API なら `fixed` ではだめだ。実際には GC がいつ圧縮するかによって、長い間無事だったのに弾けるバグだが、このラボは `GC.Collect(..., compacting: true)` でその瞬間を固定してある。

## Lab 77 — 例外経路で GCHandle.Free の漏れ
```
生き残ったバッファの数:     2  == 壊れたパケットの数
throw の行で F10:      handle.Free() に行かず catch に抜ける
メモリスナップショットのルートへのパス: Byte[] ← 固定ハンドル(Pinned Handle)
GC.GetGCMemoryInfo().PinnedObjectsCount:  壊れたパケットの数だけ増えている
本当の原因:  チェックサムが違うと throw で抜け出しながら handle.Free() を飛ばす
```
**修正:** 固定はネイティブ呼び出しの間だけにし、`finally` で必ず解きます。`Lab77_PacketReceive.cs` の `ReceivePacket` を丸ごと次のものに置き換えます。元は `return buffer;` の直前にあった `handle.Free();` は消します(二回解くと `InvalidOperationException` が出ます)。`using System.Runtime.InteropServices;` はファイルにすでにあり、`ReceiveAll` と `Run` はそのままです。
```csharp
    static byte[] ReceivePacket(int packetId, bool corrupt)
    {
        var buffer = new byte[256];
        Tracked.Add(new WeakReference(buffer));

        GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            NativeNet.Receive(handle.AddrOfPinnedObject(), buffer.Length, packetId, corrupt);
        }
        finally
        {
            handle.Free();   // 例外が出ても必ず解く
        }

        byte sum = 0;
        for (int i = 0; i < buffer.Length - 1; i++)
            sum += buffer[i];
        if (sum != buffer[^1])
            throw new InvalidDataException(Text.L($"패킷 {packetId} 체크섬 불일치", $"パケット {packetId} チェックサム不一致"));

        return buffer;
    }
```
短くだけ固定するなら `fixed (byte* p = buffer) { ... }` の方が簡単です (ブロックをどう抜けても解けます)。
**教訓:** `GCHandle` は GC の外の強い参照なので、`Free` を忘れると**GC があっても漏れ**だ。Pinned ハンドルはその上ヒープの圧縮も妨げる。得たら `finally` で返す。`lock`、セマフォ(ラボ 87)と同じ規則だ。

## Lab 78 — ネイティブに渡したコールバックのデリゲートが GC に回収される
```
コンソール(デバッガーなし):  Process terminated.
                    A callback was made on a garbage collected delegate of type 'BugHunt!BugHunt.Lab78+BeatCallback::Invoke'.
F5:                 PlayBeat の _onBeat(beat) の行で止まる
本当の原因:  GetFunctionPointerForDelegate(OnBeat) に渡したデリゲートはその場で作られた一時オブジェクトだ。
           ネイティブ側は関数ポインター(数字)だけを持っているので GC はデリゲートをゴミと見なし、LoadSong の GC.Collect が回収する。
           デリゲートと一緒にネイティブの入口(thunk)も塞がれ、次の拍でランタイムがプロセスを終わらせる。
```
**修正:** ネイティブがコールバックを持っている間、デリゲートをフィールドにつかんでおく。`Lab78_BeatCallback.cs` の `RhythmGame.Start` を次のもの(フィールド一つ + `Start`)に置き換える。`OnBeat` と `Run` はそのまま。
```csharp
        BeatCallback? _beatCallback;   // ネイティブが持っている間、生きているように

        public void Start()
        {
            _beatCallback = OnBeat;
            NativeAudio.SetBeatCallback(Marshal.GetFunctionPointerForDelegate(_beatCallback));
            Console.WriteLine(Text.L("  오디오 엔진에 박자 콜백 등록", "  オーディオ エンジンにビート コールバックを登録"));
        }
```
別の方法: デリゲートをいっそ使わない。`[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])] static int OnBeat(int beat)` を作って `&OnBeat` を渡す。(static なのでゲームオブジェクトは別に探さなければならない。`GCHandle.ToIntPtr` で作った「ユーザーデータ」ポインターを一緒に渡す方式がよくある。)

`GC.KeepAlive(callback)` は**その行までしか**生かさないので、あとで呼ばれるコールバックには役立たない。
**教訓:** .NET Framework で MDA `CallbackOnCollectedDelegate` として有名だったバグだ。今の .NET は回収されたデリゲートの入口を塞いでおくので、見当違いの所へ飛ばず**理由を書いて即座に終わる。** そのメッセージの型名が犯人だ。

## Lab 79 — ArrayPool に返却した配列を Memory で使い続ける
```
history.Add のブレークポイントごとの history[0]:  "[Kael] やあ" → 次のメッセージで上書きされた値 → ...   ← 毎回変わる
オブジェクト ID:   FormatMessage の buffer が毎回同じ $1
本当の原因:  Return(buffer) のあとも buffer.AsMemory(...) を返して記録に残す。
           同じスレッドでまた Rent すると、今返したその配列が出てきて、新しいメッセージが古い記録の上に上書きされる。
           ReadOnlyMemory/Span は配列の「窓」であってコピーではない。
```
**修正:** 長く置くデータは返却する前にコピーする。`Lab79_ChatHistory.cs` の `FormatMessage` を次のものに置き換える。`Run` はそのまま。
```csharp
    static ReadOnlyMemory<char> FormatMessage(string sender, string text)
    {
        char[] buffer = ArrayPool<char>.Shared.Rent(128);
        int length = 0;
        foreach (string part in new[] { "[", sender, "] ", text })
        {
            part.CopyTo(buffer.AsSpan(length));
            length += part.Length;
        }
        string message = new(buffer, 0, length);   // 返却する前にコピーする
        ArrayPool<char>.Shared.Return(buffer);
        return message.AsMemory();
    }
```
(もっと簡単には `string.Concat` で string をすぐ作る。プールは**そのメソッドの中だけ**で使う一時バッファに使う。)
**教訓:** `Return` は「もう他の人が使ってよい」という約束だ。返却した配列を指す `Span`、`Memory`、スライスが残っていると解放後使用(use-after-free)と同じだ。クラッシュは出ず**他人のデータが見えるだけ**なので、かえって見つけにくい。

## Lab 80 — 構造体レイアウトのパディング
```
header:   Type 7、Length とても大きい数(または負の数)、Seq 見当違いの値
Unsafe.SizeOf<PacketHeader>():   12   (1 + 4 + 2 = 7 ではない)
Marshal.SizeOf<PacketHeader>():  12
メモリウィンドウ(&header):  Type の次に空の3バイト、Seq の後ろに空の2バイト
本当の原因:  Sequential レイアウトは int を4バイト境界に合わせようと Type の後ろに3バイトを挟む。
           だから Length を1番目ではなく4番目のバイトから読み、本文も7ではなく12番目のバイトから切る。
```
**修正:** 隙間なく詰める(サイズ 7)。`Lab80_PacketHeader.cs` の `PacketHeader` の上の `[StructLayout(LayoutKind.Sequential)]` の行を次の一行に置き換える。
```csharp
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
```
別の方法: 構造体を使わず位置を直接決めて読む (エンディアンも明示されてより安全。`using System.Buffers.Binary;` が必要)。読む部分は次のようになる。
  ```csharp
  byte type = packet[0];
  int length = BinaryPrimitives.ReadInt32LittleEndian(packet[1..]);
  ushort seq = BinaryPrimitives.ReadUInt16LittleEndian(packet[5..]);
  string text = Encoding.UTF8.GetString(packet.Slice(7, length));
  ```

**教訓:** バイト形式の決まったデータ(ネットワーク、ファイル、ネイティブ構造体)を構造体でそのまま読むときは**サイズとオフセットをまず確認**する。`Marshal.SizeOf`(マーシャリングのサイズ)と `Unsafe.SizeOf`/`sizeof`(メモリのサイズ)は違うこともある。たとえば `bool` フィールドはメモリでは1バイトだがマーシャリングすると4バイト(Win32 `BOOL`)だ。


## Lab 81 — 入力サイズで `stackalloc` してスタックオーバーフロー
```
ウォッチ packet.Length:  20000000 (入力から来た値)
Checksum の最初の行を F10:  プログラムが消える。[FAIL] も例外ウィンドウもない。採点結果 CRASH (0xC00000FD)
本当の原因:  stackalloc byte[n] はスタックに n バイトを取る。スタックはふつう 1 MB 固定なので、
           数百万バイトを取ると StackOverflowException。この例外は catch で捕まえられずプロセスを即座に終わらせる。
```
**修正:** `stackalloc` のサイズは小さく固定された上限(`StackCap`)の中でだけ使い、大きい入力はヒープ(`ArrayPool<byte>`)から借ります。`Lab81_PacketBuffer.cs` で二か所を変えます。`Run` はそのままです。

1. ファイルの先頭(`namespace BugHunt;` の上)に次の一行を追加します。
```csharp
using System.Buffers;
```
2. `Checksum` を次のもの(定数 `StackCap` + `Checksum`)に丸ごと置き換えます。
```csharp
    const int StackCap = 1024;   // スタックに取ってよい最大サイズ

    static long Checksum(byte[] packet)
    {
        byte[]? rented = packet.Length <= StackCap ? null : ArrayPool<byte>.Shared.Rent(packet.Length);
        try
        {
            Span<byte> scratch = rented is null ? stackalloc byte[StackCap] : rented;
            scratch = scratch[..packet.Length];
            packet.CopyTo(scratch);
            long sum = 0;
            foreach (byte b in scratch) sum += b;
            return sum;
        }
        finally
        {
            if (rented is not null) ArrayPool<byte>.Shared.Return(rented);
        }
    }
```
(単に `Span<byte> scratch = packet.Length <= StackCap ? stackalloc byte[StackCap] : new byte[packet.Length];` でヒープに取ってもかまいません。`OutOfMemoryException` は捕まえられる例外です。)
**教訓:** `stackalloc` のサイズは**必ずコンパイル時に分かる小さい上限**の中に収めてください。入力・反復回数・再帰の深さのいずれであれ、スタックを無制限に使えばオーバーフローです。ヒープの `OutOfMemoryException` はまだ捕まえられますが、スタックオーバーフローは捕まえられません。

## Lab 82 — `unsafe` ポインターの書き込みがバッファを越えて隣のメモリを上書きする
```
ブレークポイント p[i] = incoming[i]、i == 4:  scores は 4 マス(0〜3)なのに p[4] に書く
ウォッチ block[4] (ガード):             書き込み直後に 0xC0FFEE → 50 に変わる
本当の原因:  ポインター p[i] は範囲検査をしない。p[4] はバッファのすぐ後ろ(ガードのマス)を上書きする。
           同じ場所を安全なインデクサー scores[4] で書けば IndexOutOfRangeException が出てすぐ分かる。
```
**修正:** ポインターで書くときはバッファの容量でループを制限します。`Lab82_ComboBuffer.cs` の `Fill` の `fixed` ブロックの中の `for (int i = 0; i < incoming.Length; i++)` の行を次の一行に置き換えます。
```csharp
            for (int i = 0; i < incoming.Length && i < scores.Length; i++)
```
またはポインターを使わず安全な `Span` のインデクサーを使い、あふれる入力は切り捨てるか拒否する方針を明示します。
**教訓:** `unsafe` ポインター、`Marshal`、`Unsafe.Add` でメモリを扱うときは範囲検査が消えます。一マス越えただけでも例外ではなく**静かな破壊**になり、その被害はまったく別の所で現れて追跡が困難です。性能のために `unsafe` がどうしても必要なのでなければ、`Span<T>`/`stackalloc` の安全なインデクサーを使ってください。

### 参考: ネイティブヒープの破壊 (ラボなし)
- `Marshal.AllocHGlobal` + `Marshal.Copy` でネイティブヒープを越えて書くと、CLR のヒープや別の割り当てを壊しうります。ただしその結果(クラッシュか、静かな破壊か、どこが壊れるか)がランタイムとヒープの状態によって変わり**決定的でない**ため、ラボには入れていません。
- `AllocHGlobal` のあと `FreeHGlobal` を忘れるとネイティブメモリのリークです。これは9部(リソース・メモリ)のリークのラボと主題が重なります。

---

# 11部: ハング - 無限ループとデッドロック

## Lab 83 — 実数の累積と `!=` 終了条件
```
すべて中断 → ラボ本体のスレッド:  ZoomTo の for の行
zoom:    目標(2.0)をはるかに過ぎた値 (大きくなり続け、ある大きさからは 0.1 を足しても変わらない)
(double)zoom (2.0 近く):  1.9999998807907104 の次が 2.0999999046325684
本当の原因:  0.1f を累積した値は 2.0f を正確に踏まない。zoom != target は永遠に真。
```
**修正:** 反復回数を整数で決め、値はその整数で計算する。`Lab83_CameraZoom.cs` の `ZoomTo` を次のものに置き換える。`Run` はそのまま。
```csharp
    static void ZoomTo(Camera camera, float target, float step)
    {
        float start = camera.Zoom;
        int frames = (int)MathF.Round((target - start) / step);   // 何フレームかを整数で一度だけ決める
        for (int frame = 0; frame < frames; frame++)
        {
            camera.Zoom = start + frame * step;
            camera.Render();
        }
        camera.Zoom = target;
    }
```
`zoom < target` にだけ変えると止まりはしないが、累積誤差のためフレームが一つ多く描かれることがある (`1.9999998 < 2.0`)。
**教訓:** ラボ 13 と同じ原因(実数の等値比較)が**終了条件**に入ると、「間違った値」ではなく「終わらない」になる。実数は範囲(`<`)で比較するか、反復自体は整数で数える。

## Lab 84 — byte ループ変数のあふれ
```
すべて中断を何度か:  written は大きくなり続けるのに slot はいつも 0〜255
条件付きブレークポイント slot == 255 → F10:  slot == 0
本当の原因:  byte は 255 の次が 0 になる(unchecked)。items.Count が 256 以上なら slot < items.Count が永遠に真。
```
**修正:** ループ変数を比較対象と同じ `int` にする。`Lab84_InventorySave.cs` の `SaveInventory` の `for (byte slot = 0; slot < items.Count; slot++)` の行を次の一行に置き換える。
```csharp
        for (int slot = 0; slot < items.Count; slot++)
```
セーブ形式が本当にスロット番号を1バイトでしか書けないなら、保存する前に個数を検査して形式を上げるかエラーを出す。
**教訓:** ループ変数は**比較対象と同じ型**で。同じ罠: `for (uint i = n - 1; i >= 0; i--)` (uint は 0 の下に行かない)。`checked` ブロックや「算術オーバーフローのチェック」オプションを入れると、255 の次で `OverflowException` としてすぐ見える。

## Lab 85 — ループができた親の鎖
```
加入申請:  "承認"
すべて中断 → Leader() の g:  Dawn → Ember → Frost → Gale → Dawn → ...   (オブジェクト ID がまた出る)
本当の原因:  JoinUnder は parent.Parent == this (A↔B、長さ2のループ)だけを防ぐ。
           Dawn ← Ember ← Frost ← Gale の鎖で Dawn が Gale の下に入ると長さ4のループができ、Leader() が永遠に回る。
```
**修正:** 新しい親から上へ最後まで上りながら自分自身に出会うか見る。`Lab85_GuildAlliance.cs` の `Guild.JoinUnder` を次のものに置き換える。
```csharp
        public bool JoinUnder(Guild parent)
        {
            for (Guild? g = parent; g != null; g = g.Parent)
                if (g == this)
                    return false;   // ループができる加入は拒否
            Parent = parent;
            return true;
        }
```
データがすでに壊れているときに備えて `Leader()` に訪問集合(`HashSet<Guild>`)や最大段数を置くと、無限ループの代わりにエラーで表に出る。
**教訓:** グラフや連結リストを「終わり(null)が出るまで」たどるコードは**ループがないという仮定**に頼っている。その仮定はデータを**変える所**で守らなければならない。巡回する側では、フロイドの亀とウサギのアルゴリズムでループを見つけることもできる。

## Lab 86 — ロックの順序が食い違ったデッドロック
```
並列スタック:
    取引: Kael → Mira   Monitor.Enter ← Transfer(from=Kael, to=Mira)   lock (to.Sync) の行で待機
    取引: Mira → Kael   Monitor.Enter ← Transfer(from=Mira, to=Kael)   lock (to.Sync) の行で待機
本当の原因:  一方のスレッドは Kael を握って Mira を、もう一方は Mira を握って Kael を待つ。
```
**修正:** すべての所で同じ順序でロックする。`Lab86_Trade.cs` の `Transfer` を次のものに置き換える。`WriteTradeLog` と `Run` はそのまま。
```csharp
    static void Transfer(Player from, Player to, int gold)
    {
        Player first = from.Id < to.Id ? from : to;   // Id の小さい方から先にロックする
        Player second = first == from ? to : from;
        lock (first.Sync)
        {
            WriteTradeLog(from, to, gold);
            lock (second.Sync)
            {
                from.Gold -= gold;
                to.Gold += gold;
            }
        }
    }
```
遅い仕事(`WriteTradeLog`)はロックの外に出すのもよい。ロックを握る時間が短いほどデッドロックと競合が減る。
**教訓:** デッドロックは**ロック二つ + 食い違った順序**で起きる。すべて中断 → 並列スタックで `Monitor.Enter` で待つスレッドたちを探し、各自の**すでに握っているもの**(外側の lock)と**待っているもの**(内側の lock)を書いてみるとループが見える。コマンドラインでは `dotnet-dump analyze` の `syncblk` がロックの所有者スレッドを見せる。

## Lab 87 — 例外経路で SemaphoreSlim.Release の漏れ
```
タスクウィンドウ:               DownloadAsync がいくつも「待機中」 — SemaphoreSlim.WaitAsync を待つ
downloader.FreeSlots:  0
受け取ったファイル:             1個、失敗 2個 → そのあと誰もスロットを得られない
本当の原因:  FetchAsync が例外を投げると _slots.Release() を飛ばす。失敗が maxParallel 回たまるとスロットが 0 個になる。
```
**修正:** 得たスロットは `finally` で返す。`Lab87_DownloadSlots.cs` の `Downloader.DownloadAsync` を次のものに置き換える。`FetchAsync`、`DownloadAll`、`Run` はそのまま。
```csharp
        public async Task<bool> DownloadAsync(string file)
        {
            await _slots.WaitAsync();
            try
            {
                byte[] data = await FetchAsync(file);
                Interlocked.Increment(ref Downloaded);
                return data.Length > 0;
            }
            finally
            {
                _slots.Release();   // 例外が出ても必ず返す
            }
        }
```
**教訓:** セマフォはロックと同じだ。**得たら `finally` で返す。** `Monitor.Wait`/`Pulse` も同じ種類のハングを作る。条件を `if` で一度だけ検査したり、待つ前に来た `Pulse` を逃したりすると永遠に待つ (`while (条件) Monitor.Wait(gate);` が基本形)。

## Lab 88 — volatile なしのビジーウェイト (Release でだけ)
```
Debug:     正常 (待ちながら数億回確認)
Release:   応答なし。止まった瞬間 loader.Done == True、読んだアセット 20個 (ローディングはもう終わっている!)
逆アセンブル (Release、WaitForLoading のループ):
    Done をループに入る前に一度だけ読み、ループの中では spins++ とジャンプだけを繰り返す
本当の原因:  JIT 最適化が「ループの中で誰も Done を書かない」と見て読みをループの外に出した。
           別のスレッドが Done = true を書いてもこのループは永遠に見ない。Debug は最適化を切るので毎回メモリから読む。
```
**修正:** 別のスレッドが書く値は毎回メモリから読ませる。`Lab88_LoadingSpin.cs` の `Loader` の `public bool Done;` の行を次の一行に置き換える。
```csharp
        public volatile bool Done;
```
別の方法:
- `Done` はそのままにして、`WaitForLoading` のループを `while (!Volatile.Read(ref loader.Done))` にする
- ビジーウェイトをなくす: `ManualResetEventSlim` を `Set`/`Wait` するか、ローディングを `Task` にして待つ。CPU を燃やさない。

**教訓:** 「Debug では動くのに Release でだけ止まる」なら**最適化が変えうる仮定**(別のスレッドと共有するメモリ)を疑う。別のスレッドが変える値は `volatile`/`Volatile`/`Interlocked`/`lock` のどれかを通して読み書きする。

---

# 12部: パフォーマンス - 間違ってはいないが遅い

12部のラボは結果がすべて合っています。`[FAIL]` は測定値(割り当てバイト、比較回数、例外の数)が上限を超えたときに出ます。測定値は実行時間と違って PC の負荷にぶれないので、直したあとは上限よりはるかに小さく(ほとんど 0)出ます。

## Lab 89 — ホットループの中の LINQ とキャプチャするラムダ
```
割り当ての追跡:   Func<Enemy, bool>、<>c__DisplayClass.. がフレーム数ぶん
バグ状態:   フレームあたり約 96 バイト (期待: 8 以下)
本当の原因:  enemies.Count(e => ... player ... radius) のラムダが外の変数 player、radius をキャプチャする。
           呼ぶたびにキャプチャを入れるオブジェクト一つとデリゲート一つが新しく作られる。2000 フレームなら 4000 個。
```
**修正:** ふつうの `foreach` で数えます。`radius * radius` もループの外で一度だけ計算します。`Lab89_ThreatMeter.cs` の `CountNearby` を次のものに置き換えます。ラムダがないので、キャプチャ用のオブジェクトもデリゲートも作られません。`Run` はそのままです。
```csharp
    static int CountNearby(List<Enemy> enemies, Vec player, float radius)
    {
        float radiusSq = radius * radius;
        int count = 0;
        foreach (Enemy e in enemies)
            if (e.Alive && e.Pos.DistanceSq(player) <= radiusSq)
                count++;
        return count;
    }
```

**教訓:** 1秒に数十回回るコード(Update、レンダー、サーバーティック)で LINQ とキャプチャするラムダは「見えない `new`」だ。結果が同じでも GC が頻繁に回り、そのたびにすべてのスレッドが少し止まる。キャプチャしないラムダはキャッシュされるので `static` ラムダで書いておくと、うっかりキャプチャするのをコンパイラが防いでくれる。

## Lab 90 — ループの中の文字列 `+=`
```
バグ状態:   割り当てが結果の文字列サイズの約1500倍 (期待: 8倍以下)
ウォッチ:      text.Length が行ごとに大きくなる。+= するたびにその長さぶん丸ごとコピー
本当の原因:  string は不変なので a += b は a 全体をコピーした新しい文字列を作る。n 行なら コピー量が n² に比例する。
```
**修正:** `StringBuilder` に `Append` し、最後に一度だけ `ToString()` します。`Lab90_ChatExport.cs` の `Export` を次のものに置き換え、ファイルの先頭(`namespace BugHunt;` の上)に `using System.Text;` を追加します。`Run` はそのままです。
```csharp
using System.Text;

    static string Export(List<ChatLine> lines)
    {
        var text = new StringBuilder();
        foreach (ChatLine line in lines)
            text.Append($"[{line.Minute / 60:00}:{line.Minute % 60:00}] {line.Sender}: {line.Text}\n");
        return text.ToString();
    }
```
(行数が分かれば `string.Join`、ファイルに書くなら `StreamWriter` で直接書く方法もあります)

**教訓:** いくつかをつなぐのは問題ない(コンパイラが `string.Concat` 一つにする)。**ループ**の中の `+=` が問題だ。データが2倍になると時間が4倍になるコードは、小さいテストデータでは絶対に表に出ない。隠しケースのようにサイズを変えて測ると見える。

## Lab 91 — `object` に入れた int (ボックス化)
```
割り当ての追跡:   System.Int32 がイベント数ぶん
バグ状態:   イベント 1000 個あたり約 48000 バイト (期待: 64 以下)
本当の原因:  Dictionary<string, object> に int を入れるたびにボックス化でヒープに箱ができる (ラボ 4、12)。
```
**修正:** 値を `int` のまま持つ。`Lab91_StatCounter.cs` の `Stats` クラスを丸ごと次のものに置き換える(`object` へのキャストもなくなる)。`Run` はそのまま。
```csharp
    class Stats
    {
        // 値の型を int に固定する。入れても取り出してもボックス化が起きない
        readonly Dictionary<string, int> _values = [];

        public void Add(string key, int amount) => _values[key] = _values.GetValueOrDefault(key) + amount;

        public int Get(string key) => _values.GetValueOrDefault(key);
    }
```
値を取り出して入れ直す照会二回がもったいないなら、`Add` を `CollectionsMarshal.GetValueRefOrAddDefault(_values, key, out _) += amount;` にする(`using System.Runtime.InteropServices;` が必要)。

**教訓:** 「あとで別の型も入れるかもしれないから」作った `object` の場所は今すぐ費用を払う。隠れたボックス化は、非ジェネリックのコレクション、`object`/インターフェースの引数で渡した struct、`IEquatable<T>` のない struct の `Equals`、`string.Format` の値型の引数(ラボ 94)でも生まれる。

## Lab 92 — `List.Contains` で重複排除 (O(n²))
```
バグ状態:   戦利品一つあたり比較約1200回 (期待: 3 以下)
CPU 使用率:  時間のほとんどが List.Contains → ItemId.Equals
本当の原因:  List.Contains は最初から一つずつ比較する。戦利品ごとに図鑑全体を走査するので、比較は 戦利品数 × 図鑑サイズ。
```
**修正:** 「すでにあるか?」は `HashSet<ItemId>` に尋ね、順序は `List` に別に残します。`Lab92_LootDedupe.cs` の `UniqueInOrder` を次のものに置き換えます。`ItemId` と `Run` はそのままです。
```csharp
    static List<ItemId> UniqueInOrder(List<ItemId> drops)
    {
        var seen = new HashSet<ItemId>();
        var unique = new List<ItemId>();
        foreach (ItemId id in drops)
            if (seen.Add(id))   // 初めて見た Id のときだけ true
                unique.Add(id);
        return unique;
    }
```

**教訓:** コレクションは「何をよく尋ねるか」で選ぶ。包含の有無・キーで探すは `HashSet`/`Dictionary`、前から抜くは `Queue`、ソートの維持は `SortedSet`。ループの中の `Contains`、`IndexOf`、`Remove(item)`、`Insert(0, ...)` はすべて一回で O(n) だ。ハッシュコレクションに入れる struct は `IEquatable<T>` と `GetHashCode` をきちんと実装してこそボックス化なしで速い。

## Lab 93 — 呼ぶたびに `new Regex`
```
CPU 使用率:  RegexParser、RegexWriter などパターン解析のコードがホットパスに
バグ状態:   メッセージあたり約2700バイト (期待: 64 以下)
本当の原因:  new Regex(pattern) はパターンを解析して実行できる形にする。その仕事をメッセージごとにやり直す。
```
**修正:** 一度だけ作ります。`Lab93_NameCheck.cs` の `IsValidName` の一行(とその上のコメント)を次のものに置き換えます。`using System.Text.RegularExpressions;` はファイルにすでにあり、`Run` はそのままです。
```csharp
    // 名前の規則: 英字で始まり、英字・数字・アンダースコアで 3〜16 文字。パターンは一度だけ解釈して再利用する。
    static readonly Regex NameRule = new("^[A-Za-z][A-Za-z0-9_]{2,15}$");

    static bool IsValidName(string name) => NameRule.IsMatch(name);
```
ソースジェネレーターを使ってもかまいません(ビルドのときに作っておきます)。その場合はクラスの宣言を `public static partial class Lab93` に変え、`NameRule` フィールドの代わりに `[GeneratedRegex("^[A-Za-z][A-Za-z0-9_]{2,15}$")] private static partial Regex NameRule();` を宣言して、`NameRule().IsMatch(name)` で呼びます。

静的メソッド `Regex.IsMatch(name, pattern)` は内部キャッシュ(既定15個)を使うのでこのラボでは通りますが、パターンが多いとキャッシュから押し出されて同じことが起きます。

**教訓:** 作るのが高価なオブジェクト(`Regex`、`JsonSerializerOptions`、`HttpClient`)は作っておいて再利用する。割り当ての追跡で「マイコードにない内部の型」(`RegexNode`、`RegexCharClass`)がたくさん見えたら、それを作らせたマイコードの `new` を呼び出し履歴で探す。

## Lab 94 — 切ったログの `params` 配列とボックス化
```
ローカルウィンドウ(Log.Debug の最初の行):  args = object[4] { -1, 0, 0f, 0f }  ← すでに作られている
バグ状態:   移動1回あたり152バイト (期待: 1 以下)
本当の原因:  params object?[] の配列とその中の箱(int、float のボックス化)は Log.Debug に入る「前に」、呼ぶ側で作られる。
           関数の中の if (!DebugEnabled) return; はすでに費用を払ったあとだ。
```
**修正:** 呼ぶ側で包みます。`Lab94_DebugLog.cs` の `MoveAll` を次のものに置き換えます。ログが切れていれば `Log.Debug` の引数をまったく計算しないので、`params` 配列もボックス化の箱も作られません。`Log` クラスと `Run` はそのままです。
```csharp
    static void MoveAll(Unit[] units, float dx, float dy, int frame)
    {
        for (int i = 0; i < units.Length; i++)
        {
            units[i].X += dx;
            units[i].Y += dy;
            if (Log.DebugEnabled)   // 切れていれば引数を作らない
                Log.Debug(Text.L("프레임 {0}: 유닛 {1} 이동 → ({2}, {3})", "フレーム {0}: ユニット {1} 移動 → ({2}, {3})"), frame, units[i].Id, units[i].X, units[i].Y);
        }
    }
```
別の方法:
- ジェネリックのオーバーロード `Debug<T1, T2, T3, T4>(string format, T1 a, ...)` を作って、配列とボックス化をなくします。
- 補間文字列ハンドラー(`[InterpolatedStringHandler]`)で、切れていれば何も作らないようにします (`ILogger` が使う方式です)。

参考: `[Conditional("DEBUG")]` は **Release ビルドで**呼び出しと引数の計算を丸ごと消します。Debug ビルドでは `DEBUG` が定義されているので呼び出しと `params` 配列・ボックス化がそのまま残り、Debug で採点するこのラボは通りません。出荷版の費用をなくす別の手段として覚えておきましょう。

**教訓:** 引数は関数が実行される前に計算される。`params`、ボックス化、補間文字列(`$"..."`)、引数の中のメソッド呼び出しは、すべて「切った機能」でも費用を払う。似た見えない費用に、大きな struct を値で渡すコピーがある (`in`、`ref readonly` の引数)。

## Lab 95 — 例外でフロー制御
```
例外設定で FormatException にチェック:  ほぼ毎回止まる
出力ウィンドウ:      "例外がスローされました: 'System.FormatException'" がずらりと
バグ状態:   処理中に投げられた例外約11000個 (期待: 0)
本当の原因:  「数字でない」はよくある入力なのに int.Parse の例外で処理する。例外は投げるときスタックトレースを集め、catch を探してスタックをさかのぼるので、
           if 文より数千倍高価だ。
```
**修正:** 予想される失敗は値で扱う。`Lab95_ChatCommands.cs` の `ParseRollMax` を次のものに置き換える。`Run` はそのまま。
```csharp
    static int ParseRollMax(string command)
    {
        string arg = command.Length > 6 ? command[6..] : "";
        return int.TryParse(arg, out int max) ? max : 100;   // 数字でなければ例外なしに既定値
    }
```

**教訓:** 予想される失敗(ユーザー入力、ファイルなし、キーなし)は値で扱う: `TryParse`、`TryGetValue`、`File.Exists`。例外は本当に例外的な状況用だ。`dotnet-counters` の例外数が毎秒数百個を超えたり、出力ウィンドウに最初の例外のメッセージが降り注いだりしたら、それ自体がパフォーマンスのバグの手がかりだ。攻撃者が例外の経路をわざと叩くとサービス拒否になることもある。

---
