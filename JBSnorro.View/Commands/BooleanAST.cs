using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.View.Commands
{
    public abstract class BooleanAST
    {
        public delegate bool GetFlagValue(object viewModel, object eventArgs);
        public static BooleanAST True => Commands.True.Singleton;
        public static BooleanAST Parse(string s, IReadOnlyDictionary<string, GetFlagValue> flags)
        {
            Contract.Requires(s != null);
            Contract.Requires(flags != null);

            s = s.Trim();
            if (s == "")
                return True;

            return ParseImpl(s, flags);
        }
        internal static BooleanAST ParseImpl(string s, IReadOnlyDictionary<string, GetFlagValue> flags)
        {
            s = s.Trim();

            int firstOrIndex = s.IndexOf("||");
            int firstAndIndex = s.IndexOf("&&");

            if (firstOrIndex != -1 || firstAndIndex != -1)
            {
                BooleanAST lhs = ParseImpl(s.Substring(0, firstOrIndex), flags);
                BooleanAST rhs = ParseImpl(s.Substring(firstOrIndex + 2), flags);

                if (firstOrIndex != -1 && firstOrIndex < firstAndIndex)
                {
                    return new Or(lhs, rhs);
                }
                else
                {
                    return new And(lhs, rhs);
                }
            }
            if (s[0] == '!')
            {
                return new Not(ParseImpl(s.Substring(1), flags));
            }

            if (flags.ContainsKey(s))
                return new Flag(s, flags);

            throw new ArgumentException($"Parsing '{s}' failed", nameof(s));
        }
        public abstract bool Evaluate(object viewModel, object eventArgs);
    }
    sealed class And : BooleanAST
    {
        private readonly BooleanAST lhs;
        private readonly BooleanAST rhs;
        public And(BooleanAST lhs, BooleanAST rhs)
            => (this.lhs, this.rhs) = (lhs, rhs);
        public override bool Evaluate(object viewModel, object eventArgs)
        {
            return rhs.Evaluate(viewModel, eventArgs) && lhs.Evaluate(viewModel, eventArgs);
        }

    }
    sealed class Or : BooleanAST
    {
        private readonly BooleanAST lhs;
        private readonly BooleanAST rhs;
        public Or(BooleanAST lhs, BooleanAST rhs)
            => (this.lhs, this.rhs) = (lhs, rhs);
        public override bool Evaluate(object viewModel, object eventArgs)
        {
            return rhs.Evaluate(viewModel, eventArgs) || lhs.Evaluate(viewModel, eventArgs);
        }
    }
    sealed class Not : BooleanAST
    {
        private readonly BooleanAST operand;
        public Not(BooleanAST operand) => this.operand = operand;
        public override bool Evaluate(object viewModel, object eventArgs)
        {
            return !operand.Evaluate(viewModel, eventArgs);
        }
    }
    sealed class Flag : BooleanAST
    {
        private readonly string name;
        private readonly IReadOnlyDictionary<string, GetFlagValue> flags;

        public Flag(string name, IReadOnlyDictionary<string, GetFlagValue> flags)
            => (this.name, this.flags) = (name, flags);

        public override bool Evaluate(object viewModel, object eventArgs)
        {
            if (!this.flags.TryGetValue(this.name, out var getFlag))
            {
                throw new Exception($"A flag with the name '{this.name}' was not found");
            }

            return getFlag(viewModel, eventArgs);
        }
    }
    sealed class True : BooleanAST
    {
        public static True Singleton { get; } = new True();
        private True() { }
        public override bool Evaluate(object viewModel, object eventArgs)
        {
            return true;
        }
    }
}
