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
            Box id_4bee6192687b4721a407e4b290a56920 = new Box() { Width = 50, Height = 20, Background = Brushes.White };
            TextBox id_9e65fcb3a7f1489490be5757e1847df8 = new TextBox() { Width = 100 };
            Box nodeContainer = new Box() { InstanceName = "nodeContainer", Background = Brushes.LightSkyBlue, CornerRadius = new CornerRadius(3), BorderThickness = new Thickness(2) };
            Horizontal id_d3389b8a787c4d90b83033f4245270ba = new Horizontal() {  };
            Text id_875204790d06400da304a809e8440117 = new Text(text: "input") { HorizAlignment = HorizontalAlignment.Center };
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
            Vertical id_ce8fce6dd9dc42f980dba4959919f054 = new Vertical() {  };
            Vertical outputPortsVert = new Vertical() { InstanceName = "outputPortsVert" };
            Box id_ce7c08b2395b47e4b87fbde0b2838e1f = new Box() { Width = 50, Height = 20, Background = Brushes.White };
            Text id_3ece97c920974b138abef8992b0006ff = new Text(text: "output") { HorizAlignment = HorizontalAlignment.Center };
            Vertical id_88458a91a7f14fb38b05c5399691a50f = new Vertical() {  };
            Button addNewOutputPortButton = new Button(title: "+") { InstanceName = "addNewOutputPortButton" };
            Data<object> id_79a6aee28d19462186bbffb31b177c11 = new Data<object>() { Lambda = () => outputPortsVert };
            DynamicWiring<object> id_ff2491d6a36d4186ba3689ba8ac3dbc3 = new DynamicWiring<object>(type: "UI", sourcePort: "children") {  };
            ApplyAction<object> id_87d659ff55154b6781f60856b15d6cad = new ApplyAction<object>() { Lambda = input =>{(input as IUI).GetWPFElement();} };
            UIFactory id_c22a174dc77a4ecfa6d52c6c9bb752c9 = new UIFactory(getUIContainer: () =>{return new Box() {Width = 50,Height = 20,Background = Brushes.White};}) {  };
            DynamicWiring<object> id_4784c10f4ba94c12a334a87102ff1cbb = new DynamicWiring<object>(type: "UI", sourcePort: "uiLayout") {  };
            UIFactory id_11ed3a47b25e45cb9407e2707837add3 = new UIFactory(getUIContainer: () =>{return new Text(text: "output") {HorizAlignment = HorizontalAlignment.Center};}) {  };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            rootUI.WireTo(nodeContainer, "children");
            id_bfcf985ee1a840eca6e1e56e0f058b3e.WireTo(inputPortsVert, "children");
            id_bfcf985ee1a840eca6e1e56e0f058b3e.WireTo(id_5fb315c4045b4494a81cc108261eba64, "children");
            id_eff73577dd304c63a922c85feadbff72.WireTo(id_5e4998d17fe84b12a757a4070339c92e, "children");
            id_4bee6192687b4721a407e4b290a56920.WireTo(id_875204790d06400da304a809e8440117, "uiLayout");
            nodeContainer.WireTo(id_d3389b8a787c4d90b83033f4245270ba, "uiLayout");
            id_d3389b8a787c4d90b83033f4245270ba.WireTo(id_bfcf985ee1a840eca6e1e56e0f058b3e, "children");
            id_d3389b8a787c4d90b83033f4245270ba.WireTo(id_eff73577dd304c63a922c85feadbff72, "children");
            id_d3389b8a787c4d90b83033f4245270ba.WireTo(id_ce8fce6dd9dc42f980dba4959919f054, "children");
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
            id_ce8fce6dd9dc42f980dba4959919f054.WireTo(outputPortsVert, "children");
            id_ce8fce6dd9dc42f980dba4959919f054.WireTo(id_88458a91a7f14fb38b05c5399691a50f, "children");
            outputPortsVert.WireTo(id_ce7c08b2395b47e4b87fbde0b2838e1f, "children");
            id_ce7c08b2395b47e4b87fbde0b2838e1f.WireTo(id_3ece97c920974b138abef8992b0006ff, "uiLayout");
            id_88458a91a7f14fb38b05c5399691a50f.WireTo(addNewOutputPortButton, "children");
            addNewOutputPortButton.WireTo(id_79a6aee28d19462186bbffb31b177c11, "eventButtonClicked");
            id_79a6aee28d19462186bbffb31b177c11.WireTo(id_ff2491d6a36d4186ba3689ba8ac3dbc3, "dataOutput");
            id_ff2491d6a36d4186ba3689ba8ac3dbc3.WireTo(id_87d659ff55154b6781f60856b15d6cad, "objectOutput");
            id_ff2491d6a36d4186ba3689ba8ac3dbc3.WireTo(id_c22a174dc77a4ecfa6d52c6c9bb752c9, "wireUi");
            id_c22a174dc77a4ecfa6d52c6c9bb752c9.WireTo(id_4784c10f4ba94c12a334a87102ff1cbb, "uiInstanceOutput");
            id_4784c10f4ba94c12a334a87102ff1cbb.WireTo(id_11ed3a47b25e45cb9407e2707837add3, "wireUi");
            // END AUTO-GENERATED WIRING

            return (rootUI as IUI).GetWPFElement();
        }

        // Methods

        public PortGraphNodeUI()
        {
            
        }
    }
}




























































