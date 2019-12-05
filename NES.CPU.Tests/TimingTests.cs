using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NES.CPU.Tests
{
    public class TimingTests
    {
        long cpuTicks = 0;
        int A = 0;

        [Fact(Skip = "Nope")]
        public void Test1()
        {
            int a = 0;
            for (int i = 0; i < 1790000; i++)
            {
                a++;
            }
        }

        [Fact(Skip = "Nope")]
        public void Test2()
        {
            foreach (var o in ProcessStuffIGuess().Take(1790000)) ;
        }

        [Fact(Skip = "Nope")]
        public void Test3()
        {
            int masterClock = 236_250_000 / 11;
            double cycleDuration = 1.0 / masterClock;

            var cpu = ProcessStuffIGuess().GetEnumerator();
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < masterClock; i++)
            {
                if (i % 12 == 0)
                {
                    cpu.MoveNext();
                    cpuTicks++;
                    var cpuRuntime = TimeSpan.FromSeconds((cpuTicks * 12 * 11) / 236250000.0);
                    TimeSpan tickDrift = cpuRuntime - sw.Elapsed;
                    if (tickDrift > TimeSpan.Zero)
                    {
                        Thread.Sleep(tickDrift);
                    }
                }
            }

            Assert.Equal(1000, sw.ElapsedMilliseconds);
        }

        public IEnumerable<object> ProcessStuffIGuess()
        {
            for (; ; )
            {
                foreach (var o in Add())
                {
                    yield return o;
                }
            }
        }

        private IEnumerable<object> Add()
        {
            int arg = 1;
            yield return null;
            int x = A;
            yield return null;
            x += arg;
            yield return null;
            x += arg;
            yield return null;
            x += arg;
            yield return null;
            x += arg;
            yield return null;
        }
    }
}
