using JBSnorro.Diagnostics;
using System;
using System.Threading.Tasks;

namespace JBSnorro.View
{
    /// <summary>
    /// This class collects multiple waiters, ensuring only one is waiting at the same time, waiting for longer and longer times.
    /// If a new waiter presents itself, the previous task is given a result.
    /// </summary>
    public class AtMostOneAwaiter
    {
        private readonly object _lock = new object();
        /// <summary>
        /// If the first argument is null, that means 
        /// </summary>
        private readonly Func<int?, int> getNewWaitDuration;
        private TaskCompletionSource<object> tcs;
        private int currentDuration;

        public AtMostOneAwaiter(Func<int?, int> getNewWaitDuration)
        {
            Contract.Requires(getNewWaitDuration != null);

            this.getNewWaitDuration = getNewWaitDuration;
            this.currentDuration = getNewWaitDuration(null);
        }
        public AtMostOneAwaiter(int defaultDuration, float multiplier = 2, int? maxDuration = null)
            : this(getNewWaitDurationFunction(defaultDuration, multiplier, maxDuration))
        {
        }
        private static Func<int?, int> getNewWaitDurationFunction(int defaultDuration, float incrementMultiplier, int? maxDuration)
        {
            Contract.Requires(defaultDuration > 0, "The default duration must be positive. ");
            Contract.Requires(incrementMultiplier >= 1, "The increment multipier must be at least 1. ");
            Contract.Requires(maxDuration == null || maxDuration >= defaultDuration, $"'{nameof(maxDuration)}' cannot be smaller than the default duration");

            return result;
            int result(int? previousWaitingDuration)
            {
                if (previousWaitingDuration == null)
                {
                    return defaultDuration;
                }
                int newValue = (int)Math.Round(previousWaitingDuration.Value * incrementMultiplier);
                return Math.Min(maxDuration ?? Int32.MaxValue, newValue);
            }
        }

        /// <summary>
        /// Waits for a predetermined duration, or until another waiter presents herself.
        /// </summary>
        /// <returns></returns>
        public Task Wait()
        {
            int waitDuration;
            lock (_lock)
            {
                bool somebodyWasWaiting = this.tcs != null;
                if (somebodyWasWaiting)
                {
                    Pulse();
                    Reset();
                    waitDuration = this.currentDuration;
                }
                else
                {
                    waitDuration = this.currentDuration;
                    int nextWaitDuration = getNewWaitDuration(this.currentDuration);
                    this.currentDuration = nextWaitDuration;
                }

                this.tcs = new TaskCompletionSource<object>();
            }

            Console.WriteLine("waiting for " + waitDuration);
            Task wait = Task.Delay(waitDuration);
            Task task = this.tcs.Task;
            return Task.WhenAny(task, wait)
                .ContinueWith(whenAnyTask =>
                {
                    lock (_lock)
                    {
                        if (whenAnyTask.Result == wait && this.tcs.Task == task)
                        {
                            this.tcs?.SetResult(null);
                            this.tcs = null;
                        }
                    }
                });
        }
        /// <summary>
        /// Resets the duration for which is waited the next time.
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                this.currentDuration = getNewWaitDuration(null);
            }
        }
        /// <summary>
        /// If anybody is waiting, signals that it is finished.
        /// </summary>
        public void Pulse()
        {
            lock (_lock)
            {
                this.tcs?.SetResult(null);
                this.tcs = null;
            }
        }
    }
}