namespace BugHunt;

// 보스가 등장하면 카메라를 한 프레임에 step 씩 줌한다. 목표 배율에 닿으면 연출 끝.
// [JA] ボスが登場したら、カメラを 1 フレームに step ずつズームします。目標倍率に届いたら演出終了です。
public static class Lab83
{
    class Camera
    {
        public float Zoom { get; set; } = 1.0f;
        public int FramesRendered { get; private set; }
        public void Render() => FramesRendered++;
    }

    static void ZoomTo(Camera camera, float target, float step)
    {
        for (float zoom = camera.Zoom; zoom != target; zoom += step)
        {
            camera.Zoom = zoom;
            camera.Render();
        }
        camera.Zoom = target;
    }

    public static void Run()
    {
        float step = Pick(0.1f, 0.05f, 0.1f, 0.2f, 0.25f, 0.3f);
        int frames = Pick(10, 4, 30);
        float start = Pick(1.0f, 0.5f, 1.0f, 1.5f, 2.0f);
        float target = start + frames * step;

        var camera = new Camera { Zoom = start };
        Console.WriteLine($"  보스 등장! 줌 {start:0.00}x → {target:0.00}x (프레임마다 +{step})");

        if (!FinishesWithin("보스 등장 줌 연출", () => ZoomTo(camera, target, step)))
            return;

        Console.WriteLine($"  연출 끝: {camera.FramesRendered}프레임, 줌 {camera.Zoom:0.00}x");
        Expect("연출 프레임 수", frames, camera.FramesRendered);
        Expect("최종 줌", target, camera.Zoom);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 멈추면 디버그 → 모두 중단(Ctrl+Alt+Break). 스레드 창(Ctrl+Alt+H)에서 "랩 본문" 스레드를 더블클릭하세요. 어느 줄에 있나요?
// 거기서 지역 창의 zoom 과 target 을 보세요. 이미 지나쳤나요?
//
// [JA] 止まったら デバッグ → すべて中断(Ctrl+Alt+Break)。スレッド ウィンドウ(Ctrl+Alt+H)で「ラボ本体」スレッドをダブルクリックしてください。どの行にいますか?
// [JA] そこでローカル ウィンドウの zoom と target を見てください。もう通り過ぎていますか?
#endregion

#region 힌트 2 / ヒント 2
// for 줄에 조건부 중단점: zoom > target - 0.01f. 조사식에 (double)zoom, (double)target, zoom - target 을 넣어 보세요.
// 화면에 2.00 으로 보이는 두 float 가 같은 값인가요? (랩 13과 같은 원인이지만, 이번에는 결과가 "틀림"이 아니라 "안 끝남"입니다)
//
// [JA] for の行に条件付きブレークポイント: zoom > target - 0.01f。ウォッチに (double)zoom、(double)target、zoom - target を入れてみてください。
// [JA] 画面に 2.00 と表示される二つの float は同じ値ですか? (ラボ 13 と同じ原因ですが、今回は結果が「間違い」ではなく「終わらない」です)
#endregion

#region 힌트 3 / ヒント 3
// 실수 누적 값을 != 로 종료 조건에 쓰면, 딱 그 값을 밟지 않는 순간 영원히 돕니다.
// 반복 횟수를 정수로 미리 정하고(프레임 수), 배율은 그 정수로 계산하세요. (MathF.Round((target - start) / step))
//
// [JA] 浮動小数点の累積値を != で終了条件に使うと、ちょうどその値を踏まなかった瞬間に永遠に回り続けます。
// [JA] 繰り返し回数を整数で先に決め(フレーム数)、倍率はその整数から計算してください。(MathF.Round((target - start) / step))
#endregion
