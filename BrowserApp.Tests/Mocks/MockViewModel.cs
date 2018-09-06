using BrowserApp.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using static BrowserApp.Tests.Extensions;

namespace BrowserApp.Tests.Mocks
{
    class MockViewModel : IAppViewModel
    {
        private string _prop;
        public event PropertyChangedEventHandler PropertyChanged;
        public string Prop
        {
            get { return _prop; }
            set
            {
                this._prop = value;
                Invoke("Prop");
            }
        }
        public void Invoke(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        CommandManager IAppViewModel.CommandManager { get; } = new CommandManager();
    }
}
