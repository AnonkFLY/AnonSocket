using System;
using System.Collections.Generic;
using System.Text;

namespace AnonSocket.Data
{
    public class PacketBuffer
    {
        private byte[] _buffer;
        private int _index;
        private int _reserve;

        public byte[] Buffer { get => _buffer; }
        public int Index { get => _index; }

        public PacketBuffer(int bufferSize)
        {
            _index = 0;
            _reserve = bufferSize;
            _buffer = new byte[_reserve];
        }
        public void SetBuffer(byte[] buffer)
        {
            _buffer = buffer;
            _index = _buffer.Length;
            _reserve = 0;
        }

        public void WriteBuffer(byte[] buffer, int offset, int length)
        {
            if (_reserve >= length)
            {
                Array.Copy(buffer, offset, _buffer, _index, length);
            }
            else
            {
                int totalSize = _buffer.Length + length - _reserve;
                byte[] tempBuffer = new byte[totalSize];
                Array.Copy(_buffer, 0, tempBuffer, 0, _buffer.Length);
                Array.Copy(buffer, offset, tempBuffer, Index, length);
                _buffer = tempBuffer;
            }
            _index += length;
            _reserve = _buffer.Length - Index;
        }
        public void WriteBuffer(byte[] buffer)
        {
            WriteBuffer(buffer, 0, buffer.Length);
        }
        public void ResetBuffer(int packetHead)
        {
            var length = _buffer.Length;
            var tempBuffer = new byte[length];//[1,2,3,4,5,0,0,0]; _index = 5 reserve = 3;
            length -= packetHead;
            AnonSocketUtil.Debug($"尝试重置长度，原长度{length+packetHead},现长度{length},PacketHead{packetHead}");
            Array.Copy(_buffer, packetHead, tempBuffer, 0, length);
            _index -= packetHead;
            _reserve += packetHead;
        }
    }
}
