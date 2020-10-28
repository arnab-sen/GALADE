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
        public Box SourcePort { get; set; }
        public Box DestinationPort { get; set; }

        // Private fields
        private Box rootUI;

        // Ports

        // Methods
        private Point GetCanvasPosition(UIElement element) => element.TranslatePoint(new Point(0, 0), Canvas);

        public void Paint()
        {
            var bezier = new BezierCurve();

            bezier.Point0 = GetCanvasPosition(SourcePort.Render); // Start
            bezier.Point3 = GetCanvasPosition(DestinationPort.Render); // End

            var midX= (bezier.Point0.X + bezier.Point0.X) / 2;

            bezier.Point1 = new Point(midX, bezier.Point0.Y);
            bezier.Point2 = new Point(midX, bezier.Point3.Y);

            var render = (bezier as IUI).GetWPFElement();

            Canvas.Children.Add(render);
            Canvas.SetLeft(render, 0);
            Canvas.SetTop(render, 0);
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
