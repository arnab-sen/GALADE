using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;
using RequirementsAbstractions;
using WPFCanvas = System.Windows.Controls.Canvas;
using System.IO;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;
using Application;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TestApplication
{
    /// <summary>
    /// This version of GALADE is standalone, i.e. it is a single executable.
    /// </summary>
    public class Application
    {
        // Public fields and properties

        // Private fields
        private MainWindow mainWindow = new MainWindow("GALADE");

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
                Ports = new List<Port>
                {
                    new Port() { Type = "Port", Name = "p0", IsInputPort = true },
                    new Port() { Type = "Port", Name = "p1", IsInputPort = false }
                }
            };

            newNode.ActionPerformed += undoHistory.Push;
            newNode.Initialise();

            newNode.ContextMenu = (new VPGNContextMenu() as IUI).GetWPFElement();

            if (graph.GetRoot() == null)
            {
                graph.AddNode(newNode);
            }
        }

        private void Test(object o)
        {
        }

        private void AddEdge(Graph graph, ALANode A, ALANode B, Port sourcePort = null, Port destinationPort = null)
        {

        }

        [STAThread]
        public static void Main()
        {
            Application app = new Application();
            app.Initialize().mainWindow.Run();
        }

        private Application()
        {
            // object node = new VisualPortGraphNode() { Type = "TestType", Name = "TestName" };
            // var id = node.GetHashCode();
            //
            // List<object> objs = new List<object>() {true, "str"};
            // var a = objs.FirstOrDefault(obj => obj is bool b && b == true);
            // var c = objs.FirstOrDefault(obj => obj is string s && s == "str");

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


            StateTransition<Enums.DiagramMode> stateTransition = new StateTransition<Enums.DiagramMode>(Enums.DiagramMode.Idle)
            {
                InstanceName = "stateTransition",
                Matches = (flag, currentState) => (flag & currentState) != 0
            };

            UndoHistory undoHistory = new UndoHistory() { InstanceName = "graphHistory" };

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
                // var diagramContents = mainGraph.Serialise();
                // File.WriteAllText(System.IO.Path.Combine(BACKUPS_DIRECTORY, $"{Utilities.GetCurrentTime()}.ala"), diagramContents);
            };
            #endregion

            Graph mainGraph = new Graph();

            WPFCanvas mainCanvas = null;
            AbstractionModelManager abstractionModelManager = new AbstractionModelManager();

            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR Application.xmind
            Vertical id_877db254da594e06a4760973586fc268 = new Vertical() {  };
            CanvasDisplay id_a660f1c7813542e993fc1f4d5a84a94a = new CanvasDisplay() { Width = 1920, Height = 600, Background = Brushes.White, StateTransition = stateTransition };
            ApplyAction<System.Windows.Controls.Canvas> id_f46dc425357749bc9ad36df185041529 = new ApplyAction<System.Windows.Controls.Canvas>() { Lambda = canvas => mainCanvas = canvas };
            KeyEvent id_fd1b6826eaee46398deca749b0fb7b3f = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.A }, Condition = args => mainGraph.Get("SelectedNode") != null&& stateTransition.CurrentStateMatches(Enums.DiagramMode.IdleSelected) };
            Data<object> id_f20e87b1c8f44c38bc2a5402a5acc2a9 = new Data<object>() { Lambda = () => {var node = new ALANode();node.Model = abstractionModelManager.GetAbstractionModel(abstractionModelManager.GetAbstractionTypes().FirstOrDefault());node.Graph = mainGraph;node.Canvas = mainCanvas;node.StateTransition = stateTransition;node.AvailableDomainAbstractions.AddRange( abstractionModelManager.GetAbstractionTypes());node.TypeChanged += newType => {abstractionModelManager.UpdateAbstractionModel(abstractionModelManager.GetAbstractionModel(newType),node.Model);node.UpdateUI();Dispatcher.CurrentDispatcher.Invoke(() => {var edges = mainGraph.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);};mainGraph.AddNode(node);mainGraph.Roots.Add(node);node.CreateInternals();mainCanvas.Children.Add(node.Render);return node;} };
            ApplyAction<object> initialiseNode = new ApplyAction<object>() { InstanceName = "initialiseNode", Lambda = input =>{var render = (input as ALANode).Render;var mousePos = Mouse.GetPosition(mainCanvas);WPFCanvas.SetLeft(render, mousePos.X);WPFCanvas.SetTop(render, mousePos.Y);mainGraph.Set("LatestNode", input);if (mainGraph.Get("SelectedNode") == null){mainGraph.Set("SelectedNode", input);}} };
            ContextMenu id_05e8bd791663499987114591b8a91492 = new ContextMenu() {  };
            MenuItem id_122bf3b189504866835709afba68aa45 = new MenuItem(header: "Add root") {  };
            EventConnector id_a4374a473b77457980ad823a62a5cafc = new EventConnector() {  };
            Data<ALANode> getFirstRoot = new Data<ALANode>() { InstanceName = "getFirstRoot", Lambda = () => mainGraph.Roots.First() as ALANode };
            RightTreeLayout<ALANode> id_b7b186c20c3c45a6a303ec6e2ea484bd = new RightTreeLayout<ALANode>() { GetID = n => n.Id, GetWidth = n => (n.Render as FrameworkElement).ActualWidth, GetHeight = n => (n.Render as FrameworkElement).ActualHeight, SetX = (n, x) => WPFCanvas.SetLeft(n.Render, x), SetY = (n, y) => WPFCanvas.SetTop(n.Render, y), GetChildren = n => mainGraph.Edges.Where(e => e is ALAWire wire && wire.Source != null && wire.Destination != null && wire.Source == n).Select(e => ((e as ALAWire).Destination) as ALANode), HorizontalGap = 100, VerticalGap = 20, InitialX = 50, InitialY = 50 };
            EventConnector layoutDiagram = new EventConnector() { InstanceName = "layoutDiagram" };
            DataFlowConnector<ALANode> id_812a5e03234c4de8a43db8d1cea06951 = new DataFlowConnector<ALANode>() {  };
            ApplyAction<ALANode> id_41d38f8c034d4e8980e47f9ec110363c = new ApplyAction<ALANode>() { Lambda = node =>{Dispatcher.CurrentDispatcher.Invoke(() => {var edges = mainGraph.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);} };
            KeyEvent id_bdeed944351e4a7fabc736472d6c0ebc = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.R }, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            Apply<AbstractionModel, object> id_5947be2cb9c94b2cbf0bd121e05f9277 = new Apply<AbstractionModel, object>() { Lambda = input => {var node = new ALANode();node.Model = input;node.Graph = mainGraph;node.Canvas = mainCanvas;node.StateTransition = stateTransition;node.AvailableDomainAbstractions.AddRange( abstractionModelManager.GetAbstractionTypes());node.TypeChanged += newType => {abstractionModelManager.UpdateAbstractionModel(abstractionModelManager.GetAbstractionModel(newType),node.Model);node.UpdateUI();Dispatcher.CurrentDispatcher.Invoke(() => {var edges = mainGraph.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);};mainGraph.AddNode(node);node.CreateInternals();mainCanvas.Children.Add(node.Render);return node;} };
            MenuBar id_398d4e3b3f034387998f614919f4dac1 = new MenuBar() {  };
            MenuItem id_579c916ac2c54e12be1f88c7370e1e42 = new MenuItem(header: "File") {  };
            MenuItem id_afc1b052622c4f2a833df89963795151 = new MenuItem(header: "Open Project") {  };
            FolderBrowser id_6748b68cfc334616a0e307e20d44e969 = new FolderBrowser() { Description = "" };
            DirectorySearch id_94ad39719ba244d8bb999e535ab760e7 = new DirectorySearch(directoriesToFind: new string[] { "DomainAbstractions" }) { FilenameFilter = "*.cs" };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_e7eee8e1173e4a9aa8a2c07e3912f3d5 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("DomainAbstractions")){list = input["DomainAbstractions"];}return list;} };
            ForEach<string> id_acf85474e49f4096b5e30f5b5fe3b88f = new ForEach<string>() {  };
            ApplyAction<string> id_12f3c7ced1e44879830c345249eb8f1c = new ApplyAction<string>() { Lambda = input =>{abstractionModelManager.CreateAbstractionModelFromPath(input);} };
            Data<string> id_88cea85867a7472eb3377371ff326c44 = new Data<string>() { storedData = "Apply<T1, T2>" };
            Apply<string, AbstractionModel> id_561b685a66dc4e0ea764f7d1e8155eb0 = new Apply<string, AbstractionModel>() { Lambda = input =>{return abstractionModelManager.GetAbstractionModel(input);} };
            Data<string> id_97e6242abda84736afba96663a658233 = new Data<string>() { storedData = @"F:\Projects\GALADE\ALACore" };
            DropDownMenu id_e26600bdb6f24a668787d9f97ceb3d62 = new DropDownMenu() { Items = new string[100] };
            KeyEvent id_fac73b3025124d8bb5d257172cc32a8f = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.Enter } };
            ApplyAction<object> id_889a726ef5c048b594ca58d5ffeccba9 = new ApplyAction<object>() { Lambda = input =>{(input as WPFCanvas).Focus();} };
            MenuItem id_5545c5139d344f86b33f6691682b8f9b = new MenuItem(header: "Debug") {  };
            MenuItem id_60cebfa02d71448fa8ae77c741cb0ae2 = new MenuItem(header: "TextEditor test") {  };
            PopupWindow id_4bbd69fc4eea466db19aab6bfaeaeddd = new PopupWindow(title: "") { Height = 720, Width = 1280, Resize = SizeToContent.WidthAndHeight };
            Box id_254ef684b0ee41318f61109bc1a9aa54 = new Box() { Width = 100, Height = 100 };
            TextEditor id_a1eeb453915944488e3fe9150a718b6f = new TextEditor() { Width = 1280, Height = 720 };
            DataFlowConnector<string> id_b9390f64e44a4e89af5c42289211bed3 = new DataFlowConnector<string>() {  };
            FolderWatcher projectFolderWatcher = new FolderWatcher() { InstanceName = "projectFolderWatcher", RootPath = "", Filter = "*.cs", WatchSubdirectories = true, PathRegex = @".*\.cs$" };
            Apply<string, object> id_7c3f429a14734f008a016e38f1a9ecfb = new Apply<string, object>() { Lambda = input =>{var newModel = abstractionModelManager.CreateAbstractionModelFromPath(input);foreach (var node in mainGraph.Nodes){var alaNode = node as ALANode;if (alaNode.Model.Type != newModel.Type) continue;abstractionModelManager.UpdateAbstractionModel(newModel, alaNode.Model);alaNode.UpdateUI();}return input;} };
            ConvertToEvent<object> id_f4928f93d1e74100a9e36fb4311a9535 = new ConvertToEvent<object>() {  };
            MouseButtonEvent id_da6ad3e3d78745d099a48e06a3d9f568 = new MouseButtonEvent(eventName: "MouseRightButtonDown") { ExtractSender = null, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            ApplyAction<object> id_eccee435c0fb47cd9843d27586cb3db3 = new ApplyAction<object>() { Lambda = input =>{Mouse.Capture(input as WPFCanvas);stateTransition.Update(Enums.DiagramMode.Idle);} };
            MouseButtonEvent id_70b6c522c50c4872bb13f4c0f8f36db7 = new MouseButtonEvent(eventName: "MouseRightButtonUp") { ExtractSender = null, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            ApplyAction<object> id_93336b3f8ebc45179d7b167bf8f5dc37 = new ApplyAction<object>() { Lambda = input =>{if (Mouse.Captured?.Equals(input) ?? false) Mouse.Capture(null);stateTransition.Update(Enums.DiagramMode.Idle);} };
            StateChangeListener id_fda18ec284e54094b3d1dac66f04ebe3 = new StateChangeListener() { StateTransition = stateTransition, CurrentStateShouldMatch = Enums.DiagramMode.All };
            Apply<Tuple<Enums.DiagramMode, Enums.DiagramMode>, bool> id_1f722cbd2ff84ec098d68611478d4a6f = new Apply<Tuple<Enums.DiagramMode, Enums.DiagramMode>, bool>() { Lambda = input =>{return input.Item1 == Enums.DiagramMode.AwaitingPortSelection &&input.Item2 == Enums.DiagramMode.Idle;} };
            IfElse id_e67db93257f34fc5a5176f0d28fcc87e = new IfElse() {  };
            EventConnector id_a6a14d0cd09741f0b255742e40606882 = new EventConnector() {  };
            Apply<object, object> createAndPaintALAWire = new Apply<object, object>() { InstanceName = "createAndPaintALAWire", Lambda = input =>{var source = mainGraph.Get("SelectedNode") as ALANode;var destination = input as ALANode;var sourcePort = source.GetSelectedPort(inputPort: false);var destinationPort = destination.GetSelectedPort(inputPort: true);var wire = new ALAWire(){Graph = mainGraph,Canvas = mainCanvas,Source = source,Destination = destination,SourcePort = sourcePort,DestinationPort = destinationPort,StateTransition = stateTransition};mainGraph.AddEdge(wire);wire.Paint();return wire;} };
            KeyEvent id_762a939664b2476a9cd2ea3b26df5b57 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.Delete } };
            EventLambda id_9016f22c92084db68faad7fb929b833e = new EventLambda() { Lambda = () =>{var selectedNode = mainGraph.Get("SelectedNode") as ALANode;if (selectedNode == null) return;selectedNode.Delete(deleteAttachedWires: true);} };
            MenuItem id_df0e6ba4b97f40929909adb55e10d99e = new MenuItem(header: "Refresh") {  };
            // END AUTO-GENERATED INSTANTIATIONS FOR Application.xmind

            // BEGIN AUTO-GENERATED WIRING FOR Application.xmind
            mainWindow.WireTo(id_877db254da594e06a4760973586fc268, "iuiStructure");
            mainWindow.WireTo(id_a6a14d0cd09741f0b255742e40606882, "appStart");
            id_877db254da594e06a4760973586fc268.WireTo(id_398d4e3b3f034387998f614919f4dac1, "children");
            id_877db254da594e06a4760973586fc268.WireTo(id_a660f1c7813542e993fc1f4d5a84a94a, "children");
            id_a660f1c7813542e993fc1f4d5a84a94a.WireTo(id_f46dc425357749bc9ad36df185041529, "canvasOutput");
            id_a660f1c7813542e993fc1f4d5a84a94a.WireTo(id_fd1b6826eaee46398deca749b0fb7b3f, "eventHandlers");
            id_a660f1c7813542e993fc1f4d5a84a94a.WireTo(id_bdeed944351e4a7fabc736472d6c0ebc, "eventHandlers");
            id_a660f1c7813542e993fc1f4d5a84a94a.WireTo(id_fac73b3025124d8bb5d257172cc32a8f, "eventHandlers");
            id_a660f1c7813542e993fc1f4d5a84a94a.WireTo(id_da6ad3e3d78745d099a48e06a3d9f568, "eventHandlers");
            id_a660f1c7813542e993fc1f4d5a84a94a.WireTo(id_70b6c522c50c4872bb13f4c0f8f36db7, "eventHandlers");
            id_a660f1c7813542e993fc1f4d5a84a94a.WireTo(id_762a939664b2476a9cd2ea3b26df5b57, "eventHandlers");
            id_a660f1c7813542e993fc1f4d5a84a94a.WireTo(id_05e8bd791663499987114591b8a91492, "contextMenu");
            id_fd1b6826eaee46398deca749b0fb7b3f.WireTo(id_a4374a473b77457980ad823a62a5cafc, "eventHappened");
            id_f20e87b1c8f44c38bc2a5402a5acc2a9.WireTo(initialiseNode, "dataOutput");
            id_05e8bd791663499987114591b8a91492.WireTo(id_122bf3b189504866835709afba68aa45, "children");
            id_05e8bd791663499987114591b8a91492.WireTo(id_df0e6ba4b97f40929909adb55e10d99e, "children");
            id_122bf3b189504866835709afba68aa45.WireTo(id_f20e87b1c8f44c38bc2a5402a5acc2a9, "clickedEvent");
            id_a4374a473b77457980ad823a62a5cafc.WireTo(id_88cea85867a7472eb3377371ff326c44, "fanoutList");
            id_a4374a473b77457980ad823a62a5cafc.WireTo(layoutDiagram, "complete");
            getFirstRoot.WireTo(id_812a5e03234c4de8a43db8d1cea06951, "dataOutput");
            id_df0e6ba4b97f40929909adb55e10d99e.WireTo(layoutDiagram, "clickedEvent");
            layoutDiagram.WireTo(getFirstRoot, "fanoutList");
            id_812a5e03234c4de8a43db8d1cea06951.WireTo(id_b7b186c20c3c45a6a303ec6e2ea484bd, "fanoutList");
            id_812a5e03234c4de8a43db8d1cea06951.WireTo(id_41d38f8c034d4e8980e47f9ec110363c, "fanoutList");
            id_bdeed944351e4a7fabc736472d6c0ebc.WireTo(layoutDiagram, "eventHappened");
            id_5947be2cb9c94b2cbf0bd121e05f9277.WireTo(createAndPaintALAWire, "output");
            id_398d4e3b3f034387998f614919f4dac1.WireTo(id_579c916ac2c54e12be1f88c7370e1e42, "children");
            id_398d4e3b3f034387998f614919f4dac1.WireTo(id_5545c5139d344f86b33f6691682b8f9b, "children");
            id_579c916ac2c54e12be1f88c7370e1e42.WireTo(id_afc1b052622c4f2a833df89963795151, "children");
            id_579c916ac2c54e12be1f88c7370e1e42.WireTo(id_e26600bdb6f24a668787d9f97ceb3d62, "children");
            id_afc1b052622c4f2a833df89963795151.WireTo(id_6748b68cfc334616a0e307e20d44e969, "clickedEvent");
            id_6748b68cfc334616a0e307e20d44e969.WireTo(id_b9390f64e44a4e89af5c42289211bed3, "selectedFolderPathOutput");
            id_94ad39719ba244d8bb999e535ab760e7.WireTo(id_e7eee8e1173e4a9aa8a2c07e3912f3d5, "foundFiles");
            id_e7eee8e1173e4a9aa8a2c07e3912f3d5.WireTo(id_acf85474e49f4096b5e30f5b5fe3b88f, "output");
            id_acf85474e49f4096b5e30f5b5fe3b88f.WireTo(id_12f3c7ced1e44879830c345249eb8f1c, "elementOutput");
            id_88cea85867a7472eb3377371ff326c44.WireTo(id_561b685a66dc4e0ea764f7d1e8155eb0, "dataOutput");
            id_561b685a66dc4e0ea764f7d1e8155eb0.WireTo(id_5947be2cb9c94b2cbf0bd121e05f9277, "output");
            id_97e6242abda84736afba96663a658233.WireTo(id_b9390f64e44a4e89af5c42289211bed3, "dataOutput");
            id_fac73b3025124d8bb5d257172cc32a8f.WireTo(id_889a726ef5c048b594ca58d5ffeccba9, "senderOutput");
            id_5545c5139d344f86b33f6691682b8f9b.WireTo(id_60cebfa02d71448fa8ae77c741cb0ae2, "children");
            id_60cebfa02d71448fa8ae77c741cb0ae2.WireTo(id_4bbd69fc4eea466db19aab6bfaeaeddd, "clickedEvent");
            id_4bbd69fc4eea466db19aab6bfaeaeddd.WireTo(id_254ef684b0ee41318f61109bc1a9aa54, "children");
            id_254ef684b0ee41318f61109bc1a9aa54.WireTo(id_a1eeb453915944488e3fe9150a718b6f, "uiLayout");
            id_b9390f64e44a4e89af5c42289211bed3.WireTo(id_94ad39719ba244d8bb999e535ab760e7, "fanoutList");
            id_b9390f64e44a4e89af5c42289211bed3.WireTo(projectFolderWatcher, "fanoutList");
            projectFolderWatcher.WireTo(id_7c3f429a14734f008a016e38f1a9ecfb, "changedFile");
            id_7c3f429a14734f008a016e38f1a9ecfb.WireTo(id_f4928f93d1e74100a9e36fb4311a9535, "output");
            id_f4928f93d1e74100a9e36fb4311a9535.WireTo(id_41d38f8c034d4e8980e47f9ec110363c, "eventOutput");
            id_da6ad3e3d78745d099a48e06a3d9f568.WireTo(id_eccee435c0fb47cd9843d27586cb3db3, "argsOutput");
            id_70b6c522c50c4872bb13f4c0f8f36db7.WireTo(id_93336b3f8ebc45179d7b167bf8f5dc37, "argsOutput");
            id_fda18ec284e54094b3d1dac66f04ebe3.WireTo(id_1f722cbd2ff84ec098d68611478d4a6f, "transitionOutput");
            id_1f722cbd2ff84ec098d68611478d4a6f.WireTo(id_e67db93257f34fc5a5176f0d28fcc87e, "output");
            id_e67db93257f34fc5a5176f0d28fcc87e.WireTo(layoutDiagram, "ifOutput");
            id_a6a14d0cd09741f0b255742e40606882.WireTo(id_97e6242abda84736afba96663a658233, "fanoutList");
            id_a6a14d0cd09741f0b255742e40606882.WireTo(id_fda18ec284e54094b3d1dac66f04ebe3, "fanoutList");
            id_762a939664b2476a9cd2ea3b26df5b57.WireTo(id_9016f22c92084db68faad7fb929b833e, "eventHappened");
            // END AUTO-GENERATED WIRING FOR Application.xmind

            // BEGIN MANUAL INSTANTIATIONS
            // var AMM = new AbstractionModelManager();
            // // AMM.OpenFile();
            // var code = File.ReadAllText(
            //     // "F:\\Projects\\GALADE\\ALACore\\DomainAbstractions\\CodeParser.cs");
            //     "D:\\Coding\\C#\\Projects\\GALADE\\ALACore\\DomainAbstractions\\ExampleDomainAbstraction.cs");
            // var model = AMM.CreateAbstractionModel(code);
            // END MANUAL INSTANTIATIONS

            // BEGIN MANUAL WIRING
            // END MANUAL WIRING

        }
    }
}








































































































