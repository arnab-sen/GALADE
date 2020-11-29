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
    /// <para>1. IEvent open:</para>
    /// </summary>
    public class ConfigWindow : IEvent
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        
        // Private fields
        
        // Ports
        
        // Input instances
        private EventConnector openConnector = new EventConnector() { InstanceName = "openConnector" };
        
        // Output instances
        
        // IEvent implementation
        void IEvent.Execute()
        {
            (openConnector as IEvent).Execute();
        }
        
        // PostWiringInitialize
        private void PostWiringInitialize()
        {
            // Mapping to virtual ports
            
            // IDataFlowB and IEventB event handlers
            // Send out initial values
            // (instanceNeedingInitialValue as IDataFlow<T>).Data = defaultValue;
        }
        
        /// <summary>
        /// <para></para>
        /// </summary>
        public ConfigWindow()
        {
            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR ConfigWindow.xmind
            // END AUTO-GENERATED INSTANTIATIONS FOR ConfigWindow.xmind
            
            // BEGIN AUTO-GENERATED WIRING FOR ConfigWindow.xmind
            // END AUTO-GENERATED WIRING FOR ConfigWindow.xmind
            
            // BEGIN MANUAL INSTANTIATIONS FOR ConfigWindow.xmind
            // END MANUAL INSTANTIATIONS FOR ConfigWindow.xmind
            
            // BEGIN MANUAL WIRING FOR ConfigWindow.xmind
            // END MANUAL WIRING FOR ConfigWindow.xmind
        }
    }
}
