﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions 
{
    /// <summary>
    /// An IUI decorator that applies a configuration onto a child IUI, and then propagates that child up to the parent IUI of this class.
    /// </summary>
    public class UIConfig : IUI
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string InstanceDescription { get; set; } = "";
        public string ToolTipText { get; set; } = "";
        public Func<string> UpdateToolTip { get; set; }
        public double Width { get; set; } = double.NaN;
        public double Height { get; set; } = double.NaN;
        public double MinWidth { get; set; } = double.NaN;
        public double MinHeight { get; set; } = double.NaN;
        public double MaxWidth { get; set; } = double.NaN;
        public double MaxHeight { get; set; } = double.NaN;
        public bool Visible { get; set; } = true;

        /// <summary>
        /// Choose from left, right, or middle.
        /// </summary>
        public string HorizAlignment { get; set; } = "";

        /// <summary>
        /// Choose from top, bottom, or middle.
        /// </summary>
        public string VertAlignment { get; set; } = "";

        /// <summary>
        /// Represents a margin that should be applied uniformly to all sides.
        /// </summary>
        public double UniformMargin { get; set; } = double.NaN;
        public double LeftMargin { get; set; } = double.NaN;
        public double TopMargin { get; set; } = double.NaN;
        public double RightMargin { get; set; } = double.NaN;
        public double BottomMargin { get; set; } = double.NaN;

        // Private fields
        private UIElement _uiElement = new UIElement();

        // Ports
        private IUI child;
        private List<IUI> contextMenuChildren = new List<IUI>();
        private List<IEventHandler> eventHandlers = new List<IEventHandler>();

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            if (child != null)
            {
                _uiElement = child.GetWPFElement();
                Configure(_uiElement);
            }

            return _uiElement;
        }

        // Methods
        public void Focus()
        {
            _uiElement.Focus();
        }

        private void Configure(UIElement ui)
        {
            if (ui is FrameworkElement fe)
            {
                if (contextMenuChildren.Any())
                {
                    fe.ContextMenu = new System.Windows.Controls.ContextMenu();
                    foreach (var contextMenuItem in contextMenuChildren)
                    {
                        fe.ContextMenu.Items.Add(contextMenuItem.GetWPFElement());
                    } 
                }

                if (!string.IsNullOrEmpty(ToolTipText))
                {
                    // Use a TextBlock so that the tooltip text can be dynamically updated
                    var toolTipLabel = new TextBlock()
                    {
                        Text = ToolTipText
                    };

                    var toolTip = new System.Windows.Controls.ToolTip()
                    {
                        Content = toolTipLabel
                    };

                    fe.ToolTip = toolTip;

                    if (UpdateToolTip != null)
                    {
                        toolTip.ToolTipOpening += (sender, args) => toolTipLabel.Text = UpdateToolTip();
                    } 
                }

                var horizAlignment = HorizAlignment.ToLower();
                if (horizAlignment == "left")
                {
                    fe.HorizontalAlignment = HorizontalAlignment.Left;
                }
                else if (horizAlignment == "right")
                {
                    fe.HorizontalAlignment = HorizontalAlignment.Right;
                }
                else if (horizAlignment == "middle")
                {
                    fe.HorizontalAlignment = HorizontalAlignment.Center;
                }
                else
                {
                    fe.HorizontalAlignment = HorizontalAlignment.Stretch;
                }

                var vertAlignment = VertAlignment.ToLower();
                if (vertAlignment == "top")
                {
                    fe.VerticalAlignment = VerticalAlignment.Top;
                }
                else if (vertAlignment == "bottom")
                {
                    fe.VerticalAlignment = VerticalAlignment.Bottom;
                }
                else if (vertAlignment == "middle")
                {
                    fe.VerticalAlignment = VerticalAlignment.Center;
                }
                else
                {
                    fe.VerticalAlignment = VerticalAlignment.Stretch;
                }

                if (!double.IsNaN(Height)) fe.Height = Height;
                if (!double.IsNaN(Width)) fe.Width = Width;
                if (!double.IsNaN(MinHeight)) fe.MinHeight = MinHeight;
                if (!double.IsNaN(MinWidth)) fe.MinWidth = MinWidth;
                if (!double.IsNaN(MaxHeight)) fe.MaxHeight = MaxHeight;
                if (!double.IsNaN(MaxWidth)) fe.MaxWidth = MaxWidth;

                if (double.IsNaN(UniformMargin))
                {
                    var margin = new double[] { 0, 0, 0, 0 };
                    if (!double.IsNaN(LeftMargin)) margin[0] = LeftMargin;
                    if (!double.IsNaN(TopMargin)) margin[1] = TopMargin;
                    if (!double.IsNaN(RightMargin)) margin[2] = RightMargin;
                    if (!double.IsNaN(BottomMargin)) margin[3] = BottomMargin;
                    if (margin.Any(d => d > 0 || d < 0)) fe.Margin = new Thickness(margin[0], margin[1], margin[2], margin[3]); 
                }
                else
                {
                    fe.Margin = new Thickness(UniformMargin);
                }

                fe.Visibility = Visible ? Visibility.Visible : Visibility.Collapsed;
            }

            foreach (var eventHandler in eventHandlers)
            {
                eventHandler.Sender = ui;
            }
        }

        public UIConfig()
        {

        }
    }
}
