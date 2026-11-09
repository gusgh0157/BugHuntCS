using System.Xml.Serialization;

namespace BugHunt;

// 게임 옵션(그래픽, 키 설정, 볼륨)을 XML 로 저장한다. 옛 버전은 볼륨만 저장했는데, 그래픽과 키 설정을 추가하자 옵션을 저장하는 순간 죽는다.
// [JA] ゲームオプション(グラフィック、キー設定、音量)を XML で保存します。旧バージョンは音量だけ保存していましたが、グラフィックとキー設定を追加したらオプションを保存した瞬間に落ちます。
public static partial class Lab59
{
    // XmlSerializer 가 다루는 형식은 public 이어야 한다.
    public sealed record GraphicsOptions(int Width, int Height, bool Fullscreen);

    public sealed class GameOptions
    {
        public int Volume { get; set; } = 80;
        public GraphicsOptions Graphics { get; set; } = new(1280, 720, false);
        public Dictionary<string, string> KeyBindings { get; set; } = [];
    }

    static string Save(GameOptions options)
    {
        var serializer = new XmlSerializer(typeof(GameOptions));
        using var writer = new StringWriter();
        serializer.Serialize(writer, options);
        return writer.ToString();
    }

    static GameOptions Load(string xml)
    {
        var serializer = new XmlSerializer(typeof(GameOptions));
        using var reader = new StringReader(xml);
        return (GameOptions)(serializer.Deserialize(reader) ?? throw new FormatException(Text.L("빈 옵션", "空のオプション")));
    }

