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
using System.Windows.Input;
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
            Vertical id_0d7eb18bc2fc46a9acc8d9052b9b0ad2 = new Vertical() {  };
            CanvasDisplay id_8376074377e74660851783f14addefdc = new CanvasDisplay() { Width = 1920, Height = 600, Background = Brushes.White, StateTransition = stateTransition };
            ApplyAction<System.Windows.Controls.Canvas> id_2ea55c5429104478b38dfa3fda14b86e = new ApplyAction<System.Windows.Controls.Canvas>() { Lambda = canvas => mainCanvas = canvas };
            KeyEvent id_28109be3d0d74940a67318187916543e = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.A }, Condition = args => mainGraph.Get("SelectedNode") != null };
            Data<object> id_3ac17524a5274cc3a0b9a90fd1865c8f = new Data<object>() { Lambda = () => {var node = new ALANode();node.Graph = mainGraph;node.Canvas = mainCanvas;node.StateTransition = stateTransition;mainGraph.AddNode(node);node.CreateInternals();mainCanvas.Children.Add(node.Render);return node;} };
            ApplyAction<object> initialiseNode = new ApplyAction<object>() { InstanceName = "initialiseNode", Lambda = input =>{var render = (input as ALANode).Render;var mousePos = Mouse.GetPosition(mainCanvas);WPFCanvas.SetLeft(render, mousePos.X);WPFCanvas.SetTop(render, mousePos.Y);mainGraph.Set("LatestNode", input);if (mainGraph.Get("SelectedNode") == null){mainGraph.Set("SelectedNode", input);mainGraph.Roots.Add(input);}} };
            ContextMenu id_34ccd1f4c4f14af9aa327ac9d0d8bd9d = new ContextMenu() {  };
            MenuItem id_f24e673bdbc2404f8a7462eb7c58e01d = new MenuItem(header: "Add root") {  };
            Data<object> id_4de437deb06d42738be809733aec8169 = new Data<object>() { Lambda = () => {var node = new ALANode();node.Graph = mainGraph;node.Canvas = mainCanvas;node.StateTransition = stateTransition;mainGraph.AddNode(node);node.CreateInternals();mainCanvas.Children.Add(node.Render);return node;} };
            EventConnector id_5a0dd0e6dd944d6f8eb83774fcab4a36 = new EventConnector() {  };
            Apply<object, object> createAndPaintALAWire = new Apply<object, object>() { InstanceName = "createAndPaintALAWire", Lambda = input =>{var source = mainGraph.Get("SelectedNode") as ALANode;var destination = input as ALANode;var sourcePort = source.GetSelectedPort(inputPort: false);var destinationPort = destination.GetSelectedPort(inputPort: true);var wire = new ALAWire(){Graph = mainGraph,Canvas = mainCanvas,Source = source,Destination = destination,SourcePort = sourcePort,DestinationPort = destinationPort};mainGraph.AddEdge(wire);source.PositionChanged += () => wire.Refresh();destination.PositionChanged += () => wire.Refresh();wire.Paint();return wire;} };
            UIFactory setUpGraph = new UIFactory(getUIContainer: () =>{/* This lambda executes during the UI setup call, which occurs before the app event flow.The reason for putting this lambda here is that thisensures that mainGraph is set up before being passedinto the scope of other delegates down the line (before the app event flow)*/mainGraph = new Graph();mainGraph.EdgeAdded += edge => {var wire = edge as ALAWire;var src = wire.Source as ALANode;src.Edges.Add(edge);var dest = wire.Destination as ALANode;dest.Edges.Add(edge);};mainGraph.EdgeDeleted += edge => {var wire = edge as ALAWire;var src = wire.Source as ALANode;src.Edges.Remove(edge);var dest = wire.Destination as ALANode;dest.Edges.Remove(edge);};/* Return a dummy invisible IUI */return new Text("", visible: false);}) { InstanceName = "setUpGraph" };
            // END AUTO-GENERATED INSTANTIATIONS FOR Application.xmind

            // BEGIN AUTO-GENERATED WIRING FOR Application.xmind
            mainWindow.WireTo(id_0d7eb18bc2fc46a9acc8d9052b9b0ad2, "iuiStructure");
            id_0d7eb18bc2fc46a9acc8d9052b9b0ad2.WireTo(setUpGraph, "children");
            id_0d7eb18bc2fc46a9acc8d9052b9b0ad2.WireTo(id_8376074377e74660851783f14addefdc, "children");
            id_8376074377e74660851783f14addefdc.WireTo(id_2ea55c5429104478b38dfa3fda14b86e, "canvasOutput");
            id_8376074377e74660851783f14addefdc.WireTo(id_28109be3d0d74940a67318187916543e, "eventHandlers");
            id_8376074377e74660851783f14addefdc.WireTo(id_34ccd1f4c4f14af9aa327ac9d0d8bd9d, "contextMenu");
            id_28109be3d0d74940a67318187916543e.WireTo(id_5a0dd0e6dd944d6f8eb83774fcab4a36, "eventHappened");
            id_3ac17524a5274cc3a0b9a90fd1865c8f.WireTo(initialiseNode, "dataOutput");
            id_34ccd1f4c4f14af9aa327ac9d0d8bd9d.WireTo(id_f24e673bdbc2404f8a7462eb7c58e01d, "children");
            id_f24e673bdbc2404f8a7462eb7c58e01d.WireTo(id_3ac17524a5274cc3a0b9a90fd1865c8f, "clickedEvent");
            id_4de437deb06d42738be809733aec8169.WireTo(createAndPaintALAWire, "dataOutput");
            id_5a0dd0e6dd944d6f8eb83774fcab4a36.WireTo(id_4de437deb06d42738be809733aec8169, "fanoutList");
            // END AUTO-GENERATED WIRING FOR Application.xmind

            // BEGIN MANUAL INSTANTIATIONS
            // END MANUAL INSTANTIATIONS

            // BEGIN MANUAL WIRING
            // END MANUAL WIRING

        }
    }
}




























































































































































































































