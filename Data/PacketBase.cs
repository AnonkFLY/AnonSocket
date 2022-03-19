using System;
using System.Text;
using UnityEngine;

namespace AnonSocket.Data
{
    /// <summary>
    /// Int32,float
    /// </summary>
    public class PacketBase
    {
        private int _packetID;
        private int _index;
        private int _length;
        protected byte[] _datas;
        //protected byte[] _buffer;

        public int PacketID { get => _packetID; }
        public int Length { get => _length; }
        public PacketBase(int packetId, int maxPacketSize = 1024)
        {
            _packetID = packetId;
            _datas = new byte[maxPacketSize];
            _index = 0;
            _length = 0;
            Write(_packetID);
            Write(_length);
        }
        public PacketBase(byte[] data)
        {
            _index = 0;
            _datas = data;
            _length = data.Length;
            _packetID = ReadInt32();
            _length = ReadInt32();
            AnonSocketUtil.Debug($"收到包{_packetID},包长{_length}");
        }

        public int ReadInt32()
        {
            var result = (int)(_datas[_index] | _datas[_index + 1] << 8 | _datas[_index + 2] << 16 | _datas[_index + 3] << 24);
            FillBuffer(4);
            return result;
        }
        public unsafe void Write(int value)
        {
            int TmpValue = *(int*)&value;
            _datas[_length] = (byte)TmpValue;
            _datas[_length + 1] = (byte)(TmpValue >> 8);
            _datas[_length + 2] = (byte)(TmpValue >> 16);
            _datas[_length + 3] = (byte)(TmpValue >> 24);
            _length += 4;
        }

        public unsafe float ReadFloat()
        {
            uint tmpBuffer = (uint)(_datas[_index] | _datas[_index + 1] << 8 | _datas[_index + 2] << 16 | _datas[_index + 3] << 24);
            FillBuffer(4);
            return *((float*)&tmpBuffer);
        }
        public unsafe void Write(float value)
        {
            uint TmpValue = *(uint*)&value;
            _datas[_length] = (byte)TmpValue;
            _datas[_length + 1] = (byte)(TmpValue >> 8);
            _datas[_length + 2] = (byte)(TmpValue >> 16);
            _datas[_length + 3] = (byte)(TmpValue >> 24);
            _length += 4;
        }

        public string ReadString()
        {
            var len = ReadInt32();

            byte[] buff = new byte[len];
            Array.Copy(_datas, _index, buff, 0, len);
            FillBuffer(len);
            return Encoding.UTF8.GetString(buff);
        }
        public void Write(string value)
        {
            var buff = Encoding.UTF8.GetBytes(value);
            Write(buff.Length);
            Write(buff, 0, buff.Length);
        }

        public Vector3 ReadVector3()
        {
            float x = ReadFloat();
            float y = ReadFloat();
            float z = ReadFloat();
            var result = new Vector3(x, y, z);
            return result;
        }
        public void Write(Vector3 value)
        {
            Write(value.x);
            Write(value.y);
            Write(value.z);
        }
        public ulong ReadULong()
        {
            uint lo = (uint)(_datas[_index] | _datas[_index + 1] << 8 |
                    _datas[_index + 2] << 16 | _datas[_index + 3] << 24);
            uint hi = (uint)(_datas[_index + 4] | _datas[_index + 5] << 8 |
                    _datas[_index + 6] << 16 | _datas[_index + 7] << 24);
            FillBuffer(8);
            return ((ulong)hi) << 32 | lo;
        }
        public void Write(ulong value)
        {
            _datas[_length] = (byte)value;
            _datas[_length + 1] = (byte)(value >> 8);
            _datas[_length + 2] = (byte)(value >> 16);
            _datas[_length + 3] = (byte)(value >> 24);
            _datas[_length + 4] = (byte)(value >> 32);
            _datas[_length + 5] = (byte)(value >> 40);
            _datas[_length + 6] = (byte)(value >> 48);
            _datas[_length + 7] = (byte)(value >> 56);
            _length += 8;
        }

        public bool ReadBoolean()
        {
            bool value = _datas[_index] != 0;
            FillBuffer(1);
            return value;
        }
        public void Write(bool value)
        {
            _datas[_index] = (byte)(value ? 1 : 0);
            _length++;
        }
        public byte ReadByte()
        {
            byte result = _datas[_index];
            FillBuffer(1);
            return result;
        }
        public void Write(byte value)
        {
            _datas[_index] = value;
            _length++;
        }
        protected bool FillBuffer(int numBytes)
        {
            _index += numBytes;
            return _index < _datas.Length;
        }
        public void Write(byte[] buff)
        {
            Write(buff, 0, buff.Length);
        }
        public void Write(byte[] buff, int start, int len)
        {
            Array.Copy(buff, start, _datas, _length, len);
            _length += len;
        }
        /// <summary>
        /// ReadBuffers
        /// </summary>
        /// <param name="size">-1 is read reserve</param>
        /// <returns></returns>
        public byte[] ReadBytes(int size = -1)
        {
            if (size == -1)
            {
                size = _length - _index;//剩余
            }
            else
            {
                size = Math.Min(size, _length - _index);
            }
            SetPacketLength(_length);
            var _buffer = new byte[size];
            Array.Copy(_datas, _index, _buffer, 0, size);
            FillBuffer(size);
            return _buffer;
        }
        public void ResetIndex()
        {
            _index = 0;
        }
        public unsafe void SetPacketLength(int value)
        {
            int TmpValue = *(int*)&value;
            _datas[4] = (byte)TmpValue;
            _datas[5] = (byte)(TmpValue >> 8);
            _datas[6] = (byte)(TmpValue >> 16);
            _datas[7] = (byte)(TmpValue >> 24);
        }
        public unsafe void SetPacketID(int value)
        {
            int TmpValue = *(int*)&value;
            _datas[0] = (byte)TmpValue;
            _datas[1] = (byte)(TmpValue >> 8);
            _datas[2] = (byte)(TmpValue >> 16);
            _datas[3] = (byte)(TmpValue >> 24);
        }
        public bool OnTCPConnect(bool isServer)
        {
            return isServer ? _packetID <= 0 : _packetID < 0;
        }
    }
}
