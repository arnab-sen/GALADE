using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;

namespace Application
{
    /// <summary>
    /// <para>Creates and adds an IPortConnection to the given VisualPortGraph. Can be started via an IEvent or sending the connection information as a tuple in an IDataFlow.</para>
    /// <para>Ports:</para>
    /// <para>1. IEvent start:</para>
    /// <para>2. IDataFlowB&lt;IPortGraphNode&gt; sourceInput:</para>
    /// <para>3. IDataFlowB&lt;IPortGraphNode&gt; destinationInput:</para>
    /// <para>4. IDataFlowB&lt;IPortGraphNode&gt; portInput:</para>
    /// <para>5. IDataFlowB&lt;Tuple&lt;VisualPortGraphNode, VisualPortGraphNode, Port, Port&gt;&gt; connectionTupleInput:</para>
    /// <para>6. IEvent renderLoaded: This event fires when the connection render has been laid out by WPF.</para>
    /// </summary>
    public class AddConnectionToGraph : IEvent, IDataFlow<Tuple<VisualPortGraphNode, VisualPortGraphNode, Port, Port>>
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public VisualPortGraph Graph { get; set; }
        public StateTransition<Enums.DiagramMode> StateTransition { get; set; }
        public UndoHistory UndoHistory { get; set; }
        public IVisualPortGraphNode Source { get; set; }
        public IVisualPortGraphNode Destination { get; set; }
        public Port SourcePort { get; set; }
        public Port DestinationPort { get; set; }

        // Private fields
        
        // Ports
        private IDataFlowB<IVisualPortGraphNode> sourceInput;
        private IDataFlowB<IVisualPortGraphNode> destinationInput;
        private IDataFlowB<Port> sourcePortInput;
        private IDataFlowB<Port> destinationPortInput;
        private IEvent renderLoaded;
        private List<IEventHandler> transitionEventHandlers = new List<IEventHandler>();
        private IDataFlow<string> connectionIdOutput;

        // Input instances

        // Output instances

        // IEvent implementation
        void IEvent.Execute()
        {
            VisualPortGraphNode source = null;
            VisualPortGraphNode destination = null;
            Port sourcePort = null;

            if (sourceInput != null && sourcePortInput != null)
            {
                source = (VisualPortGraphNode)sourceInput.Data;
                sourcePort = sourcePortInput.Data;
            }

            if (destinationInput != null)
            {
                destination = (VisualPortGraphNode)destinationInput.Data;
            }

            if (source != null && destination != null && source.Id == destination.Id) return;

            AddToGraph(source, destination, sourcePort, null);

        }

