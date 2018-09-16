using JBSnorro;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using JBSnorro.Logging;
using JBSnorro.View.App;
using JBSnorro.View.Commands;
using JBSnorro.View.Tests.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace JBSnorro.View.Tests
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
        public void CommandManagerConstruction()
        {
            SpecificCode.Initialize(this.commandManager);
            this.changes.Clear();
            view.AddCompleteStateAsChanges(this.commandManager);

            Assert.AreEqual(3, this.changes.Count);
            Assert.IsInstanceOfType(this.changes[0], typeof(PropertyChange));
            Assert.AreEqual(nameof(CommandManager.Commands).ToFirstLower(), ((PropertyChange)this.changes[0]).PropertyName);
            Assert.IsInstanceOfType(this.changes[1], typeof(PropertyChange));
            Assert.AreEqual("increment", ((PropertyChange)this.changes[1]).PropertyName);
            Assert.IsInstanceOfType(this.changes[2], typeof(PropertyChange));
            Assert.AreEqual(nameof(CommandViewModel.Name).ToFirstLower(), ((PropertyChange)this.changes[2]).PropertyName);
            Assert.AreEqual("increment", ((PropertyChange)this.changes[2]).Value);
        }
        [TestMethod]
        public void NoInitialChanges()
        {
            Assert.AreEqual(0, changes.Count);
        }
        [TestMethod]
        public void TestCommandManagerAdd()
        {
            view.AddCompleteStateAsChanges(this.commandManager);
            Assert.AreEqual(1, changes.Count);
        }
        [TestMethod]
        public void TestSimpleCommandRegistration()
        {
            commandManager.Add(new MockCommand(logger), "test");

            Assert.AreEqual(1, commandManager.Count);
        }

        [TestMethod]
        public void TestExecuteIncrementCommandIncrementsCounter()
        {
            var counter = new CounterViewModel();
            SpecificCode.Initialize(this.commandManager);
            this.commandManager.Execute(null, "Increment", counter, new object());

            Assert.AreEqual(1, counter.CurrentCount);
        }

    }
}
