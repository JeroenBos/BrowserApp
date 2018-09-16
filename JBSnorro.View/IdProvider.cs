using JBSnorro;
using JBSnorro.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JBSnorro.View
{
    internal sealed class IdProvider : IIdProvider
    {
        private int idCounter = -1;
        private readonly WeakTwoWayDictionary<int, object> map;

        public IdProvider(IEqualityComparer<object> equalityComparer = null)
        {
            equalityComparer = equalityComparer ?? Global.ReferenceEqualityComparer;
            this.map = new WeakTwoWayDictionary<int, object>(equalityComparer);
        }
        public IdProvider(int autoCleanup, IEqualityComparer<object> equalityComparer = null)
        {
            equalityComparer = equalityComparer ?? Global.ReferenceEqualityComparer;
            this.map = new WeakTwoWayDictionary<int, object>(equalityComparer, autoCleanup);
        }

        public int this[object obj] => this.map[obj];
        public object this[int id] => this.map[id];

        public bool TryGetKey(object value, out int key) => this.map.TryGetKey(value, out key);
        public bool TryGetValue(int key, out object value) => this.map.TryGetValue(key, out value);

        public void Add(object obj) => this.map.Add(Interlocked.Increment(ref this.idCounter), obj);
        public void Remove(int id) => this.map.Remove(id);
        public void Remove(object obj) => this.map.Remove(obj);
        public void Remove(int id, object obj) => this.map.Remove(id, obj);
        public void Clean() => this.map.Clean();

        public bool Contains(object obj) => this.map.Contains(obj);
        public bool Contains(int id) => this.map.Contains(id);
    }
}
