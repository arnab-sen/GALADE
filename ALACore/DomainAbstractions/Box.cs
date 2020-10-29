using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// 
    /// </summary>
    public class Box : IUI
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        public double Width
        {
            get => Render.ActualWidth;
            set => Render.MinWidth = value;
        }

        public double Height
        {
            get => Render.ActualHeight;
            set => Render.MinHeight = value;
        }
        
        public Brush Background
        {
            get => Render.Background;
            set => Render.Background = value;
        }

        public Brush BorderColour
        {
            get => Render.BorderBrush;
            set => Render.BorderBrush = value;
        }

        public Thickness BorderThickness
        {
            get => Render.BorderThickness;
            set => Render.BorderThickness = value;
        }

        public CornerRadius CornerRadius
        {
            get => Render.CornerRadius;
            set => Render.CornerRadius = value;
        }

        public object Payload { get; set; }

        public Border Render { get; set; } = new Border();

        // Private fields

        // Ports
        private IUI uiLayout;
        private List<IEventHandler> eventHandlers = new List<IEventHandler>();

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            if (uiLayout != null) Render.Child = uiLayout.GetWPFElement();
            SendToEventHandlers();

            return Render;
        }

        // Methods
        public void InitialiseUI()
        {
            (this as IUI).GetWPFElement();
        }

        private void SendToEventHandlers()
        {
            foreach (var eventHandler in eventHandlers)
            {
                eventHandler.Sender = this;
            }
        }

        public object GetPayload() => Payload;

        public Box()
        {
            Render = new Border()
            {
                Background = Brushes.LightBlue,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Focusable = true
            };
        }
    }
}
