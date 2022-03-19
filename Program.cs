using AnonSocket.AnonServer;
using AnonSocket.Data;
using System;

namespace AnonSocket
{
    class Program
    {
        static void Main(string[] args)
        {

            var server = new ServerSocket(10, 6666, 7777);
            AnonSocketUtil.RegisterDebug(str =>
            {
                Console.WriteLine(str);
            });
            AnonSocketUtil.IsOpenDebug = true;
            //server.onClientConnet += OnConnectClient;
            server.onClientConnet += OnGetUDPMessage;
            server.Open();
            while (true)
            {
                string input = Console.ReadLine();
                if (input == "Quit")
                    break;
                Input(input, server);
            }
        }

        private static void OnGetUDPMessage(Client arg1, int arg2)
        {
            arg1.PacketHandler.RegisterHandler(1, OnHandler);
            arg1.PacketHandler.RegisterHandler(-1, OnHandler);
        }

        private static void OnHandler(ClientData data)
        {
            var packet = data.packet;
            var str = packet.ReadString();
            AnonSocketUtil.Debug("纯文本消息:" + str);
        }

        static void Input(string str, ServerSocket server)
        {
            if (str == "ShowClients")
            {
                foreach (var item in server.Clients)
                {
                    Console.WriteLine(item.ServerTCPSocket.LocalEndPoint);
                }
            }
            var strs = str.Split(' ');
            if (strs[0] == "Send")
            {
                var packet = new PacketBase(-1);
                packet.Write(strs[1]);
                server.BroadcastPacket(packet);
            }
            if (strs[0] == "SendUDP")
            {
                var packet = new PacketBase(1);
                packet.Write(strs[1]);
                server.BroadcastPacket(packet);
            }
            if (str == "SendLong")
            {
                var packet = new PacketBase(-1,5120);
                packet.Write("这是一个很长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长的字符串");
                server.BroadcastPacket(packet);
                AnonSocketUtil.Debug("发包长度:" + packet.ReadBytes().Length);
            }
            if (str == "SendUDPLong")
            {
                var packet = new PacketBase(1,5120);
                packet.Write("这是一个很长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长的字符串");
                server.BroadcastPacket(packet);
                AnonSocketUtil.Debug("发包长度:" + packet.ReadBytes().Length);
            }
        }
        //static void OnConnectClient(Client client, int indx)
        //{
        //    client.onReceiveMessage += DataHandler;
        //}

        //private static void DataHandler(ClientData data)
        //{
        //    var packet = data.packet;
        //    var id = packet.PacketID;
        //    var length = packet.Length;
        //    if (data.buffer.Index < length)
        //    {
        //        AnonSocketUtil.Debug($"包过大,尝试分包:包长{length},收到包{data.receiveCount}");
        //        return;
        //    } 
        //    data.buffer.ResetBuffer(length);
        //    var str = packet.ReadString();
        //    AnonSocketUtil.Debug($"包id{id},包长度{length},包字符串:{str}");
        //}
    }
}
