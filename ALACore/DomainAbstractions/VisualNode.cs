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
    /// 
    /// </summary>
    public class VisualNode
    {
        // Public fields and properties
        public string Id { get; set; }
        public string Type { get; set; } = "DefaultNode";
        public string Name { get; set; }
        public Dictionary<string, object> Payload { get; set; }
        public UIElement Render { get; set; }

        // Private fields

        // Ports
        private IUI uiLayout;

        // Methods
        public void InitialiseUI()
        {
            if (uiLayout != null) Render = uiLayout.GetWPFElement();
        }

        public VisualNode()
        {
            Id = Utilities.GetUniqueId();
        }
    }
}
