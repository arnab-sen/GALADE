using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        public Brush NodeBackground { get; set; } = Utilities.BrushFromHex("#d2ecf9");
        public Brush NodeBorder { get; set; } = Brushes.Black;
        public Brush NodeHighlightedBackground { get; set; } = Utilities.BrushFromHex("#c6fce5");
        public Brush NodeHighlightedBorder { get; set; } = Brushes.Black;
        public Brush PortBackground { get; set; } = Utilities.BrushFromHex("#f4f4f2");
        public Brush PortBorder { get; set; } = Brushes.Black;
        public Brush PortHighlightedBackground { get; set; } = Utilities.BrushFromHex("#fcdada");
        public Brush PortHighlightedBorder { get; set; } = Utilities.BrushFromHex("#f05454");

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

        public List<string> GenericTypeOptions
        {
            get => _genericTypeOptions;
            set
            {
                _genericTypeOptions.Clear();
                _genericTypeOptions.AddRange(value);
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
        private List<string> _genericTypeOptions = new List<string>();
        private List<Tuple<Horizontal, DropDownMenu, TextBox, Button>> _nodeParameterRows = new List<Tuple<Horizontal, DropDownMenu, TextBox, Button>>();
        private Canvas _nodeMask = new Canvas();
        private Border _detailedRender = new Border();
        private UIElement _textMaskRender;
        private Text _textMask;
        private List<DropDownMenu> _genericDropDowns = new List<DropDownMenu>();

        // Global instances
        private Vertical _inputPortsVert;
        private Vertical _outputPortsVert;
        public StackPanel _parameterRowsPanel = new StackPanel();
        private Data<object> _refreshInputPorts;
        private Data<object> _refreshOutputPorts;
        private Horizontal _nodeIdRow;

        // Ports

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
            foreach (var outputPortBox in _outputPortBoxes)
            {
                if (outputPortBox.Payload is Port port && port.Name == name) return outputPortBox;
            }

            foreach (var inputPortBox in _inputPortBoxes)
            {
                if (inputPortBox.Payload is Port port && port.Name == name) return inputPortBox;
            }

            // Return a default port if no matches are found
            if (_outputPortBoxes.Any()) return _outputPortBoxes.First();
            if (_inputPortBoxes.Any()) return _inputPortBoxes.First();

            // Return null if the abstraction has no ports at all
            return null;
        }

        public void UpdateUI()
        {
            Render.Dispatcher.Invoke(() =>
            {
                UpdateNodeParameters();
                (_refreshInputPorts as IEvent).Execute();
                (_refreshOutputPorts as IEvent).Execute();
                (_nodeIdRow as IUI).GetWPFElement();
                Model.RefreshGenericsInPorts();
            }, DispatcherPriority.Loaded);
            
            Render.Dispatcher.Invoke(() =>
            {
                _textMaskRender = CreateTextMask(Model.FullType);
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
                Type = "UNKNOWN",
                Name = ""
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

            GenericTypeOptions = new List<string>()
            {
                "int",
                "string",
                "object"
            };

            Model.RefreshFullTypeWithGenerics();
            Model.RefreshGenericsInPorts();

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

        private void CreateNodeParameterRow() => CreateNodeParameterRow("", "");

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

            maskContainer.MouseLeftButtonDown += (sender, args) => ShowTypeTextMask(false);
            

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
                if (_textMaskRender == null) _textMaskRender = CreateTextMask(Model.FullType);

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

        public string ToInstantiation(bool singleLine = true)
        {
            var instantiation = "";

            if (singleLine)
            {
                instantiation = ToFlatInstantiation();
            }
            else
            {
                instantiation = ToFormattedInstantiation();
            }

            return instantiation;
        }

        private string ToFlatInstantiation()
        {
            var instantiation = "";

            var initialised = Model.GetInitialisedVariables();
            var constructorArgs = Model.GetConstructorArgs()
                .Where(kvp => initialised.Contains(kvp.Key))
                .ToList();

            var propertiesAndFields = Model.GetProperties()
                .Where(kvp => initialised.Contains(kvp.Key))
                .ToList();

            propertiesAndFields.AddRange(Model.GetFields()
                .Where(kvp => initialised.Contains(kvp.Key))
                .ToList());

            var sb = new StringBuilder();

            sb.Append("var ");
            sb.Append(Name);
            sb.Append(" = new ");
            sb.Append(Model.FullType);
            sb.Append("(");
            sb.Append(Flatten(GetConstructorArgumentSyntaxList(constructorArgs).ToString()));
            sb.Append(") {");
            sb.Append(Flatten(GetPropertySyntaxList(propertiesAndFields).ToString()));
            sb.Append("};");

            instantiation = sb.ToString();

            return instantiation;
        }

        private string Flatten(string input)
        {
            var flattenedString = Regex.Replace(input, @"(?<=([^\\]))[\t\n\r]", "");

            return flattenedString;
        }

        private string ToFormattedInstantiation()
        {
            // Note: must declare "using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;" in this file's usings
            // to use SyntaxFactory methods without repeatedly referencing SyntaxFactory

            // The SyntaxNode structure is generated from https://roslynquoter.azurewebsites.net/


            var initialised = Model.GetInitialisedVariables();
            var constructorArgs = Model.GetConstructorArgs()
                .Where(kvp => initialised.Contains(kvp.Key))
                .ToList();

            var propertiesAndFields = Model.GetProperties()
                .Where(kvp => initialised.Contains(kvp.Key))
                .ToList();

            propertiesAndFields.AddRange(Model.GetFields()
                .Where(kvp => initialised.Contains(kvp.Key))
                .ToList());


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
                                                        GetPropertySyntaxList(propertiesAndFields))))))));

            var instantiation = syntaxNode.NormalizeWhitespace().ToString();

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

        private IUI CreateTypeGenericsDropDownMenus()
        {
            var horiz = new Horizontal() { };
            // var openAngleBracket = new Text("<");
            // var closedAngleBracket = new Text(">");

            var generics = Model.GetGenerics();

            if (!generics.Any()) horiz.Visibility = Visibility.Collapsed;

            // horiz.WireTo(openAngleBracket, "children");

            for (int i = 0; i < generics.Count; i++)
            {
                var dropDown = new DropDownMenu() 
                {
                    Text = generics[i],
                    Items = GenericTypeOptions,
                    Width = 20,
                    Height = 25
                };

                var genericIndex = i;
                var updateGeneric = new ApplyAction<string>()
                {
                    Lambda = newType =>
                    {
                        Model.UpdateGeneric(genericIndex, newType);
                        _textMaskRender = CreateTextMask(Model.FullType);
                    }

                };

                dropDown.WireTo(updateGeneric, "selectedItem");

                horiz.WireTo(dropDown, "children");
            }

            // horiz.WireTo(closedAngleBracket, "children");

            return horiz;
        }

        private void CreateWiring()
        {
            Vertical inputPortsVert = null;
            Vertical outputPortsVert = null;

            // BEGIN AUTO-GENERATED INSTANTIATIONS
            Box rootUI = new Box() { InstanceName = "rootUI", Background = NodeBackground };
            Horizontal id_a38c965bdcac4123bb22c40a31b04de5 = new Horizontal() {  };
            Vertical nodeMiddle = new Vertical() { InstanceName = "nodeMiddle", Margin = new Thickness(1, 0, 1, 0) };
            Horizontal nodeIdRow = new Horizontal() { InstanceName = "nodeIdRow" };
            DropDownMenu nodeTypeDropDownMenu = new DropDownMenu() { InstanceName = "nodeTypeDropDownMenu", Text = Model.Type, Width = 100, Items = AvailableDomainAbstractions };
            TextBox nodeNameTextBox = new TextBox() { InstanceName = "nodeNameTextBox", Text = ShowName ? Model.Name : "", Width = 50 };
            UIFactory id_22a63aa944cd4b7882c16b4c8ea68077 = new UIFactory() { GetUIContainer = () =>{_inputPortsVert = new Vertical();_inputPortsVert.Margin = new Thickness(0);return _inputPortsVert;} };
            ConvertToEvent<object> id_327f3be8d37e46bf89784f60bb8f1f1e = new ConvertToEvent<object>() {  };
            Data<object> refreshInputPorts = new Data<object>() { InstanceName = "refreshInputPorts", Lambda = GetImplementedPorts };
            Cast<object, IEnumerable<Port>> id_756969500c42465da547dd7fd06a78ee = new Cast<object, IEnumerable<Port>>() {  };
            ForEach<Port> id_63d1bc138f4d491a8281a680b80ca3e0 = new ForEach<Port>() {  };
            Apply<Port, object> setUpPortBox = new Apply<Port, object>() { InstanceName = "setUpPortBox", Lambda = port =>{var box = new Box();box.Payload = port;box.Width = 50;box.Height = 15;box.Background = PortBackground;box.BorderThickness = new Thickness(2);var toolTipLabel = new System.Windows.Controls.Label() { Content = port.ToString()};box.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = port.ToString();var text = new Text(text: port.Name);text.HorizAlignment = HorizontalAlignment.Center;box.Render.Child = (text as IUI).GetWPFElement();if (port.IsInputPort){_inputPortsVert.WireTo(box, "children");_inputPortBoxes.Add(box);(_inputPortsVert as IUI).GetWPFElement(); /* Refresh UI */}else{_outputPortsVert.WireTo(box, "children");_outputPortBoxes.Add(box);(_outputPortsVert as IUI).GetWPFElement(); /* Refresh UI */}return box;} };
            UIFactory id_199dcd0da33348dd8dc10002fce177c0 = new UIFactory() { GetUIContainer = () =>{_outputPortsVert = new Vertical();_outputPortsVert.Margin = new Thickness(0);return _outputPortsVert;} };
            ApplyAction<object> id_e5611f2c4f14413f9f73c0e692e8d39f = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = PortHighlightedBorder;} };
            DataFlowConnector<object> id_59a97651e6a54834a7650e0a354dc8cc = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_c1542864d3f0488c8645203b260d474e = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = PortBorder;} };
            Apply<object, object> id_841ad21363904582ab3b2d4ed5454cd1 = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseEnter");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_8b7bff37bb1a4e5198ce5763967e2993 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            Apply<object, object> id_03a9f1b63cc2493eba98449ccd2dfa3a = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseLeave");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_758754b778d34af5b0d39466a551e136 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            DataFlowConnector<object> addUIEventsToPort = new DataFlowConnector<object>() { InstanceName = "addUIEventsToPort" };
            Apply<object, object> id_691425dd523f464f9ff2a53db1b2c72c = new Apply<object, object>() { Lambda = input =>{var mouseButtonEvent = new MouseButtonEvent("MouseDown");mouseButtonEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseButtonEvent, "eventHandlers");return mouseButtonEvent;} };
            DynamicWiring<IDataFlow<object>> id_6c7dda91cd684c2293fd93077c919425 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_40274a2315f646b8886ca674017c4d7d = new ApplyAction<object>() { Lambda = input =>{_selectedPort = input as Box;_selectedPort.Render.Focus();} };
            Apply<object, object> id_6a3e8fdccbfc436594f8d66cb202d166 = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("GotFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_379abf1f9092450896893ae3d5f120c9 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_aaeab10a538a49468e3d199085b8b8cd = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = PortHighlightedBackground;box.BorderColour = PortHighlightedBorder;} };
            Apply<object, object> id_7c363f29010e47cca030ed4643536616 = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("LostFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_1c133e2faf6e40c3b02fe2ad494c04fe = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_e36f33e73ed64801b150ba0c25d80581 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = PortBackground;box.BorderColour = PortBorder;} };
            MouseEvent id_1dbe8cef1e214cc1b0cde8a00897e729 = new MouseEvent(eventName: "MouseEnter") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_465bf346eed6406bb81b4c82193f6b00 = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = NodeHighlightedBackground;} };
            MouseEvent id_ca520a6781364a8eb48cee0d249bf806 = new MouseEvent(eventName: "MouseLeave") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_91a43fec880845b391df43e19d2aa9e2 = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) (input as Box).Background = NodeBackground;} };
            RoutedEventSubscriber id_ab5fee6e2f4040e1a99c884709db74ed = new RoutedEventSubscriber(eventName: "GotFocus") { ExtractSender = source => (source as Box).Render };
            RoutedEventSubscriber id_e1abcd8cc5634cb588006e4c60216502 = new RoutedEventSubscriber(eventName: "LostFocus") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_3720a6ea8a404aa592d4e026bb499360 = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = NodeBackground;} };
            ApplyAction<object> id_51ac7dee54614590b81a4a23aa387c12 = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) ui.Focus();} };
            ApplyAction<object> id_f576b4a6c3ff4a17a7c4382c71c9a901 = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = NodeHighlightedBackground;} };
            DataFlowConnector<object> id_a16789c9b2c9445ea2808b2a807bdcb7 = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_4aaa973f87384938a4caf28c40619b66 = new ApplyAction<object>() { Lambda = input =>{StateTransition.Update(Enums.DiagramMode.IdleSelected);} };
            MouseEvent id_db25438e2c014e6496d04c96c0ae75c7 = new MouseEvent(eventName: "MouseMove") { ExtractSender = source => (source as Box).Render, Condition = args => Mouse.LeftButton == MouseButtonState.Pressed && Keyboard.IsKeyDown(Key.LeftShift) };
            ApplyAction<object> id_4a560f64a713466387e68dd26e31e861 = new ApplyAction<object>() { Lambda = input =>{var mousePos = Mouse.GetPosition(Canvas);var oldPosition = new Point(PositionX, PositionY);PositionX = mousePos.X - _mousePosInBox.X;PositionY = mousePos.Y - _mousePosInBox.Y;PositionChanged?.Invoke();} };
            ApplyAction<object> id_32864ae3f39d4f319df3da2d17b97392 = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;_mousePosInBox = Mouse.GetPosition(render);Mouse.Capture(render);} };
            ApplyAction<object> id_13a07daf5fcd4f188be8c157356f43fa = new ApplyAction<object>() { Lambda = input =>{Graph.Set("SelectedNode", this);} };
            MouseButtonEvent id_fb7e7304fbd044328ecc01895bfba0db = new MouseButtonEvent(eventName: "MouseLeftButtonDown") { ExtractSender = source => (source as Box).Render };
            ApplyAction<string> id_dfa668ea3d0f4122a794598bb695eaf4 = new ApplyAction<string>() { Lambda = input =>{TypeChanged?.Invoke(input);} };
            MouseButtonEvent id_72f950fdc6a340639336060231475dfd = new MouseButtonEvent(eventName: "MouseLeftButtonUp") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_7df867a3dd6c4a46b6e6c2e3eeca1f12 = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;if (Mouse.Captured?.Equals(render) ?? false) Mouse.Capture(null);} };
            ApplyAction<object> id_c80ede2031994301b51c8056f90fb6c4 = new ApplyAction<object>() { Lambda = input =>{(input as Box).InitialiseUI();} };
            EventLambda setNodeToolTip = new EventLambda() { InstanceName = "setNodeToolTip", Lambda = () => {var toolTipLabel = new System.Windows.Controls.Label() { Content = Model.GetDocumentation() };rootUI.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };rootUI.Render.MouseEnter += (sender, args) => toolTipLabel.Content = Model.GetDocumentation();} };
            Apply<object, object> id_ef3a05d8c9814de4ada61368ab691605 = new Apply<object, object>() { Lambda = input =>{var notUpdated = UpdatePorts(input as IEnumerable<Port>);return notUpdated;} };
            ConvertToEvent<object> id_6cb20d97a21642aebfa224e7de3b05e5 = new ConvertToEvent<object>() {  };
            Data<object> refreshOutputPorts = new Data<object>() { InstanceName = "refreshOutputPorts", Lambda = GetAcceptedPorts };
            Horizontal addNewParameterRow = new Horizontal() { InstanceName = "addNewParameterRow", Ratios = new[] { 40, 20, 40 } };
            Text id_111e1cbaff304d0f93a7e96f60ede46c = new Text(text: "") {  };
            Button id_7055f0caf8dc4207a0c0f85669cdb4c2 = new Button(title: "+") { Width = 20, Margin = new Thickness(5) };
            Vertical parameterRowVert = new Vertical() { InstanceName = "parameterRowVert", Margin = new Thickness(5, 5, 5, 0) };
            EventLambda id_fce490180cc147dc8ce35cac6c22a34a = new EventLambda() { Lambda = CreateNodeParameterRow };
            EventConnector id_9be3d4da2bde4a50804489d5676c56a2 = new EventConnector() {  };
            EventLambda id_613e5ca8803c45f594bb2a933b5e7340 = new EventLambda() { Lambda = RefreshParameterRows };
            Box id_ca1ff4a6d86047418a910be655abc0fb = new Box() { Render = new Border() { Child = _parameterRowsPanel } };
            EventConnector id_272486d4193547cea85ef4b41d6bf84b = new EventConnector() {  };
            ContextMenu id_71829b744cf145a0a934a55f7768c7bf = new ContextMenu() {  };
            MenuItem id_403baaf79a824981af02ae135627767f = new MenuItem(header: "Open source code...") {  };
            EventLambda id_872f85f0291843daad50fcaf77f4e9c2 = new EventLambda() { Lambda = () =>{Process.Start(Model.GetCodeFilePath());} };
            MenuItem id_9c912c6b764641d592796b0d3753424b = new MenuItem(header: "Through your default external editor") {  };
            MenuItem id_736f961d1ae84d80bfa4570d41685828 = new MenuItem(header: "Through the GALADE text editor") {  };
            Data<string> id_a09b14f6afef41e59fac3fd10dd6ce00 = new Data<string>() { Lambda = Model.GetCodeFilePath };
            ApplyAction<object> id_b87873908d1244b5aaa8855fc0fb1b02 = new ApplyAction<object>() { Lambda = input =>{if (StateTransition.CurrentStateMatches(Enums.DiagramMode.AwaitingPortSelection)){var wire = Graph.Get("SelectedWire") as ALAWire;if (wire == null) return;if (wire.Source == null){wire.Source = this;wire.SourcePort = input as Box;}else if (wire.Destination == null){wire.Destination = this;wire.DestinationPort = input as Box;}StateTransition.Update(Enums.DiagramMode.Idle);}} };
            DataFlowConnector<object> id_28a8ae0f60734455947824da29ee692c = new DataFlowConnector<object>() {  };
            KeyEvent id_ee05f28d5f3a4c129f5ab1596cf1a8f1 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.LeftCtrl, Key.Q }, ExtractSender = source => (source as Box).Render };
            EventLambda id_4bf02b5d6c1f425899f61c1f3e1a9c95 = new EventLambda() { Lambda = () =>{var sourcePort = GetSelectedPort();if (sourcePort == null) return;var source = this;var wire = new ALAWire(){Graph = Graph,Canvas = Canvas,Source = source,Destination = null,SourcePort = sourcePort,DestinationPort = null,StateTransition = StateTransition};Graph.AddEdge(wire);wire.Paint();wire.StartMoving(source: false);} };
            UIFactory id_bde96774a43b4aacabef3519e105ace3 = new UIFactory() { GetUIContainer = () =>{foreach (var initialised in Model.GetInitialisedVariables()){CreateNodeParameterRow(initialised, Model.GetValue(initialised));}RefreshParameterRows();return new Text("");} };
            ApplyAction<string> id_059c1cbf85554c8387b3a1de3295bc29 = new ApplyAction<string>() { Lambda = input =>{Model.Name = input;} };
            UIFactory id_75916be4ed4146c38879cd55cd1da9f0 = new UIFactory() { GetUIContainer = CreateTypeGenericsDropDownMenus };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            rootUI.WireTo(id_a38c965bdcac4123bb22c40a31b04de5, "uiLayout");
            rootUI.WireTo(id_1dbe8cef1e214cc1b0cde8a00897e729, "eventHandlers");
            rootUI.WireTo(id_ca520a6781364a8eb48cee0d249bf806, "eventHandlers");
            rootUI.WireTo(id_ab5fee6e2f4040e1a99c884709db74ed, "eventHandlers");
            rootUI.WireTo(id_e1abcd8cc5634cb588006e4c60216502, "eventHandlers");
            rootUI.WireTo(id_db25438e2c014e6496d04c96c0ae75c7, "eventHandlers");
            rootUI.WireTo(id_fb7e7304fbd044328ecc01895bfba0db, "eventHandlers");
            rootUI.WireTo(id_72f950fdc6a340639336060231475dfd, "eventHandlers");
            rootUI.WireTo(id_ee05f28d5f3a4c129f5ab1596cf1a8f1, "eventHandlers");
            rootUI.WireTo(id_71829b744cf145a0a934a55f7768c7bf, "contextMenu");
            id_a38c965bdcac4123bb22c40a31b04de5.WireTo(id_22a63aa944cd4b7882c16b4c8ea68077, "children");
            id_a38c965bdcac4123bb22c40a31b04de5.WireTo(nodeMiddle, "children");
            id_a38c965bdcac4123bb22c40a31b04de5.WireTo(id_199dcd0da33348dd8dc10002fce177c0, "children");
            nodeMiddle.WireTo(nodeIdRow, "children");
            nodeMiddle.WireTo(parameterRowVert, "children");
            nodeMiddle.WireTo(addNewParameterRow, "children");
            nodeIdRow.WireTo(nodeTypeDropDownMenu, "children");
            nodeIdRow.WireTo(id_75916be4ed4146c38879cd55cd1da9f0, "children");
            nodeIdRow.WireTo(nodeNameTextBox, "children");
            nodeTypeDropDownMenu.WireTo(id_dfa668ea3d0f4122a794598bb695eaf4, "selectedItem");
            nodeNameTextBox.WireTo(id_059c1cbf85554c8387b3a1de3295bc29, "textOutput");
            id_22a63aa944cd4b7882c16b4c8ea68077.WireTo(id_327f3be8d37e46bf89784f60bb8f1f1e, "uiInstanceOutput");
            id_327f3be8d37e46bf89784f60bb8f1f1e.WireTo(refreshInputPorts, "eventOutput");
            refreshInputPorts.WireTo(id_ef3a05d8c9814de4ada61368ab691605, "dataOutput");
            id_756969500c42465da547dd7fd06a78ee.WireTo(id_63d1bc138f4d491a8281a680b80ca3e0, "output");
            id_63d1bc138f4d491a8281a680b80ca3e0.WireTo(setUpPortBox, "elementOutput");
            id_63d1bc138f4d491a8281a680b80ca3e0.WireTo(id_272486d4193547cea85ef4b41d6bf84b, "complete");
            setUpPortBox.WireTo(id_59a97651e6a54834a7650e0a354dc8cc, "output");
            id_199dcd0da33348dd8dc10002fce177c0.WireTo(id_6cb20d97a21642aebfa224e7de3b05e5, "uiInstanceOutput");
            id_59a97651e6a54834a7650e0a354dc8cc.WireTo(addUIEventsToPort, "fanoutList");
            id_59a97651e6a54834a7650e0a354dc8cc.WireTo(id_c80ede2031994301b51c8056f90fb6c4, "fanoutList");
            id_841ad21363904582ab3b2d4ed5454cd1.WireTo(id_8b7bff37bb1a4e5198ce5763967e2993, "output");
            id_8b7bff37bb1a4e5198ce5763967e2993.WireTo(id_28a8ae0f60734455947824da29ee692c, "wire");
            id_03a9f1b63cc2493eba98449ccd2dfa3a.WireTo(id_758754b778d34af5b0d39466a551e136, "output");
            id_758754b778d34af5b0d39466a551e136.WireTo(id_c1542864d3f0488c8645203b260d474e, "wire");
            addUIEventsToPort.WireTo(id_841ad21363904582ab3b2d4ed5454cd1, "fanoutList");
            addUIEventsToPort.WireTo(id_03a9f1b63cc2493eba98449ccd2dfa3a, "fanoutList");
            addUIEventsToPort.WireTo(id_691425dd523f464f9ff2a53db1b2c72c, "fanoutList");
            addUIEventsToPort.WireTo(id_6a3e8fdccbfc436594f8d66cb202d166, "fanoutList");
            addUIEventsToPort.WireTo(id_7c363f29010e47cca030ed4643536616, "fanoutList");
            id_691425dd523f464f9ff2a53db1b2c72c.WireTo(id_6c7dda91cd684c2293fd93077c919425, "output");
            id_6c7dda91cd684c2293fd93077c919425.WireTo(id_40274a2315f646b8886ca674017c4d7d, "wire");
            id_6a3e8fdccbfc436594f8d66cb202d166.WireTo(id_379abf1f9092450896893ae3d5f120c9, "output");
            id_379abf1f9092450896893ae3d5f120c9.WireTo(id_aaeab10a538a49468e3d199085b8b8cd, "wire");
            id_7c363f29010e47cca030ed4643536616.WireTo(id_1c133e2faf6e40c3b02fe2ad494c04fe, "output");
            id_1c133e2faf6e40c3b02fe2ad494c04fe.WireTo(id_e36f33e73ed64801b150ba0c25d80581, "wire");
            id_1dbe8cef1e214cc1b0cde8a00897e729.WireTo(id_465bf346eed6406bb81b4c82193f6b00, "sourceOutput");
            id_ca520a6781364a8eb48cee0d249bf806.WireTo(id_91a43fec880845b391df43e19d2aa9e2, "sourceOutput");
            id_ab5fee6e2f4040e1a99c884709db74ed.WireTo(id_f576b4a6c3ff4a17a7c4382c71c9a901, "sourceOutput");
            id_e1abcd8cc5634cb588006e4c60216502.WireTo(id_3720a6ea8a404aa592d4e026bb499360, "sourceOutput");
            id_a16789c9b2c9445ea2808b2a807bdcb7.WireTo(id_51ac7dee54614590b81a4a23aa387c12, "fanoutList");
            id_a16789c9b2c9445ea2808b2a807bdcb7.WireTo(id_4aaa973f87384938a4caf28c40619b66, "fanoutList");
            id_a16789c9b2c9445ea2808b2a807bdcb7.WireTo(id_32864ae3f39d4f319df3da2d17b97392, "fanoutList");
            id_a16789c9b2c9445ea2808b2a807bdcb7.WireTo(id_13a07daf5fcd4f188be8c157356f43fa, "fanoutList");
            id_db25438e2c014e6496d04c96c0ae75c7.WireTo(id_4a560f64a713466387e68dd26e31e861, "sourceOutput");
            id_fb7e7304fbd044328ecc01895bfba0db.WireTo(id_a16789c9b2c9445ea2808b2a807bdcb7, "sourceOutput");
            id_72f950fdc6a340639336060231475dfd.WireTo(id_7df867a3dd6c4a46b6e6c2e3eeca1f12, "sourceOutput");
            id_ef3a05d8c9814de4ada61368ab691605.WireTo(id_756969500c42465da547dd7fd06a78ee, "output");
            id_6cb20d97a21642aebfa224e7de3b05e5.WireTo(refreshOutputPorts, "eventOutput");
            refreshOutputPorts.WireTo(id_ef3a05d8c9814de4ada61368ab691605, "dataOutput");
            addNewParameterRow.WireTo(id_bde96774a43b4aacabef3519e105ace3, "children");
            addNewParameterRow.WireTo(id_7055f0caf8dc4207a0c0f85669cdb4c2, "children");
            addNewParameterRow.WireTo(id_111e1cbaff304d0f93a7e96f60ede46c, "children");
            id_7055f0caf8dc4207a0c0f85669cdb4c2.WireTo(id_9be3d4da2bde4a50804489d5676c56a2, "eventButtonClicked");
            parameterRowVert.WireTo(id_ca1ff4a6d86047418a910be655abc0fb, "children");
            id_9be3d4da2bde4a50804489d5676c56a2.WireTo(id_fce490180cc147dc8ce35cac6c22a34a, "fanoutList");
            id_9be3d4da2bde4a50804489d5676c56a2.WireTo(id_613e5ca8803c45f594bb2a933b5e7340, "complete");
            id_272486d4193547cea85ef4b41d6bf84b.WireTo(setNodeToolTip, "fanoutList");
            id_71829b744cf145a0a934a55f7768c7bf.WireTo(id_403baaf79a824981af02ae135627767f, "children");
            id_403baaf79a824981af02ae135627767f.WireTo(id_9c912c6b764641d592796b0d3753424b, "children");
            id_403baaf79a824981af02ae135627767f.WireTo(id_736f961d1ae84d80bfa4570d41685828, "children");
            id_9c912c6b764641d592796b0d3753424b.WireTo(id_872f85f0291843daad50fcaf77f4e9c2, "clickedEvent");
            id_736f961d1ae84d80bfa4570d41685828.WireTo(id_a09b14f6afef41e59fac3fd10dd6ce00, "clickedEvent");
            id_28a8ae0f60734455947824da29ee692c.WireTo(id_e5611f2c4f14413f9f73c0e692e8d39f, "fanoutList");
            id_28a8ae0f60734455947824da29ee692c.WireTo(id_b87873908d1244b5aaa8855fc0fb1b02, "fanoutList");
            id_ee05f28d5f3a4c129f5ab1596cf1a8f1.WireTo(id_4bf02b5d6c1f425899f61c1f3e1a9c95, "eventHappened");
            // END AUTO-GENERATED WIRING

            Render = _nodeMask;
            _nodeMask.Children.Clear();
            _detailedRender.Child = (rootUI as IUI).GetWPFElement();
            _nodeMask.Children.Add(_detailedRender);

            // Instance mapping
            _rootUI = rootUI;
            _refreshInputPorts = refreshInputPorts;
            _refreshOutputPorts = refreshOutputPorts;
            _nodeIdRow = nodeIdRow;
        }

        public ALANode()
        {
            Id = Utilities.GetUniqueId();
        }
    }
}












































































































































































































































































































































































































































































































































































































