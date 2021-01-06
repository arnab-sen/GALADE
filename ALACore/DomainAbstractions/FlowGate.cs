using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>A gate that blocks a data or event flow based on an input boolean.</para>
    /// <para>Ports:</para>
    /// <para>1. IDataFlow&lt;T&gt; inputData: The data flow.</para>
    /// <para>2. IEvent inputEvent: The event flow.</para>
    /// <para>3. IDataFlowB&lt;bool&gt; isOpen: Set false to block the data and event flow, and true to let them pass through.</para>
    /// </summary>
    public class FlowGate<T> : IDataFlow<T>, IEvent // inputData, inputEvent
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string InstanceDescription { get; set; } = "";
        public bool IsOpen { get; set; } = false;

        /// <summary>
        /// Determines whether or not to store the input data and event. If false, data and events can only ever pass through if sent while the gate is open.
        /// </summary>
        public bool Store { get; set; } = true;

        // Private fields
        private T _data;
        private bool _hasData = false;
        private bool _hasEvent = false;

        // Ports
        private IDataFlowB<bool> isOpen;
        private IDataFlow<T> dataOutput;
        private IEvent eventOutput;

        // Methods
        public void SendData()
        {
            if (dataOutput != null) dataOutput.Data = _data;
            _hasData = false;
        }

        public void SendEvent()
        {
            eventOutput?.Execute();
            _hasEvent = false;
        }

        // IDataFlow<T> implementation
        T IDataFlow<T>.Data
        {
            get => _data;
            set
            {
                _data = value;
                if (IsOpen)
                {
                    SendData();
                }
                else
                {
                    if (Store) _hasData = true;
                }
            }
        }

        // IEvent implementation
        void IEvent.Execute()
        {
            if (IsOpen)
            {
                SendEvent();
            }
            else
            {
                if (Store) _hasEvent = true;
            }
        }

        private void PostWiringInitialize()
        {
            if (isOpen != null) isOpen.DataChanged += () =>
            {
                IsOpen = isOpen.Data;
                if (IsOpen)
                {
                    if (_hasData) SendData();
                    if (_hasEvent) SendEvent();
                }
            };
        }

        public FlowGate()
        {

        }
    }
}
