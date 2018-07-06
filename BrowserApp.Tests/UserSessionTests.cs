using BrowserApp.POCOs;
using JBSnorro.Diagnostics;
using JBSnorro.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BrowserApp.Tests
{
    [TestClass]
    public class UserSessionTests
    {
        static UserSessionTests()
        {
            logger = new Logger();
            loggerPipe = new LoggerConsolePipe(logger);
        }
        private static readonly Logger logger;
        private static readonly LoggerConsolePipe loggerPipe;

        /// <summary>
        /// Returns a task that wait long enough to ensure that any task processing has been handled.
        /// </summary>
        private static Task DelayDelta()
        {
            return Task.Delay(10);
        }
        [TestMethod]
        public void CleanUserSessionFlushReturnsNothing()
        {
            var userSession = new UserSession(new MockViewModel(),logger);

            Assert.AreEqual(0, userSession.Flush().Changes.Length);
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
    }

    class MockViewModel : INotifyPropertyChanged
    {
        public static void ToConsole(this ILogger logger)
        {
            new LoggerConsolePipe(logger);
        }
    }
}
