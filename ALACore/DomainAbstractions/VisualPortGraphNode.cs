using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Libraries;
using ProgrammingParadigms;
using Newtonsoft.Json.Linq;
using WPFCanvas = System.Windows.Controls.Canvas;

namespace ProgrammingParadigms
{

    /// <summary>
    /// <para></para>
    /// <para>Ports:</para>
    /// <para>1. IEvent create:</para>
    /// <para>2. List&lt;IEventHandler&gt; nodeEventHandlers:</para>
    /// <para>3. List&lt;IEventHandler&gt; portEventHandlers:</para>
    /// </summary>
    public class VisualPortGraphNode : IMemento, IVisualPortGraphNode, IEvent
    {
        // Public fields and properties
        public string InstanceName = "Default";
        public string Type { get; set; } = "Default";
        public List<string> Types { get; set; }
        public List<string> PortTypes { get; set; }
        public string Name { get; set; } = "";
        public string FullName
        {
            get => !string.IsNullOrEmpty(Name) ? $"{Type} {Name}" : Type;
        }
        public VisualPortGraph Graph { get; set; }
        public StateTransition<Enums.DiagramMode> StateTransition { get; set; }
        public VisualStyle NodeStyle { get; set; }
        public VisualStyle PortStyle { get; set; }
        public double Width { get; set; } = 200;
        public double Height { get; set; } = 50;
        public Port SelectedPort { get; set; }

        public UIElement ContextMenu
        {
            get => nodeRect?.ContextMenu;
            set
            {
                if (nodeRect != null) nodeRect.ContextMenu = value as ContextMenu;
            }
        }

        public bool PortsAreEditable
        {
            get => _editPortMode;
            set
            {
                _editPortMode = value;

                // addInputPortButton.Visibility = _editPortMode ? Visibility.Visible : Visibility.Collapsed;
                // addOutputPortButton.Visibility = _editPortMode ? Visibility.Visible : Visibility.Collapsed;
                horizInputPortEditButtonsPanel.Visibility = _editPortMode ? Visibility.Visible : Visibility.Collapsed;
                horizOutputPortEditButtonsPanel.Visibility = _editPortMode ? Visibility.Visible : Visibility.Collapsed;

                foreach (NodeNameTextBoxPair port in portTexts.Values)
                {
                    port.NameIsEditable = _editPortMode;
                    port.TypeIsEditable = _editPortMode;
                    port.TypeIsVisible = _editPortMode;
                }
            }
        }
        public Dictionary<string, Tuple<string, Enums.ParameterType>> NodeParameters = new Dictionary<string, Tuple<string, Enums.ParameterType>>();

        public delegate void TextChangedDelegate(string text);
        public delegate void GenericEvent();
        public event TextChangedDelegate TypeTextChanged;

        // Private fields
        private Border nodeRect = new Border();
        private StackPanel horizNodePanel = new StackPanel() { Orientation = Orientation.Horizontal };
        private StackPanel inputPortsPanel = new StackPanel();
        private StackPanel textContentPanel = new StackPanel();
        private StackPanel outputPortsPanel = new StackPanel();
        private List<string> unnamedConstructorArguments;
        private Dictionary<string, string> namedConstructorArguments;
        private VisualStyle _baseStyle;
        private int _latestPortId = -1;
        private bool _editPortMode = false;
        private Button addNewParameterButton;
        private Button addInputPortButton;
        private Button addOutputPortButton;
        private Button deleteInputPortButton;
        private Button deleteOutputPortButton;
        private StackPanel horizInputPortEditButtonsPanel = new StackPanel() { Orientation = Orientation.Horizontal, Visibility = Visibility.Collapsed };
        private StackPanel horizOutputPortEditButtonsPanel = new StackPanel() { Orientation = Orientation.Horizontal, Visibility = Visibility.Collapsed };
        private Dictionary<string, NodeNameTextBoxPair> portTexts = new Dictionary<string, NodeNameTextBoxPair>();
        private NodeNameTextBoxPair nodeTypeAndNamePair;
        
        // Ports
        private IDataFlowB<string> typeInput;
        private IDataFlowB<List<string>> typesInput;
        private IDataFlowB<string> nameInput;
        private IDataFlowB<List<Port>> portsInput;
        private IDataFlowB<List<string>> unnamedConstructorArgumentsInput;
        private IDataFlowB<Dictionary<string, string>> namedConstructorArgumentsInput;
        private IDataFlowB<Dictionary<string, string>> nodePropertiesInput;
        private IDataFlowB<Dictionary<string, Tuple<string, Enums.ParameterType>>> nodeParametersInput;
        private List<IEventHandler> nodeEventHandlers = new List<IEventHandler>();
        private List<IEventHandler> portEventHandlers = new List<IEventHandler>();
        private IEvent renderReady;
        private IUI contextMenuInput;
        private IEvent typeChanged;

        // IVisualPortGraphNode implementation
        public string Id { get; set; }
        public List<Port> Ports { get; set; } = new List<Port>();
        public Dictionary<string, FrameworkElement> PortRenders { get; set; } = new Dictionary<string, FrameworkElement>();

        public FrameworkElement Render
        {
            get => nodeRect;
            set => nodeRect = value as Border;
        }

        public double PositionX
        {
            get => WPFCanvas.GetLeft(Render);
            set
            {
                WPFCanvas.SetLeft(Render, value);
                PositionChanged?.Invoke();
            }
        }
        public double PositionY
        {
            get => WPFCanvas.GetTop(Render);
            set
            {
                WPFCanvas.SetTop(Render, value);
                PositionChanged?.Invoke();
            }
        }

        public event NodePositionChangedDelegate PositionChanged;
        public event PortConnectionRequestedDelegate PortConnectionRequested;

