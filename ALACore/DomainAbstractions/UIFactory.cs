using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    public class UIFactory : IUI // child
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public Func<IUI> GetUIContainer { get; set; }

        // Private fields

        // Ports
        private IDataFlow<object> uiInstanceOutput;

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            var uiContainer = GetUIContainer();
            if (uiInstanceOutput != null) uiInstanceOutput.Data = uiContainer;

            return uiContainer.GetWPFElement();
        }

        // Methods

        public UIFactory()
        {

        }
    }
}
