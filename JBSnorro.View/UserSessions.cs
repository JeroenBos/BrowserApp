using JBSnorro;
using JBSnorro.Collections.Immutable;
using JBSnorro.Diagnostics;
using JBSnorro.Logging;
using JBSnorro.View.Commands;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static JBSnorro.View.Reflection;

namespace JBSnorro.View
{
    public sealed class UserSession
    {
        public static bool processCommandsSyncronously = Global.DEBUG;

        private static readonly bool alwaysRerequest = false;
        public IAppViewModel ViewModelRoot { get; }
        public CommandManager CommandManager => ViewModelRoot.CommandManager;
        internal View View { get; }
        private readonly object _lock = new object();
        private readonly ThreadSafeList<Change> changes = new ThreadSafeList<Change>();
        private readonly ProcessingQueue<UserCommandInstruction> commands;
        private readonly AtMostOneAwaiter waiter;
        private readonly ILogger logger;
        internal readonly IIdProvider viewModelIdProvider;

        public Task ExecuteCommand(string commandName, int viewModelId, object eventArgs, ClaimsPrincipal user)
        {
            return ExecuteCommand(new CommandInstruction() { CommandName = commandName, ViewModelId = viewModelId, EventArgs = eventArgs }, user);
        }
        public Task ExecuteCommand(CommandInstruction instruction, ClaimsPrincipal user)
        {
            // check and throw here because were about to transfer to another thread, which makes debugging more difficult
            CommandInstruction.CheckInvariants(instruction, logger);

            var result = new UserCommandInstruction(instruction, user, this.viewModelIdProvider);
            lock (_lock)
            {
                commands.Enqueue(result);
            }
            logger.LogInfo("UserSession: Enqueued command as eligible");
            return result.Task;
        }
        /// <summary>
        /// Returns a response with all non-propagated changes if there are any,
        /// otherwise waits (successively longer).
        /// </summary>
        internal async Task<Response> FlushOrWait()
        {
            logger.LogInfo("UserSession: Flushing or waiting");

            lock (_lock)
            {
                Response flush = Flush();
                if (flush.Changes.Length != 0)
                {
                    return flush;
                }
            }

            logger.LogInfo("UserSession: Waiting");
            await this.waiter.Wait();
            logger.LogInfo("UserSession: Waited");

            return this.Flush();
        }
        /// <summary>
        /// Immediately returns a response with all non-propagated changes.
        /// </summary>
        internal Response Flush()
        {
            lock (_lock)
            {
                var changes = this.changes.Clear().ToArray();
                logger.LogInfo($"UserSession: Flushing {changes.Length} changes");

                if (changes.Length != 0)
                {
                    logger.LogInfo($"UserSession: Resetting");

                    this.waiter.Reset();
                }

                return new Response(alwaysRerequest || this.commands.Count != 0, changes);
            }
        }
        private void RegisterChange(Change change)
        {
            logger.LogInfo("UserSession: registering change");

            this.changes.Add(change);
        }

        internal UserSession(IAppViewModel viewModelRoot, ILogger logger, AtMostOneAwaiter waiter = null)
        {
            const int _10ms = 10;
            const int _5minutes = 5 * 60 * 1000;
            Contract.Requires(viewModelRoot != null);
            Contract.Requires(logger != null);
            Contract.Requires(viewModelRoot != null && IncludeDeep(viewModelRoot.GetType()), "The view model is not of any view model type");
            Contract.Requires(viewModelRoot.CommandManager != null);

            this.logger = logger;
            this.ViewModelRoot = viewModelRoot;
            this.viewModelIdProvider = new IdProvider();
            this.View = new View(viewModelRoot, this.RegisterChange, viewModelIdProvider);
            this.commands = new ProcessingQueue<UserCommandInstruction>(this.worker);
            this.waiter = waiter ?? new AtMostOneAwaiter(defaultDuration: _10ms, maxDuration: _5minutes);
        }

