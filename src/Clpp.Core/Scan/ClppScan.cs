using System;
using Cloo;

namespace Clpp.Core.Scan
{
    public abstract class ClppScan : ClppProgram
    {
        protected readonly ClppContext _clppContext;
        protected readonly long _maxElements;
        protected readonly long _valueSize;
        protected ComputeBuffer<byte> _clBufferValues;

        protected long _dataSetSize;
        protected bool _isClBuffersOwner;

        protected IntPtr _values;
        protected long _workGroupSize;

        public ClppScan(ClppContext clppContext, long valueSize, long maxElements) : base(clppContext)
        {
            _clppContext = clppContext;
            _valueSize = valueSize;
            _maxElements = maxElements;
        }

        public static ClppScan CreateBestScan(ClppContext clppContext, long valueSize, long maxElements)
        {
            throw new NotImplementedException();
        }

        public abstract void PopDatas();

        public abstract void PopDatas(IntPtr dataSetPt);
        public abstract void PushCLDatas(ComputeBuffer<byte> computeBuffer, long datasetSize);
        public abstract void PushDatas(IntPtr values, long datasetSize);
        public abstract void Scan();
    }
}