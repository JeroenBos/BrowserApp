using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using static BrowserApp.Tests.Extensions;

namespace BrowserApp.Tests.Mocks
{
    class EmptyMockViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }
    }
}
