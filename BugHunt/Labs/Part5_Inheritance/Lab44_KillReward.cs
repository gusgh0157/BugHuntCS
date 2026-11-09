namespace BugHunt;

// 처치 보상. 보스는 500G, 정예는 50G, 일반 적은 10G, NPC(실수로 공격한 경우)는 0G.
// 보스를 잡았는데 일반 적만큼만 준다.
// [JA] 討伐報酬。ボスは 500G、精鋭は 50G、通常の敵は 10G、NPC(誤って攻撃した場合)は 0G。
// [JA] ボスを倒したのに通常の敵と同じ額しかもらえません。
public static partial class Lab44
{
    abstract class Unit(string name)
    {
        public string Name { get; } = name;
    }

    class Enemy(string name) : Unit(name);
    sealed class EliteEnemy(string name) : Enemy(name);
    sealed class BossEnemy(string name) : Enemy(name);
    sealed class Villager(string name) : Unit(name);

    static int Reward(Unit killed)
    {
        if (killed is Enemy)
            return 10;
        else if (killed is BossEnemy)
            return 500;
        else if (killed is EliteEnemy)
            return 50;
        else
            return 0;
    }

    static Unit Spawn(string kind, string name) => kind switch
    {
        "boss" => new BossEnemy(name),
        "elite" => new EliteEnemy(name),
        "enemy" => new Enemy(name),
        _ => new Villager(name),
    };

    static int ExpectedReward(Unit u) => u switch { BossEnemy => 500, EliteEnemy => 50, Enemy => 10, _ => 0 };

    public static void Run()
    {
        (string Kind, string Name)[] wave = Pick(Waves[0], Waves[1..]);
        Unit[] killed = [.. wave.Select(w => Spawn(w.Kind, w.Name))];

        int total = 0;
        foreach (Unit u in killed)
        {
            int gold = Reward(u);
            total += gold;
            Console.WriteLine(Text.L($"  {u.Name} ({u.GetType().Name}) 처치: {gold}G", $"  {u.Name} ({u.GetType().Name}) 討伐: {gold}G"));
        }

        int expected = killed.Sum(ExpectedReward);
        Console.WriteLine(Text.L($"  처치 보상 합계: {total}G", $"  討伐報酬の合計: {total}G"));

        Expect(Text.L("처치 보상 합계", "討伐報酬の合計"), expected, total);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// return 500; 줄에 중단점을 거세요. 보스를 처치해도 거기서 멈추지 않습니다.
// Reward 의 첫 if 에 중단점을 걸고 보스일 때 F10 으로 한 줄씩 가 보세요. 어느 return 으로 가나요? 조사식에 killed is Enemy, killed is BossEnemy 를 둘 다 넣어 보세요.
//
// [JA] return 500; の行にブレークポイントを置いてください。ボスを倒してもそこで止まりません。
// [JA] Reward の最初の if にブレークポイントを置き、ボスのとき F10 で一行ずつ進んでください。どの return に行きますか? ウォッチに killed is Enemy と killed is BossEnemy を両方入れてみてください。
#endregion

#region 힌트 2 / ヒント 2
// BossEnemy 는 Enemy 를 상속합니다. 그래서 보스는 killed is Enemy 도 true 입니다. 첫 if 가 모든 적을 가져가고, 뒤의 else if 두 개는 영원히 닿지 않습니다.
// 같은 코드를 switch 로 쓰면 컴파일러가 "이 경우는 이미 앞에서 처리한다"는 오류를 내 줍니다 (switch 식은 CS8510, switch 문은 CS8120). if/else if 사슬에는 그런 검사가 없습니다.
//
// [JA] BossEnemy は Enemy を継承しています。そのためボスは killed is Enemy も true です。最初の if がすべての敵を持っていき、後ろの else if 二つには永遠に届きません。
// [JA] 同じコードを switch で書くと、コンパイラが「このケースはすでに前で処理されている」というエラーを出してくれます (switch 式は CS8510、switch 文は CS8120)。if/else if の連鎖にはそういう検査がありません。
#endregion

#region 힌트 3 / ヒント 3
// 형식 검사 사슬은 "가장 파생된 형식부터" 씁니다. 보스 → 정예 → 일반 적 → 그 외.
// 더 좋은 방법은 형식 검사 자체를 없애는 것입니다: Unit 에 virtual int KillReward 를 두고 각 클래스가 자기 보상을 돌려주게 하거나, switch 식으로 써서 컴파일러가 순서를 검사하게 하세요.
// 숨은 테스트는 정예가 섞인 조합도 검사합니다. 보스 줄만 위로 올리고 끝내지 마세요.
//
// [JA] 型チェックの連鎖は「最も派生した型から」書きます。ボス → 精鋭 → 通常の敵 → その他。
// [JA] より良いのは型チェック自体をなくすことです: Unit に virtual int KillReward を置いて各クラスが自分の報酬を返すようにするか、switch 式で書いてコンパイラに順番を検査させてください。
// [JA] 隠しテストは精鋭が混ざった組み合わせも検査します。ボスの行だけ上に上げて終わりにしないでください。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 정예와 NPC가 섞인 다른 웨이브로 검사한다. 보스 분기만 맨 위로 올리면 정예가 있는 웨이브에서 걸린다.
// [JA] 隠しケースは精鋭とNPCが混ざった別のウェーブで検査します。ボスの分岐だけ一番上に上げると、精鋭のいるウェーブで引っかかります。
public static partial class Lab44
{
    static readonly (string Kind, string Name)[][] Waves =
    [
        [("enemy", Text.L("고블린", "ゴブリン")), ("enemy", Text.L("고블린", "ゴブリン")), ("boss", Text.L("고블린 왕", "ゴブリンキング"))],
        [("enemy", Text.L("늑대", "オオカミ")), ("elite", Text.L("우두머리 늑대", "群れのボスオオカミ")), ("villager", Text.L("양치기", "羊飼い"))],
        [("elite", Text.L("정예 기사", "精鋭の騎士")), ("boss", Text.L("타락한 군주", "堕ちた君主")), ("enemy", Text.L("해골 병사", "骸骨兵"))],
        [("villager", Text.L("상인", "商人")), ("elite", Text.L("정예 궁수", "精鋭の弓兵")), ("elite", Text.L("정예 궁수", "精鋭の弓兵")), ("enemy", Text.L("도적", "盗賊"))],
    ];
}
#endregion
