using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>This class propagates an input event outside of the current thread.</para>
    /// <para>Ports:</para>
    /// <para>IEvent input: The event to propagate.</para>
    /// <para>IEvent delayedEvent: The delayed output event.</para>
    /// </summary>
    public class DispatcherEvent : IEvent
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public DispatcherPriority Priority { get; set; } = DispatcherPriority.ApplicationIdle;

        // Private fields

        // Ports
        private IEvent delayedEvent;

        // IEvent implementation
        void IEvent.Execute()
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                delayedEvent?.Execute();
            }, Priority);
        }

        // Methods

        public DispatcherEvent()
        {

        }
    }
}
