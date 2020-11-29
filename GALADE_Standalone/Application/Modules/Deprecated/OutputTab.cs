using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using WPF = System.Windows.Controls;
using System.Windows.Media;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;
using Button = DomainAbstractions.Button;
using TextBox = DomainAbstractions.TextBox;

namespace Application
{
    /// <summary>
    /// <para>Contains the abstractions that have their UI represented in the "Output" tab, and has virtual port mappings from several ports from the contained abstractions.
    /// The TextBox in this tab serves as a runtime-readable console log.</para>
    /// <para>Ports:</para>
    /// <para>1. IUI child:</para>
    /// <para>2. IDataFlow&lt;string&gt; newLineInput:</para>
    /// <para>3. IEvent clear:</para>
    /// </summary>
    public class OutputTab : IUI, IDataFlow<string>, IDataFlow<List<string>>, IEvent
    {
        // Public fields and properties
        public string InstanceName = "Default";
        
        // Private fields
        private Tab mainTab = new Tab("Output" ) { InstanceName = "mainTab" };
        private TextBox consoleTextBox = new TextBox()
        {
            Height = 100,
            AcceptsReturn = true,
            TrackIndent = true
        };

        // Ports
        
        // Input instances
        
        // Output instances
        
        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            return (mainTab as IUI)?.GetWPFElement();
        }

        // IDataFlow<List<string>> implementation
        List<string> IDataFlow<List<string>>.Data
        {
            get => default;
            set
            {
                foreach (var line in value)
                {
                    consoleTextBox.Text += $"{line}{Environment.NewLine}";
                }

                consoleTextBox.Text += Environment.NewLine;
            }
        }

        // IDataFlow<string> implementation
        string IDataFlow<string>.Data
        {
            get => consoleTextBox.Text;
            set => consoleTextBox.Text += $"{value}{Environment.NewLine}";
        }

        // IEvent implementation
        void IEvent.Execute()
        {
            (consoleTextBox as IEvent).Execute();
        }
        
        // PostWiringInitialize
        private void PostWiringInitialize()
        {
            // Event subscription

        }
        
