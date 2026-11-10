namespace BugHunt;

// 몬스터가 쓰러지면 드롭 개수를 정한다. 드롭 규칙은 IDropSource 인터페이스에 모아 두었고,
// 규칙이 없는 잡몹은 인터페이스의 기본 구현(1개)을 쓴다. 보스와 정예는 운에 따라 더 떨어뜨린다.
// [JA] モンスターが倒れるとドロップ数を決めます。ドロップ規則は IDropSource インターフェイスにまとめてあり、
// [JA] 規則のない雑魚はインターフェイスの既定の実装(1個)を使います。ボスと精鋭は運に応じて多く落とします。
public static partial class Lab39
{
    interface IDropSource
    {
        string Name { get; }

        // 기본 규칙: 1개. 규칙이 있는 몬스터는 이 메서드를 구현한다.
        // [JA] 既定の規則: 1個。規則のあるモンスターはこのメソッドを実装します。
        int DropCount(int luck) => 1;
    }

    sealed class Slime : IDropSource
    {
        public string Name => Text.L("슬라임", "スライム");
    }

    // 보스: 기본 드롭 + 운 10당 1개. 정예 보스는 1개 더.
    // [JA] ボス: 基本ドロップ + 運 10 ごとに 1個。精鋭ボスは1個多い。
    sealed class Boss(string name, int baseDrops) : IDropSource
    {
        public string Name => name;
        public int DropCount(int luck, bool elite = false) => baseDrops + luck / 10 + (elite ? 1 : 0);
    }

    // 정예: 2개 + 운 20당 1개 (운은 long 으로 받는다. 서버의 운 수치가 long 이라서)
    // [JA] 精鋭: 2個 + 運 20 ごとに 1個(運は long で受け取る。サーバーの運の値が long なので)
    sealed class Elite(string name) : IDropSource
    {
        public string Name => name;
        public int DropCount(long luck) => 2 + (int)(luck / 20);
    }

    // 전투 시스템은 인터페이스로만 몬스터를 다룬다.
    // [JA] 戦闘システムはインターフェイスだけでモンスターを扱います。
    static int RollDrops(IDropSource monster, int luck) => monster.DropCount(luck);

    public static void Run()
    {
        int luck = Pick(35, 0, 99);
        int baseDrops = Pick(3, 2, 6);

        var slime = new Slime();
        var boss = new Boss(Text.L("화염 군주", "炎の君主"), baseDrops);
        var elite = new Elite(Text.L("정예 궁수", "精鋭の弓兵"));

        // 기대값: 몬스터 형식으로 직접 부른 값
        // [JA] 期待値: モンスターの型で直接呼んだ値
        int expectedBoss = boss.DropCount(luck);
        int expectedElite = elite.DropCount(luck);

        Console.WriteLine(Text.L($"  운 {luck}: 슬라임 {RollDrops(slime, luck)}개, 보스 {RollDrops(boss, luck)}개 (기대 {expectedBoss}), 정예 {RollDrops(elite, luck)}개 (기대 {expectedElite})", $"  運 {luck}: スライム {RollDrops(slime, luck)}個、ボス {RollDrops(boss, luck)}個 (期待 {expectedBoss})、精鋭 {RollDrops(elite, luck)}個 (期待 {expectedElite})"));

        Expect(Text.L("슬라임 드롭 수 (기본 규칙)", "スライムのドロップ数 (既定の規則)"), 1, RollDrops(slime, luck));
        Expect(Text.L($"보스 드롭 수 (운 {luck})", $"ボスのドロップ数 (運 {luck})"), expectedBoss, RollDrops(boss, luck));
        if (CheckElite)
            Expect(Text.L($"정예 드롭 수 (운 {luck})", $"精鋭のドロップ数 (運 {luck})"), expectedElite, RollDrops(elite, luck));
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// Boss.DropCount 에 중단점을 걸고 실행하세요. RollDrops 에서 보스를 계산할 때 거기서 멈추나요?
// 안 멈춘다면 RollDrops 의 monster.DropCount(luck) 에서 F11 로 들어가 보세요. 어느 파일, 어느 줄로 들어가나요?
//
// [JA] Boss.DropCount にブレークポイントを置いて実行してください。RollDrops がボスを計算するとき、そこで止まりますか?
// [JA] 止まらないなら RollDrops の monster.DropCount(luck) で F11 を押して中に入ってみてください。どのファイルのどの行に入りますか?
#endregion

#region 힌트 2 / ヒント 2
// 인터페이스의 기본 구현(default interface method)은 "클래스가 그 메서드를 구현하지 않았을 때"만 쓰입니다.
// 그런데 Boss 에는 DropCount 가 있는데 왜 구현으로 안 쳐 줄까요? 인터페이스의 시그니처 int DropCount(int) 와 글자 하나까지 비교해 보세요.
// 선택적 매개변수(bool elite = false)가 붙은 메서드와 long 을 받는 메서드는 호출할 때는 DropCount(luck) 으로 똑같이 보이지만, 시그니처는 다른 메서드입니다.
//
// [JA] インターフェイスの既定の実装(default interface method)は「クラスがそのメソッドを実装していないとき」だけ使われます。
// [JA] では Boss には DropCount があるのに、なぜ実装と見なされないのでしょう? インターフェイスのシグネチャ int DropCount(int) と一文字ずつ比べてください。
// [JA] 省略可能な引数(bool elite = false)が付いたメソッドと long を受け取るメソッドは、呼ぶときは DropCount(luck) と同じに見えますが、シグネチャは別のメソッドです。
#endregion

#region 힌트 3 / ヒント 3
// 인터페이스 멤버는 시그니처가 정확히 같은 public 메서드(또는 명시적 구현)만 구현으로 인정됩니다. 컴파일러는 경고도 내지 않습니다.
// 고치는 법: 시그니처를 인터페이스와 똑같이 맞추고(int DropCount(int luck)), 정예 보스 같은 변형은 별도 오버로드나 생성자 인자로 받으세요.
// 기본 구현이 있는 인터페이스에서는 "구현을 빠뜨려도 컴파일이 된다"는 점을 기억하세요. 기본 구현을 뺐을 때 컴파일 오류가 나는 클래스가 바로 범인입니다.
//
// [JA] インターフェイスのメンバーは、シグネチャが完全に同じ public メソッド(または明示的な実装)だけが実装と認められます。コンパイラは警告も出しません。
// [JA] 直し方: シグネチャをインターフェイスと同じにし(int DropCount(int luck))、精鋭ボスのような変種は別のオーバーロードやコンストラクター引数で受け取ってください。
// [JA] 既定の実装があるインターフェイスでは「実装を忘れてもコンパイルが通る」ことを覚えておいてください。既定の実装を外したときにコンパイルエラーになるクラスが犯人です。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 기본 케이스는 보스만 판정하지만, 숨은 케이스는 정예(long 매개변수)도 판정한다. 보스만 고치면 여기서 걸린다.
// [JA] 基本ケースではボスだけ判定しますが、隠しケースでは精鋭(long 引数)も判定します。ボスだけ直すとここで引っかかります。
public static partial class Lab39
{
    static bool CheckElite => Seed != 0;
}
#endregion
