using NetLib_NETStandart.Server;
using NetLib_NETStandart;
using TestCommons;

class TestServer {
    static void printHeartbeats(object? sender, ServerEventArgs args) {
        if (args.heartbeatInfo == null) return;
        foreach(KeyValuePair<uint, float> hb in args.heartbeatInfo) {
            Console.WriteLine($"Client {hb.Key} connection status: {hb.Value}ms");
        }
    }
    static void Main(string[] args) {
        Server server = new(12000);
        server.Start();

        server.onHeartbeat += printHeartbeats; 

        string? input = "";
        while (server.Running) {
            while (server.q_incomingMessages.Count > 0) { // read incoming messages (probably a separate task)
                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();
                server.q_incomingMessages.TryDequeue(out NetMessage msg);
                PartialPacket packet = (PartialPacket)msg.packet;
                byte[] payload = packet.Payload;
                PacketReader.ReadInt(ref payload, 0, out int packetType);
                Console.WriteLine("[Server] Received custom packet of type: " + packetType);
                switch (packetType) {
                    case (int)customPacketType.PlayerInput:
                        PlayerInput inputs = PlayerInputPacket.read(payload);
                        Console.WriteLine(inputs.W + " " + inputs.A + " " + inputs.S + " " + inputs.D + " ");
                        server.connection.SendUDP(packet.header.sender, new PlayerInputPacket(inputs));
                        break;
                    case (int)customPacketType.PlayerPosition:
                        PlayerPosition pos = PlayerPositionPacket.read(payload);
                        Console.WriteLine(pos.client_id + " " + pos.X + " " + pos.Y + " " + pos.rotation);
                        server.connection.SendUDP(packet.header.sender, new PlayerPositionPacket(pos));
                        break;
                    default:
                        Console.WriteLine("POOP POOP");
                        break;
                }
                watch.Stop();
                Console.WriteLine($"Execution Time: {watch.ElapsedTicks}");
            }
            //input = Console.ReadLine();
            //if (input != null && input != "h") {
            //    server.SendString_All(input);
            //}
            //if (input == "h") {
            //    server.SendHeartbeat_All(DateTime.Now);
            //}
        }
    }
}