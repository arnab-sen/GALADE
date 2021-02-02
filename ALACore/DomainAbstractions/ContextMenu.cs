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
    /// <summary>
    /// A UI container than can hold interactive items in a context menu.
    /// </summary>
    public class ContextMenu : IUI, IEvent // child, open
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public System.Windows.Controls.ContextMenu Menu { get; set; } = new System.Windows.Controls.ContextMenu();

        // Private fields

        // Ports
        private List<IUI> children = new List<IUI>();
        private IEvent opened;

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            foreach (var child in children)
            {
                Menu.Items.Add(child.GetWPFElement());
            }

            return Menu;
        }

        // IEvent implementation
        void IEvent.Execute()
        {
            Open();
        }

        // Methods
        public void Open()
        {
            Menu.IsOpen = true;
        }

        public ContextMenu()
        {
            Menu.Opened += (sender, args) => opened?.Execute();
        }
    }
}
