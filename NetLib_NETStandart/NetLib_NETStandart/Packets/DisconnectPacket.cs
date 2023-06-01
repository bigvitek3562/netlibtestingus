using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetLib_NETStandart.Packets {
    public class DisconnectPacket : Packet 
    {
        private string msg;

        public string Msg { get => msg; set => msg = value; }

        public DisconnectPacket(string msg) 
        {
            header.packetType = PacketType.DisconnectPacket;
            this.msg = msg;
        }

        public override byte[] GetRaw() 
        {
            MemoryStream payloadstream = new MemoryStream();
            PacketBuilder.WriteString(ref payloadstream, msg);
            header.payloadLength = (int)payloadstream.Length;

            MemoryStream stream = new MemoryStream();
            PacketBuilder.WriteHeader(ref stream, header);
            payloadstream.WriteTo(stream);


            return stream.ToArray();
        }
    }
}
