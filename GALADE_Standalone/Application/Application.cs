using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;
using WPFCanvas = System.Windows.Controls.Canvas;
using System.IO;
using System.Windows.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Application
{
    /// <summary>
    /// This version of GALADE is standalone, i.e. it is a single executable.
    /// </summary>
    public class Application
    {
        // Public fields and properties

        // Private fields
        private MainWindow mainWindow = new MainWindow("GALADE");
        private DataFlowConnector<Dictionary<string, JToken>> abstractionTemplatesConnector = new DataFlowConnector<Dictionary<string, JToken>> { InstanceName = "abstractionTemplatesConnector" };

        // Methods
        private Application Initialize()
        {
            Wiring.PostWiringInitialize();
            return this;
        }

        private void AddNewNode(VisualPortGraph graph, StateTransition<Enums.DiagramMode> stateTransition, UndoHistory undoHistory, VisualStyle nodeStyle, VisualStyle portStyle)
        {
            VisualPortGraphNode newNode = new VisualPortGraphNode()
            {
                Graph = graph,
                StateTransition = stateTransition,
                NodeStyle = nodeStyle,
                PortStyle = portStyle,
                Ports = new List<Port> { new Port() { Type = "Port", Name = "p0", IsInputPort = true } }
            };

            newNode.ActionPerformed += undoHistory.Push;
            newNode.Initialise();

            newNode.ContextMenu = (new VPGNContextMenu() as IUI).GetWPFElement();

            if (graph.GetRoot() == null)
            {
                graph.AddNode(newNode);
            }

            var templates = abstractionTemplatesConnector.Data;
        }

        [STAThread]
        public static void Main()
        {
            Application app = new Application();
            app.Initialize().mainWindow.Run();
        }

        private Application()
        {
            #region Set up directory and file paths
            string APP_DIRECTORY = Utilities.GetApplicationDirectory();

            var userGuidePaths = new Dictionary<string, string>()
            {
                { "Functionality", System.IO.Path.Combine(APP_DIRECTORY, "Documentation/newUserGuide_Functionality.txt") },
                { "Controls", System.IO.Path.Combine(APP_DIRECTORY, "Documentation/newUserGuide_Controls.txt") },
                { "Menu", System.IO.Path.Combine(APP_DIRECTORY, "Documentation/newUserGuide_Menu.txt") }
            };

            string SETTINGS_FILEPATH = System.IO.Path.Combine(APP_DIRECTORY, "settings.json");
            string WIRING_LOG_FILEPATH = System.IO.Path.Combine(APP_DIRECTORY, "wiringLog.log");
            string RUNTIME_LOG_FILEPATH = System.IO.Path.Combine(APP_DIRECTORY, "runtimeLog.log");
            string LOG_ARCHIVE_DIRECTORY = System.IO.Path.Combine(APP_DIRECTORY, "Logs");
            string BACKUPS_DIRECTORY = System.IO.Path.Combine(APP_DIRECTORY, "Backups");

            // Initialise and clear logs
            if (!System.IO.Directory.Exists(APP_DIRECTORY)) System.IO.Directory.CreateDirectory(APP_DIRECTORY);
            if (!System.IO.Directory.Exists(LOG_ARCHIVE_DIRECTORY)) System.IO.Directory.CreateDirectory(LOG_ARCHIVE_DIRECTORY);
            if (!System.IO.Directory.Exists(BACKUPS_DIRECTORY)) System.IO.Directory.CreateDirectory(BACKUPS_DIRECTORY);
            Logging.WriteText(path: WIRING_LOG_FILEPATH, content: "", createNewFile: true); // Create a blank log for wiring
            Logging.WriteText(path: RUNTIME_LOG_FILEPATH, content: "", createNewFile: true); // Create a blank log for all exceptions and general runtime output

            if (!File.Exists(SETTINGS_FILEPATH))
            {
                var obj = new JObject();
                obj["LatestDiagramFilePath"] = "";
                obj["LatestCodeFilePath"] = "";
                obj["ProjectFolderPath"] = "";

                File.WriteAllText(SETTINGS_FILEPATH, obj.ToString());
            }
            #endregion

            #region Diagram constants and singletons
            VisualPortGraph mainGraph = new VisualPortGraph() { InstanceName = "MainGraph", DebugOutputAll = false };
            StateTransition<Enums.DiagramMode> stateTransition = new StateTransition<Enums.DiagramMode>(Enums.DiagramMode.Idle)
            {
                InstanceName = "stateTransition",
                Matches = (flag, currentState) => (flag & currentState) != 0
            };

            UndoHistory undoHistory = new UndoHistory() { InstanceName = "graphHistory" };
            mainGraph.ActionPerformed += source => undoHistory.Push(source);

            var PRIMARY_UX_BG_COLOUR = new SolidColorBrush(Color.FromRgb(249, 249, 249));
            var PRIMARY_UX_FG_COLOUR = Brushes.Black;

            VisualStyle defaultStyle = new VisualStyle();

            VisualStyle nodeStyle = new VisualStyle()
            {
                Background = Brushes.LightSkyBlue,
                BackgroundHighlight = Brushes.Aquamarine,
                Foreground = Brushes.Black,
                Border = Brushes.Black,
                BorderHighlight = Brushes.Orange,
                BorderThickness = 3,
                Width = 200,
                Height = 50
            };

            VisualStyle portStyle = new VisualStyle()
            {
                Background = Brushes.White,
                Foreground = Brushes.Black,
                Border = Brushes.Black,
                BorderHighlight = Brushes.LightSkyBlue,
                BorderThickness = 1,
                Width = 50,
                Height = 25
            };

            VisualStyle dragRectStyle = new VisualStyle()
            {
                Background = new SolidColorBrush(Color.FromArgb(100, 171, 233, 255)),
                Border = Brushes.LightSkyBlue,
                BorderThickness = 1,
            };

            #endregion

            #region Set up logging
            Wiring.Output += output => Logging.Log(output, WIRING_LOG_FILEPATH); // Print all WireTos to a log file
            Logging.LogOutput += output =>
            {
                if (output is Exception)
                {
                    Logging.Log(output as Exception, RUNTIME_LOG_FILEPATH);
                }
                else if (output is string)
                {
                    Logging.Log(output as string, RUNTIME_LOG_FILEPATH);
                }
                else
                {
                    Logging.Log(output, RUNTIME_LOG_FILEPATH);
                }
            };

            AppDomain.CurrentDomain.FirstChanceException += (sender, e) => Logging.Log(e.Exception, RUNTIME_LOG_FILEPATH);

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                // Save a timestamped copy of the current runtime log
                Logging.Log(e.ExceptionObject as Exception ?? e.ExceptionObject, RUNTIME_LOG_FILEPATH);
                File.Copy(RUNTIME_LOG_FILEPATH, System.IO.Path.Combine(LOG_ARCHIVE_DIRECTORY, $"{Utilities.GetCurrentTime()}.log")); // Archive current log when app shuts down unexpectedly

                // Save a timestamped backup of the current diagram
                var diagramContents = mainGraph.Serialise();
                File.WriteAllText(System.IO.Path.Combine(BACKUPS_DIRECTORY, $"{Utilities.GetCurrentTime()}.ala"), diagramContents);
            };
            #endregion

            // BEGIN MANUAL INSTANTIATIONS
            // END MANUAL INSTANTIATIONS

            // BEGIN MANUAL WIRING
            // END MANUAL WIRING

            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR Application.xmind
            AddConnectionToGraph addPastedConnections = new AddConnectionToGraph() { InstanceName = "addPastedConnections", Graph = mainGraph, StateTransition = stateTransition, UndoHistory = undoHistory };
            AddConnectionToGraph id_1fda77ab37d34539ae1ad713cba306e9 = new AddConnectionToGraph() { InstanceName = "Default", Graph = mainGraph, StateTransition = stateTransition, UndoHistory = undoHistory };
            AddConnectionToGraph id_33d40c1315df4a329c8069455f07bf4c = new AddConnectionToGraph() { InstanceName = "Default", Graph = mainGraph, StateTransition = stateTransition, UndoHistory = undoHistory };
            AddConnectionToGraph id_9193d48caa854fdc953c8b4ada85fa0f = new AddConnectionToGraph() { InstanceName = "Default", Graph = mainGraph, StateTransition = stateTransition, UndoHistory = undoHistory };
            Apply<Dictionary<string,JToken>,List<string>> id_eb63936eb29948f9bef6b22f54b7abcd = new Apply<Dictionary<string,JToken>,List<string>>() { InstanceName = "Default", Lambda = d => d.Keys.OrderBy(s => s).ToList() };
            Apply<IVisualPortGraphNode,string> id_a4edfc365ec64fd7b366d694cfc078f8 = new Apply<IVisualPortGraphNode,string>() { InstanceName = "Default", Lambda = node => mainGraph.GetNodeSubtree(node.Id) };
            Apply<IVisualPortGraphNode,string> id_cd868960a86e4e578f0b936f7c309268 = new Apply<IVisualPortGraphNode,string>() { InstanceName = "Default", Lambda = n => (n as VisualPortGraphNode).Type };
            Apply<IVisualPortGraphNode,Tuple<string,JToken>> id_2a44ae50994a441b97eae994df2bb1d2 = new Apply<IVisualPortGraphNode,Tuple<string,JToken>>() { InstanceName = "Default", Lambda = n => Tuple.Create((n as VisualPortGraphNode).Type,JToken.Parse(n.Serialise(new HashSet<string>() {"Id","Name","TreeParent"}))) };
            Apply<JToken,IVisualPortGraphNode> id_c1189041ac444838bf1b62f8dfa25929 = new Apply<JToken,IVisualPortGraphNode>() { InstanceName = "Default", Lambda = jt => mainGraph.GetNode(jt.ToString()) };
            Apply<JToken,List<string>> createNewNodeIds = new Apply<JToken,List<string>>() { InstanceName = "createNewNodeIds", Lambda = jt => ((JArray)jt).Select(o => Utilities.GetUniqueId().ToString()).ToList() };
            Apply<JToken,List<string>> getOriginalNodeIds = new Apply<JToken,List<string>>() { InstanceName = "getOriginalNodeIds", Lambda = jt => ((JArray)jt).Select(o => o.ToString()).ToList() };
            Apply<List<string>,Dictionary<string,string>> id_5ada023340cc46bdbe9a1cc5668e14f9 = new Apply<List<string>,Dictionary<string,string>>() { InstanceName = "Default", Lambda = input => {var dict = new Dictionary<string,string>();foreach (var s in input) {var pair = s.Split(new []{'='},2);dict[pair[0].Trim()] = pair[1].Trim();}return dict;} };
            Apply<List<string>,Dictionary<string,string>> id_bea8af68fc04408986c8dbad2cbf795e = new Apply<List<string>,Dictionary<string,string>>() { InstanceName = "Default", Lambda = input => {var dict = new Dictionary<string,string>();foreach (var s in input) {var pair = s.Split(new []{':'},2);dict[pair[0].Trim()] = pair[1].Trim();}return dict;} };
            Apply<Point,string> id_f2277566d5da4570829eb23ba276ef28 = new Apply<Point,string>() { InstanceName = "Default", Lambda = point => $"({(int)point.X}," + " " + $"{(int)point.Y})" };
            Apply<string,bool> id_bd9c43f53b5043c8b76e6f6912138554 = new Apply<string,bool>() { InstanceName = "Default", Lambda = s => !string.IsNullOrEmpty(s) };
            Apply<string,Dictionary<string,JToken>> getTemplatesFromJSON = new Apply<string,Dictionary<string,JToken>>() { InstanceName = "getTemplatesFromJSON", Lambda = json => JsonConvert.DeserializeObject<Dictionary<string,JToken>>(json) };
            Apply<string,IEnumerable<string>> id_439d102714d245929de8b14c55d40c1d = new Apply<string,IEnumerable<string>>() { InstanceName = "Default", Lambda = input => {return input.Split(new [] { Environment.NewLine},StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());} };
            Apply<string,IEnumerable<string>> id_7af338faa6bb48c29d1b583377d6d0f0 = new Apply<string,IEnumerable<string>>() { InstanceName = "Default", Lambda = input => {return input.Split(new [] { Environment.NewLine},StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());} };
            Apply<Tuple<string,Dictionary<string,string>>,List<string>> id_0dc66c5de4cc4f639a4f8a4e68ca8a32 = new Apply<Tuple<string,Dictionary<string,string>>,List<string>>() { InstanceName = "Default", Lambda = t => t.Item2["ProvidedPorts"].Split(new[] {";"},StringSplitOptions.RemoveEmptyEntries).ToList() };
            Apply<Tuple<string,Dictionary<string,string>>,List<string>> id_a6b69c879e5c4be3b0b65df90c5f2aa3 = new Apply<Tuple<string,Dictionary<string,string>>,List<string>>() { InstanceName = "Default", Lambda = t => t.Item2["ImplementedPorts"].Split(new[] {";"},StringSplitOptions.RemoveEmptyEntries).ToList() };
            Apply<Tuple<string,Dictionary<string,string>>,string> id_94bc4bb4b0eb4992b073d93e6e62df61 = new Apply<Tuple<string,Dictionary<string,string>>,string>() { InstanceName = "Default", Lambda = t => t.Item2["AbstractionType"] };
            ApplyAction<IPortConnection> id_f42e25cc193c489aa8f52222148e000e = new ApplyAction<IPortConnection>() { InstanceName = "Default", Lambda = cxn => {var pgc = (cxn as PortGraphConnection); pgc.Opacity = pgc.Opacity < 1 ? 1.0 : 0.1;} };
            ApplyAction<IVisualPortGraphNode> id_19025a1dc84447a2a4487d854e4b3c09 = new ApplyAction<IVisualPortGraphNode>() { InstanceName = "Default", Lambda = n => (n as VisualPortGraphNode).PortsAreEditable = true };
            ApplyAction<IVisualPortGraphNode> id_2c7a7667a1a441a09259e9878a9d8319 = new ApplyAction<IVisualPortGraphNode>() { InstanceName = "Default", Lambda = input => {var node = input as VisualPortGraphNode;node.Deserialise(abstractionTemplatesConnector.Data["Data<T>"] as JObject); node.RecreateUI();} };
            ApplyAction<IVisualPortGraphNode> id_33fca3f47adf4ae09e8f6fcfacfc0b2d = new ApplyAction<IVisualPortGraphNode>() { InstanceName = "Default", Lambda = input => {var node = input as VisualPortGraphNode;node.Deserialise(abstractionTemplatesConnector.Data["Data<T>"] as JObject); node.RecreateUI();} };
            ApplyAction<IVisualPortGraphNode> id_54d29674558c4b2ab6340b22704eadff = new ApplyAction<IVisualPortGraphNode>() { InstanceName = "Default", Lambda = node => {mainGraph.SelectNode(node.Id); (node as VisualPortGraphNode)?.GetTypeTextBoxFocus();} };
            ApplyAction<IVisualPortGraphNode> id_a59158c8c97545dea21d401b8dbf77d4 = new ApplyAction<IVisualPortGraphNode>() { InstanceName = "Default", Lambda = n => { if (!mainGraph.NodeTypes.Contains((n as VisualPortGraphNode).Type)) { mainGraph.NodeTypes.Add((n as VisualPortGraphNode).Type); } } };
            ApplyAction<IVisualPortGraphNode> id_fc4da39a45c14a299cf156681769ddd1 = new ApplyAction<IVisualPortGraphNode>() { InstanceName = "Default", Lambda = n => (n as VisualPortGraphNode).PortsAreEditable = false };
            ApplyAction<JToken> loadNewAbstractionTypeTemplate = new ApplyAction<JToken>() { InstanceName = "loadNewAbstractionTypeTemplate", Lambda = jt => {var obj = jt as JObject; var node = mainGraph.GetSelectedNode() as VisualPortGraphNode; node?.Deserialise(obj); node?.RecreateUI();} };
            ApplyAction<KeyEventArgs> id_841df8fb3e4145e4845a4b87fb425b8e = new ApplyAction<KeyEventArgs>() { InstanceName = "Default", Lambda = args => args.Handled = true };
            ApplyAction<List<string>> id_47e785e85e8f4706a2cd54676ddeee07 = new ApplyAction<List<string>>() { InstanceName = "Default", Lambda = input => { mainGraph.NodeTypes = input; } };
            ApplyAction<string> id_f9a70bf4ccf346ec94c389aa45151170 = new ApplyAction<string>() { InstanceName = "Default", Lambda = input => {var cxn = mainGraph.GetConnection(input) as PortGraphConnection; cxn.SelectHandle(selectSourceHandle: false);} };
            ApplyAction<WPFCanvas> id_0b17dda4de874253b7ac486bd5f63d04 = new ApplyAction<WPFCanvas>() { InstanceName = "Default", Lambda = c => mainGraph.MainCanvas = c };
            Button id_2f3f5348a9f44aad90e115c61bde205a = new Button("Browse" ) { InstanceName = "Default", Margin = new Thickness(5) };
            Button id_4f49dc59a4974186ad62a7a7079019db = new Button("Browse" ) { InstanceName = "Default", Margin = new Thickness(5) };
            CanvasDisplay mainCanvas = new CanvasDisplay() { InstanceName = "mainCanvas", Width = 1920, Height = 600, Background = Brushes.White, StateTransition = stateTransition };
            Cast<List<Tuple<VisualPortGraphNode,VisualPortGraphNode,Port,Port>>,IEnumerable<Tuple<VisualPortGraphNode,VisualPortGraphNode,Port,Port>>> id_f16d8476ee2a49aeb64af180bfcc0fa7 = new Cast<List<Tuple<VisualPortGraphNode,VisualPortGraphNode,Port,Port>>,IEnumerable<Tuple<VisualPortGraphNode,VisualPortGraphNode,Port,Port>>>() { InstanceName = "Default" };
            Cast<string,object> id_03c7d3d4376a45b0b3d0369fd6196e73 = new Cast<string,object>() { InstanceName = "Default" };
            Cast<string,object> id_422e47c9692441a1a594dfe3aed6bd12 = new Cast<string,object>() { InstanceName = "Default" };
            Cast<string,object> id_ad62adfa4ea847359d1bd31e9cafde75 = new Cast<string,object>() { InstanceName = "Default" };
            ConvertToEvent<string> id_4b94ac7fd4434b4ea6751f501b8eaa9d = new ConvertToEvent<string>() { InstanceName = "Default" };
            ConvertToEvent<string> id_a0cf9e8153f24235b8672e8f2c5aef4d = new ConvertToEvent<string>() { InstanceName = "Default" };
            ConvertToEvent<string> id_bdc7b5211833463ba5ddde6249b71158 = new ConvertToEvent<string>() { InstanceName = "Default" };
            CreateAbstraction id_48f2e14b737742969006e5a067fde8da = new CreateAbstraction() { InstanceName = "Default", Layer = Enums.ALALayer.DomainAbstractions, WriteFile = true, FilePath = APP_DIRECTORY };
            CreateAbstraction id_7839777c9d57437f84412e1b498e1427 = new CreateAbstraction() { InstanceName = "Default", Layer = Enums.ALALayer.Application, WriteFile = true, FilePath = APP_DIRECTORY };
            CreateConnectionsFromJSON id_bd14b7b56e5b43d48292bb6ee2706f3f = new CreateConnectionsFromJSON() { InstanceName = "Default", Graph = mainGraph };
            CreateVPGNsFromJSON id_67ce70e081f94919b8ee47d4eb305b1f = new CreateVPGNsFromJSON() { InstanceName = "Default", Graph = mainGraph, StateTransition = stateTransition, UndoHistory = undoHistory, NodeStyle = nodeStyle, PortStyle = portStyle };
            Data<Dictionary<string,string>> id_d6a4e708014943de868c7290bd160681 = new Data<Dictionary<string,string>>() { InstanceName = "Default", storedData = new Dictionary<string,string>() {{"A","a"},{"B","b"}} };
            Data<IEnumerable<IPortConnection>> id_c81886b79d344934921bf9b69b8b3af9 = new Data<IEnumerable<IPortConnection>>() { InstanceName = "Default", Lambda = mainGraph.GetConnections };
            Data<IVisualPortGraphNode> id_23a5fe2b3587408e9df5e94256ead20a = new Data<IVisualPortGraphNode>() { InstanceName = "Default", Lambda = mainGraph.GetSelectedNode };
            Data<IVisualPortGraphNode> id_3784b9acfef8499795834a495d6f83a2 = new Data<IVisualPortGraphNode>() { InstanceName = "Default", Lambda = mainGraph.GetLatestNode };
            Data<IVisualPortGraphNode> id_5095928ff3f54c39aa0417b1cca9c969 = new Data<IVisualPortGraphNode>() { InstanceName = "Default", Lambda = mainGraph.GetSelectedNode };
            Data<IVisualPortGraphNode> id_63ed925f29684c5db385cefefb6b6dfa = new Data<IVisualPortGraphNode>() { InstanceName = "Default", Lambda = mainGraph.GetSelectedNode };
            Data<IVisualPortGraphNode> id_88a2f062f80d44cf82959f689ec9d48d = new Data<IVisualPortGraphNode>() { InstanceName = "Default", Lambda = mainGraph.GetSelectedNode };
            Data<IVisualPortGraphNode> id_8fcd9c561a954d528282d3b561f4fe9a = new Data<IVisualPortGraphNode>() { InstanceName = "Default", Lambda = mainGraph.GetSelectedNode };
            Data<IVisualPortGraphNode> id_a37f529cebeb4579833301089489c93b = new Data<IVisualPortGraphNode>() { InstanceName = "Default" };
            Data<IVisualPortGraphNode> id_ad282ca7505348ffa6e1580db6073003 = new Data<IVisualPortGraphNode>() { InstanceName = "Default", Lambda = mainGraph.GetSelectedNode };
            Data<IVisualPortGraphNode> id_d8af30a5ec074a94a9cbd052a5194f91 = new Data<IVisualPortGraphNode>() { InstanceName = "Default", Lambda = mainGraph.GetSelectedNode };
            Data<IVisualPortGraphNode> id_f243d49f78914970beda200fca69a288 = new Data<IVisualPortGraphNode>() { InstanceName = "Default", Lambda = mainGraph.GetSelectedNode };
            Data<IVisualPortGraphNode> refreshDiagramLayout = new Data<IVisualPortGraphNode>() { InstanceName = "refreshDiagramLayout", Lambda = mainGraph.GetRoot };
            Data<Port> id_4df5565e5cc74d80a2a2a85ae35373e6 = new Data<Port>() { InstanceName = "Default" };
            Data<Port> id_c4d15148a1e44f94b2c224e4ac006f2c = new Data<Port>() { InstanceName = "Default", Lambda = mainGraph.GetSelectedPort };
            Data<Port> id_ec235ee670574ba3a3c1b7882df3005b = new Data<Port>() { InstanceName = "Default", Lambda = mainGraph.GetSelectedPort };
            Data<Port> id_ff149259cafe456d9d100e67375dfa01 = new Data<Port>() { InstanceName = "Default", Lambda = mainGraph.GetSelectedPort };
            Data<string> id_20b52d9c5ffd4a40b0ec89ad945997c5 = new Data<string>() { InstanceName = "Default" };
            Data<string> id_28ff30e197fe4cb9b40ff44efcc0f3dd = new Data<string>() { InstanceName = "Default", storedData = "ConstructorArgs" };
            Data<string> id_307980ece0d34a65b2b69e58b0400ad7 = new Data<string>() { InstanceName = "Default", storedData = "Name" };
            Data<string> id_50e48187384540708c950df8cbd511e2 = new Data<string>() { InstanceName = "Default" };
            Data<string> id_52831ca26eb34f88867bc54a4011bc55 = new Data<string>() { InstanceName = "Default", storedData = "Properties" };
            Data<string> id_5db913688cf343229b5b022d9d9eef4d = new Data<string>() { InstanceName = "Default", storedData = "Type" };
            Data<string> id_b0c2ff43748f4ee4aabc3e9bed58b99d = new Data<string>() { InstanceName = "Default" };
            Data<string> setDomainAbstractionTemplatesFileLocation = new Data<string>() { InstanceName = "setDomainAbstractionTemplatesFileLocation", storedData = System.IO.Path.Combine(APP_DIRECTORY,"abstractionTemplates.json") };
            DataFlowConnector<Dictionary<string,string>> id_3ab6567c18b44f8698401afb8ba7b0c7 = new DataFlowConnector<Dictionary<string,string>>() { InstanceName = "Default" };
            DataFlowConnector<Dictionary<string,string>> id_41c01831f2c548eea710bc7766db0ee2 = new DataFlowConnector<Dictionary<string,string>>() { InstanceName = "Default" };
            DataFlowConnector<Dictionary<string,string>> id_e471de7bcb1845a1a60f0b824230a00f = new DataFlowConnector<Dictionary<string,string>>() { InstanceName = "Default" };
            DataFlowConnector<IVisualPortGraphNode> id_44388caa4a964fb588bbb8632f7b5fae = new DataFlowConnector<IVisualPortGraphNode>() { InstanceName = "Default" };
            DataFlowConnector<IVisualPortGraphNode> id_4bfcbdfaa7514af7ad35513cc6be635c = new DataFlowConnector<IVisualPortGraphNode>() { InstanceName = "Default" };
            DataFlowConnector<IVisualPortGraphNode> id_512b7d0231ac4dc59a1a9a22bcc6e650 = new DataFlowConnector<IVisualPortGraphNode>() { InstanceName = "Default" };
            DataFlowConnector<IVisualPortGraphNode> id_57f1e5bdd918406e849d9f0eb5c6f848 = new DataFlowConnector<IVisualPortGraphNode>() { InstanceName = "Default" };
            DataFlowConnector<IVisualPortGraphNode> id_5e6e3fe1f77d40f0ac8320a38a5ca32f = new DataFlowConnector<IVisualPortGraphNode>() { InstanceName = "Default" };
            DataFlowConnector<IVisualPortGraphNode> id_706acb0a3ea545b59bd8d3d022a1ddb5 = new DataFlowConnector<IVisualPortGraphNode>() { InstanceName = "Default" };
            DataFlowConnector<IVisualPortGraphNode> id_a5ae3d0cd5ea4f89a79279b161b0f8ef = new DataFlowConnector<IVisualPortGraphNode>() { InstanceName = "Default" };
            DataFlowConnector<IVisualPortGraphNode> id_d64d68f5fbe74192b82d38c9318dc4b6 = new DataFlowConnector<IVisualPortGraphNode>() { InstanceName = "Default" };
            DataFlowConnector<JToken> currentAbstractionTemplate = new DataFlowConnector<JToken>() { InstanceName = "currentAbstractionTemplate" };
            DataFlowConnector<List<Port>> id_7cfec3ed54d04f169a715c147700ed47 = new DataFlowConnector<List<Port>>() { InstanceName = "Default", Data = new List<Port>() { new Port() { Type = "Port",Name = "p0",IsInputPort = true },new Port() { Type = "Port",Name = "p1",IsInputPort = false},new Port() { Type = "IEvent",Name = "p2",IsInputPort = false}} };
            DataFlowConnector<List<Port>> id_c16ca284660f45679a81fd52607fec03 = new DataFlowConnector<List<Port>>() { InstanceName = "Default", Data = new List<Port>() { new Port() { Type = "Port",Name = "p0",IsInputPort = true },new Port() { Type = "Port",Name = "p1",IsInputPort = false},new Port() { Type = "IEvent",Name = "p2",IsInputPort = false}} };
            DataFlowConnector<List<string>> abstractionTemplateTypes = new DataFlowConnector<List<string>>() { InstanceName = "abstractionTemplateTypes" };
            DataFlowConnector<List<string>> id_2eb695817eb44bbf910abb5acae18e75 = new DataFlowConnector<List<string>>() { InstanceName = "Default" };
            DataFlowConnector<List<string>> id_651b4ab985874aa6abf6b8354322b0f1 = new DataFlowConnector<List<string>>() { InstanceName = "Default" };
            DataFlowConnector<List<string>> id_80fbb5a0a57a477f8cff681489fb2a1d = new DataFlowConnector<List<string>>() { InstanceName = "Default" };
            DataFlowConnector<List<string>> id_9438bab06adc4284b29a52063c06269b = new DataFlowConnector<List<string>>() { InstanceName = "Default" };
            DataFlowConnector<List<string>> id_98e6fe348eda4c089f1b1ecda4470d38 = new DataFlowConnector<List<string>>() { InstanceName = "Default" };
            DataFlowConnector<List<string>> id_b15fe1bfb8ea4c56a5332ac4cd16d89c = new DataFlowConnector<List<string>>() { InstanceName = "Default" };
            DataFlowConnector<List<string>> id_fc9f7e6aa6f74a25ba5b80e7adfe9ab2 = new DataFlowConnector<List<string>>() { InstanceName = "Default" };
            DataFlowConnector<Point> id_e0cf4df5b331452cbf15f0806c93cae3 = new DataFlowConnector<Point>() { InstanceName = "Default" };
            DataFlowConnector<Port> id_1bd1df9769a64645a11efd76789e261e = new DataFlowConnector<Port>() { InstanceName = "Default" };
            DataFlowConnector<Port> id_4b90f593b4f54d7895e5c027e1fe045a = new DataFlowConnector<Port>() { InstanceName = "Default" };
            DataFlowConnector<Port> id_6573107860ca42e1acd2edb39e4af23f = new DataFlowConnector<Port>() { InstanceName = "Default" };
            DataFlowConnector<Port> id_a961ea5c67684cd9be8db19e5508d6bc = new DataFlowConnector<Port>() { InstanceName = "Default" };
            DataFlowConnector<string> id_00be3421cb494a9d98415a4f0c5cf240 = new DataFlowConnector<string>() { InstanceName = "Default", Data = "NewNode" };
            DataFlowConnector<string> id_1fbd51c85a7a4f5d9861b9059f95d9bc = new DataFlowConnector<string>() { InstanceName = "Default" };
            DataFlowConnector<string> id_3c796a63d81949719a85f4c8dd1cfe8c = new DataFlowConnector<string>() { InstanceName = "Default" };
            DataFlowConnector<string> id_411e043c0dc6455cbd5c8b5a5aaa4408 = new DataFlowConnector<string>() { InstanceName = "Default" };
            DataFlowConnector<string> id_4124e1350f3d437cb9379eaf22b6a892 = new DataFlowConnector<string>() { InstanceName = "Default" };
            DataFlowConnector<string> id_4cc2810d4424461d885779ea461524e8 = new DataFlowConnector<string>() { InstanceName = "Default" };
            DataFlowConnector<string> id_75b23a2bca964cc3855a25fdfdda253d = new DataFlowConnector<string>() { InstanceName = "Default" };
            DataFlowConnector<string> id_76e6991c29e74f6cb3c9dd8eeea60553 = new DataFlowConnector<string>() { InstanceName = "Default" };
            DataFlowConnector<string> id_7c025bf582ea4cd59e16fbe05696259d = new DataFlowConnector<string>() { InstanceName = "Default" };
            DataFlowConnector<string> id_821a0f8e8c8149608e068ef40bd35d5c = new DataFlowConnector<string>() { InstanceName = "Default" };
            DataFlowConnector<string> id_911eb68dba704c12bd5afcf7eba7a357 = new DataFlowConnector<string>() { InstanceName = "Default" };
            DataFlowConnector<string> id_ad5bca6c08de488e8825baa888f69bbb = new DataFlowConnector<string>() { InstanceName = "Default" };
            DataFlowConnector<string> id_b687a2136e6d4e88b5dba06d359c3147 = new DataFlowConnector<string>() { InstanceName = "Default" };
            DataFlowConnector<string> id_edb0165c192041dc860e4b959d9ce45a = new DataFlowConnector<string>() { InstanceName = "Default" };
            DataFlowConnector<string> id_f92ce910e32c48928f518addb5063e8d = new DataFlowConnector<string>() { InstanceName = "Default" };
            DataFlowConnector<string> settingsFilePath = new DataFlowConnector<string>() { InstanceName = "settingsFilePath", Data = SETTINGS_FILEPATH };
            DataFlowConnector<string> unparsedConstructorArgsString = new DataFlowConnector<string>() { InstanceName = "unparsedConstructorArgsString" };
            DataFlowConnector<string> unparsedPropertiesString = new DataFlowConnector<string>() { InstanceName = "unparsedPropertiesString" };
            DataFlowConnector<Tuple<string,Dictionary<string,string>>> newlyCreatedAbstractionTemplate = new DataFlowConnector<Tuple<string,Dictionary<string,string>>>() { InstanceName = "newlyCreatedAbstractionTemplate" };
            DragRectMultiSelectNodes id_8ef091fb77c74638ad9f8c015cba207c = new DragRectMultiSelectNodes() { InstanceName = "Default", Graph = mainGraph, StateTransition = stateTransition, DragRectStyle = dragRectStyle };
            EditSetting id_126e7667e7d74d7f97e953ef306d1d63 = new EditSetting() { InstanceName = "Default", JSONPath = "$..LatestDiagramFilePath" };
            EditSetting id_d6354c421bca48b2895e2426104ac94d = new EditSetting() { InstanceName = "Default", JSONPath = "$..ProjectFolderPath" };
            EditSetting id_fb91efad04524a7e8807ac7796be254b = new EditSetting() { InstanceName = "Default", JSONPath = "$..LatestCodeFilePath" };
            EventConnector addNewNodeConnector = new EventConnector() { InstanceName = "addNewNodeConnector" };
            EventConnector addSubtreeToSelectedNode = new EventConnector() { InstanceName = "addSubtreeToSelectedNode" };
            EventConnector id_0b7120e1a1e8497cbccd23143c37962c = new EventConnector() { InstanceName = "Default" };
            EventConnector id_193ac08eecfb4a4a912c611682eacf5b = new EventConnector() { InstanceName = "Default" };
            EventConnector id_1d31f5078cad49d5b7702910eeaa20a4 = new EventConnector() { InstanceName = "Default" };
            EventConnector id_217c6c13105f4f9fbd9c5fa37762fba1 = new EventConnector() { InstanceName = "Default" };
            EventConnector id_725d235d635a4146a1742630192b5698 = new EventConnector() { InstanceName = "Default" };
            EventConnector id_9249e199685e42e99523221b2694bc43 = new EventConnector() { InstanceName = "Default" };
            EventConnector id_af3e2fd88447433ab5460ed5879032bd = new EventConnector() { InstanceName = "Default" };
            EventConnector id_ba5fd8dd21b747cbb764d0c37b1ccd40 = new EventConnector() { InstanceName = "Default" };
            EventConnector id_c2b411781c464b86880201607a2e1ee5 = new EventConnector() { InstanceName = "Default" };
            EventConnector id_c5e2618f94d34279a81f129aaf56e02d = new EventConnector() { InstanceName = "Default" };
            EventConnector id_d3aaec237ab54dab8a28f57baba6aadf = new EventConnector() { InstanceName = "Default" };
            EventConnector id_e85579ffff934a2a8374894973464fc9 = new EventConnector() { InstanceName = "Default" };
            EventConnector id_eb50e52083104a169d8c9fc12c4d0b9e = new EventConnector() { InstanceName = "Default" };
            EventConnector initialiseApp = new EventConnector() { InstanceName = "initialiseApp" };
            EventLambda id_13065e5e0bbd4f1aaee9ebd376f9c726 = new EventLambda() { InstanceName = "Default", Lambda = mainGraph.Clear };
            EventLambda id_a4f7336dc7fb48ba908eb0f07315e72d = new EventLambda() { InstanceName = "Default", Lambda = undoHistory.Redo };
            EventLambda id_b1ee7ebeff8944faab0eab9002064540 = new EventLambda() { InstanceName = "Default", Lambda = undoHistory.Undo };
            EventLambda id_ce3ff0883b504c88be21e246df7d4a54 = new EventLambda() { InstanceName = "Default", Lambda = mainGraph.DeleteSelectedNodes };
            EventLambda id_f964e6db04b84104bf69e4384732530b = new EventLambda() { InstanceName = "Default", Lambda = mainGraph.DeleteSelectedConnection };
            ExtractALACode id_9547a5e7fd764ad0a1b3fc636f24e2d9 = new ExtractALACode() { InstanceName = "Default" };
            FileBrowser id_0ca4e9a278fd45ddbb1083cea911253e = new FileBrowser() { InstanceName = "Default", Mode = "Open", Filter = "C# Files (*.cs)|*.cs;*.CS" };
            FileBrowser id_a7ec7a9c75474eee8ccee5b9e0c0e94e = new FileBrowser() { InstanceName = "Default", Mode = "Open", Filter = "ALA Files (*.ala)|*.ala;*.ALA" };
            FileBrowser id_b149f20f56d3445bba240173d3cc6b3e = new FileBrowser() { InstanceName = "Default", Mode = "Open", Filter = "C# Files (*.cs)|*.cs;*.CS" };
            FileBrowser id_d2cd88ea57e04aae8cc57bdd1466b4d6 = new FileBrowser() { InstanceName = "Default", Mode = "Save", Filter = "ALA Files (*.ala)|*.ala;*.ALA", DefaultPath = Utilities.GetApplicationDirectory() };
            FileReader id_22684335cf894258a07346efb51fed98 = new FileReader() { InstanceName = "Default" };
            FileReader id_76741d199f48475cbaec774c4ffcd806 = new FileReader() { InstanceName = "Default" };
            FileReader id_cdcf16842f164e89b234c1591cebf5d3 = new FileReader() { InstanceName = "Default" };
            FileReader id_f034fbe6dce3465cbe6edada65c8d263 = new FileReader() { InstanceName = "Default" };
            FileWriter id_c5d2d42c1be6413e92c567f58ebbe0a8 = new FileWriter() { InstanceName = "Default" };
            FolderBrowser id_2c5c5676ebf643a88ea4568bbee5c460 = new FolderBrowser() { InstanceName = "Default" };
            ForEach<IPortConnection> id_d18001b76b824b4eaadcd206701a2be4 = new ForEach<IPortConnection>() { InstanceName = "Default" };
            ForEach<string> id_e0fbfce24b1d4c78b1b521515e82f020 = new ForEach<string>() { InstanceName = "Default" };
            ForEach<string> id_e6fc28cbe37d4bdd86378e9e54b500ba = new ForEach<string>() { InstanceName = "Default" };
            ForEach<Tuple<VisualPortGraphNode,VisualPortGraphNode,Port,Port>> id_fe4cfcee326e43bbb5231a5de0115973 = new ForEach<Tuple<VisualPortGraphNode,VisualPortGraphNode,Port,Port>>() { InstanceName = "Default" };
            GenerateCode id_5e0e0964797245f7a9ebaec14901ccac = new GenerateCode() { InstanceName = "Default", Graph = mainGraph };
            GetSetting id_2ac88ef7fe0b47bbba634a8a45702a81 = new GetSetting("LatestDiagramFilePath" ) { InstanceName = "Default" };
            GetSetting id_d048bf2542914af29725a1a0074bd773 = new GetSetting("LatestDiagramFilePath" ) { InstanceName = "Default" };
            GetSetting id_e70bef52836e40a2a17c333a392b0548 = new GetSetting("LatestCodeFilePath" ) { InstanceName = "Default" };
            Horizontal currentDiagramModeHoriz = new Horizontal() { InstanceName = "currentDiagramModeHoriz", Margin = new Thickness(10) };
            Horizontal id_5f4945f32824499da007be8fd7deb435 = new Horizontal() { InstanceName = "Default", Ratios = new[] {15,75,10} };
            Horizontal id_be7144a9493b458693681f9773618056 = new Horizontal() { InstanceName = "Default", Ratios = new[] {15,75,10} };
            Horizontal mousePositionHoriz = new Horizontal() { InstanceName = "mousePositionHoriz", Margin = new Thickness(10) };
            IfElse id_1d2a4e1acf8b444aad9365ee81c1ac88 = new IfElse() { InstanceName = "Default" };
            InsertFileCodeLines id_7ca8d59d03ab4f06aad5827b56b26610 = new InsertFileCodeLines() { InstanceName = "Default", StartLandmark = "// BEGIN AUTO-GENERATED INSTANTIATIONS", EndLandmark = "// END AUTO-GENERATED INSTANTIATIONS", Indent = "            " };
            InsertFileCodeLines id_9722e72251f54855b744639c5b96fda1 = new InsertFileCodeLines() { InstanceName = "Default", StartLandmark = "// BEGIN AUTO-GENERATED WIRING", EndLandmark = "// END AUTO-GENERATED WIRING", Indent = "            " };
            InverseStringFormat id_f4f07ea01bf5480397359bf8f7e24704 = new InverseStringFormat() { InstanceName = "Default", Format = "{TypeOrVar} {Name} = new {Type}({ConstructorArgs}) {{Properties}};" };
            JSONParser id_18191d3fb86d460ab709451888c40955 = new JSONParser() { InstanceName = "Default", JSONPath = "$..NodeIds" };
            JSONParser id_2fa52c0f925d4df9b693810373f63348 = new JSONParser() { InstanceName = "Default", JSONPath = "$..Nodes" };
            JSONParser id_80d1de4e1ca94967b085e895f3d25443 = new JSONParser() { InstanceName = "Default", JSONPath = "$..Connections" };
            JSONParser id_8bed896c1b3c4a79b772d859c847c22b = new JSONParser() { InstanceName = "Default", JSONPath = "$..SubtreeRoot.Id" };
            JSONParser id_e366cba16d6443b6b36846f86aeff130 = new JSONParser() { InstanceName = "Default", JSONPath = "$..NodeIds" };
            JSONWriter<Dictionary<string,JToken>> saveAbstractionTemplatesToFile = new JSONWriter<Dictionary<string,JToken>>(System.IO.Path.Combine(APP_DIRECTORY,"abstractionTemplates.json") ) { InstanceName = "saveAbstractionTemplatesToFile" };
            KeyEvent addChildNodeEvent = new KeyEvent("KeyDown" ) { InstanceName = "addChildNodeEvent", Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected)  && args.Key == Key.A };
            KeyEvent id_15f9d7690a2942668ecc583d0f914b20 = new KeyEvent("KeyDown" ) { InstanceName = "Default", Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.IdleSelected)  &&  args.Key == Key.Delete };
            KeyEvent id_1a0f4097208c4ab284b189bf20d13144 = new KeyEvent("KeyDown" ) { InstanceName = "Default", Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected)  && Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftShift) && args.Key == Key.S };
            KeyEvent id_2a57f9b0cbaf44228172791960b2f4f6 = new KeyEvent("KeyDown" ) { InstanceName = "Default", Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected)  && Keyboard.IsKeyDown(Key.LeftCtrl) && args.Key == Key.Z };
            KeyEvent id_50f3007aa7044fc8a9ba73e553e01adc = new KeyEvent("KeyDown" ) { InstanceName = "Default", Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.IdleSelected)  && Keyboard.IsKeyDown(Key.LeftCtrl) && args.Key == Key.C };
            KeyEvent id_6301a2d8482d418190d1b0f9eacf940e = new KeyEvent("KeyDown" ) { InstanceName = "Default", Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected)  && args.Key == Key.R };
            KeyEvent id_651e418cd9c84bf5aecfa67eb3b2d83c = new KeyEvent("KeyDown" ) { InstanceName = "Default", Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected)  && Keyboard.IsKeyDown(Key.LeftCtrl) && args.Key == Key.P };
            KeyEvent id_6d5a4532a2ab493687cf1364d4cca9c4 = new KeyEvent("KeyDown" ) { InstanceName = "Default", Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected)  && Keyboard.IsKeyDown(Key.LeftCtrl) && args.Key == Key.Y };
            KeyEvent id_8185a5f1ad134ede825c7013a1b8c332 = new KeyEvent("KeyDown" ) { InstanceName = "Default", Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) && Keyboard.IsKeyDown(Key.Space) };
            KeyEvent id_b17e9b350e4147329b84bc4454d7a891 = new KeyEvent("KeyDown" ) { InstanceName = "Default", Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected)  && Keyboard.IsKeyDown(Key.LeftCtrl) && args.Key == Key.Q };
            KeyEvent id_c33489f097484f1f931e138bf8196f7d = new KeyEvent("KeyDown" ) { InstanceName = "Default", Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected)  && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && args.Key == Key.O };
            KeyEvent id_c8dcb3fa42e947ccb3bb5b4a51e497e7 = new KeyEvent("KeyDown" ) { InstanceName = "Default", Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected)  && Keyboard.IsKeyDown(Key.LeftCtrl) && args.Key == Key.G };
            KeyEvent id_ed2e008e8c7e45efb12a33509d8b4cc4 = new KeyEvent("KeyDown" ) { InstanceName = "Default", Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.IdleSelected)  && Keyboard.IsKeyDown(Key.LeftCtrl) && args.Key == Key.V };
            KeyEvent id_f375aad6e9224e4ba28ef0bb07ec6bf2 = new KeyEvent("KeyDown" ) { InstanceName = "Default", Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected)  && Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.LeftShift) && args.Key == Key.S };
            KeyEvent id_fe700e3f70184783940459fc19ee659c = new KeyEvent("KeyDown" ) { InstanceName = "Default", Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) && Keyboard.IsKeyDown(Key.E) };
            LookupTable<string,Dictionary<string,string>> id_e04a847325bc4703b8de92a10b2e75ed = new LookupTable<string,Dictionary<string,string>>() { InstanceName = "Default" };
            LookupTable<string,JToken> abstractionTemplates = new LookupTable<string,JToken>() { InstanceName = "abstractionTemplates" };
            LookupTable<string,string> id_129243f50531479fb817c9ca866f24eb = new LookupTable<string,string>() { InstanceName = "Default" };
            LookupTable<string,string> id_736e067094ca4479b4d51484d4979aa4 = new LookupTable<string,string>() { InstanceName = "Default" };
            LookupTable<string,string> id_d6c3ad8f1ec642959f21ad5e67528a26 = new LookupTable<string,string>() { InstanceName = "Default" };
            LookupTable<string,string> id_fe380a323be94bc0a4611cae9211790a = new LookupTable<string,string>() { InstanceName = "Default" };
            MenuBar id_326cdabb659f4c37b66daa7ad3ed6155 = new MenuBar() { InstanceName = "Default", Background = PRIMARY_UX_BG_COLOUR, Foreground = PRIMARY_UX_FG_COLOUR };
            MenuItem id_2a5fb749c9dc4f6880f55c124261246e = new MenuItem("Open Project Folder" ) { InstanceName = "Default" };
            MenuItem id_53bd59ce7e5243a297da6f78fc494e2f = new MenuItem("Help" ) { InstanceName = "Default" };
            MenuItem id_564ed95481054589ab6059b110b8af7a = new MenuItem("Open Debug Info" ) { InstanceName = "Default" };
            MenuItem id_5a075d3abdd94340bf112c17d9d3738f = new MenuItem("Preferences" ) { InstanceName = "Default" };
            MenuItem id_5f6a7fde612d41749444eaab610a2a50 = new MenuItem("Edit" ) { InstanceName = "Default" };
            MenuItem id_8ae7422b68a24f648234168550b7d7cf = new MenuItem("Save" ) { InstanceName = "Default" };
            MenuItem id_8f20f68bb4ea4c0b8e33de077f32c35c = new MenuItem("Open Diagram" ) { InstanceName = "Default" };
            MenuItem id_a3f53bfa6ea04ebdaa5816f9bf1273b2 = new MenuItem("Open Code File to Edit" ) { InstanceName = "Default" };
            MenuItem id_a8bdd2ae821242a4a89607db601a8ecd = new MenuItem("File" ) { InstanceName = "Default" };
            MenuItem id_ac9c725c30d94bc4aca3a74a8242f9ea = new MenuItem("New User Guide" ) { InstanceName = "Default" };
            MenuItem id_d5b1d9d4f564496eac0ce8aa0242f37f = new MenuItem("Create Diagram From Code" ) { InstanceName = "Default" };
            MenuItem id_d866e07dff2745f48dbcdf90d31f8f6f = new MenuItem("Save As" ) { InstanceName = "Default" };
            MenuItem id_e9be6ad589cb41ada5f3dabdbf4dad20 = new MenuItem("Generate Code" ) { InstanceName = "Default" };
            NewAbstractionTemplateTab id_b844b78ad0e54ca8b3ea6eb6bb831c2e = new NewAbstractionTemplateTab() { InstanceName = "Default" };
            NewVisualPortGraphNode id_144ab8b860b6473db75b7ee5594a2f6e = new NewVisualPortGraphNode() { InstanceName = "Default", Graph = mainGraph, StateTransition = stateTransition, UndoHistory = undoHistory, NodeStyle = nodeStyle, PortStyle = portStyle };
            NewVisualPortGraphNode id_f9559f3d5d6b4bd09ab31db34d2a9444 = new NewVisualPortGraphNode() { InstanceName = "Default", Graph = mainGraph, StateTransition = stateTransition, UndoHistory = undoHistory, NodeStyle = nodeStyle, PortStyle = portStyle };
            OutputTab id_66690e3d478746f485717c3de09d7e75 = new OutputTab() { InstanceName = "Default" };
            PopupWindow id_4783415fded6420a83e0b214a3abf2e0 = new PopupWindow("New User Guide" ) { InstanceName = "Default", Width = 720, Resize = SizeToContent.WidthAndHeight };
            PopupWindow id_cef138236635424782b3dd215a86008f = new PopupWindow("Debug Info" ) { InstanceName = "Default", Height = 600, Width = 600 };
            PopupWindow id_d4cff65ce0a24619b2f59178001f2041 = new PopupWindow("Preferences" ) { InstanceName = "Default", Width = 720, Resize = SizeToContent.Height };
            RightTreeLayout<IVisualPortGraphNode> id_5de5980d74a1467c8a9462e4427422bf = new RightTreeLayout<IVisualPortGraphNode>() { InstanceName = "Default", GetID = n => n.Id, GetWidth = n => (n as VisualPortGraphNode).Width, GetHeight = n => (n as VisualPortGraphNode).Height, SetX = (n,x) => n.PositionX = x, SetY = (n,y) => n.PositionY = y, GetChildren = n => n.Graph.GetChildren(n.Id), HorizontalGap = 100, VerticalGap = 20, InitialX = 50, InitialY = 50 };
            SaveGraphToFile id_367efd63e1234a75b0c82f727bce73f9 = new SaveGraphToFile() { InstanceName = "Default", Graph = mainGraph };
            SaveGraphToFile id_ad5da8fd9e5e45bf81184e4b54864a0c = new SaveGraphToFile() { InstanceName = "Default", Graph = mainGraph };
            StateChangeListener checkToResetDiagramFocus = new StateChangeListener() { InstanceName = "checkToResetDiagramFocus", StateTransition = stateTransition, CurrentStateShouldMatch = Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected };
            StateChangeListener id_2534923aff934248a5493e6d293ad205 = new StateChangeListener() { InstanceName = "Default", StateTransition = stateTransition };
            StateTransitionEvent<Enums.DiagramMode> id_da10fa6481e2458fa526983b0036cb4d = new StateTransitionEvent<Enums.DiagramMode>(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected, Enums.DiagramMode.TextEditing ) { InstanceName = "Default" };
            StringMap id_591aaec0820e498c85ae6898d47a7935 = new StringMap() { InstanceName = "Default" };
            StringSequenceExtractor id_258247722bc042a3b68b6d628416a490 = new StringSequenceExtractor() { InstanceName = "Default", Separator = ',', ConsiderBracketBalance = true, StartingBrackets = "", TrimEntries = true };
            StringSequenceExtractor id_6df96338c4fd4b2fa8f031b5a1126e73 = new StringSequenceExtractor() { InstanceName = "Default", Separator = ',', ConsiderBracketBalance = true, StartingBrackets = "", TrimEntries = true };
            Tab id_53214f9414b6403aa7b5606c2fdcabc3 = new Tab(title: "Menu" ) { InstanceName = "Default" };
            Tab id_9648955167cb40349ef63dd02441544b = new Tab(title: "Controls" ) { InstanceName = "Default" };
            Tab id_9dd636737f044cc2a09a856a53d924fe = new Tab(title: "Functionality" ) { InstanceName = "Default" };
            TabContainer id_0915024382b54ddc9024fa295cec6c6b = new TabContainer() { InstanceName = "Default" };
            TabContainer id_86e0bcc1baa94327a0cb2cc59a9c84ad = new TabContainer() { InstanceName = "Default" };
            Text currentMousePositionText = new Text("(0" + "," + " " + "0)" ) { InstanceName = "currentMousePositionText", Color = Brushes.Black };
            Text id_0cffa533202d4848a0cef1107ee8bcce = new Text(text: Utilities.ReadFileSafely(userGuidePaths["Menu"],"Error: newUserGuide_Menu.txt not found") ) { InstanceName = "Default", Margin = new Thickness(5) };
            Text id_45319d735b1040419b709825dc1cf23a = new Text(text: Utilities.ReadFileSafely(userGuidePaths["Controls"],"Error: newUserGuide_Controls.txt not found") ) { InstanceName = "Default", Margin = new Thickness(5) };
            Text id_4d5c2d30a848480ea51139695f487d9e = new Text(text: Utilities.ReadFileSafely(userGuidePaths["Functionality"],"Error: newUserGuide_Functionality.txt not found") ) { InstanceName = "Default", Margin = new Thickness(5) };
            Text id_8bf217c805564f4fb5029c180ae5d85e = new Text(text: "Diagram location:" ) { InstanceName = "Default", Margin = new Thickness(5) };
            Text id_938e460d35b74b5c8ae7227f1f8b8cc0 = new Text("Current mouse position: " ) { InstanceName = "Default", Color = Brushes.Black };
            Text id_b90ac39285c04875bdaae2919a522fed = new Text("Current diagram mode: " ) { InstanceName = "Default", Color = Brushes.Black };
            Text id_c835aee22753415894840b4d3dd6582f = new Text(text: "Code file location:" ) { InstanceName = "Default", Margin = new Thickness(5) };
            Text id_fd0e259a2e644bd8a5f548c225a5e2cd = new Text("None" ) { InstanceName = "Default", Color = Brushes.Black };
            TextBox id_8de11133b1074cfe915e847951e130a8 = new TextBox() { InstanceName = "Default", Margin = new Thickness(5) };
            TextBox id_9a2dda5813924951b284fc2b89e6b4bf = new TextBox() { InstanceName = "Default", Margin = new Thickness(5) };
            TextClipboard id_5018f4c773a34ced85806dd175659e9a = new TextClipboard() { InstanceName = "Default" };
            TextClipboard id_d65a994fdb9d450cba8cd914cad29f72 = new TextClipboard() { InstanceName = "Default" };
            Vertical id_218f2d941be344289ee5dd3d54b3dedd = new Vertical() { InstanceName = "Default" };
            Vertical id_731ccda5c9e244fb96a6007ea9455606 = new Vertical() { InstanceName = "Default", VerticalScrollBarVisible = true, HorizontalScrollBarVisible = true, Layouts = new[] {0,2} };
            Vertical id_9d790bbf1430492a8e7b01d0633d5c0b = new Vertical() { InstanceName = "Default", Layouts = new[] {2,0} };
            VPGNContextMenu id_c5563a5bfcc94cc781a29126f4fb2aab = new VPGNContextMenu() { InstanceName = "Default" };
            // END AUTO-GENERATED INSTANTIATIONS FOR Application.xmind

            // BEGIN AUTO-GENERATED WIRING FOR Application.xmind
            mainWindow.WireTo(id_731ccda5c9e244fb96a6007ea9455606, "iuiStructure"); // (@MainWindow (mainWindow).iuiStructure) -- [IUI] --> (Vertical (id_731ccda5c9e244fb96a6007ea9455606).child)
            mainWindow.WireTo(initialiseApp, "appStart"); // (@MainWindow (mainWindow).appStart) -- [IEvent] --> (EventConnector (initialiseApp).NEEDNAME)
            id_731ccda5c9e244fb96a6007ea9455606.WireTo(id_326cdabb659f4c37b66daa7ad3ed6155, "children"); // (Vertical (id_731ccda5c9e244fb96a6007ea9455606).children) -- [List<IUI>] --> (MenuBar (id_326cdabb659f4c37b66daa7ad3ed6155).child)
            id_731ccda5c9e244fb96a6007ea9455606.WireTo(id_9d790bbf1430492a8e7b01d0633d5c0b, "children"); // (Vertical (id_731ccda5c9e244fb96a6007ea9455606).children) -- [List<IUI>] --> (Vertical (id_9d790bbf1430492a8e7b01d0633d5c0b).child)
            id_326cdabb659f4c37b66daa7ad3ed6155.WireTo(id_a8bdd2ae821242a4a89607db601a8ecd, "children"); // (MenuBar (id_326cdabb659f4c37b66daa7ad3ed6155).children) -- [IUI] --> (MenuItem (id_a8bdd2ae821242a4a89607db601a8ecd).child)
            id_326cdabb659f4c37b66daa7ad3ed6155.WireTo(id_5f6a7fde612d41749444eaab610a2a50, "children"); // (MenuBar (id_326cdabb659f4c37b66daa7ad3ed6155).children) -- [IUI] --> (MenuItem (id_5f6a7fde612d41749444eaab610a2a50).child)
            id_326cdabb659f4c37b66daa7ad3ed6155.WireTo(id_53bd59ce7e5243a297da6f78fc494e2f, "children"); // (MenuBar (id_326cdabb659f4c37b66daa7ad3ed6155).children) -- [IUI] --> (MenuItem (id_53bd59ce7e5243a297da6f78fc494e2f).child)
            id_326cdabb659f4c37b66daa7ad3ed6155.WireTo(id_e9be6ad589cb41ada5f3dabdbf4dad20, "children"); // (MenuBar (id_326cdabb659f4c37b66daa7ad3ed6155).children) -- [IUI] --> (MenuItem (id_e9be6ad589cb41ada5f3dabdbf4dad20).child)
            id_326cdabb659f4c37b66daa7ad3ed6155.WireTo(id_564ed95481054589ab6059b110b8af7a, "children"); // (MenuBar (id_326cdabb659f4c37b66daa7ad3ed6155).children) -- [IUI] --> (MenuItem (id_564ed95481054589ab6059b110b8af7a).child)
            id_a8bdd2ae821242a4a89607db601a8ecd.WireTo(id_2a5fb749c9dc4f6880f55c124261246e, "children"); // (MenuItem (id_a8bdd2ae821242a4a89607db601a8ecd).children) -- [IUI] --> (MenuItem (id_2a5fb749c9dc4f6880f55c124261246e).child)
            id_a8bdd2ae821242a4a89607db601a8ecd.WireTo(id_8f20f68bb4ea4c0b8e33de077f32c35c, "children"); // (MenuItem (id_a8bdd2ae821242a4a89607db601a8ecd).children) -- [IUI] --> (MenuItem (id_8f20f68bb4ea4c0b8e33de077f32c35c).child)
            id_a8bdd2ae821242a4a89607db601a8ecd.WireTo(id_a3f53bfa6ea04ebdaa5816f9bf1273b2, "children"); // (MenuItem (id_a8bdd2ae821242a4a89607db601a8ecd).children) -- [IUI] --> (MenuItem (id_a3f53bfa6ea04ebdaa5816f9bf1273b2).child)
            id_a8bdd2ae821242a4a89607db601a8ecd.WireTo(id_8ae7422b68a24f648234168550b7d7cf, "children"); // (MenuItem (id_a8bdd2ae821242a4a89607db601a8ecd).children) -- [IUI] --> (MenuItem (id_8ae7422b68a24f648234168550b7d7cf).child)
            id_a8bdd2ae821242a4a89607db601a8ecd.WireTo(id_d866e07dff2745f48dbcdf90d31f8f6f, "children"); // (MenuItem (id_a8bdd2ae821242a4a89607db601a8ecd).children) -- [IUI] --> (MenuItem (id_d866e07dff2745f48dbcdf90d31f8f6f).child)
            id_a8bdd2ae821242a4a89607db601a8ecd.WireTo(id_d5b1d9d4f564496eac0ce8aa0242f37f, "children"); // (MenuItem (id_a8bdd2ae821242a4a89607db601a8ecd).children) -- [IUI] --> (MenuItem (id_d5b1d9d4f564496eac0ce8aa0242f37f).child)
            id_2a5fb749c9dc4f6880f55c124261246e.WireTo(id_2c5c5676ebf643a88ea4568bbee5c460, "clickedEvent"); // (MenuItem (id_2a5fb749c9dc4f6880f55c124261246e).clickedEvent) -- [IEvent] --> (FolderBrowser (id_2c5c5676ebf643a88ea4568bbee5c460).openBrowser)
            id_2c5c5676ebf643a88ea4568bbee5c460.WireTo(id_b687a2136e6d4e88b5dba06d359c3147, "selectedFolderPathOutput"); // (FolderBrowser (id_2c5c5676ebf643a88ea4568bbee5c460).selectedFolderPathOutput) -- [IDataFlow<string>] --> (DataFlowConnector<string> (id_b687a2136e6d4e88b5dba06d359c3147).dataInput)
            id_b687a2136e6d4e88b5dba06d359c3147.WireTo(id_ad62adfa4ea847359d1bd31e9cafde75, "fanoutList"); // (DataFlowConnector<string> (id_b687a2136e6d4e88b5dba06d359c3147).fanoutList) -- [IDataFlow<string>] --> (Cast<string,object> (id_ad62adfa4ea847359d1bd31e9cafde75).input)
            id_ad62adfa4ea847359d1bd31e9cafde75.WireTo(id_d6354c421bca48b2895e2426104ac94d, "output"); // (Cast<string,object> (id_ad62adfa4ea847359d1bd31e9cafde75).output) -- [IDataFlow<object>] --> (EditSetting (id_d6354c421bca48b2895e2426104ac94d).valueInput)
            id_d6354c421bca48b2895e2426104ac94d.WireTo(settingsFilePath, "filePathInput"); // (EditSetting (id_d6354c421bca48b2895e2426104ac94d).filePathInput) -- [IDataFlowB<string>] --> (@DataFlowConnector<string> (settingsFilePath).returnDataB)
            id_8f20f68bb4ea4c0b8e33de077f32c35c.WireTo(id_e85579ffff934a2a8374894973464fc9, "clickedEvent"); // (MenuItem (id_8f20f68bb4ea4c0b8e33de077f32c35c).clickedEvent) -- [IEvent] --> (EventConnector (id_e85579ffff934a2a8374894973464fc9).eventInput)
            id_e85579ffff934a2a8374894973464fc9.WireTo(id_a7ec7a9c75474eee8ccee5b9e0c0e94e, "fanoutList"); // (EventConnector (id_e85579ffff934a2a8374894973464fc9).fanoutList) -- [IEvent] --> (FileBrowser (id_a7ec7a9c75474eee8ccee5b9e0c0e94e).openBrowser)
            id_a7ec7a9c75474eee8ccee5b9e0c0e94e.WireTo(id_911eb68dba704c12bd5afcf7eba7a357, "selectedFilePathOutput"); // (FileBrowser (id_a7ec7a9c75474eee8ccee5b9e0c0e94e).selectedFilePathOutput) -- [IDataFlow<string>] --> (DataFlowConnector<string> (id_911eb68dba704c12bd5afcf7eba7a357).dataInput)
            id_911eb68dba704c12bd5afcf7eba7a357.WireTo(id_a0cf9e8153f24235b8672e8f2c5aef4d, "fanoutList"); // (DataFlowConnector<string> (id_911eb68dba704c12bd5afcf7eba7a357).fanoutList) -- [IDataFlow<string>] --> (ConvertToEvent<string> (id_a0cf9e8153f24235b8672e8f2c5aef4d).start)
            id_911eb68dba704c12bd5afcf7eba7a357.WireTo(id_76741d199f48475cbaec774c4ffcd806, "fanoutList"); // (DataFlowConnector<string> (id_911eb68dba704c12bd5afcf7eba7a357).fanoutList) -- [IDataFlow<string>] --> (FileReader (id_76741d199f48475cbaec774c4ffcd806).filePathInput)
            id_911eb68dba704c12bd5afcf7eba7a357.WireTo(id_03c7d3d4376a45b0b3d0369fd6196e73, "fanoutList"); // (DataFlowConnector<string> (id_911eb68dba704c12bd5afcf7eba7a357).fanoutList) -- [IDataFlow<string>] --> (Cast<string,object> (id_03c7d3d4376a45b0b3d0369fd6196e73).input)
            id_911eb68dba704c12bd5afcf7eba7a357.WireTo(id_9a2dda5813924951b284fc2b89e6b4bf, "fanoutList"); // (DataFlowConnector<string> (id_911eb68dba704c12bd5afcf7eba7a357).fanoutList) -- [IDataFlow<string>] --> (TextBox (id_9a2dda5813924951b284fc2b89e6b4bf).NEEDNAME)
            id_a0cf9e8153f24235b8672e8f2c5aef4d.WireTo(id_13065e5e0bbd4f1aaee9ebd376f9c726, "eventOutput"); // (ConvertToEvent<string> (id_a0cf9e8153f24235b8672e8f2c5aef4d).eventOutput) -- [IEvent] --> (EventLambda (id_13065e5e0bbd4f1aaee9ebd376f9c726).start)
            id_76741d199f48475cbaec774c4ffcd806.WireTo(id_4124e1350f3d437cb9379eaf22b6a892, "fileContentOutput"); // (FileReader (id_76741d199f48475cbaec774c4ffcd806).fileContentOutput) -- [IDataFlow<string>] --> (DataFlowConnector<string> (id_4124e1350f3d437cb9379eaf22b6a892).dataInput)
            id_03c7d3d4376a45b0b3d0369fd6196e73.WireTo(id_126e7667e7d74d7f97e953ef306d1d63, "output"); // (Cast<string,object> (id_03c7d3d4376a45b0b3d0369fd6196e73).output) -- [IDataFlow<object>] --> (EditSetting (id_126e7667e7d74d7f97e953ef306d1d63).valueInput)
            id_126e7667e7d74d7f97e953ef306d1d63.WireTo(settingsFilePath, "filePathInput"); // (EditSetting (id_126e7667e7d74d7f97e953ef306d1d63).filePathInput) -- [IDataFlowB<string>] --> (@DataFlowConnector<string> (settingsFilePath).returnDataB)
            id_a3f53bfa6ea04ebdaa5816f9bf1273b2.WireTo(id_217c6c13105f4f9fbd9c5fa37762fba1, "clickedEvent"); // (MenuItem (id_a3f53bfa6ea04ebdaa5816f9bf1273b2).clickedEvent) -- [IEvent] --> (EventConnector (id_217c6c13105f4f9fbd9c5fa37762fba1).eventInput)
            id_217c6c13105f4f9fbd9c5fa37762fba1.WireTo(id_b149f20f56d3445bba240173d3cc6b3e, "fanoutList"); // (EventConnector (id_217c6c13105f4f9fbd9c5fa37762fba1).fanoutList) -- [IEvent] --> (FileBrowser (id_b149f20f56d3445bba240173d3cc6b3e).openBrowser)
            id_b149f20f56d3445bba240173d3cc6b3e.WireTo(id_1fbd51c85a7a4f5d9861b9059f95d9bc, "selectedFilePathOutput"); // (FileBrowser (id_b149f20f56d3445bba240173d3cc6b3e).selectedFilePathOutput) -- [IDataFlow<string>] --> (DataFlowConnector<string> (id_1fbd51c85a7a4f5d9861b9059f95d9bc).dataInput)
            id_20b52d9c5ffd4a40b0ec89ad945997c5.WireTo(id_1fbd51c85a7a4f5d9861b9059f95d9bc, "inputDataB"); // (Data<string> (id_20b52d9c5ffd4a40b0ec89ad945997c5).inputDataB) -- [IDataFlowB<string>] --> (DataFlowConnector<string> (id_1fbd51c85a7a4f5d9861b9059f95d9bc).NEEDNAME)
            id_1fbd51c85a7a4f5d9861b9059f95d9bc.WireTo(id_22684335cf894258a07346efb51fed98, "fanoutList"); // (DataFlowConnector<string> (id_1fbd51c85a7a4f5d9861b9059f95d9bc).fanoutList) -- [IDataFlow<string>] --> (FileReader (id_22684335cf894258a07346efb51fed98).filePathInput)
            id_1fbd51c85a7a4f5d9861b9059f95d9bc.WireTo(id_422e47c9692441a1a594dfe3aed6bd12, "fanoutList"); // (DataFlowConnector<string> (id_1fbd51c85a7a4f5d9861b9059f95d9bc).fanoutList) -- [IDataFlow<string>] --> (Cast<string,object> (id_422e47c9692441a1a594dfe3aed6bd12).input)
            id_1fbd51c85a7a4f5d9861b9059f95d9bc.WireTo(id_8de11133b1074cfe915e847951e130a8, "fanoutList"); // (DataFlowConnector<string> (id_1fbd51c85a7a4f5d9861b9059f95d9bc).fanoutList) -- [IDataFlow<string>] --> (TextBox (id_8de11133b1074cfe915e847951e130a8).textInput)
            id_20b52d9c5ffd4a40b0ec89ad945997c5.WireTo(id_22684335cf894258a07346efb51fed98, "dataOutput"); // (Data<string> (id_20b52d9c5ffd4a40b0ec89ad945997c5).dataOutput) -- [IDataFlow<string>] --> (FileReader (id_22684335cf894258a07346efb51fed98).filePathInput)
            id_22684335cf894258a07346efb51fed98.WireTo(id_7ca8d59d03ab4f06aad5827b56b26610, "fileContentOutput"); // (FileReader (id_22684335cf894258a07346efb51fed98).fileContentOutput) -- [IDataFlow<string>] --> (InsertFileCodeLines (id_7ca8d59d03ab4f06aad5827b56b26610).fileContentsInput)
            id_422e47c9692441a1a594dfe3aed6bd12.WireTo(id_fb91efad04524a7e8807ac7796be254b, "output"); // (Cast<string,object> (id_422e47c9692441a1a594dfe3aed6bd12).output) -- [IDataFlow<object>] --> (EditSetting (id_fb91efad04524a7e8807ac7796be254b).valueInput)
            id_fb91efad04524a7e8807ac7796be254b.WireTo(settingsFilePath, "filePathInput"); // (EditSetting (id_fb91efad04524a7e8807ac7796be254b).filePathInput) -- [IDataFlowB<string>] --> (@DataFlowConnector<string> (settingsFilePath).returnDataB)
            id_8ae7422b68a24f648234168550b7d7cf.WireTo(id_d048bf2542914af29725a1a0074bd773, "clickedEvent"); // (MenuItem (id_8ae7422b68a24f648234168550b7d7cf).clickedEvent) -- [IEvent] --> (GetSetting (id_d048bf2542914af29725a1a0074bd773).start)
            id_d048bf2542914af29725a1a0074bd773.WireTo(settingsFilePath, "filePathInput"); // (GetSetting (id_d048bf2542914af29725a1a0074bd773).filePathInput) -- [IDataFlowB<string>] --> (@DataFlowConnector<string> (settingsFilePath).returnDataB)
            id_d048bf2542914af29725a1a0074bd773.WireTo(id_7c025bf582ea4cd59e16fbe05696259d, "settingJsonOutput"); // (GetSetting (id_d048bf2542914af29725a1a0074bd773).settingJsonOutput) -- [IDataFlow<string>] --> (DataFlowConnector<string> (id_7c025bf582ea4cd59e16fbe05696259d).dataInput)
            id_7c025bf582ea4cd59e16fbe05696259d.WireTo(id_bd9c43f53b5043c8b76e6f6912138554, "fanoutList"); // (DataFlowConnector<string> (id_7c025bf582ea4cd59e16fbe05696259d).fanoutList) -- [IDataFlow<string>] --> (Apply<string,bool> (id_bd9c43f53b5043c8b76e6f6912138554).input)
            id_bd9c43f53b5043c8b76e6f6912138554.WireTo(id_1d2a4e1acf8b444aad9365ee81c1ac88, "output"); // (Apply<string,bool> (id_bd9c43f53b5043c8b76e6f6912138554).output) -- [IDataFlow<bool>] --> (IfElse (id_1d2a4e1acf8b444aad9365ee81c1ac88).condition)
            id_1d2a4e1acf8b444aad9365ee81c1ac88.WireTo(id_b0c2ff43748f4ee4aabc3e9bed58b99d, "ifOutput"); // (IfElse (id_1d2a4e1acf8b444aad9365ee81c1ac88).ifOutput) -- [IEvent] --> (Data<string> (id_b0c2ff43748f4ee4aabc3e9bed58b99d).start)
            id_1d2a4e1acf8b444aad9365ee81c1ac88.WireTo(id_d2cd88ea57e04aae8cc57bdd1466b4d6, "elseOutput"); // (IfElse (id_1d2a4e1acf8b444aad9365ee81c1ac88).elseOutput) -- [IEvent] --> (FileBrowser (id_d2cd88ea57e04aae8cc57bdd1466b4d6).openBrowser)
            id_b0c2ff43748f4ee4aabc3e9bed58b99d.WireTo(id_7c025bf582ea4cd59e16fbe05696259d, "inputDataB"); // (Data<string> (id_b0c2ff43748f4ee4aabc3e9bed58b99d).inputDataB) -- [IDataFlowB<string>] --> (DataFlowConnector<string> (id_7c025bf582ea4cd59e16fbe05696259d).dataOutputB)
            id_b0c2ff43748f4ee4aabc3e9bed58b99d.WireTo(id_ad5da8fd9e5e45bf81184e4b54864a0c, "dataOutput"); // (Data<string> (id_b0c2ff43748f4ee4aabc3e9bed58b99d).dataOutput) -- [IDataFlow<string>] --> (SaveGraphToFile (id_ad5da8fd9e5e45bf81184e4b54864a0c).filePathInput)
            id_ad5da8fd9e5e45bf81184e4b54864a0c.WireTo(id_9249e199685e42e99523221b2694bc43, "complete"); // (SaveGraphToFile (id_ad5da8fd9e5e45bf81184e4b54864a0c).complete) -- [IEvent] --> (EventConnector (id_9249e199685e42e99523221b2694bc43).eventInput)
            id_d866e07dff2745f48dbcdf90d31f8f6f.WireTo(id_d2cd88ea57e04aae8cc57bdd1466b4d6, "clickedEvent"); // (MenuItem (id_d866e07dff2745f48dbcdf90d31f8f6f).clickedEvent) -- [IEvent] --> (FileBrowser (id_d2cd88ea57e04aae8cc57bdd1466b4d6).openBrowser)
            id_d2cd88ea57e04aae8cc57bdd1466b4d6.WireTo(id_edb0165c192041dc860e4b959d9ce45a, "selectedFilePathOutput"); // (FileBrowser (id_d2cd88ea57e04aae8cc57bdd1466b4d6).selectedFilePathOutput) -- [IDataFlow<string>] --> (DataFlowConnector<string> (id_edb0165c192041dc860e4b959d9ce45a).dataInput)
            id_edb0165c192041dc860e4b959d9ce45a.WireTo(id_367efd63e1234a75b0c82f727bce73f9, "fanoutList"); // (DataFlowConnector<string> (id_edb0165c192041dc860e4b959d9ce45a).fanoutList) -- [IDataFlow<string>] --> (SaveGraphToFile (id_367efd63e1234a75b0c82f727bce73f9).filePathInput)
            id_edb0165c192041dc860e4b959d9ce45a.WireTo(id_9a2dda5813924951b284fc2b89e6b4bf, "fanoutList"); // (DataFlowConnector<string> (id_edb0165c192041dc860e4b959d9ce45a).fanoutList) -- [IDataFlow<string>] --> (TextBox (id_9a2dda5813924951b284fc2b89e6b4bf).textInput)
            id_edb0165c192041dc860e4b959d9ce45a.WireTo(id_03c7d3d4376a45b0b3d0369fd6196e73, "fanoutList"); // (DataFlowConnector<string> (id_edb0165c192041dc860e4b959d9ce45a).fanoutList) -- [IDataFlow<string>] --> (Cast<string,object> (id_03c7d3d4376a45b0b3d0369fd6196e73).input)
            id_367efd63e1234a75b0c82f727bce73f9.WireTo(id_9249e199685e42e99523221b2694bc43, "complete"); // (SaveGraphToFile (id_367efd63e1234a75b0c82f727bce73f9).complete) -- [IEvent] --> (EventConnector (id_9249e199685e42e99523221b2694bc43).eventInput)
            id_d5b1d9d4f564496eac0ce8aa0242f37f.WireTo(id_0b7120e1a1e8497cbccd23143c37962c, "clickedEvent"); // (MenuItem (id_d5b1d9d4f564496eac0ce8aa0242f37f).clickedEvent) -- [IEvent] --> (EventConnector (id_0b7120e1a1e8497cbccd23143c37962c).eventInput)
            id_0b7120e1a1e8497cbccd23143c37962c.WireTo(id_0ca4e9a278fd45ddbb1083cea911253e, "fanoutList"); // (EventConnector (id_0b7120e1a1e8497cbccd23143c37962c).fanoutList) -- [IEvent] --> (FileBrowser (id_0ca4e9a278fd45ddbb1083cea911253e).openBrowser)
            id_0ca4e9a278fd45ddbb1083cea911253e.WireTo(id_75b23a2bca964cc3855a25fdfdda253d, "selectedFilePathOutput"); // (FileBrowser (id_0ca4e9a278fd45ddbb1083cea911253e).selectedFilePathOutput) -- [IDataFlow<string>] --> (DataFlowConnector<string> (id_75b23a2bca964cc3855a25fdfdda253d).dataInput)
            id_75b23a2bca964cc3855a25fdfdda253d.WireTo(id_cdcf16842f164e89b234c1591cebf5d3, "fanoutList"); // (DataFlowConnector<string> (id_75b23a2bca964cc3855a25fdfdda253d).fanoutList) -- [IDataFlow<string>] --> (FileReader (id_cdcf16842f164e89b234c1591cebf5d3).filePathInput)
            id_cdcf16842f164e89b234c1591cebf5d3.WireTo(id_9547a5e7fd764ad0a1b3fc636f24e2d9, "fileContentOutput"); // (FileReader (id_cdcf16842f164e89b234c1591cebf5d3).fileContentOutput) -- [IDataFlow<string>] --> (ExtractALACode (id_9547a5e7fd764ad0a1b3fc636f24e2d9).codeInput)
            id_9547a5e7fd764ad0a1b3fc636f24e2d9.WireTo(id_439d102714d245929de8b14c55d40c1d, "instantiationCodeOutput"); // (ExtractALACode (id_9547a5e7fd764ad0a1b3fc636f24e2d9).instantiationCodeOutput) -- [IDataFlow<string>] --> (Apply<string,IEnumerable<string>> (id_439d102714d245929de8b14c55d40c1d).input)
            id_9547a5e7fd764ad0a1b3fc636f24e2d9.WireTo(id_7af338faa6bb48c29d1b583377d6d0f0, "wiringCodeOutput"); // (ExtractALACode (id_9547a5e7fd764ad0a1b3fc636f24e2d9).wiringCodeOutput) -- [IDataFlow<string>] --> (Apply<string,IEnumerable<string>> (id_7af338faa6bb48c29d1b583377d6d0f0).input)
            id_439d102714d245929de8b14c55d40c1d.WireTo(id_e0fbfce24b1d4c78b1b521515e82f020, "output"); // (Apply<string,IEnumerable<string>> (id_439d102714d245929de8b14c55d40c1d).output) -- [IDataFlow<IEnumerable<string>>] --> (ForEach<string> (id_e0fbfce24b1d4c78b1b521515e82f020).collectionInput)
            id_e0fbfce24b1d4c78b1b521515e82f020.WireTo(id_76e6991c29e74f6cb3c9dd8eeea60553, "elementOutput"); // (ForEach<string> (id_e0fbfce24b1d4c78b1b521515e82f020).elementOutput) -- [IDataFlow<string>] --> (DataFlowConnector<string> (id_76e6991c29e74f6cb3c9dd8eeea60553).dataInput)
            id_76e6991c29e74f6cb3c9dd8eeea60553.WireTo(id_f4f07ea01bf5480397359bf8f7e24704, "fanoutList"); // (DataFlowConnector<string> (id_76e6991c29e74f6cb3c9dd8eeea60553).fanoutList) -- [IDataFlow<string>] --> (InverseStringFormat (id_f4f07ea01bf5480397359bf8f7e24704).stringInput)
            id_76e6991c29e74f6cb3c9dd8eeea60553.WireTo(id_4b94ac7fd4434b4ea6751f501b8eaa9d, "fanoutList"); // (DataFlowConnector<string> (id_76e6991c29e74f6cb3c9dd8eeea60553).fanoutList) -- [IDataFlow<string>] --> (ConvertToEvent<string> (id_4b94ac7fd4434b4ea6751f501b8eaa9d).start)
            id_f4f07ea01bf5480397359bf8f7e24704.WireTo(id_3ab6567c18b44f8698401afb8ba7b0c7, "extractedParametersOutput"); // (InverseStringFormat (id_f4f07ea01bf5480397359bf8f7e24704).extractedParametersOutput) -- [IDataFlow<Dictionary<string,string>>] --> (DataFlowConnector<Dictionary<string,string>> (id_3ab6567c18b44f8698401afb8ba7b0c7).dataInput)
            id_4b94ac7fd4434b4ea6751f501b8eaa9d.WireTo(id_c5e2618f94d34279a81f129aaf56e02d, "eventOutput"); // (ConvertToEvent<string> (id_4b94ac7fd4434b4ea6751f501b8eaa9d).eventOutput) -- [IEvent] --> (EventConnector (id_c5e2618f94d34279a81f129aaf56e02d).eventInput)
            id_c5e2618f94d34279a81f129aaf56e02d.WireTo(id_5db913688cf343229b5b022d9d9eef4d, "fanoutList"); // (EventConnector (id_c5e2618f94d34279a81f129aaf56e02d).fanoutList) -- [IEvent] --> (Data<string> (id_5db913688cf343229b5b022d9d9eef4d).start)
            id_c5e2618f94d34279a81f129aaf56e02d.WireTo(id_307980ece0d34a65b2b69e58b0400ad7, "fanoutList"); // (EventConnector (id_c5e2618f94d34279a81f129aaf56e02d).fanoutList) -- [IEvent] --> (Data<string> (id_307980ece0d34a65b2b69e58b0400ad7).start)
            id_c5e2618f94d34279a81f129aaf56e02d.WireTo(id_28ff30e197fe4cb9b40ff44efcc0f3dd, "fanoutList"); // (EventConnector (id_c5e2618f94d34279a81f129aaf56e02d).fanoutList) -- [IEvent] --> (Data<string> (id_28ff30e197fe4cb9b40ff44efcc0f3dd).start)
            id_c5e2618f94d34279a81f129aaf56e02d.WireTo(id_52831ca26eb34f88867bc54a4011bc55, "fanoutList"); // (EventConnector (id_c5e2618f94d34279a81f129aaf56e02d).fanoutList) -- [IEvent] --> (Data<string> (id_52831ca26eb34f88867bc54a4011bc55).start)
            id_c5e2618f94d34279a81f129aaf56e02d.WireTo(id_144ab8b860b6473db75b7ee5594a2f6e, "complete"); // (EventConnector (id_c5e2618f94d34279a81f129aaf56e02d).complete) -- [IEvent] --> (NewVisualPortGraphNode (id_144ab8b860b6473db75b7ee5594a2f6e).create)
            id_5db913688cf343229b5b022d9d9eef4d.WireTo(id_d6c3ad8f1ec642959f21ad5e67528a26, "dataOutput"); // (Data<string> (id_5db913688cf343229b5b022d9d9eef4d).dataOutput) -- [IDataFlow<string>] --> (LookupTable<string,string> (id_d6c3ad8f1ec642959f21ad5e67528a26).keyInput)
            id_d6c3ad8f1ec642959f21ad5e67528a26.WireTo(id_3ab6567c18b44f8698401afb8ba7b0c7, "dictionaryInput"); // (LookupTable<string,string> (id_d6c3ad8f1ec642959f21ad5e67528a26).dictionaryInput) -- [IDataFlowB<Dictionary<string,string>>] --> (DataFlowConnector<Dictionary<string,string>> (id_3ab6567c18b44f8698401afb8ba7b0c7).dataOutputB)
            id_d6c3ad8f1ec642959f21ad5e67528a26.WireTo(id_3c796a63d81949719a85f4c8dd1cfe8c, "valueOutput"); // (LookupTable<string,string> (id_d6c3ad8f1ec642959f21ad5e67528a26).valueOutput) -- [IDataFlow<string>] --> (DataFlowConnector<string> (id_3c796a63d81949719a85f4c8dd1cfe8c).dataInput)
            id_307980ece0d34a65b2b69e58b0400ad7.WireTo(id_fe380a323be94bc0a4611cae9211790a, "dataOutput"); // (Data<string> (id_307980ece0d34a65b2b69e58b0400ad7).dataOutput) -- [IDataFlow<string>] --> (LookupTable<string,string> (id_fe380a323be94bc0a4611cae9211790a).keyInput)
            id_fe380a323be94bc0a4611cae9211790a.WireTo(id_3ab6567c18b44f8698401afb8ba7b0c7, "dictionaryInput"); // (LookupTable<string,string> (id_fe380a323be94bc0a4611cae9211790a).dictionaryInput) -- [IDataFlowB<Dictionary<string,string>>] --> (DataFlowConnector<Dictionary<string,string>> (id_3ab6567c18b44f8698401afb8ba7b0c7).dataOutputB)
            id_fe380a323be94bc0a4611cae9211790a.WireTo(id_4cc2810d4424461d885779ea461524e8, "valueOutput"); // (LookupTable<string,string> (id_fe380a323be94bc0a4611cae9211790a).valueOutput) -- [IDataFlow<string>] --> (DataFlowConnector<string> (id_4cc2810d4424461d885779ea461524e8).dataInput)
            id_28ff30e197fe4cb9b40ff44efcc0f3dd.WireTo(id_736e067094ca4479b4d51484d4979aa4, "dataOutput"); // (Data<string> (id_28ff30e197fe4cb9b40ff44efcc0f3dd).dataOutput) -- [IDataFlow<string>] --> (LookupTable<string,string> (id_736e067094ca4479b4d51484d4979aa4).keyInput)
            id_736e067094ca4479b4d51484d4979aa4.WireTo(id_3ab6567c18b44f8698401afb8ba7b0c7, "dictionaryInput"); // (LookupTable<string,string> (id_736e067094ca4479b4d51484d4979aa4).dictionaryInput) -- [IDataFlowB<Dictionary<string,string>>] --> (DataFlowConnector<Dictionary<string,string>> (id_3ab6567c18b44f8698401afb8ba7b0c7).dataOutputB)
            id_736e067094ca4479b4d51484d4979aa4.WireTo(unparsedConstructorArgsString, "valueOutput"); // (LookupTable<string,string> (id_736e067094ca4479b4d51484d4979aa4).valueOutput) -- [IDataFlow<string>] --> (DataFlowConnector<string> (unparsedConstructorArgsString).dataInput)
            unparsedConstructorArgsString.WireTo(id_6df96338c4fd4b2fa8f031b5a1126e73, "fanoutList"); // (DataFlowConnector<string> (unparsedConstructorArgsString).fanoutList) -- [IDataFlow<string>] --> (StringSequenceExtractor (id_6df96338c4fd4b2fa8f031b5a1126e73).unparsedInput)
            id_6df96338c4fd4b2fa8f031b5a1126e73.WireTo(id_bea8af68fc04408986c8dbad2cbf795e, "sequenceOutput"); // (StringSequenceExtractor (id_6df96338c4fd4b2fa8f031b5a1126e73).sequenceOutput) -- [IDataFlow<List<string>>] --> (Apply<List<string>,Dictionary<string,string>> (id_bea8af68fc04408986c8dbad2cbf795e).input)
            id_bea8af68fc04408986c8dbad2cbf795e.WireTo(id_e471de7bcb1845a1a60f0b824230a00f, "output"); // (Apply<List<string>,Dictionary<string,string>> (id_bea8af68fc04408986c8dbad2cbf795e).output) -- [IDataFlow<Dictionary<string,string>>] --> (DataFlowConnector<Dictionary<string,string>> (id_e471de7bcb1845a1a60f0b824230a00f).dataInput)
            id_52831ca26eb34f88867bc54a4011bc55.WireTo(id_129243f50531479fb817c9ca866f24eb, "dataOutput"); // (Data<string> (id_52831ca26eb34f88867bc54a4011bc55).dataOutput) -- [IDataFlow<string>] --> (LookupTable<string,string> (id_129243f50531479fb817c9ca866f24eb).keyInput)
            id_129243f50531479fb817c9ca866f24eb.WireTo(id_3ab6567c18b44f8698401afb8ba7b0c7, "dictionaryInput"); // (LookupTable<string,string> (id_129243f50531479fb817c9ca866f24eb).dictionaryInput) -- [IDataFlowB<Dictionary<string,string>>] --> (DataFlowConnector<Dictionary<string,string>> (id_3ab6567c18b44f8698401afb8ba7b0c7).dataOutputB)
            id_129243f50531479fb817c9ca866f24eb.WireTo(unparsedPropertiesString, "valueOutput"); // (LookupTable<string,string> (id_129243f50531479fb817c9ca866f24eb).valueOutput) -- [IDataFlow<string>] --> (DataFlowConnector<string> (unparsedPropertiesString).dataInput)
            unparsedPropertiesString.WireTo(id_258247722bc042a3b68b6d628416a490, "fanoutList"); // (DataFlowConnector<string> (unparsedPropertiesString).fanoutList) -- [IDataFlow<string>] --> (StringSequenceExtractor (id_258247722bc042a3b68b6d628416a490).unparsedInput)
            id_258247722bc042a3b68b6d628416a490.WireTo(id_5ada023340cc46bdbe9a1cc5668e14f9, "sequenceOutput"); // (StringSequenceExtractor (id_258247722bc042a3b68b6d628416a490).sequenceOutput) -- [IDataFlow<List<string>>] --> (Apply<List<string>,Dictionary<string,string>> (id_5ada023340cc46bdbe9a1cc5668e14f9).input)
            id_5ada023340cc46bdbe9a1cc5668e14f9.WireTo(id_41c01831f2c548eea710bc7766db0ee2, "output"); // (Apply<List<string>,Dictionary<string,string>> (id_5ada023340cc46bdbe9a1cc5668e14f9).output) -- [IDataFlow<Dictionary<string,string>>] --> (DataFlowConnector<Dictionary<string,string>> (id_41c01831f2c548eea710bc7766db0ee2).dataInput)
            id_144ab8b860b6473db75b7ee5594a2f6e.WireTo(id_3c796a63d81949719a85f4c8dd1cfe8c, "typeInput"); // (NewVisualPortGraphNode (id_144ab8b860b6473db75b7ee5594a2f6e).typeInput) -- [IDataFlowB<string>] --> (DataFlowConnector<string> (id_3c796a63d81949719a85f4c8dd1cfe8c).dataOutputB)
            id_144ab8b860b6473db75b7ee5594a2f6e.WireTo(id_4cc2810d4424461d885779ea461524e8, "nameInput"); // (NewVisualPortGraphNode (id_144ab8b860b6473db75b7ee5594a2f6e).nameInput) -- [IDataFlowB<string>] --> (DataFlowConnector<string> (id_4cc2810d4424461d885779ea461524e8).dataOutputB)
            id_144ab8b860b6473db75b7ee5594a2f6e.WireTo(id_c16ca284660f45679a81fd52607fec03, "portsInput"); // (NewVisualPortGraphNode (id_144ab8b860b6473db75b7ee5594a2f6e).portsInput) -- [IDataFlowB<List<Port>>] --> (DataFlowConnector<List<Port>> (id_c16ca284660f45679a81fd52607fec03).returnDataB)
            id_144ab8b860b6473db75b7ee5594a2f6e.WireTo(id_e471de7bcb1845a1a60f0b824230a00f, "namedConstructorArgumentsInput"); // (NewVisualPortGraphNode (id_144ab8b860b6473db75b7ee5594a2f6e).namedConstructorArgumentsInput) -- [IDataFlowB<Dictionary<string,string>>] --> (DataFlowConnector<Dictionary<string,string>> (id_e471de7bcb1845a1a60f0b824230a00f).dataOutputB)
            id_144ab8b860b6473db75b7ee5594a2f6e.WireTo(id_41c01831f2c548eea710bc7766db0ee2, "nodePropertiesInput"); // (NewVisualPortGraphNode (id_144ab8b860b6473db75b7ee5594a2f6e).nodePropertiesInput) -- [IDataFlowB<Dictionary<string,string>>] --> (DataFlowConnector<Dictionary<string,string>> (id_41c01831f2c548eea710bc7766db0ee2).dataOutputB)
            id_144ab8b860b6473db75b7ee5594a2f6e.WireTo(id_2c7a7667a1a441a09259e9878a9d8319, "nodeOutput"); // (NewVisualPortGraphNode (id_144ab8b860b6473db75b7ee5594a2f6e).nodeOutput) -- [IDataFlow<IVisualPortGraphNode>] --> (ApplyAction<IVisualPortGraphNode> (id_2c7a7667a1a441a09259e9878a9d8319).input)
            id_144ab8b860b6473db75b7ee5594a2f6e.WireTo(id_c5563a5bfcc94cc781a29126f4fb2aab, "contextMenuInput"); // (NewVisualPortGraphNode (id_144ab8b860b6473db75b7ee5594a2f6e).contextMenuInput) -- [IUI] --> (VPGNContextMenu (id_c5563a5bfcc94cc781a29126f4fb2aab).child)
            id_144ab8b860b6473db75b7ee5594a2f6e.WireTo(id_d8af30a5ec074a94a9cbd052a5194f91, "typeChanged"); // (NewVisualPortGraphNode (id_144ab8b860b6473db75b7ee5594a2f6e).typeChanged) -- [IEvent] --> (Data<IVisualPortGraphNode> (id_d8af30a5ec074a94a9cbd052a5194f91).start)
            id_7af338faa6bb48c29d1b583377d6d0f0.WireTo(id_e6fc28cbe37d4bdd86378e9e54b500ba, "output"); // (Apply<string,IEnumerable<string>> (id_7af338faa6bb48c29d1b583377d6d0f0).output) -- [IDataFlow<IEnumerable<string>>] --> (ForEach<string> (id_e6fc28cbe37d4bdd86378e9e54b500ba).collectionInput)
            id_5f6a7fde612d41749444eaab610a2a50.WireTo(id_5a075d3abdd94340bf112c17d9d3738f, "children"); // (MenuItem (id_5f6a7fde612d41749444eaab610a2a50).children) -- [IUI] --> (MenuItem (id_5a075d3abdd94340bf112c17d9d3738f).child)
            id_5a075d3abdd94340bf112c17d9d3738f.WireTo(id_d4cff65ce0a24619b2f59178001f2041, "clickedEvent"); // (MenuItem (id_5a075d3abdd94340bf112c17d9d3738f).clickedEvent) -- [IEvent] --> (PopupWindow (id_d4cff65ce0a24619b2f59178001f2041).open)
            id_d4cff65ce0a24619b2f59178001f2041.WireTo(id_218f2d941be344289ee5dd3d54b3dedd, "children"); // (PopupWindow (id_d4cff65ce0a24619b2f59178001f2041).children) -- [IUI] --> (Vertical (id_218f2d941be344289ee5dd3d54b3dedd).child)
            id_218f2d941be344289ee5dd3d54b3dedd.WireTo(id_be7144a9493b458693681f9773618056, "children"); // (Vertical (id_218f2d941be344289ee5dd3d54b3dedd).children) -- [List<IUI>] --> (Horizontal (id_be7144a9493b458693681f9773618056).child)
            id_218f2d941be344289ee5dd3d54b3dedd.WireTo(id_5f4945f32824499da007be8fd7deb435, "children"); // (Vertical (id_218f2d941be344289ee5dd3d54b3dedd).children) -- [List<IUI>] --> (Horizontal (id_5f4945f32824499da007be8fd7deb435).child)
            id_be7144a9493b458693681f9773618056.WireTo(id_8bf217c805564f4fb5029c180ae5d85e, "children"); // (Horizontal (id_be7144a9493b458693681f9773618056).children) -- [IUI] --> (Text (id_8bf217c805564f4fb5029c180ae5d85e).child)
            id_be7144a9493b458693681f9773618056.WireTo(id_9a2dda5813924951b284fc2b89e6b4bf, "children"); // (Horizontal (id_be7144a9493b458693681f9773618056).children) -- [IUI] --> (TextBox (id_9a2dda5813924951b284fc2b89e6b4bf).child)
            id_be7144a9493b458693681f9773618056.WireTo(id_4f49dc59a4974186ad62a7a7079019db, "children"); // (Horizontal (id_be7144a9493b458693681f9773618056).children) -- [IUI] --> (Button (id_4f49dc59a4974186ad62a7a7079019db).child)
            id_4f49dc59a4974186ad62a7a7079019db.WireTo(id_e85579ffff934a2a8374894973464fc9, "eventButtonClicked"); // (Button (id_4f49dc59a4974186ad62a7a7079019db).eventButtonClicked) -- [IEvent] --> (EventConnector (id_e85579ffff934a2a8374894973464fc9).eventInput)
            id_5f4945f32824499da007be8fd7deb435.WireTo(id_c835aee22753415894840b4d3dd6582f, "children"); // (Horizontal (id_5f4945f32824499da007be8fd7deb435).children) -- [IUI] --> (Text (id_c835aee22753415894840b4d3dd6582f).child)
            id_5f4945f32824499da007be8fd7deb435.WireTo(id_8de11133b1074cfe915e847951e130a8, "children"); // (Horizontal (id_5f4945f32824499da007be8fd7deb435).children) -- [IUI] --> (TextBox (id_8de11133b1074cfe915e847951e130a8).child)
            id_5f4945f32824499da007be8fd7deb435.WireTo(id_2f3f5348a9f44aad90e115c61bde205a, "children"); // (Horizontal (id_5f4945f32824499da007be8fd7deb435).children) -- [IUI] --> (Button (id_2f3f5348a9f44aad90e115c61bde205a).child)
            id_2f3f5348a9f44aad90e115c61bde205a.WireTo(id_217c6c13105f4f9fbd9c5fa37762fba1, "eventButtonClicked"); // (Button (id_2f3f5348a9f44aad90e115c61bde205a).eventButtonClicked) -- [IEvent] --> (EventConnector (id_217c6c13105f4f9fbd9c5fa37762fba1).eventInput)
            id_53bd59ce7e5243a297da6f78fc494e2f.WireTo(id_ac9c725c30d94bc4aca3a74a8242f9ea, "children"); // (MenuItem (id_53bd59ce7e5243a297da6f78fc494e2f).children) -- [IUI] --> (MenuItem (id_ac9c725c30d94bc4aca3a74a8242f9ea).child)
            id_ac9c725c30d94bc4aca3a74a8242f9ea.WireTo(id_4783415fded6420a83e0b214a3abf2e0, "clickedEvent"); // (MenuItem (id_ac9c725c30d94bc4aca3a74a8242f9ea).clickedEvent) -- [IEvent] --> (PopupWindow (id_4783415fded6420a83e0b214a3abf2e0).open)
            id_4783415fded6420a83e0b214a3abf2e0.WireTo(id_0915024382b54ddc9024fa295cec6c6b, "children"); // (PopupWindow (id_4783415fded6420a83e0b214a3abf2e0).children) -- [IUI] --> (TabContainer (id_0915024382b54ddc9024fa295cec6c6b).child)
            id_0915024382b54ddc9024fa295cec6c6b.WireTo(id_9648955167cb40349ef63dd02441544b, "childrenTabs"); // (TabContainer (id_0915024382b54ddc9024fa295cec6c6b).childrenTabs) -- [List<IUI>] --> (Tab (id_9648955167cb40349ef63dd02441544b).child)
            id_0915024382b54ddc9024fa295cec6c6b.WireTo(id_53214f9414b6403aa7b5606c2fdcabc3, "childrenTabs"); // (TabContainer (id_0915024382b54ddc9024fa295cec6c6b).childrenTabs) -- [List<IUI>] --> (Tab (id_53214f9414b6403aa7b5606c2fdcabc3).child)
            id_0915024382b54ddc9024fa295cec6c6b.WireTo(id_9dd636737f044cc2a09a856a53d924fe, "childrenTabs"); // (TabContainer (id_0915024382b54ddc9024fa295cec6c6b).childrenTabs) -- [List<IUI>] --> (Tab (id_9dd636737f044cc2a09a856a53d924fe).child)
            id_9648955167cb40349ef63dd02441544b.WireTo(id_45319d735b1040419b709825dc1cf23a, "tabItemList"); // (Tab (id_9648955167cb40349ef63dd02441544b).tabItemList) -- [List<IUI>] --> (Text (id_45319d735b1040419b709825dc1cf23a).child)
            id_53214f9414b6403aa7b5606c2fdcabc3.WireTo(id_0cffa533202d4848a0cef1107ee8bcce, "tabItemList"); // (Tab (id_53214f9414b6403aa7b5606c2fdcabc3).tabItemList) -- [List<IUI>] --> (Text (id_0cffa533202d4848a0cef1107ee8bcce).child)
            id_9dd636737f044cc2a09a856a53d924fe.WireTo(id_4d5c2d30a848480ea51139695f487d9e, "tabItemList"); // (Tab (id_9dd636737f044cc2a09a856a53d924fe).tabItemList) -- [List<IUI>] --> (Text (id_4d5c2d30a848480ea51139695f487d9e).child)
            id_e9be6ad589cb41ada5f3dabdbf4dad20.WireTo(id_9249e199685e42e99523221b2694bc43, "clickedEvent"); // (MenuItem (id_e9be6ad589cb41ada5f3dabdbf4dad20).clickedEvent) -- [IEvent] --> (EventConnector (id_9249e199685e42e99523221b2694bc43).eventInput)
            id_9249e199685e42e99523221b2694bc43.WireTo(id_c2b411781c464b86880201607a2e1ee5, "fanoutList"); // (EventConnector (id_9249e199685e42e99523221b2694bc43).fanoutList) -- [IEvent] --> (EventConnector (id_c2b411781c464b86880201607a2e1ee5).eventInput)
            id_9249e199685e42e99523221b2694bc43.WireTo(id_5e0e0964797245f7a9ebaec14901ccac, "fanoutList"); // (EventConnector (id_9249e199685e42e99523221b2694bc43).fanoutList) -- [IEvent] --> (GenerateCode (id_5e0e0964797245f7a9ebaec14901ccac).start)
            id_9249e199685e42e99523221b2694bc43.WireTo(id_7ca8d59d03ab4f06aad5827b56b26610, "fanoutList"); // (EventConnector (id_9249e199685e42e99523221b2694bc43).fanoutList) -- [IEvent] --> (InsertFileCodeLines (id_7ca8d59d03ab4f06aad5827b56b26610).start)
            id_9249e199685e42e99523221b2694bc43.WireTo(id_9722e72251f54855b744639c5b96fda1, "fanoutList"); // (EventConnector (id_9249e199685e42e99523221b2694bc43).fanoutList) -- [IEvent] --> (InsertFileCodeLines (id_9722e72251f54855b744639c5b96fda1).start)
            id_c2b411781c464b86880201607a2e1ee5.WireTo(id_66690e3d478746f485717c3de09d7e75, "fanoutList"); // (EventConnector (id_c2b411781c464b86880201607a2e1ee5).fanoutList) -- [IEvent] --> (OutputTab (id_66690e3d478746f485717c3de09d7e75).clear)
            id_c2b411781c464b86880201607a2e1ee5.WireTo(id_20b52d9c5ffd4a40b0ec89ad945997c5, "fanoutList"); // (EventConnector (id_c2b411781c464b86880201607a2e1ee5).fanoutList) -- [IEvent] --> (Data<string> (id_20b52d9c5ffd4a40b0ec89ad945997c5).start)
            id_5e0e0964797245f7a9ebaec14901ccac.WireTo(id_98e6fe348eda4c089f1b1ecda4470d38, "instantiationLinesOutput"); // (GenerateCode (id_5e0e0964797245f7a9ebaec14901ccac).instantiationLinesOutput) -- [IDataFlow<List<string>>] --> (DataFlowConnector<List<string>> (id_98e6fe348eda4c089f1b1ecda4470d38).dataInput)
            id_5e0e0964797245f7a9ebaec14901ccac.WireTo(id_9438bab06adc4284b29a52063c06269b, "wiringLinesOutput"); // (GenerateCode (id_5e0e0964797245f7a9ebaec14901ccac).wiringLinesOutput) -- [IDataFlow<List<string>>] --> (DataFlowConnector<List<string>> (id_9438bab06adc4284b29a52063c06269b).dataInput)
            id_98e6fe348eda4c089f1b1ecda4470d38.WireTo(id_7ca8d59d03ab4f06aad5827b56b26610, "fanoutList"); // (DataFlowConnector<List<string>> (id_98e6fe348eda4c089f1b1ecda4470d38).fanoutList) -- [IDataFlow<List<string>>] --> (InsertFileCodeLines (id_7ca8d59d03ab4f06aad5827b56b26610).linesInput)
            id_98e6fe348eda4c089f1b1ecda4470d38.WireTo(id_66690e3d478746f485717c3de09d7e75, "fanoutList"); // (DataFlowConnector<List<string>> (id_98e6fe348eda4c089f1b1ecda4470d38).fanoutList) -- [IDataFlow<List<string>>] --> (OutputTab (id_66690e3d478746f485717c3de09d7e75).linesInput)
            id_9438bab06adc4284b29a52063c06269b.WireTo(id_9722e72251f54855b744639c5b96fda1, "fanoutList"); // (DataFlowConnector<List<string>> (id_9438bab06adc4284b29a52063c06269b).fanoutList) -- [IDataFlow<List<string>>] --> (InsertFileCodeLines (id_9722e72251f54855b744639c5b96fda1).linesInput)
            id_9438bab06adc4284b29a52063c06269b.WireTo(id_66690e3d478746f485717c3de09d7e75, "fanoutList"); // (DataFlowConnector<List<string>> (id_9438bab06adc4284b29a52063c06269b).fanoutList) -- [IDataFlow<List<string>>] --> (OutputTab (id_66690e3d478746f485717c3de09d7e75).linesInput)
            id_7ca8d59d03ab4f06aad5827b56b26610.WireTo(id_9722e72251f54855b744639c5b96fda1, "newFileContentsOutput"); // (InsertFileCodeLines (id_7ca8d59d03ab4f06aad5827b56b26610).newFileContentsOutput) -- [IDataFlow<string>] --> (InsertFileCodeLines (id_9722e72251f54855b744639c5b96fda1).fileContentsInput)
            id_9722e72251f54855b744639c5b96fda1.WireTo(id_c5d2d42c1be6413e92c567f58ebbe0a8, "newFileContentsOutput"); // (InsertFileCodeLines (id_9722e72251f54855b744639c5b96fda1).newFileContentsOutput) -- [IDataFlow<string>] --> (FileWriter (id_c5d2d42c1be6413e92c567f58ebbe0a8).fileContentInput)
            id_c5d2d42c1be6413e92c567f58ebbe0a8.WireTo(id_1fbd51c85a7a4f5d9861b9059f95d9bc, "filePathInput"); // (FileWriter (id_c5d2d42c1be6413e92c567f58ebbe0a8).filePathInput) -- [IDataFlowB<string>] --> (DataFlowConnector<string> (id_1fbd51c85a7a4f5d9861b9059f95d9bc).NEEDNAME)
            id_564ed95481054589ab6059b110b8af7a.WireTo(id_cef138236635424782b3dd215a86008f, "clickedEvent"); // (MenuItem (id_564ed95481054589ab6059b110b8af7a).clickedEvent) -- [IEvent] --> (PopupWindow (id_cef138236635424782b3dd215a86008f).open)
            id_cef138236635424782b3dd215a86008f.WireTo(mousePositionHoriz, "children"); // (PopupWindow (id_cef138236635424782b3dd215a86008f).children) -- [IUI] --> (Horizontal (mousePositionHoriz).NEEDNAME)
            id_cef138236635424782b3dd215a86008f.WireTo(currentDiagramModeHoriz, "children"); // (PopupWindow (id_cef138236635424782b3dd215a86008f).children) -- [IUI] --> (Horizontal (currentDiagramModeHoriz).NEEDNAME)
            mousePositionHoriz.WireTo(id_938e460d35b74b5c8ae7227f1f8b8cc0, "children"); // (Horizontal (mousePositionHoriz).children) -- [IUI] --> (Text (id_938e460d35b74b5c8ae7227f1f8b8cc0).NEEDNAME)
            mousePositionHoriz.WireTo(currentMousePositionText, "children"); // (Horizontal (mousePositionHoriz).children) -- [IUI] --> (Text (currentMousePositionText).NEEDNAME)
            currentDiagramModeHoriz.WireTo(id_b90ac39285c04875bdaae2919a522fed, "children"); // (Horizontal (currentDiagramModeHoriz).children) -- [IUI] --> (Text (id_b90ac39285c04875bdaae2919a522fed).NEEDNAME)
            currentDiagramModeHoriz.WireTo(id_fd0e259a2e644bd8a5f548c225a5e2cd, "children"); // (Horizontal (currentDiagramModeHoriz).children) -- [IUI] --> (Text (id_fd0e259a2e644bd8a5f548c225a5e2cd).NEEDNAME)
            id_2534923aff934248a5493e6d293ad205.WireTo(id_fd0e259a2e644bd8a5f548c225a5e2cd, "currentStateAsStringOutput"); // (StateChangeListener (id_2534923aff934248a5493e6d293ad205).currentStateAsStringOutput) -- [IDataFlow<string>] --> (Text (id_fd0e259a2e644bd8a5f548c225a5e2cd).NEEDNAME)
            id_9d790bbf1430492a8e7b01d0633d5c0b.WireTo(mainCanvas, "children"); // (Vertical (id_9d790bbf1430492a8e7b01d0633d5c0b).children) -- [List<IUI>] --> (CanvasDisplay (mainCanvas).ui)
            id_9d790bbf1430492a8e7b01d0633d5c0b.WireTo(id_86e0bcc1baa94327a0cb2cc59a9c84ad, "children"); // (Vertical (id_9d790bbf1430492a8e7b01d0633d5c0b).children) -- [List<IUI>] --> (TabContainer (id_86e0bcc1baa94327a0cb2cc59a9c84ad).child)
            mainCanvas.WireTo(id_0b17dda4de874253b7ac486bd5f63d04, "canvasOutput"); // (CanvasDisplay (mainCanvas).canvasOutput) -- [IDataFlow<WPFCanvas>] --> (ApplyAction<WPFCanvas> (id_0b17dda4de874253b7ac486bd5f63d04).input)
            mainCanvas.WireTo(id_e0cf4df5b331452cbf15f0806c93cae3, "currentMousePositionOutput"); // (CanvasDisplay (mainCanvas).currentMousePositionOutput) -- [IDataFlow<Point>] --> (DataFlowConnector<Point> (id_e0cf4df5b331452cbf15f0806c93cae3).NEEDNAME)
            mainCanvas.WireTo(id_eb50e52083104a169d8c9fc12c4d0b9e, "resetFocus"); // (CanvasDisplay (mainCanvas).resetFocus) -- [IEventB] --> (EventConnector (id_eb50e52083104a169d8c9fc12c4d0b9e).eventOutputB)
            mainCanvas.WireTo(addChildNodeEvent, "eventHandlers"); // (CanvasDisplay (mainCanvas).eventHandlers) -- [IEventHandler] --> (KeyEvent (addChildNodeEvent).handler)
            mainCanvas.WireTo(id_8ef091fb77c74638ad9f8c015cba207c, "eventHandlers"); // (CanvasDisplay (mainCanvas).eventHandlers) -- [IEventHandler] --> (DragRectMultiSelectNodes (id_8ef091fb77c74638ad9f8c015cba207c).eventHandler)
            mainCanvas.WireTo(id_2a57f9b0cbaf44228172791960b2f4f6, "eventHandlers"); // (CanvasDisplay (mainCanvas).eventHandlers) -- [IEventHandler] --> (KeyEvent (id_2a57f9b0cbaf44228172791960b2f4f6).handler)
            mainCanvas.WireTo(id_6301a2d8482d418190d1b0f9eacf940e, "eventHandlers"); // (CanvasDisplay (mainCanvas).eventHandlers) -- [IEventHandler] --> (KeyEvent (id_6301a2d8482d418190d1b0f9eacf940e).handler)
            mainCanvas.WireTo(id_6d5a4532a2ab493687cf1364d4cca9c4, "eventHandlers"); // (CanvasDisplay (mainCanvas).eventHandlers) -- [IEventHandler] --> (KeyEvent (id_6d5a4532a2ab493687cf1364d4cca9c4).handler)
            mainCanvas.WireTo(id_c33489f097484f1f931e138bf8196f7d, "eventHandlers"); // (CanvasDisplay (mainCanvas).eventHandlers) -- [IEventHandler] --> (KeyEvent (id_c33489f097484f1f931e138bf8196f7d).handler)
            mainCanvas.WireTo(id_1a0f4097208c4ab284b189bf20d13144, "eventHandlers"); // (CanvasDisplay (mainCanvas).eventHandlers) -- [IEventHandler] --> (KeyEvent (id_1a0f4097208c4ab284b189bf20d13144).handler)
            mainCanvas.WireTo(id_f375aad6e9224e4ba28ef0bb07ec6bf2, "eventHandlers"); // (CanvasDisplay (mainCanvas).eventHandlers) -- [IEventHandler] --> (KeyEvent (id_f375aad6e9224e4ba28ef0bb07ec6bf2).handler)
            mainCanvas.WireTo(id_15f9d7690a2942668ecc583d0f914b20, "eventHandlers"); // (CanvasDisplay (mainCanvas).eventHandlers) -- [IEventHandler] --> (KeyEvent (id_15f9d7690a2942668ecc583d0f914b20).handler)
            mainCanvas.WireTo(id_50f3007aa7044fc8a9ba73e553e01adc, "eventHandlers"); // (CanvasDisplay (mainCanvas).eventHandlers) -- [IEventHandler] --> (KeyEvent (id_50f3007aa7044fc8a9ba73e553e01adc).handler)
            mainCanvas.WireTo(id_ed2e008e8c7e45efb12a33509d8b4cc4, "eventHandlers"); // (CanvasDisplay (mainCanvas).eventHandlers) -- [IEventHandler] --> (KeyEvent (id_ed2e008e8c7e45efb12a33509d8b4cc4).handler)
            mainCanvas.WireTo(id_b17e9b350e4147329b84bc4454d7a891, "eventHandlers"); // (CanvasDisplay (mainCanvas).eventHandlers) -- [IEventHandler] --> (KeyEvent (id_b17e9b350e4147329b84bc4454d7a891).handler)
            mainCanvas.WireTo(id_8185a5f1ad134ede825c7013a1b8c332, "eventHandlers"); // (CanvasDisplay (mainCanvas).eventHandlers) -- [IEventHandler] --> (KeyEvent (id_8185a5f1ad134ede825c7013a1b8c332).handler)
            mainCanvas.WireTo(id_fe700e3f70184783940459fc19ee659c, "eventHandlers"); // (CanvasDisplay (mainCanvas).eventHandlers) -- [IEventHandler] --> (KeyEvent (id_fe700e3f70184783940459fc19ee659c).handler)
            mainCanvas.WireTo(id_c8dcb3fa42e947ccb3bb5b4a51e497e7, "eventHandlers"); // (CanvasDisplay (mainCanvas).eventHandlers) -- [IEventHandler] --> (KeyEvent (id_c8dcb3fa42e947ccb3bb5b4a51e497e7).handler)
            mainCanvas.WireTo(id_651e418cd9c84bf5aecfa67eb3b2d83c, "eventHandlers"); // (CanvasDisplay (mainCanvas).eventHandlers) -- [IEventHandler] --> (KeyEvent (id_651e418cd9c84bf5aecfa67eb3b2d83c).handler)
            id_e0cf4df5b331452cbf15f0806c93cae3.WireTo(id_f2277566d5da4570829eb23ba276ef28, "fanoutList"); // (DataFlowConnector<Point> (id_e0cf4df5b331452cbf15f0806c93cae3).fanoutList) -- [IDataFlow<Point>] --> (Apply<Point,string> (id_f2277566d5da4570829eb23ba276ef28).input)
            id_f2277566d5da4570829eb23ba276ef28.WireTo(currentMousePositionText, "output"); // (Apply<Point,string> (id_f2277566d5da4570829eb23ba276ef28).output) -- [IDataFlow<string>] --> (Text (currentMousePositionText).NEEDNAME)
            checkToResetDiagramFocus.WireTo(id_eb50e52083104a169d8c9fc12c4d0b9e, "stateChanged"); // (StateChangeListener (checkToResetDiagramFocus).stateChanged) -- [IEvent] --> (EventConnector (id_eb50e52083104a169d8c9fc12c4d0b9e).eventInput)
            addChildNodeEvent.WireTo(addNewNodeConnector, "eventHappened"); // (KeyEvent (addChildNodeEvent).eventHappened) -- [IEvent] --> (EventConnector (addNewNodeConnector).eventInput)
            addNewNodeConnector.WireTo(id_f9559f3d5d6b4bd09ab31db34d2a9444, "fanoutList"); // (EventConnector (addNewNodeConnector).fanoutList) -- [IEvent] --> (NewVisualPortGraphNode (id_f9559f3d5d6b4bd09ab31db34d2a9444).create)
            addNewNodeConnector.WireTo(id_88a2f062f80d44cf82959f689ec9d48d, "fanoutList"); // (EventConnector (addNewNodeConnector).fanoutList) -- [IEvent] --> (Data<IVisualPortGraphNode> (id_88a2f062f80d44cf82959f689ec9d48d).start)
            addNewNodeConnector.WireTo(id_ec235ee670574ba3a3c1b7882df3005b, "fanoutList"); // (EventConnector (addNewNodeConnector).fanoutList) -- [IEvent] --> (Data<Port> (id_ec235ee670574ba3a3c1b7882df3005b).start)
            addNewNodeConnector.WireTo(id_1fda77ab37d34539ae1ad713cba306e9, "fanoutList"); // (EventConnector (addNewNodeConnector).fanoutList) -- [IEvent] --> (AddConnectionToGraph (id_1fda77ab37d34539ae1ad713cba306e9).create)
            addNewNodeConnector.WireTo(refreshDiagramLayout, "complete"); // (EventConnector (addNewNodeConnector).complete) -- [IEvent] --> (Data<IVisualPortGraphNode> (refreshDiagramLayout).start)
            id_f9559f3d5d6b4bd09ab31db34d2a9444.WireTo(id_00be3421cb494a9d98415a4f0c5cf240, "typeInput"); // (NewVisualPortGraphNode (id_f9559f3d5d6b4bd09ab31db34d2a9444).typeInput) -- [IDataFlowB<string>] --> (DataFlowConnector<string> (id_00be3421cb494a9d98415a4f0c5cf240).returnDataB)
            id_f9559f3d5d6b4bd09ab31db34d2a9444.WireTo(id_7cfec3ed54d04f169a715c147700ed47, "portsInput"); // (NewVisualPortGraphNode (id_f9559f3d5d6b4bd09ab31db34d2a9444).portsInput) -- [IDataFlowB<List<Port>>] --> (DataFlowConnector<List<Port>> (id_7cfec3ed54d04f169a715c147700ed47).returnDataB)
            id_f9559f3d5d6b4bd09ab31db34d2a9444.WireTo(id_57f1e5bdd918406e849d9f0eb5c6f848, "nodeOutput"); // (NewVisualPortGraphNode (id_f9559f3d5d6b4bd09ab31db34d2a9444).nodeOutput) -- [IDataFlow<IVisualPortGraphNode>] --> (DataFlowConnector<IVisualPortGraphNode> (id_57f1e5bdd918406e849d9f0eb5c6f848).dataInput)
            id_f9559f3d5d6b4bd09ab31db34d2a9444.WireTo(id_c5563a5bfcc94cc781a29126f4fb2aab, "contextMenuInput"); // (NewVisualPortGraphNode (id_f9559f3d5d6b4bd09ab31db34d2a9444).contextMenuInput) -- [IUI] --> (VPGNContextMenu (id_c5563a5bfcc94cc781a29126f4fb2aab).child)
            id_f9559f3d5d6b4bd09ab31db34d2a9444.WireTo(id_d8af30a5ec074a94a9cbd052a5194f91, "typeChanged"); // (NewVisualPortGraphNode (id_f9559f3d5d6b4bd09ab31db34d2a9444).typeChanged) -- [IEvent] --> (Data<IVisualPortGraphNode> (id_d8af30a5ec074a94a9cbd052a5194f91).start)
            id_57f1e5bdd918406e849d9f0eb5c6f848.WireTo(id_33fca3f47adf4ae09e8f6fcfacfc0b2d, "fanoutList"); // (DataFlowConnector<IVisualPortGraphNode> (id_57f1e5bdd918406e849d9f0eb5c6f848).fanoutList) -- [IDataFlow<IVisualPortGraphNode>] --> (ApplyAction<IVisualPortGraphNode> (id_33fca3f47adf4ae09e8f6fcfacfc0b2d).input)
            id_57f1e5bdd918406e849d9f0eb5c6f848.WireTo(id_512b7d0231ac4dc59a1a9a22bcc6e650, "fanoutList"); // (DataFlowConnector<IVisualPortGraphNode> (id_57f1e5bdd918406e849d9f0eb5c6f848).fanoutList) -- [IDataFlow<IVisualPortGraphNode>] --> (DataFlowConnector<IVisualPortGraphNode> (id_512b7d0231ac4dc59a1a9a22bcc6e650).dataInput)
            id_c5563a5bfcc94cc781a29126f4fb2aab.WireTo(id_63ed925f29684c5db385cefefb6b6dfa, "saveTemplate"); // (VPGNContextMenu (id_c5563a5bfcc94cc781a29126f4fb2aab).saveTemplate) -- [IEvent] --> (Data<IVisualPortGraphNode> (id_63ed925f29684c5db385cefefb6b6dfa).start)
            id_c5563a5bfcc94cc781a29126f4fb2aab.WireTo(id_23a5fe2b3587408e9df5e94256ead20a, "enablePortEditing"); // (VPGNContextMenu (id_c5563a5bfcc94cc781a29126f4fb2aab).enablePortEditing) -- [IEvent] --> (Data<IVisualPortGraphNode> (id_23a5fe2b3587408e9df5e94256ead20a).start)
            id_c5563a5bfcc94cc781a29126f4fb2aab.WireTo(id_ad282ca7505348ffa6e1580db6073003, "disablePortEditing"); // (VPGNContextMenu (id_c5563a5bfcc94cc781a29126f4fb2aab).disablePortEditing) -- [IEvent] --> (Data<IVisualPortGraphNode> (id_ad282ca7505348ffa6e1580db6073003).start)
            id_63ed925f29684c5db385cefefb6b6dfa.WireTo(id_44388caa4a964fb588bbb8632f7b5fae, "dataOutput"); // (Data<IVisualPortGraphNode> (id_63ed925f29684c5db385cefefb6b6dfa).dataOutput) -- [IDataFlow<IVisualPortGraphNode>] --> (DataFlowConnector<IVisualPortGraphNode> (id_44388caa4a964fb588bbb8632f7b5fae).dataInput)
            id_44388caa4a964fb588bbb8632f7b5fae.WireTo(id_2a44ae50994a441b97eae994df2bb1d2, "fanoutList"); // (DataFlowConnector<IVisualPortGraphNode> (id_44388caa4a964fb588bbb8632f7b5fae).fanoutList) -- [IDataFlow<IVisualPortGraphNode>] --> (Apply<IVisualPortGraphNode,Tuple<string,JToken>> (id_2a44ae50994a441b97eae994df2bb1d2).input)
            id_44388caa4a964fb588bbb8632f7b5fae.WireTo(id_a59158c8c97545dea21d401b8dbf77d4, "fanoutList"); // (DataFlowConnector<IVisualPortGraphNode> (id_44388caa4a964fb588bbb8632f7b5fae).fanoutList) -- [IDataFlow<IVisualPortGraphNode>] --> (ApplyAction<IVisualPortGraphNode> (id_a59158c8c97545dea21d401b8dbf77d4).input)
            id_2a44ae50994a441b97eae994df2bb1d2.WireTo(abstractionTemplates, "output"); // (Apply<IVisualPortGraphNode,Tuple<string,JToken>> (id_2a44ae50994a441b97eae994df2bb1d2).output) -- [IDataFlow<Tuple<string,JToken>>] --> (LookupTable<string,JToken> (abstractionTemplates).pairInput)
            abstractionTemplates.WireTo(abstractionTemplatesConnector, "initialDictionaryInput"); // (LookupTable<string,JToken> (abstractionTemplates).initialDictionaryInput) -- [IDataFlowB<Dictionary<string,JToken>>] --> (@DataFlowConnector<Dictionary<string,JToken>> (abstractionTemplatesConnector).NEEDNAME)
            abstractionTemplates.WireTo(saveAbstractionTemplatesToFile, "dictionaryOutput"); // (LookupTable<string,JToken> (abstractionTemplates).dictionaryOutput) -- [IDataFlow<Dictionary<string,JToken>>] --> (JSONWriter<Dictionary<string,JToken>> (saveAbstractionTemplatesToFile).valueInput)
            abstractionTemplates.WireTo(currentAbstractionTemplate, "valueOutput"); // (LookupTable<string,JToken> (abstractionTemplates).valueOutput) -- [IDataFlow<JToken>] --> (DataFlowConnector<JToken> (currentAbstractionTemplate).dataInput)
            currentAbstractionTemplate.WireTo(loadNewAbstractionTypeTemplate, "fanoutList"); // (DataFlowConnector<JToken> (currentAbstractionTemplate).fanoutList) -- [IDataFlow<JToken>] --> (ApplyAction<JToken> (loadNewAbstractionTypeTemplate).input)
            id_23a5fe2b3587408e9df5e94256ead20a.WireTo(id_19025a1dc84447a2a4487d854e4b3c09, "dataOutput"); // (Data<IVisualPortGraphNode> (id_23a5fe2b3587408e9df5e94256ead20a).dataOutput) -- [IDataFlow<IVisualPortGraphNode>] --> (ApplyAction<IVisualPortGraphNode> (id_19025a1dc84447a2a4487d854e4b3c09).input)
            id_ad282ca7505348ffa6e1580db6073003.WireTo(id_fc4da39a45c14a299cf156681769ddd1, "dataOutput"); // (Data<IVisualPortGraphNode> (id_ad282ca7505348ffa6e1580db6073003).dataOutput) -- [IDataFlow<IVisualPortGraphNode>] --> (ApplyAction<IVisualPortGraphNode> (id_fc4da39a45c14a299cf156681769ddd1).input)
            id_d8af30a5ec074a94a9cbd052a5194f91.WireTo(id_cd868960a86e4e578f0b936f7c309268, "dataOutput"); // (Data<IVisualPortGraphNode> (id_d8af30a5ec074a94a9cbd052a5194f91).dataOutput) -- [IDataFlow<IVisualPortGraphNode>] --> (Apply<IVisualPortGraphNode,string> (id_cd868960a86e4e578f0b936f7c309268).input)
            id_cd868960a86e4e578f0b936f7c309268.WireTo(abstractionTemplates, "output"); // (Apply<IVisualPortGraphNode,string> (id_cd868960a86e4e578f0b936f7c309268).output) -- [IDataFlow<strnig>] --> (LookupTable<string,JToken> (abstractionTemplates).keyInput)
            id_88a2f062f80d44cf82959f689ec9d48d.WireTo(id_a5ae3d0cd5ea4f89a79279b161b0f8ef, "dataOutput"); // (Data<IVisualPortGraphNode> (id_88a2f062f80d44cf82959f689ec9d48d).dataOutput) -- [IDataFlow<IVisualPortGraphNode>] --> (DataFlowConnector<IVisualPortGraphNode> (id_a5ae3d0cd5ea4f89a79279b161b0f8ef).dataInput)
            id_ec235ee670574ba3a3c1b7882df3005b.WireTo(id_6573107860ca42e1acd2edb39e4af23f, "dataOutput"); // (Data<Port> (id_ec235ee670574ba3a3c1b7882df3005b).dataOutput) -- [IDataFlow<Port>] --> (DataFlowConnector<Port> (id_6573107860ca42e1acd2edb39e4af23f).dataInput)
            id_1fda77ab37d34539ae1ad713cba306e9.WireTo(id_a5ae3d0cd5ea4f89a79279b161b0f8ef, "sourceInput"); // (AddConnectionToGraph (id_1fda77ab37d34539ae1ad713cba306e9).sourceInput) -- [IDataFlowB<IVisualPortGraphNode>] --> (DataFlowConnector<IVisualPortGraphNode> (id_a5ae3d0cd5ea4f89a79279b161b0f8ef).returnDataB)
            id_1fda77ab37d34539ae1ad713cba306e9.WireTo(id_512b7d0231ac4dc59a1a9a22bcc6e650, "destinationInput"); // (AddConnectionToGraph (id_1fda77ab37d34539ae1ad713cba306e9).destinationInput) -- [IDataFlowB<IVisualPortGraphNode>] --> (DataFlowConnector<IVisualPortGraphNode> (id_512b7d0231ac4dc59a1a9a22bcc6e650).returnDataB)
            id_1fda77ab37d34539ae1ad713cba306e9.WireTo(id_6573107860ca42e1acd2edb39e4af23f, "sourcePortInput"); // (AddConnectionToGraph (id_1fda77ab37d34539ae1ad713cba306e9).sourcePortInput) -- [IDataFlowB<Port>] --> (DataFlowConnector<Port> (id_6573107860ca42e1acd2edb39e4af23f).returnDataB)
            id_1fda77ab37d34539ae1ad713cba306e9.WireTo(id_da10fa6481e2458fa526983b0036cb4d, "transitionEventHandlers"); // (AddConnectionToGraph (id_1fda77ab37d34539ae1ad713cba306e9).transitionEventHandlers) -- [IEventHandler] --> (StateTransitionEvent<Enums.DiagramMode> (id_da10fa6481e2458fa526983b0036cb4d).handler)
            refreshDiagramLayout.WireTo(id_5de5980d74a1467c8a9462e4427422bf, "dataOutput"); // (Data<IVisualPortGraphNode> (refreshDiagramLayout).dataOutput) -- [IDataFlow<IVisualPortGraphNode>] --> (RightTreeLayout<IVisualPortGraphNode> (id_5de5980d74a1467c8a9462e4427422bf).rootNodeInput)
            id_2a57f9b0cbaf44228172791960b2f4f6.WireTo(id_b1ee7ebeff8944faab0eab9002064540, "eventHappened"); // (KeyEvent (id_2a57f9b0cbaf44228172791960b2f4f6).eventHappened) -- [IEvent] --> (EventLambda (id_b1ee7ebeff8944faab0eab9002064540).start)
            id_6301a2d8482d418190d1b0f9eacf940e.WireTo(id_af3e2fd88447433ab5460ed5879032bd, "eventHappened"); // (KeyEvent (id_6301a2d8482d418190d1b0f9eacf940e).eventHappened) -- [IEvent] --> (EventConnector (id_af3e2fd88447433ab5460ed5879032bd).eventInput)
            id_af3e2fd88447433ab5460ed5879032bd.WireTo(refreshDiagramLayout, "fanoutList"); // (EventConnector (id_af3e2fd88447433ab5460ed5879032bd).fanoutList) -- [IEvent] --> (Data<IVisualPortGraphNode> (refreshDiagramLayout).start)
            id_af3e2fd88447433ab5460ed5879032bd.WireTo(id_8ef091fb77c74638ad9f8c015cba207c, "fanoutList"); // (EventConnector (id_af3e2fd88447433ab5460ed5879032bd).fanoutList) -- [IEvent] --> (DragRectMultiSelectNodes (id_8ef091fb77c74638ad9f8c015cba207c).clear)
            id_6d5a4532a2ab493687cf1364d4cca9c4.WireTo(id_a4f7336dc7fb48ba908eb0f07315e72d, "eventHappened"); // (KeyEvent (id_6d5a4532a2ab493687cf1364d4cca9c4).eventHappened) -- [IEvent] --> (EventLambda (id_a4f7336dc7fb48ba908eb0f07315e72d).start)
            id_c33489f097484f1f931e138bf8196f7d.WireTo(id_e85579ffff934a2a8374894973464fc9, "eventHappened"); // (KeyEvent (id_c33489f097484f1f931e138bf8196f7d).eventHappened) -- [IEvent] --> (EventConnector (id_e85579ffff934a2a8374894973464fc9).eventInput)
            id_1a0f4097208c4ab284b189bf20d13144.WireTo(id_d2cd88ea57e04aae8cc57bdd1466b4d6, "eventHappened"); // (KeyEvent (id_1a0f4097208c4ab284b189bf20d13144).eventHappened) -- [IEvent] --> (FileBrowser (id_d2cd88ea57e04aae8cc57bdd1466b4d6).openBrowser)
            id_f375aad6e9224e4ba28ef0bb07ec6bf2.WireTo(id_d048bf2542914af29725a1a0074bd773, "eventHappened"); // (KeyEvent (id_f375aad6e9224e4ba28ef0bb07ec6bf2).eventHappened) -- [IEvent] --> (GetSetting (id_d048bf2542914af29725a1a0074bd773).start)
            id_15f9d7690a2942668ecc583d0f914b20.WireTo(id_1d31f5078cad49d5b7702910eeaa20a4, "eventHappened"); // (KeyEvent (id_15f9d7690a2942668ecc583d0f914b20).eventHappened) -- [IEvent] --> (EventConnector (id_1d31f5078cad49d5b7702910eeaa20a4).eventInput)
            id_1d31f5078cad49d5b7702910eeaa20a4.WireTo(id_ce3ff0883b504c88be21e246df7d4a54, "fanoutList"); // (EventConnector (id_1d31f5078cad49d5b7702910eeaa20a4).fanoutList) -- [IEvent] --> (EventLambda (id_ce3ff0883b504c88be21e246df7d4a54).start)
            id_1d31f5078cad49d5b7702910eeaa20a4.WireTo(id_f964e6db04b84104bf69e4384732530b, "fanoutList"); // (EventConnector (id_1d31f5078cad49d5b7702910eeaa20a4).fanoutList) -- [IEvent] --> (EventLambda (id_f964e6db04b84104bf69e4384732530b).start)
            id_50f3007aa7044fc8a9ba73e553e01adc.WireTo(id_f243d49f78914970beda200fca69a288, "eventHappened"); // (KeyEvent (id_50f3007aa7044fc8a9ba73e553e01adc).eventHappened) -- [IEvent] --> (Data<IVisualPortGraphNode> (id_f243d49f78914970beda200fca69a288).start)
            id_f243d49f78914970beda200fca69a288.WireTo(id_a4edfc365ec64fd7b366d694cfc078f8, "dataOutput"); // (Data<IVisualPortGraphNode> (id_f243d49f78914970beda200fca69a288).dataOutput) -- [IDataFlow<IVisualPortGraphNode>] --> (Apply<IVisualPortGraphNode,string> (id_a4edfc365ec64fd7b366d694cfc078f8).input)
            id_a4edfc365ec64fd7b366d694cfc078f8.WireTo(id_d65a994fdb9d450cba8cd914cad29f72, "output"); // (Apply<IVisualPortGraphNode,string> (id_a4edfc365ec64fd7b366d694cfc078f8).output) -- [IDataFlow<string>] --> (TextClipboard (id_d65a994fdb9d450cba8cd914cad29f72).contentInput)
            id_ed2e008e8c7e45efb12a33509d8b4cc4.WireTo(id_193ac08eecfb4a4a912c611682eacf5b, "eventHappened"); // (KeyEvent (id_ed2e008e8c7e45efb12a33509d8b4cc4).eventHappened) -- [IEvent] --> (EventConnector (id_193ac08eecfb4a4a912c611682eacf5b).eventInput)
            id_193ac08eecfb4a4a912c611682eacf5b.WireTo(id_5095928ff3f54c39aa0417b1cca9c969, "fanoutList"); // (EventConnector (id_193ac08eecfb4a4a912c611682eacf5b).fanoutList) -- [IEvent] --> (Data<IVisualPortGraphNode> (id_5095928ff3f54c39aa0417b1cca9c969).start)
            id_193ac08eecfb4a4a912c611682eacf5b.WireTo(id_ff149259cafe456d9d100e67375dfa01, "fanoutList"); // (EventConnector (id_193ac08eecfb4a4a912c611682eacf5b).fanoutList) -- [IEvent] --> (Data<Port> (id_ff149259cafe456d9d100e67375dfa01).start)
            id_193ac08eecfb4a4a912c611682eacf5b.WireTo(id_5018f4c773a34ced85806dd175659e9a, "fanoutList"); // (EventConnector (id_193ac08eecfb4a4a912c611682eacf5b).fanoutList) -- [IEvent] --> (TextClipboard (id_5018f4c773a34ced85806dd175659e9a).sendOutput)
            id_193ac08eecfb4a4a912c611682eacf5b.WireTo(addSubtreeToSelectedNode, "complete"); // (EventConnector (id_193ac08eecfb4a4a912c611682eacf5b).complete) -- [IEvent] --> (EventConnector (addSubtreeToSelectedNode).eventInput)
            id_5095928ff3f54c39aa0417b1cca9c969.WireTo(id_d64d68f5fbe74192b82d38c9318dc4b6, "dataOutput"); // (Data<IVisualPortGraphNode> (id_5095928ff3f54c39aa0417b1cca9c969).dataOutput) -- [IDataFlow<IVisualPortGraphNode>] --> (DataFlowConnector<IVisualPortGraphNode> (id_d64d68f5fbe74192b82d38c9318dc4b6).dataInput)
            id_ff149259cafe456d9d100e67375dfa01.WireTo(id_1bd1df9769a64645a11efd76789e261e, "dataOutput"); // (Data<Port> (id_ff149259cafe456d9d100e67375dfa01).dataOutput) -- [IDataFlow<Port>] --> (DataFlowConnector<Port> (id_1bd1df9769a64645a11efd76789e261e).dataInput)
            id_5018f4c773a34ced85806dd175659e9a.WireTo(id_4124e1350f3d437cb9379eaf22b6a892, "contentOutput"); // (TextClipboard (id_5018f4c773a34ced85806dd175659e9a).contentOutput) -- [IDataFlow<string>] --> (DataFlowConnector<string> (id_4124e1350f3d437cb9379eaf22b6a892).dataInput)
            id_4124e1350f3d437cb9379eaf22b6a892.WireTo(id_18191d3fb86d460ab709451888c40955, "fanoutList"); // (DataFlowConnector<string> (id_4124e1350f3d437cb9379eaf22b6a892).fanoutList) -- [IDataFlow<string>] --> (JSONParser (id_18191d3fb86d460ab709451888c40955).jsonInput)
            id_4124e1350f3d437cb9379eaf22b6a892.WireTo(id_e366cba16d6443b6b36846f86aeff130, "fanoutList"); // (DataFlowConnector<string> (id_4124e1350f3d437cb9379eaf22b6a892).fanoutList) -- [IDataFlow<string>] --> (JSONParser (id_e366cba16d6443b6b36846f86aeff130).jsonInput)
            id_4124e1350f3d437cb9379eaf22b6a892.WireTo(id_bdc7b5211833463ba5ddde6249b71158, "fanoutList"); // (DataFlowConnector<string> (id_4124e1350f3d437cb9379eaf22b6a892).fanoutList) -- [IDataFlow<string>] --> (ConvertToEvent<string> (id_bdc7b5211833463ba5ddde6249b71158).start)
            id_18191d3fb86d460ab709451888c40955.WireTo(getOriginalNodeIds, "jTokenOutput"); // (JSONParser (id_18191d3fb86d460ab709451888c40955).jTokenOutput) -- [IDataFlow<JToken>] --> (Apply<JToken,List<string>> (getOriginalNodeIds).input)
            getOriginalNodeIds.WireTo(id_2eb695817eb44bbf910abb5acae18e75, "output"); // (Apply<JToken,List<string>> (getOriginalNodeIds).output) -- [IDataFlow<List<string>>] --> (DataFlowConnector<List<string>> (id_2eb695817eb44bbf910abb5acae18e75).dataInput)
            id_e366cba16d6443b6b36846f86aeff130.WireTo(createNewNodeIds, "jTokenOutput"); // (JSONParser (id_e366cba16d6443b6b36846f86aeff130).jTokenOutput) -- [IDataFlow<JToken>] --> (Apply<JToken,List<string>> (createNewNodeIds).input)
            createNewNodeIds.WireTo(id_651b4ab985874aa6abf6b8354322b0f1, "output"); // (Apply<JToken,List<string>> (createNewNodeIds).output) -- [IDataFlow<List<string>>] --> (DataFlowConnector<List<string>> (id_651b4ab985874aa6abf6b8354322b0f1).dataInput)
            id_bdc7b5211833463ba5ddde6249b71158.WireTo(id_591aaec0820e498c85ae6898d47a7935, "eventOutput"); // (ConvertToEvent<string> (id_bdc7b5211833463ba5ddde6249b71158).eventOutput) -- [IEvent] --> (StringMap (id_591aaec0820e498c85ae6898d47a7935).start)
            id_591aaec0820e498c85ae6898d47a7935.WireTo(id_4124e1350f3d437cb9379eaf22b6a892, "contentToEditInput"); // (StringMap (id_591aaec0820e498c85ae6898d47a7935).contentToEditInput) -- [IDataFlowB<string>] --> (DataFlowConnector<string> (id_4124e1350f3d437cb9379eaf22b6a892).returnDataB)
            id_591aaec0820e498c85ae6898d47a7935.WireTo(id_2eb695817eb44bbf910abb5acae18e75, "oldListInput"); // (StringMap (id_591aaec0820e498c85ae6898d47a7935).oldListInput) -- [IDataFlowB<List<string>>] --> (DataFlowConnector<List<string>> (id_2eb695817eb44bbf910abb5acae18e75).returnDataB)
            id_591aaec0820e498c85ae6898d47a7935.WireTo(id_651b4ab985874aa6abf6b8354322b0f1, "newListInput"); // (StringMap (id_591aaec0820e498c85ae6898d47a7935).newListInput) -- [IDataFlowB<List<string>>] --> (DataFlowConnector<List<string>> (id_651b4ab985874aa6abf6b8354322b0f1).returnDataB)
            id_591aaec0820e498c85ae6898d47a7935.WireTo(id_ad5bca6c08de488e8825baa888f69bbb, "newStringOutput"); // (StringMap (id_591aaec0820e498c85ae6898d47a7935).newStringOutput) -- [IDataFlow<string>] --> (DataFlowConnector<string> (id_ad5bca6c08de488e8825baa888f69bbb).dataInput)
            id_ad5bca6c08de488e8825baa888f69bbb.WireTo(id_2fa52c0f925d4df9b693810373f63348, "fanoutList"); // (DataFlowConnector<string> (id_ad5bca6c08de488e8825baa888f69bbb).fanoutList) -- [IDataFlow<string>] --> (JSONParser (id_2fa52c0f925d4df9b693810373f63348).jsonInput)
            id_ad5bca6c08de488e8825baa888f69bbb.WireTo(id_80d1de4e1ca94967b085e895f3d25443, "fanoutList"); // (DataFlowConnector<string> (id_ad5bca6c08de488e8825baa888f69bbb).fanoutList) -- [IDataFlow<string>] --> (JSONParser (id_80d1de4e1ca94967b085e895f3d25443).jsonInput)
            id_2fa52c0f925d4df9b693810373f63348.WireTo(id_67ce70e081f94919b8ee47d4eb305b1f, "jsonOutput"); // (JSONParser (id_2fa52c0f925d4df9b693810373f63348).jsonOutput) -- [IDataFlow<string>] --> (CreateVPGNsFromJSON (id_67ce70e081f94919b8ee47d4eb305b1f).jsonInput)
            id_67ce70e081f94919b8ee47d4eb305b1f.WireTo(id_c5563a5bfcc94cc781a29126f4fb2aab, "contextMenuInput"); // (CreateVPGNsFromJSON (id_67ce70e081f94919b8ee47d4eb305b1f).contextMenuInput) -- [IUI] --> (VPGNContextMenu (id_c5563a5bfcc94cc781a29126f4fb2aab).child)
            id_67ce70e081f94919b8ee47d4eb305b1f.WireTo(id_d8af30a5ec074a94a9cbd052a5194f91, "typeChanged"); // (CreateVPGNsFromJSON (id_67ce70e081f94919b8ee47d4eb305b1f).typeChanged) -- [IEvent] --> (Data<IVisualPortGraphNode> (id_d8af30a5ec074a94a9cbd052a5194f91).start)
            id_80d1de4e1ca94967b085e895f3d25443.WireTo(id_bd14b7b56e5b43d48292bb6ee2706f3f, "jsonOutput"); // (JSONParser (id_80d1de4e1ca94967b085e895f3d25443).jsonOutput) -- [IDataFlow<string>] --> (CreateConnectionsFromJSON (id_bd14b7b56e5b43d48292bb6ee2706f3f).jsonInput)
            id_bd14b7b56e5b43d48292bb6ee2706f3f.WireTo(id_f16d8476ee2a49aeb64af180bfcc0fa7, "portConnectionTuplesOutput"); // (CreateConnectionsFromJSON (id_bd14b7b56e5b43d48292bb6ee2706f3f).portConnectionTuplesOutput) -- [IDataFlow<List<Tuple<VisualPortGraphNode,VisualPortGraphNode,Port,Port>>>] --> (Cast<List<Tuple<VisualPortGraphNode,VisualPortGraphNode,Port,Port>>,IEnumerable<Tuple<VisualPortGraphNode,VisualPortGraphNode,Port,Port>>> (id_f16d8476ee2a49aeb64af180bfcc0fa7).input)
            id_f16d8476ee2a49aeb64af180bfcc0fa7.WireTo(id_fe4cfcee326e43bbb5231a5de0115973, "output"); // (Cast<List<Tuple<VisualPortGraphNode,VisualPortGraphNode,Port,Port>>,IEnumerable<Tuple<VisualPortGraphNode,VisualPortGraphNode,Port,Port>>> (id_f16d8476ee2a49aeb64af180bfcc0fa7).output) -- [IDataFlow<IEnumerable<Tuple<VisualPortGraphNode,VisualPortGraphNode,Port,Port>>>] --> (ForEach<Tuple<VisualPortGraphNode,VisualPortGraphNode,Port,Port>> (id_fe4cfcee326e43bbb5231a5de0115973).collectionInput)
            id_fe4cfcee326e43bbb5231a5de0115973.WireTo(addPastedConnections, "elementOutput"); // (ForEach<Tuple<VisualPortGraphNode,VisualPortGraphNode,Port,Port>> (id_fe4cfcee326e43bbb5231a5de0115973).elementOutput) -- [IDataFlow<Tuple<VisualPortGraphNode,VisualPortGraphNode,Port,Port>>] --> (AddConnectionToGraph (addPastedConnections).connectionTupleInput)
            addPastedConnections.WireTo(refreshDiagramLayout, "renderLoaded"); // (AddConnectionToGraph (addPastedConnections).renderLoaded) -- [IEvent] --> (Data<IVisualPortGraphNode> (refreshDiagramLayout).start)
            addSubtreeToSelectedNode.WireTo(id_a37f529cebeb4579833301089489c93b, "fanoutList"); // (EventConnector (addSubtreeToSelectedNode).fanoutList) -- [IEvent] --> (Data<IVisualPortGraphNode> (id_a37f529cebeb4579833301089489c93b).start)
            addSubtreeToSelectedNode.WireTo(id_4df5565e5cc74d80a2a2a85ae35373e6, "fanoutList"); // (EventConnector (addSubtreeToSelectedNode).fanoutList) -- [IEvent] --> (Data<Port> (id_4df5565e5cc74d80a2a2a85ae35373e6).start)
            addSubtreeToSelectedNode.WireTo(id_50e48187384540708c950df8cbd511e2, "fanoutList"); // (EventConnector (addSubtreeToSelectedNode).fanoutList) -- [IEvent] --> (Data<string> (id_50e48187384540708c950df8cbd511e2).start)
            addSubtreeToSelectedNode.WireTo(id_33d40c1315df4a329c8069455f07bf4c, "fanoutList"); // (EventConnector (addSubtreeToSelectedNode).fanoutList) -- [IEvent] --> (AddConnectionToGraph (id_33d40c1315df4a329c8069455f07bf4c).create)
            id_a37f529cebeb4579833301089489c93b.WireTo(id_d64d68f5fbe74192b82d38c9318dc4b6, "inputDataB"); // (Data<IVisualPortGraphNode> (id_a37f529cebeb4579833301089489c93b).inputDataB) -- [IDataFlowB<IVisualPortGraphNode>] --> (DataFlowConnector<IVisualPortGraphNode> (id_d64d68f5fbe74192b82d38c9318dc4b6).dataOutputB)
            id_a37f529cebeb4579833301089489c93b.WireTo(id_4bfcbdfaa7514af7ad35513cc6be635c, "dataOutput"); // (Data<IVisualPortGraphNode> (id_a37f529cebeb4579833301089489c93b).dataOutput) -- [IDataFlow<IVisualPortGraphNode>] --> (DataFlowConnector<IVisualPortGraphNode> (id_4bfcbdfaa7514af7ad35513cc6be635c).dataInput)
            id_4df5565e5cc74d80a2a2a85ae35373e6.WireTo(id_1bd1df9769a64645a11efd76789e261e, "inputDataB"); // (Data<Port> (id_4df5565e5cc74d80a2a2a85ae35373e6).inputDataB) -- [IDataFlowB<Port>] --> (DataFlowConnector<Port> (id_1bd1df9769a64645a11efd76789e261e).dataOutputB)
            id_4df5565e5cc74d80a2a2a85ae35373e6.WireTo(id_4b90f593b4f54d7895e5c027e1fe045a, "dataOutput"); // (Data<Port> (id_4df5565e5cc74d80a2a2a85ae35373e6).dataOutput) -- [IDataFlow<Port>] --> (DataFlowConnector<Port> (id_4b90f593b4f54d7895e5c027e1fe045a).dataInput)
            id_50e48187384540708c950df8cbd511e2.WireTo(id_ad5bca6c08de488e8825baa888f69bbb, "inputDataB"); // (Data<string> (id_50e48187384540708c950df8cbd511e2).inputDataB) -- [IDataFlowB<string>] --> (DataFlowConnector<string> (id_ad5bca6c08de488e8825baa888f69bbb).returnDataB)
            id_50e48187384540708c950df8cbd511e2.WireTo(id_8bed896c1b3c4a79b772d859c847c22b, "dataOutput"); // (Data<string> (id_50e48187384540708c950df8cbd511e2).dataOutput) -- [IDataFlow<string>] --> (JSONParser (id_8bed896c1b3c4a79b772d859c847c22b).jsonInput)
            id_8bed896c1b3c4a79b772d859c847c22b.WireTo(id_c1189041ac444838bf1b62f8dfa25929, "jTokenOutput"); // (JSONParser (id_8bed896c1b3c4a79b772d859c847c22b).jTokenOutput) -- [IDataFlow<JToken>] --> (Apply<JToken,IVisualPortGraphNode> (id_c1189041ac444838bf1b62f8dfa25929).input)
            id_c1189041ac444838bf1b62f8dfa25929.WireTo(id_5e6e3fe1f77d40f0ac8320a38a5ca32f, "output"); // (Apply<JToken,IVisualPortGraphNode> (id_c1189041ac444838bf1b62f8dfa25929).output) -- [IDataFlow<IVisualPortGraphNode>] --> (DataFlowConnector<IVisualPortGraphNode> (id_5e6e3fe1f77d40f0ac8320a38a5ca32f).dataInput)
            id_33d40c1315df4a329c8069455f07bf4c.WireTo(id_4bfcbdfaa7514af7ad35513cc6be635c, "sourceInput"); // (AddConnectionToGraph (id_33d40c1315df4a329c8069455f07bf4c).sourceInput) -- [IDataFlowB<IVisualPortGraphNode>] --> (DataFlowConnector<IVisualPortGraphNode> (id_4bfcbdfaa7514af7ad35513cc6be635c).returnDataB)
            id_33d40c1315df4a329c8069455f07bf4c.WireTo(id_5e6e3fe1f77d40f0ac8320a38a5ca32f, "destinationInput"); // (AddConnectionToGraph (id_33d40c1315df4a329c8069455f07bf4c).destinationInput) -- [IDataFlowB<IVisualPortGraphNode>] --> (DataFlowConnector<IVisualPortGraphNode> (id_5e6e3fe1f77d40f0ac8320a38a5ca32f).returnDataB)
            id_33d40c1315df4a329c8069455f07bf4c.WireTo(id_4b90f593b4f54d7895e5c027e1fe045a, "sourcePortInput"); // (AddConnectionToGraph (id_33d40c1315df4a329c8069455f07bf4c).sourcePortInput) -- [IDataFlowB<Port>] --> (DataFlowConnector<Port> (id_4b90f593b4f54d7895e5c027e1fe045a).returnDataB)
            id_33d40c1315df4a329c8069455f07bf4c.WireTo(refreshDiagramLayout, "renderLoaded"); // (AddConnectionToGraph (id_33d40c1315df4a329c8069455f07bf4c).renderLoaded) -- [IEvent] --> (Data<IVisualPortGraphNode> (refreshDiagramLayout).start)
            id_b17e9b350e4147329b84bc4454d7a891.WireTo(id_d3aaec237ab54dab8a28f57baba6aadf, "eventHappened"); // (KeyEvent (id_b17e9b350e4147329b84bc4454d7a891).eventHappened) -- [IEvent] --> (EventConnector (id_d3aaec237ab54dab8a28f57baba6aadf).eventInput)
            id_d3aaec237ab54dab8a28f57baba6aadf.WireTo(id_8fcd9c561a954d528282d3b561f4fe9a, "fanoutList"); // (EventConnector (id_d3aaec237ab54dab8a28f57baba6aadf).fanoutList) -- [IEvent] --> (Data<IVisualPortGraphNode> (id_8fcd9c561a954d528282d3b561f4fe9a).start)
            id_d3aaec237ab54dab8a28f57baba6aadf.WireTo(id_c4d15148a1e44f94b2c224e4ac006f2c, "fanoutList"); // (EventConnector (id_d3aaec237ab54dab8a28f57baba6aadf).fanoutList) -- [IEvent] --> (Data<Port> (id_c4d15148a1e44f94b2c224e4ac006f2c).start)
            id_d3aaec237ab54dab8a28f57baba6aadf.WireTo(id_9193d48caa854fdc953c8b4ada85fa0f, "fanoutList"); // (EventConnector (id_d3aaec237ab54dab8a28f57baba6aadf).fanoutList) -- [IEvent] --> (AddConnectionToGraph (id_9193d48caa854fdc953c8b4ada85fa0f).create)
            id_8fcd9c561a954d528282d3b561f4fe9a.WireTo(id_706acb0a3ea545b59bd8d3d022a1ddb5, "dataOutput"); // (Data<IVisualPortGraphNode> (id_8fcd9c561a954d528282d3b561f4fe9a).dataOutput) -- [IDataFlow<IVisualPortGraphNode>] --> (DataFlowConnector<IVisualPortGraphNode> (id_706acb0a3ea545b59bd8d3d022a1ddb5).dataInput)
            id_c4d15148a1e44f94b2c224e4ac006f2c.WireTo(id_a961ea5c67684cd9be8db19e5508d6bc, "dataOutput"); // (Data<Port> (id_c4d15148a1e44f94b2c224e4ac006f2c).dataOutput) -- [IDataFlow<Port>] --> (DataFlowConnector<Port> (id_a961ea5c67684cd9be8db19e5508d6bc).dataInput)
            id_9193d48caa854fdc953c8b4ada85fa0f.WireTo(id_706acb0a3ea545b59bd8d3d022a1ddb5, "sourceInput"); // (AddConnectionToGraph (id_9193d48caa854fdc953c8b4ada85fa0f).sourceInput) -- [IDataFlowB<IVisualPortGraphNode>] --> (DataFlowConnector<IVisualPortGraphNode> (id_706acb0a3ea545b59bd8d3d022a1ddb5).returnDataB)
            id_9193d48caa854fdc953c8b4ada85fa0f.WireTo(id_a961ea5c67684cd9be8db19e5508d6bc, "sourcePortInput"); // (AddConnectionToGraph (id_9193d48caa854fdc953c8b4ada85fa0f).sourcePortInput) -- [IDataFlowB<Port>] --> (DataFlowConnector<Port> (id_a961ea5c67684cd9be8db19e5508d6bc).returnDataB)
            id_9193d48caa854fdc953c8b4ada85fa0f.WireTo(id_f9a70bf4ccf346ec94c389aa45151170, "connectionIdOutput"); // (AddConnectionToGraph (id_9193d48caa854fdc953c8b4ada85fa0f).connectionIdOutput) -- [IDataFlow<string>] --> (ApplyAction<string> (id_f9a70bf4ccf346ec94c389aa45151170).input)
            id_8185a5f1ad134ede825c7013a1b8c332.WireTo(id_841df8fb3e4145e4845a4b87fb425b8e, "argsOutput"); // (KeyEvent (id_8185a5f1ad134ede825c7013a1b8c332).argsOutput) -- [IDataFlow<KeyEventArgs>] --> (ApplyAction<KeyEventArgs> (id_841df8fb3e4145e4845a4b87fb425b8e).input)
            id_8185a5f1ad134ede825c7013a1b8c332.WireTo(id_3784b9acfef8499795834a495d6f83a2, "eventHappened"); // (KeyEvent (id_8185a5f1ad134ede825c7013a1b8c332).eventHappened) -- [IEvent] --> (Data<IVisualPortGraphNode> (id_3784b9acfef8499795834a495d6f83a2).start)
            id_3784b9acfef8499795834a495d6f83a2.WireTo(id_54d29674558c4b2ab6340b22704eadff, "dataOutput"); // (Data<IVisualPortGraphNode> (id_3784b9acfef8499795834a495d6f83a2).dataOutput) -- [IDataFlow<IVisualPortGraphNode>] --> (ApplyAction<IVisualPortGraphNode> (id_54d29674558c4b2ab6340b22704eadff).input)
            id_fe700e3f70184783940459fc19ee659c.WireTo(id_c81886b79d344934921bf9b69b8b3af9, "eventHappened"); // (KeyEvent (id_fe700e3f70184783940459fc19ee659c).eventHappened) -- [IEvent] --> (Data<IEnumerable<IPortConnection>> (id_c81886b79d344934921bf9b69b8b3af9).start)
            id_c81886b79d344934921bf9b69b8b3af9.WireTo(id_d18001b76b824b4eaadcd206701a2be4, "dataOutput"); // (Data<IEnumerable<IPortConnection>> (id_c81886b79d344934921bf9b69b8b3af9).dataOutput) -- [IDataFlow<IEnumerable<IPortConnection>>] --> (ForEach<IPortConnection> (id_d18001b76b824b4eaadcd206701a2be4).collectionInput)
            id_d18001b76b824b4eaadcd206701a2be4.WireTo(id_f42e25cc193c489aa8f52222148e000e, "elementOutput"); // (ForEach<IPortConnection> (id_d18001b76b824b4eaadcd206701a2be4).elementOutput) -- [IDataFlow<IPortConnection>] --> (ApplyAction<IPortConnection> (id_f42e25cc193c489aa8f52222148e000e).input)
            id_c8dcb3fa42e947ccb3bb5b4a51e497e7.WireTo(id_9249e199685e42e99523221b2694bc43, "eventHappened"); // (KeyEvent (id_c8dcb3fa42e947ccb3bb5b4a51e497e7).eventHappened) -- [IEvent] --> (EventConnector (id_9249e199685e42e99523221b2694bc43).eventInput)
            id_651e418cd9c84bf5aecfa67eb3b2d83c.WireTo(id_d4cff65ce0a24619b2f59178001f2041, "eventHappened"); // (KeyEvent (id_651e418cd9c84bf5aecfa67eb3b2d83c).eventHappened) -- [IEvent] --> (PopupWindow (id_d4cff65ce0a24619b2f59178001f2041).open)
            id_86e0bcc1baa94327a0cb2cc59a9c84ad.WireTo(id_66690e3d478746f485717c3de09d7e75, "childrenTabs"); // (TabContainer (id_86e0bcc1baa94327a0cb2cc59a9c84ad).childrenTabs) -- [List<IUI>] --> (OutputTab (id_66690e3d478746f485717c3de09d7e75).child)
            id_86e0bcc1baa94327a0cb2cc59a9c84ad.WireTo(id_b844b78ad0e54ca8b3ea6eb6bb831c2e, "childrenTabs"); // (TabContainer (id_86e0bcc1baa94327a0cb2cc59a9c84ad).childrenTabs) -- [List<IUI>] --> (NewAbstractionTemplateTab (id_b844b78ad0e54ca8b3ea6eb6bb831c2e).child)
            id_b844b78ad0e54ca8b3ea6eb6bb831c2e.WireTo(id_b15fe1bfb8ea4c56a5332ac4cd16d89c, "programmingParadigmsDropDownListInput"); // (NewAbstractionTemplateTab (id_b844b78ad0e54ca8b3ea6eb6bb831c2e).programmingParadigmsDropDownListInput) -- [IDataFlowB<List<string>>] --> (DataFlowConnector<List<string>> (id_b15fe1bfb8ea4c56a5332ac4cd16d89c).NEEDNAME)
            id_b844b78ad0e54ca8b3ea6eb6bb831c2e.WireTo(newlyCreatedAbstractionTemplate, "newTemplateOutput"); // (NewAbstractionTemplateTab (id_b844b78ad0e54ca8b3ea6eb6bb831c2e).newTemplateOutput) -- [IDataFlow<Tuple<string,Dictionary<string,string>>>] --> (DataFlowConnector<Tuple<string,Dictionary<string,string>>> (newlyCreatedAbstractionTemplate).dataInput)
            id_b844b78ad0e54ca8b3ea6eb6bb831c2e.WireTo(id_48f2e14b737742969006e5a067fde8da, "createDomainAbstraction"); // (NewAbstractionTemplateTab (id_b844b78ad0e54ca8b3ea6eb6bb831c2e).createDomainAbstraction) -- [IEvent] --> (CreateAbstraction (id_48f2e14b737742969006e5a067fde8da).create)
            id_b844b78ad0e54ca8b3ea6eb6bb831c2e.WireTo(id_ba5fd8dd21b747cbb764d0c37b1ccd40, "createStoryAbstraction"); // (NewAbstractionTemplateTab (id_b844b78ad0e54ca8b3ea6eb6bb831c2e).createStoryAbstraction) -- [IEvent] --> (EventConnector (id_ba5fd8dd21b747cbb764d0c37b1ccd40).NEEDNAME)
            newlyCreatedAbstractionTemplate.WireTo(id_94bc4bb4b0eb4992b073d93e6e62df61, "fanoutList"); // (DataFlowConnector<Tuple<string,Dictionary<string,string>>> (newlyCreatedAbstractionTemplate).fanoutList) -- [IDataFlow<Tuple<string,Dictionary<string,string>>>] --> (Apply<Tuple<string,Dictionary<string,string>>,string> (id_94bc4bb4b0eb4992b073d93e6e62df61).input)
            newlyCreatedAbstractionTemplate.WireTo(id_a6b69c879e5c4be3b0b65df90c5f2aa3, "fanoutList"); // (DataFlowConnector<Tuple<string,Dictionary<string,string>>> (newlyCreatedAbstractionTemplate).fanoutList) -- [IDataFlow<Tuple<string,Dictionary<string,string>>>] --> (Apply<Tuple<string,Dictionary<string,string>>,List<string>> (id_a6b69c879e5c4be3b0b65df90c5f2aa3).input)
            newlyCreatedAbstractionTemplate.WireTo(id_0dc66c5de4cc4f639a4f8a4e68ca8a32, "fanoutList"); // (DataFlowConnector<Tuple<string,Dictionary<string,string>>> (newlyCreatedAbstractionTemplate).fanoutList) -- [IDataFlow<Tuple<string,Dictionary<string,string>>>] --> (Apply<Tuple<string,Dictionary<string,string>>,List<string>> (id_0dc66c5de4cc4f639a4f8a4e68ca8a32).input)
            newlyCreatedAbstractionTemplate.WireTo(id_e04a847325bc4703b8de92a10b2e75ed, "fanoutList"); // (DataFlowConnector<Tuple<string,Dictionary<string,string>>> (newlyCreatedAbstractionTemplate).fanoutList) -- [IDataFlow<Tuple<string,Dictionary<string,string>>>] --> (LookupTable<string,Dictionary<string,string>> (id_e04a847325bc4703b8de92a10b2e75ed).pairInput)
            id_94bc4bb4b0eb4992b073d93e6e62df61.WireTo(id_821a0f8e8c8149608e068ef40bd35d5c, "output"); // (Apply<Tuple<string,Dictionary<string,string>>,string> (id_94bc4bb4b0eb4992b073d93e6e62df61).output) -- [IDataFlow<string>] --> (DataFlowConnector<string> (id_821a0f8e8c8149608e068ef40bd35d5c).dataInput)
            id_a6b69c879e5c4be3b0b65df90c5f2aa3.WireTo(id_fc9f7e6aa6f74a25ba5b80e7adfe9ab2, "output"); // (Apply<Tuple<string,Dictionary<string,string>>,List<string>> (id_a6b69c879e5c4be3b0b65df90c5f2aa3).output) -- [IDataFlow<List<string>>] --> (DataFlowConnector<List<string>> (id_fc9f7e6aa6f74a25ba5b80e7adfe9ab2).dataInput)
            id_0dc66c5de4cc4f639a4f8a4e68ca8a32.WireTo(id_80fbb5a0a57a477f8cff681489fb2a1d, "output"); // (Apply<Tuple<string,Dictionary<string,string>>,List<string>> (id_0dc66c5de4cc4f639a4f8a4e68ca8a32).output) -- [IDataFlow<List<string>>] --> (DataFlowConnector<List<string>> (id_80fbb5a0a57a477f8cff681489fb2a1d).dataInput)
            id_48f2e14b737742969006e5a067fde8da.WireTo(id_821a0f8e8c8149608e068ef40bd35d5c, "classNameInput"); // (CreateAbstraction (id_48f2e14b737742969006e5a067fde8da).classNameInput) -- [IDataFlowB<string>] --> (DataFlowConnector<string> (id_821a0f8e8c8149608e068ef40bd35d5c).NEEDNAME)
            id_48f2e14b737742969006e5a067fde8da.WireTo(id_fc9f7e6aa6f74a25ba5b80e7adfe9ab2, "implementedPortsInput"); // (CreateAbstraction (id_48f2e14b737742969006e5a067fde8da).implementedPortsInput) -- [IDataFlowB<List<string>>] --> (DataFlowConnector<List<string>> (id_fc9f7e6aa6f74a25ba5b80e7adfe9ab2).NEEDNAME)
            id_48f2e14b737742969006e5a067fde8da.WireTo(id_80fbb5a0a57a477f8cff681489fb2a1d, "providedPortsInput"); // (CreateAbstraction (id_48f2e14b737742969006e5a067fde8da).providedPortsInput) -- [IDataFlowB<List<string>>] --> (DataFlowConnector<List<string>> (id_80fbb5a0a57a477f8cff681489fb2a1d).NEEDNAME)
            id_ba5fd8dd21b747cbb764d0c37b1ccd40.WireTo(id_d6a4e708014943de868c7290bd160681, "fanoutList"); // (EventConnector (id_ba5fd8dd21b747cbb764d0c37b1ccd40).fanoutList) -- [IEvent] --> (Data<Dictionary<string,string>> (id_d6a4e708014943de868c7290bd160681).start)
            id_ba5fd8dd21b747cbb764d0c37b1ccd40.WireTo(id_7839777c9d57437f84412e1b498e1427, "complete"); // (EventConnector (id_ba5fd8dd21b747cbb764d0c37b1ccd40).complete) -- [IEvent] --> (CreateAbstraction (id_7839777c9d57437f84412e1b498e1427).create)
            id_7839777c9d57437f84412e1b498e1427.WireTo(id_821a0f8e8c8149608e068ef40bd35d5c, "classNameInput"); // (CreateAbstraction (id_7839777c9d57437f84412e1b498e1427).classNameInput) -- [IDataFlowB<string>] --> (DataFlowConnector<string> (id_821a0f8e8c8149608e068ef40bd35d5c).NEEDNAME)
            id_7839777c9d57437f84412e1b498e1427.WireTo(id_fc9f7e6aa6f74a25ba5b80e7adfe9ab2, "implementedPortsInput"); // (CreateAbstraction (id_7839777c9d57437f84412e1b498e1427).implementedPortsInput) -- [IDataFlowB<List<string>>] --> (DataFlowConnector<List<string>> (id_fc9f7e6aa6f74a25ba5b80e7adfe9ab2).NEEDNAME)
            id_7839777c9d57437f84412e1b498e1427.WireTo(id_80fbb5a0a57a477f8cff681489fb2a1d, "providedPortsInput"); // (CreateAbstraction (id_7839777c9d57437f84412e1b498e1427).providedPortsInput) -- [IDataFlowB<List<string>>] --> (DataFlowConnector<List<string>> (id_80fbb5a0a57a477f8cff681489fb2a1d).NEEDNAME)
            initialiseApp.WireTo(id_725d235d635a4146a1742630192b5698, "fanoutList"); // (EventConnector (initialiseApp).fanoutList) -- [IEvent] --> (EventConnector (id_725d235d635a4146a1742630192b5698).NEEDNAME)
            initialiseApp.WireTo(id_2ac88ef7fe0b47bbba634a8a45702a81, "fanoutList"); // (EventConnector (initialiseApp).fanoutList) -- [IEvent] --> (GetSetting (id_2ac88ef7fe0b47bbba634a8a45702a81).start)
            initialiseApp.WireTo(id_e70bef52836e40a2a17c333a392b0548, "fanoutList"); // (EventConnector (initialiseApp).fanoutList) -- [IEvent] --> (GetSetting (id_e70bef52836e40a2a17c333a392b0548).start)
            id_725d235d635a4146a1742630192b5698.WireTo(setDomainAbstractionTemplatesFileLocation, "fanoutList"); // (EventConnector (id_725d235d635a4146a1742630192b5698).fanoutList) -- [IEvent] --> (Data<string> (setDomainAbstractionTemplatesFileLocation).start)
            setDomainAbstractionTemplatesFileLocation.WireTo(id_f034fbe6dce3465cbe6edada65c8d263, "dataOutput"); // (Data<string> (setDomainAbstractionTemplatesFileLocation).dataOutput) -- [IDataFlow<string>] --> (FileReader (id_f034fbe6dce3465cbe6edada65c8d263).filePathInput)
            id_f034fbe6dce3465cbe6edada65c8d263.WireTo(getTemplatesFromJSON, "fileContentOutput"); // (FileReader (id_f034fbe6dce3465cbe6edada65c8d263).fileContentOutput) -- [IDataFlow<string>] --> (Apply<string,Dictionary<string,JToken>> (getTemplatesFromJSON).input)
            getTemplatesFromJSON.WireTo(abstractionTemplatesConnector, "output"); // (Apply<string,Dictionary<string,JToken>> (getTemplatesFromJSON).output) -- [IDataFlow<Dictionary<string,JToken>>] --> (@DataFlowConnector<Dictionary<string,JToken>> (abstractionTemplatesConnector).dataInput)
            abstractionTemplatesConnector.WireTo(id_eb63936eb29948f9bef6b22f54b7abcd, "fanoutList"); // (@DataFlowConnector<Dictionary<string,JToken>> (abstractionTemplatesConnector).fanoutList) -- [IDataFlow<Dictionary<string,JToken>>] --> (Apply<Dictionary<string,JToken>,List<string>> (id_eb63936eb29948f9bef6b22f54b7abcd).input)
            id_eb63936eb29948f9bef6b22f54b7abcd.WireTo(abstractionTemplateTypes, "output"); // (Apply<Dictionary<string,JToken>,List<string>> (id_eb63936eb29948f9bef6b22f54b7abcd).output) -- [IDataFlow<List<string>>] --> (DataFlowConnector<List<string>> (abstractionTemplateTypes).dataInput)
            abstractionTemplateTypes.WireTo(id_47e785e85e8f4706a2cd54676ddeee07, "fanoutList"); // (DataFlowConnector<List<string>> (abstractionTemplateTypes).fanoutList) -- [IDataFlow<List<string>>] --> (ApplyAction<List<string>> (id_47e785e85e8f4706a2cd54676ddeee07).input)
            id_2ac88ef7fe0b47bbba634a8a45702a81.WireTo(settingsFilePath, "filePathInput"); // (GetSetting (id_2ac88ef7fe0b47bbba634a8a45702a81).filePathInput) -- [IDataFlowB<string>] --> (DataFlowConnector<string> (settingsFilePath).returnDataB)
            id_2ac88ef7fe0b47bbba634a8a45702a81.WireTo(id_411e043c0dc6455cbd5c8b5a5aaa4408, "settingJsonOutput"); // (GetSetting (id_2ac88ef7fe0b47bbba634a8a45702a81).settingJsonOutput) -- [IDataFlow<string>] --> (DataFlowConnector<string> (id_411e043c0dc6455cbd5c8b5a5aaa4408).dataInput)
            id_411e043c0dc6455cbd5c8b5a5aaa4408.WireTo(id_9a2dda5813924951b284fc2b89e6b4bf, "fanoutList"); // (DataFlowConnector<string> (id_411e043c0dc6455cbd5c8b5a5aaa4408).fanoutList) -- [IDataFlow<string>] --> (TextBox (id_9a2dda5813924951b284fc2b89e6b4bf).NEEDNAME)
            id_411e043c0dc6455cbd5c8b5a5aaa4408.WireTo(id_911eb68dba704c12bd5afcf7eba7a357, "fanoutList"); // (DataFlowConnector<string> (id_411e043c0dc6455cbd5c8b5a5aaa4408).fanoutList) -- [IDataFlow<string>] --> (DataFlowConnector<string> (id_911eb68dba704c12bd5afcf7eba7a357).dataInput)
            id_e70bef52836e40a2a17c333a392b0548.WireTo(settingsFilePath, "filePathInput"); // (GetSetting (id_e70bef52836e40a2a17c333a392b0548).filePathInput) -- [IDataFlowB<string>] --> (@DataFlowConnector<string> (settingsFilePath).returnDataB)
            id_e70bef52836e40a2a17c333a392b0548.WireTo(id_f92ce910e32c48928f518addb5063e8d, "settingJsonOutput"); // (GetSetting (id_e70bef52836e40a2a17c333a392b0548).settingJsonOutput) -- [IDataFlow<string>] --> (DataFlowConnector<string> (id_f92ce910e32c48928f518addb5063e8d).dataInput)
            id_f92ce910e32c48928f518addb5063e8d.WireTo(id_1fbd51c85a7a4f5d9861b9059f95d9bc, "fanoutList"); // (DataFlowConnector<string> (id_f92ce910e32c48928f518addb5063e8d).fanoutList) -- [IDataFlow<string>] --> (DataFlowConnector<string> (id_1fbd51c85a7a4f5d9861b9059f95d9bc).dataInput)
            id_f92ce910e32c48928f518addb5063e8d.WireTo(id_8de11133b1074cfe915e847951e130a8, "fanoutList"); // (DataFlowConnector<string> (id_f92ce910e32c48928f518addb5063e8d).fanoutList) -- [IDataFlow<string>] --> (TextBox (id_8de11133b1074cfe915e847951e130a8).textInput)
            // END AUTO-GENERATED WIRING FOR Application.xmind

        }
    }
}






































