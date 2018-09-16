using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace JBSnorro.View.Tests
{
    [TestClass]
    public class ViewBindingAsTests
    {
        class A : INotifyPropertyChanged
        {
            [ViewBindingAsTest]
            public object MyObj { get; } = new object();

            public event PropertyChangedEventHandler PropertyChanged
            {
                add { }
                remove { }
            }
        }
        class ViewBindingAsTestAttribute : ViewBindingAsAttribute
        {
            public static readonly object singleton = 1;
            protected override object createSubstitute(object obj)
            {
                return singleton;
            }
        }
        [TestMethod]
        public void SubstitutionWhenKeyIsReferenceType()
        {
            var changes = new List<Change>();
            var viewModel = new A();
            new View(viewModel, changes.Add, new IdProvider()).AddCompleteStateAsChanges(viewModel);

            Assert.AreEqual(1, changes.Count);
            Assert.IsInstanceOfType(changes[0], typeof(PropertyChange));
            Assert.AreEqual(0, changes[0].Id);
            Assert.AreEqual((int)ViewBindingAsTestAttribute.singleton, ((PropertyChange)changes[0]).Value);
        }
        class B : INotifyPropertyChanged
        {
            [ViewBindingAsTest2]
            public object MyObj { get; } = 4;

            public event PropertyChangedEventHandler PropertyChanged
            {
                add { }
                remove { }
            }
        }
        class ViewBindingAsTest2Attribute : ViewBindingAsAttribute
        {
            public static readonly object singleton = 3;
            protected override object createSubstitute(object obj)
            {
                return singleton;
            }
        }
        [TestMethod]
        public void SubstitutionWhenKeyIsValueType()
        {
            var changes = new List<Change>();
            var viewModel = new B();
            new View(viewModel, changes.Add, new IdProvider()).AddCompleteStateAsChanges(viewModel);

            Assert.AreEqual(1, changes.Count);
            Assert.IsInstanceOfType(changes[0], typeof(PropertyChange));
            Assert.AreEqual(0, changes[0].Id);
            Assert.AreEqual((int)ViewBindingAsTest2Attribute.singleton, ((PropertyChange)changes[0]).Value);
        }
    }

}
