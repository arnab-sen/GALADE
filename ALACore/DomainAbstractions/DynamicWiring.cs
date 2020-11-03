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
    /// <para>2. IEvent delete: Delete the current wiring.</para>
    /// <para>3. T wire: The port to wire.</para>
    /// <para>4. IDataFlow&lt;object&gt; objectOutput: The port that propagates the instance.</para>
    /// </summary>
    /// <typeparam name="T">The type of the port to wire.</typeparam>
    public class DynamicWiring<T> : IDataFlow<object>, IEvent
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
                SetWiring(_instance, SourcePort, delete: false);
                Output(_instance);
            }
        }

        // IEvent implementation
        void IEvent.Execute()
        {
            // Delete the wiring
            SetWiring(_instance, SourcePort, delete: true);
        }

        // Methods
        private void SetWiring(object instance, string port = "", bool delete = false)
        {
            if (!string.IsNullOrWhiteSpace(port))
            {
                if (!Reverse)
                {
                    if (!delete)
                    {
                        instance.WireTo(wire, port); 
                    }
                    else
                    {
                        instance.DeleteWireTo(wire, port);
                    }
                }
                else
                {
                    if (!delete)
                    {
                        instance.WireFrom(wire, port); 
                    }
                    else
                    {
                        wire.DeleteWireTo(instance, port);
                    }
                }
            }
            else
            {
                if (!Reverse)
                {
                    if (!delete)
                    {
                        instance.WireTo(wire); 
                    }
                    else
                    {
                        instance.DeleteWireTo(wire);
                    }
                }
                else
                {
                    if (!delete)
                    {
                        instance.WireFrom(wire); 
                    }
                    else
                    {
                        wire.DeleteWireTo(instance);
                    }
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
