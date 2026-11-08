namespace BugHunt;

// 피해 숫자를 화면에 "320 피해"처럼 표시한다. 라벨을 만드는 순간 프로그램이 통째로 사라진다.
// [JA] ダメージの数字を画面に「320 ダメージ」のように表示します。ラベルを作る瞬間にプログラムが丸ごと消えます。
public static partial class Lab08
{
    readonly struct DamageLabel(int amount)
    {
        public int Amount { get; } = amount;

        // 금액 뒤에 " 피해"를 붙여 보여 준다.
        public override string ToString() => $"{this} {Text.T("피해")}";
    }

    public static void Run()
    {
        int amount = Pick(Amounts[0], Amounts[1..]);
        var label = new DamageLabel(amount);

        // 라벨은 스택이 작은(256KB) 화면 갱신 스레드에서 만든다. (스택이 작으면 넘칠 때 빨리 넘쳐 채점이 오래 걸리지 않는다)
        string text = "";
        var ui = new Thread(() => text = label.ToString(), maxStackSize: 256 * 1024) { Name = Text.T("UI 갱신") };
        ui.Start();
        ui.Join();
        Console.WriteLine($"  라벨: {text}");
        Expect("피해 표시", $"{amount} {Text.T("피해")}", text);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// ToString 의 => 줄에 중단점을 걸고 F5. 중단점이 몇 번이나 멈추나요? 멈출 때마다 호출 스택이 길어집니다.
// 호출 스택 창을 보세요. DamageLabel.ToString 이 자기 자신을 끝없이 부르고 있습니다.
//
// [JA] ToString の => の行にブレークポイントを置いて F5。ブレークポイントは何回止まりますか? 止まるたびに呼び出し履歴が長くなります。
// [JA] 呼び出し履歴ウィンドウを見てください。DamageLabel.ToString が自分自身を延々と呼んでいます。
#endregion

#region 힌트 2 / ヒント 2
// $"{this} {Text.T("피해")}" 안의 {this} 는 this 를 문자열로 바꾸려고 this.ToString() 을 부릅니다. 그게 바로 지금 실행 중인 ToString 입니다.
// 그래서 ToString → ToString → ToString … 무한 재귀가 되고, 스택을 다 쓰면 StackOverflowException 으로 프로세스가 즉시 죽습니다.
// 이 죽음은 try/catch 로 못 잡습니다. 그래서 [FAIL] 한 줄도 없이 사라집니다. (랩 5 와 비교하세요: 거기선 다른 원인으로 죽었습니다.)
//
// [JA] $"{this} {Text.T("피해")}" の中の {this} は this を文字列にするために this.ToString() を呼びます。それが今まさに実行中の ToString です。
// [JA] だから ToString → ToString → ToString … の無限再帰になり、スタックを使い切ると StackOverflowException でプロセスが即死します。
// [JA] この死は try/catch で捕まえられません。だから [FAIL] の一行もなく消えます。(ラボ 5 と比べてください: あちらは別の原因で死にました。)
#endregion

#region 힌트 3 / ヒント 3
// ToString 안에서는 그 객체 자신을 문자열로 보간하지 마세요. 필요한 필드를 직접 쓰세요:
//   public override string ToString() => $"{Amount} {Text.T("피해")}";
// 자기 참조가 필요하면 base.ToString() 이나 각 필드를 명시적으로 조합하세요. {this} 와 {this.SomeProp} 도 SomeProp 이 다시 this 를 쓰면 같은 함정입니다.
//
// [JA] ToString の中でそのオブジェクト自身を文字列補間しないでください。必要なフィールドを直接書いてください:
// [JA]   public override string ToString() => $"{Amount} {Text.T("피해")}";
// [JA] 自己参照が必要なら base.ToString() や各フィールドを明示的に組み合わせてください。{this} や {this.SomeProp} も、SomeProp がまた this を使えば同じ罠です。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 다른 금액으로 검사한다. 보이는 금액만 문자열로 박으면 다른 금액에서 그대로 CRASH 한다.
// [JA] 隠しケースは別の金額で検査します。見えている金額だけ文字列で埋めると、別の金額でそのまま CRASH します。
public static partial class Lab08
{
    static readonly int[] Amounts = [320, 75, 1200, 9999];
}
#endregion
