using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>Scales a UIElement by a given multiplicative factor.</para>
    /// <para>Ports:</para>
    /// <para>IDataFlow&lt;UIElement&gt; input: The UIElement to scale.</para>
    /// </summary>
    public class Scale : IDataFlow<UIElement>
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public double WidthMultiplier { get; set; } = 1.0;
        public double HeightMultiplier { get; set; } = 1.0;

        // Private fields
        private UIElement _ui;
        private double _currentXScale = 1.0;
        private double _currentYScale = 1.0;

        // Ports
        
        // IDataFlow<UIElement> implementation
        UIElement IDataFlow<UIElement>.Data
        {
            get => _ui;
            set
            {
                _ui = value;
                ScaleElement(_ui);
            }
        }

        // Methods
        private void ScaleElement(UIElement input)
        {
            _currentXScale *= WidthMultiplier;
            _currentYScale *= HeightMultiplier;

            if (input.RenderTransform is ScaleTransform currentTransform)
            {
                input.RenderTransform = new ScaleTransform(currentTransform.ScaleX * WidthMultiplier, currentTransform.ScaleY * HeightMultiplier);
            }
            else
            {
                input.RenderTransform = new ScaleTransform(_currentXScale, _currentXScale);
            }
        }

        public Scale()
        {

        }
    }
}
