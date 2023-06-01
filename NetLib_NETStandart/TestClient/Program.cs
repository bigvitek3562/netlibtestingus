using NetLib_NETStandart;
using System.Net;
using TestCommons;

class TestClient {
    static void Main(string[] args) {
        Client client = new Client(
            new IPEndPoint(IPAddress.Loopback, 12000)
            );
        client.Start();

        string? input = "";
        while (input != "!exit") {
            input = Console.ReadLine();
            if (input != null) {
                client.SendString(input);
                client.connection.SendTCP(1, new PlayerPositionPacket(new PlayerPosition {
                    client_id = 1,
                    X = 1234.6f,
                    Y = 10.995f,
                    rotation = 3.141591f,
                }));
                client.connection.SendUDP(1, new PlayerInputPacket(new PlayerInput {
                    W = true,
                    A = true,
                    S = false,
                    D = false
                }));
            }
        }
        client.connection.Close();

    }
}