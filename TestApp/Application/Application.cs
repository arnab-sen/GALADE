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
            Vertical id_2da8b6d1ad0f49cba0edd9dc7c0750fe = new Vertical() {  };
            CanvasDisplay id_e8bc2db7e79b432bb2f7735875fce208 = new CanvasDisplay() { Width = 1920, Height = 600, Background = Brushes.White, StateTransition = stateTransition, Canvas = mainCanvas };
            KeyEvent id_d2f2c0b5446d4d1788046072ccc597ff = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.A }, Condition = args => mainGraph.Get("SelectedNode") != null&& stateTransition.CurrentStateMatches(Enums.DiagramMode.IdleSelected) };
            ContextMenu id_faa81053f0ce47fd91c161d23c9799ed = new ContextMenu() {  };
            MenuItem id_674faa02d35243e68b2621dd34c16eae = new MenuItem(header: "Add root") {  };
            EventConnector id_8e5948815a844cf48846079e7b2b614e = new EventConnector() {  };
            Data<ALANode> getFirstRoot = new Data<ALANode>() { InstanceName = "getFirstRoot", Lambda = () => mainGraph.Roots.First() as ALANode };
            RightTreeLayout<ALANode> id_c58a050ed0a04e6f876839cdbb541735 = new RightTreeLayout<ALANode>() { GetID = n => n.Id, GetWidth = n => (n.Render as FrameworkElement).ActualWidth, GetHeight = n => (n.Render as FrameworkElement).ActualHeight, SetX = (n, x) => WPFCanvas.SetLeft(n.Render, x), SetY = (n, y) => WPFCanvas.SetTop(n.Render, y), GetChildren = n => mainGraph.Edges.Where(e => e is ALAWire wire && wire.Source != null && wire.Destination != null && wire.Source == n).Select(e => ((e as ALAWire).Destination) as ALANode), HorizontalGap = 100, VerticalGap = 20, InitialX = 50, InitialY = 50 };
            EventConnector layoutDiagram = new EventConnector() { InstanceName = "layoutDiagram" };
            DataFlowConnector<ALANode> id_0776dda404144a2d8a98780b55581099 = new DataFlowConnector<ALANode>() {  };
            ApplyAction<ALANode> id_4895a826415745c99787bd353edbd965 = new ApplyAction<ALANode>() { Lambda = node =>{Dispatcher.CurrentDispatcher.Invoke(() => {var edges = mainGraph.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);} };
            KeyEvent id_d86e7d84c75c4f1396642de272a5b2f0 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.R }, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            Apply<AbstractionModel, object> createNewALANode = new Apply<AbstractionModel, object>() { InstanceName = "createNewALANode", Lambda = input => {var node = new ALANode();node.Model = input;node.Graph = mainGraph;node.Canvas = mainCanvas;node.StateTransition = stateTransition;node.AvailableDomainAbstractions.AddRange( abstractionModelManager.GetAbstractionTypes());node.TypeChanged += newType => {abstractionModelManager.UpdateAbstractionModel(abstractionModelManager.GetAbstractionModel(newType),node.Model);node.UpdateUI();Dispatcher.CurrentDispatcher.Invoke(() => {var edges = mainGraph.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);};mainGraph.AddNode(node);node.CreateInternals();mainCanvas.Children.Add(node.Render);return node;} };
            MenuBar id_8b8033e534a34eefab99ec59bf45342b = new MenuBar() {  };
            MenuItem id_1dbb90354cdb4791987fb1eed89bc381 = new MenuItem(header: "File") {  };
            MenuItem id_5acd9109147e46cb8d3d2da9fe142cdc = new MenuItem(header: "Open Project") {  };
            FolderBrowser id_142d32d73f194ac8949a07beb1c607a7 = new FolderBrowser() { Description = "" };
            DirectorySearch id_d7bff8d840724aecbc92e64e410c6cba = new DirectorySearch(directoriesToFind: new string[] { "DomainAbstractions", "ProgrammingParadigms", "RequirementsAbstractions" }) { FilenameFilter = "*.cs" };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_d7e3cf5c63c44e02b1d32645ffbbaceb = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("DomainAbstractions")){list = input["DomainAbstractions"];}return list;} };
            ForEach<string> id_5e809b1ebddd400eac12beca55cefcf1 = new ForEach<string>() {  };
            ApplyAction<string> id_c4023a3dde2d4e12a7642161a7c2247b = new ApplyAction<string>() { Lambda = input =>{abstractionModelManager.CreateAbstractionModelFromPath(input);} };
            Data<string> id_492f7fe0e62e4a8a8ed2ce181d1a439a = new Data<string>() { storedData = "Apply" };
            Apply<string, AbstractionModel> id_87edf0f8b7254b6cafa5bf951b65c187 = new Apply<string, AbstractionModel>() { Lambda = input =>{return abstractionModelManager.GetAbstractionModel(input);} };
            Data<string> id_9b099144a52841379ce31c86ba47bc25 = new Data<string>() { storedData = @"D:\Coding\C#\Projects\GALADE\ALACore" };
            KeyEvent id_549591978fe14e83b247b05262850ef1 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.Enter } };
            ApplyAction<object> id_89e4545b9cd24bf5954e792359ad00f4 = new ApplyAction<object>() { Lambda = input =>{(input as WPFCanvas).Focus();} };
            MenuItem id_3904f34edcac4898bae00222c9c1ce0e = new MenuItem(header: "Debug") {  };
            MenuItem id_6e5d7fbdc4e64816aec378c70b7178ec = new MenuItem(header: "TextEditor test") {  };
            PopupWindow id_ac696fe1ae424890840211de0274eb8d = new PopupWindow(title: "") { Height = 720, Width = 1280, Resize = SizeToContent.WidthAndHeight };
            Box id_ec285fd4d09f4e71b23c5d4174684f59 = new Box() { Width = 100, Height = 100 };
            TextEditor id_d84e4b152e78454cb5f1fd2fcef8a31b = new TextEditor() { Width = 1280, Height = 720 };
            DataFlowConnector<string> id_cf25687187044f9b84c8c0a5de32cd6b = new DataFlowConnector<string>() {  };
            FolderWatcher projectFolderWatcher = new FolderWatcher() { InstanceName = "projectFolderWatcher", RootPath = "", Filter = "*.cs", WatchSubdirectories = true, PathRegex = @".*\.cs$" };
            Apply<string, object> id_38528a7c9b0245618074281f543563e6 = new Apply<string, object>() { Lambda = input =>{var newModel = abstractionModelManager.CreateAbstractionModelFromPath(input);foreach (var node in mainGraph.Nodes){var alaNode = node as ALANode;if (alaNode.Model.Type != newModel.Type) continue;abstractionModelManager.UpdateAbstractionModel(newModel, alaNode.Model);alaNode.UpdateUI();}return input;} };
            ConvertToEvent<object> id_1f3483a11204417ba26ed50d1410feae = new ConvertToEvent<object>() {  };
            MouseButtonEvent id_1c03d50c931c48e0bdb4df8522a9d1db = new MouseButtonEvent(eventName: "MouseRightButtonDown") { ExtractSender = null, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            ApplyAction<object> id_3e66a4a5a76d468a9d56645f928eec10 = new ApplyAction<object>() { Lambda = input =>{Mouse.Capture(input as WPFCanvas);stateTransition.Update(Enums.DiagramMode.Idle);} };
            MouseButtonEvent id_7053e2dbabc442dbac35b40692cd49cd = new MouseButtonEvent(eventName: "MouseRightButtonUp") { ExtractSender = null, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            ApplyAction<object> id_21e9c97199284dce9ce4282a734d98ad = new ApplyAction<object>() { Lambda = input =>{if (Mouse.Captured?.Equals(input) ?? false) Mouse.Capture(null);stateTransition.Update(Enums.DiagramMode.Idle);} };
            StateChangeListener id_3176cf24056c4424a43416e165a48bb7 = new StateChangeListener() { StateTransition = stateTransition, CurrentStateShouldMatch = Enums.DiagramMode.All };
            Apply<Tuple<Enums.DiagramMode, Enums.DiagramMode>, bool> id_fb0f8d17d5d84b0ab66f5af5018a715d = new Apply<Tuple<Enums.DiagramMode, Enums.DiagramMode>, bool>() { Lambda = input =>{return input.Item1 == Enums.DiagramMode.AwaitingPortSelection &&input.Item2 == Enums.DiagramMode.Idle;} };
            IfElse id_a07a234b84a142ef9107a407ae4ba18b = new IfElse() {  };
            EventConnector id_40280c3a045945aeafae8fd83c166f27 = new EventConnector() {  };
            Apply<object, object> createAndPaintALAWire = new Apply<object, object>() { InstanceName = "createAndPaintALAWire", Lambda = input =>{var source = mainGraph.Get("SelectedNode") as ALANode;var destination = input as ALANode;var sourcePort = source.GetSelectedPort(inputPort: false);var destinationPort = destination.GetSelectedPort(inputPort: true);var wire = new ALAWire(){Graph = mainGraph,Canvas = mainCanvas,Source = source,Destination = destination,SourcePort = sourcePort,DestinationPort = destinationPort,StateTransition = stateTransition};mainGraph.AddEdge(wire);wire.Paint();return wire;} };
            KeyEvent id_b0dc7fd4262a46a4a812680a4099db58 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.Delete } };
            EventLambda id_6e8158109a4146f08537cd47b08046f4 = new EventLambda() { Lambda = () =>{var selectedNode = mainGraph.Get("SelectedNode") as ALANode;if (selectedNode == null) return;selectedNode.Delete(deleteAttachedWires: true);} };
            MenuItem id_21849188a3784f418b469846db6b1a5a = new MenuItem(header: "Refresh") {  };
            Data<AbstractionModel> id_aefb4e7d3ea94d3ebca7dbac0c182fc0 = new Data<AbstractionModel>() { Lambda = () => abstractionModelManager.GetAbstractionModel(abstractionModelManager.GetAbstractionTypes().FirstOrDefault()) };
            Apply<AbstractionModel, object> id_6c5404023b2a488aa6482aa3179ff9bd = new Apply<AbstractionModel, object>() { Lambda = createNewALANode.Lambda };
            ApplyAction<object> id_ab6932b6940442c4945854be2887cef8 = new ApplyAction<object>() { Lambda = input =>{var render = (input as ALANode).Render;var mousePos = Mouse.GetPosition(mainCanvas);WPFCanvas.SetLeft(render, mousePos.X);WPFCanvas.SetTop(render, mousePos.Y);mainGraph.Set("LatestNode", input);if (mainGraph.Get("SelectedNode") == null){mainGraph.Set("SelectedNode", input);}mainGraph.Roots.Add(input);} };
            MenuItem id_e5faccb80de644ba93e8b1847f5bf0c9 = new MenuItem(header: "Open Code File") {  };
            FileBrowser id_7c10e4dc5e1a4e4097510041d0a70965 = new FileBrowser() { Mode = "Open" };
            FileReader id_ca795d6bf5314f8180df502b00e899ac = new FileReader() {  };
            CreateDiagramFromCode id_7fcd5fc8faa34d44a8783e787b568391 = new CreateDiagramFromCode() { Graph = mainGraph, Canvas = mainCanvas, ModelManager = abstractionModelManager, StateTransition = stateTransition };
            EventConnector id_9aaf59134b9c49c5a2a6e71d080c6847 = new EventConnector() {  };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_187e6737501f460ab4bf98a1c55d523a = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("ProgrammingParadigms")){list = input["ProgrammingParadigms"];}return list;} };
            ForEach<string> id_3ace8f29282640b3972addc172b90656 = new ForEach<string>() {  };
            ApplyAction<string> id_d0d1aab3497b43eb999f382bc3f84cd0 = new ApplyAction<string>() { Lambda = input =>{abstractionModelManager.CreateAbstractionModelFromPath(input);} };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_b71d766a2c5f4fb89b1bf7895d60d099 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("RequirementsAbstractions")){list = input["RequirementsAbstractions"];}return list;} };
            ForEach<string> id_bebf96f79e3a4890b217250400f7b25d = new ForEach<string>() {  };
            ApplyAction<string> id_89eb0707bd0043078437d5ae7a51c5ca = new ApplyAction<string>() { Lambda = input =>{abstractionModelManager.CreateAbstractionModelFromPath(input);} };
            DataFlowConnector<Dictionary<string, List<string>>> id_3b34fce839ad47fab0c3eaccd41f3f3a = new DataFlowConnector<Dictionary<string, List<string>>>() {  };
            // END AUTO-GENERATED INSTANTIATIONS FOR Application.xmind

            // BEGIN AUTO-GENERATED WIRING FOR Application.xmind
            mainWindow.WireTo(id_2da8b6d1ad0f49cba0edd9dc7c0750fe, "iuiStructure");
            mainWindow.WireTo(id_40280c3a045945aeafae8fd83c166f27, "appStart");
            id_2da8b6d1ad0f49cba0edd9dc7c0750fe.WireTo(id_8b8033e534a34eefab99ec59bf45342b, "children");
            id_2da8b6d1ad0f49cba0edd9dc7c0750fe.WireTo(id_e8bc2db7e79b432bb2f7735875fce208, "children");
            id_e8bc2db7e79b432bb2f7735875fce208.WireTo(id_d2f2c0b5446d4d1788046072ccc597ff, "eventHandlers");
            id_e8bc2db7e79b432bb2f7735875fce208.WireTo(id_d86e7d84c75c4f1396642de272a5b2f0, "eventHandlers");
            id_e8bc2db7e79b432bb2f7735875fce208.WireTo(id_549591978fe14e83b247b05262850ef1, "eventHandlers");
            id_e8bc2db7e79b432bb2f7735875fce208.WireTo(id_1c03d50c931c48e0bdb4df8522a9d1db, "eventHandlers");
            id_e8bc2db7e79b432bb2f7735875fce208.WireTo(id_7053e2dbabc442dbac35b40692cd49cd, "eventHandlers");
            id_e8bc2db7e79b432bb2f7735875fce208.WireTo(id_b0dc7fd4262a46a4a812680a4099db58, "eventHandlers");
            id_e8bc2db7e79b432bb2f7735875fce208.WireTo(id_faa81053f0ce47fd91c161d23c9799ed, "contextMenu");
            id_d2f2c0b5446d4d1788046072ccc597ff.WireTo(id_8e5948815a844cf48846079e7b2b614e, "eventHappened");
            id_faa81053f0ce47fd91c161d23c9799ed.WireTo(id_674faa02d35243e68b2621dd34c16eae, "children");
            id_faa81053f0ce47fd91c161d23c9799ed.WireTo(id_21849188a3784f418b469846db6b1a5a, "children");
            id_674faa02d35243e68b2621dd34c16eae.WireTo(id_aefb4e7d3ea94d3ebca7dbac0c182fc0, "clickedEvent");
            id_8e5948815a844cf48846079e7b2b614e.WireTo(id_492f7fe0e62e4a8a8ed2ce181d1a439a, "fanoutList");
            id_8e5948815a844cf48846079e7b2b614e.WireTo(layoutDiagram, "complete");
            getFirstRoot.WireTo(id_0776dda404144a2d8a98780b55581099, "dataOutput");
            layoutDiagram.WireTo(getFirstRoot, "fanoutList");
            id_0776dda404144a2d8a98780b55581099.WireTo(id_c58a050ed0a04e6f876839cdbb541735, "fanoutList");
            id_0776dda404144a2d8a98780b55581099.WireTo(id_4895a826415745c99787bd353edbd965, "fanoutList");
            id_d86e7d84c75c4f1396642de272a5b2f0.WireTo(layoutDiagram, "eventHappened");
            createNewALANode.WireTo(createAndPaintALAWire, "output");
            id_8b8033e534a34eefab99ec59bf45342b.WireTo(id_1dbb90354cdb4791987fb1eed89bc381, "children");
            id_8b8033e534a34eefab99ec59bf45342b.WireTo(id_3904f34edcac4898bae00222c9c1ce0e, "children");
            id_1dbb90354cdb4791987fb1eed89bc381.WireTo(id_5acd9109147e46cb8d3d2da9fe142cdc, "children");
            id_1dbb90354cdb4791987fb1eed89bc381.WireTo(id_e5faccb80de644ba93e8b1847f5bf0c9, "children");
            id_5acd9109147e46cb8d3d2da9fe142cdc.WireTo(id_142d32d73f194ac8949a07beb1c607a7, "clickedEvent");
            id_142d32d73f194ac8949a07beb1c607a7.WireTo(id_cf25687187044f9b84c8c0a5de32cd6b, "selectedFolderPathOutput");
            id_d7bff8d840724aecbc92e64e410c6cba.WireTo(id_3b34fce839ad47fab0c3eaccd41f3f3a, "foundFiles");
            id_d7e3cf5c63c44e02b1d32645ffbbaceb.WireTo(id_5e809b1ebddd400eac12beca55cefcf1, "output");
            id_5e809b1ebddd400eac12beca55cefcf1.WireTo(id_c4023a3dde2d4e12a7642161a7c2247b, "elementOutput");
            id_492f7fe0e62e4a8a8ed2ce181d1a439a.WireTo(id_87edf0f8b7254b6cafa5bf951b65c187, "dataOutput");
            id_87edf0f8b7254b6cafa5bf951b65c187.WireTo(createNewALANode, "output");
            id_9b099144a52841379ce31c86ba47bc25.WireTo(id_cf25687187044f9b84c8c0a5de32cd6b, "dataOutput");
            id_549591978fe14e83b247b05262850ef1.WireTo(id_89e4545b9cd24bf5954e792359ad00f4, "senderOutput");
            id_3904f34edcac4898bae00222c9c1ce0e.WireTo(id_6e5d7fbdc4e64816aec378c70b7178ec, "children");
            id_6e5d7fbdc4e64816aec378c70b7178ec.WireTo(id_ac696fe1ae424890840211de0274eb8d, "clickedEvent");
            id_ac696fe1ae424890840211de0274eb8d.WireTo(id_ec285fd4d09f4e71b23c5d4174684f59, "children");
            id_ec285fd4d09f4e71b23c5d4174684f59.WireTo(id_d84e4b152e78454cb5f1fd2fcef8a31b, "uiLayout");
            id_cf25687187044f9b84c8c0a5de32cd6b.WireTo(id_d7bff8d840724aecbc92e64e410c6cba, "fanoutList");
            id_cf25687187044f9b84c8c0a5de32cd6b.WireTo(projectFolderWatcher, "fanoutList");
            projectFolderWatcher.WireTo(id_38528a7c9b0245618074281f543563e6, "changedFile");
            id_38528a7c9b0245618074281f543563e6.WireTo(id_1f3483a11204417ba26ed50d1410feae, "output");
            id_1f3483a11204417ba26ed50d1410feae.WireTo(id_4895a826415745c99787bd353edbd965, "eventOutput");
            id_1c03d50c931c48e0bdb4df8522a9d1db.WireTo(id_3e66a4a5a76d468a9d56645f928eec10, "argsOutput");
            id_7053e2dbabc442dbac35b40692cd49cd.WireTo(id_21e9c97199284dce9ce4282a734d98ad, "argsOutput");
            id_3176cf24056c4424a43416e165a48bb7.WireTo(id_fb0f8d17d5d84b0ab66f5af5018a715d, "transitionOutput");
            id_fb0f8d17d5d84b0ab66f5af5018a715d.WireTo(id_a07a234b84a142ef9107a407ae4ba18b, "output");
            id_a07a234b84a142ef9107a407ae4ba18b.WireTo(layoutDiagram, "ifOutput");
            id_40280c3a045945aeafae8fd83c166f27.WireTo(id_9b099144a52841379ce31c86ba47bc25, "fanoutList");
            id_40280c3a045945aeafae8fd83c166f27.WireTo(id_3176cf24056c4424a43416e165a48bb7, "fanoutList");
            id_40280c3a045945aeafae8fd83c166f27.WireTo(id_9aaf59134b9c49c5a2a6e71d080c6847, "complete");
            id_b0dc7fd4262a46a4a812680a4099db58.WireTo(id_6e8158109a4146f08537cd47b08046f4, "eventHappened");
            id_21849188a3784f418b469846db6b1a5a.WireTo(layoutDiagram, "clickedEvent");
            id_aefb4e7d3ea94d3ebca7dbac0c182fc0.WireTo(id_6c5404023b2a488aa6482aa3179ff9bd, "dataOutput");
            id_6c5404023b2a488aa6482aa3179ff9bd.WireTo(id_ab6932b6940442c4945854be2887cef8, "output");
            id_e5faccb80de644ba93e8b1847f5bf0c9.WireTo(id_7c10e4dc5e1a4e4097510041d0a70965, "clickedEvent");
            id_7c10e4dc5e1a4e4097510041d0a70965.WireTo(id_ca795d6bf5314f8180df502b00e899ac, "selectedFilePathOutput");
            id_ca795d6bf5314f8180df502b00e899ac.WireTo(id_7fcd5fc8faa34d44a8783e787b568391, "fileContentOutput");
            id_187e6737501f460ab4bf98a1c55d523a.WireTo(id_3ace8f29282640b3972addc172b90656, "output");
            id_3ace8f29282640b3972addc172b90656.WireTo(id_d0d1aab3497b43eb999f382bc3f84cd0, "elementOutput");
            id_b71d766a2c5f4fb89b1bf7895d60d099.WireTo(id_bebf96f79e3a4890b217250400f7b25d, "output");
            id_bebf96f79e3a4890b217250400f7b25d.WireTo(id_89eb0707bd0043078437d5ae7a51c5ca, "elementOutput");
            id_3b34fce839ad47fab0c3eaccd41f3f3a.WireTo(id_d7e3cf5c63c44e02b1d32645ffbbaceb, "fanoutList");
            id_3b34fce839ad47fab0c3eaccd41f3f3a.WireTo(id_187e6737501f460ab4bf98a1c55d523a, "fanoutList");
            id_3b34fce839ad47fab0c3eaccd41f3f3a.WireTo(id_b71d766a2c5f4fb89b1bf7895d60d099, "fanoutList");
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






















































































































































































