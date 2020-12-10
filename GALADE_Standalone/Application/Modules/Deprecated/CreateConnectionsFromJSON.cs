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
    /// <para>Creates tuples for PortGraphConnections from a JSON input.
    /// This expects that each connection's source and destination VPGNs are already added to the given VisualPortGraph.</para>
    /// <para>Ports:</para>
    /// <para>1. IDataFlow&lt;string&gt; jsonInput:</para>
    /// <para>2. IDataFlow&lt;List&lt;Tuple&lt;VisualPortGraphNode, VisualPortGraphNode, Port, Port&gt;&gt;&gt; portConnectionTuplesOutput:</para>
    /// </summary>
    public class CreateConnectionsFromJSON : IDataFlow<string>
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public VisualPortGraph Graph { get; set; }
        
        // Private fields

        // Ports
        private IDataFlow<List<Tuple<VisualPortGraphNode, VisualPortGraphNode, Port, Port>>> portConnectionTuplesOutput;
        
        // Input instances
        private DataFlowConnector<string> jsonInputConnector = new DataFlowConnector<string>() { InstanceName = "jsonInputConnector" };
        
        // Output instances
        
        // IDataFlow<string> implementation
        string IDataFlow<string>.Data
        {
            get
            {
                return default;
            }
            set
            {
                Create(value);
            }
        }

        // PostWiringInitialize
        private void PostWiringInitialize()
        {
            // Mapping to virtual ports
            
            // IDataFlowB and IEventB event handlers
            // Send out initial values
            // (instanceNeedingInitialValue as IDataFlow<T>).Data = defaultValue;
        }

        // Methods
        /// <summary>
        /// Get all connections by extracting the connections from every port, to maintain fanout order.
        /// </summary>
        /// <param name="json"></param>
        private void Create(string json)
        {
            List<Tuple<VisualPortGraphNode, VisualPortGraphNode, Port, Port>> portConnectionTuples =
                new List<Tuple<VisualPortGraphNode, VisualPortGraphNode, Port, Port>>();

            var array = JArray.Parse(json);
            foreach (var connDump in array)
            {
                var source = (VisualPortGraphNode)Graph.GetNode(connDump["SourceId"].ToString());
                var dest = (VisualPortGraphNode)Graph.GetNode(connDump["DestinationId"].ToString());

                if (source != null && dest != null)
                {
                    var sourcePort = source.GetPort(connDump["SourcePortBox"]["Name"].ToString());
                    var destPort = dest.GetPort(connDump["DestinationPortBox"]["Name"].ToString());

                    portConnectionTuples.Add(Tuple.Create(source, dest, sourcePort, destPort));
                }
            }

            if (portConnectionTuplesOutput != null) portConnectionTuplesOutput.Data = portConnectionTuples;
        }
        
        /// <summary>
        /// <para></para>
        /// </summary>
        public CreateConnectionsFromJSON()
        {
            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR CreateConnectionsFromJSON.xmind
            // END AUTO-GENERATED INSTANTIATIONS FOR CreateConnectionsFromJSON.xmind
            
            // BEGIN AUTO-GENERATED WIRING FOR CreateConnectionsFromJSON.xmind
            // END AUTO-GENERATED WIRING FOR CreateConnectionsFromJSON.xmind
            
            // BEGIN MANUAL INSTANTIATIONS FOR CreateConnectionsFromJSON.xmind
            // END MANUAL INSTANTIATIONS FOR CreateConnectionsFromJSON.xmind
            
            // BEGIN MANUAL WIRING FOR CreateConnectionsFromJSON.xmind
            // END MANUAL WIRING FOR CreateConnectionsFromJSON.xmind
        }
    }
}
