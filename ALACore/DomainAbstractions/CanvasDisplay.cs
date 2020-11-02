using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;
using ProgrammingParadigms;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Documents;

namespace DomainAbstractions
{
    /// <summary>
    /// Contains a WPF Canvas, as well as defining several interaction events such as moving the mouse to pan and scrolling the mouse wheel up/down to zoom in/out.
    /// </summary>
    public class CanvasDisplay : IUI, IDataFlow<UIElement>, IEvent
    {
        // Properties
        public string InstanceName = "Default";
        public StateTransition<Enums.DiagramMode> StateTransition { get; set; }

        public double Height
        {
            get => backgroundCanvas.ActualHeight;
            set => backgroundCanvas.MinHeight = value;
        }

        public double Width
        {
            get => backgroundCanvas.ActualWidth;
            set => backgroundCanvas.MinWidth = value;
        }

        public bool ClipToBounds
        {
            get => backgroundCanvas.ClipToBounds;
            set => backgroundCanvas.ClipToBounds = value;
        }

        public Brush Background
        {
            get => backgroundCanvas.Background;
            set => backgroundCanvas.Background = value;
        }

        public bool Focusable
        {
            get => backgroundCanvas.Focusable;
            set => backgroundCanvas.Focusable = value;
        }

        // Private fields
        private Canvas backgroundCanvas; // Should never move
        private Canvas foregroundCanvas; // Holds all child elements and can be moved around
        private Point lastMousePosition = new Point(0, 0);
        private double currentZoomLevel = 1; // Represents the zoom multiplier for the visuals within foregroundCanvas
        private double minZoomLevel = 0.25;
        private Matrix zoomMatrix = new Matrix();
        private bool _panning = false;
        private bool _canOpenCtxMenu = true;

        // Ports
        private IDataFlow<Canvas> canvasOutput;
        private IDataFlow<Point> currentMousePositionOutput;
        private IDataFlow<string> visualTreeOutput;
        private IUI canvasContent;
        private List<IEventHandler> eventHandlers = new List<IEventHandler>();
        private IEvent canvasLoaded;
        private IEventB resetFocus;
        private IUI contextMenu;

        public CanvasDisplay()
        {
            backgroundCanvas = new System.Windows.Controls.Canvas()
            {
                MinHeight = 500,
                MinWidth = 500,
                Background = Brushes.White,
                ClipToBounds = true,
                Focusable = true
            };
        }

