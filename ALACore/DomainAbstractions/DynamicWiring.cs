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
    /// <para>Wires an instance to any current programming paradigm port. When programming paradigms are added, this abstraction should be updated.</para>
    /// <para>The programming paradigm type and the instance's output port should be given in through the constructor.</para>
    /// <para>Only one port should be used per instance. If you want to wire multiple ports, you can chain together a sequence of
    /// DynamicWirings through the objectOutput port.</para>
    /// <para>An example instantiation:</para>
    /// <code>new DynamicWiring&lt;int&gt;("DataFlow", "myOutputPort")</code>
    /// <para>Note that when specifying "DataFlow" (which represents "IDataFlow&lt;int&gt;" here), the starting "I" and ending generic type are excluded.</para>
    /// <para>Ports:</para>
    /// <para>1. IDataFlow&lt;object&gt; objectInput: The input receiving the instance to wire.</para>
    /// <para>2. IDataFlow&lt;object&gt; objectOutput: The port that propagates the instance.</para>
    /// <para>3. IDataFlow&lt;T&gt; wireDataFlow: The IDataFlow port to wire.</para>
    /// <para>4. IEvent wireEvent: The IEvent port to wire.</para>
    /// <para>5. IUI wireUi: The IUI port to wire.</para>
    /// <para>6. IEventHandler wireEventHandler: The IEventHandler port to wire.</para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DynamicWiring<T> : IDataFlow<object>
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        // Private fields
        private object _instance;
        private string _type;
        private string _sourcePort;
        private Dictionary<string, object> _portMapping;

        // Ports
        private IDataFlow<object> objectOutput;
        private IDataFlow<T> wireDataFlow;
        private IEvent wireEvent;
        private IUI wireUi;
        private IEventHandler wireEventHandler;

        // IDataFlow<object> implementation
        object IDataFlow<object>.Data
        {
            get => _instance;
            set
            {
                _instance = value;
                SetWiring(_instance);
                Output(_instance);
            }
        }

        // Methods
        private void SetWiring(object instance)
        {
            if (_portMapping.ContainsKey(_type))
            {
                instance.WireTo(_portMapping[_type], _sourcePort);
            }
            else
            {
                var sb = new StringBuilder();
                foreach (var type in _portMapping.Keys)
                {
                    sb.Append(" " + type);
                }

                throw new Exception($"Invalid type {_type} in DynamicWiring. Valid types are:{sb}");
            }
        }

        private void Output(object instance)
        {
            if (objectOutput != null) objectOutput.Data = instance;
        }

        private void PostWiringInitialize()
        {
            _portMapping = new Dictionary<string, object>()
            {
                { "DataFlow", wireDataFlow },
                { "Event", wireEvent },
                { "UI", wireUi },
                { "EventHandler", wireEventHandler }
            };
        }

        public DynamicWiring(string type, string sourcePort)
        {
            _type = type;
            _sourcePort = sourcePort;
        }
    }
}
