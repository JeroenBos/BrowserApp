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
        public Task<object> Open()
        {
            return this.userSessionManager.Open(this.User);
        }
        [HttpPost("[action]")]
        public Task<object> ExecuteCommand([FromBody] CommandInstruction instruction)
        {
            var correctedInstruction = instruction.WithCSharpCommandName();

            return this.userSessionManager.ExecuteCommand(this.User, correctedInstruction);
        }
        [HttpPost("[action]")]
        public Task<object> RegisterRequest()
        {
            return this.userSessionManager.RegisterRequest(this.User);
        }
    }
}