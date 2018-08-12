using JBSnorro;
using JBSnorro.Diagnostics;
using System;
using System.ComponentModel;

namespace BrowserApp
{
    public class SpecificCode
    {
        public static void Initialize(CommandManager manager)
        {
            manager.Add(IncrementCounterCommand.Singleton);
        }

    }
    public class IncrementCounterCommand : ICommand<CounterViewModel, object>
    {
        public static readonly IncrementCounterCommand Singleton = new IncrementCounterCommand();
        public bool CanExecute(CounterViewModel viewModel, object eventArgs)
        {
            Contract.Requires(viewModel != null);
            return true;
        }
        public void Execute(CounterViewModel viewModel, object eventArgs)
        {
            Contract.Requires(viewModel != null);
            viewModel.Increment();
        }

        void ICommand.Execute(object viewModel, object eventArgs) => Execute((CounterViewModel)viewModel, eventArgs);
        bool ICommand.CanExecute(object viewModel, object eventArgs) => CanExecute((CounterViewModel)viewModel, eventArgs);
    }
    public class CounterViewModel : DefaultINotifyPropertyChanged
    {
        private int _currentCount;
        public int CurrentCount
        {
            get { return _currentCount; }
            set { this.Set(ref _currentCount, value); }
        }

        public void Increment()
        {
            this.CurrentCount++;
        }
    }
}