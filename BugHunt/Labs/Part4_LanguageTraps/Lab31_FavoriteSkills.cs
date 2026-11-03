namespace BugHunt;

// 즐겨찾기한 스킬을 세이브에 저장했다가 다음 접속에 별표로 표시한다. 지난 세션은 스킬 이름의 GetHashCode 를 저장했다.
// 이번 접속에서는 즐겨찾기가 하나도 표시되지 않는다.
// [JA] お気に入りに登録したスキルをセーブに保存し、次回ログイン時に星で表示します。前回のセッションはスキル名の GetHashCode を保存しました。
// [JA] 今回のログインではお気に入りが一つも表示されません。
public static partial class Lab31
{
    // 지난 세션이 저장해 둔 즐겨찾기 해시(그 프로세스의 GetHashCode 값). 재현을 위해 안정 해시로 대신 둔다.
    static int OldSessionHash(string s)
    {
        uint h = 2166136261;
        foreach (char c in s) { h ^= c; h *= 16777619; }
        return (int)h;
    }

    public static void Run()
    {
        string[] favorites = Pick(Sets[0], Sets[1..]);
        // 세이브 파일에 저장된 즐겨찾기 해시 (지난 세션의 GetHashCode 결과)
        HashSet<int> savedHashes = [.. favorites.Select(OldSessionHash)];

        string[] allSkills = ["화염구", "서리창", "치유", "축복", "질풍베기", "대지분쇄", "그림자 일격"];
        // 이번 세션: 스킬 이름의 해시로 즐겨찾기인지 판정한다.
        string[] starred = [.. allSkills.Where(s => savedHashes.Contains(s.GetHashCode()))];

        Console.WriteLine($"  저장된 즐겨찾기: {string.Join(", ", favorites)}");
        Console.WriteLine($"  이번 세션 별표: {(starred.Length == 0 ? "(없음)" : string.Join(", ", starred))}");

        Expect("별표가 붙은 스킬 수", favorites.Length, starred.Length);
        Expect("즐겨찾기 스킬이 전부 별표", true, favorites.All(starred.Contains));
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// starred 계산 줄에서 멈추고 조사식에 넣어 보세요: "화염구".GetHashCode() 와 savedHashes 의 첫 값.
// 프로그램을 멈추고 다시 F5 로 실행한 뒤 같은 조사식을 보세요. "화염구".GetHashCode() 값이 실행할 때마다 다릅니다.
//
// [JA] starred を計算する行で止めてウォッチに入れてみてください: "화염구".GetHashCode() と savedHashes の最初の値。
// [JA] プログラムを止めてもう一度 F5 で実行し、同じウォッチを見てください。"화염구".GetHashCode() の値が実行ごとに違います。
#endregion

#region 힌트 2 / ヒント 2
// .NET Core 이후 string.GetHashCode 는 보안(해시 DoS 방지)을 위해 프로세스마다 무작위 시드를 씁니다.
// 같은 프로세스 안에서는 안정적이지만, 프로그램을 껐다 켜면 "화염구"의 해시가 완전히 달라집니다.
// 그래서 지난 세션이 저장한 해시와 이번 세션이 계산한 해시가 영영 맞지 않습니다. 예외도 경고도 없습니다.
//
// [JA] .NET Core 以降、string.GetHashCode はセキュリティ(ハッシュ DoS 対策)のためプロセスごとにランダムなシードを使います。
// [JA] 同じプロセスの中では安定していますが、プログラムを再起動すると "화염구" のハッシュがまったく変わります。
// [JA] そのため前回のセッションが保存したハッシュと今回計算したハッシュが永遠に一致しません。例外も警告もありません。
#endregion

#region 힌트 3 / ヒント 3
// GetHashCode 의 결과를 디스크·DB·네트워크로 내보내 "나중에 다시 비교"하면 안 됩니다. 영속되는 값은 다음 중 하나로:
//   - 키 자체(스킬 이름)를 저장하고 이름으로 비교한다.
//   - 프로세스와 무관하게 고정된 해시(FNV-1a, SHA-256 등)를 직접 구현해 쓴다 (여기 OldSessionHash 가 FNV 예시).
// GetHashCode 는 메모리 안의 Dictionary/HashSet 처럼 "한 번 실행되는 동안"만 쓰세요. 같은 함정이 Enum.GetHashCode, 익명 형식에도 있습니다.
//
// [JA] GetHashCode の結果をディスク・DB・ネットワークに出して「あとで比較し直す」のはいけません。永続させる値は次のいずれかに:
// [JA]   - キー自体(スキル名)を保存し、名前で比較する。
// [JA]   - プロセスに依存しない固定ハッシュ(FNV-1a、SHA-256 など)を自分で実装して使う(ここの OldSessionHash が FNV の例)。
// [JA] GetHashCode はメモリ内の Dictionary/HashSet のように「一度の実行の間」だけ使ってください。同じ罠が Enum.GetHashCode や匿名型にもあります。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 다른 즐겨찾기 조합으로 검사한다. GetHashCode 대신 이름이나 안정 해시로 고치면 통과한다.
// [JA] 隠しケースは別のお気に入りの組み合わせで検査します。GetHashCode の代わりに名前か安定ハッシュで直せば通ります。
public static partial class Lab31
{
    static readonly string[][] Sets =
    [
        ["화염구", "치유", "질풍베기"],
        ["서리창", "축복"],
        ["대지분쇄", "그림자 일격", "화염구", "치유"],
        ["축복", "질풍베기", "서리창"],
    ];
}
#endregion
