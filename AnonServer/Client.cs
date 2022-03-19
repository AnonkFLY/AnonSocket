using AnonSocket.Data;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AnonSocket.AnonServer
{
    /// <summary>
    /// 客户端
    /// 发送消息
    /// 接收消息
    /// 接收事件<Packet,Buffer,int,Client>
    /// 
    /// </summary>
    public struct ClientData
    {
        public Client client;
        public PacketBase packet;
        public PacketBuffer buffer;
        public int receiveCount;

        public ClientData(Client client, PacketBase packet, PacketBuffer buffer, int receiveCount)
        {
            this.client = client;
            this.packet = packet;
            this.buffer = buffer;
            this.receiveCount = receiveCount;
        }
    }
    public class Client
    {
        public delegate void ReceiveClientMessage(ClientData data);
        private Socket _serverTCPSocket;
        private Socket _serverUDPSocket;
        private EndPoint _tcpEndPoint;
        private EndPoint _udpEndPoint;
        private PacketBuffer _buffer;
        private ServerSocket _server;
        private PacketHandler<ClientData> _packetHandler;
        private byte[] _buff;
        private int _index;
        private int _bufferSize;
        public Socket ServerTCPSocket { get => _serverTCPSocket; }
        public EndPoint TcpEndPoint { get => _tcpEndPoint; }
        public EndPoint UdpEndPoint { get => _udpEndPoint; }
        public PacketHandler<ClientData> PacketHandler { get => _packetHandler; set => _packetHandler = value; }

        public ReceiveClientMessage onReceiveMessage;

        public Client(int index, ServerSocket server,Socket clientSocket, int bufferSize = 512)
        {
            onReceiveMessage = new ReceiveClientMessage(ClientDefaultHandler);
            PacketHandler = new PacketHandler<ClientData>();
            _server = server;
            InitBuffers(bufferSize);
            InitClient(server, index,clientSocket);
        }
        public void InitClient(ServerSocket server, int index,Socket clientSocket)
        {
            _index = index;
            InitSocket(server,clientSocket);
            TCPReceiveData();
        }
        public void InitClientUDP(EndPoint endPoint)
        {
            _udpEndPoint = endPoint;
        }
        //private void UDPReceiveData()
        //{
        //    //AnonSocketUtil.Debug($"尝试从:{UdpEndPoint}获取UDP数据");
        //    _serverUDPSocket.BeginReceiveFrom(_buff, 0, _buff.Length, SocketFlags.None, ref _udpEndPoint, UDPEndReceive, _serverUDPSocket);
        //}

        //private void UDPEndReceive(IAsyncResult ar)
        //{
        //    try
        //    {
        //        Socket socket = (Socket)ar.AsyncState;
        //        var receiveCount = socket.EndReceiveFrom(ar, ref _udpEndPoint);
        //        _buffer.WriteBuffer(_buff, 0, receiveCount);
        //        PacketBase packet = new PacketBase(_buffer.Buffer);

        //        _server.onReceiveUDPMessage?.Invoke(this, _index);
        //        onReceiveMessage?.Invoke(new ClientData(this, packet, _buffer, receiveCount));
        //        if (_serverUDPSocket != null)
        //            UDPReceiveData();
        //    }
        //    catch (Exception e)
        //    {
        //        AnonSocketUtil.Debug($"UDP接收失败{UdpEndPoint},Error:{e}");
        //    }
        //}

        private void TCPReceiveData()
        {
            _serverTCPSocket.BeginReceive(_buff, 0, _buff.Length, SocketFlags.None, TCPEndReceive, _serverTCPSocket);
        }

        private void TCPEndReceive(IAsyncResult ar)
        {
            try
            {
                Socket socket = (Socket)ar.AsyncState;
                var receiveCount = socket.EndReceive(ar);
                _buffer.WriteBuffer(_buff, 0, receiveCount);
                PacketBase packet = new PacketBase(_buffer.Buffer);


                _server.onReceiveTCPMessage?.Invoke(this, _index);
                onReceiveMessage?.Invoke(new ClientData(this, packet, _buffer, receiveCount));
                TCPReceiveData();
            }
            catch (Exception e)
            {
                AnonSocketUtil.Debug($"客户端[{_index}]:{_tcpEndPoint}断开连接,Error:{e}");
                Disconnect();
            }
        }
        public void RecevieUDPData(byte[] buffer, int receiveCount)
        {
            _buffer.WriteBuffer(buffer, 0, receiveCount);
            PacketBase packet = new PacketBase(_buffer.Buffer);
            _server.onReceiveUDPMessage?.Invoke(this, _index);
            onReceiveMessage?.Invoke(new ClientData(this, packet, _buffer, receiveCount));
        }


        public IAsyncResult SendMessage(PacketBase packet)
        {
            bool isTCP = packet.OnTCPConnect(true);
            //Send Message
            try
            {
                if (isTCP)
                {
                    var buffer = packet.ReadBytes();
                    //AnonSocketUtil.Debug("on send data... on tcp");
                    packet.ResetIndex();
                    return _serverTCPSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, TCPEndSend, _serverTCPSocket);
                }
                else
                {
                    return AnonSocketUtil.SubcontractSend(_serverUDPSocket, packet, _udpEndPoint, UDPEndSend, _bufferSize);
                    //AnonSocketUtil.Debug($"on send data... on udp ep is {UdpEndPoint}");
                    //data.serverSocket.BeginSendTo(buffer, 0, buffer.Length, SocketFlags.None, _udpEndPoint, UDPEndSend, data.serverSocket);
                }
            }
            catch (Exception e)
            {
                AnonSocketUtil.Debug($"client {_tcpEndPoint} disconnected... on sendMessage type is [{(isTCP ? ProtocolType.Tcp : ProtocolType.Udp)}] {e}");
                return null;
            }
        }
        private void DisposableClient()
        {
            _serverTCPSocket?.Close();
            _serverTCPSocket = null;
            //_serverUDPSocket?.Close();
            //_serverUDPSocket = null;
            //_udpEndPoint = null;
            _tcpEndPoint = null;
            _index = -1;
        }


        private void InitSocket(ServerSocket server,Socket clientSocket)
        {
            _serverTCPSocket = clientSocket;
            _serverUDPSocket = _server.UtSocket.UdpSocket;
            _tcpEndPoint = _serverTCPSocket.RemoteEndPoint;
        }
        private void InitBuffers(int bufferSize)
        {
            _buffer = new PacketBuffer(bufferSize);
            _buff = new byte[bufferSize];
            _bufferSize = bufferSize;
        }
        private void TCPEndSend(IAsyncResult result)
        {
            Socket client = (Socket)result.AsyncState;
            var count = client.EndSendTo(result);
            //AnonSocketUtil.Debug($"End TCP Send...{count}");
        }
        private void UDPEndSend(IAsyncResult result)
        {
            Socket server = (Socket)result.AsyncState;
            var count = server.EndSendTo(result);
            //AnonSocketUtil.Debug($"End UDP Send...{count}");
        }
        private void Disconnect()
        {
            _server.onClientDisconnect?.Invoke(this, _index);
            DisposableClient();
        }
        private void ClientDefaultHandler(ClientData data)
        {
            var packet = data.packet;
            var buffer = data.buffer;
            var length = packet.Length;
            if (buffer.Index < length)
            {
                //AnonSocketUtil.Debug($"包过大,尝试分包:包长{length},收到包{data.receiveCount},缓冲区{buffer.Index}");
                return;
            }
            buffer.ResetBuffer(length);
            PacketHandler.HandlerPacket(data, packet.PacketID);
        }

    }
}
