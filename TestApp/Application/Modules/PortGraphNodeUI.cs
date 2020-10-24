using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;
using Button = DomainAbstractions.Button;
using TextBox = DomainAbstractions.TextBox;

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
            Vertical id_a92452d95a9546cdaabc4ed922d3cc1c = new Vertical() {  };
            Vertical id_d9d5bb3e7cd749169f6798c4bdb8fa61 = new Vertical() {  };
            Box id_0d2b03e4498643ee941a5af51d538877 = new Box() { Width = 50, Height = 20, Background = Brushes.White };
            TextBox id_dcd425fecba0462485910275c2eb2edb = new TextBox() { Width = 100 };
            Box nodeContainer = new Box() { InstanceName = "nodeContainer", Background = Brushes.LightSkyBlue, CornerRadius = new CornerRadius(3), BorderThickness = new Thickness(2) };
            Horizontal id_e8542f49f88b49d6a4452f0e6237eade = new Horizontal() {  };
            Text id_f9bf1ead85254b439566a640fdfbed3b = new Text(text: "input") { HorizAlignment = HorizontalAlignment.Center };
            TextBox id_bcf2e36ced1743fba6d6c294deef5728 = new TextBox() { Width = 50 };
            Horizontal id_e26b74da402b4691bb26211e4e49c731 = new Horizontal() {  };
            Vertical inputPortsVert = new Vertical() { InstanceName = "inputPortsVert" };
            Vertical id_6482b2fae26b411b8e313aa60916d6a5 = new Vertical() {  };
            Button addNewInputPortButton = new Button(title: "+") { InstanceName = "addNewInputPortButton" };
            Data<object> id_b548b0443f724be59f61b5ca9077afc4 = new Data<object>() { Lambda = () => inputPortsVert };
            DynamicWiring<object> id_2b2d5e0c21dc4ed587350eb148f4b938 = new DynamicWiring<object>(type: "UI", sourcePort: "children") {  };
            ApplyAction<object> id_693fc1e2430e422c88b26544077e8202 = new ApplyAction<object>() { Lambda = input =>{(input as IUI).GetWPFElement();} };
            UIFactory id_77bd3ec9757c4a73b16a9adfb757c519 = new UIFactory(getUIContainer: () =>{return new Box() {Width = 50,Height = 20,Background = Brushes.White};}) {  };
            DynamicWiring<object> id_14471ab23d8c42279d33b1fdcd86c1dc = new DynamicWiring<object>(type: "UI", sourcePort: "uiLayout") {  };
            UIFactory id_a9ef20ef31624d42b8294b2599cdabe8 = new UIFactory(getUIContainer: () =>{return new Text(text: "input") {HorizAlignment = HorizontalAlignment.Center};}) {  };
            Vertical id_925072d089a245fcbeedf62c53082be2 = new Vertical() {  };
            Vertical outputPortsVert = new Vertical() { InstanceName = "outputPortsVert" };
            Box id_24fb36fbbf95497abc3fc7e27067b288 = new Box() { Width = 50, Height = 20, Background = Brushes.White };
            Text id_67b7cade212e4f3db87661068cd8deeb = new Text(text: "output") { HorizAlignment = HorizontalAlignment.Center };
            Vertical id_990dc324d010492c98413c74070aa30a = new Vertical() {  };
            Button addNewOutputPortButton = new Button(title: "+") { InstanceName = "addNewOutputPortButton" };
            Data<object> id_40271280a6bc48d293c1b5eeb8b443f3 = new Data<object>() { Lambda = () => outputPortsVert };
            DynamicWiring<object> id_00b8fb81a1d047eead0066a113f2183f = new DynamicWiring<object>(type: "UI", sourcePort: "children") {  };
            ApplyAction<object> id_bcde44e7906e491abe55418adbaf3bf6 = new ApplyAction<object>() { Lambda = input =>{(input as IUI).GetWPFElement();} };
            UIFactory id_40ceff41a28f47efbdc9f7bb0b49a6f5 = new UIFactory(getUIContainer: () =>{return new Box() {Width = 50,Height = 20,Background = Brushes.White};}) {  };
            DynamicWiring<object> id_4feebe5ae4e74080995cf45dd5ada0d9 = new DynamicWiring<object>(type: "UI", sourcePort: "uiLayout") {  };
            UIFactory id_87a7dfedb4ca4e36a11c0d7bc3b23c8a = new UIFactory(getUIContainer: () =>{return new Text(text: "output") {HorizAlignment = HorizontalAlignment.Center};}) {  };
            MouseEvent id_431e2d5b83184e118ce2f334425bec51 = new MouseEvent(eventName: "MouseEnter") {  };
            ApplyAction<object> id_991e2c0985be48d18f8c43cbc5c9f6f1 = new ApplyAction<object>() { Lambda = input =>{(input as Border).BorderBrush = Brushes.Red;(input as Border).BorderThickness = new Thickness(3);} };
            MouseEvent id_b41fd61055714251b1869f2309f3c066 = new MouseEvent(eventName: "MouseLeave") {  };
            ApplyAction<object> id_0644a76ebc034f98b4bc0c8960f7a6e0 = new ApplyAction<object>() { Lambda = input =>{(input as Border).BorderBrush = Brushes.Black;(input as Border).BorderThickness = new Thickness(1);} };
            ApplyAction<object> id_c05d2afcd8b441a09e6c0352bd8119ed = new ApplyAction<object>() { Lambda = input =>{(input as Border).Focus();} };
            RoutedEventSubscriber id_0932e74cd58c4d0cbe21fa8326ab310d = new RoutedEventSubscriber(eventName: "GotFocus") {  };
            ApplyAction<object> id_e837b265d4db4ac0920f1aaca8759a6b = new ApplyAction<object>() { Lambda = input =>{(input as Border).Background = Brushes.Aquamarine;} };
            RoutedEventSubscriber id_5697706026b34af298a6d7ee8cd30b6a = new RoutedEventSubscriber(eventName: "LostFocus") {  };
            ApplyAction<object> id_c49c167c49934c44afefd11f28cab8e7 = new ApplyAction<object>() { Lambda = input =>{(input as Border).Background = Brushes.LightSkyBlue;} };
            RoutedEventSubscriber id_095fa16077014c00888da9da44572d95 = new RoutedEventSubscriber(eventName: "GotFocus") {  };
            ApplyAction<object> id_063353bc04cd4976b23d926cdf4ad00d = new ApplyAction<object>() { Lambda = input =>{(input as Border).Background = Brushes.LightSalmon;} };
            MouseButtonEvent id_90ff330bc81841e0a3859dafee9110f4 = new MouseButtonEvent(eventName: "PreviewMouseLeftButtonDown") {  };
            RoutedEventSubscriber id_8430ed032c79413fa10c861957f171b6 = new RoutedEventSubscriber(eventName: "LostFocus") {  };
            ApplyAction<object> id_6778c722e2d3413e90bd5b893971ab90 = new ApplyAction<object>() { Lambda = input =>{(input as Border).Background = Brushes.White;} };
            MouseButtonEvent id_c190d7a80e704208905ea415dfa0bf48 = new MouseButtonEvent(eventName: "MouseLeftButtonDown") {  };
            ApplyAction<object> id_97c80854fd264dc08e6497db25407383 = new ApplyAction<object>() { Lambda = input =>{var ui = input as Border;if (!ui.IsKeyboardFocusWithin) (input as Border).Focus();} };
            DynamicWiring<object> id_cfca0c2c00f54bf8ba2a1406ac8cadf6 = new DynamicWiring<object>(type: "EventHandler", sourcePort: "eventHandlers") {  };
            EventHandlerConnector portHighlightingEventHandlers = new EventHandlerConnector() { InstanceName = "portHighlightingEventHandlers" };
            DynamicWiring<object> id_594d535dd2d14bd38599afd7afcb3212 = new DynamicWiring<object>(type: "EventHandler", sourcePort: "eventHandlers") {  };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            rootUI.WireTo(nodeContainer, "children");
            id_a92452d95a9546cdaabc4ed922d3cc1c.WireTo(inputPortsVert, "children");
            id_a92452d95a9546cdaabc4ed922d3cc1c.WireTo(id_6482b2fae26b411b8e313aa60916d6a5, "children");
            id_d9d5bb3e7cd749169f6798c4bdb8fa61.WireTo(id_e26b74da402b4691bb26211e4e49c731, "children");
            id_0d2b03e4498643ee941a5af51d538877.WireTo(id_f9bf1ead85254b439566a640fdfbed3b, "uiLayout");
            id_0d2b03e4498643ee941a5af51d538877.WireTo(portHighlightingEventHandlers, "eventHandlers");
            nodeContainer.WireTo(id_e8542f49f88b49d6a4452f0e6237eade, "uiLayout");
            nodeContainer.WireTo(id_0932e74cd58c4d0cbe21fa8326ab310d, "eventHandlers");
            nodeContainer.WireTo(id_5697706026b34af298a6d7ee8cd30b6a, "eventHandlers");
            nodeContainer.WireTo(id_c190d7a80e704208905ea415dfa0bf48, "eventHandlers");
            id_e8542f49f88b49d6a4452f0e6237eade.WireTo(id_a92452d95a9546cdaabc4ed922d3cc1c, "children");
            id_e8542f49f88b49d6a4452f0e6237eade.WireTo(id_d9d5bb3e7cd749169f6798c4bdb8fa61, "children");
            id_e8542f49f88b49d6a4452f0e6237eade.WireTo(id_925072d089a245fcbeedf62c53082be2, "children");
            id_e26b74da402b4691bb26211e4e49c731.WireTo(id_dcd425fecba0462485910275c2eb2edb, "children");
            id_e26b74da402b4691bb26211e4e49c731.WireTo(id_bcf2e36ced1743fba6d6c294deef5728, "children");
            inputPortsVert.WireTo(id_0d2b03e4498643ee941a5af51d538877, "children");
            id_6482b2fae26b411b8e313aa60916d6a5.WireTo(addNewInputPortButton, "children");
            addNewInputPortButton.WireTo(id_b548b0443f724be59f61b5ca9077afc4, "eventButtonClicked");
            id_b548b0443f724be59f61b5ca9077afc4.WireTo(id_2b2d5e0c21dc4ed587350eb148f4b938, "dataOutput");
            id_2b2d5e0c21dc4ed587350eb148f4b938.WireTo(id_693fc1e2430e422c88b26544077e8202, "objectOutput");
            id_2b2d5e0c21dc4ed587350eb148f4b938.WireTo(id_77bd3ec9757c4a73b16a9adfb757c519, "wireUi");
            id_77bd3ec9757c4a73b16a9adfb757c519.WireTo(id_14471ab23d8c42279d33b1fdcd86c1dc, "uiInstanceOutput");
            id_14471ab23d8c42279d33b1fdcd86c1dc.WireTo(id_594d535dd2d14bd38599afd7afcb3212, "objectOutput");
            id_14471ab23d8c42279d33b1fdcd86c1dc.WireTo(id_a9ef20ef31624d42b8294b2599cdabe8, "wireUi");
            id_925072d089a245fcbeedf62c53082be2.WireTo(outputPortsVert, "children");
            id_925072d089a245fcbeedf62c53082be2.WireTo(id_990dc324d010492c98413c74070aa30a, "children");
            outputPortsVert.WireTo(id_24fb36fbbf95497abc3fc7e27067b288, "children");
            id_24fb36fbbf95497abc3fc7e27067b288.WireTo(id_67b7cade212e4f3db87661068cd8deeb, "uiLayout");
            id_24fb36fbbf95497abc3fc7e27067b288.WireTo(portHighlightingEventHandlers, "eventHandlers");
            id_990dc324d010492c98413c74070aa30a.WireTo(addNewOutputPortButton, "children");
            addNewOutputPortButton.WireTo(id_40271280a6bc48d293c1b5eeb8b443f3, "eventButtonClicked");
            id_40271280a6bc48d293c1b5eeb8b443f3.WireTo(id_00b8fb81a1d047eead0066a113f2183f, "dataOutput");
            id_00b8fb81a1d047eead0066a113f2183f.WireTo(id_bcde44e7906e491abe55418adbaf3bf6, "objectOutput");
            id_00b8fb81a1d047eead0066a113f2183f.WireTo(id_40ceff41a28f47efbdc9f7bb0b49a6f5, "wireUi");
            id_40ceff41a28f47efbdc9f7bb0b49a6f5.WireTo(id_4feebe5ae4e74080995cf45dd5ada0d9, "uiInstanceOutput");
            id_4feebe5ae4e74080995cf45dd5ada0d9.WireTo(id_cfca0c2c00f54bf8ba2a1406ac8cadf6, "objectOutput");
            id_4feebe5ae4e74080995cf45dd5ada0d9.WireTo(id_87a7dfedb4ca4e36a11c0d7bc3b23c8a, "wireUi");
            id_431e2d5b83184e118ce2f334425bec51.WireTo(id_991e2c0985be48d18f8c43cbc5c9f6f1, "senderOutput");
            id_b41fd61055714251b1869f2309f3c066.WireTo(id_0644a76ebc034f98b4bc0c8960f7a6e0, "senderOutput");
            id_0932e74cd58c4d0cbe21fa8326ab310d.WireTo(id_e837b265d4db4ac0920f1aaca8759a6b, "senderOutput");
            id_5697706026b34af298a6d7ee8cd30b6a.WireTo(id_c49c167c49934c44afefd11f28cab8e7, "senderOutput");
            id_095fa16077014c00888da9da44572d95.WireTo(id_063353bc04cd4976b23d926cdf4ad00d, "senderOutput");
            id_90ff330bc81841e0a3859dafee9110f4.WireTo(id_c05d2afcd8b441a09e6c0352bd8119ed, "senderOutput");
            id_8430ed032c79413fa10c861957f171b6.WireTo(id_6778c722e2d3413e90bd5b893971ab90, "senderOutput");
            id_c190d7a80e704208905ea415dfa0bf48.WireTo(id_97c80854fd264dc08e6497db25407383, "senderOutput");
            id_cfca0c2c00f54bf8ba2a1406ac8cadf6.WireTo(portHighlightingEventHandlers, "wireEventHandler");
            portHighlightingEventHandlers.WireTo(id_431e2d5b83184e118ce2f334425bec51, "propagatedHandlerFanoutList");
            portHighlightingEventHandlers.WireTo(id_b41fd61055714251b1869f2309f3c066, "propagatedHandlerFanoutList");
            portHighlightingEventHandlers.WireTo(id_90ff330bc81841e0a3859dafee9110f4, "propagatedHandlerFanoutList");
            portHighlightingEventHandlers.WireTo(id_095fa16077014c00888da9da44572d95, "propagatedHandlerFanoutList");
            portHighlightingEventHandlers.WireTo(id_8430ed032c79413fa10c861957f171b6, "propagatedHandlerFanoutList");
            id_594d535dd2d14bd38599afd7afcb3212.WireTo(portHighlightingEventHandlers, "wireEventHandler");
            // END AUTO-GENERATED WIRING

            
            return (rootUI as IUI).GetWPFElement();
        }

        // Methods

        public PortGraphNodeUI()
        {
            
        }
    }
}




































































































































