using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;
using Button = DomainAbstractions.Button;
using TextBox = DomainAbstractions.TextBox;
using System.Windows.Threading;
using ContextMenu = DomainAbstractions.ContextMenu;
using MenuItem = DomainAbstractions.MenuItem;

namespace RequirementsAbstractions
{
    public class ALANode
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string Id { get; set; }
        public string Type { get; set; } = "?";
        public string Name => Model?.Name ?? Id;
        public List<string> AvailableProgrammingParadigms { get; } = new List<string>();
        public List<string> AvailableDomainAbstractions { get; } = new List<string>();
        public List<string> AvailableRequirementsAbstractions { get; } = new List<string>();
        public Graph Graph { get; set; }
        // public List<object> Edges { get; } = new List<object>();
        public Canvas Canvas { get; set; }
        public StateTransition<Enums.DiagramMode> StateTransition { get; set; }
        public UIElement Render { get; set; }
        public AbstractionModel Model { get; set; }

        public List<string> NodeParameters
        {
            get => _nodeParameters;
            set
            {
                _nodeParameters.Clear();
                _nodeParameters.AddRange(value);
            }
        }

        public delegate void SomethingChangedDelegate();
        public delegate void TextChangedDelegate(string text);
        public TextChangedDelegate TypeChanged;

        public event SomethingChangedDelegate PositionChanged;
        public Func<Port, Point> GetAttachmentPoint { get; set; }

        // Private fields
        private Box rootUI;
        private Box _selectedPort;
        private Point _mousePosInBox = new Point(0, 0);
        private List<Box> _inputPortBoxes = new List<Box>();
        private List<Box> _outputPortBoxes = new List<Box>();
        private List<string> _nodeParameters = new List<string>();
        private List<Tuple<Horizontal, DropDownMenu, TextBox, Button>> _nodeParameterRows = new List<Tuple<Horizontal, DropDownMenu, TextBox, Button>>();

        // Global instances
        private Vertical _inputPortsVert;
        private Vertical _outputPortsVert;
        public Vertical parameterRowVert = new Vertical() { InstanceName = "parameterRowVert", Margin = new Thickness(5, 5, 5, 0) };
        public StackPanel _parameterRowsPanel = new StackPanel();

        // Ports
        private Data<object> _refreshInputPorts;
        private Data<object> _refreshOutputPorts;

        // Methods

        /// <summary>
        /// Get the currently selected port. If none are selected, then return a default port.
        /// </summary>
        /// <param name="inputPort"></param>
        /// <returns></returns>
        public Box GetSelectedPort(bool inputPort = false)
        {
            if (_selectedPort != null) return _selectedPort;

            List<Box> boxList = inputPort ? _inputPortBoxes : _outputPortBoxes;

            return boxList.FirstOrDefault(box => box.Payload is Port port && port.IsInputPort == inputPort);
        }

        public List<Port> GetImplementedPorts() => Model.GetImplementedPorts();
        public List<Port> GetAcceptedPorts() => Model.GetAcceptedPorts();

        /// <summary>
        /// Finds the first port box that matches the input name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Box GetPortBox(string name)
        {
            foreach (var inputPortBox in _inputPortBoxes)
            {
                if (inputPortBox.Payload is Port port && port.Name == name) return inputPortBox;
            }

            foreach (var outputPortBox in _outputPortBoxes)
            {
                if (outputPortBox.Payload is Port port && port.Name == name) return outputPortBox;
            }

            return null;
        }

        public void UpdateUI()
        {
            Render.Dispatcher.Invoke(() =>
            {
                UpdateNodeParameters();
                (_refreshInputPorts as IEvent).Execute();
                (_refreshOutputPorts as IEvent).Execute();
            });
        }

