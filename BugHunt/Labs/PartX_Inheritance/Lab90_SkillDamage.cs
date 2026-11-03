namespace BugHunt;

// 스킬마다 피해 계산이 다르다. 불 속성은 화상 보너스(1.5배), 얼음 속성은 둔화 보너스(+20)가 붙는다.
// 스킬 바는 기반 형식 Skill 의 목록으로 들고 다니며, 턴이 끝날 때 총 피해를 더한다.
// [JA] スキルごとにダメージ計算が違います。炎属性には火傷ボーナス(1.5倍)、氷属性には鈍足ボーナス(+20)が付きます。
// [JA] スキルバーは基底型 Skill のリストとして持ち回り、ターン終了時に合計ダメージを足し合わせます。
public static partial class Lab90
{
    class Skill(string name, int power)
    {
        public string Name { get; } = name;
        public int Power { get; } = power;
        public virtual int Damage(int attack) => attack + Power;
    }

    // 불 속성: 화상 보너스 1.5배
    class FireSkill(string name, int power) : Skill(name, power)
    {
        public new int Damage(int attack) => (attack + Power) * 3 / 2;
    }

    // 얼음 속성: 둔화 보너스 +20
    class IceSkill(string name, int power) : Skill(name, power)
    {
        public new int Damage(int attack) => attack + Power + 20;
    }

    // 스킬 바 전체의 총 피해
    static int TotalDamage(IEnumerable<Skill> bar, int attack) => bar.Sum(s => s.Damage(attack));

    static Skill Make(string kind, string name, int power) => kind switch
    {
        "fire" => new FireSkill(name, power),
        "ice" => new IceSkill(name, power),
        _ => new Skill(name, power),
    };

    public static void Run()
    {
        int attack = Pick(40, 10, 99);
        (string Kind, string Name)[] slots = Pick(Compositions[0], Compositions[1..]);
        Skill[] bar = [.. slots.Select((s, i) => Make(s.Kind, s.Name, Pick(10 * (i + 1), 5, 60)))];

        // 기대값: 스킬 하나하나를 그 스킬의 형식으로 직접 계산
        int expected = 0;
        foreach (Skill s in bar)
        {
            int each = s switch { FireSkill f => f.Damage(attack), IceSkill i => i.Damage(attack), _ => s.Damage(attack) };
            Console.WriteLine($"  {s.Name,-6} ({s.GetType().Name,-9}) 위력 {s.Power,2} → {each}");
            expected += each;
        }

        int actual = TotalDamage(bar, attack);
        Console.WriteLine($"  스킬 바 총 피해: {actual}");

        Expect("스킬 바 총 피해", expected, actual);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// FireSkill.Damage 와 Skill.Damage 양쪽에 중단점을 거세요. TotalDamage 가 불 스킬을 계산할 때 어느 쪽에서 멈추나요?
// 호출 스택 창에서 호출한 줄로 가서, 그때 s 의 정적 형식(선언 형식)과 동적 형식(조사식 s.GetType())을 비교해 보세요.
//
// [JA] FireSkill.Damage と Skill.Damage の両方にブレークポイントを置いてください。TotalDamage が炎スキルを計算するとき、どちらで止まりますか?
// [JA] 呼び出し履歴ウィンドウで呼び出し元の行に移動し、そのときの s の静的な型(宣言の型)と動的な型(ウォッチの s.GetType())を比べてみてください。
#endregion

#region 힌트 2 / ヒント 2
// FireSkill.Damage 의 선언을 보세요. override 가 아니라 new 입니다.
// new 는 "기반 메서드를 가상으로 덮어쓰지 않고, 같은 이름의 새 메서드를 하나 더 만든다"는 뜻입니다.
// 그래서 변수의 선언 형식이 Skill 이면 Skill.Damage 가, FireSkill 이면 FireSkill.Damage 가 불립니다. (실행 시간의 실제 객체와 상관없이)
// new 를 지우면 경고 CS0108 이 나옵니다. 그 경고를 없애려고 누군가 new 를 붙였을 가능성이 큽니다.
//
// [JA] FireSkill.Damage の宣言を見てください。override ではなく new です。
// [JA] new は「基底メソッドを仮想的に上書きせず、同じ名前の新しいメソッドをもう一つ作る」という意味です。
// [JA] そのため変数の宣言の型が Skill なら Skill.Damage が、FireSkill なら FireSkill.Damage が呼ばれます。(実行時の実際のオブジェクトとは無関係に)
// [JA] new を消すと警告 CS0108 が出ます。その警告を消すために誰かが new を付けた可能性が高いです。
#endregion

#region 힌트 3 / ヒント 3
// 기반 메서드가 virtual 이면 파생에서는 override 로 받아야 합니다. new 는 "일부러 숨기려는" 드문 경우에만 쓰세요.
// FireSkill 만 고치고 끝내지 마세요. 같은 실수를 한 스킬이 또 있습니다. (숨은 테스트는 다른 속성 조합으로 검사합니다)
// TotalDamage 에서 형식을 검사해 캐스팅하는 수정은 새 스킬이 추가될 때마다 다시 깨집니다.
//
// [JA] 基底メソッドが virtual なら、派生では override で受けなければなりません。new は「わざと隠したい」まれな場合だけに使ってください。
// [JA] FireSkill だけ直して終わりにしないでください。同じ間違いをしたスキルがもう一つあります。(隠しテストは別の属性の組み合わせで検査します)
// [JA] TotalDamage で型を調べてキャストする修正は、新しいスキルが追加されるたびにまた壊れます。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 기본 케이스는 불 스킬 하나만 보이지만, 숨은 케이스는 얼음 스킬이 섞인 조합과 다른 공격력·위력으로 같은 코드를 검사한다.
// FireSkill 만 override 로 고치거나 TotalDamage 에서 FireSkill 만 캐스팅하면 여기서 걸린다.
// [JA] 基本ケースでは炎スキルが一つ見えるだけですが、隠しケースでは氷スキルが混ざった組み合わせと別の攻撃力・威力で同じコードを検査します。
// [JA] FireSkill だけ override に直したり、TotalDamage で FireSkill だけキャストしたりすると、ここで引っかかります。
public static partial class Lab90
{
    static readonly (string Kind, string Name)[][] Compositions =
    [
        [("basic", "베기"), ("fire", "화염구"), ("basic", "찌르기")],
        [("basic", "베기"), ("ice", "서리창"), ("fire", "화염구")],
        [("ice", "눈보라"), ("ice", "서리창")],
        [("fire", "화염구"), ("basic", "베기"), ("ice", "빙결"), ("fire", "용의 숨결")],
    ];
}
#endregion
