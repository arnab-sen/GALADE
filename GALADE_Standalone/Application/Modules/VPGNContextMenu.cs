using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;

namespace Application
{
    /// <summary>
    /// <para>Creates a new instance of a context menu for VisualPortGraphNodes, and adds the MenuItems in its layout. The clickedEvent IEvent output from each MenuItem is mapped to virtual IEvent ports.</para>
    /// <para>The current commands are "Save template", "Enable port editing", and "Disable port editing".</para>
    /// <para>Ports:</para>
    /// <para>1. IUI child:</para>
    /// <para>2. IEvent saveTemplate:</para>
    /// </summary>
    public class VPGNContextMenu : IUI
    {
        // Public fields and properties
        public string InstanceName = "Default";
        
        // Private fields
        
        // Ports
        private IEvent saveTemplate;
        private IEvent enablePortEditing;
        private IEvent disablePortEditing;
        
        // Input instances
        
        // Output instances
        private EventConnector saveTemplateConnector = new EventConnector() { InstanceName = "saveTemplateConnector" };
        private EventConnector enablePortEditingConnector = new EventConnector() { InstanceName = "enablePortEditingConnector" };
        private EventConnector disablePortEditingConnector = new EventConnector() { InstanceName = "disablePortEditingConnector" };
        
        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            return CreateContextMenu();
        }
        
        // PostWiringInitialize
        private void PostWiringInitialize()
        {
            // Mapping to virtual ports
            Utilities.ConnectToVirtualPort(saveTemplateConnector, "complete", saveTemplate);
            Utilities.ConnectToVirtualPort(enablePortEditingConnector, "complete", enablePortEditing);
            Utilities.ConnectToVirtualPort(disablePortEditingConnector, "complete", disablePortEditing);
            
            // IDataFlowB and IEventB event handlers
            // Send out initial values
            // (instanceNeedingInitialValue as IDataFlow<T>).Data = defaultValue;
        }

        private UIElement CreateContextMenu()
        {
            ContextMenu contextMenu = new ContextMenu();

            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR VPGNContextMenu.xmind
            MenuItem id_3ee80451de3d4ca5ac07adac17db4e3b = new MenuItem("Disable port editing" ) { InstanceName = "Default" };
            MenuItem id_7202339f2d244ec69b5524188def7e3d = new MenuItem("Enable port editing" ) { InstanceName = "Default" };
            MenuItem id_b4fc46e2e1064102923a644953619592 = new MenuItem("Save template" ) { InstanceName = "Default" };
            // END AUTO-GENERATED INSTANTIATIONS FOR VPGNContextMenu.xmind
            
            // BEGIN AUTO-GENERATED WIRING FOR VPGNContextMenu.xmind
            contextMenu.WireTo(id_b4fc46e2e1064102923a644953619592, "children"); // (@ContextMenu (contextMenu).children) -- [IUI] --> (MenuItem (id_b4fc46e2e1064102923a644953619592).child)
            contextMenu.WireTo(id_7202339f2d244ec69b5524188def7e3d, "children"); // (@ContextMenu (contextMenu).children) -- [IUI] --> (MenuItem (id_7202339f2d244ec69b5524188def7e3d).child)
            contextMenu.WireTo(id_3ee80451de3d4ca5ac07adac17db4e3b, "children"); // (@ContextMenu (contextMenu).children) -- [IUI] --> (MenuItem (id_3ee80451de3d4ca5ac07adac17db4e3b).child)
            id_b4fc46e2e1064102923a644953619592.WireTo(saveTemplateConnector, "clickedEvent"); // (MenuItem (id_b4fc46e2e1064102923a644953619592).clickedEvent) -- [IEvent] --> (@EventConnector (saveTemplateConnector).eventInput)
            id_7202339f2d244ec69b5524188def7e3d.WireTo(enablePortEditingConnector, "clickedEvent"); // (MenuItem (id_7202339f2d244ec69b5524188def7e3d).clickedEvent) -- [IEvent] --> (@EventConnector (enablePortEditingConnector).eventInput)
            id_3ee80451de3d4ca5ac07adac17db4e3b.WireTo(disablePortEditingConnector, "clickedEvent"); // (MenuItem (id_3ee80451de3d4ca5ac07adac17db4e3b).clickedEvent) -- [IEvent] --> (@EventConnector (disablePortEditingConnector).eventInput)
            // END AUTO-GENERATED WIRING FOR VPGNContextMenu.xmind
            
            // BEGIN MANUAL INSTANTIATIONS FOR VPGNContextMenu.xmind
            // END MANUAL INSTANTIATIONS FOR VPGNContextMenu.xmind
            
            // BEGIN MANUAL WIRING FOR VPGNContextMenu.xmind
            // END MANUAL WIRING FOR VPGNContextMenu.xmind

            return (contextMenu as IUI).GetWPFElement();
        }

        /// <summary>
        /// <para>Creates a new instance of a context menu for VisualPortGraphNodes.</para>
        /// </summary>
        public VPGNContextMenu()
        {
            
        }
    }
}
