using JBSnorro.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BrowserApp.Tests
{
    internal static class Extensions
    {
        /// <summary>
        /// Returns a task that wait long enough to ensure that any task processing has been handled.
        /// </summary>
        public static Task DelayDelta()
        {
            return Task.Delay(10);
        }
        /// <summary>
        /// Successively executed the specified number of tasks.
        /// </summary>
        public static async Task<List<T>> RunSuccessively<T>(int count, Func<Task<T>> func)
        {
            List<T> result = new List<T>();
            for (int i = 0; i < count; i++)
            {
                result.Add(await func());
            }
            return result;
        }
    }
}
