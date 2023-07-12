namespace Feval
{
    public sealed class VariableSymbol : Symbol
    {
        public override SymbolType Type => SymbolType.Variable;

        public object Value { get; set; }

        public VariableSymbol(string name, object value) : base(name)
        {
            Value = value;
        }
    }
}