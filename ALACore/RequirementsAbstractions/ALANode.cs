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
        public Vertical parameterRowVert = new Vertical() { InstanceName = "parameterRowVert", Margin = new Thickness(5, 5, 5, 0) };
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
            // BEGIN AUTO-GENERATED INSTANTIATIONS
            Box rootUI = new Box() { InstanceName = "rootUI", Background = NodeBackground };
            Horizontal id_85e448fa744b45dd8f5929a71038bd96 = new Horizontal() {  };
            Vertical nodeMiddle = new Vertical() { InstanceName = "nodeMiddle", Margin = new Thickness(1, 0, 1, 0) };
            Horizontal nodeIdRow = new Horizontal() { InstanceName = "nodeIdRow" };
            DropDownMenu nodeTypeDropDownMenu = new DropDownMenu() { InstanceName = "nodeTypeDropDownMenu", Text = Model.Type, Width = 100, Items = AvailableDomainAbstractions };
            TextBox nodeNameTextBox = new TextBox() { InstanceName = "nodeNameTextBox", Text = ShowName ? Model.Name : "", Width = 50 };
            UIFactory id_300926a8ee77446785e15b73547248a7 = new UIFactory(getUIContainer: () =>{_inputPortsVert = new Vertical();_inputPortsVert.Margin = new Thickness(0);return _inputPortsVert;}) {  };
            ConvertToEvent<object> id_36b667d819e9490b921ce69ed4611c23 = new ConvertToEvent<object>() {  };
            Data<object> refreshInputPorts = new Data<object>() { InstanceName = "refreshInputPorts", Lambda = GetImplementedPorts };
            Cast<object, IEnumerable<Port>> id_fb1ccfe68ae04552a1c226e79c54e85a = new Cast<object, IEnumerable<Port>>() {  };
            ForEach<Port> id_412e5e2d33f04e328af832147c2461bd = new ForEach<Port>() {  };
            Apply<Port, object> setUpPortBox = new Apply<Port, object>() { InstanceName = "setUpPortBox", Lambda = port =>{var box = new Box();box.Payload = port;box.Width = 50;box.Height = 15;box.Background = PortBackground;box.BorderThickness = new Thickness(2);var toolTipLabel = new System.Windows.Controls.Label() { Content = port.ToString()};box.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = port.ToString();var text = new Text(text: port.Name);text.HorizAlignment = HorizontalAlignment.Center;box.Render.Child = (text as IUI).GetWPFElement();if (port.IsInputPort){_inputPortsVert.WireTo(box, "children");_inputPortBoxes.Add(box);(_inputPortsVert as IUI).GetWPFElement(); /* Refresh UI */}else{_outputPortsVert.WireTo(box, "children");_outputPortBoxes.Add(box);(_outputPortsVert as IUI).GetWPFElement(); /* Refresh UI */}return box;} };
            UIFactory id_668d2fe19b42434a83f39de5d26eb302 = new UIFactory(getUIContainer: () =>{_outputPortsVert = new Vertical();_outputPortsVert.Margin = new Thickness(0);return _outputPortsVert;}) {  };
            ApplyAction<object> id_5530df97a5ff4836b1e296a4573a7913 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = PortHighlightedBorder;} };
            DataFlowConnector<object> id_20b8b4d6b87f4ed38f7601fc0ce788a8 = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_8010048b12234e7c94785c630aaf47f8 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = PortBorder;} };
            Apply<object, object> id_f23af6d8b2cd48c19a1e499540530afa = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseEnter");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_524a83c7905e4080acbc4023d674a0eb = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            Apply<object, object> id_1c3c5bbe0f2647dfac9343006c32855a = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseLeave");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_928140f081da455fb136ec53765273ea = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            DataFlowConnector<object> addUIEventsToPort = new DataFlowConnector<object>() { InstanceName = "addUIEventsToPort" };
            Apply<object, object> id_23f5b7fadab346a1b2297a4fdc13f167 = new Apply<object, object>() { Lambda = input =>{var mouseButtonEvent = new MouseButtonEvent("MouseDown");mouseButtonEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseButtonEvent, "eventHandlers");return mouseButtonEvent;} };
            DynamicWiring<IDataFlow<object>> id_6513e498d86748c686f5a41901aef411 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_9264824b2d064f46b8f242ccbe8f6af1 = new ApplyAction<object>() { Lambda = input =>{_selectedPort = input as Box;_selectedPort.Render.Focus();} };
            Apply<object, object> id_891c4b2762ac4083be7c1bbd026dc8e4 = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("GotFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_dfdae7ba8c15442d8dc067bc3af1cd02 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_dc90339dd4784b88a0d610e14e2d07b6 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = PortHighlightedBackground;box.BorderColour = PortHighlightedBorder;} };
            Apply<object, object> id_c2ca7dd1d4e24118926f878b1c5948aa = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("LostFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_71bf2f733b7c4efbbb633c073ea5b88f = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_197a8be9284e4650a9bfb2dc13066c27 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = PortBackground;box.BorderColour = PortBorder;} };
            MouseEvent id_ad180d96a8ed45f382b813a5b9376157 = new MouseEvent(eventName: "MouseEnter") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_5d8934be3e3547c29d839b4f622da05c = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = NodeHighlightedBackground;} };
            MouseEvent id_834ccee2118c45029480940ba5c3fd44 = new MouseEvent(eventName: "MouseLeave") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_1a76d0c3e9d34356886f67813740db78 = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) (input as Box).Background = NodeBackground;} };
            RoutedEventSubscriber id_a050da9816504dadb848d773b2af61ee = new RoutedEventSubscriber(eventName: "GotFocus") { ExtractSender = source => (source as Box).Render };
            RoutedEventSubscriber id_b7695b11d04a4eebb3dc353c5e4d2e7c = new RoutedEventSubscriber(eventName: "LostFocus") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_39bba7b1f8844dea9a76375f90bc4dfb = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = NodeBackground;} };
            ApplyAction<object> id_bfc08d670f6348c2b34306c9394db3b1 = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) ui.Focus();} };
            ApplyAction<object> id_82b0f651f769401fab4fed153cf0d520 = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = NodeHighlightedBackground;} };
            DataFlowConnector<object> id_23a9b3f299ff470e92d7fca0be07aef9 = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_259a14bea3284f53b14d630c76defe79 = new ApplyAction<object>() { Lambda = input =>{StateTransition.Update(Enums.DiagramMode.IdleSelected);} };
            MouseEvent id_ec9b474b868e41d8b07a8f9e067ec1a1 = new MouseEvent(eventName: "MouseMove") { ExtractSender = source => (source as Box).Render, Condition = args => Mouse.LeftButton == MouseButtonState.Pressed && Keyboard.IsKeyDown(Key.LeftShift) };
            ApplyAction<object> id_261ebcc1bd3f4737aaed8f92ce14fc1e = new ApplyAction<object>() { Lambda = input =>{var mousePos = Mouse.GetPosition(Canvas);var oldPosition = new Point(PositionX, PositionY);PositionX = mousePos.X - _mousePosInBox.X;PositionY = mousePos.Y - _mousePosInBox.Y;PositionChanged?.Invoke();} };
            ApplyAction<object> id_8fb4e411123b4a65be162ab7955a496f = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;_mousePosInBox = Mouse.GetPosition(render);Mouse.Capture(render);} };
            ApplyAction<object> id_d793129cf72544e3b684e74e54bb5024 = new ApplyAction<object>() { Lambda = input =>{Graph.Set("SelectedNode", this);} };
            MouseButtonEvent id_b6e7bb630ac14eedaf71f9bf2d3ecb34 = new MouseButtonEvent(eventName: "MouseLeftButtonDown") { ExtractSender = source => (source as Box).Render };
            ApplyAction<string> id_5faf1af6aa0e444c9d2ab7a4b7f6c6c1 = new ApplyAction<string>() { Lambda = input =>{TypeChanged?.Invoke(input);} };
            MouseButtonEvent id_93cb0561751b4b53b340376715dde182 = new MouseButtonEvent(eventName: "MouseLeftButtonUp") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_93165a041fc243809b3415edc56b219a = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;if (Mouse.Captured?.Equals(render) ?? false) Mouse.Capture(null);} };
            ApplyAction<object> id_948079e584fb419da207a60b9f957e74 = new ApplyAction<object>() { Lambda = input =>{(input as Box).InitialiseUI();} };
            EventLambda setNodeToolTip = new EventLambda() { InstanceName = "setNodeToolTip", Lambda = () => {var toolTipLabel = new System.Windows.Controls.Label() { Content = Model.GetDocumentation() };rootUI.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };rootUI.Render.MouseEnter += (sender, args) => toolTipLabel.Content = Model.GetDocumentation();} };
            Apply<object, object> id_75d68da3ae524f6a90820a132929f4e8 = new Apply<object, object>() { Lambda = input =>{var notUpdated = UpdatePorts(input as IEnumerable<Port>);return notUpdated;} };
            ConvertToEvent<object> id_3402c4f9d1e144c8a3a004b22648f411 = new ConvertToEvent<object>() {  };
            Data<object> refreshOutputPorts = new Data<object>() { InstanceName = "refreshOutputPorts", Lambda = GetAcceptedPorts };
            Horizontal addNewParameterRow = new Horizontal() { InstanceName = "addNewParameterRow", Ratios = new[] { 40, 20, 40 } };
            Text id_3c75b6ea1b66418cb7e615b2606679cf = new Text(text: "") {  };
            Button id_be88730102f6429999da458d6052c186 = new Button(title: "+") { Width = 20, Margin = new Thickness(5) };
            EventLambda id_a7da3ed9102c45d98b732bf0097969b8 = new EventLambda() { Lambda = () => {CreateNodeParameterRow("", "");} };
            EventConnector id_0d226bcf444a4d15a3ca11b33cfb3217 = new EventConnector() {  };
            EventLambda id_77363f8080664137a5cdbdf65ed3acc3 = new EventLambda() { Lambda = RefreshParameterRows };
            Box id_c7f783a1838d4e64977d50749086f870 = new Box() { Render = new Border() { Child = _parameterRowsPanel } };
            EventConnector id_6cc711de25b94c58b59c8663857c84ad = new EventConnector() {  };
            ContextMenu id_f19b5e1b5b544f11974b10997a48fe73 = new ContextMenu() {  };
            MenuItem id_42ed1000fd094c928fbf74dbac761a6c = new MenuItem(header: "Open source code...") {  };
            EventLambda id_60fecf7b844b42809c919bb8ecab317f = new EventLambda() { Lambda = () =>{Process.Start(Model.GetCodeFilePath());} };
            MenuItem id_db566c5a86e345a5bda46307549883f3 = new MenuItem(header: "Through your default external editor") {  };
            MenuItem id_8edcb8a6dd85435ab06463a26d7c1955 = new MenuItem(header: "Through the GALADE text editor") {  };
            Data<string> id_abc3cc0e0ebd4f78bf445c1d131bb077 = new Data<string>() { Lambda = Model.GetCodeFilePath };
            ApplyAction<object> id_c65ca79ac65b4c44aaf448930ef06b4a = new ApplyAction<object>() { Lambda = input =>{if (StateTransition.CurrentStateMatches(Enums.DiagramMode.AwaitingPortSelection)){var wire = Graph.Get("SelectedWire") as ALAWire;if (wire == null) return;if (wire.Source == null){wire.Source = this;wire.SourcePort = input as Box;}else if (wire.Destination == null){wire.Destination = this;wire.DestinationPort = input as Box;}StateTransition.Update(Enums.DiagramMode.Idle);}} };
            DataFlowConnector<object> id_3eeadd36141843478c80a3c22006b7fa = new DataFlowConnector<object>() {  };
            KeyEvent id_92219849372649e0b52e46cf52f31926 = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.LeftCtrl, Key.Q }, ExtractSender = source => (source as Box).Render };
            EventLambda id_e582e9db437c4b85919a4518d1719f2d = new EventLambda() { Lambda = () =>{var sourcePort = GetSelectedPort();if (sourcePort == null) return;var source = this;var wire = new ALAWire(){Graph = Graph,Canvas = Canvas,Source = source,Destination = null,SourcePort = sourcePort,DestinationPort = null,StateTransition = StateTransition};Graph.AddEdge(wire);wire.Paint();wire.StartMoving(source: false);} };
            UIFactory id_024106a3acd3404db01cb4d031729ac0 = new UIFactory(getUIContainer: () =>{foreach (var initialised in Model.GetInitialisedVariables()){CreateNodeParameterRow(initialised, Model.GetValue(initialised));}RefreshParameterRows();return new Text("");}) {  };
            ApplyAction<string> id_61136b9eff524bf3841d7b8bcbd2f042 = new ApplyAction<string>() { Lambda = input =>{Model.Name = input;} };
            UIFactory id_ffd61ab0fa3b47f5ae9df9a9e3be3af3 = new UIFactory(getUIContainer: () =>{return CreateTypeGenericsDropDownMenus();}) {  };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            rootUI.WireTo(id_85e448fa744b45dd8f5929a71038bd96, "uiLayout");
            rootUI.WireTo(id_ad180d96a8ed45f382b813a5b9376157, "eventHandlers");
            rootUI.WireTo(id_834ccee2118c45029480940ba5c3fd44, "eventHandlers");
            rootUI.WireTo(id_a050da9816504dadb848d773b2af61ee, "eventHandlers");
            rootUI.WireTo(id_b7695b11d04a4eebb3dc353c5e4d2e7c, "eventHandlers");
            rootUI.WireTo(id_ec9b474b868e41d8b07a8f9e067ec1a1, "eventHandlers");
            rootUI.WireTo(id_b6e7bb630ac14eedaf71f9bf2d3ecb34, "eventHandlers");
            rootUI.WireTo(id_93cb0561751b4b53b340376715dde182, "eventHandlers");
            rootUI.WireTo(id_92219849372649e0b52e46cf52f31926, "eventHandlers");
            rootUI.WireTo(id_f19b5e1b5b544f11974b10997a48fe73, "contextMenu");
            id_85e448fa744b45dd8f5929a71038bd96.WireTo(id_300926a8ee77446785e15b73547248a7, "children");
            id_85e448fa744b45dd8f5929a71038bd96.WireTo(nodeMiddle, "children");
            id_85e448fa744b45dd8f5929a71038bd96.WireTo(id_668d2fe19b42434a83f39de5d26eb302, "children");
            nodeMiddle.WireTo(nodeIdRow, "children");
            nodeMiddle.WireTo(parameterRowVert, "children");
            nodeMiddle.WireTo(addNewParameterRow, "children");
            nodeIdRow.WireTo(nodeTypeDropDownMenu, "children");
            nodeIdRow.WireTo(id_ffd61ab0fa3b47f5ae9df9a9e3be3af3, "children");
            nodeIdRow.WireTo(nodeNameTextBox, "children");
            nodeTypeDropDownMenu.WireTo(id_5faf1af6aa0e444c9d2ab7a4b7f6c6c1, "selectedItem");
            nodeNameTextBox.WireTo(id_61136b9eff524bf3841d7b8bcbd2f042, "textOutput");
            id_300926a8ee77446785e15b73547248a7.WireTo(id_36b667d819e9490b921ce69ed4611c23, "uiInstanceOutput");
            id_36b667d819e9490b921ce69ed4611c23.WireTo(refreshInputPorts, "eventOutput");
            refreshInputPorts.WireTo(id_75d68da3ae524f6a90820a132929f4e8, "dataOutput");
            id_fb1ccfe68ae04552a1c226e79c54e85a.WireTo(id_412e5e2d33f04e328af832147c2461bd, "output");
            id_412e5e2d33f04e328af832147c2461bd.WireTo(setUpPortBox, "elementOutput");
            id_412e5e2d33f04e328af832147c2461bd.WireTo(id_6cc711de25b94c58b59c8663857c84ad, "complete");
            setUpPortBox.WireTo(id_20b8b4d6b87f4ed38f7601fc0ce788a8, "output");
            id_668d2fe19b42434a83f39de5d26eb302.WireTo(id_3402c4f9d1e144c8a3a004b22648f411, "uiInstanceOutput");
            id_20b8b4d6b87f4ed38f7601fc0ce788a8.WireTo(addUIEventsToPort, "fanoutList");
            id_20b8b4d6b87f4ed38f7601fc0ce788a8.WireTo(id_948079e584fb419da207a60b9f957e74, "fanoutList");
            id_f23af6d8b2cd48c19a1e499540530afa.WireTo(id_524a83c7905e4080acbc4023d674a0eb, "output");
            id_524a83c7905e4080acbc4023d674a0eb.WireTo(id_3eeadd36141843478c80a3c22006b7fa, "wire");
            id_1c3c5bbe0f2647dfac9343006c32855a.WireTo(id_928140f081da455fb136ec53765273ea, "output");
            id_928140f081da455fb136ec53765273ea.WireTo(id_8010048b12234e7c94785c630aaf47f8, "wire");
            addUIEventsToPort.WireTo(id_f23af6d8b2cd48c19a1e499540530afa, "fanoutList");
            addUIEventsToPort.WireTo(id_1c3c5bbe0f2647dfac9343006c32855a, "fanoutList");
            addUIEventsToPort.WireTo(id_23f5b7fadab346a1b2297a4fdc13f167, "fanoutList");
            addUIEventsToPort.WireTo(id_891c4b2762ac4083be7c1bbd026dc8e4, "fanoutList");
            addUIEventsToPort.WireTo(id_c2ca7dd1d4e24118926f878b1c5948aa, "fanoutList");
            id_23f5b7fadab346a1b2297a4fdc13f167.WireTo(id_6513e498d86748c686f5a41901aef411, "output");
            id_6513e498d86748c686f5a41901aef411.WireTo(id_9264824b2d064f46b8f242ccbe8f6af1, "wire");
            id_891c4b2762ac4083be7c1bbd026dc8e4.WireTo(id_dfdae7ba8c15442d8dc067bc3af1cd02, "output");
            id_dfdae7ba8c15442d8dc067bc3af1cd02.WireTo(id_dc90339dd4784b88a0d610e14e2d07b6, "wire");
            id_c2ca7dd1d4e24118926f878b1c5948aa.WireTo(id_71bf2f733b7c4efbbb633c073ea5b88f, "output");
            id_71bf2f733b7c4efbbb633c073ea5b88f.WireTo(id_197a8be9284e4650a9bfb2dc13066c27, "wire");
            id_ad180d96a8ed45f382b813a5b9376157.WireTo(id_5d8934be3e3547c29d839b4f622da05c, "sourceOutput");
            id_834ccee2118c45029480940ba5c3fd44.WireTo(id_1a76d0c3e9d34356886f67813740db78, "sourceOutput");
            id_a050da9816504dadb848d773b2af61ee.WireTo(id_82b0f651f769401fab4fed153cf0d520, "sourceOutput");
            id_b7695b11d04a4eebb3dc353c5e4d2e7c.WireTo(id_39bba7b1f8844dea9a76375f90bc4dfb, "sourceOutput");
            id_23a9b3f299ff470e92d7fca0be07aef9.WireTo(id_bfc08d670f6348c2b34306c9394db3b1, "fanoutList");
            id_23a9b3f299ff470e92d7fca0be07aef9.WireTo(id_259a14bea3284f53b14d630c76defe79, "fanoutList");
            id_23a9b3f299ff470e92d7fca0be07aef9.WireTo(id_8fb4e411123b4a65be162ab7955a496f, "fanoutList");
            id_23a9b3f299ff470e92d7fca0be07aef9.WireTo(id_d793129cf72544e3b684e74e54bb5024, "fanoutList");
            id_ec9b474b868e41d8b07a8f9e067ec1a1.WireTo(id_261ebcc1bd3f4737aaed8f92ce14fc1e, "sourceOutput");
            id_b6e7bb630ac14eedaf71f9bf2d3ecb34.WireTo(id_23a9b3f299ff470e92d7fca0be07aef9, "sourceOutput");
            id_93cb0561751b4b53b340376715dde182.WireTo(id_93165a041fc243809b3415edc56b219a, "sourceOutput");
            id_75d68da3ae524f6a90820a132929f4e8.WireTo(id_fb1ccfe68ae04552a1c226e79c54e85a, "output");
            id_3402c4f9d1e144c8a3a004b22648f411.WireTo(refreshOutputPorts, "eventOutput");
            refreshOutputPorts.WireTo(id_75d68da3ae524f6a90820a132929f4e8, "dataOutput");
            addNewParameterRow.WireTo(id_024106a3acd3404db01cb4d031729ac0, "children");
            addNewParameterRow.WireTo(id_be88730102f6429999da458d6052c186, "children");
            addNewParameterRow.WireTo(id_3c75b6ea1b66418cb7e615b2606679cf, "children");
            id_be88730102f6429999da458d6052c186.WireTo(id_0d226bcf444a4d15a3ca11b33cfb3217, "eventButtonClicked");
            parameterRowVert.WireTo(id_c7f783a1838d4e64977d50749086f870, "children");
            id_0d226bcf444a4d15a3ca11b33cfb3217.WireTo(id_a7da3ed9102c45d98b732bf0097969b8, "fanoutList");
            id_0d226bcf444a4d15a3ca11b33cfb3217.WireTo(id_77363f8080664137a5cdbdf65ed3acc3, "complete");
            id_6cc711de25b94c58b59c8663857c84ad.WireTo(setNodeToolTip, "fanoutList");
            id_f19b5e1b5b544f11974b10997a48fe73.WireTo(id_42ed1000fd094c928fbf74dbac761a6c, "children");
            id_42ed1000fd094c928fbf74dbac761a6c.WireTo(id_db566c5a86e345a5bda46307549883f3, "children");
            id_42ed1000fd094c928fbf74dbac761a6c.WireTo(id_8edcb8a6dd85435ab06463a26d7c1955, "children");
            id_db566c5a86e345a5bda46307549883f3.WireTo(id_60fecf7b844b42809c919bb8ecab317f, "clickedEvent");
            id_8edcb8a6dd85435ab06463a26d7c1955.WireTo(id_abc3cc0e0ebd4f78bf445c1d131bb077, "clickedEvent");
            id_3eeadd36141843478c80a3c22006b7fa.WireTo(id_5530df97a5ff4836b1e296a4573a7913, "fanoutList");
            id_3eeadd36141843478c80a3c22006b7fa.WireTo(id_c65ca79ac65b4c44aaf448930ef06b4a, "fanoutList");
            id_92219849372649e0b52e46cf52f31926.WireTo(id_e582e9db437c4b85919a4518d1719f2d, "eventHappened");
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






































































































































































































































































































































































































































































































































































































