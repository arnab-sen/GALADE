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
            Vertical id_7ce8f0b24ffe465ea528f60944e0eacb = new Vertical() { };
            CanvasDisplay id_285bd7e1bb044e38b6a6adbaf9ca421a = new CanvasDisplay() { Width = 1920, Height = 1080, Background = Brushes.White, StateTransition = stateTransition, Canvas = mainCanvas };
            KeyEvent id_894eaf59da88466aa5eee86c830579a7 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.A }, Condition = args => mainGraph.Get("SelectedNode") != null && stateTransition.CurrentStateMatches(Enums.DiagramMode.IdleSelected) };
            ContextMenu id_38d6a4d01bd443fabf19eb29eef111f6 = new ContextMenu() { };
            MenuItem id_ba528d9bfe8d4a47a56c6a745812637a = new MenuItem(header: "Add root") { };
            EventConnector id_e5b2d7dd86344fa48637a2d5d6d61c28 = new EventConnector() { };
            Data<ALANode> getFirstRoot = new Data<ALANode>() { InstanceName = "getFirstRoot", Lambda = () => mainGraph.Roots.FirstOrDefault() as ALANode };
            RightTreeLayout<ALANode> id_7f891cdfd6c44bb196d174136376d718 = new RightTreeLayout<ALANode>() { GetID = n => n.Id, GetWidth = n => n.Width, GetHeight = n => n.Height, SetX = (n, x) => n.PositionX = x, SetY = (n, y) => n.PositionY = y, GetChildren = n => mainGraph.Edges.Where(e => e is ALAWire wire && wire.Source != null && wire.Destination != null && wire.Source == n).Select(e => ((e as ALAWire).Destination) as ALANode), HorizontalGap = 100, VerticalGap = 20, InitialX = 50, InitialY = 50 };
            EventConnector layoutDiagram = new EventConnector() { InstanceName = "layoutDiagram" };
            DataFlowConnector<ALANode> id_c10c76daff934af68e851a0aac13ed11 = new DataFlowConnector<ALANode>() { };
            ApplyAction<ALANode> id_6a694fb75765482c8d9fea77bfb92f47 = new ApplyAction<ALANode>() { Lambda = node => { Dispatcher.CurrentDispatcher.Invoke(() => { var edges = mainGraph.Edges; foreach (var edge in edges) { (edge as ALAWire).Refresh(); } }, DispatcherPriority.ContextIdle); } };
            KeyEvent id_f6af8a83749e4802b21a7d191eace478 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.R }, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            Apply<AbstractionModel, object> createNewALANode = new Apply<AbstractionModel, object>() { InstanceName = "createNewALANode", Lambda = input => { var node = new ALANode(); node.Model = input; node.Graph = mainGraph; node.Canvas = mainCanvas; node.StateTransition = stateTransition; node.AvailableDomainAbstractions.AddRange(abstractionModelManager.GetAbstractionTypes()); node.TypeChanged += newType => { abstractionModelManager.UpdateAbstractionModel(abstractionModelManager.GetAbstractionModel(newType), node.Model); node.UpdateUI(); Dispatcher.CurrentDispatcher.Invoke(() => { var edges = mainGraph.Edges; foreach (var edge in edges) { (edge as ALAWire).Refresh(); } }, DispatcherPriority.ContextIdle); }; mainGraph.AddNode(node); node.CreateInternals(); mainCanvas.Children.Add(node.Render); return node; } };
            MenuBar id_1a736a566dde4928806b36f190f8760d = new MenuBar() { };
            MenuItem id_3907f8d15cb74385be5b723227faa3c1 = new MenuItem(header: "File") { };
            MenuItem id_a670218d03364b8baf0555cc1869e583 = new MenuItem(header: "Open Project") { };
            FolderBrowser id_f7c33fefb3f54e47b18331c7da1d2a9c = new FolderBrowser() { Description = "" };
            DirectorySearch id_c9fbdd0fd959480f91bc65eb3d413010 = new DirectorySearch(directoriesToFind: new string[] { "DomainAbstractions", "ProgrammingParadigms", "RequirementsAbstractions", "Modules" }) { FilenameFilter = "*.cs" };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_c25c1fb9e53c47c0a60be8058e863ec1 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input => { var list = new List<string>(); if (input.ContainsKey("DomainAbstractions")) { list = input["DomainAbstractions"]; } return list; } };
            ForEach<string> id_10adc10eaafa4ab99f704872d4d921f9 = new ForEach<string>() { };
            ApplyAction<string> id_44fdd96b957a499fa4f377f6d4f506af = new ApplyAction<string>() { Lambda = input => { abstractionModelManager.CreateAbstractionModelFromPath(input); } };
            Data<string> id_373044784cc04c1d9135dc5f8a492c39 = new Data<string>() { storedData = "Apply" };
            Apply<string, AbstractionModel> id_afb3a875a9fd4b57af31993da73f85f1 = new Apply<string, AbstractionModel>() { Lambda = input => { return abstractionModelManager.GetAbstractionModel(input); } };
            GetSetting id_6de90001454048edbfa003e7f8df777d = new GetSetting(name: "ProjectFolderPath") { };
            KeyEvent id_194cb1ec368d499cb59d6964c21b45b5 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.Enter } };
            ApplyAction<object> id_2449026275c4426e9b7a7c174e77454d = new ApplyAction<object>() { Lambda = input => { (input as WPFCanvas).Focus(); } };
            MenuItem id_5ceb1003bccc40f797645a27cae88d62 = new MenuItem(header: "Debug") { };
            MenuItem id_0f629a2602fe4156b259fc0ff6503841 = new MenuItem(header: "TextEditor test") { };
            PopupWindow id_49fd370b5053448fb7445c60fb138ed2 = new PopupWindow(title: "") { Height = 720, Width = 1280, Resize = SizeToContent.WidthAndHeight };
            Box id_3b32d63dc96a4735acc62e63414112c7 = new Box() { Width = 100, Height = 100 };
            TextEditor id_abf3c862b5334dfbbff9cecc1857f3aa = new TextEditor() { Width = 1280, Height = 720 };
            DataFlowConnector<string> id_83ec7cc08aa644cf898c5f9b12ffc8ad = new DataFlowConnector<string>() { };
            FolderWatcher projectFolderWatcher = new FolderWatcher() { InstanceName = "projectFolderWatcher", RootPath = "", Filter = "*.cs", WatchSubdirectories = true, PathRegex = @".*\.cs$" };
            Apply<string, object> id_1ef1b4b0c13448c9ad97e1a7ab5dfc68 = new Apply<string, object>() { Lambda = input => { var newModel = abstractionModelManager.CreateAbstractionModelFromPath(input); foreach (var node in mainGraph.Nodes) { var alaNode = node as ALANode; if (alaNode.Model.Type != newModel.Type) continue; abstractionModelManager.UpdateAbstractionModel(newModel, alaNode.Model); alaNode.UpdateUI(); } return input; } };
            ConvertToEvent<object> id_daa4f9180171474f94f30a24f2d86aca = new ConvertToEvent<object>() { };
            MouseButtonEvent id_8fd2fb74f4844563874b1dadb572f5af = new MouseButtonEvent(eventName: "MouseRightButtonDown") { ExtractSender = null, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            ApplyAction<object> id_f7640d1312e7485e97479497128b1cc2 = new ApplyAction<object>() { Lambda = input => { Mouse.Capture(input as WPFCanvas); stateTransition.Update(Enums.DiagramMode.Idle); } };
            MouseButtonEvent id_16d08fd8dea0403987ebb1688609e498 = new MouseButtonEvent(eventName: "MouseRightButtonUp") { ExtractSender = null, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            ApplyAction<object> id_46501e5a4e984fbc8a7cc7151cb28bc5 = new ApplyAction<object>() { Lambda = input => { if (Mouse.Captured?.Equals(input) ?? false) Mouse.Capture(null); stateTransition.Update(Enums.DiagramMode.Idle); } };
            StateChangeListener id_3c5cc1c321ea4e0985ba7307eb8133b3 = new StateChangeListener() { StateTransition = stateTransition, CurrentStateShouldMatch = Enums.DiagramMode.All };
            Apply<Tuple<Enums.DiagramMode, Enums.DiagramMode>, bool> id_65197f3246da4b5e8b1417ca0a39f88f = new Apply<Tuple<Enums.DiagramMode, Enums.DiagramMode>, bool>() { Lambda = input => { return input.Item1 == Enums.DiagramMode.AwaitingPortSelection && input.Item2 == Enums.DiagramMode.Idle; } };
            IfElse id_802f8e14d8164340b83f0fe5bde8b599 = new IfElse() { };
            EventConnector id_ce6d68324bce4d06837a5af3c3f2330b = new EventConnector() { };
            Apply<object, object> createAndPaintALAWire = new Apply<object, object>() { InstanceName = "createAndPaintALAWire", Lambda = input => { var source = mainGraph.Get("SelectedNode") as ALANode; var destination = input as ALANode; var sourcePort = source.GetSelectedPort(inputPort: false); var destinationPort = destination.GetSelectedPort(inputPort: true); var wire = new ALAWire() { Graph = mainGraph, Canvas = mainCanvas, Source = source, Destination = destination, SourcePort = sourcePort, DestinationPort = destinationPort, StateTransition = stateTransition }; mainGraph.AddEdge(wire); wire.Paint(); return wire; } };
            KeyEvent id_4dc14200c1364e9e928b4deac7a97242 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.Delete } };
            EventLambda id_2ac4aa240cd84d7b9878fe9d3595da05 = new EventLambda() { Lambda = () => { var selectedNode = mainGraph.Get("SelectedNode") as ALANode; if (selectedNode == null) return; selectedNode.Delete(deleteAttachedWires: true); } };
            MenuItem id_5a2b12d3967e4e82a001e82f67f0b202 = new MenuItem(header: "Refresh") { };
            Data<AbstractionModel> id_b4a8034a68f14f22a28a16ef5df3f9df = new Data<AbstractionModel>() { Lambda = () => abstractionModelManager.GetAbstractionModel(abstractionModelManager.GetAbstractionTypes().FirstOrDefault()) };
            Apply<AbstractionModel, object> id_13ed12a8c042445b9053d166591b82db = new Apply<AbstractionModel, object>() { Lambda = createNewALANode.Lambda };
            ApplyAction<object> id_b9f861089f954fbdad8e46216bf35eaf = new ApplyAction<object>() { Lambda = input => { var alaNode = input as ALANode; var mousePos = Mouse.GetPosition(mainCanvas); alaNode.PositionX = mousePos.X; alaNode.PositionY = mousePos.Y; mainGraph.Set("LatestNode", input); if (mainGraph.Get("SelectedNode") == null) { mainGraph.Set("SelectedNode", input); } mainGraph.Roots.Add(input); } };
            MenuItem id_8666171942f942349e49f3dadc23637f = new MenuItem(header: "Open Code File") { };
            FileBrowser id_a077218cc2e34d42b177d22893049626 = new FileBrowser() { Mode = "Open" };
            FileReader id_7aa771371ce844088ba9c6bae1116d08 = new FileReader() { };
            CreateDiagramFromCode id_8a723f07f5704c06a28653ad0671eda8 = new CreateDiagramFromCode() { Graph = mainGraph, Canvas = mainCanvas, ModelManager = abstractionModelManager, StateTransition = stateTransition };
            EventConnector id_268253c3d32746af99e469d011dee565 = new EventConnector() { };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_269f031fc9fa438b80afecb05687a619 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input => { var list = new List<string>(); if (input.ContainsKey("ProgrammingParadigms")) { list = input["ProgrammingParadigms"]; } return list; } };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_548836058067486f84bca59f47d46569 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input => { var list = new List<string>(); if (input.ContainsKey("RequirementsAbstractions")) { list = input["RequirementsAbstractions"]; } return list; } };
            DataFlowConnector<Dictionary<string, List<string>>> id_936ba5d8c9b44bd2b09d88337f0a6df4 = new DataFlowConnector<Dictionary<string, List<string>>>() { };
            Data<UIElement> id_147f36d834b54501948be51c9161b9e3 = new Data<UIElement>() { Lambda = () => mainCanvas };
            Scale id_9e38ac1a71b640debeffacc3be0b8bd7 = new Scale() { WidthMultiplier = 1.1, HeightMultiplier = 1.1 };
            Data<UIElement> id_99c819b132a44f69a77f7517d3b12655 = new Data<UIElement>() { Lambda = () => mainCanvas };
            Scale id_3e7c599d4fc54c63b3c2d67b30eaeda7 = new Scale() { WidthMultiplier = 0.9, HeightMultiplier = 0.9 };
            DataFlowConnector<UIElement> id_47860358eb5d4ec8b84f6de229938a94 = new DataFlowConnector<UIElement>() { };
            DataFlowConnector<UIElement> id_ae1ce301c5ad4108a8eaa1b3d7ae21e9 = new DataFlowConnector<UIElement>() { };
            ApplyAction<UIElement> id_2cfd028d6fad4ea6a9621bead5624bc8 = new ApplyAction<UIElement>() { Lambda = input => { if (!(input.RenderTransform is ScaleTransform)) return; var transform = input.RenderTransform as ScaleTransform; var minScale = 0.6;/*Logging.Log($"Scale: {transform.ScaleX}, {transform.ScaleX}");*/bool nodeIsTooSmall = transform.ScaleX < minScale && transform.ScaleY < minScale; var nodes = mainGraph.Nodes; foreach (var node in nodes) { if (node is ALANode alaNode) alaNode.ShowTypeTextMask(nodeIsTooSmall); } } };
            MouseWheelEvent id_9a3a49cb88ff4b8281a1153012e11d01 = new MouseWheelEvent(eventName: "MouseWheel") { };
            Apply<MouseWheelEventArgs, bool> id_6de491e90d274bfd9dda71bff52e8a0c = new Apply<MouseWheelEventArgs, bool>() { Lambda = args => { return args.Delta > 0; } };
            IfElse id_0d36a559c7ef409f8f2eb2dbd3127cbe = new IfElse() { };
            DataFlowConnector<string> id_ec34675ad7f345ce96cbeddbd1d678a9 = new DataFlowConnector<string>() { };
            ConvertToEvent<string> id_c6ec62eb5c2a4a98827c24ca52cc257e = new ConvertToEvent<string>() { };
            DispatcherEvent id_23e6ec05d72e44c0861bdd7723b57304 = new DispatcherEvent() { Priority = DispatcherPriority.ApplicationIdle };
            MenuItem id_3b2ac1efa4874f7aa0f3bf9d71fa54ff = new MenuItem(header: "Generate Code") { };
            GenerateALACode id_9b84c63af92e43a6b0779d0a554658fb = new GenerateALACode() { Graph = mainGraph };
            GetSetting id_ea0f906f67cb41d38e7e1ca72c4ce3da = new GetSetting(name: "ApplicationCodeFilePath") { };
            Data<string> id_d4c143cc03314e269efc62b4e4ca1608 = new Data<string>() { storedData = SETTINGS_FILEPATH };
            EditSetting id_2f2f8b8b46664c4f8a9b730e23ca95d2 = new EditSetting() { JSONPath = "$..ApplicationCodeFilePath" };
            Data<string> id_9379748f85804eea90cd3a0657988e5a = new Data<string>() { storedData = SETTINGS_FILEPATH };
            Cast<string, object> id_7e89b7b2b7264e8fb8b1048a41a7bc30 = new Cast<string, object>() { };
            DataFlowConnector<string> id_501d612597a344a791b91bd679815385 = new DataFlowConnector<string>() { };
            Cast<string, object> id_807fd0ab643943938ca202c6e6541e84 = new Cast<string, object>() { };
            Data<string> id_e8680948c25e426ab5543e3a49189234 = new Data<string>() { storedData = SETTINGS_FILEPATH };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_09566dcc9ae24933a842fc1378cbae97 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input => { var list = new List<string>(); if (input.ContainsKey("Modules")) { list = input["Modules"]; } return list; } };
            GetSetting id_92d00b1622ca44f19804f2302c333bc8 = new GetSetting(name: "ApplicationCodeFilePath") { };
            Data<string> id_da197a2aa9ff47aaa2f14cc2c19a451d = new Data<string>() { storedData = SETTINGS_FILEPATH };
            InsertStatements id_fc6248925b304d2e8dd797f01c9cc1ff = new InsertStatements() { MethodName = "CreateWiring" };
            FileReader id_3cf9dc65ac6d49dd8e153d183a83eb4b = new FileReader() { };
            DataFlowConnector<string> id_936ab06c80ae4eaa843f13c6f4c51dbc = new DataFlowConnector<string>() { };
            EventConnector generateCode = new EventConnector() { InstanceName = "generateCode" };
            EditSetting id_8d521770dbf84bd4b6dda99ab9c1278f = new EditSetting() { JSONPath = "$..ProjectFolderPath" };
            Data<string> id_2aec33df61e04f3e977506beae4c50fe = new Data<string>() { storedData = SETTINGS_FILEPATH };
            FileWriter id_e96405f3de214e41b1e07f6a20d16b43 = new FileWriter() { };
            DataFlowConnector<string> id_86f3c480f4ff40848d60fa149c21411d = new DataFlowConnector<string>() { };
            // END AUTO-GENERATED INSTANTIATIONS FOR Application.xmind

            // BEGIN AUTO-GENERATED WIRING FOR Application.xmind
            mainWindow.WireTo(id_7ce8f0b24ffe465ea528f60944e0eacb, "iuiStructure");
            mainWindow.WireTo(id_ce6d68324bce4d06837a5af3c3f2330b, "appStart");
            id_7ce8f0b24ffe465ea528f60944e0eacb.WireTo(id_1a736a566dde4928806b36f190f8760d, "children");
            id_7ce8f0b24ffe465ea528f60944e0eacb.WireTo(id_285bd7e1bb044e38b6a6adbaf9ca421a, "children");
            id_285bd7e1bb044e38b6a6adbaf9ca421a.WireTo(id_894eaf59da88466aa5eee86c830579a7, "eventHandlers");
            id_285bd7e1bb044e38b6a6adbaf9ca421a.WireTo(id_f6af8a83749e4802b21a7d191eace478, "eventHandlers");
            id_285bd7e1bb044e38b6a6adbaf9ca421a.WireTo(id_194cb1ec368d499cb59d6964c21b45b5, "eventHandlers");
            id_285bd7e1bb044e38b6a6adbaf9ca421a.WireTo(id_8fd2fb74f4844563874b1dadb572f5af, "eventHandlers");
            id_285bd7e1bb044e38b6a6adbaf9ca421a.WireTo(id_16d08fd8dea0403987ebb1688609e498, "eventHandlers");
            id_285bd7e1bb044e38b6a6adbaf9ca421a.WireTo(id_4dc14200c1364e9e928b4deac7a97242, "eventHandlers");
            id_285bd7e1bb044e38b6a6adbaf9ca421a.WireTo(id_9a3a49cb88ff4b8281a1153012e11d01, "eventHandlers");
            id_285bd7e1bb044e38b6a6adbaf9ca421a.WireTo(id_38d6a4d01bd443fabf19eb29eef111f6, "contextMenu");
            id_894eaf59da88466aa5eee86c830579a7.WireTo(id_e5b2d7dd86344fa48637a2d5d6d61c28, "eventHappened");
            id_38d6a4d01bd443fabf19eb29eef111f6.WireTo(id_ba528d9bfe8d4a47a56c6a745812637a, "children");
            id_38d6a4d01bd443fabf19eb29eef111f6.WireTo(id_5a2b12d3967e4e82a001e82f67f0b202, "children");
            id_ba528d9bfe8d4a47a56c6a745812637a.WireTo(id_b4a8034a68f14f22a28a16ef5df3f9df, "clickedEvent");
            id_e5b2d7dd86344fa48637a2d5d6d61c28.WireTo(id_373044784cc04c1d9135dc5f8a492c39, "fanoutList");
            id_e5b2d7dd86344fa48637a2d5d6d61c28.WireTo(layoutDiagram, "complete");
            getFirstRoot.WireTo(id_c10c76daff934af68e851a0aac13ed11, "dataOutput");
            layoutDiagram.WireTo(id_23e6ec05d72e44c0861bdd7723b57304, "fanoutList");
            id_c10c76daff934af68e851a0aac13ed11.WireTo(id_7f891cdfd6c44bb196d174136376d718, "fanoutList");
            id_c10c76daff934af68e851a0aac13ed11.WireTo(id_6a694fb75765482c8d9fea77bfb92f47, "fanoutList");
            id_f6af8a83749e4802b21a7d191eace478.WireTo(layoutDiagram, "eventHappened");
            createNewALANode.WireTo(createAndPaintALAWire, "output");
            id_1a736a566dde4928806b36f190f8760d.WireTo(id_3907f8d15cb74385be5b723227faa3c1, "children");
            id_1a736a566dde4928806b36f190f8760d.WireTo(id_5ceb1003bccc40f797645a27cae88d62, "children");
            id_3907f8d15cb74385be5b723227faa3c1.WireTo(id_a670218d03364b8baf0555cc1869e583, "children");
            id_3907f8d15cb74385be5b723227faa3c1.WireTo(id_8666171942f942349e49f3dadc23637f, "children");
            id_a670218d03364b8baf0555cc1869e583.WireTo(id_f7c33fefb3f54e47b18331c7da1d2a9c, "clickedEvent");
            id_f7c33fefb3f54e47b18331c7da1d2a9c.WireTo(id_83ec7cc08aa644cf898c5f9b12ffc8ad, "selectedFolderPathOutput");
            id_c9fbdd0fd959480f91bc65eb3d413010.WireTo(id_936ba5d8c9b44bd2b09d88337f0a6df4, "foundFiles");
            id_c25c1fb9e53c47c0a60be8058e863ec1.WireTo(id_10adc10eaafa4ab99f704872d4d921f9, "output");
            id_10adc10eaafa4ab99f704872d4d921f9.WireTo(id_44fdd96b957a499fa4f377f6d4f506af, "elementOutput");
            id_373044784cc04c1d9135dc5f8a492c39.WireTo(id_afb3a875a9fd4b57af31993da73f85f1, "dataOutput");
            id_afb3a875a9fd4b57af31993da73f85f1.WireTo(createNewALANode, "output");
            id_6de90001454048edbfa003e7f8df777d.WireTo(id_e8680948c25e426ab5543e3a49189234, "filePathInput");
            id_6de90001454048edbfa003e7f8df777d.WireTo(id_83ec7cc08aa644cf898c5f9b12ffc8ad, "settingJsonOutput");
            id_194cb1ec368d499cb59d6964c21b45b5.WireTo(id_2449026275c4426e9b7a7c174e77454d, "senderOutput");
            id_5ceb1003bccc40f797645a27cae88d62.WireTo(id_0f629a2602fe4156b259fc0ff6503841, "children");
            id_5ceb1003bccc40f797645a27cae88d62.WireTo(id_3b2ac1efa4874f7aa0f3bf9d71fa54ff, "children");
            id_0f629a2602fe4156b259fc0ff6503841.WireTo(id_49fd370b5053448fb7445c60fb138ed2, "clickedEvent");
            id_49fd370b5053448fb7445c60fb138ed2.WireTo(id_3b32d63dc96a4735acc62e63414112c7, "children");
            id_3b32d63dc96a4735acc62e63414112c7.WireTo(id_abf3c862b5334dfbbff9cecc1857f3aa, "uiLayout");
            id_83ec7cc08aa644cf898c5f9b12ffc8ad.WireTo(id_c9fbdd0fd959480f91bc65eb3d413010, "fanoutList");
            id_83ec7cc08aa644cf898c5f9b12ffc8ad.WireTo(projectFolderWatcher, "fanoutList");
            id_83ec7cc08aa644cf898c5f9b12ffc8ad.WireTo(id_807fd0ab643943938ca202c6e6541e84, "fanoutList");
            projectFolderWatcher.WireTo(id_1ef1b4b0c13448c9ad97e1a7ab5dfc68, "changedFile");
            id_1ef1b4b0c13448c9ad97e1a7ab5dfc68.WireTo(id_daa4f9180171474f94f30a24f2d86aca, "output");
            id_daa4f9180171474f94f30a24f2d86aca.WireTo(id_6a694fb75765482c8d9fea77bfb92f47, "eventOutput");
            id_8fd2fb74f4844563874b1dadb572f5af.WireTo(id_f7640d1312e7485e97479497128b1cc2, "argsOutput");
            id_16d08fd8dea0403987ebb1688609e498.WireTo(id_46501e5a4e984fbc8a7cc7151cb28bc5, "argsOutput");
            id_3c5cc1c321ea4e0985ba7307eb8133b3.WireTo(id_65197f3246da4b5e8b1417ca0a39f88f, "transitionOutput");
            id_65197f3246da4b5e8b1417ca0a39f88f.WireTo(id_802f8e14d8164340b83f0fe5bde8b599, "output");
            id_802f8e14d8164340b83f0fe5bde8b599.WireTo(layoutDiagram, "ifOutput");
            id_ce6d68324bce4d06837a5af3c3f2330b.WireTo(id_6de90001454048edbfa003e7f8df777d, "fanoutList");
            id_ce6d68324bce4d06837a5af3c3f2330b.WireTo(id_3c5cc1c321ea4e0985ba7307eb8133b3, "fanoutList");
            id_ce6d68324bce4d06837a5af3c3f2330b.WireTo(id_ea0f906f67cb41d38e7e1ca72c4ce3da, "fanoutList");
            id_ce6d68324bce4d06837a5af3c3f2330b.WireTo(id_268253c3d32746af99e469d011dee565, "complete");
            id_4dc14200c1364e9e928b4deac7a97242.WireTo(id_2ac4aa240cd84d7b9878fe9d3595da05, "eventHappened");
            id_5a2b12d3967e4e82a001e82f67f0b202.WireTo(layoutDiagram, "clickedEvent");
            id_b4a8034a68f14f22a28a16ef5df3f9df.WireTo(id_13ed12a8c042445b9053d166591b82db, "dataOutput");
            id_13ed12a8c042445b9053d166591b82db.WireTo(id_b9f861089f954fbdad8e46216bf35eaf, "output");
            id_8666171942f942349e49f3dadc23637f.WireTo(id_a077218cc2e34d42b177d22893049626, "clickedEvent");
            id_a077218cc2e34d42b177d22893049626.WireTo(id_501d612597a344a791b91bd679815385, "selectedFilePathOutput");
            id_7aa771371ce844088ba9c6bae1116d08.WireTo(id_ec34675ad7f345ce96cbeddbd1d678a9, "fileContentOutput");
            id_269f031fc9fa438b80afecb05687a619.WireTo(id_10adc10eaafa4ab99f704872d4d921f9, "output");
            id_548836058067486f84bca59f47d46569.WireTo(id_10adc10eaafa4ab99f704872d4d921f9, "output");
            id_936ba5d8c9b44bd2b09d88337f0a6df4.WireTo(id_c25c1fb9e53c47c0a60be8058e863ec1, "fanoutList");
            id_936ba5d8c9b44bd2b09d88337f0a6df4.WireTo(id_269f031fc9fa438b80afecb05687a619, "fanoutList");
            id_936ba5d8c9b44bd2b09d88337f0a6df4.WireTo(id_548836058067486f84bca59f47d46569, "fanoutList");
            id_936ba5d8c9b44bd2b09d88337f0a6df4.WireTo(id_09566dcc9ae24933a842fc1378cbae97, "fanoutList");
            id_147f36d834b54501948be51c9161b9e3.WireTo(id_47860358eb5d4ec8b84f6de229938a94, "dataOutput");
            id_99c819b132a44f69a77f7517d3b12655.WireTo(id_ae1ce301c5ad4108a8eaa1b3d7ae21e9, "dataOutput");
            id_47860358eb5d4ec8b84f6de229938a94.WireTo(id_9e38ac1a71b640debeffacc3be0b8bd7, "fanoutList");
            id_47860358eb5d4ec8b84f6de229938a94.WireTo(id_2cfd028d6fad4ea6a9621bead5624bc8, "fanoutList");
            id_ae1ce301c5ad4108a8eaa1b3d7ae21e9.WireTo(id_3e7c599d4fc54c63b3c2d67b30eaeda7, "fanoutList");
            id_ae1ce301c5ad4108a8eaa1b3d7ae21e9.WireTo(id_2cfd028d6fad4ea6a9621bead5624bc8, "fanoutList");
            id_9a3a49cb88ff4b8281a1153012e11d01.WireTo(id_6de491e90d274bfd9dda71bff52e8a0c, "argsOutput");
            id_6de491e90d274bfd9dda71bff52e8a0c.WireTo(id_0d36a559c7ef409f8f2eb2dbd3127cbe, "output");
            id_0d36a559c7ef409f8f2eb2dbd3127cbe.WireTo(id_147f36d834b54501948be51c9161b9e3, "ifOutput");
            id_0d36a559c7ef409f8f2eb2dbd3127cbe.WireTo(id_99c819b132a44f69a77f7517d3b12655, "elseOutput");
            id_ec34675ad7f345ce96cbeddbd1d678a9.WireTo(id_8a723f07f5704c06a28653ad0671eda8, "fanoutList");
            id_ec34675ad7f345ce96cbeddbd1d678a9.WireTo(id_c6ec62eb5c2a4a98827c24ca52cc257e, "fanoutList");
            id_c6ec62eb5c2a4a98827c24ca52cc257e.WireTo(layoutDiagram, "eventOutput");
            id_23e6ec05d72e44c0861bdd7723b57304.WireTo(getFirstRoot, "delayedEvent");
            id_3b2ac1efa4874f7aa0f3bf9d71fa54ff.WireTo(generateCode, "clickedEvent");
            id_9b84c63af92e43a6b0779d0a554658fb.WireTo(id_fc6248925b304d2e8dd797f01c9cc1ff, "allCode");
            id_ea0f906f67cb41d38e7e1ca72c4ce3da.WireTo(id_d4c143cc03314e269efc62b4e4ca1608, "filePathInput");
            id_2f2f8b8b46664c4f8a9b730e23ca95d2.WireTo(id_9379748f85804eea90cd3a0657988e5a, "filePathInput");
            id_7e89b7b2b7264e8fb8b1048a41a7bc30.WireTo(id_2f2f8b8b46664c4f8a9b730e23ca95d2, "output");
            id_501d612597a344a791b91bd679815385.WireTo(id_7aa771371ce844088ba9c6bae1116d08, "fanoutList");
            id_501d612597a344a791b91bd679815385.WireTo(id_7e89b7b2b7264e8fb8b1048a41a7bc30, "fanoutList");
            id_807fd0ab643943938ca202c6e6541e84.WireTo(id_8d521770dbf84bd4b6dda99ab9c1278f, "output");
            id_09566dcc9ae24933a842fc1378cbae97.WireTo(id_10adc10eaafa4ab99f704872d4d921f9, "output");
            id_92d00b1622ca44f19804f2302c333bc8.WireTo(id_da197a2aa9ff47aaa2f14cc2c19a451d, "filePathInput");
            id_92d00b1622ca44f19804f2302c333bc8.WireTo(id_86f3c480f4ff40848d60fa149c21411d, "settingJsonOutput");
            id_86f3c480f4ff40848d60fa149c21411d.WireTo(id_3cf9dc65ac6d49dd8e153d183a83eb4b, "fanoutList");
            id_fc6248925b304d2e8dd797f01c9cc1ff.WireTo(id_936ab06c80ae4eaa843f13c6f4c51dbc, "destinationCodeInput");
            id_fc6248925b304d2e8dd797f01c9cc1ff.WireTo(id_e96405f3de214e41b1e07f6a20d16b43, "newCode");
            id_3cf9dc65ac6d49dd8e153d183a83eb4b.WireTo(id_936ab06c80ae4eaa843f13c6f4c51dbc, "fileContentOutput");
            generateCode.WireTo(id_92d00b1622ca44f19804f2302c333bc8, "fanoutList");
            generateCode.WireTo(id_9b84c63af92e43a6b0779d0a554658fb, "fanoutList");
            id_8d521770dbf84bd4b6dda99ab9c1278f.WireTo(id_2aec33df61e04f3e977506beae4c50fe, "filePathInput");
            id_e96405f3de214e41b1e07f6a20d16b43.WireTo(id_86f3c480f4ff40848d60fa149c21411d, "filePathInput");
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
