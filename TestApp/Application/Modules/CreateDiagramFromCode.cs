using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.Win32;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;
using RequirementsAbstractions;
using System.Text.RegularExpressions;
using System.Windows.Threading;

namespace TestApplication
{
    public class CreateDiagramFromCode : IDataFlow<string>
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public Graph Graph { get; set; }
        public Canvas Canvas { get; set; }
        public AbstractionModelManager ModelManager { get; set; }
        public StateTransition<Enums.DiagramMode> StateTransition { get; set; }
        public string DefaultModelType { get; set; } = "Apply";

        // Private fields
        private string _code = "";
        private Dictionary<string, ALANode> _nodesByName = new Dictionary<string, ALANode>();
        private bool _rootCreated = false;

        // Ports
        
        // IDataFlow<string> implementation
        string IDataFlow<string>.Data
        {
            get => default;
            set
            {
                _code = value;
                Create(_code);
            }
        }

        // Methods
        public void Create(string code)
        {
            try
            {
                _code = code;
                CreateNodesFromInstantiations(_code);
                CreateWiresFromCode(_code);
            }
            catch (Exception e)
            {
                Logging.Log($"Error in CreateDiagramFromCode: {e}");
            }
        }

        public void CreateNodesFromInstantiations(string code)
        {
            var parser = new CodeParser();

            var classNode = parser.GetClasses(code).First() as ClassDeclarationSyntax;
            var members = parser.GetMembers(classNode);

            var wiringMethod = parser.GetMethods(classNode).FirstOrDefault(n => n is MethodDeclarationSyntax method && method.Identifier.ToString() == "CreateWiring") as MethodDeclarationSyntax;
            var instantiations = wiringMethod.Body.Statements.OfType<LocalDeclarationStatementSyntax>();

            foreach (var instantiation in instantiations)
            {
                try
                {
                    var variableName = instantiation.Declaration.Variables.First().Identifier.ToString();
                    var fullType = (instantiation.Declaration.Variables.First().Initializer.Value as ObjectCreationExpressionSyntax)?.Type ?? null;
                    if (fullType == null) continue;

                    var isGeneric = fullType is GenericNameSyntax;
                    var type = isGeneric ? (fullType as GenericNameSyntax).Identifier.ToString() : fullType.ToString();

                    var model = ModelManager.GetAbstractionModel(type);
                    if (model == null)
                    {
                        model = ModelManager.GetAbstractionModel(DefaultModelType);
                        Logging.Log($"Could not find an AbstractionModel for type {type}. Using a default model of type {DefaultModelType} instead.");
                    }

                    model.Name = variableName;
                    var node = CreateNodeFromModel(model);
                    _nodesByName[node.Name] = node;

                    // if (!_rootCreated)
                    // {
                    //     Graph.Roots.Add(node);
                    //     Canvas.Children.Add(node.Render);
                    //     _rootCreated = true;
                    // }
                }
                catch (Exception e)
                {
                    Logging.Log($"Failed to parse node instantiation.\nException: {e}");
                    continue;
                }
            }
        }