        /// <summary>
        /// Initialises the node's properties. If this node requires data from some JSON, Deserialise() should be called before calling Initialise().
        /// </summary>
        /// <param name="addToGraph"></param>
        public void Initialise(bool addToGraph = true)
        {
            InitialiseNodeData();
            CreateNodeVisualBase();
            AddPortsToVisualBase();

            SelectDefaultPort();

            if (addToGraph) Graph?.AddNode(this); // This should be called last
        }

        // IEvent implementation
        void IEvent.Execute()
        {
            Initialise();
        }

        // PostWiringInitialize
        private void PostWiringInitialize()
        {
            // foreach (var eventHandler in nodeEventHandlers)
            // {
            //     eventHandler.Sender = nodeRect;
            // }
        }

        // Methods
        private string GetUniquePortId()
        {
            _latestPortId++;
            return "_" + _latestPortId.ToString();
        }
        private void InitialiseNodeData()
        {
            Types = Graph.NodeTypes;
            Types.Sort();

            if (typeInput != null) Type = typeInput.Data;
            if (nameInput != null) Name = nameInput.Data;
            if (portsInput != null) Ports = portsInput.Data.Select(p => new Port(p.Serialise())).ToList(); // Deep copy of the input ports

            if (unnamedConstructorArgumentsInput != null)
            {
                unnamedConstructorArguments = unnamedConstructorArgumentsInput.Data;
            }
            else
            {
                // unnamedConstructorArguments = new List<string>() { "arg0", "arg1" };
            }

            if (namedConstructorArgumentsInput != null)
            {
                namedConstructorArguments = namedConstructorArgumentsInput.Data;
            }
            else
            {
                // namedConstructorArguments = new Dictionary<string, string>()
                // {
                //     { "named0", "val0" },
                //     { "named1", "val1" }
                // };
            }
        }

        private void CreateNodeVisualBase()
        {
            Width = NodeStyle.Width;
            Height = NodeStyle.Height;

            horizNodePanel.Children.Add(inputPortsPanel);
            horizNodePanel.Children.Add(textContentPanel);
            horizNodePanel.Children.Add(outputPortsPanel);

            // inputPortsPanel.MinWidth = PortStyle.Width;
            // outputPortsPanel.MinWidth = PortStyle.Width;

            nodeRect.Background = NodeStyle.Background;
            nodeRect.BorderBrush = NodeStyle.Border;
            nodeRect.BorderThickness = new Thickness(NodeStyle.BorderThickness);
            // nodeRect.CornerRadius = new CornerRadius(5);
            nodeRect.HorizontalAlignment = HorizontalAlignment.Center;
            nodeRect.VerticalAlignment = VerticalAlignment.Center;

            nodeRect.SizeChanged += (sender, args) =>
            {
                Width = args.NewSize.Width;
                Height = args.NewSize.Height;
            };

            if (double.IsNaN(PositionX)) PositionX = 0;
            if (double.IsNaN(PositionY)) PositionY = 0;

            AttachRenderEvents();

            nodeRect.Child = horizNodePanel;

            nodeTypeAndNamePair = new NodeNameTextBoxPair() { Type = Type, Name = Name, Types = Types, StateTransition = StateTransition, AutoPredict = true };
            textContentPanel.Children.Add(nodeTypeAndNamePair);

            nodeTypeAndNamePair.TextBoxEntered += () => StateTransition.Update(Enums.DiagramMode.TextEditing);

            nodeTypeAndNamePair.TypeTextChanged += text =>
            {
                Type = text;
                typeChanged?.Execute();
            };

            if (Type != "root")
            {
                addNewParameterButton = new Button()
                {
                    Content = "+",
                    Width = 100,
                    Height = 20,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent
                };

                addNewParameterButton.Click += (sender, args) => AddNewParamTextBoxPairRow(textContentPanel, textContentPanel.Children.Count - 1);

                textContentPanel.Children.Add(addNewParameterButton); 
            }

            // PullDataFromUI(); // Initialise NodeParameters with the data in the text fields

            textContentPanel.GotKeyboardFocus += (sender, args) =>
            {
                StateTransition.Update(Enums.DiagramMode.TextEditing);
            };

            textContentPanel.LostKeyboardFocus += (sender, args) =>
            {
                PullDataFromUI();
                // StateTransition.Update(Enums.DiagramMode.Idle);

                ActionPerformed?.Invoke(this);
            };

            if (contextMenuInput != null) nodeRect.ContextMenu = contextMenuInput.GetWPFElement() as ContextMenu; // WPF context menu, not the abstraction

        }

        private void AttachRenderEvents()
        {
            nodeRect.Loaded += (sender, args) => renderReady?.Execute(); // This is fired when nodeRect.ActualWidth and nodeRect.ActualHeight are ready

            // Reset all nodes on Idle
            // Note: If the Idle state is desired while also keeping node selection alive, use IdleSelected instead
            StateTransition.StateChanged += transition =>
            {
                if (StateTransition.CurrentStateMatches(Enums.DiagramMode.Idle))
                {
                    Unhighlight(nodeRect, NodeStyle);
                }
            };

            // Toggle select/deselect on mouse click
            nodeRect.PreviewMouseDown += (sender, args) =>
            {
                if (!Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    if (StateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected | Enums.DiagramMode.MultiNodeSelect))
                    {
                        if (!Graph.GetSelectedNodeIds().Contains(Id))
                        {
                            StateTransition.Update(Enums.DiagramMode.SingleNodeSelect);
                            Graph.DeselectAllNodes();
                            Graph.SelectNode(Id, multiSelect: true);
                        }
                    }
                }
                else
                {
                    if (!Graph.GetSelectedNodeIds().Contains(Id))
                    {
                        StateTransition.Update(Enums.DiagramMode.MultiNodeSelect);
                        Graph.SelectNode(Id, multiSelect: true);
                    }
                    else
                    {
                        Graph.DeselectNode(Id);
                    }
                }
            };

