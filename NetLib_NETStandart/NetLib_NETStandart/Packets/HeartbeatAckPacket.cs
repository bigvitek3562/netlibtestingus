using System;
using System.Collections.Generic;
using System.IO;

namespace NetLib_NETStandart.Packets
{
    public class HeartbeatAckPacket : Packet
    {
        private long timeStamp;
        public long TimeStamp { get => timeStamp; set => timeStamp = value; }

        public HeartbeatAckPacket(long stamp)
        {
            header.packetType = PacketType.HeartbeatAckPacket;
            timeStamp = stamp;
        }

        public override byte[] GetRaw()
        {
            MemoryStream payloadstream = new MemoryStream();
            PacketBuilder.WriteLong(ref payloadstream, timeStamp);
            header.payloadLength = (int)payloadstream.Length;

            MemoryStream stream = new MemoryStream();
            PacketBuilder.WriteHeader(ref stream, header);
            payloadstream.WriteTo(stream);


            return stream.ToArray();
        }
    }
}
