namespace BugHunt;

// 체크포인트를 지날 때마다 슬롯에 자동 저장한다. 저장이 실패하면 알림만 띄우고 게임은 계속된다.
// [JA] チェックポイントを通るたびにスロットへ自動セーブします。セーブに失敗したら通知だけ出して、ゲームは続きます。
public static class Lab62
{
    class SaveSystem
    {
        public int SavedCount;

        public async void SaveAsync(string slot)
        {
            await Task.Delay(50);   // 디스크에 쓰는 척
            if (slot == "checkpoint_3")
                throw new IOException($"{slot}: 디스크 공간이 부족합니다");
            Interlocked.Increment(ref SavedCount);
            Console.WriteLine($"  {slot} 저장 완료");
        }
    }

    static int AutoSave(SaveSystem saves, string[] checkpoints)
    {
        int failures = 0;
        foreach (string checkpoint in checkpoints)
        {
            try
            {
                saves.SaveAsync(checkpoint);
            }
            catch (IOException ex)
            {
                failures++;
                Console.WriteLine($"  [알림] 저장 실패: {ex.Message}");
            }
        }
        return failures;
    }

    public static void Run()
    {
        var saves = new SaveSystem();
        int failures = AutoSave(saves, ["checkpoint_1", "checkpoint_2", "checkpoint_3"]);

        Thread.Sleep(500);   // 게임은 계속 진행된다

        Expect("저장 성공", 2, saves.SavedCount);
        Expect("실패 알림", 1, failures);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// throw 줄에서 멈췄을 때 호출 스택을 보세요. AutoSave 나 Run 이 보이나요?
// 스레드 창(Ctrl+Alt+H)에서 지금 스레드는 메인 스레드인가요, 작업자 스레드인가요?
//
// [JA] throw の行で止まったとき、呼び出し履歴を見てください。AutoSave や Run は見えますか?
// [JA] スレッド ウィンドウ(Ctrl+Alt+H)で、今のスレッドはメインスレッドですか、ワーカー スレッドですか?
#endregion

#region 힌트 2 / ヒント 2
// try/catch 는 "그 줄을 실행하는 동안" 던져진 예외만 잡습니다.
// AutoSave 의 try 블록 안에 중단점을 걸고, SaveAsync(checkpoint) 를 F10 으로 넘기는 데 얼마나 걸리는지 보세요.
// SaveAsync 가 돌려주는 것은 무엇인가요? 그 반환 형식에서 실패 여부를 알 방법이 있나요?
//
// [JA] try/catch は「その行を実行している間に」スローされた例外だけを捕まえます。
// [JA] AutoSave の try ブロックの中にブレークポイントを置き、SaveAsync(checkpoint) を F10 でステップ オーバーするのにどれくらいかかるか見てください。
// [JA] SaveAsync が返すものは何ですか? その戻り値の型から、失敗したかどうかを知る方法はありますか?
#endregion

#region 힌트 3 / ヒント 3
// async void 메서드의 예외는 호출한 쪽으로 돌아갈 길이 없습니다.
// 그래서 .NET 은 그 예외를 SynchronizationContext(콘솔에는 없으므로 스레드 풀)에 다시 던지고, 프로세스가 죽습니다.
// async void 를 써도 되는 곳은 이벤트 처리기처럼 형식이 정해져 있을 때뿐입니다.
//
// [JA] async void メソッドの例外は、呼び出し元へ戻る道がありません。
// [JA] そのため .NET はその例外を SynchronizationContext(コンソールにはないのでスレッド プール)で再スローし、プロセスが落ちます。
// [JA] async void を使ってよいのは、イベント ハンドラーのように型が決められている場合だけです。
#endregion
