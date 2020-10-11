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
    /// <para>Saves a VisualPortGraph's memento to a file.</para>
    /// <para>Ports:</para>
    /// <para>1. IDataFlow&lt;string&gt; filePathInput:</para>
    /// <para>2. IEvent complete:</para>
    /// </summary>
    public class SaveGraphToFile : IDataFlow<string>
    {
        // Public fields and properties
        public string InstanceName = "Default";
        public VisualPortGraph Graph { get; set; }

        // Private fields
        
        // Ports
        private IEvent complete;
        
        // Input instances
        private DataFlowConnector<string> filePathInputConnector = new DataFlowConnector<string>() { InstanceName = "filePathInputConnector" };
        
        // Output instances
        private EventConnector completeEventConnector = new EventConnector() { InstanceName = "completeEventConnector" };
        
        // IDataFlow<string> implementation
        string IDataFlow<string>.Data { get { return default; } set { (filePathInputConnector as IDataFlow<string>).Data = value; } }
        
        // PostWiringInitialize
        private void PostWiringInitialize()
        {
            // Mapping to virtual ports
            Utilities.ConnectToVirtualPort(completeEventConnector, "complete", complete);

            // IDataFlowB and IEventB event handlers
            // Send out initial values
            // (instanceNeedingInitialValue as IDataFlow<T>).Data = defaultValue;
        }
        
        // Methods
        public string GetGraphFileContents()
        {
            Graph.SaveMemento();
            return Graph.Memento;
        }

        /// <summary>
        /// <para></para>
        /// </summary>
        public SaveGraphToFile()
        {
            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR SaveGraphToFile.xmind
            ConvertToEvent<string> id_3b32a534fe62443f8126d8c9f6514402 = new ConvertToEvent<string>() { InstanceName = "Default" };
            Data<string> id_32b925fc49f64594988205b71239a37e = new Data<string>() { InstanceName = "Default", Lambda = GetGraphFileContents };
            FileWriter id_209ef6e4761e424d9ffccff461998cda = new FileWriter() { InstanceName = "Default" };
            // END AUTO-GENERATED INSTANTIATIONS FOR SaveGraphToFile.xmind
            
            // BEGIN AUTO-GENERATED WIRING FOR SaveGraphToFile.xmind
            filePathInputConnector.WireTo(id_3b32a534fe62443f8126d8c9f6514402, "fanoutList"); // (@DataFlowConnector<string> (filePathInputConnector).fanoutList) -- [IDataFlow<string>] --> (ConvertToEvent<string> (id_3b32a534fe62443f8126d8c9f6514402).start)
            id_3b32a534fe62443f8126d8c9f6514402.WireTo(id_32b925fc49f64594988205b71239a37e, "eventOutput"); // (ConvertToEvent<string> (id_3b32a534fe62443f8126d8c9f6514402).eventOutput) -- [IEvent] --> (Data<string> (id_32b925fc49f64594988205b71239a37e).start)
            id_32b925fc49f64594988205b71239a37e.WireTo(id_209ef6e4761e424d9ffccff461998cda, "dataOutput"); // (Data<string> (id_32b925fc49f64594988205b71239a37e).dataOutput) -- [IDataFlow<string>] --> (FileWriter (id_209ef6e4761e424d9ffccff461998cda).fileContentInput)
            id_209ef6e4761e424d9ffccff461998cda.WireTo(filePathInputConnector, "filePathInput"); // (FileWriter (id_209ef6e4761e424d9ffccff461998cda).filePathInput) -- [IDataFlowB<string>] --> (@DataFlowConnector<string> (filePathInputConnector).NEEDNAME)
            id_209ef6e4761e424d9ffccff461998cda.WireTo(completeEventConnector, "complete"); // (FileWriter (id_209ef6e4761e424d9ffccff461998cda).complete) -- [IEvent] --> (@EventConnector (completeEventConnector).eventInput)
            // END AUTO-GENERATED WIRING FOR SaveGraphToFile.xmind
            
            // BEGIN MANUAL INSTANTIATIONS FOR SaveGraphToFile.xmind
            // END MANUAL INSTANTIATIONS FOR SaveGraphToFile.xmind
            
            // BEGIN MANUAL WIRING FOR SaveGraphToFile.xmind
            // END MANUAL WIRING FOR SaveGraphToFile.xmind
        }
    }
}
