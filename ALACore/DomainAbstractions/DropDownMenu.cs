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
    /// <summary>
    /// <para>A text box that has a dropdown menu attachment. The selected item from the dropdown is sent out
    /// whenever the dropdown is closed, keyboard focus is lost from the text box, or when the Enter key is pressed.</para>
    /// <para>If the currently selected text item is the same as the previously sent out value (if there is one), then the item will not be sent out.</para>
    /// <para>The dropdown can be forced to output its selected item by calling its Output method, although this is usually reserved for testing/debugging.
    /// The intention for this abstraction is for it to output data whenever the user makes a new selection.</para>
    /// <para>Ports:</para>
    /// <para>1. IUI child: returns the ComboBox as a UIElement</para>
    /// <para>2. IDataFlow&lt;List&lt;string&gt;&gt; items: The items to show in the dropdown menu.</para>
    /// <para>3. IDataFlow&lt;string&gt; text: The text to display in the text box. This does not cause the selected text to be sent out.</para>
    /// <para>4. IDataFlow&lt;string&gt; selectedItem: The output port for the selected text item.</para>
    /// </summary>
    public class DropDownMenu : IUI, IDataFlow<List<string>>, IDataFlow<string> // child, items, text
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
            get => _dropDown.Dispatcher.Invoke(() => _dropDown.Text);
            set
            {
                _dropDown.Dispatcher.Invoke(() => _dropDown.Text = value);
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
            set => _dropDown.MinWidth = value;
        }

        public double Height
        {
            get => _dropDown.Height;
            set => _dropDown.Height = value;
        }

        // Private fields
        private ComboBox _dropDown = new ComboBox();
        private IEnumerable<string> _items = new List<string>();
        private string _lastSentValue = "";

        // Ports
        private IDataFlow<string> selectedItem;
        // private IEvent eventEnterPressed;

        public DropDownMenu()
        {
            _dropDown.IsEditable = true;
            // _dropDown.SelectionChanged += (sender, args) =>
            // {
            //     if (selectedItem != null)
            //     {
            //         // selectedItem.Data = _dropDown.Text;
            //         selectedItem.Data = _dropDown.SelectedValue as string;
            //     }
            // };

            _dropDown.DropDownOpened += (sender, args) =>
            {
                (sender as ComboBox).Items.Refresh();
            };

            _dropDown.DropDownClosed += (sender, args) =>
            {
                if (_dropDown.Text != _lastSentValue)
                {
                    _lastSentValue = _dropDown.Text;
                    if (selectedItem != null) selectedItem.Data = _lastSentValue;
                }
                
            };

            _dropDown.StaysOpenOnEdit = true;

            // TextChangedEventHandler textChangedEventHandler = (sender, args) =>
            // {
            //     if (selectedItem != null) selectedItem.Data = _dropDown.Text;
            // };
            //
            // _dropDown.AddHandler(TextBoxBase.TextChangedEvent, textChangedEventHandler);

            _dropDown.KeyDown += (sender, args) =>
            {
                if (args.Key == Key.Enter && _dropDown.Text != _lastSentValue)
                {
                    // eventEnterPressed?.Execute();
                    _lastSentValue = _dropDown.Text;
                    if (selectedItem != null) selectedItem.Data = _lastSentValue;
                    Keyboard.ClearFocus();
                }
            };

            _dropDown.LostKeyboardFocus += (sender, args) =>
            {
                if (_dropDown.Text != _lastSentValue)
                {
                    _lastSentValue = _dropDown.Text;
                    if (selectedItem != null) selectedItem.Data = _lastSentValue;
                }
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
            set => _dropDown.Text = value;
        }

        // Methods
        private void SetItemSource(IEnumerable<string> items)
        {
            _items = items;
            _dropDown.ItemsSource = _items;
        }

        public void Output()
        {
            if (selectedItem != null) selectedItem.Data = _dropDown.Text;
        }
    }
}
