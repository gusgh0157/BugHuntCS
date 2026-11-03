namespace BugHunt;

// 버프는 남은 턴 수를 가진 작은 struct 다. 턴이 끝날 때마다 모든 버프의 남은 턴을 하나씩 줄이고, 0이 된 버프는 지운다.
// 턴을 몇 번 넘겨도 버프가 하나도 사라지지 않는다.
// [JA] バフは残りターン数を持つ小さな struct です。ターンが終わるたびに全バフの残りターンを1ずつ減らし、0になったバフは消します。
// [JA] ターンを何度進めてもバフが一つも消えません。
public static partial class Lab93
{
    interface ITickable
    {
        void Tick();
        bool Expired { get; }
    }

    struct Buff(string name, int turns) : ITickable
    {
        public string Name = name;
        public int Turns = turns;

        public void Tick() => Turns--;
        public readonly bool Expired => Turns <= 0;
    }

    // 턴 종료: 모든 버프를 한 턴씩 진행
    static void EndTurn(Buff[] buffs)
    {
        foreach (ITickable buff in buffs)
            buff.Tick();
    }

    public static void Run()
    {
        int turns = Pick(3, 2, 6);
        Buff[] buffs =
        [
            new("공격력 증가", Pick(2, 1, turns)),
            new("방어력 증가", Pick(5, turns + 1, turns + 4)),
            new("신속", Pick(3, 1, turns)),
        ];

        int[] expectedTurns = [.. buffs.Select(b => b.Turns - turns)];
        int expectedExpired = expectedTurns.Count(t => t <= 0);

        for (int t = 1; t <= turns; t++)
            EndTurn(buffs);

        foreach (Buff b in buffs)
            Console.WriteLine($"  {b.Name}: 남은 턴 {b.Turns}{(b.Expired ? " (만료)" : "")}");

        Expect($"{turns}턴 뒤 만료된 버프 수", expectedExpired, buffs.Count(b => b.Expired));
        Expect("첫 버프의 남은 턴", expectedTurns[0], buffs[0].Turns);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// Buff.Tick 에 중단점을 거세요. 불리기는 합니다. Turns 가 줄어드는 것도 보입니다.
// 그런데 EndTurn 에서 나온 뒤 조사식에 buffs[0].Turns 를 넣어 보면 그대로입니다. Tick 이 줄인 Turns 는 누구의 Turns 였을까요?
//
// [JA] Buff.Tick にブレークポイントを置いてください。呼ばれてはいます。Turns が減るのも見えます。
// [JA] でも EndTurn から戻ったあとウォッチに buffs[0].Turns を入れてみると、そのままです。Tick が減らした Turns は誰の Turns だったのでしょう?
#endregion

#region 힌트 2 / ヒント 2
// Buff 는 struct 입니다. struct 를 인터페이스 형식(ITickable)의 변수에 넣는 순간 박싱(boxing)되어 힙에 복사본이 만들어집니다.
// foreach (ITickable buff in buffs) 는 배열 칸마다 박싱 복사본을 만들고, Tick 은 그 복사본의 Turns 를 줄입니다. 배열 칸은 그대로입니다.
// Tick 안에서 멈췄을 때 조사식에 this 를 넣어 "개체 ID 만들기"를 해 보세요. struct 에는 만들 수 없습니다. 그 자체가 단서입니다.
//
// [JA] Buff は struct です。struct をインターフェイス型(ITickable)の変数に入れた瞬間にボックス化(boxing)され、ヒープにコピーが作られます。
// [JA] foreach (ITickable buff in buffs) は配列の要素ごとにボックス化コピーを作り、Tick はそのコピーの Turns を減らします。配列の要素はそのままです。
// [JA] Tick の中で止まったときウォッチに this を入れて「オブジェクト ID の作成」を試してください。struct には作れません。それ自体が手がかりです。
#endregion

#region 힌트 3 / ヒント 3
// 바뀌는 struct 를 인터페이스로 다루면 늘 복사본을 고치게 됩니다. 고치는 법 (택1):
//   - for (int i = 0; i < buffs.Length; i++) buffs[i].Tick();   (배열 인덱서는 칸 자체를 돌려준다)
//   - foreach (ref Buff buff in buffs.AsSpan()) buff.Tick();
//   - 제네릭 제약으로 박싱 없이: static void TickAll<T>(Span<T> items) where T : ITickable { foreach (ref T it in items) it.Tick(); }
//   - 애초에 바뀌는 데이터면 class 로 만든다.
// 랩 20(List<struct> 인덱서 복사본)과 같은 뿌리입니다. 이번에는 인터페이스 변환이 복사를 만들었습니다.
//
// [JA] 変化する struct をインターフェイス経由で扱うと、いつもコピーを直すことになります。直し方(いずれか):
// [JA]   - for (int i = 0; i < buffs.Length; i++) buffs[i].Tick();   (配列のインデクサーは要素そのものを返す)
// [JA]   - foreach (ref Buff buff in buffs.AsSpan()) buff.Tick();
// [JA]   - ジェネリック制約でボックス化なしに: static void TickAll<T>(Span<T> items) where T : ITickable { foreach (ref T it in items) it.Tick(); }
// [JA]   - そもそも変化するデータなら class にする。
// [JA] ラボ20(List<struct> インデクサーのコピー)と同じ根です。今回はインターフェイスへの変換がコピーを作りました。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 턴 수와 버프 지속 시간을 바꿔 검사한다. 만료 수를 박아 넣거나 Run 에서 직접 Turns 를 빼면 여기서 걸린다.
// [JA] 隠しケースはターン数とバフの持続時間を変えて検査します。満了数を埋め込んだり Run で直接 Turns を引いたりするとここで引っかかります。
#endregion
