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
        private Box rootUI;
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
            rootUI = new Box()
            {
                Background = Brushes.LightSkyBlue
            };

            // BEGIN AUTO-GENERATED INSTANTIATIONS
            Horizontal id_903b7736a4ce4cb99b0084afbb5096b6 = new Horizontal() {  };
            Vertical nodeMiddle = new Vertical() { InstanceName = "nodeMiddle", Margin = new Thickness(1, 0, 1, 0) };
            Horizontal nodeIdRow = new Horizontal() { InstanceName = "nodeIdRow" };
            DropDownMenu nodeTypeDropDownMenu = new DropDownMenu() { InstanceName = "nodeTypeDropDownMenu", Text = Model.Type, Width = 100, Items = AvailableDomainAbstractions };
            TextBox nodeNameTextBox = new TextBox() { InstanceName = "nodeNameTextBox", Text = ShowName ? Model.Name : "", Width = 50 };
            UIFactory id_0863bf95dd894622943b4fbce0b060ad = new UIFactory(getUIContainer: () =>{_inputPortsVert = new Vertical();_inputPortsVert.Margin = new Thickness(0);return _inputPortsVert;}) {  };
            ConvertToEvent<object> id_d278495ede644d308cb21b1d41b565db = new ConvertToEvent<object>() {  };
            Data<object> refreshInputPorts = new Data<object>() { InstanceName = "refreshInputPorts", Lambda = GetImplementedPorts };
            Cast<object, IEnumerable<Port>> id_a854096830654d0abc9f7fe61bd7ce94 = new Cast<object, IEnumerable<Port>>() {  };
            ForEach<Port> id_6333a91b59184c7b8b4ef9d96361dafb = new ForEach<Port>() {  };
            Apply<Port, object> setUpPortBox = new Apply<Port, object>() { InstanceName = "setUpPortBox", Lambda = port =>{var box = new Box();box.Payload = port;box.Width = 50;box.Height = 15;box.Background = Brushes.White;box.BorderThickness = new Thickness(2);var toolTipLabel = new System.Windows.Controls.Label() { Content = port.ToString()};box.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = port.ToString();var text = new Text(text: port.Name);text.HorizAlignment = HorizontalAlignment.Center;box.Render.Child = (text as IUI).GetWPFElement();if (port.IsInputPort){_inputPortsVert.WireTo(box, "children");_inputPortBoxes.Add(box);(_inputPortsVert as IUI).GetWPFElement(); /* Refresh UI */}else{_outputPortsVert.WireTo(box, "children");_outputPortBoxes.Add(box);(_outputPortsVert as IUI).GetWPFElement(); /* Refresh UI */}return box;} };
            UIFactory id_acbaa02c52e343eb80f49b7fd4653534 = new UIFactory(getUIContainer: () =>{_outputPortsVert = new Vertical();_outputPortsVert.Margin = new Thickness(0);return _outputPortsVert;}) {  };
            ApplyAction<object> id_793e8f6b46f9472fa89283baac4c06b6 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = Brushes.Red;} };
            DataFlowConnector<object> id_a1b15ee8a2854838b72661884e56abeb = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_e6465a521e1c4b29a3c9c03570b8d5df = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = Brushes.Black;} };
            Apply<object, object> id_89f4b9188c114a8f905d44f0123a36b9 = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseEnter");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_ce9b148e8bb940a0a0c3c36998570ebe = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            Apply<object, object> id_7642b46faa95401c8123966dbb569212 = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseLeave");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_0089a9d69ec64dd6ba40bcfced44006f = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            DataFlowConnector<object> addUIEventsToPort = new DataFlowConnector<object>() { InstanceName = "addUIEventsToPort" };
            Apply<object, object> id_6468ccb5db134b3ab93eb585c63137be = new Apply<object, object>() { Lambda = input =>{var mouseButtonEvent = new MouseButtonEvent("MouseDown");mouseButtonEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseButtonEvent, "eventHandlers");return mouseButtonEvent;} };
            DynamicWiring<IDataFlow<object>> id_b32bae5012fc4b6a80fdce486f37f246 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_79a407e0c90c41ac90a33f3e9f9bad71 = new ApplyAction<object>() { Lambda = input =>{_selectedPort = input as Box;_selectedPort.Render.Focus();} };
            Apply<object, object> id_a4b67d6d92214647ae34b9258809c120 = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("GotFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_000a1b06e0814d3a8e4df88fa08536f1 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_17fe4980feff4d06aaaa445adef7fc47 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = Brushes.SandyBrown;} };
            Apply<object, object> id_bde49ed2d7594153ace03b235b95d746 = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("LostFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_ca2ed8e6219c4dffb19253046c797c9b = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_ff8e12ecb3a5490d8ae3d170a21bf17c = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = Brushes.White;} };
            MouseEvent id_c57c8c4b8a6c4ad2a80085cd5fb07932 = new MouseEvent(eventName: "MouseEnter") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_0e9be4969ee647f6b55f1870a8a7afc0 = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.Aquamarine;} };
            MouseEvent id_ae9c7a98503a4c0e81055c01bcf85440 = new MouseEvent(eventName: "MouseLeave") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_759714207b024df49619f4269f203d64 = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) (input as Box).Background = Brushes.LightSkyBlue;} };
            RoutedEventSubscriber id_65a9de2617824ff98d6afeca5704a503 = new RoutedEventSubscriber(eventName: "GotFocus") { ExtractSender = source => (source as Box).Render };
            RoutedEventSubscriber id_cae5519093614595a5a99d3fd0d2affb = new RoutedEventSubscriber(eventName: "LostFocus") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_a6c18f2313084f668e539034155c42aa = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.LightSkyBlue;} };
            ApplyAction<object> id_4aaab5ccc1e44fe98d5fffdbb1b17348 = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) ui.Focus();} };
            ApplyAction<object> id_937bd98bc2ce4a9d9770295d85f5a022 = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.Aquamarine;} };
            DataFlowConnector<object> id_70c6481d584f4daaa7f34ac265068779 = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_017c0e473c434a9fad4fed193a4cbe63 = new ApplyAction<object>() { Lambda = input =>{StateTransition.Update(Enums.DiagramMode.IdleSelected);} };
            MouseEvent id_c304ff2534a14bb89d9c27fb19cf7244 = new MouseEvent(eventName: "MouseMove") { ExtractSender = source => (source as Box).Render, Condition = args => Mouse.LeftButton == MouseButtonState.Pressed && Keyboard.IsKeyDown(Key.LeftShift) };
            ApplyAction<object> id_4aaecae80d89423691bb72ba1481144c = new ApplyAction<object>() { Lambda = input =>{var mousePos = Mouse.GetPosition(Canvas);var oldPosition = new Point(PositionX, PositionY);PositionX = mousePos.X - _mousePosInBox.X;PositionY = mousePos.Y - _mousePosInBox.Y;PositionChanged?.Invoke();} };
            ApplyAction<object> id_c80d5a2aca9a42e2a64ca00dac8e5d69 = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;_mousePosInBox = Mouse.GetPosition(render);Mouse.Capture(render);} };
            ApplyAction<object> id_7e3c5563fe2743a7bda2c2e32c402c75 = new ApplyAction<object>() { Lambda = input =>{Graph.Set("SelectedNode", this);} };
            MouseButtonEvent id_17014d60960d479b8f8555c3f1235691 = new MouseButtonEvent(eventName: "MouseLeftButtonDown") { ExtractSender = source => (source as Box).Render };
            ApplyAction<string> id_a716581225354385a89e9afe1d368e2f = new ApplyAction<string>() { Lambda = input =>{TypeChanged?.Invoke(input);} };
            MouseButtonEvent id_0bf4365cab7948d98c3af9145688fb6a = new MouseButtonEvent(eventName: "MouseLeftButtonUp") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_71c2a29d5bc74664ac058cec6f638eff = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;if (Mouse.Captured?.Equals(render) ?? false) Mouse.Capture(null);} };
            ApplyAction<object> id_05db94e26fef44bba2e61fe62931e54a = new ApplyAction<object>() { Lambda = input =>{(input as Box).InitialiseUI();} };
            EventLambda setNodeToolTip = new EventLambda() { InstanceName = "setNodeToolTip", Lambda = () => {var toolTipLabel = new System.Windows.Controls.Label() { Content = Model.GetDocumentation() };rootUI.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };rootUI.Render.MouseEnter += (sender, args) => toolTipLabel.Content = Model.GetDocumentation();} };
            Apply<object, object> id_b3b0e77ff80845a88538e1a63278960a = new Apply<object, object>() { Lambda = input =>{var notUpdated = UpdatePorts(input as IEnumerable<Port>);return notUpdated;} };
            ConvertToEvent<object> id_3024b85106564d4d8874be8a95bc0fe7 = new ConvertToEvent<object>() {  };
            Data<object> refreshOutputPorts = new Data<object>() { InstanceName = "refreshOutputPorts", Lambda = GetAcceptedPorts };
            Horizontal addNewParameterRow = new Horizontal() { InstanceName = "addNewParameterRow", Ratios = new[] { 40, 20, 40 } };
            Text id_83105c35d9bd498795a1e222b74a54e2 = new Text(text: "") {  };
            Button id_08fae6bde70c43fa9e4c8021e343ba74 = new Button(title: "+") { Width = 20, Margin = new Thickness(5) };
            EventLambda id_e076f3e28abb41f8b5e4aa589cd06ffb = new EventLambda() { Lambda = () => {CreateNodeParameterRow("", "");} };
            EventConnector id_ff9a392fe5a34d21b6c6e4f82bf821e9 = new EventConnector() {  };
            EventLambda id_d9b9ad2cb0374cfd9dd2f71179eed57a = new EventLambda() { Lambda = RefreshParameterRows };
            Box id_12f992dce45a4c2f82b745b6c8c282bb = new Box() { Render = new Border() { Child = _parameterRowsPanel } };
            EventConnector id_479c88d647754844881f40d1789ec72c = new EventConnector() {  };
            ContextMenu id_aae6dbb619094f25be0bc6adcf231636 = new ContextMenu() {  };
            MenuItem id_25b3ad3184e8447d8f72ddf094c497c7 = new MenuItem(header: "Open source code...") {  };
            EventLambda id_cc740656fd01423f96743900924d507c = new EventLambda() { Lambda = () =>{Process.Start(Model.GetCodeFilePath());} };
            MenuItem id_db753da621b3407e91c37d190d242e7f = new MenuItem(header: "Through your default external editor") {  };
            MenuItem id_84243b93ab16486bbe255087cfd68743 = new MenuItem(header: "Through the GALADE text editor") {  };
            Data<string> id_44bf671639444a59a62fb58f8230106e = new Data<string>() { Lambda = Model.GetCodeFilePath };
            ApplyAction<object> id_b48da263a0a5408085b7ec66013dec3d = new ApplyAction<object>() { Lambda = input =>{if (StateTransition.CurrentStateMatches(Enums.DiagramMode.AwaitingPortSelection)){var wire = Graph.Get("SelectedWire") as ALAWire;if (wire == null) return;if (wire.Source == null){wire.Source = this;wire.SourcePort = input as Box;}else if (wire.Destination == null){wire.Destination = this;wire.DestinationPort = input as Box;}StateTransition.Update(Enums.DiagramMode.Idle);}} };
            DataFlowConnector<object> id_1c509ae465774cb3b3d479aaa1e3f344 = new DataFlowConnector<object>() {  };
            KeyEvent id_ffba7c2e90114c44a8ad43d09bcad15d = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.LeftCtrl, Key.Q }, ExtractSender = source => (source as Box).Render };
            EventLambda id_330f4402b74043f3930113a79ca80030 = new EventLambda() { Lambda = () =>{var sourcePort = GetSelectedPort();if (sourcePort == null) return;var source = this;var wire = new ALAWire(){Graph = Graph,Canvas = Canvas,Source = source,Destination = null,SourcePort = sourcePort,DestinationPort = null,StateTransition = StateTransition};Graph.AddEdge(wire);wire.Paint();wire.StartMoving(source: false);} };
            UIFactory id_7d7e5bdb0cb4470ea1f8181552731efe = new UIFactory(getUIContainer: () =>{foreach (var initialised in Model.GetInitialisedVariables()){CreateNodeParameterRow(initialised, Model.GetValue(initialised));}RefreshParameterRows();return new Text("");}) {  };
            ApplyAction<string> id_e22318db599c4565befb7e67ba14491a = new ApplyAction<string>() { Lambda = input =>{Model.Name = input;} };
            UIFactory id_8dbae6aa41f04cbda4418bcc512bcfc7 = new UIFactory(getUIContainer: () =>{return CreateTypeGenericsDropDownMenus();}) {  };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            rootUI.WireTo(id_903b7736a4ce4cb99b0084afbb5096b6, "uiLayout");
            rootUI.WireTo(id_c57c8c4b8a6c4ad2a80085cd5fb07932, "eventHandlers");
            rootUI.WireTo(id_ae9c7a98503a4c0e81055c01bcf85440, "eventHandlers");
            rootUI.WireTo(id_65a9de2617824ff98d6afeca5704a503, "eventHandlers");
            rootUI.WireTo(id_cae5519093614595a5a99d3fd0d2affb, "eventHandlers");
            rootUI.WireTo(id_c304ff2534a14bb89d9c27fb19cf7244, "eventHandlers");
            rootUI.WireTo(id_17014d60960d479b8f8555c3f1235691, "eventHandlers");
            rootUI.WireTo(id_0bf4365cab7948d98c3af9145688fb6a, "eventHandlers");
            rootUI.WireTo(id_ffba7c2e90114c44a8ad43d09bcad15d, "eventHandlers");
            rootUI.WireTo(id_aae6dbb619094f25be0bc6adcf231636, "contextMenu");
            id_903b7736a4ce4cb99b0084afbb5096b6.WireTo(id_0863bf95dd894622943b4fbce0b060ad, "children");
            id_903b7736a4ce4cb99b0084afbb5096b6.WireTo(nodeMiddle, "children");
            id_903b7736a4ce4cb99b0084afbb5096b6.WireTo(id_acbaa02c52e343eb80f49b7fd4653534, "children");
            nodeMiddle.WireTo(nodeIdRow, "children");
            nodeMiddle.WireTo(parameterRowVert, "children");
            nodeMiddle.WireTo(addNewParameterRow, "children");
            nodeIdRow.WireTo(nodeTypeDropDownMenu, "children");
            nodeIdRow.WireTo(id_8dbae6aa41f04cbda4418bcc512bcfc7, "children");
            nodeIdRow.WireTo(nodeNameTextBox, "children");
            nodeTypeDropDownMenu.WireTo(id_a716581225354385a89e9afe1d368e2f, "selectedItem");
            nodeNameTextBox.WireTo(id_e22318db599c4565befb7e67ba14491a, "textOutput");
            id_0863bf95dd894622943b4fbce0b060ad.WireTo(id_d278495ede644d308cb21b1d41b565db, "uiInstanceOutput");
            id_d278495ede644d308cb21b1d41b565db.WireTo(refreshInputPorts, "eventOutput");
            refreshInputPorts.WireTo(id_b3b0e77ff80845a88538e1a63278960a, "dataOutput");
            id_a854096830654d0abc9f7fe61bd7ce94.WireTo(id_6333a91b59184c7b8b4ef9d96361dafb, "output");
            id_6333a91b59184c7b8b4ef9d96361dafb.WireTo(setUpPortBox, "elementOutput");
            id_6333a91b59184c7b8b4ef9d96361dafb.WireTo(id_479c88d647754844881f40d1789ec72c, "complete");
            setUpPortBox.WireTo(id_a1b15ee8a2854838b72661884e56abeb, "output");
            id_acbaa02c52e343eb80f49b7fd4653534.WireTo(id_3024b85106564d4d8874be8a95bc0fe7, "uiInstanceOutput");
            id_a1b15ee8a2854838b72661884e56abeb.WireTo(addUIEventsToPort, "fanoutList");
            id_a1b15ee8a2854838b72661884e56abeb.WireTo(id_05db94e26fef44bba2e61fe62931e54a, "fanoutList");
            id_89f4b9188c114a8f905d44f0123a36b9.WireTo(id_ce9b148e8bb940a0a0c3c36998570ebe, "output");
            id_ce9b148e8bb940a0a0c3c36998570ebe.WireTo(id_1c509ae465774cb3b3d479aaa1e3f344, "wire");
            id_7642b46faa95401c8123966dbb569212.WireTo(id_0089a9d69ec64dd6ba40bcfced44006f, "output");
            id_0089a9d69ec64dd6ba40bcfced44006f.WireTo(id_e6465a521e1c4b29a3c9c03570b8d5df, "wire");
            addUIEventsToPort.WireTo(id_89f4b9188c114a8f905d44f0123a36b9, "fanoutList");
            addUIEventsToPort.WireTo(id_7642b46faa95401c8123966dbb569212, "fanoutList");
            addUIEventsToPort.WireTo(id_6468ccb5db134b3ab93eb585c63137be, "fanoutList");
            addUIEventsToPort.WireTo(id_a4b67d6d92214647ae34b9258809c120, "fanoutList");
            addUIEventsToPort.WireTo(id_bde49ed2d7594153ace03b235b95d746, "fanoutList");
            id_6468ccb5db134b3ab93eb585c63137be.WireTo(id_b32bae5012fc4b6a80fdce486f37f246, "output");
            id_b32bae5012fc4b6a80fdce486f37f246.WireTo(id_79a407e0c90c41ac90a33f3e9f9bad71, "wire");
            id_a4b67d6d92214647ae34b9258809c120.WireTo(id_000a1b06e0814d3a8e4df88fa08536f1, "output");
            id_000a1b06e0814d3a8e4df88fa08536f1.WireTo(id_17fe4980feff4d06aaaa445adef7fc47, "wire");
            id_bde49ed2d7594153ace03b235b95d746.WireTo(id_ca2ed8e6219c4dffb19253046c797c9b, "output");
            id_ca2ed8e6219c4dffb19253046c797c9b.WireTo(id_ff8e12ecb3a5490d8ae3d170a21bf17c, "wire");
            id_c57c8c4b8a6c4ad2a80085cd5fb07932.WireTo(id_0e9be4969ee647f6b55f1870a8a7afc0, "sourceOutput");
            id_ae9c7a98503a4c0e81055c01bcf85440.WireTo(id_759714207b024df49619f4269f203d64, "sourceOutput");
            id_65a9de2617824ff98d6afeca5704a503.WireTo(id_937bd98bc2ce4a9d9770295d85f5a022, "sourceOutput");
            id_cae5519093614595a5a99d3fd0d2affb.WireTo(id_a6c18f2313084f668e539034155c42aa, "sourceOutput");
            id_70c6481d584f4daaa7f34ac265068779.WireTo(id_4aaab5ccc1e44fe98d5fffdbb1b17348, "fanoutList");
            id_70c6481d584f4daaa7f34ac265068779.WireTo(id_017c0e473c434a9fad4fed193a4cbe63, "fanoutList");
            id_70c6481d584f4daaa7f34ac265068779.WireTo(id_c80d5a2aca9a42e2a64ca00dac8e5d69, "fanoutList");
            id_70c6481d584f4daaa7f34ac265068779.WireTo(id_7e3c5563fe2743a7bda2c2e32c402c75, "fanoutList");
            id_c304ff2534a14bb89d9c27fb19cf7244.WireTo(id_4aaecae80d89423691bb72ba1481144c, "sourceOutput");
            id_17014d60960d479b8f8555c3f1235691.WireTo(id_70c6481d584f4daaa7f34ac265068779, "sourceOutput");
            id_0bf4365cab7948d98c3af9145688fb6a.WireTo(id_71c2a29d5bc74664ac058cec6f638eff, "sourceOutput");
            id_b3b0e77ff80845a88538e1a63278960a.WireTo(id_a854096830654d0abc9f7fe61bd7ce94, "output");
            id_3024b85106564d4d8874be8a95bc0fe7.WireTo(refreshOutputPorts, "eventOutput");
            refreshOutputPorts.WireTo(id_b3b0e77ff80845a88538e1a63278960a, "dataOutput");
            addNewParameterRow.WireTo(id_7d7e5bdb0cb4470ea1f8181552731efe, "children");
            addNewParameterRow.WireTo(id_08fae6bde70c43fa9e4c8021e343ba74, "children");
            addNewParameterRow.WireTo(id_83105c35d9bd498795a1e222b74a54e2, "children");
            id_08fae6bde70c43fa9e4c8021e343ba74.WireTo(id_ff9a392fe5a34d21b6c6e4f82bf821e9, "eventButtonClicked");
            parameterRowVert.WireTo(id_12f992dce45a4c2f82b745b6c8c282bb, "children");
            id_ff9a392fe5a34d21b6c6e4f82bf821e9.WireTo(id_e076f3e28abb41f8b5e4aa589cd06ffb, "fanoutList");
            id_ff9a392fe5a34d21b6c6e4f82bf821e9.WireTo(id_d9b9ad2cb0374cfd9dd2f71179eed57a, "complete");
            id_479c88d647754844881f40d1789ec72c.WireTo(setNodeToolTip, "fanoutList");
            id_aae6dbb619094f25be0bc6adcf231636.WireTo(id_25b3ad3184e8447d8f72ddf094c497c7, "children");
            id_25b3ad3184e8447d8f72ddf094c497c7.WireTo(id_db753da621b3407e91c37d190d242e7f, "children");
            id_25b3ad3184e8447d8f72ddf094c497c7.WireTo(id_84243b93ab16486bbe255087cfd68743, "children");
            id_db753da621b3407e91c37d190d242e7f.WireTo(id_cc740656fd01423f96743900924d507c, "clickedEvent");
            id_84243b93ab16486bbe255087cfd68743.WireTo(id_44bf671639444a59a62fb58f8230106e, "clickedEvent");
            id_1c509ae465774cb3b3d479aaa1e3f344.WireTo(id_793e8f6b46f9472fa89283baac4c06b6, "fanoutList");
            id_1c509ae465774cb3b3d479aaa1e3f344.WireTo(id_b48da263a0a5408085b7ec66013dec3d, "fanoutList");
            id_ffba7c2e90114c44a8ad43d09bcad15d.WireTo(id_330f4402b74043f3930113a79ca80030, "eventHappened");
            // END AUTO-GENERATED WIRING

            Render = _nodeMask;
            _nodeMask.Children.Clear();
            _detailedRender.Child = (rootUI as IUI).GetWPFElement();
            _nodeMask.Children.Add(_detailedRender);

            // Instance mapping
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


















































































































































































































































































































































































































































































































































































