using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WaterBucketWeb.Models
{
    public class WorkOrder
    {
        public bool YieldWeb { get; set; }

        public bool UseWorker { get; set; }

        public bool YieldWorker { get; set; }

        public bool ObserverOnZmq { get; set; }

        public int StartDelay { get; set; }

        public int WorkDelay { get; set; }
    }
}