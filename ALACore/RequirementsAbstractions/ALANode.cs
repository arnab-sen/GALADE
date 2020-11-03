using System;
using System.Collections.Generic;
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
using System.Windows.Forms.VisualStyles;
using System.Windows.Threading;

namespace RequirementsAbstractions
{
    public class ALANode
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string Id { get; set; }
        public string Type { get; set; } = "?";
        public string Name { get; set; } = "";
        public List<string> AvailableProgrammingParadigms { get; } = new List<string>();
        public List<string> AvailableDomainAbstractions { get; } = new List<string>();
        public List<string> AvailableRequirementsAbstractions { get; } = new List<string>();
        public Graph Graph { get; set; }
        public List<object> Edges { get; } = new List<object>();
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

        public SomethingChangedDelegate PositionChanged;
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
        public Vertical parameterRowVert = new Vertical() { InstanceName = "parameterRowVert" };
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

        public void UpdateUI()
        {
            UpdateNodeParameters();
            (_refreshInputPorts as IEvent).Execute();
            (_refreshOutputPorts as IEvent).Execute();
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

        private void SetWiring()
        {
            rootUI = new Box()
            {
                Background = Brushes.LightSkyBlue
            };

            // BEGIN AUTO-GENERATED INSTANTIATIONS
            Horizontal id_ff0351e6ea004c43a1fe6853ffd60569 = new Horizontal() {  };
            Vertical nodeMiddle = new Vertical() { InstanceName = "nodeMiddle", Margin = new Thickness(1, 0, 1, 0) };
            Horizontal nodeIdRow = new Horizontal() { InstanceName = "nodeIdRow" };
            DropDownMenu nodeTypeDropDownMenu = new DropDownMenu() { InstanceName = "nodeTypeDropDownMenu", Text = Model.Type, Width = 100, Items = AvailableDomainAbstractions };
            TextBox nodeNameTextBox = new TextBox() { InstanceName = "nodeNameTextBox", Text = Model.Name, Width = 50 };
            UIFactory id_ecafe41efc6b4e0b97ede0bfd0074dc1 = new UIFactory(getUIContainer: () =>{_inputPortsVert = new Vertical();_inputPortsVert.Margin = new Thickness(0);return _inputPortsVert;}) {  };
            ConvertToEvent<object> id_44c00a9ea3d2460a97e894af32cd8ef3 = new ConvertToEvent<object>() {  };
            Data<object> refreshInputPorts = new Data<object>() { InstanceName = "refreshInputPorts", Lambda = GetImplementedPorts };
            Cast<object, IEnumerable<Port>> id_9543034122064081bd1734858a85dfc5 = new Cast<object, IEnumerable<Port>>() {  };
            ForEach<Port> id_8213cc6c156b436397bdec137cd22480 = new ForEach<Port>() {  };
            Apply<Port, object> setUpPortBox = new Apply<Port, object>() { InstanceName = "setUpPortBox", Lambda = port =>{var box = new Box();box.Payload = port;box.Width = 50;box.Height = 15;box.Background = Brushes.White;box.BorderThickness = new Thickness(2);var toolTipLabel = new System.Windows.Controls.Label() { Content = port.ToString()};box.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = port.ToString();var text = new Text(text: port.Name);text.HorizAlignment = HorizontalAlignment.Center;box.Render.Child = (text as IUI).GetWPFElement();if (port.IsInputPort){_inputPortsVert.WireTo(box, "children");_inputPortBoxes.Add(box);(_inputPortsVert as IUI).GetWPFElement(); /* Refresh UI */}else{_outputPortsVert.WireTo(box, "children");_outputPortBoxes.Add(box);(_outputPortsVert as IUI).GetWPFElement(); /* Refresh UI */}return box;} };
            UIFactory id_dc754931c65046a18375ef70859e17b0 = new UIFactory(getUIContainer: () =>{_outputPortsVert = new Vertical();_outputPortsVert.Margin = new Thickness(0);return _outputPortsVert;}) {  };
            ApplyAction<object> id_6d79ced84d9f49a4b0bcd82989c2e5b6 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = Brushes.Red;} };
            DataFlowConnector<object> id_5f9f1264838f4c659e1471e001534156 = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_84cf5f1fe385481f8031621f70c806b3 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = Brushes.Black;} };
            Apply<object, object> id_aff372ba3f8c416eb1cfe4760a258d72 = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseEnter");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_1422d2b9a70b48ee86cb31da38c37bef = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            Apply<object, object> id_b949601213a54362b8eb642b41fb1d52 = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseLeave");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_b7dce0819fa446ccbb1193683625240b = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            DataFlowConnector<object> addUIEventsToPort = new DataFlowConnector<object>() { InstanceName = "addUIEventsToPort" };
            Apply<object, object> id_d5391b57eb26405bb4e1471f2b0fa3aa = new Apply<object, object>() { Lambda = input =>{var mouseButtonEvent = new MouseButtonEvent("MouseDown");mouseButtonEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseButtonEvent, "eventHandlers");return mouseButtonEvent;} };
            DynamicWiring<IDataFlow<object>> id_571f6d04e110446db98eb767dc7f96e6 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_4b438f8bc5324df7a24534a4f1fc80d0 = new ApplyAction<object>() { Lambda = input =>{_selectedPort = input as Box;_selectedPort.Render.Focus();} };
            Apply<object, object> id_5d235db7e873475fa4ddf34b605f1bc3 = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("GotFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_e1b906c1863646899c3405bc0f79350d = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_23c325cb1f9748b7949f3623f2d9c801 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = Brushes.SandyBrown;} };
            Apply<object, object> id_10bc736207a74839aeb2c682fcee5c3a = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("LostFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_474aa4fb51ee475ea489730e99734a88 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_0e2c0779069c4cd982f4207db5313831 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = Brushes.White;} };
            MouseEvent id_8d2d4d91c9924e31b8e8ec9448019d8f = new MouseEvent(eventName: "MouseEnter") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_de892f97d75e43c6a6bb928871b3d2f9 = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.Aquamarine;} };
            MouseEvent id_db43d64d6101420b8157708c086fd355 = new MouseEvent(eventName: "MouseLeave") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_be2d174b13a44bf08771d742bdb59793 = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) (input as Box).Background = Brushes.LightSkyBlue;} };
            RoutedEventSubscriber id_639e590ebf464154bb53901ce391dc94 = new RoutedEventSubscriber(eventName: "GotFocus") { ExtractSender = source => (source as Box).Render };
            RoutedEventSubscriber id_2f89f8d3417c4338868811e1bca7f059 = new RoutedEventSubscriber(eventName: "LostFocus") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_3cc115d28aa64151a479542f69d912dc = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.LightSkyBlue;} };
            ApplyAction<object> id_27a4c9df78dd4951b3a5070a0bdf8f67 = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) ui.Focus();} };
            ApplyAction<object> id_5473538202a14ef68306746609a0e1a2 = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.Aquamarine;} };
            DataFlowConnector<object> id_2b1a255134ad4855a1cdd44daa2530a7 = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_2e57a346a8fc488d88563bae0265bd76 = new ApplyAction<object>() { Lambda = input =>{StateTransition.Update(Enums.DiagramMode.IdleSelected);} };
            MouseEvent id_caec86d0fedb43e78bb4ab8633be664c = new MouseEvent(eventName: "MouseMove") { ExtractSender = source => (source as Box).Render, Condition = args => Mouse.LeftButton == MouseButtonState.Pressed && Keyboard.IsKeyDown(Key.LeftShift) };
            ApplyAction<object> id_ddaa4542c5c846488cf4070d6ae2d07a = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;var mousePos = Mouse.GetPosition(Canvas);var oldPosition = new Point(Canvas.GetLeft(render), Canvas.GetTop(render));Canvas.SetLeft(render, mousePos.X - _mousePosInBox.X);Canvas.SetTop(render, mousePos.Y - _mousePosInBox.Y);PositionChanged?.Invoke();} };
            ApplyAction<object> id_248822dd82a745ada3f1c1cf8b4a3027 = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;_mousePosInBox = Mouse.GetPosition(render);Mouse.Capture(render);} };
            ApplyAction<object> id_f488afcee2f94881a060d883a0f6500d = new ApplyAction<object>() { Lambda = input =>{Graph.Set("SelectedNode", this);} };
            MouseButtonEvent id_c07dba8ef7914a8ea200e917b33e80d1 = new MouseButtonEvent(eventName: "MouseLeftButtonDown") { ExtractSender = source => (source as Box).Render };
            ApplyAction<string> id_e88fecbd70be4bc3b6ff912477fa4a43 = new ApplyAction<string>() { Lambda = input =>{TypeChanged?.Invoke(input);} };
            MouseButtonEvent id_21786151ad44489a9b8be75af23dcbde = new MouseButtonEvent(eventName: "MouseLeftButtonUp") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_06ad718b32684b3bb3c1192af3b38e68 = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;if (Mouse.Captured?.Equals(render) ?? false) Mouse.Capture(null);} };
            ApplyAction<object> id_02902f9cf2de4ed5a32c9e81db637b2d = new ApplyAction<object>() { Lambda = input =>{(input as Box).InitialiseUI();} };
            EventLambda setNodeToolTip = new EventLambda() { InstanceName = "setNodeToolTip", Lambda = () => {var toolTipLabel = new System.Windows.Controls.Label() { Content = Model.GetDocumentation() };rootUI.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };rootUI.Render.MouseEnter += (sender, args) => toolTipLabel.Content = Model.GetDocumentation();} };
            Apply<object, object> id_98b0792875b14bf2963774e056fa09ce = new Apply<object, object>() { Lambda = input =>{var notUpdated = UpdatePorts(input as IEnumerable<Port>);return notUpdated;} };
            ConvertToEvent<object> id_d6dfec6b08ae47ef88a05f7c69f17aa9 = new ConvertToEvent<object>() {  };
            Data<object> refreshOutputPorts = new Data<object>() { InstanceName = "refreshOutputPorts", Lambda = GetAcceptedPorts };
            Horizontal addNewParameterRow = new Horizontal() { InstanceName = "addNewParameterRow", Ratios = new[] { 40, 20, 40 } };
            Text id_7b00c293aa9f4427ac20b6350b84e7a9 = new Text(text: "") {  };
            Text id_e98d7e33d0464a6ebe4e46710bfeeb3e = new Text(text: "") {  };
            Button id_e8e7b720170e48a0b289f456608db55c = new Button(title: "+") { Width = 20, Margin = new Thickness(5) };
            EventLambda id_f8baa31ee3804ae79b81455bdf0e9c61 = new EventLambda() { Lambda = () => {var dropDown = new DropDownMenu() {Items = NodeParameters,Width = 100};var dropDownUI = (dropDown as IUI).GetWPFElement() as ComboBox;var toolTipLabel = new System.Windows.Controls.Label() { Content = "" };dropDownUI.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };dropDownUI.MouseEnter += (sender, args) => toolTipLabel.Content = Model.GetType(dropDownUI.Text);var textBox = new TextBox() {Width = 100};var deleteButton = new Button("-") {Width = 20,Height = 20};var horiz = new Horizontal();horiz.WireTo(dropDown, "children");horiz.WireTo(textBox, "children");horiz.WireTo(deleteButton, "children");var buttonUI = (deleteButton as IUI).GetWPFElement() as System.Windows.Controls.Button;buttonUI.Click += (sender, args) => {var row = _nodeParameterRows.FirstOrDefault(tuple => tuple.Item4.Equals(deleteButton));_nodeParameterRows.Remove(row);CreateParameterRows();};_nodeParameterRows.Add(Tuple.Create(horiz, dropDown, textBox, deleteButton));} };
            EventConnector id_d3053e8a06fb4a7f9bb3c23d0902c191 = new EventConnector() {  };
            EventLambda id_b728e4a4198347a6b0238676be69861e = new EventLambda() { Lambda = CreateParameterRows };
            Box id_4b5b1194dbec40a6a1e3a57a620a20a1 = new Box() { Render = new Border() { Child = _parameterRowsPanel } };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            rootUI.WireTo(id_ff0351e6ea004c43a1fe6853ffd60569, "uiLayout");
            rootUI.WireTo(id_8d2d4d91c9924e31b8e8ec9448019d8f, "eventHandlers");
            rootUI.WireTo(id_db43d64d6101420b8157708c086fd355, "eventHandlers");
            rootUI.WireTo(id_639e590ebf464154bb53901ce391dc94, "eventHandlers");
            rootUI.WireTo(id_2f89f8d3417c4338868811e1bca7f059, "eventHandlers");
            rootUI.WireTo(id_caec86d0fedb43e78bb4ab8633be664c, "eventHandlers");
            rootUI.WireTo(id_c07dba8ef7914a8ea200e917b33e80d1, "eventHandlers");
            rootUI.WireTo(id_21786151ad44489a9b8be75af23dcbde, "eventHandlers");
            id_ff0351e6ea004c43a1fe6853ffd60569.WireTo(id_ecafe41efc6b4e0b97ede0bfd0074dc1, "children");
            id_ff0351e6ea004c43a1fe6853ffd60569.WireTo(nodeMiddle, "children");
            id_ff0351e6ea004c43a1fe6853ffd60569.WireTo(id_dc754931c65046a18375ef70859e17b0, "children");
            nodeMiddle.WireTo(nodeIdRow, "children");
            nodeMiddle.WireTo(parameterRowVert, "children");
            nodeMiddle.WireTo(addNewParameterRow, "children");
            nodeIdRow.WireTo(nodeTypeDropDownMenu, "children");
            nodeIdRow.WireTo(nodeNameTextBox, "children");
            nodeTypeDropDownMenu.WireTo(id_e88fecbd70be4bc3b6ff912477fa4a43, "selectedItem");
            id_ecafe41efc6b4e0b97ede0bfd0074dc1.WireTo(id_44c00a9ea3d2460a97e894af32cd8ef3, "uiInstanceOutput");
            id_44c00a9ea3d2460a97e894af32cd8ef3.WireTo(refreshInputPorts, "eventOutput");
            refreshInputPorts.WireTo(id_98b0792875b14bf2963774e056fa09ce, "dataOutput");
            id_9543034122064081bd1734858a85dfc5.WireTo(id_8213cc6c156b436397bdec137cd22480, "output");
            id_8213cc6c156b436397bdec137cd22480.WireTo(setUpPortBox, "elementOutput");
            id_8213cc6c156b436397bdec137cd22480.WireTo(setNodeToolTip, "complete");
            setUpPortBox.WireTo(id_5f9f1264838f4c659e1471e001534156, "output");
            id_dc754931c65046a18375ef70859e17b0.WireTo(id_d6dfec6b08ae47ef88a05f7c69f17aa9, "uiInstanceOutput");
            id_5f9f1264838f4c659e1471e001534156.WireTo(addUIEventsToPort, "fanoutList");
            id_5f9f1264838f4c659e1471e001534156.WireTo(id_02902f9cf2de4ed5a32c9e81db637b2d, "fanoutList");
            id_aff372ba3f8c416eb1cfe4760a258d72.WireTo(id_1422d2b9a70b48ee86cb31da38c37bef, "output");
            id_1422d2b9a70b48ee86cb31da38c37bef.WireTo(id_6d79ced84d9f49a4b0bcd82989c2e5b6, "wire");
            id_b949601213a54362b8eb642b41fb1d52.WireTo(id_b7dce0819fa446ccbb1193683625240b, "output");
            id_b7dce0819fa446ccbb1193683625240b.WireTo(id_84cf5f1fe385481f8031621f70c806b3, "wire");
            addUIEventsToPort.WireTo(id_aff372ba3f8c416eb1cfe4760a258d72, "fanoutList");
            addUIEventsToPort.WireTo(id_b949601213a54362b8eb642b41fb1d52, "fanoutList");
            addUIEventsToPort.WireTo(id_d5391b57eb26405bb4e1471f2b0fa3aa, "fanoutList");
            addUIEventsToPort.WireTo(id_5d235db7e873475fa4ddf34b605f1bc3, "fanoutList");
            addUIEventsToPort.WireTo(id_10bc736207a74839aeb2c682fcee5c3a, "fanoutList");
            id_d5391b57eb26405bb4e1471f2b0fa3aa.WireTo(id_571f6d04e110446db98eb767dc7f96e6, "output");
            id_571f6d04e110446db98eb767dc7f96e6.WireTo(id_4b438f8bc5324df7a24534a4f1fc80d0, "wire");
            id_5d235db7e873475fa4ddf34b605f1bc3.WireTo(id_e1b906c1863646899c3405bc0f79350d, "output");
            id_e1b906c1863646899c3405bc0f79350d.WireTo(id_23c325cb1f9748b7949f3623f2d9c801, "wire");
            id_10bc736207a74839aeb2c682fcee5c3a.WireTo(id_474aa4fb51ee475ea489730e99734a88, "output");
            id_474aa4fb51ee475ea489730e99734a88.WireTo(id_0e2c0779069c4cd982f4207db5313831, "wire");
            id_8d2d4d91c9924e31b8e8ec9448019d8f.WireTo(id_de892f97d75e43c6a6bb928871b3d2f9, "sourceOutput");
            id_db43d64d6101420b8157708c086fd355.WireTo(id_be2d174b13a44bf08771d742bdb59793, "sourceOutput");
            id_639e590ebf464154bb53901ce391dc94.WireTo(id_5473538202a14ef68306746609a0e1a2, "sourceOutput");
            id_2f89f8d3417c4338868811e1bca7f059.WireTo(id_3cc115d28aa64151a479542f69d912dc, "sourceOutput");
            id_2b1a255134ad4855a1cdd44daa2530a7.WireTo(id_27a4c9df78dd4951b3a5070a0bdf8f67, "fanoutList");
            id_2b1a255134ad4855a1cdd44daa2530a7.WireTo(id_2e57a346a8fc488d88563bae0265bd76, "fanoutList");
            id_2b1a255134ad4855a1cdd44daa2530a7.WireTo(id_248822dd82a745ada3f1c1cf8b4a3027, "fanoutList");
            id_2b1a255134ad4855a1cdd44daa2530a7.WireTo(id_f488afcee2f94881a060d883a0f6500d, "fanoutList");
            id_caec86d0fedb43e78bb4ab8633be664c.WireTo(id_ddaa4542c5c846488cf4070d6ae2d07a, "sourceOutput");
            id_c07dba8ef7914a8ea200e917b33e80d1.WireTo(id_2b1a255134ad4855a1cdd44daa2530a7, "sourceOutput");
            id_21786151ad44489a9b8be75af23dcbde.WireTo(id_06ad718b32684b3bb3c1192af3b38e68, "sourceOutput");
            id_98b0792875b14bf2963774e056fa09ce.WireTo(id_9543034122064081bd1734858a85dfc5, "output");
            id_d6dfec6b08ae47ef88a05f7c69f17aa9.WireTo(refreshOutputPorts, "eventOutput");
            refreshOutputPorts.WireTo(id_98b0792875b14bf2963774e056fa09ce, "dataOutput");
            addNewParameterRow.WireTo(id_7b00c293aa9f4427ac20b6350b84e7a9, "children");
            addNewParameterRow.WireTo(id_e8e7b720170e48a0b289f456608db55c, "children");
            addNewParameterRow.WireTo(id_e98d7e33d0464a6ebe4e46710bfeeb3e, "children");
            id_e8e7b720170e48a0b289f456608db55c.WireTo(id_d3053e8a06fb4a7f9bb3c23d0902c191, "eventButtonClicked");
            parameterRowVert.WireTo(id_4b5b1194dbec40a6a1e3a57a620a20a1, "children");
            id_d3053e8a06fb4a7f9bb3c23d0902c191.WireTo(id_f8baa31ee3804ae79b81455bdf0e9c61, "fanoutList");
            id_d3053e8a06fb4a7f9bb3c23d0902c191.WireTo(id_b728e4a4198347a6b0238676be69861e, "complete");
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




















































































































































































































































































































































































































