            nodeRect.MouseEnter += (sender, args) =>
            {
                if (StateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected | Enums.DiagramMode.TextEditing))
                {
                    Highlight(nodeRect, NodeStyle);
                }
            };

            nodeRect.MouseLeave += (sender, args) =>
            {
                if (StateTransition.CurrentStateMatches(Enums.DiagramMode.Idle | Enums.DiagramMode.IdleSelected | Enums.DiagramMode.TextEditing)
                    && (!Graph.GetSelectedNodeIds().Contains(Id)))
                {
                    Unhighlight(nodeRect, NodeStyle); 
                }
            };

            nodeRect.SizeChanged += (sender, args) =>
            {
                PositionChanged?.Invoke();
            };

            Point anchorPoint = new Point(0, 0);
            nodeRect.MouseLeftButtonDown += (sender, args) =>
            {
                Mouse.Capture(nodeRect);
                anchorPoint = args.GetPosition(nodeRect);
            };

            nodeRect.MouseLeftButtonUp += (sender, args) =>
            {
                Mouse.Capture(null);

                if (Graph.GetSelectedNodeIds().Count == 0)
                {
                    StateTransition.Update(Enums.DiagramMode.Idle);
                }
                else
                {
                    if (StateTransition.CurrentStateMatches(Enums.DiagramMode.SingleNodeSelect |
                                                Enums.DiagramMode.MultiNodeSelect))
                    {
                        StateTransition.Update(Enums.DiagramMode.IdleSelected);
                    }
                }
            };

            // Using a movement threshold prevents some jitters that occur when moving a node
            var movementThresholdReached = false;
            nodeRect.MouseLeftButtonUp += (sender, args) => movementThresholdReached = false;

            nodeRect.MouseMove += (sender, args) =>
            {
                if (Mouse.LeftButton == MouseButtonState.Pressed && Mouse.Captured != null && Mouse.Captured.Equals(nodeRect))
                {
                    Point position = args.GetPosition(Graph.MainCanvas);
                    var lastX = PositionX;
                    var lastY = PositionY;

                    var newX = position.X - anchorPoint.X;
                    var newY = position.Y - anchorPoint.Y;

                    var shift = new Vector(newX - lastX, newY - lastY);

                    if (Math.Abs(shift.X) + Math.Abs(shift.Y) > 5)
                    {
                        movementThresholdReached = true;
                    }

                    if (movementThresholdReached)
                    {
                        foreach (var node in Graph.GetNodes(Graph.GetSelectedNodeIds()))
                        {
                            node.PositionX += shift.X;
                            node.PositionY += shift.Y;
                        } 
                    }

                    PositionChanged?.Invoke();
                }
            };

