#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BrowserApp.Tests
{
    class _Startup
    {
        static void Main(string[] args)
        {
            new CommandManagerTests().TestSimpleCommandRegistration();

            Task.Run(() => main(args)).GetAwaiter().GetResult();
        }
        static async Task main(string[] args)
        {
            // await new UserSessionTests().PropertyChangeIsFlushed();
        }
    }
}