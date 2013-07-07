using System;
using System.Collections.Generic;
using Cloo;
using Clpp.Core.Utilities;

namespace Clpp.Core.Scan
{
    public class ClppScanDefault : ClppScan
    {
        private readonly ComputeProgram _kernelProgram;
        private ComputeKernel _kernelScan;
        private ComputeKernel _kernelUniformAdd;
        private long[] _blockSumsSizes;
        private ComputeMemory[] _clBufferBlockSums;
        private string _kernelSource;
        private int _pass;

        public ClppScanDefault(ClppContext clppContext, long valueSize, long maxElements) : base(clppContext, valueSize, maxElements)
        {
            var source = EmbeddedResourceUtilities.ReadEmbeddedStream("Clpp.Core.Scan.clppScanDefault.cl");

            _kernelSource = PreProcess(source);
            _kernelProgram = new ComputeProgram(clppContext.Context, _kernelSource);

            const string buildOptions = "";

            _kernelProgram.Build(new List<ComputeDevice>
                                 {
                                     _clppContext.Device
                                 },
                                 buildOptions,
                                 null,
                                 IntPtr.Zero);

            _kernelScan = _kernelProgram.CreateKernel("kernel__ExclusivePrefixScan");

            _kernelUniformAdd = _kernelProgram.CreateKernel("kernel__UniformAdd");

            // Get the workgroup size
            _workGroupSize = (uint) _kernelScan.GetWorkGroupSize(_clppContext.Device);

            // Prepare all the buffers
            AllocateBlockSums(maxElements);
        }

        ~ClppScanDefault()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                //release managed resources
                if (_isClBuffersOwner)
                {
                    DisposeHelper.Dispose(ref _clBufferValues);
                }

                DisposeHelper.Dispose(ref _kernelScan);
                DisposeHelper.Dispose(ref _kernelUniformAdd);

                FreeBlockSums();
            }
            //release unmanaged resources

