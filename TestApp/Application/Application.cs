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
            Vertical id_f026d72f901b4ab09e0fb762379d4814 = new Vertical() {  };
            CanvasDisplay id_40b4e2e1531540db8fa4a48fec79e359 = new CanvasDisplay() { Width = 1920, Height = 600, Background = Brushes.White, StateTransition = stateTransition };
            ApplyAction<System.Windows.Controls.Canvas> id_d25e5e124dfe4c1aae98fc64ae574f86 = new ApplyAction<System.Windows.Controls.Canvas>() { Lambda = canvas => mainCanvas = canvas };
            KeyEvent id_141b96cca1b641dbacef5cdd8c099d70 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.A }, Condition = args => mainGraph.Get("SelectedNode") != null&& stateTransition.CurrentStateMatches(Enums.DiagramMode.IdleSelected) };
            Data<object> id_870e0c53e0e441c0a6195c6186c46512 = new Data<object>() { Lambda = () => {var node = new ALANode();node.Model = abstractionModelManager.GetAbstractionModel(abstractionModelManager.GetAbstractionTypes().FirstOrDefault());node.Graph = mainGraph;node.Canvas = mainCanvas;node.StateTransition = stateTransition;node.AvailableDomainAbstractions.AddRange( abstractionModelManager.GetAbstractionTypes());node.TypeChanged += newType => {abstractionModelManager.UpdateAbstractionModel(abstractionModelManager.GetAbstractionModel(newType),node.Model);node.UpdateUI();Dispatcher.CurrentDispatcher.Invoke(() => {var edges = mainGraph.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);};mainGraph.AddNode(node);node.CreateInternals();mainCanvas.Children.Add(node.Render);return node;} };
            ApplyAction<object> initialiseNode = new ApplyAction<object>() { InstanceName = "initialiseNode", Lambda = input =>{var render = (input as ALANode).Render;var mousePos = Mouse.GetPosition(mainCanvas);WPFCanvas.SetLeft(render, mousePos.X);WPFCanvas.SetTop(render, mousePos.Y);mainGraph.Set("LatestNode", input);if (mainGraph.Get("SelectedNode") == null){mainGraph.Set("SelectedNode", input);mainGraph.Roots.Add(input);}} };
            ContextMenu id_9efe85be341f43d2aa90dd68c4e3aa6d = new ContextMenu() {  };
            MenuItem id_80d23439338e4b408fd3a1417a4920c0 = new MenuItem(header: "Add root") {  };
            EventConnector id_0e416eab031c4909a6db9f50bd803cc5 = new EventConnector() {  };
            Data<ALANode> id_60df3f9423b44d10b2a77ce811535b96 = new Data<ALANode>() { Lambda = () => mainGraph.Roots.First() as ALANode };
            RightTreeLayout<ALANode> id_79e59d4cb985402ca474dfa2bb1b339f = new RightTreeLayout<ALANode>() { GetID = n => n.Id, GetWidth = n => (n.Render as FrameworkElement).ActualWidth, GetHeight = n => (n.Render as FrameworkElement).ActualHeight, SetX = (n, x) => WPFCanvas.SetLeft(n.Render, x), SetY = (n, y) => WPFCanvas.SetTop(n.Render, y), GetChildren = n => mainGraph.Edges.Where(e => e is ALAWire wire && wire.Source == n).Select(e => ((e as ALAWire).Destination) as ALANode), HorizontalGap = 100, VerticalGap = 20, InitialX = 50, InitialY = 50 };
            EventConnector layoutDiagram = new EventConnector() { InstanceName = "layoutDiagram" };
            DataFlowConnector<ALANode> id_0065bfa72f7e44dcb1b44f28ff7617eb = new DataFlowConnector<ALANode>() {  };
            ApplyAction<ALANode> id_7dbd843acae54d92b6ee11e4402f7c58 = new ApplyAction<ALANode>() { Lambda = node =>{Dispatcher.CurrentDispatcher.Invoke(() => {var edges = mainGraph.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);} };
            KeyEvent id_287c3b8021c64809933423ab1ea1cd16 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.R }, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            Apply<AbstractionModel, object> id_a10dd13f71134f37b233f2ff28657efb = new Apply<AbstractionModel, object>() { Lambda = input => {var node = new ALANode();node.Model = input;node.Graph = mainGraph;node.Canvas = mainCanvas;node.StateTransition = stateTransition;node.AvailableDomainAbstractions.AddRange( abstractionModelManager.GetAbstractionTypes());node.TypeChanged += newType => {abstractionModelManager.UpdateAbstractionModel(abstractionModelManager.GetAbstractionModel(newType),node.Model);node.UpdateUI();Dispatcher.CurrentDispatcher.Invoke(() => {var edges = mainGraph.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);};mainGraph.AddNode(node);node.CreateInternals();mainCanvas.Children.Add(node.Render);return node;} };
            MenuBar id_05ea80ee7e694f15a10e0f9856c686b5 = new MenuBar() {  };
            MenuItem id_37ea09a9a0d3442d9ede8d2db566d783 = new MenuItem(header: "File") {  };
            MenuItem id_5c514c97ccb04e0984fe7875019b8ec5 = new MenuItem(header: "Open Project") {  };
            FolderBrowser id_8eaf63e785904ed7a276fa81a6dda1c2 = new FolderBrowser() { Description = "" };
            DirectorySearch id_520a0086daec437885e0bbed04b3c3d4 = new DirectorySearch(directoriesToFind: new string[] { "DomainAbstractions" }) { FilenameFilter = "*.cs" };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_246224c88ae04f73a2252b454df7fb02 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("DomainAbstractions")){list = input["DomainAbstractions"];}return list;} };
            ForEach<string> id_d359fc3af99f496bbc6e5a292fe89bf3 = new ForEach<string>() {  };
            ApplyAction<string> id_136de14cbb6c4575bf4d7d37e036c60a = new ApplyAction<string>() { Lambda = input =>{abstractionModelManager.CreateAbstractionModelFromPath(input);} };
            Data<string> id_97218d99d72947859b7ab46071cbc7b4 = new Data<string>() { storedData = "Apply<T1, T2>" };
            Apply<string, AbstractionModel> id_64d56c33c1984db0ba65f06b0591bde1 = new Apply<string, AbstractionModel>() { Lambda = input =>{return abstractionModelManager.GetAbstractionModel(input);} };
            Data<string> id_bf41498cf7844e81a60e5bcd25e7934a = new Data<string>() { storedData = @"F:\Projects\GALADE\ALACore" };
            DropDownMenu id_174806188a174769af0d4bcbae4ad737 = new DropDownMenu() { Items = new string[100] };
            KeyEvent id_71408faa702f4147afd69b58141ff994 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.Enter } };
            ApplyAction<object> id_29ffb2a8733b4691b380b4d0f4b432d9 = new ApplyAction<object>() { Lambda = input =>{(input as WPFCanvas).Focus();} };
            MenuItem id_4c222ed4045747d3ad92747c70ee5ace = new MenuItem(header: "Debug") {  };
            MenuItem id_996b6e444d6d497d8b5bcf19e61af937 = new MenuItem(header: "TextEditor test") {  };
            PopupWindow id_09aeec84d6d147dbbb29d0706a8090da = new PopupWindow(title: "") { Height = 720, Width = 1280, Resize = SizeToContent.WidthAndHeight };
            Box id_b0179b13b0404f69b62dffd02a433deb = new Box() { Width = 100, Height = 100 };
            TextEditor id_3bcc853c43ed4c11ab90007535f02e18 = new TextEditor() { Width = 1280, Height = 720 };
            DataFlowConnector<string> id_a2da991ade264f7880273aaf1cc0f6af = new DataFlowConnector<string>() {  };
            FolderWatcher projectFolderWatcher = new FolderWatcher() { InstanceName = "projectFolderWatcher", RootPath = "", Filter = "*.cs", WatchSubdirectories = true, PathRegex = @".*\.cs$" };
            Apply<string, object> id_20380f2ee67744a28adfcb9a1567d324 = new Apply<string, object>() { Lambda = input =>{var newModel = abstractionModelManager.CreateAbstractionModelFromPath(input);foreach (var node in mainGraph.Nodes){var alaNode = node as ALANode;if (alaNode.Model.Type != newModel.Type) continue;abstractionModelManager.UpdateAbstractionModel(newModel, alaNode.Model);alaNode.UpdateUI();}return input;} };
            ConvertToEvent<object> id_9e5d732baf614f039aa54a44e3cab8e3 = new ConvertToEvent<object>() {  };
            MouseButtonEvent id_4f7f3189d95e49d5b7a7efd449c06bcb = new MouseButtonEvent(eventName: "MouseRightButtonDown") { ExtractSender = null, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            ApplyAction<object> id_67afe6acdef745a2b4332ee067ae0d64 = new ApplyAction<object>() { Lambda = input =>{Mouse.Capture(input as WPFCanvas);stateTransition.Update(Enums.DiagramMode.Idle);} };
            MouseButtonEvent id_e5c3464efb7642bb9fabd1516cc929de = new MouseButtonEvent(eventName: "MouseRightButtonUp") { ExtractSender = null, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            ApplyAction<object> id_89a41c44a04f4d0c90767d54d336b244 = new ApplyAction<object>() { Lambda = input =>{if (Mouse.Captured?.Equals(input) ?? false) Mouse.Capture(null);stateTransition.Update(Enums.DiagramMode.Idle);} };
            StateChangeListener id_e40ec558a4b54983bc3c6fcfc4ea0f9d = new StateChangeListener() { StateTransition = stateTransition, CurrentStateShouldMatch = Enums.DiagramMode.All };
            Apply<Tuple<Enums.DiagramMode, Enums.DiagramMode>, bool> id_d48f32df4e1549b7ae40945b4c14d2aa = new Apply<Tuple<Enums.DiagramMode, Enums.DiagramMode>, bool>() { Lambda = input =>{return input.Item1 == Enums.DiagramMode.AwaitingPortSelection &&input.Item2 == Enums.DiagramMode.Idle;} };
            IfElse id_19a82dabda6a4496876aa78d3eba5819 = new IfElse() {  };
            EventConnector id_cc600bbebaae4fd3bb1d399c688de7e3 = new EventConnector() {  };
            Apply<object, object> createAndPaintALAWire = new Apply<object, object>() { InstanceName = "createAndPaintALAWire", Lambda = input =>{var source = mainGraph.Get("SelectedNode") as ALANode;var destination = input as ALANode;var sourcePort = source.GetSelectedPort(inputPort: false);var destinationPort = destination.GetSelectedPort(inputPort: true);var wire = new ALAWire(){Graph = mainGraph,Canvas = mainCanvas,Source = source,Destination = destination,SourcePort = sourcePort,DestinationPort = destinationPort,StateTransition = stateTransition};mainGraph.AddEdge(wire);wire.Paint();return wire;} };
            KeyEvent id_05850b398be04478b0058b1724e4075e = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.Delete } };
            EventLambda id_c0d6ac07609646dd9504269f8ec9721b = new EventLambda() { Lambda = () =>{var selectedNode = mainGraph.Get("SelectedNode") as ALANode;if (selectedNode == null) return;selectedNode.Delete(deleteAttachedWires: true);} };
            // END AUTO-GENERATED INSTANTIATIONS FOR Application.xmind

            // BEGIN AUTO-GENERATED WIRING FOR Application.xmind
            mainWindow.WireTo(id_f026d72f901b4ab09e0fb762379d4814, "iuiStructure");
            mainWindow.WireTo(id_cc600bbebaae4fd3bb1d399c688de7e3, "appStart");
            id_f026d72f901b4ab09e0fb762379d4814.WireTo(id_05ea80ee7e694f15a10e0f9856c686b5, "children");
            id_f026d72f901b4ab09e0fb762379d4814.WireTo(id_40b4e2e1531540db8fa4a48fec79e359, "children");
            id_40b4e2e1531540db8fa4a48fec79e359.WireTo(id_d25e5e124dfe4c1aae98fc64ae574f86, "canvasOutput");
            id_40b4e2e1531540db8fa4a48fec79e359.WireTo(id_141b96cca1b641dbacef5cdd8c099d70, "eventHandlers");
            id_40b4e2e1531540db8fa4a48fec79e359.WireTo(id_287c3b8021c64809933423ab1ea1cd16, "eventHandlers");
            id_40b4e2e1531540db8fa4a48fec79e359.WireTo(id_71408faa702f4147afd69b58141ff994, "eventHandlers");
            id_40b4e2e1531540db8fa4a48fec79e359.WireTo(id_4f7f3189d95e49d5b7a7efd449c06bcb, "eventHandlers");
            id_40b4e2e1531540db8fa4a48fec79e359.WireTo(id_e5c3464efb7642bb9fabd1516cc929de, "eventHandlers");
            id_40b4e2e1531540db8fa4a48fec79e359.WireTo(id_05850b398be04478b0058b1724e4075e, "eventHandlers");
            id_40b4e2e1531540db8fa4a48fec79e359.WireTo(id_9efe85be341f43d2aa90dd68c4e3aa6d, "contextMenu");
            id_141b96cca1b641dbacef5cdd8c099d70.WireTo(id_0e416eab031c4909a6db9f50bd803cc5, "eventHappened");
            id_870e0c53e0e441c0a6195c6186c46512.WireTo(initialiseNode, "dataOutput");
            id_9efe85be341f43d2aa90dd68c4e3aa6d.WireTo(id_80d23439338e4b408fd3a1417a4920c0, "children");
            id_80d23439338e4b408fd3a1417a4920c0.WireTo(id_870e0c53e0e441c0a6195c6186c46512, "clickedEvent");
            id_0e416eab031c4909a6db9f50bd803cc5.WireTo(id_97218d99d72947859b7ab46071cbc7b4, "fanoutList");
            id_0e416eab031c4909a6db9f50bd803cc5.WireTo(layoutDiagram, "complete");
            id_60df3f9423b44d10b2a77ce811535b96.WireTo(id_0065bfa72f7e44dcb1b44f28ff7617eb, "dataOutput");
            layoutDiagram.WireTo(id_60df3f9423b44d10b2a77ce811535b96, "fanoutList");
            id_0065bfa72f7e44dcb1b44f28ff7617eb.WireTo(id_79e59d4cb985402ca474dfa2bb1b339f, "fanoutList");
            id_0065bfa72f7e44dcb1b44f28ff7617eb.WireTo(id_7dbd843acae54d92b6ee11e4402f7c58, "fanoutList");
            id_287c3b8021c64809933423ab1ea1cd16.WireTo(layoutDiagram, "eventHappened");
            id_a10dd13f71134f37b233f2ff28657efb.WireTo(createAndPaintALAWire, "output");
            id_05ea80ee7e694f15a10e0f9856c686b5.WireTo(id_37ea09a9a0d3442d9ede8d2db566d783, "children");
            id_05ea80ee7e694f15a10e0f9856c686b5.WireTo(id_4c222ed4045747d3ad92747c70ee5ace, "children");
            id_37ea09a9a0d3442d9ede8d2db566d783.WireTo(id_5c514c97ccb04e0984fe7875019b8ec5, "children");
            id_37ea09a9a0d3442d9ede8d2db566d783.WireTo(id_174806188a174769af0d4bcbae4ad737, "children");
            id_5c514c97ccb04e0984fe7875019b8ec5.WireTo(id_8eaf63e785904ed7a276fa81a6dda1c2, "clickedEvent");
            id_8eaf63e785904ed7a276fa81a6dda1c2.WireTo(id_a2da991ade264f7880273aaf1cc0f6af, "selectedFolderPathOutput");
            id_520a0086daec437885e0bbed04b3c3d4.WireTo(id_246224c88ae04f73a2252b454df7fb02, "foundFiles");
            id_246224c88ae04f73a2252b454df7fb02.WireTo(id_d359fc3af99f496bbc6e5a292fe89bf3, "output");
            id_d359fc3af99f496bbc6e5a292fe89bf3.WireTo(id_136de14cbb6c4575bf4d7d37e036c60a, "elementOutput");
            id_97218d99d72947859b7ab46071cbc7b4.WireTo(id_64d56c33c1984db0ba65f06b0591bde1, "dataOutput");
            id_64d56c33c1984db0ba65f06b0591bde1.WireTo(id_a10dd13f71134f37b233f2ff28657efb, "output");
            id_bf41498cf7844e81a60e5bcd25e7934a.WireTo(id_a2da991ade264f7880273aaf1cc0f6af, "dataOutput");
            id_71408faa702f4147afd69b58141ff994.WireTo(id_29ffb2a8733b4691b380b4d0f4b432d9, "senderOutput");
            id_4c222ed4045747d3ad92747c70ee5ace.WireTo(id_996b6e444d6d497d8b5bcf19e61af937, "children");
            id_996b6e444d6d497d8b5bcf19e61af937.WireTo(id_09aeec84d6d147dbbb29d0706a8090da, "clickedEvent");
            id_09aeec84d6d147dbbb29d0706a8090da.WireTo(id_b0179b13b0404f69b62dffd02a433deb, "children");
            id_b0179b13b0404f69b62dffd02a433deb.WireTo(id_3bcc853c43ed4c11ab90007535f02e18, "uiLayout");
            id_a2da991ade264f7880273aaf1cc0f6af.WireTo(id_520a0086daec437885e0bbed04b3c3d4, "fanoutList");
            id_a2da991ade264f7880273aaf1cc0f6af.WireTo(projectFolderWatcher, "fanoutList");
            projectFolderWatcher.WireTo(id_20380f2ee67744a28adfcb9a1567d324, "changedFile");
            id_20380f2ee67744a28adfcb9a1567d324.WireTo(id_9e5d732baf614f039aa54a44e3cab8e3, "output");
            id_9e5d732baf614f039aa54a44e3cab8e3.WireTo(id_7dbd843acae54d92b6ee11e4402f7c58, "eventOutput");
            id_4f7f3189d95e49d5b7a7efd449c06bcb.WireTo(id_67afe6acdef745a2b4332ee067ae0d64, "argsOutput");
            id_e5c3464efb7642bb9fabd1516cc929de.WireTo(id_89a41c44a04f4d0c90767d54d336b244, "argsOutput");
            id_e40ec558a4b54983bc3c6fcfc4ea0f9d.WireTo(id_d48f32df4e1549b7ae40945b4c14d2aa, "transitionOutput");
            id_d48f32df4e1549b7ae40945b4c14d2aa.WireTo(id_19a82dabda6a4496876aa78d3eba5819, "output");
            id_19a82dabda6a4496876aa78d3eba5819.WireTo(layoutDiagram, "ifOutput");
            id_cc600bbebaae4fd3bb1d399c688de7e3.WireTo(id_bf41498cf7844e81a60e5bcd25e7934a, "fanoutList");
            id_cc600bbebaae4fd3bb1d399c688de7e3.WireTo(id_e40ec558a4b54983bc3c6fcfc4ea0f9d, "fanoutList");
            id_05850b398be04478b0058b1724e4075e.WireTo(id_c0d6ac07609646dd9504269f8ec9721b, "eventHappened");
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


























































































