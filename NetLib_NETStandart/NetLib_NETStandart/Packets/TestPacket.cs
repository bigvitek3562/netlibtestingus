using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetLib_NETStandart.Packets {
    public class TestPacket : Packet {
        private string text;
        public string Text { get => text; set => text = value; }

        public TestPacket(string text) {
            header.packetType = PacketType.TestPacket;
            this.text = text;
        }

        public override byte[] GetRaw() {
            MemoryStream payloadstream = new MemoryStream();
            PacketBuilder.WriteString(ref payloadstream, text);
            header.payloadLength = (int)payloadstream.Length;

            MemoryStream stream = new MemoryStream();
            PacketBuilder.WriteHeader(ref stream, header);
            payloadstream.WriteTo(stream);

            return stream.ToArray();
        }
    }
}