        private void worker()
        {
            if (processCommandsSyncronously)
                workerImpl();
            else
                Task.Run(() => workerImpl());

            void workerImpl()
            {
                while (commands.TryDequeue(out UserCommandInstruction instruction))
                {
                    bool success = false;
                    logger.LogInfo("UserSession: Dequeued command");
                    try
                    {
                        if (instruction.ViewModel == null)
                        {
                            throw new InvalidOperationException($"UserSession: Command failed: View model with id '{instruction.ViewModelId}' was not found");
                        }
                        else if (!CommandManager.Exists(instruction.CommandName))
                        {
                            string message = $"UserSession: Command failed: Command with name '{instruction.CommandName}' was not found";
                            var found = CommandManager.GetCommandCaseInsensitive(instruction.CommandName);
                            if (found != null)
                                message += $", but '{found.Name}' was found (case-sensitive)";
                            throw new InvalidOperationException(message);
                        }
                        else if (!CommandManager.IsAuthorized(instruction.User, instruction.CommandName))
                        {
                            throw new UnauthorizedAccessException($"UserSession: Command failed: The user is unauthorized to execute command '{instruction.CommandName}')");
                        }
                        else if (!CommandManager.CanExecute(instruction.User, instruction.CommandName, instruction.ViewModel, instruction.EventArgs))
                        {
                            logger.LogWarning("UserSession: Command not executed");
                            instruction.tcs.TrySetCanceled();
                        }
                        else
                        {
                            CommandManager.Execute(instruction.User, instruction.CommandName, instruction.ViewModel, instruction.EventArgs);
                            instruction.tcs.TrySetResult(null);
                            success = true;
                        }
                    }
                    catch (Exception e)
                    {
                        // We catch the exception here and propagate it to the context that initiated the command execution via the task completion source.
                        // If nobody is listening then nobody cares. In any case this simple worker thread doesn't care
                        logger.LogError($"{e.GetType().Name}: {e.Message}");
                        instruction.tcs.TrySetException(e);
                    }
                    finally
                    {
                        try
                        {
                            commands.OnProcessed(instruction);
                        }
                        catch (Exception e)
                        {
                            logger.LogError($"{e.GetType().Name}: UserSession.OnProcessed(command): {e.Message}");
                        }
                    }

                    if (success)
                    {
                        if (this.changes.Count != 0)
                        {
                            logger.LogInfo("UserSession: Command completed. Pulsing");
                            this.waiter.Pulse();
                        }
                        else
                        {
                            logger.LogInfo("UserSession: Command completed");
                        }
                    }
                }
            }
        }

        private sealed class UserCommandInstruction
        {
            internal readonly TaskCompletionSource<object> tcs;
            public string CommandName { get; }
            public int ViewModelId { get; }
            public object EventArgs { get; }
            public ClaimsPrincipal User { get; }
            /// <summary>
            /// Gets the view model associated with <see cref="CommandName"/> and <see cref="User"/>; or null in case no such view model was found.
            /// </summary>
            public object ViewModel { get; }
            public Task Task => tcs.Task;

            public UserCommandInstruction(string commandName, int viewModelId, object eventArgs, ClaimsPrincipal user, IIdProvider viewModelResolver)
            {
                Contract.Requires(viewModelResolver != null);

                this.User = user;
                this.CommandName = commandName;
                this.ViewModelId = viewModelId;
                this.EventArgs = eventArgs;
                this.tcs = new TaskCompletionSource<object>();

                viewModelResolver.TryGetValue(this.ViewModelId, out object viewModel);
                this.ViewModel = viewModel; // possibly reassigns null 
            }
            public UserCommandInstruction(CommandInstruction instruction, ClaimsPrincipal user, IIdProvider viewModelResolver)
                : this(instruction.CommandName, instruction.ViewModelId, instruction.EventArgs, user, viewModelResolver)
            {
                Contract.Requires(instruction != null);

            }
        }
    }

}