        public void AddToGraph(VisualPortGraphNode source, VisualPortGraphNode destination, Port sourcePort, Port destinationPort)
        {
            if (source == null) return;

            source.GetPort(sourcePort.Name, condition: p => !p.IsInputPort);

            if (destination != null && destinationPort == null)
            {
                // Select matching port on destination
                destinationPort = destination.GetPort(sourcePort.Type, byMatchingType: true, condition: p => p.IsInputPort) ??
                                  destination.Ports.FirstOrDefault(p => p.IsInputPort); 
            }

            var sourcePositionHandler = new Action<string, Port, IPortConnection>((nodeId, port, connection) =>
            {
                var node = Graph.GetNode(nodeId);

                if (node != null)
                {
                    var portRender = node.PortRenders[port.Name];
                    var position = portRender.TranslatePoint(new Point(0, 0), node.Render);

                    position.X += node.PositionX + portRender.ActualWidth;
                    position.Y +=
                        node.PositionY
                        + portRender.ActualHeight / 2
                        + connection.SourcePort.ConnectionIds.IndexOf(connection.Id) * 5
                        - connection.SourcePort.ConnectionIds.Count * 5; 

                    connection.SourcePosition = position;  
                }
            });

            var destinationPositionHandler = new Action<string, Port, IPortConnection>((nodeId, port, connection) =>
            {
                var node = Graph.GetNode(nodeId);

                if (node != null)
                {
                    FrameworkElement portRender;
                    if (node.PortRenders.ContainsKey(port.Name))
                    {
                        portRender = node.PortRenders[port.Name];
                    }
                    else
                    {
                        portRender = node.PortRenders.Values.First();
                    }

                    var position = portRender.TranslatePoint(new Point(0, 0), node.Render);

                    position.X += node.PositionX;
                    position.Y += node.PositionY + portRender.ActualHeight / 2;

                    connection.DestinationPosition = position;  
                }
            });

            var newConnection = new PortGraphConnection()
            {
                Graph = Graph,
                StateTransition =  StateTransition,
                UndoHistory = UndoHistory,
                SourceId = source.Id,
                DestinationId = destination?.Id ?? "",
                SourcePort = sourcePort,
                DestinationPort = destinationPort ?? null
            };

            newConnection.InitialiseRender();

            newConnection.SourcePositionHandler = sourcePositionHandler;
            newConnection.DestinationPositionHandler = destinationPositionHandler;

            newConnection.Validate();

            // The following should change to accomodate manually-set positions
            if (sourcePort != null && !sourcePort.ConnectionIds.Contains(newConnection.Id))
            {
                sourcePort.ConnectionIds.Add(newConnection.Id);
            }

            if (destinationPort != null && destinationPort.ConnectionIds.Contains(newConnection.Id))
            {
                destinationPort.ConnectionIds.Add(newConnection.Id);
            }

            if (source != null) source.RefreshPortUI();
            if (destination != null) destination.RefreshPortUI();

            // Add connection when wire is manually placed onto a new source or destination
            (source as VisualPortGraphNode).PortConnectionRequested += port =>
            {
                var conn = Graph.GetConnection(Graph.GetSelectedConnectionId());

                if (conn != null)
                {
                    if (port.IsInputPort)
                    {
                        conn.ChangeDestination(source.Id, port);
                    }
                    else
                    {
                        conn.ChangeSource(source.Id, port);
                    } 
                }
            };

            if (destination != null)
            {
                (destination as VisualPortGraphNode).PortConnectionRequested += port =>
                    {
                        var conn = Graph.GetConnection(Graph.GetSelectedConnectionId());

                        if (conn != null)
                        {
                            if (port.IsInputPort)
                            {
                                conn.ChangeDestination(destination.Id, port);
                            }
                            else
                            {
                                conn.ChangeSource(destination.Id, port);
                            } 
                        }

                        StateTransition.Update(Enums.DiagramMode.Idle);
                    }; 
            }

            Graph.AddConnection(newConnection);

            newConnection.Render.Loaded += (sender, args) => renderLoaded?.Execute();

            foreach (var transitionEventHandler in transitionEventHandlers)
            {
                transitionEventHandler.Sender = newConnection;
            }

            if (destination != null) Graph.SelectNode(destination.Id);

            if (connectionIdOutput != null) connectionIdOutput.Data = newConnection.Id;
        }

        // IDataFlow<Tuple<IVisualPortGraphNode, IVisualPortGraphNode, Port, Port>> implementation
        Tuple<VisualPortGraphNode, VisualPortGraphNode, Port, Port> IDataFlow<Tuple<VisualPortGraphNode, VisualPortGraphNode, Port, Port>>.Data
        {
            get => default;
            set
            {
                AddToGraph(value.Item1, value.Item2, value.Item3, value.Item4);
            }
        }
        
        // PostWiringInitialize
        private void PostWiringInitialize()
        {

        }

        /// <summary>
        /// <para></para>
        /// </summary>
        public AddConnectionToGraph()
        {
            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR AddConnectionToGraph.xmind
            // END AUTO-GENERATED INSTANTIATIONS FOR AddConnectionToGraph.xmind
            
            // BEGIN AUTO-GENERATED WIRING FOR AddConnectionToGraph.xmind
            // END AUTO-GENERATED WIRING FOR AddConnectionToGraph.xmind
            
            // BEGIN MANUAL INSTANTIATIONS FOR AddConnectionToGraph.xmind
            // END MANUAL INSTANTIATIONS FOR AddConnectionToGraph.xmind
            
            // BEGIN MANUAL WIRING FOR AddConnectionToGraph.xmind
            // END MANUAL WIRING FOR AddConnectionToGraph.xmind
        }
    }
}
