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
            var id_65c677a6ff924211bee41357c32b4be8 = new TextBox() {}; /*  */
            // END AUTO-GENERATED INSTANTIATIONS FOR test

			// BEGIN AUTO-GENERATED WIRING FOR test
            add.WireTo(id_65c677a6ff924211bee41357c32b4be8, "inputOne"); /* {"SourceType":"TextBox","SourceIsReference":false,"DestinationType":"Add","DestinationIsReference":true} */
            // END AUTO-GENERATED WIRING FOR test
		
		
			// BEGIN AUTO-GENERATED INSTANTIATIONS FOR test1
            var test2 = new Text(true,text:"test") {InstanceName="test2"}; /*  */
            // END AUTO-GENERATED INSTANTIATIONS FOR test1

			// BEGIN AUTO-GENERATED WIRING FOR test1
            test2.WireTo(test, "?IUI"); /* {"SourceType":"Text","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":true} */
			// END AUTO-GENERATED WIRING FOR test1
        }
    }
}


































