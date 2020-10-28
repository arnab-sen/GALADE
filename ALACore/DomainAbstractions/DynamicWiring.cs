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
    /// <para>Wires an instance to a port of a given type.</para>
    /// <para>Only one port can be wired per instance of DynamicWiring. If you want to wire multiple ports, you can chain together a sequence of
    /// DynamicWirings through the objectOutput port.</para>
    /// <para>Ports:</para>
    /// <para>1. IDataFlow&lt;object&gt; objectInput: The input receiving the instance to wire.</para>
    /// <para>2. T wire: The port to wire.</para>
    /// <para>3. IDataFlow&lt;object&gt; objectOutput: The port that propagates the instance.</para>
    /// </summary>
    /// <typeparam name="T">The type of the port to wire.</typeparam>
    public class DynamicWiring<T> : IDataFlow<object>
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string SourcePort { get; set; }
        public bool Reverse { get; set; } = false;

        // Private fields
        private object _instance;

        // Ports
        private T wire;
        private IDataFlow<object> objectOutput;

        // IDataFlow<object> implementation
        object IDataFlow<object>.Data
        {
            get => _instance;
            set
            {
                _instance = value;
                SetWiring(_instance, SourcePort);
                Output(_instance);
            }
        }

        // Methods
        private void SetWiring(object instance, string port = "")
        {
            if (!string.IsNullOrWhiteSpace(port))
            {
                if (!Reverse)
                {
                    instance.WireTo(wire, port);
                }
                else
                {
                    instance.WireFrom(wire, port);
                }
            }
            else
            {
                if (!Reverse)
                {
                    instance.WireTo(wire);
                }
                else
                {
                    instance.WireFrom(wire);
                }
            }
        }

        private void Output(object instance)
        {
            if (objectOutput != null) objectOutput.Data = instance;
        }

        /// <summary>
        /// <para>Wires an instance to a port of a given type.</para>
        /// <para>Only one port can be wired per instance of DynamicWiring. If you want to wire multiple ports, you can chain together a sequence of
        /// DynamicWirings through the objectOutput port.</para>
        /// <para>Ports:</para>
        /// <para>1. IDataFlow&lt;object&gt; objectInput: The input receiving the instance to wire.</para>
        /// <para>2. T wire: The port to wire.</para>
        /// <para>3. IDataFlow&lt;object&gt; objectOutput: The port that propagates the instance.</para>
        /// </summary>
        /// <typeparam name="T">The type of the port to wire.</typeparam>
        public DynamicWiring()
        {

        }
    }
}
