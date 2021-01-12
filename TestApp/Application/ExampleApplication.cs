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
            Apply<T1, T2> A = new Apply<T1, T2>() {InstanceName="A"}; /* {"IsRoot":true} */
            Apply<T1, T2> B = new Apply<T1, T2>() {InstanceName="B"}; /* {"IsRoot":false} */
            // END AUTO-GENERATED INSTANTIATIONS FOR Main

			// BEGIN AUTO-GENERATED WIRING FOR Main
            A.WireTo(B, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":""} */
            // END AUTO-GENERATED WIRING FOR Main
        }
    }
}