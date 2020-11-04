using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;

namespace RequirementsAbstractions
{
    public class ALAWire
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string Id { get; set; }
        public string Label { get; set; } = "";
        public Graph Graph { get; set; }
        public Canvas Canvas { get; set; }
        public UIElement Render { get; set; }
        public ALANode Source { get; set; }
        public ALANode Destination { get; set; }
        public Box SourcePort { get; set; }
        public Box DestinationPort { get; set; }

        // Private fields
        private Box rootUI;
        private BezierCurve _bezier = new BezierCurve();

        // Ports

        // Methods
        private Point GetCanvasPosition(UIElement element) => element.TranslatePoint(new Point(0, 0), Canvas);

        /// <summary>
        /// Create the curve and add it to the canvas.
        /// </summary>
        public void Paint()
        {
            Refresh();

            Render = (_bezier as IUI).GetWPFElement();

            Canvas.Children.Add(Render);
            Canvas.SetLeft(Render, 0);
            Canvas.SetTop(Render, 0);
        }

        public Point GetAttachmentPoint(bool inputPort = false)
        {
            var point = new Point();

            if (inputPort)
            {
                var portConnections = Graph.Edges.Where(e => e is ALAWire wire &&
                                                             (wire.DestinationPort == DestinationPort)).ToList();

                var index = portConnections.IndexOf(this);

                var pos = GetCanvasPosition(DestinationPort.Render);

                point.X = pos.X;

                var vertDisplacement = index * 5 + 5;
                point.Y = pos.Y + vertDisplacement;

                if (vertDisplacement > DestinationPort.Height) DestinationPort.Height += 10;

            }
            else
            {
                var portConnections = Graph.Edges.Where(e => e is ALAWire wire &&
                                                             (wire.SourcePort == SourcePort)).ToList();

                var index = portConnections.IndexOf(this);

                var pos = GetCanvasPosition(SourcePort.Render);

                point.X = pos.X + SourcePort.Width;

                var vertDisplacement = index * 5 + 5;
                point.Y = pos.Y + vertDisplacement;

                if (vertDisplacement > SourcePort.Height) SourcePort.Height += 10;
            }

            return point;
        }

        /// <summary>
        /// Have the curve check its start and end points and update accordingly.
        /// </summary>
        public void Refresh()
        {
            // Start point
            // _bezier.Point0 = GetCanvasPosition(SourcePort.Render);
            _bezier.Point0 = GetAttachmentPoint(inputPort: false);

            // End point
            // _bezier.Point3 = GetCanvasPosition(DestinationPort.Render); 
            _bezier.Point3 = GetAttachmentPoint(inputPort: true); 

            var midX = (_bezier.Point0.X + _bezier.Point0.X) / 2;

            _bezier.Point1 = new Point(midX, _bezier.Point0.Y);
            _bezier.Point2 = new Point(midX, _bezier.Point3.Y);
        }

        private void SetWiring()
        {
            // BEGIN AUTO-GENERATED INSTANTIATIONS
            BezierCurve curvedWire = new BezierCurve() { InstanceName = "curvedWire" };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            // END AUTO-GENERATED WIRING

            _bezier = curvedWire;
        }

        public ALAWire()
        {
            Id = Utilities.GetUniqueId();

            SetWiring();
        }
    }
}


