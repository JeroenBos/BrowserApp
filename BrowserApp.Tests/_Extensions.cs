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
        public static void ToConsole(this ILogger logger)
        {
            new LoggerConsolePipe(logger);
        }
    }
}
