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
            Vertical id_afc99c679df848e2a7d08f2af9fce601 = new Vertical() {  };
            CanvasDisplay id_a61cdf21737b4ef283c27ee76e8bd063 = new CanvasDisplay() { Width = 1920, Height = 600, Background = Brushes.White, StateTransition = stateTransition };
            ApplyAction<System.Windows.Controls.Canvas> id_e4821da628124291a9dd58645981d5c7 = new ApplyAction<System.Windows.Controls.Canvas>() { Lambda = canvas => mainCanvas = canvas };
            KeyEvent id_b79695b746c04917a8f594b6f297957d = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.A }, Condition = args => mainGraph.Get("SelectedNode") != null&& stateTransition.CurrentStateMatches(Enums.DiagramMode.IdleSelected) };
            Data<object> id_0bb1dac56fa64e2cb1e9df78de431d50 = new Data<object>() { Lambda = () => {var node = new ALANode();node.Model = abstractionModelManager.GetAbstractionModel(abstractionModelManager.GetAbstractionTypes().FirstOrDefault());node.Graph = mainGraph;node.Canvas = mainCanvas;node.StateTransition = stateTransition;node.AvailableDomainAbstractions.AddRange( abstractionModelManager.GetAbstractionTypes());node.TypeChanged += newType => {abstractionModelManager.UpdateAbstractionModel(abstractionModelManager.GetAbstractionModel(newType),node.Model);node.UpdateUI();Dispatcher.CurrentDispatcher.Invoke(() => {var edges = node.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);};mainGraph.AddNode(node);node.CreateInternals();mainCanvas.Children.Add(node.Render);return node;} };
            ApplyAction<object> initialiseNode = new ApplyAction<object>() { InstanceName = "initialiseNode", Lambda = input =>{var render = (input as ALANode).Render;var mousePos = Mouse.GetPosition(mainCanvas);WPFCanvas.SetLeft(render, mousePos.X);WPFCanvas.SetTop(render, mousePos.Y);mainGraph.Set("LatestNode", input);if (mainGraph.Get("SelectedNode") == null){mainGraph.Set("SelectedNode", input);mainGraph.Roots.Add(input);}} };
            ContextMenu id_80e3aeaf62624f5f95bcf3ef89244f3a = new ContextMenu() {  };
            MenuItem id_98cdd12e4c4c42e686e5555b64449bc4 = new MenuItem(header: "Add root") {  };
            EventConnector id_9bb8b45e86c440b7abeb6719660208b2 = new EventConnector() {  };
            Apply<object, object> createAndPaintALAWire = new Apply<object, object>() { InstanceName = "createAndPaintALAWire", Lambda = input =>{var source = mainGraph.Get("SelectedNode") as ALANode;var destination = input as ALANode;var sourcePort = source.GetSelectedPort(inputPort: false);var destinationPort = destination.GetSelectedPort(inputPort: true);var wire = new ALAWire(){Graph = mainGraph,Canvas = mainCanvas,Source = source,Destination = destination,SourcePort = sourcePort,DestinationPort = destinationPort};mainGraph.AddEdge(wire);source.PositionChanged += () => wire.Refresh();destination.PositionChanged += () => wire.Refresh();wire.Paint();return wire;} };
            UIFactory setUpGraph = new UIFactory(getUIContainer: () =>{/* This lambda executes during the UI setup call, which occurs before the app event flow.The reason for putting this lambda here is that thisensures that mainGraph is set up before being passedinto the scope of other delegates down the line (before the app event flow)*/mainGraph = new Graph();mainGraph.EdgeAdded += edge => {var wire = edge as ALAWire;var src = wire.Source as ALANode;src.Edges.Add(edge);var dest = wire.Destination as ALANode;dest.Edges.Add(edge);};mainGraph.EdgeDeleted += edge => {var wire = edge as ALAWire;var src = wire.Source as ALANode;src.Edges.Remove(edge);var dest = wire.Destination as ALANode;dest.Edges.Remove(edge);};/* Return a dummy invisible IUI */return new Text("", visible: false);}) { InstanceName = "setUpGraph" };
            Data<ALANode> id_1ffff4932f2e46f48d236155623719dc = new Data<ALANode>() { Lambda = () => mainGraph.Roots.First() as ALANode };
            RightTreeLayout<ALANode> id_06d1e18596d948be9024c930bc6e1920 = new RightTreeLayout<ALANode>() { GetID = n => n.Id, GetWidth = n => (n.Render as FrameworkElement).ActualWidth, GetHeight = n => (n.Render as FrameworkElement).ActualHeight, SetX = (n, x) => WPFCanvas.SetLeft(n.Render, x), SetY = (n, y) => WPFCanvas.SetTop(n.Render, y), GetChildren = n => mainGraph.Edges.Where(e => e is ALAWire wire && wire.Source == n).Select(e => ((e as ALAWire).Destination) as ALANode), HorizontalGap = 100, VerticalGap = 20, InitialX = 50, InitialY = 50 };
            EventConnector layoutDiagram = new EventConnector() { InstanceName = "layoutDiagram" };
            DataFlowConnector<ALANode> id_1ff8dc64db8e4d8a8755d00eb83236ee = new DataFlowConnector<ALANode>() {  };
            ApplyAction<ALANode> id_cc3ce12bedab4eac818a92878344de82 = new ApplyAction<ALANode>() { Lambda = node =>{Dispatcher.CurrentDispatcher.Invoke(() => {var edges = mainGraph.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);} };
            KeyEvent id_244a962265324d4785674367681ab193 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.R }, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            Apply<AbstractionModel, object> id_3ce86dcc47f2479fa290cc5402e2e9b3 = new Apply<AbstractionModel, object>() { Lambda = input => {var node = new ALANode();node.Model = input;node.Graph = mainGraph;node.Canvas = mainCanvas;node.StateTransition = stateTransition;node.AvailableDomainAbstractions.AddRange( abstractionModelManager.GetAbstractionTypes());node.TypeChanged += newType => {abstractionModelManager.UpdateAbstractionModel(abstractionModelManager.GetAbstractionModel(newType),node.Model);node.UpdateUI();Dispatcher.CurrentDispatcher.Invoke(() => {var edges = node.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);};mainGraph.AddNode(node);node.CreateInternals();mainCanvas.Children.Add(node.Render);return node;} };
            MenuBar id_f5a0ac9d0a464b8fac35c840051145ce = new MenuBar() {  };
            MenuItem id_821028d50e0d47fa9074719bccd287d3 = new MenuItem(header: "File") {  };
            MenuItem id_705b37cfeda747db817c9ffec2dd6262 = new MenuItem(header: "Open Project") {  };
            FolderBrowser id_09d6d8df3d074e63870b3a707217eabc = new FolderBrowser() { Description = "" };
            DirectorySearch id_778ead7d73594d9cbbc5f05d22cf96e2 = new DirectorySearch(directoriesToFind: new string[] { "DomainAbstractions" }) { FilenameFilter = "*.cs" };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_1a2801c8c8fc42c885c0816e46104857 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("DomainAbstractions")){list = input["DomainAbstractions"];}return list;} };
            ForEach<string> id_c663d18011c54b7c8a234699603d3682 = new ForEach<string>() {  };
            ApplyAction<string> id_84e665af520944ee8e5ef3f304913bf2 = new ApplyAction<string>() { Lambda = input =>{abstractionModelManager.CreateAbstractionModelFromPath(input);} };
            Data<string> id_e738f6b3cf1f448c976f1112e5188b0b = new Data<string>() { storedData = "Apply<T1, T2>" };
            Apply<string, AbstractionModel> id_15df80094d864d7688c0c81956a2c88c = new Apply<string, AbstractionModel>() { Lambda = input =>{return abstractionModelManager.GetAbstractionModel(input);} };
            Data<string> id_aa8457d5f66449c2aaf2d31b15103cb7 = new Data<string>() { storedData = @"F:\Projects\GALADE\ALACore" };
            DropDownMenu id_dd6f685e3fb344c38410056b516c06e6 = new DropDownMenu() { Items = new string[100] };
            KeyEvent id_a6e67c10ab2e406a859087c3e6ef55dd = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.Enter } };
            ApplyAction<object> id_bba05c82f47d4173bae6fdfa1466bc1c = new ApplyAction<object>() { Lambda = input =>{(input as WPFCanvas).Focus();} };
            MenuItem id_880f4e3a614549cfbdceb7ff194b6a5d = new MenuItem(header: "Debug") {  };
            MenuItem id_15a73c3d2bce4dd09a25bb5043c2975f = new MenuItem(header: "TextEditor test") {  };
            PopupWindow id_0091f302b5ba484081dca30f50a3151e = new PopupWindow(title: "") { Height = 720, Width = 1280, Resize = SizeToContent.WidthAndHeight };
            Box id_1dfd2beb875641f3a895e222673ff3b6 = new Box() { Width = 100, Height = 100 };
            TextEditor id_c12d19cef3de43278dec3cba5a08252b = new TextEditor() { Width = 1280, Height = 720 };
            DataFlowConnector<string> id_d48f866bb7f94ab7b66d7643aa37bcdc = new DataFlowConnector<string>() {  };
            FolderWatcher projectFolderWatcher = new FolderWatcher() { InstanceName = "projectFolderWatcher", RootPath = "", Filter = "*.cs", WatchSubdirectories = true, PathRegex = @".*\.cs$" };
            Apply<string, object> id_47c40362c894491bb9a7d1d62f1611b1 = new Apply<string, object>() { Lambda = input =>{var newModel = abstractionModelManager.CreateAbstractionModelFromPath(input);foreach (var node in mainGraph.Nodes){var alaNode = node as ALANode;if (alaNode.Model.Type != newModel.Type) continue;abstractionModelManager.UpdateAbstractionModel(newModel, alaNode.Model);alaNode.UpdateUI();}return input;} };
            ConvertToEvent<object> id_20d39e5f8e3c4a218f32863da4a5c476 = new ConvertToEvent<object>() {  };
            // END AUTO-GENERATED INSTANTIATIONS FOR Application.xmind

            // BEGIN AUTO-GENERATED WIRING FOR Application.xmind
            mainWindow.WireTo(id_afc99c679df848e2a7d08f2af9fce601, "iuiStructure");
            mainWindow.WireTo(id_aa8457d5f66449c2aaf2d31b15103cb7, "appStart");
            id_afc99c679df848e2a7d08f2af9fce601.WireTo(setUpGraph, "children");
            id_afc99c679df848e2a7d08f2af9fce601.WireTo(id_f5a0ac9d0a464b8fac35c840051145ce, "children");
            id_afc99c679df848e2a7d08f2af9fce601.WireTo(id_a61cdf21737b4ef283c27ee76e8bd063, "children");
            id_a61cdf21737b4ef283c27ee76e8bd063.WireTo(id_e4821da628124291a9dd58645981d5c7, "canvasOutput");
            id_a61cdf21737b4ef283c27ee76e8bd063.WireTo(id_b79695b746c04917a8f594b6f297957d, "eventHandlers");
            id_a61cdf21737b4ef283c27ee76e8bd063.WireTo(id_244a962265324d4785674367681ab193, "eventHandlers");
            id_a61cdf21737b4ef283c27ee76e8bd063.WireTo(id_a6e67c10ab2e406a859087c3e6ef55dd, "eventHandlers");
            id_a61cdf21737b4ef283c27ee76e8bd063.WireTo(id_80e3aeaf62624f5f95bcf3ef89244f3a, "contextMenu");
            id_b79695b746c04917a8f594b6f297957d.WireTo(id_9bb8b45e86c440b7abeb6719660208b2, "eventHappened");
            id_0bb1dac56fa64e2cb1e9df78de431d50.WireTo(initialiseNode, "dataOutput");
            id_80e3aeaf62624f5f95bcf3ef89244f3a.WireTo(id_98cdd12e4c4c42e686e5555b64449bc4, "children");
            id_98cdd12e4c4c42e686e5555b64449bc4.WireTo(id_0bb1dac56fa64e2cb1e9df78de431d50, "clickedEvent");
            id_9bb8b45e86c440b7abeb6719660208b2.WireTo(id_e738f6b3cf1f448c976f1112e5188b0b, "fanoutList");
            id_9bb8b45e86c440b7abeb6719660208b2.WireTo(layoutDiagram, "complete");
            id_1ffff4932f2e46f48d236155623719dc.WireTo(id_1ff8dc64db8e4d8a8755d00eb83236ee, "dataOutput");
            layoutDiagram.WireTo(id_1ffff4932f2e46f48d236155623719dc, "fanoutList");
            id_1ff8dc64db8e4d8a8755d00eb83236ee.WireTo(id_06d1e18596d948be9024c930bc6e1920, "fanoutList");
            id_1ff8dc64db8e4d8a8755d00eb83236ee.WireTo(id_cc3ce12bedab4eac818a92878344de82, "fanoutList");
            id_20d39e5f8e3c4a218f32863da4a5c476.WireTo(id_cc3ce12bedab4eac818a92878344de82, "eventOutput");
            id_244a962265324d4785674367681ab193.WireTo(layoutDiagram, "eventHappened");
            id_3ce86dcc47f2479fa290cc5402e2e9b3.WireTo(createAndPaintALAWire, "output");
            id_f5a0ac9d0a464b8fac35c840051145ce.WireTo(id_821028d50e0d47fa9074719bccd287d3, "children");
            id_f5a0ac9d0a464b8fac35c840051145ce.WireTo(id_880f4e3a614549cfbdceb7ff194b6a5d, "children");
            id_821028d50e0d47fa9074719bccd287d3.WireTo(id_705b37cfeda747db817c9ffec2dd6262, "children");
            id_821028d50e0d47fa9074719bccd287d3.WireTo(id_dd6f685e3fb344c38410056b516c06e6, "children");
            id_705b37cfeda747db817c9ffec2dd6262.WireTo(id_09d6d8df3d074e63870b3a707217eabc, "clickedEvent");
            id_09d6d8df3d074e63870b3a707217eabc.WireTo(id_d48f866bb7f94ab7b66d7643aa37bcdc, "selectedFolderPathOutput");
            id_778ead7d73594d9cbbc5f05d22cf96e2.WireTo(id_1a2801c8c8fc42c885c0816e46104857, "foundFiles");
            id_1a2801c8c8fc42c885c0816e46104857.WireTo(id_c663d18011c54b7c8a234699603d3682, "output");
            id_c663d18011c54b7c8a234699603d3682.WireTo(id_84e665af520944ee8e5ef3f304913bf2, "elementOutput");
            id_e738f6b3cf1f448c976f1112e5188b0b.WireTo(id_15df80094d864d7688c0c81956a2c88c, "dataOutput");
            id_15df80094d864d7688c0c81956a2c88c.WireTo(id_3ce86dcc47f2479fa290cc5402e2e9b3, "output");
            id_aa8457d5f66449c2aaf2d31b15103cb7.WireTo(id_d48f866bb7f94ab7b66d7643aa37bcdc, "dataOutput");
            id_a6e67c10ab2e406a859087c3e6ef55dd.WireTo(id_bba05c82f47d4173bae6fdfa1466bc1c, "senderOutput");
            id_880f4e3a614549cfbdceb7ff194b6a5d.WireTo(id_15a73c3d2bce4dd09a25bb5043c2975f, "children");
            id_15a73c3d2bce4dd09a25bb5043c2975f.WireTo(id_0091f302b5ba484081dca30f50a3151e, "clickedEvent");
            id_0091f302b5ba484081dca30f50a3151e.WireTo(id_1dfd2beb875641f3a895e222673ff3b6, "children");
            id_1dfd2beb875641f3a895e222673ff3b6.WireTo(id_c12d19cef3de43278dec3cba5a08252b, "uiLayout");
            id_d48f866bb7f94ab7b66d7643aa37bcdc.WireTo(id_778ead7d73594d9cbbc5f05d22cf96e2, "fanoutList");
            id_d48f866bb7f94ab7b66d7643aa37bcdc.WireTo(projectFolderWatcher, "fanoutList");
            projectFolderWatcher.WireTo(id_47c40362c894491bb9a7d1d62f1611b1, "changedFile");
            id_47c40362c894491bb9a7d1d62f1611b1.WireTo(id_20d39e5f8e3c4a218f32863da4a5c476, "output");
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






























