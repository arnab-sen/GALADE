using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
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
using Newtonsoft.Json.Linq;
using CheckBox = DomainAbstractions.CheckBox;

namespace StoryAbstractions
{
    /// <summary>
    /// <para>A visual node that represents an instance of an abstraction.</para>
    /// <para>Note: This class used to define all of its UI and UI events through wiring, but for performance reasons,
    /// many of those definitions were moved into methods.</para>
    /// </summary>
    public class ALANode : IALANode
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string Id { get; set; }
        public string Type { get; set; } = "?";
        public string Name => !string.IsNullOrWhiteSpace(Model?.Name) ? Model.Name.Trim('@', ' ') : "id_" + Id;
        public List<string> AvailableProgrammingParadigms { get; } = new List<string>();
        public List<string> AvailableAbstractions { get; } = new List<string>();
        public List<string> AvailableRequirementsAbstractions { get; } = new List<string>();
        public System.Windows.Controls.ContextMenu ContextMenu { get; set; }
        public System.Windows.Controls.ContextMenu WireContextMenu { get; set; }

        public delegate void DeleteChildNodes();

        public event DeleteChildNodes OnDeleteChildNodes;
        public Action<Graph, System.Windows.Controls.Canvas, IALANode, IALANode, Box, Box, StateTransition<Enums.DiagramMode>, System.Windows.Controls.ContextMenu> CreateWireFromNode
        {
            get;
            set;
        }

        public JObject MetaData
        {
            get => _metaData;
            set
            {
                _metaData = value;
                SetMetaData(_metaData);
            }
        }

        public bool IsRoot
        {
            get => _isRoot;
            set
            {
                if (value != _isRoot)
                {
                    _isRoot = value;

                    if (_isRoot)
                    {
                        if (!Graph.Roots.Contains(this)) Graph.Roots.Add(this);
                    }
                    else
                    {
                        Graph.Roots.RemoveAll(o => o.Equals(this));
                    }
                }
            }
        }

        /// <summary>
        /// Whether this instance is actually only a reference to a node defined elsewhere. Reference nodes will not generate instantiations, but can still be used in the wiring.
        /// </summary>
        public bool IsReferenceNode
        {
            get => _isReference;
            set
            {
                _isReference = value;

                if (_nameTextBox != null)
                {
                    var currentText = _nameTextBox.Text;
                    if (_isReference)
                    {
                        if (!currentText.StartsWith("@")) _nameTextBox.Text = "@" + currentText;
                    }
                    else
                    {
                        _nameTextBox.Text = currentText.TrimStart('@');
                    }
                }

                // Update node colour
                if (_isHighlighted)
                {
                    HighlightNode();
                }
                else
                {
                    UnhighlightNode();
                }
            }
        }

        public bool HasBreakpoint
        {
            get => _hasBreakpoint;
            set
            {
                _hasBreakpoint = value;
                _hasBreakpointImage.Fill = _hasBreakpoint ? Brushes.Red : Brushes.DarkGray;
            }
        }

        public Graph Graph { get; set; }
        // public List<object> Edges { get; } = new List<object>();
        public Canvas Canvas { get; set; }
        public StateTransition<Enums.DiagramMode> StateTransition { get; set; }
        public UIElement Render { get; set; }
        public int DefaultZIndex { get; set; } = 0;
        public Brush NodeBackground { get; set; } = Utilities.BrushFromHex("#d2ecf9");
        public Brush ReferenceNodeBackground { get; set; } = Brushes.Orange;
        public Brush NodeBorder { get; set; } = Brushes.Black;
        public Brush NodeHighlightedBackground { get; set; } = Utilities.BrushFromHex("#c6fce5");
        public Brush ReferenceNodeHighlightedBackground { get; set; } = Brushes.LightPink;
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

        object IALANode.NodeModel
        {
            get => Model;
            set => Model = value as AbstractionModel;
        }

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

        public delegate void TextChangedDelegate(string text);
        public TextChangedDelegate TypeChanged;

        public event SomethingChangedDelegate PositionChanged;

        public Func<Port, Point> GetAttachmentPoint { get; set; }
        public bool IsSelected { get; set; }

