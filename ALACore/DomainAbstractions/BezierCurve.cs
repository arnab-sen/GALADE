using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    public class BezierCurve : IUI
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        public Brush Colour
        {
            get => _path.Stroke;
            set => _path.Stroke = value;
        }

        public double StrokeThickness
        {
            get => _path.StrokeThickness;
            set => _path.StrokeThickness = value;
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

        public UIElement Render => _path;

        // Private fields
        private Point _point1 = new Point(0, 0);
        private Point _point2 = new Point(0, 0);
        private Point _point3 = new Point(0, 0);
        private Path _path = new Path();
        private BezierSegment _bezier = new BezierSegment();

        // Ports
        private List<IEventHandler> eventHandlers = new List<IEventHandler>();

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            _bezier = new BezierSegment()
            {
                Point1 = Point1,
                Point2 = Point2,
                Point3 = Point3
            };

            _path = new Path()
            {
                Focusable = true,
                FocusVisualStyle = null,
                Stroke = Colour,
                StrokeThickness = StrokeThickness,
                Data = new PathGeometry()
                {
                    Figures = new PathFigureCollection()
                    {
                        new PathFigure()
                        {
                            Segments = new PathSegmentCollection()
                            {
                                _bezier
                            }
                        }
                    }
                }
            };

            foreach (var eventHandler in eventHandlers)
            {
                eventHandler.Sender = this;
            }

            return _path;
        }

        // Methods
        private void Update()
        {
            _bezier.Point1 = Point1;
            _bezier.Point2 = Point2;
            _bezier.Point3 = Point3;
        }

        public List<Point> GetPoints() => new List<Point>() { Point1, Point2, Point3 };

        public BezierCurve()
        {

        }
    }
}
