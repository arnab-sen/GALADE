using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace RequirementsAbstractions
{
    /// <summary>
    /// (In progress)
    /// </summary>
    public class InsertStatements : IDataFlow<List<string>>
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string MethodName { get; set; } = "CreateWiring";
        public string DestinationCode { get; set; }

        // Private fields
        private List<string> _inputCodeLines;
        private string _newCode = "";
        private SyntaxNode _root = null;

        // Ports
        private IDataFlowB<string> destinationCodeInput;
        private IDataFlow<string> newCode;
        private IDataFlow<SyntaxNode> root;

        // IDataFlow<List<string>> implementation
        List<string> IDataFlow<List<string>>.Data
        {
            get => _inputCodeLines;
            set
            {
                _inputCodeLines = value;
                InsertLines(_inputCodeLines);
                if (newCode != null) newCode.Data = _newCode;
                if (root != null) root.Data = _root;
            }
        }

        // Methods
        private void InsertLines(List<string> lines)
        {
            var parser = new CodeParser();
            var destinationCode = destinationCodeInput?.Data ?? DestinationCode;

            if (string.IsNullOrEmpty(destinationCode))
            {
                Logging.Log("Failed to insert code in InsertStatement: DestinationCode was not provided");
                return;
            }

            var root = parser.GetRoot(destinationCode);

            var methods = parser.GetMethods(root);
            var destinationMethod = methods
                .FirstOrDefault(method => method is MethodDeclarationSyntax syntax && syntax.Identifier.ValueText == MethodName)
                as MethodDeclarationSyntax;

            if (destinationMethod == null) return;

            // var newMethodNode =
            //         MethodDeclaration(
            //             PredefinedType(
            //                 Token(
            //                     TriviaList(),
            //                     SyntaxKind.VoidKeyword,
            //                     TriviaList(Space))),
            //             Identifier(MethodName))
            //         .WithModifiers(
            //             destinationMethod.Modifiers)
            //         .WithBody(
            //             Block());


            var statements = new List<StatementSyntax>();

            foreach (var line in lines)
            {
                var node = parser.GetRoot(line).DescendantNodesAndSelf().OfType<StatementSyntax>().FirstOrDefault();
                if (node == null) return;

                // node = node.ReplaceTrivia(SyntaxTrivia(SyntaxKind.EndOfLineTrivia, "\n"), SyntaxTrivia(SyntaxKind.WhitespaceTrivia, ""));

                // newMethodNode = newMethodNode.AddBodyStatements(node);
                statements.Add(node.WithLeadingTrivia(TriviaList(Enumerable.Repeat(Space, 8))));
            }


            // newMethodNode = newMethodNode.NormalizeWhitespace();
            // newMethodNode = newMethodNode.WithTrailingTrivia(SyntaxTrivia(SyntaxKind.EndOfLineTrivia, "\n"));

            _root = root.ReplaceNode(destinationMethod.Body, 
                destinationMethod.Body
                    .WithStatements(new SyntaxList<StatementSyntax>(statements))
                    // .WithLeadingTrivia(destinationMethod.Body.GetLeadingTrivia())
                    .WithTrailingTrivia(destinationMethod.Body.GetTrailingTrivia()));
            
            // _root = root.ReplaceNode(destinationMethod, 
            //     newMethodNode.NormalizeWhitespace().WithLeadingTrivia(destinationMethod?.GetLeadingTrivia() ?? default)
            //     .WithTrailingTrivia(destinationMethod?.GetTrailingTrivia() ?? default));
            
            // _newCode = _root.NormalizeWhitespace(elasticTrivia: true).ToString();

            Formatter.Format(_root.WithAdditionalAnnotations(SyntaxAnnotation.ElasticAnnotation), new AdhocWorkspace());

            _newCode = _root.ToString();

        }

        public InsertStatements()
        {

        }
    }
}
