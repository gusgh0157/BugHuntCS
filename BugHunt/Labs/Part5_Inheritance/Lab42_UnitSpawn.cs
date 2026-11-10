namespace BugHunt;

// 유닛을 소환하면 최대 체력을 계산해 현재 체력을 꽉 채운다. 최대 체력 공식은 병과마다 다르다(기사는 갑옷, 마법사는 지혜).
// 소환 직후 기사의 체력이 갑옷 보정 없이 기본값으로만 차 있다.
// [JA] ユニットを召喚すると最大HPを計算し、現在HPを満タンにします。最大HPの式は兵種ごとに違います(騎士は鎧、魔法使いは知恵)。
// [JA] 召喚直後、騎士のHPが鎧の補正なしの基本値だけで満たされています。
public static partial class Lab42
{
    abstract class Unit
    {
        public string Name { get; }
        public int Hp { get; protected set; }

        protected Unit(string name)
        {
            Name = name;
            Hp = MaxHp();   // 소환 직후 체력을 꽉 채운다 / 召喚直後に体力を満タンにする
        }

        public abstract int MaxHp();
    }

    sealed class Knight : Unit
    {
        readonly int baseHp = 100;
        readonly int armor;

        public Knight(string name, int armor) : base(name)
        {
            this.armor = armor;
        }

        public override int MaxHp() => baseHp + armor * 10;
    }

    sealed class Mage : Unit
    {
        readonly int baseHp = 60;
        readonly int wisdom;

        public Mage(string name, int wisdom) : base(name)
        {
            this.wisdom = wisdom;
        }

        public override int MaxHp() => baseHp + wisdom * 5;
    }

    public static void Run()
    {
        var units = new List<Unit> { new Knight(Text.L("기사 로한", "騎士ロハン"), Pick(7, 1, 20)) };
        if (Seed != 0)
            units.Add(new Mage(Text.L("마법사 이린", "魔法使いイリン"), Pick(9, 1, 20)));

        foreach (Unit unit in units)
        {
            Console.WriteLine(Text.L($"  {unit.Name} 소환: 현재 체력 {unit.Hp} / 최대 체력 {unit.MaxHp()}", $"  {unit.Name} 召喚: 現在HP {unit.Hp} / 最大HP {unit.MaxHp()}"));
            Expect(Text.L($"{unit.Name} 소환 직후 현재 체력 = 최대 체력", $"{unit.Name} 召喚直後の現在HP = 最大HP"), unit.MaxHp(), unit.Hp);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// Knight.MaxHp 에 중단점을 걸고 소환하세요. 처음 멈췄을 때 지역 창에서 armor 와 baseHp 를 보세요. armor 가 0 입니다. 왜일까요?
// 호출 스택 창을 보세요. 누가 MaxHp 를 불렀나요? Knight 생성자 본문(this.armor = armor)은 이미 실행됐나요?
//
// [JA] Knight.MaxHp にブレークポイントを置いて召喚してください。最初に止まったときローカルウィンドウで armor と baseHp を見てください。armor が 0 です。なぜでしょう?
// [JA] 呼び出し履歴ウィンドウを見てください。誰が MaxHp を呼びましたか? Knight のコンストラクター本体(this.armor = armor)はもう実行されましたか?
#endregion

#region 힌트 2 / ヒント 2
// 생성 순서: 파생 클래스의 필드 이니셜라이저(baseHp = 100) → 기반 생성자(Unit) → 파생 생성자 본문(this.armor = armor).
// 기반 생성자가 가상 메서드 MaxHp 를 부르면, 이미 Knight.MaxHp 가 실행됩니다(가상 호출은 실제 형식을 따름). 그런데 그 시점에 Knight 의 생성자 본문은 아직입니다.
// 그래서 baseHp 는 100이 들어 있고 armor 는 기본값 0 입니다. (필드 이니셜라이저와 생성자 본문의 차이가 여기서 드러납니다)
//
// [JA] 生成の順番: 派生クラスのフィールド初期化子(baseHp = 100) → 基底コンストラクター(Unit) → 派生コンストラクター本体(this.armor = armor)。
// [JA] 基底コンストラクターが仮想メソッド MaxHp を呼ぶと、すでに Knight.MaxHp が実行されます(仮想呼び出しは実際の型に従う)。しかしその時点で Knight のコンストラクター本体はまだです。
// [JA] そのため baseHp には 100 が入っていて armor は既定値 0 です。(フィールド初期化子とコンストラクター本体の違いがここで表れます)
#endregion

#region 힌트 3 / ヒント 3
// 생성자 안에서 가상 메서드를 부르지 마세요. 파생 클래스가 아직 준비되기 전의 객체를 보게 됩니다. (분석기 CA2214 가 이 경우를 잡아 줍니다. .editorconfig 에서 켜 보세요)
// 고치는 법 (택1):
//   - 기반 생성자가 최대 체력을 인자로 받는다: protected Unit(string name, int maxHp) 그리고 파생에서 base(name, 100 + armor * 10).
//   - Hp 를 생성자에서 채우지 말고, 소환이 끝난 뒤 Spawn()/Heal() 같은 별도 단계에서 채운다.
//   - 체력 계산에 필요한 값을 필드 이니셜라이저에서만 정한다(생성자 인자가 필요하면 쓸 수 없는 방법).
//
// [JA] コンストラクターの中で仮想メソッドを呼ばないでください。派生クラスがまだ準備できていないオブジェクトを見ることになります。(アナライザー CA2214 がこのケースを検出します。.editorconfig で有効にしてみてください)
// [JA] 直し方(いずれか):
// [JA]   - 基底コンストラクターが最大HPを引数で受け取る: protected Unit(string name, int maxHp)、派生では base(name, 100 + armor * 10)。
// [JA]   - Hp をコンストラクターで満たさず、召喚が終わったあと Spawn()/Heal() のような別の段階で満たす。
// [JA]   - HP計算に必要な値をフィールド初期化子だけで決める(コンストラクター引数が必要なら使えない方法)。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 기사와 함께 마법사(지혜)도 다른 수치로 소환해 검사한다. Knight 생성자 끝에서 Hp 를 다시 채우는 수정은 Mage 에서 걸린다.
// [JA] 隠しケースは騎士と一緒に魔法使い(知恵)も別の数値で召喚して検査します。Knight のコンストラクターの最後で Hp を入れ直す修正は Mage で引っかかります。
#endregion
