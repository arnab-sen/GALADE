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
using Newtonsoft.Json.Linq;

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
        public Brush WireColour { get; set; } = Brushes.Black;
        public Brush WireHighlightColour { get; set; } = Brushes.LightSkyBlue;
        public bool IsHighlighted { get; set; } = false;
        public JObject MetaData { get; set; }

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
        public Box SourcePortBox { get; set; }
        public Port SourcePort => SourcePortBox?.Payload as Port;
        public Box DestinationPortBox { get; set; }
        public Port DestinationPort => DestinationPortBox?.Payload as Port;

        public StateTransition<Enums.DiagramMode> StateTransition
        {
            get => _stateTransition;
            set
            {
                _stateTransition = value;
                _stateTransition.StateChanged += transition =>
                {
                    Validate();
                };
            }
        }

        public bool Selected { get; set; } = false;

        // Private fields
        private Box rootUI;
        private CurvedLine _bezier = new CurvedLine();
        private ALANode _source;
        private ALANode _destination;
        private StateTransition<Enums.DiagramMode> _stateTransition;

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

        public void Highlight()
        {
            _bezier.Colour = WireHighlightColour;
            IsHighlighted = true;
        }

        public void Unhighlight()
        {
            _bezier.Colour = WireColour;
            IsHighlighted = false;
        }

        public Point GetAttachmentPoint(bool inputPort = false)
        {
            var point = new Point();

            if (inputPort)
            {
                if (DestinationPortBox != null)
                {
                    var portConnections = Graph.Edges.Where(e => e is ALAWire wire &&
                                                                         (wire.DestinationPortBox == DestinationPortBox)).ToList();

                    var index = portConnections.IndexOf(this);

                    var pos = GetCanvasPosition(DestinationPortBox.Render);

                    point.X = pos.X;

                    var vertDisplacement = index * 5 + 5;
                    point.Y = pos.Y + vertDisplacement;

                    if (vertDisplacement > DestinationPortBox.Height) DestinationPortBox.Height += 10; 
                }
                else
                {
                    point = Mouse.GetPosition(Canvas);
                }

            }
            else
            {
                if (SourcePortBox != null)
                {
                    var portConnections = Graph.Edges.Where(e => e is ALAWire wire &&
                                                                         (wire.SourcePortBox == SourcePortBox)).ToList();

                    var index = portConnections.IndexOf(this);

                    var pos = GetCanvasPosition(SourcePortBox.Render);

                    point.X = pos.X + SourcePortBox.Width;

                    var vertDisplacement = index * 5 + 5;
                    point.Y = pos.Y + vertDisplacement;

                    if (vertDisplacement > SourcePortBox.Height) SourcePortBox.Height += 10; 
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
            // _bezier.Point0 = GetCanvasPosition(SourcePortBox.Render);
            _bezier.Point0 = GetAttachmentPoint(inputPort: false);

            // End point
            // _bezier.Point3 = GetCanvasPosition(DestinationPortBox.Render); 
            _bezier.Point3 = GetAttachmentPoint(inputPort: true);

            var midX = (_bezier.Point0.X + _bezier.Point3.X) / 2;

            _bezier.Point1 = new Point(midX, _bezier.Point0.Y);
            _bezier.Point2 = new Point(midX, _bezier.Point3.Y);

            Validate();
        }

        public void Select()
        {
            Selected = true;
            Highlight();
        }

        public void Deselect()
        {
            Selected = false;
            Unhighlight();
        }

        public void ToggleSelect()
        {
            if (Selected)
            {
                Deselect();
            }
            else
            {
                Select();
            }
        }

        public void Validate()
        {
            return;

            if (Source == null || Destination == null)
            {
                ChangeColour(validWire: false);
                return;
            }

            var sourcePortType = Source.Model.GetPort(SourcePort.Name)?.Type ?? "";
            var destinationPortType = Destination.Model.GetPort(DestinationPort.Name)?.Type ?? "";

            if (string.IsNullOrEmpty(sourcePortType) || string.IsNullOrEmpty(destinationPortType) || sourcePortType != destinationPortType)
            {
                ChangeColour(validWire: false);
                return;
            }

            ChangeColour(validWire: true);
        }

        private void ChangeColour(bool validWire = false)
        {
            if (validWire)
            {
                WireColour = Brushes.Black;
                WireHighlightColour = Brushes.LightSkyBlue;
            }
            else
            {
                WireColour = Brushes.Red;
                WireHighlightColour = Brushes.LightPink;
            }

            if (IsHighlighted)
            {
                Highlight();
            }
            else
            {
                Unhighlight();
            }
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

        private void CreateWiring()
        {
            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR ALAWireUI
            var curvedWire = new CurvedLine() {InstanceName="curvedWire"}; /*  */
            var wireToolTip = new ToolTip() {InstanceName="wireToolTip",GetLabel=() =>{    return $"{Source?.Model.Type}{" " + Source?.Model.Name} [{SourcePort?.Type ?? ""} {SourcePort?.Name ?? ""}] -> [{DestinationPort?.Type ?? ""} {DestinationPort?.Name ?? ""}] {Destination?.Model.Type}{" " + Destination?.Model.Name}";}}; /*  */
            var id_bd225a8fef8e4e2c895b2e67ba4a99f6 = new MouseEvent(eventName:"MouseEnter") {ExtractSender=input => (input as CurvedLine).Render,InstanceName="id_bd225a8fef8e4e2c895b2e67ba4a99f6"}; /*  */
            var id_b7877b330b854e33a1cb9ab810091c7f = new MouseEvent(eventName:"MouseLeave") {ExtractSender=input => (input as CurvedLine).Render,InstanceName="id_b7877b330b854e33a1cb9ab810091c7f"}; /*  */
            var wireContextMenu = new ContextMenu() {InstanceName="wireContextMenu"}; /*  */
            var id_5a22e8db5ff94ecf8539826f46c5b735 = new MenuItem(header:"Move source") {InstanceName="id_5a22e8db5ff94ecf8539826f46c5b735"}; /*  */
            var id_262a1b5c183d4b24bf3443567697cef1 = new MenuItem(header:"Move destination") {InstanceName="id_262a1b5c183d4b24bf3443567697cef1"}; /*  */
            var id_375a4e94d9d34270a4a028096c72ccea = new MouseEvent(eventName:"MouseMove") {ExtractSender=input => (input as CurvedLine).Render,InstanceName="id_375a4e94d9d34270a4a028096c72ccea"}; /*  */
            var id_d22091c77e774610943606a3674e7ee5 = new EventLambda() {InstanceName="id_d22091c77e774610943606a3674e7ee5",Lambda=() =>{    if (!Mouse.Captured?.Equals(Render) ?? true)        return;    if (MovingSource)    {        _bezier.Point0 = Mouse.GetPosition(Canvas);    }    else if (MovingDestination)    {        _bezier.Point3 = Mouse.GetPosition(Canvas);    }}}; /*  */
            var id_4fa94caebd1040708ad83788d3477089 = new EventLambda() {InstanceName="id_4fa94caebd1040708ad83788d3477089",Lambda=() =>{    StartMoving(source: true);}}; /*  */
            var id_0f34a06bd3574531a6c9b0579dd8b56a = new EventLambda() {InstanceName="id_0f34a06bd3574531a6c9b0579dd8b56a",Lambda=() =>{    StartMoving(source: false);}}; /*  */
            var id_a3bafb1880ea4ae3b2825dee844c50b1 = new MouseButtonEvent(eventName:"MouseLeftButtonDown") {InstanceName="id_a3bafb1880ea4ae3b2825dee844c50b1",ExtractSender=input => (input as CurvedLine).Render}; /*  */
            var id_0959a4bad0bd41f4ba02c7725022dc05 = new EventLambda() {InstanceName="id_0959a4bad0bd41f4ba02c7725022dc05",Lambda=() =>{    AttachEndToMouse(detach: true);    if (StateTransition.CurrentStateMatches(Enums.DiagramMode.MovingConnection))    {        StateTransition.Update(Enums.DiagramMode.AwaitingPortSelection);    }        Graph.Set("selectedWire", this);    ToggleSelect();}}; /*  */
            var id_55239d2e49364d59a3eb3e9a5ad20def = new MenuItem(header:"Delete wire") {InstanceName="id_55239d2e49364d59a3eb3e9a5ad20def"}; /*  */
            var id_a06846997c5341ad94996d7aaf6b7e50 = new EventLambda() {InstanceName="id_a06846997c5341ad94996d7aaf6b7e50",Lambda=() =>{    Delete();}}; /*  */
            var id_5724d3f527eb4a69baaceb9929d0361c = new EventLambda() {InstanceName="id_5724d3f527eb4a69baaceb9929d0361c",Lambda=() =>{    Highlight();}}; /*  */
            var id_f09af2cbf36c4a1f8b0f7d36707b5779 = new EventLambda() {InstanceName="id_f09af2cbf36c4a1f8b0f7d36707b5779",Lambda=() =>{    if (!Selected)        Unhighlight();}}; /*  */
            // END AUTO-GENERATED INSTANTIATIONS FOR ALAWireUI

            // BEGIN AUTO-GENERATED WIRING FOR ALAWireUI
            curvedWire.WireTo(wireToolTip, "toolTip"); /* {"SourceType":"CurvedLine","SourceIsReference":false,"DestinationType":"ToolTip","DestinationIsReference":false} */
            curvedWire.WireTo(wireContextMenu, "contextMenu"); /* {"SourceType":"CurvedLine","SourceIsReference":false,"DestinationType":"ContextMenu","DestinationIsReference":false} */
            curvedWire.WireTo(id_bd225a8fef8e4e2c895b2e67ba4a99f6, "eventHandlers"); /* {"SourceType":"CurvedLine","SourceIsReference":false,"DestinationType":"MouseEvent","DestinationIsReference":false} */
            curvedWire.WireTo(id_b7877b330b854e33a1cb9ab810091c7f, "eventHandlers"); /* {"SourceType":"CurvedLine","SourceIsReference":false,"DestinationType":"MouseEvent","DestinationIsReference":false} */
            curvedWire.WireTo(id_375a4e94d9d34270a4a028096c72ccea, "eventHandlers"); /* {"SourceType":"CurvedLine","SourceIsReference":false,"DestinationType":"MouseEvent","DestinationIsReference":false} */
            curvedWire.WireTo(id_a3bafb1880ea4ae3b2825dee844c50b1, "eventHandlers"); /* {"SourceType":"CurvedLine","SourceIsReference":false,"DestinationType":"MouseButtonEvent","DestinationIsReference":false} */
            wireContextMenu.WireTo(id_5a22e8db5ff94ecf8539826f46c5b735, "children"); /* {"SourceType":"ContextMenu","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false} */
            wireContextMenu.WireTo(id_262a1b5c183d4b24bf3443567697cef1, "children"); /* {"SourceType":"ContextMenu","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false} */
            wireContextMenu.WireTo(id_55239d2e49364d59a3eb3e9a5ad20def, "children"); /* {"SourceType":"ContextMenu","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false} */
            id_5a22e8db5ff94ecf8539826f46c5b735.WireTo(id_4fa94caebd1040708ad83788d3477089, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false} */
            id_262a1b5c183d4b24bf3443567697cef1.WireTo(id_0f34a06bd3574531a6c9b0579dd8b56a, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false} */
            id_375a4e94d9d34270a4a028096c72ccea.WireTo(id_d22091c77e774610943606a3674e7ee5, "eventHappened"); /* {"SourceType":"MouseEvent","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false} */
            id_a3bafb1880ea4ae3b2825dee844c50b1.WireTo(id_0959a4bad0bd41f4ba02c7725022dc05, "eventHappened"); /* {"SourceType":"MouseButtonEvent","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false} */
            id_55239d2e49364d59a3eb3e9a5ad20def.WireTo(id_a06846997c5341ad94996d7aaf6b7e50, "clickedEvent"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false} */
            id_bd225a8fef8e4e2c895b2e67ba4a99f6.WireTo(id_5724d3f527eb4a69baaceb9929d0361c, "eventHappened"); /* {"SourceType":"MouseEvent","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false} */
            id_b7877b330b854e33a1cb9ab810091c7f.WireTo(id_f09af2cbf36c4a1f8b0f7d36707b5779, "eventHappened"); /* {"SourceType":"MouseEvent","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false} */
            // END AUTO-GENERATED WIRING FOR ALAWireUI

            _bezier = curvedWire;
        }

        public ALAWire()
        {
            Id = Utilities.GetUniqueId();

            CreateWiring();
        }
    }
}


















