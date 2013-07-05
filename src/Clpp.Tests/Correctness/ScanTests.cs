using System;
using System.Runtime.InteropServices;
using Clpp.Core;
using Clpp.Core.Scan;
using NUnit.Framework;

namespace Clpp.Tests.Correctness
{
    [TestFixture]
    public class ScanTests
    {
        [Test]
        public void TestBasicScanDefault([Values(1024)] int testDataSize)
        {
            using (var c = new ClppContext(1, 0))
            {
                c.PrintInformation();

                var testData = new uint[testDataSize];
                var r = new Random();

                for (var i = 0; i < testData.Length; i++)
                {
                    testData[i] = (uint) i;
                    //                    testData[i] = (uint) r.Next();
                }

                var verifyData = new uint[testDataSize];
                for (uint i = 1; i < testDataSize; i++)
                {
                    verifyData[i] = verifyData[i - 1] + testData[i - 1];
                }

                var h = GCHandle.Alloc(testData, GCHandleType.Pinned);
                try
                {
                    using (var scan = new ClppScanDefault(c, sizeof (int), testDataSize))
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

                //---- Check the scan
                for (var i = 0; i < testDataSize; i++)
                {
                    Assert.AreEqual(testData[i], verifyData[i]);
                }
            }
        }

        [Test]
        public void TestBasicScanGPU([Values(1024)] int testDataSize)
        {
            using (var c = new ClppContext(0, 0))
            {
                c.PrintInformation();

                var testData = new uint[testDataSize];
                var r = new Random();

                for (var i = 0; i < testData.Length; i++)
                {
                    testData[i] = (uint) i;
//                    testData[i] = (uint) r.Next();
                }

                var verifyData = new uint[testDataSize];
                for (uint i = 1; i < testDataSize; i++)
                {
                    verifyData[i] = verifyData[i - 1] + testData[i - 1];
                }

                var h = GCHandle.Alloc(testData, GCHandleType.Pinned);
                try
                {
                    using (var scan = new ClppScanGPU(c, sizeof (int), testDataSize))
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

                //---- Check the scan
                for (var i = 0; i < testDataSize; i++)
                {
                    Assert.AreEqual(testData[i], verifyData[i]);
                }
            }
        }
    }
}