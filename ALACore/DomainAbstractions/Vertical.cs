using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// Arranges contained UI elements vertically.
    /// Automatically sizes the widths to be the same as Vertical, 
    /// but heights to be shared according to the property Layouts of the contain elements fixed size.
    /// Using 2 to share the height in average, or 0 to auto size based on the sub-elements.
    /// </summary>
    public class Vertical : IUI, IDataFlow<bool>, IEvent // child, visible, clear
    {
        // properties
        public string InstanceName { get; set; } = "Default";

        /// <summary>
        /// Layout of it's sub elements, 0 for auto sizing, 2 for averagely sharing
        /// </summary>
        public int[] Layouts { get; set; }  = new int[0];
        public Thickness Margin { set => gridPanel.Margin = value; }
        public Visibility Visibility { set => gridPanel.Visibility = value; }
        public HorizontalAlignment? HorizAlignment;
        public double Height { set => gridPanel.Height = value; }

        public bool VerticalScrollBarVisible
        {
            get => scrollViewer.VerticalScrollBarVisibility == ScrollBarVisibility.Visible;
            set
            {
                scrollViewer.VerticalScrollBarVisibility = value ? ScrollBarVisibility.Visible : ScrollBarVisibility.Hidden;
            }
        }

        public bool HorizontalScrollBarVisible
        {
            set
            {
                scrollViewer.HorizontalScrollBarVisibility = value ? ScrollBarVisibility.Visible : ScrollBarVisibility.Hidden;
            }
        }

        // Ports

        /// <summary>
        /// The children to lay out vertically.
        /// </summary>
        private List<IUI> children = new List<IUI>();
        private List<IEventHandler> eventHandlers = new List<IEventHandler>();

        // private fields
        private System.Windows.Controls.Grid gridPanel = new System.Windows.Controls.Grid();
        private ScrollViewer scrollViewer = new ScrollViewer();

        /// <summary>
        /// A layout IUI which arranges it's sub-elements vertically.
        /// Layouts properties must be assigned, '0' represents Auto Sizing, '2' represents fill parent.
        /// An example of 3 sub-elements layout: [0, 0, 2]
        /// </summary>
        public Vertical(bool visible = true)
        {
            gridPanel.ShowGridLines = false;
            gridPanel.ColumnDefinitions.Add(new ColumnDefinition() {
                Width = new GridLength(1, GridUnitType.Star)
            });
            gridPanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
        }

        // IUI implementation -------------------------------------------------------
        UIElement IUI.GetWPFElement()
        {
            gridPanel.Children.Clear();

            if (Layouts.Length < children.Count)
            {
                var newLayout = new int[children.Count];

                for (int i = 0; i < Layouts.Length; i++)
                {
                    newLayout[i] = Layouts[i];
                }

                for (int i = Layouts.Length; i < children.Count; i++)
                {
                    newLayout[i] = (int)GridUnitType.Auto;
                }

                Layouts = newLayout;
            }

            for (var i = 0; i < children.Count; i++)
            {
                GridUnitType type = (GridUnitType)Layouts[i];
                gridPanel.RowDefinitions.Add(new RowDefinition() {
                    Height = new GridLength(1, type)
                });
                var e = children[i].GetWPFElement();
                if (e is FrameworkElement && HorizAlignment != null) (e as FrameworkElement).HorizontalAlignment = (HorizontalAlignment)HorizAlignment;
                gridPanel.Children.Add(e);
                System.Windows.Controls.Grid.SetRow(e, i);
                System.Windows.Controls.Grid.SetColumn(e, 0);
            }

            if (VerticalScrollBarVisible)
            {
                scrollViewer.Content = gridPanel;
                return scrollViewer;
            }
            else
            {
                return gridPanel;
            }
        }

        // IDataFlow<bool> implementation
        bool IDataFlow<bool>.Data
        {
            get => gridPanel.Visibility == Visibility.Visible;
            set => gridPanel.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }

        // IEvent implementation
        void IEvent.Execute() // Refresh UI
        {
            gridPanel.Children.Clear();
            // (this as IUI).GetWPFElement();
        }

        private void PostWiringInitialize()
        {
            foreach (var eventHandler in eventHandlers)
            {
                eventHandler.Sender = gridPanel;
            }
        }
    }
}
