using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;

namespace StoryAbstractions
{
    /// <summary>
    /// <para>Represents the subgraph for editing an existing JSON settings file. All string inputs should be fed in before sending the new value input object.</para>
    /// <para>Ports:</para>
    /// <para>1. IDataFlow&lt;object&gt; valueInput:</para>
    /// <para>2. IDataFlowB&lt;string&gt; filePathInput:</para>
    /// <para>3. IEvent complete:</para>
    /// </summary>
    public class EditSetting : IDataFlow<object> // valueInput
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string JSONPath { set => jsonEditor.JSONPath = value; }

        // Private fields
        private JSONEditor jsonEditor = new JSONEditor() { InstanceName = "Default" };
        private Data<string> getFilePath = new Data<string>() { InstanceName = "getFilePath" };

        // Ports
        private IDataFlowB<string> filePathInput;
        private IEvent complete;
        
        // Input instances
        private DataFlowConnector<object> valueInputConnector = new DataFlowConnector<object>() { InstanceName = "valueInputConnector" };

        // Output instances
        private EventConnector completeConnector = new EventConnector() { InstanceName = "completeConnector" };
        
        // IDataFlow<object> implementation
        object IDataFlow<object>.Data 
        {
            get => default;
            set => (valueInputConnector as IDataFlow<object>).Data = value;
        }
        
        // PostWiringInitialize
        private void PostWiringInitialize()
        {
            // Mapping to virtual ports
            if (filePathInput != null) getFilePath.WireTo(filePathInput, "inputDataB");
            if (complete != null) completeConnector.WireTo(complete, "complete");
            
            // IDataFlowB and IEventB event handlers
            
            // Send out initial values
            // (instanceNeedingInitialValue as IDataFlow<T>).Data = defaultValue;
        }
        
        /// <summary>
        /// <para></para>
        /// </summary>
        public EditSetting()
        {
            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR EditSetting.xmind
            ConvertToEvent<object> id_4f177a0233864156bdfe54a4092637b2 = new ConvertToEvent<object>() { InstanceName = "Default" };
            DataFlowConnector<string> filePath = new DataFlowConnector<string>() { InstanceName = "filePath" };
            DataFlowConnector<string> id_7947079458bd41d6aa4c7069c9e758d7 = new DataFlowConnector<string>() { InstanceName = "Default" };
            FileReader id_359fb16cea854bd396c5f85fb5568d92 = new FileReader() { InstanceName = "Default" };
            FileWriter id_a03cde5436a24b338b7865b9e076b613 = new FileWriter() { InstanceName = "Default" };
            // END AUTO-GENERATED INSTANTIATIONS FOR EditSetting.xmind
            
            // BEGIN AUTO-GENERATED WIRING FOR EditSetting.xmind
            valueInputConnector.WireTo(id_4f177a0233864156bdfe54a4092637b2, "fanoutList"); // (@DataFlowConnector<object> (valueInputConnector).fanoutList) -- [IDataFlow<object>] --> (ConvertToEvent<object> (id_4f177a0233864156bdfe54a4092637b2).start)
            valueInputConnector.WireTo(jsonEditor, "fanoutList"); // (@DataFlowConnector<object> (valueInputConnector).fanoutList) -- [IDataFlow<object>] --> (@JSONEditor (jsonEditor).newContentInput)
            id_4f177a0233864156bdfe54a4092637b2.WireTo(getFilePath, "eventOutput"); // (ConvertToEvent<object> (id_4f177a0233864156bdfe54a4092637b2).eventOutput) -- [IEvent] --> (@Data<string> (getFilePath).start)
            getFilePath.WireTo(filePath, "dataOutput"); // (@Data<string> (getFilePath).dataOutput) -- [IDataFlow<string>] --> (DataFlowConnector<string> (filePath).dataInput)
            filePath.WireTo(id_359fb16cea854bd396c5f85fb5568d92, "fanoutList"); // (DataFlowConnector<string> (filePath).fanoutList) -- [IDataFlow<string>] --> (FileReader (id_359fb16cea854bd396c5f85fb5568d92).filePathInput)
            id_359fb16cea854bd396c5f85fb5568d92.WireTo(id_7947079458bd41d6aa4c7069c9e758d7, "fileContentOutput"); // (FileReader (id_359fb16cea854bd396c5f85fb5568d92).fileContentOutput) -- [IDataFlow<string>] --> (DataFlowConnector<string> (id_7947079458bd41d6aa4c7069c9e758d7).dataInput)
            jsonEditor.WireTo(id_7947079458bd41d6aa4c7069c9e758d7, "jsonInput"); // (@JSONEditor (jsonEditor).jsonInput) -- [IDataFlowB<string>] --> (DataFlowConnector<string> (id_7947079458bd41d6aa4c7069c9e758d7).NEEDNAME)
            jsonEditor.WireTo(id_a03cde5436a24b338b7865b9e076b613, "newJsonOutput"); // (@JSONEditor (jsonEditor).newJsonOutput) -- [IDataFlow<string>] --> (FileWriter (id_a03cde5436a24b338b7865b9e076b613).fileContentInput)
            id_a03cde5436a24b338b7865b9e076b613.WireTo(filePath, "filePathInput"); // (FileWriter (id_a03cde5436a24b338b7865b9e076b613).filePathInput) -- [IDataFlowB<string>] --> (DataFlowConnector<string> (filePath).returnDataB)
            // END AUTO-GENERATED WIRING FOR EditSetting.xmind
            
            // BEGIN MANUAL INSTANTIATIONS FOR EditSetting.xmind
            // END MANUAL INSTANTIATIONS FOR EditSetting.xmind
            
            // BEGIN MANUAL WIRING FOR EditSetting.xmind
            // END MANUAL WIRING FOR EditSetting.xmind
        }
    }
}
