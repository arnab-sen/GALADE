using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;
using System.Windows.Input;
using System.Windows.Shapes;

namespace Application
{
    /// <summary>
    /// <para>Creates and draws a drag rectangle that lets the user select multiple nodes. When alive, the rectangle is dynamically updated by the current mouse position on the canvas.
    /// Any selected nodes are sent as output.</para>
    /// <para>Ports:</para>
    /// <para>1. IEventHandler eventHandler:</para>
    /// <para>2. IDataFlow&lt;List&lt;IVisualPortGraphNode&gt;&gt; selectedNodesOutput:</para>
    /// </summary>
    public class DragRectMultiSelectNodes : IEventHandler, IEvent
    {
        // Public fields and properties
        public string InstanceName = "Default";
        public VisualPortGraph Graph { get; set; }
        public StateTransition<Enums.DiagramMode> StateTransition { get; set; }
        public VisualStyle DragRectStyle { get; set; } = new VisualStyle();

        // Private fields
        private EventHandlerConnector inputEventHandler = new EventHandlerConnector() { InstanceName = "inputEventHandler" };
        private Rectangle dragRect;

        // Ports
        private IDataFlow<List<IVisualPortGraphNode>> selectedNodesOutput;
        
        // Input instances
        private EventConnector clearConnector = new EventConnector() { InstanceName = "clearConnector" };
        
        // Output instances
        private Apply<List<IVisualPortGraphNode>, List<IVisualPortGraphNode>> selectedNodesOutputConnector = new Apply<List<IVisualPortGraphNode>, List<IVisualPortGraphNode>>() { InstanceName = "selectedNodesOutputConnector", Lambda = input => input };
        
        // IEventHandler implementation
        // Propagate the Sender to the event handler abstractions in the internal diagram, via the event handler connector
        public object Sender
        {
            get => default;
            set
            {
                (inputEventHandler as IEventHandler).Sender = value;
            }
        }

        public void Subscribe(string eventName, object sender)
        {
            throw new NotImplementedException();
        }

        // IEvent implementation
        void IEvent.Execute()
        {
            (clearConnector as IEvent).Execute();
        }
        
        // PostWiringInitialize
        private void PostWiringInitialize()
        {
            // Mapping to virtual ports
            Utilities.ConnectToVirtualPort(selectedNodesOutputConnector, "output", selectedNodesOutput);
            
            // IDataFlowB and IEventB event handlers
            // Send out initial values
            // (instanceNeedingInitialValue as IDataFlow<T>).Data = defaultValue;
        }
        
        // Methods
        // Need to call these here instead of in the lambdas directly due to scope issues with lambdas:
        // Lambdas are executed in the constructor if they directly reference variables outside of the lambda's scope
        // These should be moved back to the diagram once the tool supports multiline lambdas (each one can be replaced with an Operation)
        private void SetDragMode()
        {
            StateTransition.Update(Enums.DiagramMode.DragSelect);
        }

        private void CreateDragRect(Point mousePos)
        {
            if (dragRect == null)
            {
                dragRect = new Rectangle
                {
                    Fill = DragRectStyle.Background,
                    Stroke = DragRectStyle.Border,
                    StrokeThickness = DragRectStyle.BorderThickness
                };
            }

            Graph.MainCanvas.Children.Add(dragRect); 
            Canvas.SetLeft(dragRect, mousePos.X); 
            Canvas.SetTop(dragRect, mousePos.Y);
        }

        private void ResizeDragRect(Point mousePos)
        {
            dragRect.Width = Math.Abs(Canvas.GetLeft(dragRect) - mousePos.X); 
            dragRect.Height = Math.Abs(Canvas.GetTop(dragRect) - mousePos.Y);
        }

        private Rect GetBoundingRect(FrameworkElement element)
        {
            var rect = new Rect(
                new Point(Canvas.GetLeft(element), Canvas.GetTop(element)),
                new Size(element.ActualWidth, element.ActualHeight));

            return rect;
        }

        private Rect GetBoundingRect(PortGraphConnection connection)
        {
            var rect = new Rect(connection.SourcePosition, connection.DestinationPosition);
            
            return rect;
        }

        private void SelectCoveredComponents()
        {
            Graph.DeselectAllNodes();
            Graph.DeselectAllConnections();

            var rect = GetBoundingRect(dragRect);

            foreach (var node in Graph.GetNodes())
            {
                if (rect.IntersectsWith(GetBoundingRect(node.Render)))
                {
                    Graph.SelectNode(node.Id, multiSelect: true);
                }
            }

            foreach (var connection in Graph.GetConnections())
            {
                var cxnRect = GetBoundingRect(connection as PortGraphConnection);
                if (rect.IntersectsWith(cxnRect))
                {
                    Graph.SelectConnection(connection.Id, multiSelect: true);
                }
            }
        }

        private void UnhighlightAllNodes()
        {
            Graph.ApplyToAllNodes(node => (node as VisualPortGraphNode)?.UnhighlightNode());
        }

        private void ClearDragRect()
        {
            if (Graph.MainCanvas.Children.Contains(dragRect)) Graph.MainCanvas.Children.Remove(dragRect);
        }

        private void ResetDiagramMode()
        {
            if (StateTransition.CurrentState == Enums.DiagramMode.DragSelect)
            {
                if (Graph.GetSelectedNodeIds().Count > 0)
                {
                    StateTransition.Update(Enums.DiagramMode.IdleSelected);
                }
                else
                {
                    StateTransition.Update(Enums.DiagramMode.Idle);
                } 
            }
        }

