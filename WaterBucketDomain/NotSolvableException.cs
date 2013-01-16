using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaterBucket.Domain
{
    public class NotSolvableException : Exception
    {
        public NotSolvableException(string message)
            : base(message)
        {
        }
    }
}
