using JBSnorro.View.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using static JBSnorro.View.Tests.Extensions;

namespace JBSnorro.View.Tests.Mocks
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
