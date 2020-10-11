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
    public class DropDownMenu : ComboBox, IUI, IDataFlow<List<string>>, IDataFlow<string>
    {
        // Public fields and properties
        public string InstanceName = "Default";
        public bool SortItems = true;

        // Private fields
        private List<string> items = new List<string>();

        // Ports
        private IDataFlow<string> selectedItem;
        private IEvent eventEnterPressed;

        public DropDownMenu()
        {
            IsEditable = true;
            SelectionChanged += (sender, args) =>
            {
                if (selectedItem != null)
                {
                    selectedItem.Data = Text;
                }
            };

            TextChangedEventHandler textChangedEventHandler = (sender, args) =>
            {
                if (selectedItem != null) selectedItem.Data = Text;
            };

            KeyDown += (sender, args) =>
            {
                if (args.Key == Key.Enter) eventEnterPressed?.Execute();
            };

            AddHandler(TextBoxBase.TextChangedEvent, textChangedEventHandler);
        }

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            return this;
        }

        // IDataFlow<List<string>> implementation
        List<string> IDataFlow<List<string>>.Data
        {
            get => items;
            set
            {
                items = value;
                if (SortItems) items.Sort();
                ItemsSource = items;
            }
        }

        // IDataFlow<string> implementation
        string IDataFlow<string>.Data
        {
            get => items.FirstOrDefault();
            set
            {
                Text = value;

                if (selectedItem != null) selectedItem.Data = Text;
            }
        }
    }
}
