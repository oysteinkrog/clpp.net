using System;
using System.Linq;
using System.Runtime.InteropServices;
using Cloo;
using Clpp.Core;
using Clpp.Core.Scan;
using NUnit.Framework;

namespace Clpp.Tests.Correctness
{
    [TestFixture]
    public class ScanTests
    {
        [Test]
        public void ScanUint([Values(ComputeDeviceTypes.Gpu, ComputeDeviceTypes.Cpu)] ComputeDeviceTypes deviceTypes, [Values(1024*1024*10)] int testDataSize)
        {
            using (var clppContext = new ClppContext(deviceTypes))
            {
                clppContext.PrintInformation();

                var testData = new uint[testDataSize];

                for (var i = 0; i < testData.Length; i++)
                {
                    testData[i] = (uint) i;
                }

                var verifyData = new uint[testDataSize];
                for (uint i = 1; i < testDataSize; i++)
                {
                    verifyData[i] = verifyData[i - 1] + testData[i - 1];
                }

                var h = GCHandle.Alloc(testData, GCHandleType.Pinned);
                try
                {
                    using (var scan = ClppScan<uint>.CreateBest(deviceTypes, clppContext, testDataSize))
                    {
                        var addrOfPinnedObject = h.AddrOfPinnedObject();

                        scan.PushDatas(addrOfPinnedObject, testDataSize);

                        scan.Scan();

                        scan.PopDatas();
                    }
                }
                finally
                {
                    h.Free();
                }

                Assert.AreEqual(verifyData.Last(), testData.Last());
                //---- Check the scan
                for (var i = 0; i < testDataSize; i++)
                {
                    Assert.AreEqual(verifyData[i], testData[i]);
                }
            }

            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForFullGCComplete();
        }

        [Test]
        public void ScanInt([Values(ComputeDeviceTypes.Gpu, ComputeDeviceTypes.Cpu)] ComputeDeviceTypes deviceTypes, [Values(1024 * 1024 * 10)] int testDataSize)
        {
            using (var clppContext = new ClppContext(deviceTypes))
            {
                clppContext.PrintInformation();

                var testData = new int[testDataSize];

                for (var i = 0; i < testData.Length; i++)
                {
                    testData[i] = i;
                }

                var verifyData = new int[testDataSize];
                for (uint i = 1; i < testDataSize; i++)
                {
                    verifyData[i] = verifyData[i - 1] + testData[i - 1];
                }

                var h = GCHandle.Alloc(testData, GCHandleType.Pinned);
                try
                {
                    using (var scan = ClppScan<int>.CreateBest(deviceTypes, clppContext, testDataSize))
                    {
                        var addrOfPinnedObject = h.AddrOfPinnedObject();

                        scan.PushDatas(addrOfPinnedObject, testDataSize);

                        scan.Scan();

                        scan.PopDatas();
                    }
                }
                finally
                {
                    h.Free();
                }

                Assert.AreEqual(verifyData.Last(), testData.Last());
                //---- Check the scan
                for (var i = 0; i < testDataSize; i++)
                {
                    Assert.AreEqual(verifyData[i], testData[i]);
                }
            }

            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForFullGCComplete();
        }

        [Test]
        public void ScanUlong([Values(ComputeDeviceTypes.Gpu, ComputeDeviceTypes.Cpu)] ComputeDeviceTypes deviceTypes, [Values(1024 * 1024 * 10)] int testDataSize)
        {
            using (var clppContext = new ClppContext(deviceTypes))
            {
                clppContext.PrintInformation();

                var testData = new ulong[testDataSize];

                for (var i = 0; i < testData.Length; i++)
                {
                    testData[i] = (ulong) i;
                }

                var verifyData = new ulong[testDataSize];
                for (uint i = 1; i < testDataSize; i++)
                {
                    verifyData[i] = verifyData[i - 1] + testData[i - 1];
                }

                var h = GCHandle.Alloc(testData, GCHandleType.Pinned);
                try
                {
                    using (var scan = ClppScan<ulong>.CreateBest(deviceTypes, clppContext, testDataSize))
                    {
                        var addrOfPinnedObject = h.AddrOfPinnedObject();

                        scan.PushDatas(addrOfPinnedObject, testDataSize);

                        scan.Scan();

                        scan.PopDatas();
                    }
                }
                finally
                {
                    h.Free();
                }

                Assert.AreEqual(verifyData.Last(), testData.Last());
                //---- Check the scan
                for (var i = 0; i < testDataSize; i++)
                {
                    Assert.AreEqual(verifyData[i], testData[i]);
                }
            }

            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForFullGCComplete();
        }


