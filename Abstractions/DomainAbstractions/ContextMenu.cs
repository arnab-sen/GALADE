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
    public class ContextMenu : IUI
    {
        // Public fields and properties
        public string InstanceName = "Default";

        // Private fields
        private System.Windows.Controls.ContextMenu contextMenu = new System.Windows.Controls.ContextMenu();

        // Ports
        private List<IUI> children = new List<IUI>();

        public ContextMenu()
        {

        }

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            foreach (var child in children)
            {
                contextMenu.Items.Add(child.GetWPFElement());
            }

            return contextMenu;
        }
    }
}
