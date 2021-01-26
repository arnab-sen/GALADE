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
    /// <para>Creates a menu item that opens to multiple menu items based on a list of strings, and outputs the string content of a menu item when clicked.</para>
    /// <para>Ports:</para>
    /// <para>1. IUI child: Returns the parent menu item with the child menu items added.</para>
    /// <para>2. IDataFlow&lt;List&lt;string&gt;&gt; itemLabels: Updates the parent menu item with new menu items containing the elements in the input list.</para>
    /// <para>3. IDataFlow&lt;string&gt; selectedLabel: Outputs the text content of the selected child menu item.</para>
    /// <para>4. IEvent isOpening: An event emitted right before the menu is opened.</para>
    /// </summary>
    public class MultiMenu : IUI, IDataFlow<List<string>> // child, itemLabels
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string InstanceDescription { get; set; } = "";
        public string ParentHeader { get; set; } = "Open";
        public List<string> Labels { get; set; }
        public bool RemoveEmptyLabels { get; set; } = true;

        // Private fields
        private System.Windows.Controls.MenuItem _parentMenuItem = new System.Windows.Controls.MenuItem();
        private ObservableCollection<object> _menuItems = new ObservableCollection<object>();

        // Ports
        private IDataFlow<string> selectedLabel;
        private IEvent isOpening;

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
                if (RemoveEmptyLabels && string.IsNullOrWhiteSpace(item)) continue;

                var menuItem = new System.Windows.Controls.MenuItem()
                {
                    Header = new TextBlock() { Text = item }
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
            _parentMenuItem.MouseEnter += (sender, args) => isOpening?.Execute();

        }
    }
}
