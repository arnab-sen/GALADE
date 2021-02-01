using System;
using System.Collections.Generic;
using System.IO;
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
    /// <para>From an input Data&lt;string&gt;, codeData, that contains some compilable code, the constructor parameters from its first class' first found method will be sent to an output
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
    /// <para>3. IDataFlow&lt;List&lt;string&gt;&gt; interfaces: A list of the full text representation of every interface declared in the input code.</para>
    /// <para>4. IDataFlow&lt;List&lt;string&gt;&gt; baseObjects: A list of the full text representation of every base object
    /// (i.e. inherited base classes or implemented interfaces) declared in the input code.</para>
    /// <para>5. IDataFlow&lt;List&lt;string&gt;&gt; enums: A list of the full text representation of every enum declared in the input code.</para>
    /// <para>6. IDataFlow&lt;List&lt;string&gt;&gt; fields: A list of the full text representation of every field declared in the input code.</para>
    /// <para>7. IDataFlow&lt;List&lt;string&gt;&gt; properties: A list of the full text representation of every property declared in the input code.</para>
    /// <para>8. IDataFlow&lt;List&lt;string&gt;&gt; methods: A list of the full text representation of every method declared in the input code.</para>
    /// <para>9. IDataFlow&lt;List&lt;string&gt;&gt; parameters: A list of the full text representation of every parameter declared in the input code.</para>
    /// <para>10. IDataFlow&lt;List&lt;string&gt;&gt; documentationBlocks: A list of the full text representation of every document block (between &lt;summary&gt; tags) written in the input code.</para>
    /// </summary>
    public class CodeParser : IDataFlow<string> // codeInput
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        
        public string AccessLevel // private, public, protected, or any (invalid values will also be set to any)
        {
            get => _accessLevel;
            set
            {
                var level = value.ToLower();
                _accessLevel = _accessLevels.Contains(level) ? level : "any";
            }
        }

        // Private fields
        private string _unparsedCode = "";
        private SyntaxNode _root = null;
        private readonly HashSet<string> _accessLevels = new HashSet<string>() { "any", "public", "protected", "private" };
        private string _accessLevel = "any";

        // Ports
        private IDataFlow<string> name;
        private IDataFlow<List<string>> members;
        private IDataFlow<List<string>> classes;
        private IDataFlow<List<string>> interfaces;
        private IDataFlow<List<string>> baseObjects;
        private IDataFlow<List<string>> enums;
        private IDataFlow<List<string>> fields;
        private IDataFlow<List<string>> properties;
        private IDataFlow<List<string>> methods;
        private IDataFlow<List<string>> parameters;
        private IDataFlow<List<string>> documentationBlocks;

        // Methods
        public void Test(string code, string projectPath = "")
        {
            // if (_root == null) _root = GetRoot(code);
            //
            // // Get every mention of mainWindow
            // // mainWindow declaration:
            // var mwDec = _root.DescendantNodes().OfType<VariableDeclaratorSyntax>()
            //     .FirstOrDefault(v => v.Identifier.Text == "mainWindow")?.Identifier.Text;
            //
            // // All occurrences of mainWindow instantiation or wiring
            // var mw = _root.DescendantNodes()
            //     .Where(n => n is VariableDeclaratorSyntax | n is MemberAccessExpressionSyntax)
            //     .Where(n => n.ToString().Contains("mainWindow")).ToList();
            //
            // var c = GetClasses(_root).First();
            // var implemented = GetBaseObjects(_root);
            //
            // // Test getting all interfaces in ProgrammingParadigms
            // var programmingParadigmsPath = Path.Combine(projectPath, "ProgrammingParadigms");
            // var domainAbstractionsPath = Path.Combine(projectPath, "DomainAbstractions");
            //
            // var paradigmPaths = Directory.GetFiles(programmingParadigmsPath);
            // var abstractionPaths = Directory.GetFiles(domainAbstractionsPath);
            //
            // var interfaces = new List<string>();
            // // Get programmiung paradigms
            // foreach (var paradigmPath in paradigmPaths)
            // {
            //     var fileContent = Utilities.ReadFileSafely(paradigmPath);
            //     interfaces.AddRange(ExtractStrings(GetInterfaces(fileContent)));
            // }
        }

        private List<string> GenerateOutput(SyntaxNode root, Func<SyntaxNode, IEnumerable<SyntaxNode>> nodeExtractor)
        {
            var result = new List<string>();
            var nodes = nodeExtractor(root);

            nodes = FilterByAccessLevel(nodes, accessLevel: AccessLevel);

            result = ExtractStrings(nodes);

            return result;
        }

        public IEnumerable<SyntaxNode> FilterByAccessLevel(IEnumerable<SyntaxNode> nodes, string accessLevel = "any")
        {
            IEnumerable<SyntaxNode> result = default;

            if (accessLevel != "any")
            {
                if (accessLevel == "private")
                {
                    result = nodes.Where(node => ((MemberDeclarationSyntax)node).Modifiers.Any(SyntaxKind.PrivateKeyword));
                }
                else if (accessLevel == "protected")
                {
                    result = nodes.Where(node => ((MemberDeclarationSyntax)node).Modifiers.Any(SyntaxKind.ProtectedKeyword));
                }
                else if (accessLevel == "public")
                {
                    result = nodes.Where(node => ((MemberDeclarationSyntax)node).Modifiers.Any(SyntaxKind.PublicKeyword));
                }
            }
            else
            {
                result = nodes;
            }

            return result;
        }

        public SyntaxTree GetSyntaxTree(string code) => CSharpSyntaxTree.ParseText(code);
        public SyntaxNode GetRoot(string code) => GetSyntaxTree(code).GetRoot();

        public List<string> ExtractStrings(IEnumerable<SyntaxNode> nodes, bool preserveSurroundings = false) => nodes.Select(d => preserveSurroundings ? d.ToFullString() : d.ToString()).ToList();

        // Get all members
        public IEnumerable<MemberDeclarationSyntax> GetMembers(string code) => GetMembers(GetRoot(code));
        public IEnumerable<MemberDeclarationSyntax> GetMembers(SyntaxNode root) => root.DescendantNodes().OfType<MemberDeclarationSyntax>();

        // Get classes
        public IEnumerable<ClassDeclarationSyntax> GetClasses(string code) => GetClasses(GetRoot(code));
        public IEnumerable<ClassDeclarationSyntax> GetClasses(SyntaxNode root) => root.DescendantNodes().OfType<ClassDeclarationSyntax>();

        // Get interfaces
        public IEnumerable<InterfaceDeclarationSyntax> GetInterfaces(string code) => GetInterfaces(GetRoot(code));
        public IEnumerable<InterfaceDeclarationSyntax> GetInterfaces(SyntaxNode root) => root.DescendantNodes().OfType<InterfaceDeclarationSyntax>();

        // Get base classes and interfaces
        public IEnumerable<BaseListSyntax> GetBaseObjects(string code) => GetBaseObjects(GetRoot(code));
        public IEnumerable<BaseListSyntax> GetBaseObjects(SyntaxNode root) => root.DescendantNodes().OfType<BaseListSyntax>();

        // Get enums
        public IEnumerable<EnumDeclarationSyntax> GetEnums(string code) => GetEnums(GetRoot(code));
        public IEnumerable<EnumDeclarationSyntax> GetEnums(SyntaxNode root) => root.DescendantNodes().OfType<EnumDeclarationSyntax>();

        // Get fields
        public IEnumerable<FieldDeclarationSyntax> GetFields(string code) => GetFields(GetRoot(code));
        public IEnumerable<FieldDeclarationSyntax> GetFields(SyntaxNode root) => root.DescendantNodes().OfType<FieldDeclarationSyntax>();

        // Get properties
        public IEnumerable<PropertyDeclarationSyntax> GetProperties(string code) => GetProperties(GetRoot(code));
        public IEnumerable<PropertyDeclarationSyntax> GetProperties(SyntaxNode root) => root.DescendantNodes().OfType<PropertyDeclarationSyntax>();

        // Get methods
        public IEnumerable<MethodDeclarationSyntax> GetMethods(string code) => GetMethods(GetRoot(code));
        public IEnumerable<MethodDeclarationSyntax> GetMethods(SyntaxNode root) => root.DescendantNodes().OfType<MethodDeclarationSyntax>();

        // Get parameters
        public IEnumerable<ParameterSyntax> GetParameters(string code) => GetParameters(GetRoot(code));
        public IEnumerable<ParameterSyntax> GetParameters(SyntaxNode root)
        {
            var parameters = new List<ParameterSyntax>();

            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                parameters.AddRange(method.ParameterList.Parameters);
            }

            return parameters;
        }

        // Get constructor args
        public IEnumerable<ConstructorDeclarationSyntax> GetConstructors(string code) => GetConstructors(GetRoot(code));
        public IEnumerable<ConstructorDeclarationSyntax> GetConstructors(SyntaxNode root) => root.DescendantNodes().OfType<ConstructorDeclarationSyntax>();

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
                    if (name != null && _root is CompilationUnitSyntax comp)
                    {
                        string extractedName = comp.Members.FirstOrDefault()?.GetProperty<SyntaxToken>("Identifier").ToString();
                        if (!string.IsNullOrEmpty(extractedName)) name.Data = extractedName;
                    }

                    if (members != null) members.Data = GenerateOutput(_root, GetMembers);
                    if (classes != null) classes.Data = GenerateOutput(_root, GetClasses);
                    if (interfaces != null) interfaces.Data = GenerateOutput(_root, GetInterfaces);
                    if (enums != null) enums.Data = GenerateOutput(_root, GetEnums);
                    if (fields != null) fields.Data = GenerateOutput(_root, GetFields);
                    if (properties != null) properties.Data = GenerateOutput(_root, GetProperties);
                    if (methods != null) methods.Data = GenerateOutput(_root, GetMethods);
                    if (parameters != null) parameters.Data = GenerateOutput(_root, GetParameters);
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
