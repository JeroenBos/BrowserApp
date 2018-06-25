using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public Task<object> ExecuteCommand()
        {
            return RegisterRequest();
        }
        [HttpPost("[action]")]
        public async Task<object> RegisterRequest()
        {
            await Task.Delay(1000); // TODO: remove
            var userSession = await this.userSessionManager.GetOrCreateSessionAsync(User);
            var result = userSession.Flush();
            return result;
        }
    }
}