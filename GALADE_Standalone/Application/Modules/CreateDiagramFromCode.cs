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
using System.Windows.Media;
using System.Windows.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Newtonsoft.Json.Linq;

namespace Application
{
    public class CreateDiagramFromCode : IDataFlow<string>
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public Graph Graph { get; set; }
        public Canvas Canvas { get; set; }
        public AbstractionModelManager ModelManager { get; set; }
        public StateTransition<Enums.DiagramMode> StateTransition { get; set; }
        public IEvent RefreshLayout { get; set; }

        /// <summary>
        /// Determines whether to update the existing graph or to create a new one.
        /// </summary>
        public bool Update { get; set; } = false;

        // Private fields
        private string _code = "";
        private Dictionary<string, ALANode> _nodesByName = new Dictionary<string, ALANode>();
        private Dictionary<string, ALAWire> _wiresById = new Dictionary<string, ALAWire>();
        private int _instCount = 0;
        private int _instTotal = 0;
        private int _wireToCount = 0;
        private int _wireToTotal = 0;
        private HashSet<string> _nodesWithTreeParents = new HashSet<string>();
        private HashSet<string> _nodesWithoutTreeParents = new HashSet<string>();
        private string _firstRootVarName = "";
        private HashSet<string> _roots = new HashSet<string>(); // Nodes that should always be roots, e.g. ones that have names starting with "root_"
        private HashSet<string> _autoCreatedRoots = new HashSet<string>(); // Nodes that haven't been wired to yet

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
            _nodesWithTreeParents.Clear();
            _nodesWithoutTreeParents.Clear();
            _roots.Clear();
            _autoCreatedRoots.Clear();
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

                _firstRootVarName = "";
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

        public LocalDeclarationStatementSyntax CreateDummyInstantiation(string type, string name)
        {
            var instantiation =
                LocalDeclarationStatement(
                        VariableDeclaration(
                                IdentifierName("var"))
                            .WithVariables(
                                SingletonSeparatedList<VariableDeclaratorSyntax>(
                                    VariableDeclarator(
                                            Identifier(name))
                                        .WithInitializer(
                                            EqualsValueClause(
                                                ObjectCreationExpression(
                                                        IdentifierName(type))
                                                    .WithArgumentList(
                                                        ArgumentList()))))))
                    .NormalizeWhitespace();

            return instantiation;
        }

        private JObject ParseMetaData(string metaDataRaw)
        {
            JObject metaDataObj = null;
            metaDataRaw = metaDataRaw.Trim();
            if (!string.IsNullOrEmpty(metaDataRaw))
            {
                // Remove comment symbols //, /*, and */
                metaDataRaw = metaDataRaw.TrimStart(new[] { '/', '*' }).TrimEnd(new[] { '/', '*' }).Trim();
                metaDataObj = JObject.Parse(metaDataRaw);
            }

            return metaDataObj;
        }

