using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using Application;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Path = System.IO.Path;
using EnvDTE;
using EnvDTE80;
using TextEditor = DomainAbstractions.TextEditor;
using Process = System.Diagnostics.Process;
using Thread = System.Threading.Thread;

namespace Application
{
    /// <summary>
    /// This version of GALADE is standalone, i.e. it is a single executable.
    /// </summary>
    public class Application
    {
        // Public fields and properties
        public Dictionary<string, object> myDict = new Dictionary<string, object>();

        // Private fields
        private MainWindow _mainWindow = null;
        private Dictionary<string, string> _startUpSingletonSettings = new Dictionary<string, string>()
        {
            {"DefaultFilePath", "" },
            {"LatestDiagramFilePath", "" },
            {"LatestCodeFilePath", "" },
            {"ProjectFolderPath", "" },
            {"ApplicationCodeFilePath", "" }
        };

        private bool LOG_ALL_WIRING = false;

        private EnvDTE.Debugger _debugger;
        private EnvDTE.DebuggerEvents _debuggerEvents;

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
            app.InitTest();
            var mainWindow = app.Initialize()._mainWindow;
            mainWindow.CreateUI();
            var windowApp = mainWindow.CreateApp();
            mainWindow.Run(windowApp);
        }

        public async void InitTest()
        {
            DTE2 dte = (DTE2)Marshal.GetActiveObject("VisualStudio.DTE.16.0");

            try
            {
                _debugger = dte.Debugger;
                _debuggerEvents = dte.Events.DebuggerEvents;
                // _debuggerEvents.OnEnterBreakMode += (dbgEventReason reason, ref dbgExecutionAction action) =>
                // {
                //     var a = _debugger.CurrentStackFrame?.Locals.ToString() ?? "none found";
                //
                //     Logging.Log("----- Testing -----");
                //     Logging.Log(a);
                //     Logging.Log("----- End Testing -----");
                //
                // };

                while (true)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Current debugger status:");
                    sb.Append(GetDebuggerInfo());
                    Logging.Log(sb.ToString());

                    await Task.Delay(5000);
                }

            }
            catch (Exception e)
            {
                Logging.Log(e);
            }
        }

        private string GetDebuggerInfo()
        {
            var sb = new StringBuilder();

            if (_debugger.CurrentStackFrame != null)
            {
                // var locals = _debugger.CurrentStackFrame.Locals.GetEnumerator();
                // while (locals.MoveNext())
                // {
                //
                //     var comObject = locals.Current;
                //     if (comObject == null) continue;
                //
                //     
                //
                // }

                // var methodName = _debugger.CurrentStackFrame.FunctionName;
                // sb.AppendLine($"Currently in method: {methodName}");

                var lastBreakpoint = _debugger.BreakpointLastHit;
                // var className = _debugger.BreakpointLastHit.;

                // sb.AppendLine($"Currently in method {methodName} on line {lineNumber}");
                sb.AppendLine($"Status:");
                sb.AppendLine($"Method: {lastBreakpoint.FunctionName}");
                sb.AppendLine($"Line number: {lastBreakpoint.FileLine}");
                sb.AppendLine($"File path: {lastBreakpoint.File}");
                sb.AppendLine($"Condition: {lastBreakpoint.Condition}");

                // _debugger.Breakpoints.Add(File: lastBreakpoint.File, Line: lastBreakpoint.FileLine + 1);
                _debugger.StepOver(); Thread.Sleep(1000);
                _debugger.StepOver(); Thread.Sleep(1000);
                _debugger.StepOver(); Thread.Sleep(1000);
            }
            else
            {
                sb.AppendLine("Stack frame is currently null");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Initialises a JObject property and returns whether the property was missing.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propertyName"></param>
        /// <param name="initialValue"></param>
        /// <returns></returns>
        private bool InitialiseMissingJObjectProperty(JObject obj, string propertyName, JToken initialValue)
        {
            if (!obj.ContainsKey(propertyName))
            {
                obj[propertyName] = initialValue;
                return true;
            }

            return false;
        }

        private void CreateWiring()
        {
            var fullVersion = Assembly.GetExecutingAssembly().GetName().Version;
            var VERSION_NUMBER = $"{fullVersion.Major}.{fullVersion.Minor}.{fullVersion.Build}";
#if DEBUG
            VERSION_NUMBER += "-preview";
#endif

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

            JObject settingsObj = new JObject();
            if (File.Exists(SETTINGS_FILEPATH))
            {
                try
                {
                    // Parse and update existing settings
                    var settings = File.ReadAllText(SETTINGS_FILEPATH);
                    settingsObj = JObject.Parse(settings);
                    
                }
                catch (Exception e)
                {
                    Logging.Log($"Error: Your settings file at {SETTINGS_FILEPATH} is formatted incorrectly. Please delete the file and re-run GALADE to recreate it.");
                }
            }

            // Initialise and overwrite the current settings file with any missing settings
            bool settingsIncomplete = false;

            settingsIncomplete |= InitialiseMissingJObjectProperty(settingsObj, "DefaultFilePath", "");
            settingsIncomplete |= InitialiseMissingJObjectProperty(settingsObj, "LatestDiagramFilePath", "");
            settingsIncomplete |= InitialiseMissingJObjectProperty(settingsObj, "LatestCodeFilePath", "");
            settingsIncomplete |= InitialiseMissingJObjectProperty(settingsObj, "ProjectFolderPath", "");
            settingsIncomplete |= InitialiseMissingJObjectProperty(settingsObj, "ApplicationCodeFilePath", "");
            settingsIncomplete |= InitialiseMissingJObjectProperty(settingsObj, "DefaultFilePath", "");
            settingsIncomplete |= InitialiseMissingJObjectProperty(settingsObj, "RecentProjectPaths", new JArray());

            if (settingsIncomplete) File.WriteAllText(SETTINGS_FILEPATH, settingsObj.ToString());
#endregion

#region Diagram constants and singletons

            StateTransition<Enums.DiagramMode> stateTransition = new StateTransition<Enums.DiagramMode>(Enums.DiagramMode.Idle)
            {
                InstanceName = "stateTransition",
                Matches = (flag, currentState) => (flag & currentState) != 0
            };

#endregion

#region Set up logging
            if (LOG_ALL_WIRING) Wiring.Output += output => Logging.Log(output, WIRING_LOG_FILEPATH); // Print all WireTos to a log file
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

            List<string> availableAbstractions = new List<string>();
            var nodeSearchResults = new List<ALANode>();
            var nodeSearchTextResults = new System.Collections.ObjectModel.ObservableCollection<string>();

#if DEBUG
            var versionCheckSendInitialPulse = false;
            var showDebugMenu = true;
#else
            var versionCheckSendInitialPulse = true;
            var showDebugMenu = false;
#endif

            // var layoutDiagram = new RightTreeLayout<ALANode>() {InstanceName="layoutDiagram",GetID=n => n.Id,GetWidth=n => n.Width,GetHeight=n => n.Height,SetX=(n, x) => n.PositionX = x,SetY=(n, y) => n.PositionY = y,GetChildren=n => {    var GetParent = new Func<ALAWire, ALANode>(wire => (wire.SourcePortBox.Payload as Port).IsReversePort ? wire.Destination : wire.Source);    var GetChild = new Func<ALAWire, ALANode>(wire => (wire.SourcePortBox.Payload as Port).IsReversePort ? wire.Source : wire.Destination);    var children = mainGraph.Edges.OfType<ALAWire>().Where(wire => GetParent(wire)?.Equals(n) ?? false).Select(GetChild);        return children;},HorizontalGap=100,VerticalGap=20,InitialX=50,InitialY=50,GetRoots=() => mainGraph.Roots.OfType<ALANode>().Select(n => n.Id).ToHashSet()};

            var startTesting = new EventConnector();

            // // BEGIN AUTO-GENERATED INSTANTIATIONS FOR testDiagram
            // Data<string> id_0c645d85f5cf43b2a398545c953d416c = new Data<string>() {Lambda=() =>{    var sb = new StringBuilder();    sb.AppendLine("--");    sb.AppendLine("// BEGIN AUTO-GENERATED INSTANTIATIONS FOR diagram1");    sb.AppendLine("var A = new Apply<string, string>();");    sb.AppendLine("var B = new Apply<string, string>();");    sb.AppendLine("// END AUTO-GENERATED INSTANTIATIONS FOR diagram1");    sb.AppendLine("");    sb.AppendLine("// BEGIN AUTO-GENERATED WIRING FOR diagram1");    sb.AppendLine("A.WireTo(B, \"input\");");    sb.AppendLine("B.WireTo(C, \"input\");");    sb.AppendLine("// END AUTO-GENERATED WIRING FOR diagram1");    sb.AppendLine("");    sb.AppendLine("// BEGIN AUTO-GENERATED WIRING FOR diagram2");    sb.AppendLine("D.WireTo(E);");    sb.AppendLine("E.WireTo(F);");    sb.AppendLine("// END AUTO-GENERATED WIRING FOR diagram2");    return sb.ToString();}}; /* {"IsRoot":false} */
            // MultilineSegmenter id_306460661ec0481a8c80f5b622fbc86d = new MultilineSegmenter() {IsStartLine=line => line.Trim().StartsWith("// BEGIN AUTO"),IsStopLine=line => line.Trim().StartsWith("// END AUTO")}; /* {"IsRoot":false} */
            // Apply<List<Tuple<string, List<string>, string>>, Tuple<string, List<string>>> produceDiagramBundle = new Apply<List<Tuple<string, List<string>, string>>, Tuple<string, List<string>>>() {Lambda=segments =>{    var diagramName = segments[0].Item1.Trim().Split().Last();    var instantiations = segments[0].Item2;    var wireTos = segments[1].Item2;    var combined = new List<string>();    combined.AddRange(instantiations);    combined.AddRange(wireTos);    return Tuple.Create(diagramName, combined);}}; /* {"IsRoot":false} */
            // CreateDiagramFromCode id_cf45045eee0a4e54992b947823b1d77f = new CreateDiagramFromCode() {}; /* {"IsRoot":false} */
            // // END AUTO-GENERATED INSTANTIATIONS FOR testDiagram
            //
            // // BEGIN AUTO-GENERATED WIRING FOR testDiagram
            // startTesting.WireTo(id_0c645d85f5cf43b2a398545c953d416c, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":true,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            // id_0c645d85f5cf43b2a398545c953d416c.WireTo(id_306460661ec0481a8c80f5b622fbc86d, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"MultilineSegmenter","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":[]} */
            // id_306460661ec0481a8c80f5b622fbc86d.WireTo(produceDiagramBundle, "segments"); /* {"SourceType":"MultilineSegmenter","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["List<Tuple<string, List<string>, string>>","Tuple<string, List<string>>"]} */
            // produceDiagramBundle.WireTo(id_cf45045eee0a4e54992b947823b1d77f, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"CreateDiagramFromCode","DestinationIsReference":false,"Description":"","SourceGenerics":["List<Tuple<string, List<string>, string>>","Tuple<string, List<string>>"],"DestinationGenerics":[]} */
            // // END AUTO-GENERATED WIRING FOR testDiagram

            (startTesting as IEvent).Execute();

            #region main diagram
            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR GALADE_Standalone
            MainWindow mainWindow = new MainWindow(title:"GALADE") {}; /* {"IsRoot":true} */
            DataFlowConnector<string> currentDiagramName = new DataFlowConnector<string>() {}; /* {"IsRoot":false} */
            EventConnector startGuaranteedLayoutProcess = new EventConnector() {}; /* {"IsRoot":false} */
            DataFlowConnector<string> latestVersion = new DataFlowConnector<string>() {}; /* {"IsRoot":false} */
            Vertical mainWindowVertical = new Vertical() {Layouts=new[]{0, 2, 0}}; /* {"IsRoot":false} */
            UIConfig UIConfig_canvasDisplayHoriz = new UIConfig() {}; /* {"IsRoot":false} */
            CanvasDisplay mainCanvasDisplay = new CanvasDisplay() {StateTransition=stateTransition,Height=720,Width=1280,Background=Brushes.White,Canvas=mainCanvas}; /* {"IsRoot":false} */
            DataFlowConnector<bool> searchFilterNameChecked = new DataFlowConnector<bool>() {Data=true}; /* {"IsRoot":false} */
            DataFlowConnector<bool> searchFilterTypeChecked = new DataFlowConnector<bool>() {Data=true}; /* {"IsRoot":false} */
            DataFlowConnector<bool> searchFilterInstanceNameChecked = new DataFlowConnector<bool>() {Data=true}; /* {"IsRoot":false} */
            DataFlowConnector<bool> searchFilterFieldsAndPropertiesChecked = new DataFlowConnector<bool>() {Data=true}; /* {"IsRoot":false} */
            KeyEvent A_KeyPressed = new KeyEvent(eventName:"KeyUp") {Condition=args => mainGraph.Get("SelectedNode") != null && stateTransition.CurrentStateMatches(Enums.DiagramMode.IdleSelected),Key=Key.A}; /* {"IsRoot":false} */
            ContextMenu id_581015f073614919a33126efd44bf477 = new ContextMenu() {}; /* {"IsRoot":false} */
            MenuItem id_57e6a33441c54bc89dc30a28898cb1c0 = new MenuItem(header:"Add new node") {}; /* {"IsRoot":false} */
            EventConnector id_ad29db53c0d64d4b8be9e31474882158 = new EventConnector() {}; /* {"IsRoot":false} */
            RightTreeLayout<ALANode> layoutDiagram = new RightTreeLayout<ALANode>() {GetID=n => n.Id,GetWidth=n => n.Width,GetHeight=n => n.Height,SetX=(n, x) => n.PositionX = x,SetY=(n, y) => n.PositionY = y,GetChildren=n => mainGraph.Edges.Where(e => e is ALAWire wire && wire.Source != null && wire.Destination != null && wire.Source == n).Select(e => ((e as ALAWire).Destination) as ALANode),Roots=mainGraph.Roots.OfType<ALANode>().ToList(),HorizontalGap=100,VerticalGap=20,InitialX=50,InitialY=50}; /* {"IsRoot":false} */
            EventConnector startRightTreeLayoutProcess = new EventConnector() {}; /* {"IsRoot":false} */
            KeyEvent R_KeyPressed = new KeyEvent(eventName:"KeyDown") {Condition=args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected),Key=Key.R}; /* {"IsRoot":false} */
            Apply<AbstractionModel, object> createNewALANode = new Apply<AbstractionModel, object>() {Lambda=input =>{    var node = new ALANode();    node.Model = input;    node.Graph = mainGraph;    node.Canvas = mainCanvas;    node.StateTransition = stateTransition;    if (!availableAbstractions.Any())        availableAbstractions = abstractionModelManager.GetAbstractionTypes().OrderBy(s => s).ToList();    node.AvailableAbstractions.AddRange(availableAbstractions);    node.TypeChanged += newType =>    {        if (node.Model.Type == newType)            return;        node.LoadDefaultModel(abstractionModelManager.GetAbstractionModel(newType));        node.UpdateUI();        Dispatcher.CurrentDispatcher.Invoke(() =>        {            var edges = mainGraph.Edges;            foreach (var edge in edges)            {                (edge as ALAWire).Refresh();            }            (startGuaranteedLayoutProcess as IEvent).Execute();        }        , DispatcherPriority.ContextIdle);    }    ;    mainGraph.AddNode(node);    node.CreateInternals();    mainCanvas.Children.Add(node.Render);    node.FocusOnTypeDropDown();    return node;}}; /* {"IsRoot":false} */
            MenuBar id_42967d39c2334aab9c23697d04177f8a = new MenuBar() {}; /* {"IsRoot":false} */
            MenuItem menu_File = new MenuItem(header:"File") {}; /* {"IsRoot":false} */
            MenuItem menu_OpenProject = new MenuItem(header:"Open Project") {}; /* {"IsRoot":false} */
            FolderBrowser id_463b31fe2ac04972b5055a3ff2f74fe3 = new FolderBrowser() {Description=""}; /* {"IsRoot":false} */
            DirectorySearch id_63088b53f85b4e6bb564712c525e063c = new DirectorySearch(directoriesToFind:new string[] { "DomainAbstractions", "ProgrammingParadigms", "RequirementsAbstractions", "Modules" }) {FilenameFilter="*.cs"}; /* {"IsRoot":false} */
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_a98457fc05fc4e84bfb827f480db93d3 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() {Lambda=input =>{    var list = new List<string>();    if (input.ContainsKey("DomainAbstractions"))    {        list = input["DomainAbstractions"];    }    return list;}}; /* {"IsRoot":false} */
            ForEach<string> id_f5d3730393ab40d78baebcb9198808da = new ForEach<string>() {}; /* {"IsRoot":false} */
            ApplyAction<string> id_6bc94d5f257847ff8a9a9c45e02333b4 = new ApplyAction<string>() {Lambda=input =>{    abstractionModelManager.CreateAbstractionModelFromPath(input);}}; /* {"IsRoot":false} */
            GetSetting getProjectFolderPath = new GetSetting(name:"ProjectFolderPath") {}; /* {"IsRoot":false} */
            KeyEvent Enter_KeyPressed = new KeyEvent(eventName:"KeyDown") {Key=Key.Enter}; /* {"IsRoot":false} */
            ApplyAction<object> id_6e249d6520104ca5a1a4d847a6c862a8 = new ApplyAction<object>() {Lambda=input =>{    (input as WPFCanvas).Focus();}}; /* {"IsRoot":false} */
            MenuItem id_08d455bfa9744704b21570d06c3c5389 = new MenuItem(header:"Debug") {}; /* {"IsRoot":false} */
            MenuItem id_843593fbc341437bb7ade21d0c7f6729 = new MenuItem(header:"TextEditor test") {}; /* {"IsRoot":false} */
            PopupWindow id_91726b8a13804a0994e27315b0213fe8 = new PopupWindow(title:"") {Height=720,Width=1280,Resize=SizeToContent.WidthAndHeight}; /* {"IsRoot":false} */
            Box id_a2e6aa4f4d8e41b59616d63362768dde = new Box() {Width=100,Height=100}; /* {"IsRoot":false} */
            TextEditor id_826249b1b9d245709de6f3b24503be2d = new TextEditor() {Width=1280,Height=720}; /* {"IsRoot":false} */
            DataFlowConnector<string> id_a1f87102954345b69de6841053fce813 = new DataFlowConnector<string>() {}; /* {"IsRoot":false} */
            MouseButtonEvent id_6d1f4415e8d849e19f5d432ea96d9abb = new MouseButtonEvent(eventName:"MouseRightButtonDown") {Condition=args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected),ExtractSender=null}; /* {"IsRoot":false} */
            ApplyAction<object> id_e7e60dd036af4a869e10a64b2c216104 = new ApplyAction<object>() {Lambda=input =>{    Mouse.Capture(input as WPFCanvas);    stateTransition.Update(Enums.DiagramMode.Idle);}}; /* {"IsRoot":false} */
            MouseButtonEvent id_44b41ddf67864f29ae9b59ed0bec2927 = new MouseButtonEvent(eventName:"MouseRightButtonUp") {Condition=args => stateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected),ExtractSender=null}; /* {"IsRoot":false} */
            ApplyAction<object> id_da4f1dedd74549e283777b5f7259ad7f = new ApplyAction<object>() {Lambda=input =>{    if (Mouse.Captured?.Equals(input) ?? false)        Mouse.Capture(null);    stateTransition.Update(Enums.DiagramMode.Idle);}}; /* {"IsRoot":false} */
            StateChangeListener id_368a7dc77fe24060b5d4017152492c1e = new StateChangeListener() {StateTransition=stateTransition,PreviousStateShouldMatch=Enums.DiagramMode.Any,CurrentStateShouldMatch=Enums.DiagramMode.Any}; /* {"IsRoot":false} */
            Apply<Tuple<Enums.DiagramMode, Enums.DiagramMode>, bool> id_2f4df1d9817246e5a9184857ec5a2bf8 = new Apply<Tuple<Enums.DiagramMode, Enums.DiagramMode>, bool>() {Lambda=input =>{    return input.Item1 == Enums.DiagramMode.AwaitingPortSelection && input.Item2 == Enums.DiagramMode.Idle;}}; /* {"IsRoot":false} */
            IfElse id_c80f46b08d894d4faa674408bf846b3f = new IfElse() {}; /* {"IsRoot":false} */
            EventConnector id_642ae4874d1e4fd2a777715cc1996b49 = new EventConnector() {}; /* {"IsRoot":false} */
            Apply<object, object> createAndPaintALAWire = new Apply<object, object>() {Lambda=input =>{    var source = mainGraph.Get("SelectedNode") as ALANode;    var destination = input as ALANode;    var sourcePort = source.GetSelectedPort(inputPort: false);    var destinationPort = destination.GetSelectedPort(inputPort: true);    var wire = new ALAWire()    {Graph = mainGraph, Canvas = mainCanvas, Source = source, Destination = destination, SourcePortBox = sourcePort, DestinationPortBox = destinationPort, StateTransition = stateTransition};    mainGraph.AddEdge(wire);    wire.Paint();    return wire;}}; /* {"IsRoot":false} */
            KeyEvent Delete_KeyPressed = new KeyEvent(eventName:"KeyDown") {Key=Key.Delete}; /* {"IsRoot":false} */
            EventLambda id_46a4d6e6cfb940278eb27561c43cbf37 = new EventLambda() {Lambda=() =>{    var selectedNode = mainGraph.Get("SelectedNode") as ALANode;    if (selectedNode == null)        return;    selectedNode.Delete(deleteChildren: false);}}; /* {"IsRoot":false} */
            MenuItem id_83c3db6e4dfa46518991f706f8425177 = new MenuItem(header:"Refresh") {}; /* {"IsRoot":false} */
            Data<AbstractionModel> createDummyAbstractionModel = new Data<AbstractionModel>() {Lambda=() =>{    var model = new AbstractionModel()    {Type = "NewNode", Name = ""};    model.AddImplementedPort("Port", "input");    model.AddAcceptedPort("Port", "output");    return model;},storedData=default}; /* {"IsRoot":false} */
            Data<AbstractionModel> id_5297a497d2de44e5bc0ea2c431cdcee6 = new Data<AbstractionModel>() {Lambda=createDummyAbstractionModel.Lambda}; /* {"IsRoot":false} */
            Apply<AbstractionModel, object> id_9bd4555e80434a7b91b65e0b386593b0 = new Apply<AbstractionModel, object>() {Lambda=createNewALANode.Lambda}; /* {"IsRoot":false} */
            ApplyAction<object> id_7fabbaae488340a59d940100d38e9447 = new ApplyAction<object>() {Lambda=input =>{    var alaNode = input as ALANode;    var mousePos = Mouse.GetPosition(mainCanvas);    alaNode.PositionX = mousePos.X;    alaNode.PositionY = mousePos.Y;    mainGraph.Set("LatestNode", input);    if (mainGraph.Get("SelectedNode") == null)    {        mainGraph.Set("SelectedNode", input);    } /* mainGraph.Roots.Add(input);    alaNode.IsRoot = true; */}}; /* {"IsRoot":false} */
            MenuItem menu_OpenCodeFile = new MenuItem(header:"Open Code File") {}; /* {"IsRoot":false} */
            FileBrowser id_14170585873a4fb6a7550bfb3ce8ecd4 = new FileBrowser() {Mode="Open"}; /* {"IsRoot":false} */
            FileReader id_2810e4e86da348b98b39c987e6ecd7b6 = new FileReader() {}; /* {"IsRoot":false} */
            CreateDiagramFromCode createDiagramFromCode = new CreateDiagramFromCode() {Graph=mainGraph,Canvas=mainCanvas,ModelManager=abstractionModelManager,StateTransition=stateTransition,RefreshLayout=layoutDiagram,Update=false}; /* {"IsRoot":false} */
            EventConnector id_f9b8e7f524a14884be753d19a351a285 = new EventConnector() {}; /* {"IsRoot":false} */
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_8fc35564768b4a64a57dc321cc1f621f = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() {Lambda=input =>{    var list = new List<string>();    if (input.ContainsKey("ProgrammingParadigms"))    {        list = input["ProgrammingParadigms"];    }    return list;}}; /* {"IsRoot":false} */
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_0fd49143884d4a6e86e6ed0ea2f1b5b4 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() {Lambda=input =>{    var list = new List<string>();    if (input.ContainsKey("RequirementsAbstractions"))    {        list = input["RequirementsAbstractions"];    }    return list;}}; /* {"IsRoot":false} */
            DataFlowConnector<Dictionary<string, List<string>>> id_35fceab68423425195096666f27475e9 = new DataFlowConnector<Dictionary<string, List<string>>>() {}; /* {"IsRoot":false} */
            Data<UIElement> id_643997d9890f41d7a3fcab722aa48f89 = new Data<UIElement>() {Lambda=() => mainCanvas}; /* {"IsRoot":false} */
            DataFlowConnector<MouseWheelEventArgs> mouseWheelArgs = new DataFlowConnector<MouseWheelEventArgs>() {}; /* {"IsRoot":false} */
            Scale zoomIn = new Scale() {WidthMultiplier=1.1,HeightMultiplier=1.1,GetAbsoluteCentre=() => mouseWheelArgs.Data.GetPosition(mainCanvas),GetScaleSensitiveCentre=() => Mouse.GetPosition(mainCanvas)}; /* {"IsRoot":false} */
            Data<UIElement> id_261d188e3ce64cc8a06f390ba51e092f = new Data<UIElement>() {Lambda=() => mainCanvas}; /* {"IsRoot":false} */
            Scale zoomOut = new Scale() {WidthMultiplier=0.9,HeightMultiplier=0.9,GetAbsoluteCentre=() => mouseWheelArgs.Data.GetPosition(mainCanvas),GetScaleSensitiveCentre=() => Mouse.GetPosition(mainCanvas)}; /* {"IsRoot":false} */
            DataFlowConnector<UIElement> id_843620b3a9ed45bea231b841b52e5621 = new DataFlowConnector<UIElement>() {}; /* {"IsRoot":false} */
            DataFlowConnector<UIElement> id_04c07393f532472792412d2a555510b9 = new DataFlowConnector<UIElement>() {}; /* {"IsRoot":false} */
            ApplyAction<UIElement> applyZoomEffects = new ApplyAction<UIElement>() {Lambda=input =>{    var transform = (input.RenderTransform as TransformGroup)?.Children.OfType<ScaleTransform>().FirstOrDefault();    if (transform == null)        return;    var minScale = 0.6; /*Logging.Log($"Scale: {transform.ScaleX}, {transform.ScaleX}");*/    bool nodeIsTooSmall = transform.ScaleX < minScale && transform.ScaleY < minScale;    var nodes = mainGraph.Nodes;    foreach (var node in nodes)    {        if (node is ALANode alaNode)            alaNode.ShowTypeTextMask(nodeIsTooSmall);    }}}; /* {"IsRoot":false} */
            MouseWheelEvent id_2a7c8f3b6b5e4879ad5a35ff6d8538fd = new MouseWheelEvent(eventName:"MouseWheel") {}; /* {"IsRoot":false} */
            Apply<MouseWheelEventArgs, bool> id_33990435606f4bbc9ba1786ed05672ab = new Apply<MouseWheelEventArgs, bool>() {Lambda=args =>{    return args.Delta > 0;}}; /* {"IsRoot":false} */
            IfElse id_6909a5f3b0e446d3bb0c1382dac1faa9 = new IfElse() {}; /* {"IsRoot":false} */
            DataFlowConnector<string> id_cf7df48ac3304a8894a7536261a3b474 = new DataFlowConnector<string>() {}; /* {"IsRoot":false} */
            DispatcherEvent id_4a268943755348b68ee2cb6b71f73c40 = new DispatcherEvent() {Priority=DispatcherPriority.ApplicationIdle}; /* {"IsRoot":false} */
            MenuItem id_a34c047df9ae4235a08b037fd9e48ab8 = new MenuItem(header:"Generate Code") {}; /* {"IsRoot":false} */
            GenerateALACode id_b5364bf1c9cd46a28e62bb2eb0e11692 = new GenerateALACode() {Graph=mainGraph}; /* {"IsRoot":false} */
            GetSetting id_a3efe072d6b44816a631d90ccef5b71e = new GetSetting(name:"ApplicationCodeFilePath") {}; /* {"IsRoot":false} */
            Data<string> id_fcfcb5f0ae544c968dcbc734ac1db51b = new Data<string>() {storedData=SETTINGS_FILEPATH}; /* {"IsRoot":true} */
            EditSetting id_f928bf426b204bc89ba97219c97df162 = new EditSetting() {JSONPath="$..ApplicationCodeFilePath"}; /* {"IsRoot":false} */
            Data<string> id_c01710b47a2a4deb824311c4dc46222d = new Data<string>() {storedData=SETTINGS_FILEPATH}; /* {"IsRoot":true} */
            Cast<string, object> id_f07ddae8b4ee431d8ede6c21e1fe01c5 = new Cast<string, object>() {}; /* {"IsRoot":false} */
            DataFlowConnector<string> setting_currentDiagramCodeFilePath = new DataFlowConnector<string>() {}; /* {"IsRoot":false} */
            Cast<string, object> id_460891130e9e499184b84a23c2e43c9f = new Cast<string, object>() {}; /* {"IsRoot":false} */
            Data<string> id_ecfbf0b7599e4340b8b2f79b7d1e29cb = new Data<string>() {storedData=SETTINGS_FILEPATH}; /* {"IsRoot":true} */
            Apply<Dictionary<string, List<string>>, IEnumerable<string>> id_92effea7b90745299826cd566a0f2b88 = new Apply<Dictionary<string, List<string>>, IEnumerable<string>>() {Lambda=input =>{    var list = new List<string>();    if (input.ContainsKey("Modules"))    {        list = input["Modules"];    }    return list;}}; /* {"IsRoot":false} */
            Data<string> id_c5fdc10d2ceb4577bef01977ee8e9dd1 = new Data<string>() {Lambda=() => setting_currentDiagramCodeFilePath.Data}; /* {"IsRoot":false} */
            FileReader id_72140c92ac4f4255abe9d149068fa16f = new FileReader() {}; /* {"IsRoot":false} */
            DataFlowConnector<string> id_1d55a1faa3dd4f78ad22ac73051f5d2d = new DataFlowConnector<string>() {}; /* {"IsRoot":false} */
            EventConnector generateCode = new EventConnector() {}; /* {"IsRoot":false} */
            EditSetting id_60229af56d92436996d2ee8d919083a3 = new EditSetting() {JSONPath="$..ProjectFolderPath"}; /* {"IsRoot":false} */
            Data<string> id_58c03e4b18bb43de8106a4423ca54318 = new Data<string>() {storedData=SETTINGS_FILEPATH}; /* {"IsRoot":true} */
            FileWriter id_2b42bd6059334bfabc3df1d047751d7a = new FileWriter() {}; /* {"IsRoot":false} */
            DataFlowConnector<string> id_b9865ebcd2864642a96573ced52bbb7f = new DataFlowConnector<string>() {}; /* {"IsRoot":false} */
            InsertFileCodeLines insertInstantiations = new InsertFileCodeLines() {StartLandmark=$"// BEGIN AUTO-GENERATED INSTANTIATIONS FOR {currentDiagramName.Data}",EndLandmark=$"// END AUTO-GENERATED INSTANTIATIONS FOR {currentDiagramName.Data}",Indent="            "}; /* {"IsRoot":false} */
            InsertFileCodeLines insertWireTos = new InsertFileCodeLines() {StartLandmark=$"// BEGIN AUTO-GENERATED WIRING FOR {currentDiagramName.Data}",EndLandmark=$"// END AUTO-GENERATED WIRING FOR {currentDiagramName.Data}",Indent="            "}; /* {"IsRoot":false} */
            EventConnector id_0e563f77c5754bdb8a75b7f55607e9b0 = new EventConnector() {}; /* {"IsRoot":false} */
            MenuItem id_96ab5fcf787a4e6d88af011f6e3daeae = new MenuItem(header:"Generics test") {}; /* {"IsRoot":false} */
            EventLambda id_026d2d87a422495aa46c8fc4bda7cdd7 = new EventLambda() {Lambda=() =>{    var node = mainGraph.Nodes.First() as ALANode;    node.Model.UpdateGeneric(0, "testType");}}; /* {"IsRoot":false} */
            Horizontal statusBarHorizontal = new Horizontal() {Margin=new Thickness(5)}; /* {"IsRoot":false} */
            Text globalMessageTextDisplay = new Text(text:"") {Height=20}; /* {"IsRoot":false} */
            EventLambda id_c4f838d19a6b4af9ac320799ebe9791f = new EventLambda() {Lambda=() =>{    Logging.MessageOutput += message => (globalMessageTextDisplay as IDataFlow<string>).Data = message;}}; /* {"IsRoot":false} */
            EventLambda id_5e77c28f15294641bb881592d2cd7ac9 = new EventLambda() {Lambda=() =>{    Logging.Message("Beginning code generation...");}}; /* {"IsRoot":false} */
            EventLambda id_3f30a573358d4fd08c4c556281737360 = new EventLambda() {Lambda=() =>{    var sb = new StringBuilder();    sb.Append($"[{DateTime.Now:h:mm:ss tt}] Completed code generation successfully");    if (!string.IsNullOrEmpty(currentDiagramName.Data))        sb.Append($" for diagram {currentDiagramName.Data}");    sb.Append("!");    Logging.Message(sb.ToString());}}; /* {"IsRoot":false} */
            ExtractALACode extractALACode = new ExtractALACode() {}; /* {"IsRoot":false} */
            KeyEvent CTRL_S_KeyPressed = new KeyEvent(eventName:"KeyDown") {Key=Key.S,Modifiers=new Key[]{Key.LeftCtrl}}; /* {"IsRoot":false} */
            MenuItem id_6f93680658e04f8a9ab15337cee1eca3 = new MenuItem(header:"Pull from code") {}; /* {"IsRoot":false} */
            FileReader id_9f411cfea16b45ed9066dd8f2006e1f1 = new FileReader() {}; /* {"IsRoot":false} */
            EventConnector id_db598ad59e5542a0adc5df67ced27f73 = new EventConnector() {}; /* {"IsRoot":false} */
            DataFlowConnector<string> id_9b866e4112fd4347a2a3e81441401dea = new DataFlowConnector<string>() {}; /* {"IsRoot":false} */
            GetSetting id_5ddd02478c734777b9e6f1079b4b3d45 = new GetSetting(name:"ApplicationCodeFilePath") {}; /* {"IsRoot":false} */
            Apply<string, bool> id_d5d3af7a3c9a47bf9af3b1a1e1246267 = new Apply<string, bool>() {Lambda=s => !string.IsNullOrEmpty(s)}; /* {"IsRoot":false} */
            IfElse id_2ce385b32256413ab2489563287afaac = new IfElse() {}; /* {"IsRoot":false} */
            DataFlowConnector<string> latestCodeFilePath = new DataFlowConnector<string>() {}; /* {"IsRoot":false} */
            DispatcherEvent id_dcd4c90552dc4d3fb579833da87cd829 = new DispatcherEvent() {Priority=DispatcherPriority.Loaded}; /* {"IsRoot":false} */
            EventLambda id_1e62a1e411c9464c94ee234dd9dd3fdc = new EventLambda() {Lambda=() =>{    createDiagramFromCode.Update = false;    layoutDiagram.InitialY = 50;}}; /* {"IsRoot":false} */
            MouseButtonEvent MiddleMouseButton_Pressed = new MouseButtonEvent(eventName:"MouseDown") {Condition=args => args.ChangedButton == MouseButton.Middle && args.ButtonState == MouseButtonState.Pressed}; /* {"IsRoot":false} */
            ApplyAction<object> id_d90fbf714f5f4fdc9b43cbe4d5cebf1c = new ApplyAction<object>() {Lambda=input =>{    (input as UIElement)?.Focus();    stateTransition.Update(Enums.DiagramMode.Idle);}}; /* {"IsRoot":false} */
            Horizontal mainHorizontal = new Horizontal() {Ratios=new[]{1, 3}}; /* {"IsRoot":false} */
            Horizontal sidePanelHoriz = new Horizontal(visible:true) {}; /* {"IsRoot":false} */
            Vertical id_987196dd20ab4721b0c193bb7a2064f4 = new Vertical() {Layouts=new int[]{2}}; /* {"IsRoot":false} */
            TabContainer id_7b250b222ca44ba2922547f03a4aef49 = new TabContainer() {}; /* {"IsRoot":false} */
            Tab directoryExplorerTab = new Tab(title:"Directory Explorer") {}; /* {"IsRoot":false} */
            MenuItem menu_View = new MenuItem(header:"View") {}; /* {"IsRoot":false} */
            Horizontal canvasDisplayHoriz = new Horizontal() {}; /* {"IsRoot":false} */
            DirectoryTree directoryTreeExplorer = new DirectoryTree() {FilenameFilter="*.cs",Height=700}; /* {"IsRoot":false} */
            Vertical id_e8a68acda2aa4d54add689bd669589d3 = new Vertical() {Layouts=new int[]{2, 0}}; /* {"IsRoot":false} */
            Horizontal projectDirectoryTreeHoriz = new Horizontal() {}; /* {"IsRoot":false} */
            Horizontal projectDirectoryOptionsHoriz = new Horizontal() {VertAlignment=VerticalAlignment.Bottom}; /* {"IsRoot":false} */
            Button id_0d4d34a2cd6749759ac0c2708ddf0cbc = new Button(title:"Open diagram from file") {}; /* {"IsRoot":false} */
            StateChangeListener id_08a51a5702e34a38af808db65a3a6eb3 = new StateChangeListener() {StateTransition=stateTransition,PreviousStateShouldMatch=Enums.DiagramMode.Any,CurrentStateShouldMatch=Enums.DiagramMode.Idle}; /* {"IsRoot":false} */
            EventConnector id_9d14914fdf0647bb8b4b20ea799e26c8 = new EventConnector() {}; /* {"IsRoot":false} */
            EventLambda unhighlightAllWires = new EventLambda() {Lambda=() =>{    var wires = mainGraph.Edges.OfType<ALAWire>();    foreach (var wire in wires)    {        wire.Deselect();    }}}; /* {"IsRoot":false} */
            DataFlowConnector<MouseWheelEventArgs> id_6d789ff1a0bc4a2d8e88733adc266be8 = new DataFlowConnector<MouseWheelEventArgs>() {}; /* {"IsRoot":false} */
            EventConnector id_a236bd13c516401eb5a83a451a875dd0 = new EventConnector() {}; /* {"IsRoot":false} */
            EventLambda id_6fdaaf997d974e30bbb7c106c40e997c = new EventLambda() {Lambda=() => createDiagramFromCode.Update = true}; /* {"IsRoot":false} */
            DataFlowConnector<object> latestAddedNode = new DataFlowConnector<object>() {}; /* {"IsRoot":false} */
            MenuItem id_86a7f0259b204907a092da0503eb9873 = new MenuItem(header:"Test DirectoryTree") {}; /* {"IsRoot":false} */
            FolderBrowser id_3710469340354a1bbb4b9d3371c9c012 = new FolderBrowser() {}; /* {"IsRoot":false} */
            DirectoryTree testDirectoryTree = new DirectoryTree() {}; /* {"IsRoot":false} */
            MenuItem testSimulateKeyboard = new MenuItem(header:"Test SimulateKeyboard") {}; /* {"IsRoot":false} */
            SimulateKeyboard id_5c31090d2c954aa7b4a10e753bdfc03a = new SimulateKeyboard() {Keys="HELLO".Select(c => c.ToString()).ToList(),Modifiers=new List<string>(){"SHIFT"}}; /* {"IsRoot":false} */
            EventConnector id_52b8f2c28c2e40cabedbd531171c779a = new EventConnector() {}; /* {"IsRoot":false} */
            SimulateKeyboard id_86ecd8f953324e34adc6238338f75db5 = new SimulateKeyboard() {Keys=new List<string>(){"COMMA", "SPACE"}}; /* {"IsRoot":false} */
            SimulateKeyboard id_63e463749abe41d28d05b877479070f8 = new SimulateKeyboard() {Keys="WORLD".Select(c => c.ToString()).ToList(),Modifiers=new List<string>(){"SHIFT"}}; /* {"IsRoot":false} */
            SimulateKeyboard id_66e516b6027649e1995a531d03c0c518 = new SimulateKeyboard() {Keys=new List<string>(){"1"},Modifiers=new List<string>(){"SHIFT"}}; /* {"IsRoot":false} */
            KeyEvent CTRL_C_KeyPressed = new KeyEvent(eventName:"KeyDown") {Condition=args => stateTransition.CurrentStateMatches(Enums.DiagramMode.IdleSelected),Key=Key.C,Modifiers=new Key[]{Key.LeftCtrl}}; /* {"IsRoot":false} */
            Data<AbstractionModel> cloneSelectedNodeModel = new Data<AbstractionModel>() {Lambda=() =>{    var selectedNode = mainGraph.Get("SelectedNode") as ALANode;    if (selectedNode == null)        return null;    var baseModel = selectedNode.Model;    var clone = new AbstractionModel(baseModel);    return clone;}}; /* {"IsRoot":false} */
            ApplyAction<AbstractionModel> id_0f802a208aad42209777c13b2e61fe56 = new ApplyAction<AbstractionModel>() {Lambda=input =>{    if (input == null)    {        Logging.Message("Nothing was copied.", timestamp: true);    }    else    {        mainGraph.Set("ClonedModel", input);        Logging.Message($"Copied {input} successfully.", timestamp: true);    }}}; /* {"IsRoot":false} */
            KeyEvent CTRL_V_KeyPressed = new KeyEvent(eventName:"KeyUp") {Condition=args => stateTransition.CurrentStateMatches(Enums.DiagramMode.IdleSelected),Key=Key.V,Modifiers=new Key[]{Key.LeftCtrl}}; /* {"IsRoot":false} */
            ConvertToEvent<object> id_8647cbf4ac4049a99204b0e3aa70c326 = new ConvertToEvent<object>() {}; /* {"IsRoot":false} */
            EventConnector id_5a22e32e96e641d49c6fb4bdf6fcd94b = new EventConnector() {}; /* {"IsRoot":false} */
            EventLambda id_36c5f05380b04b378de94534411f3f88 = new EventLambda() {Lambda=() =>{    var clonedModel = mainGraph.Get("ClonedModel") as AbstractionModel;    var latestNode = latestAddedNode.Data as ALANode;    if (latestNode == null)        return;    var model = latestNode?.Model;    if (model == null)        return;    model.CloneFrom(clonedModel);    latestNode.UpdateUI();    latestNode.RefreshParameterRows(removeEmptyRows: true);    latestNode.ChangeTypeInUI(clonedModel.Type);    latestNode.FocusOnTypeDropDown();}}; /* {"IsRoot":false} */
            DispatcherEvent id_0945b34f58a146ff983962f595f57fb2 = new DispatcherEvent() {}; /* {"IsRoot":false} */
            ApplyAction<KeyEventArgs> id_4341066281bc4015a668a3bbbcb7256b = new ApplyAction<KeyEventArgs>() {Lambda=args => args.Handled = true}; /* {"IsRoot":false} */
            DataFlowConnector<AbstractionModel> id_024b1810c2d24db3b9fac1ccce2fad9e = new DataFlowConnector<AbstractionModel>() {}; /* {"IsRoot":false} */
            MenuItem id_2c933997055b4122bdb77945f1abb560 = new MenuItem(header:"Test reset canvas on root") {}; /* {"IsRoot":false} */
            Data<ALANode> id_0eea701e0bc84c42a9f17ccc200ef2ef = new Data<ALANode>() {Lambda=() => mainGraph?.Roots.FirstOrDefault() as ALANode}; /* {"IsRoot":false} */
            ApplyAction<ALANode> resetViewOnNode = new ApplyAction<ALANode>() {Lambda=node =>{    if (node == null)        return;    var render = node.Render;    var renderPosition = new Point(WPFCanvas.GetLeft(render), WPFCanvas.GetTop(render));    var windowWidth = UIConfig_canvasDisplayHoriz.ActualWidth;    var windowHeight = UIConfig_canvasDisplayHoriz.ActualHeight;    var centre = new Point(windowWidth / 2 - 20, windowHeight / 2 - 20);    WPFCanvas.SetLeft(mainCanvas, -renderPosition.X + centre.X / 2);    WPFCanvas.SetTop(mainCanvas, -renderPosition.Y + centre.Y);}}; /* {"IsRoot":false} */
            MenuItem id_29ed401eb9c240d98bf5c6d1f00c5c76 = new MenuItem(header:"Test reset canvas on selected node") {}; /* {"IsRoot":false} */
            Data<ALANode> id_fa857dd7432e406c8c6c642152b37730 = new Data<ALANode>() {Lambda=() => mainGraph.Get("SelectedNode") as ALANode}; /* {"IsRoot":false} */
            ConvertToEvent<Tuple<string, List<string>>> id_409be365df274cc6a7a124e8a80316a5 = new ConvertToEvent<Tuple<string, List<string>>>() {}; /* {"IsRoot":false} */
            Data<UIElement> id_5e2f0621c62142c1b5972961c93cb725 = new Data<UIElement>() {Lambda=() => mainCanvas}; /* {"IsRoot":false} */
            Scale resetScale = new Scale() {AbsoluteScale=1,Reset=true}; /* {"IsRoot":false} */
            EventConnector id_82b26eeaba664ee7b2a2c0682e25ce08 = new EventConnector() {}; /* {"IsRoot":false} */
            DataFlowConnector<UIElement> id_57e7dd98a0874e83bbd5014f7e9c9ef5 = new DataFlowConnector<UIElement>() {}; /* {"IsRoot":false} */
            ApplyAction<UIElement> id_e1e6cf54f73d4f439c6f18b668a73f1a = new ApplyAction<UIElement>() {Lambda=canvas =>{    WPFCanvas.SetLeft(canvas, 0);    WPFCanvas.SetTop(canvas, 0);}}; /* {"IsRoot":false} */
            Tab searchTab = new Tab(title:"Search") {}; /* {"IsRoot":false} */
            Horizontal id_fed56a4aef6748178fa7078388643323 = new Horizontal() {}; /* {"IsRoot":false} */
            TextBox searchTextBox = new TextBox() {}; /* {"IsRoot":false} */
            Button startSearchButton = new Button(title:"Search") {}; /* {"IsRoot":false} */
            DataFlowConnector<string> id_00b0ca72bbce4ef4ba5cf395c666a26e = new DataFlowConnector<string>() {}; /* {"IsRoot":false} */
            Data<string> id_5da1d2f5b13746f29802078592e59346 = new Data<string>() {}; /* {"IsRoot":false} */
            Vertical id_cc0c82a2157f4b0291c812236a6e45ba = new Vertical() {}; /* {"IsRoot":false} */
            ListDisplay id_3622556a1b37410691b51b83c004a315 = new ListDisplay() {ItemList=nodeSearchTextResults}; /* {"IsRoot":false} */
            Apply<int, ALANode> id_73274d9ce8d5414899772715a1d0f266 = new Apply<int, ALANode>() {Lambda=index =>{    var results = nodeSearchResults;    if (results.Count > index)    {        return results[index];    }    else    {        return null;    }}}; /* {"IsRoot":false} */
            DataFlowConnector<ALANode> id_fff8d82dbdd04da18793108f9b8dd5cf = new DataFlowConnector<ALANode>() {}; /* {"IsRoot":false} */
            ConvertToEvent<ALANode> id_75ecf8c2602c41829602707be8a8a481 = new ConvertToEvent<ALANode>() {}; /* {"IsRoot":false} */
            ApplyAction<ALANode> navigateToNode = new ApplyAction<ALANode>() {Lambda=selectedNode =>{    var nodes = mainGraph.Nodes.OfType<ALANode>();    foreach (var node in nodes)    {        node.Deselect();        node.ShowTypeTextMask(show: false);    }    selectedNode.FocusOnTypeDropDown();    selectedNode.HighlightNode();}}; /* {"IsRoot":false} */
            Apply<string, IEnumerable<ALANode>> id_5f1c0f0187eb4dc99f15254fd36fa9b6 = new Apply<string, IEnumerable<ALANode>>() {Lambda=searchQuery =>{    nodeSearchResults.Clear();    nodeSearchTextResults.Clear();    return mainGraph.Nodes.OfType<ALANode>();}}; /* {"IsRoot":false} */
            ForEach<ALANode> id_8e347b7f5f3b4aa6b1c8f1966d0280a3 = new ForEach<ALANode>() {}; /* {"IsRoot":false} */
            DataFlowConnector<ALANode> id_282744d2590b4d3e8b337d73c05e0823 = new DataFlowConnector<ALANode>() {}; /* {"IsRoot":false} */
            DataFlowConnector<int> currentSearchResultIndex = new DataFlowConnector<int>() {}; /* {"IsRoot":false} */
            ApplyAction<ALANode> id_2c9472651f984aa8ab763f327bcfa45e = new ApplyAction<ALANode>() {Lambda=node =>{    var i = currentSearchResultIndex.Data;    var total = mainGraph.Nodes.Count;    Logging.Message($"Searching node {i + 1}/{total}...");}}; /* {"IsRoot":false} */
            DataFlowConnector<string> currentSearchQuery = new DataFlowConnector<string>() {}; /* {"IsRoot":false} */
            DispatcherData<ALANode> id_1c95fb3a139b4602bba7b10201112546 = new DispatcherData<ALANode>() {}; /* {"IsRoot":false} */
            DispatcherData<ALANode> id_01bdd051f2034331bd9f121029b0e2e8 = new DispatcherData<ALANode>() {}; /* {"IsRoot":false} */
            ApplyAction<ALANode> id_67bc4eb50bb04d9694a1a0d5ce65c9d9 = new ApplyAction<ALANode>() {Lambda=node =>{    var query = currentSearchQuery.Data;    var caseSensitive = false;    var searchName = searchFilterNameChecked.Data;    var searchType = searchFilterTypeChecked.Data;    var searchInstanceName = searchFilterInstanceNameChecked.Data;    var searchProperties = searchFilterFieldsAndPropertiesChecked.Data;    if (node.IsMatch(query, caseSensitive, searchName, searchType, searchInstanceName, searchProperties))    {        nodeSearchResults.Add(node);        nodeSearchTextResults.Add($"{node.Model.FullType} {node.Model.Name}");    }    var currentIndex = currentSearchResultIndex.Data;    var total = mainGraph.Nodes.Count;    if (currentIndex == (total - 1))        Logging.Message($"Found {nodeSearchResults.Count} search results for \"{query}\"");}}; /* {"IsRoot":false} */
            MenuItem id_f526f560b3504a0b8115879e5d5354ff = new MenuItem(header:"Test ContextMenu") {}; /* {"IsRoot":false} */
            ContextMenu id_dea56e5fd7174cd7983e8f2c837a941b = new ContextMenu() {}; /* {"IsRoot":false} */
            UIConfig directoryExplorerConfig = new UIConfig() {}; /* {"IsRoot":false} */
            DataFlowConnector<string> currentSelectedDirectoryTreeFilePath = new DataFlowConnector<string>() {}; /* {"IsRoot":false} */
            MenuItem id_8b908f2be6094d5b8cd3dce5c5fc2b8b = new MenuItem(header:"Open code file") {}; /* {"IsRoot":false} */
            Data<string> id_692716a735e44e948a8d14cd550c1276 = new Data<string>() {}; /* {"IsRoot":false} */
            KeyEvent F_KeyPressed = new KeyEvent(eventName:"KeyDown") {Key=Key.F,Modifiers=new Key[]{Key.LeftCtrl}}; /* {"IsRoot":false} */
            EventConnector id_87a897a783884990bf10e4d7a9e276b9 = new EventConnector() {}; /* {"IsRoot":false} */
            DispatcherEvent id_9e6a74b0dbea488cba6027ee5187ad0f = new DispatcherEvent() {Priority=DispatcherPriority.Loaded}; /* {"IsRoot":false} */
            DispatcherEvent id_b55e77a5d78243bf9612ecb7cb20c2c7 = new DispatcherEvent() {Priority=DispatcherPriority.Loaded}; /* {"IsRoot":false} */
            DispatcherEvent id_45593aeb91a145aa9d84d8b77a8d4d8e = new DispatcherEvent() {Priority=DispatcherPriority.Loaded}; /* {"IsRoot":false} */
            UIConfig UIConfig_searchTab = new UIConfig() {}; /* {"IsRoot":false} */
            UIConfig UIConfig_searchTextBox = new UIConfig() {}; /* {"IsRoot":false} */
            EventLambda id_a690d6dd37ba4c98b5506777df6dc9db = new EventLambda() {Lambda=() =>{    UIConfig_searchTab.Focus();}}; /* {"IsRoot":false} */
            EventLambda id_63db7722e48a4c5aabd905f75b0519b2 = new EventLambda() {Lambda=() =>{    UIConfig_searchTextBox.Focus();}}; /* {"IsRoot":false} */
            EventConnector id_006b07cc90c64e398b945bb43fdd4de9 = new EventConnector() {}; /* {"IsRoot":false} */
            Data<string> id_e7da19475fcc44bdaf4a64d05f92b771 = new Data<string>() {}; /* {"IsRoot":false} */
            PopupWindow id_68cfe1cc12f948cab25289d853300813 = new PopupWindow(title:"Open diagram?") {Height=100,Resize=SizeToContent.WidthAndHeight}; /* {"IsRoot":false} */
            Vertical id_95ddd89b36d54db298eaa05165284569 = new Vertical() {}; /* {"IsRoot":false} */
            Text id_939726bef757459b914412aead1bb5f9 = new Text(text:"") {}; /* {"IsRoot":false} */
            Horizontal id_c7dc32a5f12b41ad94a910a74de38827 = new Horizontal() {}; /* {"IsRoot":false} */
            UIConfig id_89ab09564cea4a8b93d8925e8234e44c = new UIConfig() {Width=50,HorizAlignment="right",RightMargin=5,BottomMargin=5}; /* {"IsRoot":false} */
            UIConfig id_c180a82fd3a6495a885e9dde61aaaef3 = new UIConfig() {Width=50,HorizAlignment="left",RightMargin=5,BottomMargin=5}; /* {"IsRoot":false} */
            Button id_add742a4683f4dd0b34d8d0eebbe3f07 = new Button(title:"Yes") {}; /* {"IsRoot":false} */
            Button id_e82c1f80e1884a57b79c681462efd65d = new Button(title:"No") {}; /* {"IsRoot":false} */
            EventConnector id_5fbec6b061cc428a8c00e5c2a652b89e = new EventConnector() {}; /* {"IsRoot":false} */
            EventConnector id_b0d86bb898944ded83ec7f58b9f4a1b8 = new EventConnector() {}; /* {"IsRoot":false} */
            Data<string> id_721b5692fa5a4ba39f509fd7e4a6291b = new Data<string>() {}; /* {"IsRoot":false} */
            EditSetting id_1928c515b2414f6690c6924a76461081 = new EditSetting() {JSONPath="$..ApplicationCodeFilePath"}; /* {"IsRoot":false} */
            Data<object> id_1a403a85264c4074bc7ce5a71262c6c0 = new Data<object>() {storedData=""}; /* {"IsRoot":false} */
            Horizontal id_de49d2fafc2140e996eb38fbf1e62103 = new Horizontal() {}; /* {"IsRoot":false} */
            Horizontal id_d890df432c1f4e60a62b8913a5069b34 = new Horizontal() {}; /* {"IsRoot":false} */
            Apply<string, string> id_e4c9f92bbd6643a286683c9ff5f9fb3a = new Apply<string, string>() {Lambda=path => $"Default code file path is set to \"{path}\".\nOpen a diagram from this path?"}; /* {"IsRoot":false} */
            UIConfig id_5b134e68e31b40f4b3e95eb007a020dc = new UIConfig() {HorizAlignment="middle",UniformMargin=5}; /* {"IsRoot":false} */
            UIConfig id_0fafdba1ad834904ac7330f95dffd966 = new UIConfig() {HorizAlignment="left",BottomMargin=5}; /* {"IsRoot":false} */
            Button id_2bfcbb47c2c745578829e1b0f8287f42 = new Button(title:" No, and clear the setting ") {}; /* {"IsRoot":false} */
            EventConnector id_1139c3821d834efc947d5c4e949cd1ba = new EventConnector() {}; /* {"IsRoot":false} */
            Horizontal id_4686253b1d7d4cd9a4d5bf03d6b7e380 = new Horizontal() {}; /* {"IsRoot":false} */
            Data<string> id_f140e9e4ef3f4c07898073fde207da99 = new Data<string>() {storedData=SETTINGS_FILEPATH}; /* {"IsRoot":true} */
            UIConfig id_25a53022f6ab4e9284fd321e9535801b = new UIConfig() {MaxHeight=700}; /* {"IsRoot":false} */
            UIConfig id_de10db4d6b8a426ba76b02959a58cb88 = new UIConfig() {HorizAlignment="middle",UniformMargin=5}; /* {"IsRoot":false} */
            MenuItem id_a9db513fb0e749bda7f42b03964e5dce = new MenuItem(header:"Code to Diagram") {}; /* {"IsRoot":false} */
            MenuItem id_efeb87ef1b3c4f9e8ed2f8193e6b78b1 = new MenuItem(header:"Diagram to Code") {}; /* {"IsRoot":false} */
            EventConnector startDiagramCreationProcess = new EventConnector() {}; /* {"IsRoot":false} */
            EventLambda id_db77c286e64241c48de4fad0dde80024 = new EventLambda() {Lambda=() =>{    mainGraph.Clear();    mainCanvas.Children.Clear();    insertInstantiations.StartLandmark = extractALACode.Landmarks[0];    insertInstantiations.EndLandmark = extractALACode.Landmarks[1];    insertWireTos.StartLandmark = extractALACode.Landmarks[2];    insertWireTos.EndLandmark = extractALACode.Landmarks[3];}}; /* {"IsRoot":false} */
            Data<string> id_c9dbe185989e48c0869f984dd8e979f2 = new Data<string>() {Lambda=() =>{    if (!string.IsNullOrEmpty(setting_currentDiagramCodeFilePath.Data))    {        return setting_currentDiagramCodeFilePath.Data;    }    else    {        return latestCodeFilePath.Data;    }}}; /* {"IsRoot":false} */
            DataFlowConnector<string> id_17609c775b9c4dfcb1f01d427d2911ae = new DataFlowConnector<string>() {}; /* {"IsRoot":false} */
            Apply<string, string> id_e778c13b2c894113a7aff7ecfffe48f7 = new Apply<string, string>() {Lambda=path =>{    var sb = new StringBuilder();    if (!string.IsNullOrEmpty(currentDiagramName.Data))    {        sb.Append($"{currentDiagramName.Data} | ");    }    var fullPath = Path.GetFullPath(path);    if (!string.IsNullOrEmpty(fullPath))    {        sb.Append($"{fullPath}");    }    return sb.ToString();}}; /* {"IsRoot":false} */
            UIConfig id_e3837af93b584ca9874336851ff0cd31 = new UIConfig() {HorizAlignment="left"}; /* {"IsRoot":false} */
            UIConfig id_5c857c3a1a474ec19c0c3b054627c0a9 = new UIConfig() {HorizAlignment="right"}; /* {"IsRoot":false} */
            Text globalVersionNumberDisplay = new Text(text:$"v{VERSION_NUMBER}") {}; /* {"IsRoot":false} */
            MenuItem id_053e6b41724c4dcaad0b79b8924d647d = new MenuItem(header:"Check for Updates") {}; /* {"IsRoot":false} */
            ForEach<string> id_20566090f5054429aebed4d371c2a613 = new ForEach<string>() {}; /* {"IsRoot":false} */
            DataFlowConnector<string> id_97b81fc9cc04423192a12822a5a5a32e = new DataFlowConnector<string>() {}; /* {"IsRoot":false} */
            CodeParser id_cad49d55268145ab87788c650c6c5473 = new CodeParser() {}; /* {"IsRoot":false} */
            ForEach<string> id_84cf83e5511c4bcb8f83ad289d20b08d = new ForEach<string>() {}; /* {"IsRoot":false} */
            Collection<string> availableProgrammingParadigms = new Collection<string>() {OutputLength=-2,OutputOnEvent=true}; /* {"IsRoot":false} */
            ApplyAction<List<string>> id_16d8fb2a48ea4eef8839fc7aba053476 = new ApplyAction<List<string>>() {Lambda=input => abstractionModelManager.ProgrammingParadigms = input}; /* {"IsRoot":false} */
            Cast<List<string>, IEnumerable<string>> id_6625f976171c480ebd8b750aeaf4fab1 = new Cast<List<string>, IEnumerable<string>>() {}; /* {"IsRoot":false} */
            FileReader id_4577a8f0f63b4772bdc4eb4cb8581070 = new FileReader() {}; /* {"IsRoot":false} */
            CodeParser id_d920e0f3fa2d4872af1ec6f3c058c233 = new CodeParser() {}; /* {"IsRoot":false} */
            DataFlowConnector<IEnumerable<string>> id_670ce4df65564e07912ef2ce63c38e11 = new DataFlowConnector<IEnumerable<string>>() {}; /* {"IsRoot":false} */
            EventLambda id_9240933e26ea4cfdb07e6e7252bf7576 = new EventLambda() {Lambda=() =>{    layoutDiagram.InitialY = layoutDiagram.LatestY;}}; /* {"IsRoot":false} */
            EventLambda id_afc4400ecf8b4f3e9aa1a57c346c80b2 = new EventLambda() {Lambda=() =>{    var edges = mainGraph.Edges;    foreach (var edge in edges)    {        (edge as ALAWire)?.Refresh();    }}}; /* {"IsRoot":false} */
            EventConnector id_2996cb469c4442d08b7e5ca2051336b1 = new EventConnector() {}; /* {"IsRoot":false} */
            Data<string> id_846c10ca3cc14138bea1d681b146865a = new Data<string>() {Lambda=() => extractALACode.CurrentDiagramName}; /* {"IsRoot":false} */
            Data<string> id_b6f2ab59cd0642afaf0fc124e6f9f055 = new Data<string>() {}; /* {"IsRoot":false} */
            MenuItem id_4aff82900db2498e8b46be4a18b9fa8e = new MenuItem(header:"Open User Guide") {}; /* {"IsRoot":false} */
            EventLambda id_322828528d644ff883d8787c8fb63e56 = new EventLambda() {Lambda=() =>{    Process.Start("https://github.com/arnab-sen/GALADE/wiki");}}; /* {"IsRoot":false} */
            UIConfig UIConfig_debugMainMenuItem = new UIConfig() {Visible=showDebugMenu}; /* {"IsRoot":false} */
            CheckBox id_cc3adf40cb654337b01f77ade1881b44 = new CheckBox(check:true) {}; /* {"IsRoot":false} */
            EventConnector id_a61fc923019942cea819e1b8d1b10384 = new EventConnector() {}; /* {"IsRoot":false} */
            MenuItem menu_ShowSidePanel = new MenuItem(header:"Show Side Panel") {}; /* {"IsRoot":false} */
            Cast<object, ALANode> id_8b99ce9b4c97466983fc1b14ef889ee8 = new Cast<object, ALANode>() {}; /* {"IsRoot":false} */
            MenuItem id_024172dbe8e2496b97e191244e493973 = new MenuItem(header:"Jump to selected wire's source") {}; /* {"IsRoot":false} */
            Data<ALANode> id_7e64ef3262604943a2b4a086c5641d09 = new Data<ALANode>() {Lambda=() => (mainGraph.Get("SelectedWire") as ALAWire)?.Source}; /* {"IsRoot":false} */
            ConditionalData<ALANode> id_35947f28d1454366ad8ac16e08020905 = new ConditionalData<ALANode>() {Condition=input => input != null}; /* {"IsRoot":false} */
            MenuItem id_269ffcfe56874f4ba0876a93071234ae = new MenuItem(header:"Jump to selected wire's destination") {}; /* {"IsRoot":false} */
            Data<ALANode> id_40173af405c9467bbc85c79a05b9da48 = new Data<ALANode>() {Lambda=() => (mainGraph.Get("SelectedWire") as ALAWire)?.Destination}; /* {"IsRoot":false} */
            UIConfig id_72e0f3f39c364bedb36a74a011e08747 = new UIConfig() {HorizAlignment="left"}; /* {"IsRoot":false} */
            Horizontal id_0fd8aa1777474e3cafb81088519f3d97 = new Horizontal() {}; /* {"IsRoot":false} */
            CheckBox id_57dc97beb4024bf294c44fea26cc5c89 = new CheckBox(check:true) {}; /* {"IsRoot":false} */
            Text id_b6275330bff140168f4e68c87ed31b54 = new Text(text:"InstanceName") {}; /* {"IsRoot":false} */
            UIConfig id_ecd9f881354d40f485c3fadd9f577974 = new UIConfig() {UniformMargin=2}; /* {"IsRoot":false} */
            Text id_889bfe8dee4d447d8ea45c19feaf5ca2 = new Text(text:"Filters:") {}; /* {"IsRoot":false} */
            CheckBox id_abe0267c9c964e2194aa9c5bf84ac413 = new CheckBox(check:true) {}; /* {"IsRoot":false} */
            Text id_edcc6a4999a24fc2ae4b190c5619351c = new Text(text:"Fields/Properties") {}; /* {"IsRoot":false} */
            CheckBox id_6dd83767dc324c1bb4e34beafaac11fe = new CheckBox(check:true) {}; /* {"IsRoot":false} */
            CheckBox id_7daf6ef76444402d9e9c6ed68f97a6c2 = new CheckBox(check:true) {}; /* {"IsRoot":false} */
            Text id_0e0c54964c4641d2958e710121d0429a = new Text(text:"Type") {}; /* {"IsRoot":false} */
            Text id_39ae7418fea245fcaebd3a49b00d0683 = new Text(text:"Name") {}; /* {"IsRoot":false} */
            UIConfig id_cbdc03ac56ac4f179dd49e1312d7dca0 = new UIConfig() {UniformMargin=2}; /* {"IsRoot":false} */
            UIConfig id_b868797a5ef6468abe35342f796a7376 = new UIConfig() {UniformMargin=2}; /* {"IsRoot":false} */
            UIConfig id_c5fa777bee784429982813fd34ee9437 = new UIConfig() {UniformMargin=2}; /* {"IsRoot":false} */
            UIConfig id_48456b7bb4cf40769ea65b77f071a7f8 = new UIConfig() {UniformMargin=2}; /* {"IsRoot":false} */
            UIConfig UIConfig_mainCanvasDisplay = new UIConfig() {AllowDrop=true}; /* {"IsRoot":false} */
            DragEvent id_dd7bf35a9a7c42059c340c211b761af9 = new DragEvent(eventName:"Drop") {}; /* {"IsRoot":false} */
            Apply<DragEventArgs, List<string>> getDroppedFilePaths = new Apply<DragEventArgs, List<string>>() {Lambda=args =>{    var listOfFilePaths = new List<string>();    if (args.Data.GetDataPresent(DataFormats.FileDrop))    {        listOfFilePaths.AddRange((string[])args.Data.GetData(DataFormats.FileDrop));    }    return listOfFilePaths;}}; /* {"IsRoot":false} */
            Apply<List<string>, List<string>> addAbstractionsToAllNodes = new Apply<List<string>, List<string>>() {Lambda=paths =>{    var newModels = new List<AbstractionModel>();    foreach (var path in paths)    {        var model = abstractionModelManager.CreateAbstractionModelFromPath(path);        if (model != null)            newModels.Add(model);    }    var newModelTypes = newModels.Select(m => m.Type).Where(t => !availableAbstractions.Contains(t)).OrderBy(s => s).ToList();    var nodes = mainGraph.Nodes.OfType<ALANode>();    foreach (var node in nodes)    {        node.AvailableAbstractions.AddRange(newModelTypes);    }    availableAbstractions.AddRange(newModelTypes);    return newModelTypes;}}; /* {"IsRoot":false} */
            DataFlowConnector<List<string>> id_efd2a2dc177542c587c73a55def6fe3c = new DataFlowConnector<List<string>>() {}; /* {"IsRoot":false} */
            Apply<List<string>, string> id_3e341111f8224aa7b947f522ef1f65ab = new Apply<List<string>, string>() {Lambda=modelNames =>{    var sb = new StringBuilder();    sb.Append($"Successfully added {modelNames.Count} new abstraction types");    if (modelNames.Count == 0)    {        sb.Clear();        sb.Append("Error: No new abstraction types were added.");        sb.Append(" Please check if the desired types already exist by viewing any node's type dropdown.");        return sb.ToString();    }    else    {        sb.Append(": ");    }    var maxNames = 10;    sb.Append(modelNames.First());    var counter = 1;    foreach (var name in modelNames.Skip(1))    {        counter++;        if (counter > maxNames)        {            sb.Append(", ...");            return sb.ToString();        }        sb.Append($", {name}");    }    return sb.ToString();}}; /* {"IsRoot":false} */
            ApplyAction<string> updateStatusMessage = new ApplyAction<string>() {Lambda=message => Logging.Message(message)}; /* {"IsRoot":false} */
            EventConnector id_0718ee88fded4b7b88258796df7db577 = new EventConnector() {}; /* {"IsRoot":false} */
            HttpRequest id_c359484e1d7147a09d63c0671fa5f1dd = new HttpRequest(url:"https://api.github.com/repos/arnab-sen/GALADE/releases/latest") {UserAgent=$"GALADE v{VERSION_NUMBER}",requestMethod=HttpMethod.Get}; /* {"IsRoot":false} */
            JSONParser id_db35acd5215c41849c685c49fba07a3d = new JSONParser() {JSONPath="$..tag_name"}; /* {"IsRoot":false} */
            Apply<string, bool> compareVersionNumbers = new Apply<string, bool>() {Lambda=version => version == $"v{VERSION_NUMBER}" || string.IsNullOrEmpty(version)}; /* {"IsRoot":false} */
            IfElse id_e33aaa2a4a5544a89931f05048e68406 = new IfElse() {}; /* {"IsRoot":false} */
            Text id_b47ca3c51c95416383ba250af31ee564 = new Text(text:" | Latest version unknown - please check for updates") {}; /* {"IsRoot":false} */
            Text id_07f10e1650504d298bdceddff2402f31 = new Text(text:"") {}; /* {"IsRoot":false} */
            Horizontal id_66a3103c3adc426fbc8473b66a8b0d22 = new Horizontal() {}; /* {"IsRoot":false} */
            Text id_b1a5dcbe40654113b08efc4299c6fdc2 = new Text(text:"") {}; /* {"IsRoot":false} */
            Clock id_ae21c0350891480babdcd1efcb247295 = new Clock() {Period=1000 * 60 * 30,SendInitialPulse=versionCheckSendInitialPulse}; /* {"IsRoot":false} */
            Data<string> id_34c59781fa2f4c5fb9102b7a65c461a0 = new Data<string>() {storedData=" | Up to date"}; /* {"IsRoot":false} */
            EventConnector id_a46f4ed8460e421b97525bd352b58d85 = new EventConnector() {}; /* {"IsRoot":false} */
            Data<string> id_0e88688a360d451ab58c2fa25c9bf109 = new Data<string>() {Lambda=() => $" - Last checked at {Utilities.GetCurrentTime(includeDate: false)}"}; /* {"IsRoot":false} */
            EventConnector id_57972aa4bbc24e46b4b6171637d31440 = new EventConnector() {}; /* {"IsRoot":false} */
            Data<string> id_76de2a3c1e5f4fbbbe8928be48e25847 = new Data<string>() {Lambda=() => $" | Update available ({latestVersion.Data})",storedData=$" | Update available ({latestVersion.Data})"}; /* {"IsRoot":false} */
            EventConnector id_cdeb94e2daee4057966eba31781ebd0d = new EventConnector() {}; /* {"IsRoot":false} */
            EventLambda id_45968f4d70794b7c994c8e0f6ee5093a = new EventLambda() {Lambda=() =>{    abstractionModelManager.ClearAbstractions();    availableAbstractions?.Clear();}}; /* {"IsRoot":false} */
            MenuItem id_8ebb92deea4c4abf846371db834d9f87 = new MenuItem(header:"Open Releases page") {}; /* {"IsRoot":false} */
            EventLambda id_835b587c7faf4fabbbe71010d28d9280 = new EventLambda() {Lambda=() => Process.Start("https://github.com/arnab-sen/GALADE/releases")}; /* {"IsRoot":false} */
            MenuItem id_3a7125ae5c814928a55c2d29e7e8c132 = new MenuItem(header:"Use depth-first layout") {}; /* {"IsRoot":false} */
            CheckBox id_11418b009831455983cbc07c8d116a1f = new CheckBox() {}; /* {"IsRoot":false} */
            ApplyAction<bool> id_ce0bcc39dd764d1087816b79eefa76bf = new ApplyAction<bool>() {Lambda=isChecked =>{}}; /* {"IsRoot":false} */
            EventConnector id_f8930a779bd44b0792fbd4a43b3874c6 = new EventConnector() {}; /* {"IsRoot":false} */
            MenuItem id_943e3971561d493d97e38a8e29fb87dc = new MenuItem(header:"Use automatic layout when rewiring") {}; /* {"IsRoot":false} */
            CheckBox id_954c2d01269c4632a4ddccd75cde9fde = new CheckBox(check:true) {}; /* {"IsRoot":false} */
            EventConnector id_cd6186e0fe844be586191519012bb72e = new EventConnector() {}; /* {"IsRoot":false} */
            Data<bool> id_0f0046b6b91e447aa9bf0a223fd59038 = new Data<bool>() {}; /* {"IsRoot":false} */
            IfElse id_edd3648585f44954b2df337f1b7a793b = new IfElse() {}; /* {"IsRoot":false} */
            EventLambda initialiseRightTreeLayout = new EventLambda() {Lambda=() =>{    layoutDiagram.InitialY = 50;    layoutDiagram.Roots = mainGraph.Roots.OfType<ALANode>().ToList();    layoutDiagram.AllNodes = mainGraph.Nodes.OfType<ALANode>().ToList();}}; /* {"IsRoot":false} */
            UIConfig id_50349b82433f42ebb9d1ce591fc3bc35 = new UIConfig() {ToolTipText="Uncheck to stop the diagram from rewiring whenever wires change source/destination, however automatic laying out will still occur when a new node is added.\nIf you wish to add a node without the diagram automatically laying out, use right click > Add Root to add a node at the current mouse position, with this unchecked."}; /* {"IsRoot":false} */
            Data<bool> id_27ff7a25d9034a45a229edef6610e214 = new Data<bool>() {storedData=true}; /* {"IsRoot":false} */
            DataFlowConnector<bool> id_d5c22176b9bb49dd91a1cb0a7e3f7196 = new DataFlowConnector<bool>() {}; /* {"IsRoot":false} */
            DataFlowConnector<bool> useAutomaticLayout = new DataFlowConnector<bool>() {Data=true}; /* {"IsRoot":false} */
            UIConfig id_87a535a0e11441af9072d6364a8aef74 = new UIConfig() {}; /* {"IsRoot":false} */
            EventConnector id_7356212bcc714c699681e8dffc853761 = new EventConnector() {}; /* {"IsRoot":false} */
            Data<Dictionary<string, ALANode>> getTreeParentsFromGraph = new Data<Dictionary<string, ALANode>>() {Lambda=() =>{    var treeParents = new Dictionary<string, ALANode>();    foreach (var wire in mainGraph.Edges.OfType<ALAWire>())    {        var destId = wire.Destination.Id;        var sourceNode = wire.Source;        if (!treeParents.ContainsKey(destId))        {            treeParents[destId] = sourceNode;        }    }    return treeParents;}}; /* {"IsRoot":false} */
            ApplyAction<Dictionary<string, ALANode>> id_ec0f30ce468d4986abb9ad81abe73c17 = new ApplyAction<Dictionary<string, ALANode>>() {Lambda=treeParents => layoutDiagram.TreeParents = treeParents}; /* {"IsRoot":false} */
            UIConfig id_ab1d0ec0d92f4befb1ff44bb72cc8e10 = new UIConfig() {Visible=false}; /* {"IsRoot":true} */
            KeyEvent CTRL_Up_KeyPressed = new KeyEvent(eventName:"KeyDown") {Key=Key.Up,Modifiers=new Key[]{Key.LeftCtrl}}; /* {"IsRoot":false} */
            ApplyAction<KeyEventArgs> id_3c565e37c3c1486e91007c4d1d284367 = new ApplyAction<KeyEventArgs>() {Lambda=args => args.Handled = true}; /* {"IsRoot":false} */
            KeyEvent CTRL_Down_KeyPressed = new KeyEvent(eventName:"KeyDown") {Key=Key.Down,Modifiers=new Key[]{Key.LeftCtrl}}; /* {"IsRoot":false} */
            ApplyAction<KeyEventArgs> id_29a954d80a1a43ca8739e70022ebf3ec = new ApplyAction<KeyEventArgs>() {Lambda=args => args.Handled = true}; /* {"IsRoot":false} */
            DispatcherEvent id_2155bd03579a4918b01e6912a0f24188 = new DispatcherEvent() {}; /* {"IsRoot":false} */
            MenuItem menu_Tools = new MenuItem(header:"Tools") {}; /* {"IsRoot":false} */
            MenuItem id_7c21cf85883041b88e998ecc065cc4d4 = new MenuItem(header:"Create Instantiation Dictionary") {}; /* {"IsRoot":false} */
            UIConfig id_8eb5d9903d6941d285da2fc3d2ccfc3a = new UIConfig() {ToolTipText="Creates code that represents creating a dictionary of all non-reference instantiations,\nwhere each key is an instance name, and each value is the instance."}; /* {"IsRoot":false} */
            MenuItem id_180fa624d01c4759a83050e30426343a = new MenuItem(header:"Test TextEditor") {}; /* {"IsRoot":false} */
            PopupWindow id_5aec7a9782644198ab22d9ed7998ee15 = new PopupWindow(title:"Create Instantiation Dictionary") {Height=500,Width=1000,Resize=SizeToContent.WidthAndHeight}; /* {"IsRoot":false} */
            TextBox id_23e510bd08224b64b10c378f0f8fcdfe = new TextBox() {AcceptsReturn=true,AcceptsTab=true}; /* {"IsRoot":false} */
            EventConnector id_514f6109e8a24bc4b1ced57aaa255d90 = new EventConnector() {}; /* {"IsRoot":false} */
            UIConfig id_1c8c1eff6c1042cdb09364f0d4e80cf5 = new UIConfig() {Width=1000,Height=500,MaxHeight=500,LeftMargin=20,RightMargin=20,BottomMargin=20}; /* {"IsRoot":false} */
            Apply<string, string> createInstanceDictionaryCode = new Apply<string, string>() {Lambda=dictionaryName =>{    var instantiations = mainGraph.Nodes.OfType<ALANode>().Select(n => n.Model.Name).ToList();    var sb = new StringBuilder();    foreach (var instantiation in instantiations)    {        var cleanedInst = instantiation.Trim('@', ' ');        if (string.IsNullOrEmpty(cleanedInst))            continue;        sb.AppendLine($"{dictionaryName}[\"{cleanedInst}\"] = {cleanedInst};");    }    return sb.ToString();}}; /* {"IsRoot":false} */
            Vertical id_a1b1ae6b9ca64970b5b8988be0b5dda7 = new Vertical() {}; /* {"IsRoot":false} */
            Horizontal id_65e62fc671b1436191ccdc2a2e8c8af8 = new Horizontal() {}; /* {"IsRoot":false} */
            UIConfig id_ca4344b0f1334536b8ba52fda7567809 = new UIConfig() {UniformMargin=2}; /* {"IsRoot":false} */
            UIConfig id_e4615109bbba480cb0f7c11cc493cd84 = new UIConfig() {MinWidth=150,UniformMargin=2}; /* {"IsRoot":false} */
            Text id_740a947e8deb4a26868e4858d59387de = new Text(text:"Dictionary name:") {FontSize=16}; /* {"IsRoot":false} */
            TextBox id_a1163328ed694682ad454ff0f88e4dfe = new TextBox() {}; /* {"IsRoot":false} */
            DataFlowConnector<string> id_e7a7ac196c52416aa49fc77fe0503251 = new DataFlowConnector<string>() {}; /* {"IsRoot":false} */
            Data<string> id_df9b787cea7845f88e1faf65240adb4f = new Data<string>() {storedData="_abstractionsDict"}; /* {"IsRoot":false} */
            UIConfig id_28f139af6d3941658d65e5c08a79006d = new UIConfig() {Width=100,UniformMargin=2}; /* {"IsRoot":false} */
            Button id_a96a45b9b88648ebbf6ea3d24f036269 = new Button(title:"Regenerate") {}; /* {"IsRoot":false} */
            Data<string> id_b8f48b755a8545fcb626463d325ffe03 = new Data<string>() {}; /* {"IsRoot":false} */
            UIConfig id_5b1aec35b5fd47e482a25168390fcd66 = new UIConfig() {HorizAlignment="middle",LeftMargin=10,TopMargin=10,RightMargin=10,BottomMargin=10}; /* {"IsRoot":false} */
            Data<ALAWire> id_61311ea1bf8d405db0411618a8e11114 = new Data<ALAWire>() {Lambda=() => mainGraph.Edges.OfType<ALAWire>().FirstOrDefault(wire => wire.Destination.Equals(mainGraph.Get("SelectedNode")))}; /* {"IsRoot":false} */
            ApplyAction<ALAWire> id_831cf2bc59df431e9171a3887608cfae = new ApplyAction<ALAWire>() {Lambda=selectedWire =>{    if (selectedWire == null)        return;    var currentIndexInSubList = -1;    var indices = new List<int>();    for (var i = 0; i < mainGraph.Edges.Count; i++)    {        var wire = mainGraph.Edges[i] as ALAWire;        if (wire == null)            continue;        if (wire.Equals(selectedWire))        {            currentIndexInSubList = indices.Count;        }        if (wire.Source.Equals(selectedWire.Source))        {            indices.Add(i);        }    }    if (currentIndexInSubList == -1 || !indices.Any() || currentIndexInSubList == 0)        return;    mainGraph.Edges.RemoveAll(o => o.Equals(selectedWire));    currentIndexInSubList--;    var newIndex = indices[currentIndexInSubList];    mainGraph.Edges.Insert(newIndex, selectedWire);    foreach (var wire in mainGraph.Edges.OfType<ALAWire>())    {        wire.Refresh();    }}}; /* {"IsRoot":false} */
            Data<ALAWire> id_b8876ba6078448999ae1746d34ce803e = new Data<ALAWire>() {Lambda=() => mainGraph.Edges.OfType<ALAWire>().FirstOrDefault(wire => wire.Destination.Equals(mainGraph.Get("SelectedNode")))}; /* {"IsRoot":false} */
            ApplyAction<ALAWire> id_cc2aa50e0aef463ca17350d36436f98d = new ApplyAction<ALAWire>() {Lambda=selectedWire =>{    if (selectedWire == null)        return;    var currentIndexInSubList = -1;    var indices = new List<int>();    for (var i = 0; i < mainGraph.Edges.Count; i++)    {        var wire = mainGraph.Edges[i] as ALAWire;        if (wire == null)            continue;        if (wire.Equals(selectedWire))        {            currentIndexInSubList = indices.Count;        }        if (wire.Source.Equals(selectedWire.Source))        {            indices.Add(i);        }    }    if (currentIndexInSubList == -1 || !indices.Any() || currentIndexInSubList == indices.Count - 1)        return;    mainGraph.Edges.RemoveAll(o => o.Equals(selectedWire));    currentIndexInSubList++;    var newIndex = indices[currentIndexInSubList];    mainGraph.Edges.Insert(newIndex, selectedWire);    foreach (var wire in mainGraph.Edges.OfType<ALAWire>())    {        wire.Refresh();    }}}; /* {"IsRoot":false} */
            EventConnector id_94be5f8fa9014fad81fa832cdfb41c27 = new EventConnector() {}; /* {"IsRoot":false} */
            DispatcherEvent id_6377d8cb849a4a07b02d50789eab57a1 = new DispatcherEvent() {}; /* {"IsRoot":false} */
            EventConnector id_e3a05ca012df4e428f19f313109a576e = new EventConnector() {}; /* {"IsRoot":false} */
            DispatcherEvent id_6306c5f7aa3d41978599c00a5999b96f = new DispatcherEvent() {}; /* {"IsRoot":false} */
            ConvertToEvent<string> id_33d648af590b45139339fe533079ab12 = new ConvertToEvent<string>() {}; /* {"IsRoot":false} */
            EventLambda id_3605f8d8e4624d84befb96fe76ebd3ac = new EventLambda() {Lambda=() =>{    abstractionModelManager.ClearAbstractions();    availableAbstractions?.Clear();}}; /* {"IsRoot":false} */
            MultiMenu menu_OpenRecentProjects = new MultiMenu() {ParentHeader="Open Recent Projects..."}; /* {"IsRoot":false} */
            DataFlowConnector<object> id_e2c110ecff0740989d3d30144f84a94b = new DataFlowConnector<object>() {}; /* {"IsRoot":false} */
            ConvertToEvent<string> id_2b3a750d477d4e168aaa3ed0ae548650 = new ConvertToEvent<string>() {}; /* {"IsRoot":false} */
            GetSetting id_6ecefc4cdc694ef2a46a8628cadc0e1d = new GetSetting(name:"RecentProjectPaths") {}; /* {"IsRoot":false} */
            Apply<string, List<string>> id_097392c5af294d32b5c928a590bad83b = new Apply<string, List<string>>() {Lambda=json => JArray.Parse(json).Select(jt => jt.Value<string>()).ToList()}; /* {"IsRoot":false} */
            DataFlowConnector<List<string>> recentProjectPaths = new DataFlowConnector<List<string>>() {Data=new List<string>()}; /* {"IsRoot":false} */
            EventConnector id_408df459fb4c4846920b1a1edd4ac9e6 = new EventConnector() {}; /* {"IsRoot":false} */
            Data<object> id_e045b91666df454ca2f7985443af56c5 = new Data<object>() {}; /* {"IsRoot":false} */
            Apply<string, object> id_ef711f01535e48e2b65274af24d732f6 = new Apply<string, object>() {Lambda=path =>{    var paths = recentProjectPaths.Data;    if (!paths.Contains(path))        paths.Add(path);    return new JArray(paths);}}; /* {"IsRoot":false} */
            EditSetting id_6c8e7b486e894c6ca6bebaf40775b8b4 = new EditSetting() {JSONPath="$..RecentProjectPaths"}; /* {"IsRoot":false} */
            Cast<object, string> id_cb85f096416943cb9c08e4862f304568 = new Cast<object, string>() {}; /* {"IsRoot":false} */
            Apply<object, List<string>> id_5d9313a0a895402cb6be531e87c9b606 = new Apply<object, List<string>>() {Lambda=obj => (obj as JArray)?.Select(jt => jt.Value<string>()).ToList() ?? new List<string>()}; /* {"IsRoot":false} */
            DataFlowConnector<object> id_4ad460d4bd8d4a63ad7aca7ed9f1c945 = new DataFlowConnector<object>() {}; /* {"IsRoot":false} */
            MenuItem id_d386225d5368436185ff7e18a6dfd91a = new MenuItem(header:"Paste") {}; /* {"IsRoot":false} */
            TextClipboard id_355e5bd4d98745b2a42eb1266198128b = new TextClipboard() {}; /* {"IsRoot":false} */
            Apply<string, string> id_ceae580b14444b1e82c23813f47a47cd = new Apply<string, string>() {Lambda=json =>{    var jObj = JObject.Parse(json);    var instantiations = (jObj["Instantiations"] as JArray).Select(jt => jt.Value<string>()).ToList();    var wireTos = (jObj["WireTos"] as JArray).Select(jt => jt.Value<string>()).ToList();    var sb = new StringBuilder();    sb.AppendLine("class DummyClass {");    sb.AppendLine("void CreateWiring() {");    foreach (var inst in instantiations)    {        sb.AppendLine(inst);    }    foreach (var wireTo in wireTos)    {        sb.AppendLine(wireTo);    }    sb.AppendLine("}");    sb.AppendLine("}");    return sb.ToString();}}; /* {"IsRoot":false} */
            CreateDiagramFromCode pasteDiagramFromCode = new CreateDiagramFromCode() {Graph=mainGraph,Canvas=mainCanvas,ModelManager=abstractionModelManager,StateTransition=stateTransition,Update=true}; /* {"IsRoot":false} */
            DataFlowConnector<string> id_6180563898dc46da87f68e3da6bc7aa8 = new DataFlowConnector<string>() {}; /* {"IsRoot":false} */
            ConvertToEvent<string> id_6bc55844fa8f41db9a95118685504fd1 = new ConvertToEvent<string>() {}; /* {"IsRoot":false} */
            UIConfig id_e372e7c636a14549bba7cb5992874716 = new UIConfig() {Visible=false}; /* {"IsRoot":false} */
            MenuItem id_ec06a192a3b9424e996af338bd0e1699 = new MenuItem(header:"Split selected wire") {}; /* {"IsRoot":false} */
            Data<ALAWire> id_2c58e14cba984eb89065062bde6593be = new Data<ALAWire>() {Lambda=() => mainGraph.Get("SelectedWire") as ALAWire}; /* {"IsRoot":false} */
            EventConnector id_110e0e17c17e481291e3a1669fd3edaf = new EventConnector() {}; /* {"IsRoot":false} */
            ApplyAction<ALAWire> id_887caaa328ff409aa0c37fbcf3fac2b4 = new ApplyAction<ALAWire>() {Lambda=wireToSplit =>{    var latestNode = latestAddedNode.Data;    wireToSplit.Source = latestNode as ALANode;    wireToSplit.SourcePortBox = wireToSplit.Source.GetPortBox("output");    wireToSplit.Refresh();}}; /* {"IsRoot":false} */
            ApplyAction<ALAWire> id_192fe80aafb34059af0f997434d4eb24 = new ApplyAction<ALAWire>() {Lambda=wire => mainGraph.Set("SelectedNode", wire.Source)}; /* {"IsRoot":false} */
            DataFlowConnector<ALAWire> id_f135f2c631b941d4916589a8fb078d6e = new DataFlowConnector<ALAWire>() {}; /* {"IsRoot":false} */
            ConvertToEvent<ALAWire> id_1cfa104de254494cb1d4552604cc6b94 = new ConvertToEvent<ALAWire>() {}; /* {"IsRoot":false} */
            MenuItem menu_NodeSpacing = new MenuItem(header:"Node Spacing") {}; /* {"IsRoot":false} */
            PopupWindow id_72c67c7f881142c99b7021fc1f3ae6ad = new PopupWindow() {Height=200,Width=250}; /* {"IsRoot":false} */
            Vertical id_d5ea8f6014a44f6faf59a7b5768bcadf = new Vertical() {}; /* {"IsRoot":false} */
            Horizontal id_54c8bc7425ab4b4580b5584852487782 = new Horizontal() {}; /* {"IsRoot":false} */
            Text id_d696f92299cb4a86bfda5c0d70f3e6ce = new Text(text:"Horizontal Spacing: ") {}; /* {"IsRoot":false} */
            Text id_eacca7f00d5f424ea8a0d2a460f70862 = new Text(text:"Vertical Spacing: ") {}; /* {"IsRoot":false} */
            Horizontal id_0523989e80a342fa830a12c31e976794 = new Horizontal() {}; /* {"IsRoot":false} */
            TextBox id_e5180cbda745469b99d3d52c17f49119 = new TextBox() {Text=layoutDiagram.HorizontalGap.ToString()}; /* {"IsRoot":false} */
            TextBox id_6958ea0957fa4d8781c5ee3bbdaee6fd = new TextBox() {Text=layoutDiagram.VerticalGap.ToString()}; /* {"IsRoot":false} */
            ApplyAction<string> id_ea6694878c8c44c28f2d054ee089c12e = new ApplyAction<string>() {Lambda=input => layoutDiagram.HorizontalGap = double.Parse(input)}; /* {"IsRoot":false} */
            ApplyAction<string> id_c2835f5e1f3149ccb42f1865fa67de55 = new ApplyAction<string>() {Lambda=input => layoutDiagram.VerticalGap = double.Parse(input)}; /* {"IsRoot":false} */
            Button id_a417fd2a5a144349b36c5e149810c442 = new Button(title:"OK") {}; /* {"IsRoot":false} */
            UIConfig id_977932d8d02445979383614993bac82c = new UIConfig() {Width=50,HorizAlignment="right",UniformMargin=2}; /* {"IsRoot":false} */
            UIConfig id_477f5f3243c6416f99fbf40d65945e0e = new UIConfig() {UniformMargin=2}; /* {"IsRoot":false} */
            UIConfig id_97b80479ebba440d94a51a888044a581 = new UIConfig() {UniformMargin=2}; /* {"IsRoot":false} */
            MenuItem menu_Edit = new MenuItem(header:"Edit") {}; /* {"IsRoot":false} */
            DataFlowConnector<Tuple<string, List<string>>> currentDiagramCode = new DataFlowConnector<Tuple<string, List<string>>>() {}; /* {"IsRoot":false} */
            Data<Tuple<string, List<string>>> id_051136027e944f94b5adcf7f30318e4f = new Data<Tuple<string, List<string>>>() {}; /* {"IsRoot":false} */
            DataFlowConnector<Tuple<string, List<string>>> id_6276e2c141c94ae5a8af58fb7b6f70bf = new DataFlowConnector<Tuple<string, List<string>>>() {}; /* {"IsRoot":false} */
            MultiMenu menu_OpenDiagram = new MultiMenu() {ParentHeader="Open Diagram..."}; /* {"IsRoot":false} */
            DataFlowConnector<Dictionary<string, Tuple<string, List<string>>>> allDiagramsCode = new DataFlowConnector<Dictionary<string, Tuple<string, List<string>>>>() {}; /* {"IsRoot":false} */
            Apply<Dictionary<string, Tuple<string, List<string>>>, List<string>> getDiagramList = new Apply<Dictionary<string, Tuple<string, List<string>>>, List<string>>() {Lambda=allDiagrams =>{    return allDiagrams.Keys.ToList();}}; /* {"IsRoot":false} */
            Apply<string, Tuple<string, List<string>>> id_38501a618c7b4b1aac1194f24f8d325d = new Apply<string, Tuple<string, List<string>>>() {Lambda=diagramName =>{    return allDiagramsCode.Data[diagramName];}}; /* {"IsRoot":false} */
            DataFlowConnector<string> id_0bcb2cfeb90d43a5973f21d2e4c50dcc = new DataFlowConnector<string>() {}; /* {"IsRoot":false} */
            EventLambda id_fb0a6ff48d6f4360bdef001007ee8459 = new EventLambda() {Lambda=() =>{    insertInstantiations.StartLandmark = $"// BEGIN AUTO-GENERATED INSTANTIATIONS FOR {currentDiagramName.Data}";    insertInstantiations.EndLandmark = $"// END AUTO-GENERATED INSTANTIATIONS FOR {currentDiagramName.Data}";    insertWireTos.StartLandmark = $"// BEGIN AUTO-GENERATED WIRING FOR {currentDiagramName.Data}";    insertWireTos.EndLandmark = $"// END AUTO-GENERATED WIRING FOR {currentDiagramName.Data}";}}; /* {"IsRoot":false} */
            ConvertToEvent<string> id_dccd548f0c18412385231185ef028374 = new ConvertToEvent<string>() {}; /* {"IsRoot":false} */
            MultiMenu menu_OpenSelectedNodesDiagram = new MultiMenu() {ParentHeader="Open Selected Node's Diagram..."}; /* {"IsRoot":false} */
            DataFlowConnector<List<string>> currentApplicationDiagramNameList = new DataFlowConnector<List<string>>() {}; /* {"IsRoot":false} */
            Data<ALANode> id_119b86267f5046bca55e50432b342474 = new Data<ALANode>() {Lambda=() => mainGraph.Get("SelectedNode") as ALANode}; /* {"IsRoot":false} */
            Apply<ALANode, List<string>> id_de1d2e0c58cd4c4b989f5311740a2253 = new Apply<ALANode, List<string>>() {Lambda=node =>{    var diagramNames = new List<string>();    if (node == null)        return diagramNames; /* extractALACode.ExtractCode(extractALACode.SourceCode, outputUserSelection: false); */ /* Gets up-to-date info, but is slow */    diagramNames.AddRange(extractALACode.NodeToDiagramMapping[node.Name]);    return diagramNames;}}; /* {"IsRoot":false} */
            DataFlowConnector<ALANode> id_043e1ea3b057405a8c266456acdd97da = new DataFlowConnector<ALANode>() {}; /* {"IsRoot":false} */
            DataFlowConnector<string> id_4543ca6d3d6a47789f52e4cc7d841ee5 = new DataFlowConnector<string>() {}; /* {"IsRoot":false} */
            ConvertToEvent<string> id_8398ff1988b344c1841ea38cde6e1ce3 = new ConvertToEvent<string>() {}; /* {"IsRoot":false} */
            Data<ALANode> getMatchingNodeInNewGraph = new Data<ALANode>() {Lambda=() =>{    var oldSelectedNode = mainGraph.Get("SelectedNode") as ALANode;    var newSelectedNode = mainGraph.Nodes.OfType<ALANode>().First(node => node.Name == oldSelectedNode.Name);    return newSelectedNode;}}; /* {"IsRoot":false} */
            UIConfig id_24f0cee4833c4971b8c9cb2bdbb1c868 = new UIConfig() {Visible=false}; /* {"IsRoot":false} */
            MenuItem id_25ed8d5621754358bb15633274ef191a = new MenuItem(header:"Create Abstraction") {}; /* {"IsRoot":false} */
            MenuItem id_eeaef5bfff254a4f850d013288ef44fa = new MenuItem(header:"Create Code Generation Landmarks") {}; /* {"IsRoot":false} */
            PopupWindow id_c7ff61bc312843019dde00238832d5a1 = new PopupWindow() {Height=100,Width=200}; /* {"IsRoot":false} */
            UIConfig id_56042a0e94ed4d599965f1f9c4fb7b8c = new UIConfig() {UniformMargin=3}; /* {"IsRoot":false} */
            Text id_a1e3c8eca2ee4f75ab41465c2bc1a9a9 = new Text(text:"Diagram name: ") {}; /* {"IsRoot":false} */
            Horizontal id_f32979c2ffca4ab1b028ced3255bb68d = new Horizontal() {}; /* {"IsRoot":false} */
            UIConfig id_55aaae3200544344b396fd2e8a63a03c = new UIConfig() {Height=20,UniformMargin=3}; /* {"IsRoot":false} */
            TextBox id_f736325c8e074ccf92f128ffa9b3d68f = new TextBox() {}; /* {"IsRoot":false} */
            Horizontal id_9bf12baaecbe45658c5861327b37fcde = new Horizontal() {}; /* {"IsRoot":false} */
            UIConfig id_13ee0053aac24b538d4b0bc94606974f = new UIConfig() {Width=50,HorizAlignment="right",UniformMargin=3}; /* {"IsRoot":false} */
            Button id_509962caea7b49198c21f0b4c3f7db66 = new Button(title:"OK") {}; /* {"IsRoot":false} */
            Vertical id_f73625a393984a259b173f4d60d92b60 = new Vertical() {}; /* {"IsRoot":false} */
            DataFlowConnector<string> id_f0902adcccf946fe9050b5d783af0277 = new DataFlowConnector<string>() {}; /* {"IsRoot":false} */
            Data<string> id_c10e8ba484d44b708f02664b52e48a25 = new Data<string>() {}; /* {"IsRoot":false} */
            Apply<string, string> id_d5a44d60211d4434a97b6e04f4c44887 = new Apply<string, string>() {Lambda=diagramName =>{    var sb = new StringBuilder();    sb.AppendLine($"// BEGIN AUTO-GENERATED INSTANTIATIONS FOR {diagramName}\n");    sb.AppendLine($"// END AUTO-GENERATED INSTANTIATIONS FOR {diagramName}\n");    sb.AppendLine($"// BEGIN AUTO-GENERATED WIRING FOR {diagramName}\n");    sb.AppendLine($"// END AUTO-GENERATED WIRING FOR {diagramName}\n");    return sb.ToString();}}; /* {"IsRoot":false} */
            TextClipboard id_b49ee56548054b7b91bc4a6863b68112 = new TextClipboard() {}; /* {"IsRoot":false} */
            EventConnector id_6fd8927aa820450b8b1e04638661d9ce = new EventConnector() {}; /* {"IsRoot":false} */
            ConvertToEvent<string> id_782197630669407095b6042ba91bbc4b = new ConvertToEvent<string>() {}; /* {"IsRoot":false} */
            Data<string> id_a808288fa4ae48b0a33de1fda8e4b58a = new Data<string>() {}; /* {"IsRoot":false} */
            FileReader id_5997270bf4614726ac236d5536fa79ab = new FileReader() {}; /* {"IsRoot":false} */
            ApplyAction<string> id_590d452a50e4468ca15074a88f59f6d6 = new ApplyAction<string>() {Lambda=code =>{    extractALACode.ExtractCode(code);}}; /* {"IsRoot":false} */
            EventConnector id_d0697034644f4faa9dbc1f263f45708c = new EventConnector() {}; /* {"IsRoot":false} */
            ApplyAction<string> id_c31dec24e80b4e328882abbc3368489e = new ApplyAction<string>() {Lambda=name => extractALACode.CurrentDiagramName = name}; /* {"IsRoot":false} */
            DataFlowConnector<ALANode> currentALANode = new DataFlowConnector<ALANode>() {}; /* {"IsRoot":false} */
            ContextMenu alaNodeContextMenu = new ContextMenu() {}; /* {"IsRoot":false} */
            MenuItem id_403baaf79a824981af02ae135627767f = new MenuItem(header:"Open source code in your default .cs file editor") {}; /* {"IsRoot":false} */
            EventLambda id_872f85f0291843daad50fcaf77f4e9c2 = new EventLambda() {Lambda=() =>{    Process.Start(currentALANode.Data.Model.GetCodeFilePath());}}; /* {"IsRoot":false} */
            MenuItem id_506e76d969fe492291d78e607738dd48 = new MenuItem(header:"Copy variable name") {}; /* {"IsRoot":false} */
            Data<string> id_3a93eeaf377b47c8b9bbd70dda63370c = new Data<string>() {Lambda=() => currentALANode.Data.Name}; /* {"IsRoot":false} */
            TextClipboard id_67487fc1e2e949a590412918be99c15d = new TextClipboard() {}; /* {"IsRoot":false} */
            MenuItem id_1ef9731dc4674b8e97409364e29134d2 = new MenuItem(header:"Delete node") {}; /* {"IsRoot":false} */
            EventLambda id_07bac55274924004ba5f349da0f11ef7 = new EventLambda() {Lambda=() => currentALANode.Data.Delete(deleteChildren: false)}; /* {"IsRoot":false} */
            MenuItem id_5d1f3fa471fe492586d178fa2eb2fd81 = new MenuItem(header:"Delete node and children") {}; /* {"IsRoot":false} */
            EventLambda id_a68a6c716096461585853877fa2c6f7a = new EventLambda() {Lambda=() => currentALANode.Data.Delete(deleteChildren: true)}; /* {"IsRoot":false} */
            MenuItem id_4c03930a6877421eb54a5397acb93135 = new MenuItem(header:"IsRoot") {}; /* {"IsRoot":false} */
            CheckBox nodeIsRootCheckBox = new CheckBox(check:currentALANode.Data?.IsRoot ?? false) {}; /* {"IsRoot":false} */
            ApplyAction<bool> id_fc8dfeb357454d458f8bd67f185de174 = new ApplyAction<bool>() {Lambda=checkState => currentALANode.Data.IsRoot = checkState}; /* {"IsRoot":false} */
            MenuItem id_692340f2d88d4d0d80cff9daaff7350d = new MenuItem(header:"IsReferenceNode") {}; /* {"IsRoot":false} */
            CheckBox nodeIsReferenceNodeCheckBox = new CheckBox(check:currentALANode.Data?.IsReferenceNode ?? false) {}; /* {"IsRoot":false} */
            ApplyAction<bool> id_5549bbb3a73e4fceb7b571f3ba58b9db = new ApplyAction<bool>() {Lambda=checkState => currentALANode.Data.IsReferenceNode = checkState}; /* {"IsRoot":false} */
            MenuItem id_7d4b8a9390724664acd0fb4f586d0b63 = new MenuItem(header:"Copy...") {}; /* {"IsRoot":false} */
            MenuItem id_96fa54c808104c0cb7d23f092946f54d = new MenuItem(header:"This node") {}; /* {"IsRoot":false} */
            MenuItem id_a69c62a42dfc460b81024720b3d94941 = new MenuItem(header:"This node and its subtree") {}; /* {"IsRoot":false} */
            Data<string> id_52d97f7602cf47a7bc58e6a1ad1a977a = new Data<string>() {Lambda=() => currentALANode.Data.GenerateConnectedSubdiagramCode()}; /* {"IsRoot":false} */
            UIConfig id_7c333d78095d4982b82623733fbdbe00 = new UIConfig() {Visible=false}; /* {"IsRoot":false} */
            EventLambda initialiseALANodeContextMenu = new EventLambda() {Lambda=() => (alaNodeContextMenu as IUI).GetWPFElement()}; /* {"IsRoot":false} */
            // END AUTO-GENERATED INSTANTIATIONS FOR GALADE_Standalone

            // BEGIN AUTO-GENERATED WIRING FOR GALADE_Standalone
            mainWindow.WireTo(mainWindowVertical, "iuiStructure"); /* {"SourceType":"MainWindow","SourceIsReference":false,"DestinationType":"Vertical","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            mainWindow.WireTo(id_642ae4874d1e4fd2a777715cc1996b49, "appStart"); /* {"SourceType":"MainWindow","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            mainWindowVertical.WireTo(id_42967d39c2334aab9c23697d04177f8a, "children"); /* {"SourceType":"Vertical","SourceIsReference":false,"DestinationType":"MenuBar","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            mainCanvasDisplay.WireTo(id_581015f073614919a33126efd44bf477, "contextMenu"); /* {"SourceType":"CanvasDisplay","SourceIsReference":false,"DestinationType":"ContextMenu","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            mainCanvasDisplay.WireTo(A_KeyPressed, "eventHandlers"); /* {"SourceType":"CanvasDisplay","SourceIsReference":false,"DestinationType":"KeyEvent","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            mainCanvasDisplay.WireTo(R_KeyPressed, "eventHandlers"); /* {"SourceType":"CanvasDisplay","SourceIsReference":false,"DestinationType":"KeyEvent","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            mainCanvasDisplay.WireTo(Enter_KeyPressed, "eventHandlers"); /* {"SourceType":"CanvasDisplay","SourceIsReference":false,"DestinationType":"KeyEvent","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            mainCanvasDisplay.WireTo(id_6d1f4415e8d849e19f5d432ea96d9abb, "eventHandlers"); /* {"SourceType":"CanvasDisplay","SourceIsReference":false,"DestinationType":"MouseButtonEvent","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            mainCanvasDisplay.WireTo(id_44b41ddf67864f29ae9b59ed0bec2927, "eventHandlers"); /* {"SourceType":"CanvasDisplay","SourceIsReference":false,"DestinationType":"MouseButtonEvent","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            mainCanvasDisplay.WireTo(Delete_KeyPressed, "eventHandlers"); /* {"SourceType":"CanvasDisplay","SourceIsReference":false,"DestinationType":"KeyEvent","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            mainCanvasDisplay.WireTo(id_2a7c8f3b6b5e4879ad5a35ff6d8538fd, "eventHandlers"); /* {"SourceType":"CanvasDisplay","SourceIsReference":false,"DestinationType":"MouseWheelEvent","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            mainCanvasDisplay.WireTo(CTRL_S_KeyPressed, "eventHandlers"); /* {"SourceType":"CanvasDisplay","SourceIsReference":false,"DestinationType":"KeyEvent","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_581015f073614919a33126efd44bf477.WireTo(id_57e6a33441c54bc89dc30a28898cb1c0, "children"); /* {"SourceType":"ContextMenu","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_581015f073614919a33126efd44bf477.WireTo(id_83c3db6e4dfa46518991f706f8425177, "children"); /* {"SourceType":"ContextMenu","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_57e6a33441c54bc89dc30a28898cb1c0.WireTo(id_5297a497d2de44e5bc0ea2c431cdcee6, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["AbstractionModel"]} */
            id_8647cbf4ac4049a99204b0e3aa70c326.WireTo(startGuaranteedLayoutProcess, "eventOutput"); /* {"SourceType":"ConvertToEvent","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["object"],"DestinationGenerics":[]} */
            id_7356212bcc714c699681e8dffc853761.WireTo(getTreeParentsFromGraph, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["Dictionary<string, ALANode>"]} */
            id_7356212bcc714c699681e8dffc853761.WireTo(layoutDiagram, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"RightTreeLayout","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["ALANode"]} */
            R_KeyPressed.WireTo(startGuaranteedLayoutProcess, "eventHappened"); /* {"SourceType":"KeyEvent","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_42967d39c2334aab9c23697d04177f8a.WireTo(menu_File, "children"); /* {"SourceType":"MenuBar","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            menu_File.WireTo(menu_OpenProject, "children"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            menu_File.WireTo(menu_OpenCodeFile, "children"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_463b31fe2ac04972b5055a3ff2f74fe3.WireTo(id_a1f87102954345b69de6841053fce813, "selectedFolderPathOutput"); /* {"SourceType":"FolderBrowser","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_63088b53f85b4e6bb564712c525e063c.WireTo(id_35fceab68423425195096666f27475e9, "foundFiles"); /* {"SourceType":"DirectorySearch","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["Dictionary<string, List<string>>"]} */
            id_a98457fc05fc4e84bfb827f480db93d3.WireTo(id_f5d3730393ab40d78baebcb9198808da, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"ForEach","DestinationIsReference":false,"Description":"","SourceGenerics":["Dictionary<string, List<string>>","IEnumerable<string>"],"DestinationGenerics":["string"]} */
            id_f5d3730393ab40d78baebcb9198808da.WireTo(id_6bc94d5f257847ff8a9a9c45e02333b4, "elementOutput"); /* {"SourceType":"ForEach","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            getProjectFolderPath.WireTo(id_ecfbf0b7599e4340b8b2f79b7d1e29cb, "filePathInput"); /* {"SourceType":"GetSetting","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            getProjectFolderPath.WireTo(id_a1f87102954345b69de6841053fce813, "settingJsonOutput"); /* {"SourceType":"GetSetting","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            Enter_KeyPressed.WireTo(id_6e249d6520104ca5a1a4d847a6c862a8, "senderOutput"); /* {"SourceType":"KeyEvent","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["object"]} */
            id_08d455bfa9744704b21570d06c3c5389.WireTo(id_843593fbc341437bb7ade21d0c7f6729, "children"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_08d455bfa9744704b21570d06c3c5389.WireTo(id_a34c047df9ae4235a08b037fd9e48ab8, "children"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_08d455bfa9744704b21570d06c3c5389.WireTo(id_96ab5fcf787a4e6d88af011f6e3daeae, "children"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_843593fbc341437bb7ade21d0c7f6729.WireTo(id_91726b8a13804a0994e27315b0213fe8, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"PopupWindow","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_91726b8a13804a0994e27315b0213fe8.WireTo(id_a2e6aa4f4d8e41b59616d63362768dde, "children"); /* {"SourceType":"PopupWindow","SourceIsReference":false,"DestinationType":"Box","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_a2e6aa4f4d8e41b59616d63362768dde.WireTo(id_826249b1b9d245709de6f3b24503be2d, "uiLayout"); /* {"SourceType":"Box","SourceIsReference":false,"DestinationType":"TextEditor","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_a1f87102954345b69de6841053fce813.WireTo(id_33d648af590b45139339fe533079ab12, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"ConvertToEvent","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            id_a1f87102954345b69de6841053fce813.WireTo(id_63088b53f85b4e6bb564712c525e063c, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"DirectorySearch","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":[]} */
            id_a1f87102954345b69de6841053fce813.WireTo(id_460891130e9e499184b84a23c2e43c9f, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"Cast","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string","object"]} */
            id_6d1f4415e8d849e19f5d432ea96d9abb.WireTo(id_e7e60dd036af4a869e10a64b2c216104, "argsOutput"); /* {"SourceType":"MouseButtonEvent","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["object"]} */
            id_44b41ddf67864f29ae9b59ed0bec2927.WireTo(id_da4f1dedd74549e283777b5f7259ad7f, "argsOutput"); /* {"SourceType":"MouseButtonEvent","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["object"]} */
            id_368a7dc77fe24060b5d4017152492c1e.WireTo(id_2f4df1d9817246e5a9184857ec5a2bf8, "transitionOutput"); /* {"SourceType":"StateChangeListener","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["Tuple<Enums.DiagramMode, Enums.DiagramMode>","bool"]} */
            id_2f4df1d9817246e5a9184857ec5a2bf8.WireTo(id_c80f46b08d894d4faa674408bf846b3f, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"IfElse","DestinationIsReference":false,"Description":"","SourceGenerics":["Tuple<Enums.DiagramMode, Enums.DiagramMode>","bool"],"DestinationGenerics":[]} */
            id_c80f46b08d894d4faa674408bf846b3f.WireTo(startRightTreeLayoutProcess, "ifOutput"); /* {"SourceType":"IfElse","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_642ae4874d1e4fd2a777715cc1996b49.WireTo(id_cdeb94e2daee4057966eba31781ebd0d, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_642ae4874d1e4fd2a777715cc1996b49.WireTo(getProjectFolderPath, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"GetSetting","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_642ae4874d1e4fd2a777715cc1996b49.WireTo(id_368a7dc77fe24060b5d4017152492c1e, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"StateChangeListener","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_642ae4874d1e4fd2a777715cc1996b49.WireTo(id_f9b8e7f524a14884be753d19a351a285, "complete"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            Delete_KeyPressed.WireTo(id_46a4d6e6cfb940278eb27561c43cbf37, "eventHappened"); /* {"SourceType":"KeyEvent","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_83c3db6e4dfa46518991f706f8425177.WireTo(startRightTreeLayoutProcess, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_5297a497d2de44e5bc0ea2c431cdcee6.WireTo(id_9bd4555e80434a7b91b65e0b386593b0, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":["AbstractionModel"],"DestinationGenerics":["AbstractionModel","object"]} */
            id_9bd4555e80434a7b91b65e0b386593b0.WireTo(id_7fabbaae488340a59d940100d38e9447, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":["AbstractionModel","object"],"DestinationGenerics":["object"]} */
            id_2810e4e86da348b98b39c987e6ecd7b6.WireTo(id_cf7df48ac3304a8894a7536261a3b474, "fileContentOutput"); /* {"SourceType":"FileReader","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_f9b8e7f524a14884be753d19a351a285.WireTo(id_c4f838d19a6b4af9ac320799ebe9791f, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_0fd49143884d4a6e86e6ed0ea2f1b5b4.WireTo(id_f5d3730393ab40d78baebcb9198808da, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"ForEach","DestinationIsReference":false,"Description":"","SourceGenerics":["Dictionary<string, List<string>>","IEnumerable<string>"],"DestinationGenerics":["string"]} */
            id_35fceab68423425195096666f27475e9.WireTo(id_8fc35564768b4a64a57dc321cc1f621f, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":["Dictionary<string, List<string>>"],"DestinationGenerics":["Dictionary<string, List<string>>","IEnumerable<string>"]} */
            id_35fceab68423425195096666f27475e9.WireTo(id_0fd49143884d4a6e86e6ed0ea2f1b5b4, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":["Dictionary<string, List<string>>"],"DestinationGenerics":["Dictionary<string, List<string>>","IEnumerable<string>"]} */
            id_35fceab68423425195096666f27475e9.WireTo(id_92effea7b90745299826cd566a0f2b88, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":["Dictionary<string, List<string>>"],"DestinationGenerics":["Dictionary<string, List<string>>","IEnumerable<string>"]} */
            id_643997d9890f41d7a3fcab722aa48f89.WireTo(id_843620b3a9ed45bea231b841b52e5621, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["UIElement"],"DestinationGenerics":["UIElement"]} */
            id_261d188e3ce64cc8a06f390ba51e092f.WireTo(id_04c07393f532472792412d2a555510b9, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["UIElement"],"DestinationGenerics":["UIElement"]} */
            id_843620b3a9ed45bea231b841b52e5621.WireTo(zoomIn, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"Scale","DestinationIsReference":false,"Description":"","SourceGenerics":["UIElement"],"DestinationGenerics":[]} */
            id_843620b3a9ed45bea231b841b52e5621.WireTo(applyZoomEffects, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":["UIElement"],"DestinationGenerics":["UIElement"]} */
            id_04c07393f532472792412d2a555510b9.WireTo(zoomOut, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"Scale","DestinationIsReference":false,"Description":"","SourceGenerics":["UIElement"],"DestinationGenerics":[]} */
            id_04c07393f532472792412d2a555510b9.WireTo(applyZoomEffects, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":["UIElement"],"DestinationGenerics":["UIElement"]} */
            id_33990435606f4bbc9ba1786ed05672ab.WireTo(id_6909a5f3b0e446d3bb0c1382dac1faa9, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"IfElse","DestinationIsReference":false,"Description":"","SourceGenerics":["MouseWheelEventArgs","bool"],"DestinationGenerics":[]} */
            id_6909a5f3b0e446d3bb0c1382dac1faa9.WireTo(id_643997d9890f41d7a3fcab722aa48f89, "ifOutput"); /* {"SourceType":"IfElse","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["UIElement"]} */
            id_6909a5f3b0e446d3bb0c1382dac1faa9.WireTo(id_261d188e3ce64cc8a06f390ba51e092f, "elseOutput"); /* {"SourceType":"IfElse","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["UIElement"]} */
            id_cf7df48ac3304a8894a7536261a3b474.WireTo(extractALACode, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"ExtractALACode","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":[]} */
            CTRL_S_KeyPressed.WireTo(generateCode, "eventHappened"); /* {"SourceType":"KeyEvent","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_a34c047df9ae4235a08b037fd9e48ab8.WireTo(generateCode, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_b5364bf1c9cd46a28e62bb2eb0e11692.WireTo(insertInstantiations, "instantiations"); /* {"SourceType":"GenerateALACode","SourceIsReference":false,"DestinationType":"InsertFileCodeLines","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_b5364bf1c9cd46a28e62bb2eb0e11692.WireTo(insertWireTos, "wireTos"); /* {"SourceType":"GenerateALACode","SourceIsReference":false,"DestinationType":"InsertFileCodeLines","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_a3efe072d6b44816a631d90ccef5b71e.WireTo(id_fcfcb5f0ae544c968dcbc734ac1db51b, "filePathInput"); /* {"SourceType":"GetSetting","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_f928bf426b204bc89ba97219c97df162.WireTo(id_c01710b47a2a4deb824311c4dc46222d, "filePathInput"); /* {"SourceType":"EditSetting","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_f07ddae8b4ee431d8ede6c21e1fe01c5.WireTo(id_f928bf426b204bc89ba97219c97df162, "output"); /* {"SourceType":"Cast","SourceIsReference":false,"DestinationType":"EditSetting","DestinationIsReference":false,"Description":"","SourceGenerics":["string","object"],"DestinationGenerics":[]} */
            id_17609c775b9c4dfcb1f01d427d2911ae.WireTo(id_f07ddae8b4ee431d8ede6c21e1fe01c5, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"Cast","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string","object"]} */
            id_e2c110ecff0740989d3d30144f84a94b.WireTo(id_60229af56d92436996d2ee8d919083a3, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"EditSetting","DestinationIsReference":false,"Description":"","SourceGenerics":["object"],"DestinationGenerics":[]} */
            id_92effea7b90745299826cd566a0f2b88.WireTo(id_f5d3730393ab40d78baebcb9198808da, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"ForEach","DestinationIsReference":false,"Description":"","SourceGenerics":["Dictionary<string, List<string>>","IEnumerable<string>"],"DestinationGenerics":["string"]} */
            id_c5fdc10d2ceb4577bef01977ee8e9dd1.WireTo(id_b9865ebcd2864642a96573ced52bbb7f, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            id_72140c92ac4f4255abe9d149068fa16f.WireTo(id_1d55a1faa3dd4f78ad22ac73051f5d2d, "fileContentOutput"); /* {"SourceType":"FileReader","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_1d55a1faa3dd4f78ad22ac73051f5d2d.WireTo(insertInstantiations, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"InsertFileCodeLines","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":[]} */
            generateCode.WireTo(id_c5fdc10d2ceb4577bef01977ee8e9dd1, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            generateCode.WireTo(id_5e77c28f15294641bb881592d2cd7ac9, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            generateCode.WireTo(id_fb0a6ff48d6f4360bdef001007ee8459, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            generateCode.WireTo(id_b5364bf1c9cd46a28e62bb2eb0e11692, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"GenerateALACode","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            generateCode.WireTo(id_0e563f77c5754bdb8a75b7f55607e9b0, "complete"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_60229af56d92436996d2ee8d919083a3.WireTo(id_58c03e4b18bb43de8106a4423ca54318, "filePathInput"); /* {"SourceType":"EditSetting","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_2b42bd6059334bfabc3df1d047751d7a.WireTo(id_b9865ebcd2864642a96573ced52bbb7f, "filePathInput"); /* {"SourceType":"FileWriter","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_b9865ebcd2864642a96573ced52bbb7f.WireTo(id_72140c92ac4f4255abe9d149068fa16f, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"FileReader","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":[]} */
            insertInstantiations.WireTo(insertWireTos, "newFileContentsOutput"); /* {"SourceType":"InsertFileCodeLines","SourceIsReference":false,"DestinationType":"InsertFileCodeLines","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            insertWireTos.WireTo(id_2b42bd6059334bfabc3df1d047751d7a, "newFileContentsOutput"); /* {"SourceType":"InsertFileCodeLines","SourceIsReference":false,"DestinationType":"FileWriter","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_0e563f77c5754bdb8a75b7f55607e9b0.WireTo(insertInstantiations, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"InsertFileCodeLines","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_0e563f77c5754bdb8a75b7f55607e9b0.WireTo(insertWireTos, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"InsertFileCodeLines","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_0e563f77c5754bdb8a75b7f55607e9b0.WireTo(id_3f30a573358d4fd08c4c556281737360, "complete"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_96ab5fcf787a4e6d88af011f6e3daeae.WireTo(id_026d2d87a422495aa46c8fc4bda7cdd7, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_e3837af93b584ca9874336851ff0cd31.WireTo(globalMessageTextDisplay, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"Text","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_08d455bfa9744704b21570d06c3c5389.WireTo(id_6f93680658e04f8a9ab15337cee1eca3, "children"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_a3efe072d6b44816a631d90ccef5b71e.WireTo(id_9f411cfea16b45ed9066dd8f2006e1f1, "settingJsonOutput"); /* {"SourceType":"GetSetting","SourceIsReference":false,"DestinationType":"FileReader","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            menu_OpenCodeFile.WireTo(id_db598ad59e5542a0adc5df67ced27f73, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_db598ad59e5542a0adc5df67ced27f73.WireTo(id_14170585873a4fb6a7550bfb3ce8ecd4, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"FileBrowser","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_14170585873a4fb6a7550bfb3ce8ecd4.WireTo(id_9b866e4112fd4347a2a3e81441401dea, "selectedFilePathOutput"); /* {"SourceType":"FileBrowser","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_9b866e4112fd4347a2a3e81441401dea.WireTo(setting_currentDiagramCodeFilePath, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            id_9f411cfea16b45ed9066dd8f2006e1f1.WireTo(id_cf7df48ac3304a8894a7536261a3b474, "fileContentOutput"); /* {"SourceType":"FileReader","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_dcd4c90552dc4d3fb579833da87cd829.WireTo(id_5ddd02478c734777b9e6f1079b4b3d45, "delayedEvent"); /* {"SourceType":"DispatcherEvent","SourceIsReference":false,"DestinationType":"GetSetting","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_5ddd02478c734777b9e6f1079b4b3d45.WireTo(id_ecfbf0b7599e4340b8b2f79b7d1e29cb, "filePathInput"); /* {"SourceType":"GetSetting","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            latestCodeFilePath.WireTo(id_d5d3af7a3c9a47bf9af3b1a1e1246267, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string","bool"]} */
            id_d5d3af7a3c9a47bf9af3b1a1e1246267.WireTo(id_2ce385b32256413ab2489563287afaac, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"IfElse","DestinationIsReference":false,"Description":"","SourceGenerics":["string","bool"],"DestinationGenerics":[]} */
            id_5ddd02478c734777b9e6f1079b4b3d45.WireTo(latestCodeFilePath, "settingJsonOutput"); /* {"SourceType":"GetSetting","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_f9b8e7f524a14884be753d19a351a285.WireTo(id_dcd4c90552dc4d3fb579833da87cd829, "complete"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"DispatcherEvent","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            mainCanvasDisplay.WireTo(MiddleMouseButton_Pressed, "eventHandlers"); /* {"SourceType":"CanvasDisplay","SourceIsReference":false,"DestinationType":"MouseButtonEvent","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            MiddleMouseButton_Pressed.WireTo(id_d90fbf714f5f4fdc9b43cbe4d5cebf1c, "senderOutput"); /* {"SourceType":"MouseButtonEvent","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["object"]} */
            mainWindowVertical.WireTo(mainHorizontal, "children"); /* {"SourceType":"Vertical","SourceIsReference":false,"DestinationType":"Horizontal","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            mainWindowVertical.WireTo(statusBarHorizontal, "children"); /* {"SourceType":"Vertical","SourceIsReference":false,"DestinationType":"Horizontal","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            mainHorizontal.WireTo(sidePanelHoriz, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"Horizontal","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            sidePanelHoriz.WireTo(id_987196dd20ab4721b0c193bb7a2064f4, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"Vertical","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_987196dd20ab4721b0c193bb7a2064f4.WireTo(id_7b250b222ca44ba2922547f03a4aef49, "children"); /* {"SourceType":"Vertical","SourceIsReference":false,"DestinationType":"TabContainer","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_7b250b222ca44ba2922547f03a4aef49.WireTo(directoryExplorerTab, "childrenTabs"); /* {"SourceType":"TabContainer","SourceIsReference":false,"DestinationType":"Tab","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_42967d39c2334aab9c23697d04177f8a.WireTo(menu_Edit, "children"); /* {"SourceType":"MenuBar","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_42967d39c2334aab9c23697d04177f8a.WireTo(menu_View, "children"); /* {"SourceType":"MenuBar","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_a1f87102954345b69de6841053fce813.WireTo(id_2b3a750d477d4e168aaa3ed0ae548650, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"ConvertToEvent","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            directoryExplorerConfig.WireTo(directoryTreeExplorer, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"DirectoryTree","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_a1f87102954345b69de6841053fce813.WireTo(directoryTreeExplorer, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"DirectoryTree","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":[]} */
            directoryExplorerTab.WireTo(id_e8a68acda2aa4d54add689bd669589d3, "children"); /* {"SourceType":"Tab","SourceIsReference":false,"DestinationType":"Vertical","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_e8a68acda2aa4d54add689bd669589d3.WireTo(projectDirectoryTreeHoriz, "children"); /* {"SourceType":"Vertical","SourceIsReference":false,"DestinationType":"Horizontal","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_642ae4874d1e4fd2a777715cc1996b49.WireTo(id_08a51a5702e34a38af808db65a3a6eb3, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"StateChangeListener","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_08a51a5702e34a38af808db65a3a6eb3.WireTo(id_9d14914fdf0647bb8b4b20ea799e26c8, "stateChanged"); /* {"SourceType":"StateChangeListener","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_9d14914fdf0647bb8b4b20ea799e26c8.WireTo(unhighlightAllWires, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_2a7c8f3b6b5e4879ad5a35ff6d8538fd.WireTo(id_6d789ff1a0bc4a2d8e88733adc266be8, "argsOutput"); /* {"SourceType":"MouseWheelEvent","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["MouseWheelEventArgs"]} */
            id_6d789ff1a0bc4a2d8e88733adc266be8.WireTo(mouseWheelArgs, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["MouseWheelEventArgs"],"DestinationGenerics":["MouseWheelEventArgs"]} */
            id_6d789ff1a0bc4a2d8e88733adc266be8.WireTo(id_33990435606f4bbc9ba1786ed05672ab, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":["MouseWheelEventArgs"],"DestinationGenerics":["MouseWheelEventArgs","bool"]} */
            id_6f93680658e04f8a9ab15337cee1eca3.WireTo(id_a236bd13c516401eb5a83a451a875dd0, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_a236bd13c516401eb5a83a451a875dd0.WireTo(id_6fdaaf997d974e30bbb7c106c40e997c, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_a236bd13c516401eb5a83a451a875dd0.WireTo(id_a3efe072d6b44816a631d90ccef5b71e, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"GetSetting","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            createNewALANode.WireTo(latestAddedNode, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["AbstractionModel","object"],"DestinationGenerics":["object"]} */
            latestAddedNode.WireTo(createAndPaintALAWire, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":["object"],"DestinationGenerics":["object","object"]} */
            A_KeyPressed.WireTo(id_ad29db53c0d64d4b8be9e31474882158, "eventHappened"); /* {"SourceType":"KeyEvent","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_08d455bfa9744704b21570d06c3c5389.WireTo(id_86a7f0259b204907a092da0503eb9873, "children"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_86a7f0259b204907a092da0503eb9873.WireTo(id_3710469340354a1bbb4b9d3371c9c012, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"FolderBrowser","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_3710469340354a1bbb4b9d3371c9c012.WireTo(testDirectoryTree, "selectedFolderPathOutput"); /* {"SourceType":"FolderBrowser","SourceIsReference":false,"DestinationType":"DirectoryTree","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_08d455bfa9744704b21570d06c3c5389.WireTo(testSimulateKeyboard, "children"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            testSimulateKeyboard.WireTo(id_52b8f2c28c2e40cabedbd531171c779a, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_52b8f2c28c2e40cabedbd531171c779a.WireTo(id_5c31090d2c954aa7b4a10e753bdfc03a, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"SimulateKeyboard","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_52b8f2c28c2e40cabedbd531171c779a.WireTo(id_86ecd8f953324e34adc6238338f75db5, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"SimulateKeyboard","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_52b8f2c28c2e40cabedbd531171c779a.WireTo(id_63e463749abe41d28d05b877479070f8, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"SimulateKeyboard","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_52b8f2c28c2e40cabedbd531171c779a.WireTo(id_66e516b6027649e1995a531d03c0c518, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"SimulateKeyboard","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            mainCanvasDisplay.WireTo(CTRL_C_KeyPressed, "eventHandlers"); /* {"SourceType":"CanvasDisplay","SourceIsReference":false,"DestinationType":"KeyEvent","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            CTRL_C_KeyPressed.WireTo(cloneSelectedNodeModel, "eventHappened"); /* {"SourceType":"KeyEvent","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["AbstractionModel"]} */
            id_024b1810c2d24db3b9fac1ccce2fad9e.WireTo(id_0f802a208aad42209777c13b2e61fe56, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":["AbstractionModel"],"DestinationGenerics":["AbstractionModel"]} */
            mainCanvasDisplay.WireTo(CTRL_V_KeyPressed, "eventHandlers"); /* {"SourceType":"CanvasDisplay","SourceIsReference":false,"DestinationType":"KeyEvent","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            createDummyAbstractionModel.WireTo(createNewALANode, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":["AbstractionModel"],"DestinationGenerics":["AbstractionModel","object"]} */
            createAndPaintALAWire.WireTo(id_8647cbf4ac4049a99204b0e3aa70c326, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"ConvertToEvent","DestinationIsReference":false,"Description":"","SourceGenerics":["object","object"],"DestinationGenerics":["object"]} */
            CTRL_V_KeyPressed.WireTo(id_5a22e32e96e641d49c6fb4bdf6fcd94b, "eventHappened"); /* {"SourceType":"KeyEvent","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_ad29db53c0d64d4b8be9e31474882158.WireTo(createDummyAbstractionModel, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["AbstractionModel"]} */
            id_5a22e32e96e641d49c6fb4bdf6fcd94b.WireTo(createDummyAbstractionModel, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["AbstractionModel"]} */
            id_5a22e32e96e641d49c6fb4bdf6fcd94b.WireTo(id_0945b34f58a146ff983962f595f57fb2, "complete"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"DispatcherEvent","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_0945b34f58a146ff983962f595f57fb2.WireTo(id_36c5f05380b04b378de94534411f3f88, "delayedEvent"); /* {"SourceType":"DispatcherEvent","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            CTRL_V_KeyPressed.WireTo(id_4341066281bc4015a668a3bbbcb7256b, "argsOutput"); /* {"SourceType":"KeyEvent","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["KeyEventArgs"]} */
            cloneSelectedNodeModel.WireTo(id_024b1810c2d24db3b9fac1ccce2fad9e, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["AbstractionModel"],"DestinationGenerics":["AbstractionModel"]} */
            id_08d455bfa9744704b21570d06c3c5389.WireTo(id_2c933997055b4122bdb77945f1abb560, "children"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_2c933997055b4122bdb77945f1abb560.WireTo(id_0eea701e0bc84c42a9f17ccc200ef2ef, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["ALANode"]} */
            id_08d455bfa9744704b21570d06c3c5389.WireTo(id_29ed401eb9c240d98bf5c6d1f00c5c76, "children"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_29ed401eb9c240d98bf5c6d1f00c5c76.WireTo(id_fa857dd7432e406c8c6c642152b37730, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["ALANode"]} */
            id_6276e2c141c94ae5a8af58fb7b6f70bf.WireTo(createDiagramFromCode, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"CreateDiagramFromCode","DestinationIsReference":false,"Description":"","SourceGenerics":["Tuple<string, List<string>>"],"DestinationGenerics":[]} */
            id_6276e2c141c94ae5a8af58fb7b6f70bf.WireTo(id_409be365df274cc6a7a124e8a80316a5, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"ConvertToEvent","DestinationIsReference":false,"Description":"","SourceGenerics":["Tuple<string, List<string>>"],"DestinationGenerics":["Tuple<string, List<string>>"]} */
            id_57e7dd98a0874e83bbd5014f7e9c9ef5.WireTo(resetScale, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"Scale","DestinationIsReference":false,"Description":"","SourceGenerics":["UIElement"],"DestinationGenerics":[]} */
            id_409be365df274cc6a7a124e8a80316a5.WireTo(id_82b26eeaba664ee7b2a2c0682e25ce08, "eventOutput"); /* {"SourceType":"ConvertToEvent","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["Tuple<string, List<string>>"],"DestinationGenerics":[]} */
            id_82b26eeaba664ee7b2a2c0682e25ce08.WireTo(id_5e2f0621c62142c1b5972961c93cb725, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["UIElement"]} */
            id_fff8d82dbdd04da18793108f9b8dd5cf.WireTo(id_75ecf8c2602c41829602707be8a8a481, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"ConvertToEvent","DestinationIsReference":false,"Description":"","SourceGenerics":["ALANode"],"DestinationGenerics":["ALANode"]} */
            id_fff8d82dbdd04da18793108f9b8dd5cf.WireTo(navigateToNode, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":["ALANode"],"DestinationGenerics":["ALANode"]} */
            id_fff8d82dbdd04da18793108f9b8dd5cf.WireTo(resetViewOnNode, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":["ALANode"],"DestinationGenerics":["ALANode"]} */
            id_0eea701e0bc84c42a9f17ccc200ef2ef.WireTo(resetViewOnNode, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":["ALANode"],"DestinationGenerics":["ALANode"]} */
            id_fa857dd7432e406c8c6c642152b37730.WireTo(resetViewOnNode, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":["ALANode"],"DestinationGenerics":["ALANode"]} */
            id_5e2f0621c62142c1b5972961c93cb725.WireTo(id_57e7dd98a0874e83bbd5014f7e9c9ef5, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["UIElement"],"DestinationGenerics":["UIElement"]} */
            id_57e7dd98a0874e83bbd5014f7e9c9ef5.WireTo(id_e1e6cf54f73d4f439c6f18b668a73f1a, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":["UIElement"],"DestinationGenerics":["UIElement"]} */
            id_cc0c82a2157f4b0291c812236a6e45ba.WireTo(id_fed56a4aef6748178fa7078388643323, "children"); /* {"SourceType":"Vertical","SourceIsReference":false,"DestinationType":"Horizontal","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            searchTextBox.WireTo(id_00b0ca72bbce4ef4ba5cf395c666a26e, "textOutput"); /* {"SourceType":"TextBox","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            startSearchButton.WireTo(id_5da1d2f5b13746f29802078592e59346, "eventButtonClicked"); /* {"SourceType":"Button","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_5da1d2f5b13746f29802078592e59346.WireTo(id_00b0ca72bbce4ef4ba5cf395c666a26e, "inputDataB"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            id_e8a68acda2aa4d54add689bd669589d3.WireTo(projectDirectoryOptionsHoriz, "children"); /* {"SourceType":"Vertical","SourceIsReference":false,"DestinationType":"Horizontal","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            searchTextBox.WireTo(id_5da1d2f5b13746f29802078592e59346, "eventEnterPressed"); /* {"SourceType":"TextBox","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            searchTab.WireTo(id_cc0c82a2157f4b0291c812236a6e45ba, "children"); /* {"SourceType":"Tab","SourceIsReference":false,"DestinationType":"Vertical","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_3622556a1b37410691b51b83c004a315.WireTo(id_73274d9ce8d5414899772715a1d0f266, "selectedIndex"); /* {"SourceType":"ListDisplay","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["int","ALANode"]} */
            id_73274d9ce8d5414899772715a1d0f266.WireTo(id_fff8d82dbdd04da18793108f9b8dd5cf, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["int","ALANode"],"DestinationGenerics":["ALANode"]} */
            id_75ecf8c2602c41829602707be8a8a481.WireTo(id_5e2f0621c62142c1b5972961c93cb725, "eventOutput"); /* {"SourceType":"ConvertToEvent","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":["ALANode"],"DestinationGenerics":["UIElement"]} */
            currentSearchQuery.WireTo(id_5f1c0f0187eb4dc99f15254fd36fa9b6, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string","IEnumerable<ALANode>"]} */
            id_5f1c0f0187eb4dc99f15254fd36fa9b6.WireTo(id_8e347b7f5f3b4aa6b1c8f1966d0280a3, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"ForEach","DestinationIsReference":false,"Description":"","SourceGenerics":["string","IEnumerable<ALANode>"],"DestinationGenerics":["ALANode"]} */
            id_8e347b7f5f3b4aa6b1c8f1966d0280a3.WireTo(id_282744d2590b4d3e8b337d73c05e0823, "elementOutput"); /* {"SourceType":"ForEach","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["ALANode"],"DestinationGenerics":["ALANode"]} */
            id_1c95fb3a139b4602bba7b10201112546.WireTo(id_2c9472651f984aa8ab763f327bcfa45e, "delayedData"); /* {"SourceType":"DispatcherData","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":["ALANode"],"DestinationGenerics":["ALANode"]} */
            id_8e347b7f5f3b4aa6b1c8f1966d0280a3.WireTo(currentSearchResultIndex, "indexOutput"); /* {"SourceType":"ForEach","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["ALANode"],"DestinationGenerics":["int"]} */
            id_5da1d2f5b13746f29802078592e59346.WireTo(currentSearchQuery, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            id_282744d2590b4d3e8b337d73c05e0823.WireTo(id_1c95fb3a139b4602bba7b10201112546, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"DispatcherData","DestinationIsReference":false,"Description":"","SourceGenerics":["ALANode"],"DestinationGenerics":["ALANode"]} */
            id_282744d2590b4d3e8b337d73c05e0823.WireTo(id_01bdd051f2034331bd9f121029b0e2e8, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"DispatcherData","DestinationIsReference":false,"Description":"","SourceGenerics":["ALANode"],"DestinationGenerics":["ALANode"]} */
            id_01bdd051f2034331bd9f121029b0e2e8.WireTo(id_67bc4eb50bb04d9694a1a0d5ce65c9d9, "delayedData"); /* {"SourceType":"DispatcherData","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":["ALANode"],"DestinationGenerics":["ALANode"]} */
            id_08d455bfa9744704b21570d06c3c5389.WireTo(id_f526f560b3504a0b8115879e5d5354ff, "children"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_f526f560b3504a0b8115879e5d5354ff.WireTo(id_dea56e5fd7174cd7983e8f2c837a941b, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"ContextMenu","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            projectDirectoryTreeHoriz.WireTo(directoryExplorerConfig, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            directoryTreeExplorer.WireTo(currentSelectedDirectoryTreeFilePath, "selectedFullPath"); /* {"SourceType":"DirectoryTree","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            directoryExplorerConfig.WireTo(id_8b908f2be6094d5b8cd3dce5c5fc2b8b, "contextMenuChildren"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_8b908f2be6094d5b8cd3dce5c5fc2b8b.WireTo(id_692716a735e44e948a8d14cd550c1276, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_692716a735e44e948a8d14cd550c1276.WireTo(currentSelectedDirectoryTreeFilePath, "inputDataB"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            id_692716a735e44e948a8d14cd550c1276.WireTo(id_9b866e4112fd4347a2a3e81441401dea, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            id_0d4d34a2cd6749759ac0c2708ddf0cbc.WireTo(id_692716a735e44e948a8d14cd550c1276, "eventButtonClicked"); /* {"SourceType":"Button","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            mainCanvasDisplay.WireTo(F_KeyPressed, "eventHandlers"); /* {"SourceType":"CanvasDisplay","SourceIsReference":false,"DestinationType":"KeyEvent","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            F_KeyPressed.WireTo(id_87a897a783884990bf10e4d7a9e276b9, "eventHappened"); /* {"SourceType":"KeyEvent","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_87a897a783884990bf10e4d7a9e276b9.WireTo(id_9e6a74b0dbea488cba6027ee5187ad0f, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"DispatcherEvent","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_87a897a783884990bf10e4d7a9e276b9.WireTo(id_b55e77a5d78243bf9612ecb7cb20c2c7, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"DispatcherEvent","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_87a897a783884990bf10e4d7a9e276b9.WireTo(id_45593aeb91a145aa9d84d8b77a8d4d8e, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"DispatcherEvent","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_b55e77a5d78243bf9612ecb7cb20c2c7.WireTo(id_a690d6dd37ba4c98b5506777df6dc9db, "delayedEvent"); /* {"SourceType":"DispatcherEvent","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_45593aeb91a145aa9d84d8b77a8d4d8e.WireTo(id_63db7722e48a4c5aabd905f75b0519b2, "delayedEvent"); /* {"SourceType":"DispatcherEvent","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_2ce385b32256413ab2489563287afaac.WireTo(id_006b07cc90c64e398b945bb43fdd4de9, "ifOutput"); /* {"SourceType":"IfElse","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_006b07cc90c64e398b945bb43fdd4de9.WireTo(id_e7da19475fcc44bdaf4a64d05f92b771, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_006b07cc90c64e398b945bb43fdd4de9.WireTo(id_68cfe1cc12f948cab25289d853300813, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"PopupWindow","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_68cfe1cc12f948cab25289d853300813.WireTo(id_95ddd89b36d54db298eaa05165284569, "children"); /* {"SourceType":"PopupWindow","SourceIsReference":false,"DestinationType":"Vertical","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_e7da19475fcc44bdaf4a64d05f92b771.WireTo(latestCodeFilePath, "inputDataB"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            id_89ab09564cea4a8b93d8925e8234e44c.WireTo(id_add742a4683f4dd0b34d8d0eebbe3f07, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"Button","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_c180a82fd3a6495a885e9dde61aaaef3.WireTo(id_e82c1f80e1884a57b79c681462efd65d, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"Button","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_add742a4683f4dd0b34d8d0eebbe3f07.WireTo(id_5fbec6b061cc428a8c00e5c2a652b89e, "eventButtonClicked"); /* {"SourceType":"Button","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_2bfcbb47c2c745578829e1b0f8287f42.WireTo(id_b0d86bb898944ded83ec7f58b9f4a1b8, "eventButtonClicked"); /* {"SourceType":"Button","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_5fbec6b061cc428a8c00e5c2a652b89e.WireTo(id_68cfe1cc12f948cab25289d853300813, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"PopupWindow","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_b0d86bb898944ded83ec7f58b9f4a1b8.WireTo(id_68cfe1cc12f948cab25289d853300813, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"PopupWindow","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_5fbec6b061cc428a8c00e5c2a652b89e.WireTo(id_721b5692fa5a4ba39f509fd7e4a6291b, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_721b5692fa5a4ba39f509fd7e4a6291b.WireTo(latestCodeFilePath, "inputDataB"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            id_721b5692fa5a4ba39f509fd7e4a6291b.WireTo(id_9b866e4112fd4347a2a3e81441401dea, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            id_b0d86bb898944ded83ec7f58b9f4a1b8.WireTo(id_1a403a85264c4074bc7ce5a71262c6c0, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["object"]} */
            id_1a403a85264c4074bc7ce5a71262c6c0.WireTo(id_1928c515b2414f6690c6924a76461081, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"EditSetting","DestinationIsReference":false,"Description":"","SourceGenerics":["object"],"DestinationGenerics":[]} */
            id_c7dc32a5f12b41ad94a910a74de38827.WireTo(id_d890df432c1f4e60a62b8913a5069b34, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"Horizontal","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_e7da19475fcc44bdaf4a64d05f92b771.WireTo(id_e4c9f92bbd6643a286683c9ff5f9fb3a, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string","string"]} */
            id_e4c9f92bbd6643a286683c9ff5f9fb3a.WireTo(id_939726bef757459b914412aead1bb5f9, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"Text","DestinationIsReference":false,"Description":"","SourceGenerics":["string","string"],"DestinationGenerics":[]} */
            id_de49d2fafc2140e996eb38fbf1e62103.WireTo(id_89ab09564cea4a8b93d8925e8234e44c, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_de49d2fafc2140e996eb38fbf1e62103.WireTo(id_c180a82fd3a6495a885e9dde61aaaef3, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_95ddd89b36d54db298eaa05165284569.WireTo(id_5b134e68e31b40f4b3e95eb007a020dc, "children"); /* {"SourceType":"Vertical","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_5b134e68e31b40f4b3e95eb007a020dc.WireTo(id_939726bef757459b914412aead1bb5f9, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"Text","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_95ddd89b36d54db298eaa05165284569.WireTo(id_c7dc32a5f12b41ad94a910a74de38827, "children"); /* {"SourceType":"Vertical","SourceIsReference":false,"DestinationType":"Horizontal","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_de49d2fafc2140e996eb38fbf1e62103.WireTo(id_0fafdba1ad834904ac7330f95dffd966, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_0fafdba1ad834904ac7330f95dffd966.WireTo(id_2bfcbb47c2c745578829e1b0f8287f42, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"Button","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_e82c1f80e1884a57b79c681462efd65d.WireTo(id_1139c3821d834efc947d5c4e949cd1ba, "eventButtonClicked"); /* {"SourceType":"Button","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_1139c3821d834efc947d5c4e949cd1ba.WireTo(id_68cfe1cc12f948cab25289d853300813, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"PopupWindow","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_c7dc32a5f12b41ad94a910a74de38827.WireTo(id_de49d2fafc2140e996eb38fbf1e62103, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"Horizontal","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_c7dc32a5f12b41ad94a910a74de38827.WireTo(id_4686253b1d7d4cd9a4d5bf03d6b7e380, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"Horizontal","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_1928c515b2414f6690c6924a76461081.WireTo(id_f140e9e4ef3f4c07898073fde207da99, "filePathInput"); /* {"SourceType":"EditSetting","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_25a53022f6ab4e9284fd321e9535801b.WireTo(id_3622556a1b37410691b51b83c004a315, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"ListDisplay","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            projectDirectoryOptionsHoriz.WireTo(id_de10db4d6b8a426ba76b02959a58cb88, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_de10db4d6b8a426ba76b02959a58cb88.WireTo(id_0d4d34a2cd6749759ac0c2708ddf0cbc, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"Button","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_7b250b222ca44ba2922547f03a4aef49.WireTo(UIConfig_searchTab, "childrenTabs"); /* {"SourceType":"TabContainer","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            UIConfig_searchTab.WireTo(searchTab, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"Tab","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_fed56a4aef6748178fa7078388643323.WireTo(UIConfig_searchTextBox, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            UIConfig_searchTextBox.WireTo(searchTextBox, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"TextBox","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_fed56a4aef6748178fa7078388643323.WireTo(startSearchButton, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"Button","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_42967d39c2334aab9c23697d04177f8a.WireTo(id_a9db513fb0e749bda7f42b03964e5dce, "children"); /* {"SourceType":"MenuBar","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_42967d39c2334aab9c23697d04177f8a.WireTo(id_efeb87ef1b3c4f9e8ed2f8193e6b78b1, "children"); /* {"SourceType":"MenuBar","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_efeb87ef1b3c4f9e8ed2f8193e6b78b1.WireTo(generateCode, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            startDiagramCreationProcess.WireTo(id_db77c286e64241c48de4fad0dde80024, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            startDiagramCreationProcess.WireTo(id_051136027e944f94b5adcf7f30318e4f, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["Tuple<string, List<string>>"]} */
            startDiagramCreationProcess.WireTo(startRightTreeLayoutProcess, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_a9db513fb0e749bda7f42b03964e5dce.WireTo(id_c9dbe185989e48c0869f984dd8e979f2, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            setting_currentDiagramCodeFilePath.WireTo(id_17609c775b9c4dfcb1f01d427d2911ae, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            id_17609c775b9c4dfcb1f01d427d2911ae.WireTo(id_2810e4e86da348b98b39c987e6ecd7b6, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"FileReader","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":[]} */
            id_c9dbe185989e48c0869f984dd8e979f2.WireTo(id_17609c775b9c4dfcb1f01d427d2911ae, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            id_e778c13b2c894113a7aff7ecfffe48f7.WireTo(mainWindow, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"MainWindow","DestinationIsReference":false,"Description":"","SourceGenerics":["string","string"],"DestinationGenerics":[]} */
            statusBarHorizontal.WireTo(id_e3837af93b584ca9874336851ff0cd31, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_66a3103c3adc426fbc8473b66a8b0d22.WireTo(globalVersionNumberDisplay, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"Text","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_42967d39c2334aab9c23697d04177f8a.WireTo(id_053e6b41724c4dcaad0b79b8924d647d, "children"); /* {"SourceType":"MenuBar","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_4577a8f0f63b4772bdc4eb4cb8581070.WireTo(id_97b81fc9cc04423192a12822a5a5a32e, "fileContentOutput"); /* {"SourceType":"FileReader","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_97b81fc9cc04423192a12822a5a5a32e.WireTo(id_6bc94d5f257847ff8a9a9c45e02333b4, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            id_97b81fc9cc04423192a12822a5a5a32e.WireTo(id_cad49d55268145ab87788c650c6c5473, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"CodeParser","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":[]} */
            id_6625f976171c480ebd8b750aeaf4fab1.WireTo(id_84cf83e5511c4bcb8f83ad289d20b08d, "output"); /* {"SourceType":"Cast","SourceIsReference":false,"DestinationType":"ForEach","DestinationIsReference":false,"Description":"","SourceGenerics":["List<string>","IEnumerable<string>"],"DestinationGenerics":["string"]} */
            id_20566090f5054429aebed4d371c2a613.WireTo(availableProgrammingParadigms, "complete"); /* {"SourceType":"ForEach","SourceIsReference":false,"DestinationType":"Collection","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            availableProgrammingParadigms.WireTo(id_16d8fb2a48ea4eef8839fc7aba053476, "listOutput"); /* {"SourceType":"Collection","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["List<string>"]} */
            id_cad49d55268145ab87788c650c6c5473.WireTo(id_6625f976171c480ebd8b750aeaf4fab1, "interfaces"); /* {"SourceType":"CodeParser","SourceIsReference":false,"DestinationType":"Cast","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["List<string>","IEnumerable<string>"]} */
            id_20566090f5054429aebed4d371c2a613.WireTo(id_4577a8f0f63b4772bdc4eb4cb8581070, "elementOutput"); /* {"SourceType":"ForEach","SourceIsReference":false,"DestinationType":"FileReader","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":[]} */
            id_84cf83e5511c4bcb8f83ad289d20b08d.WireTo(id_d920e0f3fa2d4872af1ec6f3c058c233, "elementOutput"); /* {"SourceType":"ForEach","SourceIsReference":false,"DestinationType":"CodeParser","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":[]} */
            id_d920e0f3fa2d4872af1ec6f3c058c233.WireTo(availableProgrammingParadigms, "name"); /* {"SourceType":"CodeParser","SourceIsReference":false,"DestinationType":"Collection","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_8fc35564768b4a64a57dc321cc1f621f.WireTo(id_670ce4df65564e07912ef2ce63c38e11, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["Dictionary<string, List<string>>","IEnumerable<string>"],"DestinationGenerics":["IEnumerable<string>"]} */
            id_670ce4df65564e07912ef2ce63c38e11.WireTo(id_20566090f5054429aebed4d371c2a613, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"ForEach","DestinationIsReference":false,"Description":"","SourceGenerics":["IEnumerable<string>"],"DestinationGenerics":["string"]} */
            extractALACode.WireTo(startDiagramCreationProcess, "diagramSelected"); /* {"SourceType":"ExtractALACode","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            layoutDiagram.WireTo(id_9240933e26ea4cfdb07e6e7252bf7576, "complete"); /* {"SourceType":"RightTreeLayout","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false,"Description":"","SourceGenerics":["ALANode"],"DestinationGenerics":[]} */
            id_2155bd03579a4918b01e6912a0f24188.WireTo(id_afc4400ecf8b4f3e9aa1a57c346c80b2, "delayedEvent"); /* {"SourceType":"DispatcherEvent","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_35fceab68423425195096666f27475e9.WireTo(id_a98457fc05fc4e84bfb827f480db93d3, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":["Dictionary<string, List<string>>"],"DestinationGenerics":["Dictionary<string, List<string>>","IEnumerable<string>"]} */
            id_670ce4df65564e07912ef2ce63c38e11.WireTo(id_f5d3730393ab40d78baebcb9198808da, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"ForEach","DestinationIsReference":false,"Description":"","SourceGenerics":["IEnumerable<string>"],"DestinationGenerics":["string"]} */
            id_db77c286e64241c48de4fad0dde80024.WireTo(id_2996cb469c4442d08b7e5ca2051336b1, "complete"); /* {"SourceType":"EventLambda","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_2996cb469c4442d08b7e5ca2051336b1.WireTo(id_846c10ca3cc14138bea1d681b146865a, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_2996cb469c4442d08b7e5ca2051336b1.WireTo(id_b6f2ab59cd0642afaf0fc124e6f9f055, "complete"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_b6f2ab59cd0642afaf0fc124e6f9f055.WireTo(id_17609c775b9c4dfcb1f01d427d2911ae, "inputDataB"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            id_b6f2ab59cd0642afaf0fc124e6f9f055.WireTo(id_e778c13b2c894113a7aff7ecfffe48f7, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string","string"]} */
            id_4aff82900db2498e8b46be4a18b9fa8e.WireTo(id_322828528d644ff883d8787c8fb63e56, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_42967d39c2334aab9c23697d04177f8a.WireTo(UIConfig_debugMainMenuItem, "children"); /* {"SourceType":"MenuBar","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            UIConfig_debugMainMenuItem.WireTo(id_08d455bfa9744704b21570d06c3c5389, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_cc3adf40cb654337b01f77ade1881b44.WireTo(sidePanelHoriz, "isChecked"); /* {"SourceType":"CheckBox","SourceIsReference":false,"DestinationType":"Horizontal","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_cc3adf40cb654337b01f77ade1881b44.WireTo(id_a61fc923019942cea819e1b8d1b10384, "check"); /* {"SourceType":"CheckBox","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_9e6a74b0dbea488cba6027ee5187ad0f.WireTo(id_a61fc923019942cea819e1b8d1b10384, "delayedEvent"); /* {"SourceType":"DispatcherEvent","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            menu_View.WireTo(menu_ShowSidePanel, "children"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            menu_ShowSidePanel.WireTo(id_cc3adf40cb654337b01f77ade1881b44, "icon"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"CheckBox","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            menu_ShowSidePanel.WireTo(id_cc3adf40cb654337b01f77ade1881b44, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"CheckBox","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            mainHorizontal.WireTo(UIConfig_canvasDisplayHoriz, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            UIConfig_canvasDisplayHoriz.WireTo(canvasDisplayHoriz, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"Horizontal","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            latestAddedNode.WireTo(id_8b99ce9b4c97466983fc1b14ef889ee8, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"Cast","DestinationIsReference":false,"Description":"","SourceGenerics":["object"],"DestinationGenerics":["object","ALANode"]} */
            id_8b99ce9b4c97466983fc1b14ef889ee8.WireTo(id_fff8d82dbdd04da18793108f9b8dd5cf, "output"); /* {"SourceType":"Cast","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["object","ALANode"],"DestinationGenerics":["ALANode"]} */
            id_581015f073614919a33126efd44bf477.WireTo(id_024172dbe8e2496b97e191244e493973, "children"); /* {"SourceType":"ContextMenu","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_024172dbe8e2496b97e191244e493973.WireTo(id_7e64ef3262604943a2b4a086c5641d09, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["ALANode"]} */
            id_35947f28d1454366ad8ac16e08020905.WireTo(id_fff8d82dbdd04da18793108f9b8dd5cf, "conditionMetOutput"); /* {"SourceType":"ConditionalData","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["ALANode"],"DestinationGenerics":["ALANode"]} */
            id_7e64ef3262604943a2b4a086c5641d09.WireTo(id_35947f28d1454366ad8ac16e08020905, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"ConditionalData","DestinationIsReference":false,"Description":"","SourceGenerics":["ALANode"],"DestinationGenerics":["ALANode"]} */
            id_581015f073614919a33126efd44bf477.WireTo(id_269ffcfe56874f4ba0876a93071234ae, "children"); /* {"SourceType":"ContextMenu","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_269ffcfe56874f4ba0876a93071234ae.WireTo(id_40173af405c9467bbc85c79a05b9da48, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["ALANode"]} */
            id_40173af405c9467bbc85c79a05b9da48.WireTo(id_35947f28d1454366ad8ac16e08020905, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"ConditionalData","DestinationIsReference":false,"Description":"","SourceGenerics":["ALANode"],"DestinationGenerics":["ALANode"]} */
            id_cc0c82a2157f4b0291c812236a6e45ba.WireTo(id_72e0f3f39c364bedb36a74a011e08747, "children"); /* {"SourceType":"Vertical","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_cc0c82a2157f4b0291c812236a6e45ba.WireTo(id_25a53022f6ab4e9284fd321e9535801b, "children"); /* {"SourceType":"Vertical","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_72e0f3f39c364bedb36a74a011e08747.WireTo(id_0fd8aa1777474e3cafb81088519f3d97, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"Horizontal","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_57dc97beb4024bf294c44fea26cc5c89.WireTo(id_b6275330bff140168f4e68c87ed31b54, "content"); /* {"SourceType":"CheckBox","SourceIsReference":false,"DestinationType":"Text","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_0fd8aa1777474e3cafb81088519f3d97.WireTo(id_ecd9f881354d40f485c3fadd9f577974, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_ecd9f881354d40f485c3fadd9f577974.WireTo(id_889bfe8dee4d447d8ea45c19feaf5ca2, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"Text","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_cbdc03ac56ac4f179dd49e1312d7dca0.WireTo(id_abe0267c9c964e2194aa9c5bf84ac413, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"CheckBox","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_abe0267c9c964e2194aa9c5bf84ac413.WireTo(id_edcc6a4999a24fc2ae4b190c5619351c, "content"); /* {"SourceType":"CheckBox","SourceIsReference":false,"DestinationType":"Text","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_b868797a5ef6468abe35342f796a7376.WireTo(id_6dd83767dc324c1bb4e34beafaac11fe, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"CheckBox","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_c5fa777bee784429982813fd34ee9437.WireTo(id_7daf6ef76444402d9e9c6ed68f97a6c2, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"CheckBox","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_7daf6ef76444402d9e9c6ed68f97a6c2.WireTo(id_0e0c54964c4641d2958e710121d0429a, "content"); /* {"SourceType":"CheckBox","SourceIsReference":false,"DestinationType":"Text","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_6dd83767dc324c1bb4e34beafaac11fe.WireTo(id_39ae7418fea245fcaebd3a49b00d0683, "content"); /* {"SourceType":"CheckBox","SourceIsReference":false,"DestinationType":"Text","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_0fd8aa1777474e3cafb81088519f3d97.WireTo(id_b868797a5ef6468abe35342f796a7376, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_0fd8aa1777474e3cafb81088519f3d97.WireTo(id_c5fa777bee784429982813fd34ee9437, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_0fd8aa1777474e3cafb81088519f3d97.WireTo(id_48456b7bb4cf40769ea65b77f071a7f8, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_48456b7bb4cf40769ea65b77f071a7f8.WireTo(id_57dc97beb4024bf294c44fea26cc5c89, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"CheckBox","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_0fd8aa1777474e3cafb81088519f3d97.WireTo(id_cbdc03ac56ac4f179dd49e1312d7dca0, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_6dd83767dc324c1bb4e34beafaac11fe.WireTo(searchFilterNameChecked, "isChecked"); /* {"SourceType":"CheckBox","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["bool"]} */
            id_7daf6ef76444402d9e9c6ed68f97a6c2.WireTo(searchFilterTypeChecked, "isChecked"); /* {"SourceType":"CheckBox","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["bool"]} */
            id_57dc97beb4024bf294c44fea26cc5c89.WireTo(searchFilterInstanceNameChecked, "isChecked"); /* {"SourceType":"CheckBox","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["bool"]} */
            id_abe0267c9c964e2194aa9c5bf84ac413.WireTo(searchFilterFieldsAndPropertiesChecked, "isChecked"); /* {"SourceType":"CheckBox","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["bool"]} */
            UIConfig_mainCanvasDisplay.WireTo(mainCanvasDisplay, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"CanvasDisplay","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            canvasDisplayHoriz.WireTo(UIConfig_mainCanvasDisplay, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            mainCanvasDisplay.WireTo(id_dd7bf35a9a7c42059c340c211b761af9, "eventHandlers"); /* {"SourceType":"CanvasDisplay","SourceIsReference":false,"DestinationType":"DragEvent","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_dd7bf35a9a7c42059c340c211b761af9.WireTo(getDroppedFilePaths, "argsOutput"); /* {"SourceType":"DragEvent","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["DragEventArgs","List<string>"]} */
            id_efd2a2dc177542c587c73a55def6fe3c.WireTo(addAbstractionsToAllNodes, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":["List<string>"],"DestinationGenerics":["List<string>","List<string>"]} */
            getDroppedFilePaths.WireTo(id_efd2a2dc177542c587c73a55def6fe3c, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["DragEventArgs","List<string>"],"DestinationGenerics":["List<string>"]} */
            addAbstractionsToAllNodes.WireTo(id_3e341111f8224aa7b947f522ef1f65ab, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":["List<string>","List<string>"],"DestinationGenerics":["List<string>","string"]} */
            id_3e341111f8224aa7b947f522ef1f65ab.WireTo(updateStatusMessage, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":["List<string>","string"],"DestinationGenerics":["string"]} */
            id_053e6b41724c4dcaad0b79b8924d647d.WireTo(id_0718ee88fded4b7b88258796df7db577, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_0718ee88fded4b7b88258796df7db577.WireTo(id_c359484e1d7147a09d63c0671fa5f1dd, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"HttpRequest","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_c359484e1d7147a09d63c0671fa5f1dd.WireTo(id_db35acd5215c41849c685c49fba07a3d, "responseJsonOutput"); /* {"SourceType":"HttpRequest","SourceIsReference":false,"DestinationType":"JSONParser","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            compareVersionNumbers.WireTo(id_e33aaa2a4a5544a89931f05048e68406, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"IfElse","DestinationIsReference":false,"Description":"","SourceGenerics":["string","bool"],"DestinationGenerics":[]} */
            id_66a3103c3adc426fbc8473b66a8b0d22.WireTo(id_b47ca3c51c95416383ba250af31ee564, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"Text","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_66a3103c3adc426fbc8473b66a8b0d22.WireTo(id_07f10e1650504d298bdceddff2402f31, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"Text","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_5c857c3a1a474ec19c0c3b054627c0a9.WireTo(id_66a3103c3adc426fbc8473b66a8b0d22, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"Horizontal","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            statusBarHorizontal.WireTo(id_b1a5dcbe40654113b08efc4299c6fdc2, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"Text","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            statusBarHorizontal.WireTo(id_5c857c3a1a474ec19c0c3b054627c0a9, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_642ae4874d1e4fd2a777715cc1996b49.WireTo(id_ae21c0350891480babdcd1efcb247295, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"Clock","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_ae21c0350891480babdcd1efcb247295.WireTo(id_0718ee88fded4b7b88258796df7db577, "eventHappened"); /* {"SourceType":"Clock","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_a46f4ed8460e421b97525bd352b58d85.WireTo(id_34c59781fa2f4c5fb9102b7a65c461a0, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_e33aaa2a4a5544a89931f05048e68406.WireTo(id_a46f4ed8460e421b97525bd352b58d85, "ifOutput"); /* {"SourceType":"IfElse","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_a46f4ed8460e421b97525bd352b58d85.WireTo(id_0e88688a360d451ab58c2fa25c9bf109, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_e33aaa2a4a5544a89931f05048e68406.WireTo(id_57972aa4bbc24e46b4b6171637d31440, "elseOutput"); /* {"SourceType":"IfElse","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_57972aa4bbc24e46b4b6171637d31440.WireTo(id_0e88688a360d451ab58c2fa25c9bf109, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_34c59781fa2f4c5fb9102b7a65c461a0.WireTo(id_b47ca3c51c95416383ba250af31ee564, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"Text","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":[]} */
            id_0e88688a360d451ab58c2fa25c9bf109.WireTo(id_07f10e1650504d298bdceddff2402f31, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"Text","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":[]} */
            id_76de2a3c1e5f4fbbbe8928be48e25847.WireTo(id_b47ca3c51c95416383ba250af31ee564, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"Text","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":[]} */
            id_db35acd5215c41849c685c49fba07a3d.WireTo(latestVersion, "jsonOutput"); /* {"SourceType":"JSONParser","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            latestVersion.WireTo(compareVersionNumbers, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string","bool"]} */
            id_57972aa4bbc24e46b4b6171637d31440.WireTo(id_76de2a3c1e5f4fbbbe8928be48e25847, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            menu_OpenProject.WireTo(id_463b31fe2ac04972b5055a3ff2f74fe3, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"FolderBrowser","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_cdeb94e2daee4057966eba31781ebd0d.WireTo(id_45968f4d70794b7c994c8e0f6ee5093a, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_42967d39c2334aab9c23697d04177f8a.WireTo(id_8ebb92deea4c4abf846371db834d9f87, "children"); /* {"SourceType":"MenuBar","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_42967d39c2334aab9c23697d04177f8a.WireTo(id_4aff82900db2498e8b46be4a18b9fa8e, "children"); /* {"SourceType":"MenuBar","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_8ebb92deea4c4abf846371db834d9f87.WireTo(id_835b587c7faf4fabbbe71010d28d9280, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_ab1d0ec0d92f4befb1ff44bb72cc8e10.WireTo(id_3a7125ae5c814928a55c2d29e7e8c132, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_f8930a779bd44b0792fbd4a43b3874c6.WireTo(id_11418b009831455983cbc07c8d116a1f, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"CheckBox","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_87a535a0e11441af9072d6364a8aef74.WireTo(id_11418b009831455983cbc07c8d116a1f, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"CheckBox","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_3a7125ae5c814928a55c2d29e7e8c132.WireTo(id_f8930a779bd44b0792fbd4a43b3874c6, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_f8930a779bd44b0792fbd4a43b3874c6.WireTo(startRightTreeLayoutProcess, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_943e3971561d493d97e38a8e29fb87dc.WireTo(id_954c2d01269c4632a4ddccd75cde9fde, "icon"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"CheckBox","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_943e3971561d493d97e38a8e29fb87dc.WireTo(id_cd6186e0fe844be586191519012bb72e, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_cd6186e0fe844be586191519012bb72e.WireTo(id_954c2d01269c4632a4ddccd75cde9fde, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"CheckBox","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            startRightTreeLayoutProcess.WireTo(id_0f0046b6b91e447aa9bf0a223fd59038, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["bool"]} */
            startGuaranteedLayoutProcess.WireTo(id_4a268943755348b68ee2cb6b71f73c40, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"DispatcherEvent","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            startRightTreeLayoutProcess.WireTo(id_1e62a1e411c9464c94ee234dd9dd3fdc, "complete"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_edd3648585f44954b2df337f1b7a793b.WireTo(startGuaranteedLayoutProcess, "ifOutput"); /* {"SourceType":"IfElse","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_4a268943755348b68ee2cb6b71f73c40.WireTo(initialiseRightTreeLayout, "delayedEvent"); /* {"SourceType":"DispatcherEvent","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            menu_View.WireTo(id_50349b82433f42ebb9d1ce591fc3bc35, "children"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_50349b82433f42ebb9d1ce591fc3bc35.WireTo(id_943e3971561d493d97e38a8e29fb87dc, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_f9b8e7f524a14884be753d19a351a285.WireTo(id_27ff7a25d9034a45a229edef6610e214, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["bool"]} */
            id_27ff7a25d9034a45a229edef6610e214.WireTo(id_d5c22176b9bb49dd91a1cb0a7e3f7196, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["bool"],"DestinationGenerics":["bool"]} */
            id_954c2d01269c4632a4ddccd75cde9fde.WireTo(useAutomaticLayout, "isChecked"); /* {"SourceType":"CheckBox","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["bool"]} */
            id_3a7125ae5c814928a55c2d29e7e8c132.WireTo(id_87a535a0e11441af9072d6364a8aef74, "icon"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_11418b009831455983cbc07c8d116a1f.WireTo(id_ce0bcc39dd764d1087816b79eefa76bf, "isChecked"); /* {"SourceType":"CheckBox","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["bool"]} */
            id_0f0046b6b91e447aa9bf0a223fd59038.WireTo(id_edd3648585f44954b2df337f1b7a793b, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"IfElse","DestinationIsReference":false,"Description":"","SourceGenerics":["bool"],"DestinationGenerics":[]} */
            id_0f0046b6b91e447aa9bf0a223fd59038.WireTo(useAutomaticLayout, "inputDataB"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["bool"],"DestinationGenerics":["bool"]} */
            initialiseRightTreeLayout.WireTo(id_7356212bcc714c699681e8dffc853761, "complete"); /* {"SourceType":"EventLambda","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            getTreeParentsFromGraph.WireTo(id_ec0f30ce468d4986abb9ad81abe73c17, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":["Dictionary<string, ALANode>"],"DestinationGenerics":["Dictionary<string, ALANode>"]} */
            menu_View.WireTo(id_ab1d0ec0d92f4befb1ff44bb72cc8e10, "children"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            mainCanvasDisplay.WireTo(CTRL_Up_KeyPressed, "eventHandlers"); /* {"SourceType":"CanvasDisplay","SourceIsReference":false,"DestinationType":"KeyEvent","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            CTRL_Up_KeyPressed.WireTo(id_3c565e37c3c1486e91007c4d1d284367, "argsOutput"); /* {"SourceType":"KeyEvent","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["KeyEventArgs"]} */
            mainCanvasDisplay.WireTo(CTRL_Down_KeyPressed, "eventHandlers"); /* {"SourceType":"CanvasDisplay","SourceIsReference":false,"DestinationType":"KeyEvent","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            CTRL_Down_KeyPressed.WireTo(id_29a954d80a1a43ca8739e70022ebf3ec, "argsOutput"); /* {"SourceType":"KeyEvent","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["KeyEventArgs"]} */
            id_9240933e26ea4cfdb07e6e7252bf7576.WireTo(id_2155bd03579a4918b01e6912a0f24188, "complete"); /* {"SourceType":"EventLambda","SourceIsReference":false,"DestinationType":"DispatcherEvent","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_42967d39c2334aab9c23697d04177f8a.WireTo(menu_Tools, "children"); /* {"SourceType":"MenuBar","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_8eb5d9903d6941d285da2fc3d2ccfc3a.WireTo(id_7c21cf85883041b88e998ecc065cc4d4, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            menu_Tools.WireTo(id_8eb5d9903d6941d285da2fc3d2ccfc3a, "children"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_08d455bfa9744704b21570d06c3c5389.WireTo(id_180fa624d01c4759a83050e30426343a, "children"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_514f6109e8a24bc4b1ced57aaa255d90.WireTo(id_df9b787cea7845f88e1faf65240adb4f, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_514f6109e8a24bc4b1ced57aaa255d90.WireTo(id_5aec7a9782644198ab22d9ed7998ee15, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"PopupWindow","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_1c8c1eff6c1042cdb09364f0d4e80cf5.WireTo(id_23e510bd08224b64b10c378f0f8fcdfe, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"TextBox","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_7c21cf85883041b88e998ecc065cc4d4.WireTo(id_514f6109e8a24bc4b1ced57aaa255d90, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_180fa624d01c4759a83050e30426343a.WireTo(id_514f6109e8a24bc4b1ced57aaa255d90, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            createInstanceDictionaryCode.WireTo(id_23e510bd08224b64b10c378f0f8fcdfe, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"TextBox","DestinationIsReference":false,"Description":"","SourceGenerics":["string","string"],"DestinationGenerics":[]} */
            id_5b1aec35b5fd47e482a25168390fcd66.WireTo(id_65e62fc671b1436191ccdc2a2e8c8af8, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"Horizontal","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_a1b1ae6b9ca64970b5b8988be0b5dda7.WireTo(id_5b1aec35b5fd47e482a25168390fcd66, "children"); /* {"SourceType":"Vertical","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_a1b1ae6b9ca64970b5b8988be0b5dda7.WireTo(id_1c8c1eff6c1042cdb09364f0d4e80cf5, "children"); /* {"SourceType":"Vertical","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_df9b787cea7845f88e1faf65240adb4f.WireTo(createInstanceDictionaryCode, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string","string"]} */
            id_5aec7a9782644198ab22d9ed7998ee15.WireTo(id_a1b1ae6b9ca64970b5b8988be0b5dda7, "children"); /* {"SourceType":"PopupWindow","SourceIsReference":false,"DestinationType":"Vertical","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_65e62fc671b1436191ccdc2a2e8c8af8.WireTo(id_ca4344b0f1334536b8ba52fda7567809, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_65e62fc671b1436191ccdc2a2e8c8af8.WireTo(id_e4615109bbba480cb0f7c11cc493cd84, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_ca4344b0f1334536b8ba52fda7567809.WireTo(id_740a947e8deb4a26868e4858d59387de, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"Text","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_e4615109bbba480cb0f7c11cc493cd84.WireTo(id_a1163328ed694682ad454ff0f88e4dfe, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"TextBox","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_a1163328ed694682ad454ff0f88e4dfe.WireTo(id_e7a7ac196c52416aa49fc77fe0503251, "textOutput"); /* {"SourceType":"TextBox","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_65e62fc671b1436191ccdc2a2e8c8af8.WireTo(id_28f139af6d3941658d65e5c08a79006d, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_28f139af6d3941658d65e5c08a79006d.WireTo(id_a96a45b9b88648ebbf6ea3d24f036269, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"Button","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_a96a45b9b88648ebbf6ea3d24f036269.WireTo(id_b8f48b755a8545fcb626463d325ffe03, "eventButtonClicked"); /* {"SourceType":"Button","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_b8f48b755a8545fcb626463d325ffe03.WireTo(id_e7a7ac196c52416aa49fc77fe0503251, "inputDataB"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            id_b8f48b755a8545fcb626463d325ffe03.WireTo(createInstanceDictionaryCode, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string","string"]} */
            id_a1163328ed694682ad454ff0f88e4dfe.WireTo(id_b8f48b755a8545fcb626463d325ffe03, "eventEnterPressed"); /* {"SourceType":"TextBox","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_94be5f8fa9014fad81fa832cdfb41c27.WireTo(id_61311ea1bf8d405db0411618a8e11114, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["ALAWire"]} */
            id_61311ea1bf8d405db0411618a8e11114.WireTo(id_831cf2bc59df431e9171a3887608cfae, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":["ALAWire"],"DestinationGenerics":["ALAWire"]} */
            id_e3a05ca012df4e428f19f313109a576e.WireTo(id_b8876ba6078448999ae1746d34ce803e, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["ALAWire"]} */
            id_b8876ba6078448999ae1746d34ce803e.WireTo(id_cc2aa50e0aef463ca17350d36436f98d, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":["ALAWire"],"DestinationGenerics":["ALAWire"]} */
            CTRL_Up_KeyPressed.WireTo(id_94be5f8fa9014fad81fa832cdfb41c27, "eventHappened"); /* {"SourceType":"KeyEvent","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_94be5f8fa9014fad81fa832cdfb41c27.WireTo(id_6377d8cb849a4a07b02d50789eab57a1, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"DispatcherEvent","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_6377d8cb849a4a07b02d50789eab57a1.WireTo(startGuaranteedLayoutProcess, "delayedEvent"); /* {"SourceType":"DispatcherEvent","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            CTRL_Down_KeyPressed.WireTo(id_e3a05ca012df4e428f19f313109a576e, "eventHappened"); /* {"SourceType":"KeyEvent","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_e3a05ca012df4e428f19f313109a576e.WireTo(id_6306c5f7aa3d41978599c00a5999b96f, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"DispatcherEvent","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_6306c5f7aa3d41978599c00a5999b96f.WireTo(startGuaranteedLayoutProcess, "delayedEvent"); /* {"SourceType":"DispatcherEvent","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_33d648af590b45139339fe533079ab12.WireTo(id_3605f8d8e4624d84befb96fe76ebd3ac, "eventOutput"); /* {"SourceType":"ConvertToEvent","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":[]} */
            menu_File.WireTo(menu_OpenRecentProjects, "children"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"MultiMenu","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_460891130e9e499184b84a23c2e43c9f.WireTo(id_e2c110ecff0740989d3d30144f84a94b, "output"); /* {"SourceType":"Cast","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["string","object"],"DestinationGenerics":["object"]} */
            id_408df459fb4c4846920b1a1edd4ac9e6.WireTo(id_6ecefc4cdc694ef2a46a8628cadc0e1d, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"GetSetting","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_6ecefc4cdc694ef2a46a8628cadc0e1d.WireTo(id_097392c5af294d32b5c928a590bad83b, "settingJsonOutput"); /* {"SourceType":"GetSetting","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string","List<string>"]} */
            id_097392c5af294d32b5c928a590bad83b.WireTo(recentProjectPaths, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["string","List<string>"],"DestinationGenerics":["List<string>"]} */
            id_2b3a750d477d4e168aaa3ed0ae548650.WireTo(id_408df459fb4c4846920b1a1edd4ac9e6, "eventOutput"); /* {"SourceType":"ConvertToEvent","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":[]} */
            id_408df459fb4c4846920b1a1edd4ac9e6.WireTo(id_e045b91666df454ca2f7985443af56c5, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["object"]} */
            id_e045b91666df454ca2f7985443af56c5.WireTo(id_e2c110ecff0740989d3d30144f84a94b, "inputDataB"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["object"],"DestinationGenerics":["object"]} */
            id_cb85f096416943cb9c08e4862f304568.WireTo(id_ef711f01535e48e2b65274af24d732f6, "output"); /* {"SourceType":"Cast","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":["object","string"],"DestinationGenerics":["string","object"]} */
            id_4ad460d4bd8d4a63ad7aca7ed9f1c945.WireTo(id_6c8e7b486e894c6ca6bebaf40775b8b4, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"EditSetting","DestinationIsReference":false,"Description":"","SourceGenerics":["object"],"DestinationGenerics":[]} */
            id_4ad460d4bd8d4a63ad7aca7ed9f1c945.WireTo(id_5d9313a0a895402cb6be531e87c9b606, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":["object"],"DestinationGenerics":["object","List<string>"]} */
            id_6c8e7b486e894c6ca6bebaf40775b8b4.WireTo(id_ecfbf0b7599e4340b8b2f79b7d1e29cb, "filePathInput"); /* {"SourceType":"EditSetting","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_e045b91666df454ca2f7985443af56c5.WireTo(id_cb85f096416943cb9c08e4862f304568, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"Cast","DestinationIsReference":false,"Description":"","SourceGenerics":["object"],"DestinationGenerics":["object","string"]} */
            id_ef711f01535e48e2b65274af24d732f6.WireTo(id_4ad460d4bd8d4a63ad7aca7ed9f1c945, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["string","object"],"DestinationGenerics":["object"]} */
            id_5d9313a0a895402cb6be531e87c9b606.WireTo(menu_OpenRecentProjects, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"MultiMenu","DestinationIsReference":false,"Description":"","SourceGenerics":["object","List<string>"],"DestinationGenerics":[]} */
            id_6ecefc4cdc694ef2a46a8628cadc0e1d.WireTo(id_fcfcb5f0ae544c968dcbc734ac1db51b, "filePathInput"); /* {"SourceType":"GetSetting","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            menu_OpenRecentProjects.WireTo(id_a1f87102954345b69de6841053fce813, "selectedLabel"); /* {"SourceType":"MultiMenu","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_e372e7c636a14549bba7cb5992874716.WireTo(id_d386225d5368436185ff7e18a6dfd91a, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_d386225d5368436185ff7e18a6dfd91a.WireTo(id_355e5bd4d98745b2a42eb1266198128b, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"TextClipboard","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_355e5bd4d98745b2a42eb1266198128b.WireTo(id_ceae580b14444b1e82c23813f47a47cd, "contentOutput"); /* {"SourceType":"TextClipboard","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string","string"]} */
            id_6180563898dc46da87f68e3da6bc7aa8.WireTo(pasteDiagramFromCode, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"CreateDiagramFromCode","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":[]} */
            id_ceae580b14444b1e82c23813f47a47cd.WireTo(id_6180563898dc46da87f68e3da6bc7aa8, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["string","string"],"DestinationGenerics":["string"]} */
            id_6180563898dc46da87f68e3da6bc7aa8.WireTo(id_6bc55844fa8f41db9a95118685504fd1, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"ConvertToEvent","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            id_6bc55844fa8f41db9a95118685504fd1.WireTo(startRightTreeLayoutProcess, "eventOutput"); /* {"SourceType":"ConvertToEvent","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":[]} */
            id_581015f073614919a33126efd44bf477.WireTo(id_e372e7c636a14549bba7cb5992874716, "children"); /* {"SourceType":"ContextMenu","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_581015f073614919a33126efd44bf477.WireTo(id_ec06a192a3b9424e996af338bd0e1699, "children"); /* {"SourceType":"ContextMenu","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_110e0e17c17e481291e3a1669fd3edaf.WireTo(id_2c58e14cba984eb89065062bde6593be, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["ALAWire"]} */
            id_ec06a192a3b9424e996af338bd0e1699.WireTo(id_110e0e17c17e481291e3a1669fd3edaf, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_f135f2c631b941d4916589a8fb078d6e.WireTo(id_192fe80aafb34059af0f997434d4eb24, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":["ALAWire"],"DestinationGenerics":["ALAWire"]} */
            id_f135f2c631b941d4916589a8fb078d6e.WireTo(id_1cfa104de254494cb1d4552604cc6b94, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"ConvertToEvent","DestinationIsReference":false,"Description":"","SourceGenerics":["ALAWire"],"DestinationGenerics":["ALAWire"]} */
            id_f135f2c631b941d4916589a8fb078d6e.WireTo(id_887caaa328ff409aa0c37fbcf3fac2b4, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":["ALAWire"],"DestinationGenerics":["ALAWire"]} */
            id_2c58e14cba984eb89065062bde6593be.WireTo(id_f135f2c631b941d4916589a8fb078d6e, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["ALAWire"],"DestinationGenerics":["ALAWire"]} */
            id_1cfa104de254494cb1d4552604cc6b94.WireTo(createDummyAbstractionModel, "eventOutput"); /* {"SourceType":"ConvertToEvent","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":["ALAWire"],"DestinationGenerics":["AbstractionModel"]} */
            id_110e0e17c17e481291e3a1669fd3edaf.WireTo(startGuaranteedLayoutProcess, "complete"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            menu_Edit.WireTo(menu_NodeSpacing, "children"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            menu_NodeSpacing.WireTo(id_72c67c7f881142c99b7021fc1f3ae6ad, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"PopupWindow","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_72c67c7f881142c99b7021fc1f3ae6ad.WireTo(id_d5ea8f6014a44f6faf59a7b5768bcadf, "children"); /* {"SourceType":"PopupWindow","SourceIsReference":false,"DestinationType":"Vertical","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_97b80479ebba440d94a51a888044a581.WireTo(id_54c8bc7425ab4b4580b5584852487782, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"Horizontal","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_54c8bc7425ab4b4580b5584852487782.WireTo(id_d696f92299cb4a86bfda5c0d70f3e6ce, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"Text","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_0523989e80a342fa830a12c31e976794.WireTo(id_eacca7f00d5f424ea8a0d2a460f70862, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"Text","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_477f5f3243c6416f99fbf40d65945e0e.WireTo(id_0523989e80a342fa830a12c31e976794, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"Horizontal","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_54c8bc7425ab4b4580b5584852487782.WireTo(id_e5180cbda745469b99d3d52c17f49119, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"TextBox","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_0523989e80a342fa830a12c31e976794.WireTo(id_6958ea0957fa4d8781c5ee3bbdaee6fd, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"TextBox","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_e5180cbda745469b99d3d52c17f49119.WireTo(id_ea6694878c8c44c28f2d054ee089c12e, "textOutput"); /* {"SourceType":"TextBox","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_6958ea0957fa4d8781c5ee3bbdaee6fd.WireTo(id_c2835f5e1f3149ccb42f1865fa67de55, "textOutput"); /* {"SourceType":"TextBox","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_977932d8d02445979383614993bac82c.WireTo(id_a417fd2a5a144349b36c5e149810c442, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"Button","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_d5ea8f6014a44f6faf59a7b5768bcadf.WireTo(id_97b80479ebba440d94a51a888044a581, "children"); /* {"SourceType":"Vertical","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_d5ea8f6014a44f6faf59a7b5768bcadf.WireTo(id_477f5f3243c6416f99fbf40d65945e0e, "children"); /* {"SourceType":"Vertical","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_d5ea8f6014a44f6faf59a7b5768bcadf.WireTo(id_977932d8d02445979383614993bac82c, "children"); /* {"SourceType":"Vertical","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_a417fd2a5a144349b36c5e149810c442.WireTo(id_72c67c7f881142c99b7021fc1f3ae6ad, "eventButtonClicked"); /* {"SourceType":"Button","SourceIsReference":false,"DestinationType":"PopupWindow","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            extractALACode.WireTo(currentDiagramCode, "selectedDiagram"); /* {"SourceType":"ExtractALACode","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["Tuple<string, List<string>>"]} */
            id_051136027e944f94b5adcf7f30318e4f.WireTo(currentDiagramCode, "inputDataB"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["Tuple<string, List<string>>"],"DestinationGenerics":["Tuple<string, List<string>>"]} */
            id_051136027e944f94b5adcf7f30318e4f.WireTo(id_6276e2c141c94ae5a8af58fb7b6f70bf, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["Tuple<string, List<string>>"],"DestinationGenerics":["Tuple<string, List<string>>"]} */
            menu_File.WireTo(menu_OpenDiagram, "children"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"MultiMenu","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            extractALACode.WireTo(allDiagramsCode, "allDiagrams"); /* {"SourceType":"ExtractALACode","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["Dictionary<string, Tuple<string, List<string>>>"]} */
            allDiagramsCode.WireTo(getDiagramList, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":["Dictionary<string, Tuple<string, List<string>>>"],"DestinationGenerics":["Dictionary<string, Tuple<string, List<string>>>","List<string>"]} */
            currentApplicationDiagramNameList.WireTo(menu_OpenDiagram, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"MultiMenu","DestinationIsReference":false,"Description":"","SourceGenerics":["List<string>"],"DestinationGenerics":[]} */
            id_38501a618c7b4b1aac1194f24f8d325d.WireTo(currentDiagramCode, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["string","Tuple<string, List<string>>"],"DestinationGenerics":["Tuple<string, List<string>>"]} */
            id_0bcb2cfeb90d43a5973f21d2e4c50dcc.WireTo(id_782197630669407095b6042ba91bbc4b, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"ConvertToEvent","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            id_0bcb2cfeb90d43a5973f21d2e4c50dcc.WireTo(currentDiagramName, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            id_0bcb2cfeb90d43a5973f21d2e4c50dcc.WireTo(id_38501a618c7b4b1aac1194f24f8d325d, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string","Tuple<string, List<string>>"]} */
            id_846c10ca3cc14138bea1d681b146865a.WireTo(currentDiagramName, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            menu_OpenDiagram.WireTo(id_0bcb2cfeb90d43a5973f21d2e4c50dcc, "selectedLabel"); /* {"SourceType":"MultiMenu","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_0bcb2cfeb90d43a5973f21d2e4c50dcc.WireTo(id_dccd548f0c18412385231185ef028374, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"ConvertToEvent","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            id_dccd548f0c18412385231185ef028374.WireTo(startDiagramCreationProcess, "eventOutput"); /* {"SourceType":"ConvertToEvent","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":[]} */
            id_581015f073614919a33126efd44bf477.WireTo(menu_OpenSelectedNodesDiagram, "children"); /* {"SourceType":"ContextMenu","SourceIsReference":false,"DestinationType":"MultiMenu","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            getDiagramList.WireTo(currentApplicationDiagramNameList, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["Dictionary<string, Tuple<string, List<string>>>","List<string>"],"DestinationGenerics":["List<string>"]} */
            id_de1d2e0c58cd4c4b989f5311740a2253.WireTo(menu_OpenSelectedNodesDiagram, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"MultiMenu","DestinationIsReference":false,"Description":"","SourceGenerics":["ALANode","List<string>"],"DestinationGenerics":[]} */
            id_4543ca6d3d6a47789f52e4cc7d841ee5.WireTo(id_0bcb2cfeb90d43a5973f21d2e4c50dcc, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            menu_OpenSelectedNodesDiagram.WireTo(id_119b86267f5046bca55e50432b342474, "isOpening"); /* {"SourceType":"MultiMenu","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["ALANode"]} */
            id_043e1ea3b057405a8c266456acdd97da.WireTo(id_de1d2e0c58cd4c4b989f5311740a2253, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":["ALANode"],"DestinationGenerics":["ALANode","List<string>"]} */
            id_119b86267f5046bca55e50432b342474.WireTo(id_043e1ea3b057405a8c266456acdd97da, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["ALANode"],"DestinationGenerics":["ALANode"]} */
            menu_OpenSelectedNodesDiagram.WireTo(id_4543ca6d3d6a47789f52e4cc7d841ee5, "selectedLabel"); /* {"SourceType":"MultiMenu","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_4543ca6d3d6a47789f52e4cc7d841ee5.WireTo(id_8398ff1988b344c1841ea38cde6e1ce3, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"ConvertToEvent","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            id_8398ff1988b344c1841ea38cde6e1ce3.WireTo(getMatchingNodeInNewGraph, "eventOutput"); /* {"SourceType":"ConvertToEvent","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["ALANode"]} */
            getMatchingNodeInNewGraph.WireTo(id_fff8d82dbdd04da18793108f9b8dd5cf, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["ALANode"],"DestinationGenerics":["ALANode"]} */
            menu_Tools.WireTo(id_24f0cee4833c4971b8c9cb2bdbb1c868, "children"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_24f0cee4833c4971b8c9cb2bdbb1c868.WireTo(id_25ed8d5621754358bb15633274ef191a, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            menu_Tools.WireTo(id_eeaef5bfff254a4f850d013288ef44fa, "children"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_eeaef5bfff254a4f850d013288ef44fa.WireTo(id_c7ff61bc312843019dde00238832d5a1, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"PopupWindow","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_f32979c2ffca4ab1b028ced3255bb68d.WireTo(id_56042a0e94ed4d599965f1f9c4fb7b8c, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_56042a0e94ed4d599965f1f9c4fb7b8c.WireTo(id_a1e3c8eca2ee4f75ab41465c2bc1a9a9, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"Text","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_f73625a393984a259b173f4d60d92b60.WireTo(id_f32979c2ffca4ab1b028ced3255bb68d, "children"); /* {"SourceType":"Vertical","SourceIsReference":false,"DestinationType":"Horizontal","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_f32979c2ffca4ab1b028ced3255bb68d.WireTo(id_55aaae3200544344b396fd2e8a63a03c, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_55aaae3200544344b396fd2e8a63a03c.WireTo(id_f736325c8e074ccf92f128ffa9b3d68f, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"TextBox","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_f73625a393984a259b173f4d60d92b60.WireTo(id_9bf12baaecbe45658c5861327b37fcde, "children"); /* {"SourceType":"Vertical","SourceIsReference":false,"DestinationType":"Horizontal","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_9bf12baaecbe45658c5861327b37fcde.WireTo(id_13ee0053aac24b538d4b0bc94606974f, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"UIConfig","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_13ee0053aac24b538d4b0bc94606974f.WireTo(id_509962caea7b49198c21f0b4c3f7db66, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"Button","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_c7ff61bc312843019dde00238832d5a1.WireTo(id_f73625a393984a259b173f4d60d92b60, "children"); /* {"SourceType":"PopupWindow","SourceIsReference":false,"DestinationType":"Vertical","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_f736325c8e074ccf92f128ffa9b3d68f.WireTo(id_f0902adcccf946fe9050b5d783af0277, "textOutput"); /* {"SourceType":"TextBox","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_f736325c8e074ccf92f128ffa9b3d68f.WireTo(id_6fd8927aa820450b8b1e04638661d9ce, "eventEnterPressed"); /* {"SourceType":"TextBox","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_c10e8ba484d44b708f02664b52e48a25.WireTo(id_f0902adcccf946fe9050b5d783af0277, "inputDataB"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            id_6fd8927aa820450b8b1e04638661d9ce.WireTo(id_c10e8ba484d44b708f02664b52e48a25, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_c10e8ba484d44b708f02664b52e48a25.WireTo(id_d5a44d60211d4434a97b6e04f4c44887, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string","string"]} */
            id_d5a44d60211d4434a97b6e04f4c44887.WireTo(id_b49ee56548054b7b91bc4a6863b68112, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"TextClipboard","DestinationIsReference":false,"Description":"","SourceGenerics":["string","string"],"DestinationGenerics":[]} */
            id_509962caea7b49198c21f0b4c3f7db66.WireTo(id_6fd8927aa820450b8b1e04638661d9ce, "eventButtonClicked"); /* {"SourceType":"Button","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_6fd8927aa820450b8b1e04638661d9ce.WireTo(id_c7ff61bc312843019dde00238832d5a1, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"PopupWindow","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_a808288fa4ae48b0a33de1fda8e4b58a.WireTo(setting_currentDiagramCodeFilePath, "inputDataB"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            id_a808288fa4ae48b0a33de1fda8e4b58a.WireTo(id_5997270bf4614726ac236d5536fa79ab, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"FileReader","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":[]} */
            id_5997270bf4614726ac236d5536fa79ab.WireTo(id_590d452a50e4468ca15074a88f59f6d6, "fileContentOutput"); /* {"SourceType":"FileReader","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            id_782197630669407095b6042ba91bbc4b.WireTo(id_d0697034644f4faa9dbc1f263f45708c, "eventOutput"); /* {"SourceType":"ConvertToEvent","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":[]} */
            id_d0697034644f4faa9dbc1f263f45708c.WireTo(generateCode, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_d0697034644f4faa9dbc1f263f45708c.WireTo(id_a808288fa4ae48b0a33de1fda8e4b58a, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["string"]} */
            currentDiagramName.WireTo(id_c31dec24e80b4e328882abbc3368489e, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"ApplyAction","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            // END AUTO-GENERATED WIRING FOR GALADE_Standalone
#endregion
            _mainWindow = mainWindow;

            // BEGIN MANUAL INSTANTIATIONS
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