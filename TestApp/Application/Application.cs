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
            Vertical id_8939e832f3b14830a1b27fadb0a79daa = new Vertical() {  };
            CanvasDisplay id_83f47e04ae0c4a11bbade7875955e40c = new CanvasDisplay() { Width = 1920, Height = 600, Background = Brushes.White, StateTransition = stateTransition };
            ApplyAction<System.Windows.Controls.Canvas> id_31ed703723ce4f1088eb0980f773acff = new ApplyAction<System.Windows.Controls.Canvas>() { Lambda = canvas => mainCanvas = canvas };
            KeyEvent id_9691632fbef243b09e9d3beed5fc7745 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.A }, Condition = args => mainGraph.Get("SelectedNode") != null };
            Data<object> id_7caf8f00cb44435f8d2a8418fccd8df3 = new Data<object>() { Lambda = () => {var node = new ALANode();node.Graph = mainGraph;node.Canvas = mainCanvas;node.StateTransition = stateTransition;node.CreateInternals();return node;} };
            ApplyAction<object> initialiseNode = new ApplyAction<object>() { InstanceName = "initialiseNode", Lambda = input =>{var render = (input as ALANode).Render;mainCanvas.Children.Add(render);var mousePos = Mouse.GetPosition(mainCanvas);WPFCanvas.SetLeft(render, mousePos.X);WPFCanvas.SetTop(render, mousePos.Y);mainGraph.Set("LatestNode", input);if (mainGraph.Get("SelectedNode") == null){mainGraph.Set("SelectedNode", input);mainGraph.Roots.Add(input);}} };
            ContextMenu id_99892a3bb1954e84973ac2bba494347a = new ContextMenu() {  };
            MenuItem id_d0a3c8af78ff406b98741b0e8dfde13a = new MenuItem(header: "Add root") {  };
            Data<object> id_96a652247a1443b9a5e10c6cf0ad51f6 = new Data<object>() { Lambda = () => {var node = new ALANode();node.Graph = mainGraph;node.Canvas = mainCanvas;node.StateTransition = stateTransition;node.CreateInternals();mainCanvas.Children.Add(node.Render);return node;} };
            EventConnector id_7bad36566d7147c4a170883622b970a3 = new EventConnector() {  };
            Apply<object, object> id_a6b73ae404d943ed8640df339cf2b9b0 = new Apply<object, object>() { Lambda = input =>{var source = mainGraph.Get("SelectedNode") as ALANode;var destination = input as ALANode;var sourcePort = source.GetSelectedPort(inputPort: false);var destinationPort = destination.GetSelectedPort(inputPort: true);var wire = new ALAWire(){Graph = mainGraph,Canvas = mainCanvas,SourcePort = sourcePort,DestinationPort = destinationPort};source.PositionChanged += () => wire.Refresh();destination.PositionChanged += () => wire.Refresh();wire.Paint();return wire;} };
            // END AUTO-GENERATED INSTANTIATIONS FOR Application.xmind

            // BEGIN AUTO-GENERATED WIRING FOR Application.xmind
            mainWindow.WireTo(id_8939e832f3b14830a1b27fadb0a79daa, "iuiStructure");
            id_8939e832f3b14830a1b27fadb0a79daa.WireTo(id_83f47e04ae0c4a11bbade7875955e40c, "children");
            id_83f47e04ae0c4a11bbade7875955e40c.WireTo(id_31ed703723ce4f1088eb0980f773acff, "canvasOutput");
            id_83f47e04ae0c4a11bbade7875955e40c.WireTo(id_9691632fbef243b09e9d3beed5fc7745, "eventHandlers");
            id_83f47e04ae0c4a11bbade7875955e40c.WireTo(id_99892a3bb1954e84973ac2bba494347a, "contextMenu");
            id_9691632fbef243b09e9d3beed5fc7745.WireTo(id_7bad36566d7147c4a170883622b970a3, "eventHappened");
            id_7caf8f00cb44435f8d2a8418fccd8df3.WireTo(initialiseNode, "dataOutput");
            id_99892a3bb1954e84973ac2bba494347a.WireTo(id_d0a3c8af78ff406b98741b0e8dfde13a, "children");
            id_d0a3c8af78ff406b98741b0e8dfde13a.WireTo(id_7caf8f00cb44435f8d2a8418fccd8df3, "clickedEvent");
            id_96a652247a1443b9a5e10c6cf0ad51f6.WireTo(id_a6b73ae404d943ed8640df339cf2b9b0, "dataOutput");
            id_7bad36566d7147c4a170883622b970a3.WireTo(id_96a652247a1443b9a5e10c6cf0ad51f6, "fanoutList");
            // END AUTO-GENERATED WIRING FOR Application.xmind

            // BEGIN MANUAL INSTANTIATIONS
            // END MANUAL INSTANTIATIONS

            // BEGIN MANUAL WIRING
            // END MANUAL WIRING

        }
    }
}
















































































































































