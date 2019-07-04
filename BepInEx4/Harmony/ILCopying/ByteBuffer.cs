using System;

namespace Harmony.ILCopying
{
    public class ByteBuffer
    {
        public byte[] buffer;

        public int position;

        public ByteBuffer(byte[] buffer)
        {
            this.buffer = buffer;
        }

        public byte ReadByte()
        {
            CheckCanRead(1);
            var array = buffer;
            var num = position;
            position = num + 1;
            return array[num];
        }

        public byte[] ReadBytes(int length)
        {
            CheckCanRead(length);
            var array = new byte[length];
            Buffer.BlockCopy(buffer, position, array, 0, length);
            position += length;
            return array;
        }

        public short ReadInt16()
        {
            CheckCanRead(2);
            var result = (short) (buffer[position] | (buffer[position + 1] << 8));
            position += 2;
            return result;
        }

        public int ReadInt32()
        {
            CheckCanRead(4);
            var result = buffer[position] | (buffer[position + 1] << 8) | (buffer[position + 2] << 16) |
                         (buffer[position + 3] << 24);
            position += 4;
            return result;
        }

        public long ReadInt64()
        {
            CheckCanRead(8);
            var num = (uint) (buffer[position] | (buffer[position + 1] << 8) | (buffer[position + 2] << 16) |
                              (buffer[position + 3] << 24));
            var result = (long) (((ulong) (buffer[position + 4] | (buffer[position + 5] << 8) |
                                           (buffer[position + 6] << 16) | (buffer[position + 7] << 24)) << 32) | num);
            position += 8;
            return result;
        }

        public float ReadSingle()
        {
            if (!BitConverter.IsLittleEndian)
            {
                var array = ReadBytes(4);
                Array.Reverse(array);
                return BitConverter.ToSingle(array, 0);
            }

            CheckCanRead(4);
            var result = BitConverter.ToSingle(buffer, position);
            position += 4;
            return result;
        }

        public double ReadDouble()
        {
            if (!BitConverter.IsLittleEndian)
            {
                var array = ReadBytes(8);
                Array.Reverse(array);
                return BitConverter.ToDouble(array, 0);
            }

            CheckCanRead(8);
            var result = BitConverter.ToDouble(buffer, position);
            position += 8;
            return result;
        }

        private void CheckCanRead(int count)
        {
            if (position + count > buffer.Length) throw new ArgumentOutOfRangeException();
        }
    }
}