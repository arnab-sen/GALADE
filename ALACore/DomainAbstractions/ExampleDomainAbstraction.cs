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
        public List<string> TestProperty { get; set; }

        // Private fields
        private string _ignoreStringField = "";
		private Data<int> _testData = new Data<int>()
        {
            storedData = 10
        };

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

        public ExampleDomainAbstraction(string arg0, string arg2 = "test")
        {

        }
    }
}
