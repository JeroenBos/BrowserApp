using JBSnorro;
using JBSnorro.Diagnostics;
using JBSnorro.View.Commands;
using System;
using System.ComponentModel;
using System.IO;

namespace JBSnorro.View.App
{
    public class SpecificCode
    {
        public static void Initialize(CommandManager manager)
        {
            manager.Add(IncrementCounterCommand.Singleton, "Increment");
        }

        public static SpecificAppViewModel getRoot_TODO_ToBeProvidedByExtension(Stream data)
        {
            var result = new SpecificAppViewModel()
            {
                Counter = new CounterViewModel(),
                CommandManager = new CommandManager()
            };
            Initialize(result.CommandManager);
            return result;
        }

    }
    public class IncrementCounterCommand : ICommand<CounterViewModel, object>
    {
        public static readonly IncrementCounterCommand Singleton = new IncrementCounterCommand();

        public BooleanAST CanExecute => BooleanAST.True;
        public bool AdditionalCanExecute(CounterViewModel viewModel, object eventArgs)
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
        bool ICommand.AdditionalCanExecute(object viewModel, object eventArgs) => AdditionalCanExecute((CounterViewModel)viewModel, eventArgs);
    }
    public class SpecificAppViewModel : DefaultINotifyPropertyChanged, IAppViewModel
    {
        private CounterViewModel _counter;
        public CounterViewModel Counter
        {
            get { return _counter; }
            set { this.Set(ref _counter, value); }
        }
        private CommandManager _commandManager;
        public CommandManager CommandManager
        {
            get { return _commandManager; }
            set { this.Set(ref _commandManager, value); }
        }
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