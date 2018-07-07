﻿using JBSnorro.Diagnostics;
using JBSnorro.Logging;
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
        //TODO: implement cache retention
        private readonly Dictionary<string, UserSession> cachedSessions = new Dictionary<string, UserSession>();
        private readonly ILogger logger;

        public UserSessionManager(IUserSessionsStorage userSessionsStorage, ViewModelFactoryDelegate createInitialViewModelRoot, ILogger logger)
        {
            Contract.Requires(userSessionsStorage != null);
            Contract.Requires(createInitialViewModelRoot != null);
            Contract.Requires(logger != null);

            this.storedUserSessions = userSessionsStorage;
            this.viewModelFactory = createInitialViewModelRoot;
            this.logger = logger;
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
            Stream userSessionData = await this.storedUserSessions.TryOpen(getStorageUserIdentifier(user));
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
            var retrievedSession = await GetCachedOrStoredSessionAsync(user);
            if (retrievedSession != null)
            {
                return retrievedSession;
            }

            const Stream defaultSession = null;
            return createNewUserSession(defaultSession, user);
        }
        private UserSession createNewUserSession(Stream userSessionData, ClaimsPrincipal user)
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
