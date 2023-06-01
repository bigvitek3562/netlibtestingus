// seq is no longer needed for packets (so is the isReliable flag)

using System;
using System.Collections.Generic;
using System.IO;

namespace NetLib_NETStandart.Packets {
    public class ConnectPacket : Packet {
        private int udpPort;
        public int UdpPort { get => udpPort; set => udpPort = value; }

        public ConnectPacket(int port) {
            header.packetType = PacketType.ConnectPacket;
            udpPort = port;
        }

        public override byte[] GetRaw() {
            MemoryStream payloadstream = new MemoryStream();
            PacketBuilder.WriteInt(ref payloadstream, udpPort);
            header.payloadLength = (int)payloadstream.Length;

            MemoryStream stream = new MemoryStream();
            PacketBuilder.WriteHeader(ref stream, header);
            payloadstream.WriteTo(stream);


            return stream.ToArray();
        }
    }
}