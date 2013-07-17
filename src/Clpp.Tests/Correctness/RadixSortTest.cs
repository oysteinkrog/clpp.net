using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Cloo;
using Clpp.Core;
using Clpp.Core.Sort;
using NUnit.Framework;

namespace Clpp.Tests.Correctness
{
    [TestFixture]
    public class RadixSortTest
    {
        [Test]
        public void SortUint([Values(ComputeDeviceTypes.Gpu)] ComputeDeviceTypes deviceTypes,
                             [Values(1 << 10, 1 << 12, 1 << 13, 1 << 14, 1 << 15, 1 << 16, 1 << 17, 1 << 18, 1 << 19, 1 << 20)] int testDataSize,
            [Values(32)] int bitsToSort)
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

                var sorted = testData.ToArray();
                Array.Sort(sorted);

                CollectionAssert.AreNotEqual(sorted.ToList(), testData.ToList());

                var h = GCHandle.Alloc(testData, GCHandleType.Pinned);
                try
                {
                    using (var sort = new ClppSortRadixSortGPU(clppContext, testDataSize, bitsToSort, true))
                    {
                        var addrOfPinnedObject = h.AddrOfPinnedObject();

                        sort.PushDatas(addrOfPinnedObject, testDataSize);

                        var sw = new Stopwatch();

                        sw.Start();
                        sort.Sort();
                        clppContext.CommandQueue.Finish();
                        sw.Stop();

                        float kps = (1000 / sw.ElapsedMilliseconds) * testDataSize;
                        Console.WriteLine("Sorting {0} {1} values took {2}ms, {2} KPS",
                                          testDataSize,
                                          testData[0].GetType()
                                                     .Name,
                                          sw.Elapsed.TotalMilliseconds,
                                          kps);

                        sort.PopDatas();
                    }
                }
                finally
                {
                    h.Free();
                }

                CollectionAssert.AreEqual(sorted.ToList(), testData.ToList());
            }

            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForFullGCComplete();
        }
    }
}

