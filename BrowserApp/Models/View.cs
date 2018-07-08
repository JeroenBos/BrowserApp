using BrowserApp.POCOs;
using JBSnorro;
using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using static BrowserApp.Reflection;

namespace BrowserApp
{
    internal sealed class View
    {
        private readonly Action<Change> AddChange;
        /// <summary>
        /// Associates ids with view models.
        /// </summary>
        private readonly IIdProvider idProvider;
        private readonly List<View> views;

        internal View(object viewModel, Action<Change> addChange, IIdProvider idProvider)
        {
            Contract.Requires(viewModel != null);
            Contract.Requires(addChange != null);
            Contract.Requires(idProvider != null);
            Contract.Requires(IncludeDeep(viewModel.GetType()), $"An object of type '{viewModel.GetType()}' does not need a view");

            this.AddChange = addChange;
            this.idProvider = idProvider;
            this.views = new List<View>();

            registerViewModel(viewModel);
        }

        private void registerViewmodelAndAddCompleteStateAsChanges(object viewModel)
        {
            registerViewModel(viewModel);
            AddCompleteStateAsChanges(viewModel);
        }
        private void registerViewModel(object viewModel)
        {
            Contract.Requires(viewModel != null);
            Contract.Requires(IncludeDeep(viewModel.GetType()));
            Contract.Requires(!idProvider.Contains(viewModel), "The specified view model already has a view"); // maybe you shouldn't do anything here. The view already exists so let it be used twice...
                                                                                                               // Although then removal will be tricky: suppose the first is removed, then the second couldn't be found anymore
            VisitViewModels(viewModel, createId);
            VisitViewModels(viewModel, registerEvents);

            void createId(object viewModelNode)
            {
                idProvider.Add(viewModelNode);
            }
            void registerEvents(object viewModelNode)
            {
                if (viewModelNode is INotifyPropertyChanged pc)
                    pc.PropertyChanged += propertyChanged;
                if (viewModelNode is INotifyCollectionChanged cc)
                    cc.CollectionChanged += collectionChanged;
            }
        }
        private void unregisterViewModel(object viewModel)
        {
            Contract.Requires(viewModel != null);
            Contract.Requires(IncludeDeep(viewModel.GetType()));

            removeId();
            unregisterEvents();

            void removeId()
            {
                idProvider.Remove(viewModel);
            }
            void unregisterEvents()
            {
                if (viewModel is INotifyPropertyChanged pc)
                    pc.PropertyChanged -= propertyChanged;
                if (viewModel is INotifyCollectionChanged cc)
                    cc.CollectionChanged -= collectionChanged;
            }
        }

        private void propertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Contract.Requires(sender != null);
            Contract.Requires(e != null);
            var propertyInfo = sender.GetType().GetProperty(e.PropertyName);
            Contract.Requires(propertyInfo != null, $"No public property '{e.PropertyName}' could not be found on an instance of type '{sender.GetType().FullName}'");

            if (IncludeDeep(sender, e.PropertyName))
            {
                if (!(e is IPropertyMutatedEventArgs m))
                    throw new ContractException("For view model properties that have nested viewmodels, the old value needs to be accessible (for unregistering). ");
                // Alternatively, we may store the old values ourselves.
                // A potential bug (or it's more like an assumption) is this: 
                // Suppose a property is mutated, but it's property changed event invocation is suspended, then it is modified again, and then the event is raised.
                // Here I'm assuming that the old value is then the original value, not the intermediate value, because I still need to unregister the original value

                if (m.OldValue != null)
                {
                    this.unregisterViewModel(m.OldValue);
                }
                if (m.NewValue != null)
                {
                    this.registerViewmodelAndAddCompleteStateAsChanges(m.NewValue);
                }
            }

            if (IncludeProperty(sender, propertyInfo))
            {
                var value = propertyInfo.GetValue(sender);
                this.AddChange(PropertyChange.Create(idProvider[sender], e.PropertyName, value));
            }
        }
        private void collectionChanged(object sender_, NotifyCollectionChangedEventArgs e)
        {
            if (!(sender_ is INotifyCollectionChanged)) { throw new ArgumentException(); }
            INotifyCollectionChanged sender = (INotifyCollectionChanged)sender_;
            int senderId = 0;

            Change change;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems.Count != 1) { throw new NotImplementedException(); }
                    Contract.Requires(e.NewStartingIndex >= 0, "The index of the added item(s) must be specified");
                    if (IncludeDeep(sender, item: e.NewItems[0]))
                    {
                        registerViewmodelAndAddCompleteStateAsChanges(e.NewItems[0]);
                    }
                    change = CollectionItemAdded.Create(senderId, e.NewItems[0], idProvider, e.NewStartingIndex == -1 ? default(int?) : e.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Move:
                    throw new NotImplementedException();
                case NotifyCollectionChangedAction.Remove:
                    Contract.Requires(e.OldStartingIndex >= 0, "The index of the removed item must be specified");
                    change = CollectionItemRemoved.Create(senderId, e.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    throw new NotImplementedException();
                case NotifyCollectionChangedAction.Reset:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentException("Invalid action", nameof(e));
            }

            this.AddChange(change);
        }


        public void AddCompleteStateAsChanges(object viewModel)
        {
            var stateVisitor = new CollectStateVisitor(this.idProvider);
            VisitViewModels(viewModel, stateVisitor.Accept, stateVisitor.Accept);
            foreach (var change in stateVisitor.Changes)
            {
                this.AddChange(change);
            }
        }
    }
}
