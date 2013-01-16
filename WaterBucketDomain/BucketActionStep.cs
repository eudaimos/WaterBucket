using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroMQ;

namespace WaterBucket.Domain
{
    public enum BucketActions : byte
    {
        Init = 0,
        Empty = 1,
        Fill = 2,
        Transfer = 3
    }

    [Serializable]
    public class BucketActionStep : ProblemUpdateState
    {
        public string StrategyName { get; private set; }

        public Bucket ActedOn { get; private set; }

        public BucketActions ActionTaken { get; private set; }

        public int StepNumber { get; private set; }

        public int Amount { get; private set; }

        public string Description { get; private set; }

        public ProblemState EndingState { get; private set; }

        public BucketActionStep() { }

        public BucketActionStep(string strategyName, Bucket actedOn, BucketActions actionTaken, int stepNumber, int amount, string description, ProblemState state)
        {
            StrategyName = strategyName;
            ActedOn = actedOn;
            ActionTaken = actionTaken;
            StepNumber = stepNumber;
            Amount = amount;
            Description = description;
            EndingState = state;
        }

        public override byte[] GetBytes(Encoding textEncoding = null)
        {
            textEncoding = textEncoding ?? Encoding.UTF8;
            byte[] strategyNameBytes = textEncoding.GetBytes(StrategyName);
            byte[] actedOnBytes = ActedOn.GetBytes(textEncoding);
            byte[] descriptionBytes = textEncoding.GetBytes(Description);
            byte[] endingStateBytes = EndingState.GetBytes();
            int updatedOffset = 0;
            byte[] bytes = new byte[4 + strategyNameBytes.Length + actedOnBytes.Length + 1 + 4 + 4 + descriptionBytes.Length + endingStateBytes.Length];
            Buffer.BlockCopy(BitConverter.GetBytes(strategyNameBytes.Length), 0, bytes, updatedOffset, 4);
            updatedOffset += 4;
            Buffer.BlockCopy(strategyNameBytes, 0, bytes, 4, strategyNameBytes.Length);
            updatedOffset += strategyNameBytes.Length;
            Buffer.BlockCopy(actedOnBytes, 0, bytes, updatedOffset, actedOnBytes.Length);
            updatedOffset += actedOnBytes.Length;
            bytes[updatedOffset] = (byte)ActionTaken;
            updatedOffset++;
            Buffer.BlockCopy(BitConverter.GetBytes(Amount), 0, bytes, updatedOffset, 4);
            updatedOffset += 4;
            Buffer.BlockCopy(BitConverter.GetBytes(descriptionBytes.Length), 0, bytes, updatedOffset, 4);
            updatedOffset += 4;
            Buffer.BlockCopy(descriptionBytes, 0, bytes, updatedOffset, descriptionBytes.Length);
            updatedOffset += descriptionBytes.Length;
            Buffer.BlockCopy(endingStateBytes, 0, bytes, updatedOffset, endingStateBytes.Length);
            return bytes;
        }

        public override int OntoBuffer(byte[] buffer, int offset = 0, Encoding textEncoding = null)
        {
            byte[] actedOnBytes = ActedOn.GetBytes(textEncoding);
            byte[] endingStateBytes = EndingState.GetBytes();
            int updatedOffset = 0;
            //byte[] bytes = new byte[strategyNameBytes.Length + 4 + actedOnBytes.Length + descriptionBytes.Length + 4 + endingStateBytes.Length];
            textEncoding = textEncoding ?? Encoding.UTF8;
            byte[] strategyNameBytes = textEncoding.GetBytes(StrategyName);
            Buffer.BlockCopy(BitConverter.GetBytes(strategyNameBytes.Length), 0, buffer, updatedOffset, 4);
            updatedOffset += 4;
            Buffer.BlockCopy(strategyNameBytes, 0, buffer, 4, strategyNameBytes.Length);
            updatedOffset += strategyNameBytes.Length;
            updatedOffset = ActedOn.OntoBuffer(buffer, updatedOffset, textEncoding);
            //Buffer.BlockCopy(actedOnBytes, 0, bytes, updatedOffset, actedOnBytes.Length);
            //updatedOffset += actedOnBytes.Length;

            buffer[updatedOffset] = (byte)ActionTaken;
            updatedOffset++;
            Buffer.BlockCopy(BitConverter.GetBytes(Amount), 0, buffer, updatedOffset, 4);
            updatedOffset += 4;
            byte[] descriptionBytes = textEncoding.GetBytes(Description);
            Buffer.BlockCopy(BitConverter.GetBytes(descriptionBytes.Length), 0, buffer, updatedOffset, 4);
            updatedOffset += 4;
            Buffer.BlockCopy(descriptionBytes, 0, buffer, updatedOffset, descriptionBytes.Length);
            updatedOffset += descriptionBytes.Length;

            return EndingState.OntoBuffer(buffer, updatedOffset, textEncoding);
            //Buffer.BlockCopy(endingStateBytes, 0, bytes, updatedOffset, endingStateBytes.Length);
            //return bytes;
        }

