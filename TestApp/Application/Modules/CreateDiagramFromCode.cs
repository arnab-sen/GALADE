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
        public void Create(string code)
        {
            try
            {
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
                            model.SetValue(assignment.Left.ToString(), assignment.Right.NormalizeWhitespace().ToString(), initialise: true);
                        }
                    } 
                }

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

            var wire = CreateWire(source, destination, sourcePort, matchingPort);

            Graph.AddEdge(wire);

            _wiresById[wire.Id] = wire;

            if (!Canvas.Children.Contains(source.Render)) Canvas.Children.Add(source.Render);
            if (!Canvas.Children.Contains(destination.Render)) Canvas.Children.Add(destination.Render);

            wire.Paint();
        }

        public void CreateWiresFromCode(string code)
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

            for (var i = 0; i < wireTos.Count; i++)
            {
                try
                {
                    var wireTo = wireTos[i];

                    var sourceName = (((wireTo.Expression as InvocationExpressionSyntax)
                                ?.Expression as MemberAccessExpressionSyntax)
                                ?.Expression as IdentifierNameSyntax)
                                ?.Identifier.ToString()
                                ?? "";

                    var arguments = (wireTo.Expression as InvocationExpressionSyntax).ArgumentList.Arguments;

                    var destinationName = arguments.Count > 0 ? arguments[0].ToString() : "";
                    var sourcePortName = arguments.Count > 1 ? arguments[1].ToString().Trim('\\', '"') : "";

                    Logging.Log($"Creating wire for {sourceName}.WireTo({destinationName}, \"{sourcePortName}\") ({i}/{wireTos.Count})");

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
            if (model.Name.StartsWith("id_"))
            {
                node.Id = model.Name.Substring(3);
                node.ShowName = false;
            }

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

        public void CreateWiring()
        {
            // BEGIN AUTO-GENERATED INSTANTIATIONS
            DataFlowConnector<string> startCreation = new DataFlowConnector<string>() { InstanceName = "startCreation" };
            ForEach<LocalDeclarationStatementSyntax> id_9159f6dcfacb46dcaf5f87754cd5a711 = new ForEach<LocalDeclarationStatementSyntax>() {  };
            DispatcherData<LocalDeclarationStatementSyntax> id_981732d1fe5d4504baae744ad84c7100 = new DispatcherData<LocalDeclarationStatementSyntax>() { Priority = DispatcherPriority.ApplicationIdle };
            DataFlowConnector<LocalDeclarationStatementSyntax> id_067e853a88c74951870f18d01583d89a = new DataFlowConnector<LocalDeclarationStatementSyntax>() {  };
            ApplyAction<LocalDeclarationStatementSyntax> id_6271904b1b804398bfb1b17ddfd4bf24 = new ApplyAction<LocalDeclarationStatementSyntax>() { Lambda = instantiation =>{_instCount++;var variableName = instantiation.Declaration.Variables.First().Identifier.ToString();var fullType = (instantiation.Declaration.Variables.First().Initializer.Value as ObjectCreationExpressionSyntax)?.Type ?? null;Logging.Message($"Creating node {fullType} {variableName} ({_instCount}/{_instTotal})");} };
            DispatcherData<LocalDeclarationStatementSyntax> id_8770ef754e7349e283faf7107f8a0c39 = new DispatcherData<LocalDeclarationStatementSyntax>() { Priority = DispatcherPriority.ApplicationIdle };
            ApplyAction<LocalDeclarationStatementSyntax> id_30a039d35ac54acfa38a6b8c45500551 = new ApplyAction<LocalDeclarationStatementSyntax>() { Lambda = CreateNode };
            EventLambda id_74e03753836846afaaa2e5eda3d32c27 = new EventLambda() { Lambda = () =>{_instCount = 0;Logging.Message($"{_nodesByName.Keys.Count}/{_instTotal} nodes created.");} };
            Apply<string, IEnumerable<LocalDeclarationStatementSyntax>> getInstantiations  = new Apply<string, IEnumerable<LocalDeclarationStatementSyntax>>() { InstanceName = "getInstantiations ", Lambda = GetInstantiations };
            Apply<string, IEnumerable<ExpressionStatementSyntax>> id_44f674f2d4af4518b9ea04bcd69cdcd1 = new Apply<string, IEnumerable<ExpressionStatementSyntax>>() { Lambda = GetWireTos };
            ForEach<ExpressionStatementSyntax> id_3b5e272eaa4b4495ba21975f6aee0031 = new ForEach<ExpressionStatementSyntax>() {  };
            DataFlowConnector<ExpressionStatementSyntax> id_37888d1f75934d6887a08bc55c731927 = new DataFlowConnector<ExpressionStatementSyntax>() {  };
            DispatcherData<ExpressionStatementSyntax> id_d8670ae645a24d64864da0a32df4a732 = new DispatcherData<ExpressionStatementSyntax>() { Priority = DispatcherPriority.ApplicationIdle };
            ApplyAction<ExpressionStatementSyntax> id_4e417613657b4dc18c7d0525092a330a = new ApplyAction<ExpressionStatementSyntax>() { Lambda = wireTo =>{_wireToCount++;var arguments = (wireTo.Expression as InvocationExpressionSyntax).ArgumentList.Arguments;var sourceName = (((wireTo.Expression as InvocationExpressionSyntax)                        ?.Expression as MemberAccessExpressionSyntax)                        ?.Expression as IdentifierNameSyntax)                        ?.Identifier.ToString()                        ?? "";var destinationName = arguments.Count > 0 ? arguments[0].ToString() : "";var sourcePortName = arguments.Count > 1 ? arguments[1].ToString().Trim('\\', '"') : "";Logging.Message($"Creating wire for {sourceName}.WireTo({destinationName}, \"{sourcePortName}\") ({_wireToCount}/{_wireToTotal})");} };
            DispatcherData<ExpressionStatementSyntax> id_21ee494761ea4d23a7678d39d05e8ff6 = new DispatcherData<ExpressionStatementSyntax>() { Priority = DispatcherPriority.ApplicationIdle };
            ApplyAction<ExpressionStatementSyntax> id_239aab721a604ee79c32c6be900efc37 = new ApplyAction<ExpressionStatementSyntax>() { Lambda = CreateWire };
            EventLambda id_5d0dcadbf08243a692d90ea011545f09 = new EventLambda() { Lambda = () =>{_wireToCount = 0;Logging.Message($"{_wiresById.Keys.Count}/{_wireToTotal} wires created.");} };
            DataFlowConnector<IEnumerable<LocalDeclarationStatementSyntax>> id_0fa83d8a2c624a039d43e585bfa45234 = new DataFlowConnector<IEnumerable<LocalDeclarationStatementSyntax>>() {  };
            ApplyAction<IEnumerable<LocalDeclarationStatementSyntax>> id_22d9a298c02b40679d21233e357ca2c9 = new ApplyAction<IEnumerable<LocalDeclarationStatementSyntax>>() { Lambda = input =>{_instTotal = input.Count();} };
            DataFlowConnector<IEnumerable<ExpressionStatementSyntax>> id_c7d9438ab3814bd99afe3ba637d28968 = new DataFlowConnector<IEnumerable<ExpressionStatementSyntax>>() {  };
            ApplyAction<IEnumerable<ExpressionStatementSyntax>> id_11f3da51004f4fd58e82d50bcc09eb6e = new ApplyAction<IEnumerable<ExpressionStatementSyntax>>() { Lambda = input =>{_wireToTotal = input.Count();} };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            startCreation.WireTo(getInstantiations , "fanoutList");
            startCreation.WireTo(id_44f674f2d4af4518b9ea04bcd69cdcd1, "fanoutList");
            id_9159f6dcfacb46dcaf5f87754cd5a711.WireTo(id_067e853a88c74951870f18d01583d89a, "elementOutput");
            id_9159f6dcfacb46dcaf5f87754cd5a711.WireTo(id_74e03753836846afaaa2e5eda3d32c27, "complete");
            id_981732d1fe5d4504baae744ad84c7100.WireTo(id_6271904b1b804398bfb1b17ddfd4bf24, "delayedData");
            id_067e853a88c74951870f18d01583d89a.WireTo(id_981732d1fe5d4504baae744ad84c7100, "fanoutList");
            id_067e853a88c74951870f18d01583d89a.WireTo(id_8770ef754e7349e283faf7107f8a0c39, "fanoutList");
            id_8770ef754e7349e283faf7107f8a0c39.WireTo(id_30a039d35ac54acfa38a6b8c45500551, "delayedData");
            getInstantiations .WireTo(id_0fa83d8a2c624a039d43e585bfa45234, "output");
            id_44f674f2d4af4518b9ea04bcd69cdcd1.WireTo(id_c7d9438ab3814bd99afe3ba637d28968, "output");
            id_3b5e272eaa4b4495ba21975f6aee0031.WireTo(id_37888d1f75934d6887a08bc55c731927, "elementOutput");
            id_3b5e272eaa4b4495ba21975f6aee0031.WireTo(id_5d0dcadbf08243a692d90ea011545f09, "complete");
            id_37888d1f75934d6887a08bc55c731927.WireTo(id_d8670ae645a24d64864da0a32df4a732, "fanoutList");
            id_37888d1f75934d6887a08bc55c731927.WireTo(id_21ee494761ea4d23a7678d39d05e8ff6, "fanoutList");
            id_d8670ae645a24d64864da0a32df4a732.WireTo(id_4e417613657b4dc18c7d0525092a330a, "delayedData");
            id_21ee494761ea4d23a7678d39d05e8ff6.WireTo(id_239aab721a604ee79c32c6be900efc37, "delayedData");
            id_0fa83d8a2c624a039d43e585bfa45234.WireTo(id_22d9a298c02b40679d21233e357ca2c9, "fanoutList");
            id_0fa83d8a2c624a039d43e585bfa45234.WireTo(id_9159f6dcfacb46dcaf5f87754cd5a711, "fanoutList");
            id_c7d9438ab3814bd99afe3ba637d28968.WireTo(id_11f3da51004f4fd58e82d50bcc09eb6e, "fanoutList");
            id_c7d9438ab3814bd99afe3ba637d28968.WireTo(id_3b5e272eaa4b4495ba21975f6aee0031, "fanoutList");
            // END AUTO-GENERATED WIRING

            // Instance mapping
            _startCreation = startCreation;
        }

        public CreateDiagramFromCode()
        {
            CreateWiring();
        }

    }
}




































