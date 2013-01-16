using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaterBucket.Domain
{
    public class SingleBucketSolutionStrategy : BaseSolutionStrategy
    {
        private const string SignatureFormat = "{0}|{1}|{2}|{3}";

        private string _signature;
        public override string Signature
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_signature))
                {
                    _signature = string.Format(SignatureFormat, From.Capacity, To.Capacity, WaterGoal, StrategyName);
                }
                return _signature;
            }
        }

        public Bucket From { get; private set; }

        public Bucket To { get; private set; }

        private int _stepsTaken = 0;

        private SolutionResult _result = null;
        public override SolutionResult Result
        {
            get
            {
                if (_result != null)
                    return _result;

                if (TestGoal())
                    _result = new SolutionResult(_stepsTaken, WaterSource);

                return _result;
            }
        }

        private ProblemState _state = null;
        public override ProblemState CurrentProblemState
        {
            get
            {
                if (_state == null)
                {
                    _state = new ProblemState(WaterSource, To, From);
                }
                return _state;
            }
        }

        public SingleBucketSolutionStrategy(string name, Bucket from, Bucket to, int waterGoal, Reservoire waterSource)
            : base(name, waterGoal, waterSource)
        {
            if (from == null)
            {
                throw new ArgumentNullException("from");
            }
            if (to == null)
            {
                throw new ArgumentNullException("to");
            }
            From = from.Clone() as Bucket;
            To = to.Clone() as Bucket;
        }

        public override BucketActionStep TakeAction()
        {
            int amountInAction = 0;
            _state = null;
            _stepsTaken++;
            if (To.IsFull)
            {
                amountInAction = To.Empty(WaterSource);
                return new BucketActionStep(this.StrategyName, To, BucketActions.Empty, _stepsTaken, amountInAction, "Empty " + To.Name + " of " + amountInAction, CurrentProblemState);
            }
            else if (From.IsEmpty)
            {
                amountInAction = From.Fill(WaterSource);
                return new BucketActionStep(this.StrategyName, From, BucketActions.Fill, _stepsTaken, amountInAction, "Fill " + From.Name + " with " + amountInAction, CurrentProblemState);
            }
            amountInAction = From.TransferTo(To);
            return new BucketActionStep(this.StrategyName, From, BucketActions.Transfer, _stepsTaken, amountInAction, "Transfer " + amountInAction + " from " + From.Name + " to " + To.Name, CurrentProblemState);
        }

        public override bool TestGoal()
        {
            return ((To.CurrentFill == WaterGoal) || (From.CurrentFill == WaterGoal));
        }

        public override void RemoteResult(SolutionResult result)
        {
            _result = result;
            _stepsTaken = result.NumberOfActions;
            WaterSource = new Reservoire(result.EndingReservoireState);
        }
    }

    public class BigToSmallSingleBucketSolutionStrategy : SingleBucketSolutionStrategy
    {
        public BigToSmallSingleBucketSolutionStrategy(Problem problem)
            : base("BigToSmall", problem.FirstBucket.Capacity > problem.SecondBucket.Capacity ? problem.FirstBucket : problem.SecondBucket,
                    problem.FirstBucket.Capacity > problem.SecondBucket.Capacity ? problem.SecondBucket : problem.FirstBucket,
                    problem.GoalWaterVolume, problem.WaterSource)
        {
        }
    }

    public class SmallToBigSingleBucketSolutionStrategy : SingleBucketSolutionStrategy
    {
        public SmallToBigSingleBucketSolutionStrategy(Problem problem)
            : base("SmallToBig", problem.FirstBucket.Capacity < problem.SecondBucket.Capacity ? problem.FirstBucket : problem.SecondBucket,
                    problem.FirstBucket.Capacity < problem.SecondBucket.Capacity ? problem.SecondBucket : problem.FirstBucket,
                    problem.GoalWaterVolume, problem.WaterSource)
        {
        }
    }
}
