using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;

namespace Application
{
    public class PortGraphNodeUI : IUI
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        // Private fields

        // Ports

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            Horizontal rootUI = new Horizontal();

            // BEGIN AUTO-GENERATED INSTANTIATIONS
            Vertical id_bfcf985ee1a840eca6e1e56e0f058b3e = new Vertical() {  };
            Vertical id_eff73577dd304c63a922c85feadbff72 = new Vertical() {  };
            Vertical outputPortsVert = new Vertical() { InstanceName = "outputPortsVert" };
            Box id_4bee6192687b4721a407e4b290a56920 = new Box() { Width = 50, Height = 20, Background = Brushes.White };
            Box id_c515462c36b449f5947a006147427e28 = new Box() { Width = 50, Height = 20, Background = Brushes.White };
            TextBox id_9e65fcb3a7f1489490be5757e1847df8 = new TextBox() { Width = 100 };
            Box nodeContainer = new Box() { InstanceName = "nodeContainer", Background = Brushes.LightSkyBlue, CornerRadius = new CornerRadius(3), BorderThickness = new Thickness(2) };
            Horizontal id_d3389b8a787c4d90b83033f4245270ba = new Horizontal() {  };
            Text id_875204790d06400da304a809e8440117 = new Text(text: "input") { HorizAlignment = HorizontalAlignment.Center };
            Text id_7625ab20ed6f431d8614bc7a9a93574d = new Text(text: "output") { HorizAlignment = HorizontalAlignment.Center };
            TextBox id_4cc658d80811405f8e7bd9237e6e0180 = new TextBox() { Width = 50 };
            Horizontal id_5e4998d17fe84b12a757a4070339c92e = new Horizontal() {  };
            Vertical inputPortsVert = new Vertical() { InstanceName = "inputPortsVert" };
            Vertical id_5fb315c4045b4494a81cc108261eba64 = new Vertical() {  };
            Button addNewInputPortButton = new Button(title: "+") { InstanceName = "addNewInputPortButton" };
            Data<object> id_eb270a751cc14c6692ddbf85b9bfdf32 = new Data<object>() { Lambda = () => inputPortsVert };
            DynamicWiring<object> id_e936c2790b9b40bbbd48e03f699aef63 = new DynamicWiring<object>(type: "UI", sourcePort: "children") {  };
            ApplyAction<object> id_d276b05055fe44e5b0380f2796800167 = new ApplyAction<object>() { Lambda = input =>{(input as IUI).GetWPFElement();} };
            UIFactory id_8450b02835dc47f9b619936b2331e03c = new UIFactory(getUIContainer: () =>{return new Box() {Width = 50,Height = 20,Background = Brushes.White};}) {  };
            DynamicWiring<object> id_21c61fb964ef427e9da574d5d0dc0360 = new DynamicWiring<object>(type: "UI", sourcePort: "uiLayout") {  };
            UIFactory id_fd2e2a6c759d4140897b0877cb65fabd = new UIFactory(getUIContainer: () =>{return new Text(text: "input") {HorizAlignment = HorizontalAlignment.Center};}) {  };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            rootUI.WireTo(nodeContainer, "children");
            id_bfcf985ee1a840eca6e1e56e0f058b3e.WireTo(inputPortsVert, "children");
            id_bfcf985ee1a840eca6e1e56e0f058b3e.WireTo(id_5fb315c4045b4494a81cc108261eba64, "children");
            id_eff73577dd304c63a922c85feadbff72.WireTo(id_5e4998d17fe84b12a757a4070339c92e, "children");
            outputPortsVert.WireTo(id_c515462c36b449f5947a006147427e28, "children");
            id_4bee6192687b4721a407e4b290a56920.WireTo(id_875204790d06400da304a809e8440117, "uiLayout");
            id_c515462c36b449f5947a006147427e28.WireTo(id_7625ab20ed6f431d8614bc7a9a93574d, "uiLayout");
            nodeContainer.WireTo(id_d3389b8a787c4d90b83033f4245270ba, "uiLayout");
            id_d3389b8a787c4d90b83033f4245270ba.WireTo(id_bfcf985ee1a840eca6e1e56e0f058b3e, "children");
            id_d3389b8a787c4d90b83033f4245270ba.WireTo(id_eff73577dd304c63a922c85feadbff72, "children");
            id_d3389b8a787c4d90b83033f4245270ba.WireTo(outputPortsVert, "children");
            id_5e4998d17fe84b12a757a4070339c92e.WireTo(id_9e65fcb3a7f1489490be5757e1847df8, "children");
            id_5e4998d17fe84b12a757a4070339c92e.WireTo(id_4cc658d80811405f8e7bd9237e6e0180, "children");
            inputPortsVert.WireTo(id_4bee6192687b4721a407e4b290a56920, "children");
            id_5fb315c4045b4494a81cc108261eba64.WireTo(addNewInputPortButton, "children");
            addNewInputPortButton.WireTo(id_eb270a751cc14c6692ddbf85b9bfdf32, "eventButtonClicked");
            id_eb270a751cc14c6692ddbf85b9bfdf32.WireTo(id_e936c2790b9b40bbbd48e03f699aef63, "dataOutput");
            id_e936c2790b9b40bbbd48e03f699aef63.WireTo(id_d276b05055fe44e5b0380f2796800167, "objectOutput");
            id_e936c2790b9b40bbbd48e03f699aef63.WireTo(id_8450b02835dc47f9b619936b2331e03c, "wireUi");
            id_8450b02835dc47f9b619936b2331e03c.WireTo(id_21c61fb964ef427e9da574d5d0dc0360, "uiInstanceOutput");
            id_21c61fb964ef427e9da574d5d0dc0360.WireTo(id_fd2e2a6c759d4140897b0877cb65fabd, "wireUi");
            // END AUTO-GENERATED WIRING

            return (rootUI as IUI).GetWPFElement();
        }

        // Methods

        public PortGraphNodeUI()
        {
            
        }
    }
}


























































