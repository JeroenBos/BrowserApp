using System;
using JBSnorro.Diagnostics;
using System.Threading.Tasks;

namespace BrowserApp
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
        private readonly Func<bool> resetQ;
        private TaskCompletionSource<object> tcs;
        private int currentDuration;

        public AtMostOneAwaiter(Func<int?, int> getNewWaitDuration)
        {
            Contract.Requires(getNewWaitDuration != null);

            this.getNewWaitDuration = getNewWaitDuration;
        }
        public AtMostOneAwaiter(int defaultDuration, float multiplier = 2, int? maxDuration =null) 
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
            lock (_lock)
            {
                bool somebodyWasWaiting = this.tcs != null;
                if (somebodyWasWaiting)
                {
                    this.tcs.SetResult(null);
                    this.currentDuration = getNewWaitDuration(null);
                }
                else
                {
                    this.currentDuration = getNewWaitDuration(this.currentDuration);
                }

                this.tcs = new TaskCompletionSource<object>();
            }

            return Task.WhenAny(this.tcs.Task, Task.Delay(this.currentDuration));
        }
        /// <summary>
        /// If anybody is waiting, signals that it is finished, and resets the duration for which is waited.
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                this.currentDuration = getNewWaitDuration(null);
                this.tcs?.SetResult(null);
                this.tcs = null;
            }
        }
    }
}