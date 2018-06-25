using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BrowserApp
{
    public interface IUserSessionsStorage
    {
        void CreateOrUpdate(string user, Stream data);
        Task<Stream> TryOpen(string user);
    }


    public interface IIdProvider
    {
        int this[object obj] { get; }
        object this[int obj] { get; }

        bool Contains(object obj);
        bool Contains(int id);

        void Remove(object obj);
        void Remove(int id);

        void Add(object obj);
    }
}
