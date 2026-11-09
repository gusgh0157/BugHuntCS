namespace BugHunt;

// 폭발 마법이 모든 적에게 피해를 주고, 쓰러진 적은 전리품을 남기고 사라진다.
// [JA] 爆発魔法がすべての敵にダメージを与え、倒れた敵はドロップ品を残して消えます。
public static class Lab03
{
    class Enemy(string name, int hp, int gold)
    {
        public string Name { get; } = name;
        public int Hp { get; set; } = hp;
        public int Gold { get; } = gold;
    }

    class Battle
    {
        readonly List<Enemy> _enemies =
        [
            new("Slime", 20, 5),
            new("Orc", 50, 30),
            new("Bat", 10, 3),
            new("Troll", 80, 60),
            new("Goblin", 25, 12),
        ];

        public int LootGold { get; private set; }
        public int Survivors => _enemies.Count;

        public void CastExplosion(int damage)
        {
            foreach (Enemy enemy in _enemies)
            {
                enemy.Hp -= damage;
                if (enemy.Hp <= 0)
                    OnEnemyDied(enemy);
            }
        }

        void OnEnemyDied(Enemy enemy)
        {
            LootGold += enemy.Gold;
            Despawn(enemy);
        }

        void Despawn(Enemy enemy)
        {
            _enemies.Remove(enemy);
            Console.WriteLine(Text.L($"  {enemy.Name} 쓰러짐 (+{enemy.Gold}G)", $"  {enemy.Name} 撃破 (+{enemy.Gold}G)"));
        }
    }

    public static void Run()
    {
        var battle = new Battle();
        battle.CastExplosion(30);
        Expect(Text.L("살아남은 적 (Orc, Troll)", "生き残った敵 (Orc, Troll)"), 2, battle.Survivors);
        Expect(Text.L("전리품 골드 (5 + 3 + 12)", "ドロップ品のゴールド (5 + 3 + 12)"), 20, battle.LootGold);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 디버거가 멈춘 줄은 어디인가요? 예외 메시지는 "컬렉션이 수정되었다"고 합니다.
// 그런데 멈춘 줄에는 컬렉션을 수정하는 코드가 없습니다.
//
// [JA] デバッガーが止まった行はどこですか? 例外メッセージは「コレクションが変更された」と言っています。
// [JA] ところが止まった行にはコレクションを変更するコードがありません。
#endregion

#region 힌트 2 / ヒント 2
// 호출 스택 창을 우클릭해서 "외부 코드 표시"를 켜 보세요. 맨 위에 무엇이 보이나요?
// foreach는 매 바퀴 MoveNext()를 부릅니다. 예외는 수정한 순간이 아니라 "다음 MoveNext"에서 납니다.
// 그래서 진짜 범인은 이미 호출 스택에서 사라졌습니다.
//
// [JA] 呼び出し履歴ウィンドウを右クリックして「外部コードの表示」をオンにしてみてください。一番上に何が見えますか?
// [JA] foreach は毎周 MoveNext() を呼びます。例外は変更した瞬間ではなく「次の MoveNext」で出ます。
// [JA] だから本当の犯人はすでに呼び出し履歴から消えています。
#endregion

#region 힌트 3 / ヒント 3
// _enemies를 바꾸는 줄에 중단점을 걸고 다시 실행하세요. 그 순간의 호출 스택에는 누가 있나요?
//
// [JA] _enemies を変更する行にブレークポイントを置いてもう一度実行してください。その瞬間の呼び出し履歴には誰がいますか?
#endregion
