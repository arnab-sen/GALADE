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
            Apply<T1, T2> A = new Apply<T1, T2>() {InstanceName="A",Lambda=() =>{    DoSomething();}}; /* {"IsRoot":true,"Description":"This is a description for an Apply abstraction."} */
            Apply<T1, T2> B = new Apply<T1, T2>() {InstanceName="B"}; /* {"IsRoot":false} */
            Apply<T1, T2> C = new Apply<T1, T2>() {InstanceName="C"}; /* {"IsRoot":false} */
            Apply<T1, T2> root = new Apply<T1, T2>() {InstanceName="root"}; /* {"IsRoot":true,"Description":"Test"} */
            EventConnector id_2b735401ff6a4988aab9c016891e0fa0 = new EventConnector() {InstanceName="id_2b735401ff6a4988aab9c016891e0fa0"}; /* {"IsRoot":false} */
            Data<T> id_d04edcbd894d4a23abbeb52df75a55a1 = new Data<T>() {}; /* {"IsRoot":false} */
            Apply<T1, T2> id_ae2fee627dd0454b9ff2292035f8a343 = new Apply<T1, T2>() {}; /* {"IsRoot":false} */
            // END AUTO-GENERATED INSTANTIATIONS FOR Main

			// BEGIN AUTO-GENERATED WIRING FOR Main
            A.WireTo(B, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"This is a description for a wire"} */
            A.WireTo(C, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":""} */
            C.WireTo(B, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"test"} */
            root.WireTo(A, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":""} */
            C.WireTo(id_2b735401ff6a4988aab9c016891e0fa0, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":false,"Description":""} */
            C.WireTo(id_d04edcbd894d4a23abbeb52df75a55a1, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"Data","DestinationIsReference":false,"Description":"test desc"} */
            C.WireTo(id_ae2fee627dd0454b9ff2292035f8a343, "output"); /* {"SourceType":"Apply","SourceIsReference":false,"DestinationType":"Apply","DestinationIsReference":false,"Description":"new desc"} */
            // END AUTO-GENERATED WIRING FOR Main
        }
    }
}