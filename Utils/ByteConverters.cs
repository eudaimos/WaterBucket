using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Utils
{
    public static class ByteConverters
    {
        public static byte[] GetBytes(int i)
        {
            ByteToInteger b2i = new ByteToInteger() { integer = i };
            return new byte[4] { b2i.b0, b2i.b1, b2i.b2, b2i.b3 };
        }

        public static void OntoBuffer(byte[] buffer, int i, ref int offset)
        {
            ByteToInteger b2i = new ByteToInteger() { integer = i };
            buffer[offset] = b2i.b0;
            buffer[offset + 1] = b2i.b1;
            buffer[offset + 2] = b2i.b2;
            buffer[offset + 3] = b2i.b3;
            Interlocked.Add(ref offset, 4);
        }
    }

    // Taken from SO answer at the bottom: http://stackoverflow.com/questions/8827649/fastest-way-to-convert-int-to-4-bytes-in-c-sharp
    [StructLayout(LayoutKind.Explicit)]
    public struct ByteToInteger
    {
        [FieldOffset(0)]
        public byte b0;
        [FieldOffset(1)]
        public byte b1;
        [FieldOffset(2)]
        public byte b2;
        [FieldOffset(3)]
        public byte b3;

        [FieldOffset(0)]
        public int integer;
    }
}