            textContentPanel.PreviewKeyDown += (sender, args) =>
            {
                if (args.Key == Key.Enter)
                {
                    if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt)) != 0)
                    {
                        // Add new node parameter
                        var row = AddNewParamTextBoxPairRow(textContentPanel, textContentPanel.Children.Count - 1);
                        row.Focus(field: 0);

                        args.Handled = true;
                    }
                    else if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0)
                    {
                        // Return to the main graph
                        Keyboard.ClearFocus();
                        
                        StateTransition.Update(Enums.DiagramMode.IdleSelected);
                        Graph.SelectNode(Id);

                        args.Handled = true;
                    }
                }
            };

            foreach (var eventHandler in nodeEventHandlers)
            {
                eventHandler.Sender = nodeRect;
            }
        }

        public void RefreshUI()
        {
            RefreshPortUI();
        }

        public void RefreshPortUI()
        {
            foreach (var port in Ports)
            {
                PortRenders[port.Name].Height = PortStyle.Height + port.ConnectionIds.Count * 10;
            }
        }

        private ParameterTextBoxPair AddNewParamTextBoxPairRow(
            Panel parent, 
            int index = -1, 
            string nameText = "", 
            string valueText = "", 
            Enums.ParameterType parameterType = Enums.ParameterType.Property)
        {
            var paramTextBoxPair = new ParameterTextBoxPair()
            {
                ParameterName = nameText,
                ParameterValue = valueText,
                ParameterType = parameterType,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
            };

            parent.Children.Insert(index == -1 ? parent.Children.Count : index, paramTextBoxPair);

            paramTextBoxPair.DeletionRequested += () => parent.Children.Remove(paramTextBoxPair);

            return paramTextBoxPair;
        }


        private FrameworkElement CreatePortVisual(Port port)
        {

            Border portRect = new Border()
            {
                // Width = PortStyle.Width,
                // Height = PortStyle.Height,
                Background = PortStyle.Background,
                BorderBrush = PortStyle.Border,
                BorderThickness = new Thickness(PortStyle.BorderThickness),
                ToolTip = new ToolTip() { Content = port.FullName },
                Margin = new Thickness(0, -1, 0, 0)
            };

            // var portText = new TextBlock()
            // {
            //     Text = port.Name,
            //     HorizontalAlignment = HorizontalAlignment.Center,
            //     VerticalAlignment = VerticalAlignment.Center
            // };

            var portText = new NodeNameTextBoxPair()
            {
                Type = port.Type,
                Name = port.Name,
                Types = PortTypes,
                TypeIsEditable = PortsAreEditable,
                NameIsEditable = PortsAreEditable,
                TypeIsVisible = PortsAreEditable,
                NameIsVisible = true,
                StateTransition = StateTransition
            };

            portText.TextBoxEntered += () => StateTransition.Update(Enums.DiagramMode.TextEditing);

            portRect.HorizontalAlignment = port.IsInputPort ? HorizontalAlignment.Left : HorizontalAlignment.Right;

            portText.SizeChanged += (sender, args) =>
            {
                if (args.NewSize.Width > args.PreviousSize.Width)
                {
                    portRect.Width = Math.Max(portRect.ActualWidth, portText.ActualWidth);
                }
                else
                {
                    portRect.Width = Math.Min(portRect.ActualWidth, portText.ActualWidth);
                }
            };

            portRect.Child = portText;

            portRect.PreviewMouseLeftButtonDown += (sender, args) => GetPort(port.Name, selectPort: true);

            portRect.MouseLeftButtonDown += (sender, args) =>
            {
                SelectPort(port);
            };

            portRect.MouseEnter += (sender, args) =>
            {
                if (SelectedPort != port)
                {
                    Highlight(portRect, PortStyle);
                }
            };

            portRect.MouseLeave += (sender, args) =>
            {
                if (SelectedPort != port)
                {
                    Unhighlight(portRect, PortStyle);
                }
            };

            portText.NameTextChanged += newName =>
            {
                var portVisual = PortRenders[port.Name];
                PortRenders.Remove(port.Name);
                PortRenders[newName] = portVisual;

                portTexts.Remove(port.Name);
                portTexts[newName] = portText;

                port.Name = newName;
            };

            portText.TypeTextChanged += newType =>
            {
                port.Type = newType;
            };

            portTexts[port.Name] = portText;

            foreach (var eventHandler in portEventHandlers)
            {
                eventHandler.Sender = portRect;
            }

            portRect.MouseEnter += (sender, args) =>
            {
                if (StateTransition.CurrentState == Enums.DiagramMode.AwaitingPortSelection) PortConnectionRequested?.Invoke(port);
            };

            portRect.MouseMove += (sender, args) =>
            {
                if (StateTransition.CurrentState == Enums.DiagramMode.AwaitingPortSelection) PortConnectionRequested?.Invoke(port);
            };

            StateTransition.StateChanged += (transition) =>
            {
                if (transition.Item2 == Enums.DiagramMode.Idle || ((transition.Item2 == Enums.DiagramMode.IdleSelected) && !Graph.GetSelectedNodeIds().Contains(Id)))
                {
                    foreach (var p in Ports)
                    {
                        DeselectPort(p);
                    }
                }
            };

            return portRect;
        }

        private void AddPortsToVisualBase()
        {
            inputPortsPanel.Children.Clear();
            outputPortsPanel.Children.Clear();
            horizInputPortEditButtonsPanel.Children.Clear();
            horizOutputPortEditButtonsPanel.Children.Clear();

            int inputPortsCounter = 0;
            int outputPortsCounter = 0;

            foreach (var port in Ports)
            {
                var portVisual = CreatePortVisual(port);
                PortRenders[port.Name] = portVisual;

                if (port.IsInputPort)
                {
                    inputPortsPanel.Children.Add(portVisual);
                    port.Index = inputPortsCounter++;
                }
                else
                {
                    outputPortsPanel.Children.Add(portVisual);
                    port.Index = outputPortsCounter++;
                }
            }

            addInputPortButton = new Button()
            {
                Content = "+",
                Width = PortStyle.Width,
                Height = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                // Visibility = PortsAreEditable ? Visibility.Visible : Visibility.Collapsed
                Visibility = Visibility.Visible
            };

            addOutputPortButton = new Button()
            {
                Content = "+",
                Width = PortStyle.Width,
                Height = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                // Visibility = PortsAreEditable ? Visibility.Visible : Visibility.Collapsed
                Visibility = Visibility.Visible
            };

            deleteInputPortButton = new Button()
            {
                Content = "-",
                Width = PortStyle.Width,
                Height = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                // Visibility = PortsAreEditable ? Visibility.Visible : Visibility.Collapsed
                Visibility = Visibility.Visible,
                Margin = new Thickness(1, 0, 0, 0)
            };

            deleteOutputPortButton = new Button()
            {
                Content = "-",
                Width = PortStyle.Width,
                Height = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                // Visibility = PortsAreEditable ? Visibility.Visible : Visibility.Collapsed
                Visibility = Visibility.Visible,
                Margin = new Thickness(1, 0, 0, 0)
            };

            horizInputPortEditButtonsPanel.Children.Add(addInputPortButton);
            horizInputPortEditButtonsPanel.Children.Add(deleteInputPortButton);

            horizOutputPortEditButtonsPanel.Children.Add(addOutputPortButton);
            horizOutputPortEditButtonsPanel.Children.Add(deleteOutputPortButton);

            inputPortsPanel.Children.Add(horizInputPortEditButtonsPanel);
            outputPortsPanel.Children.Add(horizOutputPortEditButtonsPanel);

            addInputPortButton.Click += (sender, args) =>
            {
                string name = $"p{GetUniquePortId()}";
                var port = new Port() { Type = "Port", Name = name, IsInputPort = true };
                Ports.Add(port);
                var portVisual = CreatePortVisual(port);
                inputPortsPanel.Children.Insert(inputPortsPanel.Children.Count - 1, portVisual);
                PortRenders[name] = portVisual;
            };

            addOutputPortButton.Click += (sender, args) =>
            {
                string name = $"p{GetUniquePortId()}";
                var port = new Port() { Type = "Port", Name = name, IsInputPort = false };
                Ports.Add(port);
                var portVisual = CreatePortVisual(port);
                outputPortsPanel.Children.Insert(outputPortsPanel.Children.Count - 1, portVisual);
                PortRenders[name] = portVisual;
            };
            
            deleteInputPortButton.Click += (sender, args) =>
            {
                if (inputPortsPanel.Children.Count > 1)
                {
                    var toRemove = (inputPortsPanel.Children[inputPortsPanel.Children.Count - 2] as Border).Child as NodeNameTextBoxPair;
                    inputPortsPanel.Children.RemoveAt(inputPortsPanel.Children.Count - 2);
                    DeletePort(toRemove.Name);

                }
            };

            deleteOutputPortButton.Click += (sender, args) =>
            {
                if (outputPortsPanel.Children.Count > 1)
                {
                    var toRemove = (outputPortsPanel.Children[outputPortsPanel.Children.Count - 2] as Border).Child as NodeNameTextBoxPair;
                    outputPortsPanel.Children.RemoveAt(outputPortsPanel.Children.Count - 2);
                    DeletePort(toRemove.Name);
                }
            };
        }

        public void Select()
        {
            HighlightNode();
            SelectDefaultPort();
        }

        public void Deselect()
        {
            UnhighlightNode();
            if (SelectedPort != null) DeselectPort(SelectedPort);
        }

        public void SelectPort(Port port)
        {
            if (PortRenders.ContainsKey(port.Name))
            {
                foreach (var p in Ports)
                {
                    DeselectPort(p);
                }

                Highlight(PortRenders[port.Name] as Border, PortStyle);
                SelectedPort = port;
            }
        }

        public void DeselectPort(Port port)
        {
            if (PortRenders.ContainsKey(port.Name))
            {
                Unhighlight(PortRenders[port.Name] as Border, PortStyle);
                if (SelectedPort == port) SelectedPort = null;
            }
        }

        public void SelectDefaultPort()
        {
            SelectedPort = Ports.FirstOrDefault(p => !p.IsInputPort) ?? Ports.FirstOrDefault();
            SelectPort(SelectedPort);
        }

        public Port GetPort(string identifier, bool byMatchingType = false, Predicate<Port> condition = null, bool selectPort = false)
        {
            foreach (var port in Ports)
            {
                if (condition != null && !condition(port)) continue;

                if ((byMatchingType ? port.Type : port.Name) == identifier)
                {
                    if (selectPort) SelectedPort = port;
                    return port;
                }
            }

            return null;
        }

        public void DeletePort(string name)
        {
            var port = Ports.FirstOrDefault(predicate: p => p.Name == name);
            if (port != null)
            {
                Ports.Remove(port);
                var cxnsToDelete = Graph.GetConnections().Where(cxn => cxn.SourcePort == port || cxn.DestinationPort == port);
                foreach (var cxn in cxnsToDelete)
                {
                    Graph.DeleteConnection(cxn);
                }
            }
        }

        public override string ToString()
        {
            return $"{FullName}";
        }

        public void HighlightNode() => Highlight(nodeRect, NodeStyle);
        public void UnhighlightNode() => Unhighlight(nodeRect, NodeStyle);

        private void Highlight(Border rect, VisualStyle style)
        {
            rect.Background = style.BackgroundHighlight;
            rect.BorderBrush = style.BorderHighlight;
        }

        private void Unhighlight(Border rect, VisualStyle style)
        {
            rect.Background = style.Background;
            rect.BorderBrush = style.Border;
        }

        public double GetCurrentWidth()
        {
            return nodeRect.ActualWidth;
        }

        public double GetCurrentHeight()
        {
            return nodeRect.ActualHeight;
        }

        public void Update()
        {
            PullDataFromUI();
        }

        /// <summary>
        /// Updates the internal NodeParameters based on the data in the node's text fields.
        /// </summary>
        public void PullDataFromUI()
        {
            NodeParameters.Clear();
            foreach (var row in textContentPanel.Children)
            {
                if (row is NodeNameTextBoxPair)
                {
                    var pair = row as NodeNameTextBoxPair;
                    Type = pair.Type;
                    Name = pair.Name;
                }
                if (row is ParameterTextBoxPair)
                {
                    var pair = row as ParameterTextBoxPair;
                    NodeParameters[pair.ParameterName] = Tuple.Create(pair.ParameterValue, pair.ParameterType);
                }
            }
        }

        /// <summary>
        /// Recreates the UI based on the node's internal data.
        /// </summary>
        public void RecreateUI()
        {
            var nodeNameRow = textContentPanel.Children[0] as NodeNameTextBoxPair;
            nodeNameRow.Type = Type;
            nodeNameRow.Name = Name;

            textContentPanel.Children.Clear();

            textContentPanel.Children.Add(nodeNameRow);

            foreach (var np in NodeParameters)
            {
                AddNewParamTextBoxPairRow(textContentPanel, nameText: np.Key, valueText: np.Value.Item1, parameterType: np.Value.Item2);
            }
            
            Button addNewRowButton = new Button()
            {
                Content = "+",
                Width = 100,
                Height = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent
            };

            addNewRowButton.Click += (sender, args) =>
            {
                AddNewParamTextBoxPairRow(textContentPanel, textContentPanel.Children.Count - 1);
            };

            textContentPanel.Children.Add(addNewRowButton);

            AddPortsToVisualBase();
        }

        public string Serialise(HashSet<string> excludeFields = null)
        {
            if (excludeFields == null) excludeFields = new HashSet<string>();

            JObject obj = new JObject();

            // Properties
            if (!excludeFields.Contains("Id")) obj.Add(new JProperty("Id", Id));
            if (!excludeFields.Contains("Type")) obj.Add(new JProperty("Type", Type));
            if (!excludeFields.Contains("Name")) obj.Add(new JProperty("Name", Name));
            if (!excludeFields.Contains("TreeParent")) obj.Add(new JProperty("TreeParent", Graph.GetTreeParent(Id)));

            if (!excludeFields.Contains("Ports"))
            {
                JArray portsArray = new JArray();
                foreach (var port in Ports)
                {
                    portsArray.Add(JObject.Parse(port.Serialise()));
                }

                obj.Add(new JProperty("Ports", portsArray)); 
            }

            // Text content fields
            if (!excludeFields.Contains("NodeParameters"))
            {
                JProperty parameters = new JProperty("NodeParameters", new JArray());
                foreach (var nodeParameter in NodeParameters)
                {
                    string paramName = nodeParameter.Key;
                    string paramValue = nodeParameter.Value.Item1;
                    string paramType = Enum.GetName(typeof(Enums.ParameterType), nodeParameter.Value.Item2);

                    JObject paramJObject = new JObject();
                    paramJObject.Add("Name", paramName);
                    paramJObject.Add("Value", paramValue);
                    paramJObject.Add("ParameterType", paramType);

                    (parameters.Value as JArray)?.Add(paramJObject);
                }
                
                obj.Add(parameters);
            }

            obj["Visible"] = Render.Visibility == Visibility.Visible;

            return obj.ToString();
        }

        public void Deserialise(string json, HashSet<string> excludeFields = null)
        {
            if (excludeFields == null) excludeFields = new HashSet<string>();
            Deserialise(JObject.Parse(json), excludeFields);
        }

        public void Deserialise(JObject obj, HashSet<string> excludeFields = null)
        {
            if (excludeFields == null) excludeFields = new HashSet<string>();

            try
            {
                // Properties
                if (!excludeFields.Contains("Id") && obj.ContainsKey("Id")) Id = obj.GetValue("Id")?.ToString();
                if (!excludeFields.Contains("Type") && obj.ContainsKey("Type")) Type = obj.GetValue("Type")?.ToString();
                if (!excludeFields.Contains("Name") && obj.ContainsKey("Name")) Name = obj.GetValue("Name")?.ToString();
                if (!excludeFields.Contains("TreeParent") && obj.ContainsKey("TreeParent")) Graph.SetTreeParent(Id, Graph.GetNode(obj.GetValue("TreeParent").ToString())?.Id);
                if (!excludeFields.Contains("Ports") && obj.ContainsKey("Ports"))
                {
                    var inPorts = Ports.Where(p => p.IsInputPort).ToList();
                    var inPortsIndex = 0;

                    var outPorts = Ports.Where(p => !p.IsInputPort).ToList();
                    var outPortsIndex = 0;

                    var portsArray = obj.GetValue("Ports").ToObject<JArray>();

                    var updatedPorts = new List<Port>(); // Keeps track of which ports have been updated, so that non-updated ports can be deleted

                    foreach (JObject port in portsArray)
                    {
                        var isInputPort = bool.Parse(port["IsInputPort"].ToString());

                        if (isInputPort)
                        {
                            if (inPortsIndex < inPorts.Count)
                            {
                                updatedPorts.Add(inPorts[inPortsIndex]);

                                inPorts[inPortsIndex].Type = port["Type"].ToString();
                                inPorts[inPortsIndex].Name = port["Name"].ToString();
                                inPorts[inPortsIndex].IsInputPort = true;

                                inPortsIndex++;
                            }
                            else
                            {
                                var p = new Port()
                                {
                                    Type = port["Type"].ToString(),
                                    Name = port["Name"].ToString(),
                                    IsInputPort = true
                                };

                                updatedPorts.Add(p);
                            }
                        }
                        else
                        {
                            if (outPortsIndex < outPorts.Count)
                            {
                                updatedPorts.Add(outPorts[outPortsIndex]);

                                outPorts[outPortsIndex].Type = port["Type"].ToString();
                                outPorts[outPortsIndex].Name = port["Name"].ToString();
                                outPorts[outPortsIndex].IsInputPort = false;

                                outPortsIndex++;
                            }
                            else
                            {
                                var p = new Port()
                                {
                                    Type = port["Type"].ToString(),
                                    Name = port["Name"].ToString(),
                                    IsInputPort = false
                                };

                                updatedPorts.Add(p);
                            }
                        }
                    }

                    // Remove excess ports
                    Ports = updatedPorts;
                }

                

                // Text content fields
                if (!excludeFields.Contains("NodeParameters"))
                {
                    JArray nodeParams = obj.GetValue("NodeParameters")?.ToObject<JArray>() ?? new JArray();
                    NodeParameters.Clear();
                    foreach (var nodeParam in nodeParams)
                    {
                        string paramName = nodeParam["Name"].ToString();
                        string paramValue = nodeParam["Value"].ToString();

                        string paramTypeStr = nodeParam["ParameterType"].ToString();
                        var paramBuffer = Enum.Parse(typeof(Enums.ParameterType), paramTypeStr);

                        var paramType = paramBuffer is Enums.ParameterType
                            ? (Enums.ParameterType)paramBuffer
                            : Enums.ParameterType.Property;

                        NodeParameters[paramName] = Tuple.Create(paramValue, paramType);
                    }

                }

                if (obj.ContainsKey("Visible"))
                {
                    var visible = bool.Parse(obj["Visible"].ToString());
                    Render.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
                }

                foreach (var graphPortConnection in Graph.GetConnections())
                {
                    graphPortConnection.Validate();
                } 
            }
            catch (Exception e)
            {

            }
        }

        public void GetTypeTextBoxFocus() => nodeTypeAndNamePair.FocusTypeTextBox();

        // IMemento implementation
        public string Memento { get; private set; }

        /// <summary>
        /// Returns a JSON representation of this node.
        /// </summary>
        /// <returns></returns>
        public void SaveMemento()
        {

            Memento = Serialise();
        }

        /// <summary>
        /// Populates the fields and properties in this node with the data in a given memento.
        /// </summary>
        /// <param name="memento"></param>
        public void LoadMemento(string memento)
        {
            Deserialise(memento);
        }

        public event ActionPerformedDelegate ActionPerformed;

        #region Debug Methods
        [Conditional("DEBUG")]
        private void Debug_NodeSelected()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Selected VPGN {FullName}");
            sb.AppendLine(Memento);

        }

        #endregion

        /// <summary>
        /// <para></para>
        /// </summary>
        public VisualPortGraphNode()
        {
            Id = Utilities.GetUniqueId();
        }


        /// <summary>
        /// A custom WPF control for displaying and editing the name of a node.
        /// </summary>
        public class NodeNameTextBoxPair : StackPanel
        {
            // Public fields and properties
            public string Type
            {
                get => typeTextBox.Text;
                set => typeTextBox.Dispatcher.Invoke(() => typeTextBox.Text = value);
            }

            public string Name
            {
                get => variableNameTextBox.Text;
                set => variableNameTextBox.Dispatcher.Invoke(() => variableNameTextBox.Text = value);
            }

            public StateTransition<Enums.DiagramMode> StateTransition { get; set; }

            public event TextChangedDelegate TypeTextChanged;
            public event TextChangedDelegate NameTextChanged;
            public event GenericEvent TextBoxEntered;

            public List<string> Types { get; set; }
            public List<string> CurrentlyMatchedTypes { get; set; }

            public bool TypeIsEditable { set => typeTextBox.IsEnabled = value; }

            public bool NameIsEditable { set => variableNameTextBox.IsEnabled = value; }

            public bool TypeIsVisible { set => typeTextBox.Visibility = value ? Visibility.Visible : Visibility.Collapsed; }

            public bool NameIsVisible { set => variableNameTextBox.Visibility = value ? Visibility.Visible : Visibility.Collapsed; }
            public bool AutoPredict { get; set; } = false;

            // Private fields
            private TextBox typeTextBox = new TextBox()
            {
                MinWidth = 20, 
                Background = Brushes.Transparent, 
                BorderBrush = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = FontWeights.Bold,
                Text = "<Type>"
            };

            private TextBox variableNameTextBox = new TextBox()
            {
                MinWidth = 20,
                Background = Brushes.Transparent, 
                BorderBrush = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = FontWeights.Bold
            };


            private string _entryTypeString = "";
            private string _entryNameString = "";
            private int _currentMatchIndex = 0;
            private bool _validTextKeyPressed = false;
            private string _currentPrefix = "";

            // Methods
            public void FocusTypeTextBox()
            {
                typeTextBox.Dispatcher.Invoke(() =>
                {
                    typeTextBox.Focus();
                    typeTextBox.Text = typeTextBox.Text.Trim();
                    typeTextBox.SelectAll();
                    _entryTypeString = typeTextBox.Text;
                });
            }

            public void FocusNameTextBox()
            {
                variableNameTextBox.Dispatcher.Invoke(() =>
                {
                    variableNameTextBox.Focus();
                    variableNameTextBox.Text = variableNameTextBox.Text.Trim();
                    variableNameTextBox.SelectAll();
                    _entryNameString = variableNameTextBox.Text;
                });
            }

            public void UpdateMatches(string text)
            {
                CurrentlyMatchedTypes = Types.Where(t => t.StartsWith(text)).ToList();
                _currentMatchIndex = 0;
            }

            public void AddText(TextBox textBox, string text)
            {
                textBox.Dispatcher.Invoke(() =>
                {
                    textBox.Text += text;
                });
            }

            public void UpdateText(TextBox textBox, string text)
            {
                textBox.Dispatcher.Invoke(() =>
                {
                    string currentText = textBox.Text;

                    if (!string.IsNullOrEmpty(text))
                    {
                        textBox.Text = text;
                    }
                });
            }

            /// <summary>
            /// Determines if a given key is alphanumeric or an underscore, i.e. if it is valid for a variable name.
            /// Note that this currently only supports English keyboards.
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            public bool IsValidVariableKeyChar(Key key)
            {
                bool isValid = false;

                // Is digit
                if (key >= Key.D0 && key <= Key.D9) isValid = true;

                // Is letter
                if (key >= Key.A && key <= Key.Z) isValid = true;

                // Is underscore
                if ((Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) && key == Key.OemMinus) isValid = true;

                return isValid;
            }

            public void PredictMatch(TextBox textBox, List<string> matches, int matchIndex, bool select = false)
            {
                if (matches != null && matches.Count > matchIndex)
                {
                    var match = matches[matchIndex];
                    var currentText = _currentPrefix;

                    UpdateText(textBox, matches[matchIndex]);
                    if (select)
                    {
                        textBox.Select(currentText.Length, Math.Max(match.Length - currentText.Length, 0));
                    }
                    else
                    {
                        textBox.CaretIndex = currentText.Length + match.Length;
                    }
                }
            }

            /// <summary>
            /// Safely retrieves the text from a TextBox without causing any threading issues.
            /// </summary>
            /// <param name="textBox"></param>
            /// <returns></returns>
            public string GetText(TextBox textBox) => textBox.Dispatcher.Invoke(() => textBox.Text);

            public NodeNameTextBoxPair()
            {
                Orientation = Orientation.Horizontal;
                HorizontalAlignment = HorizontalAlignment.Center;
                Children.Add(typeTextBox);
                Children.Add(variableNameTextBox);

                typeTextBox.GotKeyboardFocus += (sender, args) => TextBoxEntered?.Invoke();
                variableNameTextBox.GotKeyboardFocus += (sender, args) => TextBoxEntered?.Invoke();

                typeTextBox.PreviewKeyDown += (sender, args) =>
                {
                    if (AutoPredict && StateTransition.CurrentStateMatches(Enums.DiagramMode.TextEditing))
                    {
                        _validTextKeyPressed = IsValidVariableKeyChar(args.Key);

                        if (!_validTextKeyPressed)
                        {
                            if (args.Key == Key.Up)
                            {
                                _currentMatchIndex = Math.Max(_currentMatchIndex - 1, 0);
                                PredictMatch(typeTextBox, CurrentlyMatchedTypes, _currentMatchIndex);
                            }
                            else if (args.Key == Key.Down)
                            {
                                _currentMatchIndex = Math.Min(_currentMatchIndex + 1, CurrentlyMatchedTypes.Count - 1);
                                PredictMatch(typeTextBox, CurrentlyMatchedTypes, _currentMatchIndex);
                            }

                        } 
                    }
                };

                typeTextBox.TextChanged += (sender, args) =>
                {
                    if (AutoPredict && _validTextKeyPressed)
                    {
                        _validTextKeyPressed = false;

                        _currentPrefix = GetText(typeTextBox).Substring(0, typeTextBox.CaretIndex);
                        UpdateMatches(_currentPrefix);
                        PredictMatch(typeTextBox, CurrentlyMatchedTypes, _currentMatchIndex, select: true);
                    }

                };

                typeTextBox.GotKeyboardFocus += (sender, args) => _entryTypeString = Type;

                typeTextBox.LostKeyboardFocus += (sender, args) =>
                {
                    if (Type != _entryTypeString) TypeTextChanged?.Invoke(Type);
                };

                variableNameTextBox.GotKeyboardFocus += (sender, args) => _entryNameString = Name;

                variableNameTextBox.LostKeyboardFocus += (sender, args) =>
                {
                    if (Name != _entryNameString) NameTextChanged?.Invoke(Name);
                };

                typeTextBox.LostKeyboardFocus += (sender, args) =>
                {
                    // Type is required, alert the user if no type is provided
                    typeTextBox.Background = string.IsNullOrWhiteSpace(typeTextBox.Text) ? Brushes.Red : Brushes.Transparent;
                };
            }
        }

        /// <summary>
        /// A custom WPF control that binds a pair of TextBoxes together and outputs a bundle of their text content.
        /// </summary>
        public class ParameterTextBoxPair : StackPanel
        {
            // Public fields and properties
            public string ParameterName
            {
                get => nameTextBox.Text;
                set => nameTextBox.Text = value;
            }

            public string ParameterValue
            {
                get => valueTextBox.Text;
                set => valueTextBox.Text = value;
            }

            public Enums.ParameterType ParameterType
            {
                get => paramType;
                set
                {
                    paramType = value;
                    parameterTypeButton.Content = paramType == Enums.ParameterType.Property ? "=" : ":";
                }
            }

            public new Brush Background
            {
                set
                {
                    nameTextBox.Background = value;
                    parameterTypeButton.Background = value;
                    valueTextBox.Background = value;
                }
            }

            public new Brush BorderBrush
            {
                set
                {
                    nameTextBox.BorderBrush = value;
                    parameterTypeButton.BorderBrush = value;
                    valueTextBox.BorderBrush = value;
                }
            }

            public GenericEvent DeletionRequested;

            // Private fields
            private TextBox nameTextBox = new TextBox() { MinWidth = 20, TextAlignment = TextAlignment.Center, FontFamily = new FontFamily("Consolas"), VerticalAlignment = VerticalAlignment.Center };
            private Button parameterTypeButton = new Button() { Width = 15 };
            private TextBox valueTextBox = new TextBox() { MinWidth = 100, AcceptsReturn = true, AcceptsTab = true, FontFamily = new FontFamily("Consolas"), VerticalAlignment = VerticalAlignment.Center };
            private Enums.ParameterType paramType = Enums.ParameterType.Property;

            // Methods
            public void Focus(int field = 0)
            {
                if (field == 0)
                {
                    nameTextBox.Focus();
                }
                else if (field == 1)
                {
                    parameterTypeButton.Focus();
                }
                else if (field == 2)
                {
                    valueTextBox.Focus();
                }
            }

            public ParameterTextBoxPair()
            {
                Orientation = Orientation.Horizontal;
                Children.Add(nameTextBox);
                Children.Add(parameterTypeButton);
                Children.Add(valueTextBox);

                parameterTypeButton.Click += (sender, args) =>
                {
                    ParameterType = 
                        ParameterType == Enums.ParameterType.Property
                        ? Enums.ParameterType.Constructor
                        : Enums.ParameterType.Property;
                };

                // Track indentation
                valueTextBox.PreviewKeyDown += (sender, args) =>
                {
                    if (args.Key == Key.Return && Keyboard.Modifiers == ModifierKeys.Shift)
                    {
                        args.Handled = true; // Handle enter press here rather than externally

                        var text = ParameterValue;
                        var preText = text.Substring(0, valueTextBox.CaretIndex);
                        var postText = text.Length > valueTextBox.CaretIndex ? text.Substring(valueTextBox.CaretIndex) : "";

                        var latestLine = preText.Split(new[] {Environment.NewLine}, StringSplitOptions.None).Last();
                        var startingWhiteSpace = Regex.Match(latestLine, @"^([\s]+)").Value;

                        var indentLevel = startingWhiteSpace.Count(c => c == '\t');
                        latestLine = Environment.NewLine + new string('\t', indentLevel);

                        ParameterValue = preText + latestLine + postText;
                        valueTextBox.Dispatcher.Invoke(() => valueTextBox.CaretIndex = preText.Length + latestLine.Length);
                    }
                };

                // Delete pair when pressing backspace inside either textbox and both are empty or only contain whitespace
                nameTextBox.PreviewKeyDown += (sender, args) =>
                {
                    if (args.Key == Key.Back && string.IsNullOrWhiteSpace(ParameterName) && string.IsNullOrWhiteSpace(ParameterValue))
                    {
                        args.Handled = true;
                        DeletionRequested?.Invoke();
                    }
                };

                valueTextBox.PreviewKeyDown += (sender, args) =>
                {
                    if (args.Key == Key.Back && string.IsNullOrWhiteSpace(ParameterName) && string.IsNullOrWhiteSpace(ParameterValue))
                    {
                        args.Handled = true;
                        DeletionRequested?.Invoke();
                    }
                };

            }
        }
    }
}
