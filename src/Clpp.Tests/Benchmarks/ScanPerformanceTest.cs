using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Cloo;
using Clpp.Core;
using Clpp.Core.Scan;
using NUnit.Framework;

namespace Clpp.Tests.Benchmarks
{
    public class ScanPerformanceTest
    {
        [Test]
        public void ScanUint(
            [Values(ComputeDeviceTypes.Gpu, ComputeDeviceTypes.Cpu)] ComputeDeviceTypes deviceTypes,
            [Values(100000, 1000000, 10000000, 100000000)] int testDataSize)
        {
            Trace.Close();
            using (var clppContext = new ClppContext(deviceTypes))
            {
                clppContext.PrintInformation();

                var r = new Random();
                var testData = new uint[testDataSize];
                for (var i = 0; i < testData.Length; i++)
                {
                    testData[i] = (uint) r.Next();
                }

                var h = GCHandle.Alloc(testData, GCHandleType.Pinned);
                try
                {
                    using (var scan = ClppScan<uint>.CreateBest(deviceTypes, clppContext, testDataSize))
                    {
                        var addrOfPinnedObject = h.AddrOfPinnedObject();

                        scan.PushDatas(addrOfPinnedObject, testDataSize);

                        var sw = new Stopwatch();

                        sw.Start();
                        scan.Scan();
                        clppContext.CommandQueue.Finish();
                        sw.Stop();
                        Console.WriteLine("Scanning {0} {1} values took {2}ms", testDataSize, testData[0].GetType().Name, sw.Elapsed.TotalMilliseconds);

                        scan.PopDatas();
                    }
                }
                finally
                {
                    h.Free();
                }
            }

            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForFullGCComplete();
        } 
    }
}