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
            Apply<T1, T2> A = new Apply<T1, T2>() {InstanceName="A",Lambda=() =>{    DoSomething();}}; /* {"IsRoot":true} */
            Apply<T1, T2> B = new Apply<T1, T2>() {InstanceName="B"}; /* {"IsRoot":false} */
            Apply<T1, T2> id_a916724af200415c857a4e58062ce29f = new Apply<T1, T2>() {InstanceName="A",Lambda=() =>{    DoSomethingElse();}}; /* {"IsRoot":false} */
            Apply<T1, T2> id_e2677bedbcf7485ca29b7816d96fa8fa = new Apply<T1, T2>() {InstanceName="A",Lambda=() =>{    DoSomethingNew();}}; /* {"IsRoot":false} */
            Apply<T1, T2> id_9a6b0de72c514a35b5b469c35298cbb1 = new Apply<T1, T2>() {InstanceName="id_9a6b0de72c514a35b5b469c35298cbb1",Lambda=default}; /* {"IsRoot":false} */
            // END AUTO-GENERATED INSTANTIATIONS FOR test

			// BEGIN AUTO-GENERATED WIRING FOR test
            A.WireTo(B, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false} */
            B.WireTo(id_a916724af200415c857a4e58062ce29f, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false} */
            B.WireTo(id_e2677bedbcf7485ca29b7816d96fa8fa, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false} */
            B.WireTo(id_9a6b0de72c514a35b5b469c35298cbb1, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false} */
            // END AUTO-GENERATED WIRING FOR test
        }
    }
}