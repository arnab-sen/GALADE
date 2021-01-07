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
            Button a = new Button() {InstanceName="a"}; /* {"IsRoot":false} */
            Apply<T1, T2> b = new Apply<T1, T2>() {InstanceName="b"}; /* {"IsRoot":false} */
            Object id_a99f72e217dd4d1a91666081cf222fa5 = new Object() {}; /* {"IsRoot":false} */
            Object id_c823c95cd3254c3f9939eb933c4d160a = new Object() {}; /* {"IsRoot":false} */
            Object id_9943221701854577b2a27a88006ef541 = new Object() {}; /* {"IsRoot":false} */
            Object id_09c39079f1ac4e5cb1296c717e06910d = new Object() {}; /* {"IsRoot":false} */
            // END AUTO-GENERATED INSTANTIATIONS FOR test

			// BEGIN AUTO-GENERATED WIRING FOR test
            a.WireTo(c, "eventButtonClicked"); /* {"SourceType":"Button","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":false} */
            a.WireTo(b, "eventButtonClicked"); /* {"SourceType":"Button","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false} */
            b.WireTo(d, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":false} */
            c.WireTo(e, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":false} */
            b.WireTo(e, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":false} */
            b.WireTo(id_a88c1453471840b78d6997e95a92c9a2, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":false} */
            b.WireTo(id_a99f72e217dd4d1a91666081cf222fa5, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"NewNode","DestinationIsReference":false} */
            id_a88c1453471840b78d6997e95a92c9a2.WireTo(id_c823c95cd3254c3f9939eb933c4d160a, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"NewNode","DestinationIsReference":false} */
            id_a88c1453471840b78d6997e95a92c9a2.WireTo(id_9943221701854577b2a27a88006ef541, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"NewNode","DestinationIsReference":false} */
            id_a99f72e217dd4d1a91666081cf222fa5.WireTo(id_09c39079f1ac4e5cb1296c717e06910d, "output"); /* {"SourceType":"NewNode","SourceIsReference":false,"DestinationType":"NewNode","DestinationIsReference":false} */
            // END AUTO-GENERATED WIRING FOR test
        }
    }
}