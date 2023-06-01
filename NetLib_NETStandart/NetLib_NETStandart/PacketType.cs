using System;

namespace NetLib_NETStandart {
    public enum PacketType
    {
        TestPacket,
        ConnectPacket,
        ConnectAckPacket,
        HeartbeatPacket,
        HeartbeatAckPacket,
        DisconnectPacket,
        CustomPacket,
    }
}
