using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// A placeholder abstraction that does nothing but maintain the tree structure in a GALADE diagram. This does not process data, so this should only really be used as a root.
    /// </summary>
    public class Placeholder : IEvent, IUI, IDataFlow<object> // inputEvent, child, inputData
    {
        // Ports
        private List<IEvent> eventOutputs;
        private List<IUI> children;
        private List<IDataFlow<object>> dataOutputs;

        void IEvent.Execute()
        {

        }

        UIElement IUI.GetWPFElement() => default;

        object IDataFlow<object>.Data { get; set; }

        public Placeholder()
        {

        }
    }
}
