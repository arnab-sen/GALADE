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
    /// <para>Retrieves settings from a JSON-formatted file and outputs the JSON at the setting given.</para>
    /// <para>Ports:</para>
    /// <para>1. IEvent start:</para>
    /// <para>2. IDataFlowB&lt;string&gt; filePathInput:</para>
    /// <para>3. IDataFlow&lt;string&gt; settingJsonOutput:</para>
    /// </summary>
    public class GetSetting : IEvent
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        // Private fields
        private string _settingName = "";
        private Data<string> getFilePath = new Data<string>() { InstanceName = "getFilePath" };

        // Ports
        private IDataFlowB<string> filePathInput;
        private IDataFlow<string> settingJsonOutput;
        
        // Input instances
        private EventConnector startConnector = new EventConnector() { InstanceName = "startConnector" };
        
        // Output instances
        private Apply<string, string> settingJsonOutputConnector = new Apply<string, string>() { InstanceName = "settingJsonOutputConnector", Lambda = s => s };

        // IEvent implementation
        void IEvent.Execute()
        {
            (startConnector as IEvent).Execute();
        }
        
        // PostWiringInitialize
        private void PostWiringInitialize()
        {
            // Mapping to external ports
            if (filePathInput != null) getFilePath.WireTo(filePathInput, "inputDataB");
            if (settingJsonOutput != null) settingJsonOutputConnector.WireTo(settingJsonOutput, "output");

            // IDataFlowB and IEventB event handlers
            // Send out initial values
            // (instanceNeedingInitialValue as IDataFlow<T>).Data = defaultValue;
        }
        
        /// <summary>
        /// <para></para>
        /// </summary>
        public GetSetting(string name)
        {
            _settingName = name;

            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR GetSetting.xmind
            FileReader id_9c1f2f8b8c3c45d497ba51b4a9ad677d = new FileReader() { InstanceName = "Default" };
            JSONParser id_11de140fa67346488aa1434957598797 = new JSONParser() { InstanceName = "Default", JSONPath = $"$..{_settingName}" };
            // END AUTO-GENERATED INSTANTIATIONS FOR GetSetting.xmind

            // BEGIN AUTO-GENERATED WIRING FOR GetSetting.xmind
            startConnector.WireTo(getFilePath, "fanoutList"); // (@EventConnector (startConnector).fanoutList) -- [IEvent] --> (@Data<string> (getFilePath).start)
            getFilePath.WireTo(id_9c1f2f8b8c3c45d497ba51b4a9ad677d, "dataOutput"); // (@Data<string> (getFilePath).dataOutput) -- [IDataFlow<string>] --> (FileReader (id_9c1f2f8b8c3c45d497ba51b4a9ad677d).filePathInput)
            id_9c1f2f8b8c3c45d497ba51b4a9ad677d.WireTo(id_11de140fa67346488aa1434957598797, "fileContentOutput"); // (FileReader (id_9c1f2f8b8c3c45d497ba51b4a9ad677d).fileContentOutput) -- [IDataFlow<string>] --> (JSONParser (id_11de140fa67346488aa1434957598797).jsonInput)
            id_11de140fa67346488aa1434957598797.WireTo(settingJsonOutputConnector, "jsonOutput"); // (JSONParser (id_11de140fa67346488aa1434957598797).jsonOutput) -- [IDataFlow<string>] --> (@Apply<string,string> (settingJsonOutputConnector).input)
            // END AUTO-GENERATED WIRING FOR GetSetting.xmind

            // BEGIN MANUAL INSTANTIATIONS FOR GetSetting.xmind
            // END MANUAL INSTANTIATIONS FOR GetSetting.xmind

            // BEGIN MANUAL WIRING FOR GetSetting.xmind
            // END MANUAL WIRING FOR GetSetting.xmind
        }
    }
}
