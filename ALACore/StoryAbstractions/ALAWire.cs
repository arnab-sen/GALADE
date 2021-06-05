using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;
using ToolTip = DomainAbstractions.ToolTip;
using System.Windows.Input;
using ContextMenu = DomainAbstractions.ContextMenu;
using MenuItem = DomainAbstractions.MenuItem;
using Newtonsoft.Json.Linq;
using TextBox = DomainAbstractions.TextBox;

namespace StoryAbstractions
{
    public class ALAWire : IALAWire
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string Id { get; set; }
        public string Label { get; set; } = "";
        public Graph Graph { get; set; }
        public Canvas Canvas { get; set; }
        public UIElement Render { get; set; }
        public System.Windows.Controls.ContextMenu ContextMenu { get; set; }
        public bool MovingSource { get; set; } = false;
        public bool MovingDestination { get; set; } = false;
        public Brush WireColour { get; set; } = Brushes.Black;
        public Brush WireHighlightColour { get; set; } = Brushes.LightSkyBlue;
        public bool IsHighlighted { get; set; } = false;
        public JObject MetaData { get; set; }
        public int DefaultZIndex { get; set; } = 10;
        public bool IsTreeConnection { get; set; } = false;

        object IALAWire.SourceNode
        {
            get => Source;
            set => Source = value as IALANode;
        }

        public IALANode Source
        {
            get => _source;
            set
            {
                UpdateEndpointEvents(_source, value);
                _source = value;
            }
        }

        object IALAWire.DestinationNode
        {
            get => Destination;
            set => Destination = value as IALANode;
        }

        public IALANode Destination
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

        void IALAWire.SetSourceNodeAndPort(object source, object port)
        {
            Source = source as IALANode;
            SourcePortBox = port as Box;
        }

        void IALAWire.SetDestinationNodeAndPort(object destination, object port)
        {
            Destination = destination as IALANode;
            DestinationPortBox = port as Box;
        }

        public bool IsSelected { get; set; } = false;

