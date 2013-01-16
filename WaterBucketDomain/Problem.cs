using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Reactive.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Utils;
using ZeroMQ;

namespace WaterBucket.Domain
{
    [Serializable]
    public class ProblemVM
    {
        public int FirstBucketCapacity { get; set; }

        public int SecondBucketCapacity { get; set; }

        public int GoalWaterVolume { get; set; }

        //[ScriptIgnore]
        public string CacheKey
        {
            get
            {
                return string.Format("{0}|{1}|{2}", Math.Min(FirstBucketCapacity, SecondBucketCapacity), Math.Max(FirstBucketCapacity, SecondBucketCapacity), GoalWaterVolume);
            }
        }
    }

    public class Problem
    {
        public Bucket FirstBucket { get; private set; }

        public Bucket SecondBucket { get; private set; }

        public Reservoire WaterSource { get; private set; }

        public int GoalWaterVolume { get; private set; }

        public SolutionResult Result { get; private set; }

        public Problem(int firstBucketCapacity, int secondBucketCapacity, int goalWaterVolume)
            : this(new Bucket(firstBucketCapacity, "A"), new Bucket(secondBucketCapacity, "B"), goalWaterVolume)
        {
        }

        public Problem(Bucket first, Bucket second, int goalWaterVolume)
        {
            if (goalWaterVolume < 0)
            {
                throw new ArgumentOutOfRangeException("Cannot solve for a problem with a negative goal");
            }
            FirstBucket = first;
            SecondBucket = second;
            WaterSource = new Reservoire();
            GoalWaterVolume = goalWaterVolume;
        }

        public IEnumerable<BucketActionStep> Solve(ISolutionStrategy strategy)
        {
            if (!this.IsSolvable)
                throw new NotSolvableException("Attempting to solve an unsolvable Problem will result in infinite recursion");
            ProblemState initialState = strategy.CurrentProblemState;
            yield return new BucketActionStep(strategy.StrategyName, FirstBucket, BucketActions.Init, 0, 0, "Initial State", initialState);
            while (!strategy.TestGoal())
            {
                yield return strategy.TakeAction();
                if (initialState.SameBucketState(strategy.CurrentProblemState))
                    throw new NotSolvableException("Problem has reached Initial State again and therefore is not Solvable");
            }
        }

        private bool? _isSolvable = null;
        public bool IsSolvable
        {
            get
            {
                // Since a Problems value set is immutable, test this only once and cache the result
                if (_isSolvable == null)
                {
                    // Problem is solved without filling any water
                    if (GoalWaterVolume == 0)
                    {
                        _isSolvable = true;
                    }
                    // Problem is solved by filling a single bucket once
                    else if ((FirstBucket.Capacity == GoalWaterVolume) || (SecondBucket.Capacity == GoalWaterVolume))
                    {
                        _isSolvable = true;
                    }
                    // Problem cannot be solved if the buckets are the same size and the goal is not that size
                    else if (FirstBucket.Capacity == SecondBucket.Capacity)// && (FirstBucket.Capacity != GoalWaterVolume)): falling through to here implies && condition
                    {
                        _isSolvable = false;
                    }
                    // Problem cannot be solved if the goal is larger than both buckets
                    // TODO: Figure out the algorithm allowing goal to be distributed across buckets, allowing this condition to be possible solvable
                    else if ((GoalWaterVolume > FirstBucket.Capacity) && (GoalWaterVolume > SecondBucket.Capacity))
                    {
                        _isSolvable = false;
                    }
                    else
                    {
                        // Determine if the goal can ever be met by cycling water through the buckets
                        int maxBucket = Math.Max(FirstBucket.Capacity, SecondBucket.Capacity);
                        int minBucket = Math.Min(FirstBucket.Capacity, SecondBucket.Capacity);
                        int capacityRemainder = maxBucket % minBucket;
#region use this
                        int counter = capacityRemainder > 0 ? capacityRemainder : minBucket;
                        int goalRemainder = GoalWaterVolume % counter;
                        if (goalRemainder > 0)
                        {
                            _isSolvable = false;
                        }
#endregion
#region test with this
                        //int goalRemainder = GoalWaterVolume % minBucket;
                        //if ((capacityRemainder == 0) && (goalRemainder != 0))
                        //{
                        //    _isSolvable = false;
                        //}
#endregion
                        else
                        {
                            _isSolvable = true;
                        }
                    }
                }
                return (bool)_isSolvable;
            }
        }
    }

    [Serializable]
    public class ProblemState : IBinaryConvertible, IFrameable
    {
        public IEnumerable<Bucket> BucketState { get; private set; }

        public ReservoireState SourceState { get; private set; }

        public ProblemState() { }

        public ProblemState(Reservoire source, params Bucket[] buckets)
        {
            BucketState = buckets.Select(b => b.Clone() as Bucket).ToList();
            SourceState = source.CurrentState;
        }

