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
            var root0 = new Apply<T1, T2>() {InstanceName="Default"};
            var root1 = new Apply<T1, T2>() {InstanceName="Default"};
            var nonRoot0 = new Apply<T1, T2>() {InstanceName="Default"};
            var nonRoot1 = new Apply<T1, T2>() {InstanceName="Default"};
            var root2 = new Apply<T1, T2>() {InstanceName="root2"};
            // END AUTO-GENERATED INSTANTIATIONS FOR test

			// BEGIN AUTO-GENERATED WIRING FOR test
            root0.WireTo(nonRoot0, "output");
            root1.WireTo(nonRoot1, "output");
            nonRoot1.WireTo(root0, "output");
            nonRoot0.WireTo(root1, "output");
            root2.WireTo(root1, "lambdaInput");
			// END AUTO-GENERATED WIRING FOR test
		
        }
    }
}




















