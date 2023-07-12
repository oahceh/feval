using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Feval.Syntax;

namespace Feval
{
    public sealed class Context
    {
        #region Property

        public static Context Main { get; private set; }

        public string Name { get; }

        public SyntaxTree SyntaxTree { get; private set; }

        public IEnumerable<string> UsingNamespaces => m_UsingNamespaces;

        public IEnumerable<Assembly> ImportedAssemblies => m_ImportedAssemblies;

        public IEnumerable<VariableSymbol> Variables => GetVariables();

        #endregion

        #region Interface

        public static Context Create(string name = "", bool main = true)
        {
            if (main)
            {
                return Main ?? (Main = new Context("main"));
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new Exception("Must give a context name if you want create a not-mainly context");
            }

            // Lock for concurrent Unit-Tests initializing ONLY
            lock (m_Contexts)
            {
                if (!m_Contexts.TryGetValue(name, out var context))
                {
                    context = new Context(name);
                    m_Contexts.Add(name, context);
                }

                return context;
            }
        }

        public void Clear()
        {
            SyntaxTree = null;
            m_ImportedAssemblies.Clear();
            m_VisitedNamespaces.Clear();
            m_Symbols.Clear();
            m_UsingNamespaces.Clear();
        }

        public void RegisterDumper(Func<object, string> dumper)
        {
            if (dumper == null)
            {
                return;
            }

            ObjDumper.Dumper = dumper;
        }

        public EvaluationResult Evaluate(string text)
        {
            var ret = m_Evaluator.Evaluate(text, out var tree);
            SyntaxTree = tree;
            SetVariable(LastAnswerVariableName, ret.Value);
            return ret;
        }

        public Context WithReferences(IEnumerable<Assembly> assemblies)
        {
            foreach (var assembly in assemblies)
            {
                WithReference(assembly);
            }

            return this;
        }

        public Context WithReference(Assembly assembly)
        {
            if (!m_ImportedAssemblies.Contains(assembly))
            {
                m_ImportedAssemblies.Add(assembly);
            }

            return this;
        }

        /// <summary>
        /// 获取临时变量
        /// </summary>
        /// <param name="name">变量名</param>
        /// <returns>变量值</returns>
        public Symbol GetSymbol(string name)
        {
            return m_Symbols.TryGetValue(name, out var value) ? value : null;
        }

        /// <summary>
        /// 设置临时变量
        /// </summary>
        /// <param name="name">变量名</param>
        /// <param name="value">变量新值</param>
        public VariableSymbol SetVariable(string name, object value)
        {
            if (!m_Symbols.TryGetValue(name, out var symbol) || !(symbol is VariableSymbol variable))
            {
                variable = new VariableSymbol(name, value);
                AddSymbol(variable);
            }

            variable.Value = value;
            return variable;
        }

        public void CreateVariable(string name)
        {
            SetVariable(name, null);
        }

        public void UsingNameSpace(string nameSpace)
        {
            m_UsingNamespaces.Add(nameSpace);
        }

        public bool IsNamespace(string name)
        {
            return m_VisitedNamespaces.Contains(name) || VisitNamespace(name);
        }

        public bool TryLookupTypeOrNamespace(string name, out string @namespace, out List<Type> types)
        {
            // Type keywords
            if (SyntaxDefinition.TryGetTypeKeyword(name, out var type))
            {
                @namespace = string.Empty;
                types = new List<Type> { type };
                return true;
            }

            // Type from using namespaces
            foreach (var ns in m_UsingNamespaces)
            {
                var fullName = $"{ns}.{name}";
                types = LookupTypes(fullName);
                if (types.Count <= 0)
                {
                    continue;
                }

                @namespace = ns;
                return true;
            }

            // Namespace
            foreach (var ns in m_UsingNamespaces)
            {
                var fullName = $"{ns}.{name}";
                if (!IsNamespace(fullName))
                {
                    continue;
                }

                @namespace = fullName;
                types = new List<Type>();
                return true;
            }

            types = null;
            @namespace = null;
            return false;
        }

        public List<Type> LookupTypes(string fullName)
        {
            var ret = new List<Type>();
            foreach (var assembly in m_ImportedAssemblies)
            {
                var type = assembly.GetType(fullName);
                if (type != null)
                {
                    ret.Add(type);
                }
            }

            return ret;
        }

        #endregion

        #region Method

        private Context(string name)
        {
            Name = name;
            m_Evaluator = new Evaluator(this);
            CreateVariable(LastAnswerVariableName);
            RegisterBuiltinFunctions();
        }

        private void AddSymbol(Symbol symbol)
        {
            m_Symbols[symbol.Name] = symbol;
        }

        private void RegisterBuiltinFunctions()
        {
            foreach (var symbol in BuiltinFunctions.GetAll())
            {
                AddSymbol(symbol);
            }
        }

        private IEnumerable<VariableSymbol> GetVariables()
        {
            return m_Symbols.Where(kv => kv.Value.Type == SymbolType.Variable && kv.Key != LastAnswerVariableName)
                .Select(kv => kv.Value as VariableSymbol);
        }

        private bool VisitNamespace(string name)
        {
            foreach (var assembly in m_ImportedAssemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    var ns = type.Namespace;
                    if (string.IsNullOrEmpty(ns))
                    {
                        continue;
                    }

                    if (!m_VisitedNamespaces.Contains(ns))
                    {
                        m_VisitedNamespaces.Add(ns);
                    }

                    if (ns.Contains(name))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion

        #region Field

        private readonly Evaluator m_Evaluator;

        private static readonly Dictionary<string, Context> m_Contexts = new Dictionary<string, Context>();

        private readonly HashSet<Assembly> m_ImportedAssemblies = new HashSet<Assembly>();

        private readonly HashSet<string> m_VisitedNamespaces = new HashSet<string>();

        private readonly Dictionary<string, Symbol> m_Symbols = new Dictionary<string, Symbol>();

        private readonly HashSet<string> m_UsingNamespaces = new HashSet<string>();

        private const string LastAnswerVariableName = "ans";

        #endregion
    }
}