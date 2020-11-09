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
        private MainWindow _mainWindow = null;

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
            app.Initialize()._mainWindow.Run();
        }

        private void CreateWiring()
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

            WPFCanvas mainCanvas = new WPFCanvas();
            AbstractionModelManager abstractionModelManager = new AbstractionModelManager();

            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR Application.xmind
            MainWindow mainWindow = new MainWindow(title: "GALADE") { InstanceName = "mainWindow" };
            Vertical id_a492431fd74544f8875704b800186f7e = new Vertical() {  };
            CanvasDisplay id_7cc52c9442f64bf7b4494cb9797a12e4 = new CanvasDisplay() { Width = 1920, Height = 600, Background = Brushes.White, StateTransition = stateTransition, Canvas = mainCanvas };
            KeyEvent id_4fb4ea9344c8474284773e2c780cd248 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.A }, Condition = args => mainGraph.Get("SelectedNode") != null&& stateTransition.CurrentStateMatches(Enums.DiagramMode.IdleSelected) };
            ContextMenu id_ad1264752d354cbe88641af4a9b5da5b = new ContextMenu() {  };
            MenuItem id_ca9db8e8e8434659b43931e7b76b6324 = new MenuItem(header: "Add root") {  };
            EventConnector id_e5653fa0b2b74847b953499359682f53 = new EventConnector() {  };
            Data<ALANode> getFirstRoot = new Data<ALANode>() { InstanceName = "getFirstRoot", Lambda = () => mainGraph.Roots.FirstOrDefault() as ALANode };
            RightTreeLayout<ALANode> id_ae376a5905ea425099e3a35dc1f19e95 = new RightTreeLayout<ALANode>() { GetID = n => n.Id, GetWidth = n => n.Width, GetHeight = n => n.Height, SetX = (n, x) => n.PositionX = x, SetY = (n, y) => n.PositionY = y, GetChildren = n => mainGraph.Edges.Where(e => e is ALAWire wire && wire.Source != null && wire.Destination != null && wire.Source == n).Select(e => ((e as ALAWire).Destination) as ALANode), HorizontalGap = 100, VerticalGap = 20, InitialX = 50, InitialY = 50 };
            EventConnector layoutDiagram = new EventConnector() { InstanceName = "layoutDiagram" };
            DataFlowConnector<ALANode> id_9bed4bb4cc08495d8e509c5c030a7829 = new DataFlowConnector<ALANode>() {  };
            ApplyAction<ALANode> id_a904222e05504395bdce0ca3faa0bb5f = new ApplyAction<ALANode>() { Lambda = node =>{Dispatcher.CurrentDispatcher.Invoke(() => {var edges = mainGraph.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);} };
            KeyEvent id_3f3ef7db83ec4c77830a3e20364433df = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.R }, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            Apply<AbstractionModel, object> createNewALANode = new Apply<AbstractionModel, object>() { InstanceName = "createNewALANode", Lambda = input => {var node = new ALANode();node.Model = input;node.Graph = mainGraph;node.Canvas = mainCanvas;node.StateTransition = stateTransition;node.AvailableDomainAbstractions.AddRange( abstractionModelManager.GetAbstractionTypes());node.TypeChanged += newType => {abstractionModelManager.UpdateAbstractionModel(abstractionModelManager.GetAbstractionModel(newType),node.Model);node.UpdateUI();Dispatcher.CurrentDispatcher.Invoke(() => {var edges = mainGraph.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);};mainGraph.AddNode(node);node.CreateInternals();mainCanvas.Children.Add(node.Render);return node;} };
            MenuBar id_e82a887f0141403e9250c85f9479fff4 = new MenuBar() {  };
            MenuItem id_3123efab2edc4bebafb6f37f35bbf012 = new MenuItem(header: "File") {  };
            MenuItem id_50eff2838e144c0fb57729f022ec3b08 = new MenuItem(header: "Open Project") {  };
            FolderBrowser id_cda28e9c3924418dafb98e229b5e28f9 = new FolderBrowser() { Description = "" };
            DirectorySearch id_7a0b8a3a7af84da990dc21cc818a01b2 = new DirectorySearch(directoriesToFind: new string[] { "DomainAbstractions", "ProgrammingParadigms", "RequirementsAbstractions" }) { FilenameFilter = "*.cs" };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_419bc16018474d21b992de09ef042e06 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("DomainAbstractions")){list = input["DomainAbstractions"];}return list;} };
            ForEach<string> id_7ac11cfdd654474a98a2e0a473926124 = new ForEach<string>() {  };
            ApplyAction<string> id_8b226dc849154d08add2ffa5c388472c = new ApplyAction<string>() { Lambda = input =>{abstractionModelManager.CreateAbstractionModelFromPath(input);} };
            Data<string> id_89edef60ed6e44b7973cae48517a7ac5 = new Data<string>() { storedData = "Apply" };
            Apply<string, AbstractionModel> id_294dd014e3b04e129ec985dc6e6c784d = new Apply<string, AbstractionModel>() { Lambda = input =>{return abstractionModelManager.GetAbstractionModel(input);} };
            Data<string> id_229ddbbedbf14e2a9785f49e8b9c1a71 = new Data<string>() { storedData = @"F:\Projects\GALADE\ALACore" };
            KeyEvent id_bb8aafcd4d8742b4baf17b622a040e34 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.Enter } };
            ApplyAction<object> id_47c1e2b9853f47cbbec12858bf431cbb = new ApplyAction<object>() { Lambda = input =>{(input as WPFCanvas).Focus();} };
            MenuItem id_198f1aa93ec745cd8f0ccb5b6d6acac0 = new MenuItem(header: "Debug") {  };
            MenuItem id_fc4026ff3bfc4338afd9e9874df6cc6f = new MenuItem(header: "TextEditor test") {  };
            PopupWindow id_5d2ae916f40341dc9da707ce9fd59258 = new PopupWindow(title: "") { Height = 720, Width = 1280, Resize = SizeToContent.WidthAndHeight };
            Box id_eea8623846204833b755a515c6fcbbf4 = new Box() { Width = 100, Height = 100 };
            TextEditor id_30ad28498a37475fabc93c2ba301975f = new TextEditor() { Width = 1280, Height = 720 };
            DataFlowConnector<string> id_29148cfd1b1e49ca8e36bf2d00a67b1c = new DataFlowConnector<string>() {  };
            FolderWatcher projectFolderWatcher = new FolderWatcher() { InstanceName = "projectFolderWatcher", RootPath = "", Filter = "*.cs", WatchSubdirectories = true, PathRegex = @".*\.cs$" };
            Apply<string, object> id_cc071479d50b42cca962fb700bf5e527 = new Apply<string, object>() { Lambda = input =>{var newModel = abstractionModelManager.CreateAbstractionModelFromPath(input);foreach (var node in mainGraph.Nodes){var alaNode = node as ALANode;if (alaNode.Model.Type != newModel.Type) continue;abstractionModelManager.UpdateAbstractionModel(newModel, alaNode.Model);alaNode.UpdateUI();}return input;} };
            ConvertToEvent<object> id_b40bf94baea04511bd04e441149e5da8 = new ConvertToEvent<object>() {  };
            MouseButtonEvent id_fd7fba3d8b6340f9a8768e7e3638bc0c = new MouseButtonEvent(eventName: "MouseRightButtonDown") { ExtractSender = null, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            ApplyAction<object> id_2dea5ebb1c9746029fb5af886cb4686f = new ApplyAction<object>() { Lambda = input =>{Mouse.Capture(input as WPFCanvas);stateTransition.Update(Enums.DiagramMode.Idle);} };
            MouseButtonEvent id_fd52ee8bcc51414b901d333cf729e2a7 = new MouseButtonEvent(eventName: "MouseRightButtonUp") { ExtractSender = null, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            ApplyAction<object> id_c5684a5e04334c54beb6274c17c159c6 = new ApplyAction<object>() { Lambda = input =>{if (Mouse.Captured?.Equals(input) ?? false) Mouse.Capture(null);stateTransition.Update(Enums.DiagramMode.Idle);} };
            StateChangeListener id_8b606e181d69439f8ce84f56b7c27429 = new StateChangeListener() { StateTransition = stateTransition, CurrentStateShouldMatch = Enums.DiagramMode.All };
            Apply<Tuple<Enums.DiagramMode, Enums.DiagramMode>, bool> id_9e30b1f34ed4447b991284bb8afddb82 = new Apply<Tuple<Enums.DiagramMode, Enums.DiagramMode>, bool>() { Lambda = input =>{return input.Item1 == Enums.DiagramMode.AwaitingPortSelection &&input.Item2 == Enums.DiagramMode.Idle;} };
            IfElse id_b1c0a1dc098a44a19b82fc2767fb3a46 = new IfElse() {  };
            EventConnector id_753cee00cd624c9ca8e156f2e1dc4b5c = new EventConnector() {  };
            Apply<object, object> createAndPaintALAWire = new Apply<object, object>() { InstanceName = "createAndPaintALAWire", Lambda = input =>{var source = mainGraph.Get("SelectedNode") as ALANode;var destination = input as ALANode;var sourcePort = source.GetSelectedPort(inputPort: false);var destinationPort = destination.GetSelectedPort(inputPort: true);var wire = new ALAWire(){Graph = mainGraph,Canvas = mainCanvas,Source = source,Destination = destination,SourcePort = sourcePort,DestinationPort = destinationPort,StateTransition = stateTransition};mainGraph.AddEdge(wire);wire.Paint();return wire;} };
            KeyEvent id_b88cd6e0cb8f4b0d80f976659635e2af = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.Delete } };
            EventLambda id_5f8312b09cd8463a960549cf0e1a2813 = new EventLambda() { Lambda = () =>{var selectedNode = mainGraph.Get("SelectedNode") as ALANode;if (selectedNode == null) return;selectedNode.Delete(deleteAttachedWires: true);} };
            MenuItem id_543c29de2c21449b8fcc6af9ebd3fb9e = new MenuItem(header: "Refresh") {  };
            Data<AbstractionModel> id_087cb1aaae8e4cacb83b434a7aba83bd = new Data<AbstractionModel>() { Lambda = () => abstractionModelManager.GetAbstractionModel(abstractionModelManager.GetAbstractionTypes().FirstOrDefault()) };
            Apply<AbstractionModel, object> id_05911d075a474e05a272039fa3836d44 = new Apply<AbstractionModel, object>() { Lambda = createNewALANode.Lambda };
            ApplyAction<object> id_0f93b3a3682c4782bb1b9bee714921d0 = new ApplyAction<object>() { Lambda = input =>{var alaNode = input as ALANode;var mousePos = Mouse.GetPosition(mainCanvas);alaNode.PositionX = mousePos.X;alaNode.PositionY = mousePos.Y;mainGraph.Set("LatestNode", input);if (mainGraph.Get("SelectedNode") == null){mainGraph.Set("SelectedNode", input);}mainGraph.Roots.Add(input);} };
            MenuItem id_383a98ef53254766b3d5f5bae7b98183 = new MenuItem(header: "Open Code File") {  };
            FileBrowser id_e632b932a3b8446481a9c3f0d45762bf = new FileBrowser() { Mode = "Open" };
            FileReader id_ab893cb1a7a742f3b39b3aae36961f49 = new FileReader() {  };
            CreateDiagramFromCode id_708911b340024617bd09bb3fe6544682 = new CreateDiagramFromCode() { Graph = mainGraph, Canvas = mainCanvas, ModelManager = abstractionModelManager, StateTransition = stateTransition };
            EventConnector id_207c0f31375d4fc7a5b869599112ff4b = new EventConnector() {  };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_9e1242663bfd4fd8ac6d925bbe330bf5 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("ProgrammingParadigms")){list = input["ProgrammingParadigms"];}return list;} };
            ForEach<string> id_d45e194904004e74a65c07c7bf188929 = new ForEach<string>() {  };
            ApplyAction<string> id_b87c40333f92497fad485add9dd6e174 = new ApplyAction<string>() { Lambda = input =>{abstractionModelManager.CreateAbstractionModelFromPath(input);} };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_ef95ae622ba64f13bb38b933db923eba = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("RequirementsAbstractions")){list = input["RequirementsAbstractions"];}return list;} };
            ForEach<string> id_6baa98e72c384d9c95615335fccf9c21 = new ForEach<string>() {  };
            ApplyAction<string> id_cf7199415b17419fb7825a6dfc38182f = new ApplyAction<string>() { Lambda = input =>{abstractionModelManager.CreateAbstractionModelFromPath(input);} };
            DataFlowConnector<Dictionary<string, List<string>>> id_a66d1004626a4135af4eeacc93775c93 = new DataFlowConnector<Dictionary<string, List<string>>>() {  };
            MenuItem id_b64a9db1adda4aa88d05bc4d454cca48 = new MenuItem(header: "Zoom In") {  };
            Data<UIElement> id_ed9dfea1514548978cd4aa892cd7f7e2 = new Data<UIElement>() { Lambda = () => mainCanvas };
            Scale id_fc2a858b886344baa2f16a2a2a64bf4c = new Scale() { WidthMultiplier = 1.1, HeightMultiplier = 1.1 };
            MenuItem id_b662e2aff2504a1085bedef81ec89afe = new MenuItem(header: "Zoom Out") {  };
            Data<UIElement> id_74adea5c1e684639a7b00785d32597d7 = new Data<UIElement>() { Lambda = () => mainCanvas };
            Scale id_ff1c750633424481868f502fccfa098f = new Scale() { WidthMultiplier = 0.9, HeightMultiplier = 0.9 };
            DataFlowConnector<UIElement> id_555602f5b70842dda8297b7d6940324e = new DataFlowConnector<UIElement>() {  };
            DataFlowConnector<UIElement> id_c74c14c719a64a3db64f85d6a070b0f2 = new DataFlowConnector<UIElement>() {  };
            ApplyAction<UIElement> id_ebb189441dfa4f35af082effa764f9a8 = new ApplyAction<UIElement>() { Lambda = input => {if (!(input.RenderTransform is ScaleTransform)) return;var transform = input.RenderTransform as ScaleTransform;var minScale = 0.8;/*Logging.Log($"Scale: {transform.ScaleX}, {transform.ScaleX}");*/bool nodeIsTooSmall = transform.ScaleX < minScale && transform.ScaleY < minScale;var nodes = mainGraph.Nodes;foreach (var node in nodes){if (node is ALANode alaNode) alaNode.ShowTypeTextMask(nodeIsTooSmall);}} };
            // END AUTO-GENERATED INSTANTIATIONS FOR Application.xmind

            // BEGIN AUTO-GENERATED WIRING FOR Application.xmind
            mainWindow.WireTo(id_a492431fd74544f8875704b800186f7e, "iuiStructure");
            mainWindow.WireTo(id_753cee00cd624c9ca8e156f2e1dc4b5c, "appStart");
            id_a492431fd74544f8875704b800186f7e.WireTo(id_e82a887f0141403e9250c85f9479fff4, "children");
            id_a492431fd74544f8875704b800186f7e.WireTo(id_7cc52c9442f64bf7b4494cb9797a12e4, "children");
            id_7cc52c9442f64bf7b4494cb9797a12e4.WireTo(id_4fb4ea9344c8474284773e2c780cd248, "eventHandlers");
            id_7cc52c9442f64bf7b4494cb9797a12e4.WireTo(id_3f3ef7db83ec4c77830a3e20364433df, "eventHandlers");
            id_7cc52c9442f64bf7b4494cb9797a12e4.WireTo(id_bb8aafcd4d8742b4baf17b622a040e34, "eventHandlers");
            id_7cc52c9442f64bf7b4494cb9797a12e4.WireTo(id_fd7fba3d8b6340f9a8768e7e3638bc0c, "eventHandlers");
            id_7cc52c9442f64bf7b4494cb9797a12e4.WireTo(id_fd52ee8bcc51414b901d333cf729e2a7, "eventHandlers");
            id_7cc52c9442f64bf7b4494cb9797a12e4.WireTo(id_b88cd6e0cb8f4b0d80f976659635e2af, "eventHandlers");
            id_7cc52c9442f64bf7b4494cb9797a12e4.WireTo(id_ad1264752d354cbe88641af4a9b5da5b, "contextMenu");
            id_4fb4ea9344c8474284773e2c780cd248.WireTo(id_e5653fa0b2b74847b953499359682f53, "eventHappened");
            id_ad1264752d354cbe88641af4a9b5da5b.WireTo(id_ca9db8e8e8434659b43931e7b76b6324, "children");
            id_ad1264752d354cbe88641af4a9b5da5b.WireTo(id_543c29de2c21449b8fcc6af9ebd3fb9e, "children");
            id_ca9db8e8e8434659b43931e7b76b6324.WireTo(id_087cb1aaae8e4cacb83b434a7aba83bd, "clickedEvent");
            id_e5653fa0b2b74847b953499359682f53.WireTo(id_89edef60ed6e44b7973cae48517a7ac5, "fanoutList");
            id_e5653fa0b2b74847b953499359682f53.WireTo(layoutDiagram, "complete");
            getFirstRoot.WireTo(id_9bed4bb4cc08495d8e509c5c030a7829, "dataOutput");
            layoutDiagram.WireTo(getFirstRoot, "fanoutList");
            id_9bed4bb4cc08495d8e509c5c030a7829.WireTo(id_ae376a5905ea425099e3a35dc1f19e95, "fanoutList");
            id_9bed4bb4cc08495d8e509c5c030a7829.WireTo(id_a904222e05504395bdce0ca3faa0bb5f, "fanoutList");
            id_3f3ef7db83ec4c77830a3e20364433df.WireTo(layoutDiagram, "eventHappened");
            createNewALANode.WireTo(createAndPaintALAWire, "output");
            id_e82a887f0141403e9250c85f9479fff4.WireTo(id_3123efab2edc4bebafb6f37f35bbf012, "children");
            id_e82a887f0141403e9250c85f9479fff4.WireTo(id_198f1aa93ec745cd8f0ccb5b6d6acac0, "children");
            id_e82a887f0141403e9250c85f9479fff4.WireTo(id_b64a9db1adda4aa88d05bc4d454cca48, "children");
            id_e82a887f0141403e9250c85f9479fff4.WireTo(id_b662e2aff2504a1085bedef81ec89afe, "children");
            id_3123efab2edc4bebafb6f37f35bbf012.WireTo(id_50eff2838e144c0fb57729f022ec3b08, "children");
            id_3123efab2edc4bebafb6f37f35bbf012.WireTo(id_383a98ef53254766b3d5f5bae7b98183, "children");
            id_50eff2838e144c0fb57729f022ec3b08.WireTo(id_cda28e9c3924418dafb98e229b5e28f9, "clickedEvent");
            id_cda28e9c3924418dafb98e229b5e28f9.WireTo(id_29148cfd1b1e49ca8e36bf2d00a67b1c, "selectedFolderPathOutput");
            id_7a0b8a3a7af84da990dc21cc818a01b2.WireTo(id_a66d1004626a4135af4eeacc93775c93, "foundFiles");
            id_419bc16018474d21b992de09ef042e06.WireTo(id_7ac11cfdd654474a98a2e0a473926124, "output");
            id_7ac11cfdd654474a98a2e0a473926124.WireTo(id_8b226dc849154d08add2ffa5c388472c, "elementOutput");
            id_89edef60ed6e44b7973cae48517a7ac5.WireTo(id_294dd014e3b04e129ec985dc6e6c784d, "dataOutput");
            id_294dd014e3b04e129ec985dc6e6c784d.WireTo(createNewALANode, "output");
            id_229ddbbedbf14e2a9785f49e8b9c1a71.WireTo(id_29148cfd1b1e49ca8e36bf2d00a67b1c, "dataOutput");
            id_bb8aafcd4d8742b4baf17b622a040e34.WireTo(id_47c1e2b9853f47cbbec12858bf431cbb, "senderOutput");
            id_198f1aa93ec745cd8f0ccb5b6d6acac0.WireTo(id_fc4026ff3bfc4338afd9e9874df6cc6f, "children");
            id_fc4026ff3bfc4338afd9e9874df6cc6f.WireTo(id_5d2ae916f40341dc9da707ce9fd59258, "clickedEvent");
            id_5d2ae916f40341dc9da707ce9fd59258.WireTo(id_eea8623846204833b755a515c6fcbbf4, "children");
            id_eea8623846204833b755a515c6fcbbf4.WireTo(id_30ad28498a37475fabc93c2ba301975f, "uiLayout");
            id_29148cfd1b1e49ca8e36bf2d00a67b1c.WireTo(id_7a0b8a3a7af84da990dc21cc818a01b2, "fanoutList");
            id_29148cfd1b1e49ca8e36bf2d00a67b1c.WireTo(projectFolderWatcher, "fanoutList");
            projectFolderWatcher.WireTo(id_cc071479d50b42cca962fb700bf5e527, "changedFile");
            id_cc071479d50b42cca962fb700bf5e527.WireTo(id_b40bf94baea04511bd04e441149e5da8, "output");
            id_b40bf94baea04511bd04e441149e5da8.WireTo(id_a904222e05504395bdce0ca3faa0bb5f, "eventOutput");
            id_fd7fba3d8b6340f9a8768e7e3638bc0c.WireTo(id_2dea5ebb1c9746029fb5af886cb4686f, "argsOutput");
            id_fd52ee8bcc51414b901d333cf729e2a7.WireTo(id_c5684a5e04334c54beb6274c17c159c6, "argsOutput");
            id_8b606e181d69439f8ce84f56b7c27429.WireTo(id_9e30b1f34ed4447b991284bb8afddb82, "transitionOutput");
            id_9e30b1f34ed4447b991284bb8afddb82.WireTo(id_b1c0a1dc098a44a19b82fc2767fb3a46, "output");
            id_b1c0a1dc098a44a19b82fc2767fb3a46.WireTo(layoutDiagram, "ifOutput");
            id_753cee00cd624c9ca8e156f2e1dc4b5c.WireTo(id_229ddbbedbf14e2a9785f49e8b9c1a71, "fanoutList");
            id_753cee00cd624c9ca8e156f2e1dc4b5c.WireTo(id_8b606e181d69439f8ce84f56b7c27429, "fanoutList");
            id_753cee00cd624c9ca8e156f2e1dc4b5c.WireTo(id_207c0f31375d4fc7a5b869599112ff4b, "complete");
            id_b88cd6e0cb8f4b0d80f976659635e2af.WireTo(id_5f8312b09cd8463a960549cf0e1a2813, "eventHappened");
            id_543c29de2c21449b8fcc6af9ebd3fb9e.WireTo(layoutDiagram, "clickedEvent");
            id_087cb1aaae8e4cacb83b434a7aba83bd.WireTo(id_05911d075a474e05a272039fa3836d44, "dataOutput");
            id_05911d075a474e05a272039fa3836d44.WireTo(id_0f93b3a3682c4782bb1b9bee714921d0, "output");
            id_383a98ef53254766b3d5f5bae7b98183.WireTo(id_e632b932a3b8446481a9c3f0d45762bf, "clickedEvent");
            id_e632b932a3b8446481a9c3f0d45762bf.WireTo(id_ab893cb1a7a742f3b39b3aae36961f49, "selectedFilePathOutput");
            id_ab893cb1a7a742f3b39b3aae36961f49.WireTo(id_708911b340024617bd09bb3fe6544682, "fileContentOutput");
            id_9e1242663bfd4fd8ac6d925bbe330bf5.WireTo(id_d45e194904004e74a65c07c7bf188929, "output");
            id_d45e194904004e74a65c07c7bf188929.WireTo(id_b87c40333f92497fad485add9dd6e174, "elementOutput");
            id_ef95ae622ba64f13bb38b933db923eba.WireTo(id_6baa98e72c384d9c95615335fccf9c21, "output");
            id_6baa98e72c384d9c95615335fccf9c21.WireTo(id_cf7199415b17419fb7825a6dfc38182f, "elementOutput");
            id_a66d1004626a4135af4eeacc93775c93.WireTo(id_419bc16018474d21b992de09ef042e06, "fanoutList");
            id_a66d1004626a4135af4eeacc93775c93.WireTo(id_9e1242663bfd4fd8ac6d925bbe330bf5, "fanoutList");
            id_a66d1004626a4135af4eeacc93775c93.WireTo(id_ef95ae622ba64f13bb38b933db923eba, "fanoutList");
            id_b64a9db1adda4aa88d05bc4d454cca48.WireTo(id_ed9dfea1514548978cd4aa892cd7f7e2, "clickedEvent");
            id_ed9dfea1514548978cd4aa892cd7f7e2.WireTo(id_555602f5b70842dda8297b7d6940324e, "dataOutput");
            id_b662e2aff2504a1085bedef81ec89afe.WireTo(id_74adea5c1e684639a7b00785d32597d7, "clickedEvent");
            id_74adea5c1e684639a7b00785d32597d7.WireTo(id_c74c14c719a64a3db64f85d6a070b0f2, "dataOutput");
            id_555602f5b70842dda8297b7d6940324e.WireTo(id_fc2a858b886344baa2f16a2a2a64bf4c, "fanoutList");
            id_555602f5b70842dda8297b7d6940324e.WireTo(id_ebb189441dfa4f35af082effa764f9a8, "fanoutList");
            id_c74c14c719a64a3db64f85d6a070b0f2.WireTo(id_ff1c750633424481868f502fccfa098f, "fanoutList");
            id_c74c14c719a64a3db64f85d6a070b0f2.WireTo(id_ebb189441dfa4f35af082effa764f9a8, "fanoutList");
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


            _mainWindow = mainWindow;
        }

        private Application()
        {
            CreateWiring();
        }
    }
}






















































































































































































































