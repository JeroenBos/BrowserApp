﻿using JBSnorro;
using JBSnorro.Diagnostics;
using JBSnorro.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using static JBSnorro.View.Reflection;

namespace JBSnorro.View
{
    class Response
    {
        public Change[] Changes { get; }
        public bool Rerequest { get; }
        public Response(bool rerequest, params Change[] changes)
        {
            if (changes == null) throw new ArgumentNullException(nameof(changes));

            this.Changes = changes;
            this.Rerequest = rerequest;
        }
    }
    abstract class Change
    {
        /// <summary>
        /// The id of the object that contains the change (i.e. the object that has the property that changed, or the collection that has the newly added item, etc).
        /// </summary>
        public int Id { get; set; }
    }
    class Reference
    {
        public bool IsCollection { get; set; }
        public int __id { get; set; }
        /// <summary>
        /// Returns the specified object as reference if it is a view model, otherwise returns the object itself.
        /// </summary>
        public static object AsReferenceOrDefault(object obj, IIdProvider idProvider)
        {
            Contract.Requires(idProvider != null);

            if (obj == null)
            {
                return null;
            }
            if (IncludeDeep(obj.GetType()))
            {
                return Create(obj, obj is INotifyCollectionChanged, idProvider);
            }
            return obj;
        }

        public static Reference Create(INotifyPropertyChanged viewModel, IIdProvider idProvider)
            => Create(viewModel, isCollection: false, idProvider);
        public static Reference Create(INotifyCollectionChanged viewModel, IIdProvider idProvider)
            => Create(viewModel, isCollection: true, idProvider);
        private static Reference Create(object viewModel, bool isCollection, IIdProvider idProvider)
        {
            Contract.Requires(viewModel != null);
            Contract.Requires(idProvider != null);

            int id = idProvider[viewModel];
            return Create(id, isCollection);
        }

        public static Reference Create(int id, bool isCollection = false)
        {
            Contract.Requires(id >= 0);

            return new Reference { __id = id, IsCollection = isCollection };
        }
    }
    class PropertyChange : Change
    {
        public string PropertyName { get; set; }
        public object Value { get; set; }


        /// <param name="container"> The object that contains the specified property. </param>
        public static PropertyChange Create(PropertyInfo propertyInfo, object container, IIdProvider idProvider)
        {
            object value = propertyInfo.GetValue(container);
            string propertyName = IdentifierViewBinding.ToTypescriptIdentifier(propertyInfo.Name);
            return Create(idProvider[container], propertyName, value, idProvider);
        }
        /// <param name="propertyName"> The name of the property how it will be serialized. </param>
        /// <param name="value"> Can be a view model. </param>
        public static PropertyChange Create(int containerId, string propertyName, object value, IIdProvider idProvider)
        {
            Contract.Requires(idProvider != null);

            return Create(containerId, propertyName, Reference.AsReferenceOrDefault(value, idProvider));
        }
        /// <param name="propertyName"> The name of the property how it will be serialized. </param>
        /// <param name="value"> Cannot be a view model. </param>
        public static PropertyChange Create(int containerId, string propertyName, object value)
        {
            Contract.Requires(containerId >= 0);
            Contract.Requires(!string.IsNullOrEmpty(propertyName));
            Contract.Requires(value == null || !IncludeDeep(value.GetType()),
                $"Objects of type '{value?.GetType()}' cannot be serialized. Use a reference instead, or provide an idProvider. ");

            // TODO: validate that value can be serialized

            return new PropertyChange() { Id = containerId, PropertyName = propertyName, Value = value };
        }

        public override string ToString()
        {
            string propertyValue = Value is Reference r ? $"(id={r.__id})" : (Value?.ToString() ?? "null");
            return $"(id={Id}).{PropertyName}->{propertyValue}";
        }
    }
    abstract class CollectionChange : Change
    {
    }
    class CollectionItemRemoved : CollectionChange
    {
        public int Index { get; set; }

        public static CollectionItemRemoved Create(int collectionId, int index)
        {
            return new CollectionItemRemoved { Index = index, Id = collectionId };
        }
    }
    class CollectionItemAdded : CollectionChange
    {
        public object Item { get; set; }
        public int? Index { get; set; }

        /// <param name="item"> Can be a view model. </param>
        public static CollectionItemAdded Create(int collectionId, object item, IIdProvider idProvider, int? index = null)
        {
            Contract.Requires(idProvider != null);

            return Create(collectionId, Reference.AsReferenceOrDefault(item, idProvider), index);
        }
        /// <param name="item"> Cannot be a view model. </param>
        public static CollectionItemAdded Create(int collectionId, object item, int? index = null)
        {
            Contract.Requires(collectionId >= 0);
            Contract.Requires(item == null || !IncludeDeep(item.GetType()), $"Objects of type '{item.GetType()}' cannot be serialized. Use a reference instead. ");

            return new CollectionItemAdded { Item = item, Index = index, Id = collectionId };
        }
    }
    class CollectionItemsReordered : CollectionChange
    {
        public int Index1 { get; set; }
        public int Index2 { get; set; }
    }
    [Serializable]
    public sealed class CommandInstruction
    {
        public string CommandName { get; set; }
        public int ViewModelId { get; set; }
        public object EventArgs { get; set; }

        public static void CheckInvariants(CommandInstruction commandInstruction, ILogger logger)
        {
            string error = null;
            if (commandInstruction == null)
            {
                error = "command instruction was null";
            }
            else if (string.IsNullOrWhiteSpace(commandInstruction.CommandName))
            {
                error = "An empty command name was specified";
            }
            else if (commandInstruction.EventArgs == null)
            {
                error = "The event args on the command instruction was null";
            }
            else if (commandInstruction.ViewModelId < 0)
            {
                error = "A negative view model id was specified";
            }

            if (error != null)
            {
                logger?.LogError(error);
                throw new ArgumentException(error, nameof(commandInstruction));
            }
        }

        public CommandInstruction WithCSharpCommandName()
        {
            return new CommandInstruction
            {
                CommandName = IdentifierViewBinding.ToCSharpIdentifier(this.CommandName),
                EventArgs = this.EventArgs,
                ViewModelId = this.ViewModelId
            };
        }
    }
}