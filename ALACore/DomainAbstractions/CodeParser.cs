using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;
using ProgrammingParadigms;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>A generic parser for any compilable C# code. The expected usage is that a chain of CodeParser instances would be used to extract the information required.</para>
    /// <para>For example:</para>
    /// <para>From an input Data&lt;string&gt;, codeData, that contains some compilable code, the constructor parameters from its first class' constructor will be sent to an output
    /// Data&lt;string&gt;, resultData:</para>
    /// <code>var classParser = new CodeParser();</code>
    /// <code>var methodParser = new CodeParser();</code>
    /// <code>var parameterParser = new CodeParser();</code>
    /// <code>codeData.WireTo(classParser);</code>
    /// <code>classParser.WireTo(new Apply&lt;List&lt;string&gt;, string&gt;() { Lambda = _list => _list.First() }.WireTo(methodParser), "classes");</code>
    /// <code>methodParser.WireTo(new Apply&lt;List&lt;string&gt;, string&gt;() { Lambda = _list => _list.First() }.WireTo(parameterParser), "methods");</code>
    /// <code>parameterParser.WireTo(new Apply&lt;List&lt;string&gt;, string&gt;() { Lambda = _list => _list.First() }.WireTo(resultData), "parameters");</code>
    /// <para>Ports:</para>
    /// <para>1. IDataFlow&lt;string&gt; code: The compilable input code to parse.</para>
    /// <para>2. IDataFlow&lt;List&lt;string&gt;&gt; classes: A list of the full text representation of every class declared in the input code.
    /// The IntelliSense/documentation code above every class will also be preserved, e.g. between &lt;summary&gt; tags, if PreserveSurroundings is set to true.</para>
    /// <para>3. IDataFlow&lt;List&lt;string&gt;&gt; fields: A list of the full text representation of every field declared in the input code.</para>
    /// <para>4. IDataFlow&lt;List&lt;string&gt;&gt; properties: A list of the full text representation of every property declared in the input code.</para>
    /// <para>5. IDataFlow&lt;List&lt;string&gt;&gt; methods: A list of the full text representation of every method declared in the input code.</para>
    /// <para>6. IDataFlow&lt;List&lt;string&gt;&gt; parameters: A list of the full text representation of every parameter declared in the input code.</para>
    /// <para>7. IDataFlow&lt;List&lt;string&gt;&gt; documentationBlocks: A list of the full text representation of every document block (between &lt;summary&gt; tags) written in the input code.</para>
    /// </summary>
    public class CodeParser : IDataFlow<string>
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public bool PreserveSurroundings { get; set; } = false;

        // Private fields
        private string _unparsedCode = "";
        private SyntaxNode _root = null;

        // Ports
        private IDataFlow<List<string>> classes;
        private IDataFlow<List<string>> fields;
        private IDataFlow<List<string>> properties;
        private IDataFlow<List<string>> methods;
        private IDataFlow<List<string>> parameters;
        private IDataFlow<List<string>> documentationBlocks;

        // Methods
        public void Test(string code)
        {
            if (_root == null) _root = GetRoot(code);

            var classes = GetClasses(_root);
            var fields = GetFields(_root);
            var properties = GetProperties(_root);
            var methods = GetMethods(_root);
            var parameters = GetParameters(_root);

        }

        private SyntaxTree GetSyntaxTree(string code) => CSharpSyntaxTree.ParseText(code);
        private SyntaxNode GetRoot(string code) => GetSyntaxTree(code).GetRoot();

        private List<string> ExtractStrings(IEnumerable<SyntaxNode> nodes, bool preserveSurroundings = false) => nodes.Select(d => preserveSurroundings ? d.ToFullString() : d.ToString()).ToList();

        // Get classes
        public List<string> GetClasses(string code) => GetClasses(GetRoot(code));
        private List<string> GetClasses(SyntaxNode root) => ExtractStrings(root.DescendantNodes().OfType<ClassDeclarationSyntax>(), PreserveSurroundings);

        // Get fields
        public List<string> GetFields(string code) => GetFields(GetRoot(code));
        private List<string> GetFields(SyntaxNode root) => ExtractStrings(root.DescendantNodes().OfType<FieldDeclarationSyntax>(), PreserveSurroundings);

        // Get properties
        public List<string> GetProperties(string code) => GetProperties(GetRoot(code));
        private List<string> GetProperties(SyntaxNode root) => ExtractStrings(root.DescendantNodes().OfType<PropertyDeclarationSyntax>(), PreserveSurroundings);

        // Get methods
        public List<string> GetMethods(string code) => GetMethods(GetRoot(code));
        private List<string> GetMethods(SyntaxNode root) => ExtractStrings(root.DescendantNodes().OfType<MethodDeclarationSyntax>(), PreserveSurroundings);

        // Get parameters
        public List<string> GetParameters(string code) => GetParameters(GetRoot(code));
        private List<string> GetParameters(SyntaxNode root)
        {
            var parameters = new List<string>();

            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                parameters.AddRange(ExtractStrings(method.ParameterList.Parameters));
            }

            return parameters;
        }

        // Get documentation blocks
        //TODO

        // IDataFlow<string> implementation
        string IDataFlow<string>.Data
        {
            get => _unparsedCode;
            set
            {
                _unparsedCode = value;
                _root = GetRoot(_unparsedCode);

                try
                {
                    if (classes != null) classes.Data = GetClasses(_root);
                    if (fields != null) fields.Data = GetFields(_root);
                    if (properties != null) properties.Data = GetProperties(_root);
                    if (methods != null) methods.Data = GetMethods(_root);
                    if (parameters != null) parameters.Data = GetParameters(_root);
                }
                catch (Exception e)
                {
                    Logging.Log($"Failed to parse code in CodeParser {InstanceName}.\nInput code:\n{_unparsedCode}\nError message:\n{e.Message}");
                }
            }
        }

        public CodeParser()
        {

        }
    }
}
