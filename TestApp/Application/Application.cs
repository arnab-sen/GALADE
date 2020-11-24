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
using System.Text;
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
        private Dictionary<string, string> _startUpSettings = new Dictionary<string, string>()
        {
            {"DefaultFilePath", "" },
            {"LatestDiagramFilePath", "" },
            {"LatestCodeFilePath", "" },
            {"ProjectFolderPath", "" },
            {"ApplicationCodeFilePath", "" }
        };

        // Methods
        private Application Initialize()
        {
            Wiring.PostWiringInitialize();
            return this;
        }

        [STAThread]
        public static void Main(string[] args)
        {
            Logging.Log(args.ToString());

            Application app = new Application();
            var mainWindow = app.Initialize()._mainWindow;
            mainWindow.CreateUI();
            var windowApp = mainWindow.CreateApp();
            windowApp.Startup += (sender, eventArgs) =>
            {
                var filePath = "";
                if (eventArgs.Args.Length > 0) filePath = eventArgs.Args[0];

                app.ChangeSetting("DefaultFilePath", filePath);
            };

            mainWindow.Run(windowApp);
        }

        public string GetSetting(string name)
        {
            var value = "";
            if (_startUpSettings.ContainsKey(name)) value = _startUpSettings[name];
            return value;
        }

        public bool ChangeSetting(string name, string value)
        {
            if (_startUpSettings.ContainsKey(name))
            {
                _startUpSettings[name] = value;
                return true;
            }

            return false;
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
                File.WriteAllText(SETTINGS_FILEPATH, JObject.FromObject(_startUpSettings).ToString());
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

            var globalMessages = new List<string>();

            Logging.MessageOutput += message =>
            {
                globalMessages.Add(message);
                Logging.Log(message);
            };

            #endregion

            Graph mainGraph = new Graph();

            WPFCanvas mainCanvas = new WPFCanvas();
            AbstractionModelManager abstractionModelManager = new AbstractionModelManager();

            List<string> availableAbstractions = null;
            List<ALANode> nodeSearchResults = new List<ALANode>();

            // BEGIN AUTO-GENERATED INSTANTIATIONS
            var mainWindow = new MainWindow(title:"GALADE") {InstanceName="mainWindow"};
            var mainWindowVertical = new Vertical() {InstanceName="mainWindowVertical",Layouts=new[]{0, 2, 0}};
            var mainCanvasDisplay = new CanvasDisplay() {StateTransition=stateTransition,Height=720,Width=1280,Background=Brushes.White,Canvas=mainCanvas,InstanceName="mainCanvasDisplay"};
            var id_855f86954b3e4776909cde23cd96d071 = new KeyEvent(eventName:"KeyUp") {InstanceName="Pressed the A key",Condition=args => mainGraph.Get("SelectedNode") != null && stateTransition.CurrentStateMatches(Enums.DiagramMode.IdleSelected),Key=Key.A};
            var id_581015f073614919a33126efd44bf477 = new ContextMenu() {InstanceName="id_581015f073614919a33126efd44bf477"};
            var id_57e6a33441c54bc89dc30a28898cb1c0 = new MenuItem(header:"Add root") {InstanceName="id_57e6a33441c54bc89dc30a28898cb1c0"};
            var id_ad29db53c0d64d4b8be9e31474882158 = new EventConnector() {InstanceName="id_ad29db53c0d64d4b8be9e31474882158"};
            var getFirstRoot = new Data<ALANode>() {InstanceName="getFirstRoot",Lambda=() => mainGraph.Roots.FirstOrDefault() as ALANode};
            var id_54cdb3b62fb0433a996dc0dc58ddfa93 = new RightTreeLayout<ALANode>() {InstanceName="id_54cdb3b62fb0433a996dc0dc58ddfa93",GetID=n => n.Id,GetWidth=n => n.Width,GetHeight=n => n.Height,SetX=(n, x) => n.PositionX = x,SetY=(n, y) => n.PositionY = y,GetChildren=n => mainGraph.Edges.Where(e => e is ALAWire wire && wire.Source != null && wire.Destination != null && wire.Source == n).Select(e => ((e as ALAWire).Destination) as ALANode),HorizontalGap=100,VerticalGap=20,InitialX=50,InitialY=50};
            var layoutDiagram = new EventConnector() {InstanceName="layoutDiagram"};
            var id_9f631ef9374f4ca3b7b106434fb0f49c = new DataFlowConnector<ALANode>() {InstanceName="id_9f631ef9374f4ca3b7b106434fb0f49c"};
            var id_ed16dd83790542f4bce1db7c9f2b928f = new KeyEvent(eventName:"KeyDown") {InstanceName="R key pressed",Condition=args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected),Key=Key.R};
            var createNewALANode = new Apply<AbstractionModel, object>() {InstanceName="createNewALANode",Lambda=input =>{    var node = new ALANode();    node.Model = input;    node.Graph = mainGraph;    node.Canvas = mainCanvas;    node.StateTransition = stateTransition;    if (availableAbstractions == null)        availableAbstractions = abstractionModelManager.GetAbstractionTypes().OrderBy(s => s).ToList();    node.AvailableAbstractions.AddRange(availableAbstractions);    node.TypeChanged += newType =>    {        if (node.Model.Type == newType)            return;        node.Model.CloneFrom(abstractionModelManager.GetAbstractionModel(newType));        node.UpdateUI();        Dispatcher.CurrentDispatcher.Invoke(() =>        {            var edges = mainGraph.Edges;            foreach (var edge in edges)            {                (edge as ALAWire).Refresh();            }        }        , DispatcherPriority.ContextIdle);    }    ;    mainGraph.AddNode(node);    node.CreateInternals();    mainCanvas.Children.Add(node.Render);    node.FocusOnTypeDropDown();    return node;}};
            var id_42967d39c2334aab9c23697d04177f8a = new MenuBar() {InstanceName="id_42967d39c2334aab9c23697d04177f8a"};
            var id_f19494c1e76f460a9189c172ac98de60 = new MenuItem(header:"File") {InstanceName="File"};
            var id_d59c0c09aeaf46c186317b9aeaf95e2e = new MenuItem(header:"Open Project") {InstanceName="Open Project"};
            var id_463b31fe2ac04972b5055a3ff2f74fe3 = new FolderBrowser() {InstanceName="id_463b31fe2ac04972b5055a3ff2f74fe3",Description=""};
            var id_63088b53f85b4e6bb564712c525e063c = new DirectorySearch(directoriesToFind:new string[] { "DomainAbstractions", "ProgrammingParadigms", "RequirementsAbstractions", "Modules" }) {InstanceName="id_63088b53f85b4e6bb564712c525e063c",FilenameFilter="*.cs"};
            var id_a98457fc05fc4e84bfb827f480db93d3 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() {InstanceName="id_a98457fc05fc4e84bfb827f480db93d3",Lambda=input =>{    var list = new List<string>();    if (input.ContainsKey("DomainAbstractions"))    {        list = input["DomainAbstractions"];    }    return list;}};
            var id_f5d3730393ab40d78baebcb9198808da = new ForEach<string>() {InstanceName="id_f5d3730393ab40d78baebcb9198808da"};
            var id_6bc94d5f257847ff8a9a9c45e02333b4 = new ApplyAction<string>() {InstanceName="id_6bc94d5f257847ff8a9a9c45e02333b4",Lambda=input =>{    abstractionModelManager.CreateAbstractionModelFromPath(input);}};
            var id_57a0335de8c047d2b2e99333c37753c1 = new Data<string>() {InstanceName="[Deprecated]",storedData="Apply"};
            var createNewAbstractionModel = new Apply<string, AbstractionModel>() {InstanceName="[Deprecated]",Lambda=input =>{    var baseModel = abstractionModelManager.GetAbstractionModel(input);    var newModel = new AbstractionModel();    newModel.CloneFrom(baseModel);    return newModel;}};
            var getProjectFolderPath = new GetSetting(name:"ProjectFolderPath") {InstanceName="getProjectFolderPath"};
            var id_bbd9df1f15ea4926b97567d08b6835dd = new KeyEvent(eventName:"KeyDown") {InstanceName="Enter key pressed",Key=Key.Enter};
            var id_6e249d6520104ca5a1a4d847a6c862a8 = new ApplyAction<object>() {InstanceName="Focus on backgroundCanvas",Lambda=input =>{    (input as WPFCanvas).Focus();}};
            var id_08d455bfa9744704b21570d06c3c5389 = new MenuItem(header:"Debug") {InstanceName="Debug"};
            var id_843593fbc341437bb7ade21d0c7f6729 = new MenuItem(header:"TextEditor test") {InstanceName="TextEditor test"};
            var id_91726b8a13804a0994e27315b0213fe8 = new PopupWindow(title:"") {Width=1280,Height=720,Resize=SizeToContent.WidthAndHeight,InstanceName="id_91726b8a13804a0994e27315b0213fe8"};
            var id_a2e6aa4f4d8e41b59616d63362768dde = new Box() {InstanceName="id_a2e6aa4f4d8e41b59616d63362768dde",Width=100,Height=100};
            var id_826249b1b9d245709de6f3b24503be2d = new TextEditor() {InstanceName="id_826249b1b9d245709de6f3b24503be2d",Width=1280,Height=720};
            var id_a1f87102954345b69de6841053fce813 = new DataFlowConnector<string>() {InstanceName="id_a1f87102954345b69de6841053fce813"};
            var id_6d1f4415e8d849e19f5d432ea96d9abb = new MouseButtonEvent(eventName:"MouseRightButtonDown") {InstanceName="Right button down on canvas",Condition=args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected),ExtractSender=null};
            var id_e7e60dd036af4a869e10a64b2c216104 = new ApplyAction<object>() {InstanceName="Update to Idle",Lambda=input =>{    Mouse.Capture(input as WPFCanvas);    stateTransition.Update(Enums.DiagramMode.Idle);}};
            var id_44b41ddf67864f29ae9b59ed0bec2927 = new MouseButtonEvent(eventName:"MouseRightButtonUp") {InstanceName="Right button up on canvas",Condition=args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected),ExtractSender=null};
            var id_da4f1dedd74549e283777b5f7259ad7f = new ApplyAction<object>() {InstanceName="Release capture and update to Idle",Lambda=input =>{    if (Mouse.Captured?.Equals(input) ?? false)        Mouse.Capture(null);    stateTransition.Update(Enums.DiagramMode.Idle);}};
            var id_368a7dc77fe24060b5d4017152492c1e = new StateChangeListener() {StateTransition=stateTransition,PreviousStateShouldMatch=Enums.DiagramMode.Any,CurrentStateShouldMatch=Enums.DiagramMode.Any,InstanceName="id_368a7dc77fe24060b5d4017152492c1e"};
            var id_2f4df1d9817246e5a9184857ec5a2bf8 = new Apply<Tuple<Enums.DiagramMode, Enums.DiagramMode>, bool>() {InstanceName="id_2f4df1d9817246e5a9184857ec5a2bf8",Lambda=input =>{    return input.Item1 == Enums.DiagramMode.AwaitingPortSelection && input.Item2 == Enums.DiagramMode.Idle;}};
            var id_c80f46b08d894d4faa674408bf846b3f = new IfElse() {InstanceName="id_c80f46b08d894d4faa674408bf846b3f"};
            var id_642ae4874d1e4fd2a777715cc1996b49 = new EventConnector() {InstanceName="id_642ae4874d1e4fd2a777715cc1996b49"};
            var createAndPaintALAWire = new Apply<object, object>() {InstanceName="createAndPaintALAWire",Lambda=input =>{    var source = mainGraph.Get("SelectedNode") as ALANode;    var destination = input as ALANode;    var sourcePort = source.GetSelectedPort(inputPort: false);    var destinationPort = destination.GetSelectedPort(inputPort: true);    var wire = new ALAWire()    {Graph = mainGraph, Canvas = mainCanvas, Source = source, Destination = destination, SourcePort = sourcePort, DestinationPort = destinationPort, StateTransition = stateTransition};    mainGraph.AddEdge(wire);    wire.Paint();    return wire;}};
            var id_1de443ed1108447199237a8c0c584fcf = new KeyEvent(eventName:"KeyDown") {InstanceName="Delete pressed",Key=Key.Delete};
            var id_46a4d6e6cfb940278eb27561c43cbf37 = new EventLambda() {InstanceName="id_46a4d6e6cfb940278eb27561c43cbf37",Lambda=() =>{    var selectedNode = mainGraph.Get("SelectedNode") as ALANode;    if (selectedNode == null)        return;    selectedNode.Delete(deleteChildren: false);}};
            var id_83c3db6e4dfa46518991f706f8425177 = new MenuItem(header:"Refresh") {InstanceName="id_83c3db6e4dfa46518991f706f8425177"};
            var createDummyAbstractionModel = new Data<AbstractionModel>() {InstanceName="createDummyAbstractionModel",Lambda=() =>{    var model = new AbstractionModel()    {Type = "NewNode", Name = ""};    model.AddImplementedPort("Port", "input");    model.AddAcceptedPort("Port", "output");    return model;},storedData=default};
            var id_5297a497d2de44e5bc0ea2c431cdcee6 = new Data<AbstractionModel>() {InstanceName="id_5297a497d2de44e5bc0ea2c431cdcee6",Lambda=createDummyAbstractionModel.Lambda};
            var id_9bd4555e80434a7b91b65e0b386593b0 = new Apply<AbstractionModel, object>() {InstanceName="id_9bd4555e80434a7b91b65e0b386593b0",Lambda=createNewALANode.Lambda};
            var id_7fabbaae488340a59d940100d38e9447 = new ApplyAction<object>() {InstanceName="id_7fabbaae488340a59d940100d38e9447",Lambda=input =>{    var alaNode = input as ALANode;    var mousePos = Mouse.GetPosition(mainCanvas);    alaNode.PositionX = mousePos.X;    alaNode.PositionY = mousePos.Y;    mainGraph.Set("LatestNode", input);    if (mainGraph.Get("SelectedNode") == null)    {        mainGraph.Set("SelectedNode", input);    }    mainGraph.Roots.Add(input);}};
            var id_bb687ee0b7dd4b86a38a3f81ddbab75f = new MenuItem(header:"Open Code File") {InstanceName="Open Code File"};
            var id_14170585873a4fb6a7550bfb3ce8ecd4 = new FileBrowser() {InstanceName="id_14170585873a4fb6a7550bfb3ce8ecd4",Mode="Open"};
            var id_2810e4e86da348b98b39c987e6ecd7b6 = new FileReader() {InstanceName="id_2810e4e86da348b98b39c987e6ecd7b6"};
            var createDiagramFromCode = new CreateDiagramFromCode() {InstanceName="createDiagramFromCode",Graph=mainGraph,Canvas=mainCanvas,ModelManager=abstractionModelManager,StateTransition=stateTransition,Update=false};
            var id_f9b8e7f524a14884be753d19a351a285 = new EventConnector() {InstanceName="id_f9b8e7f524a14884be753d19a351a285"};
            var id_8fc35564768b4a64a57dc321cc1f621f = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() {InstanceName="id_8fc35564768b4a64a57dc321cc1f621f",Lambda=input =>{    var list = new List<string>();    if (input.ContainsKey("ProgrammingParadigms"))    {        list = input["ProgrammingParadigms"];    }    return list;}};
            var id_0fd49143884d4a6e86e6ed0ea2f1b5b4 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() {InstanceName="id_0fd49143884d4a6e86e6ed0ea2f1b5b4",Lambda=input =>{    var list = new List<string>();    if (input.ContainsKey("RequirementsAbstractions"))    {        list = input["RequirementsAbstractions"];    }    return list;}};
            var id_35fceab68423425195096666f27475e9 = new DataFlowConnector<Dictionary<string, List<string>>>() {InstanceName="id_35fceab68423425195096666f27475e9"};
            var id_643997d9890f41d7a3fcab722aa48f89 = new Data<UIElement>() {InstanceName="id_643997d9890f41d7a3fcab722aa48f89",Lambda=() => mainCanvas};
            var mouseWheelArgs = new DataFlowConnector<MouseWheelEventArgs>() {InstanceName="mouseWheelArgs"};
            var id_39850a5c8e0941b3bfe846cbc45ebc90 = new Scale() {InstanceName="Zoom in by 10%",WidthMultiplier=1.1,HeightMultiplier=1.1,GetAbsoluteCentre=() => mouseWheelArgs.Data.GetPosition(mainCanvas),GetScaleSensitiveCentre=() => Mouse.GetPosition(mainCanvas)};
            var id_261d188e3ce64cc8a06f390ba51e092f = new Data<UIElement>() {InstanceName="id_261d188e3ce64cc8a06f390ba51e092f",Lambda=() => mainCanvas};
            var id_607ebc3589a34e86a6eee0c0639f57cc = new Scale() {InstanceName="Zoom out by 10%",WidthMultiplier=0.9,HeightMultiplier=0.9,GetAbsoluteCentre=() => mouseWheelArgs.Data.GetPosition(mainCanvas),GetScaleSensitiveCentre=() => Mouse.GetPosition(mainCanvas)};
            var id_843620b3a9ed45bea231b841b52e5621 = new DataFlowConnector<UIElement>() {InstanceName="id_843620b3a9ed45bea231b841b52e5621"};
            var id_04c07393f532472792412d2a555510b9 = new DataFlowConnector<UIElement>() {InstanceName="id_04c07393f532472792412d2a555510b9"};
            var id_841e8fee0e8a4f45819508b2086496cc = new ApplyAction<UIElement>() {InstanceName="id_841e8fee0e8a4f45819508b2086496cc",Lambda=input =>{    var transform = (input.RenderTransform as TransformGroup)?.Children.OfType<ScaleTransform>().FirstOrDefault();    if (transform == null)        return;    var minScale = 0.6; /*Logging.Log($"Scale: {transform.ScaleX}, {transform.ScaleX}");*/    bool nodeIsTooSmall = transform.ScaleX < minScale && transform.ScaleY < minScale;    var nodes = mainGraph.Nodes;    foreach (var node in nodes)    {        if (node is ALANode alaNode)            alaNode.ShowTypeTextMask(nodeIsTooSmall);    }}};
            var id_2a7c8f3b6b5e4879ad5a35ff6d8538fd = new MouseWheelEvent(eventName:"MouseWheel") {InstanceName="id_2a7c8f3b6b5e4879ad5a35ff6d8538fd"};
            var id_33990435606f4bbc9ba1786ed05672ab = new Apply<MouseWheelEventArgs, bool>() {InstanceName="Is scroll up?",Lambda=args =>{    return args.Delta > 0;}};
            var id_6909a5f3b0e446d3bb0c1382dac1faa9 = new IfElse() {InstanceName="id_6909a5f3b0e446d3bb0c1382dac1faa9"};
            var id_cf7df48ac3304a8894a7536261a3b474 = new DataFlowConnector<string>() {InstanceName="id_cf7df48ac3304a8894a7536261a3b474"};
            var id_8dd402ea46b042f6a0ab358514fa6a1f = new ConvertToEvent<string>() {InstanceName="id_8dd402ea46b042f6a0ab358514fa6a1f"};
            var id_4a268943755348b68ee2cb6b71f73c40 = new DispatcherEvent() {InstanceName="id_4a268943755348b68ee2cb6b71f73c40",Priority=DispatcherPriority.ApplicationIdle};
            var id_a34c047df9ae4235a08b037fd9e48ab8 = new MenuItem(header:"Generate Code") {InstanceName="Generate Code"};
            var id_b5364bf1c9cd46a28e62bb2eb0e11692 = new GenerateALACode() {InstanceName="id_b5364bf1c9cd46a28e62bb2eb0e11692",Graph=mainGraph};
            var id_a3efe072d6b44816a631d90ccef5b71e = new GetSetting(name:"ApplicationCodeFilePath") {InstanceName="id_a3efe072d6b44816a631d90ccef5b71e"};
            var id_fcfcb5f0ae544c968dcbc734ac1db51b = new Data<string>() {InstanceName="id_fcfcb5f0ae544c968dcbc734ac1db51b",storedData=SETTINGS_FILEPATH};
            var id_f928bf426b204bc89ba97219c97df162 = new EditSetting() {InstanceName="id_f928bf426b204bc89ba97219c97df162",JSONPath="$..ApplicationCodeFilePath"};
            var id_c01710b47a2a4deb824311c4dc46222d = new Data<string>() {InstanceName="id_c01710b47a2a4deb824311c4dc46222d",storedData=SETTINGS_FILEPATH};
            var id_f07ddae8b4ee431d8ede6c21e1fe01c5 = new Cast<string, object>() {InstanceName="id_f07ddae8b4ee431d8ede6c21e1fe01c5"};
            var id_d56630aa25974f9a9c8d1ecf188f88ac = new DataFlowConnector<string>() {InstanceName="id_d56630aa25974f9a9c8d1ecf188f88ac"};
            var id_460891130e9e499184b84a23c2e43c9f = new Cast<string, object>() {InstanceName="id_460891130e9e499184b84a23c2e43c9f"};
            var id_ecfbf0b7599e4340b8b2f79b7d1e29cb = new Data<string>() {InstanceName="id_ecfbf0b7599e4340b8b2f79b7d1e29cb",storedData=SETTINGS_FILEPATH};
            var id_92effea7b90745299826cd566a0f2b88 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() {InstanceName="id_92effea7b90745299826cd566a0f2b88",Lambda=input =>{    var list = new List<string>();    if (input.ContainsKey("Modules"))    {        list = input["Modules"];    }    return list;}};
            var id_c5fdc10d2ceb4577bef01977ee8e9dd1 = new GetSetting(name:"ApplicationCodeFilePath") {InstanceName="id_c5fdc10d2ceb4577bef01977ee8e9dd1"};
            var id_33f5719681ad40f29e7a729d5c8e2246 = new Data<string>() {InstanceName="id_33f5719681ad40f29e7a729d5c8e2246",storedData=SETTINGS_FILEPATH};
            var id_72140c92ac4f4255abe9d149068fa16f = new FileReader() {InstanceName="id_72140c92ac4f4255abe9d149068fa16f"};
            var id_1d55a1faa3dd4f78ad22ac73051f5d2d = new DataFlowConnector<string>() {InstanceName="id_1d55a1faa3dd4f78ad22ac73051f5d2d"};
            var generateCode = new EventConnector() {InstanceName="generateCode"};
            var id_60229af56d92436996d2ee8d919083a3 = new EditSetting() {InstanceName="id_60229af56d92436996d2ee8d919083a3",JSONPath="$..ProjectFolderPath"};
            var id_58c03e4b18bb43de8106a4423ca54318 = new Data<string>() {InstanceName="id_58c03e4b18bb43de8106a4423ca54318",storedData=SETTINGS_FILEPATH};
            var id_2b42bd6059334bfabc3df1d047751d7a = new FileWriter() {InstanceName="id_2b42bd6059334bfabc3df1d047751d7a"};
            var id_b9865ebcd2864642a96573ced52bbb7f = new DataFlowConnector<string>() {InstanceName="id_b9865ebcd2864642a96573ced52bbb7f"};
            var id_891aef13eb18444ea94b9e071c7966d7 = new InsertFileCodeLines() {StartLandmark="// BEGIN AUTO-GENERATED INSTANTIATIONS",EndLandmark="// END AUTO-GENERATED INSTANTIATIONS",Indent="            ",InstanceName="id_891aef13eb18444ea94b9e071c7966d7"};
            var id_62ac925f4ee1421dbe7a781823d7876c = new InsertFileCodeLines() {StartLandmark="// BEGIN AUTO-GENERATED WIRING",EndLandmark="// END AUTO-GENERATED WIRING",Indent="            ",InstanceName="id_62ac925f4ee1421dbe7a781823d7876c"};
            var id_0e563f77c5754bdb8a75b7f55607e9b0 = new EventConnector() {InstanceName="id_0e563f77c5754bdb8a75b7f55607e9b0"};
            var id_96ab5fcf787a4e6d88af011f6e3daeae = new MenuItem(header:"Generics test") {InstanceName="Generics test"};
            var id_026d2d87a422495aa46c8fc4bda7cdd7 = new EventLambda() {InstanceName="id_026d2d87a422495aa46c8fc4bda7cdd7",Lambda=() =>{    var node = mainGraph.Nodes.First() as ALANode;    node.Model.UpdateGeneric(0, "testType");}};
            var statusBarHorizontal = new Horizontal() {Margin=new Thickness(5),InstanceName="statusBarHorizontal"};
            var globalMessageTextDisplay = new Text(text:"") {Height=20,InstanceName="globalMessageTextDisplay"};
            var id_c4f838d19a6b4af9ac320799ebe9791f = new EventLambda() {InstanceName="id_c4f838d19a6b4af9ac320799ebe9791f",Lambda=() =>{    Logging.MessageOutput += message => (globalMessageTextDisplay as IDataFlow<string>).Data = message;}};
            var id_5e77c28f15294641bb881592d2cd7ac9 = new EventLambda() {InstanceName="id_5e77c28f15294641bb881592d2cd7ac9",Lambda=() =>{    Logging.Message("Beginning code generation...");}};
            var id_3f30a573358d4fd08c4c556281737360 = new EventLambda() {InstanceName="Print code generation success message",Lambda=() =>{    Logging.Message($"[{DateTime.Now:h:mm:ss tt}] Completed code generation successfully!");}};
            var extractALACode = new ExtractALACode() {InstanceName="extractALACode"};
            var id_13061fa931bc49d599a3a2f0b1cab26c = new ConvertToEvent<string>() {InstanceName="id_13061fa931bc49d599a3a2f0b1cab26c"};
            var id_a2d71044048840b0a69356270e6520ac = new Data<string>() {InstanceName="id_a2d71044048840b0a69356270e6520ac",Lambda=() =>{ /* Put the code inside a CreateWiring() method in a dummy class so that CreateDiagramFromCode uses it correctly. TODO: Update CreateDiagramFromCode to use landmarks by default. */    var sb = new StringBuilder();    sb.AppendLine("class DummyClass {");    sb.AppendLine("void CreateWiring() {");    sb.AppendLine(extractALACode.Instantiations);    sb.AppendLine(extractALACode.Wiring);    sb.AppendLine("}");    sb.AppendLine("}");    return sb.ToString();}};
            var id_a26b08b25184469db6f0c4987d4c68dd = new KeyEvent(eventName:"KeyDown") {InstanceName="CTRL + S pressed",Key=Key.S,Modifiers=new Key[]{Key.LeftCtrl}};
            var id_6f93680658e04f8a9ab15337cee1eca3 = new MenuItem(header:"Pull from code") {InstanceName="Pull from code"};
            var id_9f411cfea16b45ed9066dd8f2006e1f1 = new FileReader() {InstanceName="id_9f411cfea16b45ed9066dd8f2006e1f1"};
            var id_db598ad59e5542a0adc5df67ced27f73 = new EventConnector() {InstanceName="id_db598ad59e5542a0adc5df67ced27f73"};
            var id_f3bf83d06926453bb054330f899b605b = new EventLambda() {InstanceName="id_f3bf83d06926453bb054330f899b605b",Lambda=() =>{    mainGraph.Clear();    mainCanvas.Children.Clear();}};
            var startDiagramCreationProcess = new DataFlowConnector<string>() {InstanceName="startDiagramCreationProcess"};
            var id_d59ccc1fe1ef492e9b436b3464466171 = new ConvertToEvent<string>() {InstanceName="id_d59ccc1fe1ef492e9b436b3464466171"};
            var id_5ddd02478c734777b9e6f1079b4b3d45 = new GetSetting(name:"DefaultFilePath") {InstanceName="id_5ddd02478c734777b9e6f1079b4b3d45"};
            var id_d5d3af7a3c9a47bf9af3b1a1e1246267 = new Apply<string, bool>() {InstanceName="id_d5d3af7a3c9a47bf9af3b1a1e1246267",Lambda=s => !string.IsNullOrEmpty(s)};
            var id_2ce385b32256413ab2489563287afaac = new IfElse() {InstanceName="id_2ce385b32256413ab2489563287afaac"};
            var id_5e96a550771141bc8cc378e652d16250 = new DataFlowConnector<string>() {InstanceName="id_5e96a550771141bc8cc378e652d16250"};
            var id_7a3fa22880894f01a993fad31c8354a3 = new Data<string>() {InstanceName="id_7a3fa22880894f01a993fad31c8354a3"};
            var id_28d229073cb049c997824e1d436eaa7e = new DispatcherEvent() {InstanceName="id_28d229073cb049c997824e1d436eaa7e"};
            var id_dcd4c90552dc4d3fb579833da87cd829 = new DispatcherEvent() {InstanceName="id_dcd4c90552dc4d3fb579833da87cd829",Priority=DispatcherPriority.Loaded};
            var id_1e62a1e411c9464c94ee234dd9dd3fdc = new EventLambda() {InstanceName="id_1e62a1e411c9464c94ee234dd9dd3fdc",Lambda=() =>{    stateTransition.Update(Enums.DiagramMode.Idle);    createDiagramFromCode.Update = false;}};
            var id_0b4478e56d614ca091979014db65d076 = new MouseButtonEvent(eventName:"MouseDown") {InstanceName="id_0b4478e56d614ca091979014db65d076",Condition=args => args.ChangedButton == MouseButton.Middle && args.ButtonState == MouseButtonState.Pressed};
            var id_d90fbf714f5f4fdc9b43cbe4d5cebf1c = new ApplyAction<object>() {InstanceName="id_d90fbf714f5f4fdc9b43cbe4d5cebf1c",Lambda=input =>{    (input as UIElement)?.Focus();    stateTransition.Update(Enums.DiagramMode.Idle);}};
            var mainHorizontal = new Horizontal() {Ratios=new[]{1, 3},InstanceName="mainHorizontal"};
            var sidePanelHoriz = new Horizontal(visible:false) {InstanceName="sidePanelHoriz"};
            var id_987196dd20ab4721b0c193bb7a2064f4 = new Vertical() {InstanceName="id_987196dd20ab4721b0c193bb7a2064f4",Layouts=new int[]{2}};
            var id_7b250b222ca44ba2922547f03a4aef49 = new TabContainer() {InstanceName="id_7b250b222ca44ba2922547f03a4aef49"};
            var directoryExplorerTab = new Tab(title:"Directory Explorer") {InstanceName="directoryExplorerTab"};
            var id_4a42bbf671cd4dba8987bd656e5a2ced = new MenuItem(header:"View") {InstanceName="View"};
            var id_b5985971664e42b3a5b0869fce7b0f9b = new MenuItem(header:"Show side panel") {InstanceName="Show side panel"};
            var id_ba60beaed16c4e2f8ac431a8174ed12b = new MenuItem(header:"Hide side panel") {InstanceName="Hide side panel"};
            var id_4dd09c40831648ea884eed68407b900e = new Data<bool>() {InstanceName="id_4dd09c40831648ea884eed68407b900e",storedData=true};
            var id_e5ab69539a364aee809c668bc9d0e1a8 = new Data<bool>() {InstanceName="id_e5ab69539a364aee809c668bc9d0e1a8",storedData=false};
            var canvasDisplayHoriz = new Horizontal() {InstanceName="canvasDisplayHoriz"};
            var id_225b04d097d24d0eb277c1c0df4a47db = new DirectoryTree() {InstanceName="id_225b04d097d24d0eb277c1c0df4a47db",FilenameFilter="*.cs",Height=700};
            var id_e8a68acda2aa4d54add689bd669589d3 = new Vertical() {InstanceName="id_e8a68acda2aa4d54add689bd669589d3",Layouts=new int[]{2, 0}};
            var projectDirectoryTreeHoriz = new Horizontal() {InstanceName="projectDirectoryTreeHoriz"};
            var projectDirectoryOptionsHoriz = new Horizontal() {VertAlignment=VerticalAlignment.Bottom,InstanceName="projectDirectoryOptionsHoriz"};
            var id_0d4d34a2cd6749759ac0c2708ddf0cbc = new Button(title:"Open diagram from file") {InstanceName="id_0d4d34a2cd6749759ac0c2708ddf0cbc"};
            var id_08a51a5702e34a38af808db65a3a6eb3 = new StateChangeListener() {StateTransition=stateTransition,PreviousStateShouldMatch=Enums.DiagramMode.Any,CurrentStateShouldMatch=Enums.DiagramMode.Idle,InstanceName="id_08a51a5702e34a38af808db65a3a6eb3"};
            var id_9d14914fdf0647bb8b4b20ea799e26c8 = new EventConnector() {InstanceName="id_9d14914fdf0647bb8b4b20ea799e26c8"};
            var unhighlightAllWires = new EventLambda() {InstanceName="unhighlightAllWires",Lambda=() =>{    var wires = mainGraph.Edges.OfType<ALAWire>();    foreach (var wire in wires)    {        wire.Deselect();    }}};
            var id_6d789ff1a0bc4a2d8e88733adc266be8 = new DataFlowConnector<MouseWheelEventArgs>() {InstanceName="id_6d789ff1a0bc4a2d8e88733adc266be8"};
            var id_8ba0c38df0f041a3a7e75fb859376491 = new ApplyAction<ALANode>() {InstanceName="id_8ba0c38df0f041a3a7e75fb859376491",Lambda=node =>{    var edges = mainGraph.Edges;    foreach (var edge in edges)    {        (edge as ALAWire).Refresh();    }}};
            var id_a236bd13c516401eb5a83a451a875dd0 = new EventConnector() {InstanceName="id_a236bd13c516401eb5a83a451a875dd0"};
            var id_6fdaaf997d974e30bbb7c106c40e997c = new EventLambda() {InstanceName="Change createDiagramFromCode.Update to true",Lambda=() => createDiagramFromCode.Update = true};
            var latestAddedNode = new DataFlowConnector<object>() {InstanceName="latestAddedNode"};
            var id_86a7f0259b204907a092da0503eb9873 = new MenuItem(header:"Test DirectoryTree") {InstanceName="Test DirectoryTree"};
            var id_3710469340354a1bbb4b9d3371c9c012 = new FolderBrowser() {InstanceName="Choose test folder"};
            var testDirectoryTree = new DirectoryTree() {InstanceName="testDirectoryTree"};
            var testSimulateKeyboard = new MenuItem(header:"Test SimulateKeyboard") {InstanceName="testSimulateKeyboard"};
            var id_5c31090d2c954aa7b4a10e753bdfc03a = new SimulateKeyboard() {InstanceName="Type 'HELLO'",Keys="HELLO".Select(c => c.ToString()).ToList(),Modifiers=new List<string>(){"SHIFT"}};
            var id_52b8f2c28c2e40cabedbd531171c779a = new EventConnector() {InstanceName="id_52b8f2c28c2e40cabedbd531171c779a"};
            var id_86ecd8f953324e34adc6238338f75db5 = new SimulateKeyboard() {InstanceName="Type comma and space",Keys=new List<string>(){"COMMA", "SPACE"}};
            var id_63e463749abe41d28d05b877479070f8 = new SimulateKeyboard() {InstanceName="Type 'WORLD'",Keys="WORLD".Select(c => c.ToString()).ToList(),Modifiers=new List<string>(){"SHIFT"}};
            var id_66e516b6027649e1995a531d03c0c518 = new SimulateKeyboard() {InstanceName="Type '!'",Keys=new List<string>(){"1"},Modifiers=new List<string>(){"SHIFT"}};
            var id_8863f404bed34d47922654bd0190259c = new KeyEvent(eventName:"KeyDown") {InstanceName="CTRL + C pressed",Condition=args => stateTransition.CurrentStateMatches(Enums.DiagramMode.IdleSelected),Key=Key.C,Modifiers=new Key[]{Key.LeftCtrl}};
            var cloneSelectedNodeModel = new Data<AbstractionModel>() {InstanceName="cloneSelectedNodeModel",Lambda=() =>{    var selectedNode = mainGraph.Get("SelectedNode") as ALANode;    if (selectedNode == null)        return null;    var baseModel = selectedNode.Model;    var clone = new AbstractionModel();    clone.CloneFrom(baseModel);    return clone;}};
            var id_0f802a208aad42209777c13b2e61fe56 = new ApplyAction<AbstractionModel>() {InstanceName="id_0f802a208aad42209777c13b2e61fe56",Lambda=input =>{    if (input == null)    {        Logging.Message("Nothing was copied.", timestamp: true);    }    else    {        mainGraph.Set("ClonedModel", input);        Logging.Message($"Copied {input} successfully.", timestamp: true);    }}};
            var id_7363c80d952e4246aba050e007287444 = new KeyEvent(eventName:"KeyUp") {InstanceName="CTRL + V pressed",Condition=args => stateTransition.CurrentStateMatches(Enums.DiagramMode.IdleSelected),Key=Key.V,Modifiers=new Key[]{Key.LeftCtrl}};
            var id_316a3befaa364f0186efabcf5efaa33f = new Data<AbstractionModel>() {InstanceName="Create empty model",Lambda=() =>{    var clonedModel = mainGraph.Get("ClonedModel") as AbstractionModel;    var tempModel = new AbstractionModel();    var clonedModelType = abstractionModelManager.GetAbstractionModel(clonedModel?.Type ?? "Apply");    return clonedModelType;}};
            var id_8647cbf4ac4049a99204b0e3aa70c326 = new ConvertToEvent<object>() {InstanceName="id_8647cbf4ac4049a99204b0e3aa70c326"};
            var id_5a22e32e96e641d49c6fb4bdf6fcd94b = new EventConnector() {InstanceName="id_5a22e32e96e641d49c6fb4bdf6fcd94b"};
            var id_36c5f05380b04b378de94534411f3f88 = new EventLambda() {InstanceName="Overwrite with cloned model",Lambda=() =>{    var clonedModel = mainGraph.Get("ClonedModel") as AbstractionModel;    var latestNode = latestAddedNode.Data as ALANode;    var model = latestNode?.Model;    if (model == null)        return;    model.CloneFrom(clonedModel);    latestNode?.UpdateUI();    latestNode.RefreshParameterRows(removeEmptyRows: true);}};
            var id_0945b34f58a146ff983962f595f57fb2 = new DispatcherEvent() {InstanceName="id_0945b34f58a146ff983962f595f57fb2"};
            var id_4341066281bc4015a668a3bbbcb7256b = new ApplyAction<KeyEventArgs>() {InstanceName="id_4341066281bc4015a668a3bbbcb7256b",Lambda=args => args.Handled = true};
            var id_024b1810c2d24db3b9fac1ccce2fad9e = new DataFlowConnector<AbstractionModel>() {InstanceName="id_024b1810c2d24db3b9fac1ccce2fad9e"};
            var id_2c933997055b4122bdb77945f1abb560 = new MenuItem(header:"Test reset canvas on root") {InstanceName="Test reset canvas on root"};
            var id_0eea701e0bc84c42a9f17ccc200ef2ef = new Data<ALANode>() {InstanceName="id_0eea701e0bc84c42a9f17ccc200ef2ef",Lambda=() => mainGraph?.Roots.FirstOrDefault() as ALANode};
            var resetViewOnNode = new ApplyAction<ALANode>() {InstanceName="resetViewOnNode",Lambda=node =>{    if (node == null)        return;    var render = node.Render;    var renderPosition = new Point(WPFCanvas.GetLeft(render), WPFCanvas.GetTop(render));    WPFCanvas.SetLeft(mainCanvas, -renderPosition.X + 20);    WPFCanvas.SetTop(mainCanvas, -renderPosition.Y + 20);}};
            var id_29ed401eb9c240d98bf5c6d1f00c5c76 = new MenuItem(header:"Test reset canvas on selected node") {InstanceName="Test reset canvas on selected node"};
            var id_fa857dd7432e406c8c6c642152b37730 = new Data<ALANode>() {InstanceName="id_fa857dd7432e406c8c6c642152b37730",Lambda=() => mainGraph.Get("SelectedNode") as ALANode};
            var id_61b3caf63ee84893babc3972f0887b44 = new DispatcherData<ALANode>() {InstanceName="id_61b3caf63ee84893babc3972f0887b44",Priority=DispatcherPriority.ApplicationIdle};
            var id_40ca2809cd8744c780b0c99165e6a7bd = new DataFlowConnector<ALANode>() {InstanceName="id_40ca2809cd8744c780b0c99165e6a7bd"};
            var id_42c7f12c13804ec7b111291739be78f5 = new DataFlowConnector<string>() {InstanceName="id_42c7f12c13804ec7b111291739be78f5"};
            var id_409be365df274cc6a7a124e8a80316a5 = new ConvertToEvent<string>() {InstanceName="id_409be365df274cc6a7a124e8a80316a5"};
            var id_5e2f0621c62142c1b5972961c93cb725 = new Data<UIElement>() {InstanceName="id_5e2f0621c62142c1b5972961c93cb725",Lambda=() => mainCanvas};
            var resetScale = new Scale() {InstanceName="resetScale",AbsoluteScale=1,Reset=true};
            var id_82b26eeaba664ee7b2a2c0682e25ce08 = new EventConnector() {InstanceName="id_82b26eeaba664ee7b2a2c0682e25ce08"};
            var id_57e7dd98a0874e83bbd5014f7e9c9ef5 = new DataFlowConnector<UIElement>() {InstanceName="id_57e7dd98a0874e83bbd5014f7e9c9ef5"};
            var id_e1e6cf54f73d4f439c6f18b668a73f1a = new ApplyAction<UIElement>() {InstanceName="Reset mainCanvas position",Lambda=canvas =>{    WPFCanvas.SetLeft(canvas, 0);    WPFCanvas.SetTop(canvas, 0);}};
            var searchTab = new Tab(title:"Search") {InstanceName="searchTab"};
            var id_fed56a4aef6748178fa7078388643323 = new Horizontal() {InstanceName="id_fed56a4aef6748178fa7078388643323"};
            var searchTextBox = new TextBox() {InstanceName="searchTextBox"};
            var startSearchButton = new Button(title:"Search") {InstanceName="startSearchButton"};
            var id_00b0ca72bbce4ef4ba5cf395c666a26e = new DataFlowConnector<string>() {InstanceName="id_00b0ca72bbce4ef4ba5cf395c666a26e"};
            var id_5da1d2f5b13746f29802078592e59346 = new Data<string>() {InstanceName="id_5da1d2f5b13746f29802078592e59346"};
            var id_cc0c82a2157f4b0291c812236a6e45ba = new Vertical() {InstanceName="id_cc0c82a2157f4b0291c812236a6e45ba"};
            var id_3622556a1b37410691b51b83c004a315 = new ListDisplay() {InstanceName="id_3622556a1b37410691b51b83c004a315"};
            var id_06910bcd35b847d9a1ed9ce47caf3822 = new Apply<List<ALANode>, List<string>>() {InstanceName="id_06910bcd35b847d9a1ed9ce47caf3822",Lambda=input => input.Select(n => $"{n.Model.FullType} {n.Model.Name}").ToList()};
            var id_73274d9ce8d5414899772715a1d0f266 = new Apply<int, ALANode>() {InstanceName="id_73274d9ce8d5414899772715a1d0f266",Lambda=index =>{    var results = nodeSearchResults;    if (results.Count > index)    {        return results[index];    }    else    {        return null;    }}};
            var id_fff8d82dbdd04da18793108f9b8dd5cf = new DataFlowConnector<ALANode>() {InstanceName="id_fff8d82dbdd04da18793108f9b8dd5cf"};
            var id_75ecf8c2602c41829602707be8a8a481 = new ConvertToEvent<ALANode>() {InstanceName="id_75ecf8c2602c41829602707be8a8a481"};
            var id_23a625377ea745ee8253482ee1f0d437 = new ApplyAction<ALANode>() {InstanceName="id_23a625377ea745ee8253482ee1f0d437",Lambda=selectedNode =>{    var nodes = mainGraph.Nodes.OfType<ALANode>();    foreach (var node in nodes)    {        node.Deselect();    }    selectedNode.Select();}};
            var id_5f1c0f0187eb4dc99f15254fd36fa9b6 = new Apply<string, IEnumerable<ALANode>>() {InstanceName="findNodesMatchingSearchQuery",Lambda=searchQuery =>{    nodeSearchResults.Clear();    return mainGraph.Nodes.OfType<ALANode>();}};
            var id_8e347b7f5f3b4aa6b1c8f1966d0280a3 = new ForEach<ALANode>() {InstanceName="id_8e347b7f5f3b4aa6b1c8f1966d0280a3"};
            var id_282744d2590b4d3e8b337d73c05e0823 = new DataFlowConnector<ALANode>() {InstanceName="id_282744d2590b4d3e8b337d73c05e0823"};
            var currentSearchResultIndex = new DataFlowConnector<int>() {InstanceName="currentSearchResultIndex"};
            var id_2c9472651f984aa8ab763f327bcfa45e = new ApplyAction<ALANode>() {InstanceName="id_2c9472651f984aa8ab763f327bcfa45e",Lambda=node =>{    var i = currentSearchResultIndex.Data;    var total = mainGraph.Nodes.Count;    Logging.Message($"Searching node {i+1}/{total}...");}};
            var currentSearchQuery = new DataFlowConnector<string>() {InstanceName="currentSearchQuery"};
            var id_08aea84aa9b54808b173fe1a29163d9b = new Data<List<ALANode>>() {Lambda=() => nodeSearchResults};
            var id_1c95fb3a139b4602bba7b10201112546 = new DispatcherData<ALANode>() {};
            var id_01bdd051f2034331bd9f121029b0e2e8 = new DispatcherData<ALANode>() {};
            var id_67bc4eb50bb04d9694a1a0d5ce65c9d9 = new ApplyAction<ALANode>() {Lambda=node => {    var query = currentSearchQuery.Data;    if (node.IsMatch(query)) nodeSearchResults.Add(node);        var currentIndex = currentSearchResultIndex.Data;    var total = mainGraph.Nodes.Count;    if (currentIndex == (total - 1)) Logging.Message($"Found {nodeSearchResults.Count} search results for \"{query}\"");}};
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            mainWindow.WireTo(mainWindowVertical, "iuiStructure");
            mainWindow.WireTo(id_642ae4874d1e4fd2a777715cc1996b49, "appStart");
            mainWindowVertical.WireTo(id_42967d39c2334aab9c23697d04177f8a, "children");
            mainCanvasDisplay.WireTo(id_855f86954b3e4776909cde23cd96d071, "eventHandlers");
            mainCanvasDisplay.WireTo(id_ed16dd83790542f4bce1db7c9f2b928f, "eventHandlers");
            mainCanvasDisplay.WireTo(id_bbd9df1f15ea4926b97567d08b6835dd, "eventHandlers");
            mainCanvasDisplay.WireTo(id_6d1f4415e8d849e19f5d432ea96d9abb, "eventHandlers");
            mainCanvasDisplay.WireTo(id_44b41ddf67864f29ae9b59ed0bec2927, "eventHandlers");
            mainCanvasDisplay.WireTo(id_1de443ed1108447199237a8c0c584fcf, "eventHandlers");
            mainCanvasDisplay.WireTo(id_2a7c8f3b6b5e4879ad5a35ff6d8538fd, "eventHandlers");
            mainCanvasDisplay.WireTo(id_a26b08b25184469db6f0c4987d4c68dd, "eventHandlers");
            mainCanvasDisplay.WireTo(id_581015f073614919a33126efd44bf477, "contextMenu");
            id_581015f073614919a33126efd44bf477.WireTo(id_57e6a33441c54bc89dc30a28898cb1c0, "children");
            id_581015f073614919a33126efd44bf477.WireTo(id_83c3db6e4dfa46518991f706f8425177, "children");
            id_57e6a33441c54bc89dc30a28898cb1c0.WireTo(id_5297a497d2de44e5bc0ea2c431cdcee6, "clickedEvent");
            id_ad29db53c0d64d4b8be9e31474882158.WireTo(id_57a0335de8c047d2b2e99333c37753c1, "fanoutList");
            id_8647cbf4ac4049a99204b0e3aa70c326.WireTo(layoutDiagram, "eventOutput");
            getFirstRoot.WireTo(id_9f631ef9374f4ca3b7b106434fb0f49c, "dataOutput");
            layoutDiagram.WireTo(id_4a268943755348b68ee2cb6b71f73c40, "fanoutList");
            id_9f631ef9374f4ca3b7b106434fb0f49c.WireTo(id_54cdb3b62fb0433a996dc0dc58ddfa93, "fanoutList");
            id_ed16dd83790542f4bce1db7c9f2b928f.WireTo(layoutDiagram, "eventHappened");
            id_42967d39c2334aab9c23697d04177f8a.WireTo(id_f19494c1e76f460a9189c172ac98de60, "children");
            id_42967d39c2334aab9c23697d04177f8a.WireTo(id_08d455bfa9744704b21570d06c3c5389, "children");
            id_f19494c1e76f460a9189c172ac98de60.WireTo(id_d59c0c09aeaf46c186317b9aeaf95e2e, "children");
            id_f19494c1e76f460a9189c172ac98de60.WireTo(id_bb687ee0b7dd4b86a38a3f81ddbab75f, "children");
            id_d59c0c09aeaf46c186317b9aeaf95e2e.WireTo(id_463b31fe2ac04972b5055a3ff2f74fe3, "clickedEvent");
            id_463b31fe2ac04972b5055a3ff2f74fe3.WireTo(id_a1f87102954345b69de6841053fce813, "selectedFolderPathOutput");
            id_63088b53f85b4e6bb564712c525e063c.WireTo(id_35fceab68423425195096666f27475e9, "foundFiles");
            id_a98457fc05fc4e84bfb827f480db93d3.WireTo(id_f5d3730393ab40d78baebcb9198808da, "output");
            id_f5d3730393ab40d78baebcb9198808da.WireTo(id_6bc94d5f257847ff8a9a9c45e02333b4, "elementOutput");
            id_57a0335de8c047d2b2e99333c37753c1.WireTo(createNewAbstractionModel, "dataOutput");
            getProjectFolderPath.WireTo(id_ecfbf0b7599e4340b8b2f79b7d1e29cb, "filePathInput");
            getProjectFolderPath.WireTo(id_a1f87102954345b69de6841053fce813, "settingJsonOutput");
            id_bbd9df1f15ea4926b97567d08b6835dd.WireTo(id_6e249d6520104ca5a1a4d847a6c862a8, "senderOutput");
            id_08d455bfa9744704b21570d06c3c5389.WireTo(id_843593fbc341437bb7ade21d0c7f6729, "children");
            id_08d455bfa9744704b21570d06c3c5389.WireTo(id_a34c047df9ae4235a08b037fd9e48ab8, "children");
            id_08d455bfa9744704b21570d06c3c5389.WireTo(id_96ab5fcf787a4e6d88af011f6e3daeae, "children");
            id_843593fbc341437bb7ade21d0c7f6729.WireTo(id_91726b8a13804a0994e27315b0213fe8, "clickedEvent");
            id_91726b8a13804a0994e27315b0213fe8.WireTo(id_a2e6aa4f4d8e41b59616d63362768dde, "children");
            id_a2e6aa4f4d8e41b59616d63362768dde.WireTo(id_826249b1b9d245709de6f3b24503be2d, "uiLayout");
            id_a1f87102954345b69de6841053fce813.WireTo(id_63088b53f85b4e6bb564712c525e063c, "fanoutList");
            id_a1f87102954345b69de6841053fce813.WireTo(id_460891130e9e499184b84a23c2e43c9f, "fanoutList");
            id_6d1f4415e8d849e19f5d432ea96d9abb.WireTo(id_e7e60dd036af4a869e10a64b2c216104, "argsOutput");
            id_44b41ddf67864f29ae9b59ed0bec2927.WireTo(id_da4f1dedd74549e283777b5f7259ad7f, "argsOutput");
            id_368a7dc77fe24060b5d4017152492c1e.WireTo(id_2f4df1d9817246e5a9184857ec5a2bf8, "transitionOutput");
            id_2f4df1d9817246e5a9184857ec5a2bf8.WireTo(id_c80f46b08d894d4faa674408bf846b3f, "output");
            id_c80f46b08d894d4faa674408bf846b3f.WireTo(layoutDiagram, "ifOutput");
            id_642ae4874d1e4fd2a777715cc1996b49.WireTo(getProjectFolderPath, "fanoutList");
            id_642ae4874d1e4fd2a777715cc1996b49.WireTo(id_368a7dc77fe24060b5d4017152492c1e, "fanoutList");
            id_642ae4874d1e4fd2a777715cc1996b49.WireTo(id_f9b8e7f524a14884be753d19a351a285, "complete");
            id_1de443ed1108447199237a8c0c584fcf.WireTo(id_46a4d6e6cfb940278eb27561c43cbf37, "eventHappened");
            id_83c3db6e4dfa46518991f706f8425177.WireTo(layoutDiagram, "clickedEvent");
            id_5297a497d2de44e5bc0ea2c431cdcee6.WireTo(id_9bd4555e80434a7b91b65e0b386593b0, "dataOutput");
            id_9bd4555e80434a7b91b65e0b386593b0.WireTo(id_7fabbaae488340a59d940100d38e9447, "output");
            id_2810e4e86da348b98b39c987e6ecd7b6.WireTo(id_cf7df48ac3304a8894a7536261a3b474, "fileContentOutput");
            id_f9b8e7f524a14884be753d19a351a285.WireTo(id_c4f838d19a6b4af9ac320799ebe9791f, "fanoutList");
            id_8fc35564768b4a64a57dc321cc1f621f.WireTo(id_f5d3730393ab40d78baebcb9198808da, "output");
            id_0fd49143884d4a6e86e6ed0ea2f1b5b4.WireTo(id_f5d3730393ab40d78baebcb9198808da, "output");
            id_35fceab68423425195096666f27475e9.WireTo(id_a98457fc05fc4e84bfb827f480db93d3, "fanoutList");
            id_35fceab68423425195096666f27475e9.WireTo(id_8fc35564768b4a64a57dc321cc1f621f, "fanoutList");
            id_35fceab68423425195096666f27475e9.WireTo(id_0fd49143884d4a6e86e6ed0ea2f1b5b4, "fanoutList");
            id_35fceab68423425195096666f27475e9.WireTo(id_92effea7b90745299826cd566a0f2b88, "fanoutList");
            id_643997d9890f41d7a3fcab722aa48f89.WireTo(id_843620b3a9ed45bea231b841b52e5621, "dataOutput");
            id_261d188e3ce64cc8a06f390ba51e092f.WireTo(id_04c07393f532472792412d2a555510b9, "dataOutput");
            id_843620b3a9ed45bea231b841b52e5621.WireTo(id_39850a5c8e0941b3bfe846cbc45ebc90, "fanoutList");
            id_843620b3a9ed45bea231b841b52e5621.WireTo(id_841e8fee0e8a4f45819508b2086496cc, "fanoutList");
            id_04c07393f532472792412d2a555510b9.WireTo(id_607ebc3589a34e86a6eee0c0639f57cc, "fanoutList");
            id_04c07393f532472792412d2a555510b9.WireTo(id_841e8fee0e8a4f45819508b2086496cc, "fanoutList");
            id_33990435606f4bbc9ba1786ed05672ab.WireTo(id_6909a5f3b0e446d3bb0c1382dac1faa9, "output");
            id_6909a5f3b0e446d3bb0c1382dac1faa9.WireTo(id_643997d9890f41d7a3fcab722aa48f89, "ifOutput");
            id_6909a5f3b0e446d3bb0c1382dac1faa9.WireTo(id_261d188e3ce64cc8a06f390ba51e092f, "elseOutput");
            id_cf7df48ac3304a8894a7536261a3b474.WireTo(extractALACode, "fanoutList");
            id_cf7df48ac3304a8894a7536261a3b474.WireTo(id_13061fa931bc49d599a3a2f0b1cab26c, "fanoutList");
            id_cf7df48ac3304a8894a7536261a3b474.WireTo(id_8dd402ea46b042f6a0ab358514fa6a1f, "fanoutList");
            id_8dd402ea46b042f6a0ab358514fa6a1f.WireTo(layoutDiagram, "eventOutput");
            id_4a268943755348b68ee2cb6b71f73c40.WireTo(getFirstRoot, "delayedEvent");
            id_a34c047df9ae4235a08b037fd9e48ab8.WireTo(generateCode, "clickedEvent");
            id_b5364bf1c9cd46a28e62bb2eb0e11692.WireTo(id_891aef13eb18444ea94b9e071c7966d7, "instantiations");
            id_b5364bf1c9cd46a28e62bb2eb0e11692.WireTo(id_62ac925f4ee1421dbe7a781823d7876c, "wireTos");
            id_a3efe072d6b44816a631d90ccef5b71e.WireTo(id_fcfcb5f0ae544c968dcbc734ac1db51b, "filePathInput");
            id_f928bf426b204bc89ba97219c97df162.WireTo(id_c01710b47a2a4deb824311c4dc46222d, "filePathInput");
            id_f07ddae8b4ee431d8ede6c21e1fe01c5.WireTo(id_f928bf426b204bc89ba97219c97df162, "output");
            id_d56630aa25974f9a9c8d1ecf188f88ac.WireTo(id_2810e4e86da348b98b39c987e6ecd7b6, "fanoutList");
            id_d56630aa25974f9a9c8d1ecf188f88ac.WireTo(id_f07ddae8b4ee431d8ede6c21e1fe01c5, "fanoutList");
            id_460891130e9e499184b84a23c2e43c9f.WireTo(id_60229af56d92436996d2ee8d919083a3, "output");
            id_92effea7b90745299826cd566a0f2b88.WireTo(id_f5d3730393ab40d78baebcb9198808da, "output");
            id_c5fdc10d2ceb4577bef01977ee8e9dd1.WireTo(id_33f5719681ad40f29e7a729d5c8e2246, "filePathInput");
            id_c5fdc10d2ceb4577bef01977ee8e9dd1.WireTo(id_b9865ebcd2864642a96573ced52bbb7f, "settingJsonOutput");
            id_72140c92ac4f4255abe9d149068fa16f.WireTo(id_1d55a1faa3dd4f78ad22ac73051f5d2d, "fileContentOutput");
            id_1d55a1faa3dd4f78ad22ac73051f5d2d.WireTo(id_891aef13eb18444ea94b9e071c7966d7, "fanoutList");
            id_a26b08b25184469db6f0c4987d4c68dd.WireTo(generateCode, "eventHappened");
            generateCode.WireTo(id_c5fdc10d2ceb4577bef01977ee8e9dd1, "fanoutList");
            generateCode.WireTo(id_5e77c28f15294641bb881592d2cd7ac9, "fanoutList");
            generateCode.WireTo(id_b5364bf1c9cd46a28e62bb2eb0e11692, "fanoutList");
            generateCode.WireTo(id_0e563f77c5754bdb8a75b7f55607e9b0, "complete");
            id_60229af56d92436996d2ee8d919083a3.WireTo(id_58c03e4b18bb43de8106a4423ca54318, "filePathInput");
            id_2b42bd6059334bfabc3df1d047751d7a.WireTo(id_b9865ebcd2864642a96573ced52bbb7f, "filePathInput");
            id_b9865ebcd2864642a96573ced52bbb7f.WireTo(id_72140c92ac4f4255abe9d149068fa16f, "fanoutList");
            id_891aef13eb18444ea94b9e071c7966d7.WireTo(id_62ac925f4ee1421dbe7a781823d7876c, "newFileContentsOutput");
            id_62ac925f4ee1421dbe7a781823d7876c.WireTo(id_2b42bd6059334bfabc3df1d047751d7a, "newFileContentsOutput");
            id_0e563f77c5754bdb8a75b7f55607e9b0.WireTo(id_891aef13eb18444ea94b9e071c7966d7, "fanoutList");
            id_0e563f77c5754bdb8a75b7f55607e9b0.WireTo(id_62ac925f4ee1421dbe7a781823d7876c, "fanoutList");
            id_0e563f77c5754bdb8a75b7f55607e9b0.WireTo(id_3f30a573358d4fd08c4c556281737360, "complete");
            id_96ab5fcf787a4e6d88af011f6e3daeae.WireTo(id_026d2d87a422495aa46c8fc4bda7cdd7, "clickedEvent");
            statusBarHorizontal.WireTo(globalMessageTextDisplay, "children");
            id_13061fa931bc49d599a3a2f0b1cab26c.WireTo(id_a2d71044048840b0a69356270e6520ac, "eventOutput");
            id_42c7f12c13804ec7b111291739be78f5.WireTo(createDiagramFromCode, "fanoutList");
            id_08d455bfa9744704b21570d06c3c5389.WireTo(id_6f93680658e04f8a9ab15337cee1eca3, "children");
            id_a3efe072d6b44816a631d90ccef5b71e.WireTo(id_9f411cfea16b45ed9066dd8f2006e1f1, "settingJsonOutput");
            id_bb687ee0b7dd4b86a38a3f81ddbab75f.WireTo(id_db598ad59e5542a0adc5df67ced27f73, "clickedEvent");
            id_db598ad59e5542a0adc5df67ced27f73.WireTo(id_14170585873a4fb6a7550bfb3ce8ecd4, "fanoutList");
            id_14170585873a4fb6a7550bfb3ce8ecd4.WireTo(startDiagramCreationProcess, "selectedFilePathOutput");
            startDiagramCreationProcess.WireTo(id_d59ccc1fe1ef492e9b436b3464466171, "fanoutList");
            id_d59ccc1fe1ef492e9b436b3464466171.WireTo(id_f3bf83d06926453bb054330f899b605b, "eventOutput");
            startDiagramCreationProcess.WireTo(id_d56630aa25974f9a9c8d1ecf188f88ac, "fanoutList");
            id_9f411cfea16b45ed9066dd8f2006e1f1.WireTo(id_cf7df48ac3304a8894a7536261a3b474, "fileContentOutput");
            id_dcd4c90552dc4d3fb579833da87cd829.WireTo(id_5ddd02478c734777b9e6f1079b4b3d45, "delayedEvent");
            id_5ddd02478c734777b9e6f1079b4b3d45.WireTo(id_ecfbf0b7599e4340b8b2f79b7d1e29cb, "filePathInput");
            id_5e96a550771141bc8cc378e652d16250.WireTo(id_d5d3af7a3c9a47bf9af3b1a1e1246267, "fanoutList");
            id_d5d3af7a3c9a47bf9af3b1a1e1246267.WireTo(id_2ce385b32256413ab2489563287afaac, "output");
            id_5ddd02478c734777b9e6f1079b4b3d45.WireTo(id_5e96a550771141bc8cc378e652d16250, "settingJsonOutput");
            id_28d229073cb049c997824e1d436eaa7e.WireTo(id_7a3fa22880894f01a993fad31c8354a3, "delayedEvent");
            id_7a3fa22880894f01a993fad31c8354a3.WireTo(id_5e96a550771141bc8cc378e652d16250, "inputDataB");
            id_7a3fa22880894f01a993fad31c8354a3.WireTo(startDiagramCreationProcess, "dataOutput");
            id_2ce385b32256413ab2489563287afaac.WireTo(id_28d229073cb049c997824e1d436eaa7e, "ifOutput");
            id_f9b8e7f524a14884be753d19a351a285.WireTo(id_dcd4c90552dc4d3fb579833da87cd829, "complete");
            layoutDiagram.WireTo(id_1e62a1e411c9464c94ee234dd9dd3fdc, "complete");
            mainCanvasDisplay.WireTo(id_0b4478e56d614ca091979014db65d076, "eventHandlers");
            id_0b4478e56d614ca091979014db65d076.WireTo(id_d90fbf714f5f4fdc9b43cbe4d5cebf1c, "senderOutput");
            mainWindowVertical.WireTo(mainHorizontal, "children");
            mainWindowVertical.WireTo(statusBarHorizontal, "children");
            mainHorizontal.WireTo(sidePanelHoriz, "children");
            canvasDisplayHoriz.WireTo(mainCanvasDisplay, "children");
            sidePanelHoriz.WireTo(id_987196dd20ab4721b0c193bb7a2064f4, "children");
            id_987196dd20ab4721b0c193bb7a2064f4.WireTo(id_7b250b222ca44ba2922547f03a4aef49, "children");
            id_7b250b222ca44ba2922547f03a4aef49.WireTo(directoryExplorerTab, "childrenTabs");
            id_42967d39c2334aab9c23697d04177f8a.WireTo(id_4a42bbf671cd4dba8987bd656e5a2ced, "children");
            id_4a42bbf671cd4dba8987bd656e5a2ced.WireTo(id_b5985971664e42b3a5b0869fce7b0f9b, "children");
            id_4a42bbf671cd4dba8987bd656e5a2ced.WireTo(id_ba60beaed16c4e2f8ac431a8174ed12b, "children");
            id_b5985971664e42b3a5b0869fce7b0f9b.WireTo(id_4dd09c40831648ea884eed68407b900e, "clickedEvent");
            id_ba60beaed16c4e2f8ac431a8174ed12b.WireTo(id_e5ab69539a364aee809c668bc9d0e1a8, "clickedEvent");
            id_4dd09c40831648ea884eed68407b900e.WireTo(sidePanelHoriz, "dataOutput");
            id_e5ab69539a364aee809c668bc9d0e1a8.WireTo(sidePanelHoriz, "dataOutput");
            mainHorizontal.WireTo(canvasDisplayHoriz, "children");
            projectDirectoryTreeHoriz.WireTo(id_225b04d097d24d0eb277c1c0df4a47db, "children");
            id_a1f87102954345b69de6841053fce813.WireTo(id_225b04d097d24d0eb277c1c0df4a47db, "fanoutList");
            directoryExplorerTab.WireTo(id_e8a68acda2aa4d54add689bd669589d3, "children");
            id_e8a68acda2aa4d54add689bd669589d3.WireTo(projectDirectoryTreeHoriz, "children");
            projectDirectoryOptionsHoriz.WireTo(id_0d4d34a2cd6749759ac0c2708ddf0cbc, "children");
            id_642ae4874d1e4fd2a777715cc1996b49.WireTo(id_08a51a5702e34a38af808db65a3a6eb3, "fanoutList");
            id_08a51a5702e34a38af808db65a3a6eb3.WireTo(id_9d14914fdf0647bb8b4b20ea799e26c8, "stateChanged");
            id_9d14914fdf0647bb8b4b20ea799e26c8.WireTo(unhighlightAllWires, "fanoutList");
            id_2a7c8f3b6b5e4879ad5a35ff6d8538fd.WireTo(id_6d789ff1a0bc4a2d8e88733adc266be8, "argsOutput");
            id_6d789ff1a0bc4a2d8e88733adc266be8.WireTo(mouseWheelArgs, "fanoutList");
            id_6d789ff1a0bc4a2d8e88733adc266be8.WireTo(id_33990435606f4bbc9ba1786ed05672ab, "fanoutList");
            id_6f93680658e04f8a9ab15337cee1eca3.WireTo(id_a236bd13c516401eb5a83a451a875dd0, "clickedEvent");
            id_a236bd13c516401eb5a83a451a875dd0.WireTo(id_6fdaaf997d974e30bbb7c106c40e997c, "fanoutList");
            id_a236bd13c516401eb5a83a451a875dd0.WireTo(id_a3efe072d6b44816a631d90ccef5b71e, "fanoutList");
            createNewALANode.WireTo(latestAddedNode, "output");
            latestAddedNode.WireTo(createAndPaintALAWire, "fanoutList");
            id_855f86954b3e4776909cde23cd96d071.WireTo(id_ad29db53c0d64d4b8be9e31474882158, "eventHappened");
            id_08d455bfa9744704b21570d06c3c5389.WireTo(id_86a7f0259b204907a092da0503eb9873, "children");
            id_86a7f0259b204907a092da0503eb9873.WireTo(id_3710469340354a1bbb4b9d3371c9c012, "clickedEvent");
            id_3710469340354a1bbb4b9d3371c9c012.WireTo(testDirectoryTree, "selectedFolderPathOutput");
            id_08d455bfa9744704b21570d06c3c5389.WireTo(testSimulateKeyboard, "children");
            testSimulateKeyboard.WireTo(id_52b8f2c28c2e40cabedbd531171c779a, "clickedEvent");
            id_52b8f2c28c2e40cabedbd531171c779a.WireTo(id_5c31090d2c954aa7b4a10e753bdfc03a, "fanoutList");
            id_52b8f2c28c2e40cabedbd531171c779a.WireTo(id_86ecd8f953324e34adc6238338f75db5, "fanoutList");
            id_52b8f2c28c2e40cabedbd531171c779a.WireTo(id_63e463749abe41d28d05b877479070f8, "fanoutList");
            id_52b8f2c28c2e40cabedbd531171c779a.WireTo(id_66e516b6027649e1995a531d03c0c518, "fanoutList");
            mainCanvasDisplay.WireTo(id_8863f404bed34d47922654bd0190259c, "eventHandlers");
            id_8863f404bed34d47922654bd0190259c.WireTo(cloneSelectedNodeModel, "eventHappened");
            id_024b1810c2d24db3b9fac1ccce2fad9e.WireTo(id_0f802a208aad42209777c13b2e61fe56, "fanoutList");
            mainCanvasDisplay.WireTo(id_7363c80d952e4246aba050e007287444, "eventHandlers");
            id_316a3befaa364f0186efabcf5efaa33f.WireTo(createNewALANode, "dataOutput");
            createAndPaintALAWire.WireTo(id_8647cbf4ac4049a99204b0e3aa70c326, "output");
            id_7363c80d952e4246aba050e007287444.WireTo(id_5a22e32e96e641d49c6fb4bdf6fcd94b, "eventHappened");
            id_5a22e32e96e641d49c6fb4bdf6fcd94b.WireTo(id_316a3befaa364f0186efabcf5efaa33f, "fanoutList");
            id_5a22e32e96e641d49c6fb4bdf6fcd94b.WireTo(id_0945b34f58a146ff983962f595f57fb2, "complete");
            id_0945b34f58a146ff983962f595f57fb2.WireTo(id_36c5f05380b04b378de94534411f3f88, "delayedEvent");
            id_7363c80d952e4246aba050e007287444.WireTo(id_4341066281bc4015a668a3bbbcb7256b, "argsOutput");
            cloneSelectedNodeModel.WireTo(id_024b1810c2d24db3b9fac1ccce2fad9e, "dataOutput");
            id_08d455bfa9744704b21570d06c3c5389.WireTo(id_2c933997055b4122bdb77945f1abb560, "children");
            id_2c933997055b4122bdb77945f1abb560.WireTo(id_0eea701e0bc84c42a9f17ccc200ef2ef, "clickedEvent");
            id_08d455bfa9744704b21570d06c3c5389.WireTo(id_29ed401eb9c240d98bf5c6d1f00c5c76, "children");
            id_29ed401eb9c240d98bf5c6d1f00c5c76.WireTo(id_fa857dd7432e406c8c6c642152b37730, "clickedEvent");
            id_9f631ef9374f4ca3b7b106434fb0f49c.WireTo(id_61b3caf63ee84893babc3972f0887b44, "fanoutList");
            id_61b3caf63ee84893babc3972f0887b44.WireTo(id_40ca2809cd8744c780b0c99165e6a7bd, "delayedData");
            id_40ca2809cd8744c780b0c99165e6a7bd.WireTo(id_8ba0c38df0f041a3a7e75fb859376491, "fanoutList");
            id_a2d71044048840b0a69356270e6520ac.WireTo(id_42c7f12c13804ec7b111291739be78f5, "dataOutput");
            id_42c7f12c13804ec7b111291739be78f5.WireTo(id_409be365df274cc6a7a124e8a80316a5, "fanoutList");
            id_57e7dd98a0874e83bbd5014f7e9c9ef5.WireTo(resetScale, "fanoutList");
            id_409be365df274cc6a7a124e8a80316a5.WireTo(id_82b26eeaba664ee7b2a2c0682e25ce08, "eventOutput");
            id_82b26eeaba664ee7b2a2c0682e25ce08.WireTo(id_5e2f0621c62142c1b5972961c93cb725, "fanoutList");
            id_0eea701e0bc84c42a9f17ccc200ef2ef.WireTo(resetViewOnNode, "dataOutput");
            id_fa857dd7432e406c8c6c642152b37730.WireTo(resetViewOnNode, "dataOutput");
            id_5e2f0621c62142c1b5972961c93cb725.WireTo(id_57e7dd98a0874e83bbd5014f7e9c9ef5, "dataOutput");
            id_57e7dd98a0874e83bbd5014f7e9c9ef5.WireTo(id_e1e6cf54f73d4f439c6f18b668a73f1a, "fanoutList");
            id_7b250b222ca44ba2922547f03a4aef49.WireTo(searchTab, "childrenTabs");
            id_cc0c82a2157f4b0291c812236a6e45ba.WireTo(id_fed56a4aef6748178fa7078388643323, "children");
            id_fed56a4aef6748178fa7078388643323.WireTo(searchTextBox, "children");
            id_fed56a4aef6748178fa7078388643323.WireTo(startSearchButton, "children");
            searchTextBox.WireTo(id_00b0ca72bbce4ef4ba5cf395c666a26e, "textOutput");
            startSearchButton.WireTo(id_5da1d2f5b13746f29802078592e59346, "eventButtonClicked");
            id_5da1d2f5b13746f29802078592e59346.WireTo(id_00b0ca72bbce4ef4ba5cf395c666a26e, "inputDataB");
            id_ad29db53c0d64d4b8be9e31474882158.WireTo(createDummyAbstractionModel, "fanoutList");
            createDummyAbstractionModel.WireTo(createNewALANode, "dataOutput");
            id_e8a68acda2aa4d54add689bd669589d3.WireTo(projectDirectoryOptionsHoriz, "children");
            searchTextBox.WireTo(id_5da1d2f5b13746f29802078592e59346, "eventEnterPressed");
            searchTab.WireTo(id_cc0c82a2157f4b0291c812236a6e45ba, "children");
            id_cc0c82a2157f4b0291c812236a6e45ba.WireTo(id_3622556a1b37410691b51b83c004a315, "children");
            id_06910bcd35b847d9a1ed9ce47caf3822.WireTo(id_3622556a1b37410691b51b83c004a315, "output");
            id_3622556a1b37410691b51b83c004a315.WireTo(id_73274d9ce8d5414899772715a1d0f266, "selectedIndex");
            id_73274d9ce8d5414899772715a1d0f266.WireTo(id_fff8d82dbdd04da18793108f9b8dd5cf, "output");
            id_fff8d82dbdd04da18793108f9b8dd5cf.WireTo(id_75ecf8c2602c41829602707be8a8a481, "fanoutList");
            id_fff8d82dbdd04da18793108f9b8dd5cf.WireTo(id_23a625377ea745ee8253482ee1f0d437, "fanoutList");
            id_75ecf8c2602c41829602707be8a8a481.WireTo(id_5e2f0621c62142c1b5972961c93cb725, "eventOutput");
            id_fff8d82dbdd04da18793108f9b8dd5cf.WireTo(resetViewOnNode, "fanoutList");
            currentSearchQuery.WireTo(id_5f1c0f0187eb4dc99f15254fd36fa9b6, "fanoutList");
            id_5f1c0f0187eb4dc99f15254fd36fa9b6.WireTo(id_8e347b7f5f3b4aa6b1c8f1966d0280a3, "output");
            id_8e347b7f5f3b4aa6b1c8f1966d0280a3.WireTo(id_282744d2590b4d3e8b337d73c05e0823, "elementOutput");
            id_1c95fb3a139b4602bba7b10201112546.WireTo(id_2c9472651f984aa8ab763f327bcfa45e, "delayedData");
            id_8e347b7f5f3b4aa6b1c8f1966d0280a3.WireTo(currentSearchResultIndex, "indexOutput");
            id_5da1d2f5b13746f29802078592e59346.WireTo(currentSearchQuery, "dataOutput");
            id_8e347b7f5f3b4aa6b1c8f1966d0280a3.WireTo(id_08aea84aa9b54808b173fe1a29163d9b, "complete");
            id_08aea84aa9b54808b173fe1a29163d9b.WireTo(id_06910bcd35b847d9a1ed9ce47caf3822, "dataOutput");
            id_282744d2590b4d3e8b337d73c05e0823.WireTo(id_1c95fb3a139b4602bba7b10201112546, "fanoutList");
            id_282744d2590b4d3e8b337d73c05e0823.WireTo(id_01bdd051f2034331bd9f121029b0e2e8, "fanoutList");
            id_01bdd051f2034331bd9f121029b0e2e8.WireTo(id_67bc4eb50bb04d9694a1a0d5ce65c9d9, "delayedData");
            // END AUTO-GENERATED WIRING

            _mainWindow = mainWindow;

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

        private Application()
        {
            CreateWiring();
        }
    }
}











































































































































































































































































































































































































































































































































