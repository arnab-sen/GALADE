using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ProgrammingParadigms;
using System.Windows;
using System.Windows.Forms;
using WPF = System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>Contains a WPF TextBox and both implements and provides ports for setting/getting the text inside.</para>
    /// <para>Ports:</para>
    /// <para>1. IUI wpfElement: returns the contained TextBox</para>
    /// <para>2. IDataFlow&lt;string&gt; content: The string contained in the TextBox</para>
    /// <para>3. IDataFlowB&lt;string&gt; returnContent: returns the string contained in the TextBox</para>
    /// <para>4. IEvent clear: clears the text content inside the TextBox</para>
    /// <para>5. IDataFlow&lt;string&gt; textOutput: outputs the string contained in the TextBox</para>
    /// </summary>
    public class TextBox : IUI, IDataFlow<string>, IEvent // child, textInput, clear
    {
        // properties
        public string InstanceName { get; set; } = "Default";
        public string Text
        {
            get => _textBox.Text;
            set
            {
                _textBox.Dispatcher.Invoke(() => _textBox.Text = value);
            }
        }

        public Thickness Margin
        {
            get => _textBox.Margin;
            set => _textBox.Margin = value;
        }

        public bool AcceptsReturn
        {
            get => _textBox.AcceptsReturn;
            set => _textBox.AcceptsReturn = value;
        }

        public bool TrackIndent { get; set; } = false;
        public string TabString { get; set; } = "\t";

        public bool AcceptsTab
        {
            get => _textBox.AcceptsTab;
            set => _textBox.AcceptsTab = value;
        }

        public double Height
        {
            get => _textBox.Height;
            set => _textBox.MinHeight = value;
        }

        public double Width
        {
            get => _textBox.Width;
            set => _textBox.MinWidth = value;
        }

        public string Font
        {
            get => _textBox.FontFamily.ToString();
            set => _textBox.FontFamily = new FontFamily(value);
        }

        // Fields
        private WPF.TextBox _textBox = new WPF.TextBox();

        // Outputs
        private IDataFlow<string> textOutput;

        /// <summary>
        /// Sends an event when the enter key is released.
        /// </summary>
        private IEvent eventEnterPressed;

        /// <summary>
        /// <para>Contains a WPF TextBox and both implements and provides ports for setting/getting the text inside.</para>
        /// </summary>
        public TextBox(bool readOnly = false)
        {
            _textBox.TextWrapping = TextWrapping.Wrap;

            _textBox.AcceptsTab = true;
            // _textBox.AcceptsReturn = true;

            _textBox.TextChanged += (sender, args) =>
            {
                if (textOutput != null) textOutput.Data = Text;
                if (_textBox.HorizontalScrollBarVisibility == WPF.ScrollBarVisibility.Visible)
                {
                    _textBox.ScrollToEnd(); 
                }

            };

            // Track indentation
            _textBox.PreviewKeyDown += (sender, args) =>
            {
                if (TrackIndent && args.Key == Key.Return && (Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                {
                    var text = Text;
                    var preText = text.Substring(0, _textBox.CaretIndex);
                    var postText = text.Length > _textBox.CaretIndex ? text.Substring(_textBox.CaretIndex) : "";

                    var latestLine = preText.Split(new[] {Environment.NewLine}, StringSplitOptions.None).Last();
                    var startingWhiteSpace = Regex.Match(latestLine, @"^([\s]+)").Value;

                    // Find the number of consecutive instances of TabString from the start of startingWhiteSpace
                    int indentLevel = 0;
                    string remaining = startingWhiteSpace;

                    while (remaining != "")
                    {
                        if (remaining.StartsWith(TabString))
                        {
                            indentLevel++;
                            remaining = remaining.Remove(0, TabString.Length);
                        }
                        else
                        {
                            break;
                        }
                    }

                    latestLine = Environment.NewLine + string.Concat(Enumerable.Repeat(TabString, indentLevel));

                    Text = preText + latestLine + postText;
                    _textBox.Dispatcher.Invoke(() => _textBox.CaretIndex = preText.Length + latestLine.Length);
                }
                else if (_textBox.AcceptsTab && args.Key == Key.Tab)
                {
                    var text = Text;
                    var preText = text.Substring(0, _textBox.CaretIndex);
                    var postText = text.Length > _textBox.CaretIndex ? text.Substring(_textBox.CaretIndex) : "";

                    Text = preText + TabString + postText;
                    _textBox.Dispatcher.Invoke(() => _textBox.CaretIndex = preText.Length + TabString.Length);

                }
                else if (args.Key == Key.V && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
                {
                    // Temporary enable AcceptsReturn to handle multiline text
                    var acceptsReturn = _textBox.AcceptsReturn;
                    _textBox.AcceptsReturn = true;
                    _textBox.Paste();
                    _textBox.AcceptsReturn = acceptsReturn;
                }
                else
                {
                    return;
                }

                args.Handled = true;

            };

            _textBox.HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto;
            _textBox.VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto;
            _textBox.IsReadOnly = readOnly;

            _textBox.KeyUp += (sender, args) =>
            {
                if (args.Key == Key.Enter) eventEnterPressed?.Execute();
            };

        }

        // Methods


        // IUI implementation
        System.Windows.UIElement IUI.GetWPFElement()
        {
            return _textBox;
        }

        // IDataFlow<string> implementation
        string IDataFlow<string>.Data
        {
            get => Text;
            set
            {
                Text = value;
            }
        }

        // IEvent implementation
        void IEvent.Execute()
        {
            _textBox.Clear();
        }

    }
}
