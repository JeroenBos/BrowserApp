using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BrowserApp.Tests
{
    class _Startup
    {
        public static async Task Main(string[] args)
        {
            await new UserSessionTests().SecondaryFlushWaitCompletesFirst();
        }
    }
}
