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
            LiteralString stringXR5000Connected = new LiteralString("Connected to XR5000") {InstanceName="stringXR5000Connected"}; /* {"IsRoot":false} */
            MenuItem XR5000DeleteMenu = new MenuItem("Delete information off device",false) {IconName="5000Delete.png",InstanceName="XR5000DeleteMenu"}; /* {"IsRoot":false} */
            MenuItem XR5000ExportMenu = new MenuItem("Put information onto device",false) {IconName="5000Export.png",InstanceName="XR5000ExportMenu"}; /* {"IsRoot":false} */
            MenuItem XR5000ImportMenu = new MenuItem("Get information off device",false) {IconName="5000Import.png",InstanceName="XR5000ImportMenu"}; /* {"IsRoot":false} */
            Wizard XR5000getInfoWizard = new Wizard("Get information off device") {SecondTitle="What information do you want to get off the device?",InstanceName="XR5000getInfoWizard"}; /* {"IsRoot":false} */
            WizardItem id_39cc8679a22b46b4aebeef125cd5f0ae = new WizardItem("Get selected session files") {ImageName="Icon_Session.png",Checked=true,InstanceName="Default"}; /* {"IsRoot":false} */
            WizardItem id_5ffa0209425b4dcea9e1a3f02ff216ea = new WizardItem("Save all animal lifetime information as a file on the PC") {ImageName="Icon_Animal.png",InstanceName="Default"}; /* {"IsRoot":false} */
            WizardItem id_8ea685db14ec42f4bd8bb4a297d16289 = new WizardItem("Save favourite setups as files on the PC") {ImageName="Favourite_48x48px.png",InstanceName="Default"}; /* {"IsRoot":false} */
            WizardItem id_f62a188e32b04fd494e53c0486fd417d = new WizardItem("Back up the device database onto the PC") {ImageName="Icon_Database.png",InstanceName="Default"}; /* {"IsRoot":false} */
            WizardItem id_673d82ee3c3b49e39694cd342c73d4ad = new WizardItem("Generate a report") {ImageName="reporticon.png",InstanceName="Default"}; /* {"IsRoot":false} */
            Wizard XR5000getSessionWizard = new Wizard("Get information off device") {SecondTitle="What do you want to do with the session files?",ShowBackButton=true,InstanceName="XR5000getSessionWizard"}; /* {"IsRoot":false} */
            WizardItem id_210bf17d3a114bc5a7a19a6ab678dc48 = new WizardItem("Save selected session files as files on the PC") {ImageName="Icon_Session.png",Checked=true,InstanceName="Default"}; /* {"IsRoot":false} */
            WizardItem id_ae94e2df21a4408e8b0bc9a55d2d481e = new WizardItem("Send records to NAIT") {ImageName="NAIT.png",InstanceName="Default"}; /* {"IsRoot":false} */
            WizardItem id_09bf8feb99e14e8e82def502ce66a0bf = new WizardItem("Send records to NLIS") {ImageName="NLIS_logo.jpg",InstanceName="Default"}; /* {"IsRoot":false} */
            WizardItem id_db985bc62bfd412d8db7559b1ffa2ad0 = new WizardItem("Send sessions to MiHub Livestock") {ImageName="MiHub40x40_ltblue_cloud.png",InstanceName="Default"}; /* {"IsRoot":false} */
            Wizard XR5000putInfoWizard = new Wizard("Put information onto device") {SecondTitle="What information do you want to put onto the device?",InstanceName="XR5000putInfoWizard"}; /* {"IsRoot":false} */
            WizardItem id_748496bee52a4661b3699f367afb7904 = new WizardItem("Session files") {ImageName="Icon_Session.png",Checked=true,InstanceName=""}; /* {"IsRoot":false} */
            WizardItem id_48aaec02f3c245329aa25660a915faf7 = new WizardItem() {ContentText="Animal lifetime information",ImageName="Icon_Animal.png",InstanceName="Default"}; /* {"IsRoot":false} */
            WizardItem id_a2cf0f9732584f008b372e537dd1cefb = new WizardItem("Favourite setups from my PC") {ImageName="Favourite_48x48px.png",InstanceName="Default"}; /* {"IsRoot":false} */
            WizardItem id_835f7e3a147544e2997e5e7edeb864a7 = new WizardItem("Favourite setups from the Tru-Test website") {ImageName="Icon_Favourite_Web.png",InstanceName="Default"}; /* {"IsRoot":false} */
            Horizontal XR5000Tools = new Horizontal() {InstanceName="XR5000Tools"}; /* {"IsRoot":false} */
            Tool XR5000DeleteTool = new Tool("5000Delete.png",false) {InstanceName="XR5000DeleteTool",ToolTip="Delete information off device"}; /* {"IsRoot":false} */
            Tool XR5000ExportTool = new Tool("5000Export.png",false) {InstanceName="XR5000ExportTool",ToolTip="Put information onto device"}; /* {"IsRoot":false} */
            Tool XR5000ImportTool = new Tool("5000Import.png",false) {InstanceName="XR5000ImportTool",ToolTip="Get information off device"}; /* {"IsRoot":false} */
            // END AUTO-GENERATED INSTANTIATIONS FOR test

			// BEGIN AUTO-GENERATED WIRING FOR test
            XR5000Connected.WireTo(stringXR5000Connected, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":true,"DestinationType":"LiteralString","DestinationIsReference":false} */
            stringXR5000Connected.WireTo(textDeviceConnected, "dataFlowOutput"); /* {"SourceType":"LiteralString","SourceIsReference":false,"DestinationType":"Text","DestinationIsReference":true} */
            XR5000Connected.WireTo(XR5000ImportMenu, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":true,"DestinationType":"MenuItem","DestinationIsReference":false} */
            XR5000Connected.WireTo(XR5000ExportMenu, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":true,"DestinationType":"MenuItem","DestinationIsReference":false} */
            XR5000Connected.WireTo(XR5000DeleteMenu, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":true,"DestinationType":"MenuItem","DestinationIsReference":false} */
            XR5000Connected.WireTo(XR5000ImportTool, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":true,"DestinationType":"Tool","DestinationIsReference":false} */
            XR5000Connected.WireTo(XR5000ExportTool, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":true,"DestinationType":"Tool","DestinationIsReference":false} */
            XR5000Connected.WireTo(XR5000DeleteTool, "fanoutList"); /* {"SourceType":"DataFlowConnector","SourceIsReference":true,"DestinationType":"Tool","DestinationIsReference":false} */
            fileMenu.WireTo(XR5000ImportMenu, "children"); /* {"SourceType":"Menu","SourceIsReference":true,"DestinationType":"MenuItem","DestinationIsReference":false} */
            fileMenu.WireTo(XR5000ExportMenu, "children"); /* {"SourceType":"Menu","SourceIsReference":true,"DestinationType":"MenuItem","DestinationIsReference":false} */
            fileMenu.WireTo(XR5000DeleteMenu, "children"); /* {"SourceType":"Menu","SourceIsReference":true,"DestinationType":"MenuItem","DestinationIsReference":false} */
            XR5000getInfoWizard.WireTo(id_39cc8679a22b46b4aebeef125cd5f0ae, "children"); /* {"SourceType":"Wizard","SourceIsReference":false,"DestinationType":"WizardItem","DestinationIsReference":false} */
            XR5000getInfoWizard.WireTo(id_5ffa0209425b4dcea9e1a3f02ff216ea, "children"); /* {"SourceType":"Wizard","SourceIsReference":false,"DestinationType":"WizardItem","DestinationIsReference":false} */
            XR5000getInfoWizard.WireTo(id_8ea685db14ec42f4bd8bb4a297d16289, "children"); /* {"SourceType":"Wizard","SourceIsReference":false,"DestinationType":"WizardItem","DestinationIsReference":false} */
            XR5000getInfoWizard.WireTo(id_f62a188e32b04fd494e53c0486fd417d, "children"); /* {"SourceType":"Wizard","SourceIsReference":false,"DestinationType":"WizardItem","DestinationIsReference":false} */
            XR5000getInfoWizard.WireTo(id_673d82ee3c3b49e39694cd342c73d4ad, "children"); /* {"SourceType":"Wizard","SourceIsReference":false,"DestinationType":"WizardItem","DestinationIsReference":false} */
            id_39cc8679a22b46b4aebeef125cd5f0ae.WireTo(XR5000getSessionWizard, "eventOutput"); /* {"SourceType":"WizardItem","SourceIsReference":false,"DestinationType":"Wizard","DestinationIsReference":false} */
            XR5000getSessionWizard.WireTo(id_210bf17d3a114bc5a7a19a6ab678dc48, "children"); /* {"SourceType":"Wizard","SourceIsReference":false,"DestinationType":"WizardItem","DestinationIsReference":false} */
            XR5000getSessionWizard.WireTo(id_ae94e2df21a4408e8b0bc9a55d2d481e, "children"); /* {"SourceType":"Wizard","SourceIsReference":false,"DestinationType":"WizardItem","DestinationIsReference":false} */
            XR5000getSessionWizard.WireTo(id_09bf8feb99e14e8e82def502ce66a0bf, "children"); /* {"SourceType":"Wizard","SourceIsReference":false,"DestinationType":"WizardItem","DestinationIsReference":false} */
            XR5000getSessionWizard.WireTo(id_db985bc62bfd412d8db7559b1ffa2ad0, "children"); /* {"SourceType":"Wizard","SourceIsReference":false,"DestinationType":"WizardItem","DestinationIsReference":false} */
            XR5000getSessionWizard.WireTo(XR5000getInfoWizard, "backEventOutput"); /* {"SourceType":"Wizard","SourceIsReference":false,"DestinationType":"Wizard","DestinationIsReference":false} */
            XR5000ImportMenu.WireTo(XR5000getInfoWizard, "eventOutput"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"Wizard","DestinationIsReference":false} */
            id_210bf17d3a114bc5a7a19a6ab678dc48.WireTo(saveUsbSessionsDataToFileBrowser, "eventOutput"); /* {"SourceType":"WizardItem","SourceIsReference":false,"DestinationType":"SaveFileBrowser","DestinationIsReference":true} */
            id_ae94e2df21a4408e8b0bc9a55d2d481e.WireTo(naitLoginWindow, "eventOutput"); /* {"SourceType":"WizardItem","SourceIsReference":false,"DestinationType":"PopupWindow","DestinationIsReference":true} */
            id_09bf8feb99e14e8e82def502ce66a0bf.WireTo(nlisLoginWindow, "eventOutput"); /* {"SourceType":"WizardItem","SourceIsReference":false,"DestinationType":"PopupWindow","DestinationIsReference":true} */
            id_db985bc62bfd412d8db7559b1ffa2ad0.WireTo(mihubEventConnector, "eventOutput"); /* {"SourceType":"WizardItem","SourceIsReference":false,"DestinationType":"EventConnector","DestinationIsReference":true} */
            id_5ffa0209425b4dcea9e1a3f02ff216ea.WireTo(saveUsbLifeDataToFileBrowser, "eventOutput"); /* {"SourceType":"WizardItem","SourceIsReference":false,"DestinationType":"SaveFileBrowser","DestinationIsReference":true} */
            id_8ea685db14ec42f4bd8bb4a297d16289.WireTo(getInformationOffDeviceBrower, "eventOutput"); /* {"SourceType":"WizardItem","SourceIsReference":false,"DestinationType":"FolderBrowser","DestinationIsReference":true} */
            XR5000ExportMenu.WireTo(XR5000putInfoWizard, "eventOutput"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"Wizard","DestinationIsReference":false} */
            XR5000putInfoWizard.WireTo(id_748496bee52a4661b3699f367afb7904, "children"); /* {"SourceType":"Wizard","SourceIsReference":false,"DestinationType":"WizardItem","DestinationIsReference":false} */
            XR5000putInfoWizard.WireTo(id_48aaec02f3c245329aa25660a915faf7, "children"); /* {"SourceType":"Wizard","SourceIsReference":false,"DestinationType":"WizardItem","DestinationIsReference":false} */
            XR5000putInfoWizard.WireTo(id_a2cf0f9732584f008b372e537dd1cefb, "children"); /* {"SourceType":"Wizard","SourceIsReference":false,"DestinationType":"WizardItem","DestinationIsReference":false} */
            XR5000putInfoWizard.WireTo(id_835f7e3a147544e2997e5e7edeb864a7, "children"); /* {"SourceType":"Wizard","SourceIsReference":false,"DestinationType":"WizardItem","DestinationIsReference":false} */
            id_748496bee52a4661b3699f367afb7904.WireTo(uploadSessionFromPCToXr5000, "eventOutput"); /* {"SourceType":"WizardItem","SourceIsReference":false,"DestinationType":"OpenFileBrowser","DestinationIsReference":true} */
            id_a2cf0f9732584f008b372e537dd1cefb.WireTo(uploadFavSettingsBrowser, "eventOutput"); /* {"SourceType":"WizardItem","SourceIsReference":false,"DestinationType":"OpenFileBrowser","DestinationIsReference":true} */
            ToolBar.WireTo(XR5000Tools, "children"); /* {"SourceType":"Toolbar","SourceIsReference":true,"DestinationType":"Horizontal","DestinationIsReference":false} */
            XR5000Tools.WireTo(XR5000ImportTool, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"Tool","DestinationIsReference":false} */
            XR5000Tools.WireTo(XR5000ExportTool, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"Tool","DestinationIsReference":false} */
            XR5000Tools.WireTo(XR5000DeleteTool, "children"); /* {"SourceType":"Horizontal","SourceIsReference":false,"DestinationType":"Tool","DestinationIsReference":false} */
            XR5000ImportTool.WireTo(XR5000getInfoWizard, "eventOutput"); /* {"SourceType":"Tool","SourceIsReference":false,"DestinationType":"Wizard","DestinationIsReference":false} */
            XR5000ExportTool.WireTo(XR5000putInfoWizard, "eventOutput"); /* {"SourceType":"Tool","SourceIsReference":false,"DestinationType":"Wizard","DestinationIsReference":false} */
            XR5000DeleteTool.WireTo(Scale5000DeleteSessions, "eventOutput"); /* {"SourceType":"Tool","SourceIsReference":false,"DestinationType":"Wizard","DestinationIsReference":true} */
            XR5000DeleteMenu.WireTo(Scale5000DeleteSessions, "eventOutput"); /* {"SourceType":"MenuItem","SourceIsReference":false,"DestinationType":"Wizard","DestinationIsReference":true} */
            // END AUTO-GENERATED WIRING FOR test
        }
    }
}