using BrowserApp.POCOs;
using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace BrowserApp
{
    public static class Reflection
    {
        /// <param name="info"> Is null iff the container is a collection. </param>
        public delegate void AcceptDelegate(object container, PropertyInfo info, object value);


        /// <summary>
        /// Visists all included properties and collection items under the specified view model.
        /// </summary>
        public static void VisitProperties(object viewModel, AcceptDelegate visitNode)
        {
            visit(viewModel, null, null, visitNode);
        }
        /// <summary>
        /// Visits all included view models under the specified view model (only once, as opposed to possibly twice if it's a <see cref="INotifyPropertyChanged"/> and a <see cref="INotifyCollectionChanged"/>).
        /// </summary>
        public static void VisitViewModels(object viewModel, Action<object> visitViewModel)
        {
            VisitViewModels(viewModel,
                            propertyChangedNode => visitViewModel(propertyChangedNode),
                            collectionChangedNode => visitViewModelIfNotNotifyPropertyChanged(collectionChangedNode));

            // alternative implementation:
            // visit(viewModel, null, null, (container, propertyInfo, value) =>
            // {
            //     if (value != null && IncludeDeep(value.GetType()))
            //     {
            //         visitViewModel(value);
            //     }
            // });

            void visitViewModelIfNotNotifyPropertyChanged(INotifyCollectionChanged collectionChangedNode)
            {
                if (!(collectionChangedNode is INotifyPropertyChanged)) // otherwise it's already visited as a property changed node
                {
                    visitViewModel(collectionChangedNode);
                }
            }
        }
        /// <summary>
        /// Visits all included view models under the specified view model.
        /// </summary>
        public static void VisitViewModels(object viewModel, Action<INotifyPropertyChanged> visitPropertyChangedNode, Action<INotifyCollectionChanged> visitCollectionChangedNode)
        {
            visit(viewModel, visitPropertyChangedNode, visitCollectionChangedNode, null);
        }
        /// <summary>
        /// Visits all included view models under the specified view model.
        /// </summary>
        private static void visit(object viewModel,
                           Action<INotifyPropertyChanged> visitPropertyChangedNode,
                           Action<INotifyCollectionChanged> visitCollectionChangedNode,
                           AcceptDelegate visitProperty)
        {
            Contract.Requires(viewModel != null);

            if (viewModel is INotifyPropertyChanged pc)
                visitPropertyChangedNode?.Invoke(pc);
            if (viewModel is INotifyCollectionChanged cc)
                visitCollectionChangedNode?.Invoke(cc);

            if (viewModel is INotifyPropertyChanged container)
            {
                foreach (var propertyInfo in GetIncludedProperties(container))
                {
                    var value = new Lazy<object>(() => propertyInfo.GetValue(viewModel));
                    if (visitProperty != null && IncludeProperty(viewModel, propertyInfo))
                    {
                        visitProperty(viewModel, propertyInfo, value.Value);
                    }

                    if (IncludeDeep(viewModel, propertyInfo))
                    {
                        visit(value.Value, visitPropertyChangedNode, visitCollectionChangedNode, visitProperty);
                    }
                }
            }
            if (viewModel is INotifyCollectionChanged collection)
            {
                foreach (var item in GetIncludedItems(collection))
                {
                    visitProperty?.Invoke(viewModel, null, item);

                    if (IncludeDeep(collection, item))
                    {
                        visit(item, visitPropertyChangedNode, visitCollectionChangedNode, visitProperty);
                    }
                }
            }
        }

        /// <summary>
        /// Returns whether the specified property on the specified view model should be included in the view.
        /// </summary>
        public static bool IncludeProperty(object viewModel, PropertyInfo property)
        {
            if (property == null) { throw new ArgumentNullException(nameof(property)); }
            if (viewModel == null) { return false; }
            if (!IncludeDeep(viewModel.GetType())) { throw new ArgumentException($"No view model is necessary for type '{viewModel.GetType()}", nameof(viewModel)); }

            return true; // for now any property on any view model object is included. TODO: some attribute reflection 
        }
        public static bool IncludeDeep(object viewModel, string propertyName)
        {
            if (viewModel == null)
            {
                return false;
            }
            var property = viewModel.GetType().GetProperty(propertyName);
            if (property == null)
            {
                throw new ArgumentException($"The property '{propertyName}' was not found on type '{viewModel.GetType().FullName}'");
            }
            return IncludeDeep(viewModel, property);
        }
        public static bool IncludeDeep(object viewModel, PropertyInfo property)
        {
            bool result = viewModel != null && IncludeDeep(property.PropertyType);

            Contract.Ensures(!result || (result && IncludeProperty(viewModel, property)), $"'{nameof(IncludeDeep)}' should imply '{nameof(IncludeProperty)}'");
            return result;
        }
        public static bool IncludeDeep(INotifyCollectionChanged viewModel, object item)
        {
            if (viewModel == null || item == null)
            {
                return false;
            }

            return IncludeDeep(item.GetType());
        }
        public static bool IncludeDeep(Type type)
        {
            return type == typeof(INotifyPropertyChanged)
                || type == typeof(INotifyCollectionChanged);
        }

        public static IEnumerable<PropertyInfo> GetIncludedProperties(INotifyPropertyChanged container)
        {
            Contract.Requires(container != null);

            return container.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        }
        public static IEnumerable<object> GetIncludedItems(INotifyCollectionChanged collection)
        {
            if (!(collection is System.Collections.IEnumerable sequence))
                throw new Exception("All INotifyCollectionChangeds must implement IEnumerable, otherwise it's impossible to retrieve the initial elements");

            return sequence.Cast<object>();
        }


        internal sealed class CollectStateVisitor
        {
            public IReadOnlyList<Change> Changes => new ReadOnlyCollection<Change>(changes);
            private readonly List<Change> changes = new List<Change>();
            private readonly IIdProvider idProvider;

            public CollectStateVisitor(IIdProvider idProvider)
            {
                this.idProvider = idProvider;
            }

            public void Accept(INotifyPropertyChanged container)
            {
                int containerId = this.idProvider[container];

                foreach (var propertyInfo in GetIncludedProperties(container))
                {
                    object value = propertyInfo.GetValue(container);
                    this.changes.Add(PropertyChange.Create(containerId, propertyInfo.Name, value, idProvider));
                }
            }
            public void Accept(INotifyCollectionChanged collection)
            {
                int collectionId = this.idProvider[collection];

                int index = 0;
                foreach (object item in GetIncludedItems(collection))
                {
                    this.changes.Add(CollectionItemAdded.Create(collectionId, item, idProvider, index++));
                }
            }
        }
    }
}
