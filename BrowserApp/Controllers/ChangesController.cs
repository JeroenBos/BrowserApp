using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BrowserApp;
using BrowserApp.POCOs;
using Microsoft.AspNetCore.Mvc;

namespace BrowserApp.Controllers
{
    [Route("api/[controller]")]
    public class ChangesController : Controller
    {
        private readonly UserSessionManager userSessionManager;
        public ChangesController(UserSessionManager userSessionManager)
        {
            if(userSessionManager == null) { throw new ArgumentNullException(nameof(userSessionManager)); }

            this.userSessionManager = userSessionManager;
        }

        [HttpPost("[action]")]
        public async Task<object> Open()
        {
            var userSession = await this.userSessionManager.GetOrCreateSessionAsync(this.User);
            userSession.View.AddCompleteStateAsChanges(userSession.ViewModelRoot);
            return userSession.Flush();
        }
        [HttpPost("[action]")]
        public async Task<object> ExecuteCommand(CommandInstruction instruction)
        {
            var userSession = await this.userSessionManager.GetOrCreateSessionAsync(this.User);
            userSession.ExecuteCommand(instruction, this.User);
            return await userSession.FlushOrWait();
        }
        [HttpPost("[action]")]
        public async Task<object> RegisterRequest()
        {
            var userSession = await this.userSessionManager.GetOrCreateSessionAsync(User);
            return await userSession.FlushOrWait();
        }
    }
}