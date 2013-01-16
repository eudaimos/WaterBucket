using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaterBucket.Domain
{
    public interface ISolutionStrategy
    {
        string StrategyName { get; }

        string Signature { get; }

        Reservoire WaterSource { get; }

        int WaterGoal { get; }

        BucketActionStep TakeAction();

        bool TestGoal();

        SolutionResult Result { get; }

        ProblemState CurrentProblemState { get; }

        void RemoteResult(SolutionResult result);
    }

    public abstract class BaseSolutionStrategy : ISolutionStrategy
    {
        public string StrategyName { get; private set; }

        public abstract string Signature { get; }

        public Reservoire WaterSource { get; protected set; }

        public int WaterGoal { get; private set; }

        public abstract BucketActionStep TakeAction();

        public abstract bool TestGoal();

        public abstract SolutionResult Result { get; }

        public abstract ProblemState CurrentProblemState { get; }

        public BaseSolutionStrategy(string name, int waterGoal, Reservoire waterSource)
        {
            if (waterSource == null)
            {
                throw new ArgumentNullException("waterSource");
            }
            StrategyName = name;
            WaterGoal = waterGoal;
            WaterSource = waterSource.Clone() as Reservoire;
        }

        public abstract void RemoteResult(SolutionResult result);
    }
}
