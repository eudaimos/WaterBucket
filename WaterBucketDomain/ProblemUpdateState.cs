using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroMQ;

namespace WaterBucket.Domain
{
    [Serializable]
    public abstract class ProblemUpdateState : IBinaryConvertible, IFrameable
    {
        public abstract byte[] GetBytes(Encoding textEncoding = null);

        public abstract int OntoBuffer(byte[] buffer, int offset = 0, Encoding textEncoding = null);

        public abstract int FromBytes(byte[] data, int offset = 0, Encoding textEncoding = null);

        public abstract IEnumerable<Frame> GetFrames(Encoding textEncoding = null);

        public abstract int FromFrames(IEnumerable<Frame> frames, int offsetFrame = 0, Encoding textEncoding = null);
    }
}
