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
    /// <para>This class propagates an input data flow outside of the current thread.</para>
    /// <para>Ports:</para>
    /// <para>IDataFlow&lt;T&gt;&gt; input: The data to propagate.</para>
    /// <para>IDataFlow&lt;T&gt;&gt; delayedData: The delayed output data.</para>
    /// </summary>
    public class DispatcherData<T> : IDataFlow<T>
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public DispatcherPriority Priority { get; set; } = DispatcherPriority.ApplicationIdle;

        // Private fields
        private T _data;

        // Ports
        private IDataFlow<T> delayedData;

        // IDataFlow<T>> implementation
        T IDataFlow<T>.Data
        {
            get => _data;
            set
            {
                _data = value;

                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    if (delayedData != null) delayedData.Data = _data;
                }, Priority);
            }
        }

        // Methods

        public DispatcherData()
        {

        }
    }
}