using JBSnorro.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace JBSnorro.View.App
{
    class TempUserSessionsStorage : IUserSessionsStorage<Stream>
    {
        public void CreateOrUpdate(string user, Stream data)
        {
        }
        public Task<Stream> TryOpen(string user)
        {
            return Task.FromResult<Stream>(new MemoryStream());
        }

        void IUserSessionsStorage.CreateOrUpdate(string user, object data) => CreateOrUpdate(user, (Stream)data);
        Task<object> IUserSessionsStorage.TryOpen(string user) => TryOpen(user).Cast<object, Stream>();

        public Stream DefaultSession => null;
        object IUserSessionsStorage.DefaultSession => this.DefaultSession;
    }
}
