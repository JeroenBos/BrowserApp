using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace JBSnorro.View
{
    public interface IUserSessionsStorage
    {
        void CreateOrUpdate(string user, object data);
        Task<object> TryOpen(string user);
        object DefaultSession { get; }
    }
    public interface IUserSessionsStorage<TData> : IUserSessionsStorage
    {
        void CreateOrUpdate(string user, TData data);
        new Task<TData> TryOpen(string user);
        new TData DefaultSession { get; }
    }


    public interface IIdProvider
    {
        int this[object obj] { get; }
        object this[int obj] { get; }

        bool TryGetKey(object value, out int key);
        bool TryGetValue(int key, out object value);

        bool Contains(object obj);
        bool Contains(int id);

        void Remove(object obj);
        void Remove(int id);

        void Add(object obj);
    }
}
