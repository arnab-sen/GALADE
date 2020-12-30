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
    /// Displays a vertical list of strings that can be selected. 
    /// </summary>
    public class ListDisplay : IUI, IDataFlow<ObservableCollection<string>> // child, items
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string InstanceDescription { get; set; } = "";

        public bool CanSelectMultiple
        {
            get => _canSelectMultiple;
            set
            {
                _canSelectMultiple = value;
                _listBox.SelectionMode = _canSelectMultiple ? SelectionMode.Extended : SelectionMode.Single;
            }
        }

        public ObservableCollection<string> ItemList
        {
            get => _listBox.ItemsSource as ObservableCollection<string>;
            set => _listBox.ItemsSource = value;
        }

        // Private fields
        private ListBox _listBox = new ListBox()
        {
            SelectionMode = SelectionMode.Single
        };

        private bool _canSelectMultiple = false;

        // Ports
        private IDataFlow<string> selectedItem;
        private IDataFlow<int> selectedIndex;

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            return _listBox;
        }

        // IDataFlow<List<string>> implementation
        ObservableCollection<string> IDataFlow<ObservableCollection<string>>.Data
        {
            get => _listBox.ItemsSource as ObservableCollection<string>;
            set => _listBox.ItemsSource = value;
        }

        // Methods
        private void Output()
        {
            if (_listBox.SelectedValue == null) return;

            if (selectedItem != null) selectedItem.Data = _listBox.SelectedValue.ToString();
            if (selectedIndex != null) selectedIndex.Data = _listBox.SelectedIndex;
        }

        public ListDisplay()
        {
            _listBox.PreviewMouseLeftButtonDown += (sender, args) =>
            {
                Output();
            };

            _listBox.SelectionChanged += (sender, args) =>
            {
                Output();
            };
        }
    }
}