        public static BucketActionStep GetFromBytes(byte[] data, int offset = 0, Encoding textEncoding = null)
        {
            BucketActionStep bs = new BucketActionStep();
            bs.FromBytes(data, offset, textEncoding);
            return bs;
        }

        public static BucketActionStep GetFromBytes(byte[] data, ref int offset, Encoding textEncoding = null)
        {
            BucketActionStep bs = new BucketActionStep();
            offset = bs.FromBytes(data, offset, textEncoding);
            return bs;
        }

        public override int FromBytes(byte[] data, int offset = 0, Encoding textEncoding = null)
        {
            textEncoding = textEncoding ?? Encoding.UTF8;
            int updatedOffset = offset;
            int stringByteLength = BitConverter.ToInt32(data, updatedOffset);
            updatedOffset += 4;
            StrategyName = textEncoding.GetString(data, updatedOffset, stringByteLength);
            updatedOffset += stringByteLength;
            ActedOn = Bucket.GetFromBytes(data, ref updatedOffset, textEncoding);
            ActionTaken = (BucketActions)data[updatedOffset];
            updatedOffset++;
            Amount = BitConverter.ToInt32(data, updatedOffset);
            updatedOffset += 4;
            stringByteLength = BitConverter.ToInt32(data, updatedOffset);
            updatedOffset += 4;
            Description = textEncoding.GetString(data, updatedOffset, stringByteLength);
            updatedOffset += stringByteLength;
            EndingState = ProblemState.GetFromBytes(data, ref updatedOffset, textEncoding);
            return updatedOffset;
        }

        public override IEnumerable<Frame> GetFrames(Encoding textEncoding = null)
        {
            textEncoding = textEncoding ?? Encoding.UTF8;
            yield return this.GetFramePart(StrategyName, textEncoding);
            foreach (var f in this.GetFrameParts(ActedOn, textEncoding))
                yield return f;
            yield return this.GetFramePart((byte)ActionTaken);
            yield return this.GetCombinedFrame(StepNumber, Amount);
            yield return this.GetFramePart(Description, textEncoding);
            foreach (var f in this.GetFrameParts(EndingState, textEncoding))
                yield return f;
        }

        public override int FromFrames(IEnumerable<Frame> frames, int offsetFrame = 0, Encoding textEncoding = null)
        {
            textEncoding = textEncoding ?? Encoding.UTF8;
            int updatedOffset = this.FromFramePart(frames, (bs, s) => bs.StrategyName = s, offsetFrame, textEncoding);
            updatedOffset = this.FromFrameParts<BucketActionStep, Bucket>(frames, (bs, b) => bs.ActedOn = b, updatedOffset, textEncoding);
            updatedOffset = this.FromFramePart(frames, (bs, b) => bs.ActionTaken = (BucketActions)b, updatedOffset);
            updatedOffset = this.FromCombinedFramePart(frames, updatedOffset, (bs, i) => bs.StepNumber = i, (bs, i) => bs.Amount = i);
            updatedOffset = this.FromFramePart(frames, (bs, s) => bs.Description = s, updatedOffset, textEncoding);
            updatedOffset = this.FromFrameParts<BucketActionStep, ProblemState>(frames, (bs, ps) => bs.EndingState = ps, updatedOffset, textEncoding);
            return updatedOffset;
        }
    }
}