    public static void Run()
    {
        (int W, int H) res = Pick((1920, 1080), (2560, 1440), (1366, 768), (3840, 2160));
        var options = new GameOptions
        {
            Volume = Pick(65, 0, 100),
            Graphics = new GraphicsOptions(res.W, res.H, Pick(true, true, false)),
        };
        foreach ((string action, string key) in Pick(Bindings[0], Bindings[1..]))
            options.KeyBindings[action] = key;

        string xml = Save(options);
        Console.WriteLine(Text.L($"  저장한 옵션 XML: {xml.Length}자", $"  保存したオプション XML: {xml.Length}文字"));

        GameOptions loaded = Load(xml);
        Console.WriteLine(Text.L($"  불러온 옵션: 볼륨 {loaded.Volume}, {loaded.Graphics.Width}x{loaded.Graphics.Height}, 키 {loaded.KeyBindings.Count}개", $"  ロードしたオプション: 音量 {loaded.Volume}、{loaded.Graphics.Width}x{loaded.Graphics.Height}、キー {loaded.KeyBindings.Count}個"));

        Expect(Text.L("볼륨", "音量"), options.Volume, loaded.Volume);
        Expect(Text.L("해상도", "解像度"), $"{options.Graphics.Width}x{options.Graphics.Height}", $"{loaded.Graphics.Width}x{loaded.Graphics.Height}");
        Expect(Text.L("전체 화면", "フルスクリーン"), options.Graphics.Fullscreen, loaded.Graphics.Fullscreen);
        Expect(Text.L("키 설정", "キー設定"), string.Join(",", options.KeyBindings.OrderBy(p => p.Key, StringComparer.Ordinal)), string.Join(",", loaded.KeyBindings.OrderBy(p => p.Key, StringComparer.Ordinal)));
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// 예외 메시지는 "There was an error reflecting type ..." 뿐입니다. 이것만으로는 무엇이 문제인지 모릅니다.
// 예외 도우미의 "내부 예외 보기"를 끝까지 따라가세요(또는 조사식에 $exception.InnerException, $exception.InnerException.InnerException). 맨 안쪽 예외가 진짜 이유입니다.
//
// [JA] 例外メッセージは "There was an error reflecting type ..." だけです。これだけでは何が問題なのか分かりません。
// [JA] 例外ヘルパーの「内部例外の表示」を最後までたどってください(またはウォッチに $exception.InnerException、$exception.InnerException.InnerException)。一番内側の例外が本当の理由です。
#endregion

#region 힌트 2 / ヒント 2
// XmlSerializer 는 new XmlSerializer(typeof(...)) 를 만드는 순간 형식 전체를 훑어서 읽기·쓰기 코드를 만듭니다. 그래서 데이터가 무엇이든 생성자에서 죽습니다.
// XmlSerializer 의 규칙: (1) public 형식, (2) 매개변수 없는 생성자, (3) public 이고 읽고 쓸 수 있는 속성/필드만, (4) IDictionary 를 구현한 형식(Dictionary)은 지원 안 함.
// 위치 기반 record(GraphicsOptions)는 매개변수 없는 생성자가 없습니다. 그것을 고치면 다음 내부 예외로 Dictionary 가 나옵니다. 문제는 두 개입니다.
//
// [JA] XmlSerializer は new XmlSerializer(typeof(...)) を作った瞬間に型全体を走査して読み書きのコードを作ります。そのためデータが何であれコンストラクターで落ちます。
// [JA] XmlSerializer の規則: (1) public 型、(2) 引数なしのコンストラクター、(3) public で読み書きできるプロパティ/フィールドだけ、(4) IDictionary を実装した型(Dictionary)は非対応。
// [JA] 位置指定の record(GraphicsOptions)には引数なしのコンストラクターがありません。それを直すと次の内部例外で Dictionary が出てきます。問題は二つです。
#endregion

#region 힌트 3 / ヒント 3
// GraphicsOptions: 매개변수 없는 생성자와 public set 이 있는 class 로 바꾸세요(public int Width { get; set; } ...).
// KeyBindings: XML 에는 목록으로 두고 Dictionary 는 코드 쪽에만 두세요. 예:
//   [XmlIgnore] public Dictionary<string, string> KeyBindings { get; set; } = [];
//   [XmlArray("KeyBindings")] public KeyBinding[] KeyBindingList
//   {
//       get => [.. KeyBindings.Select(p => new KeyBinding { Action = p.Key, Key = p.Value })];
//       set => KeyBindings = value.ToDictionary(b => b.Action, b => b.Key);
//   }
//   public sealed class KeyBinding { [XmlAttribute] public string Action { get; set; } = ""; [XmlAttribute] public string Key { get; set; } = ""; }
// 아니면 이 옵션 파일은 System.Text.Json 으로 저장하세요. JSON 은 record 와 Dictionary 를 바로 다룹니다. 그것도 올바른 수정입니다.
// try/catch 로 저장 실패를 삼키면 옵션이 저장되지 않습니다. 숨은 테스트는 다른 값과 키 설정으로 왕복을 검사합니다.
// (XmlSerializer 는 XXE 같은 외부 엔터티 공격에 대해 .NET Core 에서 기본으로 안전합니다. 보안 쪽은 해답의 참고를 보세요.)
//
// [JA] GraphicsOptions: 引数なしのコンストラクターと public set を持つ class に変えてください(public int Width { get; set; } ...)。
// [JA] KeyBindings: XML にはリストとして置き、Dictionary はコード側だけに置いてください。例:
// [JA]   [XmlIgnore] public Dictionary<string, string> KeyBindings { get; set; } = [];
// [JA]   [XmlArray("KeyBindings")] public KeyBinding[] KeyBindingList
// [JA]   {
// [JA]       get => [.. KeyBindings.Select(p => new KeyBinding { Action = p.Key, Key = p.Value })];
// [JA]       set => KeyBindings = value.ToDictionary(b => b.Action, b => b.Key);
// [JA]   }
// [JA]   public sealed class KeyBinding { [XmlAttribute] public string Action { get; set; } = ""; [XmlAttribute] public string Key { get; set; } = ""; }
// [JA] あるいはこのオプションファイルは System.Text.Json で保存してください。JSON は record と Dictionary をそのまま扱えます。それも正しい修正です。
// [JA] try/catch で保存の失敗を飲み込むとオプションが保存されません。隠しテストは別の値とキー設定で往復を検査します。
// [JA] (XmlSerializer は XXE のような外部エンティティ攻撃に対して .NET Core では既定で安全です。セキュリティ面は解答の参考を見てください。)
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 다른 해상도·볼륨·키 설정으로 왕복을 검사한다.
// [JA] 隠しケースは別の解像度・音量・キー設定で往復を検査します。
public static partial class Lab59
{
    static readonly (string, string)[][] Bindings =
    [
        [("jump", "Space"), ("attack", "J"), ("inventory", "I")],
        [("jump", "W"), ("dash", "LeftShift")],
        [("attack", "Mouse0"), ("skill1", "Q"), ("skill2", "E"), ("map", "M")],
        [("inventory", "Tab")],
    ];
}
#endregion
