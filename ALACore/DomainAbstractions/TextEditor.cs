using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using ScintillaNET.WPF;
using Libraries;
using ProgrammingParadigms;
using ScintillaNET;
using Style = ScintillaNET.Style;

namespace DomainAbstractions
{
    public class TextEditor : IUI
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        public double Width
        {
            get => _editor.Width;
            set => _editor.MinWidth = value;
        }

        public double Height
        {
            get => _editor.Height;
            set => _editor.MinHeight = value;
        }

        public string Text
        {
            get => _editor.Text;
            set => _editor.Text = value;
        }

        // Private fields
        // private ScintillaNET.Scintilla _editor = new Scintilla();
        private ScintillaWPF _editor = new ScintillaWPF();

        // Ports

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            _editor.Lexer = Lexer.Cpp;
            SetUpEditorStyle();

            _editor.HScrollBar = false;
            _editor.VScrollBar = false;
            
            _editor.MinHeight = 50;
            _editor.MinWidth = 100;

            return _editor;
        }

        // Methods
        private void SetUpEditorStyle()
        {
            // Credit to Jacob Slusser at https://github.com/jacobslusser/ScintillaNET/wiki/Automatic-Syntax-Highlighting for the following syntax recipe
            // Configuring the default style with properties
            _editor.StyleResetDefault();
            _editor.Styles[Style.Default].Font = "Consolas";
            _editor.Styles[Style.Default].Size = 10;
            _editor.StyleClearAll();

            // Configure the CPP (C#) lexer styles
            _editor.Styles[Style.Cpp.Default].ForeColor = System.Drawing.Color.Silver;
            _editor.Styles[Style.Cpp.Comment].ForeColor = System.Drawing.Color.FromArgb(0, 128, 0); // Green
            _editor.Styles[Style.Cpp.CommentLine].ForeColor = System.Drawing.Color.FromArgb(0, 128, 0); // Green
            _editor.Styles[Style.Cpp.CommentLineDoc].ForeColor = System.Drawing.Color.FromArgb(128, 128, 128); // Gray
            _editor.Styles[Style.Cpp.Number].ForeColor = System.Drawing.Color.Olive;
            _editor.Styles[Style.Cpp.Word].ForeColor = System.Drawing.Color.Blue;
            _editor.Styles[Style.Cpp.Word2].ForeColor = System.Drawing.Color.Blue;
            _editor.Styles[Style.Cpp.String].ForeColor = System.Drawing.Color.FromArgb(163, 21, 21); // Red
            _editor.Styles[Style.Cpp.Character].ForeColor = System.Drawing.Color.FromArgb(163, 21, 21); // Red
            _editor.Styles[Style.Cpp.Verbatim].ForeColor = System.Drawing.Color.FromArgb(163, 21, 21); // Red
            _editor.Styles[Style.Cpp.StringEol].BackColor = System.Drawing.Color.Pink;
            _editor.Styles[Style.Cpp.Operator].ForeColor = System.Drawing.Color.Purple;
            _editor.Styles[Style.Cpp.Preprocessor].ForeColor = System.Drawing.Color.Maroon;
            _editor.Lexer = Lexer.Cpp;

            // Set the keywords
            _editor.SetKeywords(0, "abstract as base break case catch checked continue default delegate do else event explicit extern false finally fixed for foreach goto if implicit in interface internal is lock namespace new null object operator out override params private protected public readonly ref return sealed sizeof stackalloc switch this throw true try typeof unchecked unsafe using virtual while");
            _editor.SetKeywords(1, "bool byte char class const decimal double enum float int long sbyte short static string struct uint ulong ushort void");
        }

        public TextEditor()
        {

        }
    }
}
