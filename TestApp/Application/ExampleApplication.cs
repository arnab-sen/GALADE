using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Libraries;
using ProgrammingParadigms;

namespace Application
{
    /// <summary>
    /// <para>This is a dummy abstraction to use as an example, either to learn from or to use in testing.</para>
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


        // Methods

		public void DoSomething()
		{
			
		}

        public ExampleDomainAbstraction(string arg0, string arg2 = "test")
        {		
			// BEGIN AUTO-GENERATED INSTANTIATIONS FOR test
            var root0 = new DataFlowConnector<string>() {InstanceName="root0"};
            var sf0 = new StringFormat() {InstanceName="sf0"};
            // END AUTO-GENERATED INSTANTIATIONS FOR test

			// BEGIN AUTO-GENERATED WIRING FOR test
            sf0.WireTo(root0, "dataFlowBsList");
			// END AUTO-GENERATED WIRING FOR test
		
        }
    }
}




