        // Private fields
        private Box rootUI;
        private CurvedLine _bezier;
        private UIConfig _bezierConfig;
        private IALANode _source;
        private IALANode _destination;
        private StateTransition<Enums.DiagramMode> _stateTransition;
        private double _sourceX;
        private double _sourceY;
        private double _destX;
        private double _destY;
        private bool _isPreviewingSource = false;
        private bool _isPreviewingDest = false;
        private Canvas _previewCanvas = new Canvas();
        private Border _previewBlock = new Border() { Width = 500, Height = 150, Background = Brushes.White, BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) };
        private Brush _tempColour;

        // Ports

        // Methods
        public override string ToString()
        {
            return $"{Source} [{SourcePort.Type} {SourcePort.Name}] -> [{DestinationPort.Type} {DestinationPort.Name}] {Destination}";
        }

        private Point GetCanvasPosition(UIElement element) => element.TranslatePoint(new Point(0, 0), Canvas);

        /// <summary>
        /// Create the curve and add it to the canvas.
        /// </summary>
        public void Paint()
        {
            Refresh();

            Render = (_bezierConfig as IUI).GetWPFElement();

            Canvas.Children.Add(Render);
            Canvas.SetLeft(Render, 0);
            Canvas.SetTop(Render, 0);
            // Canvas.SetZIndex(Render, DefaultZIndex);

            _bezier.Colour = IsHighlighted ? WireHighlightColour : WireColour;
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
            if (!IsSelected)
            {
                IsSelected = true;
                Highlight();
                Canvas.SetZIndex(Render, 99);
                Graph.Set("SelectedWire", this);
            }
        }

        public void Deselect()
        {
            if (IsSelected)
            {
                IsSelected = false;
                Unhighlight();
                Canvas.SetZIndex(Render, DefaultZIndex); 
            }
        }

        public void ToggleSelect()
        {
            if (IsSelected)
            {
                Deselect();
            }
            else
            {
                Select();
            }
        }

        private bool PortsMatch(string portNameA, string portNameB)
        {
            if (portNameA.StartsWith("List<")) portNameA = Regex.Match(portNameA, @"(?<=List<).+(?=>)").Value;
            if (portNameB.StartsWith("List<")) portNameB = Regex.Match(portNameB, @"(?<=List<).+(?=>)").Value;

            return portNameA == portNameB;
        }

        public void Validate()
        {
            return; 

            if (Source == null || Destination == null)
            {
                ChangeColour(colour: Brushes.Red, highlightColour: Brushes.LightPink);
                return;
            }

            var sourcePortType = (Source.NodeModel as AbstractionModel).GetPort(SourcePort.Name)?.Type ?? "";
            var destinationPortType = (Destination.NodeModel as AbstractionModel).GetPort(DestinationPort.Name)?.Type ?? "";

            var validWire = !(string.IsNullOrEmpty(sourcePortType) 
                              || string.IsNullOrEmpty(destinationPortType) 
                              || !PortsMatch(sourcePortType, destinationPortType));

            var newColour = validWire ? Brushes.Black : Brushes.Red;
            var newHighlightColor = validWire ? Brushes.LightSkyBlue : Brushes.LightPink;

            ChangeColour(colour: newColour, highlightColour: newHighlightColor);

            if (IsHighlighted)
            {
                Highlight();
            }
            else
            {
                Unhighlight();
            }

        }

        public void ChangeColour(SolidColorBrush colour = null, SolidColorBrush highlightColour = null)
        {
            if (colour != null)
            {
                WireColour = colour;
                _bezier.Colour = WireColour;
            }
            if (highlightColour != null)
            {
                WireHighlightColour = highlightColour;
            }
        }

        public void AddTempHighlight(SolidColorBrush colour)
        {
            _tempColour = WireColour;
            WireColour = colour;
            _bezier.Colour = colour;
        }

        public void RemoveTempHighlight()
        {
            if (_tempColour != null) WireColour = _tempColour;
            _bezier.Colour = WireColour;
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

        private void UpdateEndpointEvents(IALANode oldNode, IALANode newNode)
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

        public string ToWireTo(JObject metaData = null)
        {
            var sb = new StringBuilder();
            var source = Source;
            var destination = Destination;
            var sourcePort = SourcePortBox.Payload as Port;
            var destinationPort = DestinationPortBox.Payload as Port;

            if (sourcePort.IsReversePort)
            {
                IALANode _temp1 = source;
                source = destination;
                destination = _temp1;

                Port _temp2 = sourcePort;
                sourcePort = destinationPort;
                destinationPort = _temp2;
            }

            sb.Append(string.IsNullOrWhiteSpace(sourcePort.Name) ? $"{source.Name}.WireTo({destination.Name});" : $"{source.Name}.WireTo({destination.Name}, \"{sourcePort.Name}\");");

            sb.Append(" /* ");

            if (metaData == null) metaData = new JObject();

            var sourceModel = (AbstractionModel)source.NodeModel;
            var destModel = (AbstractionModel)destination.NodeModel;

            if (!metaData.ContainsKey("SourceType")) metaData["SourceType"] = sourceModel.Type;
            if (!metaData.ContainsKey("SourceIsReference")) metaData["SourceIsReference"] = source.IsReferenceNode;
            if (!metaData.ContainsKey("DestinationType")) metaData["DestinationType"] = destModel.Type;
            if (!metaData.ContainsKey("DestinationIsReference")) metaData["DestinationIsReference"] = destination.IsReferenceNode;
            if (!metaData.ContainsKey("Description")) metaData["Description"] = GetDescription();
            if (!metaData.ContainsKey("SourceGenerics")) metaData["SourceGenerics"] = new JArray(sourceModel.GetGenerics());
            if (!metaData.ContainsKey("DestinationGenerics")) metaData["DestinationGenerics"] = new JArray(destModel.GetGenerics());

            sb.Append(metaData.ToString(Newtonsoft.Json.Formatting.None));

            sb.Append(" */");

            return sb.ToString();
        }

        public void OpenDescriptionEditor()
        {
            // Use a Popup to open text outside the node
            var descPopup = new Popup()
            {
                AllowsTransparency = true,
                Placement = PlacementMode.Bottom
            };

            var popupBackground = new Border()
            {
                Background = Brushes.White,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1)
            };

            var popupText = new System.Windows.Controls.TextBox()
            {
                MinWidth = 200,
                MinHeight = 50,
                AcceptsTab = true,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 500,
                MaxHeight = 200,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            popupBackground.Child = popupText;

            descPopup.Child = popupBackground;

            descPopup.Opened += (sender, args) => popupText.Text = GetDescription();
            popupText.TextChanged += (sender, args) => SetDescription(popupText.Text);

            descPopup.PlacementTarget = Render;
            descPopup.Placement = PlacementMode.MousePoint;
            descPopup.StaysOpen = false;
            descPopup.IsOpen = true;

            popupText.Focus();
        }

        private string GetDescription()
        {
            if (MetaData != null && MetaData.ContainsKey("Description")) return MetaData["Description"].Value<string>();
            return "";
        }

        private void SetDescription(string text)
        {
            if (MetaData == null) MetaData = CreateDefaultMetaData();
            MetaData["Description"] = text;
        }

        private JObject CreateDefaultMetaData()
        {
            var obj = new JObject()
            {
                ["SourceType"] = (Source.NodeModel as AbstractionModel).Type,
                ["SourceIsReference"] = Source.IsReferenceNode,
                ["DestinationType"] = (Destination.NodeModel as AbstractionModel).Type,
                ["DestinationIsReference"] = Destination.IsReferenceNode,
                ["Description"] = ""
            };

            return obj;
        }

        private void CreateWiring()
        {
            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR ALAWireUI
            CurvedLine curvedWire = new CurvedLine() {InstanceName="curvedWire"}; /* {"IsRoot":true} */
            ToolTip wireToolTip = new ToolTip() {InstanceName="wireToolTip",GetLabel=() =>{    var sb = new StringBuilder();
                var sourceModel = (Source?.NodeModel as AbstractionModel); 
                var destModel = (Destination?.NodeModel as AbstractionModel); 
                sb.Append($"{sourceModel?.Type}{" " + sourceModel?.Name} [{SourcePort?.Type ?? ""} {SourcePort?.Name ?? ""}] -> [{DestinationPort?.Type ?? ""} {DestinationPort?.Name ?? ""}] {destModel?.Type}{" " + destModel?.Name}");    if (MetaData != null && MetaData.ContainsKey("Description") && !string.IsNullOrWhiteSpace(MetaData["Description"].Value<string>()))        sb.AppendLine("\n\n" + MetaData["Description"].Value<string>());    return sb.ToString();}}; /* {"IsRoot":false} */
            MouseEvent id_bd225a8fef8e4e2c895b2e67ba4a99f6 = new MouseEvent(eventName:"MouseEnter") {InstanceName="id_bd225a8fef8e4e2c895b2e67ba4a99f6",ExtractSender=input => (input as CurvedLine).Render}; /* {"IsRoot":false} */
            MouseEvent id_b7877b330b854e33a1cb9ab810091c7f = new MouseEvent(eventName:"MouseLeave") {InstanceName="id_b7877b330b854e33a1cb9ab810091c7f",ExtractSender=input => (input as CurvedLine).Render}; /* {"IsRoot":false} */
            MouseEvent id_375a4e94d9d34270a4a028096c72ccea = new MouseEvent(eventName:"MouseMove") {InstanceName="id_375a4e94d9d34270a4a028096c72ccea",ExtractSender=input => (input as CurvedLine).Render}; /* {"IsRoot":false} */
            EventLambda id_d22091c77e774610943606a3674e7ee5 = new EventLambda() {InstanceName="id_d22091c77e774610943606a3674e7ee5",Lambda=() =>{    if (!Mouse.Captured?.Equals(Render) ?? true)        return;    if (MovingSource)    {        _bezier.Point0 = Mouse.GetPosition(Canvas);    }    else if (MovingDestination)    {        _bezier.Point3 = Mouse.GetPosition(Canvas);    }}}; /* {"IsRoot":false} */
            MouseButtonEvent id_a3bafb1880ea4ae3b2825dee844c50b1 = new MouseButtonEvent(eventName:"MouseLeftButtonDown") {InstanceName="id_a3bafb1880ea4ae3b2825dee844c50b1",ExtractSender=input => (input as CurvedLine).Render}; /* {"IsRoot":false} */
            EventLambda id_0959a4bad0bd41f4ba02c7725022dc05 = new EventLambda() {InstanceName="id_0959a4bad0bd41f4ba02c7725022dc05",Lambda=() =>{    AttachEndToMouse(detach: true);    if (StateTransition.CurrentStateMatches(Enums.DiagramMode.MovingConnection))    {        StateTransition.Update(Enums.DiagramMode.AwaitingPortSelection);    }    ToggleSelect();}}; /* {"IsRoot":false} */
            EventLambda id_5724d3f527eb4a69baaceb9929d0361c = new EventLambda() {InstanceName="id_5724d3f527eb4a69baaceb9929d0361c",Lambda=() =>{    Highlight();}}; /* {"IsRoot":false} */
            EventLambda id_f09af2cbf36c4a1f8b0f7d36707b5779 = new EventLambda() {InstanceName="id_f09af2cbf36c4a1f8b0f7d36707b5779",Lambda=() =>{    if (!IsSelected)        Unhighlight();}}; /* {"IsRoot":false} */
            UIConfig UIConfig_curvedWire = new UIConfig() {InstanceName="UIConfig_curvedWire",ToolTipShowDuration=60,ContextMenu=ContextMenu}; /* {"IsRoot":true} */
            PopupBox id_28ec5549505f4edda6b4d3837d650c99 = new PopupBox() {InstanceName="id_28ec5549505f4edda6b4d3837d650c99",GetPlacementObject=() => Render}; /* {"IsRoot":false} */
            UIFactory id_c3585d05ffb044a9ab3ae9b827a87eef = new UIFactory() {InstanceName="id_c3585d05ffb044a9ab3ae9b827a87eef",GetUIElement=() => _previewBlock}; /* {"IsRoot":false} */
            EventConnector id_89e2805960f44fbea06651d1976b7100 = new EventConnector() {InstanceName="id_89e2805960f44fbea06651d1976b7100"}; /* {"IsRoot":false} */
            EventConnector id_497a79197c1548ccb70f4a3b9daaefe9 = new EventConnector() {InstanceName="id_497a79197c1548ccb70f4a3b9daaefe9"}; /* {"IsRoot":false} */
            EventLambda id_632c53de1e7142ba80bf2a38691670a8 = new EventLambda() {InstanceName="id_632c53de1e7142ba80bf2a38691670a8",Lambda=() =>{    if (!_isPreviewingSource)        return;    _previewCanvas.Children.Remove(Source.Render);    Canvas.SetLeft(Source.Render, _sourceX);    Canvas.SetTop(Source.Render, _sourceY);    Canvas.Children.Add(Source.Render);    _isPreviewingSource = false;}}; /* {"IsRoot":false} */
            EventLambda id_0d037abe6d514666b413fcc70ccc5408 = new EventLambda() {InstanceName="id_0d037abe6d514666b413fcc70ccc5408",Lambda=() =>{    if (_isPreviewingSource)        return;    if (Canvas.Children.Contains(Source.Render))        Canvas.Children.Remove(Source.Render);    _isPreviewingSource = true;    if (!_previewCanvas.Children.Contains(Source.Render))        _previewCanvas.Children.Add(Source.Render);    _sourceX = Canvas.GetLeft(Source.Render);    _sourceY = Canvas.GetTop(Source.Render);    Canvas.SetLeft(Source.Render, 0);    Canvas.SetTop(Source.Render, 0);}}; /* {"IsRoot":false} */
            Data<bool> id_afe9b89a89b34fecaaa5798198d4a018 = new Data<bool>() {InstanceName="id_afe9b89a89b34fecaaa5798198d4a018",Lambda=() => _isPreviewingSource}; /* {"IsRoot":false} */
            IfElse id_b17b7a05e6a94403a88b872dac929eae = new IfElse() {InstanceName="id_b17b7a05e6a94403a88b872dac929eae"}; /* {"IsRoot":false} */
            FlowGate<object> id_a700ea30b2894d8f9c3ed466cedc47ab = new FlowGate<object>() {InstanceName="id_a700ea30b2894d8f9c3ed466cedc47ab",IsOpen=false}; /* {"IsRoot":false} */
            EventConnector id_dedab574624f47f6a769e36f92853c19 = new EventConnector() {InstanceName="id_dedab574624f47f6a769e36f92853c19"}; /* {"IsRoot":false} */
            MouseButtonEvent id_ae07194d93f94cdb82ddd5d90cbd3d09 = new MouseButtonEvent(eventName:"MouseRightButtonDown") {InstanceName="id_ae07194d93f94cdb82ddd5d90cbd3d09",ExtractSender=input => (input as CurvedLine).Render}; /* {"IsRoot":false} */
            EventLambda id_b21c36d6b0044b74b47304738d8299a8 = new EventLambda() {InstanceName="id_b21c36d6b0044b74b47304738d8299a8",Lambda=() => {    Select();}}; /* {"IsRoot":false} */
            // END AUTO-GENERATED INSTANTIATIONS FOR ALAWireUI

            // BEGIN AUTO-GENERATED WIRING FOR ALAWireUI
            curvedWire.WireTo(wireToolTip, "toolTip"); /* {"SourceType":"CurvedLine","SourceIsReference":false,"DestinationType":"ToolTip","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            curvedWire.WireTo(id_bd225a8fef8e4e2c895b2e67ba4a99f6, "eventHandlers"); /* {"SourceType":"CurvedLine","SourceIsReference":false,"DestinationType":"MouseEvent","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            curvedWire.WireTo(id_b7877b330b854e33a1cb9ab810091c7f, "eventHandlers"); /* {"SourceType":"CurvedLine","SourceIsReference":false,"DestinationType":"MouseEvent","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            curvedWire.WireTo(id_375a4e94d9d34270a4a028096c72ccea, "eventHandlers"); /* {"SourceType":"CurvedLine","SourceIsReference":false,"DestinationType":"MouseEvent","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            curvedWire.WireTo(id_a3bafb1880ea4ae3b2825dee844c50b1, "eventHandlers"); /* {"SourceType":"CurvedLine","SourceIsReference":false,"DestinationType":"MouseButtonEvent","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_dedab574624f47f6a769e36f92853c19.WireTo(id_d22091c77e774610943606a3674e7ee5, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_89e2805960f44fbea06651d1976b7100.WireTo(id_632c53de1e7142ba80bf2a38691670a8, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_a3bafb1880ea4ae3b2825dee844c50b1.WireTo(id_0959a4bad0bd41f4ba02c7725022dc05, "eventHappened"); /* {"SourceType":"MouseButtonEvent","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_bd225a8fef8e4e2c895b2e67ba4a99f6.WireTo(id_5724d3f527eb4a69baaceb9929d0361c, "eventHappened"); /* {"SourceType":"MouseEvent","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_497a79197c1548ccb70f4a3b9daaefe9.WireTo(id_f09af2cbf36c4a1f8b0f7d36707b5779, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            UIConfig_curvedWire.WireTo(curvedWire, "child"); /* {"SourceType":"UIConfig","SourceIsReference":false,"DestinationType":"CurvedLine","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_89e2805960f44fbea06651d1976b7100.WireTo(id_0d037abe6d514666b413fcc70ccc5408, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_b17b7a05e6a94403a88b872dac929eae.WireTo(id_28ec5549505f4edda6b4d3837d650c99, "ifOutput"); /* {"SourceType":"IfElse","SourceIsReference":false,"DestinationType":"PopupBox","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_28ec5549505f4edda6b4d3837d650c99.WireTo(id_c3585d05ffb044a9ab3ae9b827a87eef, "child"); /* {"SourceType":"PopupBox","SourceIsReference":false,"DestinationType":"UIFactory","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_a700ea30b2894d8f9c3ed466cedc47ab.WireTo(id_89e2805960f44fbea06651d1976b7100, "eventOutput"); /* {"SourceType":"FlowGate","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["object"],"DestinationGenerics":[]} */
            id_b7877b330b854e33a1cb9ab810091c7f.WireTo(id_497a79197c1548ccb70f4a3b9daaefe9, "eventHappened"); /* {"SourceType":"MouseEvent","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_89e2805960f44fbea06651d1976b7100.WireTo(id_afe9b89a89b34fecaaa5798198d4a018, "complete"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["bool"]} */
            id_afe9b89a89b34fecaaa5798198d4a018.WireTo(id_b17b7a05e6a94403a88b872dac929eae, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"IfElse","DestinationIsReference":false,"Description":"","SourceGenerics":["bool"],"DestinationGenerics":[]} */
            id_dedab574624f47f6a769e36f92853c19.WireTo(id_a700ea30b2894d8f9c3ed466cedc47ab, "fanoutList"); /* {"SourceType":"EventConnector","SourceIsReference":false,"DestinationType":"FlowGate","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":["object"]} */
            id_375a4e94d9d34270a4a028096c72ccea.WireTo(id_dedab574624f47f6a769e36f92853c19, "eventHappened"); /* {"SourceType":"MouseEvent","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            curvedWire.WireTo(id_ae07194d93f94cdb82ddd5d90cbd3d09, "eventHandlers"); /* {"SourceType":"CurvedLine","SourceIsReference":false,"DestinationType":"MouseButtonEvent","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            id_ae07194d93f94cdb82ddd5d90cbd3d09.WireTo(id_b21c36d6b0044b74b47304738d8299a8, "eventHappened"); /* {"SourceType":"MouseButtonEvent","SourceIsReference":false,"DestinationType":"EventLambda","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            // END AUTO-GENERATED WIRING FOR ALAWireUI

            _bezier = curvedWire;
            _bezierConfig = UIConfig_curvedWire;
        }

        public void Initialise()
        {
            _previewBlock.Child = _previewCanvas;

            CreateWiring();
        }

        public ALAWire()
        {
            Id = Utilities.GetUniqueId();
        }
    }
}