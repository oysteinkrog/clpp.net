using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cloo;

namespace Clpp.Core
{
    public class ClppContext : IDisposable
    {
        private readonly ComputePlatform _platform;

        public ClppContext() : this(0, 0) {}

        public ClppContext(int platformId, int deviceId)
        {
            var platforms = ComputePlatform.Platforms;

            platformId = Math.Min(platformId, platforms.Count);
            _platform = platforms[platformId];

            if (_platform.Vendor.Equals("Intel", StringComparison.OrdinalIgnoreCase))
            {
                Vendor = VendorEnum.Intel;
            }
            else if (_platform.Vendor.Equals("AMD"))
            {
                Vendor = VendorEnum.AMD;
            }
            else if (_platform.Vendor.Equals("Advanced Micro Devices"))
            {
                Vendor = VendorEnum.AMD;
            }
            else if (_platform.Vendor.Equals("NVidia"))
            {
                Vendor = VendorEnum.NVidia;
            }
            else if (_platform.Vendor.Equals("Apple"))
            {
                Vendor = VendorEnum.NVidia;
            }

            Device = _platform.Devices[Math.Min(deviceId, _platform.Devices.Count)];

            ContextPropertyList = new ComputeContextPropertyList(_platform);
            Context = new ComputeContext(new List<ComputeDevice>
                                         {
                                             Device
                                         },
                                         ContextPropertyList,
                                         ErrorHandler,
                                         IntPtr.Zero);

            CommandQueue = new ComputeCommandQueue(Context, Device, ComputeCommandQueueFlags.Profiling);
        }

        ~ClppContext()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // get rid of managed resources
            }
            // get rid of unmanaged resources
            if (CommandQueue != null)
            {
                CommandQueue.Dispose();
                CommandQueue = null;
            }

            if (Context != null)
            {
                Context.Dispose();
                Context = null;
            }
        }

        public ComputeCommandQueue CommandQueue { get; private set; }
        public ComputeContext Context { get; private set; }
        public ComputeContextPropertyList ContextPropertyList { get; private set; }
        public ComputeDevice Device { get; private set; }

        // only if you use unmanaged resources directly in B

        public VendorEnum Vendor { get; private set; }

        public void PrintInformation()
        {
            Console.WriteLine("OpenCL Platform : " + _platform.Name);
            Console.WriteLine("OpenCL Device : " + Device.Name);
        }

        private void ErrorHandler(string errorinfo, IntPtr cldataptr, IntPtr cldatasize, IntPtr userdataptr)
        {
            Debug.WriteLine(errorinfo);
        }
    }

    public enum VendorEnum
    {
        Intel,
        AMD,
        NVidia
    }
}