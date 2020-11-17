using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Libraries;
using ProgrammingParadigms;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;

namespace DomainAbstractions
{
    /// <summary>
    /// A line graphic that displays a Bezier curve.
    /// </summary>
    public class CurvedLine : IUI
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        public Brush Colour
        {
            get => _path.Stroke;
            set
            {
                _path.Stroke = value;
                _arrowCap.Stroke = value;
                _arrowCap.Fill = value;
            }
        }

        public double StrokeThickness
        {
            get => _path.StrokeThickness;
            set
            {
                _path.StrokeThickness = value;
                _arrowCap.StrokeThickness = value;
            }
        }

        public Point Point0
        {
            get => _point0;
            set
            {
                _point0 = value;
                Update();
            }
        }

        public Point Point1
        {
            get => _point1;
            set
            {
                _point1 = value;
                Update();
            }
        }

        public Point Point2
        {
            get => _point2;
            set
            {
                _point2 = value;
                Update();
            }
        }

        public Point Point3
        {
            get => _point3;
            set
            {
                _point3 = value;
                Update();
            }
        }

        public bool StartArrowHead { get; set; } = false;
        public bool EndArrowHead { get; set; } = false;

        public UIElement Render => _render;

        // Private fields
        private Point _point0 = new Point(0, 0);
        private Point _point1 = new Point(0, 0);
        private Point _point2 = new Point(0, 0);
        private Point _point3 = new Point(0, 0);
        private Canvas _render = new Canvas();
        private Path _path = new Path();
        private PathFigure _pathFigure = new PathFigure();
        private BezierSegment _bezier = new BezierSegment();
        private Polygon _arrowCap = new Polygon();
        private double _arrowCapWidth = 10;
        private double _arrowCapHeight = 15;

        // Ports
        private IUI toolTip;
        private IUI contextMenu;
        private List<IEventHandler> eventHandlers = new List<IEventHandler>();

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {

            _render.Children.Clear();

            _pathFigure.Segments = new PathSegmentCollection()
            {
                _bezier
            };

            _path = new Path()
            {
                Focusable = true,
                FocusVisualStyle = null,
                Data = new PathGeometry()
                {
                    Figures = new PathFigureCollection()
                    {
                        _pathFigure
                    }
                }
            };

            Colour = Brushes.Black;
            StrokeThickness = 3;

            foreach (var eventHandler in eventHandlers)
            {
                eventHandler.Sender = this;
            }

            if (toolTip != null) _path.ToolTip = toolTip.GetWPFElement();
            if (contextMenu != null) _path.ContextMenu = contextMenu.GetWPFElement() as System.Windows.Controls.ContextMenu;

            _render.Children.Add(_path);

            if (EndArrowHead)
            {
                var trianglePoints = new List<Point>();
                trianglePoints.Add(new Point(Point3.X - _arrowCapHeight, Point3.Y - _arrowCapWidth / 2.0));
                trianglePoints.Add(new Point(Point3.X - _arrowCapHeight, Point3.Y + _arrowCapWidth / 2.0));
                trianglePoints.Add(new Point(Point3.X, Point3.Y));
                trianglePoints.Add(new Point(Point3.X - _arrowCapHeight, Point3.Y - _arrowCapWidth / 2.0));

                _arrowCap.Points = new PointCollection(trianglePoints);

                _render.Children.Add(_arrowCap); 
            }

            Update();

            return _render;
        }

        // Methods
        private void Update()
        {
            _pathFigure.StartPoint = Point0;
            _bezier.Point1 = Point1;
            _bezier.Point2 = Point2;
            _bezier.Point3 = Point3;

            if (EndArrowHead)
            {
                Canvas.SetLeft(_arrowCap, Point3.X);
                Canvas.SetTop(_arrowCap, Point3.Y - _arrowCapWidth / 2.0);

                AlignArrowCapRotation(); 
            }
        }

        /// <summary>
        /// Rotate the arrow cap to align with the curve.
        /// </summary>
        private void AlignArrowCapRotation()
        {
            // Rotate about the connection point of the arrow cap to the curve
            double centreX = 0;
            double centreY = _arrowCapWidth / 2.0;

            // Get triangle measurements

            // Since the line is a Bezier curve, the angle that the curve is pointing with is not consistent when compared to a straight line.
            // Usually, when |Point3.X - Point0.X| >> 0, using the last segment (Point2 to the end point Point3) gives the best approximate angle of rotation.
            // However, as |Point3.X - Point0.X| gets smaller, the angle calculation using the last segment becomes more inaccurate, so for small |Point3.X - Point0.X|,
            // treating the entire curve as a straight line gives a better approximate angle.

            double x1, y1;
            // if (Math.Abs(Point3.Y - Point0.Y) > Math.Abs(Point3.X - Point0.X) * 2) 
            if (Math.Abs(Point3.X - Point0.X) < 75) 
            {
                x1 = Point0.X;
                y1 = Point0.Y; 
            }
            else
            {
                x1 = Point2.X;
                y1 = Point2.Y; 
            }

            var x2 = Point3.X;
            var y2 = Point3.Y;

            // Calculate the rotation by assuming that the start and end points form the hypotenuse of a right angled triangle
            var adjacent = Math.Abs(x2 - x1);
            var opposite = Math.Abs(y2 - y1);
            var hypotenuse = Math.Sqrt(Math.Pow(adjacent, 2) + Math.Pow(opposite, 2));
            var angle = Math.Asin(opposite / hypotenuse); // Returns angle in radians
            angle = angle * 180 / Math.PI; // Convert angle to degrees

            // Add an angular offset (90 to 270 deg) based on the current rotational quadrant
            // if (x2 > x1 && y2 > y1) then currently in the first (bottom right) quadrant, no change needed
            if (x2 < x1 && y2 > y1) // In the second (bottom left) quadrant
            {
                angle = 90 + (90 - angle); // Triangle is mirrored horizontally but not vertically, so need to negate the angle
            }
            else if (x2 < x1 && y2 < y1) // In the third (top left) quadrant
            {
                angle += 180; // Triangle is mirrored both horizontally and vertically, so the angle just needs to be offset
            }
            else if (x2 > x1 && y2 < y1) // In the fourth (top right) quadrant
            {
                angle = 270 + (90 - angle); // Triangle is mirrored vertically but not horizontally, so need to negate the angle
            }

            if (_arrowCap.RenderTransform != null && _arrowCap.RenderTransform is RotateTransform transform)
            {
                transform.Angle = angle;
                transform.CenterX = centreX;
                transform.CenterY = centreY;
            }
            else
            {
                _arrowCap.RenderTransform = new RotateTransform(angle, centreX, centreY);
            }
        }

        public List<Point> GetPoints() => new List<Point>() { Point1, Point2, Point3 };

        public CurvedLine()
        {

        }
    }
}
