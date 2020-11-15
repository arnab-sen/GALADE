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

            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR Application.xmind
            var mainWindow = new MainWindow(title:"GALADE") {InstanceName="mainWindow"};
            var id_ee1e7b52a2e8458fbd2a889433dfca79 = new Vertical() {Layouts=new[]{0, 2, 0}};
            var id_88aa5fdf3bbc4e429db278dd29f81159 = new CanvasDisplay() {StateTransition=stateTransition,Height=720,Width=1280,Background=Brushes.White,Canvas=mainCanvas};
            var id_855f86954b3e4776909cde23cd96d071 = new KeyEvent(eventName:"KeyDown") {Condition=args => mainGraph.Get("SelectedNode") != null && stateTransition.CurrentStateMatches(Enums.DiagramMode.IdleSelected),Keys=new[]{Key.A}};
            var id_581015f073614919a33126efd44bf477 = new ContextMenu() {};
            var id_57e6a33441c54bc89dc30a28898cb1c0 = new MenuItem(header:"Add root") {};
            var id_ad29db53c0d64d4b8be9e31474882158 = new EventConnector() {};
            var getFirstRoot = new Data<ALANode>() {InstanceName="getFirstRoot",Lambda=() => mainGraph.Roots.FirstOrDefault() as ALANode};
            var id_54cdb3b62fb0433a996dc0dc58ddfa93 = new RightTreeLayout<ALANode>() {GetID=n => n.Id,GetWidth=n => n.Width,GetHeight=n => n.Height,SetX=(n, x) => n.PositionX = x,SetY=(n, y) => n.PositionY = y,GetChildren=n => mainGraph.Edges.Where(e => e is ALAWire wire && wire.Source != null && wire.Destination != null && wire.Source == n).Select(e => ((e as ALAWire).Destination) as ALANode),HorizontalGap=100,VerticalGap=20,InitialX=50,InitialY=50};
            var layoutDiagram = new EventConnector() {InstanceName="layoutDiagram"};
            var id_9f631ef9374f4ca3b7b106434fb0f49c = new DataFlowConnector<ALANode>() {};
            var id_15060f49bdb841e5beeca76952775df3 = new ApplyAction<ALANode>() {Lambda=node =>{    Dispatcher.CurrentDispatcher.Invoke(() =>    {        var edges = mainGraph.Edges;        foreach (var edge in edges)        {            (edge as ALAWire).Refresh();        }    }    , DispatcherPriority.ContextIdle);}};
            var id_ed16dd83790542f4bce1db7c9f2b928f = new KeyEvent(eventName:"KeyDown") {Condition=args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected),Keys=new[]{Key.R}};
            var createNewALANode = new Apply<AbstractionModel, object>() {InstanceName="createNewALANode",Lambda=input =>{    var node = new ALANode();    node.Model = input;    node.Graph = mainGraph;    node.Canvas = mainCanvas;    node.StateTransition = stateTransition;    node.AvailableDomainAbstractions.AddRange(abstractionModelManager.GetAbstractionTypes());    node.TypeChanged += newType =>    {        abstractionModelManager.UpdateAbstractionModel(abstractionModelManager.GetAbstractionModel(newType), node.Model);        node.UpdateUI();        Dispatcher.CurrentDispatcher.Invoke(() =>        {            var edges = mainGraph.Edges;            foreach (var edge in edges)            {                (edge as ALAWire).Refresh();            }        }        , DispatcherPriority.ContextIdle);    }    ;    mainGraph.AddNode(node);    node.CreateInternals();    mainCanvas.Children.Add(node.Render);    return node;}};
            var id_42967d39c2334aab9c23697d04177f8a = new MenuBar() {};
            var id_f19494c1e76f460a9189c172ac98de60 = new MenuItem(header:"File") {};
            var id_d59c0c09aeaf46c186317b9aeaf95e2e = new MenuItem(header:"Open Project") {};
            var id_463b31fe2ac04972b5055a3ff2f74fe3 = new FolderBrowser() {Description=""};
            var id_63088b53f85b4e6bb564712c525e063c = new DirectorySearch(directoriesToFind:new string[] { "DomainAbstractions", "ProgrammingParadigms", "RequirementsAbstractions", "Modules" }) {FilenameFilter="*.cs"};
            var id_a98457fc05fc4e84bfb827f480db93d3 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() {Lambda=input =>{    var list = new List<string>();    if (input.ContainsKey("DomainAbstractions"))    {        list = input["DomainAbstractions"];    }    return list;}};
            var id_f5d3730393ab40d78baebcb9198808da = new ForEach<string>() {};
            var id_6bc94d5f257847ff8a9a9c45e02333b4 = new ApplyAction<string>() {Lambda=input =>{    abstractionModelManager.CreateAbstractionModelFromPath(input);}};
            var id_57a0335de8c047d2b2e99333c37753c1 = new Data<string>() {storedData="Apply"};
            var id_cced83e62b6f411f9afae897f48ae148 = new Apply<string, AbstractionModel>() {Lambda=input =>{    return abstractionModelManager.GetAbstractionModel(input);}};
            var id_fcc9a08cebcf4c9c89188d7033288e1c = new GetSetting(name:"ProjectFolderPath") {};
            var id_bbd9df1f15ea4926b97567d08b6835dd = new KeyEvent(eventName:"KeyDown") {Keys=new[]{Key.Enter}};
            var id_6e249d6520104ca5a1a4d847a6c862a8 = new ApplyAction<object>() {Lambda=input =>{    (input as WPFCanvas).Focus();}};
            var id_08d455bfa9744704b21570d06c3c5389 = new MenuItem(header:"Debug") {};
            var id_843593fbc341437bb7ade21d0c7f6729 = new MenuItem(header:"TextEditor test") {};
            var id_91726b8a13804a0994e27315b0213fe8 = new PopupWindow(title:"") {Width=1280,Height=720,Resize=SizeToContent.WidthAndHeight};
            var id_a2e6aa4f4d8e41b59616d63362768dde = new Box() {};
            var id_826249b1b9d245709de6f3b24503be2d = new TextEditor() {Width=1280,Height=720};
            var id_a1f87102954345b69de6841053fce813 = new DataFlowConnector<string>() {};
            var projectFolderWatcher = new FolderWatcher() {InstanceName="projectFolderWatcher",RootPath="",Filter="*.cs",PathRegex=@".*\.cs$",WatchSubdirectories=true};
            var id_9370d465784448c897f2121e81e2ad1a = new Apply<string, object>() {Lambda=input =>{    var newModel = abstractionModelManager.CreateAbstractionModelFromPath(input);    foreach (var node in mainGraph.Nodes)    {        var alaNode = node as ALANode;        if (alaNode.Model.Type != newModel.Type)            continue;        abstractionModelManager.UpdateAbstractionModel(newModel, alaNode.Model);        alaNode.UpdateUI();    }    return input;}};
            var id_8be754ff1ae8469d85b6c52692dd1628 = new ConvertToEvent<object>() {};
            var id_6d1f4415e8d849e19f5d432ea96d9abb = new MouseButtonEvent(eventName:"MouseRightButtonDown") {Condition=args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected),ExtractSender=null};
            var id_e7e60dd036af4a869e10a64b2c216104 = new ApplyAction<object>() {Lambda=input =>{    Mouse.Capture(input as WPFCanvas);    stateTransition.Update(Enums.DiagramMode.Idle);}};
            var id_44b41ddf67864f29ae9b59ed0bec2927 = new MouseButtonEvent(eventName:"MouseRightButtonUp") {Condition=args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected),ExtractSender=null};
            var id_da4f1dedd74549e283777b5f7259ad7f = new ApplyAction<object>() {Lambda=input =>{    if (Mouse.Captured?.Equals(input) ?? false)        Mouse.Capture(null);    stateTransition.Update(Enums.DiagramMode.Idle);}};
            var id_368a7dc77fe24060b5d4017152492c1e = new StateChangeListener() {StateTransition=stateTransition,CurrentStateShouldMatch=Enums.DiagramMode.All};
            var id_2f4df1d9817246e5a9184857ec5a2bf8 = new Apply<Tuple<Enums.DiagramMode, Enums.DiagramMode>, bool>() {Lambda=input =>{    return input.Item1 == Enums.DiagramMode.AwaitingPortSelection && input.Item2 == Enums.DiagramMode.Idle;}};
            var id_c80f46b08d894d4faa674408bf846b3f = new IfElse() {};
            var id_642ae4874d1e4fd2a777715cc1996b49 = new EventConnector() {};
            var createAndPaintALAWire = new Apply<object, object>() {InstanceName="createAndPaintALAWire",Lambda=input =>{    var source = mainGraph.Get("SelectedNode") as ALANode;    var destination = input as ALANode;    var sourcePort = source.GetSelectedPort(inputPort: false);    var destinationPort = destination.GetSelectedPort(inputPort: true);    var wire = new ALAWire()    {Graph = mainGraph, Canvas = mainCanvas, Source = source, Destination = destination, SourcePort = sourcePort, DestinationPort = destinationPort, StateTransition = stateTransition};    mainGraph.AddEdge(wire);    wire.Paint();    return wire;}};
            var id_1de443ed1108447199237a8c0c584fcf = new KeyEvent(eventName:"KeyDown") {Keys=new[]{Key.Delete}};
            var id_46a4d6e6cfb940278eb27561c43cbf37 = new EventLambda() {Lambda=() =>{    var selectedNode = mainGraph.Get("SelectedNode") as ALANode;    if (selectedNode == null)        return;    selectedNode.Delete(deleteAttachedWires: true);}};
            var id_83c3db6e4dfa46518991f706f8425177 = new MenuItem(header:"Refresh") {};
            var id_5297a497d2de44e5bc0ea2c431cdcee6 = new Data<AbstractionModel>() {Lambda=() => abstractionModelManager.GetAbstractionModel(abstractionModelManager.GetAbstractionTypes().FirstOrDefault())};
            var id_9bd4555e80434a7b91b65e0b386593b0 = new Apply<AbstractionModel, object>() {Lambda=createNewALANode.Lambda};
            var id_7fabbaae488340a59d940100d38e9447 = new ApplyAction<object>() {Lambda=input =>{    var alaNode = input as ALANode;    var mousePos = Mouse.GetPosition(mainCanvas);    alaNode.PositionX = mousePos.X;    alaNode.PositionY = mousePos.Y;    mainGraph.Set("LatestNode", input);    if (mainGraph.Get("SelectedNode") == null)    {        mainGraph.Set("SelectedNode", input);    }    mainGraph.Roots.Add(input);}};
            var id_bb687ee0b7dd4b86a38a3f81ddbab75f = new MenuItem(header:"Open Code File") {};
            var id_14170585873a4fb6a7550bfb3ce8ecd4 = new FileBrowser() {Mode="Open"};
            var id_2810e4e86da348b98b39c987e6ecd7b6 = new FileReader() {};
            var id_c72bf019e76a4e44831cc0bba40caa50 = new CreateDiagramFromCode() {Graph=mainGraph,Canvas=mainCanvas,ModelManager=abstractionModelManager,StateTransition=stateTransition};
            var id_f9b8e7f524a14884be753d19a351a285 = new EventConnector() {};
            var id_8fc35564768b4a64a57dc321cc1f621f = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() {Lambda=input =>{    var list = new List<string>();    if (input.ContainsKey("ProgrammingParadigms"))    {        list = input["ProgrammingParadigms"];    }    return list;}};
            var id_0fd49143884d4a6e86e6ed0ea2f1b5b4 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() {Lambda=input =>{    var list = new List<string>();    if (input.ContainsKey("RequirementsAbstractions"))    {        list = input["RequirementsAbstractions"];    }    return list;}};
            var id_35fceab68423425195096666f27475e9 = new DataFlowConnector<Dictionary<string, List<string>>>() {};
            var id_643997d9890f41d7a3fcab722aa48f89 = new Data<UIElement>() {Lambda=() => mainCanvas};
            var id_39850a5c8e0941b3bfe846cbc45ebc90 = new Scale() {WidthMultiplier=1.1,HeightMultiplier=1.1};
            var id_261d188e3ce64cc8a06f390ba51e092f = new Data<UIElement>() {Lambda=() => mainCanvas};
            var id_607ebc3589a34e86a6eee0c0639f57cc = new Scale() {WidthMultiplier=0.9,HeightMultiplier=0.9};
            var id_843620b3a9ed45bea231b841b52e5621 = new DataFlowConnector<UIElement>() {};
            var id_04c07393f532472792412d2a555510b9 = new DataFlowConnector<UIElement>() {};
            var id_841e8fee0e8a4f45819508b2086496cc = new ApplyAction<UIElement>() {Lambda=input =>{    if (!(input.RenderTransform is ScaleTransform))        return;    var transform = input.RenderTransform as ScaleTransform;    var minScale = 0.6; /*Logging.Log($"Scale: {transform.ScaleX}, {transform.ScaleX}");*/    bool nodeIsTooSmall = transform.ScaleX < minScale && transform.ScaleY < minScale;    var nodes = mainGraph.Nodes;    foreach (var node in nodes)    {        if (node is ALANode alaNode)            alaNode.ShowTypeTextMask(nodeIsTooSmall);    }}};
            var id_2a7c8f3b6b5e4879ad5a35ff6d8538fd = new MouseWheelEvent(eventName:"MouseWheel") {};
            var id_33990435606f4bbc9ba1786ed05672ab = new Apply<MouseWheelEventArgs, bool>() {Lambda=args =>{    return args.Delta > 0;}};
            var id_6909a5f3b0e446d3bb0c1382dac1faa9 = new IfElse() {};
            var id_cf7df48ac3304a8894a7536261a3b474 = new DataFlowConnector<string>() {};
            var id_8dd402ea46b042f6a0ab358514fa6a1f = new ConvertToEvent<string>() {};
            var id_4a268943755348b68ee2cb6b71f73c40 = new DispatcherEvent() {Priority=DispatcherPriority.ApplicationIdle};
            var id_a34c047df9ae4235a08b037fd9e48ab8 = new MenuItem(header:"Generate Code") {};
            var id_b5364bf1c9cd46a28e62bb2eb0e11692 = new GenerateALACode() {Graph=mainGraph};
            var id_a3efe072d6b44816a631d90ccef5b71e = new GetSetting(name:"ApplicationCodeFilePath") {};
            var id_fcfcb5f0ae544c968dcbc734ac1db51b = new Data<string>() {storedData=SETTINGS_FILEPATH};
            var id_f928bf426b204bc89ba97219c97df162 = new EditSetting() {JSONPath="$..ApplicationCodeFilePath"};
            var id_c01710b47a2a4deb824311c4dc46222d = new Data<string>() {storedData=SETTINGS_FILEPATH};
            var id_f07ddae8b4ee431d8ede6c21e1fe01c5 = new Cast<string, object>() {};
            var id_d56630aa25974f9a9c8d1ecf188f88ac = new DataFlowConnector<string>() {};
            var id_460891130e9e499184b84a23c2e43c9f = new Cast<string, object>() {};
            var id_ecfbf0b7599e4340b8b2f79b7d1e29cb = new Data<string>() {storedData=SETTINGS_FILEPATH};
            var id_92effea7b90745299826cd566a0f2b88 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() {Lambda=input =>{    var list = new List<string>();    if (input.ContainsKey("Modules"))    {        list = input["Modules"];    }    return list;}};
            var id_c5fdc10d2ceb4577bef01977ee8e9dd1 = new GetSetting(name:"ApplicationCodeFilePath") {};
            var id_33f5719681ad40f29e7a729d5c8e2246 = new Data<string>() {storedData=SETTINGS_FILEPATH};
            var id_72140c92ac4f4255abe9d149068fa16f = new FileReader() {};
            var id_1d55a1faa3dd4f78ad22ac73051f5d2d = new DataFlowConnector<string>() {};
            var generateCode = new EventConnector() {InstanceName="generateCode"};
            var id_60229af56d92436996d2ee8d919083a3 = new EditSetting() {JSONPath="$..ProjectFolderPath"};
            var id_58c03e4b18bb43de8106a4423ca54318 = new Data<string>() {storedData=SETTINGS_FILEPATH};
            var id_2b42bd6059334bfabc3df1d047751d7a = new FileWriter() {};
            var id_b9865ebcd2864642a96573ced52bbb7f = new DataFlowConnector<string>() {};
            var id_891aef13eb18444ea94b9e071c7966d7 = new InsertFileCodeLines() {StartLandmark="// BEGIN AUTO-GENERATED INSTANTIATIONS",EndLandmark="// END AUTO-GENERATED INSTANTIATIONS",Indent="            "};
            var id_62ac925f4ee1421dbe7a781823d7876c = new InsertFileCodeLines() {StartLandmark="// BEGIN AUTO-GENERATED WIRING",EndLandmark="// END AUTO-GENERATED WIRING",Indent="            "};
            var id_0e563f77c5754bdb8a75b7f55607e9b0 = new EventConnector() {};
            var id_96ab5fcf787a4e6d88af011f6e3daeae = new MenuItem(header:"Generics test") {};
            var id_026d2d87a422495aa46c8fc4bda7cdd7 = new EventLambda() {Lambda=() =>{    var node = mainGraph.Nodes.First() as ALANode;    node.Model.UpdateGeneric(0, "testType");}};
            var statusBarHorizontal = new Horizontal() {Margin=new Thickness(5),InstanceName="statusBarHorizontal"};
            var globalMessageTextDisplay = new Text(text:"") {Height=20,InstanceName="globalMessageTextDisplay"};
            var id_c4f838d19a6b4af9ac320799ebe9791f = new EventLambda() {Lambda=() =>{    Logging.MessageOutput += message => (globalMessageTextDisplay as IDataFlow<string>).Data = message;}};
            var id_5e77c28f15294641bb881592d2cd7ac9 = new EventLambda() {Lambda=() =>{    Logging.Message("Beginning code generation...");}};
            var id_3f30a573358d4fd08c4c556281737360 = new EventLambda() {Lambda=() =>{    Logging.Message("Completed code generation!");}};
            var extractALACode = new ExtractALACode() {InstanceName="extractALACode"};
            var id_13061fa931bc49d599a3a2f0b1cab26c = new ConvertToEvent<string>() {};
            var id_a2d71044048840b0a69356270e6520ac = new Data<string>() {Lambda=() =>{ /* Put the code inside a CreateWiring() method in a dummy class so that CreateDiagramFromCode uses it correctly. TODO: Update CreateDiagramFromCode to use landmarks by default. */    var sb = new StringBuilder();    sb.AppendLine("class DummyClass {");    sb.AppendLine("void CreateWiring() {");    sb.AppendLine(extractALACode.Instantiations);    sb.AppendLine(extractALACode.Wiring);    sb.AppendLine("}");    sb.AppendLine("}");    return sb.ToString();}};
            // END AUTO-GENERATED INSTANTIATIONS FOR Application.xmind

            // BEGIN AUTO-GENERATED WIRING FOR Application.xmind
            mainWindow.WireTo(id_ee1e7b52a2e8458fbd2a889433dfca79, "iuiStructure");
            mainWindow.WireTo(id_642ae4874d1e4fd2a777715cc1996b49, "appStart");
            id_ee1e7b52a2e8458fbd2a889433dfca79.WireTo(id_42967d39c2334aab9c23697d04177f8a, "children");
            id_ee1e7b52a2e8458fbd2a889433dfca79.WireTo(id_88aa5fdf3bbc4e429db278dd29f81159, "children");
            id_ee1e7b52a2e8458fbd2a889433dfca79.WireTo(statusBarHorizontal, "children");
            id_88aa5fdf3bbc4e429db278dd29f81159.WireTo(id_855f86954b3e4776909cde23cd96d071, "eventHandlers");
            id_88aa5fdf3bbc4e429db278dd29f81159.WireTo(id_ed16dd83790542f4bce1db7c9f2b928f, "eventHandlers");
            id_88aa5fdf3bbc4e429db278dd29f81159.WireTo(id_bbd9df1f15ea4926b97567d08b6835dd, "eventHandlers");
            id_88aa5fdf3bbc4e429db278dd29f81159.WireTo(id_6d1f4415e8d849e19f5d432ea96d9abb, "eventHandlers");
            id_88aa5fdf3bbc4e429db278dd29f81159.WireTo(id_44b41ddf67864f29ae9b59ed0bec2927, "eventHandlers");
            id_88aa5fdf3bbc4e429db278dd29f81159.WireTo(id_1de443ed1108447199237a8c0c584fcf, "eventHandlers");
            id_88aa5fdf3bbc4e429db278dd29f81159.WireTo(id_2a7c8f3b6b5e4879ad5a35ff6d8538fd, "eventHandlers");
            id_88aa5fdf3bbc4e429db278dd29f81159.WireTo(id_581015f073614919a33126efd44bf477, "contextMenu");
            id_855f86954b3e4776909cde23cd96d071.WireTo(id_ad29db53c0d64d4b8be9e31474882158, "eventHappened");
            id_581015f073614919a33126efd44bf477.WireTo(id_57e6a33441c54bc89dc30a28898cb1c0, "children");
            id_581015f073614919a33126efd44bf477.WireTo(id_83c3db6e4dfa46518991f706f8425177, "children");
            id_57e6a33441c54bc89dc30a28898cb1c0.WireTo(id_5297a497d2de44e5bc0ea2c431cdcee6, "clickedEvent");
            id_ad29db53c0d64d4b8be9e31474882158.WireTo(id_57a0335de8c047d2b2e99333c37753c1, "fanoutList");
            id_ad29db53c0d64d4b8be9e31474882158.WireTo(layoutDiagram, "complete");
            getFirstRoot.WireTo(id_9f631ef9374f4ca3b7b106434fb0f49c, "dataOutput");
            layoutDiagram.WireTo(id_4a268943755348b68ee2cb6b71f73c40, "fanoutList");
            id_9f631ef9374f4ca3b7b106434fb0f49c.WireTo(id_54cdb3b62fb0433a996dc0dc58ddfa93, "fanoutList");
            id_9f631ef9374f4ca3b7b106434fb0f49c.WireTo(id_15060f49bdb841e5beeca76952775df3, "fanoutList");
            id_ed16dd83790542f4bce1db7c9f2b928f.WireTo(layoutDiagram, "eventHappened");
            createNewALANode.WireTo(createAndPaintALAWire, "output");
            id_42967d39c2334aab9c23697d04177f8a.WireTo(id_f19494c1e76f460a9189c172ac98de60, "children");
            id_42967d39c2334aab9c23697d04177f8a.WireTo(id_08d455bfa9744704b21570d06c3c5389, "children");
            id_f19494c1e76f460a9189c172ac98de60.WireTo(id_d59c0c09aeaf46c186317b9aeaf95e2e, "children");
            id_f19494c1e76f460a9189c172ac98de60.WireTo(id_bb687ee0b7dd4b86a38a3f81ddbab75f, "children");
            id_d59c0c09aeaf46c186317b9aeaf95e2e.WireTo(id_463b31fe2ac04972b5055a3ff2f74fe3, "clickedEvent");
            id_463b31fe2ac04972b5055a3ff2f74fe3.WireTo(id_a1f87102954345b69de6841053fce813, "selectedFolderPathOutput");
            id_63088b53f85b4e6bb564712c525e063c.WireTo(id_35fceab68423425195096666f27475e9, "foundFiles");
            id_a98457fc05fc4e84bfb827f480db93d3.WireTo(id_f5d3730393ab40d78baebcb9198808da, "output");
            id_f5d3730393ab40d78baebcb9198808da.WireTo(id_6bc94d5f257847ff8a9a9c45e02333b4, "elementOutput");
            id_57a0335de8c047d2b2e99333c37753c1.WireTo(id_cced83e62b6f411f9afae897f48ae148, "dataOutput");
            id_cced83e62b6f411f9afae897f48ae148.WireTo(createNewALANode, "output");
            id_fcc9a08cebcf4c9c89188d7033288e1c.WireTo(id_ecfbf0b7599e4340b8b2f79b7d1e29cb, "filePathInput");
            id_fcc9a08cebcf4c9c89188d7033288e1c.WireTo(id_a1f87102954345b69de6841053fce813, "settingJsonOutput");
            id_bbd9df1f15ea4926b97567d08b6835dd.WireTo(id_6e249d6520104ca5a1a4d847a6c862a8, "senderOutput");
            id_08d455bfa9744704b21570d06c3c5389.WireTo(id_843593fbc341437bb7ade21d0c7f6729, "children");
            id_08d455bfa9744704b21570d06c3c5389.WireTo(id_a34c047df9ae4235a08b037fd9e48ab8, "children");
            id_08d455bfa9744704b21570d06c3c5389.WireTo(id_96ab5fcf787a4e6d88af011f6e3daeae, "children");
            id_843593fbc341437bb7ade21d0c7f6729.WireTo(id_91726b8a13804a0994e27315b0213fe8, "clickedEvent");
            id_91726b8a13804a0994e27315b0213fe8.WireTo(id_a2e6aa4f4d8e41b59616d63362768dde, "children");
            id_a2e6aa4f4d8e41b59616d63362768dde.WireTo(id_826249b1b9d245709de6f3b24503be2d, "uiLayout");
            id_a1f87102954345b69de6841053fce813.WireTo(id_63088b53f85b4e6bb564712c525e063c, "fanoutList");
            id_a1f87102954345b69de6841053fce813.WireTo(projectFolderWatcher, "fanoutList");
            id_a1f87102954345b69de6841053fce813.WireTo(id_460891130e9e499184b84a23c2e43c9f, "fanoutList");
            projectFolderWatcher.WireTo(id_9370d465784448c897f2121e81e2ad1a, "changedFile");
            id_9370d465784448c897f2121e81e2ad1a.WireTo(id_8be754ff1ae8469d85b6c52692dd1628, "output");
            id_8be754ff1ae8469d85b6c52692dd1628.WireTo(id_15060f49bdb841e5beeca76952775df3, "eventOutput");
            id_6d1f4415e8d849e19f5d432ea96d9abb.WireTo(id_e7e60dd036af4a869e10a64b2c216104, "argsOutput");
            id_44b41ddf67864f29ae9b59ed0bec2927.WireTo(id_da4f1dedd74549e283777b5f7259ad7f, "argsOutput");
            id_368a7dc77fe24060b5d4017152492c1e.WireTo(id_2f4df1d9817246e5a9184857ec5a2bf8, "transitionOutput");
            id_2f4df1d9817246e5a9184857ec5a2bf8.WireTo(id_c80f46b08d894d4faa674408bf846b3f, "output");
            id_c80f46b08d894d4faa674408bf846b3f.WireTo(layoutDiagram, "ifOutput");
            id_642ae4874d1e4fd2a777715cc1996b49.WireTo(id_fcc9a08cebcf4c9c89188d7033288e1c, "fanoutList");
            id_642ae4874d1e4fd2a777715cc1996b49.WireTo(id_368a7dc77fe24060b5d4017152492c1e, "fanoutList");
            id_642ae4874d1e4fd2a777715cc1996b49.WireTo(id_a3efe072d6b44816a631d90ccef5b71e, "fanoutList");
            id_642ae4874d1e4fd2a777715cc1996b49.WireTo(id_f9b8e7f524a14884be753d19a351a285, "complete");
            id_1de443ed1108447199237a8c0c584fcf.WireTo(id_46a4d6e6cfb940278eb27561c43cbf37, "eventHappened");
            id_83c3db6e4dfa46518991f706f8425177.WireTo(layoutDiagram, "clickedEvent");
            id_5297a497d2de44e5bc0ea2c431cdcee6.WireTo(id_9bd4555e80434a7b91b65e0b386593b0, "dataOutput");
            id_9bd4555e80434a7b91b65e0b386593b0.WireTo(id_7fabbaae488340a59d940100d38e9447, "output");
            id_bb687ee0b7dd4b86a38a3f81ddbab75f.WireTo(id_14170585873a4fb6a7550bfb3ce8ecd4, "clickedEvent");
            id_14170585873a4fb6a7550bfb3ce8ecd4.WireTo(id_d56630aa25974f9a9c8d1ecf188f88ac, "selectedFilePathOutput");
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
            id_2a7c8f3b6b5e4879ad5a35ff6d8538fd.WireTo(id_33990435606f4bbc9ba1786ed05672ab, "argsOutput");
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
            id_a2d71044048840b0a69356270e6520ac.WireTo(id_c72bf019e76a4e44831cc0bba40caa50, "dataOutput");
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


































































