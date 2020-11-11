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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
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
        public string Name => !string.IsNullOrWhiteSpace(Model?.Name) ? Model.Name : "id_" + Id;
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
		        Width = 100,
                Height = 25
	        };
		        
	        var textBox = new TextBox() 
	        {
		        Text = name,
                Width = 100,
		        TrackIndent = true,
		        Font = "Consolas",
                TabString = "    "
	        };

            textBox.WireTo(new ApplyAction<string>()
            {
                Lambda = s =>
                {
                    Model.SetValue(dropDown.Text, s, initialise: true);
                }
            }, "textOutput");
	        
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

        public string ToInstantiation()
        {
            // Note: must declare "using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;" in this file's usings
            // to use SyntaxFactory methods without repeatedly referencing SyntaxFactory

            // The SyntaxNode structure is generated from https://roslynquoter.azurewebsites.net/

            var instantiation = "";
            var fullStr = "";

            var initialised = Model.GetInitialisedVariables();
            var constructorArgs = Model.GetConstructorArgs()
                .Where(kvp => initialised.Contains(kvp.Key))
                .ToList();

            var properties = Model.GetProperties()
                .Where(kvp => initialised.Contains(kvp.Key))
                .ToList();

            var syntaxNode =
                LocalDeclarationStatement(
                    VariableDeclaration(
                            IdentifierName("var"))
                        .WithVariables(
                            SingletonSeparatedList<VariableDeclaratorSyntax>(
                                VariableDeclarator(
                                        Identifier(Model.Name == "" ? "id_" + Id : Model.Name))
                                    .WithInitializer(
                                        EqualsValueClause(
                                            ObjectCreationExpression(
                                                    IdentifierName(Model.FullType))
                                                .WithArgumentList(
                                                    ArgumentList(
                                                        GetConstructorArgumentSyntaxList(constructorArgs)))
                                                .WithInitializer(
                                                    InitializerExpression(
                                                        SyntaxKind.ObjectInitializerExpression,
                                                        GetPropertySyntaxList(properties))))))));

            fullStr = syntaxNode.ToFullString();

            instantiation = syntaxNode.NormalizeWhitespace().ToString();

            return instantiation;
        }

        private SeparatedSyntaxList<ArgumentSyntax> GetConstructorArgumentSyntaxList(List<KeyValuePair<string, string>> args)
        {
            var list = new List<SyntaxNodeOrToken>();

            foreach (var arg in args)
            {
                var argName = arg.Key;
                var argValue = arg.Value;

                var argNode = 
                    Argument(
                            IdentifierName(argValue))
                        .WithNameColon(
                            NameColon(
                                IdentifierName(argName)));

                if (list.Count > 0)
                {
                    list.Add(Token(SyntaxKind.CommaToken));
                }

                list.Add(argNode);
            }

            return SeparatedList<ArgumentSyntax>(list);
        }

        private SeparatedSyntaxList<ExpressionSyntax> GetPropertySyntaxList(List<KeyValuePair<string, string>> properties)
        {
            var list = new List<SyntaxNodeOrToken>();

            foreach (var prop in properties)
            {
                var propName = prop.Key;
                var propValue = prop.Value;

                var propNode =
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(propName),
                        IdentifierName(propValue));

                if (list.Count > 0)
                {
                    list.Add(Token(SyntaxKind.CommaToken));
                }

                list.Add(propNode);
            }

            return SeparatedList<ExpressionSyntax>(list);
        }

        private void CreateWiring()
        {
            rootUI = new Box()
            {
                Background = Brushes.LightSkyBlue
            };

            // BEGIN AUTO-GENERATED INSTANTIATIONS
            Horizontal id_9d624a2b2b564e35bb5078a69edd3791 = new Horizontal() {  };
            Vertical nodeMiddle = new Vertical() { InstanceName = "nodeMiddle", Margin = new Thickness(1, 0, 1, 0) };
            Horizontal nodeIdRow = new Horizontal() { InstanceName = "nodeIdRow" };
            DropDownMenu nodeTypeDropDownMenu = new DropDownMenu() { InstanceName = "nodeTypeDropDownMenu", Text = Model.Type, Width = 100, Items = AvailableDomainAbstractions };
            TextBox nodeNameTextBox = new TextBox() { InstanceName = "nodeNameTextBox", Text = ShowName ? Model.Name : "", Width = 50 };
            UIFactory id_cc0274fb92154581b473209f4d9f6c06 = new UIFactory(getUIContainer: () =>{_inputPortsVert = new Vertical();_inputPortsVert.Margin = new Thickness(0);return _inputPortsVert;}) {  };
            ConvertToEvent<object> id_a12557620a094dd6ae74b4cb8fc2f011 = new ConvertToEvent<object>() {  };
            Data<object> refreshInputPorts = new Data<object>() { InstanceName = "refreshInputPorts", Lambda = GetImplementedPorts };
            Cast<object, IEnumerable<Port>> id_2f4940b68752432eb5f18aa91f276cb0 = new Cast<object, IEnumerable<Port>>() {  };
            ForEach<Port> id_c27a41137e3f45c78b67144163627dd4 = new ForEach<Port>() {  };
            Apply<Port, object> setUpPortBox = new Apply<Port, object>() { InstanceName = "setUpPortBox", Lambda = port =>{var box = new Box();box.Payload = port;box.Width = 50;box.Height = 15;box.Background = Brushes.White;box.BorderThickness = new Thickness(2);var toolTipLabel = new System.Windows.Controls.Label() { Content = port.ToString()};box.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = port.ToString();var text = new Text(text: port.Name);text.HorizAlignment = HorizontalAlignment.Center;box.Render.Child = (text as IUI).GetWPFElement();if (port.IsInputPort){_inputPortsVert.WireTo(box, "children");_inputPortBoxes.Add(box);(_inputPortsVert as IUI).GetWPFElement(); /* Refresh UI */}else{_outputPortsVert.WireTo(box, "children");_outputPortBoxes.Add(box);(_outputPortsVert as IUI).GetWPFElement(); /* Refresh UI */}return box;} };
            UIFactory id_67b48c399ec640bba6aa409fd5cf6bd2 = new UIFactory(getUIContainer: () =>{_outputPortsVert = new Vertical();_outputPortsVert.Margin = new Thickness(0);return _outputPortsVert;}) {  };
            ApplyAction<object> id_86b463a992174079afa241c1aa42c05f = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = Brushes.Red;} };
            DataFlowConnector<object> id_7c99b6987fce48f99351f3a0d6045075 = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_f260c09b8c88430ba5832afcb7520244 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = Brushes.Black;} };
            Apply<object, object> id_50392509d3fd4fbc94e3353d3d3447c4 = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseEnter");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_20d36e8ff00045c6896f2200b9068e0f = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            Apply<object, object> id_b339d290f2bd4db59f4566689a7e2bbf = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseLeave");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_8d5c4dddd7c34a74a4a4f11ef3dd8db9 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            DataFlowConnector<object> addUIEventsToPort = new DataFlowConnector<object>() { InstanceName = "addUIEventsToPort" };
            Apply<object, object> id_cce4c32ddb5746ab99f7f101ba85097e = new Apply<object, object>() { Lambda = input =>{var mouseButtonEvent = new MouseButtonEvent("MouseDown");mouseButtonEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseButtonEvent, "eventHandlers");return mouseButtonEvent;} };
            DynamicWiring<IDataFlow<object>> id_1879c0e488534615a8e571e5e40eb661 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_ffaa712b9d26422da8ac6840f228bee2 = new ApplyAction<object>() { Lambda = input =>{_selectedPort = input as Box;_selectedPort.Render.Focus();} };
            Apply<object, object> id_3154b8324ea34b8bba3052a914f4b611 = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("GotFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_a1508569b00f4856aaa35390d746201c = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_0568cb2edbb84e02a199e5df5f6c3a1e = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = Brushes.SandyBrown;} };
            Apply<object, object> id_ad95b0114e4d4fc8bf4da30476209a97 = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("LostFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_5e3fbeb0c15b481a88747035fd14b049 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_53ae33610d0d4396bf8408a0391b54ec = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = Brushes.White;} };
            MouseEvent id_18938ca5b8324a8c805f6a9190cb79f8 = new MouseEvent(eventName: "MouseEnter") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_7038e9301b8f42d29abc03a4152ed88b = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.Aquamarine;} };
            MouseEvent id_11f5e83640d9433788585fd1c0cfba8e = new MouseEvent(eventName: "MouseLeave") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_832713c5c1c24ca8ae20ddd48360b420 = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) (input as Box).Background = Brushes.LightSkyBlue;} };
            RoutedEventSubscriber id_688acd495c4942fbb796183fc7b0f454 = new RoutedEventSubscriber(eventName: "GotFocus") { ExtractSender = source => (source as Box).Render };
            RoutedEventSubscriber id_9042cfdb1f50407dae652da1e4e072f2 = new RoutedEventSubscriber(eventName: "LostFocus") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_eb3156915d2047efa7706c86fc38e2d5 = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.LightSkyBlue;} };
            ApplyAction<object> id_ab5f3dd7a4344afcb1d05c21a522629c = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) ui.Focus();} };
            ApplyAction<object> id_438fd1479ba343a985a38be1c6267f07 = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.Aquamarine;} };
            DataFlowConnector<object> id_b21b566b17fe4343b6e96cc1b19afdce = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_1f0d01bcdf30470791a50d75cd69a0e5 = new ApplyAction<object>() { Lambda = input =>{StateTransition.Update(Enums.DiagramMode.IdleSelected);} };
            MouseEvent id_4092a8fcc0e5437faab2de15c29e540c = new MouseEvent(eventName: "MouseMove") { ExtractSender = source => (source as Box).Render, Condition = args => Mouse.LeftButton == MouseButtonState.Pressed && Keyboard.IsKeyDown(Key.LeftShift) };
            ApplyAction<object> id_e2cb57aab2d54ff884d5ef64476e335d = new ApplyAction<object>() { Lambda = input =>{var mousePos = Mouse.GetPosition(Canvas);var oldPosition = new Point(PositionX, PositionY);PositionX = mousePos.X - _mousePosInBox.X;PositionY = mousePos.Y - _mousePosInBox.Y;PositionChanged?.Invoke();} };
            ApplyAction<object> id_fdc35b60fa9a46478b75020e5c244f90 = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;_mousePosInBox = Mouse.GetPosition(render);Mouse.Capture(render);} };
            ApplyAction<object> id_147093c880c04b888952afa977dca339 = new ApplyAction<object>() { Lambda = input =>{Graph.Set("SelectedNode", this);} };
            MouseButtonEvent id_569fbefece75444080a2e577ef67329a = new MouseButtonEvent(eventName: "MouseLeftButtonDown") { ExtractSender = source => (source as Box).Render };
            ApplyAction<string> id_5e806d54c31440219227bd3aeb5cd71e = new ApplyAction<string>() { Lambda = input =>{TypeChanged?.Invoke(input);} };
            MouseButtonEvent id_9128ab8d47e84d77a9ac2846a2ef160f = new MouseButtonEvent(eventName: "MouseLeftButtonUp") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_96f2445e2d824dbf9f7d7a5ba99ac80d = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;if (Mouse.Captured?.Equals(render) ?? false) Mouse.Capture(null);} };
            ApplyAction<object> id_782ed13f4131486e88c0361868cf9b4c = new ApplyAction<object>() { Lambda = input =>{(input as Box).InitialiseUI();} };
            EventLambda setNodeToolTip = new EventLambda() { InstanceName = "setNodeToolTip", Lambda = () => {var toolTipLabel = new System.Windows.Controls.Label() { Content = Model.GetDocumentation() };rootUI.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };rootUI.Render.MouseEnter += (sender, args) => toolTipLabel.Content = Model.GetDocumentation();} };
            Apply<object, object> id_6c9b8deaa3584178bd497347615ce613 = new Apply<object, object>() { Lambda = input =>{var notUpdated = UpdatePorts(input as IEnumerable<Port>);return notUpdated;} };
            ConvertToEvent<object> id_5325cc4494e7453db39ee8c550d2d314 = new ConvertToEvent<object>() {  };
            Data<object> refreshOutputPorts = new Data<object>() { InstanceName = "refreshOutputPorts", Lambda = GetAcceptedPorts };
            Horizontal addNewParameterRow = new Horizontal() { InstanceName = "addNewParameterRow", Ratios = new[] { 40, 20, 40 } };
            Text id_0f97f97574ab46aaba7046044b5cbddc = new Text(text: "") {  };
            Button id_a571df327c994486b385bed708bcb9d5 = new Button(title: "+") { Width = 20, Margin = new Thickness(5) };
            EventLambda id_f6243c4b498449748dbdffed2de78018 = new EventLambda() { Lambda = () => {CreateNodeParameterRow("", "");} };
            EventConnector id_c8fc18724a1f4fc0a5a7828b10975bdb = new EventConnector() {  };
            EventLambda id_bdae853307a4497287860c7099a16b21 = new EventLambda() { Lambda = RefreshParameterRows };
            Box id_b2d9d1f95b6449cb8d75fdac26c40c34 = new Box() { Render = new Border() { Child = _parameterRowsPanel } };
            EventConnector id_f813d7e5bf0c4bdbb91d947c7f22ac18 = new EventConnector() {  };
            ContextMenu id_c2f72614beea4df18486904db0a72add = new ContextMenu() {  };
            MenuItem id_9d497129d253436baae86b8c278f7900 = new MenuItem(header: "Open source code...") {  };
            EventLambda id_c7b2e18c9f5f45d3bb47a3d65334ad59 = new EventLambda() { Lambda = () =>{Process.Start(Model.GetCodeFilePath());} };
            MenuItem id_0fb1c96671214dcdbf0f681c1b3f4589 = new MenuItem(header: "Through your default external editor") {  };
            MenuItem id_e5083acd796f40c2bce94c1cada87a19 = new MenuItem(header: "Through the GALADE text editor") {  };
            Data<string> id_43057edd46e94e7f898b078c6b2ef4f9 = new Data<string>() { Lambda = Model.GetCodeFilePath };
            ApplyAction<object> id_443e6e6f8b2444568ed3187c7675c9d2 = new ApplyAction<object>() { Lambda = input =>{if (StateTransition.CurrentStateMatches(Enums.DiagramMode.AwaitingPortSelection)){var wire = Graph.Get("SelectedWire") as ALAWire;if (wire == null) return;if (wire.Source == null){wire.Source = this;wire.SourcePort = input as Box;}else if (wire.Destination == null){wire.Destination = this;wire.DestinationPort = input as Box;}StateTransition.Update(Enums.DiagramMode.Idle);}} };
            DataFlowConnector<object> id_f1b15d71f8c04a02aaeb16b6cb2e4805 = new DataFlowConnector<object>() {  };
            KeyEvent id_32114a89a10d4c5185be428f80b4d6ee = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.LeftCtrl, Key.Q }, ExtractSender = source => (source as Box).Render };
            EventLambda id_1d81d33bf2f44255861ba1827d615d6f = new EventLambda() { Lambda = () =>{var sourcePort = GetSelectedPort();if (sourcePort == null) return;var source = this;var wire = new ALAWire(){Graph = Graph,Canvas = Canvas,Source = source,Destination = null,SourcePort = sourcePort,DestinationPort = null,StateTransition = StateTransition};Graph.AddEdge(wire);wire.Paint();wire.StartMoving(source: false);} };
            UIFactory id_0deeb22bb11f4c5daa4174935ea9be02 = new UIFactory(getUIContainer: () =>{foreach (var initialised in Model.GetInitialisedVariables()){CreateNodeParameterRow(initialised, Model.GetValue(initialised));}RefreshParameterRows();return new Text("");}) {  };
            ApplyAction<string> id_f45bef62245340b8b44e968086c1ab47 = new ApplyAction<string>() { Lambda = input =>{Model.Name = input;} };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            rootUI.WireTo(id_9d624a2b2b564e35bb5078a69edd3791, "uiLayout");
            rootUI.WireTo(id_18938ca5b8324a8c805f6a9190cb79f8, "eventHandlers");
            rootUI.WireTo(id_11f5e83640d9433788585fd1c0cfba8e, "eventHandlers");
            rootUI.WireTo(id_688acd495c4942fbb796183fc7b0f454, "eventHandlers");
            rootUI.WireTo(id_9042cfdb1f50407dae652da1e4e072f2, "eventHandlers");
            rootUI.WireTo(id_4092a8fcc0e5437faab2de15c29e540c, "eventHandlers");
            rootUI.WireTo(id_569fbefece75444080a2e577ef67329a, "eventHandlers");
            rootUI.WireTo(id_9128ab8d47e84d77a9ac2846a2ef160f, "eventHandlers");
            rootUI.WireTo(id_32114a89a10d4c5185be428f80b4d6ee, "eventHandlers");
            rootUI.WireTo(id_c2f72614beea4df18486904db0a72add, "contextMenu");
            id_9d624a2b2b564e35bb5078a69edd3791.WireTo(id_cc0274fb92154581b473209f4d9f6c06, "children");
            id_9d624a2b2b564e35bb5078a69edd3791.WireTo(nodeMiddle, "children");
            id_9d624a2b2b564e35bb5078a69edd3791.WireTo(id_67b48c399ec640bba6aa409fd5cf6bd2, "children");
            nodeMiddle.WireTo(nodeIdRow, "children");
            nodeMiddle.WireTo(parameterRowVert, "children");
            nodeMiddle.WireTo(addNewParameterRow, "children");
            nodeIdRow.WireTo(nodeTypeDropDownMenu, "children");
            nodeIdRow.WireTo(nodeNameTextBox, "children");
            nodeTypeDropDownMenu.WireTo(id_5e806d54c31440219227bd3aeb5cd71e, "selectedItem");
            nodeNameTextBox.WireTo(id_f45bef62245340b8b44e968086c1ab47, "textOutput");
            id_cc0274fb92154581b473209f4d9f6c06.WireTo(id_a12557620a094dd6ae74b4cb8fc2f011, "uiInstanceOutput");
            id_a12557620a094dd6ae74b4cb8fc2f011.WireTo(refreshInputPorts, "eventOutput");
            refreshInputPorts.WireTo(id_6c9b8deaa3584178bd497347615ce613, "dataOutput");
            id_2f4940b68752432eb5f18aa91f276cb0.WireTo(id_c27a41137e3f45c78b67144163627dd4, "output");
            id_c27a41137e3f45c78b67144163627dd4.WireTo(setUpPortBox, "elementOutput");
            id_c27a41137e3f45c78b67144163627dd4.WireTo(id_f813d7e5bf0c4bdbb91d947c7f22ac18, "complete");
            setUpPortBox.WireTo(id_7c99b6987fce48f99351f3a0d6045075, "output");
            id_67b48c399ec640bba6aa409fd5cf6bd2.WireTo(id_5325cc4494e7453db39ee8c550d2d314, "uiInstanceOutput");
            id_7c99b6987fce48f99351f3a0d6045075.WireTo(addUIEventsToPort, "fanoutList");
            id_7c99b6987fce48f99351f3a0d6045075.WireTo(id_782ed13f4131486e88c0361868cf9b4c, "fanoutList");
            id_50392509d3fd4fbc94e3353d3d3447c4.WireTo(id_20d36e8ff00045c6896f2200b9068e0f, "output");
            id_20d36e8ff00045c6896f2200b9068e0f.WireTo(id_f1b15d71f8c04a02aaeb16b6cb2e4805, "wire");
            id_b339d290f2bd4db59f4566689a7e2bbf.WireTo(id_8d5c4dddd7c34a74a4a4f11ef3dd8db9, "output");
            id_8d5c4dddd7c34a74a4a4f11ef3dd8db9.WireTo(id_f260c09b8c88430ba5832afcb7520244, "wire");
            addUIEventsToPort.WireTo(id_50392509d3fd4fbc94e3353d3d3447c4, "fanoutList");
            addUIEventsToPort.WireTo(id_b339d290f2bd4db59f4566689a7e2bbf, "fanoutList");
            addUIEventsToPort.WireTo(id_cce4c32ddb5746ab99f7f101ba85097e, "fanoutList");
            addUIEventsToPort.WireTo(id_3154b8324ea34b8bba3052a914f4b611, "fanoutList");
            addUIEventsToPort.WireTo(id_ad95b0114e4d4fc8bf4da30476209a97, "fanoutList");
            id_cce4c32ddb5746ab99f7f101ba85097e.WireTo(id_1879c0e488534615a8e571e5e40eb661, "output");
            id_1879c0e488534615a8e571e5e40eb661.WireTo(id_ffaa712b9d26422da8ac6840f228bee2, "wire");
            id_3154b8324ea34b8bba3052a914f4b611.WireTo(id_a1508569b00f4856aaa35390d746201c, "output");
            id_a1508569b00f4856aaa35390d746201c.WireTo(id_0568cb2edbb84e02a199e5df5f6c3a1e, "wire");
            id_ad95b0114e4d4fc8bf4da30476209a97.WireTo(id_5e3fbeb0c15b481a88747035fd14b049, "output");
            id_5e3fbeb0c15b481a88747035fd14b049.WireTo(id_53ae33610d0d4396bf8408a0391b54ec, "wire");
            id_18938ca5b8324a8c805f6a9190cb79f8.WireTo(id_7038e9301b8f42d29abc03a4152ed88b, "sourceOutput");
            id_11f5e83640d9433788585fd1c0cfba8e.WireTo(id_832713c5c1c24ca8ae20ddd48360b420, "sourceOutput");
            id_688acd495c4942fbb796183fc7b0f454.WireTo(id_438fd1479ba343a985a38be1c6267f07, "sourceOutput");
            id_9042cfdb1f50407dae652da1e4e072f2.WireTo(id_eb3156915d2047efa7706c86fc38e2d5, "sourceOutput");
            id_b21b566b17fe4343b6e96cc1b19afdce.WireTo(id_ab5f3dd7a4344afcb1d05c21a522629c, "fanoutList");
            id_b21b566b17fe4343b6e96cc1b19afdce.WireTo(id_1f0d01bcdf30470791a50d75cd69a0e5, "fanoutList");
            id_b21b566b17fe4343b6e96cc1b19afdce.WireTo(id_fdc35b60fa9a46478b75020e5c244f90, "fanoutList");
            id_b21b566b17fe4343b6e96cc1b19afdce.WireTo(id_147093c880c04b888952afa977dca339, "fanoutList");
            id_4092a8fcc0e5437faab2de15c29e540c.WireTo(id_e2cb57aab2d54ff884d5ef64476e335d, "sourceOutput");
            id_569fbefece75444080a2e577ef67329a.WireTo(id_b21b566b17fe4343b6e96cc1b19afdce, "sourceOutput");
            id_9128ab8d47e84d77a9ac2846a2ef160f.WireTo(id_96f2445e2d824dbf9f7d7a5ba99ac80d, "sourceOutput");
            id_6c9b8deaa3584178bd497347615ce613.WireTo(id_2f4940b68752432eb5f18aa91f276cb0, "output");
            id_5325cc4494e7453db39ee8c550d2d314.WireTo(refreshOutputPorts, "eventOutput");
            refreshOutputPorts.WireTo(id_6c9b8deaa3584178bd497347615ce613, "dataOutput");
            addNewParameterRow.WireTo(id_0deeb22bb11f4c5daa4174935ea9be02, "children");
            addNewParameterRow.WireTo(id_a571df327c994486b385bed708bcb9d5, "children");
            addNewParameterRow.WireTo(id_0f97f97574ab46aaba7046044b5cbddc, "children");
            id_a571df327c994486b385bed708bcb9d5.WireTo(id_c8fc18724a1f4fc0a5a7828b10975bdb, "eventButtonClicked");
            parameterRowVert.WireTo(id_b2d9d1f95b6449cb8d75fdac26c40c34, "children");
            id_c8fc18724a1f4fc0a5a7828b10975bdb.WireTo(id_f6243c4b498449748dbdffed2de78018, "fanoutList");
            id_c8fc18724a1f4fc0a5a7828b10975bdb.WireTo(id_bdae853307a4497287860c7099a16b21, "complete");
            id_f813d7e5bf0c4bdbb91d947c7f22ac18.WireTo(setNodeToolTip, "fanoutList");
            id_c2f72614beea4df18486904db0a72add.WireTo(id_9d497129d253436baae86b8c278f7900, "children");
            id_9d497129d253436baae86b8c278f7900.WireTo(id_0fb1c96671214dcdbf0f681c1b3f4589, "children");
            id_9d497129d253436baae86b8c278f7900.WireTo(id_e5083acd796f40c2bce94c1cada87a19, "children");
            id_0fb1c96671214dcdbf0f681c1b3f4589.WireTo(id_c7b2e18c9f5f45d3bb47a3d65334ad59, "clickedEvent");
            id_e5083acd796f40c2bce94c1cada87a19.WireTo(id_43057edd46e94e7f898b078c6b2ef4f9, "clickedEvent");
            id_f1b15d71f8c04a02aaeb16b6cb2e4805.WireTo(id_86b463a992174079afa241c1aa42c05f, "fanoutList");
            id_f1b15d71f8c04a02aaeb16b6cb2e4805.WireTo(id_443e6e6f8b2444568ed3187c7675c9d2, "fanoutList");
            id_32114a89a10d4c5185be428f80b4d6ee.WireTo(id_1d81d33bf2f44255861ba1827d615d6f, "eventHappened");
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












































































































































































































































































































































































































































































































































































