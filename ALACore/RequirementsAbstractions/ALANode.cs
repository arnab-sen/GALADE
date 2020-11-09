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
        public bool ShowName { get; set; } = true;

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
        private Box _rootUI;
        private Box _selectedPort;
        private Point _mousePosInBox = new Point(0, 0);
        private List<Box> _inputPortBoxes = new List<Box>();
        private List<Box> _outputPortBoxes = new List<Box>();
        private List<string> _nodeParameters = new List<string>();
        private List<Tuple<Horizontal, DropDownMenu, TextBox, Button>> _nodeParameterRows = new List<Tuple<Horizontal, DropDownMenu, TextBox, Button>>();
        private Canvas _nodeMask = new Canvas();
        private Border _detailedRender = new Border();
        private Border _textMaskRender;
        private Text _textMask;

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

            CreateWiring();

        }

        public void RefreshParameterRows()
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

        private void CreateNodeParameterRow(string type = "", string name = "")
        {
	        var dropDown = new DropDownMenu() 
	        {
		        Text = type,
                Items = NodeParameters,
		        Width = 100
	        };
		        
	        var textBox = new TextBox() 
	        {
		        Text = name,
                Width = 100,
		        TrackIndent = true,
		        Font = "Consolas"
	        };
	        
	        var deleteButton = new Button("-") 
	        {
		        Width = 20,
		        Height = 20
	        };
	        
	        var dropDownUI = (dropDown as IUI).GetWPFElement() as ComboBox;
	        
	        var toolTipLabel = new System.Windows.Controls.Label() { Content = "" };
	        dropDownUI.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };
	        dropDownUI.MouseEnter += (sender, args) => toolTipLabel.Content = Model.GetType(dropDownUI.Text);
	        
	        dropDownUI.SelectionChanged += (sender, args) => textBox.Text = Model.GetValue(dropDownUI.SelectedValue?.ToString() ?? "");
	        
	        var horiz = new Horizontal();
	        horiz.WireTo(dropDown, "children");
	        horiz.WireTo(textBox, "children");
	        horiz.WireTo(deleteButton, "children");
	        
	        var buttonUI = (deleteButton as IUI).GetWPFElement() as System.Windows.Controls.Button;
	        
	        buttonUI.Click += (sender, args) => 
	        {
		        var row = _nodeParameterRows.FirstOrDefault(tuple => tuple.Item4.Equals(deleteButton));
		        _nodeParameterRows.Remove(row);
		        RefreshParameterRows();
	        };
	        
	        _nodeParameterRows.Add(Tuple.Create(horiz, dropDown, textBox, deleteButton));
        }

        private Border CreateTextMask(string text)
        {
            var maskContainer = new Border();

            _textMask = new Text(text)
            {
                FontSize = 30,
                FontWeight = FontWeights.Bold
            };

            maskContainer.Child = (_textMask as IUI).GetWPFElement();

            return maskContainer;
        }

        /// <summary>
        /// Replaces the node's UI with an enlarged text label containing the AbstractionModel's type. Useful for when the node is too small to read.
        /// </summary>
        /// <param name="show"></param>
        public void ShowTypeTextMask(bool show = true)
        {
            if (show)
            {
                if (_textMaskRender == null) _textMaskRender = CreateTextMask(Model.Type);

                if (!_nodeMask.Children.Contains(_textMaskRender))
                {
                    _nodeMask.Children.Add(_textMaskRender);
                }
                else
                {
                    _textMaskRender.Visibility = Visibility.Visible;
                }

                _detailedRender.Visibility = Visibility.Collapsed;
            }
            else
            {
                _textMaskRender.Visibility = Visibility.Collapsed;
                _detailedRender.Visibility = Visibility.Visible;
            }

        }

        private void CreateWiring()
        {
            _rootUI = new Box()
            {
                Background = Brushes.LightSkyBlue
            };

            // BEGIN AUTO-GENERATED INSTANTIATIONS
            Horizontal id_4f66a5309d464c62a1c6fafd0dc44779 = new Horizontal() {  };
            Vertical nodeMiddle = new Vertical() { InstanceName = "nodeMiddle", Margin = new Thickness(1, 0, 1, 0) };
            Horizontal nodeIdRow = new Horizontal() { InstanceName = "nodeIdRow" };
            DropDownMenu nodeTypeDropDownMenu = new DropDownMenu() { InstanceName = "nodeTypeDropDownMenu", Text = Model.Type, Width = 100, Items = AvailableDomainAbstractions };
            TextBox nodeNameTextBox = new TextBox() { InstanceName = "nodeNameTextBox", Text = ShowName ? Model.Name : "", Width = 50 };
            UIFactory id_e055b7e6dc8d480f9c1f80ab88451b98 = new UIFactory(getUIContainer: () =>{_inputPortsVert = new Vertical();_inputPortsVert.Margin = new Thickness(0);return _inputPortsVert;}) {  };
            ConvertToEvent<object> id_83e138e6890048b1aea41cf74216fa32 = new ConvertToEvent<object>() {  };
            Data<object> refreshInputPorts = new Data<object>() { InstanceName = "refreshInputPorts", Lambda = GetImplementedPorts };
            Cast<object, IEnumerable<Port>> id_a2fcb922f3d040f99adfedacbad79934 = new Cast<object, IEnumerable<Port>>() {  };
            ForEach<Port> id_fd2919809a774e6f83e06edae869dde3 = new ForEach<Port>() {  };
            Apply<Port, object> setUpPortBox = new Apply<Port, object>() { InstanceName = "setUpPortBox", Lambda = port =>{var box = new Box();box.Payload = port;box.Width = 50;box.Height = 15;box.Background = Brushes.White;box.BorderThickness = new Thickness(2);var toolTipLabel = new System.Windows.Controls.Label() { Content = port.ToString()};box.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = port.ToString();var text = new Text(text: port.Name);text.HorizAlignment = HorizontalAlignment.Center;box.Render.Child = (text as IUI).GetWPFElement();if (port.IsInputPort){_inputPortsVert.WireTo(box, "children");_inputPortBoxes.Add(box);(_inputPortsVert as IUI).GetWPFElement(); /* Refresh UI */}else{_outputPortsVert.WireTo(box, "children");_outputPortBoxes.Add(box);(_outputPortsVert as IUI).GetWPFElement(); /* Refresh UI */}return box;} };
            UIFactory id_c7df542fdf7f48708843d6bdd4be15ea = new UIFactory(getUIContainer: () =>{_outputPortsVert = new Vertical();_outputPortsVert.Margin = new Thickness(0);return _outputPortsVert;}) {  };
            ApplyAction<object> id_afcd6288424c4621b589d05af20eb2d7 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = Brushes.Red;} };
            DataFlowConnector<object> id_1efbf92d2af344c6b688c9d636252b56 = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_071eb926e57c41f2a47cd155746f8e85 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = Brushes.Black;} };
            Apply<object, object> id_cec56afb114e4a459e22c9bc53b17401 = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseEnter");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_73a1a2b69ac647128b19a0b9231e9ce7 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            Apply<object, object> id_841583aa6c3a452b99ba2bd6eaa79046 = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseLeave");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_b99c67f8f97e4d67be25f1f0915f2679 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            DataFlowConnector<object> addUIEventsToPort = new DataFlowConnector<object>() { InstanceName = "addUIEventsToPort" };
            Apply<object, object> id_4217d01954064647aaa95683bfd68756 = new Apply<object, object>() { Lambda = input =>{var mouseButtonEvent = new MouseButtonEvent("MouseDown");mouseButtonEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseButtonEvent, "eventHandlers");return mouseButtonEvent;} };
            DynamicWiring<IDataFlow<object>> id_07c75a68cdd64db8ab0848f196c97a2f = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_2cb584190bbe42be9a189755802ebc97 = new ApplyAction<object>() { Lambda = input =>{_selectedPort = input as Box;_selectedPort.Render.Focus();} };
            Apply<object, object> id_74701bb44c8541c8b8c828bcb25c9c1c = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("GotFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_1dfb3f0fd12646b9b1fbf95abd0869ed = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_65b4eebadd9449529f13e2531ccc07bd = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = Brushes.SandyBrown;} };
            Apply<object, object> id_7f052ccc12db48bd83a5f4b392b7ca84 = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("LostFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_774961256a9943cdb169fd34b5a0535a = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_90592f988d854331a253c06dc598b1fa = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = Brushes.White;} };
            MouseEvent id_c6b41e09d4a9460ea502017c5100d2e4 = new MouseEvent(eventName: "MouseEnter") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_6c83716ad86d45dea95ff9098f338dad = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.Aquamarine;} };
            MouseEvent id_87301b4c9909434d85ea45714da12967 = new MouseEvent(eventName: "MouseLeave") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_d69b233995474426b9ab1c025ae77be3 = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) (input as Box).Background = Brushes.LightSkyBlue;} };
            RoutedEventSubscriber id_2bcd551e8e594de0a7df510a479c7f38 = new RoutedEventSubscriber(eventName: "GotFocus") { ExtractSender = source => (source as Box).Render };
            RoutedEventSubscriber id_092d0fffc43c423b93c0db48b72cd059 = new RoutedEventSubscriber(eventName: "LostFocus") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_e0ae17cda7524a2ea6858b3f7bfa6020 = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.LightSkyBlue;} };
            ApplyAction<object> id_78d05efd1d9c406c8855bda74972cd5f = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) ui.Focus();} };
            ApplyAction<object> id_6885aee183d44ed0bbc8ca3199d55898 = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.Aquamarine;} };
            DataFlowConnector<object> id_65a655e33e2e479a88ae615abba3c5e1 = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_73ed03aefc584cdf8b41f5ed23ec1cfd = new ApplyAction<object>() { Lambda = input =>{StateTransition.Update(Enums.DiagramMode.IdleSelected);} };
            MouseEvent id_4bbab4363ad34037ad2b1ddd39c69ff2 = new MouseEvent(eventName: "MouseMove") { ExtractSender = source => (source as Box).Render, Condition = args => Mouse.LeftButton == MouseButtonState.Pressed && Keyboard.IsKeyDown(Key.LeftShift) };
            ApplyAction<object> id_b53d4e09d21e471dbcbd6dd6f14f4e3a = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;var mousePos = Mouse.GetPosition(Canvas);var oldPosition = new Point(Canvas.GetLeft(render), Canvas.GetTop(render));Canvas.SetLeft(render, mousePos.X - _mousePosInBox.X);Canvas.SetTop(render, mousePos.Y - _mousePosInBox.Y);PositionChanged?.Invoke();} };
            ApplyAction<object> id_b4d792842e354fbeba1c09c4d3b3d617 = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;_mousePosInBox = Mouse.GetPosition(render);Mouse.Capture(render);} };
            ApplyAction<object> id_a6edb39cb71e4085bb5e231c750e1bf4 = new ApplyAction<object>() { Lambda = input =>{Graph.Set("SelectedNode", this);} };
            MouseButtonEvent id_1ce4663bf4ad408a9001b8a8374386b2 = new MouseButtonEvent(eventName: "MouseLeftButtonDown") { ExtractSender = source => (source as Box).Render };
            ApplyAction<string> id_f2fc431eb19042e4bb84e11fab23f7f2 = new ApplyAction<string>() { Lambda = input =>{TypeChanged?.Invoke(input);} };
            MouseButtonEvent id_5916fef7c3e74af096f3f3f94c61f1ad = new MouseButtonEvent(eventName: "MouseLeftButtonUp") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_04be7b92b1994ee3a4159a9896c94be3 = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;if (Mouse.Captured?.Equals(render) ?? false) Mouse.Capture(null);} };
            ApplyAction<object> id_85297990877e41d6ad34a2ba08aebd70 = new ApplyAction<object>() { Lambda = input =>{(input as Box).InitialiseUI();} };
            EventLambda setNodeToolTip = new EventLambda() { InstanceName = "setNodeToolTip", Lambda = () => {var toolTipLabel = new System.Windows.Controls.Label() { Content = Model.GetDocumentation() };_rootUI.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };_rootUI.Render.MouseEnter += (sender, args) => toolTipLabel.Content = Model.GetDocumentation();} };
            Apply<object, object> id_d7bdacc2521448d7a478aa97ce449744 = new Apply<object, object>() { Lambda = input =>{var notUpdated = UpdatePorts(input as IEnumerable<Port>);return notUpdated;} };
            ConvertToEvent<object> id_413e0dc5adda4ed19709a75a233fb99b = new ConvertToEvent<object>() {  };
            Data<object> refreshOutputPorts = new Data<object>() { InstanceName = "refreshOutputPorts", Lambda = GetAcceptedPorts };
            Horizontal addNewParameterRow = new Horizontal() { InstanceName = "addNewParameterRow", Ratios = new[] { 40, 20, 40 } };
            Text id_3f8816d31b7840d4bb266bd7e009359a = new Text(text: "") {  };
            Button id_fa8527b246ab43288956bb5fcc3cb221 = new Button(title: "+") { Width = 20, Margin = new Thickness(5) };
            EventLambda id_4e600812333548ea8f7d12472ec7e6a8 = new EventLambda() { Lambda = () => {CreateNodeParameterRow("", "");} };
            EventConnector id_49f1116e5d194d13b5ea93c9e942bed8 = new EventConnector() {  };
            EventLambda id_64ff0d9e68dc4bf3834c4137b41f7926 = new EventLambda() { Lambda = RefreshParameterRows };
            Box id_bac316b759694cd0bc2cf71e4cb3158f = new Box() { Render = new Border() { Child = _parameterRowsPanel } };
            EventConnector id_88eaf01f8fce4eccadb07006c803379f = new EventConnector() {  };
            ContextMenu id_e743e199d8ea43f6963c5e0edd05f433 = new ContextMenu() {  };
            MenuItem id_550ac788b43946acbd5300225edb8b9d = new MenuItem(header: "Open source code...") {  };
            EventLambda id_504dc9df623f479eb04005e8a1feacb6 = new EventLambda() { Lambda = () =>{Process.Start(Model.GetCodeFilePath());} };
            MenuItem id_441a07d91a3d4078becf7b4e4de4d527 = new MenuItem(header: "Through your default external editor") {  };
            MenuItem id_d20e5d8ddbef4470a1beab41cf2e331a = new MenuItem(header: "Through the GALADE text editor") {  };
            Data<string> id_ac66465d328e4d1993b87879d468e48e = new Data<string>() { Lambda = Model.GetCodeFilePath };
            ApplyAction<object> id_412db3fb87e043dc83f97897382ebc6c = new ApplyAction<object>() { Lambda = input =>{if (StateTransition.CurrentStateMatches(Enums.DiagramMode.AwaitingPortSelection)){var wire = Graph.Get("SelectedWire") as ALAWire;if (wire == null) return;if (wire.Source == null){wire.Source = this;wire.SourcePort = input as Box;}else if (wire.Destination == null){wire.Destination = this;wire.DestinationPort = input as Box;}StateTransition.Update(Enums.DiagramMode.Idle);}} };
            DataFlowConnector<object> id_0a61a7dc202146c09afea8a2f95f8140 = new DataFlowConnector<object>() {  };
            KeyEvent id_4b8f5486bdd840ca93fb8567b9a481fb = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.LeftCtrl, Key.Q }, ExtractSender = source => (source as Box).Render };
            EventLambda id_5c544306db9f433682d2a7bb6e2f40b5 = new EventLambda() { Lambda = () =>{var sourcePort = GetSelectedPort();if (sourcePort == null) return;var source = this;var wire = new ALAWire(){Graph = Graph,Canvas = Canvas,Source = source,Destination = null,SourcePort = sourcePort,DestinationPort = null,StateTransition = StateTransition};Graph.AddEdge(wire);wire.Paint();wire.StartMoving(source: false);} };
            UIFactory id_5c42dae2d28f40b6b263bb71b9ea29e8 = new UIFactory(getUIContainer: () =>{foreach (var initialised in Model.GetInitialisedVariables()){CreateNodeParameterRow(initialised, Model.GetValue(initialised));}RefreshParameterRows();return new Text("");}) {  };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            _rootUI.WireTo(id_4f66a5309d464c62a1c6fafd0dc44779, "uiLayout");
            _rootUI.WireTo(id_c6b41e09d4a9460ea502017c5100d2e4, "eventHandlers");
            _rootUI.WireTo(id_87301b4c9909434d85ea45714da12967, "eventHandlers");
            _rootUI.WireTo(id_2bcd551e8e594de0a7df510a479c7f38, "eventHandlers");
            _rootUI.WireTo(id_092d0fffc43c423b93c0db48b72cd059, "eventHandlers");
            _rootUI.WireTo(id_4bbab4363ad34037ad2b1ddd39c69ff2, "eventHandlers");
            _rootUI.WireTo(id_1ce4663bf4ad408a9001b8a8374386b2, "eventHandlers");
            _rootUI.WireTo(id_5916fef7c3e74af096f3f3f94c61f1ad, "eventHandlers");
            _rootUI.WireTo(id_4b8f5486bdd840ca93fb8567b9a481fb, "eventHandlers");
            _rootUI.WireTo(id_e743e199d8ea43f6963c5e0edd05f433, "contextMenu");
            id_4f66a5309d464c62a1c6fafd0dc44779.WireTo(id_e055b7e6dc8d480f9c1f80ab88451b98, "children");
            id_4f66a5309d464c62a1c6fafd0dc44779.WireTo(nodeMiddle, "children");
            id_4f66a5309d464c62a1c6fafd0dc44779.WireTo(id_c7df542fdf7f48708843d6bdd4be15ea, "children");
            nodeMiddle.WireTo(nodeIdRow, "children");
            nodeMiddle.WireTo(parameterRowVert, "children");
            nodeMiddle.WireTo(addNewParameterRow, "children");
            nodeIdRow.WireTo(nodeTypeDropDownMenu, "children");
            nodeIdRow.WireTo(nodeNameTextBox, "children");
            nodeTypeDropDownMenu.WireTo(id_f2fc431eb19042e4bb84e11fab23f7f2, "selectedItem");
            id_e055b7e6dc8d480f9c1f80ab88451b98.WireTo(id_83e138e6890048b1aea41cf74216fa32, "uiInstanceOutput");
            id_83e138e6890048b1aea41cf74216fa32.WireTo(refreshInputPorts, "eventOutput");
            refreshInputPorts.WireTo(id_d7bdacc2521448d7a478aa97ce449744, "dataOutput");
            id_a2fcb922f3d040f99adfedacbad79934.WireTo(id_fd2919809a774e6f83e06edae869dde3, "output");
            id_fd2919809a774e6f83e06edae869dde3.WireTo(setUpPortBox, "elementOutput");
            id_fd2919809a774e6f83e06edae869dde3.WireTo(id_88eaf01f8fce4eccadb07006c803379f, "complete");
            setUpPortBox.WireTo(id_1efbf92d2af344c6b688c9d636252b56, "output");
            id_c7df542fdf7f48708843d6bdd4be15ea.WireTo(id_413e0dc5adda4ed19709a75a233fb99b, "uiInstanceOutput");
            id_1efbf92d2af344c6b688c9d636252b56.WireTo(addUIEventsToPort, "fanoutList");
            id_1efbf92d2af344c6b688c9d636252b56.WireTo(id_85297990877e41d6ad34a2ba08aebd70, "fanoutList");
            id_cec56afb114e4a459e22c9bc53b17401.WireTo(id_73a1a2b69ac647128b19a0b9231e9ce7, "output");
            id_73a1a2b69ac647128b19a0b9231e9ce7.WireTo(id_0a61a7dc202146c09afea8a2f95f8140, "wire");
            id_841583aa6c3a452b99ba2bd6eaa79046.WireTo(id_b99c67f8f97e4d67be25f1f0915f2679, "output");
            id_b99c67f8f97e4d67be25f1f0915f2679.WireTo(id_071eb926e57c41f2a47cd155746f8e85, "wire");
            addUIEventsToPort.WireTo(id_cec56afb114e4a459e22c9bc53b17401, "fanoutList");
            addUIEventsToPort.WireTo(id_841583aa6c3a452b99ba2bd6eaa79046, "fanoutList");
            addUIEventsToPort.WireTo(id_4217d01954064647aaa95683bfd68756, "fanoutList");
            addUIEventsToPort.WireTo(id_74701bb44c8541c8b8c828bcb25c9c1c, "fanoutList");
            addUIEventsToPort.WireTo(id_7f052ccc12db48bd83a5f4b392b7ca84, "fanoutList");
            id_4217d01954064647aaa95683bfd68756.WireTo(id_07c75a68cdd64db8ab0848f196c97a2f, "output");
            id_07c75a68cdd64db8ab0848f196c97a2f.WireTo(id_2cb584190bbe42be9a189755802ebc97, "wire");
            id_74701bb44c8541c8b8c828bcb25c9c1c.WireTo(id_1dfb3f0fd12646b9b1fbf95abd0869ed, "output");
            id_1dfb3f0fd12646b9b1fbf95abd0869ed.WireTo(id_65b4eebadd9449529f13e2531ccc07bd, "wire");
            id_7f052ccc12db48bd83a5f4b392b7ca84.WireTo(id_774961256a9943cdb169fd34b5a0535a, "output");
            id_774961256a9943cdb169fd34b5a0535a.WireTo(id_90592f988d854331a253c06dc598b1fa, "wire");
            id_c6b41e09d4a9460ea502017c5100d2e4.WireTo(id_6c83716ad86d45dea95ff9098f338dad, "sourceOutput");
            id_87301b4c9909434d85ea45714da12967.WireTo(id_d69b233995474426b9ab1c025ae77be3, "sourceOutput");
            id_2bcd551e8e594de0a7df510a479c7f38.WireTo(id_6885aee183d44ed0bbc8ca3199d55898, "sourceOutput");
            id_092d0fffc43c423b93c0db48b72cd059.WireTo(id_e0ae17cda7524a2ea6858b3f7bfa6020, "sourceOutput");
            id_65a655e33e2e479a88ae615abba3c5e1.WireTo(id_78d05efd1d9c406c8855bda74972cd5f, "fanoutList");
            id_65a655e33e2e479a88ae615abba3c5e1.WireTo(id_73ed03aefc584cdf8b41f5ed23ec1cfd, "fanoutList");
            id_65a655e33e2e479a88ae615abba3c5e1.WireTo(id_b4d792842e354fbeba1c09c4d3b3d617, "fanoutList");
            id_65a655e33e2e479a88ae615abba3c5e1.WireTo(id_a6edb39cb71e4085bb5e231c750e1bf4, "fanoutList");
            id_4bbab4363ad34037ad2b1ddd39c69ff2.WireTo(id_b53d4e09d21e471dbcbd6dd6f14f4e3a, "sourceOutput");
            id_1ce4663bf4ad408a9001b8a8374386b2.WireTo(id_65a655e33e2e479a88ae615abba3c5e1, "sourceOutput");
            id_5916fef7c3e74af096f3f3f94c61f1ad.WireTo(id_04be7b92b1994ee3a4159a9896c94be3, "sourceOutput");
            id_d7bdacc2521448d7a478aa97ce449744.WireTo(id_a2fcb922f3d040f99adfedacbad79934, "output");
            id_413e0dc5adda4ed19709a75a233fb99b.WireTo(refreshOutputPorts, "eventOutput");
            refreshOutputPorts.WireTo(id_d7bdacc2521448d7a478aa97ce449744, "dataOutput");
            addNewParameterRow.WireTo(id_5c42dae2d28f40b6b263bb71b9ea29e8, "children");
            addNewParameterRow.WireTo(id_fa8527b246ab43288956bb5fcc3cb221, "children");
            addNewParameterRow.WireTo(id_3f8816d31b7840d4bb266bd7e009359a, "children");
            id_fa8527b246ab43288956bb5fcc3cb221.WireTo(id_49f1116e5d194d13b5ea93c9e942bed8, "eventButtonClicked");
            parameterRowVert.WireTo(id_bac316b759694cd0bc2cf71e4cb3158f, "children");
            id_49f1116e5d194d13b5ea93c9e942bed8.WireTo(id_4e600812333548ea8f7d12472ec7e6a8, "fanoutList");
            id_49f1116e5d194d13b5ea93c9e942bed8.WireTo(id_64ff0d9e68dc4bf3834c4137b41f7926, "complete");
            id_88eaf01f8fce4eccadb07006c803379f.WireTo(setNodeToolTip, "fanoutList");
            id_e743e199d8ea43f6963c5e0edd05f433.WireTo(id_550ac788b43946acbd5300225edb8b9d, "children");
            id_550ac788b43946acbd5300225edb8b9d.WireTo(id_441a07d91a3d4078becf7b4e4de4d527, "children");
            id_550ac788b43946acbd5300225edb8b9d.WireTo(id_d20e5d8ddbef4470a1beab41cf2e331a, "children");
            id_441a07d91a3d4078becf7b4e4de4d527.WireTo(id_504dc9df623f479eb04005e8a1feacb6, "clickedEvent");
            id_d20e5d8ddbef4470a1beab41cf2e331a.WireTo(id_ac66465d328e4d1993b87879d468e48e, "clickedEvent");
            id_0a61a7dc202146c09afea8a2f95f8140.WireTo(id_afcd6288424c4621b589d05af20eb2d7, "fanoutList");
            id_0a61a7dc202146c09afea8a2f95f8140.WireTo(id_412db3fb87e043dc83f97897382ebc6c, "fanoutList");
            id_4b8f5486bdd840ca93fb8567b9a481fb.WireTo(id_5c544306db9f433682d2a7bb6e2f40b5, "eventHappened");
            // END AUTO-GENERATED WIRING

            Render = _nodeMask;
            _nodeMask.Children.Clear();
            _detailedRender.Child = (_rootUI as IUI).GetWPFElement();
            _nodeMask.Children.Add(_detailedRender);

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


































































































































































































































































































































































































































































































































































