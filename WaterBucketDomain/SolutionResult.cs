using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroMQ;

namespace WaterBucket.Domain
{
    [Serializable]
    public class SolutionResult : ProblemUpdateState
    {
        public int NumberOfActions { get; private set; }

        public ReservoireState EndingReservoireState { get; private set; }

        public SolutionResult() { }

        public SolutionResult(int numberOfActions, Reservoire reservoire)
        {
            NumberOfActions = numberOfActions;
            EndingReservoireState = reservoire.CurrentState;
        }

        public SolutionResult(int numberOfActions, ReservoireState endingReservoireState)
        {
            NumberOfActions = numberOfActions;
            EndingReservoireState = endingReservoireState;
        }

        public override byte[] GetBytes(Encoding textEncoding = null)
        {
            textEncoding = textEncoding ?? Encoding.UTF8;
            byte[] reservoireBytes = EndingReservoireState.GetBytes(textEncoding);
            byte[] returnBytes = new byte[4 + reservoireBytes.Length];
            Buffer.BlockCopy(BitConverter.GetBytes(NumberOfActions), 0, returnBytes, 0, 4);
            Buffer.BlockCopy(reservoireBytes, 0, returnBytes, 4, reservoireBytes.Length);
            return returnBytes;
        }

        public override int OntoBuffer(byte[] buffer, int offset = 0, Encoding textEncoding = null)
        {
            textEncoding = textEncoding ?? Encoding.UTF8;
            int updatedOffset = offset;
            Buffer.BlockCopy(BitConverter.GetBytes(NumberOfActions), 0, buffer, updatedOffset, 4);
            updatedOffset += 4;
            updatedOffset = EndingReservoireState.OntoBuffer(buffer, updatedOffset, textEncoding);
            return updatedOffset;
        }

        public static SolutionResult GetFromBytes(byte[] data, int offset = 0, Encoding textEncoding = null)
        {
            SolutionResult sr = new SolutionResult();
            sr.FromBytes(data, offset, textEncoding);
            return sr;
        }

        public static SolutionResult GetFromBytes(byte[] data, ref int offset, Encoding textEncoding = null)
        {
            SolutionResult sr = new SolutionResult();
            offset = sr.FromBytes(data, offset, textEncoding);
            return sr;
        }

        public override int FromBytes(byte[] data, int offset = 0, Encoding textEncoding = null)
        {
            textEncoding = textEncoding ?? Encoding.UTF8;
            int updatedOffset = offset;
            NumberOfActions = BitConverter.ToInt32(data, updatedOffset);
            updatedOffset += 4;
            EndingReservoireState = ReservoireState.GetFromBytes(data, ref updatedOffset);
            return updatedOffset;
        }

        public override IEnumerable<Frame> GetFrames(Encoding textEncoding = null)
        {
            yield return this.GetFramePart(NumberOfActions);
            textEncoding = textEncoding ?? Encoding.UTF8;
            foreach (var f in this.GetFrameParts(EndingReservoireState, textEncoding))
                yield return f;
        }

        public override int FromFrames(IEnumerable<Frame> frames, int offsetFrame = 0, Encoding textEncoding = null)
        {
            int updatedOffset = this.FromFramePart(frames, i => this.NumberOfActions = i, offsetFrame);
            textEncoding = textEncoding ?? Encoding.UTF8;
            updatedOffset = this.FromFrameParts<SolutionResult, ReservoireState>(frames, (sr, rs) => sr.EndingReservoireState = rs, updatedOffset, textEncoding);
            return updatedOffset;
        }
    }
}
