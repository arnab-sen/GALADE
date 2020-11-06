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
            Vertical id_5196db19defd4c27b1fc79050fa11ae3 = new Vertical() {  };
            CanvasDisplay id_127a8ab9a2e54b7fb4800dd8ff00e21a = new CanvasDisplay() { Width = 1920, Height = 600, Background = Brushes.White, StateTransition = stateTransition, Canvas = mainCanvas };
            KeyEvent id_44c261ac75c14d6080ce4a5ecf3d6b3d = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.A }, Condition = args => mainGraph.Get("SelectedNode") != null&& stateTransition.CurrentStateMatches(Enums.DiagramMode.IdleSelected) };
            ContextMenu id_eca28d2f9c284892bf22cd2fffb8d6c8 = new ContextMenu() {  };
            MenuItem id_666c37a95fe7446384c340bd8b3ec738 = new MenuItem(header: "Add root") {  };
            EventConnector id_d35fc547579d4a7fbf3965e87cc2b4a3 = new EventConnector() {  };
            Data<ALANode> getFirstRoot = new Data<ALANode>() { InstanceName = "getFirstRoot", Lambda = () => mainGraph.Roots.First() as ALANode };
            RightTreeLayout<ALANode> id_361c514b1c034dae84a2fb23f6c29da7 = new RightTreeLayout<ALANode>() { GetID = n => n.Id, GetWidth = n => (n.Render as FrameworkElement).ActualWidth, GetHeight = n => (n.Render as FrameworkElement).ActualHeight, SetX = (n, x) => WPFCanvas.SetLeft(n.Render, x), SetY = (n, y) => WPFCanvas.SetTop(n.Render, y), GetChildren = n => mainGraph.Edges.Where(e => e is ALAWire wire && wire.Source != null && wire.Destination != null && wire.Source == n).Select(e => ((e as ALAWire).Destination) as ALANode), HorizontalGap = 100, VerticalGap = 20, InitialX = 50, InitialY = 50 };
            EventConnector layoutDiagram = new EventConnector() { InstanceName = "layoutDiagram" };
            DataFlowConnector<ALANode> id_a359f6f02cbd46b3aad98b95992c9d7f = new DataFlowConnector<ALANode>() {  };
            ApplyAction<ALANode> id_c7a4497282484a8e860365555522d214 = new ApplyAction<ALANode>() { Lambda = node =>{Dispatcher.CurrentDispatcher.Invoke(() => {var edges = mainGraph.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);} };
            KeyEvent id_4ad564f187264b64a46d002afc89fa55 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.R }, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            Apply<AbstractionModel, object> createNewALANode = new Apply<AbstractionModel, object>() { InstanceName = "createNewALANode", Lambda = input => {var node = new ALANode();node.Model = input;node.Graph = mainGraph;node.Canvas = mainCanvas;node.StateTransition = stateTransition;node.AvailableDomainAbstractions.AddRange( abstractionModelManager.GetAbstractionTypes());node.TypeChanged += newType => {abstractionModelManager.UpdateAbstractionModel(abstractionModelManager.GetAbstractionModel(newType),node.Model);node.UpdateUI();Dispatcher.CurrentDispatcher.Invoke(() => {var edges = mainGraph.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);};mainGraph.AddNode(node);node.CreateInternals();mainCanvas.Children.Add(node.Render);return node;} };
            MenuBar id_2b4c8b103e544d2ea4f82e3ecb1e838c = new MenuBar() {  };
            MenuItem id_8b2b2dd939ab4567881dbd1af062ed99 = new MenuItem(header: "File") {  };
            MenuItem id_2f40a513f14c4f2e8ce51fa5da90ee15 = new MenuItem(header: "Open Project") {  };
            FolderBrowser id_cb396a05c2dc42749faafec1b9e43515 = new FolderBrowser() { Description = "" };
            DirectorySearch id_f224783b3f1f4c7a935da47776fde447 = new DirectorySearch(directoriesToFind: new string[] { "DomainAbstractions" }) { FilenameFilter = "*.cs" };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_f4ed33bb9872401b9084bdc1c5ed8a36 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("DomainAbstractions")){list = input["DomainAbstractions"];}return list;} };
            ForEach<string> id_58a8797b59f346acad2baf135887a328 = new ForEach<string>() {  };
            ApplyAction<string> id_a9d4c1a9c469426f87f8c206268d285d = new ApplyAction<string>() { Lambda = input =>{abstractionModelManager.CreateAbstractionModelFromPath(input);} };
            Data<string> id_4079402332d143cd92ba41c222d2486b = new Data<string>() { storedData = "Apply<T1, T2>" };
            Apply<string, AbstractionModel> id_7df1901cd99244d6b373eb8fc8cd01a7 = new Apply<string, AbstractionModel>() { Lambda = input =>{return abstractionModelManager.GetAbstractionModel(input);} };
            Data<string> id_f5285c818a39457ea6aab586729837a1 = new Data<string>() { storedData = @"F:\Projects\GALADE\ALACore" };
            KeyEvent id_f1c7a5340568448f926c6d691c06309d = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.Enter } };
            ApplyAction<object> id_58d2b145b68f4cccbdd2dac80b1d630f = new ApplyAction<object>() { Lambda = input =>{(input as WPFCanvas).Focus();} };
            MenuItem id_9910d0db14ff46229ed483d25504ed04 = new MenuItem(header: "Debug") {  };
            MenuItem id_49a6c9993f6049b5b707f5c85cccc501 = new MenuItem(header: "TextEditor test") {  };
            PopupWindow id_34e83ac23d6d40809cfb17dfc31710d8 = new PopupWindow(title: "") { Height = 720, Width = 1280, Resize = SizeToContent.WidthAndHeight };
            Box id_9ebb880b33854eba8382f09643125f8b = new Box() { Width = 100, Height = 100 };
            TextEditor id_f094a45b511f4effb85d5cb0e2233b2a = new TextEditor() { Width = 1280, Height = 720 };
            DataFlowConnector<string> id_9a32610df90d4f3fb6898823f828a68e = new DataFlowConnector<string>() {  };
            FolderWatcher projectFolderWatcher = new FolderWatcher() { InstanceName = "projectFolderWatcher", RootPath = "", Filter = "*.cs", WatchSubdirectories = true, PathRegex = @".*\.cs$" };
            Apply<string, object> id_b78db9e2d0aa4f37a80bec98e5ee2dec = new Apply<string, object>() { Lambda = input =>{var newModel = abstractionModelManager.CreateAbstractionModelFromPath(input);foreach (var node in mainGraph.Nodes){var alaNode = node as ALANode;if (alaNode.Model.Type != newModel.Type) continue;abstractionModelManager.UpdateAbstractionModel(newModel, alaNode.Model);alaNode.UpdateUI();}return input;} };
            ConvertToEvent<object> id_7fd140ff9c644b0b80cc85eac08a1086 = new ConvertToEvent<object>() {  };
            MouseButtonEvent id_c08e61cdd76b4bfc8bafacde44cf3a54 = new MouseButtonEvent(eventName: "MouseRightButtonDown") { ExtractSender = null, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            ApplyAction<object> id_cb3ce4a113274fbb8932c64f9309c895 = new ApplyAction<object>() { Lambda = input =>{Mouse.Capture(input as WPFCanvas);stateTransition.Update(Enums.DiagramMode.Idle);} };
            MouseButtonEvent id_ed490444f6e549c789bd85329338f17c = new MouseButtonEvent(eventName: "MouseRightButtonUp") { ExtractSender = null, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            ApplyAction<object> id_07063bdf9be146d19e0b944b767db391 = new ApplyAction<object>() { Lambda = input =>{if (Mouse.Captured?.Equals(input) ?? false) Mouse.Capture(null);stateTransition.Update(Enums.DiagramMode.Idle);} };
            StateChangeListener id_45c0feb63c144b52a90af3f80a1648b6 = new StateChangeListener() { StateTransition = stateTransition, CurrentStateShouldMatch = Enums.DiagramMode.All };
            Apply<Tuple<Enums.DiagramMode, Enums.DiagramMode>, bool> id_fba00f72a702489d8dde9c4add237164 = new Apply<Tuple<Enums.DiagramMode, Enums.DiagramMode>, bool>() { Lambda = input =>{return input.Item1 == Enums.DiagramMode.AwaitingPortSelection &&input.Item2 == Enums.DiagramMode.Idle;} };
            IfElse id_d3a595f9823b4986aed1fc52453b77f7 = new IfElse() {  };
            EventConnector id_6aecfeefc6e247b29dba50345bfe9844 = new EventConnector() {  };
            Apply<object, object> createAndPaintALAWire = new Apply<object, object>() { InstanceName = "createAndPaintALAWire", Lambda = input =>{var source = mainGraph.Get("SelectedNode") as ALANode;var destination = input as ALANode;var sourcePort = source.GetSelectedPort(inputPort: false);var destinationPort = destination.GetSelectedPort(inputPort: true);var wire = new ALAWire(){Graph = mainGraph,Canvas = mainCanvas,Source = source,Destination = destination,SourcePort = sourcePort,DestinationPort = destinationPort,StateTransition = stateTransition};mainGraph.AddEdge(wire);wire.Paint();return wire;} };
            KeyEvent id_314af1cea5a64040b02e30a1c45bcaff = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.Delete } };
            EventLambda id_51db9e171b3d42ac9536ebfbb1f91752 = new EventLambda() { Lambda = () =>{var selectedNode = mainGraph.Get("SelectedNode") as ALANode;if (selectedNode == null) return;selectedNode.Delete(deleteAttachedWires: true);} };
            MenuItem id_c8bc099d934e4673b48421cdc5614ac4 = new MenuItem(header: "Refresh") {  };
            Data<AbstractionModel> id_e7047f8099354522a3b28e95388258b5 = new Data<AbstractionModel>() { Lambda = () => abstractionModelManager.GetAbstractionModel(abstractionModelManager.GetAbstractionTypes().FirstOrDefault()) };
            Apply<AbstractionModel, object> id_0cda5390bfa045dd998b9e05c6e6e55d = new Apply<AbstractionModel, object>() { Lambda = createNewALANode.Lambda };
            ApplyAction<object> id_c5b238fd466e4f489d46a7f375920f79 = new ApplyAction<object>() { Lambda = input =>{var render = (input as ALANode).Render;var mousePos = Mouse.GetPosition(mainCanvas);WPFCanvas.SetLeft(render, mousePos.X);WPFCanvas.SetTop(render, mousePos.Y);mainGraph.Set("LatestNode", input);if (mainGraph.Get("SelectedNode") == null){mainGraph.Set("SelectedNode", input);}mainGraph.Roots.Add(input);} };
            MenuItem id_a6d568dd3330419c8f15b73329dd2412 = new MenuItem(header: "Open Code File") {  };
            FileBrowser id_2a25c684a6fe4f748e494f6d26f861a6 = new FileBrowser() { Mode = "Open" };
            FileReader id_a50a0a63a1864532a06d6353c01d1ccd = new FileReader() {  };
            CreateDiagramFromCode id_49b16d830c454c9fa33be2a8fd2f3771 = new CreateDiagramFromCode() { Graph = mainGraph, Canvas = mainCanvas, ModelManager = abstractionModelManager, StateTransition = stateTransition };
            EventConnector id_a7dcad9d3eff4295ba0fc7c007f2b069 = new EventConnector() {  };
            // END AUTO-GENERATED INSTANTIATIONS FOR Application.xmind

            // BEGIN AUTO-GENERATED WIRING FOR Application.xmind
            mainWindow.WireTo(id_5196db19defd4c27b1fc79050fa11ae3, "iuiStructure");
            mainWindow.WireTo(id_6aecfeefc6e247b29dba50345bfe9844, "appStart");
            id_5196db19defd4c27b1fc79050fa11ae3.WireTo(id_2b4c8b103e544d2ea4f82e3ecb1e838c, "children");
            id_5196db19defd4c27b1fc79050fa11ae3.WireTo(id_127a8ab9a2e54b7fb4800dd8ff00e21a, "children");
            id_127a8ab9a2e54b7fb4800dd8ff00e21a.WireTo(id_44c261ac75c14d6080ce4a5ecf3d6b3d, "eventHandlers");
            id_127a8ab9a2e54b7fb4800dd8ff00e21a.WireTo(id_4ad564f187264b64a46d002afc89fa55, "eventHandlers");
            id_127a8ab9a2e54b7fb4800dd8ff00e21a.WireTo(id_f1c7a5340568448f926c6d691c06309d, "eventHandlers");
            id_127a8ab9a2e54b7fb4800dd8ff00e21a.WireTo(id_c08e61cdd76b4bfc8bafacde44cf3a54, "eventHandlers");
            id_127a8ab9a2e54b7fb4800dd8ff00e21a.WireTo(id_ed490444f6e549c789bd85329338f17c, "eventHandlers");
            id_127a8ab9a2e54b7fb4800dd8ff00e21a.WireTo(id_314af1cea5a64040b02e30a1c45bcaff, "eventHandlers");
            id_127a8ab9a2e54b7fb4800dd8ff00e21a.WireTo(id_eca28d2f9c284892bf22cd2fffb8d6c8, "contextMenu");
            id_44c261ac75c14d6080ce4a5ecf3d6b3d.WireTo(id_d35fc547579d4a7fbf3965e87cc2b4a3, "eventHappened");
            id_eca28d2f9c284892bf22cd2fffb8d6c8.WireTo(id_666c37a95fe7446384c340bd8b3ec738, "children");
            id_eca28d2f9c284892bf22cd2fffb8d6c8.WireTo(id_c8bc099d934e4673b48421cdc5614ac4, "children");
            id_666c37a95fe7446384c340bd8b3ec738.WireTo(id_e7047f8099354522a3b28e95388258b5, "clickedEvent");
            id_d35fc547579d4a7fbf3965e87cc2b4a3.WireTo(id_4079402332d143cd92ba41c222d2486b, "fanoutList");
            id_d35fc547579d4a7fbf3965e87cc2b4a3.WireTo(layoutDiagram, "complete");
            getFirstRoot.WireTo(id_a359f6f02cbd46b3aad98b95992c9d7f, "dataOutput");
            layoutDiagram.WireTo(getFirstRoot, "fanoutList");
            id_a359f6f02cbd46b3aad98b95992c9d7f.WireTo(id_361c514b1c034dae84a2fb23f6c29da7, "fanoutList");
            id_a359f6f02cbd46b3aad98b95992c9d7f.WireTo(id_c7a4497282484a8e860365555522d214, "fanoutList");
            id_4ad564f187264b64a46d002afc89fa55.WireTo(layoutDiagram, "eventHappened");
            createNewALANode.WireTo(createAndPaintALAWire, "output");
            id_2b4c8b103e544d2ea4f82e3ecb1e838c.WireTo(id_8b2b2dd939ab4567881dbd1af062ed99, "children");
            id_2b4c8b103e544d2ea4f82e3ecb1e838c.WireTo(id_9910d0db14ff46229ed483d25504ed04, "children");
            id_8b2b2dd939ab4567881dbd1af062ed99.WireTo(id_2f40a513f14c4f2e8ce51fa5da90ee15, "children");
            id_8b2b2dd939ab4567881dbd1af062ed99.WireTo(id_a6d568dd3330419c8f15b73329dd2412, "children");
            id_2f40a513f14c4f2e8ce51fa5da90ee15.WireTo(id_cb396a05c2dc42749faafec1b9e43515, "clickedEvent");
            id_cb396a05c2dc42749faafec1b9e43515.WireTo(id_9a32610df90d4f3fb6898823f828a68e, "selectedFolderPathOutput");
            id_f224783b3f1f4c7a935da47776fde447.WireTo(id_f4ed33bb9872401b9084bdc1c5ed8a36, "foundFiles");
            id_f4ed33bb9872401b9084bdc1c5ed8a36.WireTo(id_58a8797b59f346acad2baf135887a328, "output");
            id_58a8797b59f346acad2baf135887a328.WireTo(id_a9d4c1a9c469426f87f8c206268d285d, "elementOutput");
            id_4079402332d143cd92ba41c222d2486b.WireTo(id_7df1901cd99244d6b373eb8fc8cd01a7, "dataOutput");
            id_7df1901cd99244d6b373eb8fc8cd01a7.WireTo(createNewALANode, "output");
            id_f5285c818a39457ea6aab586729837a1.WireTo(id_9a32610df90d4f3fb6898823f828a68e, "dataOutput");
            id_f1c7a5340568448f926c6d691c06309d.WireTo(id_58d2b145b68f4cccbdd2dac80b1d630f, "senderOutput");
            id_9910d0db14ff46229ed483d25504ed04.WireTo(id_49a6c9993f6049b5b707f5c85cccc501, "children");
            id_49a6c9993f6049b5b707f5c85cccc501.WireTo(id_34e83ac23d6d40809cfb17dfc31710d8, "clickedEvent");
            id_34e83ac23d6d40809cfb17dfc31710d8.WireTo(id_9ebb880b33854eba8382f09643125f8b, "children");
            id_9ebb880b33854eba8382f09643125f8b.WireTo(id_f094a45b511f4effb85d5cb0e2233b2a, "uiLayout");
            id_9a32610df90d4f3fb6898823f828a68e.WireTo(id_f224783b3f1f4c7a935da47776fde447, "fanoutList");
            id_9a32610df90d4f3fb6898823f828a68e.WireTo(projectFolderWatcher, "fanoutList");
            projectFolderWatcher.WireTo(id_b78db9e2d0aa4f37a80bec98e5ee2dec, "changedFile");
            id_b78db9e2d0aa4f37a80bec98e5ee2dec.WireTo(id_7fd140ff9c644b0b80cc85eac08a1086, "output");
            id_7fd140ff9c644b0b80cc85eac08a1086.WireTo(id_c7a4497282484a8e860365555522d214, "eventOutput");
            id_c08e61cdd76b4bfc8bafacde44cf3a54.WireTo(id_cb3ce4a113274fbb8932c64f9309c895, "argsOutput");
            id_ed490444f6e549c789bd85329338f17c.WireTo(id_07063bdf9be146d19e0b944b767db391, "argsOutput");
            id_45c0feb63c144b52a90af3f80a1648b6.WireTo(id_fba00f72a702489d8dde9c4add237164, "transitionOutput");
            id_fba00f72a702489d8dde9c4add237164.WireTo(id_d3a595f9823b4986aed1fc52453b77f7, "output");
            id_d3a595f9823b4986aed1fc52453b77f7.WireTo(layoutDiagram, "ifOutput");
            id_6aecfeefc6e247b29dba50345bfe9844.WireTo(id_f5285c818a39457ea6aab586729837a1, "fanoutList");
            id_6aecfeefc6e247b29dba50345bfe9844.WireTo(id_45c0feb63c144b52a90af3f80a1648b6, "fanoutList");
            id_6aecfeefc6e247b29dba50345bfe9844.WireTo(id_a7dcad9d3eff4295ba0fc7c007f2b069, "complete");
            id_314af1cea5a64040b02e30a1c45bcaff.WireTo(id_51db9e171b3d42ac9536ebfbb1f91752, "eventHappened");
            id_c8bc099d934e4673b48421cdc5614ac4.WireTo(layoutDiagram, "clickedEvent");
            id_e7047f8099354522a3b28e95388258b5.WireTo(id_0cda5390bfa045dd998b9e05c6e6e55d, "dataOutput");
            id_0cda5390bfa045dd998b9e05c6e6e55d.WireTo(id_c5b238fd466e4f489d46a7f375920f79, "output");
            id_a6d568dd3330419c8f15b73329dd2412.WireTo(id_2a25c684a6fe4f748e494f6d26f861a6, "clickedEvent");
            id_2a25c684a6fe4f748e494f6d26f861a6.WireTo(id_a50a0a63a1864532a06d6353c01d1ccd, "selectedFilePathOutput");
            id_a50a0a63a1864532a06d6353c01d1ccd.WireTo(id_49b16d830c454c9fa33be2a8fd2f3771, "fileContentOutput");
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






























































































































































