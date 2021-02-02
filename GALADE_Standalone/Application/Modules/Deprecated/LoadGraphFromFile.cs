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
    /// <para>Creates VisualPortGraphNodes and connection tuples from a JSON representation of a VisualPortGraph. The connection tuples are output to be added to the graph externally.</para>
    /// <para>Ports:</para>
    /// <para>1. IDataFlow&lt;string&gt; filePathInput:</para>
    /// <para>2. IUI contextMenuInput:</para>
    /// <para>3. IDataFlowB&lt;List&lt;string&gt;&gt; typesInput:</para>
    /// <para>4. IEvent typeChanged:</para>
    /// <para>5. IEvent loadComplete:</para>
    /// <para>6. IDataFlow&lt;List&lt;IPortConnection&gt;&gt; connectionTuplesOutput:</para>
    /// </summary>
    public class LoadGraphFromFile : IDataFlow<string>
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public VisualPortGraph Graph { get; set; }
        public VisualStyle NodeStyle { get; set; }
        public VisualStyle PortStyle { get; set; }
        public StateTransition<Enums.DiagramMode> StateTransition { get; set; }
        public UndoHistory UndoHistory { get; set; }

        // Private fields
        
        // Ports
        private IUI contextMenuInput;
        private IDataFlowB<List<string>> typesInput;
        private IEvent typeChanged;
        private IEvent loadComplete;
        private IDataFlow<List<IPortConnection>> connectionTuplesOutput; 

        // Input instances
        private DataFlowConnector<string> filePathInputConnector = new DataFlowConnector<string>() { InstanceName = "filePathInputConnector" };
        
        // Output instances
        
        // IDataFlow<string> implementation
        string IDataFlow<string>.Data { get { return default; } set { (filePathInputConnector as IDataFlow<string>).Data = value; } }
        
        // PostWiringInitialize
        private void PostWiringInitialize()
        {
            // Mapping to virtual ports
            
            // IDataFlowB and IEventB event handlers
            // Send out initial values
            // (instanceNeedingInitialValue as IDataFlow<T>).Data = defaultValue;
        }

        // Methods
        public void LoadGraph(string memento)
        {
            try
            {
                Graph.Clear();

                JObject obj = JObject.Parse(memento);

                JArray nodeArray = obj["Nodes"].ToObject<JArray>();
                foreach (var unparsedNode in nodeArray)
                {
                    JObject nodeObj = JObject.Parse(unparsedNode.ToString());
                    var newNode = new VisualPortGraphNode()
                    {
                        Graph = Graph,
                        StateTransition = StateTransition,
                        NodeStyle = NodeStyle,
                        PortStyle = PortStyle,
                        Id = nodeObj["Id"].ToString(),
                        Type = nodeObj["Type"].ToString(),
                        Name = nodeObj["Name"].ToString()
                    };

                    foreach (JObject port in nodeObj["Ports"].ToObject<JArray>())
                    {
                        newNode.Ports.Add(new Port()
                        {
                            Type = port["Type"].ToString(), 
                            Name = port["Name"].ToString(), 
                            IsInputPort = bool.Parse(port["IsInputPort"].ToString())
                        });
                    }  

                    newNode.ActionPerformed += (source) => UndoHistory.Push(source);

                    Utilities.ConnectToVirtualPort(newNode, "contextMenuInput", contextMenuInput);
                    Utilities.ConnectToVirtualPort(newNode, "typesInput", typesInput);
                    Utilities.ConnectToVirtualPort(newNode, "typeChanged", typeChanged);

                    (newNode as IEvent).Execute(); // Initialise render
                }

                // The graph will call LoadMemento on each node here, so it can be called just once rather than once for every node
                Graph.LoadMemento(memento);

                // The call stack needs to be collapsed before the node renders can be loaded as WPF FrameworkElements.
                // The output IEvent thus needs to be sent when an external source (the root's Render.Loaded) is ready,
                // rather than just sending the IEvent out as a standard sequential "loadComplete?.Execute();" call.
                Graph.Root.Render.Loaded += (sender, args) => loadComplete?.Execute(); 

            }
            catch (Exception e)
            {

            }
        }
        
        /// <summary>
        /// <para></para>
        /// </summary>
        public LoadGraphFromFile()
        {
            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR LoadGraphFromFile.xmind
            ApplyAction<string> id_37ba5874eb184b3197afc50cb3f18c0d = new ApplyAction<string>() { InstanceName = "Default", Lambda = s => LoadGraph(s) };
            FileReader id_d96bce7dc40d4224bbf6c0b29b742445 = new FileReader() { InstanceName = "Default" };
            // END AUTO-GENERATED INSTANTIATIONS FOR LoadGraphFromFile.xmind
            
            // BEGIN AUTO-GENERATED WIRING FOR LoadGraphFromFile.xmind
            filePathInputConnector.WireTo(id_d96bce7dc40d4224bbf6c0b29b742445, "fanoutList"); // (@DataFlowConnector<string> (filePathInputConnector).fanoutList) -- [IDataFlow<string>] --> (FileReader (id_d96bce7dc40d4224bbf6c0b29b742445).filePathInput)
            id_d96bce7dc40d4224bbf6c0b29b742445.WireTo(id_37ba5874eb184b3197afc50cb3f18c0d, "fileContentOutput"); // (FileReader (id_d96bce7dc40d4224bbf6c0b29b742445).fileContentOutput) -- [IDataFlow<string>] --> (ApplyAction<string> (id_37ba5874eb184b3197afc50cb3f18c0d).input)
            // END AUTO-GENERATED WIRING FOR LoadGraphFromFile.xmind
            
            // BEGIN MANUAL INSTANTIATIONS FOR LoadGraphFromFile.xmind
            // END MANUAL INSTANTIATIONS FOR LoadGraphFromFile.xmind
            
            // BEGIN MANUAL WIRING FOR LoadGraphFromFile.xmind
            // END MANUAL WIRING FOR LoadGraphFromFile.xmind
        }
    }
}
