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
            Vertical id_e231535a158a41bf883566c7fb90067f = new Vertical() {  };
            CanvasDisplay id_af59f5e023724a249779314ea5335b6a = new CanvasDisplay() { Width = 1920, Height = 600, Background = Brushes.White, StateTransition = stateTransition };
            ApplyAction<System.Windows.Controls.Canvas> id_9c7e3d90eb6c4f67aadc41e0d2ef4197 = new ApplyAction<System.Windows.Controls.Canvas>() { Lambda = canvas => mainCanvas = canvas };
            KeyEvent id_233a4b035e0f47f4a4eb688f5d118acd = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.A }, Condition = args => mainGraph.Get("SelectedNode") != null&& stateTransition.CurrentStateMatches(Enums.DiagramMode.IdleSelected) };
            Data<object> id_9934608829a049aebef6fba88b0d9363 = new Data<object>() { Lambda = () => {var node = new ALANode();node.Graph = mainGraph;node.Canvas = mainCanvas;node.StateTransition = stateTransition;mainGraph.AddNode(node);node.CreateInternals();mainCanvas.Children.Add(node.Render);return node;} };
            ApplyAction<object> initialiseNode = new ApplyAction<object>() { InstanceName = "initialiseNode", Lambda = input =>{var render = (input as ALANode).Render;var mousePos = Mouse.GetPosition(mainCanvas);WPFCanvas.SetLeft(render, mousePos.X);WPFCanvas.SetTop(render, mousePos.Y);mainGraph.Set("LatestNode", input);if (mainGraph.Get("SelectedNode") == null){mainGraph.Set("SelectedNode", input);mainGraph.Roots.Add(input);}} };
            ContextMenu id_644990acc9d841d482d3ec40a67c0696 = new ContextMenu() {  };
            MenuItem id_5a2f90317ced4ca9963b1fb8355f7eaa = new MenuItem(header: "Add root") {  };
            EventConnector id_2abda330913b432aa6f4c707bdecd5fb = new EventConnector() {  };
            Apply<object, object> createAndPaintALAWire = new Apply<object, object>() { InstanceName = "createAndPaintALAWire", Lambda = input =>{var source = mainGraph.Get("SelectedNode") as ALANode;var destination = input as ALANode;var sourcePort = source.GetSelectedPort(inputPort: false);var destinationPort = destination.GetSelectedPort(inputPort: true);var wire = new ALAWire(){Graph = mainGraph,Canvas = mainCanvas,Source = source,Destination = destination,SourcePort = sourcePort,DestinationPort = destinationPort};mainGraph.AddEdge(wire);source.PositionChanged += () => wire.Refresh();destination.PositionChanged += () => wire.Refresh();wire.Paint();return wire;} };
            UIFactory setUpGraph = new UIFactory(getUIContainer: () =>{/* This lambda executes during the UI setup call, which occurs before the app event flow.The reason for putting this lambda here is that thisensures that mainGraph is set up before being passedinto the scope of other delegates down the line (before the app event flow)*/mainGraph = new Graph();mainGraph.EdgeAdded += edge => {var wire = edge as ALAWire;var src = wire.Source as ALANode;src.Edges.Add(edge);var dest = wire.Destination as ALANode;dest.Edges.Add(edge);};mainGraph.EdgeDeleted += edge => {var wire = edge as ALAWire;var src = wire.Source as ALANode;src.Edges.Remove(edge);var dest = wire.Destination as ALANode;dest.Edges.Remove(edge);};/* Return a dummy invisible IUI */return new Text("", visible: false);}) { InstanceName = "setUpGraph" };
            Data<ALANode> id_ea27a0e2d48b4870841e9d04a81490a2 = new Data<ALANode>() { Lambda = () => mainGraph.Roots.First() as ALANode };
            RightTreeLayout<ALANode> id_03aedd0e50f148f4bdb901929bc1ae66 = new RightTreeLayout<ALANode>() { GetID = n => n.Id, GetWidth = n => (n.Render as FrameworkElement).ActualWidth, GetHeight = n => (n.Render as FrameworkElement).ActualHeight, SetX = (n, x) => WPFCanvas.SetLeft(n.Render, x), SetY = (n, y) => WPFCanvas.SetTop(n.Render, y), GetChildren = n => mainGraph.Edges.Where(e => e is ALAWire wire && wire.Source == n).Select(e => ((e as ALAWire).Destination) as ALANode), HorizontalGap = 100, VerticalGap = 20, InitialX = 50, InitialY = 50 };
            EventConnector layoutDiagram = new EventConnector() { InstanceName = "layoutDiagram" };
            DataFlowConnector<ALANode> id_f8ca8a9169ae47bca361dae84c3b57ed = new DataFlowConnector<ALANode>() {  };
            ApplyAction<ALANode> id_75c407aa6c5642e29e730d2d55c75fd5 = new ApplyAction<ALANode>() { Lambda = node =>{Dispatcher.CurrentDispatcher.Invoke(() => {var edges = mainGraph.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);} };
            KeyEvent id_1880adec211d481c81f1f6bf9d68bb44 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.R }, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            Apply<AbstractionModel, object> id_2b7993c7c45b463cb6e6bb67d9da4859 = new Apply<AbstractionModel, object>() { Lambda = input => {var node = new ALANode();node.Model = input;node.Graph = mainGraph;node.Canvas = mainCanvas;node.StateTransition = stateTransition;node.AvailableDomainAbstractions.AddRange( abstractionModelManager.GetAbstractionTypes());mainGraph.AddNode(node);node.CreateInternals();mainCanvas.Children.Add(node.Render);return node;} };
            MenuBar id_fc4988b6201345f3b672a5d09d799a46 = new MenuBar() {  };
            MenuItem id_1e72afed9a9243a986e2537e69645d8f = new MenuItem(header: "File") {  };
            MenuItem id_b9c48dd303ab43709d7ad870490ae587 = new MenuItem(header: "Open Project") {  };
            FolderBrowser id_aa32d532df564af79473ecbab4babfbe = new FolderBrowser() { Description = "" };
            DirectorySearch id_7ed9e624f7074dfca8249bebace7945b = new DirectorySearch(directoriesToFind: new string[] { "DomainAbstractions" }) { FilenameFilter = "*.cs" };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_c24618240076440b9fd8e242ff63d492 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("DomainAbstractions")){list = input["DomainAbstractions"];}return list;} };
            ForEach<string> id_3ea196e603934fd0995b8a05b2240e9b = new ForEach<string>() {  };
            FileReader id_9e540f6fb2d14f798ebe82a810b7e841 = new FileReader() {  };
            ApplyAction<string> id_09d933f3cdb04ba8823a74ae06256d01 = new ApplyAction<string>() { Lambda = input =>{abstractionModelManager.CreateAbstractionModel(input);} };
            Data<string> id_b6f624d01b74402db0d0cf8daafe0eaf = new Data<string>() { storedData = "Apply<T1, T2>" };
            Apply<string, AbstractionModel> id_921f6ad099bf4800ad55ed55f4b7dd0e = new Apply<string, AbstractionModel>() { Lambda = input =>{return abstractionModelManager.GetAbstractionModel(input);} };
            Data<string> id_af65024e6f414a59967f286a1997494f = new Data<string>() { storedData = @"D:\Coding\C#\Projects\GALADE\ALACore" };
            DropDownMenu id_97465d1ac2514c4ca11cf024cf65c705 = new DropDownMenu() { Items = new string[100] };
            // END AUTO-GENERATED INSTANTIATIONS FOR Application.xmind

            // BEGIN AUTO-GENERATED WIRING FOR Application.xmind
            mainWindow.WireTo(id_e231535a158a41bf883566c7fb90067f, "iuiStructure");
            mainWindow.WireTo(id_af65024e6f414a59967f286a1997494f, "appStart");
            id_e231535a158a41bf883566c7fb90067f.WireTo(setUpGraph, "children");
            id_e231535a158a41bf883566c7fb90067f.WireTo(id_fc4988b6201345f3b672a5d09d799a46, "children");
            id_e231535a158a41bf883566c7fb90067f.WireTo(id_af59f5e023724a249779314ea5335b6a, "children");
            id_af59f5e023724a249779314ea5335b6a.WireTo(id_9c7e3d90eb6c4f67aadc41e0d2ef4197, "canvasOutput");
            id_af59f5e023724a249779314ea5335b6a.WireTo(id_233a4b035e0f47f4a4eb688f5d118acd, "eventHandlers");
            id_af59f5e023724a249779314ea5335b6a.WireTo(id_1880adec211d481c81f1f6bf9d68bb44, "eventHandlers");
            id_af59f5e023724a249779314ea5335b6a.WireTo(id_644990acc9d841d482d3ec40a67c0696, "contextMenu");
            id_233a4b035e0f47f4a4eb688f5d118acd.WireTo(id_2abda330913b432aa6f4c707bdecd5fb, "eventHappened");
            id_9934608829a049aebef6fba88b0d9363.WireTo(initialiseNode, "dataOutput");
            id_644990acc9d841d482d3ec40a67c0696.WireTo(id_5a2f90317ced4ca9963b1fb8355f7eaa, "children");
            id_5a2f90317ced4ca9963b1fb8355f7eaa.WireTo(id_9934608829a049aebef6fba88b0d9363, "clickedEvent");
            id_2abda330913b432aa6f4c707bdecd5fb.WireTo(id_b6f624d01b74402db0d0cf8daafe0eaf, "fanoutList");
            id_2abda330913b432aa6f4c707bdecd5fb.WireTo(layoutDiagram, "complete");
            id_ea27a0e2d48b4870841e9d04a81490a2.WireTo(id_f8ca8a9169ae47bca361dae84c3b57ed, "dataOutput");
            layoutDiagram.WireTo(id_ea27a0e2d48b4870841e9d04a81490a2, "fanoutList");
            id_f8ca8a9169ae47bca361dae84c3b57ed.WireTo(id_03aedd0e50f148f4bdb901929bc1ae66, "fanoutList");
            id_f8ca8a9169ae47bca361dae84c3b57ed.WireTo(id_75c407aa6c5642e29e730d2d55c75fd5, "fanoutList");
            id_1880adec211d481c81f1f6bf9d68bb44.WireTo(layoutDiagram, "eventHappened");
            id_921f6ad099bf4800ad55ed55f4b7dd0e.WireTo(id_2b7993c7c45b463cb6e6bb67d9da4859, "output");
            id_2b7993c7c45b463cb6e6bb67d9da4859.WireTo(createAndPaintALAWire, "output");
            id_fc4988b6201345f3b672a5d09d799a46.WireTo(id_1e72afed9a9243a986e2537e69645d8f, "children");
            id_1e72afed9a9243a986e2537e69645d8f.WireTo(id_b9c48dd303ab43709d7ad870490ae587, "children");
            id_1e72afed9a9243a986e2537e69645d8f.WireTo(id_97465d1ac2514c4ca11cf024cf65c705, "children");
            id_b9c48dd303ab43709d7ad870490ae587.WireTo(id_aa32d532df564af79473ecbab4babfbe, "clickedEvent");
            id_aa32d532df564af79473ecbab4babfbe.WireTo(id_7ed9e624f7074dfca8249bebace7945b, "selectedFolderPathOutput");
            id_af65024e6f414a59967f286a1997494f.WireTo(id_7ed9e624f7074dfca8249bebace7945b, "dataOutput");
            id_7ed9e624f7074dfca8249bebace7945b.WireTo(id_c24618240076440b9fd8e242ff63d492, "foundFiles");
            id_c24618240076440b9fd8e242ff63d492.WireTo(id_3ea196e603934fd0995b8a05b2240e9b, "output");
            id_3ea196e603934fd0995b8a05b2240e9b.WireTo(id_9e540f6fb2d14f798ebe82a810b7e841, "elementOutput");
            id_9e540f6fb2d14f798ebe82a810b7e841.WireTo(id_09d933f3cdb04ba8823a74ae06256d01, "fileContentOutput");
            id_b6f624d01b74402db0d0cf8daafe0eaf.WireTo(id_921f6ad099bf4800ad55ed55f4b7dd0e, "dataOutput");
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




















































































































































































































































































































