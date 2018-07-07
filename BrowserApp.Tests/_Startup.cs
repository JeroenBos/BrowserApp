#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
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
            await new UserSessionTests().PropertyChangeIsFlushed();
        }
    }
}