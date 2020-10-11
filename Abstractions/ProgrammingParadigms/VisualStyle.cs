using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Libraries;

namespace ProgrammingParadigms
{
    /// <summary>
    /// <para></para>
    /// </summary>
    public class VisualStyle
    {
        // Public fields and properties
        public string InstanceName = "Default";
        public Brush Background { get; set; } = Brushes.White;
        public Brush BackgroundHighlight { get; set; } = Brushes.White;
        public Brush Foreground { get; set; } = Brushes.Black;
        public Brush ForegroundHighlight { get; set; } = Brushes.Black;
        public Brush Border { get; set; } = Brushes.Black;
        public Brush BorderHighlight { get; set; } = Brushes.Black;
        public int BorderThickness { get; set; } = 1;
        public int Width { get; set; }
        public int Height { get; set; }

        // Private fields
        
        
        /// <summary>
        /// <para></para>
        /// </summary>
        public VisualStyle()
        {
            
        }
    }
}
