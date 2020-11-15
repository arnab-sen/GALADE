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

        private IUI CreatePortsVertical(bool inputPorts = true)
        {
            var portsVert = new Vertical()
            {
                Margin = new Thickness(0)
            };

            var ports = inputPorts ? GetImplementedPorts() : GetAcceptedPorts();

            var notUpdated = UpdatePorts(ports);

            foreach (var port in notUpdated)
            {
                SetUpPortBox(port, portsVert);
            }

            return portsVert;
        }


        private void SetUpPortBox(Port port, Vertical vert)
        {
            var box = new Box();
            box.Payload = port;
            box.Width = 50;
            box.Height = 15;
            box.Background = PortBackground;
            box.BorderThickness = new Thickness(2);

            var toolTipLabel = new System.Windows.Controls.Label()
            {
                Content = port.ToString()

            };

            box.Render.ToolTip = new System.Windows.Controls.ToolTip()
            {
                Content = toolTipLabel
            };

            box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = port.ToString();

            var text = new Text(text: port.Name);
            text.HorizAlignment = HorizontalAlignment.Center;
            box.Render.Child = (text as IUI).GetWPFElement();

            if (port.IsInputPort)
            {
                vert.WireTo(box, "children");
                _inputPortBoxes.Add(box);
                (vert as IUI).GetWPFElement(); /* Refresh UI */
            }
            else
            {
                vert.WireTo(box, "children");
                _outputPortBoxes.Add(box);
                (vert as IUI).GetWPFElement(); /* Refresh UI */
            }

            AddUIEventsToPort(box);
            box.InitialiseUI();
        }

        private void AddUIEventsToPort(Box portBox)
        {
            var render = portBox.Render;

            render.MouseEnter += (sender, args) =>
            {
                portBox.BorderColour = PortHighlightedBorder;

                if (StateTransition.CurrentStateMatches(Enums.DiagramMode.AwaitingPortSelection))
                {
                    var wire = Graph.Get("SelectedWire") as ALAWire;
                    if (wire == null)
                        return;
                    if (wire.Source == null)
                    {
                        wire.Source = this;
                        wire.SourcePort = portBox;
                    }
                    else if (wire.Destination == null)
                    {
                        wire.Destination = this;
                        wire.DestinationPort = portBox;
                    }

                    StateTransition.Update(Enums.DiagramMode.Idle);
                }
            };

            render.MouseLeave += (sender, args) =>
            {
                portBox.BorderColour = PortBorder;
            };

            render.MouseDown += (sender, args) =>
            {
                _selectedPort = portBox;
                _selectedPort.Render.Focus();
            };

            render.GotFocus += (sender, args) =>
            {
                portBox.Background = PortHighlightedBackground;
                portBox.BorderColour = PortHighlightedBorder;
            };

            render.LostFocus += (sender, args) =>
            {
                portBox.Background = PortBackground;
                portBox.BorderColour = PortBorder;
            };
        }

        private IUI CreateNodeMiddleVertical()
        {
            var nodeMiddle = new Vertical()
            {
                Margin = new Thickness(1, 0, 1, 0)
            };

            nodeMiddle.WireTo(CreateNodeIdRow(), "children");
            nodeMiddle.WireTo(CreateParameterRowVert(), "children");
            nodeMiddle.WireTo(CreateAddNewParameterRowHoriz(), "children");

            return nodeMiddle;
        }

        private IUI CreateNodeIdRow()
        {
            var nodeIdRow = new Horizontal();
            var nodeTypeDropDownMenu = new DropDownMenu()
            {
                Items = AvailableDomainAbstractions,
                Text = Model.Type,
                Width = 100
            };
            var createGenericDropDownMenus = new UIFactory() { GetUIContainer = CreateTypeGenericsDropDownMenus };
            var nodeNameTextBox = new TextBox()
            {
                Text = ShowName ? Model.Name : "",
                Width = 50
            };
            var typeChanged = new ApplyAction<string>() { Lambda = input => TypeChanged?.Invoke(input) };
            var nameChanged = new ApplyAction<string>() { Lambda = input => Model.Name = input };

            nodeIdRow.WireTo(nodeTypeDropDownMenu, "children");
            nodeIdRow.WireTo(createGenericDropDownMenus, "children");
            nodeIdRow.WireTo(nodeNameTextBox, "children");

            nodeTypeDropDownMenu.WireTo(typeChanged, "selectedItem");

            nodeNameTextBox.WireTo(nameChanged, "textOutput");

            return nodeIdRow;
        }

        private IUI CreateParameterRowVert()
        {
            var parameterRowVert = new Vertical()
            {
                Margin = new Thickness(5, 5, 5, 0)
            };

            parameterRowVert.WireTo(new Box()
            {
                Render = new Border()
                {
                    Child = _parameterRowsPanel
                }
            });

            return parameterRowVert;
        }

        private IUI CreateAddNewParameterRowHoriz()
        {
            var addNewParameterRow = new Horizontal()
            {
                Ratios = new[] { 40, 20, 40 }
            };

            var getInitialisedRow = new UIFactory()
            {
                GetUIContainer = () =>
                {
                    foreach (var initialised in Model.GetInitialisedVariables())
                    {
                        CreateNodeParameterRow(initialised, Model.GetValue(initialised));
                    }

                    RefreshParameterRows();
                    return new Text("");
                }
            };

            var addNewRowButton = new Button("+")
            {
                Width = 20,
                Margin = new Thickness(5)
            };

            addNewParameterRow.WireTo(getInitialisedRow, "children");
            addNewParameterRow.WireTo(addNewRowButton, "children");
            addNewParameterRow.WireTo(new Text(""), "children");

            addNewRowButton.WireTo(new EventLambda()
            {
                Lambda = () =>
                {
                    CreateNodeParameterRow();
                    RefreshParameterRows();
                }
            }, "eventButtonClicked");

            return addNewParameterRow;
        }

        private void AddUIEventsToNode(Box nodeBox)
        {
            var render = nodeBox.Render;

            render.MouseEnter += (sender, args) =>
            {
                nodeBox.Background = NodeHighlightedBackground;
            };

            render.MouseLeave += (sender, args) =>
            {
                if (!render.IsKeyboardFocusWithin) nodeBox.Background = NodeBackground;
            };

            render.GotFocus += (sender, args) =>
            {
                nodeBox.Background = NodeHighlightedBackground;
            };

            render.LostFocus += (sender, args) =>
            {
                nodeBox.Background = NodeBackground;
            };
            
            render.MouseMove += (sender, args) =>
            {
                if (Mouse.LeftButton == MouseButtonState.Pressed && Keyboard.IsKeyDown(Key.LeftShift))
                {
                    var mousePos = Mouse.GetPosition(Canvas);
                    var oldPosition = new Point(PositionX, PositionY);
                    PositionX = mousePos.X - _mousePosInBox.X;
                    PositionY = mousePos.Y - _mousePosInBox.Y;
                    PositionChanged?.Invoke();
                }
            };

            render.MouseLeftButtonDown += (sender, args) =>
            {
                if (!render.IsKeyboardFocusWithin) render.Focus();

                StateTransition.Update(Enums.DiagramMode.IdleSelected);

                _mousePosInBox = Mouse.GetPosition(render);
                Mouse.Capture(render);

                Graph.Set("SelectedNode", this);
            };

            render.MouseLeftButtonUp += (sender, args) =>
            {
                if (Mouse.Captured?.Equals(render) ?? false) Mouse.Capture(null);
            };

            render.KeyDown += (sender, args) =>
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.Q))
                {
                    var sourcePort = GetSelectedPort();
                    if (sourcePort == null) return;

                    var source = this;
                    var wire = new ALAWire()
                    {
                        Graph = Graph, 
                        Canvas = Canvas, 
                        Source = source, 
                        Destination = null, 
                        SourcePort = sourcePort, 
                        DestinationPort = null, 
                        StateTransition = StateTransition
                    };

                    Graph.AddEdge(wire);
                    wire.Paint();
                    wire.StartMoving(source: false);
                }
            };

        }

        private void CreateWiring()
        {
            Vertical inputPortsVert = null;
            Vertical outputPortsVert = null;

            // BEGIN AUTO-GENERATED INSTANTIATIONS
            var rootUI = new Box() {InstanceName="rootUI",Background=NodeBackground};
            var id_a38c965bdcac4123bb22c40a31b04de5 = new Horizontal() {};
            var createInputPortsVertical = new UIFactory() {GetUIContainer=() => CreatePortsVertical(inputPorts: true)};
            var createNodeMiddleVertical = new UIFactory() {GetUIContainer=CreateNodeMiddleVertical};
            var createOutputPortsVertical = new UIFactory() {GetUIContainer=() => CreatePortsVertical(inputPorts: false)};
            var id_71829b744cf145a0a934a55f7768c7bf = new ContextMenu() {};
            var id_403baaf79a824981af02ae135627767f = new MenuItem(header:"Open source code...") {};
            var id_872f85f0291843daad50fcaf77f4e9c2 = new EventLambda() {Lambda=() =>{    Process.Start(Model.GetCodeFilePath());}};
            var id_9c912c6b764641d592796b0d3753424b = new MenuItem(header:"Through your default external editor") {};
            var id_736f961d1ae84d80bfa4570d41685828 = new MenuItem(header:"Through the GALADE text editor") {};
            var id_a09b14f6afef41e59fac3fd10dd6ce00 = new Data<string>() {Lambda=Model.GetCodeFilePath};
            var id_8725125c990e428181b7b064a7b95b72 = new ApplyAction<object>() {Lambda=input => AddUIEventsToNode(input as Box)};
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            rootUI.WireTo(id_a38c965bdcac4123bb22c40a31b04de5, "uiLayout");
            rootUI.WireTo(id_71829b744cf145a0a934a55f7768c7bf, "contextMenu");
            id_a38c965bdcac4123bb22c40a31b04de5.WireTo(createInputPortsVertical, "children");
            id_a38c965bdcac4123bb22c40a31b04de5.WireTo(createNodeMiddleVertical, "children");
            id_a38c965bdcac4123bb22c40a31b04de5.WireTo(createOutputPortsVertical, "children");
            id_71829b744cf145a0a934a55f7768c7bf.WireTo(id_403baaf79a824981af02ae135627767f, "children");
            id_403baaf79a824981af02ae135627767f.WireTo(id_9c912c6b764641d592796b0d3753424b, "children");
            id_403baaf79a824981af02ae135627767f.WireTo(id_736f961d1ae84d80bfa4570d41685828, "children");
            id_9c912c6b764641d592796b0d3753424b.WireTo(id_872f85f0291843daad50fcaf77f4e9c2, "clickedEvent");
            id_736f961d1ae84d80bfa4570d41685828.WireTo(id_a09b14f6afef41e59fac3fd10dd6ce00, "clickedEvent");
            rootUI.WireTo(id_8725125c990e428181b7b064a7b95b72, "eventHandlers");
            // END AUTO-GENERATED WIRING

            Render = _nodeMask;
            _nodeMask.Children.Clear();
            _detailedRender.Child = (rootUI as IUI).GetWPFElement();
            _nodeMask.Children.Add(_detailedRender);

            // Instance mapping
            _rootUI = rootUI;
        }

        public ALANode()
        {
            Id = Utilities.GetUniqueId();
        }
    }
}




























































































































































































































































































































































































































































































































































































































