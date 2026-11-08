namespace BugHunt;

// 길드는 다른 길드 밑으로 들어가 동맹이 될 수 있다. 동맹의 맹주(맨 위 길드)는 상위 길드를 따라 올라가서 찾는다.
// 상위 길드를 고리로 만드는 가입 신청(A 밑에 B, B 밑에 A)은 거절해야 한다.
// [JA] ギルドは別のギルドの下に入って同盟になれます。同盟の盟主(一番上のギルド)は、上位ギルドをたどって上っていって探します。
// [JA] 上位ギルドを輪にしてしまう加入申請(A の下に B、B の下に A)は拒否しなければなりません。
public static class Lab85
{
    class Guild(string name)
    {
        public string Name { get; } = name;
        public Guild? Parent { get; private set; }

        public bool JoinUnder(Guild parent)
        {
            if (parent == this || parent.Parent == this)
                return false;   // 고리가 생기는 가입은 거절
            Parent = parent;
            return true;
        }

        public Guild Leader()
        {
            Guild g = this;
            while (g.Parent != null)
                g = g.Parent;
            return g;
        }
    }

    public static void Run()
    {
        string[] names = ["Dawn", "Ember", "Frost", "Gale", "Haze", "Iron", "Jade", "Kite"];
        int count = Pick(4, 4, names.Length);
        Guild[] guilds = [.. names.Take(count).Select(n => new Guild(n))];

        // 기본 케이스: Ember→Dawn, Frost→Ember, Gale→Frost 사슬을 만든 뒤, Dawn 이 Gale 밑으로 들어가려 한다.
        // 숨은 케이스: 사슬 순서를 섞는다.
        int[] order = [.. Enumerable.Range(0, count)];
        if (Seed != 0)
            for (int i = order.Length - 1; i > 0; i--)
            {
                int j = Pick(0, 0, i);
                (order[i], order[j]) = (order[j], order[i]);
            }
        for (int i = 1; i < count; i++)
            guilds[order[i]].JoinUnder(guilds[order[i - 1]]);

        Guild top = guilds[order[0]];
        Guild bottom = guilds[order[^1]];
        bool accepted = top.JoinUnder(bottom);
        Console.WriteLine($"  {top.Name} → {bottom.Name} 밑으로 가입 신청: {(accepted ? "승인" : "거절")}");

        string leaders = "";
        if (!FinishesWithin("동맹 맹주 찾기", () => leaders = string.Join(", ", guilds.Select(g => g.Leader().Name))))
            return;

        Console.WriteLine($"  각 길드의 맹주: {leaders}");
        Expect("고리를 만드는 가입은 거절", false, accepted);
        Expect("각 길드의 맹주", string.Join(", ", guilds.Select(_ => top.Name)), leaders);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 멈추면 모두 중단 → "랩 본문" 스레드 → Leader() 프레임. 지역 창의 g 를 펼치고 Parent, Parent.Parent, ... 를 계속 펼쳐 보세요.
// 개체 ID(우클릭 → 개체 ID 만들기)를 g 에 붙이면 같은 길드가 다시 나오는 순간 $1 이 보입니다.
//
// [JA] 止まったら すべて中断 → 「ラボ本体」スレッド → Leader() のフレーム。ローカル ウィンドウの g を展開し、Parent、Parent.Parent、... を展開し続けてみてください。
// [JA] オブジェクト ID(右クリック → オブジェクト ID の作成)を g に付けると、同じギルドがまた出てきた瞬間に $1 が見えます。
#endregion

#region 힌트 2 / ヒント 2
// 가입 신청이 "승인"으로 나왔습니다. JoinUnder 의 거절 조건은 어떤 고리까지 막나요? A↔B 처럼 두 길드짜리 고리만인가요?
// 사슬이 Dawn ← Ember ← Frost ← Gale 일 때 Dawn 이 Gale 밑으로 들어가면 고리의 길이는 몇인가요?
//
// [JA] 加入申請が「승인」(承認)と出ました。JoinUnder の拒否条件はどんな輪まで防いでいますか? A↔B のような二つのギルドの輪だけですか?
// [JA] 鎖が Dawn ← Ember ← Frost ← Gale のとき、Dawn が Gale の下に入ると輪の長さはいくつですか?
#endregion

#region 힌트 3 / ヒント 3
// 새 부모에서 위로 끝까지 올라가는 동안 내 자신(this)을 만나면 고리입니다. 그 검사를 가입할 때 하세요.
// 데이터가 이미 망가졌을 때를 대비해 Leader() 같은 순회에 "방문한 노드 집합"이나 최대 단계를 두는 것도 방어가 됩니다.
//
// [JA] 新しい親から上へ最後までたどる間に自分自身(this)に出会ったら輪です。そのチェックを加入するときに行ってください。
// [JA] データがすでに壊れているときに備えて、Leader() のような巡回に「訪問済みノードの集合」や最大段数を設けるのも防御になります。
#endregion
