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

        public double PositionX
        {
            get => Canvas.GetLeft(Render);
            set => Canvas.SetLeft(Render, value);
        }

        public double PositionY
        {
            get => Canvas.GetTop(Render);
            set => Canvas.SetTop(Render, value);
        }

        public double Width => _detailedRender.ActualWidth;
        public double Height => _detailedRender.ActualHeight;

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
        private Box rootUI;
        private Box _selectedPort;
        private Point _mousePosInBox = new Point(0, 0);
        private List<Box> _inputPortBoxes = new List<Box>();
        private List<Box> _outputPortBoxes = new List<Box>();
        private List<string> _nodeParameters = new List<string>();
        private List<Tuple<Horizontal, DropDownMenu, TextBox, Button>> _nodeParameterRows = new List<Tuple<Horizontal, DropDownMenu, TextBox, Button>>();
        private Canvas _nodeMask = new Canvas();
        private Border _detailedRender = new Border();
        private UIElement _textMaskRender;
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
            }, DispatcherPriority.Loaded);
            
            Render.Dispatcher.Invoke(() =>
            {
                _textMaskRender = CreateTextMask(Model.Type);
            }, DispatcherPriority.Loaded);

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

        private UIElement CreateTextMask(string text)
        {
            var maskContainer = new Canvas()
            {
                
            };

            var background = new Border()
            {
                Background = Brushes.White,
                Opacity = 0.5,
                Width = Width,
                Height = Height
            };

            // var foreground = new Border()
            // {
            //     Background = Brushes.Transparent,
            //     Width = Width,
            //     Height = Height
            // };

            var foreground = new Viewbox()
            {
                Width = Width,
                Height = Height
            };

            _textMask = new Text(text)
            {
                FontSize = 40,
                FontWeight = FontWeights.Bold,
                HorizAlignment = HorizontalAlignment.Center,
            };

            var textUI = (_textMask as IUI).GetWPFElement();
            textUI.ClipToBounds = false;
            foreground.Child = textUI;
            
            maskContainer.Children.Add(background);
            maskContainer.Children.Add(foreground);

            maskContainer.MouseEnter += (sender, args) => ShowTypeTextMask(false);
            

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
                    if (_textMaskRender != null) _textMaskRender.Visibility = Visibility.Visible;
                }

                // _detailedRender.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (_textMaskRender != null) _textMaskRender.Visibility = Visibility.Collapsed;
                // _detailedRender.Visibility = Visibility.Visible;
            }

        }

        private void CreateWiring()
        {
            rootUI = new Box()
            {
                Background = Brushes.LightSkyBlue
            };

            // BEGIN AUTO-GENERATED INSTANTIATIONS
            Horizontal id_a5e06e4b9dd2466296c8c52755aa94c7 = new Horizontal() {  };
            Vertical nodeMiddle = new Vertical() { InstanceName = "nodeMiddle", Margin = new Thickness(1, 0, 1, 0) };
            Horizontal nodeIdRow = new Horizontal() { InstanceName = "nodeIdRow" };
            DropDownMenu nodeTypeDropDownMenu = new DropDownMenu() { InstanceName = "nodeTypeDropDownMenu", Text = Model.Type, Width = 100, Items = AvailableDomainAbstractions };
            TextBox nodeNameTextBox = new TextBox() { InstanceName = "nodeNameTextBox", Text = ShowName ? Model.Name : "", Width = 50 };
            UIFactory id_2e29596583cb413783c3d09457d2e2a4 = new UIFactory(getUIContainer: () =>{_inputPortsVert = new Vertical();_inputPortsVert.Margin = new Thickness(0);return _inputPortsVert;}) {  };
            ConvertToEvent<object> id_e67a609eb2e2432c99930b57a81d0512 = new ConvertToEvent<object>() {  };
            Data<object> refreshInputPorts = new Data<object>() { InstanceName = "refreshInputPorts", Lambda = GetImplementedPorts };
            Cast<object, IEnumerable<Port>> id_970c7a8bdcb34509a6b8402f914a3fef = new Cast<object, IEnumerable<Port>>() {  };
            ForEach<Port> id_e41790a7bd904a7b98030afcaef81409 = new ForEach<Port>() {  };
            Apply<Port, object> setUpPortBox = new Apply<Port, object>() { InstanceName = "setUpPortBox", Lambda = port =>{var box = new Box();box.Payload = port;box.Width = 50;box.Height = 15;box.Background = Brushes.White;box.BorderThickness = new Thickness(2);var toolTipLabel = new System.Windows.Controls.Label() { Content = port.ToString()};box.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = port.ToString();var text = new Text(text: port.Name);text.HorizAlignment = HorizontalAlignment.Center;box.Render.Child = (text as IUI).GetWPFElement();if (port.IsInputPort){_inputPortsVert.WireTo(box, "children");_inputPortBoxes.Add(box);(_inputPortsVert as IUI).GetWPFElement(); /* Refresh UI */}else{_outputPortsVert.WireTo(box, "children");_outputPortBoxes.Add(box);(_outputPortsVert as IUI).GetWPFElement(); /* Refresh UI */}return box;} };
            UIFactory id_6972c047a04d4f6dbe2417bb58275a98 = new UIFactory(getUIContainer: () =>{_outputPortsVert = new Vertical();_outputPortsVert.Margin = new Thickness(0);return _outputPortsVert;}) {  };
            ApplyAction<object> id_2416e3ce8b8c4f359e01b3edeac009ae = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = Brushes.Red;} };
            DataFlowConnector<object> id_f3dbb781c1d34739a0029932f8b04f30 = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_41f9b51e1825435c9930ddd012650c1b = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = Brushes.Black;} };
            Apply<object, object> id_08901c73c88e4f038cf5a390be13f345 = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseEnter");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_52ba1a36ec6e4425bc613972324bf567 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            Apply<object, object> id_f26e2f3546774f8bb95199119c1962de = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseLeave");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_f25730cacae649eb8fb5bdb267410d3a = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            DataFlowConnector<object> addUIEventsToPort = new DataFlowConnector<object>() { InstanceName = "addUIEventsToPort" };
            Apply<object, object> id_bde0d5b29f364d918ff5d1bc5acf77af = new Apply<object, object>() { Lambda = input =>{var mouseButtonEvent = new MouseButtonEvent("MouseDown");mouseButtonEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseButtonEvent, "eventHandlers");return mouseButtonEvent;} };
            DynamicWiring<IDataFlow<object>> id_7624c8c7cf764b35b15b50cac8f59955 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_34cc04308ad145549df173dcdcae3648 = new ApplyAction<object>() { Lambda = input =>{_selectedPort = input as Box;_selectedPort.Render.Focus();} };
            Apply<object, object> id_bef63d80635a4b46be21fb072a5840d8 = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("GotFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_eb6bd0f0196b4ed789faec7700c18a86 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_5cdebf5177014b15bf0f160b88e701ed = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = Brushes.SandyBrown;} };
            Apply<object, object> id_956be038fd614053aa1233c8412e736c = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("LostFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_5992c124d1b341a99cb9eb7ec56bab2c = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_10683130fc9a495aa215265c4e3d0b97 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = Brushes.White;} };
            MouseEvent id_ae85f6a47c714fc48abc335cd2b46eb9 = new MouseEvent(eventName: "MouseEnter") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_caa68fc34d3145a9a88820625fe01240 = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.Aquamarine;} };
            MouseEvent id_108c3cf4c6ba4bb6bc05390abdddb79b = new MouseEvent(eventName: "MouseLeave") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_f661b21514a843fc943e33bab2664389 = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) (input as Box).Background = Brushes.LightSkyBlue;} };
            RoutedEventSubscriber id_029a8c38eaae4e86acb184daa8e1d852 = new RoutedEventSubscriber(eventName: "GotFocus") { ExtractSender = source => (source as Box).Render };
            RoutedEventSubscriber id_e7be38702bbd497b9f1c5279dfd6d343 = new RoutedEventSubscriber(eventName: "LostFocus") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_74ea462ae1114a9fb55f34b46b94392d = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.LightSkyBlue;} };
            ApplyAction<object> id_5e83b59f404f409583588ded0e085b40 = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) ui.Focus();} };
            ApplyAction<object> id_d73cd920bad74fccae203508181a3612 = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.Aquamarine;} };
            DataFlowConnector<object> id_b36b7fdbe13841669615741b9c16ce9b = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_69dd6066e5cc494f87d7be966185324f = new ApplyAction<object>() { Lambda = input =>{StateTransition.Update(Enums.DiagramMode.IdleSelected);} };
            MouseEvent id_fd83c28d8ccb47bd9f77ba80318929ae = new MouseEvent(eventName: "MouseMove") { ExtractSender = source => (source as Box).Render, Condition = args => Mouse.LeftButton == MouseButtonState.Pressed && Keyboard.IsKeyDown(Key.LeftShift) };
            ApplyAction<object> id_d4b8eed7a1b44946870ce21294b34b20 = new ApplyAction<object>() { Lambda = input =>{var mousePos = Mouse.GetPosition(Canvas);var oldPosition = new Point(PositionX, PositionY);PositionX = mousePos.X - _mousePosInBox.X;PositionY = mousePos.Y - _mousePosInBox.Y;PositionChanged?.Invoke();} };
            ApplyAction<object> id_73374037c62b49ed821905c4b166c01d = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;_mousePosInBox = Mouse.GetPosition(render);Mouse.Capture(render);} };
            ApplyAction<object> id_e5271567f709419c9cc12337a2668693 = new ApplyAction<object>() { Lambda = input =>{Graph.Set("SelectedNode", this);} };
            MouseButtonEvent id_acf5b98cdf814e73bfd3f1967c633308 = new MouseButtonEvent(eventName: "MouseLeftButtonDown") { ExtractSender = source => (source as Box).Render };
            ApplyAction<string> id_8dcd392a34f641c989709f4be1ba1958 = new ApplyAction<string>() { Lambda = input =>{TypeChanged?.Invoke(input);} };
            MouseButtonEvent id_6d2af0cd86b8451a97506cd61e2099f3 = new MouseButtonEvent(eventName: "MouseLeftButtonUp") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_cdd3e075b8724ecd9ec3a4d6a275480c = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;if (Mouse.Captured?.Equals(render) ?? false) Mouse.Capture(null);} };
            ApplyAction<object> id_682faf4db2df490aa5496b1d4ca562fd = new ApplyAction<object>() { Lambda = input =>{(input as Box).InitialiseUI();} };
            EventLambda setNodeToolTip = new EventLambda() { InstanceName = "setNodeToolTip", Lambda = () => {var toolTipLabel = new System.Windows.Controls.Label() { Content = Model.GetDocumentation() };rootUI.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };rootUI.Render.MouseEnter += (sender, args) => toolTipLabel.Content = Model.GetDocumentation();} };
            Apply<object, object> id_7ba97fdf7a6a46ecb0a39a1fa9cfe560 = new Apply<object, object>() { Lambda = input =>{var notUpdated = UpdatePorts(input as IEnumerable<Port>);return notUpdated;} };
            ConvertToEvent<object> id_28cfe12e225542428265f746ca7156b6 = new ConvertToEvent<object>() {  };
            Data<object> refreshOutputPorts = new Data<object>() { InstanceName = "refreshOutputPorts", Lambda = GetAcceptedPorts };
            Horizontal addNewParameterRow = new Horizontal() { InstanceName = "addNewParameterRow", Ratios = new[] { 40, 20, 40 } };
            Text id_e4080a86062a46339bfb7bcffcf46e0a = new Text(text: "") {  };
            Button id_76635d7291ee4adeacccf570d667d41f = new Button(title: "+") { Width = 20, Margin = new Thickness(5) };
            EventLambda id_1b8f34457ed34c91b2a6e036d6449748 = new EventLambda() { Lambda = () => {CreateNodeParameterRow("", "");} };
            EventConnector id_514bd27531974b49ab1d2c3b69119eca = new EventConnector() {  };
            EventLambda id_904892a5b72042838191acf66e9537fc = new EventLambda() { Lambda = RefreshParameterRows };
            Box id_3c43deb17270472e93de17357bf2b394 = new Box() { Render = new Border() { Child = _parameterRowsPanel } };
            EventConnector id_3c6a66f87ad84fed90915ab5d0bce21c = new EventConnector() {  };
            ContextMenu id_e819c773581c4490b1449b9e4e3f52dc = new ContextMenu() {  };
            MenuItem id_2222ef15316047b58bb5d3d3707ef492 = new MenuItem(header: "Open source code...") {  };
            EventLambda id_4803091f12dd46959aac3edacb8545c5 = new EventLambda() { Lambda = () =>{Process.Start(Model.GetCodeFilePath());} };
            MenuItem id_041b027553f748e8b24d4ecae0813865 = new MenuItem(header: "Through your default external editor") {  };
            MenuItem id_a065bea77d724332b6346cd5e361456c = new MenuItem(header: "Through the GALADE text editor") {  };
            Data<string> id_a33a2f182eff45c6b3d9a1ed9de1f3b8 = new Data<string>() { Lambda = Model.GetCodeFilePath };
            ApplyAction<object> id_63f4dd6f49c74cd681ad11acd5e34017 = new ApplyAction<object>() { Lambda = input =>{if (StateTransition.CurrentStateMatches(Enums.DiagramMode.AwaitingPortSelection)){var wire = Graph.Get("SelectedWire") as ALAWire;if (wire == null) return;if (wire.Source == null){wire.Source = this;wire.SourcePort = input as Box;}else if (wire.Destination == null){wire.Destination = this;wire.DestinationPort = input as Box;}StateTransition.Update(Enums.DiagramMode.Idle);}} };
            DataFlowConnector<object> id_be434728a47043839978d8c92852e93c = new DataFlowConnector<object>() {  };
            KeyEvent id_460664cc6a364d8b9f33f311e80d1d75 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.LeftCtrl, Key.Q }, ExtractSender = source => (source as Box).Render };
            EventLambda id_a38df30ea73444d8a37b3b4f2c552d38 = new EventLambda() { Lambda = () =>{var sourcePort = GetSelectedPort();if (sourcePort == null) return;var source = this;var wire = new ALAWire(){Graph = Graph,Canvas = Canvas,Source = source,Destination = null,SourcePort = sourcePort,DestinationPort = null,StateTransition = StateTransition};Graph.AddEdge(wire);wire.Paint();wire.StartMoving(source: false);} };
            UIFactory id_e9315dd648f34268a882c632d7ddea68 = new UIFactory(getUIContainer: () =>{foreach (var initialised in Model.GetInitialisedVariables()){CreateNodeParameterRow(initialised, Model.GetValue(initialised));}RefreshParameterRows();return new Text("");}) {  };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            rootUI.WireTo(id_a5e06e4b9dd2466296c8c52755aa94c7, "uiLayout");
            rootUI.WireTo(id_ae85f6a47c714fc48abc335cd2b46eb9, "eventHandlers");
            rootUI.WireTo(id_108c3cf4c6ba4bb6bc05390abdddb79b, "eventHandlers");
            rootUI.WireTo(id_029a8c38eaae4e86acb184daa8e1d852, "eventHandlers");
            rootUI.WireTo(id_e7be38702bbd497b9f1c5279dfd6d343, "eventHandlers");
            rootUI.WireTo(id_fd83c28d8ccb47bd9f77ba80318929ae, "eventHandlers");
            rootUI.WireTo(id_acf5b98cdf814e73bfd3f1967c633308, "eventHandlers");
            rootUI.WireTo(id_6d2af0cd86b8451a97506cd61e2099f3, "eventHandlers");
            rootUI.WireTo(id_460664cc6a364d8b9f33f311e80d1d75, "eventHandlers");
            rootUI.WireTo(id_e819c773581c4490b1449b9e4e3f52dc, "contextMenu");
            id_a5e06e4b9dd2466296c8c52755aa94c7.WireTo(id_2e29596583cb413783c3d09457d2e2a4, "children");
            id_a5e06e4b9dd2466296c8c52755aa94c7.WireTo(nodeMiddle, "children");
            id_a5e06e4b9dd2466296c8c52755aa94c7.WireTo(id_6972c047a04d4f6dbe2417bb58275a98, "children");
            nodeMiddle.WireTo(nodeIdRow, "children");
            nodeMiddle.WireTo(parameterRowVert, "children");
            nodeMiddle.WireTo(addNewParameterRow, "children");
            nodeIdRow.WireTo(nodeTypeDropDownMenu, "children");
            nodeIdRow.WireTo(nodeNameTextBox, "children");
            nodeTypeDropDownMenu.WireTo(id_8dcd392a34f641c989709f4be1ba1958, "selectedItem");
            id_2e29596583cb413783c3d09457d2e2a4.WireTo(id_e67a609eb2e2432c99930b57a81d0512, "uiInstanceOutput");
            id_e67a609eb2e2432c99930b57a81d0512.WireTo(refreshInputPorts, "eventOutput");
            refreshInputPorts.WireTo(id_7ba97fdf7a6a46ecb0a39a1fa9cfe560, "dataOutput");
            id_970c7a8bdcb34509a6b8402f914a3fef.WireTo(id_e41790a7bd904a7b98030afcaef81409, "output");
            id_e41790a7bd904a7b98030afcaef81409.WireTo(setUpPortBox, "elementOutput");
            id_e41790a7bd904a7b98030afcaef81409.WireTo(id_3c6a66f87ad84fed90915ab5d0bce21c, "complete");
            setUpPortBox.WireTo(id_f3dbb781c1d34739a0029932f8b04f30, "output");
            id_6972c047a04d4f6dbe2417bb58275a98.WireTo(id_28cfe12e225542428265f746ca7156b6, "uiInstanceOutput");
            id_f3dbb781c1d34739a0029932f8b04f30.WireTo(addUIEventsToPort, "fanoutList");
            id_f3dbb781c1d34739a0029932f8b04f30.WireTo(id_682faf4db2df490aa5496b1d4ca562fd, "fanoutList");
            id_08901c73c88e4f038cf5a390be13f345.WireTo(id_52ba1a36ec6e4425bc613972324bf567, "output");
            id_52ba1a36ec6e4425bc613972324bf567.WireTo(id_be434728a47043839978d8c92852e93c, "wire");
            id_f26e2f3546774f8bb95199119c1962de.WireTo(id_f25730cacae649eb8fb5bdb267410d3a, "output");
            id_f25730cacae649eb8fb5bdb267410d3a.WireTo(id_41f9b51e1825435c9930ddd012650c1b, "wire");
            addUIEventsToPort.WireTo(id_08901c73c88e4f038cf5a390be13f345, "fanoutList");
            addUIEventsToPort.WireTo(id_f26e2f3546774f8bb95199119c1962de, "fanoutList");
            addUIEventsToPort.WireTo(id_bde0d5b29f364d918ff5d1bc5acf77af, "fanoutList");
            addUIEventsToPort.WireTo(id_bef63d80635a4b46be21fb072a5840d8, "fanoutList");
            addUIEventsToPort.WireTo(id_956be038fd614053aa1233c8412e736c, "fanoutList");
            id_bde0d5b29f364d918ff5d1bc5acf77af.WireTo(id_7624c8c7cf764b35b15b50cac8f59955, "output");
            id_7624c8c7cf764b35b15b50cac8f59955.WireTo(id_34cc04308ad145549df173dcdcae3648, "wire");
            id_bef63d80635a4b46be21fb072a5840d8.WireTo(id_eb6bd0f0196b4ed789faec7700c18a86, "output");
            id_eb6bd0f0196b4ed789faec7700c18a86.WireTo(id_5cdebf5177014b15bf0f160b88e701ed, "wire");
            id_956be038fd614053aa1233c8412e736c.WireTo(id_5992c124d1b341a99cb9eb7ec56bab2c, "output");
            id_5992c124d1b341a99cb9eb7ec56bab2c.WireTo(id_10683130fc9a495aa215265c4e3d0b97, "wire");
            id_ae85f6a47c714fc48abc335cd2b46eb9.WireTo(id_caa68fc34d3145a9a88820625fe01240, "sourceOutput");
            id_108c3cf4c6ba4bb6bc05390abdddb79b.WireTo(id_f661b21514a843fc943e33bab2664389, "sourceOutput");
            id_029a8c38eaae4e86acb184daa8e1d852.WireTo(id_d73cd920bad74fccae203508181a3612, "sourceOutput");
            id_e7be38702bbd497b9f1c5279dfd6d343.WireTo(id_74ea462ae1114a9fb55f34b46b94392d, "sourceOutput");
            id_b36b7fdbe13841669615741b9c16ce9b.WireTo(id_5e83b59f404f409583588ded0e085b40, "fanoutList");
            id_b36b7fdbe13841669615741b9c16ce9b.WireTo(id_69dd6066e5cc494f87d7be966185324f, "fanoutList");
            id_b36b7fdbe13841669615741b9c16ce9b.WireTo(id_73374037c62b49ed821905c4b166c01d, "fanoutList");
            id_b36b7fdbe13841669615741b9c16ce9b.WireTo(id_e5271567f709419c9cc12337a2668693, "fanoutList");
            id_fd83c28d8ccb47bd9f77ba80318929ae.WireTo(id_d4b8eed7a1b44946870ce21294b34b20, "sourceOutput");
            id_acf5b98cdf814e73bfd3f1967c633308.WireTo(id_b36b7fdbe13841669615741b9c16ce9b, "sourceOutput");
            id_6d2af0cd86b8451a97506cd61e2099f3.WireTo(id_cdd3e075b8724ecd9ec3a4d6a275480c, "sourceOutput");
            id_7ba97fdf7a6a46ecb0a39a1fa9cfe560.WireTo(id_970c7a8bdcb34509a6b8402f914a3fef, "output");
            id_28cfe12e225542428265f746ca7156b6.WireTo(refreshOutputPorts, "eventOutput");
            refreshOutputPorts.WireTo(id_7ba97fdf7a6a46ecb0a39a1fa9cfe560, "dataOutput");
            addNewParameterRow.WireTo(id_e9315dd648f34268a882c632d7ddea68, "children");
            addNewParameterRow.WireTo(id_76635d7291ee4adeacccf570d667d41f, "children");
            addNewParameterRow.WireTo(id_e4080a86062a46339bfb7bcffcf46e0a, "children");
            id_76635d7291ee4adeacccf570d667d41f.WireTo(id_514bd27531974b49ab1d2c3b69119eca, "eventButtonClicked");
            parameterRowVert.WireTo(id_3c43deb17270472e93de17357bf2b394, "children");
            id_514bd27531974b49ab1d2c3b69119eca.WireTo(id_1b8f34457ed34c91b2a6e036d6449748, "fanoutList");
            id_514bd27531974b49ab1d2c3b69119eca.WireTo(id_904892a5b72042838191acf66e9537fc, "complete");
            id_3c6a66f87ad84fed90915ab5d0bce21c.WireTo(setNodeToolTip, "fanoutList");
            id_e819c773581c4490b1449b9e4e3f52dc.WireTo(id_2222ef15316047b58bb5d3d3707ef492, "children");
            id_2222ef15316047b58bb5d3d3707ef492.WireTo(id_041b027553f748e8b24d4ecae0813865, "children");
            id_2222ef15316047b58bb5d3d3707ef492.WireTo(id_a065bea77d724332b6346cd5e361456c, "children");
            id_041b027553f748e8b24d4ecae0813865.WireTo(id_4803091f12dd46959aac3edacb8545c5, "clickedEvent");
            id_a065bea77d724332b6346cd5e361456c.WireTo(id_a33a2f182eff45c6b3d9a1ed9de1f3b8, "clickedEvent");
            id_be434728a47043839978d8c92852e93c.WireTo(id_2416e3ce8b8c4f359e01b3edeac009ae, "fanoutList");
            id_be434728a47043839978d8c92852e93c.WireTo(id_63f4dd6f49c74cd681ad11acd5e34017, "fanoutList");
            id_460664cc6a364d8b9f33f311e80d1d75.WireTo(id_a38df30ea73444d8a37b3b4f2c552d38, "eventHappened");
            // END AUTO-GENERATED WIRING

            Render = _nodeMask;
            _nodeMask.Children.Clear();
            _detailedRender.Child = (rootUI as IUI).GetWPFElement();
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










































































































































































































































































































































































































































































































































































