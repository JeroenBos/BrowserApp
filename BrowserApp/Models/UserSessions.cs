using System;
using System.IO;
using System.Linq;
using JBSnorro;
using JBSnorro.Collections.Immutable;
using JBSnorro.Diagnostics;
using static BrowserApp.Reflection;
using BrowserApp.POCOs;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Input;
using JBSnorro.Logging;

namespace BrowserApp
{
    public class UserSession
    {
        public object ViewModelRoot { get; }
        internal View View { get; }
        private readonly object _lock = new object();
        private readonly ThreadSafeList<Change> changes = new ThreadSafeList<Change>();
        private readonly ProcessingQueue<ICommand> commands;
        private readonly AtMostOneAwaiter waiter;
        private readonly ILogger logger;

        public void ExecuteCommand(ICommand command)
        {
            lock (_lock)
            {
                commands.Enqueue(command);
            }
            logger.LogInfo("UserSession: Enqueued command as eligible");
        }
        /// <summary>
        /// Returns a response with all non-propagated changes if there are any,
        /// otherwise waits (successively longer).
        /// </summary>
        internal async Task<Response> FlushOrWait()
        {
            logger.LogInfo("UserSession: Flushing or waiting");
            this.waiter.Reset();

            lock (_lock)
            {
                var changes = this.changes.Clear().ToArray();

                if (changes.Length != 0)
                {
                    return new Response(this.commands.Count != 0, changes);
                }
            }

            logger.LogInfo("UserSession: Waiting");
            await this.waiter.Wait();

            return this.Flush();
        }
        /// <summary>
        /// Immediately returns a response with all non-propagated changes.
        /// </summary>
        internal Response Flush()
        {
            logger.LogInfo("UserSession: Flushing");

            lock (_lock)
            {
                var changes = this.changes.Clear().ToArray();

                return new Response(this.commands.Count != 0, changes);
            }
        }
        internal void RegisterChange(Change change)
        {
            logger.LogInfo("UserSession: registering change");

            this.changes.Add(change);
        }

        internal UserSession(object viewModelRoot, ILogger logger, AtMostOneAwaiter waiter = null)
        {
            const int _10ms = 10;
            const int _5minutes = 5 * 60 * 1000;
            Contract.Requires(viewModelRoot != null);
            Contract.Requires(logger != null);
            Contract.Requires(viewModelRoot != null && IncludeDeep(viewModelRoot.GetType()), "The view model is not of any view model type");

            this.logger = logger;
            this.View = new View(viewModelRoot, this.RegisterChange, new IdProvider());
            this.commands = new ProcessingQueue<ICommand>(() => Task.Run(this.worker));
            this.waiter = waiter ?? new AtMostOneAwaiter(defaultDuration: _10ms, maxDuration: _5minutes);
        }

        private void worker()
        {
            while (commands.TryDequeue(out ICommand command))
            {
                logger.LogInfo("UserSession: Dequeued command");
                try
                {
                    command.Execute(null);
                }
                catch (Exception e)
                {
                    logger.LogError($"UserSession: Command failed: {e.Message}");
                    throw;
                }
                finally
                {
                    commands.OnProcessed(command);
                    logger.LogInfo("UserSession: Command completed");
                }
            }
        }
    }
}