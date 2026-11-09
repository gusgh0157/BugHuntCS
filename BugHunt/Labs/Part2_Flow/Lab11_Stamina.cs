namespace BugHunt;

// 달리기는 스태미나를 30 쓴다. 스태미나가 모자라면 달릴 수 없다.
// [JA] ダッシュはスタミナを 30 使います。スタミナが足りなければダッシュできません。
public static class Lab11
{
    struct Stamina(int max)
    {
        public int Current = max;
        public int Max = max;

        public bool TryUse(int amount)
        {
            if (Current < amount)
                return false;
            Current -= amount;
            return true;
        }
    }

    class Runner
    {
        public const int SprintCost = 30;

        readonly Stamina _stamina = new(100);

        public int StaminaLeft => _stamina.Current;

        public bool Sprint() => _stamina.TryUse(SprintCost);
    }

    public static void Run()
    {
        var runner = new Runner();
        int sprints = 0;
        for (int i = 0; i < 4; i++)
        {
            bool ok = runner.Sprint();
            Console.WriteLine(Text.L($"  달리기 {i + 1}: {(ok ? "성공" : "스태미나 부족")}, 남은 스태미나 {runner.StaminaLeft}", $"  ダッシュ {i + 1}: {(ok ? "成功" : "スタミナ不足")}、残りスタミナ {runner.StaminaLeft}"));
            if (ok) sprints++;
        }

        Expect(Text.L("성공한 달리기 (100 / 30)", "成功したダッシュ (100 / 30)"), 3, sprints);
        Expect(Text.L("남은 스태미나", "残りスタミナ"), 10, runner.StaminaLeft);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// TryUse 안의 Current -= amount; 줄에 중단점을 걸고, F10 으로 넘긴 뒤 this.Current 를 보세요. 줄었나요?
// 그다음 Shift+F11 로 Sprint 로 나와서 조사식에 _stamina.Current 를 넣어 보세요.
//
// [JA] TryUse の中の Current -= amount; の行にブレークポイントを置き、F10 で進めたあと this.Current を見てください。減りましたか?
// [JA] 次に Shift+F11 で Sprint に戻り、ウォッチに _stamina.Current を入れてみてください。
#endregion

#region 힌트 2 / ヒント 2
// 방금 TryUse 가 바꾼 this 는 _stamina 그 자체였을까요?
// Stamina 는 struct(값 형식)이고, _stamina 필드에는 어떤 한정자가 붙어 있나요?
//
// [JA] 今 TryUse が変更した this は、_stamina そのものだったでしょうか?
// [JA] Stamina は struct(値型)で、_stamina フィールドにはどんな修飾子が付いていますか?
#endregion

#region 힌트 3 / ヒント 3
// 컴파일러는 readonly 필드가 바뀌지 않도록, 그 필드의 메서드를 부를 때 "복사본"을 만들어서 부릅니다(방어적 복사).
// 그래서 경고도 오류도 없이 복사본만 줄어듭니다.
//
// [JA] コンパイラーは readonly フィールドが変わらないように、そのフィールドのメソッドを呼ぶときに「コピー」を作って呼びます(防御的コピー)。
// [JA] だから警告もエラーもなく、コピーだけが減ります。
#endregion
