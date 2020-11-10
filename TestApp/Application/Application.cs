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
            Vertical id_06f1d0d57ef94ca0a1b84eeb78bffcb8 = new Vertical() {  };
            CanvasDisplay id_3dd9c6b85d5f4720ad0b954852eb8bd5 = new CanvasDisplay() { Width = 1920, Height = 1080, Background = Brushes.White, StateTransition = stateTransition, Canvas = mainCanvas };
            KeyEvent id_0f2d6129132e456abc43e57dd30d3e74 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.A }, Condition = args => mainGraph.Get("SelectedNode") != null&& stateTransition.CurrentStateMatches(Enums.DiagramMode.IdleSelected) };
            ContextMenu id_de5f959c05754afcbed3aa3185cfa7ee = new ContextMenu() {  };
            MenuItem id_e07d2d0fb7014af78f8e524147997d35 = new MenuItem(header: "Add root") {  };
            EventConnector id_d428060e562e48b3bae6cacc417dabb3 = new EventConnector() {  };
            Data<ALANode> getFirstRoot = new Data<ALANode>() { InstanceName = "getFirstRoot", Lambda = () => mainGraph.Roots.FirstOrDefault() as ALANode };
            RightTreeLayout<ALANode> id_13a9cf4d34894dd8b00a76c1033d0bbf = new RightTreeLayout<ALANode>() { GetID = n => n.Id, GetWidth = n => n.Width, GetHeight = n => n.Height, SetX = (n, x) => n.PositionX = x, SetY = (n, y) => n.PositionY = y, GetChildren = n => mainGraph.Edges.Where(e => e is ALAWire wire && wire.Source != null && wire.Destination != null && wire.Source == n).Select(e => ((e as ALAWire).Destination) as ALANode), HorizontalGap = 100, VerticalGap = 20, InitialX = 50, InitialY = 50 };
            EventConnector layoutDiagram = new EventConnector() { InstanceName = "layoutDiagram" };
            DataFlowConnector<ALANode> id_dfad0e10d103413ca62341600eb53b84 = new DataFlowConnector<ALANode>() {  };
            ApplyAction<ALANode> id_3f6a1b7cd38e4abcb8e6020205bb3149 = new ApplyAction<ALANode>() { Lambda = node =>{Dispatcher.CurrentDispatcher.Invoke(() => {var edges = mainGraph.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);} };
            KeyEvent id_91540e8151384b00859c7cd7ac0db336 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.R }, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            Apply<AbstractionModel, object> createNewALANode = new Apply<AbstractionModel, object>() { InstanceName = "createNewALANode", Lambda = input => {var node = new ALANode();node.Model = input;node.Graph = mainGraph;node.Canvas = mainCanvas;node.StateTransition = stateTransition;node.AvailableDomainAbstractions.AddRange( abstractionModelManager.GetAbstractionTypes());node.TypeChanged += newType => {abstractionModelManager.UpdateAbstractionModel(abstractionModelManager.GetAbstractionModel(newType),node.Model);node.UpdateUI();Dispatcher.CurrentDispatcher.Invoke(() => {var edges = mainGraph.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);};mainGraph.AddNode(node);node.CreateInternals();mainCanvas.Children.Add(node.Render);return node;} };
            MenuBar id_8a303e6870e045f3bfbe81afe713410f = new MenuBar() {  };
            MenuItem id_72a644b6154e4dbc94903372d2dd52c8 = new MenuItem(header: "File") {  };
            MenuItem id_d006c330b62c4aefb8ee80e29c3d90a7 = new MenuItem(header: "Open Project") {  };
            FolderBrowser id_5b358134e6c240e8861382d073b6daa9 = new FolderBrowser() { Description = "" };
            DirectorySearch id_1223b6c4a9fb4a078880318febcdaceb = new DirectorySearch(directoriesToFind: new string[] { "DomainAbstractions", "ProgrammingParadigms", "RequirementsAbstractions" }) { FilenameFilter = "*.cs" };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_52bdc5261a8344d4b061df873795daa4 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("DomainAbstractions")){list = input["DomainAbstractions"];}return list;} };
            ForEach<string> id_28f4b656ee7640d1b74aa495c685c98a = new ForEach<string>() {  };
            ApplyAction<string> id_4ef72270c04442c990d7c38c30df9083 = new ApplyAction<string>() { Lambda = input =>{abstractionModelManager.CreateAbstractionModelFromPath(input);} };
            Data<string> id_216d5307f4424a08bf9a8640a6b7a367 = new Data<string>() { storedData = "Apply" };
            Apply<string, AbstractionModel> id_85f5a744cd0d408b8ba10fa7653f27bd = new Apply<string, AbstractionModel>() { Lambda = input =>{return abstractionModelManager.GetAbstractionModel(input);} };
            Data<string> id_54bf34594d0c485384eb3f349fd5e182 = new Data<string>() { storedData = @"D:\Coding\C#\Projects\GALADE\ALACore" };
            KeyEvent id_36f646b635c34537abc0345c9ead9807 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.Enter } };
            ApplyAction<object> id_2ee0c75f6cd04fd8bd1a8f50396b54a8 = new ApplyAction<object>() { Lambda = input =>{(input as WPFCanvas).Focus();} };
            MenuItem id_114addd3cd73472099d3b79f85c796fe = new MenuItem(header: "Debug") {  };
            MenuItem id_c8c8d57ff6a94373a4bac1f7206d9bff = new MenuItem(header: "TextEditor test") {  };
            PopupWindow id_5a248626b00d48d6af02e424703b1436 = new PopupWindow(title: "") { Height = 720, Width = 1280, Resize = SizeToContent.WidthAndHeight };
            Box id_8474f7bc283d460eb3a00dccc8a534f6 = new Box() { Width = 100, Height = 100 };
            TextEditor id_db234cdb4bbb4de38a88dc88eb4c5c9d = new TextEditor() { Width = 1280, Height = 720 };
            DataFlowConnector<string> id_ad36a59dd1394606a949607e4943d4ad = new DataFlowConnector<string>() {  };
            FolderWatcher projectFolderWatcher = new FolderWatcher() { InstanceName = "projectFolderWatcher", RootPath = "", Filter = "*.cs", WatchSubdirectories = true, PathRegex = @".*\.cs$" };
            Apply<string, object> id_cf6ee2e2add04654a1a3a885aacec011 = new Apply<string, object>() { Lambda = input =>{var newModel = abstractionModelManager.CreateAbstractionModelFromPath(input);foreach (var node in mainGraph.Nodes){var alaNode = node as ALANode;if (alaNode.Model.Type != newModel.Type) continue;abstractionModelManager.UpdateAbstractionModel(newModel, alaNode.Model);alaNode.UpdateUI();}return input;} };
            ConvertToEvent<object> id_2576a7a2171a48c5b44198efcc488012 = new ConvertToEvent<object>() {  };
            MouseButtonEvent id_494151e402ea4a5eb4dba8abe2440e4a = new MouseButtonEvent(eventName: "MouseRightButtonDown") { ExtractSender = null, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            ApplyAction<object> id_799e0914bfa74332bc18486b8e6e9334 = new ApplyAction<object>() { Lambda = input =>{Mouse.Capture(input as WPFCanvas);stateTransition.Update(Enums.DiagramMode.Idle);} };
            MouseButtonEvent id_42d03798020349a2a41089b5cac3e89d = new MouseButtonEvent(eventName: "MouseRightButtonUp") { ExtractSender = null, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            ApplyAction<object> id_4349830b8b124c6ebd440a5a0352bfda = new ApplyAction<object>() { Lambda = input =>{if (Mouse.Captured?.Equals(input) ?? false) Mouse.Capture(null);stateTransition.Update(Enums.DiagramMode.Idle);} };
            StateChangeListener id_745aa054b25b4843a32d0725334698f4 = new StateChangeListener() { StateTransition = stateTransition, CurrentStateShouldMatch = Enums.DiagramMode.All };
            Apply<Tuple<Enums.DiagramMode, Enums.DiagramMode>, bool> id_8a611d15af834af3a0a2623cd4d0dbec = new Apply<Tuple<Enums.DiagramMode, Enums.DiagramMode>, bool>() { Lambda = input =>{return input.Item1 == Enums.DiagramMode.AwaitingPortSelection &&input.Item2 == Enums.DiagramMode.Idle;} };
            IfElse id_46a7fdc3f4ce4e759958a47827fe8f84 = new IfElse() {  };
            EventConnector id_e1c72d1563cd43c59927cdb480bf2ccd = new EventConnector() {  };
            Apply<object, object> createAndPaintALAWire = new Apply<object, object>() { InstanceName = "createAndPaintALAWire", Lambda = input =>{var source = mainGraph.Get("SelectedNode") as ALANode;var destination = input as ALANode;var sourcePort = source.GetSelectedPort(inputPort: false);var destinationPort = destination.GetSelectedPort(inputPort: true);var wire = new ALAWire(){Graph = mainGraph,Canvas = mainCanvas,Source = source,Destination = destination,SourcePort = sourcePort,DestinationPort = destinationPort,StateTransition = stateTransition};mainGraph.AddEdge(wire);wire.Paint();return wire;} };
            KeyEvent id_821befd29b254bbe9eb5d992d5a6e261 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.Delete } };
            EventLambda id_fabe6c8c7ff040758ecdd96a27df2399 = new EventLambda() { Lambda = () =>{var selectedNode = mainGraph.Get("SelectedNode") as ALANode;if (selectedNode == null) return;selectedNode.Delete(deleteAttachedWires: true);} };
            MenuItem id_d810a11d5c884963b1a2e9246aae8427 = new MenuItem(header: "Refresh") {  };
            Data<AbstractionModel> id_650dcd46c0184db1b9f248e6f93d62ad = new Data<AbstractionModel>() { Lambda = () => abstractionModelManager.GetAbstractionModel(abstractionModelManager.GetAbstractionTypes().FirstOrDefault()) };
            Apply<AbstractionModel, object> id_c1837a68ef004a42916551a89058e204 = new Apply<AbstractionModel, object>() { Lambda = createNewALANode.Lambda };
            ApplyAction<object> id_5ca3d72138804db4bcb1b009662ec4c5 = new ApplyAction<object>() { Lambda = input =>{var alaNode = input as ALANode;var mousePos = Mouse.GetPosition(mainCanvas);alaNode.PositionX = mousePos.X;alaNode.PositionY = mousePos.Y;mainGraph.Set("LatestNode", input);if (mainGraph.Get("SelectedNode") == null){mainGraph.Set("SelectedNode", input);}mainGraph.Roots.Add(input);} };
            MenuItem id_e64b7ef98e00461eaf464122f79658a8 = new MenuItem(header: "Open Code File") {  };
            FileBrowser id_3ebc8dbcae7943a3a3ea9f11b09d8b58 = new FileBrowser() { Mode = "Open" };
            FileReader id_f73e477ba258460fb79ec1a709d95999 = new FileReader() {  };
            CreateDiagramFromCode id_bde216fd6b864182857ac3f91c6473ad = new CreateDiagramFromCode() { Graph = mainGraph, Canvas = mainCanvas, ModelManager = abstractionModelManager, StateTransition = stateTransition };
            EventConnector id_522fbc174d774db89c8d90eeab24358a = new EventConnector() {  };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_bde1265620d742a49ab4056d84661f12 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("ProgrammingParadigms")){list = input["ProgrammingParadigms"];}return list;} };
            ForEach<string> id_d040cfaff591463b9b912c5d608c99d3 = new ForEach<string>() {  };
            ApplyAction<string> id_fd3ae12d55d4404aa6bb34664d50e352 = new ApplyAction<string>() { Lambda = input =>{abstractionModelManager.CreateAbstractionModelFromPath(input);} };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_016ee1833bf84da989ef2518169fd3a8 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("RequirementsAbstractions")){list = input["RequirementsAbstractions"];}return list;} };
            ForEach<string> id_4ba321618cbd46ef9e1371ecc2b73a23 = new ForEach<string>() {  };
            ApplyAction<string> id_e9cc0ab4112c44a78dd70d1cceaf2a67 = new ApplyAction<string>() { Lambda = input =>{abstractionModelManager.CreateAbstractionModelFromPath(input);} };
            DataFlowConnector<Dictionary<string, List<string>>> id_9a7d724b16de4bfd86837021ab25aed1 = new DataFlowConnector<Dictionary<string, List<string>>>() {  };
            Data<UIElement> id_ae274fb8c8754469bd390fad7467f3ad = new Data<UIElement>() { Lambda = () => mainCanvas };
            Scale id_947a99633a854b6ea03ad8ee9d48aef9 = new Scale() { WidthMultiplier = 1.1, HeightMultiplier = 1.1 };
            Data<UIElement> id_776ca16650954eef91e75ebe2ebe9e4e = new Data<UIElement>() { Lambda = () => mainCanvas };
            Scale id_b905260ac5a44c5caeb6fd68f1e1db88 = new Scale() { WidthMultiplier = 0.9, HeightMultiplier = 0.9 };
            DataFlowConnector<UIElement> id_2ca711558c924435b95fe6a1276b4d55 = new DataFlowConnector<UIElement>() {  };
            DataFlowConnector<UIElement> id_7316b641a8304948993f4e5cbc542ef6 = new DataFlowConnector<UIElement>() {  };
            ApplyAction<UIElement> id_82b2298c450741848a279c0d5de88a8e = new ApplyAction<UIElement>() { Lambda = input => {if (!(input.RenderTransform is ScaleTransform)) return;var transform = input.RenderTransform as ScaleTransform;var minScale = 0.6;/*Logging.Log($"Scale: {transform.ScaleX}, {transform.ScaleX}");*/bool nodeIsTooSmall = transform.ScaleX < minScale && transform.ScaleY < minScale;var nodes = mainGraph.Nodes;foreach (var node in nodes){if (node is ALANode alaNode) alaNode.ShowTypeTextMask(nodeIsTooSmall);}} };
            MouseWheelEvent id_9e249cf001ab46d4b11e6dd4d0b2f136 = new MouseWheelEvent(eventName: "MouseWheel") {  };
            Apply<MouseWheelEventArgs, bool> id_681985f0f46849fdb582544f39d9b31f = new Apply<MouseWheelEventArgs, bool>() { Lambda = args =>{return args.Delta > 0;} };
            IfElse id_ef8e7eeb74e0437bbcad7fd147cd92d2 = new IfElse() {  };
            DataFlowConnector<string> id_1bf76c59a9cb4960a4d40e6cb14d170b = new DataFlowConnector<string>() {  };
            ConvertToEvent<string> id_ffad88c305444e8ebc8780fc39dbef66 = new ConvertToEvent<string>() {  };
            DispatcherEvent id_da9da8de8ba749f9be7f7a4856d00bb3 = new DispatcherEvent() { Priority = DispatcherPriority.ApplicationIdle };
            // END AUTO-GENERATED INSTANTIATIONS FOR Application.xmind

            // BEGIN AUTO-GENERATED WIRING FOR Application.xmind
            mainWindow.WireTo(id_06f1d0d57ef94ca0a1b84eeb78bffcb8, "iuiStructure");
            mainWindow.WireTo(id_e1c72d1563cd43c59927cdb480bf2ccd, "appStart");
            id_06f1d0d57ef94ca0a1b84eeb78bffcb8.WireTo(id_8a303e6870e045f3bfbe81afe713410f, "children");
            id_06f1d0d57ef94ca0a1b84eeb78bffcb8.WireTo(id_3dd9c6b85d5f4720ad0b954852eb8bd5, "children");
            id_3dd9c6b85d5f4720ad0b954852eb8bd5.WireTo(id_0f2d6129132e456abc43e57dd30d3e74, "eventHandlers");
            id_3dd9c6b85d5f4720ad0b954852eb8bd5.WireTo(id_91540e8151384b00859c7cd7ac0db336, "eventHandlers");
            id_3dd9c6b85d5f4720ad0b954852eb8bd5.WireTo(id_36f646b635c34537abc0345c9ead9807, "eventHandlers");
            id_3dd9c6b85d5f4720ad0b954852eb8bd5.WireTo(id_494151e402ea4a5eb4dba8abe2440e4a, "eventHandlers");
            id_3dd9c6b85d5f4720ad0b954852eb8bd5.WireTo(id_42d03798020349a2a41089b5cac3e89d, "eventHandlers");
            id_3dd9c6b85d5f4720ad0b954852eb8bd5.WireTo(id_821befd29b254bbe9eb5d992d5a6e261, "eventHandlers");
            id_3dd9c6b85d5f4720ad0b954852eb8bd5.WireTo(id_9e249cf001ab46d4b11e6dd4d0b2f136, "eventHandlers");
            id_3dd9c6b85d5f4720ad0b954852eb8bd5.WireTo(id_de5f959c05754afcbed3aa3185cfa7ee, "contextMenu");
            id_0f2d6129132e456abc43e57dd30d3e74.WireTo(id_d428060e562e48b3bae6cacc417dabb3, "eventHappened");
            id_de5f959c05754afcbed3aa3185cfa7ee.WireTo(id_e07d2d0fb7014af78f8e524147997d35, "children");
            id_de5f959c05754afcbed3aa3185cfa7ee.WireTo(id_d810a11d5c884963b1a2e9246aae8427, "children");
            id_e07d2d0fb7014af78f8e524147997d35.WireTo(id_650dcd46c0184db1b9f248e6f93d62ad, "clickedEvent");
            id_d428060e562e48b3bae6cacc417dabb3.WireTo(id_216d5307f4424a08bf9a8640a6b7a367, "fanoutList");
            id_d428060e562e48b3bae6cacc417dabb3.WireTo(layoutDiagram, "complete");
            getFirstRoot.WireTo(id_dfad0e10d103413ca62341600eb53b84, "dataOutput");
            id_ffad88c305444e8ebc8780fc39dbef66.WireTo(layoutDiagram, "eventOutput");
            layoutDiagram.WireTo(id_da9da8de8ba749f9be7f7a4856d00bb3, "fanoutList");
            id_da9da8de8ba749f9be7f7a4856d00bb3.WireTo(getFirstRoot, "delayedEvent");
            id_dfad0e10d103413ca62341600eb53b84.WireTo(id_13a9cf4d34894dd8b00a76c1033d0bbf, "fanoutList");
            id_dfad0e10d103413ca62341600eb53b84.WireTo(id_3f6a1b7cd38e4abcb8e6020205bb3149, "fanoutList");
            id_91540e8151384b00859c7cd7ac0db336.WireTo(layoutDiagram, "eventHappened");
            createNewALANode.WireTo(createAndPaintALAWire, "output");
            id_8a303e6870e045f3bfbe81afe713410f.WireTo(id_72a644b6154e4dbc94903372d2dd52c8, "children");
            id_8a303e6870e045f3bfbe81afe713410f.WireTo(id_114addd3cd73472099d3b79f85c796fe, "children");
            id_72a644b6154e4dbc94903372d2dd52c8.WireTo(id_d006c330b62c4aefb8ee80e29c3d90a7, "children");
            id_72a644b6154e4dbc94903372d2dd52c8.WireTo(id_e64b7ef98e00461eaf464122f79658a8, "children");
            id_d006c330b62c4aefb8ee80e29c3d90a7.WireTo(id_5b358134e6c240e8861382d073b6daa9, "clickedEvent");
            id_5b358134e6c240e8861382d073b6daa9.WireTo(id_ad36a59dd1394606a949607e4943d4ad, "selectedFolderPathOutput");
            id_1223b6c4a9fb4a078880318febcdaceb.WireTo(id_9a7d724b16de4bfd86837021ab25aed1, "foundFiles");
            id_52bdc5261a8344d4b061df873795daa4.WireTo(id_28f4b656ee7640d1b74aa495c685c98a, "output");
            id_28f4b656ee7640d1b74aa495c685c98a.WireTo(id_4ef72270c04442c990d7c38c30df9083, "elementOutput");
            id_216d5307f4424a08bf9a8640a6b7a367.WireTo(id_85f5a744cd0d408b8ba10fa7653f27bd, "dataOutput");
            id_85f5a744cd0d408b8ba10fa7653f27bd.WireTo(createNewALANode, "output");
            id_54bf34594d0c485384eb3f349fd5e182.WireTo(id_ad36a59dd1394606a949607e4943d4ad, "dataOutput");
            id_36f646b635c34537abc0345c9ead9807.WireTo(id_2ee0c75f6cd04fd8bd1a8f50396b54a8, "senderOutput");
            id_114addd3cd73472099d3b79f85c796fe.WireTo(id_c8c8d57ff6a94373a4bac1f7206d9bff, "children");
            id_c8c8d57ff6a94373a4bac1f7206d9bff.WireTo(id_5a248626b00d48d6af02e424703b1436, "clickedEvent");
            id_5a248626b00d48d6af02e424703b1436.WireTo(id_8474f7bc283d460eb3a00dccc8a534f6, "children");
            id_8474f7bc283d460eb3a00dccc8a534f6.WireTo(id_db234cdb4bbb4de38a88dc88eb4c5c9d, "uiLayout");
            id_ad36a59dd1394606a949607e4943d4ad.WireTo(id_1223b6c4a9fb4a078880318febcdaceb, "fanoutList");
            id_ad36a59dd1394606a949607e4943d4ad.WireTo(projectFolderWatcher, "fanoutList");
            projectFolderWatcher.WireTo(id_cf6ee2e2add04654a1a3a885aacec011, "changedFile");
            id_cf6ee2e2add04654a1a3a885aacec011.WireTo(id_2576a7a2171a48c5b44198efcc488012, "output");
            id_2576a7a2171a48c5b44198efcc488012.WireTo(id_3f6a1b7cd38e4abcb8e6020205bb3149, "eventOutput");
            id_494151e402ea4a5eb4dba8abe2440e4a.WireTo(id_799e0914bfa74332bc18486b8e6e9334, "argsOutput");
            id_42d03798020349a2a41089b5cac3e89d.WireTo(id_4349830b8b124c6ebd440a5a0352bfda, "argsOutput");
            id_745aa054b25b4843a32d0725334698f4.WireTo(id_8a611d15af834af3a0a2623cd4d0dbec, "transitionOutput");
            id_8a611d15af834af3a0a2623cd4d0dbec.WireTo(id_46a7fdc3f4ce4e759958a47827fe8f84, "output");
            id_46a7fdc3f4ce4e759958a47827fe8f84.WireTo(layoutDiagram, "ifOutput");
            id_e1c72d1563cd43c59927cdb480bf2ccd.WireTo(id_54bf34594d0c485384eb3f349fd5e182, "fanoutList");
            id_e1c72d1563cd43c59927cdb480bf2ccd.WireTo(id_745aa054b25b4843a32d0725334698f4, "fanoutList");
            id_e1c72d1563cd43c59927cdb480bf2ccd.WireTo(id_522fbc174d774db89c8d90eeab24358a, "complete");
            id_821befd29b254bbe9eb5d992d5a6e261.WireTo(id_fabe6c8c7ff040758ecdd96a27df2399, "eventHappened");
            id_d810a11d5c884963b1a2e9246aae8427.WireTo(layoutDiagram, "clickedEvent");
            id_650dcd46c0184db1b9f248e6f93d62ad.WireTo(id_c1837a68ef004a42916551a89058e204, "dataOutput");
            id_c1837a68ef004a42916551a89058e204.WireTo(id_5ca3d72138804db4bcb1b009662ec4c5, "output");
            id_e64b7ef98e00461eaf464122f79658a8.WireTo(id_3ebc8dbcae7943a3a3ea9f11b09d8b58, "clickedEvent");
            id_3ebc8dbcae7943a3a3ea9f11b09d8b58.WireTo(id_f73e477ba258460fb79ec1a709d95999, "selectedFilePathOutput");
            id_f73e477ba258460fb79ec1a709d95999.WireTo(id_1bf76c59a9cb4960a4d40e6cb14d170b, "fileContentOutput");
            id_1bf76c59a9cb4960a4d40e6cb14d170b.WireTo(id_bde216fd6b864182857ac3f91c6473ad, "fanoutList");
            id_bde1265620d742a49ab4056d84661f12.WireTo(id_d040cfaff591463b9b912c5d608c99d3, "output");
            id_d040cfaff591463b9b912c5d608c99d3.WireTo(id_fd3ae12d55d4404aa6bb34664d50e352, "elementOutput");
            id_016ee1833bf84da989ef2518169fd3a8.WireTo(id_4ba321618cbd46ef9e1371ecc2b73a23, "output");
            id_4ba321618cbd46ef9e1371ecc2b73a23.WireTo(id_e9cc0ab4112c44a78dd70d1cceaf2a67, "elementOutput");
            id_9a7d724b16de4bfd86837021ab25aed1.WireTo(id_52bdc5261a8344d4b061df873795daa4, "fanoutList");
            id_9a7d724b16de4bfd86837021ab25aed1.WireTo(id_bde1265620d742a49ab4056d84661f12, "fanoutList");
            id_9a7d724b16de4bfd86837021ab25aed1.WireTo(id_016ee1833bf84da989ef2518169fd3a8, "fanoutList");
            id_ae274fb8c8754469bd390fad7467f3ad.WireTo(id_2ca711558c924435b95fe6a1276b4d55, "dataOutput");
            id_776ca16650954eef91e75ebe2ebe9e4e.WireTo(id_7316b641a8304948993f4e5cbc542ef6, "dataOutput");
            id_2ca711558c924435b95fe6a1276b4d55.WireTo(id_947a99633a854b6ea03ad8ee9d48aef9, "fanoutList");
            id_2ca711558c924435b95fe6a1276b4d55.WireTo(id_82b2298c450741848a279c0d5de88a8e, "fanoutList");
            id_7316b641a8304948993f4e5cbc542ef6.WireTo(id_b905260ac5a44c5caeb6fd68f1e1db88, "fanoutList");
            id_7316b641a8304948993f4e5cbc542ef6.WireTo(id_82b2298c450741848a279c0d5de88a8e, "fanoutList");
            id_9e249cf001ab46d4b11e6dd4d0b2f136.WireTo(id_681985f0f46849fdb582544f39d9b31f, "argsOutput");
            id_681985f0f46849fdb582544f39d9b31f.WireTo(id_ef8e7eeb74e0437bbcad7fd147cd92d2, "output");
            id_ef8e7eeb74e0437bbcad7fd147cd92d2.WireTo(id_ae274fb8c8754469bd390fad7467f3ad, "ifOutput");
            id_ef8e7eeb74e0437bbcad7fd147cd92d2.WireTo(id_776ca16650954eef91e75ebe2ebe9e4e, "elseOutput");
            id_1bf76c59a9cb4960a4d40e6cb14d170b.WireTo(id_ffad88c305444e8ebc8780fc39dbef66, "fanoutList");
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
















































































































































































































































