using JBSnorro;
using JBSnorro.Collections;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace JBSnorro.View
{
    public static class Reflection
    {
        /// <param name="info"> Is null iff the container is a collection. </param>
        public delegate void AcceptDelegate(object container, PropertyInfo info, object value);

        /// <summary>
        /// Visits all included properties and collection items under the specified view model.
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
            return type.Implements(typeof(INotifyPropertyChanged))
                || type.Implements(typeof(INotifyCollectionChanged));
        }

        public static IEnumerable<PropertyInfo> GetIncludedProperties(INotifyPropertyChanged container)
        {
            Contract.Requires(container != null);

            var regularProperties = container.GetType()
                                             .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                             .Where(property => !property.IsIndexer())
                                             .Where(property => !property.HasAttribute<NoViewBindingAttribute>())
                                             .Select(property => (PropertyInfo)property);

            if (container is IExtraViewPropertiesContainer propertiesContainer)
            {
                var extraProperties = propertiesContainer.Properties.Select(p => PropertyInfo.Create(propertiesContainer, p.Key));
                return regularProperties.Concat(extraProperties);
            }

            return regularProperties;
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
                foreach (var propertyInfo in GetIncludedProperties(container))
                {
                    this.changes.Add(PropertyChange.Create(propertyInfo, container, idProvider));
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

        public abstract class PropertyInfo
        {
            public static implicit operator PropertyInfo(System.Reflection.PropertyInfo propertyInfo) => new RegularPropertyInfo(propertyInfo);
            internal static PropertyInfo Create(IExtraViewPropertiesContainer extraProperties, string name) => new ExtraPropertyInfo(extraProperties, name);
            public static PropertyInfo From(object sender, string propertyName)
            {
                if (sender is IExtraViewPropertiesContainer container)
                {
                    if (container.Properties.ContainsKey(propertyName))
                    {
                        return Create(container, propertyName);
                    }
                }

                var propertyInfo = sender.GetType().GetProperty(propertyName);
                Contract.Ensures(propertyInfo != null, $"No public property '{propertyName}' could not be found on an instance of type '{sender.GetType().FullName}'"
                    + (sender is IExtraViewPropertiesContainer ? $", nor did the {nameof(IExtraViewPropertiesContainer)} implementation specify a property with that name" : ""));
                return propertyInfo;
            }
            public abstract object GetValue(object container);
            /// <summary>
            /// Gets the name of the property, by which it can be retrieved via reflection or <see cref="IExtraViewPropertiesContainer.Properties"/>.
            /// This may differ from the name of the property by which is it serialized, 
            /// which is the result of calling <see cref="IdentifierViewBinding.ToTypescriptIdentifier(string)"/> on <see cref="this.Name"/>.
            /// </summary>
            public abstract string Name { get; }
            public abstract Type PropertyType { get; }

            private PropertyInfo() { }

            private sealed class RegularPropertyInfo : PropertyInfo
            {
                private readonly System.Reflection.PropertyInfo info;
                public override string Name => info.Name;
                public override Type PropertyType => info.PropertyType;
                public override object GetValue(object container)
                {
                    var value = info.GetValue(container);
                    var substitutionAttribute = info.GetCustomAttribute<ViewBindingAsAttribute>();
                    if (substitutionAttribute != null)
                    {
                        return substitutionAttribute.GetOrCreateSubstitute(value);
                    }
                    return value;
                }

                public RegularPropertyInfo(System.Reflection.PropertyInfo propertyInfo)
                {
                    Contract.Requires(propertyInfo != null);

                    this.info = propertyInfo;
                }
            }
            private sealed class ExtraPropertyInfo : PropertyInfo
            {
                private readonly IExtraViewPropertiesContainer extraProperties;
                public override string Name { get; }
                public override Type PropertyType
                {
                    get
                    {
                        var value = extraProperties.Properties[this.Name];
                        if (value == null)
                        {
                            return typeof(object);
                        }
                        return value.GetType();
                    }
                }
                public override object GetValue(object container)
                {
                    Contract.Requires(container == extraProperties);
                    return extraProperties.Properties[Name];
                }
                public ExtraPropertyInfo(IExtraViewPropertiesContainer extraProperties, string name)
                {
                    Contract.Requires(extraProperties != null);
                    Contract.Requires(!string.IsNullOrEmpty(name));

                    this.Name = name;
                    this.extraProperties = extraProperties;
                }
            }
        }

    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class NoViewBindingAttribute : Attribute
    {

    }
    public interface IExtraViewPropertiesContainer
    {
        IReadOnlyDictionary<string, object> Properties { get; }
    }

    public abstract class ViewBindingAsAttribute : Attribute
    {
        /// <summary>
        /// This is a global dictionary that maps all objects that have ever been substituted by another object to that object.
        /// </summary>
        internal static readonly WeakReferenceDictionary<object, object> mappedObjects
            = new WeakReferenceDictionary<object, object>(JBSnorro.Global.ReferenceEqualityComparer);

        /// <summary>
        /// Gets an object that is represents the actual view model of the specified object.
        /// </summary>
        public object GetOrCreateSubstitute(object obj)
        {
            Contract.Requires(obj != null, "Only non-null properties can be substituted");
            // Contract.Requires(obj.GetType().IsClass, "Only reference types can be substituted");

            if (!mappedObjects.TryGetValue(obj, out object substitute))
            {
                substitute = this.createSubstitute(obj);
                mappedObjects.Add(obj, substitute);
            }
            return substitute;
        }
        /// <summary>
        /// Creates an object that is represents the actual view model of the specified object, assuming the specified object does not already have a substitute.
        /// </summary>
        protected abstract object createSubstitute(object obj);
    }
    /// <summary>
    /// Maps a collection to a typescript map object, which is basically a Dictionary&lt;string, object&gt;
    /// </summary>
    public abstract class ViewBindingAsMapAttribute : ViewBindingAsAttribute
    {
        /// <summary>
        /// Gets the name of the typescript property under which the object is to be assigned.
        /// From the perspective that we're mapping a collection to a Dictionary&lt;string,object&gt;, this function gets the key.
        /// </summary>
        /// <param name="value"> The object for which we're returning the key. </param>
        /// <param name="index"> The index in the collection for which we're returning the key. </param>
        protected abstract string GetAttributeName(object value, int index);

        /// <summary>
        /// Creates an object that is represents the actual view model of the specified object, assuming the specified object does not already have a substitute.
        /// </summary>
        protected sealed override object createSubstitute(object collection) => createSubstitute((INotifyCollectionChanged)collection);
        public INotifyPropertyChanged createSubstitute(INotifyCollectionChanged collection)
        {
            var result = new Notifier();
            collection.CollectionChanged += onCollectionChange;
            return result;
            void onCollectionChange(object sender, NotifyCollectionChangedEventArgs e)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        int newIndex = e.NewStartingIndex;
                        foreach (object newItem in e.NewItems)
                        {
                            string name = this.GetAttributeName(newItem, newIndex);
                            Contract.Assert(!string.IsNullOrEmpty(name), "The attribute name may not be null or empty");
                            Contract.Assert(!result.Properties.ContainsKey(name), $"The collection already contains a member with name '${name}'");
                            result.Properties[name] = newItem;
                            result.Invoke(result, PropertyMutatedEventArgsExtensions.Create(name, typeof(object), null, newItem));
                            newIndex++;
                        }
                        break;
                    case NotifyCollectionChangedAction.Move:
                        break; // don't do anything
                    case NotifyCollectionChangedAction.Remove:
                        int oldIndex = e.OldStartingIndex;
                        foreach (object newItem in e.OldItems)
                        {
                            string name = this.GetAttributeName(newItem, oldIndex);
                            Contract.Assert(!string.IsNullOrEmpty(name), "The attribute name may not be null or empty");
                            Contract.Assert(result.Properties.ContainsKey(name), $"The collection did not contain a member with name '${name}' to remove");
                            var oldItem = result.Properties[name];
                            result.Properties[name] = null;
                            result.Invoke(result, PropertyMutatedEventArgsExtensions.Create(name, typeof(object), oldItem, null));
                            oldIndex++;
                        }
                        break;
                    case NotifyCollectionChangedAction.Replace:
                    case NotifyCollectionChangedAction.Reset:
                        throw new NotImplementedException();
                    default:
                        throw new ArgumentException();
                }
            }
        }
        private sealed class Notifier : INotifyPropertyChanged, IExtraViewPropertiesContainer
        {
            [NoViewBinding]
            public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();

            IReadOnlyDictionary<string, object> IExtraViewPropertiesContainer.Properties => Properties;

            public event PropertyChangedEventHandler PropertyChanged;

            internal void Invoke(object sender, PropertyChangedEventArgs e)
                => this.PropertyChanged?.Invoke(sender, e);
        }
    }
    /// <summary>
    /// Specifies that the string property this attribute is attached to contains a identifier, 
    /// such that it will be serialize accordingly (i.e. by default the first letter is lowercased).
    /// </summary>
    public class IdentifierViewBinding : ViewBindingAsAttribute
    {
        public static string ToTypescriptIdentifier(string s) => s.ToFirstLower();
        public static string ToCSharpIdentifier(string s) => s.ToFirstUpper();

        protected override object createSubstitute(object obj)
        {
            switch (obj)
            {
                case null:
                    return null;
                case string s:
                    return ToTypescriptIdentifier(s);
                default:
                    throw new ArgumentException($"The attribute '${nameof(IdentifierViewBinding)}' can only be applied to string properties");
            }
        }
    }
}
