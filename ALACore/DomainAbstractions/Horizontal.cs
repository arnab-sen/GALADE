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
    /// 1. IUI child: input IUI to get the WPF element
    /// 2. IDataFlow&lt;bool&gt; visible: input boolean value that the visibility (visible or collapsed)
    /// 3. List&lt;IUI&gt; childrenTabs: output list of IU elements contained in this Horizontal
    /// </summary>
    public class Horizontal : IUI, IDataFlow<bool>, IEvent // child, visible, clear
    {
        // properties ---------------------------------------------------------------------
        public string InstanceName { get; set; } = "Default";
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
        private List<IEventHandler> eventHandlers = new List<IEventHandler>();

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
            gridPanel.Children.Clear();

            if (Background != null) gridPanel.Background = Background;

            for (var i = 0; i < children.Count; i++)
            {
                var ratio = (Ratios != null && i < Ratios.Length) ? Ratios[i] : 100;

                var uiElement = children[i].GetWPFElement();
                gridPanel.Children.Add(uiElement);

                gridPanel.ColumnDefinitions.Add(new ColumnDefinition() {
                    Width = uiElement.Visibility == Visibility.Collapsed ? new GridLength(0, GridUnitType.Auto) : new GridLength(ratio, GridUnitType.Star)
                });

                System.Windows.Controls.Grid.SetColumn(uiElement, i);
                System.Windows.Controls.Grid.SetRow(uiElement, 0);

                // Hide cell when content is collapsed
                uiElement.IsVisibleChanged += (sender, args) =>
                {
                    if (uiElement.Visibility == Visibility.Collapsed)
                    {
                        var index = gridPanel.Children.IndexOf(uiElement);
                        if (index != -1 && gridPanel.ColumnDefinitions.Count > index) 
                            gridPanel.ColumnDefinitions[index].Width = new GridLength(0, GridUnitType.Auto);
                    }
                    else if (uiElement.Visibility == Visibility.Visible)
                    {
                        var index = gridPanel.Children.IndexOf(uiElement);
                        var width = (Ratios != null && index < Ratios.Length) ? Ratios[index] : 100;
                        if (index != -1 && gridPanel.ColumnDefinitions.Count > index) 
                            gridPanel.ColumnDefinitions[index].Width = new GridLength(width, GridUnitType.Star);
                    }
                };

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
