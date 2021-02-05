using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// A popup that appears above all other elements.
    /// </summary>
    public class PopupBox : IEvent // toggle
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public Func<UIElement> GetPlacementObject { get; set; }
        public PlacementMode PlacementMode { get; set; } = PlacementMode.MousePoint;
        public bool StaysOpen { get; set; } = true;

        // Private fields
        private Popup _popup = new Popup();
        private UIElement _childContent = null;

        // Ports
        private IUI child;

        // IEvent implementation
        void IEvent.Execute()
        {
            if (!_popup.IsOpen)
            {
                if (_childContent == null && child != null)
                {
                    _childContent = child.GetWPFElement();
                    _popup.Child = _childContent;
                }

                if (GetPlacementObject != null) _popup.PlacementTarget = GetPlacementObject();
                _popup.Placement = PlacementMode;
                _popup.IsOpen = true;
                _popup.PlacementRectangle = new Rect(new Point(), new Point(10, 10));
                _popup.StaysOpen = StaysOpen;
            }
            else
            {
                _popup.IsOpen = false;
            }
        }

        // Methods

        public PopupBox()
        {

        }
    }
}
