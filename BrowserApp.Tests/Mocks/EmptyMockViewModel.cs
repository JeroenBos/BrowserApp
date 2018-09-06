using BrowserApp.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using static BrowserApp.Tests.Extensions;

namespace BrowserApp.Tests.Mocks
{
    class EmptyMockViewModel : IAppViewModel
    {
        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }
        CommandManager IAppViewModel.CommandManager { get; } = new CommandManager();
    }
}
