using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// A UI element, just like a label with a text string on it. Two inputs:
    /// 1. IUI for getting the WPF element.
    /// 2. IDataFlow<string> for inputting the text.
    /// </summary>
    public class Text : IUI, IDataFlow<string>, IDataFlow<bool> // child, textInput, visible
    {
        // properties
        public string InstanceName { get; set; } = "Default";
        public Brush Color { get; set; } = Brushes.Black;
        public Brush Background { get; set; }
        public double HeightRatio { get; set; }
        public Thickness Margin { get; set; }
        public Thickness Padding { get; set; }
        public bool ShowBorder { get; set; } = false;

        // Properties for customizing UI
        public string InnerText
        {
            get => textBlock.Text;
            set => textBlock.Text = value;
        }

        public int Width
        {
            set => textBlock.Width = value;
        }

        public int Height
        {
            set => textBlock.Height = value;
        }

        public HorizontalAlignment HorizAlignment
        {
            set => textBlock.HorizontalAlignment = value;
        }

        public Visibility Visibility
        {
            set => textBlock.Visibility = value;
        }

        public TextAlignment TextAlignment
        {
            set => textBlock.TextAlignment = value;
        }

        public TextWrapping TextWrapping
        {
            set => textBlock.TextWrapping = value;
        }

        public FontWeight FontWeight
        {
            set => textBlock.FontWeight = value;
        }

        public FontStyle FontStyle
        {
            set => textBlock.FontStyle = value;
        }

        public double FontSize
        {
            set => textBlock.FontSize = value;
        }

        // private fields
        private UIElement wpfElement;
        private TextBlock textBlock = new TextBlock();

        /// <summary>
        /// An IUI abstraction which displays a given text.
        /// </summary>
        /// <param name="text">text displayed</param>
        /// <param name="visible">control the visibility of the text</param>
        public Text(string text = null, bool visible = true)
        {
            textBlock.Text = text;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            //textBlock.FontFamily = new FontFamily("Arial");
            textBlock.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        // IUI implementation ---------------------------------------------------------------
        UIElement IUI.GetWPFElement()
        {
            if (HeightRatio != 0)
            {
                textBlock.LayoutTransform = new ScaleTransform() { ScaleY = HeightRatio };
            }

            if (ShowBorder)
            {
                var border = new Border();
                border.Margin = Margin;
                border.Child = textBlock;
                border.BorderThickness = new Thickness(1);
                border.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 180, 180));
                textBlock.Padding = Padding;
                wpfElement = border;
            }
            else
            {
                textBlock.Margin = Margin;
                wpfElement = textBlock;
            }

            textBlock.Foreground = Color;
            if (Background != null) textBlock.Background = Background;

            return wpfElement;
        }

        // IDataFlow<string> implementation ---------------------------------------------------------------
        string IDataFlow<string>.Data
        {
            get => textBlock.Text;
            set => textBlock.Text = value;
        }

        // IDataFlow<bool> implementation -----------------------------------------------------------------
        bool IDataFlow<bool>.Data
        {
            get => wpfElement.Visibility == Visibility.Visible;
            set => wpfElement.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
