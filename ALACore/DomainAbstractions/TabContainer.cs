using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// The abstraction is a UI element of TabControl for the containment of tabs.
    /// Margin can be specified for customisation.
    /// ------------------------------------------------------------------------------------------------------------------
    /// Ports:
    /// IUI "inputIUI": input IUI to get the WPF element
    /// List<IUI> "childrenTabs": output list of all the children tabs within this tab container
    /// </summary>

    public class TabContainer : IUI // child
    {
        //outputs
        private List<IUI> childrenTabs = new List<IUI>();

        // properties
        public string InstanceName { get; set; } = "Default";
        public Thickness Margin { set => tabControl.Margin = value; }

        //private fields
        private TabControl tabControl;

        /// <summary>
        /// UI element for containment of tabs
        /// </summary>
        public TabContainer()
        {
            tabControl = new TabControl();
        }

        // IUI implementation ------------------------------------------------------
        // Adds all the children tabs to the tab control container
        UIElement IUI.GetWPFElement()
        {
            foreach (var c in childrenTabs) tabControl.Items.Add(c.GetWPFElement());
            return tabControl;
        }
    }
}
