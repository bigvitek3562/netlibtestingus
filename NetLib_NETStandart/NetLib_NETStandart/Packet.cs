using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;


// seq is no longer needed for packets (so is the isReliable flag)
namespace NetLib_NETStandart {

    [Serializable]
    public struct PacketHeader {
        public PacketType packetType;
        public uint sender;
        public int payloadLength;
    }

    public abstract class Packet
    {
        public PacketHeader header;
        public abstract byte[] GetRaw();
    }

    public class PartialPacket : Packet {
        private byte[] payload;
        public byte[] Payload { get => payload; private set => payload = value; }
        public PartialPacket(byte[] payload) {
            this.payload = payload;
        }

        public override byte[] GetRaw() {
            MemoryStream payloadstream = new MemoryStream();
            PacketBuilder.WriteBytes(ref payloadstream, payload);
            header.payloadLength = (int)payloadstream.Length;

            MemoryStream stream = new MemoryStream();
            PacketBuilder.WriteHeader(ref stream, header);
            payloadstream.WriteTo(stream);

            return stream.ToArray();
        }
    }
}