        /// <summary>
        /// <para></para>
        /// </summary>
        public OutputTab()
        {
            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR OutputTab.xmind
            DataFlowConnector<string> id_fe42e058579a4bcb81235ad236cf496a = new DataFlowConnector<string>() { InstanceName = "Default" };
            EventConnector id_b25740f646f84856969d05cf2684ac81 = new EventConnector() { InstanceName = "Default" };
            Horizontal id_e801f27bfcda442ead1bcf5cf0051df3 = new Horizontal() { InstanceName = "Default", Margin = new Thickness(10) };
            MenuBar id_1f81f86effe548948bcc08c65fd7d9d9 = new MenuBar() { InstanceName = "Default", Background = Brushes.Transparent };
            MenuItem id_00fba5bcebf34ab69faeed3f361afd39 = new MenuItem("Open in new window" ) { InstanceName = "Default" };
            MenuItem id_c7c3e3d696944eccada04dacfba70207 = new MenuItem("Clear" ) { InstanceName = "Default" };
            PopupWindow id_b00b249750ed4a559599c2885d2fbe3c = new PopupWindow("Console Output" ) { InstanceName = "Default", MinHeight = 500, MinWidth = 1000 };
            TextBox id_3bd4ae00ec0547e69c421bd95a62bbd7 = new TextBox() { InstanceName = "Default", Height = 500, AcceptsReturn = true };
            Vertical id_c0819407c7c04027a51fa8448c75949a = new Vertical() { InstanceName = "Default" };
            // END AUTO-GENERATED INSTANTIATIONS FOR OutputTab.xmind
            
            // BEGIN AUTO-GENERATED WIRING FOR OutputTab.xmind
            mainTab.WireTo(id_c0819407c7c04027a51fa8448c75949a, "tabItemList"); // (@Tab (mainTab).tabItemList) -- [List<IUI>] --> (Vertical (id_c0819407c7c04027a51fa8448c75949a).child)
            id_c0819407c7c04027a51fa8448c75949a.WireTo(id_e801f27bfcda442ead1bcf5cf0051df3, "children"); // (Vertical (id_c0819407c7c04027a51fa8448c75949a).children) -- [List<IUI>] --> (Horizontal (id_e801f27bfcda442ead1bcf5cf0051df3).NEEDNAME)
            id_c0819407c7c04027a51fa8448c75949a.WireTo(id_1f81f86effe548948bcc08c65fd7d9d9, "children"); // (Vertical (id_c0819407c7c04027a51fa8448c75949a).children) -- [List<IUI>] --> (MenuBar (id_1f81f86effe548948bcc08c65fd7d9d9).NEEDNAME)
            id_e801f27bfcda442ead1bcf5cf0051df3.WireTo(consoleTextBox, "children"); // (Horizontal (id_e801f27bfcda442ead1bcf5cf0051df3).children) -- [IUI] --> (@TextBox (consoleTextBox).NEEDNAME)
            consoleTextBox.WireTo(id_fe42e058579a4bcb81235ad236cf496a, "textOutput"); // (@TextBox (consoleTextBox).textOutput) -- [IDataFlow<string>] --> (DataFlowConnector<string> (id_fe42e058579a4bcb81235ad236cf496a).dataInput)
            id_fe42e058579a4bcb81235ad236cf496a.WireTo(id_3bd4ae00ec0547e69c421bd95a62bbd7, "fanoutList"); // (DataFlowConnector<string> (id_fe42e058579a4bcb81235ad236cf496a).fanoutList) -- [IDataFlow<string>] --> (TextBox (id_3bd4ae00ec0547e69c421bd95a62bbd7).textInput)
            id_1f81f86effe548948bcc08c65fd7d9d9.WireTo(id_c7c3e3d696944eccada04dacfba70207, "children"); // (MenuBar (id_1f81f86effe548948bcc08c65fd7d9d9).children) -- [List<IUI>] --> (MenuItem (id_c7c3e3d696944eccada04dacfba70207).child)
            id_1f81f86effe548948bcc08c65fd7d9d9.WireTo(id_00fba5bcebf34ab69faeed3f361afd39, "children"); // (MenuBar (id_1f81f86effe548948bcc08c65fd7d9d9).children) -- [List<IUI>] --> (MenuItem (id_00fba5bcebf34ab69faeed3f361afd39).child)
            id_c7c3e3d696944eccada04dacfba70207.WireTo(consoleTextBox, "clickedEvent"); // (MenuItem (id_c7c3e3d696944eccada04dacfba70207).clickedEvent) -- [IEvent] --> (@TextBox (consoleTextBox).clear)
            id_00fba5bcebf34ab69faeed3f361afd39.WireTo(id_b25740f646f84856969d05cf2684ac81, "clickedEvent"); // (MenuItem (id_00fba5bcebf34ab69faeed3f361afd39).clickedEvent) -- [IEvent] --> (EventConnector (id_b25740f646f84856969d05cf2684ac81).eventInput)
            id_b25740f646f84856969d05cf2684ac81.WireTo(id_b00b249750ed4a559599c2885d2fbe3c, "fanoutList"); // (EventConnector (id_b25740f646f84856969d05cf2684ac81).fanoutList) -- [IEvent] --> (PopupWindow (id_b00b249750ed4a559599c2885d2fbe3c).open)
            id_b00b249750ed4a559599c2885d2fbe3c.WireTo(id_3bd4ae00ec0547e69c421bd95a62bbd7, "children"); // (PopupWindow (id_b00b249750ed4a559599c2885d2fbe3c).children) -- [IUI] --> (TextBox (id_3bd4ae00ec0547e69c421bd95a62bbd7).NEEDNAME)
            // END AUTO-GENERATED WIRING FOR OutputTab.xmind
            
            // BEGIN MANUAL INSTANTIATIONS FOR OutputTab.xmind
            // END MANUAL INSTANTIATIONS FOR OutputTab.xmind
            
            // BEGIN MANUAL WIRING FOR OutputTab.xmind
            // END MANUAL WIRING FOR OutputTab.xmind
        }
    }
}