        public void CreateWiresFromCode(string code)
        {
            var parser = new CodeParser();

            var classNode = parser.GetClasses(code).First() as ClassDeclarationSyntax;
            var members = parser.GetMembers(classNode);

            var wiringMethod = parser.GetMethods(classNode).FirstOrDefault(n => n is MethodDeclarationSyntax method && method.Identifier.ToString() == "CreateWiring") as MethodDeclarationSyntax;
            var wireTos = wiringMethod.Body.Statements
                .OfType<ExpressionStatementSyntax>()
                .Where(syntax => syntax.Expression is InvocationExpressionSyntax invoc && invoc.Expression is MemberAccessExpressionSyntax memb && memb.Name.Identifier.ToString() == "WireTo");

            foreach (var wireTo in wireTos)
            {
                try
                {
                    var sourceName = (((wireTo.Expression as InvocationExpressionSyntax)
                                ?.Expression as MemberAccessExpressionSyntax)
                                ?.Expression as IdentifierNameSyntax)
                                ?.Identifier.ToString()
                                ?? "";

                    var arguments = (wireTo.Expression as InvocationExpressionSyntax).ArgumentList.Arguments;

                    var destinationName = arguments.Count > 0 ? arguments[0].ToString() : "";
                    var sourcePortName = arguments.Count > 1 ? arguments[1].ToString().Trim('\\', '"') : "";

                    if (!_nodesByName.ContainsKey(sourceName))
                    {
                        Logging.Log($"Failed to parse WireTo in CreateDiagramFromCode from line: {wireTo}\nCause: source node {sourceName} not created.");
                        continue;
                    }

                    if (!_nodesByName.ContainsKey(destinationName))
                    {
                        Logging.Log($"Failed to parse WireTo in CreateDiagramFromCode from line: {wireTo}\nCause: destination node {destinationName} not created.");
                        continue;
                    }

                    var source = _nodesByName[sourceName];
                    var destination = _nodesByName[destinationName];
                    var sourcePort = !string.IsNullOrEmpty(sourcePortName) ? source.GetPortBox(sourcePortName) : source.GetSelectedPort(inputPort: false);
                    var matchingPort = FindMatchingDestinationPortBox(sourcePort.Payload as Port, destination);

                    if (matchingPort == null)
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine($"Failed to parse WireTo in CreateDiagramFromCode from line: {wireTo}");
                        sb.AppendLine($"source: {source.Model.Type} {source.Name}");
                        sb.AppendLine($"destination: {destination.Model.Type} {destination.Name}");
                        sb.AppendLine($"sourcePort: {(sourcePort.Payload is Port port ? port.Type + " " + port.Name : "")}");
                        sb.AppendLine($"destinationPort: None found that match sourcePort in destination.");

                        Logging.Log(sb.ToString());
                        continue;
                    }

                    if (!_rootCreated)
                    {
                        Graph.Roots.Add(source);
                        Canvas.Children.Add(source.Render);
                        _rootCreated = true;
                    }

                    var wire = CreateWire(source, destination, sourcePort, matchingPort);

                    Graph.AddEdge(wire);

                    if (!Canvas.Children.Contains(source.Render)) Canvas.Children.Add(source.Render);
                    if (!Canvas.Children.Contains(destination.Render)) Canvas.Children.Add(destination.Render);

                    wire.Paint();
                }
                catch (Exception e)
                {
                    Logging.Log(e);
                    continue;
                }
            }
        }

        private Box FindMatchingDestinationPortBox(Port portToMatch, ALANode destination)
        {
            var inputPorts = destination.GetImplementedPorts();
            var matchingPort = inputPorts.FirstOrDefault(p => p.Type == portToMatch.Type);
            if (matchingPort == null) matchingPort = inputPorts.FirstOrDefault(p => Regex.IsMatch(portToMatch.Type, $@"List<{p.Type}>"));
            if (matchingPort == null) matchingPort = inputPorts.FirstOrDefault(p => p.IsInputPort);

            if (matchingPort == null) return null;

            return destination.GetPortBox(matchingPort.Name);
        }

        private ALAWire CreateWire(ALANode source, ALANode destination, Box sourcePort, Box destinationPort)
        {
            var wire = new ALAWire()
            {
                Graph = Graph,
                Canvas = Canvas,
                Source = source,
                Destination = destination,
                SourcePort = sourcePort,
                DestinationPort = destinationPort,
                StateTransition = StateTransition
            };

            return wire;
        }

        public ALANode CreateNodeFromModel(AbstractionModel model, bool draw = false)
        {
            var node = new ALANode();
            node.Model = model;
            node.Graph = Graph;
            node.Canvas = Canvas;
            node.StateTransition = StateTransition;
            node.AvailableDomainAbstractions.AddRange(
                ModelManager.GetAbstractionTypes());

            node.TypeChanged += newType =>
            {
                ModelManager.UpdateAbstractionModel(
                    ModelManager.GetAbstractionModel(newType),
                    node.Model);

                node.UpdateUI();

                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    var edges = Graph.Edges;
                    foreach (var edge in edges)
                    {
                        (edge as ALAWire).Refresh();
                    }
                }, DispatcherPriority.ContextIdle);
            };

            Graph.AddNode(node);
            node.CreateInternals();
            if (draw) Canvas.Children.Add(node.Render);

            return node;
        }

        public CreateDiagramFromCode()
        {

        }

    }
}
