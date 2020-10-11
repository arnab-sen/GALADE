using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    public class ToolTip : IUI
    {
        // Public fields and properties
        public string InstanceName = "Default";

        // Private fields
        private System.Windows.Controls.ToolTip toolTip = new System.Windows.Controls.ToolTip();

        // Ports
        private List<IEventHandler> eventHandlers = new List<IEventHandler>();

        public ToolTip()
        {

        }

        private void OnWiringInitialize()
        {
            foreach (var eventHandler in eventHandlers)
            {
                if (eventHandler.Sender == null) eventHandler.Sender = toolTip;
            }
        }

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            return toolTip;
        }
    }
}
