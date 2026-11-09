using System.Globalization;
using System.Xml;

namespace BugHunt;

// 퀘스트 데이터를 XML 에서 읽어 보상 골드를 찾는다. 기획팀이 XML 에 스키마(네임스페이스)를 붙인 뒤로 모든 퀘스트의 보상이 0G 다.
// [JA] クエストデータを XML から読んで報酬ゴールドを探します。企画チームが XML にスキーマ(名前空間)を付けてから、すべてのクエストの報酬が 0G です。
public static partial class Lab60
{
    static int RewardGold(XmlDocument doc, string questId)
    {
        XmlNode? reward = doc.SelectSingleNode($"/quests/quest[@id='{questId}']/reward");
        string? gold = reward?.Attributes?["gold"]?.Value;
        return gold == null ? 0 : int.Parse(gold, CultureInfo.InvariantCulture);
    }

    public static void Run()
    {
        (string Id, string Title, int Gold)[] quests = Pick(Sets[0], Sets[1..]);
        string body = string.Concat(quests.Select(q => $"""

              <quest id="{q.Id}">
                <title>{q.Title}</title>
                <reward gold="{q.Gold.ToString(CultureInfo.InvariantCulture)}" />
              </quest>
            """));
        string xml = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <quests xmlns="urn:bughunt:quests">{body}
            </quests>
            """;

        var doc = new XmlDocument();
        doc.LoadXml(xml);

        foreach ((string id, string title, int gold) in quests)
        {
            int found = RewardGold(doc, id);
            Console.WriteLine(Text.L($"  {id} {title}: 보상 {found}G", $"  {id} {title}: 報酬 {found}G"));
            Expect(Text.L($"{id} 보상 골드", $"{id} の報酬ゴールド"), gold, found);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 詰まったら一つずつ開いてください。(Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追ってから開いてください)
// RewardGold 에 중단점을 걸고 조사식에 넣어 보세요. 어느 단계에서 null 이 되나요?
//   doc.SelectSingleNode("/quests")
//   doc.DocumentElement.Name, doc.DocumentElement.NamespaceURI
//   doc.SelectSingleNode("/*[local-name()='quests']")
//
// [JA] RewardGold にブレークポイントを置いてウォッチに入れてみてください。どの段階で null になりますか?
// [JA]   doc.SelectSingleNode("/quests")
// [JA]   doc.DocumentElement.Name, doc.DocumentElement.NamespaceURI
// [JA]   doc.SelectSingleNode("/*[local-name()='quests']")
#endregion

#region 힌트 2 / ヒント 2
// xmlns="urn:bughunt:quests" 는 "이 요소와 그 안의 모든 요소는 이 네임스페이스에 속한다"는 기본 네임스페이스 선언입니다. 문서의 요소 이름은 이제 (urn:bughunt:quests, quests) 입니다.
// XPath 의 접두어 없는 이름 /quests 는 "네임스페이스가 없는 quests" 만 찾습니다. XPath 1.0 에는 "기본 네임스페이스"라는 개념이 없습니다. 그래서 첫 단계부터 아무것도 못 찾고, 코드가 null 을 0G 로 바꿉니다.
// 속성(id, gold)은 접두어가 없으면 네임스페이스에 속하지 않으므로 그대로 @id 로 찾아도 됩니다.
//
// [JA] xmlns="urn:bughunt:quests" は「この要素とその中のすべての要素はこの名前空間に属する」という既定の名前空間の宣言です。文書の要素名は今や (urn:bughunt:quests, quests) です。
// [JA] XPath の接頭辞なしの名前 /quests は「名前空間のない quests」だけを探します。XPath 1.0 には「既定の名前空間」という概念がありません。そのため最初の段階から何も見つからず、コードが null を 0G に変えます。
// [JA] 属性(id, gold)は接頭辞がなければ名前空間に属さないので、そのまま @id で探して構いません。
#endregion

#region 힌트 3 / ヒント 3
// 네임스페이스에 접두어를 붙여 XPath 에 알려 주세요:
//   var ns = new XmlNamespaceManager(doc.NameTable);
//   ns.AddNamespace("q", "urn:bughunt:quests");
//   doc.SelectSingleNode($"/q:quests/q:quest[@id='{questId}']/q:reward", ns);
// LINQ to XML 이면 XNamespace q = "urn:bughunt:quests"; 그리고 root.Elements(q + "quest") 처럼 이름에 네임스페이스를 붙입니다.
// 더 큰 문제는 "못 찾으면 0" 입니다. 필수 데이터를 못 찾았을 때는 기본값 대신 예외를 내야 이런 버그가 조용히 숨지 않습니다.
// (참고: PreserveWhitespace = true 로 읽으면 들여쓰기도 텍스트 노드가 되어 ChildNodes/FirstChild 가 엉뚱한 노드를 가리킵니다. 요소만 볼 때는 SelectNodes 나 Elements() 를 쓰세요.)
//
// [JA] 名前空間に接頭辞を付けて XPath に教えてください:
// [JA]   var ns = new XmlNamespaceManager(doc.NameTable);
// [JA]   ns.AddNamespace("q", "urn:bughunt:quests");
// [JA]   doc.SelectSingleNode($"/q:quests/q:quest[@id='{questId}']/q:reward", ns);
// [JA] LINQ to XML なら XNamespace q = "urn:bughunt:quests"; そして root.Elements(q + "quest") のように名前に名前空間を付けます。
// [JA] より大きな問題は「見つからなければ 0」です。必須データが見つからないときは既定値の代わりに例外を出さないと、この種のバグが黙って隠れます。
// [JA] (参考: PreserveWhitespace = true で読むとインデントもテキストノードになり、ChildNodes/FirstChild が想定外のノードを指します。要素だけを見るときは SelectNodes や Elements() を使ってください。)
#endregion

#region 채점용 숨은 테스트 / 採点用の隠しテスト
// 숨은 케이스는 다른 퀘스트 Id·보상으로 검사한다. 보이는 보상 값을 박아 넣으면 여기서 걸린다.
// [JA] 隠しケースは別のクエスト Id・報酬で検査します。見えている報酬の値を埋め込むとここで引っかかります。
public static partial class Lab60
{
    static readonly (string, string, int)[][] Sets =
    [
        [("q1", Text.L("늑대 사냥", "オオカミ狩り"), 120), ("q2", Text.L("약초 수집", "薬草集め"), 80)],
        [("m7", Text.L("고블린 소탕", "ゴブリン掃討"), 300), ("m8", Text.L("잃어버린 반지", "なくした指輪"), 450), ("m9", Text.L("유적 탐사", "遺跡探索"), 1000)],
        [("d1", Text.L("일일 출석", "デイリー出席"), 10)],
        [("e2", Text.L("할로윈 호박 모으기", "ハロウィンかぼちゃ集め"), 777), ("e3", Text.L("유령 퇴치", "幽霊退治"), 666)],
    ];
}
#endregion
