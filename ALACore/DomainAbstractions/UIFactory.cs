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
    public class UIFactory : IUI
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        // Private fields
        private Func<IUI> _getUIDelegate;

        // Ports
        private IDataFlow<object> uiInstanceOutput;

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            var uiContainer = _getUIDelegate();
            if (uiInstanceOutput != null) uiInstanceOutput.Data = uiContainer;

            return uiContainer.GetWPFElement();
        }

        // Methods

        public UIFactory(Func<IUI> getUIContainer)
        {
            _getUIDelegate = getUIContainer;
        }
    }
}
