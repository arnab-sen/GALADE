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
            MainWindow mainWindow = new MainWindow() { InstanceName = "mainWindow" };
            MenuBar menuBar = new MenuBar() { InstanceName = "menuBar" };
            MenuItem menuItem1 = new MenuItem(header: "") { InstanceName = "menuItem1" };
			
            mainWindow.WireTo(menuBar, "iuiStructure");
            menuBar.WireTo(menuItem1, "children");
			
			var a = new Data<string>() { storedData = "test", PerishCount = 20 };
			var b = new DataFlowConnector<string>();
			
			menuItem1.WireTo(a, "clickedEvent");
			a.WireTo(b, "dataOutput");
		}

        public ExampleDomainAbstraction(string arg0, string arg2 = "test")
        {		
			CreateWiring();
        }
    }
}








