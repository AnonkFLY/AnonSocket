using AnonSocket.Data;
using System;
using System.Net;
using System.Net.Sockets;

namespace AnonSocket.AnonClient
{
    public class ClientSocket : IDisposable
    {
        private UTSocket _utSocket;
        private PacketBuffer _buffer;
        private EndPoint _serverUDPEP;
        private byte[] _buff;
        private int _index;
        private PacketHandler<PacketBase> _packetHandler;

        public event Action<IPAddress> onBeginConnect;                        //尝试连接服务器
        public event Action<ClientSocket> onEndTCPConnect;                     //连接成功
        public event Action<ClientSocket> onEndUDPConnect;
        public event Action<ClientSocket, Exception> onConnectionFailed;       //连接失败或断开连接
        /// <summary>
        /// UDP数据发送失败
        /// </summary>
        public event Action<ClientSocket, Exception> onUDPSendFailed;
        /// <summary>
        /// 接收UDP数据失败
        /// </summary>
        public event Action<ClientSocket, Exception> onUDPReceiveFailed;
        /// <summary>
        /// TCP数据发送失败
        /// </summary>
        public event Action<ClientSocket, Exception> onTCPSendFailed;
        /// <summary>
        /// 接收UDP数据失败
        /// </summary>
        public event Action<ClientSocket, Exception> onTCPReceiveFailed;
        /// <summary>
        /// 接收到TCP数据包
        /// </summary>
        public event Action<PacketBase, PacketBuffer, int> onTCPAcceptPacket;
        /// <summary>
        /// 接收到UDP数据包
        /// </summary>
        public event Action<PacketBase, PacketBuffer, int> onUDPAcceptPacket;
        /// <summary>
        /// 接收到数据包的事件，将在对应协议之后执行
        /// </summary>
        public event Action<PacketBase, PacketBuffer, int> onReceivePacket;   //接收到数据包
        /// <summary>
        /// 数据包处理器
        /// </summary>
        public PacketHandler<PacketBase> PacketHandler { get => _packetHandler; set => _packetHandler = value; }
        public int Index { get => _index; set => _index = value; }

        /// <summary>
        /// Init
        /// </summary>
        /// <param name="tpcPort">服务端的TCP端口</param>
        /// <param name="udpPort">服务端的UDP端口</param>
        /// <param name="bufferSize">单个数据包大小，缓冲区大小</param>
        public ClientSocket(int tpcPort, int udpPort, int bufferSize = 512)
        {
            _buffer = new PacketBuffer(bufferSize);
            _buff = new byte[bufferSize];
            _utSocket = new UTSocket(UTSocketType.Client, tpcPort, udpPort);
            _packetHandler = new PacketHandler<PacketBase>();
            onReceivePacket = new Action<PacketBase, PacketBuffer, int>(ClientDefaultHandler);

            _packetHandler.RegisterHandler(0, UDPConnect);
        }
        /// <summary>
        /// 尝试连接服务器
        /// </summary>
        /// <param name="serverIP">服务器IP</param>
        public IAsyncResult Connect(IPAddress serverIP)
        {
            onBeginConnect?.Invoke(serverIP);
            return _utSocket.ConnectToServer(serverIP, OnConnect, _utSocket.TcpSocket);
        }
        /// <summary>
        /// 发送数据包给服务器
        /// </summary>
        /// <param name="packet"></param>
        public void SendMessage(PacketBase packet)
        {
            AnonSocketUtil.Debug($"发送包{packet.PacketID},包长{packet.Length}");
            if (packet.OnTCPConnect(false))
            {
                SendTCPPacket(packet);
            }
            else
            {
                SendUDPPacket(packet);
            }
        }
        private void SendTCPPacket(PacketBase packet)
        {
            try
            {
                var buffer = packet.ReadBytes();
                //AnonSocketUtil.Debug("on send data... use tcp");
                _utSocket.TcpSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, TCPEndSend, _utSocket.TcpSocket);
            }
            //Debug code
            catch (Exception e)
            {
                onTCPSendFailed?.Invoke(this, e);
            }
        }
        private void SendUDPPacket(PacketBase packet)
        {
            try
            {
                Socket udpSocket = _utSocket.UdpSocket;
                //AnonSocketUtil.Debug($"on send data... use udp on {_utSocket.ServerUDPEP}");
                //udpSocket.BeginSendTo(buffer, 0, buffer.Length, SocketFlags.None, _utSocket.ServerUDPEP, UDPEndSend, udpSocket);
                AnonSocketUtil.SubcontractSend(udpSocket, packet, _utSocket.ServerUDPEP, UDPEndSend, _buff.Length);
            }
            catch (Exception e)
            {
                onUDPSendFailed?.Invoke(this, e);
            }
        }

