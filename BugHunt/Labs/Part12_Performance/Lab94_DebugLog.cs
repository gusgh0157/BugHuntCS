namespace BugHunt;

// 개발 중에 넣어 둔 디버그 로그. 출시판에서는 로그를 꺼 두었는데도 이동 처리 때 GC 가 자주 돈다.
// [JA] 開発中に入れておいたデバッグ ログ。製品版ではログをオフにしてあるのに、移動処理のときに GC が頻繁に走ります。
public static class Lab94
{
    static class Log
    {
        public static bool DebugEnabled;   // 출시판에서는 false

        public static void Debug(string format, params object?[] args)
        {
            if (!DebugEnabled)
                return;
            Console.WriteLine(format, args);
        }
    }

    struct Unit(int id, float x, float y)
    {
        public int Id = id;
        public float X = x, Y = y;
    }

    static void MoveAll(Unit[] units, float dx, float dy, int frame)
    {
        for (int i = 0; i < units.Length; i++)
        {
            units[i].X += dx;
            units[i].Y += dy;
            Log.Debug("프레임 {0}: 유닛 {1} 이동 → ({2}, {3})", frame, units[i].Id, units[i].X, units[i].Y);
        }
    }

    public static void Run()
    {
        Log.DebugEnabled = false;
        int count = Pick(200, 100, 400);
        int frames = Pick(200, 100, 300);
        var units = new Unit[count];
        for (int i = 0; i < count; i++)
            units[i] = new Unit(i, i, -i);
        MoveAll(units, 0, 0, -1);   // 처음 한 번만 드는 비용 빼기

        (long bytes, long ms) = Measure(() =>
        {
            for (int frame = 0; frame < frames; frame++)
                MoveAll(units, 0.5f, 0.25f, frame);
        });
        long calls = (long)count * frames;
        Console.WriteLine($"  유닛 {count} × {frames}프레임 이동. 로그는 꺼져 있음. 할당 {bytes:N0}바이트 (이동 1회당 {bytes / calls}), {ms}ms");

        Expect("마지막 유닛 위치", $"{count - 1 + frames * 0.5f}, {-(count - 1) + frames * 0.25f}", $"{units[^1].X}, {units[^1].Y}");
        ExpectAtMost("이동 1회당 할당 (바이트)", 1, bytes / calls);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 로그는 꺼져 있습니다. 그런데 .NET 개체 할당 추적에는 Object[], Int32, Single 이 잔뜩 보입니다. 누가 만들었을까요?
//
// [JA] ログはオフです。それなのに .NET オブジェクトの割り当ての追跡には Object[]、Int32、Single がたくさん見えます。誰が作ったのでしょう?
#endregion

#region 힌트 2 / ヒント 2
// Log.Debug 의 첫 줄(if (!DebugEnabled))에 중단점을 걸고, 멈췄을 때 지역 창에서 args 를 펼쳐 보세요. 이미 무엇이 들어 있나요?
// params 배열과 그 안의 상자(박싱된 int, float)는 Log.Debug 에 "들어가기 전에", 부르는 쪽에서 만들어집니다.
//
// [JA] Log.Debug の最初の行(if (!DebugEnabled))にブレークポイントを置き、止まったらローカル ウィンドウで args を展開してみてください。すでに何が入っていますか?
// [JA] params 配列とその中の箱(ボックス化された int、float)は、Log.Debug に「入る前に」呼び出し側で作られます。
#endregion

#region 힌트 3 / ヒント 3
// 로그를 끄는 검사가 함수 안에 있으면, 인자를 준비하는 비용은 이미 치른 뒤입니다. 부르는 쪽에서 if (Log.DebugEnabled) 로 감싸거나,
// 제네릭 오버로드(Debug<T1, T2>(...))로 배열과 박싱을 없애세요. ([Conditional("DEBUG")] 는 Release 에서만 호출을 지우므로 Debug 로 채점하는 이 랩은 통과하지 못합니다.)
// 큰 struct 를 값으로 여러 번 넘기는 복사도 비슷하게 "안 보이는 비용"입니다(in, ref readonly 매개변수).
//
// [JA] ログをオフにするチェックが関数の中にあると、引数を準備するコストはもう払ったあとです。呼び出し側で if (Log.DebugEnabled) で囲むか、
// [JA] ジェネリック オーバーロード(Debug<T1, T2>(...))で配列とボックス化をなくしてください。([Conditional("DEBUG")] は Release でしか呼び出しを消さないので、Debug で採点するこのラボは通りません。)
// [JA] 大きな struct を値で何度も渡すコピーも、同じように「見えないコスト」です(in、ref readonly 引数)。
#endregion
