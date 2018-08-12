using BrowserApp.POCOs;
using BrowserApp.Tests.Mocks;
using JBSnorro.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserApp.Tests
{
    [TestClass]
    public class CommandManagerTests
    {
        CommandManager commandManager;
        List<Change> changes;
        View view;
        Logger logger;

        [TestInitialize]
        public void Initialize()
        {
            this.logger = new Logger();
            this.commandManager = new CommandManager();
            this.changes = new List<Change>();
            this.view = new View(commandManager, changes.Add, new IdProvider());
        }
        public CommandManagerTests()
        {
            this.Initialize();
        }


        [TestMethod]
        public void TestCommandManagerAdd()
        {
            Assert.AreEqual(1, changes.Count);
        }
        [TestMethod]
        public void TestSimpleCommandRegistration()
        {
            commandManager.Add(new MockCommand(logger), typeof(object));

            Assert.AreEqual(1, commandManager.Count);
        }
    }
}
