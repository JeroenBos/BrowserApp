using BrowserApp;
using BrowserApp.POCOs;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BrowserApp.Controllers
{
    [Route("api/[controller]")]
    public class ChangesController : Controller
    {
        private readonly UserSessionManager userSessionManager;
        public ChangesController(UserSessionManager userSessionManager)
        {
            if (userSessionManager == null) { throw new ArgumentNullException(nameof(userSessionManager)); }

            this.userSessionManager = userSessionManager;
        }

        [HttpPost("[action]")]
        public async Task<object> Open()
        {
            var userSession = await this.userSessionManager.GetOrCreateSessionAsync(this.User);
            SpecificCode.Initialize(userSession.CommandManager);
            userSession.Flush();  // to prevent double entries in complete state
            userSession.View.AddCompleteStateAsChanges(userSession.ViewModelRoot);
            return userSession.Flush();
        }
        [HttpPost("[action]")]
        public async Task<object> ExecuteCommand([FromBody] CommandInstruction instruction)
        {
            var userSession = await this.userSessionManager.GetOrCreateSessionAsync(this.User);
            SpecificCode.Initialize(userSession.CommandManager); // because alwaysReturnNewUserSessions
            instruction.CommandName = IdentifierViewBinding.ToCSharpIdentifier(instruction.CommandName);
            await userSession.ExecuteCommand(instruction, this.User);
            return await userSession.FlushOrWait();
        }
        [HttpPost("[action]")]
        public async Task<object> RegisterRequest()
        {
            var userSession = await this.userSessionManager.GetOrCreateSessionAsync(User);
            SpecificCode.Initialize(userSession.CommandManager); // because alwaysReturnNewUserSessions
            return await userSession.FlushOrWait();
        }
    }
}