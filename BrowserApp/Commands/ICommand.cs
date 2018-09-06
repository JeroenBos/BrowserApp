using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BrowserApp.Commands
{

    public interface ICommand<TViewModel, TEventArgs> : ICommand
    {
        bool AdditionalCanExecute(TViewModel viewModel, TEventArgs eventArgs);
        void Execute(TViewModel viewModel, TEventArgs eventArgs);
    }
    public interface ICommand
    {
        /// <summary>
        /// Represents the conditions under which this command can execute, which are clientside evaluable.
        /// </summary>
        BooleanAST CanExecute { get; }
        /// <summary>
        /// Adds extra checks that cannot be verified clientside.
        /// </summary>
        bool AdditionalCanExecute(object viewModel, object eventArgs);
        void Execute(object viewModel, object eventArgs);
    }
}
