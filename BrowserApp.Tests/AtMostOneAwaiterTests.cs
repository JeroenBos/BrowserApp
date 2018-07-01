using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace BrowserApp.Tests
{
    [TestClass]
    public class AtMostOneAwaiterTests
    {
        /// <summary>
        /// Returns a task that wait long enough to ensure that any task processing has been handled.
        /// </summary>
        private static Task DelayDelta()
        {
            return Task.Delay(10);
        }

        [TestMethod]
        public async Task TaskSucceedsAfterDefaultDuration()
        {
            const int duration = 100;
            AtMostOneAwaiter awaiter = new AtMostOneAwaiter(duration);
            Task wait = awaiter.Wait();

            await DelayDelta();

            Assert.IsFalse(!wait.IsCompleted);

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
        public async Task ResetSucceedsPreviousAwaiter()
        {
            AtMostOneAwaiter awaiter = new AtMostOneAwaiter(100);
            Task wait = awaiter.Wait();

            await DelayDelta();

            awaiter.Reset();

            await DelayDelta();

            Assert.IsTrue(wait.IsCompletedSuccessfully);
        }
    }
}
