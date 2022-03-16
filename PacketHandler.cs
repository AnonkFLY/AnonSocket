using AnonSocket.Data;
using System;

namespace AnonSocket
{
    public class PacketHandler<T>
    {
        private readonly int initalLength = 16;

        public delegate void PacketProcessingEvents(T packet);
        private PacketProcessingEvents[] _packetUDPProcessingEvents;
        private PacketProcessingEvents[] _packetTCPProcessingEvents;
        private PacketProcessingEvents _onConnectEvents;
        public PacketHandler()
        {
            _packetUDPProcessingEvents = new PacketProcessingEvents[initalLength];
            _packetTCPProcessingEvents = new PacketProcessingEvents[initalLength];
        }
        public void RegisterHandler(int packetID, PacketProcessingEvents handler)
        {
            //AnonSocketUtil.Debug($"尝试注册id:{packetID}的处理");
            if (packetID < 0)
            {
                //AnonSocketUtil.Debug($"尝试注册TCP协议,id:{-packetID - 1}");
                SetProcessingEvents(ref _packetTCPProcessingEvents, -packetID - 1, handler);
            }
            else if (packetID > 0)
            {
                //AnonSocketUtil.Debug($"尝试注册UDP协议,id:{packetID - 1}");
                SetProcessingEvents(ref _packetUDPProcessingEvents, packetID - 1, handler);
            }
            else
                _onConnectEvents = handler;
        }
        public void HandlerPacket(T packet, int id)
        {
            var action = GetIDHandler(id);
            if (action != null)
                action.Invoke(packet);
            else
                DefaultHanlder(packet, id);

        }
        private void DefaultHanlder(T packet, int id)
        {
            AnonSocketUtil.Debug($"收到无法处理的数据包:ID[{id}],Type[{packet.GetType()}]");
        }
        private PacketProcessingEvents GetIDHandler(int id)
        {
            if (id < 0)
            {
                //AnonSocketUtil.Debug("获得TCP数据包" + id);
                return _packetTCPProcessingEvents[-id - 1];
            }
            else if (id > 0)
            {
                //AnonSocketUtil.Debug("获得UDP数据包" + id);
                return _packetUDPProcessingEvents[id - 1];
            }
            //AnonSocketUtil.Debug("获得连接数据包");
            return _onConnectEvents;
        }
        private void SetProcessingEvents(ref PacketProcessingEvents[] packetProcessingEvents, int index, PacketProcessingEvents handler)
        {
            int length = packetProcessingEvents.Length;
            while (index > length - 1)
            {
                //AnonSocketUtil.Debug($"索引为{index},处理器长度过短,尝试扩展:{length}");
                var tempPPE = new PacketProcessingEvents[length * 2];
                packetProcessingEvents.CopyTo(tempPPE, 0);
                packetProcessingEvents = tempPPE;
                length = packetProcessingEvents.Length;
            }
            packetProcessingEvents[index] = handler;
            //AnonSocketUtil.Debug($"UDP长度{_packetUDPProcessingEvents.Length},TCP长度:{_packetTCPProcessingEvents.Length}");
        }
    }
}
