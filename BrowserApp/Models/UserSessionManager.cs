using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BrowserApp
{
    public delegate object ViewModelFactoryDelegate(Stream stream);
    public sealed class UserSessionManager
    {
        private readonly IUserSessionsStorage storedUserSessions;
        private readonly ViewModelFactoryDelegate viewModelFactory;
        private readonly Dictionary<string, UserSession> cachedSessions = new Dictionary<string, UserSession>();

        public UserSessionManager(IUserSessionsStorage userSessionsStorage, ViewModelFactoryDelegate createInitialViewModelRoot)
        {
            if (userSessionsStorage == null) { throw new ArgumentNullException(nameof(userSessionsStorage)); }
            if (createInitialViewModelRoot == null) { throw new ArgumentNullException(nameof(createInitialViewModelRoot)); }

            this.storedUserSessions = userSessionsStorage;
            this.viewModelFactory = createInitialViewModelRoot;
        }

        public Task<UserSession> GetCachedSessionAsync(ClaimsPrincipal user)
        {
            if (user == null)
            {
                return null;
            }
            if (cachedSessions.TryGetValue(getStorageUserIdentifier(user), out var session))
            {
                return Task.FromResult(session);
            }
            return null;
        }
        public async Task<UserSession> GetStoredSessionAsync(ClaimsPrincipal user)
        {
            Stream userSessionData = await this.storedUserSessions.TryOpen(getStorageUserIdentifier(user));
            if (userSessionData != null)
            {
                return new UserSession(this.viewModelFactory, userSessionData);
            }
            return null;
        }
        public async Task<UserSession> GetCachedOrStoredSessionAsync(ClaimsPrincipal user)
        {
            var cachedSession = await GetCachedSessionAsync(user);
            if (cachedSession != null)
            {
                return cachedSession;
            }

            return await GetStoredSessionAsync(user);
        }
        public async Task<UserSession> GetOrCreateSessionAsync(ClaimsPrincipal user)
        {
            var retrievedSession = await GetCachedOrStoredSessionAsync(user);
            if (retrievedSession != null)
            {
                return retrievedSession;
            }

            return new UserSession(this.viewModelFactory);
        }
        private static string getStorageUserIdentifier(ClaimsPrincipal user)
        {
            return user?.Identity?.Name;
        }
    }
}
