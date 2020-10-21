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
        public object Payload { get; set; }

        // Private fields
        private UIElement _externalRender;
        private Border _visualContainer;

        // Ports
        private IUI uiLayout;

        // Methods
        public void Initialise()
        {
            _visualContainer = new Border();
            if (uiLayout != null) _externalRender = uiLayout.GetWPFElement();
            _visualContainer.Child = _externalRender;
        }

        public VisualNode()
        {
            Id = Utilities.GetUniqueId();
        }
    }
}
