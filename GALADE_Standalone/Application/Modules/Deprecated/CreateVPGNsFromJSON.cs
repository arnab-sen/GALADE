using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;
using Newtonsoft.Json.Linq;

namespace Application
{
    /// <summary>
    /// <para>Creates VPGNs from a JSON input and adds them to the given VisualPortGraph.</para>
    /// <para>Ports:</para>
    /// <para>1. IDataFlow&lt;string&gt; jsonInput: An input JArray string of VPGN dumps. Each element in this array should be valid for VPGN.Deserialise().</para>
    /// <para>2. IUI contextMenuInput:</para>
    /// <para>3. IDataFlow&lt;List&lt;VisualPortGraphNode&gt;&gt; nodesOutput:</para>
    /// </summary>
    public class CreateVPGNsFromJSON : IDataFlow<string>
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public VisualStyle NodeStyle { get; set; }
        public VisualStyle PortStyle { get; set; }
        public VisualPortGraph Graph { get; set; }
        public StateTransition<Enums.DiagramMode> StateTransition { get; set; }
        public UndoHistory UndoHistory { get; set; }
        
        // Private fields
        
        // Ports
        private IUI contextMenuInput;
        private IDataFlow<List<VisualPortGraphNode>> nodesOutput;
        private IEvent typeChanged;

        // Input instances
        private DataFlowConnector<string> jsonInputConnector = new DataFlowConnector<string>() { InstanceName = "jsonInputConnector" };
        
        // Output instances
        private Apply<List<VisualPortGraphNode>, List<VisualPortGraphNode>> nodesOutputConnector = new Apply<List<VisualPortGraphNode>, List<VisualPortGraphNode>>() { InstanceName = "nodesOutputConnector", Lambda = input => input };
        
        // IDataFlow<string> implementation
        string IDataFlow<string>.Data
        {
            get
            {
                return default;
            }
            set
            {
                (jsonInputConnector as IDataFlow<string>).Data = value;
            }
        }

        // PostWiringInitialize
        private void PostWiringInitialize()
        {
            // Mapping to virtual ports
            Utilities.ConnectToVirtualPort(nodesOutputConnector, "output", nodesOutput);

            // IDataFlowB and IEventB event handlers
            // Send out initial values
            // (instanceNeedingInitialValue as IDataFlow<T>).Data = defaultValue;
        }

        // Methods
        private VisualPortGraphNode CreateVPGN(string memento)
        {
            var newNode = new VisualPortGraphNode()
            {
                Graph = Graph, StateTransition = StateTransition, NodeStyle = NodeStyle, PortStyle = PortStyle
            };

            if (contextMenuInput != null)
            {
                newNode.WireTo(contextMenuInput, "contextMenuInput"); 
            }

            if (typeChanged != null)
            {
                newNode.WireTo(typeChanged, "typeChanged"); 
            }

            newNode.ActionPerformed += (source) => UndoHistory.Push(source);

            newNode.Deserialise(memento, excludeFields: new HashSet<string>() { "TreeParent" });

            newNode.Initialise();

            newNode.RecreateUI();


            return newNode;
        }
        
        /// <summary>
        /// <para></para>
        /// </summary>
        public CreateVPGNsFromJSON()
        {
            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR CreateVPGNsFromJSON.xmind
            Apply<JToken,VisualPortGraphNode> id_221f6a2c3d40454f843163cb7761b912 = new Apply<JToken,VisualPortGraphNode>() { InstanceName = "Default", Lambda = jt => CreateVPGN(jt.ToString()) };
            Apply<string,IEnumerable<JToken>> id_d105b9870a7f4918a2424087f2702c47 = new Apply<string,IEnumerable<JToken>>() { InstanceName = "Default", Lambda = s => JArray.Parse(s).ToList() };
            ForEach<JToken> id_fd6166abbb7d475e9fc55e5babb07894 = new ForEach<JToken>() { InstanceName = "Default" };
            // END AUTO-GENERATED INSTANTIATIONS FOR CreateVPGNsFromJSON.xmind
            
            // BEGIN AUTO-GENERATED WIRING FOR CreateVPGNsFromJSON.xmind
            jsonInputConnector.WireTo(id_d105b9870a7f4918a2424087f2702c47, "fanoutList"); // (@DataFlowConnector<string> (jsonInputConnector).fanoutList) -- [IDataFlow<string>] --> (Apply<string,IEnumerable<JToken>> (id_d105b9870a7f4918a2424087f2702c47).input)
            id_d105b9870a7f4918a2424087f2702c47.WireTo(id_fd6166abbb7d475e9fc55e5babb07894, "output"); // (Apply<string,IEnumerable<JToken>> (id_d105b9870a7f4918a2424087f2702c47).output) -- [IDataFlow<IEnumerable<JToken>>] --> (ForEach<JToken> (id_fd6166abbb7d475e9fc55e5babb07894).collectionInput)
            id_fd6166abbb7d475e9fc55e5babb07894.WireTo(id_221f6a2c3d40454f843163cb7761b912, "elementOutput"); // (ForEach<JToken> (id_fd6166abbb7d475e9fc55e5babb07894).elementOutput) -- [IDataFlow<JToken>] --> (Apply<JToken,VisualPortGraphNode> (id_221f6a2c3d40454f843163cb7761b912).input)
            // END AUTO-GENERATED WIRING FOR CreateVPGNsFromJSON.xmind
            
            // BEGIN MANUAL INSTANTIATIONS FOR CreateVPGNsFromJSON.xmind
            // END MANUAL INSTANTIATIONS FOR CreateVPGNsFromJSON.xmind
            
            // BEGIN MANUAL WIRING FOR CreateVPGNsFromJSON.xmind
            // END MANUAL WIRING FOR CreateVPGNsFromJSON.xmind
        }
    }
}
