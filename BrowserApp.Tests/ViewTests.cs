using BrowserApp.POCOs;
using JBSnorro.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserApp.Tests
{
    [TestClass]
    public class ViewTests
    {
        [TestMethod]
        public void ViewAddsRootToChanges()
        {
            var logger = new Logger();
            var commandManager = new CommandManager();
            var changes = new List<Change>();
            var view = new View(commandManager, changes.Add, new IdProvider());

            Assert.AreEqual(1, changes.Count);
            Assert.IsInstanceOfType(changes[0], typeof(PropertyChange));
        }
    }
}
