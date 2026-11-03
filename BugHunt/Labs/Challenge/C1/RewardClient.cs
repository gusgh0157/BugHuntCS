namespace BugHunt.Challenge.C1;

// 클라이언트 쪽 보상 수신함. 받은 패킷마다 ack 를 보내고, 처음 보는 패킷이면 RewardArrived 를 알린다.
public sealed class RewardInbox(UnreliableLink link)
{
    readonly HashSet<Guid> _seen = [];

    public event Action<RewardPacket>? RewardArrived;

    public int Received { get; private set; }
    public int Duplicates { get; private set; }

    public void Tick()
    {
        foreach (string json in link.DrainClient())
        {
            RewardPacket packet;
            try
            {
                packet = Wire.Decode(json);
            }
            catch (FormatException)
            {
                continue;   // 깨진 패킷은 ack 없이 버린다. 서버가 다시 보낸다.
            }

            Received++;
            link.SendToServer(Wire.EncodeAck(packet.Id));

            if (!_seen.Add(packet.Id))
            {
                Duplicates++;
                continue;   // 재전송된 패킷. ack 만 다시 보내고 무시
            }
            RewardArrived?.Invoke(packet);
        }
    }
}

// 파티원마다 보이는 가방 창. 수신함의 알림을 받아 아이템 줄을 추가한다.
public sealed class InventoryView
{
    readonly Dictionary<string, List<string>> _lines = [];

    public void Attach(RewardInbox inbox) => inbox.RewardArrived += OnRewardArrived;

    void OnRewardArrived(RewardPacket packet)
    {
        if (!_lines.TryGetValue(packet.Member, out List<string>? lines))
            _lines[packet.Member] = lines = [];
        lines.Add(packet.Count == 1 ? packet.Item : $"{packet.Item} x{packet.Count}");
    }

    public IReadOnlyList<string> LinesOf(string member) => _lines.TryGetValue(member, out List<string>? lines) ? lines : [];
}
