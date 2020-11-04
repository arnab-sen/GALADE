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
            Vertical id_e5fefe7bc8a444b2b9adabbcc5d41291 = new Vertical() {  };
            CanvasDisplay id_9d592da50c5a4fa5bdcdb08b47f7809a = new CanvasDisplay() { Width = 1920, Height = 600, Background = Brushes.White, StateTransition = stateTransition };
            ApplyAction<System.Windows.Controls.Canvas> id_0ecc0e7b9fec45f19c95287796a4074d = new ApplyAction<System.Windows.Controls.Canvas>() { Lambda = canvas => mainCanvas = canvas };
            KeyEvent id_5576b5a7f925477c8674cd1a4057bf2a = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.A }, Condition = args => mainGraph.Get("SelectedNode") != null&& stateTransition.CurrentStateMatches(Enums.DiagramMode.IdleSelected) };
            Data<object> id_f066fb4914e3489598fcaeb18cc75098 = new Data<object>() { Lambda = () => {var node = new ALANode();node.Model = abstractionModelManager.GetAbstractionModel(abstractionModelManager.GetAbstractionTypes().FirstOrDefault());node.Graph = mainGraph;node.Canvas = mainCanvas;node.StateTransition = stateTransition;node.AvailableDomainAbstractions.AddRange( abstractionModelManager.GetAbstractionTypes());node.TypeChanged += newType => {abstractionModelManager.UpdateAbstractionModel(abstractionModelManager.GetAbstractionModel(newType),node.Model);node.UpdateUI();Dispatcher.CurrentDispatcher.Invoke(() => {var edges = node.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);};mainGraph.AddNode(node);node.CreateInternals();mainCanvas.Children.Add(node.Render);return node;} };
            ApplyAction<object> initialiseNode = new ApplyAction<object>() { InstanceName = "initialiseNode", Lambda = input =>{var render = (input as ALANode).Render;var mousePos = Mouse.GetPosition(mainCanvas);WPFCanvas.SetLeft(render, mousePos.X);WPFCanvas.SetTop(render, mousePos.Y);mainGraph.Set("LatestNode", input);if (mainGraph.Get("SelectedNode") == null){mainGraph.Set("SelectedNode", input);mainGraph.Roots.Add(input);}} };
            ContextMenu id_570d8e5c11ea4ead875674b9f3336e1f = new ContextMenu() {  };
            MenuItem id_f575dc072678429c9c384bdd95998f9b = new MenuItem(header: "Add root") {  };
            EventConnector id_391f7928f2fa4da283d18b46450af68b = new EventConnector() {  };
            Apply<object, object> createAndPaintALAWire = new Apply<object, object>() { InstanceName = "createAndPaintALAWire", Lambda = input =>{var source = mainGraph.Get("SelectedNode") as ALANode;var destination = input as ALANode;var sourcePort = source.GetSelectedPort(inputPort: false);var destinationPort = destination.GetSelectedPort(inputPort: true);var wire = new ALAWire(){Graph = mainGraph,Canvas = mainCanvas,Source = source,Destination = destination,SourcePort = sourcePort,DestinationPort = destinationPort};mainGraph.AddEdge(wire);source.PositionChanged += () => wire.Refresh();destination.PositionChanged += () => wire.Refresh();wire.Paint();return wire;} };
            UIFactory setUpGraph = new UIFactory(getUIContainer: () =>{/* This lambda executes during the UI setup call, which occurs before the app event flow.The reason for putting this lambda here is that thisensures that mainGraph is set up before being passedinto the scope of other delegates down the line (before the app event flow)*/mainGraph = new Graph();mainGraph.EdgeAdded += edge => {var wire = edge as ALAWire;var src = wire.Source as ALANode;src.Edges.Add(edge);var dest = wire.Destination as ALANode;dest.Edges.Add(edge);};mainGraph.EdgeDeleted += edge => {var wire = edge as ALAWire;var src = wire.Source as ALANode;src.Edges.Remove(edge);var dest = wire.Destination as ALANode;dest.Edges.Remove(edge);};/* Return a dummy invisible IUI */return new Text("", visible: false);}) { InstanceName = "setUpGraph" };
            Data<ALANode> id_444e2026d8a6432195984548e143b0de = new Data<ALANode>() { Lambda = () => mainGraph.Roots.First() as ALANode };
            RightTreeLayout<ALANode> id_c051baa946f046c5bb253702167ffb2c = new RightTreeLayout<ALANode>() { GetID = n => n.Id, GetWidth = n => (n.Render as FrameworkElement).ActualWidth, GetHeight = n => (n.Render as FrameworkElement).ActualHeight, SetX = (n, x) => WPFCanvas.SetLeft(n.Render, x), SetY = (n, y) => WPFCanvas.SetTop(n.Render, y), GetChildren = n => mainGraph.Edges.Where(e => e is ALAWire wire && wire.Source == n).Select(e => ((e as ALAWire).Destination) as ALANode), HorizontalGap = 100, VerticalGap = 20, InitialX = 50, InitialY = 50 };
            EventConnector layoutDiagram = new EventConnector() { InstanceName = "layoutDiagram" };
            DataFlowConnector<ALANode> id_fcf0405f4ad749918fe57c071933192e = new DataFlowConnector<ALANode>() {  };
            ApplyAction<ALANode> id_dfe6763e4f26438aabb920fe2c67fd3f = new ApplyAction<ALANode>() { Lambda = node =>{Dispatcher.CurrentDispatcher.Invoke(() => {var edges = mainGraph.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);} };
            KeyEvent id_2d39d2452e3b4f99a1d5b7f88b75af93 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.R }, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            Apply<AbstractionModel, object> id_4e3fab65ad804a6e912f986962f6b0ff = new Apply<AbstractionModel, object>() { Lambda = input => {var node = new ALANode();node.Model = input;node.Graph = mainGraph;node.Canvas = mainCanvas;node.StateTransition = stateTransition;node.AvailableDomainAbstractions.AddRange( abstractionModelManager.GetAbstractionTypes());node.TypeChanged += newType => {abstractionModelManager.UpdateAbstractionModel(abstractionModelManager.GetAbstractionModel(newType),node.Model);node.UpdateUI();Dispatcher.CurrentDispatcher.Invoke(() => {var edges = node.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);};mainGraph.AddNode(node);node.CreateInternals();mainCanvas.Children.Add(node.Render);return node;} };
            MenuBar id_883773815f7042bda7be8ab58869baed = new MenuBar() {  };
            MenuItem id_495406eea6e74cfdb0abb7d130c4187e = new MenuItem(header: "File") {  };
            MenuItem id_c473a6fc883f4ac6a333ea0d7ee370b6 = new MenuItem(header: "Open Project") {  };
            FolderBrowser id_47a6b84fe07b4c7eb5aa48dcf120b0fd = new FolderBrowser() { Description = "" };
            DirectorySearch id_6e956301e1d545b39da87cefd4e0070d = new DirectorySearch(directoriesToFind: new string[] { "DomainAbstractions" }) { FilenameFilter = "*.cs" };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_88c0cb3840eb40959597060da5c387c0 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("DomainAbstractions")){list = input["DomainAbstractions"];}return list;} };
            ForEach<string> id_eef4daad84484c61ae41590a6ba437b9 = new ForEach<string>() {  };
            ApplyAction<string> id_93b9f61cd2dc4f8293424d081e8c6db4 = new ApplyAction<string>() { Lambda = input =>{abstractionModelManager.CreateAbstractionModelFromPath(input);} };
            Data<string> id_3a32e33964294e6f8ff6e65163e8b8dd = new Data<string>() { storedData = "Apply<T1, T2>" };
            Apply<string, AbstractionModel> id_859f25e7fab44d3eb3fa218f335a479f = new Apply<string, AbstractionModel>() { Lambda = input =>{return abstractionModelManager.GetAbstractionModel(input);} };
            Data<string> id_37f4bd8a31634c1eb9c3d65e8aed10c1 = new Data<string>() { storedData = @"D:\Coding\C#\Projects\GALADE\ALACore" };
            DropDownMenu id_1a8ed61b43fe4f9283b1f4d80ce4cf8c = new DropDownMenu() { Items = new string[100] };
            KeyEvent id_1e122d468c4c46f6a51c1ccd8342b13b = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.Enter } };
            ApplyAction<object> id_ab1fe89cf03245909771699a05d7faaf = new ApplyAction<object>() { Lambda = input =>{(input as WPFCanvas).Focus();} };
            MenuItem id_170c8116118a43c59551456da3e5edcb = new MenuItem(header: "Debug") {  };
            MenuItem id_ecf965328d774f2c94b70583ce4cf053 = new MenuItem(header: "TextEditor test") {  };
            PopupWindow id_cf25298cc6294110aeb0715ddaabdb30 = new PopupWindow(title: "") { Height = 720, Width = 1280, Resize = SizeToContent.WidthAndHeight };
            Box id_7b34c588378841c48db0a5a833650a6b = new Box() { Width = 100, Height = 100 };
            TextEditor id_637b610231c04e51bb0d6b718d65ab97 = new TextEditor() { Width = 1280, Height = 720 };
            DataFlowConnector<string> id_bbf0ad94024446d994602ccceb91d29c = new DataFlowConnector<string>() {  };
            FolderWatcher projectFolderWatcher = new FolderWatcher() { InstanceName = "projectFolderWatcher", RootPath = "", Filter = "*.cs", WatchSubdirectories = true, PathRegex = @".*\.cs$" };
            ConvertToEvent<string> id_fea3bc85154e471197761dacc55286ca = new ConvertToEvent<string>() {  };
            Data<List<string>> id_5034c453f6f741e8bc846e29d471653b = new Data<List<string>>() { Lambda = () => {var path = projectFolderWatcher.RootPath;return default;} };
            // END AUTO-GENERATED INSTANTIATIONS FOR Application.xmind

            // BEGIN AUTO-GENERATED WIRING FOR Application.xmind
            mainWindow.WireTo(id_e5fefe7bc8a444b2b9adabbcc5d41291, "iuiStructure");
            mainWindow.WireTo(id_37f4bd8a31634c1eb9c3d65e8aed10c1, "appStart");
            id_e5fefe7bc8a444b2b9adabbcc5d41291.WireTo(setUpGraph, "children");
            id_e5fefe7bc8a444b2b9adabbcc5d41291.WireTo(id_883773815f7042bda7be8ab58869baed, "children");
            id_e5fefe7bc8a444b2b9adabbcc5d41291.WireTo(id_9d592da50c5a4fa5bdcdb08b47f7809a, "children");
            id_9d592da50c5a4fa5bdcdb08b47f7809a.WireTo(id_0ecc0e7b9fec45f19c95287796a4074d, "canvasOutput");
            id_9d592da50c5a4fa5bdcdb08b47f7809a.WireTo(id_5576b5a7f925477c8674cd1a4057bf2a, "eventHandlers");
            id_9d592da50c5a4fa5bdcdb08b47f7809a.WireTo(id_2d39d2452e3b4f99a1d5b7f88b75af93, "eventHandlers");
            id_9d592da50c5a4fa5bdcdb08b47f7809a.WireTo(id_1e122d468c4c46f6a51c1ccd8342b13b, "eventHandlers");
            id_9d592da50c5a4fa5bdcdb08b47f7809a.WireTo(id_570d8e5c11ea4ead875674b9f3336e1f, "contextMenu");
            id_5576b5a7f925477c8674cd1a4057bf2a.WireTo(id_391f7928f2fa4da283d18b46450af68b, "eventHappened");
            id_f066fb4914e3489598fcaeb18cc75098.WireTo(initialiseNode, "dataOutput");
            id_570d8e5c11ea4ead875674b9f3336e1f.WireTo(id_f575dc072678429c9c384bdd95998f9b, "children");
            id_f575dc072678429c9c384bdd95998f9b.WireTo(id_f066fb4914e3489598fcaeb18cc75098, "clickedEvent");
            id_391f7928f2fa4da283d18b46450af68b.WireTo(id_3a32e33964294e6f8ff6e65163e8b8dd, "fanoutList");
            id_391f7928f2fa4da283d18b46450af68b.WireTo(layoutDiagram, "complete");
            id_444e2026d8a6432195984548e143b0de.WireTo(id_fcf0405f4ad749918fe57c071933192e, "dataOutput");
            layoutDiagram.WireTo(id_444e2026d8a6432195984548e143b0de, "fanoutList");
            id_fcf0405f4ad749918fe57c071933192e.WireTo(id_c051baa946f046c5bb253702167ffb2c, "fanoutList");
            id_fcf0405f4ad749918fe57c071933192e.WireTo(id_dfe6763e4f26438aabb920fe2c67fd3f, "fanoutList");
            id_2d39d2452e3b4f99a1d5b7f88b75af93.WireTo(layoutDiagram, "eventHappened");
            id_4e3fab65ad804a6e912f986962f6b0ff.WireTo(createAndPaintALAWire, "output");
            id_883773815f7042bda7be8ab58869baed.WireTo(id_495406eea6e74cfdb0abb7d130c4187e, "children");
            id_883773815f7042bda7be8ab58869baed.WireTo(id_170c8116118a43c59551456da3e5edcb, "children");
            id_495406eea6e74cfdb0abb7d130c4187e.WireTo(id_c473a6fc883f4ac6a333ea0d7ee370b6, "children");
            id_495406eea6e74cfdb0abb7d130c4187e.WireTo(id_1a8ed61b43fe4f9283b1f4d80ce4cf8c, "children");
            id_c473a6fc883f4ac6a333ea0d7ee370b6.WireTo(id_47a6b84fe07b4c7eb5aa48dcf120b0fd, "clickedEvent");
            id_47a6b84fe07b4c7eb5aa48dcf120b0fd.WireTo(id_bbf0ad94024446d994602ccceb91d29c, "selectedFolderPathOutput");
            id_bbf0ad94024446d994602ccceb91d29c.WireTo(id_6e956301e1d545b39da87cefd4e0070d, "fanoutList");
            id_6e956301e1d545b39da87cefd4e0070d.WireTo(id_88c0cb3840eb40959597060da5c387c0, "foundFiles");
            id_88c0cb3840eb40959597060da5c387c0.WireTo(id_eef4daad84484c61ae41590a6ba437b9, "output");
            id_eef4daad84484c61ae41590a6ba437b9.WireTo(id_93b9f61cd2dc4f8293424d081e8c6db4, "elementOutput");
            id_3a32e33964294e6f8ff6e65163e8b8dd.WireTo(id_859f25e7fab44d3eb3fa218f335a479f, "dataOutput");
            id_859f25e7fab44d3eb3fa218f335a479f.WireTo(id_4e3fab65ad804a6e912f986962f6b0ff, "output");
            id_37f4bd8a31634c1eb9c3d65e8aed10c1.WireTo(id_bbf0ad94024446d994602ccceb91d29c, "dataOutput");
            id_1e122d468c4c46f6a51c1ccd8342b13b.WireTo(id_ab1fe89cf03245909771699a05d7faaf, "senderOutput");
            id_170c8116118a43c59551456da3e5edcb.WireTo(id_ecf965328d774f2c94b70583ce4cf053, "children");
            id_ecf965328d774f2c94b70583ce4cf053.WireTo(id_cf25298cc6294110aeb0715ddaabdb30, "clickedEvent");
            id_cf25298cc6294110aeb0715ddaabdb30.WireTo(id_7b34c588378841c48db0a5a833650a6b, "children");
            id_7b34c588378841c48db0a5a833650a6b.WireTo(id_637b610231c04e51bb0d6b718d65ab97, "uiLayout");
            id_bbf0ad94024446d994602ccceb91d29c.WireTo(projectFolderWatcher, "fanoutList");
            projectFolderWatcher.WireTo(id_fea3bc85154e471197761dacc55286ca, "changedFile");
            id_fea3bc85154e471197761dacc55286ca.WireTo(id_5034c453f6f741e8bc846e29d471653b, "eventOutput");
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
