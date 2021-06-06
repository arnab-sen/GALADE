using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>A grid display with individually editable cells.</para>
    /// <para>Ports:</para>
    /// <para></para>
    /// </summary>
    public class EditableGrid : IUI
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public int NumCols { get; set; } = 2;
        public int NumRows { get; set; } = 1;

        // Private fields
        private Grid _innerGrid;

        // Ports
        private IEventB newRow;
        private IEventB clear;
        private IEventB sendOutput;
        private IDataFlow<List<List<string>>> cellData;

        // Methods
        private void PostWiringInitialize()
        {
            newRow.EventHappened += CreateNewRow;
            clear.EventHappened += Clear;

        }

        UIElement IUI.GetWPFElement()
        {
            var _innerGrid = new Grid()
            {
                Background = Brushes.White
            };

            ScrollViewer.SetCanContentScroll(_innerGrid, true);

            _innerGrid.RowDefinitions.Clear();

            for (int i = 0; i < NumRows; i++)
            {
                CreateNewRow();
            }

            return _innerGrid;
        }

        private void CreateNewRow()
        {
            var tempRow = new List<UIElement>();

            for (int j = 0; j < NumCols; j++)
            {
                tempRow.Add(CreateCell($"", new Thickness(1)));
            }

            _innerGrid.AddRow(tempRow.ToArray());
        }

        private void Clear()
        {
            _innerGrid.RowDefinitions.Clear();
        }

        private Border CreateCell(string textContent, Thickness borderThickness)
        {
            var border = new Border()
            {
                BorderBrush = Brushes.Black,
                BorderThickness = borderThickness
            };

            var textbox = new System.Windows.Controls.TextBox()
            {
                Text = textContent,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(2),
                BorderBrush = Brushes.Transparent,
                TextAlignment = TextAlignment.Center
            };

            border.Child = textbox;

            return border;
        }

        private List<List<string>> CreateGridPackage(Grid grid)
        {
            var package = new List<List<string>>();

            for (int i = 0; i < grid.RowDefinitions.Count; i++)
            {
                var tempRow = new List<string>();

                for (int j = 0; j < grid.ColumnDefinitions.Count; j++)
                {
                    tempRow.Add();
                }

                package.Add(tempRow);
            }

            return package;
        }


        public EditableGrid()
        {

        }
    }
}