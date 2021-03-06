﻿using JBSnorro.Diagnostics;
using JBSnorro.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Input;
using static BrowserApp.Tests.Extensions;

namespace BrowserApp.Tests.Mocks
{
    class MockCommand : ICommand
    {
        private readonly ILogger logger;
        private readonly MockViewModel viewModel;
        [DebuggerHidden]
        public MockCommand(MockViewModel viewModel, ILogger logger)
        {
            Contract.Requires(viewModel != null);
            Contract.Requires(logger != null);
            this.viewModel = viewModel;
            this.logger = logger;
        }
        public event EventHandler CanExecuteChanged
        {
            add { throw new NotSupportedException(); }
            remove { }
        }

        [DebuggerHidden]
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            logger.LogInfo("MockCommand: executing command");
            DelayDelta().Wait();
            viewModel.Invoke(nameof(MockViewModel.Prop));
            logger.LogInfo($"MockCommand: invoked property change '{nameof(MockViewModel.Prop)}'");
        }
    }
}
