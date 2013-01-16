using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroMQ;

namespace WaterBucket.Domain
{
    public interface IFrameable
    {
        IEnumerable<Frame> GetFrames(Encoding textEncoding = null);

        int FromFrames(IEnumerable<Frame> frames, int offsetFrame = 0, Encoding textEncoding = null);
    }

    public static class FrameableExtensions
    {
        public static Frame GetFramePart(this IFrameable obj, byte bProp)
        {
            var f = new Frame(1);
            f.Buffer[0] = bProp;
            return f;
        }

        public static Frame GetFramePart(this IFrameable obj, int iProp)
        {
            return new Frame(BitConverter.GetBytes(iProp));
        }

        public static Frame GetCombinedFrame(this IFrameable obj, params int[] iProps)
        {
            Frame f = new Frame(iProps.Length * 4);
            for (int i = 0; i < iProps.Length; i++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(iProps[i]), 0, f.Buffer, i * 4, 4);
            }
            return f;
        }

        public static Frame GetCombinedFrame<TObject>(this TObject obj, params Func<TObject, int>[] iProps)
            where TObject : IFrameable
        {
            Frame f = new Frame(iProps.Length * 4);
            for (int i = 0; i < iProps.Length; i++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(iProps[i](obj)), 0, f.Buffer, i * 4, 4);
            }
            return f;
        }

        public static Frame GetFramePart(this IFrameable obj, string sProp, Encoding textEncoding = null)
        {
            if (sProp == null)
                return Frame.Empty;

            return new Frame(textEncoding.GetBytes(sProp));
        }

        public static Frame GetFramePart<TObject>(this TObject obj, Func<TObject, string> sProp, Encoding textEncoding = null)
            where TObject : IFrameable
        {
            string s = sProp(obj);
            if (s == null)
                return Frame.Empty;

            return new Frame(textEncoding.GetBytes(s));
        }

        public static IEnumerable<Frame> GetFrameParts(this IFrameable obj, IFrameable oProp, Encoding textEncoding = null)
        {
            if (oProp == null)
                return Enumerable.Repeat(Frame.Empty, 1);

            return oProp.GetFrames(textEncoding);
        }

        public static IEnumerable<Frame> GetFrameParts<TObject, TPropType>(this TObject obj, Func<TObject, TPropType> oPropGetter, Encoding textEncoding = null)
            where TObject : IFrameable
            where TPropType : IFrameable, new()
        {
            return GetFrameParts(obj, oPropGetter(obj), textEncoding);
        }

        public static int FromFramePart<TObject>(this TObject obj, IEnumerable<Frame> frames, Action<TObject, byte> bPropSetter, int offsetFrame = 0)
            where TObject : IFrameable
        {
            int updatedOffset = offsetFrame;
            Frame bFrame = frames.ElementAt(updatedOffset);
            bPropSetter(obj, bFrame.Buffer[0]);
            return updatedOffset + 1;
        }

        public static int FromFramePart<TObject>(this TObject obj, IEnumerable<Frame> frames, Action<int> iPropSetter, int offsetFrame = 0)
            where TObject : IFrameable
        {
            int updatedOffset = offsetFrame;
            Frame iFrame = frames.ElementAt(updatedOffset);
            iPropSetter(BitConverter.ToInt32(iFrame.Buffer, 0));
            return updatedOffset + 1;
        }

        public static int FromCombinedFramePart<TObject>(this TObject obj, IEnumerable<Frame> frames, int offsetFrame, params Action<TObject, int>[] iPropSetters)
            where TObject : IFrameable
        {
            int updatedOffset = offsetFrame;
            Frame iFrame = frames.ElementAt(updatedOffset);
            for (int i = 0; i < iPropSetters.Length; i++)
            {
                iPropSetters[i](obj, BitConverter.ToInt32(iFrame.Buffer, i * 4));
            }
            return updatedOffset + 1;
        }

        public static int FromFramePart<TObject>(this TObject obj, IEnumerable<Frame> frames, Action<TObject, string> sPropSetter, int offsetFrame = 0, Encoding textEncoding = null)
            where TObject : IFrameable
        {
            int updatedOffset = offsetFrame;
            Frame sFrame = frames.ElementAt(updatedOffset);
            if (sFrame.BufferSize == 0)
                sPropSetter(obj, null);
            else
                sPropSetter(obj, textEncoding.GetString(sFrame.Buffer));
            return updatedOffset + 1;
        }

        public static int FromFrameParts<TObject, TPropType>(this TObject obj, IEnumerable<Frame> frames, Action<TObject, TPropType> oPropSetter, int offsetFrame = 0, Encoding textEncoding = null)
            where TObject : IFrameable where TPropType : IFrameable, new()
        {
            int updatedOffset = offsetFrame;
            Frame startFrame = frames.ElementAt(updatedOffset);
            if (startFrame.BufferSize == 0)
            {
                oPropSetter(obj, default(TPropType));
                return updatedOffset + 1;
            }

            var oProp = new TPropType();
            updatedOffset = oProp.FromFrames(frames, offsetFrame, textEncoding);
            oPropSetter(obj, oProp);
            return updatedOffset;
        }
    }
}
