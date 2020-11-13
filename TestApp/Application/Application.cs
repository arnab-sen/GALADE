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
                obj["ApplicationCodeFilePath"] = "";

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
            Vertical id_24e40d4dd7c84b0c8cac4804255e544b = new Vertical() {  };
            CanvasDisplay id_bb7e3d1ced0b4b84b8dcf7acff2adae3 = new CanvasDisplay() { Width = 1920, Height = 1080, Background = Brushes.White, StateTransition = stateTransition, Canvas = mainCanvas };
            KeyEvent id_936c81d212d2433bb2d7ceb5e14adcb3 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.A }, Condition = args => mainGraph.Get("SelectedNode") != null&& stateTransition.CurrentStateMatches(Enums.DiagramMode.IdleSelected) };
            ContextMenu id_1d225d905da04108b536c0c5071f105a = new ContextMenu() {  };
            MenuItem id_0adf682aa76f4efea41836a6a23f1787 = new MenuItem(header: "Add root") {  };
            EventConnector id_356b52a323794284b5c1841879bec421 = new EventConnector() {  };
            Data<ALANode> getFirstRoot = new Data<ALANode>() { InstanceName = "getFirstRoot", Lambda = () => mainGraph.Roots.FirstOrDefault() as ALANode };
            RightTreeLayout<ALANode> id_ab7c6a97760f4ed59003e90fefa8154a = new RightTreeLayout<ALANode>() { GetID = n => n.Id, GetWidth = n => n.Width, GetHeight = n => n.Height, SetX = (n, x) => n.PositionX = x, SetY = (n, y) => n.PositionY = y, GetChildren = n => mainGraph.Edges.Where(e => e is ALAWire wire && wire.Source != null && wire.Destination != null && wire.Source == n).Select(e => ((e as ALAWire).Destination) as ALANode), HorizontalGap = 100, VerticalGap = 20, InitialX = 50, InitialY = 50 };
            EventConnector layoutDiagram = new EventConnector() { InstanceName = "layoutDiagram" };
            DataFlowConnector<ALANode> id_8c3f48364c954347b61bcb6ee03c3ff5 = new DataFlowConnector<ALANode>() {  };
            ApplyAction<ALANode> id_bf50f7ec71cb4734bebeee3dd0b9ac9d = new ApplyAction<ALANode>() { Lambda = node =>{Dispatcher.CurrentDispatcher.Invoke(() => {var edges = mainGraph.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);} };
            KeyEvent id_8daf6656e19046109021ed0e09df34c0 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.R }, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            Apply<AbstractionModel, object> createNewALANode = new Apply<AbstractionModel, object>() { InstanceName = "createNewALANode", Lambda = input => {var node = new ALANode();node.Model = input;node.Graph = mainGraph;node.Canvas = mainCanvas;node.StateTransition = stateTransition;node.AvailableDomainAbstractions.AddRange( abstractionModelManager.GetAbstractionTypes());node.TypeChanged += newType => {abstractionModelManager.UpdateAbstractionModel(abstractionModelManager.GetAbstractionModel(newType),node.Model);node.UpdateUI();Dispatcher.CurrentDispatcher.Invoke(() => {var edges = mainGraph.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);};mainGraph.AddNode(node);node.CreateInternals();mainCanvas.Children.Add(node.Render);return node;} };
            MenuBar id_98261c05e417447a8d95e965b6a16b84 = new MenuBar() {  };
            MenuItem id_2394034f78e445318e0b76c390aeca64 = new MenuItem(header: "File") {  };
            MenuItem id_3a42cddbe7c3451cae71d6163a07d84b = new MenuItem(header: "Open Project") {  };
            FolderBrowser id_7b08320ddfe248718d4424b3c4d58d52 = new FolderBrowser() { Description = "" };
            DirectorySearch id_69e7fa1185c34c01aeb711254e850b05 = new DirectorySearch(directoriesToFind: new string[] { "DomainAbstractions", "ProgrammingParadigms", "RequirementsAbstractions", "Modules" }) { FilenameFilter = "*.cs" };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_fb170d142f834f5e872b58af20476911 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("DomainAbstractions")){list = input["DomainAbstractions"];}return list;} };
            ForEach<string> id_5caffdfbb31e420a8239b66e80955aa8 = new ForEach<string>() {  };
            ApplyAction<string> id_a6ffb9b2088b4704b5463d9f9a6124c0 = new ApplyAction<string>() { Lambda = input =>{abstractionModelManager.CreateAbstractionModelFromPath(input);} };
            Data<string> id_a3784e11a49a488eb06adde362524bd1 = new Data<string>() { storedData = "Apply" };
            Apply<string, AbstractionModel> id_3ca59fdbc12e4be8b1a0e903bcac3164 = new Apply<string, AbstractionModel>() { Lambda = input =>{return abstractionModelManager.GetAbstractionModel(input);} };
            GetSetting id_c568c64d2a4f4bef976e0963b22c1f01 = new GetSetting(name: "ProjectFolderPath") {  };
            KeyEvent id_36bc6ed720864c7da326e5a2778d9bd5 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.Enter } };
            ApplyAction<object> id_1b8e251463014a43a8d1006290941440 = new ApplyAction<object>() { Lambda = input =>{(input as WPFCanvas).Focus();} };
            MenuItem id_0a045762f07b43e09ba09d10ded9c5a1 = new MenuItem(header: "Debug") {  };
            MenuItem id_46402a90d31b413d99414a0266a25a08 = new MenuItem(header: "TextEditor test") {  };
            PopupWindow id_09c8659d61ba49d589cd011d3ee011bd = new PopupWindow(title: "") { Height = 720, Width = 1280, Resize = SizeToContent.WidthAndHeight };
            Box id_04155bb492d24717879b41689160f164 = new Box() { Width = 100, Height = 100 };
            TextEditor id_6a5384dab5e94be9b4169f8268772052 = new TextEditor() { Width = 1280, Height = 720 };
            DataFlowConnector<string> id_cfa60f6b93c243f39b873a428a8790c9 = new DataFlowConnector<string>() {  };
            FolderWatcher projectFolderWatcher = new FolderWatcher() { InstanceName = "projectFolderWatcher", RootPath = "", Filter = "*.cs", WatchSubdirectories = true, PathRegex = @".*\.cs$" };
            Apply<string, object> id_b3e6f2e9a36643418a69dd7f6e5ebbeb = new Apply<string, object>() { Lambda = input =>{var newModel = abstractionModelManager.CreateAbstractionModelFromPath(input);foreach (var node in mainGraph.Nodes){var alaNode = node as ALANode;if (alaNode.Model.Type != newModel.Type) continue;abstractionModelManager.UpdateAbstractionModel(newModel, alaNode.Model);alaNode.UpdateUI();}return input;} };
            ConvertToEvent<object> id_f980107594ab46af8b1a58adae0a8eb8 = new ConvertToEvent<object>() {  };
            MouseButtonEvent id_64a90cddfb164dd28c09f76aa6c02080 = new MouseButtonEvent(eventName: "MouseRightButtonDown") { ExtractSender = null, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            ApplyAction<object> id_4aed31a491824a90bfa7a3a516d4ac15 = new ApplyAction<object>() { Lambda = input =>{Mouse.Capture(input as WPFCanvas);stateTransition.Update(Enums.DiagramMode.Idle);} };
            MouseButtonEvent id_21062f6fa88149efb814af1357f700ca = new MouseButtonEvent(eventName: "MouseRightButtonUp") { ExtractSender = null, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            ApplyAction<object> id_bd424287c27645a0949c577aecd4848d = new ApplyAction<object>() { Lambda = input =>{if (Mouse.Captured?.Equals(input) ?? false) Mouse.Capture(null);stateTransition.Update(Enums.DiagramMode.Idle);} };
            StateChangeListener id_0223968931234184bc3ea5c8c9a653e9 = new StateChangeListener() { StateTransition = stateTransition, CurrentStateShouldMatch = Enums.DiagramMode.All };
            Apply<Tuple<Enums.DiagramMode, Enums.DiagramMode>, bool> id_430d8ef174374904bee7b283dec49a29 = new Apply<Tuple<Enums.DiagramMode, Enums.DiagramMode>, bool>() { Lambda = input =>{return input.Item1 == Enums.DiagramMode.AwaitingPortSelection &&input.Item2 == Enums.DiagramMode.Idle;} };
            IfElse id_fdf2cf28be1545f0978ecc377df7e142 = new IfElse() {  };
            EventConnector id_f54a15b16b194f0297b551c9126abaf1 = new EventConnector() {  };
            Apply<object, object> createAndPaintALAWire = new Apply<object, object>() { InstanceName = "createAndPaintALAWire", Lambda = input =>{var source = mainGraph.Get("SelectedNode") as ALANode;var destination = input as ALANode;var sourcePort = source.GetSelectedPort(inputPort: false);var destinationPort = destination.GetSelectedPort(inputPort: true);var wire = new ALAWire(){Graph = mainGraph,Canvas = mainCanvas,Source = source,Destination = destination,SourcePort = sourcePort,DestinationPort = destinationPort,StateTransition = stateTransition};mainGraph.AddEdge(wire);wire.Paint();return wire;} };
            KeyEvent id_3cc331e2ade44d8f856bb0912fa1b4b8 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.Delete } };
            EventLambda id_730230c7a29e46dba21589cbb4062941 = new EventLambda() { Lambda = () =>{var selectedNode = mainGraph.Get("SelectedNode") as ALANode;if (selectedNode == null) return;selectedNode.Delete(deleteAttachedWires: true);} };
            MenuItem id_09120bb1632c495595a829ae309d0a05 = new MenuItem(header: "Refresh") {  };
            Data<AbstractionModel> id_84447fc36c5a4fd18a84bcdc105c861d = new Data<AbstractionModel>() { Lambda = () => abstractionModelManager.GetAbstractionModel(abstractionModelManager.GetAbstractionTypes().FirstOrDefault()) };
            Apply<AbstractionModel, object> id_a0d389a58a124657826b587cb24c793f = new Apply<AbstractionModel, object>() { Lambda = createNewALANode.Lambda };
            ApplyAction<object> id_31b9b7d02bcc450bbe909f7bc8194099 = new ApplyAction<object>() { Lambda = input =>{var alaNode = input as ALANode;var mousePos = Mouse.GetPosition(mainCanvas);alaNode.PositionX = mousePos.X;alaNode.PositionY = mousePos.Y;mainGraph.Set("LatestNode", input);if (mainGraph.Get("SelectedNode") == null){mainGraph.Set("SelectedNode", input);}mainGraph.Roots.Add(input);} };
            MenuItem id_7b7249c8ba2443c18188d726eed5ddf3 = new MenuItem(header: "Open Code File") {  };
            FileBrowser id_26adb93de4fe4475bc797f2acc73d9f8 = new FileBrowser() { Mode = "Open" };
            FileReader id_0b1b264d0de04034abea6ee29816f8a7 = new FileReader() {  };
            CreateDiagramFromCode id_bf3c7eef31d949e885a25d2854668024 = new CreateDiagramFromCode() { Graph = mainGraph, Canvas = mainCanvas, ModelManager = abstractionModelManager, StateTransition = stateTransition };
            EventConnector id_fbb638a63f774d919bd331e242687030 = new EventConnector() {  };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_3e3d4b6d6d8d4858b0396219dcaf9295 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("ProgrammingParadigms")){list = input["ProgrammingParadigms"];}return list;} };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_2cf10c1414b9438785ab6519ad64c724 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("RequirementsAbstractions")){list = input["RequirementsAbstractions"];}return list;} };
            DataFlowConnector<Dictionary<string, List<string>>> id_b1f51e8156e84abf9519ee826b310769 = new DataFlowConnector<Dictionary<string, List<string>>>() {  };
            Data<UIElement> id_e1ededa54cb649a88ffcc248120c39c2 = new Data<UIElement>() { Lambda = () => mainCanvas };
            Scale id_08d97753ddb44d25b2739d35def1a5a6 = new Scale() { WidthMultiplier = 1.1, HeightMultiplier = 1.1 };
            Data<UIElement> id_539a875b07ab449caaae0490c824ae36 = new Data<UIElement>() { Lambda = () => mainCanvas };
            Scale id_daf3074eb03c41d7940d1fcbaf0ef65e = new Scale() { WidthMultiplier = 0.9, HeightMultiplier = 0.9 };
            DataFlowConnector<UIElement> id_2086aeb04dfe4c01ae0f4a669772537f = new DataFlowConnector<UIElement>() {  };
            DataFlowConnector<UIElement> id_15daff883d3c4b0ba48ed38e9afd027c = new DataFlowConnector<UIElement>() {  };
            ApplyAction<UIElement> id_6efae636bc3d4a6584cb9451151ff14a = new ApplyAction<UIElement>() { Lambda = input => {if (!(input.RenderTransform is ScaleTransform)) return;var transform = input.RenderTransform as ScaleTransform;var minScale = 0.6;/*Logging.Log($"Scale: {transform.ScaleX}, {transform.ScaleX}");*/bool nodeIsTooSmall = transform.ScaleX < minScale && transform.ScaleY < minScale;var nodes = mainGraph.Nodes;foreach (var node in nodes){if (node is ALANode alaNode) alaNode.ShowTypeTextMask(nodeIsTooSmall);}} };
            MouseWheelEvent id_e4d9250ea81e4bff8454bc54bf7fb787 = new MouseWheelEvent(eventName: "MouseWheel") {  };
            Apply<MouseWheelEventArgs, bool> id_09071d95d2844b20849c11febdaa4596 = new Apply<MouseWheelEventArgs, bool>() { Lambda = args =>{return args.Delta > 0;} };
            IfElse id_dae387347c174a6e8036f54c7af21eae = new IfElse() {  };
            DataFlowConnector<string> id_ed085fd9854241f1a269c76f1f7d901a = new DataFlowConnector<string>() {  };
            ConvertToEvent<string> id_de4233d94c534f1ea34fd55b967b8707 = new ConvertToEvent<string>() {  };
            DispatcherEvent id_b3a410ac8bcb4bd48f61e2abcd772a55 = new DispatcherEvent() { Priority = DispatcherPriority.ApplicationIdle };
            MenuItem id_841b4cd516b64304a046afcb7a2417eb = new MenuItem(header: "Generate Code") {  };
            GenerateALACode id_86f3bd33841b4a228c3c38dd3391c9bc = new GenerateALACode() { Graph = mainGraph };
            GetSetting id_ca0c8ffb0fea4cb1bd87f6efb3caf04f = new GetSetting(name: "ApplicationCodeFilePath") {  };
            Data<string> id_c2baa61645514db38bb2bdcec4401901 = new Data<string>() { storedData = SETTINGS_FILEPATH };
            EditSetting id_4a806bcc9d3e470682727913114356b9 = new EditSetting() { JSONPath = "$..ApplicationCodeFilePath" };
            Data<string> id_ce6adbee004047ad809c62b2fe14365c = new Data<string>() { storedData = SETTINGS_FILEPATH };
            Cast<string, object> id_4ee812e61dc84c59b196fb08e77a8edd = new Cast<string, object>() {  };
            DataFlowConnector<string> id_03ae7251e217413b8f2c78106da0dd79 = new DataFlowConnector<string>() {  };
            Cast<string, object> id_9c5eb0644c0147fdbc2b55dd5c08796f = new Cast<string, object>() {  };
            Data<string> id_0761bfc4af4048868a1d69c937a32156 = new Data<string>() { storedData = SETTINGS_FILEPATH };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_589f58d662434cf4b3eb44d7358449b7 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("Modules")){list = input["Modules"];}return list;} };
            GetSetting id_e4ca095bc4024827bfa22625f6a1835e = new GetSetting(name: "ApplicationCodeFilePath") {  };
            Data<string> id_addc2af8003f4b18ae421ab7a79b26b4 = new Data<string>() { storedData = SETTINGS_FILEPATH };
            FileReader id_58617bcea7554d0787a726d9b07c214b = new FileReader() {  };
            DataFlowConnector<string> id_5f3773e2981640708e316f6fd9fd6431 = new DataFlowConnector<string>() {  };
            EventConnector generateCode = new EventConnector() { InstanceName = "generateCode" };
            EditSetting id_c672849f94d74ae6a86dd81077ff13fe = new EditSetting() { JSONPath = "$..ProjectFolderPath" };
            Data<string> id_f89c71e9f0fe423987218a4e91f430d0 = new Data<string>() { storedData = SETTINGS_FILEPATH };
            FileWriter id_bb380057584d4e3dbc2569a5232eb5a4 = new FileWriter() {  };
            DataFlowConnector<string> id_786167b281e04de49ba5ad483e1ff246 = new DataFlowConnector<string>() {  };
            InsertFileCodeLines id_069b428be7294a67be3efb898aae6232 = new InsertFileCodeLines() { StartLandmark = "// BEGIN AUTO-GENERATED INSTANTIATIONS", EndLandmark = "// END AUTO-GENERATED INSTANTIATIONS", Indent = "            " };
            InsertFileCodeLines id_0d6abbea0ad64c4bb689bd7ea169d8c4 = new InsertFileCodeLines() { StartLandmark = "// BEGIN AUTO-GENERATED WIRING", EndLandmark = "// END AUTO-GENERATED WIRING", Indent = "            " };
            EventConnector id_29a95f9b55f64df086af7bfc24b165a3 = new EventConnector() {  };
            MenuItem id_4d01de40a3cf470a8f6a79d34eecd227 = new MenuItem(header: "Generics test") {  };
            EventLambda id_c6eb0cfba7ce4a118f70cb255e36168d = new EventLambda() { Lambda = () =>{var node = mainGraph.Nodes.First() as ALANode;node.Model.UpdateGeneric(0, "testType");} };
            // END AUTO-GENERATED INSTANTIATIONS FOR Application.xmind

            // BEGIN AUTO-GENERATED WIRING FOR Application.xmind
            mainWindow.WireTo(id_24e40d4dd7c84b0c8cac4804255e544b, "iuiStructure");
            mainWindow.WireTo(id_f54a15b16b194f0297b551c9126abaf1, "appStart");
            id_24e40d4dd7c84b0c8cac4804255e544b.WireTo(id_98261c05e417447a8d95e965b6a16b84, "children");
            id_24e40d4dd7c84b0c8cac4804255e544b.WireTo(id_bb7e3d1ced0b4b84b8dcf7acff2adae3, "children");
            id_bb7e3d1ced0b4b84b8dcf7acff2adae3.WireTo(id_936c81d212d2433bb2d7ceb5e14adcb3, "eventHandlers");
            id_bb7e3d1ced0b4b84b8dcf7acff2adae3.WireTo(id_8daf6656e19046109021ed0e09df34c0, "eventHandlers");
            id_bb7e3d1ced0b4b84b8dcf7acff2adae3.WireTo(id_36bc6ed720864c7da326e5a2778d9bd5, "eventHandlers");
            id_bb7e3d1ced0b4b84b8dcf7acff2adae3.WireTo(id_64a90cddfb164dd28c09f76aa6c02080, "eventHandlers");
            id_bb7e3d1ced0b4b84b8dcf7acff2adae3.WireTo(id_21062f6fa88149efb814af1357f700ca, "eventHandlers");
            id_bb7e3d1ced0b4b84b8dcf7acff2adae3.WireTo(id_3cc331e2ade44d8f856bb0912fa1b4b8, "eventHandlers");
            id_bb7e3d1ced0b4b84b8dcf7acff2adae3.WireTo(id_e4d9250ea81e4bff8454bc54bf7fb787, "eventHandlers");
            id_bb7e3d1ced0b4b84b8dcf7acff2adae3.WireTo(id_1d225d905da04108b536c0c5071f105a, "contextMenu");
            id_936c81d212d2433bb2d7ceb5e14adcb3.WireTo(id_356b52a323794284b5c1841879bec421, "eventHappened");
            id_1d225d905da04108b536c0c5071f105a.WireTo(id_0adf682aa76f4efea41836a6a23f1787, "children");
            id_1d225d905da04108b536c0c5071f105a.WireTo(id_09120bb1632c495595a829ae309d0a05, "children");
            id_0adf682aa76f4efea41836a6a23f1787.WireTo(id_84447fc36c5a4fd18a84bcdc105c861d, "clickedEvent");
            id_356b52a323794284b5c1841879bec421.WireTo(id_a3784e11a49a488eb06adde362524bd1, "fanoutList");
            id_356b52a323794284b5c1841879bec421.WireTo(layoutDiagram, "complete");
            getFirstRoot.WireTo(id_8c3f48364c954347b61bcb6ee03c3ff5, "dataOutput");
            layoutDiagram.WireTo(id_b3a410ac8bcb4bd48f61e2abcd772a55, "fanoutList");
            id_8c3f48364c954347b61bcb6ee03c3ff5.WireTo(id_ab7c6a97760f4ed59003e90fefa8154a, "fanoutList");
            id_8c3f48364c954347b61bcb6ee03c3ff5.WireTo(id_bf50f7ec71cb4734bebeee3dd0b9ac9d, "fanoutList");
            id_8daf6656e19046109021ed0e09df34c0.WireTo(layoutDiagram, "eventHappened");
            createNewALANode.WireTo(createAndPaintALAWire, "output");
            id_98261c05e417447a8d95e965b6a16b84.WireTo(id_2394034f78e445318e0b76c390aeca64, "children");
            id_98261c05e417447a8d95e965b6a16b84.WireTo(id_0a045762f07b43e09ba09d10ded9c5a1, "children");
            id_2394034f78e445318e0b76c390aeca64.WireTo(id_3a42cddbe7c3451cae71d6163a07d84b, "children");
            id_2394034f78e445318e0b76c390aeca64.WireTo(id_7b7249c8ba2443c18188d726eed5ddf3, "children");
            id_3a42cddbe7c3451cae71d6163a07d84b.WireTo(id_7b08320ddfe248718d4424b3c4d58d52, "clickedEvent");
            id_7b08320ddfe248718d4424b3c4d58d52.WireTo(id_cfa60f6b93c243f39b873a428a8790c9, "selectedFolderPathOutput");
            id_69e7fa1185c34c01aeb711254e850b05.WireTo(id_b1f51e8156e84abf9519ee826b310769, "foundFiles");
            id_fb170d142f834f5e872b58af20476911.WireTo(id_5caffdfbb31e420a8239b66e80955aa8, "output");
            id_5caffdfbb31e420a8239b66e80955aa8.WireTo(id_a6ffb9b2088b4704b5463d9f9a6124c0, "elementOutput");
            id_a3784e11a49a488eb06adde362524bd1.WireTo(id_3ca59fdbc12e4be8b1a0e903bcac3164, "dataOutput");
            id_3ca59fdbc12e4be8b1a0e903bcac3164.WireTo(createNewALANode, "output");
            id_c568c64d2a4f4bef976e0963b22c1f01.WireTo(id_0761bfc4af4048868a1d69c937a32156, "filePathInput");
            id_c568c64d2a4f4bef976e0963b22c1f01.WireTo(id_cfa60f6b93c243f39b873a428a8790c9, "settingJsonOutput");
            id_36bc6ed720864c7da326e5a2778d9bd5.WireTo(id_1b8e251463014a43a8d1006290941440, "senderOutput");
            id_0a045762f07b43e09ba09d10ded9c5a1.WireTo(id_46402a90d31b413d99414a0266a25a08, "children");
            id_0a045762f07b43e09ba09d10ded9c5a1.WireTo(id_841b4cd516b64304a046afcb7a2417eb, "children");
            id_0a045762f07b43e09ba09d10ded9c5a1.WireTo(id_4d01de40a3cf470a8f6a79d34eecd227, "children");
            id_46402a90d31b413d99414a0266a25a08.WireTo(id_09c8659d61ba49d589cd011d3ee011bd, "clickedEvent");
            id_09c8659d61ba49d589cd011d3ee011bd.WireTo(id_04155bb492d24717879b41689160f164, "children");
            id_04155bb492d24717879b41689160f164.WireTo(id_6a5384dab5e94be9b4169f8268772052, "uiLayout");
            id_cfa60f6b93c243f39b873a428a8790c9.WireTo(id_69e7fa1185c34c01aeb711254e850b05, "fanoutList");
            id_cfa60f6b93c243f39b873a428a8790c9.WireTo(projectFolderWatcher, "fanoutList");
            id_cfa60f6b93c243f39b873a428a8790c9.WireTo(id_9c5eb0644c0147fdbc2b55dd5c08796f, "fanoutList");
            projectFolderWatcher.WireTo(id_b3e6f2e9a36643418a69dd7f6e5ebbeb, "changedFile");
            id_b3e6f2e9a36643418a69dd7f6e5ebbeb.WireTo(id_f980107594ab46af8b1a58adae0a8eb8, "output");
            id_f980107594ab46af8b1a58adae0a8eb8.WireTo(id_bf50f7ec71cb4734bebeee3dd0b9ac9d, "eventOutput");
            id_64a90cddfb164dd28c09f76aa6c02080.WireTo(id_4aed31a491824a90bfa7a3a516d4ac15, "argsOutput");
            id_21062f6fa88149efb814af1357f700ca.WireTo(id_bd424287c27645a0949c577aecd4848d, "argsOutput");
            id_0223968931234184bc3ea5c8c9a653e9.WireTo(id_430d8ef174374904bee7b283dec49a29, "transitionOutput");
            id_430d8ef174374904bee7b283dec49a29.WireTo(id_fdf2cf28be1545f0978ecc377df7e142, "output");
            id_fdf2cf28be1545f0978ecc377df7e142.WireTo(layoutDiagram, "ifOutput");
            id_f54a15b16b194f0297b551c9126abaf1.WireTo(id_c568c64d2a4f4bef976e0963b22c1f01, "fanoutList");
            id_f54a15b16b194f0297b551c9126abaf1.WireTo(id_0223968931234184bc3ea5c8c9a653e9, "fanoutList");
            id_f54a15b16b194f0297b551c9126abaf1.WireTo(id_ca0c8ffb0fea4cb1bd87f6efb3caf04f, "fanoutList");
            id_f54a15b16b194f0297b551c9126abaf1.WireTo(id_fbb638a63f774d919bd331e242687030, "complete");
            id_3cc331e2ade44d8f856bb0912fa1b4b8.WireTo(id_730230c7a29e46dba21589cbb4062941, "eventHappened");
            id_09120bb1632c495595a829ae309d0a05.WireTo(layoutDiagram, "clickedEvent");
            id_84447fc36c5a4fd18a84bcdc105c861d.WireTo(id_a0d389a58a124657826b587cb24c793f, "dataOutput");
            id_a0d389a58a124657826b587cb24c793f.WireTo(id_31b9b7d02bcc450bbe909f7bc8194099, "output");
            id_7b7249c8ba2443c18188d726eed5ddf3.WireTo(id_26adb93de4fe4475bc797f2acc73d9f8, "clickedEvent");
            id_26adb93de4fe4475bc797f2acc73d9f8.WireTo(id_03ae7251e217413b8f2c78106da0dd79, "selectedFilePathOutput");
            id_0b1b264d0de04034abea6ee29816f8a7.WireTo(id_ed085fd9854241f1a269c76f1f7d901a, "fileContentOutput");
            id_3e3d4b6d6d8d4858b0396219dcaf9295.WireTo(id_5caffdfbb31e420a8239b66e80955aa8, "output");
            id_2cf10c1414b9438785ab6519ad64c724.WireTo(id_5caffdfbb31e420a8239b66e80955aa8, "output");
            id_b1f51e8156e84abf9519ee826b310769.WireTo(id_fb170d142f834f5e872b58af20476911, "fanoutList");
            id_b1f51e8156e84abf9519ee826b310769.WireTo(id_3e3d4b6d6d8d4858b0396219dcaf9295, "fanoutList");
            id_b1f51e8156e84abf9519ee826b310769.WireTo(id_2cf10c1414b9438785ab6519ad64c724, "fanoutList");
            id_b1f51e8156e84abf9519ee826b310769.WireTo(id_589f58d662434cf4b3eb44d7358449b7, "fanoutList");
            id_e1ededa54cb649a88ffcc248120c39c2.WireTo(id_2086aeb04dfe4c01ae0f4a669772537f, "dataOutput");
            id_539a875b07ab449caaae0490c824ae36.WireTo(id_15daff883d3c4b0ba48ed38e9afd027c, "dataOutput");
            id_2086aeb04dfe4c01ae0f4a669772537f.WireTo(id_08d97753ddb44d25b2739d35def1a5a6, "fanoutList");
            id_2086aeb04dfe4c01ae0f4a669772537f.WireTo(id_6efae636bc3d4a6584cb9451151ff14a, "fanoutList");
            id_15daff883d3c4b0ba48ed38e9afd027c.WireTo(id_daf3074eb03c41d7940d1fcbaf0ef65e, "fanoutList");
            id_15daff883d3c4b0ba48ed38e9afd027c.WireTo(id_6efae636bc3d4a6584cb9451151ff14a, "fanoutList");
            id_e4d9250ea81e4bff8454bc54bf7fb787.WireTo(id_09071d95d2844b20849c11febdaa4596, "argsOutput");
            id_09071d95d2844b20849c11febdaa4596.WireTo(id_dae387347c174a6e8036f54c7af21eae, "output");
            id_dae387347c174a6e8036f54c7af21eae.WireTo(id_e1ededa54cb649a88ffcc248120c39c2, "ifOutput");
            id_dae387347c174a6e8036f54c7af21eae.WireTo(id_539a875b07ab449caaae0490c824ae36, "elseOutput");
            id_ed085fd9854241f1a269c76f1f7d901a.WireTo(id_bf3c7eef31d949e885a25d2854668024, "fanoutList");
            id_ed085fd9854241f1a269c76f1f7d901a.WireTo(id_de4233d94c534f1ea34fd55b967b8707, "fanoutList");
            id_de4233d94c534f1ea34fd55b967b8707.WireTo(layoutDiagram, "eventOutput");
            id_b3a410ac8bcb4bd48f61e2abcd772a55.WireTo(getFirstRoot, "delayedEvent");
            id_841b4cd516b64304a046afcb7a2417eb.WireTo(generateCode, "clickedEvent");
            id_86f3bd33841b4a228c3c38dd3391c9bc.WireTo(id_0d6abbea0ad64c4bb689bd7ea169d8c4, "wireTos");
            id_86f3bd33841b4a228c3c38dd3391c9bc.WireTo(id_069b428be7294a67be3efb898aae6232, "instantiations");
            id_ca0c8ffb0fea4cb1bd87f6efb3caf04f.WireTo(id_c2baa61645514db38bb2bdcec4401901, "filePathInput");
            id_4a806bcc9d3e470682727913114356b9.WireTo(id_ce6adbee004047ad809c62b2fe14365c, "filePathInput");
            id_4ee812e61dc84c59b196fb08e77a8edd.WireTo(id_4a806bcc9d3e470682727913114356b9, "output");
            id_03ae7251e217413b8f2c78106da0dd79.WireTo(id_0b1b264d0de04034abea6ee29816f8a7, "fanoutList");
            id_03ae7251e217413b8f2c78106da0dd79.WireTo(id_4ee812e61dc84c59b196fb08e77a8edd, "fanoutList");
            id_9c5eb0644c0147fdbc2b55dd5c08796f.WireTo(id_c672849f94d74ae6a86dd81077ff13fe, "output");
            id_589f58d662434cf4b3eb44d7358449b7.WireTo(id_5caffdfbb31e420a8239b66e80955aa8, "output");
            id_e4ca095bc4024827bfa22625f6a1835e.WireTo(id_addc2af8003f4b18ae421ab7a79b26b4, "filePathInput");
            id_e4ca095bc4024827bfa22625f6a1835e.WireTo(id_786167b281e04de49ba5ad483e1ff246, "settingJsonOutput");
            id_0d6abbea0ad64c4bb689bd7ea169d8c4.WireTo(id_bb380057584d4e3dbc2569a5232eb5a4, "newFileContentsOutput");
            id_58617bcea7554d0787a726d9b07c214b.WireTo(id_5f3773e2981640708e316f6fd9fd6431, "fileContentOutput");
            id_5f3773e2981640708e316f6fd9fd6431.WireTo(id_069b428be7294a67be3efb898aae6232, "fanoutList");
            generateCode.WireTo(id_e4ca095bc4024827bfa22625f6a1835e, "fanoutList");
            generateCode.WireTo(id_86f3bd33841b4a228c3c38dd3391c9bc, "fanoutList");
            id_29a95f9b55f64df086af7bfc24b165a3.WireTo(id_069b428be7294a67be3efb898aae6232, "fanoutList");
            id_29a95f9b55f64df086af7bfc24b165a3.WireTo(id_0d6abbea0ad64c4bb689bd7ea169d8c4, "fanoutList");
            generateCode.WireTo(id_29a95f9b55f64df086af7bfc24b165a3, "complete");
            id_c672849f94d74ae6a86dd81077ff13fe.WireTo(id_f89c71e9f0fe423987218a4e91f430d0, "filePathInput");
            id_bb380057584d4e3dbc2569a5232eb5a4.WireTo(id_786167b281e04de49ba5ad483e1ff246, "filePathInput");
            id_786167b281e04de49ba5ad483e1ff246.WireTo(id_58617bcea7554d0787a726d9b07c214b, "fanoutList");
            id_069b428be7294a67be3efb898aae6232.WireTo(id_0d6abbea0ad64c4bb689bd7ea169d8c4, "newFileContentsOutput");
            id_4d01de40a3cf470a8f6a79d34eecd227.WireTo(id_c6eb0cfba7ce4a118f70cb255e36168d, "clickedEvent");
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