            base.Dispose(disposing);
        }

        public override void PopDatas()
        {
            _clppContext.CommandQueue.Read(_clBufferValues, true, 0, _valueSize*_dataSetSize, _values, null);
        }

        public override void PopDatas(IntPtr outBuffer, long sizeBytes)
        {
            _clppContext.CommandQueue.Read(_clBufferValues, true, 0, _valueSize * sizeBytes, outBuffer, null);
        }

        public override void PushCLDatas(ComputeBuffer<byte> clBufferValues)
        {
            _values = IntPtr.Zero;
            _clBufferValues = clBufferValues;
            var recompute = clBufferValues.Size != _dataSetSize;
            _dataSetSize = clBufferValues.Size;

            //---- Compute the size of the different block we can use for '_datasetSize' (can be < maxElements)
            // Compute the number of levels requested to do the scan
            if (recompute)
            {
                _pass = 0;
                var n = _dataSetSize;
                do
                {
                    n = (n + _workGroupSize - 1)/_workGroupSize; // round up
                    _pass++;
                } while (n > 1);

                // Compute the block-sum sizes
                n = _dataSetSize;
                for (uint i = 0; i < _pass; i++)
                {
                    _blockSumsSizes[i] = n;
                    n = (n + _workGroupSize - 1)/_workGroupSize; // round up
                }
                _blockSumsSizes[_pass] = n;
            }

            _isClBuffersOwner = false;
        }

        public override void PushDatas(IntPtr inBuffer, long sizeBytes)
        {
            //---- Store some values
            _values = inBuffer;
            var reallocate = sizeBytes > _dataSetSize || !_isClBuffersOwner;
            var recompute = sizeBytes != _dataSetSize;
            _dataSetSize = sizeBytes;

            //---- Compute the size of the different block we can use for '_datasetSize' (can be < maxElements)
            // Compute the number of levels requested to do the scan
            if (recompute)
            {
                _pass = 0;
                var n = _dataSetSize;
                do
                {
                    n = (n + _workGroupSize - 1)/_workGroupSize; // round up
                    _pass++;
                } while (n > 1);

                // Compute the block-sum sizes
                n = _dataSetSize;
                for (uint i = 0; i < _pass; i++)
                {
                    _blockSumsSizes[i] = n;
                    n = (n + _workGroupSize - 1)/_workGroupSize; // round up
                }
                _blockSumsSizes[_pass] = n;
            }

            //---- Copy on the device
            if (reallocate)
            {
                //---- Release
                DisposeHelper.Dispose(ref _clBufferValues);

                //---- Allocate & copy on the device
                _clBufferValues = new ComputeBuffer<byte>(_clppContext.Context,
                                                          ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer,
                                                          _valueSize*_dataSetSize,
                                                          _values);
                _isClBuffersOwner = true;
            }
            else
            {
                // Just resend
                _clppContext.CommandQueue.Write(_clBufferValues, false, 0, _valueSize * _dataSetSize, _values, null);
            }
        }

        public override void Scan()
        {
            _kernelScan.SetLocalArgument(1, _workGroupSize*_valueSize);


            // Apply the scan to each level
            ComputeMemory clValues = _clBufferValues;
            for (uint i = 0; i < _pass; i++)
            {
                var globalWorkSize = new[] {ToMultipleOf(_blockSumsSizes[i]/2, _workGroupSize/2)};
                var localWorkSize = new[] {_workGroupSize/2};

                _kernelScan.SetMemoryArgument(0, clValues);
                _kernelScan.SetMemoryArgument(2, _clBufferBlockSums[i]);
                _kernelScan.SetValueArgument(3, (uint) _blockSumsSizes[i]);

                _clppContext.CommandQueue.Execute(_kernelScan, null, globalWorkSize, localWorkSize, null);

                clValues = _clBufferBlockSums[i];
            }

            // Uniform addition
            for (var i = _pass - 2; i >= 0; i--)
            {
                var globalWorkSize = new[] {ToMultipleOf(_blockSumsSizes[i]/2, _workGroupSize/2)};
                var localWorkSize = new[] {_workGroupSize/2};

                var dest = (i > 0) ? _clBufferBlockSums[i - 1] : _clBufferValues;

                _kernelUniformAdd.SetMemoryArgument(0, dest);
                _kernelUniformAdd.SetMemoryArgument(1, _clBufferBlockSums[i]);
                _kernelUniformAdd.SetValueArgument(2, (int)_blockSumsSizes[i]);

                _clppContext.CommandQueue.Execute(_kernelUniformAdd, null, globalWorkSize, localWorkSize, null);
            }
        }

        private void AllocateBlockSums(long maxElements)
        {
            // Compute the number of buffers we need for the scan
            _pass = 0;
            var n = maxElements;
            do
            {
                n = (n + _workGroupSize - 1)/_workGroupSize; // round up
                _pass++;
            } while (n > 1);

            // Allocate the arrays
            _clBufferBlockSums = new ComputeMemory[_pass];
            _blockSumsSizes = new long[_pass + 1];

            // Create the cl-buffers
            n = maxElements;
            for (uint i = 0; i < _pass; i++)
            {
                _blockSumsSizes[i] = n;

                _clBufferBlockSums[i] = new ComputeBuffer<byte>(_clppContext.Context, ComputeMemoryFlags.ReadWrite, sizeof (int)*n, IntPtr.Zero);

                n = (n + _workGroupSize - 1)/_workGroupSize; // round up
            }
            _blockSumsSizes[_pass] = n;
        }

        private void FreeBlockSums()
        {
            if (_clBufferBlockSums == null)
            {
                return;
            }

            for (uint i = 0; i < _pass; i++)
            {
                _clBufferBlockSums[i].Dispose();
            }

            _clBufferBlockSums = null;
            _blockSumsSizes = null;
        }

        private static long ToMultipleOf(long N, long @base)
        {
            return (long) (Math.Ceiling((double) N/@base)*@base);
        }
    }
}