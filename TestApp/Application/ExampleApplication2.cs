using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Libraries;
using ProgrammingParadigms;

namespace Application
{
    /// <summary>
    /// <para>This is a dummy abstraction to use as an example, either to learn from or to use in testing.</para>
    /// <para>Ports:</para>
    /// <para>1. IEvent start: Start</para>
    /// </summary>
    public class ExampleDomainAbstraction : UIElement, IEvent, IDataFlow<string>
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public List<string> TestProperty { get; set; }

        // Private fields
        private string _ignoreStringField = "";
		private Data<int> _testData = new Data<int>()
        {
            storedData = 10
        };


        // Methods

		public void DoSomething()
		{
			
		}

        public ExampleDomainAbstraction(string arg0, string arg2 = "test")
        {		
			// BEGIN AUTO-GENERATED INSTANTIATIONS FOR test
            MenuItem XR5000DeleteMenu = new MenuItem("Delete information off device",false) {InstanceName="XR5000DeleteMenu"}; /* {"IsRoot":false} */
            MenuItem XR5000ExportMenu = new MenuItem("Put information onto device",false) {InstanceName="XR5000ExportMenu"}; /* {"IsRoot":false,"Description":"test description\r\n"} */
            MenuItem XR5000ImportMenu = new MenuItem("Get information off device",false) {InstanceName="XR5000ImportMenu"}; /* {"IsRoot":false} */
            Horizontal XR5000Tools = new Horizontal() {InstanceName="XR5000Tools"}; /* {"IsRoot":false} */
            // END AUTO-GENERATED INSTANTIATIONS FOR test

			// BEGIN AUTO-GENERATED WIRING FOR test
            XR5000Connected.WireTo(XR5000ExportMenu, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":true,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":""} */
            XR5000Connected.WireTo(stringXR5000Connected, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":true,"DestinationType":"UNDEFINED","DestinationIsReference":false,"Description":""} */
            stringXR5000Connected.WireTo(textDeviceConnected, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"Text","DestinationIsReference":true,"Description":""} */
            XR5000Connected.WireTo(XR5000ImportMenu, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":true,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":""} */
            XR5000Connected.WireTo(XR5000DeleteMenu, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":true,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":""} */
            XR5000Connected.WireTo(XR5000ImportTool, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":true,"DestinationType":"UNDEFINED","DestinationIsReference":false,"Description":""} */
            XR5000Connected.WireTo(XR5000ExportTool, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":true,"DestinationType":"UNDEFINED","DestinationIsReference":false,"Description":""} */
            XR5000Connected.WireTo(XR5000DeleteTool, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":true,"DestinationType":"UNDEFINED","DestinationIsReference":false,"Description":""} */
            fileMenu.WireTo(XR5000ImportMenu, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":true,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":""} */
            fileMenu.WireTo(XR5000ExportMenu, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":true,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":""} */
            fileMenu.WireTo(XR5000DeleteMenu, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":true,"DestinationType":"MenuItem","DestinationIsReference":false,"Description":""} */
            XR5000getInfoWizard.WireTo(id_39cc8679a22b46b4aebeef125cd5f0ae, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":false,"Description":""} */
            XR5000getInfoWizard.WireTo(id_5ffa0209425b4dcea9e1a3f02ff216ea, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":false,"Description":""} */
            XR5000getInfoWizard.WireTo(id_8ea685db14ec42f4bd8bb4a297d16289, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":false,"Description":""} */
            XR5000getInfoWizard.WireTo(id_f62a188e32b04fd494e53c0486fd417d, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":false,"Description":""} */
            XR5000getInfoWizard.WireTo(id_673d82ee3c3b49e39694cd342c73d4ad, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":false,"Description":""} */
            id_39cc8679a22b46b4aebeef125cd5f0ae.WireTo(XR5000getSessionWizard, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":false,"Description":""} */
            XR5000getSessionWizard.WireTo(id_210bf17d3a114bc5a7a19a6ab678dc48, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":false,"Description":""} */
            XR5000getSessionWizard.WireTo(id_ae94e2df21a4408e8b0bc9a55d2d481e, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":false,"Description":""} */
            XR5000getSessionWizard.WireTo(id_09bf8feb99e14e8e82def502ce66a0bf, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":false,"Description":""} */
            XR5000getSessionWizard.WireTo(id_db985bc62bfd412d8db7559b1ffa2ad0, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":false,"Description":""} */
            XR5000ImportTool.WireTo(XR5000getInfoWizard, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":false,"Description":""} */
            XR5000getSessionWizard.WireTo(XR5000getInfoWizard, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":false,"Description":""} */
            XR5000ImportMenu.WireTo(XR5000getInfoWizard, "children"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":false,"Description":""} */
            id_210bf17d3a114bc5a7a19a6ab678dc48.WireTo(saveUsbSessionsDataToFileBrowser, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":true,"Description":""} */
            id_ae94e2df21a4408e8b0bc9a55d2d481e.WireTo(naitLoginWindow, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"PopupWindow","DestinationIsReference":true,"Description":""} */
            id_09bf8feb99e14e8e82def502ce66a0bf.WireTo(nlisLoginWindow, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"PopupWindow","DestinationIsReference":true,"Description":""} */
            id_db985bc62bfd412d8db7559b1ffa2ad0.WireTo(mihubEventConnector, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":true,"Description":""} */
            id_5ffa0209425b4dcea9e1a3f02ff216ea.WireTo(saveUsbLifeDataToFileBrowser, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":true,"Description":""} */
            id_8ea685db14ec42f4bd8bb4a297d16289.WireTo(getInformationOffDeviceBrower, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"FolderBrowser","DestinationIsReference":true,"Description":""} */
            XR5000ExportMenu.WireTo(XR5000putInfoWizard, "children"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":false,"Description":""} */
            XR5000putInfoWizard.WireTo(id_748496bee52a4661b3699f367afb7904, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":false,"Description":""} */
            XR5000putInfoWizard.WireTo(id_48aaec02f3c245329aa25660a915faf7, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":false,"Description":""} */
            XR5000putInfoWizard.WireTo(id_a2cf0f9732584f008b372e537dd1cefb, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":false,"Description":""} */
            XR5000putInfoWizard.WireTo(id_835f7e3a147544e2997e5e7edeb864a7, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":false,"Description":""} */
            id_748496bee52a4661b3699f367afb7904.WireTo(uploadSessionFromPCToXr5000, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":true,"Description":""} */
            id_a2cf0f9732584f008b372e537dd1cefb.WireTo(uploadFavSettingsBrowser, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":true,"Description":""} */
            ToolBar.WireTo(XR5000Tools, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":true,"DestinationType":"Horizontal","DestinationIsReference":false,"Description":""} */
            XR5000Tools.WireTo(XR5000ImportTool, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":false,"Description":""} */
            XR5000Tools.WireTo(XR5000ExportTool, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":false,"Description":""} */
            XR5000Tools.WireTo(XR5000DeleteTool, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":false,"Description":""} */
            XR5000ExportTool.WireTo(XR5000putInfoWizard, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":false,"Description":""} */
            XR5000DeleteTool.WireTo(Scale5000DeleteSessions, "output"); /* {"SourceType":"UNDEFINED","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":true,"Description":""} */
            XR5000DeleteMenu.WireTo(Scale5000DeleteSessions, "children"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"UNDEFINED","DestinationIsReference":true,"Description":""} */
            // END AUTO-GENERATED WIRING FOR test
        }
    }
}