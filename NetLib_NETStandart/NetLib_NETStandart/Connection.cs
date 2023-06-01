using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Text;
using NetLib_NETStandart.Packets;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;


//TODO: fix all nullable underlines cause they piss me off
// properly handle disconnections (that would probably work through some sort of disconnectpacket + heartbeats
namespace NetLib_NETStandart {

    public class ConnectionEventArgs : EventArgs {
        public uint client_id { get; set; }
        public Dictionary<uint, float>? heartbeatInfo { get; set; }
    }

    public struct NetMessage {
        public Packet packet { get; set; }
        public DateTime RecieveTime { get; set; }
    }
    public class ClientInfo {
        internal TcpClient? clientTCP;
        internal IPEndPoint? clientUDPEndPoint;
        internal long lastSeq = 0;
        internal long seq = 0;
        
        internal bool isAlive = true;

        float rtt = 0;
        public float RTT {  get => rtt;  internal set => rtt = value; }
    }

    public class Connection
    {
        private ConcurrentQueue<NetMessage> q_incomingMessages = new ConcurrentQueue<NetMessage>();
        public ConcurrentDictionary<uint, ClientInfo> activeClients = new ConcurrentDictionary<uint, ClientInfo>();
        public ConcurrentDictionary<IPEndPoint, uint> UDPAliases = new ConcurrentDictionary<IPEndPoint, uint>();
        private uint max_connections = 100;
        private Queue<uint> availableIds;

        private uint connection_key = 0;
        public void SetConnectionKey(uint key) { this.connection_key = key; }

        private CancellationTokenSource t_cts = new CancellationTokenSource();
        private CancellationToken t_ct;
        private Task t_networkTcpListener;
        private Task t_networkUdpListener;
        private Task t_heartbeatSender;

        private TcpListener tcpListener;
        private UdpClient udpListener;
        //private UdpClient udpClient;
        
        private bool _connectionRunning = false;
        private int port;

        public event EventHandler<ConnectionEventArgs>? onNewConnection;
        public event EventHandler<ConnectionEventArgs>? onClientDisconnected;
        public event EventHandler<ConnectionEventArgs>? onHeartbeat;
        public event EventHandler<ConnectionEventArgs>? onConnect;

        public Connection(int port = 0) {
            this.port = port;

            Console.WriteLine($"[Connection] Setting up ports {port}...");

            if (port == 0) {
                tcpListener = new TcpListener(IPAddress.Any, port);
                udpListener = new UdpClient(port);
            }
            else {
                tcpListener = new TcpListener(IPAddress.Any, port);
                udpListener = new UdpClient(port);
            }

            t_ct = t_cts.Token;
            t_networkTcpListener = new Task(_networkTcpReceive, t_ct);
            t_networkUdpListener = new Task(_networkUdpReceive, t_ct);
            t_heartbeatSender = new Task(_heartbeatSender, t_ct);

            availableIds = new Queue<uint>();
            for (uint i = 2; i <= max_connections; i++) availableIds.Enqueue(i); // 1 is the server i guess
        }

        ~Connection() {
            udpListener.Close();
        }
        
        public void Start() {
            _connectionRunning = true;

            tcpListener.Start();
            t_networkTcpListener.Start();
            t_networkUdpListener.Start();
            if(port!=0)
                t_heartbeatSender.Start();

            

            Console.WriteLine($"[Connection] Started connection: \n" +
                $"Local address:\n" +
                $"  [TCP] - {tcpListener.LocalEndpoint}\n" +
                $"  [UDP] - {udpListener.Client.LocalEndPoint}(listener)\n");

        }
        
        public async void Close() {
            SendUDP(1, new DisconnectPacket("bye bye"));

            _connectionRunning = false;

            await t_networkTcpListener;
            await t_networkUdpListener;
            await t_heartbeatSender;
            t_networkTcpListener.Dispose();
            t_networkUdpListener.Dispose();
            t_heartbeatSender.Dispose();

            tcpListener.Stop();
            //udpClient.Close();

            activeClients.Clear();
        }

        private uint GetAvailableClientId() {
            if (availableIds.Count > 0)
                return availableIds.Dequeue();
            Console.WriteLine("Ran out of client id's");
            return 0;
        }

        public bool Available() {
            return !q_incomingMessages.IsEmpty;
        }

        public NetMessage GetMessage() {
            q_incomingMessages.TryDequeue(out NetMessage result);
            return result;
        }

        public void ConnectToServer(IPEndPoint receiver) {
            TcpClient client;
            try {
                client = new TcpClient(receiver.Address.ToString(), receiver.Port);
            }
            catch (Exception ex) {
                Console.WriteLine(ex.ToString());
                Close();
                return;
            }
            //IPEndPoint serverUDP = new IPEndPoint(receiver.Address, receiver.Port + 2);
            activeClients.TryAdd(1, new ClientInfo() { clientTCP = client, clientUDPEndPoint = receiver, isAlive = true});
            UDPAliases.TryAdd(receiver, 1);
            SendTCP(1, new ConnectPacket(((IPEndPoint)udpListener.Client.LocalEndPoint!).Port));
        }

