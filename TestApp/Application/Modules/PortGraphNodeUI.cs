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
            Vertical id_d875be24622240a1b8945fba84d5ccb6 = new Vertical() {  };
            Vertical id_a613d39b64ba4037a4fd3fc61b4cf3d0 = new Vertical() {  };
            Box id_dfa127e777a24a59a43f2655ffda9222 = new Box() { Width = 50, Height = 20, Background = Brushes.White };
            TextBox id_f903bda8ecaf461f9d23d932cdbce658 = new TextBox() { Width = 100 };
            Box nodeContainer = new Box() { InstanceName = "nodeContainer", Background = Brushes.LightSkyBlue, CornerRadius = new CornerRadius(3), BorderThickness = new Thickness(2) };
            Horizontal id_1505cdf911eb42bcaac9afc8845b36b3 = new Horizontal() {  };
            Text id_4f477e1d764444f9b82e0424eeaa259d = new Text(text: "input") { HorizAlignment = HorizontalAlignment.Center };
            TextBox id_672a1c0f6e744206b25f319732a42482 = new TextBox() { Width = 50 };
            Horizontal id_d3ee4eec61cd4569a9e062ccb4fd165a = new Horizontal() {  };
            Vertical inputPortsVert = new Vertical() { InstanceName = "inputPortsVert" };
            Vertical id_8da72a92a9d2404597ee47f5f37862af = new Vertical() {  };
            Button addNewInputPortButton = new Button(title: "+") { InstanceName = "addNewInputPortButton" };
            Data<object> id_c3f7ec4f4cb14ba2bd8aa6906aab2e40 = new Data<object>() { Lambda = () => inputPortsVert };
            ApplyAction<object> id_2a53d75c726e445dbcf0746388a4cd51 = new ApplyAction<object>() { Lambda = input =>{(input as IUI).GetWPFElement();} };
            UIFactory id_eec07c533aeb405ba6bf267ce1ed2361 = new UIFactory(getUIContainer: () =>{return new Box() {Width = 50,Height = 20,Background = Brushes.White};}) {  };
            DynamicWiring<IUI> id_c5aa5049d11042fba4005d32426cd809 = new DynamicWiring<IUI>() { SourcePort = "uiLayout" };
            Vertical id_04d34510a53c452094cf555e090ac256 = new Vertical() {  };
            Vertical outputPortsVert = new Vertical() { InstanceName = "outputPortsVert" };
            Box id_e4cd3068f497436297d8d9b3af10b72d = new Box() { Width = 50, Height = 20, Background = Brushes.White };
            Text id_1d665a1bff7e4a01bf9112b2c0fd2df7 = new Text(text: "output") { HorizAlignment = HorizontalAlignment.Center };
            Vertical id_05b4db694f49468f84a16665d0b330db = new Vertical() {  };
            Button addNewOutputPortButton = new Button(title: "+") { InstanceName = "addNewOutputPortButton" };
            Data<object> id_abd8cfb2a3824f0da60ca4a5a06f0bd6 = new Data<object>() { Lambda = () => outputPortsVert };
            DynamicWiring<IUI> id_ba5eb00fc8f442d2ab427d07cff82e0f = new DynamicWiring<IUI>() { SourcePort = "children" };
            ApplyAction<object> id_e179fdc566804e09a32a6b6a2b9f2f5c = new ApplyAction<object>() { Lambda = input =>{(input as IUI).GetWPFElement();} };
            MouseEvent id_9a597fa1298b4c689aaa91a03147a3b0 = new MouseEvent(eventName: "MouseEnter") {  };
            ApplyAction<object> id_16987d7f777249a19e7b5d6bc574fc1d = new ApplyAction<object>() { Lambda = input =>{(input as Border).BorderBrush = Brushes.Red;(input as Border).BorderThickness = new Thickness(3);} };
            MouseEvent id_2f83642905874b88abce2836b0bd5e12 = new MouseEvent(eventName: "MouseLeave") {  };
            ApplyAction<object> id_0ada100382134ba5a0ab5b35e77cade5 = new ApplyAction<object>() { Lambda = input =>{(input as Border).BorderBrush = Brushes.Black;(input as Border).BorderThickness = new Thickness(1);} };
            ApplyAction<object> id_08b48037664e466d86d0ca0851b93f55 = new ApplyAction<object>() { Lambda = input =>{(input as Border).Focus();} };
            RoutedEventSubscriber id_486862d8d7e543f8b4b3da12ad3b9b61 = new RoutedEventSubscriber(eventName: "GotFocus") {  };
            ApplyAction<object> id_efe2192ebdf0426c8e3384c6348c296e = new ApplyAction<object>() { Lambda = input =>{(input as Border).Background = Brushes.Aquamarine;} };
            RoutedEventSubscriber id_f12593350255460f94ec17349f3bcd32 = new RoutedEventSubscriber(eventName: "LostFocus") {  };
            ApplyAction<object> id_679a1b5ec68e4e8fbb964fbb6f5c1ce1 = new ApplyAction<object>() { Lambda = input =>{(input as Border).Background = Brushes.LightSkyBlue;} };
            RoutedEventSubscriber id_86e21b3a82a94017a9459acee40e842e = new RoutedEventSubscriber(eventName: "GotFocus") {  };
            ApplyAction<object> id_d7ef067799fc4293986d32bf573a15c1 = new ApplyAction<object>() { Lambda = input =>{(input as Border).Background = Brushes.LightSalmon;} };
            MouseButtonEvent id_72bb699c091442e0935d827757d6fd4e = new MouseButtonEvent(eventName: "PreviewMouseLeftButtonDown") {  };
            RoutedEventSubscriber id_04d0b90171c647b48d2ba5fc26456d96 = new RoutedEventSubscriber(eventName: "LostFocus") {  };
            ApplyAction<object> id_e571cb8e4de44f4ab7cddc8598275b4a = new ApplyAction<object>() { Lambda = input =>{(input as Border).Background = Brushes.White;} };
            MouseButtonEvent id_c9f05448d22d4b81859718349a0d8873 = new MouseButtonEvent(eventName: "MouseLeftButtonDown") {  };
            ApplyAction<object> id_d315aa701432480bb9dce6f67dc4dcb3 = new ApplyAction<object>() { Lambda = input =>{var ui = input as Border;if (!ui.IsKeyboardFocusWithin) (input as Border).Focus();} };
            EventHandlerConnector portHighlightingEventHandlers = new EventHandlerConnector() { InstanceName = "portHighlightingEventHandlers" };
            DynamicWiring<IEventHandler> id_0172168573d44cdca488bbf138805151 = new DynamicWiring<IEventHandler>() { SourcePort = "eventHandlers" };
            DynamicWiring<IUI> id_47cbe03dcbca4f73b451334d8b06866a = new DynamicWiring<IUI>() { SourcePort = "children" };
            UIFactory id_36df652fcb2c450a80df1593d5513d84 = new UIFactory(getUIContainer: () =>{return new Box() {Width = 50,Height = 20,Background = Brushes.White};}) {  };
            DynamicWiring<IUI> id_cce272b656054c99b398323e84421517 = new DynamicWiring<IUI>() { SourcePort = "uiLayout" };
            DynamicWiring<IEventHandler> id_10d135652de14cbfba074536274556bd = new DynamicWiring<IEventHandler>() { SourcePort = "eventHandlers" };
            UIFactory id_6cc88bc590e2455595fc9396525d80cd = new UIFactory(getUIContainer: () =>{return new Text(text: "output") {HorizAlignment = HorizontalAlignment.Center};}) {  };
            UIFactory id_5a5fb30e467e4a5c8b504bdf68e8606f = new UIFactory(getUIContainer: () =>{return new Text(text: "input") {HorizAlignment = HorizontalAlignment.Center};}) {  };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            rootUI.WireTo(nodeContainer, "children");
            id_d875be24622240a1b8945fba84d5ccb6.WireTo(inputPortsVert, "children");
            id_d875be24622240a1b8945fba84d5ccb6.WireTo(id_8da72a92a9d2404597ee47f5f37862af, "children");
            id_a613d39b64ba4037a4fd3fc61b4cf3d0.WireTo(id_d3ee4eec61cd4569a9e062ccb4fd165a, "children");
            id_dfa127e777a24a59a43f2655ffda9222.WireTo(id_4f477e1d764444f9b82e0424eeaa259d, "uiLayout");
            id_dfa127e777a24a59a43f2655ffda9222.WireTo(portHighlightingEventHandlers, "eventHandlers");
            nodeContainer.WireTo(id_1505cdf911eb42bcaac9afc8845b36b3, "uiLayout");
            nodeContainer.WireTo(id_486862d8d7e543f8b4b3da12ad3b9b61, "eventHandlers");
            nodeContainer.WireTo(id_f12593350255460f94ec17349f3bcd32, "eventHandlers");
            nodeContainer.WireTo(id_c9f05448d22d4b81859718349a0d8873, "eventHandlers");
            id_1505cdf911eb42bcaac9afc8845b36b3.WireTo(id_d875be24622240a1b8945fba84d5ccb6, "children");
            id_1505cdf911eb42bcaac9afc8845b36b3.WireTo(id_a613d39b64ba4037a4fd3fc61b4cf3d0, "children");
            id_1505cdf911eb42bcaac9afc8845b36b3.WireTo(id_04d34510a53c452094cf555e090ac256, "children");
            id_d3ee4eec61cd4569a9e062ccb4fd165a.WireTo(id_f903bda8ecaf461f9d23d932cdbce658, "children");
            id_d3ee4eec61cd4569a9e062ccb4fd165a.WireTo(id_672a1c0f6e744206b25f319732a42482, "children");
            inputPortsVert.WireTo(id_dfa127e777a24a59a43f2655ffda9222, "children");
            id_8da72a92a9d2404597ee47f5f37862af.WireTo(addNewInputPortButton, "children");
            addNewInputPortButton.WireTo(id_c3f7ec4f4cb14ba2bd8aa6906aab2e40, "eventButtonClicked");
            id_c3f7ec4f4cb14ba2bd8aa6906aab2e40.WireTo(id_47cbe03dcbca4f73b451334d8b06866a, "dataOutput");
            id_eec07c533aeb405ba6bf267ce1ed2361.WireTo(id_c5aa5049d11042fba4005d32426cd809, "uiInstanceOutput");
            id_c5aa5049d11042fba4005d32426cd809.WireTo(id_5a5fb30e467e4a5c8b504bdf68e8606f, "wire");
            id_c5aa5049d11042fba4005d32426cd809.WireTo(id_0172168573d44cdca488bbf138805151, "objectOutput");
            id_04d34510a53c452094cf555e090ac256.WireTo(outputPortsVert, "children");
            id_04d34510a53c452094cf555e090ac256.WireTo(id_05b4db694f49468f84a16665d0b330db, "children");
            outputPortsVert.WireTo(id_e4cd3068f497436297d8d9b3af10b72d, "children");
            id_e4cd3068f497436297d8d9b3af10b72d.WireTo(id_1d665a1bff7e4a01bf9112b2c0fd2df7, "uiLayout");
            id_e4cd3068f497436297d8d9b3af10b72d.WireTo(portHighlightingEventHandlers, "eventHandlers");
            id_05b4db694f49468f84a16665d0b330db.WireTo(addNewOutputPortButton, "children");
            addNewOutputPortButton.WireTo(id_abd8cfb2a3824f0da60ca4a5a06f0bd6, "eventButtonClicked");
            id_abd8cfb2a3824f0da60ca4a5a06f0bd6.WireTo(id_ba5eb00fc8f442d2ab427d07cff82e0f, "dataOutput");
            id_ba5eb00fc8f442d2ab427d07cff82e0f.WireTo(id_36df652fcb2c450a80df1593d5513d84, "wire");
            id_ba5eb00fc8f442d2ab427d07cff82e0f.WireTo(id_e179fdc566804e09a32a6b6a2b9f2f5c, "objectOutput");
            id_9a597fa1298b4c689aaa91a03147a3b0.WireTo(id_16987d7f777249a19e7b5d6bc574fc1d, "senderOutput");
            id_2f83642905874b88abce2836b0bd5e12.WireTo(id_0ada100382134ba5a0ab5b35e77cade5, "senderOutput");
            id_486862d8d7e543f8b4b3da12ad3b9b61.WireTo(id_efe2192ebdf0426c8e3384c6348c296e, "senderOutput");
            id_f12593350255460f94ec17349f3bcd32.WireTo(id_679a1b5ec68e4e8fbb964fbb6f5c1ce1, "senderOutput");
            id_86e21b3a82a94017a9459acee40e842e.WireTo(id_d7ef067799fc4293986d32bf573a15c1, "senderOutput");
            id_72bb699c091442e0935d827757d6fd4e.WireTo(id_08b48037664e466d86d0ca0851b93f55, "senderOutput");
            id_04d0b90171c647b48d2ba5fc26456d96.WireTo(id_e571cb8e4de44f4ab7cddc8598275b4a, "senderOutput");
            id_c9f05448d22d4b81859718349a0d8873.WireTo(id_d315aa701432480bb9dce6f67dc4dcb3, "senderOutput");
            portHighlightingEventHandlers.WireTo(id_9a597fa1298b4c689aaa91a03147a3b0, "propagatedHandlerFanoutList");
            portHighlightingEventHandlers.WireTo(id_2f83642905874b88abce2836b0bd5e12, "propagatedHandlerFanoutList");
            portHighlightingEventHandlers.WireTo(id_72bb699c091442e0935d827757d6fd4e, "propagatedHandlerFanoutList");
            portHighlightingEventHandlers.WireTo(id_86e21b3a82a94017a9459acee40e842e, "propagatedHandlerFanoutList");
            portHighlightingEventHandlers.WireTo(id_04d0b90171c647b48d2ba5fc26456d96, "propagatedHandlerFanoutList");
            id_0172168573d44cdca488bbf138805151.WireTo(portHighlightingEventHandlers, "wire");
            id_47cbe03dcbca4f73b451334d8b06866a.WireTo(id_eec07c533aeb405ba6bf267ce1ed2361, "wire");
            id_47cbe03dcbca4f73b451334d8b06866a.WireTo(id_2a53d75c726e445dbcf0746388a4cd51, "objectOutput");
            id_36df652fcb2c450a80df1593d5513d84.WireTo(id_cce272b656054c99b398323e84421517, "uiInstanceOutput");
            id_cce272b656054c99b398323e84421517.WireTo(id_6cc88bc590e2455595fc9396525d80cd, "wire");
            id_cce272b656054c99b398323e84421517.WireTo(id_10d135652de14cbfba074536274556bd, "objectOutput");
            id_10d135652de14cbfba074536274556bd.WireTo(portHighlightingEventHandlers, "wire");
            // END AUTO-GENERATED WIRING

            
            return (rootUI as IUI).GetWPFElement();
        }

        // Methods

        public PortGraphNodeUI()
        {
            
        }
    }
}






















