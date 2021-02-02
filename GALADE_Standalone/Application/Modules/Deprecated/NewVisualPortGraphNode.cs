using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;

namespace Application
{
    /// <summary>
    /// <para>Creates a new instance of a VisualPortGraphNode.</para>
    /// <para>Ports:</para>
    /// <para>1. IEvent create:</para>
    /// <para>2. IDataFlowB&lt;string&gt; typeInput:</para>
    /// <para>3. IDataFlowB&lt;string&gt; nameInput:</para>
    /// <para>4. IDataFlowB&lt;List&lt;Port&gt;&gt; portsInput:</para>
    /// <para>5. IDataFlowB&lt;List&lt;string&gt;&gt; unnamedConstructorArgumentsInput:</para>
    /// <para>6. IDataFlowB&lt;Dictionary&lt;string,string&gt;&gt; namedConstructorArgumentsInput:</para>
    /// <para>7. IDataFlowB&lt;Dictionary&lt;string,string&gt;&gt; nodePropertiesInput:</para>
    /// <para>8. List&lt;IEventHandler&gt; nodeEventHandlers:</para>
    /// <para>9. List&lt;IEventHandler&gt; nodeEventHandlers:</para>
    /// </summary>
    public class NewVisualPortGraphNode : IEvent
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public VisualStyle NodeStyle { get; set; }
        public VisualStyle PortStyle { get; set; }
        public VisualPortGraph Graph { get; set; }
        public StateTransition<Enums.DiagramMode> StateTransition { get; set; }
        public UndoHistory UndoHistory { get; set; }

        // Private fields
        private VisualPortGraphNode newNode;
        private EventHandlerConnector nodeEventHandlerConnector = new EventHandlerConnector();
        private EventHandlerConnector portEventHandlerConnector = new EventHandlerConnector();
        
        // Ports
        private IDataFlowB<string> typeInput;
        private IDataFlowB<List<string>> typesInput;
        private IDataFlowB<string> nameInput;
        private IDataFlowB<List<Port>> portsInput;
        private IDataFlowB<List<string>> unnamedConstructorArgumentsInput;
        private IDataFlowB<Dictionary<string,string>> namedConstructorArgumentsInput;
        private IDataFlowB<Dictionary<string,string>> nodePropertiesInput;
        private IEventHandler nodeBehaviour;
        private IEventHandler portBehaviour;
        private IDataFlow<IVisualPortGraphNode> nodeOutput;
        private IEvent renderReady;
        private IUI contextMenuInput;
        private IEvent typeChanged;

        
        // Input instances
        
        // Output instances
        
        // IEvent implementation
        void IEvent.Execute()
        {
            newNode = CreateNewNode();
            Utilities.ConnectToVirtualPort(newNode, "typeInput", typeInput);
            Utilities.ConnectToVirtualPort(newNode, "typesInput", typesInput);
            Utilities.ConnectToVirtualPort(newNode, "nameInput", nameInput);
            Utilities.ConnectToVirtualPort(newNode, "portsInput", portsInput);
            Utilities.ConnectToVirtualPort(newNode, "unnamedConstructorArgumentsInput", unnamedConstructorArgumentsInput);
            Utilities.ConnectToVirtualPort(newNode, "namedConstructorArgumentsInput", namedConstructorArgumentsInput);
            Utilities.ConnectToVirtualPort(newNode, "nodePropertiesInput", nodePropertiesInput);
            Utilities.ConnectToVirtualPort(newNode, "renderReady", renderReady);
            Utilities.ConnectToVirtualPort(newNode, "contextMenuInput", contextMenuInput);
            Utilities.ConnectToVirtualPort(newNode, "typeChanged", typeChanged);

            // newNode.WireTo(nodeEventHandlerConnector, "nodeEventHandlers");
            // newNode.WireTo(portEventHandlerConnector, "portEventHandlers");
            // Utilities.ConnectToVirtualPort(nodeEventHandlerConnector, "propagatedHandler", nodeBehaviour);
            // Utilities.ConnectToVirtualPort(portEventHandlerConnector, "propagatedHandler", portBehaviour);

            newNode.ActionPerformed += (source) => UndoHistory.Push(source);

            newNode.Initialise();

            if (nodeOutput != null) nodeOutput.Data = newNode;
        }
        
        // PostWiringInitialize
        private void PostWiringInitialize()
        {
            // Mapping to virtual ports
            
            // IDataFlowB and IEventB event handlers
            
            // Send out initial values
            // (instanceNeedingInitialValue as IDataFlow<T>).Data = defaultValue;
        }

        private VisualPortGraphNode CreateNewNode()
        {
            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR NewVisualPortGraphNode.xmind
            VisualPortGraphNode newNode = new VisualPortGraphNode() { InstanceName = "newNode", Graph = Graph, StateTransition = StateTransition, NodeStyle = NodeStyle, PortStyle = PortStyle, PositionX = 400, PositionY = 200 };
            // END AUTO-GENERATED INSTANTIATIONS FOR NewVisualPortGraphNode.xmind
            
            // BEGIN AUTO-GENERATED WIRING FOR NewVisualPortGraphNode.xmind
            // END AUTO-GENERATED WIRING FOR NewVisualPortGraphNode.xmind
            
            // BEGIN MANUAL INSTANTIATIONS FOR NewVisualPortGraphNode.xmind
            // END MANUAL INSTANTIATIONS FOR NewVisualPortGraphNode.xmind
            
            // BEGIN MANUAL WIRING FOR NewVisualPortGraphNode.xmind
            // END MANUAL WIRING FOR NewVisualPortGraphNode.xmind

            return newNode;
        }
        
        /// <summary>
        /// <para></para>
        /// </summary>
        public NewVisualPortGraphNode()
        {
            
        }
    }
}
