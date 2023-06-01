using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetLib_NETStandart.Packets {
    public class ConnectAckPacket : Packet {
        private uint key;
        public uint Key { get => key; set => key = value; }
        public ConnectAckPacket(uint key) {
            header.packetType = PacketType.ConnectAckPacket;
            this.key = key;
        }

        public override byte[] GetRaw() {
            MemoryStream payloadstream = new MemoryStream();
            PacketBuilder.WriteUint(ref payloadstream, key);
            header.payloadLength = (int)payloadstream.Length;

            MemoryStream stream = new MemoryStream();
            PacketBuilder.WriteHeader(ref stream, header);
            payloadstream.WriteTo(stream);


            return stream.ToArray();
        }
    }
}
