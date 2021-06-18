using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProgrammingParadigms;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DomainAbstractions
{
    /// <summary>
    /// Creates a C# source file with a single class or interface.
    /// </summary>
    public class SourceFileGenerator
    {
        public List<string> Usings { get; set; } = new List<string>()
        {
            "System"
        };

        public string Namespace { get; set; } = "_namespace_";
        public string ClassName { get; set; } = "_class_";
        public Enums.AccessLevel ClassAccessLevel { get; set; } = Enums.AccessLevel.Public;

        private CompilationUnitSyntax _fileUnit;
        private NamespaceDeclarationSyntax _namespaceUnit;
        private ClassDeclarationSyntax _mainClass;
        private List<MemberDeclarationSyntax> _mainClassMembers = new List<MemberDeclarationSyntax>();

        private readonly Dictionary<Enums.AccessLevel, SyntaxKind> _accessLevelMap = new Dictionary<Enums.AccessLevel, SyntaxKind>()
        {
            {Enums.AccessLevel.None, SyntaxKind.None},
            {Enums.AccessLevel.Public, SyntaxKind.PublicKeyword},
            {Enums.AccessLevel.Private, SyntaxKind.PrivateKeyword},
            {Enums.AccessLevel.Protected, SyntaxKind.ProtectedKeyword},
            {Enums.AccessLevel.Internal, SyntaxKind.InternalKeyword}
        };


        public void AddMethod(string name, Enums.AccessLevel accessLevel, string returnType, bool hasBody = true, params Variable[] arguments)
        {
            var method = MethodDeclaration(
                    IdentifierName(returnType),
                    Identifier(name))
                .WithModifiers(TokenList(Token(_accessLevelMap[accessLevel])))
                .WithParameterList(GetParameterList(arguments.ToList()));

            if (hasBody)
            {
                method = method.WithBody(Block());
            }

            _mainClassMembers.Add(method);
        }

        private ParameterListSyntax GetParameterList(List<Variable> variables)
        {
            var tokens = new List<SyntaxNodeOrToken>();

            for (var i = 0; i < variables.Count; i++)
            {
                var variable = variables[i];
                var parameter = 
                    Parameter(
                        Identifier(variable.Name))
                    .WithType(IdentifierName(variable.Type));

                if (!string.IsNullOrEmpty(variable.InitialValue))
                {
                    parameter = parameter.WithDefault(EqualsValueClause(IdentifierName(variable.InitialValue)));
                }

                tokens.Add(parameter);

                if (i < variables.Count - 1)
                {
                    tokens.Add(Token(SyntaxKind.CommaToken));
                }
            }

            return ParameterList(SeparatedList<ParameterSyntax>(tokens.ToArray()));
        }

        private CompilationUnitSyntax AddUsings(CompilationUnitSyntax root, List<string> usings)
        {
            foreach (var namespaceName in usings)
            {
                root = root.AddUsings(UsingDirective(IdentifierName(namespaceName)));
            }

            return root;
        }

        private NamespaceDeclarationSyntax CreateNamespace(string name)
        {
            return NamespaceDeclaration(IdentifierName(Namespace));
        }


        private ClassDeclarationSyntax CreateClass(string className, Enums.AccessLevel accessLevel)
        {
            var accessKeyword = _accessLevelMap.ContainsKey(accessLevel) && accessLevel != Enums.AccessLevel.None
                ? _accessLevelMap[accessLevel]
                : SyntaxKind.PublicKeyword;


            var classUnit =
                ClassDeclaration(className)
                    .WithModifiers(
                        TokenList(
                            Token(accessKeyword)));

            return classUnit;
        }



        private void Initialise()
        {
            _fileUnit = CompilationUnit();
            _fileUnit = AddUsings(_fileUnit, Usings);
            _namespaceUnit = CreateNamespace(Namespace);
            _mainClass = CreateClass(ClassName, ClassAccessLevel);
        }

        public string Generate()
        {
            Initialise();

            string formattedString = "";

            _mainClass = _mainClass.AddMembers(_mainClassMembers.ToArray());
            _namespaceUnit = _namespaceUnit.AddMembers(_mainClass);
            _fileUnit = _fileUnit.AddMembers(_namespaceUnit);

            formattedString = _fileUnit.NormalizeWhitespace(indentation: "\t").ToString();

            return formattedString;
        }

        public SourceFileGenerator()
        {

        }

        public class Variable
        {
            public Enums.AccessLevel AccessLevel { get; set; } = Enums.AccessLevel.None;
            public string Type { get; set; }
            public string Name { get; set; }
            public string InitialValue { get; set; }
        }
    }
}
