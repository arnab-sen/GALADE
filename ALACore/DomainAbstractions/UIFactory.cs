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
    /// <summary>
    /// Creates and returns a UI instance. Either GetUIContainer or GetUIElement must be defined.
    /// </summary>
    public class UIFactory : IUI // child
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public Func<IUI> GetUIContainer { get; set; }
        public Func<UIElement> GetUIElement { get; set; }

        // Private fields

        // Ports
        private IDataFlow<object> uiInstanceOutput;

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            if (GetUIContainer != null)
            {
                var uiContainer = GetUIContainer();
                if (uiInstanceOutput != null) uiInstanceOutput.Data = uiContainer;

                return uiContainer.GetWPFElement(); 
            }
            else
            {
                var uiElement = GetUIElement();
                if (uiInstanceOutput != null) uiInstanceOutput.Data = uiElement;

                return uiElement;
            }
        }

        // Methods

        public UIFactory()
        {

        }
    }
}