        private void Initialise()
        {
            if (foregroundCanvas == null)
            {
                foregroundCanvas = new System.Windows.Controls.Canvas()
                {
                    Height = 1,
                    Width = 1,
                    Background = Brushes.Transparent,
                    Focusable = true
                };

                backgroundCanvas.Children.Add(foregroundCanvas);
                // foregroundCanvas.Loaded += (sender, args) => canvasLoaded?.Execute();
            }

            if (contextMenu != null)
            {
                backgroundCanvas.ContextMenu = contextMenu.GetWPFElement() as System.Windows.Controls.ContextMenu;
                backgroundCanvas.ContextMenuOpening += (sender, args) =>
                {
                    if (!_canOpenCtxMenu) args.Handled = true;
                };
            }

            System.Windows.Controls.Canvas.SetLeft(foregroundCanvas, 0);
            System.Windows.Controls.Canvas.SetTop(foregroundCanvas, 0);

            backgroundCanvas.Focusable = true; // Ensure that the canvas can raise key press events
            backgroundCanvas.FocusVisualStyle = null; // Remove dashed line when the canvas is focused
            // backgroundCanvas.Loaded += (sender, args) => backgroundCanvas.Focus();
            // backgroundCanvas.PreviewMouseDown += (sender, args) => backgroundCanvas.Focus(); // Use PreviewMouseDown so that this happens before any normal MouseDown events
            
            // Right click is used to reset focus to the backgroundCanvas, when it is the only element found from a hit test.
            // Left click is not used here because elements like combobox dropdown menus are treated as popup windows, and don't appear in the hit test results, meaning
            // that a left click on one of those elements would be mistaken for a left click on the blank backgroundCanvas, and unintentionally resetting the focus
            backgroundCanvas.PreviewMouseRightButtonDown += (sender, args) =>
            {
                var hitTestItems = GetHitTestItems(args.GetPosition(backgroundCanvas));
                if (hitTestItems.Count == 1 && hitTestItems.Last().Equals(backgroundCanvas))
                {
                    backgroundCanvas.Focus();
                }
            };

            // backgroundCanvas.PreviewMouseLeftButtonDown += (sender, args) =>
            // {
            //     if (GetHitTestItems(args.GetPosition(backgroundCanvas)).Count == 1)
            //     {
            //         StateTransition.Update(Enums.DiagramMode.Idle);
            //     }
            // };

            StateTransition.StateChanged += transition =>
            {
                if (transition.Item2 == Enums.DiagramMode.Idle)
                {
                    backgroundCanvas.Focus();
                }
            };

            backgroundCanvas.MouseRightButtonDown += (sender, args) =>
            {
                lastMousePosition = args.GetPosition(backgroundCanvas);
                _panning = true;
                Mouse.Capture(backgroundCanvas);
                _canOpenCtxMenu = true;
            };

            backgroundCanvas.MouseRightButtonUp += (sender, args) =>
            {
                _panning = false;
                if (Mouse.Captured?.Equals(backgroundCanvas) ?? false) Mouse.Capture(null);
            };

            backgroundCanvas.MouseMove += (sender, args) =>
            {
                if (_panning)
                {
                    // Calculate relative mouse movement
                    Point currentMousePosition = args.GetPosition(backgroundCanvas);
                    double relativeMouseMovementY = currentMousePosition.Y - lastMousePosition.Y;
                    double relativeMouseMovementX = currentMousePosition.X - lastMousePosition.X;

                    // Translate foregroundCanvas by relative movement amount
                    var left = System.Windows.Controls.Canvas.GetLeft(foregroundCanvas);
                    var top = System.Windows.Controls.Canvas.GetTop(foregroundCanvas);
                    System.Windows.Controls.Canvas.SetLeft(foregroundCanvas, left + relativeMouseMovementX);
                    System.Windows.Controls.Canvas.SetTop(foregroundCanvas, top + relativeMouseMovementY);
                    Logging.Log($"Moved from {lastMousePosition} to {currentMousePosition}");

                    lastMousePosition = currentMousePosition;
                    _canOpenCtxMenu = false;
                }

                if (currentMousePositionOutput != null) currentMousePositionOutput.Data = args.GetPosition(backgroundCanvas);
                // if (visualTreeOutput != null) visualTreeOutput.Data = PrintHitTestItems(GetHitTestItems(args.GetPosition(backgroundCanvas)));

            };


            foregroundCanvas.RenderTransform = new ScaleTransform(1, 1);

            backgroundCanvas.MouseWheel += (sender, args) =>
            {
                // Check if zoom has been changed externally, in which case reset the internal zoom level
                if (!foregroundCanvas.RenderTransform.Value.Equals(zoomMatrix))
                {
                    currentZoomLevel = 1;
                }

                currentZoomLevel = Math.Max(currentZoomLevel + args.Delta * 0.001, minZoomLevel); // Standard mousewheel delta is +120/-120 per tick
                foregroundCanvas.RenderTransform = new ScaleTransform(currentZoomLevel, currentZoomLevel);
                zoomMatrix = foregroundCanvas.RenderTransform.Value;

                args.Handled = true;
            };
        }

        private List<object> GetHitTestItems(Point position)
        {
            List<object> items = new List<object>();

            VisualTreeHelper.HitTest
            (
                backgroundCanvas,
                null,
                r =>
                {
                    items.Add(r.VisualHit as object);
                    return HitTestResultBehavior.Continue;
                },
                new PointHitTestParameters(position)
            );

            return items;
        }

        /// <summary>
        /// Returns a string describing the top-down visual tree at given point
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private string PrintHitTestItems(List<object> results)
        {
            var sb = new StringBuilder();

            try
            {
                sb.Append(results.First());

                var remaining = results.Skip(1);
                foreach (object result in remaining)
                {
                    sb.Append(" <- " + result.ToString());
                }
            }
            catch (Exception e)
            {

            }

            return sb.ToString();
        }

        private void PostWiringInitialize()
        {
            foreach (var eventHandler in eventHandlers)
            {
                eventHandler.Sender = backgroundCanvas;
            }

            if (resetFocus != null) resetFocus.EventHappened += () =>
            {
                backgroundCanvas.Focus();
            };
        }

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            Initialise();
            if (canvasOutput != null) canvasOutput.Data = foregroundCanvas;
            return backgroundCanvas;
        }

        // IDataFlow<UIElement> implementation
        UIElement IDataFlow<UIElement>.Data
        {
            get => default;
            set
            {
                if (foregroundCanvas.Children.Contains(value)) foregroundCanvas.Children.Remove(value);
                foregroundCanvas.Children.Add(value);
            }
        }

        // IEvent implementation
        void IEvent.Execute()
        {
            foregroundCanvas.Children.Clear();
            if (canvasOutput != null) canvasOutput.Data = foregroundCanvas;
        }

    }
}
