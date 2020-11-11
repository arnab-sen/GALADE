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
            Vertical id_1bac690327e048c5956c3f6a1deb9a9d = new Vertical() {  };
            CanvasDisplay id_6d2fd70706e84b329a800c1ef3be39e4 = new CanvasDisplay() { Width = 1920, Height = 1080, Background = Brushes.White, StateTransition = stateTransition, Canvas = mainCanvas };
            KeyEvent id_4668ef2c02254d8783a1f5a1e6d2a7d4 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.A }, Condition = args => mainGraph.Get("SelectedNode") != null&& stateTransition.CurrentStateMatches(Enums.DiagramMode.IdleSelected) };
            ContextMenu id_a480e806c7de437880e399b703805838 = new ContextMenu() {  };
            MenuItem id_018d857185044ace97a9870f72c8d363 = new MenuItem(header: "Add root") {  };
            EventConnector id_9f92c6af8bfc4931a05b9736da93a3b8 = new EventConnector() {  };
            Data<ALANode> getFirstRoot = new Data<ALANode>() { InstanceName = "getFirstRoot", Lambda = () => mainGraph.Roots.FirstOrDefault() as ALANode };
            RightTreeLayout<ALANode> id_8f88e251a93847e8b7de87025ebab0c9 = new RightTreeLayout<ALANode>() { GetID = n => n.Id, GetWidth = n => n.Width, GetHeight = n => n.Height, SetX = (n, x) => n.PositionX = x, SetY = (n, y) => n.PositionY = y, GetChildren = n => mainGraph.Edges.Where(e => e is ALAWire wire && wire.Source != null && wire.Destination != null && wire.Source == n).Select(e => ((e as ALAWire).Destination) as ALANode), HorizontalGap = 100, VerticalGap = 20, InitialX = 50, InitialY = 50 };
            EventConnector layoutDiagram = new EventConnector() { InstanceName = "layoutDiagram" };
            DataFlowConnector<ALANode> id_36aff5167d8945a78f2c4c378a7efc06 = new DataFlowConnector<ALANode>() {  };
            ApplyAction<ALANode> id_0a56668b6e05478fa80e8840e4b6f4ca = new ApplyAction<ALANode>() { Lambda = node =>{Dispatcher.CurrentDispatcher.Invoke(() => {var edges = mainGraph.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);} };
            KeyEvent id_9da5c762f60d463eb4f272dc5a14dc76 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.R }, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            Apply<AbstractionModel, object> createNewALANode = new Apply<AbstractionModel, object>() { InstanceName = "createNewALANode", Lambda = input => {var node = new ALANode();node.Model = input;node.Graph = mainGraph;node.Canvas = mainCanvas;node.StateTransition = stateTransition;node.AvailableDomainAbstractions.AddRange( abstractionModelManager.GetAbstractionTypes());node.TypeChanged += newType => {abstractionModelManager.UpdateAbstractionModel(abstractionModelManager.GetAbstractionModel(newType),node.Model);node.UpdateUI();Dispatcher.CurrentDispatcher.Invoke(() => {var edges = mainGraph.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);};mainGraph.AddNode(node);node.CreateInternals();mainCanvas.Children.Add(node.Render);return node;} };
            MenuBar id_53e62c67141a440a9a3bf41ae2fc30ba = new MenuBar() {  };
            MenuItem id_730165afcb4a4b4dba9cf27ad47e251b = new MenuItem(header: "File") {  };
            MenuItem id_0102d05328854a58baca74be81bba822 = new MenuItem(header: "Open Project") {  };
            FolderBrowser id_fc287e91629f46caa818e8713d88f3c9 = new FolderBrowser() { Description = "" };
            DirectorySearch id_63f40bc272be49c191391abafe60a412 = new DirectorySearch(directoriesToFind: new string[] { "DomainAbstractions", "ProgrammingParadigms", "RequirementsAbstractions", "Modules" }) { FilenameFilter = "*.cs" };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_23a8ab2a82684336868c81fb753c5f87 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("DomainAbstractions")){list = input["DomainAbstractions"];}return list;} };
            ForEach<string> id_744c65cc8c8e4f99b9d9fa448b6ac1a0 = new ForEach<string>() {  };
            ApplyAction<string> id_0f821bd2a21748d897d21e29f8d530d7 = new ApplyAction<string>() { Lambda = input =>{abstractionModelManager.CreateAbstractionModelFromPath(input);} };
            Data<string> id_c21df1ba3b9949aa8f4cdce874431152 = new Data<string>() { storedData = "Apply" };
            Apply<string, AbstractionModel> id_8280102e24a9441cbff65050ae981c40 = new Apply<string, AbstractionModel>() { Lambda = input =>{return abstractionModelManager.GetAbstractionModel(input);} };
            GetSetting id_18bd9d022fd543379c34c2397eda78c9 = new GetSetting(name: "ProjectFolderPath") {  };
            KeyEvent id_93d99e75ac104f76a1321b13f03fba20 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.Enter } };
            ApplyAction<object> id_7415e096d292451fba81ee1e426aaa12 = new ApplyAction<object>() { Lambda = input =>{(input as WPFCanvas).Focus();} };
            MenuItem id_917cdedb4d4b48688c4a146aee3900d0 = new MenuItem(header: "Debug") {  };
            MenuItem id_5c8f6a1dd623468ba0884e01614997cd = new MenuItem(header: "TextEditor test") {  };
            PopupWindow id_3229eda2842046acb26b044b86f85d7c = new PopupWindow(title: "") { Height = 720, Width = 1280, Resize = SizeToContent.WidthAndHeight };
            Box id_206ea38c01e14db8b863e29da514ed7c = new Box() { Width = 100, Height = 100 };
            TextEditor id_b7ab115f4fca42c2b2c4b017bcbadb7c = new TextEditor() { Width = 1280, Height = 720 };
            DataFlowConnector<string> id_6ca14376eac748a59d7408ad15b23c9d = new DataFlowConnector<string>() {  };
            FolderWatcher projectFolderWatcher = new FolderWatcher() { InstanceName = "projectFolderWatcher", RootPath = "", Filter = "*.cs", WatchSubdirectories = true, PathRegex = @".*\.cs$" };
            Apply<string, object> id_59b68df7393a42c3952f7cac9e66b1b8 = new Apply<string, object>() { Lambda = input =>{var newModel = abstractionModelManager.CreateAbstractionModelFromPath(input);foreach (var node in mainGraph.Nodes){var alaNode = node as ALANode;if (alaNode.Model.Type != newModel.Type) continue;abstractionModelManager.UpdateAbstractionModel(newModel, alaNode.Model);alaNode.UpdateUI();}return input;} };
            ConvertToEvent<object> id_7e3a40151eb344e7ae5800a49f35952f = new ConvertToEvent<object>() {  };
            MouseButtonEvent id_0ae00b97dab24d739d56320d55762818 = new MouseButtonEvent(eventName: "MouseRightButtonDown") { ExtractSender = null, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            ApplyAction<object> id_de35f670b6614158a46e8121b3423551 = new ApplyAction<object>() { Lambda = input =>{Mouse.Capture(input as WPFCanvas);stateTransition.Update(Enums.DiagramMode.Idle);} };
            MouseButtonEvent id_06a051d6356b4ab5b25652e52005292a = new MouseButtonEvent(eventName: "MouseRightButtonUp") { ExtractSender = null, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            ApplyAction<object> id_b941859c8b7e43c6b089b7b9a743b185 = new ApplyAction<object>() { Lambda = input =>{if (Mouse.Captured?.Equals(input) ?? false) Mouse.Capture(null);stateTransition.Update(Enums.DiagramMode.Idle);} };
            StateChangeListener id_073ac138bed14be6a3257d5aa25c1102 = new StateChangeListener() { StateTransition = stateTransition, CurrentStateShouldMatch = Enums.DiagramMode.All };
            Apply<Tuple<Enums.DiagramMode, Enums.DiagramMode>, bool> id_26707a2bae6e42018a73fbf3448adfb4 = new Apply<Tuple<Enums.DiagramMode, Enums.DiagramMode>, bool>() { Lambda = input =>{return input.Item1 == Enums.DiagramMode.AwaitingPortSelection &&input.Item2 == Enums.DiagramMode.Idle;} };
            IfElse id_77cc4b4bf206457aa0707cd62b0a8b3d = new IfElse() {  };
            EventConnector id_24a4f7de276d4d25a6d83f03287b91c4 = new EventConnector() {  };
            Apply<object, object> createAndPaintALAWire = new Apply<object, object>() { InstanceName = "createAndPaintALAWire", Lambda = input =>{var source = mainGraph.Get("SelectedNode") as ALANode;var destination = input as ALANode;var sourcePort = source.GetSelectedPort(inputPort: false);var destinationPort = destination.GetSelectedPort(inputPort: true);var wire = new ALAWire(){Graph = mainGraph,Canvas = mainCanvas,Source = source,Destination = destination,SourcePort = sourcePort,DestinationPort = destinationPort,StateTransition = stateTransition};mainGraph.AddEdge(wire);wire.Paint();return wire;} };
            KeyEvent id_6f6567556a3d4498922811e883718de7 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.Delete } };
            EventLambda id_f511275835d041dc81ff1d1e7ba2c934 = new EventLambda() { Lambda = () =>{var selectedNode = mainGraph.Get("SelectedNode") as ALANode;if (selectedNode == null) return;selectedNode.Delete(deleteAttachedWires: true);} };
            MenuItem id_393a0bdbfa9445b2a6aa9b72f328549d = new MenuItem(header: "Refresh") {  };
            Data<AbstractionModel> id_e9b33cf79bc64ee499c613f69d793554 = new Data<AbstractionModel>() { Lambda = () => abstractionModelManager.GetAbstractionModel(abstractionModelManager.GetAbstractionTypes().FirstOrDefault()) };
            Apply<AbstractionModel, object> id_2c6f2b5ec3af41c890d06f991bcfe787 = new Apply<AbstractionModel, object>() { Lambda = createNewALANode.Lambda };
            ApplyAction<object> id_fe9b707ec6d34b39bae8c47ad358a13b = new ApplyAction<object>() { Lambda = input =>{var alaNode = input as ALANode;var mousePos = Mouse.GetPosition(mainCanvas);alaNode.PositionX = mousePos.X;alaNode.PositionY = mousePos.Y;mainGraph.Set("LatestNode", input);if (mainGraph.Get("SelectedNode") == null){mainGraph.Set("SelectedNode", input);}mainGraph.Roots.Add(input);} };
            MenuItem id_12b6da8a39364c0ba13669f3184e2881 = new MenuItem(header: "Open Code File") {  };
            FileBrowser id_ee0a515e899b435487d3253fea73553a = new FileBrowser() { Mode = "Open" };
            FileReader id_4282da786aaa49f6bc4626ea8f406550 = new FileReader() {  };
            CreateDiagramFromCode id_26334af38aff4f90ba86d4fc6c1917e2 = new CreateDiagramFromCode() { Graph = mainGraph, Canvas = mainCanvas, ModelManager = abstractionModelManager, StateTransition = stateTransition };
            EventConnector id_107b41f3ebb643cab8759eaeace75128 = new EventConnector() {  };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_470e37a1122647e59d26d761462815be = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("ProgrammingParadigms")){list = input["ProgrammingParadigms"];}return list;} };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_ccd3b8cd7a014b278f23dcc12a2365ff = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("RequirementsAbstractions")){list = input["RequirementsAbstractions"];}return list;} };
            DataFlowConnector<Dictionary<string, List<string>>> id_5cb778da617c47c8b823587747365efa = new DataFlowConnector<Dictionary<string, List<string>>>() {  };
            Data<UIElement> id_0178b984a11848dbb7dfb4f9f4ec0ee4 = new Data<UIElement>() { Lambda = () => mainCanvas };
            Scale id_befdcba1b58f4deb985c5ffa88b0b7a0 = new Scale() { WidthMultiplier = 1.1, HeightMultiplier = 1.1 };
            Data<UIElement> id_30ba35edf0a044879f4ddc8b051a90a5 = new Data<UIElement>() { Lambda = () => mainCanvas };
            Scale id_182d30e7e2194c15977c8af69728b897 = new Scale() { WidthMultiplier = 0.9, HeightMultiplier = 0.9 };
            DataFlowConnector<UIElement> id_c7dbb329107d48daab53486e3407657d = new DataFlowConnector<UIElement>() {  };
            DataFlowConnector<UIElement> id_54f7d906df0e496a86f8b36c53dc0c38 = new DataFlowConnector<UIElement>() {  };
            ApplyAction<UIElement> id_9bc20ab2fd534226937bb88728040474 = new ApplyAction<UIElement>() { Lambda = input => {if (!(input.RenderTransform is ScaleTransform)) return;var transform = input.RenderTransform as ScaleTransform;var minScale = 0.6;/*Logging.Log($"Scale: {transform.ScaleX}, {transform.ScaleX}");*/bool nodeIsTooSmall = transform.ScaleX < minScale && transform.ScaleY < minScale;var nodes = mainGraph.Nodes;foreach (var node in nodes){if (node is ALANode alaNode) alaNode.ShowTypeTextMask(nodeIsTooSmall);}} };
            MouseWheelEvent id_b16bc00a7ee14900bfec6fdbffe15fb2 = new MouseWheelEvent(eventName: "MouseWheel") {  };
            Apply<MouseWheelEventArgs, bool> id_8ebf849257ec46668df231141c2b4d48 = new Apply<MouseWheelEventArgs, bool>() { Lambda = args =>{return args.Delta > 0;} };
            IfElse id_dc92199646074c119461ad2e3231526c = new IfElse() {  };
            DataFlowConnector<string> id_e7164e6a1e2b4e7ca0ee0a5098410afa = new DataFlowConnector<string>() {  };
            ConvertToEvent<string> id_93fb7699b5eb4717972cf992ecfa80df = new ConvertToEvent<string>() {  };
            DispatcherEvent id_f10a082c130d4c35b897bd30108f4b8d = new DispatcherEvent() { Priority = DispatcherPriority.ApplicationIdle };
            MenuItem id_8698cc76f97c43d2aa45d98aef64d5d6 = new MenuItem(header: "Generate Code") {  };
            GenerateALACode id_ac0ab1f71d514e8681bd6c5a9c351f79 = new GenerateALACode() { Graph = mainGraph };
            DataFlowConnector<List<string>> generatedInstantiations = new DataFlowConnector<List<string>>() { InstanceName = "generatedInstantiations" };
            DataFlowConnector<List<string>> generatedWireTos = new DataFlowConnector<List<string>>() { InstanceName = "generatedWireTos" };
            GetSetting id_f7a9e4b38afd4d6199d9c5e22fdb17cf = new GetSetting(name: "ApplicationCodeFilePath") {  };
            Data<string> id_ec9634f4f667478880e127a2ae6f3fce = new Data<string>() { storedData = SETTINGS_FILEPATH };
            EditSetting id_ada202987ff348c8af7f446f5fc705ac = new EditSetting() { JSONPath = "$..ProjectFolderPath" };
            Data<string> id_1bf9e0f91d6c4f63bbca1ba11c561cb0 = new Data<string>() { storedData = SETTINGS_FILEPATH };
            Cast<string, object> id_dd5807d3ffa5416b97b1b1545c162d75 = new Cast<string, object>() {  };
            DataFlowConnector<string> id_ab400da881684a97a43abcb43e52a155 = new DataFlowConnector<string>() {  };
            Cast<string, object> id_9bfc3fa948124a77b0775db221383c07 = new Cast<string, object>() {  };
            Data<string> id_8f0c7e7581054d37ab51b7a37368294e = new Data<string>() { storedData = SETTINGS_FILEPATH };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_bd279b9fbe8f4a23973e0c4f0d36c6d2 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("Modules")){list = input["Modules"];}return list;} };
            // END AUTO-GENERATED INSTANTIATIONS FOR Application.xmind

            // BEGIN AUTO-GENERATED WIRING FOR Application.xmind
            mainWindow.WireTo(id_1bac690327e048c5956c3f6a1deb9a9d, "iuiStructure");
            mainWindow.WireTo(id_24a4f7de276d4d25a6d83f03287b91c4, "appStart");
            id_1bac690327e048c5956c3f6a1deb9a9d.WireTo(id_53e62c67141a440a9a3bf41ae2fc30ba, "children");
            id_1bac690327e048c5956c3f6a1deb9a9d.WireTo(id_6d2fd70706e84b329a800c1ef3be39e4, "children");
            id_6d2fd70706e84b329a800c1ef3be39e4.WireTo(id_4668ef2c02254d8783a1f5a1e6d2a7d4, "eventHandlers");
            id_6d2fd70706e84b329a800c1ef3be39e4.WireTo(id_9da5c762f60d463eb4f272dc5a14dc76, "eventHandlers");
            id_6d2fd70706e84b329a800c1ef3be39e4.WireTo(id_93d99e75ac104f76a1321b13f03fba20, "eventHandlers");
            id_6d2fd70706e84b329a800c1ef3be39e4.WireTo(id_0ae00b97dab24d739d56320d55762818, "eventHandlers");
            id_6d2fd70706e84b329a800c1ef3be39e4.WireTo(id_06a051d6356b4ab5b25652e52005292a, "eventHandlers");
            id_6d2fd70706e84b329a800c1ef3be39e4.WireTo(id_6f6567556a3d4498922811e883718de7, "eventHandlers");
            id_6d2fd70706e84b329a800c1ef3be39e4.WireTo(id_b16bc00a7ee14900bfec6fdbffe15fb2, "eventHandlers");
            id_6d2fd70706e84b329a800c1ef3be39e4.WireTo(id_a480e806c7de437880e399b703805838, "contextMenu");
            id_4668ef2c02254d8783a1f5a1e6d2a7d4.WireTo(id_9f92c6af8bfc4931a05b9736da93a3b8, "eventHappened");
            id_a480e806c7de437880e399b703805838.WireTo(id_018d857185044ace97a9870f72c8d363, "children");
            id_a480e806c7de437880e399b703805838.WireTo(id_393a0bdbfa9445b2a6aa9b72f328549d, "children");
            id_018d857185044ace97a9870f72c8d363.WireTo(id_e9b33cf79bc64ee499c613f69d793554, "clickedEvent");
            id_9f92c6af8bfc4931a05b9736da93a3b8.WireTo(id_c21df1ba3b9949aa8f4cdce874431152, "fanoutList");
            id_9f92c6af8bfc4931a05b9736da93a3b8.WireTo(layoutDiagram, "complete");
            getFirstRoot.WireTo(id_36aff5167d8945a78f2c4c378a7efc06, "dataOutput");
            layoutDiagram.WireTo(id_f10a082c130d4c35b897bd30108f4b8d, "fanoutList");
            id_36aff5167d8945a78f2c4c378a7efc06.WireTo(id_8f88e251a93847e8b7de87025ebab0c9, "fanoutList");
            id_36aff5167d8945a78f2c4c378a7efc06.WireTo(id_0a56668b6e05478fa80e8840e4b6f4ca, "fanoutList");
            id_9da5c762f60d463eb4f272dc5a14dc76.WireTo(layoutDiagram, "eventHappened");
            createNewALANode.WireTo(createAndPaintALAWire, "output");
            id_53e62c67141a440a9a3bf41ae2fc30ba.WireTo(id_730165afcb4a4b4dba9cf27ad47e251b, "children");
            id_53e62c67141a440a9a3bf41ae2fc30ba.WireTo(id_917cdedb4d4b48688c4a146aee3900d0, "children");
            id_730165afcb4a4b4dba9cf27ad47e251b.WireTo(id_0102d05328854a58baca74be81bba822, "children");
            id_730165afcb4a4b4dba9cf27ad47e251b.WireTo(id_12b6da8a39364c0ba13669f3184e2881, "children");
            id_0102d05328854a58baca74be81bba822.WireTo(id_fc287e91629f46caa818e8713d88f3c9, "clickedEvent");
            id_fc287e91629f46caa818e8713d88f3c9.WireTo(id_6ca14376eac748a59d7408ad15b23c9d, "selectedFolderPathOutput");
            id_63f40bc272be49c191391abafe60a412.WireTo(id_5cb778da617c47c8b823587747365efa, "foundFiles");
            id_23a8ab2a82684336868c81fb753c5f87.WireTo(id_744c65cc8c8e4f99b9d9fa448b6ac1a0, "output");
            id_470e37a1122647e59d26d761462815be.WireTo(id_744c65cc8c8e4f99b9d9fa448b6ac1a0, "output");
            id_ccd3b8cd7a014b278f23dcc12a2365ff.WireTo(id_744c65cc8c8e4f99b9d9fa448b6ac1a0, "output");
            id_bd279b9fbe8f4a23973e0c4f0d36c6d2.WireTo(id_744c65cc8c8e4f99b9d9fa448b6ac1a0, "output");
            id_744c65cc8c8e4f99b9d9fa448b6ac1a0.WireTo(id_0f821bd2a21748d897d21e29f8d530d7, "elementOutput");
            id_c21df1ba3b9949aa8f4cdce874431152.WireTo(id_8280102e24a9441cbff65050ae981c40, "dataOutput");
            id_8280102e24a9441cbff65050ae981c40.WireTo(createNewALANode, "output");
            id_18bd9d022fd543379c34c2397eda78c9.WireTo(id_8f0c7e7581054d37ab51b7a37368294e, "filePathInput");
            id_18bd9d022fd543379c34c2397eda78c9.WireTo(id_6ca14376eac748a59d7408ad15b23c9d, "settingJsonOutput");
            id_93d99e75ac104f76a1321b13f03fba20.WireTo(id_7415e096d292451fba81ee1e426aaa12, "senderOutput");
            id_917cdedb4d4b48688c4a146aee3900d0.WireTo(id_5c8f6a1dd623468ba0884e01614997cd, "children");
            id_917cdedb4d4b48688c4a146aee3900d0.WireTo(id_8698cc76f97c43d2aa45d98aef64d5d6, "children");
            id_5c8f6a1dd623468ba0884e01614997cd.WireTo(id_3229eda2842046acb26b044b86f85d7c, "clickedEvent");
            id_3229eda2842046acb26b044b86f85d7c.WireTo(id_206ea38c01e14db8b863e29da514ed7c, "children");
            id_206ea38c01e14db8b863e29da514ed7c.WireTo(id_b7ab115f4fca42c2b2c4b017bcbadb7c, "uiLayout");
            id_6ca14376eac748a59d7408ad15b23c9d.WireTo(id_63f40bc272be49c191391abafe60a412, "fanoutList");
            id_6ca14376eac748a59d7408ad15b23c9d.WireTo(projectFolderWatcher, "fanoutList");
            id_6ca14376eac748a59d7408ad15b23c9d.WireTo(id_9bfc3fa948124a77b0775db221383c07, "fanoutList");
            projectFolderWatcher.WireTo(id_59b68df7393a42c3952f7cac9e66b1b8, "changedFile");
            id_59b68df7393a42c3952f7cac9e66b1b8.WireTo(id_7e3a40151eb344e7ae5800a49f35952f, "output");
            id_7e3a40151eb344e7ae5800a49f35952f.WireTo(id_0a56668b6e05478fa80e8840e4b6f4ca, "eventOutput");
            id_0ae00b97dab24d739d56320d55762818.WireTo(id_de35f670b6614158a46e8121b3423551, "argsOutput");
            id_06a051d6356b4ab5b25652e52005292a.WireTo(id_b941859c8b7e43c6b089b7b9a743b185, "argsOutput");
            id_073ac138bed14be6a3257d5aa25c1102.WireTo(id_26707a2bae6e42018a73fbf3448adfb4, "transitionOutput");
            id_26707a2bae6e42018a73fbf3448adfb4.WireTo(id_77cc4b4bf206457aa0707cd62b0a8b3d, "output");
            id_77cc4b4bf206457aa0707cd62b0a8b3d.WireTo(layoutDiagram, "ifOutput");
            id_24a4f7de276d4d25a6d83f03287b91c4.WireTo(id_18bd9d022fd543379c34c2397eda78c9, "fanoutList");
            id_24a4f7de276d4d25a6d83f03287b91c4.WireTo(id_073ac138bed14be6a3257d5aa25c1102, "fanoutList");
            id_24a4f7de276d4d25a6d83f03287b91c4.WireTo(id_f7a9e4b38afd4d6199d9c5e22fdb17cf, "fanoutList");
            id_24a4f7de276d4d25a6d83f03287b91c4.WireTo(id_107b41f3ebb643cab8759eaeace75128, "complete");
            id_6f6567556a3d4498922811e883718de7.WireTo(id_f511275835d041dc81ff1d1e7ba2c934, "eventHappened");
            id_393a0bdbfa9445b2a6aa9b72f328549d.WireTo(layoutDiagram, "clickedEvent");
            id_e9b33cf79bc64ee499c613f69d793554.WireTo(id_2c6f2b5ec3af41c890d06f991bcfe787, "dataOutput");
            id_2c6f2b5ec3af41c890d06f991bcfe787.WireTo(id_fe9b707ec6d34b39bae8c47ad358a13b, "output");
            id_12b6da8a39364c0ba13669f3184e2881.WireTo(id_ee0a515e899b435487d3253fea73553a, "clickedEvent");
            id_ee0a515e899b435487d3253fea73553a.WireTo(id_ab400da881684a97a43abcb43e52a155, "selectedFilePathOutput");
            id_ab400da881684a97a43abcb43e52a155.WireTo(id_4282da786aaa49f6bc4626ea8f406550, "fanoutList");
            id_4282da786aaa49f6bc4626ea8f406550.WireTo(id_e7164e6a1e2b4e7ca0ee0a5098410afa, "fileContentOutput");
            id_5cb778da617c47c8b823587747365efa.WireTo(id_23a8ab2a82684336868c81fb753c5f87, "fanoutList");
            id_5cb778da617c47c8b823587747365efa.WireTo(id_470e37a1122647e59d26d761462815be, "fanoutList");
            id_5cb778da617c47c8b823587747365efa.WireTo(id_ccd3b8cd7a014b278f23dcc12a2365ff, "fanoutList");
            id_5cb778da617c47c8b823587747365efa.WireTo(id_bd279b9fbe8f4a23973e0c4f0d36c6d2, "fanoutList");
            id_0178b984a11848dbb7dfb4f9f4ec0ee4.WireTo(id_c7dbb329107d48daab53486e3407657d, "dataOutput");
            id_30ba35edf0a044879f4ddc8b051a90a5.WireTo(id_54f7d906df0e496a86f8b36c53dc0c38, "dataOutput");
            id_c7dbb329107d48daab53486e3407657d.WireTo(id_befdcba1b58f4deb985c5ffa88b0b7a0, "fanoutList");
            id_c7dbb329107d48daab53486e3407657d.WireTo(id_9bc20ab2fd534226937bb88728040474, "fanoutList");
            id_54f7d906df0e496a86f8b36c53dc0c38.WireTo(id_182d30e7e2194c15977c8af69728b897, "fanoutList");
            id_54f7d906df0e496a86f8b36c53dc0c38.WireTo(id_9bc20ab2fd534226937bb88728040474, "fanoutList");
            id_b16bc00a7ee14900bfec6fdbffe15fb2.WireTo(id_8ebf849257ec46668df231141c2b4d48, "argsOutput");
            id_8ebf849257ec46668df231141c2b4d48.WireTo(id_dc92199646074c119461ad2e3231526c, "output");
            id_dc92199646074c119461ad2e3231526c.WireTo(id_0178b984a11848dbb7dfb4f9f4ec0ee4, "ifOutput");
            id_dc92199646074c119461ad2e3231526c.WireTo(id_30ba35edf0a044879f4ddc8b051a90a5, "elseOutput");
            id_e7164e6a1e2b4e7ca0ee0a5098410afa.WireTo(id_26334af38aff4f90ba86d4fc6c1917e2, "fanoutList");
            id_e7164e6a1e2b4e7ca0ee0a5098410afa.WireTo(id_93fb7699b5eb4717972cf992ecfa80df, "fanoutList");
            id_dd5807d3ffa5416b97b1b1545c162d75.WireTo(id_ada202987ff348c8af7f446f5fc705ac, "output");
            id_ab400da881684a97a43abcb43e52a155.WireTo(id_dd5807d3ffa5416b97b1b1545c162d75, "fanoutList");
            id_93fb7699b5eb4717972cf992ecfa80df.WireTo(layoutDiagram, "eventOutput");
            id_f10a082c130d4c35b897bd30108f4b8d.WireTo(getFirstRoot, "delayedEvent");
            id_8698cc76f97c43d2aa45d98aef64d5d6.WireTo(id_ac0ab1f71d514e8681bd6c5a9c351f79, "clickedEvent");
            id_ac0ab1f71d514e8681bd6c5a9c351f79.WireTo(generatedInstantiations, "instantiations");
            id_ac0ab1f71d514e8681bd6c5a9c351f79.WireTo(generatedWireTos, "wireTos");
            id_f7a9e4b38afd4d6199d9c5e22fdb17cf.WireTo(id_ec9634f4f667478880e127a2ae6f3fce, "filePathInput");
            id_ada202987ff348c8af7f446f5fc705ac.WireTo(id_1bf9e0f91d6c4f63bbca1ba11c561cb0, "filePathInput");
            id_9bfc3fa948124a77b0775db221383c07.WireTo(id_ada202987ff348c8af7f446f5fc705ac, "output");
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


































































































































































































































































