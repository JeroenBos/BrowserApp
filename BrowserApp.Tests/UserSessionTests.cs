using JBSnorro.Logging;
using JBSnorro.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using static BrowserApp.Tests.Extensions;
using BrowserApp.Tests.Mocks;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System;

namespace BrowserApp.Tests
{
    [TestClass]
    public class UserSessionTests
    {
        static UserSessionTests()
        {
            logger = new Logger();
            loggerPipe = new LoggerConsolePipe(logger, prependThreadId: true);
        }
        private static readonly Logger logger;
        private static readonly LoggerConsolePipe loggerPipe;

        [TestMethod]
        public void CleanUserSessionFlushWithEmptyViewModelReturnsNothing()
        {
            var userSession = new UserSession(new EmptyMockViewModel(), logger);
            var response = userSession.Flush();

            Assert.AreEqual(0, response.Changes.Length);
        }
        [TestMethod]
        public void CleanUserSessionFlushReturnsDoesNotViewModelState()
        {
            var userSession = new UserSession(new MockViewModel(), logger);
            var response = userSession.Flush();

            Assert.AreEqual(0, response.Changes.Length, "The initial state of the viewmodel was appended as change");
        }

        [TestMethod]
        public async Task CleanUserSessionFlushWaitWaits()
        {
            const int t = 100;
            var userSession = new UserSession(new MockViewModel(), logger, new AtMostOneAwaiter(t * 2));

            Task wait = userSession.FlushOrWait();
            Task delay = Task.Delay(t);
            Task firstCompleted = await Task.WhenAny(wait, delay);

            Assert.AreEqual(delay, firstCompleted);
        }
        [TestMethod]
        public async Task SecondaryFlushWaitCompletesFirst()
        {
            const int t = 100;
            var userSession = new UserSession(new MockViewModel(), logger, new AtMostOneAwaiter(t * 2));

            Task firstWait = userSession.FlushOrWait();
            await Task.Delay(t);
            Task secondWait = userSession.FlushOrWait();
            await DelayDelta();

            Assert.IsTrue(firstWait.IsCompleted);
        }
        [TestMethod]
        public async Task PropertyChangeIsFlushed()
        {
            const int t = 100;
            var viewModel = new MockViewModel();
            var userSession = new UserSession(viewModel, logger, new AtMostOneAwaiter(t * 2));

            var wait = userSession.FlushOrWait();

            userSession.ExecuteCommand(new MockCommand(viewModel, logger));

            var result = await wait;
            Assert.AreEqual(1, result.Changes.Length);
        }
        [TestMethod]
        public async Task ResetSucceedsPreviousAwaiter()
        {
            AtMostOneAwaiter awaiter = new AtMostOneAwaiter(100);
            Task wait = awaiter.Wait();

            await DelayDelta();

            awaiter.Reset();

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
    }
}
