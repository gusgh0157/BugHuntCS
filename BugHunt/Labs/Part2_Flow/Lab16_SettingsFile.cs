using System.Globalization;

namespace BugHunt;

// 옵션을 텍스트 파일로 저장했다가 다음 실행 때 다시 읽는다.
// [JA] オプションをテキスト ファイルに保存しておき、次回の実行時に読み直します。
public static class Lab16
{
    record Options(double MoveSpeed, double MouseSensitivity);

    static string Save(Options o) =>
        $"moveSpeed={o.MoveSpeed}\nmouseSensitivity={o.MouseSensitivity}";

    // 파일 형식은 모든 나라에서 같아야 하니까 읽을 때는 InvariantCulture 로 읽는다.
    static Options Load(string text)
    {
        var values = new Dictionary<string, double>();
        foreach (string line in text.Split('\n'))
        {
            string[] kv = line.Split('=');
            values[kv[0]] = double.Parse(kv[1], CultureInfo.InvariantCulture);
        }
        return new Options(values["moveSpeed"], values["mouseSensitivity"]);
    }

    static Options SaveAndReload(Options options, string playerCulture)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            // 플레이어 PC 의 지역 설정을 흉내 낸다.
            CultureInfo.CurrentCulture = new CultureInfo(playerCulture);
            string file = Save(options);
            return Load(file);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    public static void Run()
    {
        var options = new Options(MoveSpeed: 1.5, MouseSensitivity: 0.75);

        foreach (string culture in new[] { "ko-KR", "en-US", "de-DE" })
        {
            Options loaded = SaveAndReload(options, culture);
            Console.WriteLine(Text.L($"  {culture}: 이동 속도 {loaded.MoveSpeed.ToString(CultureInfo.InvariantCulture)}, " +
                                     $"마우스 감도 {loaded.MouseSensitivity.ToString(CultureInfo.InvariantCulture)}",
                                     $"  {culture}: 移動速度 {loaded.MoveSpeed.ToString(CultureInfo.InvariantCulture)}、" +
                                     $"マウス感度 {loaded.MouseSensitivity.ToString(CultureInfo.InvariantCulture)}"));
            Expect(Text.L($"{culture} 플레이어의 이동 속도", $"{culture} のプレイヤーの移動速度"), 1.5, loaded.MoveSpeed);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// ko-KR 과 en-US 는 통과합니다. de-DE 만 다릅니다.
// SaveAndReload 의 return Load(file); 줄에 조건부 중단점을 거세요. 조건: playerCulture == "de-DE"
// 지역 창에서 file 값 옆의 돋보기(텍스트 시각화 도우미)를 눌러 저장된 내용을 보세요.
//
// [JA] ko-KR と en-US は通ります。de-DE だけが違います。
// [JA] SaveAndReload の return Load(file); の行に条件付きブレークポイントを置いてください。条件: playerCulture == "de-DE"
// [JA] ローカル ウィンドウで file の値の横にある虫眼鏡(テキスト ビジュアライザー)を押して、保存された内容を見てください。
#endregion

#region 힌트 2 / ヒント 2
// 조사식에 CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator 를 넣어 보세요.
// 그리고 double.Parse("1,5", CultureInfo.InvariantCulture) 도 넣어 보세요. 왜 그 값이 나올까요?
//
// [JA] ウォッチに CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator を入れてみてください。
// [JA] そして double.Parse("1,5", CultureInfo.InvariantCulture) も入れてみてください。なぜその値になるのでしょう?
#endregion

#region 힌트 3 / ヒント 3
// 읽는 쪽은 InvariantCulture 를 지정했습니다. 쓰는 쪽은요?
// 문자열 보간 $"{double}" 은 현재 문화권으로 숫자를 씁니다. 분석기 규칙 CA1305 가 이것을 잡습니다.
//
// [JA] 読む側は InvariantCulture を指定しました。書く側は?
// [JA] 文字列補間 $"{double}" は現在のカルチャで数値を書きます。アナライザーのルール CA1305 がこれを捕まえます。
#endregion