        public void SendTCP(uint receiver, Packet packet) { // i have no idea with this networkstream ting if i should read first to clear the stream... probably a TODO thing
            packet.header.sender = connection_key;
            byte[] data = Utils.Compress(packet.GetRaw());
            Console.WriteLine($"[TCP] Sending message to {receiver}; of size {data.Length}");

            if (!activeClients.TryGetValue(receiver, out ClientInfo? client)) return; //get client
            if (client == null) return;
            Console.WriteLine($"[TCP] Receiver info: {client.clientTCP.Client.RemoteEndPoint}");

            NetworkStream stream = client.clientTCP.GetStream();
            stream.Write(data, 0, data.Length);
        }

        public void SendUDP(uint receiver, Packet packet) {
            packet.header.sender = connection_key;
            byte[] data = Utils.Compress(packet.GetRaw());
            //Console.WriteLine($"[UDP] Sending message to {receiver}; of size {data.Length}");
            activeClients.TryGetValue(receiver, out ClientInfo client);
            bool exists = false;
            foreach (var item in UDPAliases.Where(kvp => kvp.Value == receiver).ToList()) {
                exists = true;
            }
            if (!exists) {
                Console.WriteLine("Tried to send to non registered client");
                return;
            }
            udpListener.Send(data, data.Length, client.clientUDPEndPoint);
        }

        public void SendToAll(Packet packet, bool useTcp = false) {
            Console.WriteLine("[Connection] Starting parallel foreach...");
            Parallel.ForEach(activeClients, (KeyValuePair<uint, ClientInfo> client) => {
                if (useTcp) SendTCP(client.Key, packet);
                else SendUDP(client.Key, packet);
            });
        }

        public void registerActiveClient(IPEndPoint endPoint, TcpClient client) { ////client side ONLY
            if (endPoint == null) return;
            uint newId = GetAvailableClientId();
            activeClients.TryAdd(newId, new ClientInfo() { clientTCP = client, clientUDPEndPoint = endPoint});
            UDPAliases.TryAdd(endPoint, newId);
        }

        public void disconnectClient(uint clientId, bool confirm = false) {            
            if(confirm) SendUDP(clientId, new DisconnectPacket("You have been disconnected!"));
            activeClients.TryGetValue(clientId, out ClientInfo client);
            client.isAlive = false;

            if(client != null && client.clientTCP != null) client.clientTCP.Close();
            activeClients.TryRemove(clientId, out _);

            foreach (var item in UDPAliases.Where(kvp => kvp.Value == clientId).ToList()) {
                UDPAliases.TryRemove(item.Key, out uint _);
            }
            
            
            availableIds.Enqueue(clientId);
            
            onClientDisconnected?.Invoke(this, new ConnectionEventArgs { client_id = clientId } );
            Console.WriteLine($"[Connection] Client {clientId} disconnected!");
        }


        private bool handle_internal_packets(uint sender, Packet packet) {
            switch (packet.header.packetType) {
                case PacketType.ConnectPacket:
                    Console.WriteLine($"[Connection] Got connectpacket from {sender}");
                    if (!activeClients.TryGetValue(sender, out ClientInfo? client)) {
                        Console.WriteLine("user was not present in activeClients");
                        return true; // what
                    }
                    if (client == null) {
                        Console.WriteLine("client was null (???)");
                        return true;
                    }// also what

                    ConnectPacket cp = (ConnectPacket)packet;
                    IPEndPoint newClientEndPoint = new IPEndPoint((client.clientTCP.Client.RemoteEndPoint as IPEndPoint).Address, cp.UdpPort);
                    client.clientUDPEndPoint = newClientEndPoint;
                    Console.WriteLine($"[Connection] Adding client to UDPAliases result: {UDPAliases.TryAdd(newClientEndPoint, sender)}");

                    ConnectAckPacket cap = new ConnectAckPacket(sender);
                    Console.WriteLine($"[Connection] Sending conACK packet to {sender}!");
                    SendTCP(sender, cap);

                    onNewConnection?.Invoke(this, new ConnectionEventArgs { client_id = sender });

                    return true;

                case PacketType.ConnectAckPacket:
                    ConnectAckPacket cap_recv = (ConnectAckPacket)packet;
                    this.connection_key = cap_recv.Key;
                    Console.WriteLine($"[Connection] НОВЫЙ ГОД получили в подарок ключ!!: {cap_recv.Key}");
                    onConnect?.Invoke(this, new ConnectionEventArgs() { client_id = cap_recv.Key });
                    return true;

                case PacketType.HeartbeatPacket:
                    HeartbeatPacket hp = (HeartbeatPacket)packet;
                    HeartbeatAckPacket hap = new HeartbeatAckPacket(hp.TimeStamp);
                    SendUDP(sender, hap);
                    return true;

                case PacketType.HeartbeatAckPacket:
                    HeartbeatAckPacket hap_recv = (HeartbeatAckPacket)packet;
                    float newrtt = TimeSpan.FromTicks((Stopwatch.GetTimestamp() - hap_recv.TimeStamp)).Milliseconds;
                    activeClients.TryGetValue(hap_recv.header.sender, out client);
                    client.RTT = newrtt;
                    client.lastSeq++;
                    //Console.WriteLine($"[Connection] {sender} heartbeat: {newrtt}ms");
                    return true;

                case PacketType.DisconnectPacket:
                    disconnectClient(sender);
                    return true;

                default: return false;
            }
        }

