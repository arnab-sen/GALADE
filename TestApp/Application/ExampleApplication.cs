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

		public void CreateWiring()
		{
			
		}

        public ExampleDomainAbstraction(string arg0, string arg2 = "test")
        {		
			// BEGIN AUTO-GENERATED INSTANTIATIONS
            var id_798fc60f12954410b2c64f2f9dcb24a6 = new Apply<T1, T2>() {};
            var id_d86679376a34439a8a824907fcb2af49 = new ApplyAction<T>() {};
            var id_3be082c66e214d9eb85f3b1f717dd47f = new Apply<string, string>() {};
            var id_ecdcc130ff464419a29b6df5306a2049 = new DataFlowConnector<object>() {};
            // END AUTO-GENERATED INSTANTIATIONS
			
			// BEGIN AUTO-GENERATED WIRING
            id_798fc60f12954410b2c64f2f9dcb24a6.WireTo(id_d86679376a34439a8a824907fcb2af49, "lambdaInput");
			id_798fc60f12954410b2c64f2f9dcb24a6.WireTo(id_ecdcc130ff464419a29b6df5306a2049, "lambdaInput");
			id_798fc60f12954410b2c64f2f9dcb24a6.WireTo(id_3be082c66e214d9eb85f3b1f717dd47f, "lambdaInput");
			// END AUTO-GENERATED WIRING
        }
    }
}












































