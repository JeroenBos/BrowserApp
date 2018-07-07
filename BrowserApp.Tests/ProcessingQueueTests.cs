using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JBSnorro.Extensions;
using JBSnorro;
using System.Linq;
using static BrowserApp.Tests.Extensions;

namespace BrowserApp.Tests
{
    [TestClass]
    public class ProcessingQueueTests
    {
        [TestMethod]
        public async Task AddedItemCanBeDequeued()
        {
            const int item = 0;
            ProcessingQueue<int> queue = new ProcessingQueue<int>(() => { });
            queue.Enqueue(item);
            await DelayDelta();

            bool dequeued = queue.TryDequeue(out int dequeuedItem);

            Assert.IsTrue(dequeued);
            Assert.AreEqual(item, dequeuedItem);
        }
        [TestMethod]
        public async Task DequeuedItemIsStillIncludedInCount()
        {
            const int item = 0;
            ProcessingQueue<int> queue = new ProcessingQueue<int>(() => { });
            queue.Enqueue(item);
            await DelayDelta();

            bool dequeued = queue.TryDequeue(out int dequeuedItem);

            Assert.AreEqual(1, queue.Count);
        }
        [TestMethod]
        public async Task DequeuedItemIsInProcessingList()
        {
            const int item = 0;
            ProcessingQueue<int> queue = new ProcessingQueue<int>(() => { });
            queue.Enqueue(item);
            await DelayDelta();
            queue.TryDequeue(out int dequeuedItem);
            queue.OnProcessed(dequeuedItem);

            Assert.IsTrue(queue.CurrentlyProcessingTasks.Contains(dequeuedItem));
        }
        [TestMethod]
        public async Task DequeuedItemCanBeRemoved()
        {
            const int item = 0;
            ProcessingQueue<int> queue = new ProcessingQueue<int>(() => { });
            queue.Enqueue(item);
            await DelayDelta();
            queue.TryDequeue(out int dequeuedItem);

            // assertion is that the following does not throw
            queue.OnProcessed(dequeuedItem);

            Assert.AreEqual(0, queue.Count);
        }

        [TestMethod, ExpectedException(typeof(InvalidOperationException))]
        public async Task NeverDequeuedItemCannotBeRemoved()
        {
            const int item = 0;
            const int otherItem = 1;
            ProcessingQueue<int> queue = new ProcessingQueue<int>(() => { });
            queue.Enqueue(item);
            await DelayDelta();

            // should throw because otherItem is not being processed yet
            queue.OnProcessed(otherItem);
        }
        [TestMethod, ExpectedException(typeof(InvalidOperationException))]
        public async Task NotDequeuedItemCannotBeRemoved()
        {
            const int item = 0;
            const int otherItem = 1;
            ProcessingQueue<int> queue = new ProcessingQueue<int>(() => { });
            queue.Enqueue(item);
            await DelayDelta();
            queue.TryDequeue(out int dequeuedItem);

            // should throw because otherItem is not in the processingQueue
            queue.OnProcessed(otherItem);
        }

        [TestMethod]
        public async Task AllTasksAreDequeuedOnce()
        {
            const int count = 5000;
            const int workerCount = 10;
            ProcessingQueue<int> queue = new ProcessingQueue<int>(() => { });
            foreach (int task in Enumerable.Range(0, count))
            {
                queue.Enqueue(task);
            }


            var tasks = Enumerable.Range(0, workerCount)
                .Select(workerIndex => Task.Run(() =>
                {
                    var dequeuedItems = new List<int>();
                    while (queue.TryDequeue(out int item))
                    {
                        dequeuedItems.Add(item);
                    }
                    return dequeuedItems;
                }))
                .ToList();
            await Task.WhenAll(tasks);

            var countsPerThread = tasks.Select(t => t.Result.Count).ToList(); // just verify it does not have an extremely peaked distribution
            var allDequeuedItems = tasks.Select(t => t.Result)
                                        .Concat()
                                        .Distinct()
                                        .ToList();

            Assert.AreEqual(count, allDequeuedItems.Count);

        }
    }
}
