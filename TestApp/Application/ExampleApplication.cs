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
            var test1 = new Apply<T1, T2>() {InstanceName="test1"}; /*  */
            var test3 = new Apply<T1, T2>() {InstanceName="test3"}; /*  */
            var id_a8054d7507df4d81b0fd638c12dc3d1b = new Object() {}; /*  */
            // END AUTO-GENERATED INSTANTIATIONS FOR test

			// BEGIN AUTO-GENERATED WIRING FOR test
            test3.WireTo(id_a8054d7507df4d81b0fd638c12dc3d1b, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"NewNode","DestinationIsReference":false} */
			// END AUTO-GENERATED WIRING FOR test
		
		
			// BEGIN AUTO-GENERATED INSTANTIATIONS FOR test1
            var test2 = new Cast<T1, T2>() {InstanceName="test2"}; /*  */
            // END AUTO-GENERATED INSTANTIATIONS FOR test1

			// BEGIN AUTO-GENERATED WIRING FOR test1
            test2.WireTo(test, "output"); /* {"SourceType":"Cast","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":true} */
			// END AUTO-GENERATED WIRING FOR test1
        }
    }
}






