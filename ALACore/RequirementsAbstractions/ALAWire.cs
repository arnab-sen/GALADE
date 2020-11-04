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
using ToolTip = DomainAbstractions.ToolTip;

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
            ToolTip id_0663c9c760394c43868900e499dc702f = new ToolTip() { GetLabel = () => {return $"{Source?.Model.Type}{" " + Source?.Model.Name} -> {Destination?.Model.Type}{" " + Destination?.Model.Name}";} };
            MouseEvent id_5cbfa71fc2e24aaeaa254c475982d73c = new MouseEvent(eventName: "MouseEnter") { ExtractSender = input => (input as BezierCurve).Render };
            ApplyAction<object> id_e845a90275d24798a0de80974b64f28e = new ApplyAction<object>() { Lambda = input =>{var curve = input as BezierCurve;curve.Colour = Brushes.LightSkyBlue;} };
            MouseEvent id_045af4c412264be883982200a58d4860 = new MouseEvent(eventName: "MouseLeave") { ExtractSender = input => (input as BezierCurve).Render };
            ApplyAction<object> id_062f58d1b3ec48d89c22331268796edc = new ApplyAction<object>() { Lambda = input =>{var curve = input as BezierCurve;curve.Colour = Brushes.Black;} };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            curvedWire.WireTo(id_0663c9c760394c43868900e499dc702f, "toolTip");
            curvedWire.WireTo(id_5cbfa71fc2e24aaeaa254c475982d73c, "eventHandlers");
            curvedWire.WireTo(id_045af4c412264be883982200a58d4860, "eventHandlers");
            id_5cbfa71fc2e24aaeaa254c475982d73c.WireTo(id_e845a90275d24798a0de80974b64f28e, "sourceOutput");
            id_045af4c412264be883982200a58d4860.WireTo(id_062f58d1b3ec48d89c22331268796edc, "sourceOutput");
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












