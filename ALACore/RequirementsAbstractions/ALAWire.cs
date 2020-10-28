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
using DomainAbstractions;

namespace RequirementsAbstractions
{
    public class ALAWire
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string Label { get; set; } = "";
        public Graph Graph { get; set; }
        public Canvas Canvas { get; set; }
        public UIElement Render { get; set; }
        public object Source { get; set; }
        public object Destination { get; set; }
        public Box SourcePort { get; set; }
        public Box DestinationPort { get; set; }

        // Private fields
        private Box rootUI;
        private BezierCurve _bezier;

        // Ports

        // Methods
        private Point GetCanvasPosition(UIElement element) => element.TranslatePoint(new Point(0, 0), Canvas);

        /// <summary>
        /// Create the curve and add it to the canvas.
        /// </summary>
        public void Paint()
        {
            _bezier = new BezierCurve();

            Refresh();

            var render = (_bezier as IUI).GetWPFElement();

            Canvas.Children.Add(render);
            Canvas.SetLeft(render, 0);
            Canvas.SetTop(render, 0);
        }

        /// <summary>
        /// Have the curve check its start and end points and update accordingly.
        /// </summary>
        public void Refresh()
        {
            _bezier.Point0 = GetCanvasPosition(SourcePort.Render); // Start
            _bezier.Point3 = GetCanvasPosition(DestinationPort.Render); // End

            var midX = (_bezier.Point0.X + _bezier.Point0.X) / 2;

            _bezier.Point1 = new Point(midX, _bezier.Point0.Y);
            _bezier.Point2 = new Point(midX, _bezier.Point3.Y);
        }

        private void SetWiring()
        {
            rootUI = new Box() { Background = Brushes.Transparent };

            // BEGIN AUTO-GENERATED INSTANTIATIONS
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            // END AUTO-GENERATED WIRING

            Render = (rootUI as IUI).GetWPFElement();
        }

        public void CreateInternals()
        {
            SetWiring();
        }

        public ALAWire()
        {

        }
    }
}
