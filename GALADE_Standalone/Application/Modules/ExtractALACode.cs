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
    /// <para></para>
    /// <para>Ports:</para>
    /// <para>1. IDataFlow&lt;string&gt; codeInput:</para>
    /// </summary>
    public class ExtractALACode : IDataFlow<string>
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        // Private fields

        // Ports
        private IDataFlow<string> instantiationCodeOutput;
        private IDataFlow<string> wiringCodeOutput;
        
        // Input instances
        private DataFlowConnector<string> codeInputConnector = new DataFlowConnector<string>() { InstanceName = "codeInputConnector" };

        // Output instances
        private DataFlowConnector<string> instantiationCodeOutputConnector = new DataFlowConnector<string>() { InstanceName = "instantiationCodeOutputConnector" };
        private DataFlowConnector<string> wiringCodeOutputConnector = new DataFlowConnector<string>() { InstanceName = "wiringCodeOutputConnector" };


        // IDataFlow<string> implementation
        string IDataFlow<string>.Data
        {
            get => default;
            set
            {
                (codeInputConnector as IDataFlow<string>).Data = value;
            }
        }
        
        // PostWiringInitialize
        private void PostWiringInitialize()
        {
            // Mapping to virtual ports
            if (instantiationCodeOutput != null) instantiationCodeOutputConnector.WireTo(instantiationCodeOutput);
            if (wiringCodeOutput != null) wiringCodeOutputConnector.WireTo(wiringCodeOutput);

            // IDataFlowB and IEventB event handlers
            // Send out initial values
            // (instanceNeedingInitialValue as IDataFlow<T>).Data = defaultValue;
        }
        
        /// <summary>
        /// <para></para>
        /// </summary>
        public ExtractALACode()
        {
            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR ExtractALACode.xmind
            FileSplit id_50a2021cb090489f9baf57992f68be19 = new FileSplit() { InstanceName = "Default", Match = "// BEGIN", SplitAfterMatch = true };
            FileSplit id_57594e708e264e5f95ac4ac289b4eac5 = new FileSplit() { InstanceName = "Default", Match = "// END", SplitAfterMatch = false };
            FileSplit id_7e9585fe244a4bde871b94fc9ad52e7b = new FileSplit() { InstanceName = "Default", Match = "// BEGIN", SplitAfterMatch = true };
            FileSplit id_ce5ad295709f4bb2a042b55aa7777258 = new FileSplit() { InstanceName = "Default", Match = "// END", SplitAfterMatch = false };
            // END AUTO-GENERATED INSTANTIATIONS FOR ExtractALACode.xmind
            
            // BEGIN AUTO-GENERATED WIRING FOR ExtractALACode.xmind
            codeInputConnector.WireTo(id_50a2021cb090489f9baf57992f68be19, "fanoutList"); // (@DataFlowConnector<string> (codeInputConnector).fanoutList) -- [IDataFlow<string>] --> (FileSplit (id_50a2021cb090489f9baf57992f68be19).fileContentsInput)
            id_50a2021cb090489f9baf57992f68be19.WireTo(id_ce5ad295709f4bb2a042b55aa7777258, "lowerHalfOutput"); // (FileSplit (id_50a2021cb090489f9baf57992f68be19).lowerHalfOutput) -- [IDataFlow<string>] --> (FileSplit (id_ce5ad295709f4bb2a042b55aa7777258).fileContentsInput)
            id_ce5ad295709f4bb2a042b55aa7777258.WireTo(instantiationCodeOutputConnector, "upperHalfOutput"); // (FileSplit (id_ce5ad295709f4bb2a042b55aa7777258).upperHalfOutput) -- [IDataFlow<string>] --> (@DataFlowConnector<string> (instantiationCodeOutputConnector).dataInput)
            id_ce5ad295709f4bb2a042b55aa7777258.WireTo(id_7e9585fe244a4bde871b94fc9ad52e7b, "lowerHalfOutput"); // (FileSplit (id_ce5ad295709f4bb2a042b55aa7777258).lowerHalfOutput) -- [IDataFlow<string>] --> (FileSplit (id_7e9585fe244a4bde871b94fc9ad52e7b).fileContentsInput)
            id_7e9585fe244a4bde871b94fc9ad52e7b.WireTo(id_57594e708e264e5f95ac4ac289b4eac5, "lowerHalfOutput"); // (FileSplit (id_7e9585fe244a4bde871b94fc9ad52e7b).lowerHalfOutput) -- [IDataFlow<string>] --> (FileSplit (id_57594e708e264e5f95ac4ac289b4eac5).fileContentsInput)
            id_57594e708e264e5f95ac4ac289b4eac5.WireTo(wiringCodeOutputConnector, "upperHalfOutput"); // (FileSplit (id_57594e708e264e5f95ac4ac289b4eac5).upperHalfOutput) -- [IDataFlow<string>] --> (@DataFlowConnector<string> (wiringCodeOutputConnector).dataInput)
            // END AUTO-GENERATED WIRING FOR ExtractALACode.xmind
            
            // BEGIN MANUAL INSTANTIATIONS FOR ExtractALACode.xmind
            // END MANUAL INSTANTIATIONS FOR ExtractALACode.xmind
            
            // BEGIN MANUAL WIRING FOR ExtractALACode.xmind
            // END MANUAL WIRING FOR ExtractALACode.xmind
        }
    }
}
