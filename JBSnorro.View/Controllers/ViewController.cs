using JBSnorro.Diagnostics;
using JBSnorro.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace JBSnorro.View.Controllers
{
    public static class ViewControllerExtension
    {
        public static async Task<object> Open(this UserSessionManager userSessionManager, ClaimsPrincipal user)
        {
            var userSession = await userSessionManager.GetOrCreateSessionAsync(user);
            // SpecificCode.Initialize(userSession.CommandManager);
            userSession.Flush();  // to prevent double entries in complete state
            userSession.View.AddCompleteStateAsChanges(userSession.ViewModelRoot);
            return userSession.Flush();
        }
        public static async Task<object> ExecuteCommand(this UserSessionManager userSessionManager,
                                                        ClaimsPrincipal user,
                                                        CommandInstruction instruction)
        {
            var userSession = await userSessionManager.GetOrCreateSessionAsync(user);
            instruction.CommandName = IdentifierViewBinding.ToCSharpIdentifier(instruction.CommandName);
            await userSession.ExecuteCommand(instruction, user);
            return await userSession.FlushOrWait();
        }
        public static async Task<object> RegisterRequest(this UserSessionManager userSessionManager,
                                                         ClaimsPrincipal user)
        {
            var userSession = await userSessionManager.GetOrCreateSessionAsync(user);
            return await userSession.FlushOrWait();
        }
    }
}