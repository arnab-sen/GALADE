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
    /// <para>An abstraction that creates an object using a given delegate.</para>
    /// <para>Ports:</para>
    /// <para>IEvent create: The event to start the creation and output process.</para>
    /// <para>IDataFlow&lt;object&gt; objectOutput: The output port where the created object is sent.</para>
    /// </summary>
    public class ObjectFactory : IEvent // create
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        // Private fields
        private Func<object> _create;

        // Ports
        private IDataFlow<object> objectOutput;

        // IEvent implementation
        void IEvent.Execute()
        {
            if (objectOutput != null) objectOutput.Data = _create?.Invoke();
        }

        // Methods

        /// <summary>
        /// <para>An abstraction that creates an object using a given delegate.</para>
        /// <para>Ports:</para>
        /// <para>IEvent create: The event to start the creation and output process.</para>
        /// <para>IDataFlow&lt;object&gt; objectOutput: The output port where the created object is sent.</para>
        /// </summary>
        public ObjectFactory(Func<object> createDelegate)
        {
            _create = createDelegate;
        }
    }
}
