using AnonSocket.Data;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AnonSocket.AnonServer
{
    public class UDPClientList
    {

        private Dictionary<EndPoint, Client> _udpClients;
        public UDPClientList()
        {
            _udpClients = new Dictionary<EndPoint, Client>();
        }
        public void SendUDPPacket(byte[] buffer, EndPoint endPoint, int receiveCount)
        {
            //AnonSocketUtil.Debug($"尝试发包{packet.PacketID}");
            _udpClients.TryGetValue(endPoint, out var client);
            if (client != null)
                client.RecevieUDPData(buffer, receiveCount);
        }
        public void RegisterClientUDP(EndPoint endPoint, Client client)
        {
            _udpClients.Add(endPoint, client);
            client.InitClientUDP(endPoint);
        }
        public void RemoveClient(EndPoint endPoint)
        {
            _udpClients.Remove(endPoint);
        }
    }
}
