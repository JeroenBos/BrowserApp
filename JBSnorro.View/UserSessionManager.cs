using JBSnorro.Diagnostics;
using JBSnorro.Logging;
using JBSnorro.View.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace JBSnorro.View
{
    public interface IAppViewModel : INotifyPropertyChanged
    {
        CommandManager CommandManager { get; }
    }
    /// <summary>
    /// A delegate that creates a view model root and populates the command manager with commands.
    /// </summary>
    public delegate IAppViewModel ViewModelFactoryDelegate<in TData>(TData data);
    public sealed class UserSessionManager
    {
#if DEBUG
        private readonly bool alwaysReturnNewUserSession = true;
#else
        private readonly bool alwaysReturnNewUserSession = false;
#endif

        private readonly Type storedUserSessionsDataType;
        private readonly IUserSessionsStorage storedUserSessions;
        private readonly ViewModelFactoryDelegate<object> viewModelFactory;
        //TODO: implement cache retention
        private readonly Dictionary<string, UserSession> cachedSessions = new Dictionary<string, UserSession>();
        private readonly ILogger logger;

        public static UserSessionManager Create<TData>(IUserSessionsStorage<TData> userSessionStorage,
                                                       ViewModelFactoryDelegate<TData> createInitialViewModelRoot,
                                                       ILogger logger)
        {
            return new UserSessionManager(userSessionStorage, data => createInitialViewModelRoot((TData)data), logger);
        }
        private UserSessionManager(IUserSessionsStorage userSessionsStorage,
                                   ViewModelFactoryDelegate<object> createInitialViewModelRoot,
                                   ILogger logger,
                                   Type createInitiViewModelTypeArg)
            : this(userSessionsStorage, createInitialViewModelRoot, logger)
        {
            Contract.Requires(createInitialViewModelRoot != null);

            this.storedUserSessionsDataType = createInitiViewModelTypeArg;
        }
        public UserSessionManager(IUserSessionsStorage userSessionsStorage, ViewModelFactoryDelegate<object> createInitialViewModelRoot, ILogger logger)
        {
            Contract.Requires(userSessionsStorage != null);
            Contract.Requires(createInitialViewModelRoot != null);
            Contract.Requires(logger != null);

            this.storedUserSessions = userSessionsStorage;
            this.viewModelFactory = createInitialViewModelRoot;
            this.logger = logger;
            this.storedUserSessionsDataType = typeof(object);
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
            return Task.FromResult<UserSession>(null);
        }
        public async Task<UserSession> GetStoredSessionAsync(ClaimsPrincipal user)
        {
            var userSessionData = await this.storedUserSessions.TryOpen(getStorageUserIdentifier(user));
            if (userSessionData != null)
            {
                return createNewUserSession(userSessionData, user);
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
            if (!alwaysReturnNewUserSession)
            {
                var retrievedSession = await GetCachedOrStoredSessionAsync(user);
                if (retrievedSession != null)
                {
                    return retrievedSession;
                }
            }
            return createNewUserSession(this.storedUserSessions.DefaultSession, user);
        }
        private UserSession createNewUserSession(object userSessionData, ClaimsPrincipal user)
        {
            var result = new UserSession(this.viewModelFactory(userSessionData), this.logger);
            this.cachedSessions[getStorageUserIdentifier(user)] = result;
            return result;
        }
        private static string getStorageUserIdentifier(ClaimsPrincipal user)
        {
            return user?.Identity?.Name ?? "";
        }
    }
}
