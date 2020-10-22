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
    public class Box : IUI
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        public double Width
        {
            get => _uiContainer.Width;
            set => _uiContainer.Width = value;
        }

        public double Height
        {
            get => _uiContainer.Height;
            set => _uiContainer.Height = value;
        }
        
        public Brush Background
        {
            get => _uiContainer.Background;
            set => _uiContainer.Background = value;
        }

        public Brush BorderColour
        {
            get => _uiContainer.BorderBrush;
            set => _uiContainer.BorderBrush = value;
        }

        public Thickness BorderThickness
        {
            get => _uiContainer.BorderThickness;
            set => _uiContainer.BorderThickness = value;
        }

        public CornerRadius CornerRadius
        {
            get => _uiContainer.CornerRadius;
            set => _uiContainer.CornerRadius = value;
        }

        // Private fields
        private Border _uiContainer;

        // Ports
        private IUI child;
        private List<IEventHandler> eventHandlers = new List<IEventHandler>();

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            _uiContainer.Child = child?.GetWPFElement();
            return _uiContainer;
        }

        // Methods
        public void InitialiseUI()
        {
            (this as IUI).GetWPFElement();
            PostWiringInitialize();
        }

        private void PostWiringInitialize()
        {
            foreach (var eventHandler in eventHandlers)
            {
                eventHandler.Sender = _uiContainer;
            }
        }

        public Box()
        {
            _uiContainer = new Border()
            {
                Background = Brushes.LightBlue,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(2)
            };
        }
    }
}