        // Private fields
        private Box _rootUI;
        private Box _selectedPort;
        private Point _mousePosInBox = new Point(0, 0);
        private List<Box> _inputPortBoxes = new List<Box>();
        private List<Box> _outputPortBoxes = new List<Box>();
        private List<string> _nodeParameters = new List<string>();
        private List<string> _genericTypeOptions = new List<string>();
        private List<Tuple<Horizontal, DropDownMenu, TextBox, Button>> _nodeParameterRows = new List<Tuple<Horizontal, DropDownMenu, TextBox, Button>>();
        // private List<Tuple<Horizontal, DropDownMenu, TextEditor, Button>> _nodeParameterRows = new List<Tuple<Horizontal, DropDownMenu, TextEditor, Button>>();
        private Canvas _nodeMask = new Canvas();
        private Border _detailedRender = new Border();
        private UIElement _textMaskRender;
        private Text _textMask;
        private List<DropDownMenu> _genericDropDowns = new List<DropDownMenu>();
        private DropDownMenu _typeDropDown;
        private TextBox _nameTextBox;
        private SimulateKeyboard _keyboardSim = new SimulateKeyboard();
        private JObject _metaData = null;
        private string _noAcceptedPortDescriptionFound = "(none found in abstraction class - please add a description in a comment above the port declaration in the source file)";
        private string _noImplementedPortDescriptionFound = "(none found in abstraction class - please add a description in the <summary> section in the class documentation)";
        private bool _isRoot = false;
        private bool _isReference = false;
        private bool _isHighlighted = false;
        private Dictionary<string, AbstractionModel> _loadedModels = new Dictionary<string, AbstractionModel>();
        private System.Windows.Controls.Button _descPopupButton = null;
        private System.Windows.Controls.TextBox _popupText = null;
        private bool _canMove = true;
        private bool _hasBreakpoint = false;
        private Brush _tempBorderColour;
        private Thickness _tempBorderThickness;