        /// <summary>
        /// Updates existing ports with the information from a new set of ports,
        /// and returns a list containing the ports that were not updated due to a lack of
        /// existing instantiated Boxes.
        /// </summary>
        /// <param name="newPorts"></param>
        private List<Port> UpdatePorts(IEnumerable<Port> newPorts)
        {
            var notUpdated = new List<Port>();
            int inputIndex = 0;
            int outputIndex = 0;

            // Update current ports
            foreach (var newPort in newPorts)
            {
                if (newPort.IsInputPort)
                {
                    if (inputIndex < _inputPortBoxes.Count)
                    {
                        var box = _inputPortBoxes[inputIndex];
                        box.Render.Visibility = Visibility.Visible;

                        var oldPort = box.Payload as Port;

                        oldPort.Type = newPort.Type;
                        oldPort.Name = newPort.Name;

                        var newText = new Text(newPort.Name)
                        {
                            HorizAlignment = HorizontalAlignment.Center
                        };

                        box.Render.Child = (newText as IUI).GetWPFElement();
                        
                        inputIndex++;
                    }
                    else
                    {
                        notUpdated.Add(newPort);
                    }
                }
                else
                {
                    if (outputIndex < _outputPortBoxes.Count)
                    {
                        var box = _outputPortBoxes[outputIndex];
                        box.Render.Visibility = Visibility.Visible;

                        var oldPort = box.Payload as Port;

                        oldPort.Type = newPort.Type;
                        oldPort.Name = newPort.Name;

                        var newText = new Text(newPort.Name)
                        {
                            HorizAlignment = HorizontalAlignment.Center
                        };

                        box.Render.Child = (newText as IUI).GetWPFElement();
                        
                        outputIndex++;
                    }
                    else
                    {
                        notUpdated.Add(newPort);
                    }
                }
            }

            // Hide any extra port boxes
            if (notUpdated.Count == 0)
            {
                var numInputsUpdated = newPorts.Count(p => p.IsInputPort);
                if (numInputsUpdated > 0 || Model.GetImplementedPorts().Count == 0)
                {
                    for (int i = numInputsUpdated; i < _inputPortBoxes.Count; i++)
                    {
                        _inputPortBoxes[i].Render.Visibility = Visibility.Collapsed;
                    } 
                }

                var numOutputsUpdated = newPorts.Count(p => !p.IsInputPort);
                if (numOutputsUpdated > 0 || Model.GetAcceptedPorts().Count == 0)
                {
                    for (int i = numOutputsUpdated; i < _outputPortBoxes.Count; i++)
                    {
                        _outputPortBoxes[i].Render.Visibility = Visibility.Collapsed;
                    }  
                }
            }

            return notUpdated;
        }

        private AbstractionModel CreateDummyAbstractionModel()
        {
            var model = new AbstractionModel()
            {
                Type = "NewNode",
                Name = "defaultName"
            };

            model.AddImplementedPort("Port", "input");
            model.AddAcceptedPort("Port", "output");

            return model;
        }

        private void UpdateNodeParameters()
        {
            NodeParameters.Clear();
            NodeParameters.AddRange(Model.GetConstructorArgs().Select(kvp => kvp.Key));
            NodeParameters.AddRange(Model.GetProperties().Select(kvp => kvp.Key));
            NodeParameters.AddRange(Model.GetFields().Select(kvp => kvp.Key));
        }

        public void CreateInternals()
        {
            if (Model == null) Model = CreateDummyAbstractionModel();

            UpdateNodeParameters();

            SetWiring();
        }

        public void CreateParameterRows()
        {
            _parameterRowsPanel.Children.Clear();

            foreach (var row in _nodeParameterRows)
            {
                _parameterRowsPanel.Children.Add((row.Item1 as IUI).GetWPFElement());
            }
        }

        public void Delete(bool deleteAttachedWires = false)
        {
            if (Graph.Get("SelectedNode")?.Equals(this) ?? false) Graph.Set("SelectedNode", null);
            Graph.DeleteNode(this);
            if (Canvas.Children.Contains(Render)) Canvas.Children.Remove(Render);

            // Convert to edgesToDelete list to avoid issue with enumeration being modified (when an edge is deleted from Graph.Edges) within the loop over edgesToDelete
            var edgesToDelete = Graph.Edges
                .Where(e => e is ALAWire wire 
                            && (wire.Source == this || wire.Destination == this) 
                            && Graph.ContainsEdge(wire))
                .Select(e => e as ALAWire).ToList();

            foreach (var edge in edgesToDelete)
            {
                edge?.Delete(deleteDestination: deleteAttachedWires);
            }
        }

