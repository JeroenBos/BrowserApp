using JBSnorro.Diagnostics;
using JBSnorro.Logging;
using JBSnorro.View.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace JBSnorro.View.App.Tests

{
    [TestClass]
    public class CounterTests
    {
        SpecificAppViewModel root;
        List<Change> changes;
        View view;
        Logger logger;
        IIdProvider idProvider;
        CommandManager commandManager => root.CommandManager;
        CounterViewModel counter => root.Counter;

        [TestInitialize]
        public void Initialize()
        {
            this.root = SpecificCode.getRoot_TODO_ToBeProvidedByExtension(null);
            SpecificCode.Initialize(commandManager);
            this.logger = new Logger();
            this.changes = new List<Change>();
            this.idProvider = new IdProvider();
            this.view = new View(root, changes.Add, this.idProvider);
        }

        public CounterTests() => Initialize();
        [TestMethod]
        public void TestExecuteIncrementCommandResponse()
        {
            Contract.Assert(this.changes.Count == 0);

            this.commandManager.Execute(null, "Increment", counter, new object());

            Assert.AreEqual(1, this.changes.Count);
            Assert.IsInstanceOfType(this.changes[0], typeof(PropertyChange));
            var change = this.changes[0] as PropertyChange;
            Assert.AreEqual(idProvider[counter], change.Id);
            Assert.AreEqual("currentCount", change.PropertyName);

        }
    }
}