        public bool SameBucketState(ProblemState other)
        {
            var states = (from b in BucketState
                          join bo in other.BucketState on new { b.Name, b.Capacity } equals new { bo.Name, bo.Capacity } into otherStates
                          from o in otherStates.DefaultIfEmpty()
                          select new { mine = b, theirs = o })
                         .Union(
                          from o in other.BucketState
                          join bm in BucketState on new { o.Name, o.Capacity } equals new { bm.Name, bm.Capacity } into myStates
                          from b in myStates.DefaultIfEmpty()
                          select new { mine = b, theirs = o }
                         );
            foreach (var compare in states)
            {
                if ((compare.mine == null) || (compare.theirs == null))
                    return false;
                if (compare.mine.CurrentFill != compare.theirs.CurrentFill)
                    return false;
            }
            return true;
        }

        public byte[] GetBytes(Encoding textEncoding = null)
        {
            textEncoding = textEncoding ?? Encoding.UTF8;
            // Need a better way than this
            List<byte[]> data = new List<byte[]>();
            byte[] temp = new byte[4];
            int updatedOffset = 0;
            Buffer.BlockCopy(BitConverter.GetBytes(BucketState.Count()), 0, temp, 0, 4);
            data.Add(temp);
            updatedOffset += 4;
            foreach (var bucket in BucketState)
            {
                temp = bucket.GetBytes(textEncoding);
                data.Add(temp);
                updatedOffset += temp.Length;
            }
            temp = SourceState.GetBytes(textEncoding);
            data.Add(temp);
            updatedOffset += temp.Length;
            byte[] returnBytes = new byte[updatedOffset];
            updatedOffset = 0;
            foreach (var d in data)
            {
                Buffer.BlockCopy(d, 0, returnBytes, updatedOffset, d.Length);
                updatedOffset += d.Length;
            }
            return returnBytes;
        }

        public int OntoBuffer(byte[] buffer, int offset = 0, Encoding textEncoding = null)
        {
            textEncoding = textEncoding ?? Encoding.UTF8;
            int updatedOffset = offset;
            Buffer.BlockCopy(BitConverter.GetBytes(BucketState.Count()), 0, buffer, updatedOffset, 4);
            updatedOffset += 4;
            foreach (var bucket in BucketState)
            {
                updatedOffset = bucket.OntoBuffer(buffer, updatedOffset, textEncoding);
            }
            return SourceState.OntoBuffer(buffer, updatedOffset, textEncoding);
        }

        public static ProblemState GetFromBytes(byte[] data, int offset = 0, Encoding textEncoding = null)
        {
            ProblemState ps = new ProblemState();
            ps.FromBytes(data, offset, textEncoding);
            return ps;
        }

        public static ProblemState GetFromBytes(byte[] data, ref int offset, Encoding textEncoding = null)
        {
            ProblemState ps = new ProblemState();
            offset = ps.FromBytes(data, offset, textEncoding);
            return ps;
        }

        public int FromBytes(byte[] data, int offset = 0, Encoding textEncoding = null)
        {
            int updatedOffset = offset;
            textEncoding = textEncoding ?? Encoding.UTF8;
            int numBuckets = BitConverter.ToInt32(data, updatedOffset);
            updatedOffset += 4;
            
            var buckets = new List<Bucket>(numBuckets);
            for (int b = 0; b < numBuckets; b++)
            {
                buckets.Add(Bucket.GetFromBytes(data, ref updatedOffset, textEncoding));
            }
            BucketState = buckets;
            SourceState = ReservoireState.GetFromBytes(data, ref updatedOffset);
            return updatedOffset;
        }

        public IEnumerable<Frame> GetFrames(Encoding textEncoding = null)
        {
            textEncoding = textEncoding ?? Encoding.UTF8;
            yield return this.GetFramePart(this.BucketState.Count());
            foreach (var bs in this.BucketState)
                foreach (var f in this.GetFrameParts(bs, textEncoding))
                    yield return f;
            foreach (var f in this.GetFrameParts(SourceState, textEncoding))
                yield return f;
        }

        public int FromFrames(IEnumerable<Frame> frames, int offsetFrame = 0, Encoding textEncoding = null)
        {
            textEncoding = textEncoding ?? Encoding.UTF8;
            int numBuckets = 0;
            int updatedOffset = this.FromFramePart(frames, i => numBuckets = i, offsetFrame);
            List<Bucket> buckets = new List<Bucket>(numBuckets);
            for (int b = 0; b < numBuckets; b++)
            {
                //Bucket bucket = new Bucket();
                //updatedOffset = bucket.FromFrames(frames, updatedOffset, textEncoding);
                //buckets.Add(bucket);
                updatedOffset = this.FromFrameParts<ProblemState, Bucket>(frames, (ps, bucket) => buckets.Add(bucket), updatedOffset, textEncoding);
            }
            BucketState = buckets;
            updatedOffset = this.FromFrameParts<ProblemState, ReservoireState>(frames, (ps, rs) => ps.SourceState = rs, updatedOffset, textEncoding);
            return updatedOffset;
        }
    }
}