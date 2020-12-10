using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>A UI CheckBox that can contain a single child item, and outputs a boolean representing whether the CheckBox is checked, whenever that state is changed.</para>
    /// <para>Ports:</para>
    /// <para>1. IUI child: returns the internal CheckBox control.</para>
    /// <para>2. IEvent toggle: Flips the current selection state.</para>
    /// <para>3. IUI content: Gets the child UI content for the internal CheckBox.</para>
    /// <para>4. IDataFlow&lt;bool&gt; isChecked: Outputs whether the internal CheckBox is checked.</para>
    /// </summary>
    public class CheckBox : IUI, IEvent // child, toggle
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string InstanceDescription { get; set; } = "Default";

        // Private fields
        private System.Windows.Controls.CheckBox _checkBox = new System.Windows.Controls.CheckBox();

        // Ports
        private IUI content;
        private IDataFlow<bool> isChecked;
        private IEventB check;
        private IEventB uncheck;

        // Methods

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            _checkBox.Content = content?.GetWPFElement();
            return _checkBox;
        }

        // IEvent implementation
        void IEvent.Execute()
        {
            Toggle();
        }

        private void PostWiringInitialize()
        {
            if (check != null) check.EventHappened += () => Change(true);
            if (uncheck != null) uncheck.EventHappened += () => Change(false);
        }

        public void Change(bool state)
        {
            _checkBox.IsChecked = state;
        }

        public void Toggle()
        {
            Change(!_checkBox.IsChecked ?? false);
        }

        public CheckBox(bool check = false)
        {
            _checkBox.IsChecked = check;

            _checkBox.Checked += (sender, args) =>
            {
                if (isChecked != null) isChecked.Data = true;
            };

            _checkBox.Unchecked += (sender, args) =>
            {
                if (isChecked != null) isChecked.Data = false;
            };
        }
    }
}
