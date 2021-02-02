using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;
using ProgrammingParadigms;
using System.Windows.Media;

namespace DomainAbstractions
{
    public class MenuBar : System.Windows.Controls.Menu, IUI // child
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        // Private fields

        // Ports
        private List<IUI> children = new List<IUI>();

        public MenuBar()
        {
            
        }

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            foreach (var child in children)
            {
                this.Items.Add(child.GetWPFElement());
            }

            return this;
        }

    }
}
