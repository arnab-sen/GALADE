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
    /// <para>IDataFlow&lt;UIElement&gt; uiElementInput: The UIElement to scale.</para>
    /// </summary>
    public class Scale : IDataFlow<UIElement> // uiElementInput
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        
        /// <summary>
        /// The scale to apply to the UIElement. If this is set to a value greater than 0, then neither of the multipliers will be used.
        /// If this is set to 0, then it will not be used, and the existing scale will be multiplied by the multipliers instead.
        /// </summary>
        public double AbsoluteScale { get; set; } = 0;
        public double WidthMultiplier { get; set; } = 1.0;
        public double HeightMultiplier { get; set; } = 1.0;
        public Func<Point> GetAbsoluteCentre { get; set; } = () => new Point(0, 0); // e.g. MouseWheelEventArgs.GetPosition(canvas)
        public Func<Point> GetScaleSensitiveCentre { get; set; } = () => new Point(0, 0); // e.g. Mouse.GetPosition(canvas)
        public bool MouseOnElement { get; set; } = true;
        public bool Reset { get; set; } = false;

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
            Point scaledCentre;
            Point centreDiff;

            if (AbsoluteScale > 0)
            {
                _currentXScale = AbsoluteScale;
                _currentYScale = AbsoluteScale;
            }
            else
            {
                _currentXScale *= WidthMultiplier;
                _currentYScale *= HeightMultiplier; 
            }

            var absCentre = GetAbsoluteCentre();

            ScaleTransform scaleTransform;
            TransformGroup transformGroup;
            TranslateTransform translateTransform;

            if (input.RenderTransform is TransformGroup group)
            {
                transformGroup = group;

                scaleTransform = transformGroup.Children.OfType<ScaleTransform>().FirstOrDefault();
                if (scaleTransform != null)
                {
                    if (AbsoluteScale > 0)
                    {
                        scaleTransform.ScaleX = AbsoluteScale;
                        scaleTransform.ScaleY = AbsoluteScale; 
                    }
                    else
                    {
                        scaleTransform.ScaleX *= WidthMultiplier;
                        scaleTransform.ScaleY *= HeightMultiplier; 
                    }
                    scaleTransform.CenterX = absCentre.X;
                    scaleTransform.CenterY = absCentre.Y;
                }
                else
                {
                    scaleTransform = new ScaleTransform(_currentXScale, _currentYScale, absCentre.X, absCentre.Y);
                    group.Children.Add(scaleTransform);
                }

                scaledCentre = GetScaleSensitiveCentre();
                centreDiff = new Point(scaledCentre.X - absCentre.X, scaledCentre.Y - absCentre.Y);
                translateTransform = transformGroup.Children.OfType<TranslateTransform>().FirstOrDefault();
                if (translateTransform != null)
                {
                    if (!Reset)
                    {
                        translateTransform.X += centreDiff.X;
                        translateTransform.Y += centreDiff.Y; 
                    }
                    else
                    {
                        translateTransform.X = 0;
                        translateTransform.Y = 0;
                    }
                }
                else
                {
                    translateTransform = !Reset ? new TranslateTransform(centreDiff.X, centreDiff.Y) : new TranslateTransform(0, 0);
                    group.Children.Add(translateTransform);
                }
            }
            else
            {
                transformGroup = new TransformGroup();
                var otherTransform = input.RenderTransform;
                if (!(otherTransform is ScaleTransform))
                {
                    scaleTransform = new ScaleTransform(_currentXScale, _currentYScale, absCentre.X, absCentre.Y);
                    transformGroup.Children.Add(scaleTransform); 
                }

                if (!(otherTransform is TranslateTransform))
                {
                    scaledCentre = GetScaleSensitiveCentre();
                    centreDiff = new Point(scaledCentre.X - absCentre.X, scaledCentre.Y - absCentre.Y);
                    translateTransform = new TranslateTransform(centreDiff.X, centreDiff.Y);
                    transformGroup.Children.Add(translateTransform); 
                }
                transformGroup.Children.Add(otherTransform);

                input.RenderTransform = transformGroup;
            }


        }

        public Scale()
        {

        }
    }
}