        private void OutputSelectedNodes()
        {

        }

        /// <summary>
        /// <para></para>
        /// </summary>
        public DragRectMultiSelectNodes()
        {
            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR DragRectMultiSelectNodes.xmind
            ApplyAction<MouseButtonEventArgs> id_488c22d9831a4fb990439b1815639e8e = new ApplyAction<MouseButtonEventArgs>() { InstanceName = "Default", Lambda = args => {SelectCoveredComponents(); ClearDragRect(); ResetDiagramMode(); OutputSelectedNodes();} };
            ApplyAction<MouseButtonEventArgs> id_69593de862824499a1024ddc4bcedf4d = new ApplyAction<MouseButtonEventArgs>() { InstanceName = "Default", Lambda = args => {UnhighlightAllNodes(); SetDragMode(); CreateDragRect(args.GetPosition(Graph.MainCanvas));} };
            ApplyAction<MouseEventArgs> id_910efed20fc1455783b53d8f479d209c = new ApplyAction<MouseEventArgs>() { InstanceName = "Default", Lambda = args => ResizeDragRect(args.GetPosition(Graph.MainCanvas)) };
            EventLambda id_dda1e0ff448d42cba1978733e77b23d2 = new EventLambda() { InstanceName = "Default", Lambda = ClearDragRect };
            MouseButtonEvent id_0cba86660c254d07b71549a1fd5121d9 = new MouseButtonEvent("MouseLeftButtonUp" ) { InstanceName = "Default", Condition = args => StateTransition.CurrentStateMatches(Enums.DiagramMode.DragSelect) };
            MouseButtonEvent id_11fc212067fa4f87b6be4e8f63237fd6 = new MouseButtonEvent("MouseLeftButtonDown" ) { InstanceName = "Default", Condition = args => StateTransition.CurrentStateMatches(Enums.DiagramMode.Idle) };
            MouseEvent id_fe9c3ba1919f4bddba8557b812211ab0 = new MouseEvent("MouseMove" ) { InstanceName = "Default", Condition = args => StateTransition.CurrentStateMatches(Enums.DiagramMode.DragSelect) };
            // END AUTO-GENERATED INSTANTIATIONS FOR DragRectMultiSelectNodes.xmind
            
            // BEGIN AUTO-GENERATED WIRING FOR DragRectMultiSelectNodes.xmind
            inputEventHandler.WireTo(id_11fc212067fa4f87b6be4e8f63237fd6, "propagatedHandlerFanoutList"); // (@EventHandlerConnector (inputEventHandler).propagatedHandlerFanoutList) -- [IEventHandler] --> (MouseButtonEvent (id_11fc212067fa4f87b6be4e8f63237fd6).handler)
            inputEventHandler.WireTo(id_0cba86660c254d07b71549a1fd5121d9, "propagatedHandlerFanoutList"); // (@EventHandlerConnector (inputEventHandler).propagatedHandlerFanoutList) -- [IEventHandler] --> (MouseButtonEvent (id_0cba86660c254d07b71549a1fd5121d9).handler)
            inputEventHandler.WireTo(id_fe9c3ba1919f4bddba8557b812211ab0, "propagatedHandlerFanoutList"); // (@EventHandlerConnector (inputEventHandler).propagatedHandlerFanoutList) -- [IEventHandler] --> (MouseEvent (id_fe9c3ba1919f4bddba8557b812211ab0).handler)
            id_11fc212067fa4f87b6be4e8f63237fd6.WireTo(id_69593de862824499a1024ddc4bcedf4d, "argsOutput"); // (MouseButtonEvent (id_11fc212067fa4f87b6be4e8f63237fd6).argsOutput) -- [IDataFlow<MouseButtonEventArgs>] --> (ApplyAction<MouseButtonEventArgs> (id_69593de862824499a1024ddc4bcedf4d).input)
            id_0cba86660c254d07b71549a1fd5121d9.WireTo(id_488c22d9831a4fb990439b1815639e8e, "argsOutput"); // (MouseButtonEvent (id_0cba86660c254d07b71549a1fd5121d9).argsOutput) -- [IDataFlow<MouseButtonEventArgs>] --> (ApplyAction<MouseButtonEventArgs> (id_488c22d9831a4fb990439b1815639e8e).input)
            id_fe9c3ba1919f4bddba8557b812211ab0.WireTo(id_910efed20fc1455783b53d8f479d209c, "argsOutput"); // (MouseEvent (id_fe9c3ba1919f4bddba8557b812211ab0).argsOutput) -- [IDataFlow<MouseEventArgs>] --> (ApplyAction<MouseEventArgs> (id_910efed20fc1455783b53d8f479d209c).input)
            clearConnector.WireTo(id_dda1e0ff448d42cba1978733e77b23d2, "fanoutList"); // (@EventConnector (clearConnector).fanoutList) -- [IEvent] --> (EventLambda (id_dda1e0ff448d42cba1978733e77b23d2).start)
            // END AUTO-GENERATED WIRING FOR DragRectMultiSelectNodes.xmind
            
            // BEGIN MANUAL INSTANTIATIONS FOR DragRectMultiSelectNodes.xmind
            // END MANUAL INSTANTIATIONS FOR DragRectMultiSelectNodes.xmind
            
            // BEGIN MANUAL WIRING FOR DragRectMultiSelectNodes.xmind
            // END MANUAL WIRING FOR DragRectMultiSelectNodes.xmind
        }
    }
}
