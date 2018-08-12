using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Windows.Input;
using BrowserApp.POCOs;
using JBSnorro;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using JetBrains.Annotations;

namespace BrowserApp
{
    public interface ICommand<TViewModel, TEventArgs> : ICommand
    {
        bool CanExecute(TViewModel viewModel, TEventArgs eventArgs);
        void Execute(TViewModel viewModel, TEventArgs eventArgs);
    }
    public interface ICommand
    {
        bool CanExecute(object viewModel, object eventArgs);
        void Execute(object viewModel, object eventArgs);
    }
    public sealed class CommandManager : INotifyCollectionChanged, IEnumerable
    {
        private readonly ObservableCollection<CommandViewModel> _commands;
        private readonly ReadOnlyObservableCollection<CommandViewModel> commands;
        public int Count => commands.Count;

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add => ((INotifyCollectionChanged)commands).CollectionChanged += value;
            remove => ((INotifyCollectionChanged)commands).CollectionChanged -= value;
        }


        internal CommandManager()
        {
            this._commands = new ObservableCollection<CommandViewModel>();
            this.commands = new ReadOnlyObservableCollection<CommandViewModel>(_commands);
        }

        public int Add<TViewModel, TEventArgs>(ICommand<TViewModel, TEventArgs> command)
        {
            return Add(command, typeof(TViewModel));
        }
        public int Add<TViewModel, TEventArgs>(ICommand<TViewModel, TEventArgs> command, Func<ClaimsPrincipal, bool> isAuthorized)
        {
            return Add(command, isAuthorized, typeof(TViewModel));
        }
        public int Add(ICommand command, params Type[] viewModelTypes)
        {
            return Add(command, _ => true, viewModelTypes);
        }
        /// <summary>
        /// Registers the specified command to be applicable on the specified viewmodel types.
        /// </summary>
        /// <returns> The id of the command to be used when calling. </returns>
        public int Add(ICommand command, Func<ClaimsPrincipal, bool> isAuthorized, params Type[] viewModelTypes)
        {
            Contract.Requires(command != null);
            Contract.Requires(isAuthorized != null);
            Contract.Requires(viewModelTypes != null);
            Contract.Requires(viewModelTypes.Length != 0);
            Contract.Requires(viewModelTypes.AreUnique());

            var alreadyRegisteredCommandId = commands.IndexOf(item => item.Command == command);
            if (alreadyRegisteredCommandId != -1)
            {
                commands[alreadyRegisteredCommandId].AddRange(viewModelTypes);
                return alreadyRegisteredCommandId;
            }
            else
            {
                this._commands.Add(new CommandViewModel(this, command, isAuthorized, viewModelTypes));
                return commands.Count - 1;
            }
        }
        internal bool CanExecute(ClaimsPrincipal user, int commandId, object viewModel, object eventArgs)
        {
            Contract.Requires<ArgumentNullException>(viewModel != null, nameof(viewModel));
            Contract.Requires<ArgumentNullException>(eventArgs != null, nameof(eventArgs));
            Contract.Requires(commandId >= 0, $"'{nameof(commandId)}' must be nonnegative");

            if (commandId >= this.commands.Count)
            {
                return false;
            }
            return commands[commandId].CanExecute(user, viewModel, eventArgs);
        }
        internal void Execute(ClaimsPrincipal user, int commandId, object viewModel, object eventArgs)
        {
            Contract.Requires(CanExecute(user, commandId, viewModel, eventArgs));

            commands[commandId].Execute(user, viewModel, eventArgs);
        }

        public bool IsAuthorized(ClaimsPrincipal user, int commandId)
        {
            Contract.Requires(commandId >= 0);
            Contract.Requires(Exists(commandId));

            return this.commands[commandId].IsAuthorized(user);
        }
        public bool Exists(int commandId)
        {
            Contract.Requires(commandId >= 0);

            return commandId < this.Count;
        }
        internal int GetIdOf(CommandViewModel command)
        {
            Contract.Requires(command != null);
            Contract.Requires(this.commands.Contains(command));

            return this.commands.IndexOf(command);
        }
        internal static bool AreCompatible(Type actualViewModelType, Type expectedViewModelType)
        {
            return actualViewModelType == expectedViewModelType
                || (expectedViewModelType.IsClass && actualViewModelType.IsSubclassOf(expectedViewModelType))
                || (expectedViewModelType.IsInterface && actualViewModelType.Implements(expectedViewModelType));
        }
        internal int[] GetViewModelTypeIds(IReadOnlyList<Type> viewModelTypes)
        {
            return null;
        }

        IEnumerator IEnumerable.GetEnumerator() => this.commands.GetEnumerator();
    }

    internal sealed class CommandViewModel : INotifyPropertyChanged
    {
        private readonly CommandManager manager;
        private readonly List<Type> _viewModelTypes;
        private readonly Func<ClaimsPrincipal, bool> isAuthorized;

        public event PropertyChangedEventHandler PropertyChanged;
        [NoViewBinding]
        public ICommand Command { get; }
        [NoViewBinding]
        public IReadOnlyList<Type> ViewModelTypes { get; }
        [UsedImplicitly]
        public int[] ViewModelTypeIds
        {
            get => manager.GetViewModelTypeIds(this.ViewModelTypes);
        }
        [UsedImplicitly]
        public int CommandId => manager.GetIdOf(this);

        internal CommandViewModel(CommandManager manager,
                                  ICommand command,
                                  Func<ClaimsPrincipal, bool> isAuthorized,
                                  IList<Type> viewModelTypes)
        {
            Contract.Requires(manager != null);
            Contract.Requires(command != null);
            Contract.Requires(isAuthorized != null);
            Contract.Requires(viewModelTypes != null);
            Contract.Requires(viewModelTypes.Count != 0);

            this.manager = manager;
            this.Command = command;
            this.isAuthorized = isAuthorized;
            this._viewModelTypes = viewModelTypes.ToList();
            this.ViewModelTypes = new ReadOnlyCollection<Type>(this._viewModelTypes);
        }

        public void AddRange(IEnumerable<Type> viewModelTypes)
        {
            bool changed = false;
            foreach (var viewModelType in viewModelTypes)
            {
                if (!ViewModelTypes.Contains(viewModelType))
                {
                    changed = true;
                    _viewModelTypes.Add(viewModelType);
                }
            }
            if (changed)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ViewModelTypeIds)));
            }
        }

        public bool CanExecute(ClaimsPrincipal user, object viewModel, object eventArgs)
        {
            Contract.Requires(viewModel != null, nameof(viewModel));
            Contract.Requires(eventArgs != null, nameof(eventArgs));

            if (!this.isAuthorized(user))
            {
                return false;
            }

            bool isOfCorrectType = this.ViewModelTypes.Any(t => CommandManager.AreCompatible(viewModel.GetType(), t));
            bool result = isOfCorrectType && this.Command.CanExecute(viewModel, eventArgs);
            return result;
        }
        public void Execute(ClaimsPrincipal user, object viewModel, object eventArgs)
        {
            Contract.Requires(this.CanExecute(user, viewModel, eventArgs));

            if (this.isAuthorized(user))
            {
                this.Command.Execute(viewModel, eventArgs);
            }
        }
        public bool IsAuthorized(ClaimsPrincipal user)
        {
            return this.isAuthorized(user);
        }
    }
}
