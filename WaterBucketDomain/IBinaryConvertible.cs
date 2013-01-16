using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaterBucket.Domain
{
    public interface IBinaryConvertible
    {
        byte[] GetBytes(Encoding textEncoding = null);

        int OntoBuffer(byte[] buffer, int offset = 0, Encoding textEncoding = null);

        int FromBytes(byte[] data, int offset = 0, Encoding textEncoding = null);
    }

    public interface IBinaryConvertible<T> : IBinaryConvertible where T : new()
    {
        void FromTypedBytes(byte[] data, int offset = 0, Encoding textEncoding = null);
    }

    //public abstract class BinaryConvertible<T> : IBinaryConvertible<T>
    //{
    //    public virtual void FromTypedBytes(byte[] data, int offset = 0, Encoding textEncoding = null)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public virtual byte[] GetBytes(Encoding textEncoding = null)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public virtual int OntoBuffer(byte[] buffer, int offset = 0, Encoding textEncoding = null)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public virtual int FromBytes(byte[] data, int offset = 0, Encoding textEncoding = null)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public static T 
    //}

    //public static class BinaryConvertibleExtensions
    //{
    //    public static T 
    //}

    //public class BinaryNull : IBinaryConvertible
    //{
    //    public static byte[] AsNull()
    //    {
    //        return new byte[1] { 0 };
    //    }
    //}

    //public class BinaryNull<T> : BinaryNull where T : IBinaryConvertible<T>, new()
    //{
    //    public static T GetFromBytes(byte[] data, int offset = 0, Encoding textEncoding = null)
    //    {
    //        byte nullByte = data[offset];
    //        if (nullByte == (byte)0)
    //            return default(T);
    //        T t = new T();
    //        t.FromBytes(data, offset, textEncoding);
    //        return t;
    //    }

    //    public static byte[] GetBytes(T data, Encoding textEncoding = null)
    //    {
    //        if (data == null)
    //            return AsNull();
    //        byte[] buffer = data.GetBytes(textEncoding);
    //        byte[] returnBytes = new byte[buffer.Length + 1];
    //        returnBytes[0] = (byte)127;
    //        Buffer.BlockCopy(buffer, 0, returnBytes, 1, buffer.Length);
    //        return returnBytes;
    //    }
    //}
}