        public void CreateNode(LocalDeclarationStatementSyntax instantiation)
        {
            try
            {
                var variableName = instantiation.Declaration.Variables.First().Identifier.ToString();

                var fullType = (instantiation.Declaration.Variables.First().Initializer.Value as ObjectCreationExpressionSyntax)?.Type ?? null;

                if (fullType == null) return;

                var metaDataRaw = instantiation.GetTrailingTrivia().ToString();
                JObject metaDataObj = null;
                try
                {
                    metaDataObj = ParseMetaData(metaDataRaw);
                }
                catch (Exception e)
                {
                    Logging.Log($"Failed to parse the following metadata for node {variableName}: {metaDataRaw}");
                }
                

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
                    modelTemplate = ModelManager.CreateDummyAbstractionModel("UNDEFINED");
                }

                var model = new AbstractionModel();
                model.CloneFrom(modelTemplate);

                model.Name = variableName;
                model.SetValue("InstanceName", $"\"{variableName}\"");
                model.SetGenerics(generics);

                var initialiser = instantiation.Declaration.Variables.FirstOrDefault()?.Initializer.Value as ObjectCreationExpressionSyntax;

                // Get constructor arguments
                var constructorArgs = initialiser.ArgumentList.Arguments;
                var unnamedArgCount = 0;
                foreach (var constructorArg in constructorArgs)
                {
                    string argName;
                    if (constructorArg.NameColon == null)
                    {
                        argName = $"~arg{unnamedArgCount}";
                        unnamedArgCount++;
                        model.AddConstructorArg(argName, type: "Unnamed constructor argument");
                    }
                    else
                    {
                        argName = constructorArg.NameColon.ToString().Trim(':');
                    }

                    var argValue = constructorArg.Expression.ToString();

                    model.SetValue(argName, argValue, initialise: true);

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

                model.RefreshGenerics();
                
                ALANode node;

                if (!_nodesByName.ContainsKey(model.Name))
                {
                    node = CreateNodeFromModel(model);
                    _nodesByName[node.Name] = node; 
                }
                else
                {
                    node = _nodesByName[model.Name];
                    node.Model.CloneFrom(model);
                }

                node.MetaData = metaDataObj;
                if (node.Model.Type == "UNDEFINED")
                {
                    node.IsReferenceNode = true;
                }

                node.UpdateUI();

                // if (!_rootCreated)
                // {
                //     Graph.Roots.Add(node);
                //     Canvas.Children.Add(node.Render);
                //     _rootCreated = true;
                // }
            }
            catch (Exception e)
            {
                Logging.Log($"Failed to parse node instantiation in CreateDiagramFromCode.\nException: {e}");
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

            JObject metaDataObj = null;
            var metaDataRaw = wireTo.GetTrailingTrivia().ToString();
            try
            {
                metaDataObj = ParseMetaData(metaDataRaw);
            }
            catch (Exception e)
            {
                Logging.Log($"Failed to parse the following metadata for wire {sourceName} -> {destinationName}: {metaDataRaw}");
            }

            if (!_nodesByName.ContainsKey(sourceName))
            {
                Logging.Log($"Error: No instantiation for {sourceName} found. Creating a dummy one to use instead.");

                var dummyInstantiation = CreateDummyInstantiation("UNDEFINED", sourceName);
                CreateNode(dummyInstantiation);
            }

            if (!_nodesByName.ContainsKey(destinationName))
            {
                Logging.Log($"Error: No instantiation for {destinationName} found. Creating a dummy one to use instead.");

                var dummyInstantiation = CreateDummyInstantiation("UNDEFINED", destinationName);
                CreateNode(dummyInstantiation);
            }

            var source = _nodesByName[sourceName];
            var destination = _nodesByName[destinationName];

            bool isReversedWire = false;

            if (metaDataObj != null)
            {
                if (metaDataObj.ContainsKey("IsReversed")) isReversedWire = bool.Parse(metaDataObj.GetValue("IsReversed")?.ToString() ?? "false");

                if (source.Model.Type == "UNDEFINED" && metaDataObj.ContainsKey("SourceType"))
                {
                    var newType = metaDataObj.GetValue("SourceType").ToString();
                    source.ChangeTypeInUI(newType);
                    source.TypeChanged?.Invoke(newType);
                }

                if (metaDataObj.ContainsKey("SourceGenerics"))
                {
                    var generics = metaDataObj["SourceGenerics"].Values().ToList();

                    for (var i = 0; i < generics.Count(); i++)
                    {
                        source.Model.UpdateGeneric(i, generics[i].Value<string>());
                    }

                    source.Model.RefreshGenerics();
                    source.UpdateUI();
                }

                if (destination.Model.Type == "UNDEFINED" && metaDataObj.ContainsKey("DestinationType"))
                {
                    var newType = metaDataObj.GetValue("DestinationType").ToString();
                    destination.ChangeTypeInUI(newType);
                    destination.TypeChanged?.Invoke(newType);
                }

                if (metaDataObj.ContainsKey("DestinationGenerics"))
                {
                    var generics = metaDataObj["DestinationGenerics"].Values().ToList();

                    for (var i = 0; i < generics.Count(); i++)
                    {
                        destination.Model.UpdateGeneric(i, generics[i].Value<string>());
                    }

                    destination.Model.RefreshGenerics();
                    destination.UpdateUI();
                }

                if (metaDataObj.ContainsKey("SourceIsReference")) source.IsReferenceNode = bool.Parse(metaDataObj.GetValue("SourceIsReference").ToString());
                if (metaDataObj.ContainsKey("DestinationIsReference")) destination.IsReferenceNode = bool.Parse(metaDataObj.GetValue("DestinationIsReference").ToString());
            }

            var sourcePortBox = !string.IsNullOrEmpty(sourcePortName) ? source.GetPortBox(sourcePortName) : source.GetSelectedPort(inputPort: false);
            var sourcePort = (Port)sourcePortBox.Payload;
            var destinationPortBox = FindMatchingPortBox(sourcePort, destination, !sourcePort.IsReversePort);

            if (destinationPortBox == null)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Failed to parse WireTo in CreateDiagramFromCode from line: {wireTo}");
                sb.AppendLine($"source: {source.Model.Type} {source.Name}");
                sb.AppendLine($"destination: {destination.Model.Type} {destination.Name}");
                sb.AppendLine($"sourcePort: {(sourcePortBox.Payload is Port port ? port.Type + " " + port.Name : "")}");
                sb.AppendLine("destinationPort: None found that match sourcePort in destination.");

                Logging.Log(sb.ToString());
                return;
            }

            var destinationPort = (Port)destinationPortBox.Payload;

            if (isReversedWire || sourcePort.IsReversePort) // Swap source and destination variables
            {
                object temp = source;
                source = destination;
                destination = (ALANode)temp;

                temp = sourceName;
                sourceName = destinationName;
                destinationName = (string)temp;

                temp = sourcePort;
                sourcePort = destinationPort;
                destinationPort = (Port)temp;

                temp = sourcePortBox;
                sourcePortBox = destinationPortBox;
                destinationPortBox = (Box)temp;
            }

            if (string.IsNullOrEmpty(_firstRootVarName)) _firstRootVarName = sourceName;
            if (sourceName.StartsWith("root_") && !_roots.Contains(sourceName))
            {
                _roots.Add(sourceName);
                if (!Graph.Roots.Contains(source)) Graph.Roots.Add(source);
                source.IsRoot = true;
                _autoCreatedRoots.Add(sourceName);
            }

            SolidColorBrush wireColour = null;
            if (!_nodesWithTreeParents.Add(destinationName))
            {
                wireColour = new SolidColorBrush(Colors.Black);
                wireColour.Opacity = 0.1;
            }
            // if (_autoCreatedRoots.Contains(destinationName))
            // {
            //     _autoCreatedRoots.Remove(destinationName);
            //     _nodesWithoutTreeParents.Remove(destinationName);
            //     Graph.Roots.RemoveAll(o => o.Equals(destination));
            //     destination.IsRoot = false;
            // }

            if (!_nodesWithTreeParents.Contains(sourceName))
            {
                if (!_nodesWithoutTreeParents.Contains(sourceName) && !_roots.Contains(sourceName))
                {
                    if (!Graph.Roots.Contains(source)) Graph.Roots.Add(source);
                    _nodesWithoutTreeParents.Add(sourceName);
                    source.IsRoot = true;
                    _autoCreatedRoots.Add(sourceName);
                }
            }

            if (_nodesWithoutTreeParents.Contains(destinationName) && destinationName != _firstRootVarName && !_roots.Contains(destinationName))
            {
                _nodesWithoutTreeParents.Remove(destinationName);
                Graph.Roots.RemoveAll(o => o.Equals(destination));
                destination.IsRoot = false;
            }

            var wire = CreateALAWire(source, destination, sourcePortBox, destinationPortBox);
            if (metaDataObj != null) wire.MetaData = metaDataObj;
            if (wireColour != null) wire.WireColour = wireColour;

            Graph.AddEdge(wire);

            _wiresById[wire.Id] = wire;

            // if (!Canvas.Children.Contains(source.Render)) Canvas.Children.Add(source.Render);
            // if (!Canvas.Children.Contains(destination.Render)) Canvas.Children.Add(destination.Render);
            //
            // wire.Paint();
        }

