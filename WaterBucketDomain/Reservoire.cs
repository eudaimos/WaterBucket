using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using ZeroMQ;

namespace WaterBucket.Domain
{
    [Serializable]
    public class Reservoire : ICloneable, IBinaryConvertible, IFrameable
    {
        public int VolumeUsed { get; private set; }

        public int VolumeThrownOut { get; private set; }

        public virtual int GetWater(int amount)
        {
            if (amount < 1)
                throw new ArgumentOutOfRangeException("amount", "Amount must be a positive integer and greater than 0");
            if (int.MaxValue - VolumeUsed < amount)
                throw new ArgumentOutOfRangeException("amount", "Amount would exceed int.MaxValue in VolumeUsed on Reservoire");
            VolumeUsed += amount;
            return amount;
        }

        public virtual void PutWater(int amount)
        {
            if (amount < 1)
                throw new ArgumentOutOfRangeException("amount", "Amount must be a positive integer and greater than 0");
            if (int.MaxValue - VolumeThrownOut < amount)
                throw new ArgumentOutOfRangeException("amount", "Amount would exceed int.MaxValue in VolumeThrownOut on Reservoire");
            VolumeThrownOut += amount;
        }

        public ReservoireState CurrentState
        {
            get
            {
                return new ReservoireState(VolumeUsed, VolumeThrownOut);
            }
            private set
            {
                if (value == null)
                {
                    VolumeUsed = 0;
                    VolumeThrownOut = 0;
                }
                else
                {
                    VolumeUsed = value.VolumeUsed;
                    VolumeThrownOut = value.VolumeThrownOut;
                }
            }
        }

        public Reservoire()
        {
            VolumeUsed = 0;
            VolumeThrownOut = 0;
        }

        public Reservoire(ReservoireState state)
        {
            VolumeUsed = state.VolumeUsed;
            VolumeThrownOut = state.VolumeThrownOut;
        }

        public object Clone()
        {
            var clone = new Reservoire();
            clone.VolumeUsed = this.VolumeUsed;
            clone.VolumeThrownOut = this.VolumeThrownOut;
            return clone;
        }

        public static Reservoire GetFromBytes(byte[] data, int offset = 0)
        {
            Reservoire r = new Reservoire();
            r.FromBytes(data, offset);
            return r;
        }

        public byte[] GetBytes(Encoding textEncoding = null)
        {
            return this.CurrentState.GetBytes(textEncoding);
        }

        public int OntoBuffer(byte[] buffer, int offset = 0, Encoding textEncoding = null)
        {
            return this.CurrentState.OntoBuffer(buffer, offset, textEncoding);
        }

        public int FromBytes(byte[] data, int offset = 0, Encoding textEncoding = null)
        {
            ReservoireState rs = new ReservoireState();
            int updatedOffset = rs.FromBytes(data, offset, textEncoding);
            CurrentState = rs;
            return updatedOffset;
        }

        public IEnumerable<Frame> GetFrames(Encoding textEncoding = null)
        {
            return this.CurrentState.GetFrames(textEncoding);
        }

        public int FromFrames(IEnumerable<Frame> frames, int offsetFrame = 0, Encoding textEncoding = null)
        {
            ReservoireState rs = new ReservoireState();
            int updatedOffset = rs.FromFrames(frames, offsetFrame, textEncoding);
            CurrentState = rs;
            return updatedOffset;
        }
    }

    [Serializable]
    public class ReservoireState : IBinaryConvertible, IFrameable
    {
        public int VolumeUsed { get; private set; }

        public int VolumeThrownOut { get; private set; }

        public ReservoireState()
        {
            VolumeUsed = 0;
            VolumeThrownOut = 0;
        }

        public ReservoireState(int volumeUsed, int volumeThrownOut)
        {
            if (volumeUsed < 0)
                throw new ArgumentOutOfRangeException("volumeUsed");
            if (volumeThrownOut < 0)
                throw new ArgumentOutOfRangeException("volumeThrownOut");

            VolumeUsed = volumeUsed;
            VolumeThrownOut = volumeThrownOut;
        }

        public static ReservoireState GetFromBytes(byte[] data, int offset = 0)
        {
            ReservoireState rs = new ReservoireState();
            rs.FromBytes(data, offset);
            return rs;
        }

        public static ReservoireState GetFromBytes(byte[] data, ref int offset)
        {
            ReservoireState rs = new ReservoireState();
            offset = rs.FromBytes(data, offset);
            return rs;
        }

        public byte[] GetBytes(Encoding textEncoding = null)
        {
            byte[] bytes = new byte[8];
            Buffer.BlockCopy(BitConverter.GetBytes(VolumeUsed), 0, bytes, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(VolumeThrownOut), 0, bytes, 4, 4);
            return bytes;
        }

        public int OntoBuffer(byte[] buffer, int offset = 0, Encoding textEncoding = null)
        {
            int updatedOffset = offset;
            Buffer.BlockCopy(BitConverter.GetBytes(VolumeUsed), 0, buffer, updatedOffset, 4);
            updatedOffset += 4;
            Buffer.BlockCopy(BitConverter.GetBytes(VolumeThrownOut), 0, buffer, updatedOffset, 4);
            return updatedOffset + 4;
        }

        public int FromBytes(byte[] data, int offset = 0, Encoding textEncoding = null)
        {
            int updatedOffset = offset;
            VolumeUsed = BitConverter.ToInt32(data, updatedOffset);
            updatedOffset += 4;
            VolumeThrownOut = BitConverter.ToInt32(data, updatedOffset);
            return updatedOffset + 4;
        }

        public IEnumerable<Frame> GetFrames(Encoding textEncoding = null)
        {
            yield return this.GetCombinedFrame(rs => rs.VolumeUsed, rs => rs.VolumeThrownOut);
        }

        public int FromFrames(IEnumerable<Frame> frames, int offsetFrame = 0, Encoding textEncoding = null)
        {
            return this.FromCombinedFramePart(frames, offsetFrame, (rs, i) => rs.VolumeUsed = i, (rs, i) => rs.VolumeThrownOut = i);
        }
    }
}