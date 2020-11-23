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
    /// Displays a vertical list of strings that can be selected. 
    /// </summary>
    public class ListDisplay : IUI, IDataFlow<List<string>>
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string InstanceDescription { get; set; } = "";
        
        // Private fields
        private ListBox _listBox = new ListBox();

        // Ports
        private IDataFlow<string> selectedItem;
        private IDataFlow<int> selectedIndex;

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            return _listBox;
        }

        // IDataFlow<List<string>> implementation
        List<string> IDataFlow<List<string>>.Data
        {
            get => _listBox.ItemsSource as List<string>;
            set => _listBox.ItemsSource = value;
        }

        // Methods

        public ListDisplay()
        {
            _listBox.SelectionChanged += (sender, args) =>
            {
                var i = 0;
                while (args.AddedItems.GetEnumerator().MoveNext())
                {
                    if (selectedItem != null) selectedItem.Data = args.AddedItems[i].ToString();
                    if (selectedIndex != null) selectedIndex.Data = i;
                    i++;
                }
            };
        }
    }
}
