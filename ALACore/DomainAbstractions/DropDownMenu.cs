using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Libraries;
using ProgrammingParadigms;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace DomainAbstractions
{
    public class DropDownMenu : IUI, IDataFlow<List<string>>, IDataFlow<string>
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public bool SortItems { get; set; } = true;

        public IEnumerable<string> Items
        {
            get => _items;
            set => SetItemSource(value);
        }

        public string Text
        {
            get => "";// _dropDown.Dispatcher.Invoke(() => _dropDown.SelectedValue.ToString());
            set
            {
                //_dropDown.Dispatcher.Invoke(() => _dropDown.SelectedValue = value);
            }
        }

        public Thickness Margin
        {
            get => _dropDown.Margin;
            set => _dropDown.Margin = value;
        }

        public double Width
        {
            get => _dropDown.Width;
            set => _dropDown.Width = value;
        }

        public double Height
        {
            get => _dropDown.Height;
            set => _dropDown.Height = value;
        }

        // Private fields
        private ComboBox _dropDown = new ComboBox();
        private IEnumerable<string> _items = new List<string>();

        // Ports
        private IDataFlow<string> selectedItem;
        private IEvent eventEnterPressed;

        public DropDownMenu()
        {
            _dropDown.IsEditable = true;
            _dropDown.SelectionChanged += (sender, args) =>
            {
                if (selectedItem != null)
                {
                    selectedItem.Data = _dropDown.Text;
                }
            };

            _dropDown.StaysOpenOnEdit = true;

            _dropDown.PreviewMouseLeftButtonDown += (sender, args) =>
            {
            };

            _dropDown.PreviewLostKeyboardFocus += (sender, args) =>
            {
                
            };

            TextChangedEventHandler textChangedEventHandler = (sender, args) =>
            {
                if (selectedItem != null) selectedItem.Data = _dropDown.Text;
            };

            _dropDown.AddHandler(TextBoxBase.TextChangedEvent, textChangedEventHandler);


            _dropDown.KeyDown += (sender, args) =>
            {
                if (args.Key == Key.Enter) eventEnterPressed?.Execute();
            };

        }

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            return _dropDown;
        }

        // IDataFlow<List<string>> implementation
        List<string> IDataFlow<List<string>>.Data
        {
            get => _items.ToList();
            set => SetItemSource(value);
        }

        // IDataFlow<string> implementation
        string IDataFlow<string>.Data
        {
            get => _items.FirstOrDefault();
            set
            {
                _dropDown.Text = value;

                if (selectedItem != null) selectedItem.Data = _dropDown.Text;
            }
        }

        // Methods
        private void SetItemSource(IEnumerable<string> items)
        {
            _items = items;
            _dropDown.ItemsSource = _items;
        }
    }
}
