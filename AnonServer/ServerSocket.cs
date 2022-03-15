using AnonSocket.Data;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace AnonSocket.AnonServer
{
    public class ServerSocket : IDisposable
    {
        private UTSocket _utSocket;
        private PacketBuffer _buffer;
        private List<Client> _clients;
        private List<Client> _clientBufferPool;
        private byte[] _buffArray;
        private int _bufferSize;

        /// <summary>
        /// 接收到一个客户端的连接
        /// </summary>
        public Action<Client, int> onClientConnet;              
        /// <summary>
        /// 客户端断开连接
        /// </summary>
        public Action<Client, int> onClientDisconnect;
        /// <summary>
        /// 接收到TCP数据
        /// </summary>
        public Action<Client, int> onReceiveTCPMessage; 
        /// <summary>
        /// 接收到UDP数据
        /// </summary>
        public Action<Client, int> onReceiveUDPMessage;
        public List<Client> Clients { get => _clients; }
        public List<Client> ClientBufferPool { get => _clientBufferPool; }

        public ServerSocket(int maxCnt, int tcpPort, int udpPort,int bufferSize = 512)
        {
            _utSocket = new UTSocket(UTSocketType.Server, tcpPort, udpPort);
            _utSocket.ListenerClients(maxCnt);

            _buffArray = new byte[bufferSize];
            _bufferSize = bufferSize;
            _buffer = new PacketBuffer(512);
            _clients = new List<Client>();
            _clientBufferPool = new List<Client>();

            InitEvent();

            //Debug Client缓存池
            onClientDisconnect += (client, index) =>
            {
                AnonSocketUtil.Debug($"客户端[{index}]:{client.TcpEndPoint}断开连接");
                AnonSocketUtil.Debug("缓存池数量:"+_clientBufferPool.Count);
                AnonSocketUtil.Debug("在线客户端:");
                for(int i = 0;i<_clients.Count;i++)
                {
                    AnonSocketUtil.Debug($"---客户端[{i}]:{_clients[i].TcpEndPoint}在线");
                }
                Console.WriteLine();
            };
        }



        public void Open()
        {
            AnonSocketUtil.Debug("Start listening to " + _utSocket.TcpSocket.LocalEndPoint);
            _utSocket.TcpSocket.BeginAccept(OnAccept, _utSocket.TcpSocket);
        }
        public void Close()
        {
            Dispose();
        }
        public void Dispose()
        {
            _utSocket.Dispose();
        }

        public void BroadcastPacket(PacketBase packet)
        {
            Clients.ForEach(client =>
            {
                MessageData messageData = new MessageData();
                messageData.serverSocket = packet.PacketID < 0 ? _utSocket.TcpSocket : _utSocket.UdpSocket;
                messageData.packetData = packet;
                client.SendMessage(messageData);
            });
        }
        private void OnAccept(IAsyncResult result)
        {
            var serverSocket = (Socket)result.AsyncState;
            var clientSocket = serverSocket.EndAccept(result);
            EndPoint iPEndPoint = clientSocket.RemoteEndPoint;
            serverSocket.BeginAccept(OnAccept, serverSocket);
            AnonSocketUtil.Debug(iPEndPoint.ToString() + " connection succeeded!");
            var client = GetNewClient(clientSocket, this);
            var index = _clients.Count;
            Clients.Add(client);

            onClientConnet?.Invoke(client, index);
        }
        private Client GetNewClient(Socket socket, ServerSocket server)
        {
            if (ClientBufferPool.Count == 0)
                return new Client(socket, _clients.Count, server,_bufferSize);
            int index = ClientBufferPool.Count - 1;
            var result = ClientBufferPool[index];
            result.InitClient(socket, _clients.Count);
            ClientBufferPool.RemoveAt(index);
            return result;
        }

        private void OnAcceptUDPConnect(Client client, int index)
        {
            EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
            _utSocket.UdpSocket.BeginReceiveFrom(_buffArray, 0, _buffArray.Length, SocketFlags.None, ref endPoint, EndUDPAccept, new ReceiveState(_buffArray, endPoint, _utSocket.UdpSocket));
        }
        private void EndUDPAccept(IAsyncResult result)
        {
            var receiveState = (ReceiveState)result.AsyncState;
            receiveState.socket.EndReceiveFrom(result, ref receiveState.endPoint);
            var packet = new PacketBase(receiveState.buffer);
            if (packet.PacketID == 0)
            {
                var index = packet.ReadInt32();
                InitClientUDP(index, receiveState.endPoint);
                AnonSocketUtil.Debug($"{receiveState.endPoint} UDP连接成功 接收数据:{index}");
            }
        }


        private void InitClientUDP(int index, EndPoint end)
        {
            _clients[index].InitClientUDP(end, _utSocket.UdpSocket);
        }


        private void InitEvent()
        {
            onClientDisconnect = new Action<Client, int>(RemoveClient);
            onClientConnet = new Action<Client, int>(SendIndexPacket);
            onClientConnet += OnAcceptUDPConnect;
        }
        private void RemoveClient(Client client, int index)
        {
            Clients.RemoveAt(index);
            ClientBufferPool.Add(client);
        }
        private void SendIndexPacket(Client client, int index)
        {
            var packet = new PacketBase(0);
            packet.Write(index);
            client.SendMessage(new MessageData(packet, _utSocket.TcpSocket));
        }

    }
    public struct ReceiveState
    {
        public EndPoint endPoint;
        public byte[] buffer;
        public Socket socket;

        public ReceiveState(byte[] buffer, EndPoint endPoint, Socket socket)
        {
            this.buffer = buffer;
            this.endPoint = endPoint;
            this.socket = socket;
        }
    }
}