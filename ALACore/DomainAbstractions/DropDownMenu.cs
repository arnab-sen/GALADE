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
        public string InstanceName = "Default";
        public bool SortItems = true;

        public string Text
        {
            get => dropDown.Dispatcher.Invoke(() => dropDown.Text);
            set
            {
                dropDown.Dispatcher.Invoke(() => dropDown.Text = value);
            }
        }

        public Thickness Margin
        {
            get => dropDown.Margin;
            set => dropDown.Margin = value;
        }

        public double Width
        {
            get => dropDown.Width;
            set => dropDown.Width = value;
        }

        public double Height
        {
            get => dropDown.Height;
            set => dropDown.Height = value;
        }

        // Private fields
        private ComboBox dropDown = new ComboBox();
        private List<string> items = new List<string>();

        // Ports
        private IDataFlow<string> selectedItem;
        private IEvent eventEnterPressed;

        public DropDownMenu()
        {
            dropDown.IsEditable = true;
            dropDown.SelectionChanged += (sender, args) =>
            {
                if (selectedItem != null)
                {
                    selectedItem.Data = dropDown.Text;
                }
            };

            TextChangedEventHandler textChangedEventHandler = (sender, args) =>
            {
                if (selectedItem != null) selectedItem.Data = dropDown.Text;
            };

            dropDown.KeyDown += (sender, args) =>
            {
                if (args.Key == Key.Enter) eventEnterPressed?.Execute();
            };

            dropDown.AddHandler(TextBoxBase.TextChangedEvent, textChangedEventHandler);
        }

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            return dropDown;
        }

        // IDataFlow<List<string>> implementation
        List<string> IDataFlow<List<string>>.Data
        {
            get => items;
            set
            {
                items = value;
                if (SortItems) items.Sort();
                dropDown.ItemsSource = items;
            }
        }

        // IDataFlow<string> implementation
        string IDataFlow<string>.Data
        {
            get => items.FirstOrDefault();
            set
            {
                dropDown.Text = value;

                if (selectedItem != null) selectedItem.Data = dropDown.Text;
            }
        }
    }
}
