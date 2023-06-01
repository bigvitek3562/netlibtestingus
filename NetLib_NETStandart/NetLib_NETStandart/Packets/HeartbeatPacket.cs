using System;
using System.Collections.Generic;
using System.IO;

namespace NetLib_NETStandart.Packets {
    public struct HeartbeatStatus {
        public float rtt;
        public bool packetloss;
    }

    public class HeartbeatPacket : Packet {
        private long timeStamp;
        public long TimeStamp { get => timeStamp; set => timeStamp = value; }

        public HeartbeatPacket(long stamp) {
            header.packetType = PacketType.HeartbeatPacket;
            timeStamp = stamp;
        }

        public override byte[] GetRaw() {
            MemoryStream payloadstream = new MemoryStream();
            PacketBuilder.WriteLong(ref payloadstream, TimeStamp);
            header.payloadLength = (int)payloadstream.Length;

            MemoryStream stream = new MemoryStream();
            PacketBuilder.WriteHeader(ref stream, header);
            payloadstream.WriteTo(stream);

            return stream.ToArray();
        }
    }
}
