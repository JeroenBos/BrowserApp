using JBSnorro.Diagnostics;
using JBSnorro.Logging;
using JBSnorro.View.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using static JBSnorro.View.Tests.Extensions;

namespace JBSnorro.View.Tests.Mocks
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

        public BooleanAST CanExecute { get; } = BooleanAST.True;
        [DebuggerHidden]
        public virtual bool AdditionalCanExecute(MockViewModel viewModel, object parameter) => true;
        public void Execute(MockViewModel viewModel, object parameter)
        {
            logger.LogInfo("MockCommand: executing command");
            DelayDelta().Wait();
            logger.LogInfo("MockCommand: waited delta");
            viewModel.Invoke(nameof(MockViewModel.Prop));
            logger.LogInfo($"MockCommand: invoked property change '{nameof(MockViewModel.Prop)}'");
        }

        [DebuggerHidden]
        bool ICommand.AdditionalCanExecute(object viewModel, object eventArgs) => AdditionalCanExecute((MockViewModel)viewModel, eventArgs);
        [DebuggerHidden]
        void ICommand.Execute(object viewModel, object eventArgs) => Execute((MockViewModel)viewModel, eventArgs);
    }
    class UnexecutableMockCommand : MockCommand
    {
        public UnexecutableMockCommand(ILogger logger) : base(logger) { }

        public override bool AdditionalCanExecute(MockViewModel viewModel, object parameter) => false;
    }
}
