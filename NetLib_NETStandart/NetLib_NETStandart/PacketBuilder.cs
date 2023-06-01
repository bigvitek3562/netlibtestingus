using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace NetLib_NETStandart {
    public static class PacketBuilder {
        public static void WriteInt(ref MemoryStream stream, int data) {
            byte[] bytes = BitConverter.GetBytes(data);
            stream.Write(bytes, 0, bytes.Length);
        }

        public static void WriteLong(ref MemoryStream stream, long data) {
            byte[] bytes = BitConverter.GetBytes(data);
            stream.Write(bytes, 0, bytes.Length);
        }

        public static void WriteFloat(ref MemoryStream stream, float data) {
            byte[] bytes = BitConverter.GetBytes(data);
            stream.Write(bytes, 0, bytes.Length);
        }

        public static void WriteBool(ref MemoryStream stream, bool data) {
            byte[] bytes = BitConverter.GetBytes(data);
            stream.Write(bytes, 0, bytes.Length);
        }

        public static void WriteUint(ref MemoryStream stream, uint data) {
            byte[] bytes = BitConverter.GetBytes(data);
            stream.Write(bytes, 0, bytes.Length);
        }

        public static void WriteString(ref MemoryStream stream, string data) {
            byte[] bytes = Encoding.Unicode.GetBytes(data);
            byte[] size = BitConverter.GetBytes(bytes.Length);
            stream.Write(size, 0, size.Length);
            stream.Write(bytes, 0, bytes.Length);
        }

        public static void WriteBytes(ref MemoryStream stream, byte[] data) {
            stream.Write(data, 0, data.Length);
        }

        public static void WriteHeader(ref MemoryStream stream, PacketHeader data) {
            byte[] bytes = BitConverter.GetBytes((int)data.packetType);
            stream.Write(bytes, 0, bytes.Length);
            bytes = BitConverter.GetBytes(data.sender);
            stream.Write(bytes, 0, bytes.Length);
            bytes = BitConverter.GetBytes(data.payloadLength);
            stream.Write(bytes, 0, bytes.Length);
        }

    }
}
