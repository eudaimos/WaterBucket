using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using ZeroMQ;

namespace WaterBucket.Domain
{
    [Serializable]
    public class Bucket : ICloneable, IBinaryConvertible, IFrameable
    {
        //private object _lock = new object();

        public int Capacity { get; private set; }

        public int CurrentFill { get; private set; }

        public bool IsFull { get { return Capacity - CurrentFill == 0; } }

        public bool IsEmpty { get { return CurrentFill == 0; } }

        public string Name { get; set; }

        public Bucket()
        {
        }

        public Bucket(int capacity, string name = "")
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException("capacity", "A Bucket cannot have a Capacity that is less than 1");
            Capacity = capacity;
            CurrentFill = 0;
            Name = name;
        }

        public int Fill(Reservoire source)
        {
            if (!this.IsFull)
            {
                int amount = this.Capacity - this.CurrentFill;
                this.CurrentFill += source.GetWater(this.Capacity - this.CurrentFill);
                return amount;
            }
            return 0;
        }

        /// <summary>
        /// Transfers water from this Bucket to the argument Bucket (toBucket), returning the amount left
        /// </summary>
        /// <param name="otherBucket"></param>
        /// <returns>Amount transferred</returns>
        public int TransferTo(Bucket otherBucket)
        {
            if (this.IsEmpty)
                return 0;
            if (otherBucket.IsFull)
                throw new ArgumentOutOfRangeException("otherBucket", "Cannot Transfer water to Bucket that is already Full");
            int amountTransferred = 0;
            int amountOfSpace = otherBucket.Capacity - otherBucket.CurrentFill;
            int amountAfterTransfer = this.CurrentFill - amountOfSpace;
            if (amountAfterTransfer < 0)
            {
                otherBucket.CurrentFill += this.CurrentFill;
                amountTransferred = this.CurrentFill;
                this.CurrentFill = 0;
            }
            else
            {
                otherBucket.CurrentFill += amountOfSpace;
                this.CurrentFill -= amountOfSpace;
                amountTransferred = amountOfSpace;
            }
            return amountTransferred;
        }

        public int Empty(Reservoire into)
        {
            int amount = this.CurrentFill;
            into.PutWater(amount);
            this.CurrentFill = 0;
            return amount;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Bucket);
        }

        public bool Equals(Bucket other)
        {
            if (other == null)
                return false;

            if (!string.IsNullOrWhiteSpace(this.Name) || !string.IsNullOrWhiteSpace(other.Name))
            {
                return (this.Capacity == other.Capacity) && (this.CurrentFill == other.CurrentFill) && this.Name.Equals(other.Name);
            }
            return (this.Capacity == other.Capacity) && (this.CurrentFill == other.CurrentFill);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public object Clone()
        {
            Bucket clone = new Bucket(this.Capacity, this.Name);
            clone.CurrentFill = this.CurrentFill;
            return clone;
        }

        public byte[] GetBytes(Encoding textEncoding = null)
        {
            textEncoding = textEncoding ?? Encoding.UTF8;
            byte[] nameBytes = textEncoding.GetBytes(Name);
            byte[] bytes = new byte[nameBytes.Length + 12];
            Buffer.BlockCopy(BitConverter.GetBytes(Capacity), 0, bytes, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(CurrentFill), 0, bytes, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(nameBytes.Length), 0, bytes, 8, 4);
            Buffer.BlockCopy(nameBytes, 0, bytes, 12, nameBytes.Length);
            return bytes;
        }

        public int OntoBuffer(byte[] buffer, int offset = 0, Encoding textEncoding = null)
        {
            int updatedOffset = offset;
            Buffer.BlockCopy(BitConverter.GetBytes(Capacity), 0, buffer, updatedOffset, 4);
            updatedOffset += 4;
            Buffer.BlockCopy(BitConverter.GetBytes(CurrentFill), 0, buffer, updatedOffset, 4);
            updatedOffset += 4;

            textEncoding = textEncoding ?? Encoding.UTF8;
            byte[] nameBytes = textEncoding.GetBytes(Name);
            Buffer.BlockCopy(BitConverter.GetBytes(nameBytes.Length), 0, buffer, updatedOffset, 4);
            updatedOffset += 4;
            Buffer.BlockCopy(nameBytes, 0, buffer, updatedOffset, nameBytes.Length);
            return updatedOffset + nameBytes.Length;
        }

        public static Bucket GetFromBytes(byte[] data, int offset = 0, Encoding textEncoding = null)
        {
            Bucket bucket = new Bucket();
            bucket.FromBytes(data, offset, textEncoding);
            return bucket;
        }

        public static Bucket GetFromBytes(byte[] data, ref int offset, Encoding textEncoding = null)
        {
            Bucket bucket = new Bucket();
            offset = bucket.FromBytes(data, offset, textEncoding);
            return bucket;
        }

        public int FromBytes(byte[] data, int offset = 0, Encoding textEncoding = null)
        {
            int updatedOffset = offset;
            textEncoding = textEncoding ?? Encoding.UTF8;
            Capacity = BitConverter.ToInt32(data, updatedOffset);
            updatedOffset += 4;
            CurrentFill = BitConverter.ToInt32(data, updatedOffset);
            updatedOffset += 4;
            int nameByteLength = BitConverter.ToInt32(data, updatedOffset);
            updatedOffset += 4;
            Name = textEncoding.GetString(data, updatedOffset, nameByteLength);
            return updatedOffset + nameByteLength;
        }

        public IEnumerable<Frame> GetFrames(Encoding textEncoding = null)
        {
            yield return this.GetCombinedFrame(b => b.Capacity, b => b.CurrentFill);
            textEncoding = textEncoding ?? Encoding.UTF8;
            yield return this.GetFramePart(Name, textEncoding);
        }

        public int FromFrames(IEnumerable<Frame> frames, int offsetFrame = 0, Encoding textEncoding = null)
        {
            int updatedOffset = offsetFrame;
            updatedOffset = this.FromCombinedFramePart(frames, updatedOffset, (b, i) => { b.Capacity = i; }, (b, i) => { b.CurrentFill = i; });
            textEncoding = textEncoding ?? Encoding.UTF8;
            updatedOffset = this.FromFramePart(frames, (b, name) => b.Name = name, updatedOffset, textEncoding);
            return updatedOffset;
        }
    }
}