        private void TCPEndSend(IAsyncResult result)
        {
            Socket client = (Socket)result.AsyncState;
            client.EndSend(result);
            //AnonSocketUtil.Debug("End TCP Send...");
        }
        private void UDPEndSend(IAsyncResult result)
        {
            Socket client = (Socket)result.AsyncState;
            client.EndSendTo(result);
            //AnonSocketUtil.Debug("End UDP Send...");
        }
        private void OnConnect(IAsyncResult result)
        {
            try
            {
                var client = (Socket)result.AsyncState;
                //TCP Connect
                client.EndConnect(result);
                AnonSocketUtil.Debug("On connected!");
                onEndTCPConnect?.Invoke(this);

                //UDP Connect
                //UDPConnect();

                TCPReceiveData();
                StartUDPReceiveData();
            }
            catch (SystemException e)
            {
                //AnonSocketUtil.Debug($"Connection failed, error {e}");
                onConnectionFailed?.Invoke(this, e);
            }
        }

        private void UDPConnect(PacketBase packet)
        {
            //Send index packet
            AnonSocketUtil.Debug($"on ConnectUDP");
            onEndUDPConnect?.Invoke(this);

            int index = packet.ReadInt32();
            SendMessage(packet);
        }

        private void TCPReceiveData()
        {
            //AnonSocketUtil.Debug("Try getting the packet and use TCP");
            try
            {
                _utSocket.TcpSocket.BeginReceive(_buff, 0, _buff.Length, SocketFlags.None, TCPEndReceive, _utSocket.TcpSocket.RemoteEndPoint);
            }
            catch (Exception e)
            {
                onTCPReceiveFailed?.Invoke(this, e);
            }
        }
        private void StartUDPReceiveData()
        {
            //AnonSocketUtil.Debug($"Try getting the packet and use UDP {_serverUDPEP}");
            EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
            _utSocket.UdpSocket.BeginReceiveFrom(_buff, 0, _buff.Length, SocketFlags.None, ref endPoint, UDPEndReceive, endPoint);
        }
        private void UDPReceiveData()
        {
            try
            {
                _utSocket.UdpSocket.BeginReceiveFrom(_buff, 0, _buff.Length, SocketFlags.None, ref _serverUDPEP, UDPEndReceive, _serverUDPEP);
            }
            catch (Exception e)
            {
                //AnonSocketUtil.Debug($"UDP接收数据包失败{e}");
                onUDPReceiveFailed?.Invoke(this, e);
            }
        }

        private void UDPEndReceive(IAsyncResult result)
        {
            try
            {
                EndPoint endPoint = (EndPoint)result.AsyncState;
                var receiveCount = _utSocket.UdpSocket.EndReceiveFrom(result, ref endPoint);
                _serverUDPEP = endPoint;
                _buffer.WriteBuffer(_buff, 0, receiveCount);
                PacketBase packet = new PacketBase(_buffer.Buffer);

                onUDPAcceptPacket?.Invoke(packet, _buffer, receiveCount);
                onReceivePacket?.Invoke(packet, _buffer, receiveCount);

                UDPReceiveData();
            }
            catch (Exception e)
            {
                //AnonSocketUtil.Debug($"UDP接收数据包失败{e}");
                onUDPReceiveFailed?.Invoke(this, e);
            }
        }

        private void TCPEndReceive(IAsyncResult result)
        {
            try
            {
                EndPoint endPoint = (EndPoint)result.AsyncState;
                var receiveCount = _utSocket.TcpSocket.EndReceive(result);
                _buffer.WriteBuffer(_buff, 0, receiveCount);
                PacketBase packet = new PacketBase(_buffer.Buffer);

                onTCPAcceptPacket?.Invoke(packet, _buffer, receiveCount);
                onReceivePacket?.Invoke(packet, _buffer, receiveCount);

                TCPReceiveData();
            }
            catch (Exception e)
            {
                AnonSocketUtil.Debug($"与服务器断开连接...{e}");
                onTCPReceiveFailed?.Invoke(this, e);
                onConnectionFailed?.Invoke(this, e);
            }
        }
        private void ClientDefaultHandler(PacketBase packet, PacketBuffer buffer, int receiveCount)
        {
            var id = packet.PacketID;
            var length = packet.Length;
            //AnonSocketUtil.Debug($"收到包长为{length},收到数据长度{receiveCount}");
            if (buffer.Index < length)
            {
                //AnonSocketUtil.Debug($"包过大,尝试分包:包长{length},收到包{receiveCount}");
                return;
            }
            buffer.ResetBuffer(length);
            PacketHandler.HandlerPacket(packet, packet.PacketID);
        }

        public void Dispose()
        {
            _utSocket.Dispose();
        }
        public void Close()
        {
            Dispose();
        }
    }
}
