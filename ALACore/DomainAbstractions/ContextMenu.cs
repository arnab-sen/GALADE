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
        public string InstanceName = "Default";

        // Private fields
        private System.Windows.Controls.ContextMenu _contextMenu = new System.Windows.Controls.ContextMenu();

        // Ports
        private List<IUI> children = new List<IUI>();

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            foreach (var child in children)
            {
                _contextMenu.Items.Add(child.GetWPFElement());
            }

            return _contextMenu;
        }

        // IEvent implementation
        void IEvent.Execute()
        {
            Open();
        }

        // Methods
        public void Open()
        {
            _contextMenu.IsOpen = true;
        }

        public ContextMenu()
        {

        }
    }
}
