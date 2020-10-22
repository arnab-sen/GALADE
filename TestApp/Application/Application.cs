using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;
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

        [STAThread]
        public static void Main()
        {
            Application app = new Application();
            app.Initialize().mainWindow.Run();
        }

        private Application()
        {
            object node = new VisualPortGraphNode() { Type = "TestType", Name = "TestName" };
            var id = node.GetHashCode();

            List<object> objs = new List<object>() {true, "str"};
            var a = objs.FirstOrDefault(obj => obj is bool b && b == true);
            var c = objs.FirstOrDefault(obj => obj is string s && s == "str");

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
            VisualPortGraph mainGraph = new VisualPortGraph() { InstanceName = "MainGraph", DebugOutputAll = false };
            StateTransition<Enums.DiagramMode> stateTransition = new StateTransition<Enums.DiagramMode>(Enums.DiagramMode.Idle)
            {
                InstanceName = "stateTransition",
                Matches = (flag, currentState) => (flag & currentState) != 0
            };

            UndoHistory undoHistory = new UndoHistory() { InstanceName = "graphHistory" };
            mainGraph.ActionPerformed += source => undoHistory.Push(source);

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
                var diagramContents = mainGraph.Serialise();
                File.WriteAllText(System.IO.Path.Combine(BACKUPS_DIRECTORY, $"{Utilities.GetCurrentTime()}.ala"), diagramContents);
            };
            #endregion

            WPFCanvas mainCanvas = null;

            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR Application.xmind
            Vertical id_a82e08b4b54e442c875a592c7144e557 = new Vertical() {  };
            CanvasDisplay id_fa98abe4716244d287a155872f500ffd = new CanvasDisplay() { Width = 1920, Height = 600, Background = Brushes.White, StateTransition = stateTransition };
            ApplyAction<System.Windows.Controls.Canvas> id_6fbc73910d274b879ac09b04ae3db95f = new ApplyAction<System.Windows.Controls.Canvas>() { Lambda = canvas => mainCanvas = canvas };
            KeyEvent id_8d6c977fce064800ba9376a786be2151 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.A } };
            Data<object> id_8975dd31a6ab40e1a10b6a324551b7b6 = new Data<object>() { Lambda = () => new VisualNode() };
            DynamicWiring<object> id_85b693e727094176b5df63b206f55259 = new DynamicWiring<object>(type: "UI", sourcePort: "uiLayout") {  };
            Box id_ea37425e89764c9fac7551cf4767e2d7 = new Box() { Width = 200, Height = 50 };
            ApplyAction<object> id_a92e990057ba41a4bf1f1863ec059def = new ApplyAction<object>() { Lambda = input =>{(input as VisualNode).InitialiseUI();var render = (input as VisualNode).Render;mainCanvas.Children.Add(render);WPFCanvas.SetLeft(render, 20);WPFCanvas.SetTop(render, 20);} };
            // END AUTO-GENERATED INSTANTIATIONS FOR Application.xmind

            // BEGIN AUTO-GENERATED WIRING FOR Application.xmind
            mainWindow.WireTo(id_a82e08b4b54e442c875a592c7144e557, "iuiStructure");
            id_a82e08b4b54e442c875a592c7144e557.WireTo(id_fa98abe4716244d287a155872f500ffd, "children");
            id_fa98abe4716244d287a155872f500ffd.WireTo(id_6fbc73910d274b879ac09b04ae3db95f, "canvasOutput");
            id_fa98abe4716244d287a155872f500ffd.WireTo(id_8d6c977fce064800ba9376a786be2151, "eventHandlers");
            id_8d6c977fce064800ba9376a786be2151.WireTo(id_8975dd31a6ab40e1a10b6a324551b7b6, "eventHappened");
            id_8975dd31a6ab40e1a10b6a324551b7b6.WireTo(id_85b693e727094176b5df63b206f55259, "dataOutput");
            id_85b693e727094176b5df63b206f55259.WireTo(id_a92e990057ba41a4bf1f1863ec059def, "objectOutput");
            id_85b693e727094176b5df63b206f55259.WireTo(id_ea37425e89764c9fac7551cf4767e2d7, "wireUi");
            // END AUTO-GENERATED WIRING FOR Application.xmind

            // BEGIN MANUAL INSTANTIATIONS
            // END MANUAL INSTANTIATIONS

            // BEGIN MANUAL WIRING
            // END MANUAL WIRING

        }
    }
}
















































































