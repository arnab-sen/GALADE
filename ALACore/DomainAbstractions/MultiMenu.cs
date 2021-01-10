using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// <para>Creates multiple menu items based on a list of strings, and outputs the string content of a menu item when clicked.</para>
    /// </summary>
    public class MultiMenu : IUI, IDataFlow<List<string>> // child, itemLabels
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string InstanceDescription { get; set; } = "";
        public string ParentHeader { get; set; } = "Open";
        public List<string> Labels { get; set; }

        // Private fields
        private System.Windows.Controls.MenuItem _parentMenuItem = new System.Windows.Controls.MenuItem();
        private ObservableCollection<object> _menuItems = new ObservableCollection<object>();

        // Ports
        private IDataFlow<string> selectedLabel;

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            if (Labels != null) UpdateMenuItems(Labels);
            _parentMenuItem.Header = ParentHeader;

            return _parentMenuItem;
        }

        // IDataFlow<List<string>> implementation
        List<string> IDataFlow<List<string>>.Data
        {
            get => Labels;
            set
            {
                Labels = value;
                UpdateMenuItems(Labels);
            }
        }

        // Methods
        private void UpdateMenuItems(List<string> labels)
        {
            _menuItems.Clear();

            foreach (var item in labels)
            {
                var menuItem = new System.Windows.Controls.MenuItem()
                {
                    Header = item
                };

                _menuItems.Add(menuItem);

                menuItem.Click += (sender, args) =>
                {
                    if (selectedLabel != null) selectedLabel.Data = item;
                };
            }
        }

        public MultiMenu()
        {
            _parentMenuItem.ItemsSource = _menuItems;
        }
    }
}
