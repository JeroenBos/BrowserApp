using JBSnorro.Diagnostics;
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

        public event EventHandler CanExecuteChanged
        {
            add { throw new NotSupportedException(); }
            remove { }
        }

        [DebuggerHidden]
        public MockCommand(ILogger logger)
        {
            Contract.Requires(logger != null);

            this.logger = logger;
        }

        [DebuggerHidden]
        public virtual bool CanExecute(MockViewModel viewModel, object parameter) => true;
        public void Execute(MockViewModel viewModel, object parameter)
        {
            logger.LogInfo("MockCommand: executing command");
            DelayDelta().Wait();
            logger.LogInfo("MockCommand: waited delta");
            viewModel.Invoke(nameof(MockViewModel.Prop));
            logger.LogInfo($"MockCommand: invoked property change '{nameof(MockViewModel.Prop)}'");
        }

        [DebuggerHidden]
        bool ICommand.CanExecute(object viewModel, object eventArgs) => CanExecute((MockViewModel)viewModel, eventArgs);
        [DebuggerHidden]
        void ICommand.Execute(object viewModel, object eventArgs) => Execute((MockViewModel)viewModel, eventArgs);
    }
    class UnexecutableMockCommand : MockCommand
    {
        public UnexecutableMockCommand(ILogger logger) : base(logger) { }

        public override bool CanExecute(MockViewModel viewModel, object parameter) => false;
    }
}
