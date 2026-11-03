namespace BugHunt;

// 가방 화면은 아이템 아이콘을 많이 그린다. 디스크에서 읽은 아이콘은 캐시에 두고, 캐시에 있으면 디스크를 건드리지 않는다.
// [JA] バッグ画面はアイテムのアイコンをたくさん描きます。ディスクから読んだアイコンはキャッシュに置き、キャッシュにあればディスクに触れません。
public static class Lab23
{
    class IconLoader
    {
        readonly Dictionary<string, string> _cache = [];
        public int DiskReads { get; private set; }

        bool LoadFromDisk(string id, out string icon)
        {
            DiskReads++;
            icon = $"[{id}.png]";
            _cache[id] = icon;
            return true;
        }

        public string Get(string id)
        {
            if (_cache.TryGetValue(id, out string? icon) | LoadFromDisk(id, out icon))
                return icon;
            return "[?]";
        }
    }

    public static void Run()
    {
        string[] bag = ["sword", "potion", "potion", "potion", "shield", "sword", "potion"];
        if (Seed != 0)
        {
            string[] ids = ["bow", "arrow", "ring", "gem", "map", "key"];
            bag = Enumerable.Range(0, Pick(7, 5, 30)).Select(_ => Pick("x", ids)).ToArray();
        }

        var loader = new IconLoader();
        string drawn = string.Join(" ", bag.Select(loader.Get));
        Console.WriteLine($"  그린 아이콘: {drawn}");
        Console.WriteLine($"  디스크 읽기: {loader.DiskReads}번 (아이템 {bag.Length}칸, 종류 {bag.Distinct().Count()}개)");

        Expect("그린 아이콘", string.Join(" ", bag.Select(id => $"[{id}.png]")), drawn);
        Expect("디스크 읽기 횟수 (종류마다 한 번)", bag.Distinct().Count(), loader.DiskReads);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// LoadFromDisk 에 중단점을 거세요. 이미 캐시에 있는 "potion" 인데도 멈추나요?
// 멈췄을 때 호출 스택에서 Get 프레임을 더블클릭하고, if 줄을 천천히 읽어 보세요.
//
// [JA] LoadFromDisk にブレークポイントを置いてください。すでにキャッシュにある "potion" なのに止まりますか?
// [JA] 止まったら呼び出し履歴で Get のフレームをダブルクリックし、if の行をゆっくり読んでみてください。
#endregion

#region 힌트 2 / ヒント 2
// 조사식에 true | Side() 와 true || Side() 처럼 넣으면 무엇이 다른지 볼 수 있습니다(조사식도 함수를 실제로 부릅니다).
// || 는 왼쪽이 true 면 오른쪽을 계산하지 않습니다(단락 평가). | 는 bool 에 써도 컴파일되지만, 양쪽을 언제나 다 계산합니다.
//
// [JA] ウォッチに true | Side() と true || Side() のように入れると、何が違うかを見られます(ウォッチも関数を実際に呼びます)。
// [JA] || は左が true なら右を評価しません(短絡評価)。| は bool に使ってもコンパイルできますが、両側を必ず評価します。
#endregion

#region 힌트 3 / ヒント 3
// "캐시에 있으면, 아니면 디스크에서" 는 || 입니다. | 와 & 는 비트 연산(그리고 양쪽을 다 계산하는 논리 연산)입니다.
// 같은 실수가 if (target == null | target.Hp <= 0) 에 있으면, target 이 null 일 때 오른쪽이 계산돼 NullReferenceException 이 납니다.
//
// [JA] 「キャッシュにあれば、なければディスクから」は || です。| と & はビット演算(そして両側を評価する論理演算)です。
// [JA] 同じミスが if (target == null | target.Hp <= 0) にあると、target が null のときに右側が評価されて NullReferenceException が出ます。
#endregion
