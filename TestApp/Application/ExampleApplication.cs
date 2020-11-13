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
		
		public void CreateWiring() // Wiring should always be in a CreateWiring method
		{		
            // BEGIN AUTO-GENERATED INSTANTIATIONS
            var id_bb9476c0448b4417bb857b437ab4687e = new Apply<T1, T2>() {};
            var id_6327733ea04148289bf5631ba122a37e = new Apply<T1, T2>() {};
            // END AUTO-GENERATED INSTANTIATIONS
			
			// BEGIN AUTO-GENERATED WIRING
            id_bb9476c0448b4417bb857b437ab4687e.WireTo(id_6327733ea04148289bf5631ba122a37e, "lambdaInput");
			// END AUTO-GENERATED WIRING
		}
		
		public void DoSomething()
		{
			
		}

        public ExampleDomainAbstraction(string arg0, string arg2 = "test")
        {		
			CreateWiring();
        }
    }
}











