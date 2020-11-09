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
            Vertical id_9af884fdd740441b9807661935bc59c5 = new Vertical() {  };
            CanvasDisplay id_06da5c839f3d434f8d85b919a260ffde = new CanvasDisplay() { Width = 1920, Height = 600, Background = Brushes.White, StateTransition = stateTransition, Canvas = mainCanvas };
            KeyEvent id_4f33f0ca8c5940aaa9f6e86976fcff50 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.A }, Condition = args => mainGraph.Get("SelectedNode") != null&& stateTransition.CurrentStateMatches(Enums.DiagramMode.IdleSelected) };
            ContextMenu id_61eb9dab031a4f7eae4fe0e144b919c9 = new ContextMenu() {  };
            MenuItem id_2d32b91d2ab1435cafe28988425376d0 = new MenuItem(header: "Add root") {  };
            EventConnector id_05664e895ab748888e1938ef6bfe89d0 = new EventConnector() {  };
            Data<ALANode> getFirstRoot = new Data<ALANode>() { InstanceName = "getFirstRoot", Lambda = () => mainGraph.Roots.FirstOrDefault() as ALANode };
            RightTreeLayout<ALANode> id_5ebc05846871488d94e745d60e073ae1 = new RightTreeLayout<ALANode>() { GetID = n => n.Id, GetWidth = n => n.Width, GetHeight = n => n.Height, SetX = (n, x) => n.PositionX = x, SetY = (n, y) => n.PositionY = y, GetChildren = n => mainGraph.Edges.Where(e => e is ALAWire wire && wire.Source != null && wire.Destination != null && wire.Source == n).Select(e => ((e as ALAWire).Destination) as ALANode), HorizontalGap = 100, VerticalGap = 20, InitialX = 50, InitialY = 50 };
            EventConnector layoutDiagram = new EventConnector() { InstanceName = "layoutDiagram" };
            DataFlowConnector<ALANode> id_d4e13fc2e593411581d05445df76680a = new DataFlowConnector<ALANode>() {  };
            ApplyAction<ALANode> id_8789d0f39d654e0491be09c47d3a5b2f = new ApplyAction<ALANode>() { Lambda = node =>{Dispatcher.CurrentDispatcher.Invoke(() => {var edges = mainGraph.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);} };
            KeyEvent id_2e77dfeef6784689b0278b810a4ac1bd = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.R }, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            Apply<AbstractionModel, object> createNewALANode = new Apply<AbstractionModel, object>() { InstanceName = "createNewALANode", Lambda = input => {var node = new ALANode();node.Model = input;node.Graph = mainGraph;node.Canvas = mainCanvas;node.StateTransition = stateTransition;node.AvailableDomainAbstractions.AddRange( abstractionModelManager.GetAbstractionTypes());node.TypeChanged += newType => {abstractionModelManager.UpdateAbstractionModel(abstractionModelManager.GetAbstractionModel(newType),node.Model);node.UpdateUI();Dispatcher.CurrentDispatcher.Invoke(() => {var edges = mainGraph.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);};mainGraph.AddNode(node);node.CreateInternals();mainCanvas.Children.Add(node.Render);return node;} };
            MenuBar id_d8118e87cb804432aaa99fdda34d898c = new MenuBar() {  };
            MenuItem id_1b2cd1cf3cd74e8e9ae1007ea2358ab0 = new MenuItem(header: "File") {  };
            MenuItem id_c95a8721239446eb8639ec850a919e7d = new MenuItem(header: "Open Project") {  };
            FolderBrowser id_51d5b3ef79fa4df8bf61e5a1707d2306 = new FolderBrowser() { Description = "" };
            DirectorySearch id_aab768da24834cb38b43f133fedbf9e6 = new DirectorySearch(directoriesToFind: new string[] { "DomainAbstractions", "ProgrammingParadigms", "RequirementsAbstractions" }) { FilenameFilter = "*.cs" };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_498218c0e56a460d8ce3fd523a80fbd9 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("DomainAbstractions")){list = input["DomainAbstractions"];}return list;} };
            ForEach<string> id_812f12033d83445b87341d91360ec75d = new ForEach<string>() {  };
            ApplyAction<string> id_524c2a62d216486588f32fba188e3f9c = new ApplyAction<string>() { Lambda = input =>{abstractionModelManager.CreateAbstractionModelFromPath(input);} };
            Data<string> id_ddb7f3a51361468eac1fe2749c428c3f = new Data<string>() { storedData = "Apply" };
            Apply<string, AbstractionModel> id_f1559abb466d462aa56ad39fade394d8 = new Apply<string, AbstractionModel>() { Lambda = input =>{return abstractionModelManager.GetAbstractionModel(input);} };
            Data<string> id_b887925e80a04eecaf7a4952b6faa955 = new Data<string>() { storedData = @"D:\Coding\C#\Projects\GALADE\ALACore" };
            KeyEvent id_51a2f831f3324544a5c822f0ad87f183 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.Enter } };
            ApplyAction<object> id_743db84d62174872aeb9d584512f43a1 = new ApplyAction<object>() { Lambda = input =>{(input as WPFCanvas).Focus();} };
            MenuItem id_166556ede1554f0dab90cbb29695c09e = new MenuItem(header: "Debug") {  };
            MenuItem id_339082f64d3040eb9657766ecf8022ac = new MenuItem(header: "TextEditor test") {  };
            PopupWindow id_d3957e7e921a4fbe9a2f2bd0e7fcc755 = new PopupWindow(title: "") { Height = 720, Width = 1280, Resize = SizeToContent.WidthAndHeight };
            Box id_6ae37fb756524c18ac91bbea4329ddfd = new Box() { Width = 100, Height = 100 };
            TextEditor id_990e13f3d27f421f8defa88097adb244 = new TextEditor() { Width = 1280, Height = 720 };
            DataFlowConnector<string> id_2a64f4bdfa2945ceb8f3776be4e22ff6 = new DataFlowConnector<string>() {  };
            FolderWatcher projectFolderWatcher = new FolderWatcher() { InstanceName = "projectFolderWatcher", RootPath = "", Filter = "*.cs", WatchSubdirectories = true, PathRegex = @".*\.cs$" };
            Apply<string, object> id_5458eaa0447b4d50b956df98dad83c62 = new Apply<string, object>() { Lambda = input =>{var newModel = abstractionModelManager.CreateAbstractionModelFromPath(input);foreach (var node in mainGraph.Nodes){var alaNode = node as ALANode;if (alaNode.Model.Type != newModel.Type) continue;abstractionModelManager.UpdateAbstractionModel(newModel, alaNode.Model);alaNode.UpdateUI();}return input;} };
            ConvertToEvent<object> id_0e60b27014834d10aed3bd00b7ef107e = new ConvertToEvent<object>() {  };
            MouseButtonEvent id_d9711c1f0e094b7b878241a3543e976e = new MouseButtonEvent(eventName: "MouseRightButtonDown") { ExtractSender = null, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            ApplyAction<object> id_67ab50fd0be44d1f8eb0a499965877a7 = new ApplyAction<object>() { Lambda = input =>{Mouse.Capture(input as WPFCanvas);stateTransition.Update(Enums.DiagramMode.Idle);} };
            MouseButtonEvent id_5add0167cfa14031a7b1cb90fa97a306 = new MouseButtonEvent(eventName: "MouseRightButtonUp") { ExtractSender = null, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            ApplyAction<object> id_bb3939df5a22411ba7ac969f945bc14e = new ApplyAction<object>() { Lambda = input =>{if (Mouse.Captured?.Equals(input) ?? false) Mouse.Capture(null);stateTransition.Update(Enums.DiagramMode.Idle);} };
            StateChangeListener id_19b72be522944e2ab4156c95a21f4709 = new StateChangeListener() { StateTransition = stateTransition, CurrentStateShouldMatch = Enums.DiagramMode.All };
            Apply<Tuple<Enums.DiagramMode, Enums.DiagramMode>, bool> id_9ee0e984042e40f7bd5e1d465c61dd8f = new Apply<Tuple<Enums.DiagramMode, Enums.DiagramMode>, bool>() { Lambda = input =>{return input.Item1 == Enums.DiagramMode.AwaitingPortSelection &&input.Item2 == Enums.DiagramMode.Idle;} };
            IfElse id_aa6b93b76d8d4e2b99aa1fe605dca94f = new IfElse() {  };
            EventConnector id_d5cd2524f016425f981480555ce1614c = new EventConnector() {  };
            Apply<object, object> createAndPaintALAWire = new Apply<object, object>() { InstanceName = "createAndPaintALAWire", Lambda = input =>{var source = mainGraph.Get("SelectedNode") as ALANode;var destination = input as ALANode;var sourcePort = source.GetSelectedPort(inputPort: false);var destinationPort = destination.GetSelectedPort(inputPort: true);var wire = new ALAWire(){Graph = mainGraph,Canvas = mainCanvas,Source = source,Destination = destination,SourcePort = sourcePort,DestinationPort = destinationPort,StateTransition = stateTransition};mainGraph.AddEdge(wire);wire.Paint();return wire;} };
            KeyEvent id_82ed0ca3559541a9862b7a40664cf4b0 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.Delete } };
            EventLambda id_754ae983659849e6ba2bd52e1cf414a8 = new EventLambda() { Lambda = () =>{var selectedNode = mainGraph.Get("SelectedNode") as ALANode;if (selectedNode == null) return;selectedNode.Delete(deleteAttachedWires: true);} };
            MenuItem id_8bc3a324e4b24bed80feabe0b29198c2 = new MenuItem(header: "Refresh") {  };
            Data<AbstractionModel> id_f476bd25f79b40da97dfb5b618dc9d0d = new Data<AbstractionModel>() { Lambda = () => abstractionModelManager.GetAbstractionModel(abstractionModelManager.GetAbstractionTypes().FirstOrDefault()) };
            Apply<AbstractionModel, object> id_04c749d47d3f4eb08b73083ecfa660a3 = new Apply<AbstractionModel, object>() { Lambda = createNewALANode.Lambda };
            ApplyAction<object> id_d0d668c705b44105a1113700a96595a5 = new ApplyAction<object>() { Lambda = input =>{var alaNode = input as ALANode;var mousePos = Mouse.GetPosition(mainCanvas);alaNode.PositionX = mousePos.X;alaNode.PositionY = mousePos.Y;mainGraph.Set("LatestNode", input);if (mainGraph.Get("SelectedNode") == null){mainGraph.Set("SelectedNode", input);}mainGraph.Roots.Add(input);} };
            MenuItem id_44178dd9dd254a2198b965b1475d84ee = new MenuItem(header: "Open Code File") {  };
            FileBrowser id_cb07eedc04654713a1ce963e26ba8cf9 = new FileBrowser() { Mode = "Open" };
            FileReader id_028d6d1f0576435fb31e49ca42dd3575 = new FileReader() {  };
            CreateDiagramFromCode id_2551af782af64df99f69cd19a8dbab89 = new CreateDiagramFromCode() { Graph = mainGraph, Canvas = mainCanvas, ModelManager = abstractionModelManager, StateTransition = stateTransition };
            EventConnector id_eb50ed83ba144eefba73121d64ecbba6 = new EventConnector() {  };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_4c371c1493ed4977a362bfca7f748b1b = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("ProgrammingParadigms")){list = input["ProgrammingParadigms"];}return list;} };
            ForEach<string> id_14a13a361b6f456093e8fe16753babb4 = new ForEach<string>() {  };
            ApplyAction<string> id_1a39027770504dda9c92ad7ddfb0f2cc = new ApplyAction<string>() { Lambda = input =>{abstractionModelManager.CreateAbstractionModelFromPath(input);} };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_1ea538484f2e4166a1719274ce36f83b = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("RequirementsAbstractions")){list = input["RequirementsAbstractions"];}return list;} };
            ForEach<string> id_85d4ca0a6d24415a844d1a42fe195265 = new ForEach<string>() {  };
            ApplyAction<string> id_eeafc223e1bb4ee9a4732c7c243aed53 = new ApplyAction<string>() { Lambda = input =>{abstractionModelManager.CreateAbstractionModelFromPath(input);} };
            DataFlowConnector<Dictionary<string, List<string>>> id_943b8bcd5d9c4918a8d2435b14800342 = new DataFlowConnector<Dictionary<string, List<string>>>() {  };
            MenuItem id_d4fed3a34fab485189dd2b906bd8d1f6 = new MenuItem(header: "Zoom In") {  };
            Data<UIElement> id_32dd0fb7ba144f26944ae981b95941cd = new Data<UIElement>() { Lambda = () => mainCanvas };
            Scale id_950f4ff2e7a64d2685d99a4b7cf11a30 = new Scale() { WidthMultiplier = 1.1, HeightMultiplier = 1.1 };
            MenuItem id_103f48c4d4b84541ad602e567f6e74bc = new MenuItem(header: "Zoom Out") {  };
            Data<UIElement> id_a2d918ad7a5c415885151819dd928159 = new Data<UIElement>() { Lambda = () => mainCanvas };
            Scale id_0b095ae199f24077ba6170af1da66ec7 = new Scale() { WidthMultiplier = 0.9, HeightMultiplier = 0.9 };
            DataFlowConnector<UIElement> id_cb9b0ae6ec0449aaba009907f078e312 = new DataFlowConnector<UIElement>() {  };
            DataFlowConnector<UIElement> id_1a0994ae2b5e4f96b9ef59d078d9e252 = new DataFlowConnector<UIElement>() {  };
            ApplyAction<UIElement> id_d913536450e1487ab5ddf012fcc0ebee = new ApplyAction<UIElement>() { Lambda = input => {if (!(input.RenderTransform is ScaleTransform)) return;var transform = input.RenderTransform as ScaleTransform;var minScale = 0.8;/*Logging.Log($"Scale: {transform.ScaleX}, {transform.ScaleX}");*/bool nodeIsTooSmall = transform.ScaleX < minScale && transform.ScaleY < minScale;var nodes = mainGraph.Nodes;foreach (var node in nodes){if (node is ALANode alaNode) alaNode.ShowTypeTextMask(nodeIsTooSmall);}} };
            // END AUTO-GENERATED INSTANTIATIONS FOR Application.xmind

            // BEGIN AUTO-GENERATED WIRING FOR Application.xmind
            mainWindow.WireTo(id_9af884fdd740441b9807661935bc59c5, "iuiStructure");
            mainWindow.WireTo(id_d5cd2524f016425f981480555ce1614c, "appStart");
            id_9af884fdd740441b9807661935bc59c5.WireTo(id_d8118e87cb804432aaa99fdda34d898c, "children");
            id_9af884fdd740441b9807661935bc59c5.WireTo(id_06da5c839f3d434f8d85b919a260ffde, "children");
            id_06da5c839f3d434f8d85b919a260ffde.WireTo(id_4f33f0ca8c5940aaa9f6e86976fcff50, "eventHandlers");
            id_06da5c839f3d434f8d85b919a260ffde.WireTo(id_2e77dfeef6784689b0278b810a4ac1bd, "eventHandlers");
            id_06da5c839f3d434f8d85b919a260ffde.WireTo(id_51a2f831f3324544a5c822f0ad87f183, "eventHandlers");
            id_06da5c839f3d434f8d85b919a260ffde.WireTo(id_d9711c1f0e094b7b878241a3543e976e, "eventHandlers");
            id_06da5c839f3d434f8d85b919a260ffde.WireTo(id_5add0167cfa14031a7b1cb90fa97a306, "eventHandlers");
            id_06da5c839f3d434f8d85b919a260ffde.WireTo(id_82ed0ca3559541a9862b7a40664cf4b0, "eventHandlers");
            id_06da5c839f3d434f8d85b919a260ffde.WireTo(id_61eb9dab031a4f7eae4fe0e144b919c9, "contextMenu");
            id_4f33f0ca8c5940aaa9f6e86976fcff50.WireTo(id_05664e895ab748888e1938ef6bfe89d0, "eventHappened");
            id_61eb9dab031a4f7eae4fe0e144b919c9.WireTo(id_2d32b91d2ab1435cafe28988425376d0, "children");
            id_61eb9dab031a4f7eae4fe0e144b919c9.WireTo(id_8bc3a324e4b24bed80feabe0b29198c2, "children");
            id_2d32b91d2ab1435cafe28988425376d0.WireTo(id_f476bd25f79b40da97dfb5b618dc9d0d, "clickedEvent");
            id_05664e895ab748888e1938ef6bfe89d0.WireTo(id_ddb7f3a51361468eac1fe2749c428c3f, "fanoutList");
            id_05664e895ab748888e1938ef6bfe89d0.WireTo(layoutDiagram, "complete");
            getFirstRoot.WireTo(id_d4e13fc2e593411581d05445df76680a, "dataOutput");
            layoutDiagram.WireTo(getFirstRoot, "fanoutList");
            id_d4e13fc2e593411581d05445df76680a.WireTo(id_5ebc05846871488d94e745d60e073ae1, "fanoutList");
            id_d4e13fc2e593411581d05445df76680a.WireTo(id_8789d0f39d654e0491be09c47d3a5b2f, "fanoutList");
            id_2e77dfeef6784689b0278b810a4ac1bd.WireTo(layoutDiagram, "eventHappened");
            createNewALANode.WireTo(createAndPaintALAWire, "output");
            id_d8118e87cb804432aaa99fdda34d898c.WireTo(id_1b2cd1cf3cd74e8e9ae1007ea2358ab0, "children");
            id_d8118e87cb804432aaa99fdda34d898c.WireTo(id_166556ede1554f0dab90cbb29695c09e, "children");
            id_d8118e87cb804432aaa99fdda34d898c.WireTo(id_d4fed3a34fab485189dd2b906bd8d1f6, "children");
            id_d8118e87cb804432aaa99fdda34d898c.WireTo(id_103f48c4d4b84541ad602e567f6e74bc, "children");
            id_1b2cd1cf3cd74e8e9ae1007ea2358ab0.WireTo(id_c95a8721239446eb8639ec850a919e7d, "children");
            id_1b2cd1cf3cd74e8e9ae1007ea2358ab0.WireTo(id_44178dd9dd254a2198b965b1475d84ee, "children");
            id_c95a8721239446eb8639ec850a919e7d.WireTo(id_51d5b3ef79fa4df8bf61e5a1707d2306, "clickedEvent");
            id_51d5b3ef79fa4df8bf61e5a1707d2306.WireTo(id_2a64f4bdfa2945ceb8f3776be4e22ff6, "selectedFolderPathOutput");
            id_aab768da24834cb38b43f133fedbf9e6.WireTo(id_943b8bcd5d9c4918a8d2435b14800342, "foundFiles");
            id_498218c0e56a460d8ce3fd523a80fbd9.WireTo(id_812f12033d83445b87341d91360ec75d, "output");
            id_812f12033d83445b87341d91360ec75d.WireTo(id_524c2a62d216486588f32fba188e3f9c, "elementOutput");
            id_ddb7f3a51361468eac1fe2749c428c3f.WireTo(id_f1559abb466d462aa56ad39fade394d8, "dataOutput");
            id_f1559abb466d462aa56ad39fade394d8.WireTo(createNewALANode, "output");
            id_b887925e80a04eecaf7a4952b6faa955.WireTo(id_2a64f4bdfa2945ceb8f3776be4e22ff6, "dataOutput");
            id_51a2f831f3324544a5c822f0ad87f183.WireTo(id_743db84d62174872aeb9d584512f43a1, "senderOutput");
            id_166556ede1554f0dab90cbb29695c09e.WireTo(id_339082f64d3040eb9657766ecf8022ac, "children");
            id_339082f64d3040eb9657766ecf8022ac.WireTo(id_d3957e7e921a4fbe9a2f2bd0e7fcc755, "clickedEvent");
            id_d3957e7e921a4fbe9a2f2bd0e7fcc755.WireTo(id_6ae37fb756524c18ac91bbea4329ddfd, "children");
            id_6ae37fb756524c18ac91bbea4329ddfd.WireTo(id_990e13f3d27f421f8defa88097adb244, "uiLayout");
            id_2a64f4bdfa2945ceb8f3776be4e22ff6.WireTo(id_aab768da24834cb38b43f133fedbf9e6, "fanoutList");
            id_2a64f4bdfa2945ceb8f3776be4e22ff6.WireTo(projectFolderWatcher, "fanoutList");
            projectFolderWatcher.WireTo(id_5458eaa0447b4d50b956df98dad83c62, "changedFile");
            id_5458eaa0447b4d50b956df98dad83c62.WireTo(id_0e60b27014834d10aed3bd00b7ef107e, "output");
            id_0e60b27014834d10aed3bd00b7ef107e.WireTo(id_8789d0f39d654e0491be09c47d3a5b2f, "eventOutput");
            id_d9711c1f0e094b7b878241a3543e976e.WireTo(id_67ab50fd0be44d1f8eb0a499965877a7, "argsOutput");
            id_5add0167cfa14031a7b1cb90fa97a306.WireTo(id_bb3939df5a22411ba7ac969f945bc14e, "argsOutput");
            id_19b72be522944e2ab4156c95a21f4709.WireTo(id_9ee0e984042e40f7bd5e1d465c61dd8f, "transitionOutput");
            id_9ee0e984042e40f7bd5e1d465c61dd8f.WireTo(id_aa6b93b76d8d4e2b99aa1fe605dca94f, "output");
            id_aa6b93b76d8d4e2b99aa1fe605dca94f.WireTo(layoutDiagram, "ifOutput");
            id_d5cd2524f016425f981480555ce1614c.WireTo(id_b887925e80a04eecaf7a4952b6faa955, "fanoutList");
            id_d5cd2524f016425f981480555ce1614c.WireTo(id_19b72be522944e2ab4156c95a21f4709, "fanoutList");
            id_d5cd2524f016425f981480555ce1614c.WireTo(id_eb50ed83ba144eefba73121d64ecbba6, "complete");
            id_82ed0ca3559541a9862b7a40664cf4b0.WireTo(id_754ae983659849e6ba2bd52e1cf414a8, "eventHappened");
            id_8bc3a324e4b24bed80feabe0b29198c2.WireTo(layoutDiagram, "clickedEvent");
            id_f476bd25f79b40da97dfb5b618dc9d0d.WireTo(id_04c749d47d3f4eb08b73083ecfa660a3, "dataOutput");
            id_04c749d47d3f4eb08b73083ecfa660a3.WireTo(id_d0d668c705b44105a1113700a96595a5, "output");
            id_44178dd9dd254a2198b965b1475d84ee.WireTo(id_cb07eedc04654713a1ce963e26ba8cf9, "clickedEvent");
            id_cb07eedc04654713a1ce963e26ba8cf9.WireTo(id_028d6d1f0576435fb31e49ca42dd3575, "selectedFilePathOutput");
            id_028d6d1f0576435fb31e49ca42dd3575.WireTo(id_2551af782af64df99f69cd19a8dbab89, "fileContentOutput");
            id_4c371c1493ed4977a362bfca7f748b1b.WireTo(id_14a13a361b6f456093e8fe16753babb4, "output");
            id_14a13a361b6f456093e8fe16753babb4.WireTo(id_1a39027770504dda9c92ad7ddfb0f2cc, "elementOutput");
            id_1ea538484f2e4166a1719274ce36f83b.WireTo(id_85d4ca0a6d24415a844d1a42fe195265, "output");
            id_85d4ca0a6d24415a844d1a42fe195265.WireTo(id_eeafc223e1bb4ee9a4732c7c243aed53, "elementOutput");
            id_943b8bcd5d9c4918a8d2435b14800342.WireTo(id_498218c0e56a460d8ce3fd523a80fbd9, "fanoutList");
            id_943b8bcd5d9c4918a8d2435b14800342.WireTo(id_4c371c1493ed4977a362bfca7f748b1b, "fanoutList");
            id_943b8bcd5d9c4918a8d2435b14800342.WireTo(id_1ea538484f2e4166a1719274ce36f83b, "fanoutList");
            id_d4fed3a34fab485189dd2b906bd8d1f6.WireTo(id_32dd0fb7ba144f26944ae981b95941cd, "clickedEvent");
            id_32dd0fb7ba144f26944ae981b95941cd.WireTo(id_cb9b0ae6ec0449aaba009907f078e312, "dataOutput");
            id_cb9b0ae6ec0449aaba009907f078e312.WireTo(id_950f4ff2e7a64d2685d99a4b7cf11a30, "fanoutList");
            id_103f48c4d4b84541ad602e567f6e74bc.WireTo(id_a2d918ad7a5c415885151819dd928159, "clickedEvent");
            id_a2d918ad7a5c415885151819dd928159.WireTo(id_1a0994ae2b5e4f96b9ef59d078d9e252, "dataOutput");
            id_1a0994ae2b5e4f96b9ef59d078d9e252.WireTo(id_0b095ae199f24077ba6170af1da66ec7, "fanoutList");
            id_cb9b0ae6ec0449aaba009907f078e312.WireTo(id_d913536450e1487ab5ddf012fcc0ebee, "fanoutList");
            id_1a0994ae2b5e4f96b9ef59d078d9e252.WireTo(id_d913536450e1487ab5ddf012fcc0ebee, "fanoutList");
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




















































































































































































































