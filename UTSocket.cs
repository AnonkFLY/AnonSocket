using AnonSocket.Data;
using System;
using System.Net;
using System.Net.Sockets;

namespace AnonSocket
{
    public struct MessageData
    {
        public Socket serverSocket;
        public PacketBase packetData;
        public MessageData(PacketBase packet,Socket socket)
        {
            this.packetData = packet;
            this.serverSocket = socket;
        }
    }
    public class UTSocket : IDisposable
    {
        private Socket _udpSocket;
        private Socket _tcpSocket;
        private UTSocketType _type;
        private int _udpPort;
        private int _tcpPort;
        private EndPoint _serverUDPEP;
        public Socket TcpSocket { get => _tcpSocket; }
        public Socket UdpSocket { get => _udpSocket; }
        public EndPoint ServerUDPEP { get => _serverUDPEP; }

        public UTSocket(UTSocketType type, int tcpPort, int udpPort)
        {
            _type = type;
            _tcpPort = tcpPort;
            _udpPort = udpPort;
            InitUTSocket();
            BindIPEndPoint();
        }

        private void BindIPEndPoint()
        {
            switch (_type)
            {
                case UTSocketType.Server:
                    ServerBind();
                    break;

                case UTSocketType.Client:
                    ClientBind();
                    break;
            }
        }
        private void ServerBind()
        {
            _udpSocket.Bind(new IPEndPoint(IPAddress.Any, _udpPort));
            _tcpSocket.Bind(new IPEndPoint(IPAddress.Any, _tcpPort));
        }
        private void ClientBind(int port = 0)
        {
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, port);
            _tcpSocket.Bind(ip);
            _udpSocket.Bind(ip);
        }
        private void InitUTSocket()
        {
            _tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }
        public void Dispose()
        {
            AnonSocketUtil.Debug("关闭了服务器！！！！！！！！！！");
            _tcpSocket.Close();
            _udpSocket.Close();
        }
        /// <summary>
        /// Server set max listener
        /// </summary>
        /// <param name="count">max connect</param>
        public void ListenerClients(int count)
        {
            _tcpSocket.Listen(count);
        }
        public void ConnectToServer(IPAddress ip, AsyncCallback callBack, object state)
        {
            IPEndPoint tcpEP = new IPEndPoint(ip, _tcpPort);
            IPEndPoint udpEP = new IPEndPoint(ip, _udpPort);
            _tcpSocket.BeginConnect(tcpEP, callBack, state);
            //_udpSocket.BeginConnect(udpEP, callBack, state);
            _serverUDPEP = new IPEndPoint(ip, _udpPort);
        }
        public void ConnectToServer(ConnectState connect)
        {
            IPEndPoint tcpEP = new IPEndPoint(connect.ip, _tcpPort);
            IPEndPoint udpEP = new IPEndPoint(connect.ip, _udpPort);
            _tcpSocket.BeginConnect(tcpEP, connect.tcpCallBack, connect.tcpState);
            _tcpSocket.BeginConnect(tcpEP, connect.udpCallBack, connect.udpState);
        }
    }
    public class ConnectState
    {
        public IPAddress ip;
        public AsyncCallback tcpCallBack;
        public AsyncCallback udpCallBack;
        public object tcpState;
        public object udpState;
        public ConnectState(IPAddress ip)
        {
            this.ip = ip;
        }
        public void SetAsyncCallBack(AsyncCallback async)
        {
            tcpCallBack = async;
            udpCallBack = async;
        }
        public void SetState(object state)
        {
            udpState = state;
            tcpState = state;
        }
    }
    public enum UTSocketType
    {
        Server,
        Client
    }
}
