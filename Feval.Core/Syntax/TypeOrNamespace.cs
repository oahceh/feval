using System;
using System.Collections.Generic;
using System.Reflection;

namespace Feval
{
    public class TypeOrNamespace
    {
        public string Namespace { get; set; }

        public List<Type> Types { get; set; }

        public string ToBindName { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Namespace))
            {
                return $"<Namespace: {Namespace}>";
            }

            if (Types.Count > 0)
            {
                return Types[0].ToString();
            }

            return "Unknown";
        }
    }
}