        //-----------------------------------------separate tasks
        private async void _networkTcpReceive() {
            if (!_connectionRunning) return;

            while (_connectionRunning && !t_ct.IsCancellationRequested) {

                if (tcpListener.Pending()) { //check for incoming connections
                    Console.WriteLine($"this shouldnt happen on client");
                    TcpClient client = tcpListener.AcceptTcpClient(); //blocking 
                    Console.WriteLine($"New client connected: {client.Client.RemoteEndPoint}"); //TODO: some sort of client checking so that we dont accept random connections

                    uint newId = 0;
                    if (client.Client.RemoteEndPoint != null) {
                        newId = GetAvailableClientId();
                        activeClients.TryAdd(newId, new ClientInfo { clientTCP = client }); //accept connection and add to active connections
                    }

                    //might want to do smth here like send a greeting message or whatever
                    //or probably should read the connectPacket uuhhhh idk
                    //SendTCP(newId, new TestPacket("oh, hi!!"));
                }

                Parallel.ForEach(activeClients, (KeyValuePair<uint, ClientInfo> client) => {  //read all incoming messages (this is probably wrong i have no idea)
                    if (!client.Value.isAlive) return;

                    NetworkStream stream = client.Value.clientTCP.GetStream();
                    if (stream.DataAvailable) {
                        while (stream.DataAvailable) {
                            Console.WriteLine($"[TCP] Received message from {client.Key}, reading...");
                            Packet? packet = PacketReader.ReadFromStream(stream);
                            Console.WriteLine($"{packet!.header.packetType}");
                            if(packet != null) 
                                if(!handle_internal_packets(client.Key, packet))
                                    q_incomingMessages.Enqueue(new NetMessage { packet = packet, RecieveTime = DateTime.Now });
                        }
                    }
                });

                if (t_ct.IsCancellationRequested) t_ct.ThrowIfCancellationRequested();
            }

        }


        private async void _networkUdpReceive() { 
            if (!_connectionRunning) return;
        
            IPEndPoint connection = new IPEndPoint(IPAddress.Any, 0);
        
            while (_connectionRunning && !t_ct.IsCancellationRequested) {
                if(udpListener.Available > 0) { //UDP Listen
                    try {
                        byte[] data = udpListener.Receive(ref connection);
                        if (!UDPAliases.TryGetValue(connection, out uint _)) continue;


                        //Console.WriteLine(connection);
                        Packet? packet = PacketReader.ReadFromRaw(data);
                        uint sender = packet.header.sender;
                        if (activeClients.TryGetValue(sender, out _)) {
                            //Console.WriteLine($"[UDP] Received message from {sender}, reading...");
                            if (packet != null)
                                if (!handle_internal_packets(sender, packet))
                                    q_incomingMessages.Enqueue(new NetMessage { packet = packet, RecieveTime = DateTime.Now });
                        }
                        else {
                            Console.WriteLine($"[UDP] Received message from a non-registered user...");
                        }
                    } catch (SocketException e) {
                        Console.WriteLine($"Caught a socket expression while receiving from {connection}! disconnecting the client...");
                    }
                }
                else {
                   await Task.Delay(1); //nothing to do => sleepy time 
                }
                if(t_ct.IsCancellationRequested) t_ct.ThrowIfCancellationRequested();
            }    
        }

        private async void _heartbeatSender() {
            if (!_connectionRunning) return;

            while (_connectionRunning && !t_ct.IsCancellationRequested) {
                Parallel.ForEach(activeClients, (KeyValuePair<uint, ClientInfo> client) => {
                    if (!client.Value.isAlive) return;
                    if (client.Value.seq - client.Value.lastSeq > 5) {  //packet loss of five or more --> disconnect due to bad network connection
                        disconnectClient(client.Key);
                    }
                    else {
                        SendUDP(client.Key, new HeartbeatPacket(Stopwatch.GetTimestamp()));
                        client.Value.seq++;
                    }
                });

                await Task.Delay(250);

                Dictionary<uint, float> heartbeats = new Dictionary<uint, float>();
                foreach (KeyValuePair<uint, ClientInfo> client in activeClients) {
                    heartbeats.Add(client.Key, client.Value.RTT);
                }
                onHeartbeat?.Invoke(this, new ConnectionEventArgs() {
                    heartbeatInfo = heartbeats
                });
                if (t_ct.IsCancellationRequested) t_ct.ThrowIfCancellationRequested();
            }

        }
    }
}
