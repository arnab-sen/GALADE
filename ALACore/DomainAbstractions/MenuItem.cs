using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    public class MenuItem : System.Windows.Controls.MenuItem,  IUI
    {
        // Public fields and properties
        public string InstanceName = "Default";

        // Private fields

        // Ports
        private List<IUI> children = new List<IUI>();
        private IEvent clickedEvent;

        public MenuItem(string header = "")
        {
            Header = header;
            FontSize = 12;
        }

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            foreach (var child in children)
            {
                this.Items.Add(child.GetWPFElement());
            }

            Click += (sender, args) => clickedEvent?.Execute();

            return this;
        }
    }
}
