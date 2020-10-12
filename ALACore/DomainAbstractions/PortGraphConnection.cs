using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Libraries;
using ProgrammingParadigms;
using Newtonsoft.Json.Linq;

namespace DomainAbstractions
{
    /// <summary>
    /// <para></para>
    /// <para>Ports:</para>
    /// <para>1. List&lt;IEventHandler&gt; wireEventHandlers:</para>
    /// </summary>
    public class PortGraphConnection : IPortConnection, IEvent
    {
        // Public fields and properties
        public string InstanceName = "Default";
        public VisualPortGraph Graph { get; set; }
        public StateTransition<Enums.DiagramMode> StateTransition { get; set; }
        public UndoHistory UndoHistory { get; set; }

        public SolidColorBrush Stroke
        {
            get => _stroke;
            set
            {
                _stroke = new SolidColorBrush(value.Color);
            }
        }

        public SolidColorBrush HighlightStroke
        {
            get => _highlightStroke;
            set
            {
                _highlightStroke = new SolidColorBrush(value.Color);
            }
        }

        public double Opacity
        {
            get => _opacity;
            set
            {
                _opacity = value;
                Stroke.Opacity = _opacity;
                HighlightStroke.Opacity = value;
            }
        }

        // Private fields
        private SolidColorBrush _stroke;
        private SolidColorBrush _highlightStroke;
        private double _opacity = 1.0;
        private PathFigure pathFigure = new PathFigure();
        private BezierSegment bezier;
        private Point sourcePosition = new Point(0, 0);
        private Point destinationPosition = new Point(0, 0);
        private bool selected = false;
        private Handle sourceHandle = new Handle();
        private Handle destHandle = new Handle();
        private bool sourceHandleSelected = false;
        private bool destHandleSelected = false;
        private string lastSourceId = "";
        private string lastDestId = "";
        private string _sourceId;
        private string _destId;
        private Action<string, Port, IPortConnection> _sourcePosHandler;
        private Action<string, Port, IPortConnection> _destPosHandler;
        private NodePositionChangedDelegate _sourcePosDelegate;
        private NodePositionChangedDelegate _destPosDelegate;

        // Ports
        private List<IEventHandler> wireEventHandlers = new List<IEventHandler>();
        private List<IEventHandler> transitionEventHandlers = new List<IEventHandler>();
        private IUI contextMenuInput;

        // IEvent implementation
        void IEvent.Execute()
        {
            InitialiseRender();
        }

        public void InitialiseRender()
        {
            CreateRender();
        }

        private void CreateRender()
        {
            Render = new Path()
            {
                Stroke = Brushes.Black,
                StrokeThickness = 3
            };

            Render.Focusable = true;
            Render.FocusVisualStyle = null; // Remove dashed line when the connection is focused

            pathFigure.StartPoint = sourcePosition; // Source point
            
            double middle = (sourcePosition.X + destinationPosition.X) / 2;
            
            // pathFigure.Segments = new PathSegmentCollection()
            // {
            //     new LineSegment(new Point(middle, sourcePosition.Y), true),
            //     new LineSegment(new Point(middle, destinationPosition.Y), true),
            //     new LineSegment(destinationPosition, true)
            // };
            
            pathFigure.Segments = new PathSegmentCollection()
            {
                new BezierSegment()
                {
                    Point1 = new Point(middle, sourcePosition.Y),
                    Point2 = new Point(middle, destinationPosition.Y),
                    Point3 = destinationPosition
                }
            };

            Render.Data = new PathGeometry()
            {
                Figures = new PathFigureCollection()
                {
                    pathFigure
                }
            };

            Graph.MainCanvas.Children.Add(sourceHandle.Render);
            sourceHandle.Position = sourcePosition;
            Canvas.SetZIndex(sourceHandle.Render, (int)VisualPortGraph.ZIndex.WireHandle);
            sourceHandle.Hide();

            Graph.MainCanvas.Children.Add(destHandle.Render);
            destHandle.Position = destinationPosition;
            Canvas.SetZIndex(destHandle.Render, (int)VisualPortGraph.ZIndex.WireHandle);
            destHandle.Hide();

            Render.MouseEnter += (sender, args) =>
            {
                if (!selected)
                {
                    sourceHandle.Show();
                    destHandle.Show(); 
                }
            };

            Render.MouseLeave += (sender, args) =>
            {
                if (!selected)
                {
                    sourceHandle.Hide();
                    destHandle.Hide(); 
                }
            };

            Render.MouseDown += (sender, args) =>
            {
                Select();
                args.Handled = true;
            };

            Render.KeyDown += (sender, args) =>
            {
                if (args.Key == Key.Delete) Graph.DeleteConnection(this);
            };

            sourceHandle.HandleSelected += () =>
            {
                lastSourceId = SourceId;
                SourceId = sourceHandle.Id;

                if (SourcePort != null && SourcePort.ConnectionIds.Contains(Id)) SourcePort.ConnectionIds.Remove(Id);

                Graph.SelectConnection(Id);
                StateTransition.Update(Enums.DiagramMode.MovingConnection);
            };

            sourceHandle.HandleDeselected += () =>
            {
                StateTransition.Update(Enums.DiagramMode.AwaitingPortSelection);
            };

            sourceHandle.PositionChanged += () => { SourcePosition = sourceHandle.Position; };

            destHandle.HandleSelected += () =>
            {
                lastSourceId = DestinationId;
                DestinationId = destHandle.Id;

                if (DestinationPort != null && DestinationPort.ConnectionIds.Contains(Id)) DestinationPort.ConnectionIds.Remove(Id);

                Graph.SelectConnection(Id);
                StateTransition.Update(Enums.DiagramMode.MovingConnection);
            };

            destHandle.HandleDeselected += () =>
            {
                StateTransition.Update(Enums.DiagramMode.AwaitingPortSelection);
            };

            destHandle.PositionChanged += () => { DestinationPosition = destHandle.Position; };

            if (StateTransition != null) StateTransition.StateChanged += transition =>
            {
                if (selected)
                {
                    if (StateTransition.CurrentStateMatches(Enums.DiagramMode.Idle))
                    {
                        Deselect();
                    } 
                }
            };

            if (contextMenuInput != null) Render.ContextMenu = contextMenuInput.GetWPFElement() as System.Windows.Controls.ContextMenu;

            Label toolTipLabel = new Label() { Content = this.ToString() };
            Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };
            Render.MouseEnter += (sender, args) =>
            {
                toolTipLabel.Content = this.ToString();
            };

