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
            Vertical id_d037aa8c10a84b7aaad4c9f5d9493cc4 = new Vertical() {  };
            CanvasDisplay id_cef9ca58951448259dc885a01a22e286 = new CanvasDisplay() { Width = 1920, Height = 600, Background = Brushes.White, StateTransition = stateTransition };
            ApplyAction<System.Windows.Controls.Canvas> id_86ae5288a1dc4590a9f23c46fa9a5c5b = new ApplyAction<System.Windows.Controls.Canvas>() { Lambda = canvas => mainCanvas = canvas };
            KeyEvent id_5911b04a873646be9fd9b0a2e542eba0 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.A }, Condition = args => mainGraph.Get("SelectedNode") != null&& stateTransition.CurrentStateMatches(Enums.DiagramMode.IdleSelected) };
            Data<object> id_744c8ab13e224a929e20fc471858dee7 = new Data<object>() { Lambda = () => {var node = new ALANode();node.Model = abstractionModelManager.GetAbstractionModel(abstractionModelManager.GetAbstractionTypes().FirstOrDefault());node.Graph = mainGraph;node.Canvas = mainCanvas;node.StateTransition = stateTransition;node.AvailableDomainAbstractions.AddRange( abstractionModelManager.GetAbstractionTypes());node.TypeChanged += newType => {abstractionModelManager.UpdateAbstractionModel(abstractionModelManager.GetAbstractionModel(newType),node.Model);node.UpdateUI();Dispatcher.CurrentDispatcher.Invoke(() => {var edges = node.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);};mainGraph.AddNode(node);node.CreateInternals();mainCanvas.Children.Add(node.Render);return node;} };
            ApplyAction<object> initialiseNode = new ApplyAction<object>() { InstanceName = "initialiseNode", Lambda = input =>{var render = (input as ALANode).Render;var mousePos = Mouse.GetPosition(mainCanvas);WPFCanvas.SetLeft(render, mousePos.X);WPFCanvas.SetTop(render, mousePos.Y);mainGraph.Set("LatestNode", input);if (mainGraph.Get("SelectedNode") == null){mainGraph.Set("SelectedNode", input);mainGraph.Roots.Add(input);}} };
            ContextMenu id_fd5edff2f69240ee80da7b2dc94124e8 = new ContextMenu() {  };
            MenuItem id_44f6e9a052d64f68a879a9e2c89e4b17 = new MenuItem(header: "Add root") {  };
            EventConnector id_0226b5fc30a743819d3ca8e57165c00b = new EventConnector() {  };
            Apply<object, object> createAndPaintALAWire = new Apply<object, object>() { InstanceName = "createAndPaintALAWire", Lambda = input =>{var source = mainGraph.Get("SelectedNode") as ALANode;var destination = input as ALANode;var sourcePort = source.GetSelectedPort(inputPort: false);var destinationPort = destination.GetSelectedPort(inputPort: true);var wire = new ALAWire(){Graph = mainGraph,Canvas = mainCanvas,Source = source,Destination = destination,SourcePort = sourcePort,DestinationPort = destinationPort};mainGraph.AddEdge(wire);source.PositionChanged += () => wire.Refresh();destination.PositionChanged += () => wire.Refresh();wire.Paint();return wire;} };
            UIFactory setUpGraph = new UIFactory(getUIContainer: () =>{/* This lambda executes during the UI setup call, which occurs before the app event flow.The reason for putting this lambda here is that thisensures that mainGraph is set up before being passedinto the scope of other delegates down the line (before the app event flow)*/mainGraph = new Graph();mainGraph.EdgeAdded += edge => {var wire = edge as ALAWire;var src = wire.Source as ALANode;src.Edges.Add(edge);var dest = wire.Destination as ALANode;dest.Edges.Add(edge);};mainGraph.EdgeDeleted += edge => {var wire = edge as ALAWire;var src = wire.Source as ALANode;src.Edges.Remove(edge);var dest = wire.Destination as ALANode;dest.Edges.Remove(edge);};/* Return a dummy invisible IUI */return new Text("", visible: false);}) { InstanceName = "setUpGraph" };
            Data<ALANode> id_3c2b8288400b4041b41f61171d837ce3 = new Data<ALANode>() { Lambda = () => mainGraph.Roots.First() as ALANode };
            RightTreeLayout<ALANode> id_59b4df9482964ab0b634b5f3f03bcebc = new RightTreeLayout<ALANode>() { GetID = n => n.Id, GetWidth = n => (n.Render as FrameworkElement).ActualWidth, GetHeight = n => (n.Render as FrameworkElement).ActualHeight, SetX = (n, x) => WPFCanvas.SetLeft(n.Render, x), SetY = (n, y) => WPFCanvas.SetTop(n.Render, y), GetChildren = n => mainGraph.Edges.Where(e => e is ALAWire wire && wire.Source == n).Select(e => ((e as ALAWire).Destination) as ALANode), HorizontalGap = 100, VerticalGap = 20, InitialX = 50, InitialY = 50 };
            EventConnector layoutDiagram = new EventConnector() { InstanceName = "layoutDiagram" };
            DataFlowConnector<ALANode> id_72f3e9ac140347e698736452a575f0ea = new DataFlowConnector<ALANode>() {  };
            ApplyAction<ALANode> id_45637591712946648182f5ebf1beb35b = new ApplyAction<ALANode>() { Lambda = node =>{Dispatcher.CurrentDispatcher.Invoke(() => {var edges = mainGraph.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);} };
            KeyEvent id_f99422f907974f099e5edcfa210675c2 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.R }, Condition = args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected) };
            Apply<AbstractionModel, object> id_247672cb97bb40e2a5245b5599fc78eb = new Apply<AbstractionModel, object>() { Lambda = input => {var node = new ALANode();node.Model = input;node.Graph = mainGraph;node.Canvas = mainCanvas;node.StateTransition = stateTransition;node.AvailableDomainAbstractions.AddRange( abstractionModelManager.GetAbstractionTypes());node.TypeChanged += newType => {abstractionModelManager.UpdateAbstractionModel(abstractionModelManager.GetAbstractionModel(newType),node.Model);node.UpdateUI();Dispatcher.CurrentDispatcher.Invoke(() => {var edges = node.Edges;foreach (var edge in edges){(edge as ALAWire).Refresh();}}, DispatcherPriority.ContextIdle);};mainGraph.AddNode(node);node.CreateInternals();mainCanvas.Children.Add(node.Render);return node;} };
            MenuBar id_0e76d2a57b5d415fa31af9ba9579ad62 = new MenuBar() {  };
            MenuItem id_5ec424d26f8e4313964b9b8272662570 = new MenuItem(header: "File") {  };
            MenuItem id_b07eae4d4fd14f619a22d8e1659b351d = new MenuItem(header: "Open Project") {  };
            FolderBrowser id_af662b42ae01406b85c73d58f28f0370 = new FolderBrowser() { Description = "" };
            DirectorySearch id_a428c0eb9cdd486f8b1483108d9a052d = new DirectorySearch(directoriesToFind: new string[] { "DomainAbstractions" }) { FilenameFilter = "*.cs" };
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_6e54a9fe650049918db6cae1f07b0d75 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() { Lambda = input =>{var list = new List<string>();if (input.ContainsKey("DomainAbstractions")){list = input["DomainAbstractions"];}return list;} };
            ForEach<string> id_798489065a4c44cdb092a9105b496280 = new ForEach<string>() {  };
            ApplyAction<string> id_99829877caa1447eb28a7d3710eb92d7 = new ApplyAction<string>() { Lambda = input =>{abstractionModelManager.CreateAbstractionModelFromPath(input);} };
            Data<string> id_5ef82569998444b3a34bfe0554b6fe5c = new Data<string>() { storedData = "Apply<T1, T2>" };
            Apply<string, AbstractionModel> id_c29c7d7c13884f39a423d3c4d685e75c = new Apply<string, AbstractionModel>() { Lambda = input =>{return abstractionModelManager.GetAbstractionModel(input);} };
            Data<string> id_a32b8dfa4c394491abec169f94c38ba3 = new Data<string>() { storedData = @"F:\Projects\GALADE\ALACore" };
            DropDownMenu id_3358a77449ac413db708c400ce9a6362 = new DropDownMenu() { Items = new string[100] };
            KeyEvent id_eefec91643de417f92c8b2b9f029862a = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.Enter } };
            ApplyAction<object> id_e27c2c1a6a7a4307bae4fea1e4a2e1c4 = new ApplyAction<object>() { Lambda = input =>{(input as WPFCanvas).Focus();} };
            MenuItem id_84ed7a9c294d4b5ea3303d8ef7a4848b = new MenuItem(header: "Debug") {  };
            MenuItem id_9ef95d70528e40c184b64fc3774f2d0a = new MenuItem(header: "TextEditor test") {  };
            PopupWindow id_09ccc8be40cc4805a2da37c794320aa0 = new PopupWindow(title: "") { Height = 720, Width = 1280, Resize = SizeToContent.WidthAndHeight };
            Box id_396ef27795b847378d1a487ad8d84393 = new Box() { Width = 100, Height = 100 };
            TextEditor id_bb60a4a340c54cb1ad40b3fd1aa4353c = new TextEditor() { Width = 1280, Height = 720 };
            DataFlowConnector<string> id_da1311a017ba4db195af6478603169f4 = new DataFlowConnector<string>() {  };
            FolderWatcher projectFolderWatcher = new FolderWatcher() { InstanceName = "projectFolderWatcher", RootPath = "", Filter = "*.cs", WatchSubdirectories = true, PathRegex = @".*\.cs$" };
            ConvertToEvent<string> id_b3a838ba623e476d9995a587fe3b1e45 = new ConvertToEvent<string>() {  };
            Data<List<string>> id_0b1b578047244502a1c5328334780356 = new Data<List<string>>() { Lambda = () => {var path = projectFolderWatcher.RootPath;return default;} };
            // END AUTO-GENERATED INSTANTIATIONS FOR Application.xmind

            // BEGIN AUTO-GENERATED WIRING FOR Application.xmind
            mainWindow.WireTo(id_d037aa8c10a84b7aaad4c9f5d9493cc4, "iuiStructure");
            mainWindow.WireTo(id_a32b8dfa4c394491abec169f94c38ba3, "appStart");
            id_d037aa8c10a84b7aaad4c9f5d9493cc4.WireTo(setUpGraph, "children");
            id_d037aa8c10a84b7aaad4c9f5d9493cc4.WireTo(id_0e76d2a57b5d415fa31af9ba9579ad62, "children");
            id_d037aa8c10a84b7aaad4c9f5d9493cc4.WireTo(id_cef9ca58951448259dc885a01a22e286, "children");
            id_cef9ca58951448259dc885a01a22e286.WireTo(id_86ae5288a1dc4590a9f23c46fa9a5c5b, "canvasOutput");
            id_cef9ca58951448259dc885a01a22e286.WireTo(id_5911b04a873646be9fd9b0a2e542eba0, "eventHandlers");
            id_cef9ca58951448259dc885a01a22e286.WireTo(id_f99422f907974f099e5edcfa210675c2, "eventHandlers");
            id_cef9ca58951448259dc885a01a22e286.WireTo(id_eefec91643de417f92c8b2b9f029862a, "eventHandlers");
            id_cef9ca58951448259dc885a01a22e286.WireTo(id_fd5edff2f69240ee80da7b2dc94124e8, "contextMenu");
            id_5911b04a873646be9fd9b0a2e542eba0.WireTo(id_0226b5fc30a743819d3ca8e57165c00b, "eventHappened");
            id_744c8ab13e224a929e20fc471858dee7.WireTo(initialiseNode, "dataOutput");
            id_fd5edff2f69240ee80da7b2dc94124e8.WireTo(id_44f6e9a052d64f68a879a9e2c89e4b17, "children");
            id_44f6e9a052d64f68a879a9e2c89e4b17.WireTo(id_744c8ab13e224a929e20fc471858dee7, "clickedEvent");
            id_0226b5fc30a743819d3ca8e57165c00b.WireTo(id_5ef82569998444b3a34bfe0554b6fe5c, "fanoutList");
            id_0226b5fc30a743819d3ca8e57165c00b.WireTo(layoutDiagram, "complete");
            id_3c2b8288400b4041b41f61171d837ce3.WireTo(id_72f3e9ac140347e698736452a575f0ea, "dataOutput");
            layoutDiagram.WireTo(id_3c2b8288400b4041b41f61171d837ce3, "fanoutList");
            id_72f3e9ac140347e698736452a575f0ea.WireTo(id_59b4df9482964ab0b634b5f3f03bcebc, "fanoutList");
            id_72f3e9ac140347e698736452a575f0ea.WireTo(id_45637591712946648182f5ebf1beb35b, "fanoutList");
            id_f99422f907974f099e5edcfa210675c2.WireTo(layoutDiagram, "eventHappened");
            id_247672cb97bb40e2a5245b5599fc78eb.WireTo(createAndPaintALAWire, "output");
            id_0e76d2a57b5d415fa31af9ba9579ad62.WireTo(id_5ec424d26f8e4313964b9b8272662570, "children");
            id_0e76d2a57b5d415fa31af9ba9579ad62.WireTo(id_84ed7a9c294d4b5ea3303d8ef7a4848b, "children");
            id_5ec424d26f8e4313964b9b8272662570.WireTo(id_b07eae4d4fd14f619a22d8e1659b351d, "children");
            id_5ec424d26f8e4313964b9b8272662570.WireTo(id_3358a77449ac413db708c400ce9a6362, "children");
            id_b07eae4d4fd14f619a22d8e1659b351d.WireTo(id_af662b42ae01406b85c73d58f28f0370, "clickedEvent");
            id_af662b42ae01406b85c73d58f28f0370.WireTo(id_da1311a017ba4db195af6478603169f4, "selectedFolderPathOutput");
            id_a428c0eb9cdd486f8b1483108d9a052d.WireTo(id_6e54a9fe650049918db6cae1f07b0d75, "foundFiles");
            id_6e54a9fe650049918db6cae1f07b0d75.WireTo(id_798489065a4c44cdb092a9105b496280, "output");
            id_798489065a4c44cdb092a9105b496280.WireTo(id_99829877caa1447eb28a7d3710eb92d7, "elementOutput");
            id_5ef82569998444b3a34bfe0554b6fe5c.WireTo(id_c29c7d7c13884f39a423d3c4d685e75c, "dataOutput");
            id_c29c7d7c13884f39a423d3c4d685e75c.WireTo(id_247672cb97bb40e2a5245b5599fc78eb, "output");
            id_a32b8dfa4c394491abec169f94c38ba3.WireTo(id_da1311a017ba4db195af6478603169f4, "dataOutput");
            id_eefec91643de417f92c8b2b9f029862a.WireTo(id_e27c2c1a6a7a4307bae4fea1e4a2e1c4, "senderOutput");
            id_84ed7a9c294d4b5ea3303d8ef7a4848b.WireTo(id_9ef95d70528e40c184b64fc3774f2d0a, "children");
            id_9ef95d70528e40c184b64fc3774f2d0a.WireTo(id_09ccc8be40cc4805a2da37c794320aa0, "clickedEvent");
            id_09ccc8be40cc4805a2da37c794320aa0.WireTo(id_396ef27795b847378d1a487ad8d84393, "children");
            id_396ef27795b847378d1a487ad8d84393.WireTo(id_bb60a4a340c54cb1ad40b3fd1aa4353c, "uiLayout");
            id_da1311a017ba4db195af6478603169f4.WireTo(id_a428c0eb9cdd486f8b1483108d9a052d, "fanoutList");
            id_da1311a017ba4db195af6478603169f4.WireTo(projectFolderWatcher, "fanoutList");
            projectFolderWatcher.WireTo(id_b3a838ba623e476d9995a587fe3b1e45, "changedFile");
            id_b3a838ba623e476d9995a587fe3b1e45.WireTo(id_0b1b578047244502a1c5328334780356, "eventOutput");
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


