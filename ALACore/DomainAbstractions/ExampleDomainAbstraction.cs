using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>This is a dummy domain abstraction to use as an example, either to learn from or to use in testing.</para>
    /// <para>Ports:</para>
    /// <para>1. IEvent start: Start</para>
    /// </summary>
    public class ExampleDomainAbstraction : UIElement, IEvent, IDataFlow<string>
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public object TestProperty { get; set; }

        // Private fields
        private string _ignoreStringField = "";

        // Ports
        private IDataFlow<string> stringPort;
        private IEvent eventPort;
        private IUI uiPort1;
        private IUI uiPort2;

        // IEvent implementation
        void IEvent.Execute()
        {

        }

        // IDataFlow<string> implementation
        string IDataFlow<string>.Data
        {
            get => default;
            set
            {

            }
        }

        // Methods

        public ExampleDomainAbstraction()
        {

        }
    }
}
