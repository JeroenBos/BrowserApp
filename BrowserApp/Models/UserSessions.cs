using System;
using System.IO;
using System.Linq;
using JBSnorro;
using JBSnorro.Collections.Immutable;
using JBSnorro.Diagnostics;
using static BrowserApp.Reflection;
using BrowserApp.POCOs;

namespace BrowserApp
{
    public class UserSession
    {
        public object ViewModelRoot { get; }
        internal View View { get; }
        private readonly ThreadSafeList<Change> changes = new ThreadSafeList<Change>();

        internal Response Flush()
        {
            return new Response(this.changes.Clear().ToArray());
        }
        internal void RegisterChange(Change change)
        {
            this.changes.Add(change);
        }

        internal UserSession(ViewModelFactoryDelegate viewModelFactory, Stream initialData = null)
        {
            Contract.Requires(viewModelFactory != null);

            var viewModelRoot = viewModelFactory(initialData);
            Contract.Assert(viewModelRoot != null && IncludeDeep(viewModelRoot.GetType()), "The view model is not of any view model type");
            this.View = new View(viewModelRoot, this.RegisterChange, new IdProvider());
        }
    }
}