            Render.GotFocus += (sender, args) =>
            {

            };            
            
            Render.LostFocus += (sender, args) =>
            {

            };

        }

        public void Select()
        {
            selected = true;
            if (!string.IsNullOrEmpty(Graph.GetSelectedConnectionId()) && Graph.GetSelectedConnectionId() != Id)
            {
                Graph.GetSelectedConnection().Deselect();
            }

            Graph.SelectConnection(Id, selectConnectionUI: false);
            Render.Stroke = HighlightStroke;
            StateTransition.Update(Enums.DiagramMode.SingleConnectionSelect);
            sourceHandle.Show();
            destHandle.Show();

            Render.Focus();
        }

        public void Deselect()
        {
            selected = false;
            if (Graph.GetSelectedConnectionId() == Id) Graph.DeselectConnection(Id);
            Render.Stroke = Stroke;
            sourceHandle.Hide();
            destHandle.Hide();
        }

        public string Serialise()
        {
            var obj = JObject.FromObject(new
            {
                Id,
                SourceId,
                SourcePort = SourcePort != null ? JObject.Parse(SourcePort.Serialise()) : null,
                DestinationId,
                DestinationPort = DestinationPort != null ? JObject.Parse(DestinationPort.Serialise()) : null,
                Visible = Render.Visibility == Visibility.Visible
            });

            return obj.ToString();
        }

