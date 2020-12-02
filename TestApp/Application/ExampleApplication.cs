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
            var a = new Apply<string, string>() {InstanceName="a"};
            var b = new Apply<string, string>() {InstanceName="b"};
            // END AUTO-GENERATED INSTANTIATIONS FOR test
			
			// BEGIN AUTO-GENERATED INSTANTIATIONS
			var _00 = new Object();
			var _01 = new Object();
			// END AUTO-GENERATED INSTANTIATIONS
			
			// BEGIN AUTO-GENERATED INSTANTIATIONS
			var _10 = new Object();
			var _11 = new Object();
			// END AUTO-GENERATED INSTANTIATIONS

			// BEGIN AUTO-GENERATED WIRING FOR test
            a.WireTo(b, "output");
			// END AUTO-GENERATED WIRING FOR test
			
			// BEGIN AUTO-GENERATED WIRING
			_00.WireTo(_01);
			// END AUTO-GENERATED WIRING
			
			// BEGIN AUTO-GENERATED WIRING
			_10.WireTo(_11);
			// END AUTO-GENERATED WIRING
        }
    }
}










