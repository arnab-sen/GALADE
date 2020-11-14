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
using System.Text;
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

            var globalMessages = new List<string>();

            Logging.MessageOutput += message =>
            {
                globalMessages.Add(message);
                Logging.Log(message);
            };

            #endregion

            Graph mainGraph = new Graph();

            WPFCanvas mainCanvas = new WPFCanvas();
            AbstractionModelManager abstractionModelManager = new AbstractionModelManager();

            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR Application.xmind
            MainWindow mainWindow = new MainWindow(title: "GALADE") { InstanceName = "mainWindow" };
            Vertical id_7c078701861e4f67afc100e7bbe7c106 = new Vertical() { Layouts = new[] {0, 2, 0} };
            CanvasDisplay id_720a828eadfa48bfb8ffa5e59a1971ae = new CanvasDisplay() { Width = 1280, Height = 720, Background = Brushes.White, StateTransition = stateTransition, Canvas = mainCanvas };
            KeyEvent id_059d15fbce7442c2a229823ff75052d8 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.A }, Condition = args => mainGraph.Get("SelectedNode") != null&& stateTransition.CurrentStateMatches(Enums.DiagramMode.IdleSelected) };
            ContextMenu id_d6bccf60bded43cf93644dd0b689dd73 = new ContextMenu() {  };
            MenuItem id_fb979b19beb742ce82cf3b8cedd8f567 = new MenuItem(header: "Add root") {  };
            EventConnector id_e0a20720c820440aad32438da5e5fe42 = new EventConnector() {  };
            Data<ALANode> getFirstRoot = new Data<ALANode>() { InstanceName = "getFirstRoot", Lambda = () => mainGraph.Roots.FirstOrDefault() as ALANode };
            RightTreeLayout<ALANode> id_d5b7081d7d8f4ae7a2f84f77e07309c9 = new RightTreeLayout<ALANode>() { GetID = n => n.Id, GetWidth = n => n.Width, GetHeight = n => n.Height, SetX = (n, x) => n.PositionX = x, SetY = (n, y) => n.PositionY = y, GetChildren = n => mainGraph.Edges.Where(e => e is ALAWire wire && wire.Source != null && wire.Destination != null && wire.Source == n).Select(e => ((e as ALAWire).Destination) as ALANode), HorizontalGap = 100, VerticalGap = 20, InitialX = 50, InitialY = 50 };
            EventConnector layoutDiagram = new EventConnector() { InstanceName = "layoutDiagram" };
            DataFlowConnector<ALANode> id_bfd1544f03514a4c9547c381dae58dac = new DataFlowConnector<ALANode>() {  };
            ApplyAction<ALANode> id_61903a17294a4bc1929bad491a4b3e9b = new ApplyAction<ALANode>() { Lambda = node =>{Dispatcher.CurrentDispatcher.Invoke(() => {var edges = mainGraph.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);} };
            KeyEvent id_bcbbc36b3d64495ebc80e061f0417def = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.R }, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            Apply<AbstractionModel, object> createNewALANode = new Apply<AbstractionModel, object>() { InstanceName = "createNewALANode", Lambda = input => {var node = new ALANode();node.Model = input;node.Graph = mainGraph;node.Canvas = mainCanvas;node.StateTransition = stateTransition;node.AvailableDomainAbstractions.AddRange( abstractionModelManager.GetAbstractionTypes());node.TypeChanged += newType => {abstractionModelManager.UpdateAbstractionModel(abstractionModelManager.GetAbstractionModel(newType),node.Model);node.UpdateUI();Dispatcher.CurrentDispatcher.Invoke(() => {var edges = mainGraph.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);};mainGraph.AddNode(node);node.CreateInternals();mainCanvas.Children.Add(node.Render);return node;} };
            MenuBar id_351ee1af5ac440a3aad46fb7755643ff = new MenuBar() {  };
            MenuItem id_d97a9c606cb94091af13126fa46218c0 = new MenuItem(header: "File") {  };
            MenuItem id_79d726fcfa7645b39def8ebb3fd87776 = new MenuItem(header: "Open Project") {  };
            FolderBrowser id_3d9fe889f9d344bf89272813d0fa0a1e = new FolderBrowser() { Description = "" };
            DirectorySearch id_074f513cf8ff4d26b818337a9e8942bd = new DirectorySearch(directoriesToFind: new string[] { "DomainAbstractions", "ProgrammingParadigms", "RequirementsAbstractions", "Modules" }) { FilenameFilter = "*.cs" };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_e5d0bcfae5844c87b18389cd794f3cbe = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("DomainAbstractions")){list = input["DomainAbstractions"];}return list;} };
            ForEach<string> id_b1b142cbaeb244c88da4a0ce51697888 = new ForEach<string>() {  };
            ApplyAction<string> id_8ee5d29b5a394affa16b757317b363e4 = new ApplyAction<string>() { Lambda = input =>{abstractionModelManager.CreateAbstractionModelFromPath(input);} };
            Data<string> id_31017c3a82444114b80af37b2d162d90 = new Data<string>() { storedData = "Apply" };
            Apply<string, AbstractionModel> id_cb7deef56cd946a899c278b2b752e9dc = new Apply<string, AbstractionModel>() { Lambda = input =>{return abstractionModelManager.GetAbstractionModel(input);} };
            GetSetting id_e1522705079742d7889095ec7255dbd4 = new GetSetting(name: "ProjectFolderPath") {  };
            KeyEvent id_8466d92f47ab4870a2ca9b9ee88fcbe1 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.Enter } };
            ApplyAction<object> id_c0cb387de35a4208acddcaccf2c1a7c0 = new ApplyAction<object>() { Lambda = input =>{(input as WPFCanvas).Focus();} };
            MenuItem id_a3d76e6ee95f46ff94892c2751e019e1 = new MenuItem(header: "Debug") {  };
            MenuItem id_db9c5362c2894280838c082cfec0a5b0 = new MenuItem(header: "TextEditor test") {  };
            PopupWindow id_8f9afec293c048d094a5dfa77da8d2e3 = new PopupWindow(title: "") { Height = 720, Width = 1280, Resize = SizeToContent.WidthAndHeight };
            Box id_7ee2a3bb3a0a4c75af9697f54d69a517 = new Box() { Width = 100, Height = 100 };
            TextEditor id_c524932886814ef9a486986610f3dfc0 = new TextEditor() { Width = 1280, Height = 720 };
            DataFlowConnector<string> id_359cefff36f94e3a9a990ea5e4cb89cf = new DataFlowConnector<string>() {  };
            FolderWatcher projectFolderWatcher = new FolderWatcher() { InstanceName = "projectFolderWatcher", RootPath = "", Filter = "*.cs", WatchSubdirectories = true, PathRegex = @".*\.cs$" };
            Apply<string, object> id_8a1cdf5d884b4ff5ae38b5db51a99821 = new Apply<string, object>() { Lambda = input =>{var newModel = abstractionModelManager.CreateAbstractionModelFromPath(input);foreach (var node in mainGraph.Nodes){var alaNode = node as ALANode;if (alaNode.Model.Type != newModel.Type) continue;abstractionModelManager.UpdateAbstractionModel(newModel, alaNode.Model);alaNode.UpdateUI();}return input;} };
            ConvertToEvent<object> id_acbae079403943149cf17f654531baa4 = new ConvertToEvent<object>() {  };
            MouseButtonEvent id_69e9dc1aa2cd476fa7e7c452d76fd305 = new MouseButtonEvent(eventName: "MouseRightButtonDown") { ExtractSender = null, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            ApplyAction<object> id_1efc610e098946b99b1082f41f058319 = new ApplyAction<object>() { Lambda = input =>{Mouse.Capture(input as WPFCanvas);stateTransition.Update(Enums.DiagramMode.Idle);} };
            MouseButtonEvent id_2a29a2b86eae451ca1420a92a8265450 = new MouseButtonEvent(eventName: "MouseRightButtonUp") { ExtractSender = null, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            ApplyAction<object> id_1246fedb727749239205c0e52d4c61c3 = new ApplyAction<object>() { Lambda = input =>{if (Mouse.Captured?.Equals(input) ?? false) Mouse.Capture(null);stateTransition.Update(Enums.DiagramMode.Idle);} };
            StateChangeListener id_c885702e83194e15ac829b66ad2e31c1 = new StateChangeListener() { StateTransition = stateTransition, CurrentStateShouldMatch = Enums.DiagramMode.All };
            Apply<Tuple<Enums.DiagramMode, Enums.DiagramMode>, bool> id_058aa56b8075461c920e53eefb38c824 = new Apply<Tuple<Enums.DiagramMode, Enums.DiagramMode>, bool>() { Lambda = input =>{return input.Item1 == Enums.DiagramMode.AwaitingPortSelection &&input.Item2 == Enums.DiagramMode.Idle;} };
            IfElse id_86c7875c6c714057a9c75b4206a3a5e0 = new IfElse() {  };
            EventConnector id_3420cc2d6d7d47e8a8c1541020faea22 = new EventConnector() {  };
            Apply<object, object> createAndPaintALAWire = new Apply<object, object>() { InstanceName = "createAndPaintALAWire", Lambda = input =>{var source = mainGraph.Get("SelectedNode") as ALANode;var destination = input as ALANode;var sourcePort = source.GetSelectedPort(inputPort: false);var destinationPort = destination.GetSelectedPort(inputPort: true);var wire = new ALAWire(){Graph = mainGraph,Canvas = mainCanvas,Source = source,Destination = destination,SourcePort = sourcePort,DestinationPort = destinationPort,StateTransition = stateTransition};mainGraph.AddEdge(wire);wire.Paint();return wire;} };
            KeyEvent id_7665caf423f9438aa2eefd7e63c48b9e = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.Delete } };
            EventLambda id_c41dc190838b4060b5b68edfc093959a = new EventLambda() { Lambda = () =>{var selectedNode = mainGraph.Get("SelectedNode") as ALANode;if (selectedNode == null) return;selectedNode.Delete(deleteAttachedWires: true);} };
            MenuItem id_6befced306b445aeb67486cdff32b7c6 = new MenuItem(header: "Refresh") {  };
            Data<AbstractionModel> id_560c494d0f9846d389a491a06e3eac50 = new Data<AbstractionModel>() { Lambda = () => abstractionModelManager.GetAbstractionModel(abstractionModelManager.GetAbstractionTypes().FirstOrDefault()) };
            Apply<AbstractionModel, object> id_3accbc78ddcc45169c8f9cb89ee3a0bb = new Apply<AbstractionModel, object>() { Lambda = createNewALANode.Lambda };
            ApplyAction<object> id_0776d2ad76d84d1ba572565ba90c751f = new ApplyAction<object>() { Lambda = input =>{var alaNode = input as ALANode;var mousePos = Mouse.GetPosition(mainCanvas);alaNode.PositionX = mousePos.X;alaNode.PositionY = mousePos.Y;mainGraph.Set("LatestNode", input);if (mainGraph.Get("SelectedNode") == null){mainGraph.Set("SelectedNode", input);}mainGraph.Roots.Add(input);} };
            MenuItem id_1628d3add46041f1baef8b0db02206f2 = new MenuItem(header: "Open Code File") {  };
            FileBrowser id_8da8bff344f14b7e977163b52cd1d34a = new FileBrowser() { Mode = "Open" };
            FileReader id_2e564dc2204b4e96a2178dd6b61cb605 = new FileReader() {  };
            CreateDiagramFromCode id_f50a9253f5c04b26a24cf2b52e468398 = new CreateDiagramFromCode() { Graph = mainGraph, Canvas = mainCanvas, ModelManager = abstractionModelManager, StateTransition = stateTransition };
            EventConnector id_117273d6371a4865b4a625e1f5b882ee = new EventConnector() {  };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_e81631c0ba62497bb3fb012a48f632aa = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("ProgrammingParadigms")){list = input["ProgrammingParadigms"];}return list;} };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_28f612f5eab7467294f0149fd02e0788 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("RequirementsAbstractions")){list = input["RequirementsAbstractions"];}return list;} };
            DataFlowConnector<Dictionary<string, List<string>>> id_3d87d77a224942ca84995ecccb572f4d = new DataFlowConnector<Dictionary<string, List<string>>>() {  };
            Data<UIElement> id_e483058ba59d475997f41e7ac12f7114 = new Data<UIElement>() { Lambda = () => mainCanvas };
            Scale id_0190b8a0c79a49fb987411e7bf1e7c75 = new Scale() { WidthMultiplier = 1.1, HeightMultiplier = 1.1 };
            Data<UIElement> id_e02c99168a24478da6ae2b3bf9061430 = new Data<UIElement>() { Lambda = () => mainCanvas };
            Scale id_b64ef9ca5e674dd1b1b3cef43ca88b9c = new Scale() { WidthMultiplier = 0.9, HeightMultiplier = 0.9 };
            DataFlowConnector<UIElement> id_252df84089444e4fa56bcfa57d6a3881 = new DataFlowConnector<UIElement>() {  };
            DataFlowConnector<UIElement> id_521110f8d1874679bc6300bd6c06a69e = new DataFlowConnector<UIElement>() {  };
            ApplyAction<UIElement> id_3020fd26fdea44d7a46cabd833290528 = new ApplyAction<UIElement>() { Lambda = input => {if (!(input.RenderTransform is ScaleTransform)) return;var transform = input.RenderTransform as ScaleTransform;var minScale = 0.6;/*Logging.Log($"Scale: {transform.ScaleX}, {transform.ScaleX}");*/bool nodeIsTooSmall = transform.ScaleX < minScale && transform.ScaleY < minScale;var nodes = mainGraph.Nodes;foreach (var node in nodes){if (node is ALANode alaNode) alaNode.ShowTypeTextMask(nodeIsTooSmall);}} };
            MouseWheelEvent id_ac2fdd46fb5c41c1ad50a43998203229 = new MouseWheelEvent(eventName: "MouseWheel") {  };
            Apply<MouseWheelEventArgs, bool> id_6c43c96a46974a88a712dd51e05eb614 = new Apply<MouseWheelEventArgs, bool>() { Lambda = args =>{return args.Delta > 0;} };
            IfElse id_a45a053408d64551ae2b38dba9d8f6d6 = new IfElse() {  };
            DataFlowConnector<string> id_9083cba7f786401f872613f39fc2ab1e = new DataFlowConnector<string>() {  };
            ConvertToEvent<string> id_7cf3c04cf59043d9a8043800e1c749d7 = new ConvertToEvent<string>() {  };
            DispatcherEvent id_e51d1744c2f0443099b4c756ecaebe6e = new DispatcherEvent() { Priority = DispatcherPriority.ApplicationIdle };
            MenuItem id_3795b7bdccb146b790d1ae40424ebf72 = new MenuItem(header: "Generate Code") {  };
            GenerateALACode id_adf7c73611c64ab893bea592df4d7bb2 = new GenerateALACode() { Graph = mainGraph };
            GetSetting id_0fcf63ed0ca14af1a00b00e826698ad4 = new GetSetting(name: "ApplicationCodeFilePath") {  };
            Data<string> id_543e99e9ec38448e99dd13812282bc78 = new Data<string>() { storedData = SETTINGS_FILEPATH };
            EditSetting id_15e3b3952b544961b74688e61820fc9f = new EditSetting() { JSONPath = "$..ApplicationCodeFilePath" };
            Data<string> id_15950285d21f43ed95a678750bba4552 = new Data<string>() { storedData = SETTINGS_FILEPATH };
            Cast<string, object> id_8eb15b54c02441e3b6852332d1867047 = new Cast<string, object>() {  };
            DataFlowConnector<string> id_e9273271ad194b25abbafb027b70a743 = new DataFlowConnector<string>() {  };
            Cast<string, object> id_134fe7af51b4480fa2ba723b0df351b3 = new Cast<string, object>() {  };
            Data<string> id_72bd24e9e30f4987af2bc19013ce169a = new Data<string>() { storedData = SETTINGS_FILEPATH };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_ee24d52f9d304c10b96113eca4519bee = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("Modules")){list = input["Modules"];}return list;} };
            GetSetting id_e2c33a51ebaa48ccb7093b8d37cb6df1 = new GetSetting(name: "ApplicationCodeFilePath") {  };
            Data<string> id_0d4fe856b85d4b2887f607cc993c91e5 = new Data<string>() { storedData = SETTINGS_FILEPATH };
            FileReader id_7d0c0ed4d28d49ef9c3e73e78dee6a2e = new FileReader() {  };
            DataFlowConnector<string> id_cc2179d6fd3c40ea9df3096821dd35f5 = new DataFlowConnector<string>() {  };
            EventConnector generateCode = new EventConnector() { InstanceName = "generateCode" };
            EditSetting id_82f4a2f0b6e74e58ac34aa0c84ad1820 = new EditSetting() { JSONPath = "$..ProjectFolderPath" };
            Data<string> id_3e1f6938cd654c7bb272ad108ab5875b = new Data<string>() { storedData = SETTINGS_FILEPATH };
            FileWriter id_7cddbf2f5cb843978be7bc94422952ac = new FileWriter() {  };
            DataFlowConnector<string> id_4d84d2ac16264701a0995f26a17b63d7 = new DataFlowConnector<string>() {  };
            InsertFileCodeLines id_120a171e9efd40c6a79fdff7dc26f797 = new InsertFileCodeLines() { StartLandmark = "// BEGIN AUTO-GENERATED INSTANTIATIONS", EndLandmark = "// END AUTO-GENERATED INSTANTIATIONS", Indent = "            " };
            InsertFileCodeLines id_ac061441d04c4d50bf4feee0774455b1 = new InsertFileCodeLines() { StartLandmark = "// BEGIN AUTO-GENERATED WIRING", EndLandmark = "// END AUTO-GENERATED WIRING", Indent = "            " };
            EventConnector id_24748e5db5c74497a756acd21c83976b = new EventConnector() {  };
            MenuItem id_07a6a66f53834d3784f221b41cda487b = new MenuItem(header: "Generics test") {  };
            EventLambda id_e133c85acdac40c1afbbd4b0ee21eb2a = new EventLambda() { Lambda = () =>{var node = mainGraph.Nodes.First() as ALANode;node.Model.UpdateGeneric(0, "testType");} };
            Horizontal statusBarHorizontal = new Horizontal() { InstanceName = "statusBarHorizontal", Margin = new Thickness(5) };
            Text globalMessageTextDisplay = new Text(text: "") { InstanceName = "globalMessageTextDisplay", Height = 20 };
            EventLambda id_a6e8f322f210437299111bc385f9ba4a = new EventLambda() { Lambda = () =>{Logging.MessageOutput += message => (globalMessageTextDisplay as IDataFlow<string>).Data = message;} };
            EventLambda id_305af633e78d4341bd6ecc1672fa7fca = new EventLambda() { Lambda = () =>{Logging.Message("Beginning code generation...");} };
            EventLambda id_698c68c998c1487784a0096658c4eea6 = new EventLambda() { Lambda = () =>{Logging.Message("Completed code generation!");} };
            ExtractALACode extractALACode = new ExtractALACode() { InstanceName = "extractALACode" };
            ConvertToEvent<string> id_122cab5811fa469abdfd1b4e36e28f68 = new ConvertToEvent<string>() {  };
            Data<string> id_b79514b23eea4e848baf5ad05f0e934e = new Data<string>() { Lambda = () => {/* Put the code inside a CreateWiring() method in a dummy class so that CreateDiagramFromCode uses it correctly. TODO: Update CreateDiagramFromCode to use landmarks by default. */var sb = new StringBuilder();sb.AppendLine("class DummyClass {");sb.AppendLine("void CreateWiring() {");sb.AppendLine(extractALACode.Instantiations);sb.AppendLine(extractALACode.Wiring);sb.AppendLine("}");sb.AppendLine("}");return sb.ToString();} };
            // END AUTO-GENERATED INSTANTIATIONS FOR Application.xmind

            // BEGIN AUTO-GENERATED WIRING FOR Application.xmind
            mainWindow.WireTo(id_7c078701861e4f67afc100e7bbe7c106, "iuiStructure");
            mainWindow.WireTo(id_3420cc2d6d7d47e8a8c1541020faea22, "appStart");
            id_7c078701861e4f67afc100e7bbe7c106.WireTo(id_351ee1af5ac440a3aad46fb7755643ff, "children");
            id_7c078701861e4f67afc100e7bbe7c106.WireTo(id_720a828eadfa48bfb8ffa5e59a1971ae, "children");
            id_7c078701861e4f67afc100e7bbe7c106.WireTo(statusBarHorizontal, "children");
            id_720a828eadfa48bfb8ffa5e59a1971ae.WireTo(id_059d15fbce7442c2a229823ff75052d8, "eventHandlers");
            id_720a828eadfa48bfb8ffa5e59a1971ae.WireTo(id_bcbbc36b3d64495ebc80e061f0417def, "eventHandlers");
            id_720a828eadfa48bfb8ffa5e59a1971ae.WireTo(id_8466d92f47ab4870a2ca9b9ee88fcbe1, "eventHandlers");
            id_720a828eadfa48bfb8ffa5e59a1971ae.WireTo(id_69e9dc1aa2cd476fa7e7c452d76fd305, "eventHandlers");
            id_720a828eadfa48bfb8ffa5e59a1971ae.WireTo(id_2a29a2b86eae451ca1420a92a8265450, "eventHandlers");
            id_720a828eadfa48bfb8ffa5e59a1971ae.WireTo(id_7665caf423f9438aa2eefd7e63c48b9e, "eventHandlers");
            id_720a828eadfa48bfb8ffa5e59a1971ae.WireTo(id_ac2fdd46fb5c41c1ad50a43998203229, "eventHandlers");
            id_720a828eadfa48bfb8ffa5e59a1971ae.WireTo(id_d6bccf60bded43cf93644dd0b689dd73, "contextMenu");
            id_059d15fbce7442c2a229823ff75052d8.WireTo(id_e0a20720c820440aad32438da5e5fe42, "eventHappened");
            id_d6bccf60bded43cf93644dd0b689dd73.WireTo(id_fb979b19beb742ce82cf3b8cedd8f567, "children");
            id_d6bccf60bded43cf93644dd0b689dd73.WireTo(id_6befced306b445aeb67486cdff32b7c6, "children");
            id_fb979b19beb742ce82cf3b8cedd8f567.WireTo(id_560c494d0f9846d389a491a06e3eac50, "clickedEvent");
            id_e0a20720c820440aad32438da5e5fe42.WireTo(id_31017c3a82444114b80af37b2d162d90, "fanoutList");
            id_e0a20720c820440aad32438da5e5fe42.WireTo(layoutDiagram, "complete");
            getFirstRoot.WireTo(id_bfd1544f03514a4c9547c381dae58dac, "dataOutput");
            layoutDiagram.WireTo(id_e51d1744c2f0443099b4c756ecaebe6e, "fanoutList");
            id_bfd1544f03514a4c9547c381dae58dac.WireTo(id_d5b7081d7d8f4ae7a2f84f77e07309c9, "fanoutList");
            id_bfd1544f03514a4c9547c381dae58dac.WireTo(id_61903a17294a4bc1929bad491a4b3e9b, "fanoutList");
            id_bcbbc36b3d64495ebc80e061f0417def.WireTo(layoutDiagram, "eventHappened");
            createNewALANode.WireTo(createAndPaintALAWire, "output");
            id_351ee1af5ac440a3aad46fb7755643ff.WireTo(id_d97a9c606cb94091af13126fa46218c0, "children");
            id_351ee1af5ac440a3aad46fb7755643ff.WireTo(id_a3d76e6ee95f46ff94892c2751e019e1, "children");
            id_d97a9c606cb94091af13126fa46218c0.WireTo(id_79d726fcfa7645b39def8ebb3fd87776, "children");
            id_d97a9c606cb94091af13126fa46218c0.WireTo(id_1628d3add46041f1baef8b0db02206f2, "children");
            id_79d726fcfa7645b39def8ebb3fd87776.WireTo(id_3d9fe889f9d344bf89272813d0fa0a1e, "clickedEvent");
            id_3d9fe889f9d344bf89272813d0fa0a1e.WireTo(id_359cefff36f94e3a9a990ea5e4cb89cf, "selectedFolderPathOutput");
            id_074f513cf8ff4d26b818337a9e8942bd.WireTo(id_3d87d77a224942ca84995ecccb572f4d, "foundFiles");
            id_e5d0bcfae5844c87b18389cd794f3cbe.WireTo(id_b1b142cbaeb244c88da4a0ce51697888, "output");
            id_b1b142cbaeb244c88da4a0ce51697888.WireTo(id_8ee5d29b5a394affa16b757317b363e4, "elementOutput");
            id_31017c3a82444114b80af37b2d162d90.WireTo(id_cb7deef56cd946a899c278b2b752e9dc, "dataOutput");
            id_cb7deef56cd946a899c278b2b752e9dc.WireTo(createNewALANode, "output");
            id_e1522705079742d7889095ec7255dbd4.WireTo(id_72bd24e9e30f4987af2bc19013ce169a, "filePathInput");
            id_e1522705079742d7889095ec7255dbd4.WireTo(id_359cefff36f94e3a9a990ea5e4cb89cf, "settingJsonOutput");
            id_8466d92f47ab4870a2ca9b9ee88fcbe1.WireTo(id_c0cb387de35a4208acddcaccf2c1a7c0, "senderOutput");
            id_a3d76e6ee95f46ff94892c2751e019e1.WireTo(id_db9c5362c2894280838c082cfec0a5b0, "children");
            id_a3d76e6ee95f46ff94892c2751e019e1.WireTo(id_3795b7bdccb146b790d1ae40424ebf72, "children");
            id_a3d76e6ee95f46ff94892c2751e019e1.WireTo(id_07a6a66f53834d3784f221b41cda487b, "children");
            id_db9c5362c2894280838c082cfec0a5b0.WireTo(id_8f9afec293c048d094a5dfa77da8d2e3, "clickedEvent");
            id_8f9afec293c048d094a5dfa77da8d2e3.WireTo(id_7ee2a3bb3a0a4c75af9697f54d69a517, "children");
            id_7ee2a3bb3a0a4c75af9697f54d69a517.WireTo(id_c524932886814ef9a486986610f3dfc0, "uiLayout");
            id_359cefff36f94e3a9a990ea5e4cb89cf.WireTo(id_074f513cf8ff4d26b818337a9e8942bd, "fanoutList");
            id_359cefff36f94e3a9a990ea5e4cb89cf.WireTo(projectFolderWatcher, "fanoutList");
            id_359cefff36f94e3a9a990ea5e4cb89cf.WireTo(id_134fe7af51b4480fa2ba723b0df351b3, "fanoutList");
            projectFolderWatcher.WireTo(id_8a1cdf5d884b4ff5ae38b5db51a99821, "changedFile");
            id_8a1cdf5d884b4ff5ae38b5db51a99821.WireTo(id_acbae079403943149cf17f654531baa4, "output");
            id_acbae079403943149cf17f654531baa4.WireTo(id_61903a17294a4bc1929bad491a4b3e9b, "eventOutput");
            id_69e9dc1aa2cd476fa7e7c452d76fd305.WireTo(id_1efc610e098946b99b1082f41f058319, "argsOutput");
            id_2a29a2b86eae451ca1420a92a8265450.WireTo(id_1246fedb727749239205c0e52d4c61c3, "argsOutput");
            id_c885702e83194e15ac829b66ad2e31c1.WireTo(id_058aa56b8075461c920e53eefb38c824, "transitionOutput");
            id_058aa56b8075461c920e53eefb38c824.WireTo(id_86c7875c6c714057a9c75b4206a3a5e0, "output");
            id_86c7875c6c714057a9c75b4206a3a5e0.WireTo(layoutDiagram, "ifOutput");
            id_3420cc2d6d7d47e8a8c1541020faea22.WireTo(id_e1522705079742d7889095ec7255dbd4, "fanoutList");
            id_3420cc2d6d7d47e8a8c1541020faea22.WireTo(id_c885702e83194e15ac829b66ad2e31c1, "fanoutList");
            id_3420cc2d6d7d47e8a8c1541020faea22.WireTo(id_0fcf63ed0ca14af1a00b00e826698ad4, "fanoutList");
            id_3420cc2d6d7d47e8a8c1541020faea22.WireTo(id_117273d6371a4865b4a625e1f5b882ee, "complete");
            id_7665caf423f9438aa2eefd7e63c48b9e.WireTo(id_c41dc190838b4060b5b68edfc093959a, "eventHappened");
            id_6befced306b445aeb67486cdff32b7c6.WireTo(layoutDiagram, "clickedEvent");
            id_560c494d0f9846d389a491a06e3eac50.WireTo(id_3accbc78ddcc45169c8f9cb89ee3a0bb, "dataOutput");
            id_3accbc78ddcc45169c8f9cb89ee3a0bb.WireTo(id_0776d2ad76d84d1ba572565ba90c751f, "output");
            id_1628d3add46041f1baef8b0db02206f2.WireTo(id_8da8bff344f14b7e977163b52cd1d34a, "clickedEvent");
            id_8da8bff344f14b7e977163b52cd1d34a.WireTo(id_e9273271ad194b25abbafb027b70a743, "selectedFilePathOutput");
            id_2e564dc2204b4e96a2178dd6b61cb605.WireTo(id_9083cba7f786401f872613f39fc2ab1e, "fileContentOutput");
            id_9083cba7f786401f872613f39fc2ab1e.WireTo(extractALACode, "fanoutList");
            id_117273d6371a4865b4a625e1f5b882ee.WireTo(id_a6e8f322f210437299111bc385f9ba4a, "fanoutList");
            id_e81631c0ba62497bb3fb012a48f632aa.WireTo(id_b1b142cbaeb244c88da4a0ce51697888, "output");
            id_28f612f5eab7467294f0149fd02e0788.WireTo(id_b1b142cbaeb244c88da4a0ce51697888, "output");
            id_3d87d77a224942ca84995ecccb572f4d.WireTo(id_e5d0bcfae5844c87b18389cd794f3cbe, "fanoutList");
            id_3d87d77a224942ca84995ecccb572f4d.WireTo(id_e81631c0ba62497bb3fb012a48f632aa, "fanoutList");
            id_3d87d77a224942ca84995ecccb572f4d.WireTo(id_28f612f5eab7467294f0149fd02e0788, "fanoutList");
            id_3d87d77a224942ca84995ecccb572f4d.WireTo(id_ee24d52f9d304c10b96113eca4519bee, "fanoutList");
            id_e483058ba59d475997f41e7ac12f7114.WireTo(id_252df84089444e4fa56bcfa57d6a3881, "dataOutput");
            id_e02c99168a24478da6ae2b3bf9061430.WireTo(id_521110f8d1874679bc6300bd6c06a69e, "dataOutput");
            id_252df84089444e4fa56bcfa57d6a3881.WireTo(id_0190b8a0c79a49fb987411e7bf1e7c75, "fanoutList");
            id_252df84089444e4fa56bcfa57d6a3881.WireTo(id_3020fd26fdea44d7a46cabd833290528, "fanoutList");
            id_521110f8d1874679bc6300bd6c06a69e.WireTo(id_b64ef9ca5e674dd1b1b3cef43ca88b9c, "fanoutList");
            id_521110f8d1874679bc6300bd6c06a69e.WireTo(id_3020fd26fdea44d7a46cabd833290528, "fanoutList");
            id_ac2fdd46fb5c41c1ad50a43998203229.WireTo(id_6c43c96a46974a88a712dd51e05eb614, "argsOutput");
            id_6c43c96a46974a88a712dd51e05eb614.WireTo(id_a45a053408d64551ae2b38dba9d8f6d6, "output");
            id_a45a053408d64551ae2b38dba9d8f6d6.WireTo(id_e483058ba59d475997f41e7ac12f7114, "ifOutput");
            id_a45a053408d64551ae2b38dba9d8f6d6.WireTo(id_e02c99168a24478da6ae2b3bf9061430, "elseOutput");
            id_9083cba7f786401f872613f39fc2ab1e.WireTo(id_122cab5811fa469abdfd1b4e36e28f68, "fanoutList");
            id_9083cba7f786401f872613f39fc2ab1e.WireTo(id_7cf3c04cf59043d9a8043800e1c749d7, "fanoutList");
            id_b79514b23eea4e848baf5ad05f0e934e.WireTo(id_f50a9253f5c04b26a24cf2b52e468398, "dataOutput");
            id_7cf3c04cf59043d9a8043800e1c749d7.WireTo(layoutDiagram, "eventOutput");
            id_e51d1744c2f0443099b4c756ecaebe6e.WireTo(getFirstRoot, "delayedEvent");
            id_3795b7bdccb146b790d1ae40424ebf72.WireTo(generateCode, "clickedEvent");
            id_adf7c73611c64ab893bea592df4d7bb2.WireTo(id_120a171e9efd40c6a79fdff7dc26f797, "instantiations");
            id_adf7c73611c64ab893bea592df4d7bb2.WireTo(id_ac061441d04c4d50bf4feee0774455b1, "wireTos");
            id_0fcf63ed0ca14af1a00b00e826698ad4.WireTo(id_543e99e9ec38448e99dd13812282bc78, "filePathInput");
            id_15e3b3952b544961b74688e61820fc9f.WireTo(id_15950285d21f43ed95a678750bba4552, "filePathInput");
            id_8eb15b54c02441e3b6852332d1867047.WireTo(id_15e3b3952b544961b74688e61820fc9f, "output");
            id_e9273271ad194b25abbafb027b70a743.WireTo(id_2e564dc2204b4e96a2178dd6b61cb605, "fanoutList");
            id_e9273271ad194b25abbafb027b70a743.WireTo(id_8eb15b54c02441e3b6852332d1867047, "fanoutList");
            id_134fe7af51b4480fa2ba723b0df351b3.WireTo(id_82f4a2f0b6e74e58ac34aa0c84ad1820, "output");
            id_ee24d52f9d304c10b96113eca4519bee.WireTo(id_b1b142cbaeb244c88da4a0ce51697888, "output");
            id_e2c33a51ebaa48ccb7093b8d37cb6df1.WireTo(id_0d4fe856b85d4b2887f607cc993c91e5, "filePathInput");
            id_e2c33a51ebaa48ccb7093b8d37cb6df1.WireTo(id_4d84d2ac16264701a0995f26a17b63d7, "settingJsonOutput");
            id_7d0c0ed4d28d49ef9c3e73e78dee6a2e.WireTo(id_cc2179d6fd3c40ea9df3096821dd35f5, "fileContentOutput");
            id_cc2179d6fd3c40ea9df3096821dd35f5.WireTo(id_120a171e9efd40c6a79fdff7dc26f797, "fanoutList");
            generateCode.WireTo(id_e2c33a51ebaa48ccb7093b8d37cb6df1, "fanoutList");
            generateCode.WireTo(id_305af633e78d4341bd6ecc1672fa7fca, "fanoutList");
            generateCode.WireTo(id_adf7c73611c64ab893bea592df4d7bb2, "fanoutList");
            generateCode.WireTo(id_24748e5db5c74497a756acd21c83976b, "complete");
            id_82f4a2f0b6e74e58ac34aa0c84ad1820.WireTo(id_3e1f6938cd654c7bb272ad108ab5875b, "filePathInput");
            id_7cddbf2f5cb843978be7bc94422952ac.WireTo(id_4d84d2ac16264701a0995f26a17b63d7, "filePathInput");
            id_4d84d2ac16264701a0995f26a17b63d7.WireTo(id_7d0c0ed4d28d49ef9c3e73e78dee6a2e, "fanoutList");
            id_120a171e9efd40c6a79fdff7dc26f797.WireTo(id_ac061441d04c4d50bf4feee0774455b1, "newFileContentsOutput");
            id_ac061441d04c4d50bf4feee0774455b1.WireTo(id_7cddbf2f5cb843978be7bc94422952ac, "newFileContentsOutput");
            id_24748e5db5c74497a756acd21c83976b.WireTo(id_120a171e9efd40c6a79fdff7dc26f797, "fanoutList");
            id_24748e5db5c74497a756acd21c83976b.WireTo(id_ac061441d04c4d50bf4feee0774455b1, "fanoutList");
            id_24748e5db5c74497a756acd21c83976b.WireTo(id_698c68c998c1487784a0096658c4eea6, "complete");
            id_07a6a66f53834d3784f221b41cda487b.WireTo(id_e133c85acdac40c1afbbd4b0ee21eb2a, "clickedEvent");
            statusBarHorizontal.WireTo(globalMessageTextDisplay, "children");
            id_122cab5811fa469abdfd1b4e36e28f68.WireTo(id_b79514b23eea4e848baf5ad05f0e934e, "eventOutput");
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
















