        public void Deserialise(string memento)
        {
            var obj = JObject.Parse(memento);

            Id = obj.GetValue("Id")?.ToString() ?? Id;
            SourceId = obj.GetValue("SourceId")?.ToString() ?? SourceId;
            DestinationId = obj.GetValue("DestinationId")?.ToString() ?? DestinationId;

            if (obj.ContainsKey("SourcePort"))
            {
                SourcePort = new Port(obj.GetValue("SourcePort").ToString());
            }

            if (obj.ContainsKey("DestinationPort"))
            {
                DestinationPort = new Port(obj.GetValue("DestinationPort").ToString());
            }

            if (obj.ContainsKey("Visible"))
            {
                var visible = bool.Parse(obj["Visible"].ToString());
                Render.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        
        /// <summary>
        /// <para></para>
        /// </summary>
        public PortGraphConnection()
        {
            Id = Utilities.GetUniqueId();

            Stroke = Brushes.Black;
            HighlightStroke = Brushes.LightSkyBlue;
        }

        public string Id { get; set; }

        public string SourceId
        {
            get => _sourceId;
            set => _sourceId = value;
        }

        public string DestinationId
        {
            get => _destId;
            set => _destId = value;
        }

        public Port SourcePort { get; set; }
        public Port DestinationPort { get; set; }

        public Point SourcePosition
        {
            get => sourcePosition;
            set
            {
                sourcePosition = value;
                pathFigure.StartPoint = sourcePosition;
                double middle = (sourcePosition.X + destinationPosition.X) / 2;
                // pathFigure.Segments[0] = new LineSegment(new Point(middle, sourcePosition.Y), true);
                // pathFigure.Segments[1] = new LineSegment(new Point(middle, destinationPosition.Y), true);

                if (bezier == null)
                {
                    bezier = new BezierSegment()
                    {
                        Point1 = new Point(middle, sourcePosition.Y),
                        Point2 = new Point(middle, destinationPosition.Y),
                        Point3 = destinationPosition
                    };

                    pathFigure.Segments = new PathSegmentCollection()
                    {
                        bezier
                    };
                }
                else
                {
                    bezier.Point1 = new Point(middle, sourcePosition.Y);
                    bezier.Point2 = new Point(middle, destinationPosition.Y);
                    bezier.Point3 = destinationPosition;
                }

                sourceHandle.Position = sourcePosition;
            }
        }

        public Point DestinationPosition
        {
            get => destinationPosition;
            set
            {
                destinationPosition = value;
                double middle = (sourcePosition.X + destinationPosition.X) / 2;
                // pathFigure.Segments[0] = new LineSegment(new Point(middle, sourcePosition.Y), true);
                // pathFigure.Segments[1] = new LineSegment(new Point(middle, destinationPosition.Y), true);
                // pathFigure.Segments[2] = new LineSegment(destinationPosition, true);

                if (bezier == null)
                {
                    bezier = new BezierSegment()
                    {
                        Point1 = new Point(middle, sourcePosition.Y),
                        Point2 = new Point(middle, destinationPosition.Y),
                        Point3 = destinationPosition
                    };

                    pathFigure.Segments = new PathSegmentCollection()
                    {
                        bezier
                    };
                }
                else
                {
                    bezier.Point1 = new Point(middle, sourcePosition.Y);
                    bezier.Point2 = new Point(middle, destinationPosition.Y);
                    bezier.Point3 = destinationPosition;
                }

                destHandle.Position = destinationPosition;
            }
        }
        public Path Render { get; set; }
        public HashSet<string> SourceIds { get; set; } = new HashSet<string>();
        public HashSet<string> DestinationIds { get; set; } = new HashSet<string>();

        public Action<string, Port, IPortConnection> SourcePositionHandler
        {
            get => _sourcePosHandler;
            set
            {
                _sourcePosHandler = value;

                if (!string.IsNullOrEmpty(SourceId))
                {
                    if (_sourcePosDelegate != null) Graph.GetNode(SourceId).PositionChanged -= _sourcePosDelegate;
                    _sourcePosDelegate = () => SourcePositionHandler(SourceId, SourcePort, this);
                    Graph.GetNode(SourceId).PositionChanged += _sourcePosDelegate;

                    _sourcePosDelegate(); // Initialise
                }
            }
        }

        public Action<string, Port, IPortConnection> DestinationPositionHandler
        {
            get => _destPosHandler;
            set
            {
                _destPosHandler = value;

                if (!string.IsNullOrEmpty(DestinationId))
                {
                    if (_destPosDelegate != null) Graph.GetNode(DestinationId).PositionChanged -= _destPosDelegate;
                    _destPosDelegate = () => DestinationPositionHandler(DestinationId, DestinationPort, this);
                    Graph.GetNode(DestinationId).PositionChanged += _destPosDelegate; 

                    _destPosDelegate(); // Initialise
                }
                else
                {
                    DestinationPosition = new Point(SourcePosition.X + 100, SourcePosition.Y + 100);
                }
            }
        }

        public void ChangePoint(int index, Point newPosition, PathFigureCollection path)
        {
            
        }

        public void ChangeSource(string nodeId, Port sourcePort)
        {
            if (!Graph.Contains(Graph.GetNode(SourceId))) SourceId = lastSourceId; // When handle becomes source

            if (!string.IsNullOrEmpty(SourceId) && _sourcePosDelegate != null && Graph.Contains(Graph.GetNode(SourceId))) 
                Graph.GetNode(SourceId).PositionChanged -= _sourcePosDelegate;

            if (!SourcePort.ConnectionIds.Contains(Id)) SourcePort.ConnectionIds.Add(Id);

            SourcePort = sourcePort;
            SourceId = nodeId;

            if (_sourcePosDelegate != null && Graph.Contains(Graph.GetNode(SourceId)))
                Graph.GetNode(SourceId).PositionChanged += _sourcePosDelegate;

            Graph.GetNode(SourceId)?.RefreshUI();

            Validate();
        }

        public void ChangeDestination(string nodeId, Port destinationPort)
        {
            if (!Graph.Contains(Graph.GetNode(DestinationId))) DestinationId = lastDestId; // When handle becomes destination

            if (!string.IsNullOrEmpty(DestinationId) && _destPosDelegate != null && Graph.Contains(Graph.GetNode(DestinationId))) 
                Graph.GetNode(DestinationId).PositionChanged -= _destPosDelegate;

            DestinationPort = destinationPort;
            DestinationId = nodeId;

            if (!DestinationPort.ConnectionIds.Contains(Id)) DestinationPort.ConnectionIds.Add(Id);

            if (_destPosDelegate == null)
            {
                _destPosDelegate = () => DestinationPositionHandler(DestinationId, DestinationPort, this);
            }

            if (_destPosDelegate != null && Graph.Contains(Graph.GetNode(DestinationId)))
            {
                Graph.GetNode(DestinationId).PositionChanged += _destPosDelegate;
            }

            Graph.GetNode(DestinationId)?.RefreshUI();

            Validate();
        }

        public void Validate()
        {
            if (SourcePort != null && DestinationPort != null)
            {
                // if (SourcePort.Type != DestinationPort.Type)
                // {
                //     Stroke = Brushes.Red;
                //     Render.Stroke = Brushes.Red;
                // }
                // else
                // {
                //     Stroke = Brushes.Black;
                //     Render.Stroke = Brushes.Black;
                // }

                sourceHandle.Hide();
                destHandle.Hide();
                Deselect();

                _sourcePosDelegate?.Invoke();
                _destPosDelegate?.Invoke();
            }
            else
            {
                Select();
                sourceHandle.Show();
                destHandle.Show();
            }

        }

        /// <summary>
        /// Selects the source handle if selectSourceHandle is true, otherwise selects the destination handle.
        /// </summary>
        /// <param name="selectSourceHandle"></param>
        public void SelectHandle(bool selectSourceHandle = true)
        {
            if (selectSourceHandle)
            {
                sourceHandle.Select();
            }
            else
            {
                destHandle.Select();
            }
        }

        public override string ToString()
        {
            if (Graph == null) return "";

            var source = Graph.GetNode(SourceId)?.ToString() ?? "null";
            var dest = Graph.GetNode(DestinationId)?.ToString() ?? "null";

            return $"{source} --> {dest}";
        }

        private void PostWiringInitialize()
        {
            foreach (var transitionEventHandler in transitionEventHandlers)
            {
                transitionEventHandler.Sender = this;
            }
        }

        public delegate void HandleSelectedDelegate();
        public class Handle
        {
            // Public fields and properties
            public string Id { get; set; }
            public Ellipse Render { get; set; }
            public double Width { get; set; } = 10;
            public double Height { get; set; } = 10;
            public bool IsSelected { get; set; } = false;
            public HandleSelectedDelegate HandleSelected;
            public HandleSelectedDelegate HandleDeselected;
            public NodePositionChangedDelegate PositionChanged;

            // Want the handle circle to be centred at this position
            public Point Position
            {
                get => new Point(Canvas.GetLeft(Render), Canvas.GetTop(Render));
                set
                {
                    Canvas.SetLeft(Render, value.X - Width / 2);
                    Canvas.SetTop(Render, value.Y - Height / 2);
                }
            }

            // Private fields
            public void Show()
            {
                Render.Visibility = Visibility.Visible;
            }

            public void Hide()
            {
                Deselect();
                Render.Visibility = Visibility.Collapsed;
            }

            public void Select()
            {
                if (!IsSelected)
                {
                    Mouse.Capture(Render);
                    IsSelected = true;
                    HandleSelected?.Invoke(); 
                }
            }

            public void Deselect()
            {
                if (IsSelected)
                {
                    Mouse.Capture(null);
                    IsSelected = false;
                    HandleDeselected?.Invoke();
                }
            }

            public void MoveTo(Point point)
            {
                Position = point;
                PositionChanged?.Invoke();
            }

            public Handle()
            {
                Id = Guid.NewGuid().ToString();
                Render = new Ellipse()
                {
                    Width = Width,
                    Height = Height,
                    Fill = Brushes.White,
                    Stroke =  Brushes.Black,
                    StrokeThickness = 3
                };

                Render.PreviewMouseDown += (sender, args) =>
                {
                    if (!IsSelected)
                    {
                        Select();
                    }
                };

                Render.MouseLeftButtonUp += (sender, args) =>
                {
                    Deselect();
                };

                Render.MouseMove += (sender, args) =>
                {
                    if (IsSelected)
                    {
                        MoveTo(args.GetPosition((Canvas) VisualTreeHelper.GetParent(Render)));
                    }
                };
            }
        }
    }
}