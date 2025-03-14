﻿using System.Reflection;

namespace Feval
{
    internal sealed class FunctionSymbol : Symbol
    {
        public override SymbolType Type => SymbolType.Function;

        public MethodInfo MethodInfo { get; }

        public FunctionSymbol(string name, MethodInfo methodInfo) : base(name)
        {
            MethodInfo = methodInfo;
        }
    }
}