        // Fill should be either Brushes.Red or Brushes.DarkGray to symbolise that it does/doesn't have a breakpoint in its source file
        private Ellipse _hasBreakpointImage = new Ellipse()
        {
            Width = 20,
            Height = 20,
            Fill = Brushes.DarkGray,
            Stroke = Brushes.Black,
            StrokeThickness = 2,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        // Global instances
        public Vertical _inputPortsVert;
        public Vertical _outputPortsVert;
        public StackPanel _parameterRowsPanel = new StackPanel();
        private IUI _nodeIdRow;
        private CheckBox _nodeIsRootCheckBox;
        private CheckBox _nodeIsReferenceNodeCheckBox;
        private ContextMenu _mainContextMenu;


        // Ports

        // Methods

        public override string ToString()
        {
            return $"{Model.FullType} {Model.Name}";
        }

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
        /// <param name="name">The variable name of the port.</param>
        /// <param name="useDefault">Whether to return a default port box if the desired port does not exist.</param>
        /// <returns></returns>
        public Box GetPortBox(string name = "", bool useDefault = true)
        {
            foreach (var outputPortBox in _outputPortBoxes)
            {
                if (outputPortBox.Payload is Port port && port.Name == name) return outputPortBox;
            }

            foreach (var inputPortBox in _inputPortBoxes)
            {
                if (inputPortBox.Payload is Port port && port.Name == name) return inputPortBox;
            }

            if (useDefault)
            {
                if (_outputPortBoxes.Any()) return _outputPortBoxes.First();
                if (_inputPortBoxes.Any()) return _inputPortBoxes.First();
            }

            return null;
        }

        public void UpdateUI()
        {
            Render.Dispatcher.Invoke(() =>
            {
                UpdateNodeParameters();
                RefreshPorts(inputPorts: true);
                RefreshPorts(inputPorts: false);
                _nodeIdRow.GetWPFElement();
                Model.RefreshGenerics();
                // RefreshParameterRows();
            }, DispatcherPriority.Loaded);

            Render.Dispatcher.Invoke(() =>
            {
                if (_nodeMask.Children.Contains(_textMaskRender)) _nodeMask.Children.Remove(_textMaskRender);
                _textMaskRender = CreateTextMask();

                if (IsReferenceNode && !_nameTextBox.Text.StartsWith("@")) _nameTextBox.Text = "@" + Model.Name;
            }, DispatcherPriority.Loaded);

            _popupText.Dispatcher.Invoke(() => _popupText.Text = GetDescription());

        }

        private void SetMetaData(JObject metaDataObj)
        {
            _metaData = metaDataObj;
            if (_metaData != null)
            {
                if (_metaData.ContainsKey("IsRoot")) IsRoot = bool.Parse(_metaData.GetValue("IsRoot")?.ToString() ?? "false");
            }
        }

        private bool IsFocusedWithin()
        {
            return _rootUI.Render.IsKeyboardFocusWithin;
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

                        var oldPort = (Port)box.Payload;

                        oldPort.CloneFrom(newPort);

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

                        var oldPort = (Port)box.Payload;

                        oldPort.CloneFrom(newPort);

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

        private void UpdateNodeParameters()
        {
            NodeParameters.Clear();
            NodeParameters.AddRange(Model.GetConstructorArgs().Select(kvp => kvp.Key));
            NodeParameters.AddRange(Model.GetProperties().Select(kvp => kvp.Key));
            NodeParameters.AddRange(Model.GetFields().Select(kvp => kvp.Key));

            var initVars = Model.GetInitialisedVariables();

            _nodeParameterRows.Clear();
            foreach (var initVar in initVars)
            {
                if (string.IsNullOrWhiteSpace(initVar))
                {
                    Model.RemoveValue("");
                    continue;
                }

                CreateNodeParameterRow(initVar, Model.GetValue(initVar));
            }

            RefreshParameterRows();
        }

        private AbstractionModel CreateDummyAbstractionModel()
        {
            var model = new AbstractionModel()
            {
                Type = "UNDEFINED",
                Name = ""
            };

            model.AddImplementedPort("Port", "input");
            model.AddAcceptedPort("Port", "output");

            return model;
        }

        public void CreateInternals()
        {
            if (Model == null) Model = CreateDummyAbstractionModel();

            GenericTypeOptions = new List<string>()
            {
                "int",
                "string",
                "bool",
                "object",
                "double",
                "float",
                "char",
                "DateTime"
            };

            Model.RefreshGenerics();

            CreateWiring();

            if (ContextMenu != null && Render is FrameworkElement fe) fe.ContextMenu = ContextMenu;

            // UpdateNodeParameters();
            RefreshParameterRows(removeEmptyRows: true);

            Canvas.SetZIndex(Render, DefaultZIndex);
        }

        public void RefreshParameterRows(bool removeEmptyRows = false)
        {
            _parameterRowsPanel.Children.Clear();

            foreach (var row in _nodeParameterRows)
            {
                if (removeEmptyRows && string.IsNullOrEmpty(row.Item2?.Text) && string.IsNullOrEmpty(row.Item3?.Text)) continue;

                _parameterRowsPanel.Children.Add((row.Item1 as IUI).GetWPFElement());
            }
        }

        public void Delete(bool deleteChildren = false)
        {
            if (Graph.Get("SelectedNode")?.Equals(this) ?? false) Graph.Set("SelectedNode", null);
            Graph.DeleteNode(this);
            if (Canvas.Children.Contains(Render)) Canvas.Children.Remove(Render);

            if (deleteChildren) OnDeleteChildNodes();
        }

        private void CreateNodeParameterRow() => CreateNodeParameterRow("", "");

        private void CreateNodeParameterRow(string type, string name)
        {
            var dropDown = new DropDownMenu()
            {
                Text = type,
                Items = NodeParameters,
                Width = 100,
                Height = 25
            };

            SubscribeTextEditingEvent(dropDown);

            var textBox = new TextBox()
            {
                Text = name,
                Width = 100,
                TrackIndent = true,
                Font = "Consolas",
                TabString = "    "
            };

            var UIConfig_textBox = new UIConfig()
            {
                MaxWidth = 1000
            };

            UIConfig_textBox.WireTo(textBox, "child");

            // var textBox = new TextEditor()
            // {
            //     Text = name,
            //     Width = 100
            // };

            SubscribeTextEditingEvent(textBox);

            textBox.WireTo(new ApplyAction<string>()
            {
                Lambda = text =>
                {
                    Model.SetValue(dropDown.Text, text, initialise: true);
                }
            }, "textOutput");

            var deleteButton = new Button("-")
            {
                Width = 20,
                Height = 20,
                Margin = new Thickness(5, 0, 5, 0),
                HorizAlignment = HorizontalAlignment.Left
            };


            var dropDownUI = (dropDown as IUI).GetWPFElement() as ComboBox;

            var toolTipLabel = new System.Windows.Controls.Label() { Content = "" };
            dropDownUI.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };
            ToolTipService.SetShowDuration(dropDownUI, 60000); // Show tooltip for 60 seconds

            dropDownUI.MouseEnter += (sender, args) =>
            {
                _canMove = false;
            };

            dropDownUI.MouseLeave += (sender, args) =>
            {
                _canMove = true;
            };

            dropDownUI.MouseEnter += (sender, args) =>
            {
                var memberDocumentation = Model.GetMemberDocumentation(dropDownUI.Text);
                var tooltipDoc = "";

                if (!string.IsNullOrEmpty(memberDocumentation)) tooltipDoc += memberDocumentation + "\n";
                tooltipDoc += "Type: " + Model.GetType(dropDownUI.Text);

                toolTipLabel.Content = tooltipDoc;
            };

            // Only show uninitialised fields and properties
            dropDownUI.DropDownOpened += (sender, args) =>
            {
                dropDown.Items = dropDown.Items.Where(item => dropDownUI.SelectedItem is string str && str == item || !Model.GetInitialisedVariables().Contains(item));
            };

            dropDownUI.SelectionChanged += (sender, args) =>
            {
                // Deinitialise previous selection
                if (args.RemovedItems.Count > 0 && args.RemovedItems[0] is string)
                {
                    var oldVarName = (string)args.RemovedItems[0];
                    Model.Deinitialise(oldVarName);
                }

                // Initialise new selection
                var varName = dropDownUI.SelectedValue?.ToString() ?? "";
                dropDown.Text = varName;
                textBox.Text = Model.GetValue(varName);
                Model.Initialise(varName);
            };

            var horiz = new Horizontal();
            horiz.WireTo(dropDown, "children");
            horiz.WireTo(UIConfig_textBox, "children");
            horiz.WireTo(deleteButton, "children");

            var buttonUI = (deleteButton as IUI).GetWPFElement() as System.Windows.Controls.Button;

            buttonUI.Click += (sender, args) =>
            {
                var row = _nodeParameterRows.FirstOrDefault(tuple => tuple.Item4.Equals(deleteButton));
                var varName = row?.Item2.Text ?? "";
                Model.Deinitialise(varName);
                _nodeParameterRows.Remove(row);
                RefreshParameterRows();
            };

            _nodeParameterRows.Add(Tuple.Create(horiz, dropDown, textBox, deleteButton));
        }

        private UIElement CreateTextMask(string text = "")
        {
            if (string.IsNullOrEmpty(text))
            {
                text = $"{Model.Type}";
                var description = Model.Name;

                if (!string.IsNullOrEmpty(Model.Name) && !Model.Name.StartsWith("id_"))
                    text = text + "\n" + description;
            }

            var maskContainer = new Canvas()
            {

            };

            var background = new Border()
            {
                Background = Brushes.White,
                Opacity = 0.8,
                Width = Width,
                Height = Height,
                BorderBrush = _rootUI.BorderColour,
                BorderThickness = _rootUI.BorderThickness
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
                Color = _hasBreakpoint ? Brushes.MediumVioletRed : Brushes.Black
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
                if (_textMaskRender == null) _textMaskRender = CreateTextMask();

                if (!_nodeMask.Children.Contains(_textMaskRender)) _nodeMask.Children.Add(_textMaskRender);
            }
            else
            {
                if (_textMaskRender != null) _textMaskRender.Visibility = Visibility.Collapsed;

                if (_nodeMask.Children.Contains(_textMaskRender)) _nodeMask.Children.Remove(_textMaskRender);

                _textMaskRender = null;
            }
        }

        public string ToInstantiation(bool singleLine = true) => singleLine ? ToFlatInstantiation() : ToFormattedInstantiation();

        private string ToFlatInstantiation(JObject metaData = null)
        {
            var instantiation = "";

            var sb = new StringBuilder();
            // sb.Append("var ");
            sb.Append($"{Model.FullType} ");
            var trimmedName = Name.Trim('@', ' ');
            sb.Append(trimmedName);
            sb.Append(" = new ");
            sb.Append(Model.FullType);


            var initialised = Model.GetInitialisedVariables();
            var constructorArgs = Model.GetConstructorArgs()
                .Where(kvp => initialised.Contains(kvp.Key))
                .OrderBy(kvp => kvp.Key)
                .ToList();

            var propertiesAndFields = new List<KeyValuePair<string, string>>();

            if (!initialised.Contains("InstanceName")
                && (Model.ContainsField("InstanceName") || Model.ContainsProperty("InstanceName")))
                propertiesAndFields.Add(new KeyValuePair<string, string>("InstanceName", $"\"{trimmedName}\""));

            propertiesAndFields.AddRange(Model.GetProperties()
                .Where(kvp => initialised.Contains(kvp.Key))
                .ToList());

            propertiesAndFields.AddRange(Model.GetFields()
                .Where(kvp => initialised.Contains(kvp.Key))
                .ToList());


            sb.Append("(");
            sb.Append(Flatten(GetConstructorArgumentSyntaxList(constructorArgs).ToString()));
            sb.Append(") {");
            sb.Append(Flatten(GetPropertySyntaxList(propertiesAndFields).ToString()));
            sb.Append("};");


            JObject tempMetaData;
            if (metaData == null)
            {
                if (MetaData == null)
                {
                    MetaData = new JObject();
                    MetaData["IsRoot"] = IsRoot;
                }

                tempMetaData = MetaData;
            }
            else
            {
                tempMetaData = metaData;
            }

            sb.Append(" /* ");
            sb.Append(tempMetaData.ToString(Newtonsoft.Json.Formatting.None));
            sb.Append(" */");

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

                ArgumentSyntax argNode;
                if (!argName.StartsWith("~")) // If the arg is not an unnamed constructor arg
                {
                    argNode =
                        Argument(IdentifierName(argValue))
                            .WithNameColon(
                                NameColon(
                                    IdentifierName(argName)));
                }
                else
                {
                    argNode = Argument(IdentifierName(argValue));
                }

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

        private void SubscribeTextEditingEvent(IUI uiAbstraction)
        {
            var ui = uiAbstraction.GetWPFElement();
            ui.GotFocus += (sender, args) => StateTransition.Update(Enums.DiagramMode.TextEditing);
            ui.GotKeyboardFocus += (sender, args) => StateTransition.Update(Enums.DiagramMode.TextEditing);
            // ui.LostFocus += (sender, args) => { };
            // ui.LostKeyboardFocus += (sender, args) => { };

            ui.MouseEnter += (sender, args) => _canMove = false;
            ui.MouseLeave += (sender, args) => _canMove = true;
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

                        if (_nodeMask.Children.Contains(_textMaskRender)) _nodeMask.Children.Remove(_textMaskRender);
                        _textMaskRender = CreateTextMask();
                    }

                };

                dropDown.WireTo(updateGeneric, "selectedItem");

                horiz.WireTo(dropDown, "children");

                SubscribeTextEditingEvent(dropDown);
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

            if (inputPorts)
            {
                _inputPortsVert = portsVert;
            }
            else
            {
                _outputPortsVert = portsVert;
            }

            RefreshPorts(inputPorts: inputPorts);

            return portsVert;
        }

