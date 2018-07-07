using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
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
        public async Task<object> ExecuteCommand()
        {
            var userSession = await this.userSessionManager.GetOrCreateSessionAsync(User);
            userSession.ExecuteCommand(new DummyCommand(userSession));
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
class DummyCommand : ICommand
{
    private UserSession userSession;
    public DummyCommand(UserSession userSession)
    {
        this.userSession = userSession;
    }
    public event EventHandler CanExecuteChanged;

    public bool CanExecute(object parameter)
    {
        return true;
    }

    public void Execute(object parameter)
    {
        System.Threading.Thread.Sleep(3000);
        userSession.RegisterChange(PropertyChange.Create(0, "prop", 1));
    }
}