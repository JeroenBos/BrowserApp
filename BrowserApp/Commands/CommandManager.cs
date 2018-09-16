using BrowserApp.Commands;
using BrowserApp.POCOs;
using JBSnorro;
using JBSnorro.Collections.ObjectModel;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BrowserApp.Commands
{

    class CommandViewModelCollectionAsMapAttribute : ViewBindingAsMapAttribute
    {
        protected override string GetAttributeName(object key, int index) => ((CommandViewModel)key).Name;
    }

    public sealed class CommandManager : DefaultINotifyPropertyChanged, IEnumerable
    {
        private readonly ObservableCollection<CommandViewModel> _commands;
        [CommandViewModelCollectionAsMap]
        public MyReadOnlyObservableCollection<CommandViewModel> Commands { get; }
        [NoViewBinding]
        public int Count => Commands.Count;

        internal CommandManager()
        {
            this._commands = new ObservableCollection<CommandViewModel>();
            this.Commands = new MyReadOnlyObservableCollection<CommandViewModel>(_commands);
        }

        public void Add(ICommand command, string commandName)
        {
            Add(command, commandName, _ => true);
        }
        /// <summary>
        /// Registers the specified command to be applicable on the specified viewmodel types.
        /// </summary>
        /// <returns> The id of the command to be used when calling. </returns>
        public void Add(ICommand command, string commandName, Func<ClaimsPrincipal, bool> isAuthorized)
        {
            Contract.Requires(command != null);
            Contract.Requires(isAuthorized != null);

            var alreadyRegisteredCommandIndex = Commands.IndexOf(item => item.Name == commandName);
            if (alreadyRegisteredCommandIndex != -1)
            {
                this._commands.RemoveAt(alreadyRegisteredCommandIndex);
            }
            this._commands.Add(new CommandViewModel(this, command, commandName, isAuthorized));
        }
        internal bool CanExecute(ClaimsPrincipal user, string commandName, object viewModel, object eventArgs)
        {
            Contract.Requires<ArgumentNullException>(viewModel != null, nameof(viewModel));
            Contract.Requires<ArgumentNullException>(eventArgs != null, nameof(eventArgs));
            Contract.Requires(!string.IsNullOrEmpty(commandName));

            var command = GetCommand(commandName);
            if (command == null)
                throw new ArgumentException($"A command with the name '{commandName}' does not exist");
            return command.CanExecute(user, viewModel, eventArgs);
        }
        internal void Execute(ClaimsPrincipal user, string commandName, object viewModel, object eventArgs)
        {
            Contract.Requires(CanExecute(user, commandName, viewModel, eventArgs));

            var command = GetCommand(commandName);
            if (command == null)
                throw new ArgumentException($"A command with the name '{commandName}' does not exist");
            command.Execute(user, viewModel, eventArgs);
        }

        public bool IsAuthorized(ClaimsPrincipal user, string commandName)
        {
            Contract.Requires(!string.IsNullOrEmpty(commandName));
            Contract.Requires(this.Exists(commandName));

            return GetCommand(commandName).IsAuthorized(user);
        }
        internal CommandViewModel GetCommand(string commandName)
        {
            return this.Commands.FirstOrDefault(c => c.Name == commandName);
        }
        internal CommandViewModel GetCommandCaseInsensitive(string commandName)
        {
            return this.Commands.FirstOrDefault(c => StringComparer.OrdinalIgnoreCase.Equals(c.Name, commandName));
        }
        public bool Exists(string commandName)
        {
            Contract.Requires(!string.IsNullOrEmpty(commandName));

            return this.GetCommand(commandName) != null;
        }
        public bool ExistsCaseInsensitive(string commandName)
        {
            Contract.Requires(!string.IsNullOrEmpty(commandName));

            return GetCommandCaseInsensitive(commandName) != null;
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

        IEnumerator IEnumerable.GetEnumerator() => this.Commands.GetEnumerator();
    }
    public sealed class CommandViewModel : INotifyPropertyChanged
    {
        private readonly CommandManager manager;
        private readonly Func<ClaimsPrincipal, bool> isAuthorized;

        public event PropertyChangedEventHandler PropertyChanged { add { } remove { } }
        [NoViewBinding]
        public ICommand Command { get; }
        [IdentifierViewBinding]
        public string Name { get; }

        internal CommandViewModel(CommandManager manager,
                                  ICommand command,
                                  string commandName,
                                  Func<ClaimsPrincipal, bool> isAuthorized)
        {
            Contract.Requires(manager != null);
            Contract.Requires(command != null);
            Contract.Requires(isAuthorized != null);
            Contract.Requires(!string.IsNullOrEmpty(commandName));

            this.manager = manager;
            this.Command = command;
            this.Name = commandName;
            this.isAuthorized = isAuthorized;
        }

        public bool CanExecute(ClaimsPrincipal user, object viewModel, object eventArgs)
        {
            Contract.Requires(viewModel != null, nameof(viewModel));
            Contract.Requires(eventArgs != null, nameof(eventArgs));

            if (!this.isAuthorized(user))
            {
                return false;
            }

            bool firstCondition = this.Command.CanExecute.Evaluate(viewModel, eventArgs);
            if (firstCondition)
            {
                bool secondCondition = this.Command.AdditionalCanExecute(viewModel, eventArgs);
                if (secondCondition)
                {
                    return true;
                }
            }
            return false;
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