        private void RefreshPorts(bool inputPorts = true)
        {
            var ports = inputPorts ? GetImplementedPorts() : GetAcceptedPorts();

            var notUpdated = UpdatePorts(ports);

            foreach (var port in notUpdated)
            {
                CreatePortBox(port, inputPorts ? _inputPortsVert : _outputPortsVert);
            }
        }


        private string GetPortDocumentation(string portName)
        {
            var port = Model.GetPort(portName);
            if (port == null) return "";

            var sb = new StringBuilder();
            sb.AppendLine($"Type: {port.Type}");
            sb.AppendLine($"Name: {port.Name}");

            var portIsAcceptedPort = !port.IsInputPort && !port.IsReversePort;
            sb.AppendLine($"Description: {(!string.IsNullOrEmpty(port.Description) ? port.Description : portIsAcceptedPort ? _noAcceptedPortDescriptionFound : _noImplementedPortDescriptionFound)}");

            return sb.ToString();
        }

        private void CreatePortBox(Port port, Vertical vert)
        {
            var box = new Box();
            box.Payload = port;
            box.Width = 50;
            box.Height = 15;
            box.Background = PortBackground;
            box.BorderThickness = new Thickness(2);

            var toolTipLabel = new System.Windows.Controls.Label()
            {
                Content = GetPortDocumentation(port.Name)
            };

            box.Render.ToolTip = new System.Windows.Controls.ToolTip()
            {
                Content = toolTipLabel
            };

            ToolTipService.SetShowDuration(box.Render, 60000); // Show tooltip for 60 seconds

            box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = GetPortDocumentation(port.Name);

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
                    var wire = Graph.Get("SelectedWire") as IALAWire;
                    if (wire == null)
                        return;
                    if (wire.SourceNode == null)
                    {
                        wire.SetSourceNodeAndPort(this, portBox);
                    }
                    else if (wire.DestinationNode == null)
                    {
                        wire.SetDestinationNodeAndPort(this, portBox);
                    }

                    StateTransition.Update(Enums.DiagramMode.Idle);
                }
            };

            render.MouseLeave += (sender, args) =>
            {
                portBox.BorderColour = PortBorder;
            };

            render.MouseUp += (sender, args) =>
            {
                _selectedPort = portBox;
                _selectedPort.Render.Focus();
            };

            render.GotFocus += (sender, args) =>
            {
                portBox.Background = PortHighlightedBackground;
                portBox.BorderColour = PortHighlightedBorder;
                _selectedPort = portBox;

                Select(focus: false);
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

            _nodeIdRow = CreateNodeIdRow();
            nodeMiddle.WireTo(_nodeIdRow, "children");
            nodeMiddle.WireTo(CreateParameterRowVert(), "children");
            nodeMiddle.WireTo(CreateAddNewParameterRowHoriz(), "children");

            return nodeMiddle;
        }

        private IUI CreateNodeIdRow()
        {
            var nodeIdRow = new Horizontal();
            var nodeTypeDropDownMenu = new DropDownMenu()
            {
                Items = AvailableAbstractions,
                Text = Model.Type,
                Width = 100
            };

            _typeDropDown = nodeTypeDropDownMenu;

            var createGenericDropDownMenus = new UIFactory() { GetUIContainer = CreateTypeGenericsDropDownMenus };
            var nodeNameTextBox = new TextBox()
            {
                Text = ShowName ? Model.Name : "",
                Width = 50,
                AcceptsTab = false
            };

            _nameTextBox = nodeNameTextBox;
            var typeChanged = new ApplyAction<string>() { Lambda = input => TypeChanged?.Invoke(input) };
            var nameChanged = new ApplyAction<string>()
            {
                Lambda = input =>
                {
                    if (input.StartsWith("@"))
                    {
                        if (!IsReferenceNode) IsReferenceNode = true;
                    }
                    else
                    {
                        if (IsReferenceNode) IsReferenceNode = false;
                    }

                    Model.Name = input;

                    Model.SetValue("InstanceName", $"\"{input}\"");
                }
            };

            nodeIdRow.WireTo(nodeTypeDropDownMenu, "children");
            nodeIdRow.WireTo(createGenericDropDownMenus, "children");
            nodeIdRow.WireTo(nodeNameTextBox, "children");

            nodeTypeDropDownMenu.WireTo(typeChanged, "selectedItem");

            nodeNameTextBox.WireTo(nameChanged, "textOutput");

            SubscribeTextEditingEvent(nodeTypeDropDownMenu);
            SubscribeTextEditingEvent(nodeNameTextBox);

            return nodeIdRow;
        }

        public void ChangeTypeInUI(string newType)
        {
            _typeDropDown.Text = newType;
        }

        public void ChangeNameInUI(string newName)
        {
            _nameTextBox.Text = newName;
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
            // addNewParameterRow.WireTo(new Text(""), "children");

            addNewRowButton.WireTo(new EventLambda()
            {
                Lambda = () =>
                {
                    CreateNodeParameterRow();
                    RefreshParameterRows();
                }
            }, "eventButtonClicked");

            // Use an Expander to open text inside the node
            // var descriptionExpander = new Expander();
            // descriptionExpander.ExpandDirection = ExpandDirection.Down;
            // var descriptionExpanderText = new Label()
            // {
            //     Content = "Test\nString"
            // };
            //
            // descriptionExpander.Content = descriptionExpanderText;
            //
            // addNewParameterRow.WireTo(new UIFactory()
            // {
            //     GetUIElement = () => descriptionExpander
            // });

            // Use a Popup to open text outside the node
            _descPopupButton = new System.Windows.Controls.Button()
            {
                Content = "?",
                Width = 20,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(5),
                Background = new SolidColorBrush(Color.FromRgb(221, 221, 221)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(112, 112, 112))
            };

            var descPopup = new Popup()
            {
                AllowsTransparency = true,
                Placement = PlacementMode.Bottom
            };

            var popupBackground = new Border()
            {
                Background = Brushes.White,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1)
            };

            _popupText = new System.Windows.Controls.TextBox()
            {
                MinWidth = 200,
                MinHeight = 50,
                AcceptsTab = true,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 500,
                MaxHeight = 200,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var desc = GetDescription();
            if (!string.IsNullOrWhiteSpace(desc))
            {
                _descPopupButton.Background = new SolidColorBrush(Color.FromRgb(167, 220, 252));
                _descPopupButton.BorderBrush = new SolidColorBrush(Color.FromRgb(60, 127, 177));
            }

            popupBackground.Child = _popupText;

            descPopup.Child = popupBackground;

            descPopup.Opened += (sender, args) => _popupText.Text = GetDescription();
            // descPopup.Closed += (sender, args) => SetDescription(popupText.Text);
            _popupText.TextChanged += (sender, args) =>
            {
                SetDescription(_popupText.Text);
                if (!string.IsNullOrWhiteSpace(_popupText.Text))
                {
                    _descPopupButton.Background = new SolidColorBrush(Color.FromRgb(167, 220, 252));
                    _descPopupButton.BorderBrush = new SolidColorBrush(Color.FromRgb(60, 127, 177));
                }
                else
                {
                    _descPopupButton.Background = new SolidColorBrush(Color.FromRgb(221, 221, 221));
                    _descPopupButton.BorderBrush = new SolidColorBrush(Color.FromRgb(112, 112, 112));
                }
            };

            _descPopupButton.Click += (sender, args) =>
            {
                descPopup.PlacementTarget = _descPopupButton;
                descPopup.StaysOpen = false;
                descPopup.IsOpen = true;
            };

            addNewParameterRow.WireTo(new UIFactory()
            {
                GetUIElement = () => _descPopupButton
            });

            addNewParameterRow.WireTo(new UIFactory()
            {
                GetUIElement = () => _hasBreakpointImage
            });

            return addNewParameterRow;
        }

        public string GetDescription()
        {
            return MetaData?.GetValue("Description")?.Value<string>() ?? "";
        }

        public void SetDescription(string text)
        {
            if (MetaData != null) MetaData["Description"] = text;
        }

        private void AddUIEventsToNode(Box nodeBox)
        {
            var render = nodeBox.Render;

            var toolTipText = new System.Windows.Controls.TextBlock()
            {
                Text = Model.GetDocumentation()
            };

            render.ToolTip = new System.Windows.Controls.ToolTip()
            {
                Content = toolTipText
            };

            ToolTipService.SetShowDuration(render, 60000); // Show tooltip for 60 seconds

            render.MouseEnter += (sender, args) =>
            {
                toolTipText.Text = $"Instance Name: {Model.Name}\n\nDocumentation:\n{Model.GetDocumentation()}";
                HighlightNode();
            };

            render.MouseLeave += (sender, args) =>
            {
                if (!IsSelected) UnhighlightNode();
            };

            render.LostFocus += (sender, args) =>
            {
                if (!Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    UnhighlightNode();
                }
            };

            render.MouseMove += (sender, args) =>
            {
                if (Mouse.LeftButton == MouseButtonState.Pressed
                    // && (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    && _canMove
                    && StateTransition.CurrentStateMatches(Enums.DiagramMode.IdleSelected))
                {
                    if (!Mouse.Captured?.Equals(render) ?? true) Mouse.Capture(render);

                    var mousePos = Mouse.GetPosition(Canvas);
                    PositionX = mousePos.X - _mousePosInBox.X;
                    PositionY = mousePos.Y - _mousePosInBox.Y;
                    PositionChanged?.Invoke();
                }
            };

            render.MouseLeftButtonDown += (sender, args) =>
            {
                _mousePosInBox = Mouse.GetPosition(render);

                Select();
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
                    CreateWireFromNode(Graph, Canvas, source, null, sourcePort, null, StateTransition, WireContextMenu);
                }
            };

            render.MouseRightButtonDown += (sender, args) => Select();

        }

        public void HighlightNode()
        {
            _rootUI.Background = !IsReferenceNode ? NodeHighlightedBackground : ReferenceNodeHighlightedBackground;
            _isHighlighted = true;

            Canvas.SetZIndex(Render, 99);
            foreach (var wire in Graph.Edges.OfType<IALAWire>().Where(w => (w.SourceNode?.Equals(this) ?? false) || (w.DestinationNode?.Equals(this) ?? false)))
            {
                if (wire.Render == null) continue;
                Canvas.SetZIndex(wire.Render, 100);
            }
        }

        public void UnhighlightNode()
        {
            _rootUI.Background = !IsReferenceNode ? NodeBackground : ReferenceNodeBackground;
            _isHighlighted = false;

            Canvas.SetZIndex(Render, DefaultZIndex);
            foreach (var wire in Graph.Edges.OfType<IALAWire>().Where(w => (!w.IsHighlighted && (w.SourceNode?.Equals(this) ?? false) || (w.DestinationNode?.Equals(this) ?? false))))
            {
                if (wire.Render == null) continue;
                Canvas.SetZIndex(wire.Render, wire.DefaultZIndex);
            }
        }

        public void Select(bool focus = true)
        {
            IsSelected = true;

            HighlightNode();
            Graph.Set("SelectedNode", this);
            if (focus) _rootUI.Render.Focus();

            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                StateTransition.Update(Enums.DiagramMode.MultiNodeSelect);
            }
            else
            {
                StateTransition.Update(Enums.DiagramMode.IdleSelected);
            }
        }

        public void Deselect()
        {
            IsSelected = false;

            UnhighlightNode();
            if (Graph.Get("SelectedNode")?.Equals(this) ?? false) Graph.Set("SelectedNode", null);

            if (Mouse.Captured?.Equals(_rootUI.Render) ?? false) Mouse.Capture(null);
        }

        public void FocusOnTypeDropDown()
        {
            var dropDown = (_typeDropDown as IUI).GetWPFElement() as ComboBox;
            if (dropDown == null)
            {
                Logging.Log("Failed to convert DropDownMenu to ComboBox in ALANode.FocusOnTypeDropDown()");
                return;
            }

            // Get focus on inner TextBox, must use the dispatcher to ensure that the internal TextBox is loaded and ready to be focused
            dropDown.Dispatcher.Invoke(() => dropDown.Focus(), DispatcherPriority.ApplicationIdle);
        }

        public void LoadDefaultModel(AbstractionModel model)
        {
            if (model == null)
            {
                Logging.Log("Failed to load null model");
                return;
            }

            if (Model == null) Model = new AbstractionModel();

            // Save a clone of the current model
            var loadedModel = new AbstractionModel(Model);
            _loadedModels[Model.Type] = loadedModel;

            // Reuse a previously loaded model if possible, otherwise use the new model
            if (_loadedModels.ContainsKey(model.Type))
            {
                Model.CloneFrom(_loadedModels[model.Type]);
            }
            else
            {
                Model.CloneFrom(model);
            }
        }

        /// <summary>
        /// Searches the contents of the node to determine whether this node is a match for the search query.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public bool IsMatch(string query, bool matchCase = false, bool searchName = true, bool searchType = true, bool searchInstanceName = true, bool searchFieldsAndProperties = true)
        {
            if (searchName && MatchString(Model.Name, query, matchCase)) return true;
            if (searchType && MatchString(Model.FullType, query, matchCase)) return true;
            if (searchInstanceName && MatchString(Model.GetValue("InstanceName"), query, matchCase)) return true;
            if (searchFieldsAndProperties)
            {
                var fieldsAndProperties = Model.GetProperties();
                fieldsAndProperties.AddRange(Model.GetFields());
                fieldsAndProperties.AddRange(Model.GetConstructorArgs());

                foreach (var pair in fieldsAndProperties)
                {
                    if (MatchString(pair.Key, query, matchCase)) return true;
                    if (MatchString(pair.Value, query, matchCase)) return true;
                }
            }

            return false;
        }

        private bool MatchString(string toMatch, string searchToken, bool matchCase = false)
        {
            if (toMatch == null || searchToken == null) return false;

            try
            {
                if (!matchCase)
                {
                    searchToken = searchToken.ToLower();
                    toMatch = toMatch.ToLower();
                }

                return Regex.IsMatch(toMatch, searchToken);
            }
            catch (Exception e)
            {
                Logging.Log($"Failed to match string in ALANode for toMatch: {toMatch} and searchToken: {searchToken} with options [matchCase: {matchCase}].\nException: {e}");
                return false;
            }
        }

        public List<IALAWire> GetConnectedWires(Dictionary<string, string> treeParents = null)
        {
            var connectedWires = new List<IALAWire>();

            var q = new Queue<IALANode>();
            q.Enqueue(this);
            var visited = new HashSet<string>();
            if (treeParents == null) treeParents = new Dictionary<string, string>();

            while (q.Any())
            {
                var parent = q.Dequeue();

                if (!visited.Contains(parent.Id))
                {
                    visited.Add(parent.Id);

                    var directWires = Graph.Edges.OfType<IALAWire>().Where(wire => wire.SourceNode == parent).ToList();
                    connectedWires.AddRange(directWires);
                    var children = directWires.Select(wire => wire.DestinationNode).OfType<IALANode>();

                    foreach (var child in children)
                    {
                        if (!visited.Contains(child.Id))
                        {
                            q.Enqueue(child);
                            treeParents[child.Id] = parent.Id;
                        }
                    }
                }
            }

            return connectedWires;
        }

        public string GenerateConnectedSubdiagramCode()
        {
            var jObj = new JObject();
            var instantiations = new JArray(ToFlatInstantiation());
            var addedNodes = new HashSet<string>() { Id };
            var addedWires = new HashSet<string>();

            var wireTos = new JArray();
            jObj["Instantiations"] = instantiations;
            jObj["WireTos"] = wireTos;

            var treeParents = new Dictionary<string, string>();

            var wires = GetConnectedWires(treeParents);
            foreach (var wire in wires)
            {
                var sourceNode = wire.SourceNode as IALANode;
                var destNode = wire.DestinationNode as IALANode;

                if (addedWires.Contains(wire.Id) || sourceNode == null || destNode == null || sourceNode.Equals(destNode)) continue;

                if (!addedNodes.Contains(sourceNode.Id))
                {
                    addedNodes.Add(sourceNode.Id);
                    instantiations.Add(sourceNode.ToInstantiation());
                }

                if (!addedNodes.Contains(destNode.Id))
                {
                    addedNodes.Add(destNode.Id);
                    instantiations.Add(destNode.ToInstantiation());
                }

                wireTos.Add(wire.ToWireTo());
                addedWires.Add(wire.Id);
            }

            return jObj.ToString();
        }

        public void AddTempBorder(Brush colour, int thickness)
        {
            _tempBorderColour = _rootUI.BorderColour;
            _tempBorderThickness = _rootUI.BorderThickness;
            NodeBorder = colour;

            _rootUI.BorderColour = colour;
            _rootUI.BorderThickness = new Thickness(thickness);
        }

        public void RemoveTempBorder()
        {
            if (_tempBorderColour != default) _rootUI.BorderColour = _tempBorderColour;
            if (_tempBorderThickness != default) _rootUI.BorderThickness = _tempBorderThickness;

            NodeBorder = _rootUI.BorderColour;
            _tempBorderColour = default;
            _tempBorderThickness = default;
        }

        private void CreateWiring()
        {
            Vertical inputPortsVert = null;
            Vertical outputPortsVert = null;

            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR ALANodeUI
            Box rootUI = new Box() { InstanceName = "rootUI", Background = NodeBackground }; /* {"IsRoot":true} */
            Horizontal id_a38c965bdcac4123bb22c40a31b04de5 = new Horizontal() { InstanceName = "id_a38c965bdcac4123bb22c40a31b04de5" }; /* {"IsRoot":false} */
            UIFactory createInputPortsVertical = new UIFactory() { InstanceName = "createInputPortsVertical", GetUIContainer = () => CreatePortsVertical(inputPorts: true) }; /* {"IsRoot":false} */
            UIFactory createNodeMiddleVertical = new UIFactory() { InstanceName = "createNodeMiddleVertical", GetUIContainer = CreateNodeMiddleVertical }; /* {"IsRoot":false} */
            UIFactory createOutputPortsVertical = new UIFactory() { InstanceName = "createOutputPortsVertical", GetUIContainer = () => CreatePortsVertical(inputPorts: false) }; /* {"IsRoot":false} */
            // END AUTO-GENERATED INSTANTIATIONS FOR ALANodeUI

            // BEGIN AUTO-GENERATED WIRING FOR ALANodeUI
            rootUI.WireTo(id_a38c965bdcac4123bb22c40a31b04de5, "uiLayout"); /* {"SourceType":"Box","SourceIsReference":false,"DestinationType":"Horizontal","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_a38c965bdcac4123bb22c40a31b04de5.WireTo(createInputPortsVertical, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"UIFactory","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_a38c965bdcac4123bb22c40a31b04de5.WireTo(createNodeMiddleVertical, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"UIFactory","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_a38c965bdcac4123bb22c40a31b04de5.WireTo(createOutputPortsVertical, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"UIFactory","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            // END AUTO-GENERATED WIRING FOR ALANodeUI

            Render = _nodeMask;
            _nodeMask.Children.Clear();
            _detailedRender.Child = (rootUI as IUI).GetWPFElement(); // perf
            _nodeMask.Children.Add(_detailedRender);
            // _nodeIsRootCheckBox = nodeIsRootCheckBox;
            // _nodeIsReferenceNodeCheckBox = nodeIsReferenceNodeCheckBox;

            AddUIEventsToNode(rootUI);

            // Instance mapping
            _rootUI = rootUI;
        }

        public ALANode()
        {
            Id = Utilities.GetUniqueId();
        }
    }
}