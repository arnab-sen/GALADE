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
    public class MenuItem : IUI // child
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        // Private fields
        private System.Windows.Controls.MenuItem _menuItem = new System.Windows.Controls.MenuItem();

        // Ports
        private List<IUI> children = new List<IUI>();
        private IEvent clickedEvent;
        private IUI icon;

        public MenuItem(string header = "")
        {
            _menuItem.Header = header;
            _menuItem.FontSize = 12;
        }

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            foreach (var child in children)
            {
                _menuItem.Items.Add(child.GetWPFElement());
            }

            _menuItem.Click += (sender, args) => clickedEvent?.Execute();

            if (icon != null) _menuItem.Icon = icon.GetWPFElement();

            return _menuItem;
        }
    }
}