        private void SetWiring()
        {
            rootUI = new Box()
            {
                Background = Brushes.LightSkyBlue
            };

            // BEGIN AUTO-GENERATED INSTANTIATIONS
            Horizontal id_75625903dc7b472cacea8a79ce6b370b = new Horizontal() {  };
            Vertical nodeMiddle = new Vertical() { InstanceName = "nodeMiddle", Margin = new Thickness(1, 0, 1, 0) };
            Horizontal nodeIdRow = new Horizontal() { InstanceName = "nodeIdRow" };
            DropDownMenu nodeTypeDropDownMenu = new DropDownMenu() { InstanceName = "nodeTypeDropDownMenu", Text = Model.Type, Width = 100, Items = AvailableDomainAbstractions };
            TextBox nodeNameTextBox = new TextBox() { InstanceName = "nodeNameTextBox", Text = Model.Name, Width = 50 };
            UIFactory id_6736671f4a634e49b5553fe895634e12 = new UIFactory(getUIContainer: () =>{_inputPortsVert = new Vertical();_inputPortsVert.Margin = new Thickness(0);return _inputPortsVert;}) {  };
            ConvertToEvent<object> id_59ea720e2b23461ead85728f1952b3ea = new ConvertToEvent<object>() {  };
            Data<object> refreshInputPorts = new Data<object>() { InstanceName = "refreshInputPorts", Lambda = GetImplementedPorts };
            Cast<object, IEnumerable<Port>> id_6b21514cf849449397430c5a1c33baa7 = new Cast<object, IEnumerable<Port>>() {  };
            ForEach<Port> id_08e03288cff54cde995e5c58754dfda0 = new ForEach<Port>() {  };
            Apply<Port, object> setUpPortBox = new Apply<Port, object>() { InstanceName = "setUpPortBox", Lambda = port =>{var box = new Box();box.Payload = port;box.Width = 50;box.Height = 15;box.Background = Brushes.White;box.BorderThickness = new Thickness(2);var toolTipLabel = new System.Windows.Controls.Label() { Content = port.ToString()};box.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = port.ToString();var text = new Text(text: port.Name);text.HorizAlignment = HorizontalAlignment.Center;box.Render.Child = (text as IUI).GetWPFElement();if (port.IsInputPort){_inputPortsVert.WireTo(box, "children");_inputPortBoxes.Add(box);(_inputPortsVert as IUI).GetWPFElement(); /* Refresh UI */}else{_outputPortsVert.WireTo(box, "children");_outputPortBoxes.Add(box);(_outputPortsVert as IUI).GetWPFElement(); /* Refresh UI */}return box;} };
            UIFactory id_be4c15e234214a41892d0b14bc7cd402 = new UIFactory(getUIContainer: () =>{_outputPortsVert = new Vertical();_outputPortsVert.Margin = new Thickness(0);return _outputPortsVert;}) {  };
            ApplyAction<object> id_64b9c521e09541c99fb1e28ac590a2b3 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = Brushes.Red;} };
            DataFlowConnector<object> id_b21c94b898e841e099322d8db39101f0 = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_e15fa745b6bf40b5b62502ccebfa21c7 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = Brushes.Black;} };
            Apply<object, object> id_dc50e45078164677a8d05f8cf25272a0 = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseEnter");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_44f7e8c7bde24fb1bc905282604901eb = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            Apply<object, object> id_332a20d3f28e4245a9e8ca64a919aecb = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseLeave");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_d63f719a88a445f99431127c2111a4da = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            DataFlowConnector<object> addUIEventsToPort = new DataFlowConnector<object>() { InstanceName = "addUIEventsToPort" };
            Apply<object, object> id_0a4751521ee542dfb10cae2a837338d5 = new Apply<object, object>() { Lambda = input =>{var mouseButtonEvent = new MouseButtonEvent("MouseDown");mouseButtonEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseButtonEvent, "eventHandlers");return mouseButtonEvent;} };
            DynamicWiring<IDataFlow<object>> id_653463cdbd564c9e9382d126cdc58523 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_2b34cb29fbca413889daa97689e74f5c = new ApplyAction<object>() { Lambda = input =>{_selectedPort = input as Box;_selectedPort.Render.Focus();} };
            Apply<object, object> id_6ed5708d6e584da4a55ba09ed0978ceb = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("GotFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_e736949492d24352aab7e2ece1f5bf94 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_b27dc22e497d43f288a4dbdb2878d064 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = Brushes.SandyBrown;} };
            Apply<object, object> id_d7ef64fd1004445b81c611c29331a3ac = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("LostFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_ddf6bf06bf854c5a971adb87223d494b = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_00e5f63846344dcfa7b9345345ea6f2b = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = Brushes.White;} };
            MouseEvent id_d126ce2a01f84b9f8f762edd99d49b34 = new MouseEvent(eventName: "MouseEnter") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_44ba67d3fed5441d9d51d35b6245d27f = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.Aquamarine;} };
            MouseEvent id_9d88dea594664ac2a105f0364abdebc8 = new MouseEvent(eventName: "MouseLeave") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_840b9f3edca942cf8842a33e8a37688c = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) (input as Box).Background = Brushes.LightSkyBlue;} };
            RoutedEventSubscriber id_773a67799e954b5688918808f5938460 = new RoutedEventSubscriber(eventName: "GotFocus") { ExtractSender = source => (source as Box).Render };
            RoutedEventSubscriber id_64232ca21622471fbcdc5bc89764e406 = new RoutedEventSubscriber(eventName: "LostFocus") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_a5d4b981067b4ace8387f8e4e57e8da9 = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.LightSkyBlue;} };
            ApplyAction<object> id_c50518c8027842cf9dbf4edffe350caf = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) ui.Focus();} };
            ApplyAction<object> id_1c0c6c82677e44b0996cf357d2b4b9d8 = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.Aquamarine;} };
            DataFlowConnector<object> id_eb92dbc9eb1040d6bb96d1377d088bbe = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_564bf7b95f4b4701a3ecfa2a7f266246 = new ApplyAction<object>() { Lambda = input =>{StateTransition.Update(Enums.DiagramMode.IdleSelected);} };
            MouseEvent id_077e13e5736b485b9e63ab82071b58df = new MouseEvent(eventName: "MouseMove") { ExtractSender = source => (source as Box).Render, Condition = args => Mouse.LeftButton == MouseButtonState.Pressed && Keyboard.IsKeyDown(Key.LeftShift) };
            ApplyAction<object> id_348a0b9d99034defb80c707246383f92 = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;var mousePos = Mouse.GetPosition(Canvas);var oldPosition = new Point(Canvas.GetLeft(render), Canvas.GetTop(render));Canvas.SetLeft(render, mousePos.X - _mousePosInBox.X);Canvas.SetTop(render, mousePos.Y - _mousePosInBox.Y);PositionChanged?.Invoke();} };
            ApplyAction<object> id_8cb46a85910b4b938b767a0d44e9808b = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;_mousePosInBox = Mouse.GetPosition(render);Mouse.Capture(render);} };
            ApplyAction<object> id_dc7ce74e91db4f7880367de2567357ff = new ApplyAction<object>() { Lambda = input =>{Graph.Set("SelectedNode", this);} };
            MouseButtonEvent id_62a403cdad0d4163873575c438c2972b = new MouseButtonEvent(eventName: "MouseLeftButtonDown") { ExtractSender = source => (source as Box).Render };
            ApplyAction<string> id_09595d7b2d714f548128265f905eed49 = new ApplyAction<string>() { Lambda = input =>{TypeChanged?.Invoke(input);} };
            MouseButtonEvent id_e4a7d2e0815543de934dfbc97d67dbab = new MouseButtonEvent(eventName: "MouseLeftButtonUp") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_5f81adc96093401ebceb0c616de7f1c2 = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;if (Mouse.Captured?.Equals(render) ?? false) Mouse.Capture(null);} };
            ApplyAction<object> id_31b6c29fa6c0489e9d36335bc415dc22 = new ApplyAction<object>() { Lambda = input =>{(input as Box).InitialiseUI();} };
            EventLambda setNodeToolTip = new EventLambda() { InstanceName = "setNodeToolTip", Lambda = () => {var toolTipLabel = new System.Windows.Controls.Label() { Content = Model.GetDocumentation() };rootUI.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };rootUI.Render.MouseEnter += (sender, args) => toolTipLabel.Content = Model.GetDocumentation();} };
            Apply<object, object> id_0a503773af51406f9ae9b09a33a7c96d = new Apply<object, object>() { Lambda = input =>{var notUpdated = UpdatePorts(input as IEnumerable<Port>);return notUpdated;} };
            ConvertToEvent<object> id_f7e7ed0280c84f859425cb46a07d225f = new ConvertToEvent<object>() {  };
            Data<object> refreshOutputPorts = new Data<object>() { InstanceName = "refreshOutputPorts", Lambda = GetAcceptedPorts };
            Horizontal addNewParameterRow = new Horizontal() { InstanceName = "addNewParameterRow", Ratios = new[] { 40, 20, 40 } };
            Text id_fa81097e75354f27b9e7f693ccce75dc = new Text(text: "") {  };
            Text id_db8d03bf9a9244369ee42224391e5eac = new Text(text: "") {  };
            Button id_1643cc2e66bc42eab1ebfabbf0c7384b = new Button(title: "+") { Width = 20, Margin = new Thickness(5) };
            EventLambda id_eb10d9ea45a2462eb159e62fa1be34e0 = new EventLambda() { Lambda = () => {var dropDown = new DropDownMenu() {Items = NodeParameters,Width = 100};var textBox = new TextBox() {Width = 100,TrackIndent = true,Font = "Consolas"};var deleteButton = new Button("-") {Width = 20,Height = 20};var dropDownUI = (dropDown as IUI).GetWPFElement() as ComboBox;var toolTipLabel = new System.Windows.Controls.Label() { Content = "" };dropDownUI.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };dropDownUI.MouseEnter += (sender, args) => toolTipLabel.Content = Model.GetType(dropDownUI.Text);dropDownUI.SelectionChanged += (sender, args) => textBox.Text = Model.GetValue(dropDownUI.SelectedValue?.ToString() ?? "");var horiz = new Horizontal();horiz.WireTo(dropDown, "children");horiz.WireTo(textBox, "children");horiz.WireTo(deleteButton, "children");var buttonUI = (deleteButton as IUI).GetWPFElement() as System.Windows.Controls.Button;buttonUI.Click += (sender, args) => {var row = _nodeParameterRows.FirstOrDefault(tuple => tuple.Item4.Equals(deleteButton));_nodeParameterRows.Remove(row);CreateParameterRows();};_nodeParameterRows.Add(Tuple.Create(horiz, dropDown, textBox, deleteButton));} };
            EventConnector id_e3e252ebefe84ae996ed9840eb65412b = new EventConnector() {  };
            EventLambda id_31795940f22448549d2fb36593972e4c = new EventLambda() { Lambda = CreateParameterRows };
            Box id_7941dfa65f464029800cc66dcafc357c = new Box() { Render = new Border() { Child = _parameterRowsPanel } };
            EventConnector id_2b59b8bf546a4c9eb27b04db2b0ee303 = new EventConnector() {  };
            ContextMenu id_bc5a04a3bb3c4fc298da5efff0c834d9 = new ContextMenu() {  };
            MenuItem id_f4d8e65f75a8499e8bfe03f104f83b6c = new MenuItem(header: "Open source code...") {  };
            EventLambda id_d793058a88744e23bd54511b242af467 = new EventLambda() { Lambda = () =>{Process.Start(Model.GetCodeFilePath());} };
            MenuItem id_42b675b153e440a090a0da96c6f72f01 = new MenuItem(header: "Through your default external editor") {  };
            MenuItem id_30b3a5a022904bab99cf8561365a5b68 = new MenuItem(header: "Through the GALADE text editor") {  };
            Data<string> id_22cc79db892e4276a0843318a1741c00 = new Data<string>() { Lambda = Model.GetCodeFilePath };
            ApplyAction<object> id_be7f4f361ae5412883ffc8b178930537 = new ApplyAction<object>() { Lambda = input =>{if (StateTransition.CurrentStateMatches(Enums.DiagramMode.AwaitingPortSelection)){var wire = Graph.Get("SelectedWire") as ALAWire;if (wire == null) return;if (wire.Source == null){wire.Source = this;wire.SourcePort = input as Box;}else if (wire.Destination == null){wire.Destination = this;wire.DestinationPort = input as Box;}StateTransition.Update(Enums.DiagramMode.Idle);}} };
            DataFlowConnector<object> id_9ecb3f09011d45bda1639c409fd52e88 = new DataFlowConnector<object>() {  };
            KeyEvent id_654c6efa96cb4fbc9c9d72934fbe7c01 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.LeftCtrl, Key.Q }, ExtractSender = source => (source as Box).Render };
            EventLambda id_4eb111002215437dad05d6296664b885 = new EventLambda() { Lambda = () =>{var sourcePort = GetSelectedPort();if (sourcePort == null) return;var source = this;var wire = new ALAWire(){Graph = Graph,Canvas = Canvas,Source = source,Destination = null,SourcePort = sourcePort,DestinationPort = null,StateTransition = StateTransition};Graph.AddEdge(wire);wire.Paint();wire.StartMoving(source: false);} };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            rootUI.WireTo(id_75625903dc7b472cacea8a79ce6b370b, "uiLayout");
            rootUI.WireTo(id_d126ce2a01f84b9f8f762edd99d49b34, "eventHandlers");
            rootUI.WireTo(id_9d88dea594664ac2a105f0364abdebc8, "eventHandlers");
            rootUI.WireTo(id_773a67799e954b5688918808f5938460, "eventHandlers");
            rootUI.WireTo(id_64232ca21622471fbcdc5bc89764e406, "eventHandlers");
            rootUI.WireTo(id_077e13e5736b485b9e63ab82071b58df, "eventHandlers");
            rootUI.WireTo(id_62a403cdad0d4163873575c438c2972b, "eventHandlers");
            rootUI.WireTo(id_e4a7d2e0815543de934dfbc97d67dbab, "eventHandlers");
            rootUI.WireTo(id_654c6efa96cb4fbc9c9d72934fbe7c01, "eventHandlers");
            rootUI.WireTo(id_bc5a04a3bb3c4fc298da5efff0c834d9, "contextMenu");
            id_75625903dc7b472cacea8a79ce6b370b.WireTo(id_6736671f4a634e49b5553fe895634e12, "children");
            id_75625903dc7b472cacea8a79ce6b370b.WireTo(nodeMiddle, "children");
            id_75625903dc7b472cacea8a79ce6b370b.WireTo(id_be4c15e234214a41892d0b14bc7cd402, "children");
            nodeMiddle.WireTo(nodeIdRow, "children");
            nodeMiddle.WireTo(parameterRowVert, "children");
            nodeMiddle.WireTo(addNewParameterRow, "children");
            nodeIdRow.WireTo(nodeTypeDropDownMenu, "children");
            nodeIdRow.WireTo(nodeNameTextBox, "children");
            nodeTypeDropDownMenu.WireTo(id_09595d7b2d714f548128265f905eed49, "selectedItem");
            id_6736671f4a634e49b5553fe895634e12.WireTo(id_59ea720e2b23461ead85728f1952b3ea, "uiInstanceOutput");
            id_59ea720e2b23461ead85728f1952b3ea.WireTo(refreshInputPorts, "eventOutput");
            refreshInputPorts.WireTo(id_0a503773af51406f9ae9b09a33a7c96d, "dataOutput");
            id_6b21514cf849449397430c5a1c33baa7.WireTo(id_08e03288cff54cde995e5c58754dfda0, "output");
            id_08e03288cff54cde995e5c58754dfda0.WireTo(setUpPortBox, "elementOutput");
            id_08e03288cff54cde995e5c58754dfda0.WireTo(id_2b59b8bf546a4c9eb27b04db2b0ee303, "complete");
            setUpPortBox.WireTo(id_b21c94b898e841e099322d8db39101f0, "output");
            id_be4c15e234214a41892d0b14bc7cd402.WireTo(id_f7e7ed0280c84f859425cb46a07d225f, "uiInstanceOutput");
            id_b21c94b898e841e099322d8db39101f0.WireTo(addUIEventsToPort, "fanoutList");
            id_b21c94b898e841e099322d8db39101f0.WireTo(id_31b6c29fa6c0489e9d36335bc415dc22, "fanoutList");
            id_dc50e45078164677a8d05f8cf25272a0.WireTo(id_44f7e8c7bde24fb1bc905282604901eb, "output");
            id_44f7e8c7bde24fb1bc905282604901eb.WireTo(id_9ecb3f09011d45bda1639c409fd52e88, "wire");
            id_9ecb3f09011d45bda1639c409fd52e88.WireTo(id_64b9c521e09541c99fb1e28ac590a2b3, "fanoutList");
            id_332a20d3f28e4245a9e8ca64a919aecb.WireTo(id_d63f719a88a445f99431127c2111a4da, "output");
            id_d63f719a88a445f99431127c2111a4da.WireTo(id_e15fa745b6bf40b5b62502ccebfa21c7, "wire");
            addUIEventsToPort.WireTo(id_dc50e45078164677a8d05f8cf25272a0, "fanoutList");
            addUIEventsToPort.WireTo(id_332a20d3f28e4245a9e8ca64a919aecb, "fanoutList");
            addUIEventsToPort.WireTo(id_0a4751521ee542dfb10cae2a837338d5, "fanoutList");
            addUIEventsToPort.WireTo(id_6ed5708d6e584da4a55ba09ed0978ceb, "fanoutList");
            addUIEventsToPort.WireTo(id_d7ef64fd1004445b81c611c29331a3ac, "fanoutList");
            id_0a4751521ee542dfb10cae2a837338d5.WireTo(id_653463cdbd564c9e9382d126cdc58523, "output");
            id_653463cdbd564c9e9382d126cdc58523.WireTo(id_2b34cb29fbca413889daa97689e74f5c, "wire");
            id_6ed5708d6e584da4a55ba09ed0978ceb.WireTo(id_e736949492d24352aab7e2ece1f5bf94, "output");
            id_e736949492d24352aab7e2ece1f5bf94.WireTo(id_b27dc22e497d43f288a4dbdb2878d064, "wire");
            id_d7ef64fd1004445b81c611c29331a3ac.WireTo(id_ddf6bf06bf854c5a971adb87223d494b, "output");
            id_ddf6bf06bf854c5a971adb87223d494b.WireTo(id_00e5f63846344dcfa7b9345345ea6f2b, "wire");
            id_d126ce2a01f84b9f8f762edd99d49b34.WireTo(id_44ba67d3fed5441d9d51d35b6245d27f, "sourceOutput");
            id_9d88dea594664ac2a105f0364abdebc8.WireTo(id_840b9f3edca942cf8842a33e8a37688c, "sourceOutput");
            id_773a67799e954b5688918808f5938460.WireTo(id_1c0c6c82677e44b0996cf357d2b4b9d8, "sourceOutput");
            id_64232ca21622471fbcdc5bc89764e406.WireTo(id_a5d4b981067b4ace8387f8e4e57e8da9, "sourceOutput");
            id_eb92dbc9eb1040d6bb96d1377d088bbe.WireTo(id_c50518c8027842cf9dbf4edffe350caf, "fanoutList");
            id_eb92dbc9eb1040d6bb96d1377d088bbe.WireTo(id_564bf7b95f4b4701a3ecfa2a7f266246, "fanoutList");
            id_eb92dbc9eb1040d6bb96d1377d088bbe.WireTo(id_8cb46a85910b4b938b767a0d44e9808b, "fanoutList");
            id_eb92dbc9eb1040d6bb96d1377d088bbe.WireTo(id_dc7ce74e91db4f7880367de2567357ff, "fanoutList");
            id_077e13e5736b485b9e63ab82071b58df.WireTo(id_348a0b9d99034defb80c707246383f92, "sourceOutput");
            id_62a403cdad0d4163873575c438c2972b.WireTo(id_eb92dbc9eb1040d6bb96d1377d088bbe, "sourceOutput");
            id_e4a7d2e0815543de934dfbc97d67dbab.WireTo(id_5f81adc96093401ebceb0c616de7f1c2, "sourceOutput");
            id_0a503773af51406f9ae9b09a33a7c96d.WireTo(id_6b21514cf849449397430c5a1c33baa7, "output");
            id_f7e7ed0280c84f859425cb46a07d225f.WireTo(refreshOutputPorts, "eventOutput");
            refreshOutputPorts.WireTo(id_0a503773af51406f9ae9b09a33a7c96d, "dataOutput");
            addNewParameterRow.WireTo(id_fa81097e75354f27b9e7f693ccce75dc, "children");
            addNewParameterRow.WireTo(id_1643cc2e66bc42eab1ebfabbf0c7384b, "children");
            addNewParameterRow.WireTo(id_db8d03bf9a9244369ee42224391e5eac, "children");
            id_1643cc2e66bc42eab1ebfabbf0c7384b.WireTo(id_e3e252ebefe84ae996ed9840eb65412b, "eventButtonClicked");
            parameterRowVert.WireTo(id_7941dfa65f464029800cc66dcafc357c, "children");
            id_e3e252ebefe84ae996ed9840eb65412b.WireTo(id_eb10d9ea45a2462eb159e62fa1be34e0, "fanoutList");
            id_e3e252ebefe84ae996ed9840eb65412b.WireTo(id_31795940f22448549d2fb36593972e4c, "complete");
            id_2b59b8bf546a4c9eb27b04db2b0ee303.WireTo(setNodeToolTip, "fanoutList");
            id_bc5a04a3bb3c4fc298da5efff0c834d9.WireTo(id_f4d8e65f75a8499e8bfe03f104f83b6c, "children");
            id_f4d8e65f75a8499e8bfe03f104f83b6c.WireTo(id_42b675b153e440a090a0da96c6f72f01, "children");
            id_f4d8e65f75a8499e8bfe03f104f83b6c.WireTo(id_30b3a5a022904bab99cf8561365a5b68, "children");
            id_42b675b153e440a090a0da96c6f72f01.WireTo(id_d793058a88744e23bd54511b242af467, "clickedEvent");
            id_30b3a5a022904bab99cf8561365a5b68.WireTo(id_22cc79db892e4276a0843318a1741c00, "clickedEvent");
            id_9ecb3f09011d45bda1639c409fd52e88.WireTo(id_be7f4f361ae5412883ffc8b178930537, "fanoutList");
            id_654c6efa96cb4fbc9c9d72934fbe7c01.WireTo(id_4eb111002215437dad05d6296664b885, "eventHappened");
            // END AUTO-GENERATED WIRING

            Render = (rootUI as IUI).GetWPFElement();

            // Instance mapping
            _refreshInputPorts = refreshInputPorts;
            _refreshOutputPorts = refreshOutputPorts;
        }

        public ALANode()
        {
            Id = Utilities.GetUniqueId();
        }
    }
}


























































































































































































































































































































































































































































































































































