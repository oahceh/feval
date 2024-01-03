using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Feval
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class BuiltinAttribute : Attribute
    {
        public string Name { get; }

        public string Help { get; }

        public BuiltinAttribute(string name, string help = "")
        {
            Name = name;
            Help = help;
        }
    }

    internal sealed class BuiltinFunctionResult
    {
        public object Value { get; }

        public BuiltinFunctionResult(object value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    internal static class BuiltinFunctions
    {
        internal static IEnumerable<FunctionSymbol> GetAll()
        {
            return typeof(BuiltinFunctions).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .Where(f => f.GetCustomAttribute<BuiltinAttribute>() != null).Select(f =>
                    new FunctionSymbol(f.GetCustomAttribute<BuiltinAttribute>().Name, f));
        }

        #region Built-in Functions

        private static IReadOnlyList<BuiltinAttribute> GetAllBuiltinAttributes()
        {
            return typeof(BuiltinFunctions).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .Select(method => method.GetCustomAttribute<BuiltinAttribute>())
                .Where(attribute => attribute != null).ToList();
        }

        [Builtin("help", "Display the help messages.")]
        private static BuiltinFunctionResult Help()
        {
            var attributes = GetAllBuiltinAttributes();
            var builder = new StringBuilder();
            builder.AppendLine(Version().Value as string);
            builder.AppendLine(Copyright().Value as string);
            for (var i = 0; i < attributes.Count; i++)
            {
                var attribute = attributes[i];
                builder.Append($"  {attribute.Name,-10}\t{attribute.Help,-10}");
                if (i != attributes.Count - 1)
                {
                    builder.AppendLine();
                }
            }

            return new BuiltinFunctionResult(builder.ToString());
        }

        [Builtin("dump",
            "Dump the given object with the dumper registered by Context.RegisterDumper or Object.ToString by default.")]
        private static BuiltinFunctionResult Objdump(object obj)
        {
            return new BuiltinFunctionResult(ObjDumper.Dump(obj));
        }

        [Builtin("usings", "List all using namespaces.")]
        private static BuiltinFunctionResult Usings()
        {
            var namespaces = string.Join("\n", Context.Main.UsingNamespaces);
            return new BuiltinFunctionResult($"Using Namespaces:\n{namespaces}");
        }

        [Builtin("vars", "List all declared local variables in current context.")]
        private static BuiltinFunctionResult Vars()
        {
            var variables = string.Join("\n",
                Context.Main.Variables.Select(variable => $"{variable.Name} -> {variable.Value}"));
            return new BuiltinFunctionResult($"Local Variables:\n{variables}");
        }

        [Builtin("assemblies", "List all referenced assemblies.")]
        private static BuiltinFunctionResult Assemblies()
        {
            var assemblies = string.Join("\n", Context.Main.ImportedAssemblies.Select(assembly => assembly.FullName));
            return new BuiltinFunctionResult($"Referenced Assemblies:\n{assemblies}");
        }

        [Builtin("version", "Display version information.")]
        private static BuiltinFunctionResult Version()
        {
            var assembly = typeof(Evaluator).Assembly;
            var assemblyName = assembly.GetName();
            var version = assemblyName.Version;
            var builder = new StringBuilder();
            builder.Append(assemblyName.Name);
            builder.Append(" ");
            builder.Append($"{version.Major}.{version.Minor}.{version.Build}");
            return new BuiltinFunctionResult(builder.ToString());
        }

        [Builtin("copyright", "Display copyright information")]
        private static BuiltinFunctionResult Copyright()
        {
            var assembly = typeof(Evaluator).Assembly;
            var assemblyName = assembly.GetName();
            var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;
            var builder = new StringBuilder();
            builder.Append(copyright);
            builder.Append(" ");
            builder.Append(assemblyName.Name);
            return new BuiltinFunctionResult(builder.ToString());
        }

        #endregion
    }
}