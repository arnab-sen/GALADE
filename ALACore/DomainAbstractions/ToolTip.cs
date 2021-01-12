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
    public class ToolTip : IUI // child
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public Func<string> GetLabel { get; set; }
        public string Label { get; set; } = "";

        // Private fields
        private System.Windows.Controls.ToolTip _toolTip = new System.Windows.Controls.ToolTip();

        // Ports
        private List<IEventHandler> eventHandlers = new List<IEventHandler>();

        private void OnWiringInitialize()
        {
            foreach (var eventHandler in eventHandlers)
            {
                if (eventHandler.Sender == null) eventHandler.Sender = _toolTip;
            }
        }

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            if (GetLabel != null)
            {
                _toolTip.Opened += (sender, args) => _toolTip.Content = GetLabel();
            }
            else
            {
                _toolTip.Content = Label;
            }

            return _toolTip;
        }

        public ToolTip()
        {

        }
    }
}
