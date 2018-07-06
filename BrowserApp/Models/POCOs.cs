using JBSnorro;
using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static BrowserApp.Reflection;

namespace BrowserApp.POCOs
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
                int id = idProvider[obj];
                return Create(id);
            }
            return obj;
        }
        public static Reference Create(int id)
        {
            Contract.Requires(id >= 0);

            return new Reference { __id = id };
        }
    }
    class PropertyChange : Change
    {
        public string PropertyName { get; set; }
        public object Value { get; set; }

        /// <param name="value"> Can be a view model. </param>
        public static PropertyChange Create(int containerId, string propertyName, object value, IIdProvider idProvider)
        {
            Contract.Requires(idProvider != null);

            return Create(containerId, propertyName, Reference.AsReferenceOrDefault(value, idProvider));
        }
        /// <param name="value"> Cannot be a view model. </param>
        public static PropertyChange Create(int containerId, string propertyName, object value)
        {
            Contract.Requires(containerId >= 0);
            Contract.Requires(!string.IsNullOrEmpty(propertyName));
            Contract.Requires(value == null || !IncludeDeep(value.GetType()), $"Objects of type '{value.GetType()}' cannot be serialized. Use a reference instead. ");

            // TODO: validate that value can be serialized

            return new PropertyChange() { Id = containerId, PropertyName = propertyName, Value = value };
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
}
