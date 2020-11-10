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
            Vertical id_48e5732cfb8447039e7a938eb491b546 = new Vertical() {  };
            CanvasDisplay id_ff8ec569cd894208a7db159a411700eb = new CanvasDisplay() { Width = 1920, Height = 1080, Background = Brushes.White, StateTransition = stateTransition, Canvas = mainCanvas };
            KeyEvent id_7ed18ec5ac584aac9bc847be98f90b1c = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.A }, Condition = args => mainGraph.Get("SelectedNode") != null&& stateTransition.CurrentStateMatches(Enums.DiagramMode.IdleSelected) };
            ContextMenu id_44369e82a62d452abbc223f2a7ec0631 = new ContextMenu() {  };
            MenuItem id_cd618ec6a64642d48214baf23434554d = new MenuItem(header: "Add root") {  };
            EventConnector id_7727faf96aaf4148bf4a5ee0085853dc = new EventConnector() {  };
            Data<ALANode> getFirstRoot = new Data<ALANode>() { InstanceName = "getFirstRoot", Lambda = () => mainGraph.Roots.FirstOrDefault() as ALANode };
            RightTreeLayout<ALANode> id_d25c017eea99404b96c3493ba711561f = new RightTreeLayout<ALANode>() { GetID = n => n.Id, GetWidth = n => n.Width, GetHeight = n => n.Height, SetX = (n, x) => n.PositionX = x, SetY = (n, y) => n.PositionY = y, GetChildren = n => mainGraph.Edges.Where(e => e is ALAWire wire && wire.Source != null && wire.Destination != null && wire.Source == n).Select(e => ((e as ALAWire).Destination) as ALANode), HorizontalGap = 100, VerticalGap = 20, InitialX = 50, InitialY = 50 };
            EventConnector layoutDiagram = new EventConnector() { InstanceName = "layoutDiagram" };
            DataFlowConnector<ALANode> id_9c3fa17d04f6462f9cc86c60f6f06523 = new DataFlowConnector<ALANode>() {  };
            ApplyAction<ALANode> id_60147eea09be4ca9a09f40a181ee375d = new ApplyAction<ALANode>() { Lambda = node =>{Dispatcher.CurrentDispatcher.Invoke(() => {var edges = mainGraph.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);} };
            KeyEvent id_b50ae9f26d7949ee9a57f6d422e3a829 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.R }, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            Apply<AbstractionModel, object> createNewALANode = new Apply<AbstractionModel, object>() { InstanceName = "createNewALANode", Lambda = input => {var node = new ALANode();node.Model = input;node.Graph = mainGraph;node.Canvas = mainCanvas;node.StateTransition = stateTransition;node.AvailableDomainAbstractions.AddRange( abstractionModelManager.GetAbstractionTypes());node.TypeChanged += newType => {abstractionModelManager.UpdateAbstractionModel(abstractionModelManager.GetAbstractionModel(newType),node.Model);node.UpdateUI();Dispatcher.CurrentDispatcher.Invoke(() => {var edges = mainGraph.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);};mainGraph.AddNode(node);node.CreateInternals();mainCanvas.Children.Add(node.Render);return node;} };
            MenuBar id_a30af7db1b814a2da665d27020893cd9 = new MenuBar() {  };
            MenuItem id_d6e0940ddd234e69b2fff5765a5813f5 = new MenuItem(header: "File") {  };
            MenuItem id_56ea5ed2c6f54363afcd1bc993645a2a = new MenuItem(header: "Open Project") {  };
            FolderBrowser id_ac09edc4d4e04115aa8b797fc96e27f9 = new FolderBrowser() { Description = "" };
            DirectorySearch id_0f6407edc97a43c08e50751ef812e4d3 = new DirectorySearch(directoriesToFind: new string[] { "DomainAbstractions", "ProgrammingParadigms", "RequirementsAbstractions" }) { FilenameFilter = "*.cs" };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_596d1a32ed684d8f80499b1eb6923de6 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("DomainAbstractions")){list = input["DomainAbstractions"];}return list;} };
            ForEach<string> id_18905148644849acaa955c66e4e09095 = new ForEach<string>() {  };
            ApplyAction<string> id_0a5054799a6245d98228a0e692fe04f7 = new ApplyAction<string>() { Lambda = input =>{abstractionModelManager.CreateAbstractionModelFromPath(input);} };
            Data<string> id_2544f5b51c0744a58f0b2fa24e02603d = new Data<string>() { storedData = "Apply" };
            Apply<string, AbstractionModel> id_e92604cfdb924371b93363ed649c4f30 = new Apply<string, AbstractionModel>() { Lambda = input =>{return abstractionModelManager.GetAbstractionModel(input);} };
            Data<string> id_d2725271cad045fea9b76c7a6784609c = new Data<string>() { storedData = @"F:\Projects\GALADE\ALACore" };
            KeyEvent id_8f98f10135984c3baa818340d6980ed0 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.Enter } };
            ApplyAction<object> id_d5b1ae07dea543929088c03334064e76 = new ApplyAction<object>() { Lambda = input =>{(input as WPFCanvas).Focus();} };
            MenuItem id_8a5cb24d104d4c9b90204d8794b023a6 = new MenuItem(header: "Debug") {  };
            MenuItem id_973a68d61cfb4429a1f8efd70c59d3b3 = new MenuItem(header: "TextEditor test") {  };
            PopupWindow id_7687b418000547bc82cac32ddcfae5e3 = new PopupWindow(title: "") { Height = 720, Width = 1280, Resize = SizeToContent.WidthAndHeight };
            Box id_bfb0d9906d5b44afbe20ea9404bccad7 = new Box() { Width = 100, Height = 100 };
            TextEditor id_2cf5e98f656a44b48a09e822603fbcec = new TextEditor() { Width = 1280, Height = 720 };
            DataFlowConnector<string> id_93b83090ffcf4d268bb198007e0ba48f = new DataFlowConnector<string>() {  };
            FolderWatcher projectFolderWatcher = new FolderWatcher() { InstanceName = "projectFolderWatcher", RootPath = "", Filter = "*.cs", WatchSubdirectories = true, PathRegex = @".*\.cs$" };
            Apply<string, object> id_63e014b5c9414e2a82d9dc7687bc8875 = new Apply<string, object>() { Lambda = input =>{var newModel = abstractionModelManager.CreateAbstractionModelFromPath(input);foreach (var node in mainGraph.Nodes){var alaNode = node as ALANode;if (alaNode.Model.Type != newModel.Type) continue;abstractionModelManager.UpdateAbstractionModel(newModel, alaNode.Model);alaNode.UpdateUI();}return input;} };
            ConvertToEvent<object> id_6b6c21e9b44a4b74a7e0765d07ee8ed1 = new ConvertToEvent<object>() {  };
            MouseButtonEvent id_5bafcb222c4543e3ba3d79cbf7da5092 = new MouseButtonEvent(eventName: "MouseRightButtonDown") { ExtractSender = null, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            ApplyAction<object> id_51ff58ba66b74936b64341c1a893b97a = new ApplyAction<object>() { Lambda = input =>{Mouse.Capture(input as WPFCanvas);stateTransition.Update(Enums.DiagramMode.Idle);} };
            MouseButtonEvent id_7615d9fb2cc84700bff603c810ef0d4e = new MouseButtonEvent(eventName: "MouseRightButtonUp") { ExtractSender = null, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            ApplyAction<object> id_e842696945c4411e8d53fa95d4373c1f = new ApplyAction<object>() { Lambda = input =>{if (Mouse.Captured?.Equals(input) ?? false) Mouse.Capture(null);stateTransition.Update(Enums.DiagramMode.Idle);} };
            StateChangeListener id_8057e1345acb4a9bb86c3c3a9e0eb2c7 = new StateChangeListener() { StateTransition = stateTransition, CurrentStateShouldMatch = Enums.DiagramMode.All };
            Apply<Tuple<Enums.DiagramMode, Enums.DiagramMode>, bool> id_9c316b47dce74aa7a89f09cd862285fa = new Apply<Tuple<Enums.DiagramMode, Enums.DiagramMode>, bool>() { Lambda = input =>{return input.Item1 == Enums.DiagramMode.AwaitingPortSelection &&input.Item2 == Enums.DiagramMode.Idle;} };
            IfElse id_867f27780b204c3d86a1d69cfce58a48 = new IfElse() {  };
            EventConnector id_5e8ce99f314b452eb7ce8454876faf31 = new EventConnector() {  };
            Apply<object, object> createAndPaintALAWire = new Apply<object, object>() { InstanceName = "createAndPaintALAWire", Lambda = input =>{var source = mainGraph.Get("SelectedNode") as ALANode;var destination = input as ALANode;var sourcePort = source.GetSelectedPort(inputPort: false);var destinationPort = destination.GetSelectedPort(inputPort: true);var wire = new ALAWire(){Graph = mainGraph,Canvas = mainCanvas,Source = source,Destination = destination,SourcePort = sourcePort,DestinationPort = destinationPort,StateTransition = stateTransition};mainGraph.AddEdge(wire);wire.Paint();return wire;} };
            KeyEvent id_4ba8eac3897c4474b4d20f968ba18b6e = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.Delete } };
            EventLambda id_e8965235ddea4be592cf4668de74aef0 = new EventLambda() { Lambda = () =>{var selectedNode = mainGraph.Get("SelectedNode") as ALANode;if (selectedNode == null) return;selectedNode.Delete(deleteAttachedWires: true);} };
            MenuItem id_51daac05ef8d4f1e9b74769d6f81f4c1 = new MenuItem(header: "Refresh") {  };
            Data<AbstractionModel> id_8ae9ecfa72b5450c8b3b70edfe0e9007 = new Data<AbstractionModel>() { Lambda = () => abstractionModelManager.GetAbstractionModel(abstractionModelManager.GetAbstractionTypes().FirstOrDefault()) };
            Apply<AbstractionModel, object> id_2b0e80a3a51f492ea8cb26c306d7b33a = new Apply<AbstractionModel, object>() { Lambda = createNewALANode.Lambda };
            ApplyAction<object> id_5756fa423a6a49adae366abb3b93fad0 = new ApplyAction<object>() { Lambda = input =>{var alaNode = input as ALANode;var mousePos = Mouse.GetPosition(mainCanvas);alaNode.PositionX = mousePos.X;alaNode.PositionY = mousePos.Y;mainGraph.Set("LatestNode", input);if (mainGraph.Get("SelectedNode") == null){mainGraph.Set("SelectedNode", input);}mainGraph.Roots.Add(input);} };
            MenuItem id_a1cf8933f2754bf59c04580e3eded976 = new MenuItem(header: "Open Code File") {  };
            FileBrowser id_4d16094d5ebc4accad0c27a29825b92a = new FileBrowser() { Mode = "Open" };
            FileReader id_7b34df1b1dd040cc9a26e52ea6bb720d = new FileReader() {  };
            CreateDiagramFromCode id_b599e9daee7a418880bb73fd2ac586bd = new CreateDiagramFromCode() { Graph = mainGraph, Canvas = mainCanvas, ModelManager = abstractionModelManager, StateTransition = stateTransition };
            EventConnector id_b543f63fcf964cbcafbeb5f9a097d450 = new EventConnector() {  };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_077c164739de48d98a955f7721753959 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("ProgrammingParadigms")){list = input["ProgrammingParadigms"];}return list;} };
            ForEach<string> id_f01a1258087c414cbdf0d33720430b4e = new ForEach<string>() {  };
            ApplyAction<string> id_5144fc6962c94b09bc557c5b2a3ad56a = new ApplyAction<string>() { Lambda = input =>{abstractionModelManager.CreateAbstractionModelFromPath(input);} };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_7f6b8473cea74482a6d948699bb8d8b7 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("RequirementsAbstractions")){list = input["RequirementsAbstractions"];}return list;} };
            ForEach<string> id_594152cd55874e648b4f1b43bf11b448 = new ForEach<string>() {  };
            ApplyAction<string> id_e5fecd28d4544a4baf38dc7bc9de7769 = new ApplyAction<string>() { Lambda = input =>{abstractionModelManager.CreateAbstractionModelFromPath(input);} };
            DataFlowConnector<Dictionary<string, List<string>>> id_6730da67dc5b4b80b9645f0ccb168368 = new DataFlowConnector<Dictionary<string, List<string>>>() {  };
            Data<UIElement> id_30d57c4538de421580e0276a6ce60e5d = new Data<UIElement>() { Lambda = () => mainCanvas };
            Scale id_54d3309077eb4a4ca472378df66cfc00 = new Scale() { WidthMultiplier = 1.1, HeightMultiplier = 1.1 };
            Data<UIElement> id_8e142cb1ebb8449cad4fa77220be92ab = new Data<UIElement>() { Lambda = () => mainCanvas };
            Scale id_2fb8ec56cfc44bafa9acdfdb3ddd919b = new Scale() { WidthMultiplier = 0.9, HeightMultiplier = 0.9 };
            DataFlowConnector<UIElement> id_78101b6d5cc649b5b2dc49e91b2b41dc = new DataFlowConnector<UIElement>() {  };
            DataFlowConnector<UIElement> id_40ad71e26dd54620b2b02d29ae684de9 = new DataFlowConnector<UIElement>() {  };
            ApplyAction<UIElement> id_519fb89612054c51ba4dd46c65a0b21c = new ApplyAction<UIElement>() { Lambda = input => {if (!(input.RenderTransform is ScaleTransform)) return;var transform = input.RenderTransform as ScaleTransform;var minScale = 0.6;/*Logging.Log($"Scale: {transform.ScaleX}, {transform.ScaleX}");*/bool nodeIsTooSmall = transform.ScaleX < minScale && transform.ScaleY < minScale;var nodes = mainGraph.Nodes;foreach (var node in nodes){if (node is ALANode alaNode) alaNode.ShowTypeTextMask(nodeIsTooSmall);}} };
            MouseWheelEvent id_401ce57e89204b74b954974e634d6da0 = new MouseWheelEvent(eventName: "MouseWheel") {  };
            Apply<MouseWheelEventArgs, bool> id_a56f02395479430c855562b834dc226f = new Apply<MouseWheelEventArgs, bool>() { Lambda = args =>{return args.Delta > 0;} };
            IfElse id_34c122f7ceb74fbb8e969fa7913de1a4 = new IfElse() {  };
            // END AUTO-GENERATED INSTANTIATIONS FOR Application.xmind

            // BEGIN AUTO-GENERATED WIRING FOR Application.xmind
            mainWindow.WireTo(id_48e5732cfb8447039e7a938eb491b546, "iuiStructure");
            mainWindow.WireTo(id_5e8ce99f314b452eb7ce8454876faf31, "appStart");
            id_48e5732cfb8447039e7a938eb491b546.WireTo(id_a30af7db1b814a2da665d27020893cd9, "children");
            id_48e5732cfb8447039e7a938eb491b546.WireTo(id_ff8ec569cd894208a7db159a411700eb, "children");
            id_ff8ec569cd894208a7db159a411700eb.WireTo(id_7ed18ec5ac584aac9bc847be98f90b1c, "eventHandlers");
            id_ff8ec569cd894208a7db159a411700eb.WireTo(id_b50ae9f26d7949ee9a57f6d422e3a829, "eventHandlers");
            id_ff8ec569cd894208a7db159a411700eb.WireTo(id_8f98f10135984c3baa818340d6980ed0, "eventHandlers");
            id_ff8ec569cd894208a7db159a411700eb.WireTo(id_5bafcb222c4543e3ba3d79cbf7da5092, "eventHandlers");
            id_ff8ec569cd894208a7db159a411700eb.WireTo(id_7615d9fb2cc84700bff603c810ef0d4e, "eventHandlers");
            id_ff8ec569cd894208a7db159a411700eb.WireTo(id_4ba8eac3897c4474b4d20f968ba18b6e, "eventHandlers");
            id_ff8ec569cd894208a7db159a411700eb.WireTo(id_401ce57e89204b74b954974e634d6da0, "eventHandlers");
            id_ff8ec569cd894208a7db159a411700eb.WireTo(id_44369e82a62d452abbc223f2a7ec0631, "contextMenu");
            id_7ed18ec5ac584aac9bc847be98f90b1c.WireTo(id_7727faf96aaf4148bf4a5ee0085853dc, "eventHappened");
            id_44369e82a62d452abbc223f2a7ec0631.WireTo(id_cd618ec6a64642d48214baf23434554d, "children");
            id_44369e82a62d452abbc223f2a7ec0631.WireTo(id_51daac05ef8d4f1e9b74769d6f81f4c1, "children");
            id_cd618ec6a64642d48214baf23434554d.WireTo(id_8ae9ecfa72b5450c8b3b70edfe0e9007, "clickedEvent");
            id_7727faf96aaf4148bf4a5ee0085853dc.WireTo(id_2544f5b51c0744a58f0b2fa24e02603d, "fanoutList");
            id_7727faf96aaf4148bf4a5ee0085853dc.WireTo(layoutDiagram, "complete");
            getFirstRoot.WireTo(id_9c3fa17d04f6462f9cc86c60f6f06523, "dataOutput");
            layoutDiagram.WireTo(getFirstRoot, "fanoutList");
            id_9c3fa17d04f6462f9cc86c60f6f06523.WireTo(id_d25c017eea99404b96c3493ba711561f, "fanoutList");
            id_9c3fa17d04f6462f9cc86c60f6f06523.WireTo(id_60147eea09be4ca9a09f40a181ee375d, "fanoutList");
            id_b50ae9f26d7949ee9a57f6d422e3a829.WireTo(layoutDiagram, "eventHappened");
            createNewALANode.WireTo(createAndPaintALAWire, "output");
            id_a30af7db1b814a2da665d27020893cd9.WireTo(id_d6e0940ddd234e69b2fff5765a5813f5, "children");
            id_a30af7db1b814a2da665d27020893cd9.WireTo(id_8a5cb24d104d4c9b90204d8794b023a6, "children");
            id_d6e0940ddd234e69b2fff5765a5813f5.WireTo(id_56ea5ed2c6f54363afcd1bc993645a2a, "children");
            id_d6e0940ddd234e69b2fff5765a5813f5.WireTo(id_a1cf8933f2754bf59c04580e3eded976, "children");
            id_56ea5ed2c6f54363afcd1bc993645a2a.WireTo(id_ac09edc4d4e04115aa8b797fc96e27f9, "clickedEvent");
            id_ac09edc4d4e04115aa8b797fc96e27f9.WireTo(id_93b83090ffcf4d268bb198007e0ba48f, "selectedFolderPathOutput");
            id_0f6407edc97a43c08e50751ef812e4d3.WireTo(id_6730da67dc5b4b80b9645f0ccb168368, "foundFiles");
            id_596d1a32ed684d8f80499b1eb6923de6.WireTo(id_18905148644849acaa955c66e4e09095, "output");
            id_18905148644849acaa955c66e4e09095.WireTo(id_0a5054799a6245d98228a0e692fe04f7, "elementOutput");
            id_2544f5b51c0744a58f0b2fa24e02603d.WireTo(id_e92604cfdb924371b93363ed649c4f30, "dataOutput");
            id_e92604cfdb924371b93363ed649c4f30.WireTo(createNewALANode, "output");
            id_d2725271cad045fea9b76c7a6784609c.WireTo(id_93b83090ffcf4d268bb198007e0ba48f, "dataOutput");
            id_8f98f10135984c3baa818340d6980ed0.WireTo(id_d5b1ae07dea543929088c03334064e76, "senderOutput");
            id_8a5cb24d104d4c9b90204d8794b023a6.WireTo(id_973a68d61cfb4429a1f8efd70c59d3b3, "children");
            id_973a68d61cfb4429a1f8efd70c59d3b3.WireTo(id_7687b418000547bc82cac32ddcfae5e3, "clickedEvent");
            id_7687b418000547bc82cac32ddcfae5e3.WireTo(id_bfb0d9906d5b44afbe20ea9404bccad7, "children");
            id_bfb0d9906d5b44afbe20ea9404bccad7.WireTo(id_2cf5e98f656a44b48a09e822603fbcec, "uiLayout");
            id_93b83090ffcf4d268bb198007e0ba48f.WireTo(id_0f6407edc97a43c08e50751ef812e4d3, "fanoutList");
            id_93b83090ffcf4d268bb198007e0ba48f.WireTo(projectFolderWatcher, "fanoutList");
            projectFolderWatcher.WireTo(id_63e014b5c9414e2a82d9dc7687bc8875, "changedFile");
            id_63e014b5c9414e2a82d9dc7687bc8875.WireTo(id_6b6c21e9b44a4b74a7e0765d07ee8ed1, "output");
            id_6b6c21e9b44a4b74a7e0765d07ee8ed1.WireTo(id_60147eea09be4ca9a09f40a181ee375d, "eventOutput");
            id_5bafcb222c4543e3ba3d79cbf7da5092.WireTo(id_51ff58ba66b74936b64341c1a893b97a, "argsOutput");
            id_7615d9fb2cc84700bff603c810ef0d4e.WireTo(id_e842696945c4411e8d53fa95d4373c1f, "argsOutput");
            id_8057e1345acb4a9bb86c3c3a9e0eb2c7.WireTo(id_9c316b47dce74aa7a89f09cd862285fa, "transitionOutput");
            id_9c316b47dce74aa7a89f09cd862285fa.WireTo(id_867f27780b204c3d86a1d69cfce58a48, "output");
            id_867f27780b204c3d86a1d69cfce58a48.WireTo(layoutDiagram, "ifOutput");
            id_5e8ce99f314b452eb7ce8454876faf31.WireTo(id_d2725271cad045fea9b76c7a6784609c, "fanoutList");
            id_5e8ce99f314b452eb7ce8454876faf31.WireTo(id_8057e1345acb4a9bb86c3c3a9e0eb2c7, "fanoutList");
            id_5e8ce99f314b452eb7ce8454876faf31.WireTo(id_b543f63fcf964cbcafbeb5f9a097d450, "complete");
            id_4ba8eac3897c4474b4d20f968ba18b6e.WireTo(id_e8965235ddea4be592cf4668de74aef0, "eventHappened");
            id_51daac05ef8d4f1e9b74769d6f81f4c1.WireTo(layoutDiagram, "clickedEvent");
            id_8ae9ecfa72b5450c8b3b70edfe0e9007.WireTo(id_2b0e80a3a51f492ea8cb26c306d7b33a, "dataOutput");
            id_2b0e80a3a51f492ea8cb26c306d7b33a.WireTo(id_5756fa423a6a49adae366abb3b93fad0, "output");
            id_a1cf8933f2754bf59c04580e3eded976.WireTo(id_4d16094d5ebc4accad0c27a29825b92a, "clickedEvent");
            id_4d16094d5ebc4accad0c27a29825b92a.WireTo(id_7b34df1b1dd040cc9a26e52ea6bb720d, "selectedFilePathOutput");
            id_7b34df1b1dd040cc9a26e52ea6bb720d.WireTo(id_b599e9daee7a418880bb73fd2ac586bd, "fileContentOutput");
            id_077c164739de48d98a955f7721753959.WireTo(id_f01a1258087c414cbdf0d33720430b4e, "output");
            id_f01a1258087c414cbdf0d33720430b4e.WireTo(id_5144fc6962c94b09bc557c5b2a3ad56a, "elementOutput");
            id_7f6b8473cea74482a6d948699bb8d8b7.WireTo(id_594152cd55874e648b4f1b43bf11b448, "output");
            id_594152cd55874e648b4f1b43bf11b448.WireTo(id_e5fecd28d4544a4baf38dc7bc9de7769, "elementOutput");
            id_6730da67dc5b4b80b9645f0ccb168368.WireTo(id_596d1a32ed684d8f80499b1eb6923de6, "fanoutList");
            id_6730da67dc5b4b80b9645f0ccb168368.WireTo(id_077c164739de48d98a955f7721753959, "fanoutList");
            id_6730da67dc5b4b80b9645f0ccb168368.WireTo(id_7f6b8473cea74482a6d948699bb8d8b7, "fanoutList");
            id_34c122f7ceb74fbb8e969fa7913de1a4.WireTo(id_30d57c4538de421580e0276a6ce60e5d, "ifOutput");
            id_30d57c4538de421580e0276a6ce60e5d.WireTo(id_78101b6d5cc649b5b2dc49e91b2b41dc, "dataOutput");
            id_34c122f7ceb74fbb8e969fa7913de1a4.WireTo(id_8e142cb1ebb8449cad4fa77220be92ab, "elseOutput");
            id_8e142cb1ebb8449cad4fa77220be92ab.WireTo(id_40ad71e26dd54620b2b02d29ae684de9, "dataOutput");
            id_78101b6d5cc649b5b2dc49e91b2b41dc.WireTo(id_54d3309077eb4a4ca472378df66cfc00, "fanoutList");
            id_78101b6d5cc649b5b2dc49e91b2b41dc.WireTo(id_519fb89612054c51ba4dd46c65a0b21c, "fanoutList");
            id_40ad71e26dd54620b2b02d29ae684de9.WireTo(id_2fb8ec56cfc44bafa9acdfdb3ddd919b, "fanoutList");
            id_40ad71e26dd54620b2b02d29ae684de9.WireTo(id_519fb89612054c51ba4dd46c65a0b21c, "fanoutList");
            id_401ce57e89204b74b954974e634d6da0.WireTo(id_a56f02395479430c855562b834dc226f, "argsOutput");
            id_a56f02395479430c855562b834dc226f.WireTo(id_34c122f7ceb74fbb8e969fa7913de1a4, "output");
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








































































































































































































































