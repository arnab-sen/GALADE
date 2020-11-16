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
        
        /// <summary>
        /// Determines whether to update the existing graph or to create a new one.
        /// </summary>
        public bool Update { get; set; } = false;

        // Private fields
        private string _code = "";
        private Dictionary<string, ALANode> _nodesByName = new Dictionary<string, ALANode>();
        private Dictionary<string, ALAWire> _wiresById = new Dictionary<string, ALAWire>();
        private bool _rootCreated = false;
        private int _instCount = 0;
        private int _instTotal = 0;
        private int _wireToCount = 0;
        private int _wireToTotal = 0;

        // Global instances
        private DataFlowConnector<string> _startCreation;

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
        public void Reset()
        {
            _nodesByName.Clear();
            _wiresById.Clear();
            _rootCreated = false;
        }

        public void Create(string code)
        {
            try
            {
                if (!Update) Reset();

                // Should still recreate all wires
                _wiresById.Clear();

                var edges = Graph.Edges.ToList();

                foreach (object edge in edges)
                {
                    if (edge is ALAWire wire && Canvas.Children.Contains(wire.Render))
                    {
                        Canvas.Children.Remove(wire.Render);
                        Graph.DeleteEdge(edge);
                    }
                }

                Graph.Edges.Clear();

                // Logging.Message("Beginning diagram generation from code...");

                _code = code;

                (_startCreation as IDataFlow<string>).Data = _code;
                // CreateWiresFromCode(_code);
            }
            catch (Exception e)
            {
                Logging.Log($"Error in CreateDiagramFromCode: {e}");
            }
        }

        public IEnumerable<LocalDeclarationStatementSyntax> GetInstantiations(string code)
        {
            var parser = new CodeParser();

            var classNode = parser.GetClasses(code).First() as ClassDeclarationSyntax;
            var members = parser.GetMembers(classNode);

            var wiringMethod = parser.GetMethods(classNode).FirstOrDefault(n => n is MethodDeclarationSyntax method && method.Identifier.ToString() == "CreateWiring") as MethodDeclarationSyntax;
            var instantiations = wiringMethod?.Body.Statements.OfType<LocalDeclarationStatementSyntax>() ?? new List<LocalDeclarationStatementSyntax>();

            return instantiations;
        }

        public void CreateNode(LocalDeclarationStatementSyntax instantiation)
        {
            try
            {
                var variableName = instantiation.Declaration.Variables.First().Identifier.ToString();

                var fullType = (instantiation.Declaration.Variables.First().Initializer.Value as ObjectCreationExpressionSyntax)?.Type ?? null;

                if (fullType == null) return;

                var isGeneric = fullType is GenericNameSyntax;

                var typeWithoutGenerics = "";
                var generics = new List<string>();

                if (isGeneric)
                {
                    typeWithoutGenerics = (fullType as GenericNameSyntax).Identifier.ToString();
                    generics = (fullType as GenericNameSyntax).TypeArgumentList.Arguments.Select(t => t.ToString()).ToList();
                }
                else
                {
                    typeWithoutGenerics = fullType.ToString();
                }

                var modelTemplate = ModelManager.GetAbstractionModel(typeWithoutGenerics);
                if (modelTemplate == null)
                {
                    modelTemplate = ModelManager.GetAbstractionModel(DefaultModelType);
                    Logging.Log($"Could not find an AbstractionModel for type {typeWithoutGenerics}. Using a default model of type {DefaultModelType} instead.");
                }

                var model = new AbstractionModel();
                model.CloneFrom(modelTemplate);

                model.Name = variableName;
                model.SetGenerics(generics);

                var initialiser = instantiation.Declaration.Variables.FirstOrDefault()?.Initializer.Value as ObjectCreationExpressionSyntax;

                // Get constructor arguments
                var constructorArgs = initialiser.ArgumentList.Arguments.Where(arg => arg.NameColon != null);

                foreach (var constructorArg in constructorArgs)
                {
                    model.SetValue(constructorArg.NameColon.ToString().Trim(':'), constructorArg.Expression.ToString(), initialise: true);
                }

                // Get initializer properties
                var expressions = initialiser.Initializer?.Expressions;

                if (expressions != null)
                {
                    foreach (var expression in expressions)
                    {
                        if (expression is AssignmentExpressionSyntax assignment)
                        {
                            model.SetValue(assignment.Left.ToString(), assignment.Right.NormalizeWhitespace(elasticTrivia: true).ToString(), initialise: true);
                        }
                    } 
                }

                if (!_nodesByName.ContainsKey(model.Name))
                {
                    var node = CreateNodeFromModel(model);
                    _nodesByName[node.Name] = node; 
                }
                else
                {
                    var node = _nodesByName[model.Name];
                    node.Model.CloneFrom(model);
                    node.UpdateUI();
                }

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
            }
        }

        public IEnumerable<ExpressionStatementSyntax> GetWireTos(string code)
        {
            var parser = new CodeParser();

            var classNode = parser.GetClasses(code).First() as ClassDeclarationSyntax;
            var members = parser.GetMembers(classNode);

            var wiringMethod = parser.GetMethods(classNode).FirstOrDefault(n => n is MethodDeclarationSyntax method && method.Identifier.ToString() == "CreateWiring") as MethodDeclarationSyntax;
            var wireTos = wiringMethod.Body.Statements
                .OfType<ExpressionStatementSyntax>()
                .Where(syntax => syntax.Expression is InvocationExpressionSyntax invoc && invoc.Expression is MemberAccessExpressionSyntax memb 
                                                                                       && memb.Name.Identifier.ToString() == "WireTo")
                .ToList();

            return wireTos;
        }

        public void CreateWire(ExpressionStatementSyntax wireTo)
        {
            var arguments = (wireTo.Expression as InvocationExpressionSyntax).ArgumentList.Arguments;

            var sourceName = (((wireTo.Expression as InvocationExpressionSyntax)
                        ?.Expression as MemberAccessExpressionSyntax)
                        ?.Expression as IdentifierNameSyntax)
                        ?.Identifier.ToString()
                        ?? "";


            var destinationName = arguments.Count > 0 ? arguments[0].ToString() : "";
            var sourcePortName = arguments.Count > 1 ? arguments[1].ToString().Trim('\\', '"') : "";

            if (!_nodesByName.ContainsKey(sourceName))
            {
                Logging.Log($"Failed to parse WireTo in CreateDiagramFromCode from line: {wireTo}\nCause: source node {sourceName} not created.");
                return;
            }

            if (!_nodesByName.ContainsKey(destinationName))
            {
                Logging.Log($"Failed to parse WireTo in CreateDiagramFromCode from line: {wireTo}\nCause: destination node {destinationName} not created.");
                return;
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
                return;
            }

            if (!_rootCreated)
            {
                Graph.Roots.Add(source);
                Canvas.Children.Add(source.Render);
                _rootCreated = true;
            }

            var wire = CreateALAWire(source, destination, sourcePort, matchingPort);

            Graph.AddEdge(wire);

            _wiresById[wire.Id] = wire;

            if (!Canvas.Children.Contains(source.Render)) Canvas.Children.Add(source.Render);
            if (!Canvas.Children.Contains(destination.Render)) Canvas.Children.Add(destination.Render);

            wire.Paint();
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

        private ALAWire CreateALAWire(ALANode source, ALANode destination, Box sourcePort, Box destinationPort)
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
            if (model.Name.StartsWith("id_"))
            {
                node.Id = model.Name.Substring(3);
                node.ShowName = false;
            }

            node.Model = model;
            node.Graph = Graph;
            node.Canvas = Canvas;
            node.StateTransition = StateTransition;
            node.AvailableAbstractions.AddRange(
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
            // BEGIN AUTO-GENERATED INSTANTIATIONS
            DataFlowConnector<string> startCreation = new DataFlowConnector<string>() { InstanceName = "startCreation" };
            ForEach<LocalDeclarationStatementSyntax> id_fcaabe33216f4a58a93b0b2ef5f15010 = new ForEach<LocalDeclarationStatementSyntax>() {  };
            DispatcherData<LocalDeclarationStatementSyntax> id_e9b477643ed94a8c9247968436584d38 = new DispatcherData<LocalDeclarationStatementSyntax>() { Priority = DispatcherPriority.ApplicationIdle };
            DataFlowConnector<LocalDeclarationStatementSyntax> id_1cf09862061644bd81f58e0965b6420a = new DataFlowConnector<LocalDeclarationStatementSyntax>() {  };
            ApplyAction<LocalDeclarationStatementSyntax> id_1588adb8d64b4fe6876b20a0eb5075fd = new ApplyAction<LocalDeclarationStatementSyntax>() { Lambda = instantiation =>{_instCount++;Logging.Message($"Creating node {_instCount}/{_instTotal}...");} };
            ApplyAction<LocalDeclarationStatementSyntax> id_b7f04a7310894b7a884b171a3bade791 = new ApplyAction<LocalDeclarationStatementSyntax>() { Lambda = CreateNode };
            EventLambda id_80f41b60cacd493c84808b1c4c8755f1 = new EventLambda() { Lambda = () =>{_instCount = 0;Logging.Message($"{_nodesByName.Keys.Count}/{_instTotal} nodes created.");} };
            Apply<string, IEnumerable<LocalDeclarationStatementSyntax>> getInstantiations  = new Apply<string, IEnumerable<LocalDeclarationStatementSyntax>>() { InstanceName = "getInstantiations ", Lambda = GetInstantiations };
            Apply<string, IEnumerable<ExpressionStatementSyntax>> id_8c692c6a27f449619146ed6dd8d9c621 = new Apply<string, IEnumerable<ExpressionStatementSyntax>>() { Lambda = GetWireTos };
            ForEach<ExpressionStatementSyntax> id_22451793e0224fb4b2f387639abc3ff6 = new ForEach<ExpressionStatementSyntax>() {  };
            DataFlowConnector<ExpressionStatementSyntax> id_3d924167517f4128bb970f478986843e = new DataFlowConnector<ExpressionStatementSyntax>() {  };
            DispatcherData<ExpressionStatementSyntax> id_1475c43393ee4fc99a723c2e19c9630f = new DispatcherData<ExpressionStatementSyntax>() { Priority = DispatcherPriority.ApplicationIdle };
            ApplyAction<ExpressionStatementSyntax> id_19d1288ca6b6498a97baa8269a45462c = new ApplyAction<ExpressionStatementSyntax>() { Lambda = wireTo =>{_wireToCount++;Logging.Message($"Creating wire {_wireToCount}/{_wireToTotal}...");} };
            ApplyAction<ExpressionStatementSyntax> id_f5021eb48f0d462895ac67cd14f14031 = new ApplyAction<ExpressionStatementSyntax>() { Lambda = CreateWire };
            EventLambda id_e5707746b1484708a6d77003adedaa8b = new EventLambda() { Lambda = () =>{_wireToCount = 0;Logging.Message($"{_wiresById.Keys.Count}/{_wireToTotal} wires created.");} };
            DataFlowConnector<IEnumerable<LocalDeclarationStatementSyntax>> id_09ac5cb7b8854dffa0d0755e4d99d4f9 = new DataFlowConnector<IEnumerable<LocalDeclarationStatementSyntax>>() {  };
            ApplyAction<IEnumerable<LocalDeclarationStatementSyntax>> id_2906f2e0dee248f5abe56872822e6ca7 = new ApplyAction<IEnumerable<LocalDeclarationStatementSyntax>>() { Lambda = input =>{_instTotal = input.Count();} };
            DataFlowConnector<IEnumerable<ExpressionStatementSyntax>> id_eead8c20b8644a5b99bab14b5c783d0c = new DataFlowConnector<IEnumerable<ExpressionStatementSyntax>>() {  };
            ApplyAction<IEnumerable<ExpressionStatementSyntax>> id_8abe632830344b4585a69c3fe4d099a7 = new ApplyAction<IEnumerable<ExpressionStatementSyntax>>() { Lambda = input =>{_wireToTotal = input.Count();} };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            startCreation.WireTo(getInstantiations , "fanoutList");
            startCreation.WireTo(id_8c692c6a27f449619146ed6dd8d9c621, "fanoutList");
            id_fcaabe33216f4a58a93b0b2ef5f15010.WireTo(id_1cf09862061644bd81f58e0965b6420a, "elementOutput");
            id_fcaabe33216f4a58a93b0b2ef5f15010.WireTo(id_80f41b60cacd493c84808b1c4c8755f1, "complete");
            id_e9b477643ed94a8c9247968436584d38.WireTo(id_1588adb8d64b4fe6876b20a0eb5075fd, "delayedData");
            id_1cf09862061644bd81f58e0965b6420a.WireTo(id_e9b477643ed94a8c9247968436584d38, "fanoutList");
            id_1cf09862061644bd81f58e0965b6420a.WireTo(id_b7f04a7310894b7a884b171a3bade791, "fanoutList");
            getInstantiations .WireTo(id_09ac5cb7b8854dffa0d0755e4d99d4f9, "output");
            id_8c692c6a27f449619146ed6dd8d9c621.WireTo(id_eead8c20b8644a5b99bab14b5c783d0c, "output");
            id_22451793e0224fb4b2f387639abc3ff6.WireTo(id_3d924167517f4128bb970f478986843e, "elementOutput");
            id_22451793e0224fb4b2f387639abc3ff6.WireTo(id_e5707746b1484708a6d77003adedaa8b, "complete");
            id_3d924167517f4128bb970f478986843e.WireTo(id_1475c43393ee4fc99a723c2e19c9630f, "fanoutList");
            id_3d924167517f4128bb970f478986843e.WireTo(id_f5021eb48f0d462895ac67cd14f14031, "fanoutList");
            id_1475c43393ee4fc99a723c2e19c9630f.WireTo(id_19d1288ca6b6498a97baa8269a45462c, "delayedData");
            id_09ac5cb7b8854dffa0d0755e4d99d4f9.WireTo(id_2906f2e0dee248f5abe56872822e6ca7, "fanoutList");
            id_09ac5cb7b8854dffa0d0755e4d99d4f9.WireTo(id_fcaabe33216f4a58a93b0b2ef5f15010, "fanoutList");
            id_eead8c20b8644a5b99bab14b5c783d0c.WireTo(id_8abe632830344b4585a69c3fe4d099a7, "fanoutList");
            id_eead8c20b8644a5b99bab14b5c783d0c.WireTo(id_22451793e0224fb4b2f387639abc3ff6, "fanoutList");
            // END AUTO-GENERATED WIRING

            // Instance mapping
            _startCreation = startCreation;
        }

    }
}


















































