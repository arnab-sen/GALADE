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

            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR Application.xmind
            Vertical id_58d47835aa804f72b65834c3f2cf5941 = new Vertical() {  };
            CanvasDisplay id_a0c000fa91fe442aaaa81edf29205c42 = new CanvasDisplay() { Width = 1920, Height = 600, Background = Brushes.White, StateTransition = stateTransition };
            ApplyAction<System.Windows.Controls.Canvas> id_0a036ddd76474b9a86c2b07055c6e230 = new ApplyAction<System.Windows.Controls.Canvas>() { Lambda = canvas => mainCanvas = canvas };
            KeyEvent id_92d19af29da649afa586f0e9996c9e6d = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.A }, Condition = args => mainGraph.Get("SelectedNode") != null&& stateTransition.CurrentStateMatches(Enums.DiagramMode.IdleSelected) };
            Data<object> id_196a977bbe1d4c3c89d1e93a7712a641 = new Data<object>() { Lambda = () => {var node = new ALANode();node.Graph = mainGraph;node.Canvas = mainCanvas;node.StateTransition = stateTransition;mainGraph.AddNode(node);node.CreateInternals();mainCanvas.Children.Add(node.Render);return node;} };
            ApplyAction<object> initialiseNode = new ApplyAction<object>() { InstanceName = "initialiseNode", Lambda = input =>{var render = (input as ALANode).Render;var mousePos = Mouse.GetPosition(mainCanvas);WPFCanvas.SetLeft(render, mousePos.X);WPFCanvas.SetTop(render, mousePos.Y);mainGraph.Set("LatestNode", input);if (mainGraph.Get("SelectedNode") == null){mainGraph.Set("SelectedNode", input);mainGraph.Roots.Add(input);}} };
            ContextMenu id_c7c3f07e6bba4f5a9320ea65cffcb083 = new ContextMenu() {  };
            MenuItem id_1684f51a9e3142318904afe44a7e6e9a = new MenuItem(header: "Add root") {  };
            Data<object> id_f3efdcf09a8442bb8164abb109a386a4 = new Data<object>() { Lambda = () => {var node = new ALANode();node.Graph = mainGraph;node.Canvas = mainCanvas;node.StateTransition = stateTransition;mainGraph.AddNode(node);node.CreateInternals();mainCanvas.Children.Add(node.Render);return node;} };
            EventConnector id_fb4c2692a9b24c9abeecbe03b40f9e6b = new EventConnector() {  };
            Apply<object, object> createAndPaintALAWire = new Apply<object, object>() { InstanceName = "createAndPaintALAWire", Lambda = input =>{var source = mainGraph.Get("SelectedNode") as ALANode;var destination = input as ALANode;var sourcePort = source.GetSelectedPort(inputPort: false);var destinationPort = destination.GetSelectedPort(inputPort: true);var wire = new ALAWire(){Graph = mainGraph,Canvas = mainCanvas,Source = source,Destination = destination,SourcePort = sourcePort,DestinationPort = destinationPort};mainGraph.AddEdge(wire);source.PositionChanged += () => wire.Refresh();destination.PositionChanged += () => wire.Refresh();wire.Paint();return wire;} };
            UIFactory setUpGraph = new UIFactory(getUIContainer: () =>{/* This lambda executes during the UI setup call, which occurs before the app event flow.The reason for putting this lambda here is that thisensures that mainGraph is set up before being passedinto the scope of other delegates down the line (before the app event flow)*/mainGraph = new Graph();mainGraph.EdgeAdded += edge => {var wire = edge as ALAWire;var src = wire.Source as ALANode;src.Edges.Add(edge);var dest = wire.Destination as ALANode;dest.Edges.Add(edge);};mainGraph.EdgeDeleted += edge => {var wire = edge as ALAWire;var src = wire.Source as ALANode;src.Edges.Remove(edge);var dest = wire.Destination as ALANode;dest.Edges.Remove(edge);};/* Return a dummy invisible IUI */return new Text("", visible: false);}) { InstanceName = "setUpGraph" };
            Data<ALANode> id_b899ded467fc4404a59ac992245ddd60 = new Data<ALANode>() { Lambda = () => mainGraph.Roots.First() as ALANode };
            RightTreeLayout<ALANode> id_1fa70ade349f40bc9b3f6f68e4832c3e = new RightTreeLayout<ALANode>() { GetID = n => n.Id, GetWidth = n => (n.Render as FrameworkElement).ActualWidth, GetHeight = n => (n.Render as FrameworkElement).ActualHeight, SetX = (n, x) => WPFCanvas.SetLeft(n.Render, x), SetY = (n, y) => WPFCanvas.SetTop(n.Render, y), GetChildren = n => mainGraph.Edges.Where(e => e is ALAWire wire && wire.Source == n).Select(e => ((e as ALAWire).Destination) as ALANode), HorizontalGap = 100, VerticalGap = 20, InitialX = 50, InitialY = 50 };
            EventConnector layoutDiagram = new EventConnector() { InstanceName = "layoutDiagram" };
            DataFlowConnector<ALANode> id_aaaa34e9e11548fe8ba394a0c5b9c43a = new DataFlowConnector<ALANode>() {  };
            ApplyAction<ALANode> id_02c44df8844d43e9b8b76d61e542b3b6 = new ApplyAction<ALANode>() { Lambda = node =>{Dispatcher.CurrentDispatcher.Invoke(() => {var edges = mainGraph.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);} };
            KeyEvent id_dc314dc6074343839a75e93957d05c3c = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.R }, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            // END AUTO-GENERATED INSTANTIATIONS FOR Application.xmind

            // BEGIN AUTO-GENERATED WIRING FOR Application.xmind
            mainWindow.WireTo(id_58d47835aa804f72b65834c3f2cf5941, "iuiStructure");
            id_58d47835aa804f72b65834c3f2cf5941.WireTo(setUpGraph, "children");
            id_58d47835aa804f72b65834c3f2cf5941.WireTo(id_a0c000fa91fe442aaaa81edf29205c42, "children");
            id_a0c000fa91fe442aaaa81edf29205c42.WireTo(id_0a036ddd76474b9a86c2b07055c6e230, "canvasOutput");
            id_a0c000fa91fe442aaaa81edf29205c42.WireTo(id_92d19af29da649afa586f0e9996c9e6d, "eventHandlers");
            id_a0c000fa91fe442aaaa81edf29205c42.WireTo(id_dc314dc6074343839a75e93957d05c3c, "eventHandlers");
            id_a0c000fa91fe442aaaa81edf29205c42.WireTo(id_c7c3f07e6bba4f5a9320ea65cffcb083, "contextMenu");
            id_92d19af29da649afa586f0e9996c9e6d.WireTo(id_fb4c2692a9b24c9abeecbe03b40f9e6b, "eventHappened");
            id_196a977bbe1d4c3c89d1e93a7712a641.WireTo(initialiseNode, "dataOutput");
            id_c7c3f07e6bba4f5a9320ea65cffcb083.WireTo(id_1684f51a9e3142318904afe44a7e6e9a, "children");
            id_1684f51a9e3142318904afe44a7e6e9a.WireTo(id_196a977bbe1d4c3c89d1e93a7712a641, "clickedEvent");
            id_f3efdcf09a8442bb8164abb109a386a4.WireTo(createAndPaintALAWire, "dataOutput");
            id_fb4c2692a9b24c9abeecbe03b40f9e6b.WireTo(id_f3efdcf09a8442bb8164abb109a386a4, "fanoutList");
            id_fb4c2692a9b24c9abeecbe03b40f9e6b.WireTo(layoutDiagram, "complete");
            id_b899ded467fc4404a59ac992245ddd60.WireTo(id_aaaa34e9e11548fe8ba394a0c5b9c43a, "dataOutput");
            id_dc314dc6074343839a75e93957d05c3c.WireTo(layoutDiagram, "eventHappened");
            layoutDiagram.WireTo(id_b899ded467fc4404a59ac992245ddd60, "fanoutList");
            id_aaaa34e9e11548fe8ba394a0c5b9c43a.WireTo(id_1fa70ade349f40bc9b3f6f68e4832c3e, "fanoutList");
            id_aaaa34e9e11548fe8ba394a0c5b9c43a.WireTo(id_02c44df8844d43e9b8b76d61e542b3b6, "fanoutList");
            // END AUTO-GENERATED WIRING FOR Application.xmind

            // BEGIN MANUAL INSTANTIATIONS
            var AMM = new AbstractionModelManager();
            AMM.OpenFile();
            // END MANUAL INSTANTIATIONS

            // BEGIN MANUAL WIRING
            // END MANUAL WIRING

        }
    }
}






































































































































































































































































