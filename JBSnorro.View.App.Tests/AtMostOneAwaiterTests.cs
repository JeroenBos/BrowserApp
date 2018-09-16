using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using static JBSnorro.View.Tests.Extensions;

namespace JBSnorro.View.Tests
{
    [TestClass]
    public class AtMostOneAwaiterTests
    {
        [TestMethod]
        public async Task TaskSucceedsAfterDefaultDuration()
        {
            const int duration = 100;
            AtMostOneAwaiter awaiter = new AtMostOneAwaiter(duration);
            Task wait = awaiter.Wait();

            await DelayDelta();

            Assert.IsFalse(wait.IsCompleted);

            Task firstFinishedTask = await Task.WhenAny(wait, Task.Delay(2 * duration));

            Assert.AreEqual(wait, firstFinishedTask);
        }
        [TestMethod]
        public async Task TasksSucceedsOnNewAwaiter()
        {
            AtMostOneAwaiter awaiter = new AtMostOneAwaiter(100);
            Task firstWait = awaiter.Wait();

            await DelayDelta();

            Task secondWait = awaiter.Wait();

            await DelayDelta();

            Assert.IsTrue(firstWait.IsCompletedSuccessfully);
        }
        [TestMethod]
        public async Task NewAwaiterDoesNotSucceedImmediately()
        {
            AtMostOneAwaiter awaiter = new AtMostOneAwaiter(100);
            Task firstWait = awaiter.Wait();

            await DelayDelta();

            Task secondWait = awaiter.Wait();

            await DelayDelta();

            Assert.IsTrue(!secondWait.IsCompletedSuccessfully);
        }
        [TestMethod]
        public async Task PulseSucceedsPreviousAwaiter()
        {
            AtMostOneAwaiter awaiter = new AtMostOneAwaiter(100);
            Task wait = awaiter.Wait();

            await DelayDelta();

            awaiter.Pulse();

            await DelayDelta();

            Assert.IsTrue(wait.IsCompletedSuccessfully);
        }
        [TestMethod]
        public async Task MultipleWaitAndFlushesWithoutChangesTakeSuccessivelyLonger()
        {
            const int waitsCount = 4;
            const double relativeAllowedError = 0.5;
            const int defaultDuration = 100;
            const float multiplier = 2f;
            AtMostOneAwaiter awaiter = new AtMostOneAwaiter(defaultDuration, multiplier);
            Stopwatch watch = new Stopwatch();
            watch.Start();

            IReadOnlyList<long> durations = await RunSuccessively(waitsCount, async () =>
            {
                await awaiter.Wait();
                long duration = watch.ElapsedMilliseconds;
                watch.Restart();
                return duration;
            });

            for (int i = 0; i < waitsCount; i++)
            {
                double expectedDuration = Math.Pow(multiplier, i) * defaultDuration;
                Assert.AreEqual(expectedDuration, durations[i], durations[i] * relativeAllowedError);
            }
        }
        [TestMethod]
        public async Task MaximumDurationIsSustained()
        {
            const int waitsCount = 4;
            const double relativeAllowedError = 0.5;
            const int defaultDuration = 100;
            const float multiplier = 3f;
            const int maxDuration = 300;
            AtMostOneAwaiter awaiter = new AtMostOneAwaiter(defaultDuration, multiplier, maxDuration);
            Stopwatch watch = new Stopwatch();
            watch.Start();

            IReadOnlyList<long> durations = await RunSuccessively(waitsCount, async () =>
            {
                await awaiter.Wait();
                long duration = watch.ElapsedMilliseconds;
                watch.Restart();
                return duration;
            });

            Assert.AreEqual(defaultDuration, durations[0], durations[0] * relativeAllowedError);
            for (int i = 1; i < waitsCount; i++)
            {
                Assert.AreEqual(300, durations[i], durations[i] * relativeAllowedError);
            }
        }
    }
}
