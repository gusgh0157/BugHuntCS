namespace BugHunt.Challenge.C1;

// 레이드가 끝나면 드롭마다 보상 패킷을 만들어 보낸다.
// 일정 틱 안에 ack 가 안 오면 같은 패킷을 다시 보낸다(재전송). 받는 쪽은 Id 로 중복을 걸러야 한다.
// [JA] レイドが終わったら、ドロップごとに報酬パケットを作って送ります。
// [JA] 一定ティック内に ack が来なければ同じパケットをもう一度送ります(再送)。受け取る側は Id で重複を除かなければなりません。
public sealed class RewardServer(UnreliableLink link)
{
    const int ResendAfterTicks = 3;
    const int MaxAttempts = 8;

    sealed class Outgoing(RewardPacket packet)
    {
        public RewardPacket Packet { get; } = packet;
        public int SentAtTick;
        public int Attempts;
    }

    readonly Dictionary<Guid, Outgoing> _unacked = [];
    int _tick;

    public int Resends { get; private set; }
    public int Unacked => _unacked.Count;

    public void Distribute(int raidId, IEnumerable<(string Member, string Item)> drops)
    {
        foreach ((string member, string item) in drops)
        {
            var packet = new RewardPacket { RaidId = raidId, Member = member, Item = item };
            var outgoing = new Outgoing(packet);
            _unacked[packet.Id] = outgoing;
            Transmit(outgoing);
        }
    }

    // 게임 서버의 한 틱
    public void Tick()
    {
        _tick++;
        foreach (string message in link.DrainServer())
            if (Wire.TryDecodeAck(message, out Guid id))
                _unacked.Remove(id);

        foreach (Outgoing outgoing in _unacked.Values.ToList())
        {
            if (_tick - outgoing.SentAtTick < ResendAfterTicks)
                continue;
            if (outgoing.Attempts >= MaxAttempts)
            {
                Console.WriteLine(Text.L($"  [서버] 포기: {outgoing.Packet}", $"  [サーバー] 断念: {outgoing.Packet}"));
                _unacked.Remove(outgoing.Packet.Id);
                continue;
            }
            Resends++;
            Transmit(outgoing);
        }
    }

    void Transmit(Outgoing outgoing)
    {
        outgoing.Attempts++;
        outgoing.SentAtTick = _tick;
        link.SendToClient(Wire.Encode(outgoing.Packet));
    }
}