        [Test]
        public void ScanLong([Values(ComputeDeviceTypes.Gpu, ComputeDeviceTypes.Cpu)] ComputeDeviceTypes deviceTypes, [Values(1024 * 1024 * 10)] int testDataSize)
        {
            using (var clppContext = new ClppContext(deviceTypes))
            {
                clppContext.PrintInformation();

                var testData = new long[testDataSize];

                for (var i = 0; i < testData.Length; i++)
                {
                    testData[i] = i;
                }

                var verifyData = new long[testDataSize];
                for (uint i = 1; i < testDataSize; i++)
                {
                    verifyData[i] = verifyData[i - 1] + testData[i - 1];
                }

                var h = GCHandle.Alloc(testData, GCHandleType.Pinned);
                try
                {
                    using (var scan = ClppScan<long>.CreateBest(deviceTypes, clppContext, testDataSize))
                    {
                        var addrOfPinnedObject = h.AddrOfPinnedObject();

                        scan.PushDatas(addrOfPinnedObject, testDataSize);

                        scan.Scan();

                        scan.PopDatas();
                    }
                }
                finally
                {
                    h.Free();
                }

                Assert.AreEqual(verifyData.Last(), testData.Last());
                //---- Check the scan
                for (var i = 0; i < testDataSize; i++)
                {
                    Assert.AreEqual(verifyData[i], testData[i]);
                }
            }

            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForFullGCComplete();
        }

        [Ignore("Disabled due to inaccuracies in results, probably because GPU is less accurate, leading to accumulative floating point errors?")]
        [Test]
        public void ScanFloat([Values(ComputeDeviceTypes.Gpu, ComputeDeviceTypes.Cpu)] ComputeDeviceTypes deviceTypes, [Values(1024*1024*10)] int testDataSize)
        {
            using (var clppContext = new ClppContext(deviceTypes))
            {
                clppContext.PrintInformation();

                var testData = new float[testDataSize];

                for (var i = 0; i < testData.Length; i++)
                {
                    testData[i] = i;
                }

                var verifyData = new float[testDataSize];
                for (uint i = 1; i < testDataSize; i++)
                {
                    verifyData[i] = verifyData[i - 1] + testData[i - 1];
                }

                var h = GCHandle.Alloc(testData, GCHandleType.Pinned);
                try
                {
                    using (var scan = ClppScan<float>.CreateBest(deviceTypes, clppContext, testDataSize))
                    {
                        var addrOfPinnedObject = h.AddrOfPinnedObject();

                        scan.PushDatas(addrOfPinnedObject, testDataSize);

                        scan.Scan();

                        scan.PopDatas(addrOfPinnedObject, testDataSize);
                    }
                }
                finally
                {
                    h.Free();
                }

                Assert.AreEqual(verifyData.Last(), testData.Last());

                //---- Check the scan
                for (var i = 0; i < testDataSize; i++)
                {
                    Assert.AreEqual(verifyData[i], testData[i]);
                }
            }

            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForFullGCComplete();
        }


        [Test]
        public void ScanDouble([Values(ComputeDeviceTypes.Gpu, ComputeDeviceTypes.Cpu)] ComputeDeviceTypes deviceTypes, [Values(1024 * 1024 * 10)] int testDataSize)
        {
            using (var clppContext = new ClppContext(deviceTypes))
            {
                clppContext.PrintInformation();

                var testData = new double[testDataSize];

                for (var i = 0; i < testData.Length; i++)
                {
                    testData[i] = i;
                }

                var verifyData = new double[testDataSize];
                for (uint i = 1; i < testDataSize; i++)
                {
                    verifyData[i] = verifyData[i - 1] + testData[i - 1];
                }

                var h = GCHandle.Alloc(testData, GCHandleType.Pinned);
                try
                {
                    using (var scan = ClppScan<double>.CreateBest(deviceTypes, clppContext, testDataSize))
                    {
                        var addrOfPinnedObject = h.AddrOfPinnedObject();

                        scan.PushDatas(addrOfPinnedObject, testDataSize);

                        scan.Scan();

                        scan.PopDatas();
                    }
                }
                finally
                {
                    h.Free();
                }

                Assert.AreEqual(verifyData.Last(), testData.Last());
                //---- Check the scan
                for (var i = 0; i < testDataSize; i++)
                {
                    Assert.AreEqual(verifyData[i], testData[i]);
                }
            }

            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForFullGCComplete();
        }
    }
}