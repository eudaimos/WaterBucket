using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using ZeroMQ;

namespace WaterBucket.Domain
{
    public enum ProblemUpdateType : byte
    {
        Initial = 0,
        Action = 1,
        Completion = 2,
        Error = 4
    }

    [Serializable]
    public class ProblemUpdate
    {
        public ProblemUpdateType Type { get; private set; }

        public bool IsInitial { get { return ProblemUpdateType.Initial == Type; } }

        public bool IsAction { get { return ProblemUpdateType.Action == Type; } }

        public bool IsCompletion { get { return ProblemUpdateType.Completion == Type; } }

        public bool IsError { get { return ProblemUpdateType.Error == Type; } }

        protected byte[][] _updateData;

        protected IEnumerable<Frame> _updateFrames;

        protected bool IsFramed { get { return _updateFrames != null; } }

        //public string SolutionString { get; private set; }

        //private Encoding _textEncoding;

        public ProblemUpdate(ProblemUpdateType type, params byte[][] data)
        {
            Type = type;
            _updateData = data;
        }

        public ProblemUpdate(ProblemUpdateType type, IEnumerable<Frame> updateFrames)
        {
            Type = type;
            if (updateFrames.Count() > 0)
            {
                var firstFrame = updateFrames.First();
                if ((firstFrame.BufferSize == 1) && (firstFrame.Buffer[0] == 0x7f))
                    updateFrames = updateFrames.Skip(1).ToList();
            }
            _updateFrames = updateFrames;
        }

        public ProblemUpdate(params byte[][] data)
        {
            if ((data.Length == 0) || (data[0].Length == 0))
                throw new ArgumentNullException("data", "No ProblemUpdateType specified in data passed to ProblemUpdate constructor");

            Type = (ProblemUpdateType)data[0][0];
            _updateData = data.SkipWhile(b => b.Length > 0).Skip(1).ToArray();
        }

        public ProblemUpdate<TUpdateState> IntoType<TUpdateState>() where TUpdateState : ProblemUpdateState, new()
        {
            return new ProblemUpdate<TUpdateState>(this/*, _updateData[0]*/);
        }

        public ProblemUpdate<TUpdateState> IntoFrameableType<TUpdateState>(Encoding textEncoding = null) where TUpdateState : ProblemUpdateState, IFrameable, new()
        {
            return new ProblemUpdate<TUpdateState>(this, _updateFrames, textEncoding);
        }

        public ProblemUpdate(ProblemUpdate update)
            : this(update.Type, update._updateData)
        {
        }

        public TException GetException<TException>() where TException : Exception
        {
            using (MemoryStream ms = new MemoryStream(_updateData[0]))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return (TException)formatter.Deserialize(ms);
            }
        }

        //public T GetData<T>() where T : ProblemActionState
        //{
        //    // TODO: Test for inheritance as well: contravariance/covariance
        //    if (typeof(T).Equals(typeof(TActionState)))
        //    {
        //        throw new ArgumentException("Cannot get data with type '" + typeof(T).Name + "' from ProblemUpdate<'" + typeof(TActionState).Name + "'>");
        //    }
        //    return _actionState as T;
        //}

        //public byte[] ToMessage()
        //{
        //    byte[] msgPrefix = _textEncoding.GetBytes(SolutionString);
        //    byte actionState = (byte)Type;

        //}

        //public static ProblemUpdate FromMessage(params byte[] messageData)
        //{

        //    MemoryStream ms = new MemoryStream(messageData);
        //    BinaryFormatter formatter = new BinaryFormatter();
        //    return (ProblemUpdate<TActionState>)formatter.Deserialize(ms);
        //}
    }

    [Serializable]
    public class ProblemUpdate<TUpdateState> : ProblemUpdate where TUpdateState : ProblemUpdateState, new()
    {
        //public string SolutionString { get; private set; }

        private Encoding _textEncoding;

        //private byte[] _updateData;

        [NonSerialized]
        private TUpdateState _updateState = null;
        public TUpdateState UpdateState
        {
            get
            {
                if (_updateState == null)
                {
                    if (IsFramed)
                    {
                        if (_updateFrames.Count() == 0)
                            return null;

                        _updateState = new TUpdateState();
                        _updateState.FromFrames(_updateFrames, 0, _textEncoding);
                    }
                    else
                    {
                        if ((_updateData == null) || (_updateData.Length == 0) || (_updateData[0].Length == 0))
                            return null;

                        _updateState = new TUpdateState();
                        _updateState.FromBytes(_updateData[0]);
                        //using (MemoryStream ms = new MemoryStream(_updateData[0]))
                        //{
                        //    BinaryFormatter formatter = new BinaryFormatter();
                        //    _updateState = (TUpdateState)formatter.Deserialize(ms);
                        //}
                    }
                }
                return _updateState;
            }
        }

        public ProblemUpdate(ProblemUpdate update, /*byte[] actionStateData,*/ Encoding textEncoding = null)
            : base(update)
        {
            //SolutionString = solutionString;
            _textEncoding = textEncoding ?? Encoding.UTF8;
            //_updateData = actionStateData;
        }

        public ProblemUpdate(ProblemUpdate update, IEnumerable<Frame> framedUpdate, Encoding textEncoding = null)
            : base(update.Type, framedUpdate)
        {
            _textEncoding = _textEncoding ?? Encoding.UTF8;
        }

        public static ProblemUpdate<TUpdateState> DefineType(ProblemUpdate update, /* byte[] data,*/ Encoding textEncoding = null)
        {
            return new ProblemUpdate<TUpdateState>(update, /*data,*/ textEncoding);
        }

        //public T GetData<T>() where T : ProblemActionState
        //{
        //    // TODO: Test for inheritance as well: contravariance/covariance
        //    if (typeof(T).Equals(typeof(TActionState)))
        //    {
        //        throw new ArgumentException("Cannot get data with type '" + typeof(T).Name + "' from ProblemUpdate<'" + typeof(TActionState).Name + "'>");
        //    }
        //    return _actionState as T;
        //}

        //public byte[] ToMessage()
        //{
        //    byte[] msgPrefix = _textEncoding.GetBytes(SolutionString);
        //    byte actionState = (byte)Type;

        //}

        public static ProblemUpdate<TUpdateState> FromMessage(params byte[] messageData)
        {
            MemoryStream ms = new MemoryStream(messageData);
            BinaryFormatter formatter = new BinaryFormatter();
            return (ProblemUpdate<TUpdateState>)formatter.Deserialize(ms);
        }
    }

    [Serializable]
    public class SignedProblemUpdate : ProblemUpdate
    {
        public string Signature { get; private set; }

        public SignedProblemUpdate(params byte[][] data)
            : base(data.Skip(1).ToArray())
        {
            Signature = Encoding.UTF8.GetString(data[0]);
        }
    }
}
