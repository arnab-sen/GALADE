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
using System.Windows.Input;
using ContextMenu = DomainAbstractions.ContextMenu;
using MenuItem = DomainAbstractions.MenuItem;

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
        public bool MovingSource { get; set; } = false;
        public bool MovingDestination { get; set; } = false;

        public ALANode Source
        {
            get => _source;
            set
            {
                UpdateEndpointEvents(_source, value);
                _source = value;
            }
        }

        public ALANode Destination
        {
            get => _destination;
            set
            {
                UpdateEndpointEvents(_destination, value);
                _destination = value;
            }
        }
        public Box SourcePort { get; set; }
        public Box DestinationPort { get; set; }
        public StateTransition<Enums.DiagramMode> StateTransition { get; set; }

        // Private fields
        private Box rootUI;
        private BezierCurve _bezier = new BezierCurve();
        private ALANode _source;
        private ALANode _destination;

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

        public void AttachEndToMouse(bool source = true, bool detach = false)
        {
            if (!detach)
            {
                MovingDestination = !source;
                MovingSource = source;

                if (source)
                {
                    Source = null;
                }
                else
                {
                    Destination = null;
                }

                Mouse.Capture(Render);
            }
            else
            {
                MovingDestination = false;
                MovingSource = false;
                if (Mouse.Captured?.Equals(Render) ?? false) Mouse.Capture(null);
            }

        }

        private void UpdateEndpointEvents(ALANode oldNode, ALANode newNode)
        {
            if (oldNode != null) oldNode.PositionChanged -= Refresh;
            if (newNode != null) newNode.PositionChanged += Refresh;
        }

        private void SetWiring()
        {
            // BEGIN AUTO-GENERATED INSTANTIATIONS
            BezierCurve curvedWire = new BezierCurve() { InstanceName = "curvedWire" };
            ToolTip wireToolTip = new ToolTip() { InstanceName = "wireToolTip", GetLabel = () => {return $"{Source?.Model.Type}{" " + Source?.Model.Name} -> {Destination?.Model.Type}{" " + Destination?.Model.Name}";} };
            MouseEvent id_5cbfa71fc2e24aaeaa254c475982d73c = new MouseEvent(eventName: "MouseEnter") { ExtractSender = input => (input as BezierCurve).Render };
            ApplyAction<object> id_e845a90275d24798a0de80974b64f28e = new ApplyAction<object>() { Lambda = input =>{var curve = input as BezierCurve;curve.Colour = Brushes.LightSkyBlue;} };
            MouseEvent id_045af4c412264be883982200a58d4860 = new MouseEvent(eventName: "MouseLeave") { ExtractSender = input => (input as BezierCurve).Render };
            ApplyAction<object> id_062f58d1b3ec48d89c22331268796edc = new ApplyAction<object>() { Lambda = input =>{var curve = input as BezierCurve;curve.Colour = Brushes.Black;} };
            ContextMenu wireContextMenu = new ContextMenu() { InstanceName = "wireContextMenu" };
            MenuItem id_38637d4ea8e5484095c44e5a277ed41d = new MenuItem(header: "Move source") {  };
            MenuItem id_e737be9060d9408480e63df4100427d6 = new MenuItem(header: "Move destination") {  };
            MouseEvent id_79c105f12b704cdab341140439f5c83c = new MouseEvent(eventName: "MouseMove") { ExtractSender = input => (input as BezierCurve).Render };
            EventLambda id_0ff77fc1747e44cf821392b22256af77 = new EventLambda() { Lambda = () =>{if (MovingSource){_bezier.Point0 = Mouse.GetPosition(Canvas);}else if (MovingDestination){_bezier.Point3 = Mouse.GetPosition(Canvas);}} };
            EventLambda id_93119877199a4e209af643d5890824d3 = new EventLambda() { Lambda = () => {AttachEndToMouse(source: true);Graph.Set("SelectedWire", this);StateTransition.Update(Enums.DiagramMode.MovingConnection);} };
            EventLambda id_e00405baf1684774bf314edcdf30e93b = new EventLambda() { Lambda = () => {AttachEndToMouse(source: false);Graph.Set("SelectedWire", this);StateTransition.Update(Enums.DiagramMode.MovingConnection);} };
            MouseButtonEvent id_5560764b7baf441989e0aa477f1dcc7d = new MouseButtonEvent(eventName: "MouseLeftButtonDown") { ExtractSender = input => (input as BezierCurve).Render };
            EventLambda id_819aef3d2edf43408e572bfc2ddab210 = new EventLambda() { Lambda = () => {AttachEndToMouse(detach: true);StateTransition.Update(Enums.DiagramMode.AwaitingPortSelection);} };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            curvedWire.WireTo(wireToolTip, "toolTip");
            curvedWire.WireTo(wireContextMenu, "contextMenu");
            curvedWire.WireTo(id_5cbfa71fc2e24aaeaa254c475982d73c, "eventHandlers");
            curvedWire.WireTo(id_045af4c412264be883982200a58d4860, "eventHandlers");
            curvedWire.WireTo(id_79c105f12b704cdab341140439f5c83c, "eventHandlers");
            curvedWire.WireTo(id_5560764b7baf441989e0aa477f1dcc7d, "eventHandlers");
            id_5cbfa71fc2e24aaeaa254c475982d73c.WireTo(id_e845a90275d24798a0de80974b64f28e, "sourceOutput");
            id_045af4c412264be883982200a58d4860.WireTo(id_062f58d1b3ec48d89c22331268796edc, "sourceOutput");
            wireContextMenu.WireTo(id_38637d4ea8e5484095c44e5a277ed41d, "children");
            wireContextMenu.WireTo(id_e737be9060d9408480e63df4100427d6, "children");
            id_38637d4ea8e5484095c44e5a277ed41d.WireTo(id_93119877199a4e209af643d5890824d3, "clickedEvent");
            id_e737be9060d9408480e63df4100427d6.WireTo(id_e00405baf1684774bf314edcdf30e93b, "clickedEvent");
            id_79c105f12b704cdab341140439f5c83c.WireTo(id_0ff77fc1747e44cf821392b22256af77, "eventHappened");
            id_5560764b7baf441989e0aa477f1dcc7d.WireTo(id_819aef3d2edf43408e572bfc2ddab210, "eventHappened");
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




















































