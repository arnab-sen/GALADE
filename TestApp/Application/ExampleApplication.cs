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
            var a = new Menu() {InstanceName="Default"}; /* {"IsRoot":false} */
            var id_44c89cda571c4b5d933e6523fe13f3be = new MenuItem() {InstanceName="id_44c89cda571c4b5d933e6523fe13f3be"}; /* {"IsRoot":false} */
            var id_6624a0ab3cb64b9fa1f735c65bf1ed83 = new MenuItem() {InstanceName="id_6624a0ab3cb64b9fa1f735c65bf1ed83"}; /* {"IsRoot":false} */
            var id_431601fd3eaa41db9fd94758db97488f = new Data<T>() {InstanceName="id_431601fd3eaa41db9fd94758db97488f"}; /* {"IsRoot":false} */
            var id_fbad612b486d4f80a813f29858979d87 = new DataFlowConnector<bool>() {InstanceName="id_fbad612b486d4f80a813f29858979d87"}; /* {"IsRoot":false} */
            // END AUTO-GENERATED INSTANTIATIONS FOR test

			// BEGIN AUTO-GENERATED WIRING FOR test
            a.WireTo(id_44c89cda571c4b5d933e6523fe13f3be, "children"); /* {"SourceType":"Menu","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false} */
            a.WireTo(id_6624a0ab3cb64b9fa1f735c65bf1ed83, "children"); /* {"SourceType":"Menu","SourceIsReference":false,"DestinationType":"MenuItem","DestinationIsReference":false} */
            id_6624a0ab3cb64b9fa1f735c65bf1ed83.WireTo(id_431601fd3eaa41db9fd94758db97488f, "eventOutput"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false} */
            id_431601fd3eaa41db9fd94758db97488f.WireTo(id_fbad612b486d4f80a813f29858979d87, "dataOutput"); /* {"SourceType":"Data","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false} */
            id_44c89cda571c4b5d933e6523fe13f3be.WireTo(id_fbad612b486d4f80a813f29858979d87, "dataFlowBOutput"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"DataFlowConnector","DestinationIsReference":false} */
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


















































































