using JBSnorro.Diagnostics;
using JBSnorro.View;
using JBSnorro.View.Controllers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JBSnorro.View.App.Controllers
{
    [Route("api/[controller]")]
    public class ChangesController : Controller
    {
        private readonly UserSessionManager userSessionManager;
        public ChangesController(UserSessionManager userSessionManager)
        {
            Contract.Requires(userSessionManager != null);

            this.userSessionManager = userSessionManager;
        }

        [HttpPost("[action]")]
        public async Task<object> Open()
        {
            var result = await this.userSessionManager.Open(this.User);
            return result;
        }
        [HttpPost("[action]")]
        public async Task<object> ExecuteCommand([FromBody] CommandInstruction instruction)
        {
            var correctedInstruction = instruction.WithCSharpCommandName();

            var result = await this.userSessionManager.ExecuteCommand(this.User, correctedInstruction);
            return result;
        }
        [HttpPost("[action]")]
        public async Task<object> RegisterRequest()
        {
            var result = await this.userSessionManager.RegisterRequest(this.User);
            return result;
        }
    }
}