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

        public ExampleDomainAbstraction(string arg0, string arg2 = "Main")
        {		
			// BEGIN AUTO-GENERATED INSTANTIATIONS FOR Main
            DataFlowConnector<string> B = new DataFlowConnector<string>() {}; /* {"IsRoot":false} */
            Apply<string, bool> id_67c6c080ba7641ad84fb67d7fa5e4c49 = new Apply<string, bool>() {}; /* {"IsRoot":false} */
            AbstractionModel id_c1ebb1132d90433b97a4e344b4dddf6e = new AbstractionModel() {}; /* {"IsRoot":false} */
            AbstractionModel id_f1661fa73da64a9ba6769d8a32e1048b = new AbstractionModel() {}; /* {"IsRoot":false} */
            AbstractionModel id_5197c8183ea440f797efcfd411b71f24 = new AbstractionModel() {}; /* {"IsRoot":false} */
            // END AUTO-GENERATED INSTANTIATIONS FOR Main

			// BEGIN AUTO-GENERATED WIRING FOR Main
            A.WireTo(B, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":true,"DestinationType":"DataFlowConnector","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            B.WireTo(id_67c6c080ba7641ad84fb67d7fa5e4c49, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string","bool"]} */
            B.WireTo(test, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":true,"Description":"","SourceGenerics":["string"],"DestinationGenerics":["string"]} */
            // END AUTO-GENERATED WIRING FOR Main
        }
    }
}