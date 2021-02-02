using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>Copies text to the user's clipboard.</para>
    /// <para>Ports:</para>
    /// <para>1. IDataFlow&lt;string&gt; contentInput:</para>
    /// <para>2. IEvent sendOutput:</para>
    /// <para>3. IDataFlow&lt;string&gt; contentOutput:</para>
    /// </summary>
    public class TextClipboard : IDataFlow<string>, IEvent // contentInput, sendOutput
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public bool ClearOnOutput { get; set; } = false;

        // Private fields
        private string _content;
        
        // Ports
        private IDataFlow<string> contentOutput;
        
        // IDataFlow<string> implementation
        string IDataFlow<string>.Data
        {
            get => _content;
            set
            {
                _content = value;

                Clipboard.SetText(_content);
            }
        }

        // IEvent implementation
        void IEvent.Execute()
        {
            if (contentOutput != null)
            {
                contentOutput.Data = Clipboard.GetText();
                if (ClearOnOutput) Clipboard.SetText("");
            }
        }
        
        /// <summary>
        /// <para></para>
        /// </summary>
        public TextClipboard()
        {
            
        }
    }
}
