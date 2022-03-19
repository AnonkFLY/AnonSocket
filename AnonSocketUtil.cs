using AnonSocket.Data;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AnonSocket
{
    static class AnonSocketUtil
    {
        private static Action<string> _DebugLog;
        private static bool _isOpenDebug;

        public static bool IsOpenDebug { get => _isOpenDebug; set => _isOpenDebug = value; }

        public static void RegisterDebug(Action<string> action)
        {
            _DebugLog += action;
        }
        public static void Debug(string value)
        {
            if (!_isOpenDebug)
                return;
            _DebugLog?.Invoke(value);
        }
        public static IAsyncResult SubcontractSend(Socket socket, PacketBase packet, EndPoint endPoint, AsyncCallback callback,int subcontractSize)
        {
            //发送数据小于等于缓冲区长度则直接发送
            
            if (packet.Length <= subcontractSize)
            {
                var buff = packet.ReadBytes();
                return socket.BeginSendTo(buff, 0, buff.Length, SocketFlags.None, endPoint, callback, socket);
            }
            //Debug($"包过大，尝试分包发送{packet.Length}");
            byte[] buffer;
            do
            {
                //写入
                buffer = packet.ReadBytes(subcontractSize);
                if (buffer.Length == 0)
                    break;
                socket.BeginSendTo(buffer, 0, buffer.Length, SocketFlags.None, endPoint, callback, socket);
                //Debug($"读出{buffer.Length}并发送,剩余{bufferSize}");
            } while (true);
            packet.ResetIndex();
            return null;
        }
    }
}
