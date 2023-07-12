namespace Feval
{
    public abstract class Symbol
    {
        public abstract SymbolType Type { get; }

        public string Name { get; }

        protected Symbol(string name)
        {
            Name = name;
        }
    }
}