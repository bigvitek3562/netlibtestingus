using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NetLib_NETStandart.Packets;

// GET incoming partial packets
namespace NetLib_NETStandart {
    namespace Server {
        public class ServerEventArgs : EventArgs {
            public uint client_id { get; set; }
            public Dictionary<uint, float>? heartbeatInfo { get; set; }
        }

        public class Server
        {
            private Task t_serverRunTask;
            public bool Running { get => _serverRunning; }
            private bool _serverRunning = false;
            public Connection connection;

            public ConcurrentQueue<NetMessage> q_incomingMessages = new ConcurrentQueue<NetMessage>();            

            public event EventHandler<ServerEventArgs>? onNewConnection;
            public event EventHandler<ServerEventArgs>? onClientDisconnect;
            public event EventHandler<ServerEventArgs>? onHeartbeat;

            public Server(int port) 
            { 
                connection = new Connection(port);
                connection.SetConnectionKey(1);
                connection.onNewConnection += NewClientConnected;
                connection.onHeartbeat += OnHeartbeat;
                connection.onClientDisconnected += ClientDisconnected;
                t_serverRunTask = new Task(_serverRunLoop);
            }

            public void Start()
            {
                _serverRunning = true;

                connection.Start();
                t_serverRunTask.Start();

                Console.WriteLine($"[Server] Successfully started! Listening for messages...");
            }

            public void SendString_All(string msg) {
                Console.WriteLine("[Server] start send all...");
                connection.SendToAll(new TestPacket(msg));
            }

            public void SendHeartbeat_All(long time)
            {
                Console.WriteLine("[Server] start send all...");
                connection.SendToAll(new HeartbeatPacket(time));
            }

            public void Tick()
            {

            }

            private void OnHeartbeat(object sender, ConnectionEventArgs args) {
                if (args.heartbeatInfo == null) return;
                onHeartbeat?.Invoke(this, new ServerEventArgs() { heartbeatInfo = args.heartbeatInfo});
            }

            private void NewClientConnected(object sender, ConnectionEventArgs args) {
                onNewConnection?.Invoke(this, new ServerEventArgs { client_id = args.client_id });
            }

            private void ClientDisconnected(object sender, ConnectionEventArgs args) {
                onClientDisconnect?.Invoke(this, new ServerEventArgs { client_id = args.client_id });
            }

            //------------------------separate task
            private async void _serverRunLoop() {
                if (!_serverRunning) return;

                while (_serverRunning) {
                    //read all incoming messages from connection
                    if (connection.Available()) {
                        while (connection.Available()) {
                            NetMessage msg = connection.GetMessage();

                            switch (msg.packet.header.packetType) {
                                case PacketType.TestPacket:
                                    TestPacket tp = (TestPacket)msg.packet;
                                    Console.WriteLine($"[Server] Received TestPacket: \"{tp.Text}\"");
                                    break;
                                default:
                                    q_incomingMessages.Enqueue(msg);
                                    break;

                            }

                        }
                    }
                    else {
                        await Task.Delay(1); //sleepy time
                    }
                }

            }
        }
    }
}