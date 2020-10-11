using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// The UI elements will be sized automatically and arranged horizontally.
    /// The container of the Horizontal is a Grid which can organize the UI elements in a 
    /// table-style. Here the Horizontal is a specific Grid which has only one Row.
    /// The layout can sized with the Ratio property.
    /// ------------------------------------------------------------------------------------------------------------------
    /// Ports:
    /// 1. IUI "inputIUI": input IUI to get the WPF element
    /// 2. IDataFlow<bool> "NEEDNAME": input boolean value that the visibility (visible or collapsed)
    /// 3. List<IUI> "childrenTabs": output list of IU elements contained in this Horizontal
    /// </summary>
    public class Horizontal : IUI, IDataFlow<bool>, IEvent
    {
        // properties ---------------------------------------------------------------------
        public string InstanceName = "Default";
        public int[] Ratios { get; set; }
        public Thickness Margin { set => gridPanel.Margin = value; }
        public Brush Background;
        public Visibility Visibility { set => gridPanel.Visibility = value;}
        public HorizontalAlignment HorizAlignment
        {
            set => gridPanel.HorizontalAlignment = value;
        }
        public VerticalAlignment VertAlignment
        {
            set => gridPanel.VerticalAlignment = value;
        }

        // ports ---------------------------------------------------------------------
        private List<IUI> children = new List<IUI>();

        // private fields ---------------------------------------------------------------------
        private System.Windows.Controls.Grid gridPanel = new System.Windows.Controls.Grid();

        /// <summary>
        /// A layout IUI which arranges it's sub-elements horizontally and can be controlled with a Ratio property.
        /// </summary>
        public Horizontal(bool visible = true)
        {
            gridPanel.ShowGridLines = false;
            gridPanel.RowDefinitions.Add(new RowDefinition() {
                Height = new GridLength(1, GridUnitType.Star)
            });
            gridPanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        // IUI implmentation -----------------------------------------------------------
        UIElement IUI.GetWPFElement()
        {
            if (Background != null) gridPanel.Background = Background;

            for (var i = 0; i < children.Count; i++)
            {
                var r = (Ratios != null && i < Ratios.Length) ? Ratios[i] : 100;
                gridPanel.ColumnDefinitions.Add(new ColumnDefinition() {
                    Width = new GridLength(r, GridUnitType.Star)
                });

                var e = children[i].GetWPFElement();
                gridPanel.Children.Add(e);
                System.Windows.Controls.Grid.SetColumn(e, i);
                System.Windows.Controls.Grid.SetRow(e, 0);
            }

            return gridPanel;
        }

        // IDataFlow<bool> implementation ---------------------------------------------------------
        bool IDataFlow<bool>.Data
        {
            get => gridPanel.Visibility == Visibility.Visible;
            set => gridPanel.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }

        // IEvent implementation
        void IEvent.Execute() // Refresh UI
        {
            gridPanel.Children.Clear();
            (this as IUI).GetWPFElement();
        }
    }
}
