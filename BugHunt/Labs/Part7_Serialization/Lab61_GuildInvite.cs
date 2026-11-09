using System.Xml;
using System.Xml.Linq;

namespace BugHunt;

// 길드 초대장을 XML 메시지로 만들어 보낸다. 받는 쪽은 XML 을 읽어 초대 창을 띄우고, 읽을 수 없는 메시지는 버린다.
// 어떤 길드의 초대장은 보내도 도착하지 않는다.
// [JA] ギルドの招待状を XML メッセージとして作って送ります。受け取る側は XML を読んで招待画面を表示し、読めないメッセージは捨てます。
// [JA] あるギルドの招待状は送っても届きません。
public static partial class Lab61
{
    // 보내는 쪽: 초대장 XML 만들기
    static string BuildInvite(string guild, string from, string message) =>
        $"<invite guild=\"{guild}\" from=\"{from}\"><message>{message}</message></invite>";

    // 받는 쪽: 읽을 수 없는 메시지는 버린다.
    static (string Guild, string From, string Message)? Receive(string xml)
    {
        try
        {
            XElement e = XElement.Parse(xml);
            return ((string?)e.Attribute("guild") ?? "", (string?)e.Attribute("from") ?? "", (string?)e.Element("message") ?? "");
        }
        catch (XmlException ex)
        {
            Console.WriteLine(Text.L($"  (받는 쪽) 읽을 수 없는 메시지를 버림: {ex.Message}", $"  (受信側) 読めないメッセージを破棄: {ex.Message}"));
            return null;
        }
    }

    public static void Run()
    {
        (string Guild, string From, string Message)[] invites = Pick(Invites[0], Invites[1..]);
        int arrived = 0;
        foreach ((string guild, string from, string message) in invites)
        {
            string xml = BuildInvite(guild, from, message);
            Console.WriteLine(Text.L($"  보냄: {xml}", $"  送信: {xml}"));
            var got = Receive(xml);
            if (got is { } g)
            {
                arrived++;
                Expect(Text.L($"{guild} 초대장 내용", $"{guild} の招待状の内容"), $"{guild}|{from}|{message}", $"{g.Guild}|{g.From}|{g.Message}");
            }
        }
        Expect(Text.L("도착한 초대장 수", "届いた招待状の数"), invites.Length, arrived);
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// "(받는 쪽) 읽을 수 없는 메시지를 버림" 줄의 XmlException 메시지에 줄과 위치(Line 1, position N)가 나옵니다. 바로 위 "보냄" 줄에서 그 위치의 글자를 세어 보세요.
// 예외 설정 창(Ctrl+Alt+E)에서 System.Xml.XmlException 을 체크하면 Parse 안에서 멈춥니다.
//
// [JA] 「(받는 쪽) 읽을 수 없는 메시지를 버림」(受信側: 読めないメッセージを破棄)の行の XmlException メッセージに行と位置(Line 1, position N)が出ます。すぐ上の「보냄」(送信)の行でその位置の文字を数えてみてください。
// [JA] 例外設定ウィンドウ(Ctrl+Alt+E)で System.Xml.XmlException にチェックを入れると Parse の中で止まります。
#endregion

#region 힌트 2 / ヒント 2
// XML 에서 < 와 & 는 문법 기호입니다. 텍스트에 그대로 넣으면 문서가 깨집니다. 속성 값 안에서는 " 도(따옴표로 감쌌다면) 깨뜨립니다.
// BuildInvite 는 문자열을 이어 붙여 XML 을 만드니, 플레이어가 입력한 길드 이름과 메시지가 그대로 문법 안으로 들어갑니다. 문법을 아는 쪽(XML API)이 만들지 않았기 때문입니다.
// 받는 쪽이 예외를 잡아 조용히 버리는 것도 증상을 "안 도착함"으로 바꿔 숨겼습니다.
//
// [JA] XML では < と & は文法記号です。テキストにそのまま入れると文書が壊れます。属性値の中では " も(引用符で囲んだ場合)壊します。
// [JA] BuildInvite は文字列をつなげて XML を作るので、プレイヤーが入力したギルド名とメッセージがそのまま文法の中に入り込みます。文法を知っている側(XML API)が作っていないからです。
// [JA] 受け取る側が例外を捕まえて黙って捨てていることも、症状を「届かない」に変えて隠しています。
#endregion

#region 힌트 3 / ヒント 3
// XML 은 XML API 로 만드세요. 이스케이프는 API 가 합니다:
//   new XElement("invite", new XAttribute("guild", guild), new XAttribute("from", from), new XElement("message", message)).ToString(SaveOptions.DisableFormatting)
// (XmlWriter 나 XmlDocument 도 같습니다.) 꼭 문자열로 만들어야 한다면 SecurityElement.Escape 로 값마다 이스케이프하세요.
// '<' 만 &lt; 로 바꾸는 수정은 숨은 케이스의 & 와 " 에서 걸립니다. 같은 실수가 SQL(주입), HTML(XSS), 셸 명령, JSON 문자열 이어 붙이기에도 그대로 있습니다.
//
// [JA] XML は XML API で作ってください。エスケープは API がやります:
// [JA]   new XElement("invite", new XAttribute("guild", guild), new XAttribute("from", from), new XElement("message", message)).ToString(SaveOptions.DisableFormatting)
// [JA] (XmlWriter や XmlDocument も同じです。)どうしても文字列で作るなら SecurityElement.Escape で値ごとにエスケープしてください。
// [JA] '<' だけ &lt; に変える修正は隠しケースの & と " で引っかかります。同じ間違いは SQL(インジェクション)、HTML(XSS)、シェルコマンド、JSON 文字列の連結にもそのまま存在します。
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 & 와 " 가 들어간 길드 이름·보낸 사람·메시지로 검사한다.
// [JA] 隠しケースは & と " が入ったギルド名・送信者・メッセージで検査します。
public static partial class Lab61
{
    static readonly (string, string, string)[][] Invites =
    [
        [(Text.L("새벽의 검", "暁の剣"), Text.L("아린", "アリン"), Text.L("같이 레이드 가요!", "一緒にレイド行こう!")), (Text.L("불꽃 연대", "炎の連隊"), Text.L("케인", "ケイン"), Text.L("레이드 같이 가요 <3", "レイド一緒に行こう <3"))],
        [(Text.L("검 & 방패", "剣 & 盾"), Text.L("미라", "ミラ"), Text.L("주말 공성전 함께해요", "週末の攻城戦、一緒にどう")), (Text.L("고요한 숲", "静かな森"), Text.L("도란", "ドラン"), Text.L("환영합니다", "歓迎します"))],
        [(Text.L("\"전설\" 길드", "\"伝説\" ギルド"), Text.L("리라", "リラ"), Text.L("초보 환영 & 친목", "初心者歓迎 & 交流")), (Text.L("별빛", "星明かり"), Text.L("오렌", "オレン"), Text.L("A<B 이면 가입!", "A<B なら加入!"))],
        [(Text.L("늑대 무리", "オオカミの群れ"), Text.L("케인 \"대장\"", "ケイン \"隊長\""), "1 < 2 & 3 > 2"), (Text.L("평화", "平和"), Text.L("아린", "アリン"), Text.L("안녕하세요", "こんにちは"))],
    ];
}
#endregion
