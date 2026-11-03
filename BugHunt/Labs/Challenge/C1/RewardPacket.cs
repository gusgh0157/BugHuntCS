using System.Text.Json;

namespace BugHunt.Challenge.C1;

// 서버와 클라이언트가 같이 쓰는 보상 패킷. 회선으로는 JSON 문자열로 오간다.
public sealed class RewardPacket
{
    // 패킷마다 붙는 고유 번호. 재전송된 패킷을 알아보는 데 쓴다.
    public Guid Id { get; private set; } = Guid.NewGuid();

    public int RaidId { get; set; }
    public string Member { get; set; } = "";
    public string Item { get; set; } = "";
    public int Count { get; set; } = 1;

    public override string ToString() => $"{Member} ← {Item} x{Count} ({Id.ToString()[..8]})";
}

// 회선 위의 형식. 받은 쪽이 확인(ack)을 보내야 보낸 쪽이 그 패킷을 잊는다.
public static class Wire
{
    static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Encode(RewardPacket packet) => JsonSerializer.Serialize(packet, Options);

    public static RewardPacket Decode(string json) =>
        JsonSerializer.Deserialize<RewardPacket>(json, Options) ?? throw new FormatException("빈 패킷");

    public static string EncodeAck(Guid id) => $"ACK {id}";

    public static bool TryDecodeAck(string text, out Guid id)
    {
        id = Guid.Empty;
        return text.StartsWith("ACK ", StringComparison.Ordinal) && Guid.TryParse(text.AsSpan(4), out id);
    }
}

// 믿을 수 없는 회선. 틱마다 쌓인 메시지를 건네준다.
// 패킷이 통째로 사라지거나(drop), 패킷은 도착했는데 ack 가 사라지는(ack loss) 일이 생긴다.
public sealed class UnreliableLink(Func<int, bool> dropPacket, Func<int, bool> dropAck)
{
    readonly Queue<string> _toClient = new();
    readonly Queue<string> _toServer = new();
    int _packetsSent, _acksSent;

    public int PacketsSent => _packetsSent;

    public void SendToClient(string json)
    {
        int n = _packetsSent++;
        if (!dropPacket(n))
            _toClient.Enqueue(json);
    }

    public void SendToServer(string ack)
    {
        int n = _acksSent++;
        if (!dropAck(n))
            _toServer.Enqueue(ack);
    }

    public IEnumerable<string> DrainClient()
    {
        while (_toClient.Count > 0)
            yield return _toClient.Dequeue();
    }

    public IEnumerable<string> DrainServer()
    {
        while (_toServer.Count > 0)
            yield return _toServer.Dequeue();
    }
}
