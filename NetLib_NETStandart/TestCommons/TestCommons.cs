using NetLib_NETStandart;

namespace TestCommons {

    public enum customPacketType {
        PlayerInput,
        PlayerPosition,
    }

    public struct PlayerInput {
        public bool W;
        public bool A;
        public bool S;
        public bool D;
    }

    public struct PlayerPosition {
        public uint client_id;
        public float X;
        public float Y;
        public float rotation;
    }

    public class PlayerInputPacket : Packet {
        customPacketType c_packetType = customPacketType.PlayerInput;
        PlayerInput inputs;
        public PlayerInputPacket(PlayerInput inputs) {
            header.packetType = NetLib_NETStandart.PacketType.CustomPacket;
            this.inputs = inputs;
        }

        public static PlayerInput read(byte[] payload) {
            int index = sizeof(int);
            index = PacketReader.ReadBool(ref payload, index, out bool W);
            index = PacketReader.ReadBool(ref payload, index, out bool A);
            index = PacketReader.ReadBool(ref payload, index, out bool S);
                    PacketReader.ReadBool(ref payload, index, out bool D);

            return new PlayerInput() { W = W, A = A, S = S, D = D };
        }

        public override byte[] GetRaw() {
            MemoryStream payloadstream = new MemoryStream();
            PacketBuilder.WriteInt(ref payloadstream, (int)c_packetType);
            PacketBuilder.WriteBool(ref payloadstream, inputs.W);
            PacketBuilder.WriteBool(ref payloadstream, inputs.A);
            PacketBuilder.WriteBool(ref payloadstream, inputs.S);
            PacketBuilder.WriteBool(ref payloadstream, inputs.D);
            header.payloadLength = (int)payloadstream.Length;

            MemoryStream stream = new MemoryStream();
            PacketBuilder.WriteHeader(ref stream, header);
            payloadstream.WriteTo(stream);


            return stream.ToArray();
        }
    }

    public class PlayerPositionPacket : Packet {
        customPacketType c_packetType = customPacketType.PlayerPosition;
        PlayerPosition input;
        public PlayerPositionPacket(PlayerPosition input) {
            header.packetType = NetLib_NETStandart.PacketType.CustomPacket;
            this.input = input;
        }

        public static PlayerPosition read(byte[] payload) {
            int index = sizeof(int);
            index = PacketReader.ReadUint(ref payload, index, out uint client_id);
            index = PacketReader.ReadFloat(ref payload, index, out float X);
            index = PacketReader.ReadFloat(ref payload, index, out float Y);
            PacketReader.ReadFloat(ref payload, index, out float rotation);

            return new PlayerPosition() { X = X, Y = Y, rotation = rotation };
        }

        public override byte[] GetRaw() {
            MemoryStream payloadstream = new MemoryStream();
            PacketBuilder.WriteInt(ref payloadstream, (int)c_packetType);
            PacketBuilder.WriteUint(ref payloadstream, input.client_id);
            PacketBuilder.WriteFloat(ref payloadstream, input.X);
            PacketBuilder.WriteFloat(ref payloadstream, input.Y);
            PacketBuilder.WriteFloat(ref payloadstream, input.rotation);
            header.payloadLength = (int)payloadstream.Length;

            MemoryStream stream = new MemoryStream();
            PacketBuilder.WriteHeader(ref stream, header);
            payloadstream.WriteTo(stream);

            return stream.ToArray();
        }
    }

}