        private void PaintAllWires()
        {
            var wires = _wiresById.Values.ToList();
            foreach (var wire in wires)
            {
                if (!Canvas.Children.Contains(wire.Source.Render)) Canvas.Children.Add(wire.Source.Render);
                if (!Canvas.Children.Contains(wire.Destination.Render)) Canvas.Children.Add(wire.Destination.Render);

                wire.Paint();
            }
        }

        private Box FindMatchingPortBox(Port portToMatch, ALANode destination, bool getInputPort = true)
        {
            var ports = getInputPort ? destination.GetImplementedPorts() : destination.GetAcceptedPorts();
            var matchingPort = ports.FirstOrDefault(p => p.Type == portToMatch.Type);
            if (matchingPort == null) matchingPort = ports.FirstOrDefault(p => Regex.IsMatch(portToMatch.Type, $@"List<{p.Type}>"));
            if (matchingPort == null) matchingPort = ports.FirstOrDefault(p => p.IsInputPort == getInputPort);

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
                SourcePortBox = sourcePort,
                DestinationPortBox = destinationPort,
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

            if (model.Type == "UNDEFINED")
            {
                node.NodeBackground = Brushes.Orange;
                var sb = new StringBuilder();

                sb.AppendLine("Error: No instantiation for this instance was found in the source code. Please add one in the code, and then regenerate this diagram.");
                sb.AppendLine("");
                sb.AppendLine("If this instance is a global variable, then simply set a dummy instantiation in the auto-generated instantiations area, and in ");
                sb.AppendLine("between that area and the wiring code area, assign a reference from the global variable to that dummy instantiation, so that the");
                sb.AppendLine("global variable is used in the WireTos instead of the dummy instantiation.");
                sb.AppendLine("");
                sb.AppendLine("The dummy instantiation will appear in the diagram, so maybe give it an InstanceName of \"Global variable\" to make it clearer");
                sb.AppendLine("to anyone else viewing the diagram.");

                var documentation = sb.ToString();
                model.AddDocumentation(documentation);
            }

            node.Model = model;
            node.Graph = Graph;
            node.Canvas = Canvas;
            node.StateTransition = StateTransition;
            node.AvailableAbstractions.AddRange(
                ModelManager.GetAbstractionTypes());

            node.TypeChanged += newType =>
            {
                var regularNodeColour = Utilities.BrushFromHex("#d2ecf9");
                var referenceNodeColour = Brushes.Orange;
                node.NodeBackground = !node.IsReferenceNode ? regularNodeColour : referenceNodeColour; 

                node.LoadDefaultModel(ModelManager.GetAbstractionModel(newType));

                node.UpdateUI();

                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    var edges = Graph.Edges;
                    foreach (var edge in edges)
                    {
                        (edge as ALAWire)?.Refresh();
                    }

                    RefreshLayout.Execute();

                }, DispatcherPriority.ContextIdle);
            };

            Graph.AddNode(node);
            node.CreateInternals();
            node.LoadDefaultModel(node.Model);

            if (draw) Canvas.Children.Add(node.Render);

            return node;
        }

        public CreateDiagramFromCode()
        {
            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR main
            DataFlowConnector<string> startCreation = new DataFlowConnector<string>() {InstanceName="startCreation"}; /* {"IsRoot":true} */
            ForEach<LocalDeclarationStatementSyntax> id_fcaabe33216f4a58a93b0b2ef5f15010 = new ForEach<LocalDeclarationStatementSyntax>() {InstanceName="id_fcaabe33216f4a58a93b0b2ef5f15010"}; /* {"IsRoot":false} */
            DispatcherData<LocalDeclarationStatementSyntax> id_e9b477643ed94a8c9247968436584d38 = new DispatcherData<LocalDeclarationStatementSyntax>() {InstanceName="id_e9b477643ed94a8c9247968436584d38",Priority=DispatcherPriority.ApplicationIdle}; /* {"IsRoot":false} */
            DataFlowConnector<LocalDeclarationStatementSyntax> id_1cf09862061644bd81f58e0965b6420a = new DataFlowConnector<LocalDeclarationStatementSyntax>() {InstanceName="id_1cf09862061644bd81f58e0965b6420a"}; /* {"IsRoot":false} */
            ApplyAction<LocalDeclarationStatementSyntax> id_1588adb8d64b4fe6876b20a0eb5075fd = new ApplyAction<LocalDeclarationStatementSyntax>() {InstanceName="id_1588adb8d64b4fe6876b20a0eb5075fd",Lambda=instantiation =>{    _instCount++;    Logging.Message($"Creating node {_instCount}/{_instTotal}...");}}; /* {"IsRoot":false} */
            ApplyAction<LocalDeclarationStatementSyntax> id_b7f04a7310894b7a884b171a3bade791 = new ApplyAction<LocalDeclarationStatementSyntax>() {InstanceName="id_b7f04a7310894b7a884b171a3bade791",Lambda=CreateNode}; /* {"IsRoot":false} */
            EventLambda id_80f41b60cacd493c84808b1c4c8755f1 = new EventLambda() {InstanceName="id_80f41b60cacd493c84808b1c4c8755f1",Lambda=() =>{    _instCount = 0;    Logging.Message($"{_nodesByName.Keys.Count}/{_instTotal} nodes created");}}; /* {"IsRoot":false} */
            Apply<string, IEnumerable<LocalDeclarationStatementSyntax>> getInstantiations = new Apply<string, IEnumerable<LocalDeclarationStatementSyntax>>() {InstanceName="getInstantiations ",Lambda=GetInstantiations}; /* {"IsRoot":false} */
            Apply<string, IEnumerable<ExpressionStatementSyntax>> id_8c692c6a27f449619146ed6dd8d9c621 = new Apply<string, IEnumerable<ExpressionStatementSyntax>>() {InstanceName="id_8c692c6a27f449619146ed6dd8d9c621",Lambda=GetWireTos}; /* {"IsRoot":false} */
            ForEach<ExpressionStatementSyntax> id_22451793e0224fb4b2f387639abc3ff6 = new ForEach<ExpressionStatementSyntax>() {InstanceName="id_22451793e0224fb4b2f387639abc3ff6"}; /* {"IsRoot":false} */
            DataFlowConnector<ExpressionStatementSyntax> id_3d924167517f4128bb970f478986843e = new DataFlowConnector<ExpressionStatementSyntax>() {InstanceName="id_3d924167517f4128bb970f478986843e"}; /* {"IsRoot":false} */
            DispatcherData<ExpressionStatementSyntax> id_1475c43393ee4fc99a723c2e19c9630f = new DispatcherData<ExpressionStatementSyntax>() {InstanceName="id_1475c43393ee4fc99a723c2e19c9630f",Priority=DispatcherPriority.ApplicationIdle}; /* {"IsRoot":false} */
            ApplyAction<ExpressionStatementSyntax> id_19d1288ca6b6498a97baa8269a45462c = new ApplyAction<ExpressionStatementSyntax>() {InstanceName="id_19d1288ca6b6498a97baa8269a45462c",Lambda=wireTo =>{    _wireToCount++;    Logging.Message($"Creating wire {_wireToCount}/{_wireToTotal}...");}}; /* {"IsRoot":false} */
            ApplyAction<ExpressionStatementSyntax> id_f5021eb48f0d462895ac67cd14f14031 = new ApplyAction<ExpressionStatementSyntax>() {InstanceName="id_f5021eb48f0d462895ac67cd14f14031",Lambda=CreateWire}; /* {"IsRoot":false} */
            DataFlowConnector<IEnumerable<LocalDeclarationStatementSyntax>> id_09ac5cb7b8854dffa0d0755e4d99d4f9 = new DataFlowConnector<IEnumerable<LocalDeclarationStatementSyntax>>() {InstanceName="id_09ac5cb7b8854dffa0d0755e4d99d4f9"}; /* {"IsRoot":false} */
            ApplyAction<IEnumerable<LocalDeclarationStatementSyntax>> id_2906f2e0dee248f5abe56872822e6ca7 = new ApplyAction<IEnumerable<LocalDeclarationStatementSyntax>>() {InstanceName="id_2906f2e0dee248f5abe56872822e6ca7",Lambda=input =>{    _instTotal = input.Count();}}; /* {"IsRoot":false} */
            DataFlowConnector<IEnumerable<ExpressionStatementSyntax>> id_eead8c20b8644a5b99bab14b5c783d0c = new DataFlowConnector<IEnumerable<ExpressionStatementSyntax>>() {InstanceName="id_eead8c20b8644a5b99bab14b5c783d0c"}; /* {"IsRoot":false} */
            ApplyAction<IEnumerable<ExpressionStatementSyntax>> id_8abe632830344b4585a69c3fe4d099a7 = new ApplyAction<IEnumerable<ExpressionStatementSyntax>>() {InstanceName="id_8abe632830344b4585a69c3fe4d099a7",Lambda=input =>{    _wireToTotal = input.Count();}}; /* {"IsRoot":false} */
            ConvertToEvent<string> id_c55992ef95c5435499982c2cd8e1b746 = new ConvertToEvent<string>() {InstanceName="id_c55992ef95c5435499982c2cd8e1b746"}; /* {"IsRoot":false} */
            Stopwatch id_cafde6e0d8754dd1a5bb3b699628ef4a = new Stopwatch() {InstanceName="id_cafde6e0d8754dd1a5bb3b699628ef4a"}; /* {"IsRoot":false} */
            Data<string> id_c4326ea59fdb4fc88c3eaaf12f66400e = new Data<string>() {InstanceName="id_c4326ea59fdb4fc88c3eaaf12f66400e",Lambda=() =>{    _wireToCount = 0;    return $"{_wiresById.Keys.Count}/{_wireToTotal} wires created";}}; /* {"IsRoot":false} */
            DataFlowConnector<string> diagramCreatedMessage = new DataFlowConnector<string>() {InstanceName="diagramCreatedMessage"}; /* {"IsRoot":false} */
            ApplyAction<double> id_534f5b681fb746fcbbe0717eb7a6eef3 = new ApplyAction<double>() {InstanceName="id_534f5b681fb746fcbbe0717eb7a6eef3",Lambda=time =>{    Logging.Message($"{diagramCreatedMessage.Data} | Elapsed time: {time} seconds");}}; /* {"IsRoot":false} */
            EventConnector id_4ad815216ff8473ab0a61d1309adc7ac = new EventConnector() {InstanceName="id_4ad815216ff8473ab0a61d1309adc7ac"}; /* {"IsRoot":false} */
            EventLambda id_f26fca52bbc04d1d807e6e0da5150b83 = new EventLambda() {InstanceName="id_f26fca52bbc04d1d807e6e0da5150b83",Lambda=PaintAllWires}; /* {"IsRoot":false} */
            EventConnector id_7ae4a459ae0f4a6babc4f76085f973c6 = new EventConnector() {InstanceName="id_7ae4a459ae0f4a6babc4f76085f973c6"}; /* {"IsRoot":false} */
            DispatcherEvent id_c8785c2e47e042ae89c8c1281a0733f3 = new DispatcherEvent() {InstanceName="id_c8785c2e47e042ae89c8c1281a0733f3",Priority=DispatcherPriority.ApplicationIdle}; /* {"IsRoot":false} */
            EventConnector id_3f04fdee5e074be2acca707a7e00b38f = new EventConnector() {InstanceName="id_3f04fdee5e074be2acca707a7e00b38f"}; /* {"IsRoot":false} */
            EventLambda id_24ac836ec52c4df5ac9699594045b35b = new EventLambda() {Lambda=() => Logging.Message("Rendering diagram...")}; /* {"IsRoot":false} */
            // END AUTO-GENERATED INSTANTIATIONS FOR main

            // BEGIN AUTO-GENERATED WIRING FOR main
            startCreation.WireTo(id_c55992ef95c5435499982c2cd8e1b746, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"ConvertToEvent","DestinationIsReference":false} */
            id_fcaabe33216f4a58a93b0b2ef5f15010.WireTo(id_1cf09862061644bd81f58e0965b6420a, "elementOutput"); /* {"SourceType":"ForEach","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false} */
            id_fcaabe33216f4a58a93b0b2ef5f15010.WireTo(id_80f41b60cacd493c84808b1c4c8755f1, "complete"); /* {"SourceType":"ForEach","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false} */
            id_e9b477643ed94a8c9247968436584d38.WireTo(id_1588adb8d64b4fe6876b20a0eb5075fd, "delayedData"); /* {"SourceType":"DispatcherData","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false} */
            id_1cf09862061644bd81f58e0965b6420a.WireTo(id_e9b477643ed94a8c9247968436584d38, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"DispatcherData","DestinationIsReference":false} */
            id_1cf09862061644bd81f58e0965b6420a.WireTo(id_b7f04a7310894b7a884b171a3bade791, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false} */
            getInstantiations.WireTo(id_09ac5cb7b8854dffa0d0755e4d99d4f9, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false} */
            id_8c692c6a27f449619146ed6dd8d9c621.WireTo(id_eead8c20b8644a5b99bab14b5c783d0c, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false} */
            id_22451793e0224fb4b2f387639abc3ff6.WireTo(id_3d924167517f4128bb970f478986843e, "elementOutput"); /* {"SourceType":"ForEach","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false} */
            id_3d924167517f4128bb970f478986843e.WireTo(id_1475c43393ee4fc99a723c2e19c9630f, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"DispatcherData","DestinationIsReference":false} */
            id_3d924167517f4128bb970f478986843e.WireTo(id_f5021eb48f0d462895ac67cd14f14031, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false} */
            id_1475c43393ee4fc99a723c2e19c9630f.WireTo(id_19d1288ca6b6498a97baa8269a45462c, "delayedData"); /* {"SourceType":"DispatcherData","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false} */
            id_09ac5cb7b8854dffa0d0755e4d99d4f9.WireTo(id_2906f2e0dee248f5abe56872822e6ca7, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false} */
            id_09ac5cb7b8854dffa0d0755e4d99d4f9.WireTo(id_fcaabe33216f4a58a93b0b2ef5f15010, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"ForEach","DestinationIsReference":false} */
            id_eead8c20b8644a5b99bab14b5c783d0c.WireTo(id_8abe632830344b4585a69c3fe4d099a7, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false} */
            id_eead8c20b8644a5b99bab14b5c783d0c.WireTo(id_22451793e0224fb4b2f387639abc3ff6, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"ForEach","DestinationIsReference":false} */
            id_c55992ef95c5435499982c2cd8e1b746.WireTo(id_cafde6e0d8754dd1a5bb3b699628ef4a, "eventOutput"); /* {"SourceType":"ConvertToEvent","SourceIsReference":false,"DestinationType":"Stopwatch","DestinationIsReference":false} */
            startCreation.WireTo(getInstantiations, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false} */
            startCreation.WireTo(id_8c692c6a27f449619146ed6dd8d9c621, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false} */
            id_c4326ea59fdb4fc88c3eaaf12f66400e.WireTo(diagramCreatedMessage, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false} */
            id_cafde6e0d8754dd1a5bb3b699628ef4a.WireTo(id_534f5b681fb746fcbbe0717eb7a6eef3, "elapsedSeconds"); /* {"SourceType":"Stopwatch","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false} */
            id_7ae4a459ae0f4a6babc4f76085f973c6.WireTo(id_c4326ea59fdb4fc88c3eaaf12f66400e, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false} */
            id_4ad815216ff8473ab0a61d1309adc7ac.WireTo(id_cafde6e0d8754dd1a5bb3b699628ef4a, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"Stopwatch","DestinationIsReference":false} */
            id_22451793e0224fb4b2f387639abc3ff6.WireTo(id_7ae4a459ae0f4a6babc4f76085f973c6, "complete"); /* {"SourceType":"ForEach","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false} */
            id_c8785c2e47e042ae89c8c1281a0733f3.WireTo(id_3f04fdee5e074be2acca707a7e00b38f, "delayedEvent"); /* {"SourceType":"DispatcherEvent","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false} */
            id_3f04fdee5e074be2acca707a7e00b38f.WireTo(id_f26fca52bbc04d1d807e6e0da5150b83, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false} */
            id_3f04fdee5e074be2acca707a7e00b38f.WireTo(id_4ad815216ff8473ab0a61d1309adc7ac, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false} */
            id_7ae4a459ae0f4a6babc4f76085f973c6.WireTo(id_24ac836ec52c4df5ac9699594045b35b, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false} */
            id_7ae4a459ae0f4a6babc4f76085f973c6.WireTo(id_c8785c2e47e042ae89c8c1281a0733f3, "complete"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"DispatcherEvent","DestinationIsReference":false} */
            // END AUTO-GENERATED WIRING FOR main

            // Instance mapping
            _startCreation = startCreation;
        }

    }
}


































