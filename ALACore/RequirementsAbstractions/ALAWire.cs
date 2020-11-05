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
                if (DestinationPort != null)
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
                    point = Mouse.GetPosition(Canvas);
                }

            }
            else
            {
                if (SourcePort != null)
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
                else
                {
                    point = Mouse.GetPosition(Canvas);
                }
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

        public void StartMoving(bool source = false)
        {
            AttachEndToMouse(source: source);
            Graph.Set("SelectedWire", this);
            StateTransition.Update(Enums.DiagramMode.MovingConnection);
        }

        public void Delete(bool deleteSource = false, bool deleteDestination = false)
        {
            Graph.DeleteEdge(this);

            if (Canvas.Children.Contains(Render)) Canvas.Children.Remove(Render);

            if (deleteSource && Source != null && Graph.ContainsNode(Source)) Source.Delete(deleteSource);
            if (deleteDestination && Destination != null && Graph.ContainsNode(Destination)) Destination.Delete(deleteDestination);
        }

        private void SetWiring()
        {
            // BEGIN AUTO-GENERATED INSTANTIATIONS
            BezierCurve curvedWire = new BezierCurve() { InstanceName = "curvedWire" };
            ToolTip wireToolTip = new ToolTip() { InstanceName = "wireToolTip", GetLabel = () => {return $"{Source?.Model.Type}{" " + Source?.Model.Name} -> {Destination?.Model.Type}{" " + Destination?.Model.Name}";} };
            MouseEvent id_bd225a8fef8e4e2c895b2e67ba4a99f6 = new MouseEvent(eventName: "MouseEnter") { ExtractSender = input => (input as BezierCurve).Render };
            ApplyAction<object> id_f32929906cd24a5eb978130e10f76155 = new ApplyAction<object>() { Lambda = input =>{var curve = input as BezierCurve;curve.Colour = Brushes.LightSkyBlue;} };
            MouseEvent id_b7877b330b854e33a1cb9ab810091c7f = new MouseEvent(eventName: "MouseLeave") { ExtractSender = input => (input as BezierCurve).Render };
            ApplyAction<object> id_2f22cc567e744a88a1e63e7b495af5a6 = new ApplyAction<object>() { Lambda = input =>{var curve = input as BezierCurve;curve.Colour = Brushes.Black;} };
            ContextMenu wireContextMenu = new ContextMenu() { InstanceName = "wireContextMenu" };
            MenuItem id_5a22e8db5ff94ecf8539826f46c5b735 = new MenuItem(header: "Move source") {  };
            MenuItem id_262a1b5c183d4b24bf3443567697cef1 = new MenuItem(header: "Move destination") {  };
            MouseEvent id_375a4e94d9d34270a4a028096c72ccea = new MouseEvent(eventName: "MouseMove") { ExtractSender = input => (input as BezierCurve).Render };
            EventLambda id_d22091c77e774610943606a3674e7ee5 = new EventLambda() { Lambda = () =>{if (MovingSource){_bezier.Point0 = Mouse.GetPosition(Canvas);}else if (MovingDestination){_bezier.Point3 = Mouse.GetPosition(Canvas);}} };
            EventLambda id_4fa94caebd1040708ad83788d3477089 = new EventLambda() { Lambda = () => {StartMoving(source: true);} };
            EventLambda id_0f34a06bd3574531a6c9b0579dd8b56a = new EventLambda() { Lambda = () => {StartMoving(source: false);} };
            MouseButtonEvent id_a3bafb1880ea4ae3b2825dee844c50b1 = new MouseButtonEvent(eventName: "MouseLeftButtonDown") { ExtractSender = input => (input as BezierCurve).Render };
            EventLambda id_0959a4bad0bd41f4ba02c7725022dc05 = new EventLambda() { Lambda = () => {AttachEndToMouse(detach: true);StateTransition.Update(Enums.DiagramMode.AwaitingPortSelection);} };
            MenuItem id_55239d2e49364d59a3eb3e9a5ad20def = new MenuItem(header: "Delete wire") {  };
            EventLambda id_a06846997c5341ad94996d7aaf6b7e50 = new EventLambda() { Lambda = () =>{Delete();} };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            curvedWire.WireTo(wireToolTip, "toolTip");
            curvedWire.WireTo(wireContextMenu, "contextMenu");
            curvedWire.WireTo(id_bd225a8fef8e4e2c895b2e67ba4a99f6, "eventHandlers");
            curvedWire.WireTo(id_b7877b330b854e33a1cb9ab810091c7f, "eventHandlers");
            curvedWire.WireTo(id_375a4e94d9d34270a4a028096c72ccea, "eventHandlers");
            curvedWire.WireTo(id_a3bafb1880ea4ae3b2825dee844c50b1, "eventHandlers");
            id_bd225a8fef8e4e2c895b2e67ba4a99f6.WireTo(id_f32929906cd24a5eb978130e10f76155, "sourceOutput");
            id_b7877b330b854e33a1cb9ab810091c7f.WireTo(id_2f22cc567e744a88a1e63e7b495af5a6, "sourceOutput");
            wireContextMenu.WireTo(id_5a22e8db5ff94ecf8539826f46c5b735, "children");
            wireContextMenu.WireTo(id_262a1b5c183d4b24bf3443567697cef1, "children");
            wireContextMenu.WireTo(id_55239d2e49364d59a3eb3e9a5ad20def, "children");
            id_5a22e8db5ff94ecf8539826f46c5b735.WireTo(id_4fa94caebd1040708ad83788d3477089, "clickedEvent");
            id_262a1b5c183d4b24bf3443567697cef1.WireTo(id_0f34a06bd3574531a6c9b0579dd8b56a, "clickedEvent");
            id_375a4e94d9d34270a4a028096c72ccea.WireTo(id_d22091c77e774610943606a3674e7ee5, "eventHappened");
            id_a3bafb1880ea4ae3b2825dee844c50b1.WireTo(id_0959a4bad0bd41f4ba02c7725022dc05, "eventHappened");
            id_55239d2e49364d59a3eb3e9a5ad20def.WireTo(id_a06846997c5341ad94996d7aaf6b7e50, "clickedEvent");
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
































































