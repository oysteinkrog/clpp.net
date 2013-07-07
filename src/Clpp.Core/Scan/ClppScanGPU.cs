﻿using System;
using System.Collections.Generic;
using Cloo;
using Clpp.Core.Utilities;

namespace Clpp.Core.Scan
{
    public class ClppScanGPU : ClppScan
    {
        private ComputeKernel _kernelScan;
        private ComputeProgram _kernelScanProgram;
        private string _kernelSource;

        public ClppScanGPU(ClppContext clppContext, long valueSize, long maxElements)
            : base(clppContext, valueSize, maxElements)
        {
            var source = EmbeddedResourceUtilities.ReadEmbeddedStream("Clpp.Core.Scan.clppScanGPU.cl");

            _kernelSource = PreProcess(source);
            _kernelScanProgram = new ComputeProgram(clppContext.Context, _kernelSource);


#if __APPLE__
    //const char buildOptions = "-DMAC -cl-fast-relaxed-math";
	const string buildOptions = "";
#else
            //const char* buildOptions = "-cl-fast-relaxed-math";
            const string buildOptions = "";
#endif

            _kernelScanProgram.Build(new List<ComputeDevice>
                                     {
                                         _clppContext.Device
                                     },
                                     buildOptions,
                                     null,
                                     IntPtr.Zero);


            _kernelScan = _kernelScanProgram.CreateKernel("kernel__scan_block_anylength");

            //---- Get the workgroup size
            // ATI : Actually the wavefront size is only 64 for the highend cards(48XX, 58XX, 57XX), but 32 for the middleend cards and 16 for the lowend cards.
            // NVidia : 32
            _workGroupSize = (uint) _kernelScan.GetWorkGroupSize(_clppContext.Device);

            _isClBuffersOwner = false;
        }

        ~ClppScanGPU()
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

            _isClBuffersOwner = false;

            _clBufferValues = clBufferValues;
            _dataSetSize = clBufferValues.Size;
        }

        public override void PushDatas(IntPtr inBuffer, long sizeBytes)
        {
            //---- Store some values
            _values = inBuffer;
            var reallocate = sizeBytes > _dataSetSize || !_isClBuffersOwner;
            _dataSetSize = sizeBytes;

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
                _clppContext.CommandQueue.Write(_clBufferValues, false, 0, _valueSize*_dataSetSize, _values, null);
            }
        }

        public override void Scan()
        {
            var blockSize = _dataSetSize/_workGroupSize;
            var B = blockSize*_workGroupSize;
            if ((_dataSetSize%_workGroupSize) > 0)
            {
                blockSize++;
            }

            var localWorkSize = new long[] {_workGroupSize};
            var globalWorkSize = new[] {ToMultipleOf(_dataSetSize/blockSize, _workGroupSize)};

            _kernelScan.SetLocalArgument(0, _workGroupSize*_valueSize);
            _kernelScan.SetMemoryArgument(1, _clBufferValues);
            _kernelScan.SetValueArgument<uint>(2, (uint) B);
            _kernelScan.SetValueArgument<uint>(3, (uint) _dataSetSize);
            _kernelScan.SetValueArgument<uint>(4, (uint) blockSize);

            _clppContext.CommandQueue.Execute(_kernelScan, null, globalWorkSize, localWorkSize, null);

            //            _clppContext.CommandQueue.Wait(ev);
        }

        private static long ToMultipleOf(long N, long @base)
        {
            return (long) (Math.Ceiling((double) N/@base)*@base);
        }
    }
}