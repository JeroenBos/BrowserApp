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
using BrowserApp.POCOs;

namespace BrowserApp.Tests
{
    [TestClass]
    public class UserSessionTests
    {
        private static object emptyEventArgs => new object();
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
        public async Task NewUserSessionsAddsRootViewModel()
        {
            var userSession = new UserSession(new MockViewModel(), logger);
            userSession.CommandManager.Add(new MockCommand(logger), typeof(object));

            Task task = userSession.ExecuteCommand(0, 0, emptyEventArgs, null);
            await task;

            Assert.IsTrue(task.IsCompletedSuccessfully);
        }
        [TestMethod, ExpectedException(typeof(InvalidOperationException))]
        public async Task ExecutingNonExistingCommandFails()
        {
            var userSession = new UserSession(new MockViewModel(), logger);
            userSession.CommandManager.Add(new MockCommand(logger), typeof(object));

            Task task = userSession.ExecuteCommand(2, 0, emptyEventArgs, null);
            await task;

            Assert.IsTrue(task.IsFaulted);
        }
        [TestMethod, ExpectedException(typeof(InvalidOperationException))]
        public async Task ExecutingCommandOnNonExistingViewModelFails()
        {
            var userSession = new UserSession(new MockViewModel(), logger);
            userSession.CommandManager.Add(new MockCommand(logger), typeof(object));

            await userSession.ExecuteCommand(0, 2, emptyEventArgs, null);
        }
        [TestMethod, ExpectedException(typeof(TaskCanceledException))]
        public async Task ExecutingNonExecutableCommandCancels()
        {
            var userSession = new UserSession(new MockViewModel(), logger);
            userSession.CommandManager.Add(new UnexecutableMockCommand(logger), typeof(object));

            await userSession.ExecuteCommand(0, 0, emptyEventArgs, null);
        }
        [TestMethod]
        public async Task PropertyChangeIsFlushed()
        {
            const int t = 100;
            var viewModel = new MockViewModel();
            var userSession = new UserSession(viewModel, logger, new AtMostOneAwaiter(t * 2));
            userSession.CommandManager.Add(new MockCommand(logger), typeof(object));

            var wait = userSession.FlushOrWait();
            var task = userSession.ExecuteCommand(new CommandInstruction() { CommandId = 0, ViewModelId = 0, EventArgs = emptyEventArgs }, null);

            var result = await wait;
            Assert.IsTrue(task.IsCompleted);
            Assert.AreEqual(1, result.Changes.Length);
        }

    }
}
