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
            Vertical id_4a2c3b0ac86e4c08848f14f30c640cb0 = new Vertical() {  };
            Vertical id_4970deca1e9a4bf4b088a91f5f6260a5 = new Vertical() {  };
            Box id_ba9f2552dc7a40afae2ea9b9d5e1321e = new Box() { Width = 50, Height = 20, Background = Brushes.White };
            TextBox id_e378b8a4b01f43e9aaa86a8346084168 = new TextBox() { Width = 100 };
            Box nodeContainer = new Box() { InstanceName = "nodeContainer", Background = Brushes.LightSkyBlue, CornerRadius = new CornerRadius(3), BorderThickness = new Thickness(2) };
            Horizontal id_538a92db77414f16ad46065f9fe3cad6 = new Horizontal() {  };
            Text id_d619e0130e14474fb02d55db468e3829 = new Text(text: "input") { HorizAlignment = HorizontalAlignment.Center };
            TextBox id_ac6018ea85a3493885ccf1a0ea005c4c = new TextBox() { Width = 50 };
            Horizontal id_5ebddd2ada474e48b7c345a09dea21cc = new Horizontal() {  };
            Vertical inputPortsVert = new Vertical() { InstanceName = "inputPortsVert" };
            Vertical id_efc9a596414b4797847c5ddf8d82c634 = new Vertical() {  };
            Button addNewInputPortButton = new Button(title: "+") { InstanceName = "addNewInputPortButton" };
            Data<object> id_4d9c56f52279408b935faa27743d7b06 = new Data<object>() { Lambda = () => inputPortsVert };
            ApplyAction<object> id_68cd5521f81543d68bd67c62fa7367d0 = new ApplyAction<object>() { Lambda = input =>{(input as IUI).GetWPFElement();} };
            UIFactory id_ad5bde7abe1d458b98be2aac70473476 = new UIFactory(getUIContainer: () =>{return new Box() {Width = 50,Height = 20,Background = Brushes.White};}) {  };
            DynamicWiring<IUI> id_8e86d62f84b34863a6a62d53a032e3c5 = new DynamicWiring<IUI>() { SourcePort = "uiLayout" };
            UIFactory id_0150f777202545ff937cfed0730a0d03 = new UIFactory(getUIContainer: () =>{return new Text(text: "input") {HorizAlignment = HorizontalAlignment.Center};}) {  };
            Vertical id_e7ec539dc8f14104a73e529078265da6 = new Vertical() {  };
            Vertical outputPortsVert = new Vertical() { InstanceName = "outputPortsVert" };
            Box id_8d6fd5b0d45d4687b85c49a0eed97df1 = new Box() { Width = 50, Height = 20, Background = Brushes.White };
            Text id_5a2bd3d4434f42d89f11a7779d75c579 = new Text(text: "output") { HorizAlignment = HorizontalAlignment.Center };
            Vertical id_779111549ca04d2aa3c61295f0ecc363 = new Vertical() {  };
            Button addNewOutputPortButton = new Button(title: "+") { InstanceName = "addNewOutputPortButton" };
            Data<object> id_c19ba6d398f743558d477625dc927ebb = new Data<object>() { Lambda = () => outputPortsVert };
            DynamicWiring<IUI> id_8fdaabf0f7be47d3827202826085f315 = new DynamicWiring<IUI>() { SourcePort = "children" };
            ApplyAction<object> id_df1aed92066b447cadb10911cd5fbcfc = new ApplyAction<object>() { Lambda = input =>{(input as IUI).GetWPFElement();} };
            UIFactory id_13dd2ae8ed794e3b9f6e20cf572029a1 = new UIFactory(getUIContainer: () =>{return new Box() {Width = 50,Height = 20,Background = Brushes.White};}) {  };
            DynamicWiring<IUI> id_b90796dadf5845f48dc61f76eddae066 = new DynamicWiring<IUI>() { SourcePort = "uiLayout" };
            UIFactory id_1a68f6ae2a084ba9a61faf77be9e7099 = new UIFactory(getUIContainer: () =>{return new Text(text: "output") {HorizAlignment = HorizontalAlignment.Center};}) {  };
            MouseEvent id_44958262f161495b95793211ad858ecc = new MouseEvent(eventName: "MouseEnter") {  };
            ApplyAction<object> id_06f38856907347a780d9d6577b8b63d1 = new ApplyAction<object>() { Lambda = input =>{(input as Border).BorderBrush = Brushes.Red;(input as Border).BorderThickness = new Thickness(3);} };
            MouseEvent id_324a69051a7e47708d1c438f78304895 = new MouseEvent(eventName: "MouseLeave") {  };
            ApplyAction<object> id_b180ea6bc3144a79a1ac4ae4d03360b5 = new ApplyAction<object>() { Lambda = input =>{(input as Border).BorderBrush = Brushes.Black;(input as Border).BorderThickness = new Thickness(1);} };
            ApplyAction<object> id_bf054f397bd34b91a91c633e2afa180f = new ApplyAction<object>() { Lambda = input =>{(input as Border).Focus();} };
            RoutedEventSubscriber id_74c1b0b39c9c401aa1e94511ef66098e = new RoutedEventSubscriber(eventName: "GotFocus") {  };
            ApplyAction<object> id_ed929b67420349048209c70d8855e705 = new ApplyAction<object>() { Lambda = input =>{(input as Border).Background = Brushes.Aquamarine;} };
            RoutedEventSubscriber id_ed203e3b832b47f1ad6ac0c50ca1e019 = new RoutedEventSubscriber(eventName: "LostFocus") {  };
            ApplyAction<object> id_74822d51f2d44806bca5502182141685 = new ApplyAction<object>() { Lambda = input =>{(input as Border).Background = Brushes.LightSkyBlue;} };
            RoutedEventSubscriber id_bd3532856dbc4836824dcef169f007ae = new RoutedEventSubscriber(eventName: "GotFocus") {  };
            ApplyAction<object> id_d204e174801146b4888b24eb0f0795e3 = new ApplyAction<object>() { Lambda = input =>{(input as Border).Background = Brushes.LightSalmon;} };
            MouseButtonEvent id_ff90b17e6f384ae69ffbb3e5736c6957 = new MouseButtonEvent(eventName: "PreviewMouseLeftButtonDown") {  };
            RoutedEventSubscriber id_9c0b406fbdeb4614931b796fb4daf065 = new RoutedEventSubscriber(eventName: "LostFocus") {  };
            ApplyAction<object> id_f6052d7c78414c2d910aeacf425c0807 = new ApplyAction<object>() { Lambda = input =>{(input as Border).Background = Brushes.White;} };
            MouseButtonEvent id_8c83281fd8904a55a65c0f259fd73d99 = new MouseButtonEvent(eventName: "MouseLeftButtonDown") {  };
            ApplyAction<object> id_b0f88e4a77b44df78077d37d7f8828c9 = new ApplyAction<object>() { Lambda = input =>{var ui = input as Border;if (!ui.IsKeyboardFocusWithin) (input as Border).Focus();} };
            DynamicWiring<IEventHandler> id_66a273fb6d074dd8a18978ed71d5d768 = new DynamicWiring<IEventHandler>() { SourcePort = "eventHandlers" };
            EventHandlerConnector portHighlightingEventHandlers = new EventHandlerConnector() { InstanceName = "portHighlightingEventHandlers" };
            DynamicWiring<IEventHandler> id_af953ee65d6a40fd8daaea897010da1e = new DynamicWiring<IEventHandler>() { SourcePort = "eventHandlers" };
            DynamicWiring<IUI> id_3e4e55db2b8c409bb740b4a0e992c086 = new DynamicWiring<IUI>() { SourcePort = "children" };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            rootUI.WireTo(nodeContainer, "children");
            id_4a2c3b0ac86e4c08848f14f30c640cb0.WireTo(inputPortsVert, "children");
            id_4a2c3b0ac86e4c08848f14f30c640cb0.WireTo(id_efc9a596414b4797847c5ddf8d82c634, "children");
            id_4970deca1e9a4bf4b088a91f5f6260a5.WireTo(id_5ebddd2ada474e48b7c345a09dea21cc, "children");
            id_ba9f2552dc7a40afae2ea9b9d5e1321e.WireTo(id_d619e0130e14474fb02d55db468e3829, "uiLayout");
            id_ba9f2552dc7a40afae2ea9b9d5e1321e.WireTo(portHighlightingEventHandlers, "eventHandlers");
            nodeContainer.WireTo(id_538a92db77414f16ad46065f9fe3cad6, "uiLayout");
            nodeContainer.WireTo(id_74c1b0b39c9c401aa1e94511ef66098e, "eventHandlers");
            nodeContainer.WireTo(id_ed203e3b832b47f1ad6ac0c50ca1e019, "eventHandlers");
            nodeContainer.WireTo(id_8c83281fd8904a55a65c0f259fd73d99, "eventHandlers");
            id_538a92db77414f16ad46065f9fe3cad6.WireTo(id_4a2c3b0ac86e4c08848f14f30c640cb0, "children");
            id_538a92db77414f16ad46065f9fe3cad6.WireTo(id_4970deca1e9a4bf4b088a91f5f6260a5, "children");
            id_538a92db77414f16ad46065f9fe3cad6.WireTo(id_e7ec539dc8f14104a73e529078265da6, "children");
            id_5ebddd2ada474e48b7c345a09dea21cc.WireTo(id_e378b8a4b01f43e9aaa86a8346084168, "children");
            id_5ebddd2ada474e48b7c345a09dea21cc.WireTo(id_ac6018ea85a3493885ccf1a0ea005c4c, "children");
            inputPortsVert.WireTo(id_ba9f2552dc7a40afae2ea9b9d5e1321e, "children");
            id_efc9a596414b4797847c5ddf8d82c634.WireTo(addNewInputPortButton, "children");
            addNewInputPortButton.WireTo(id_4d9c56f52279408b935faa27743d7b06, "eventButtonClicked");
            id_4d9c56f52279408b935faa27743d7b06.WireTo(id_3e4e55db2b8c409bb740b4a0e992c086, "dataOutput");
            id_3e4e55db2b8c409bb740b4a0e992c086.WireTo(id_68cd5521f81543d68bd67c62fa7367d0, "objectOutput");
            id_3e4e55db2b8c409bb740b4a0e992c086.WireTo(id_ad5bde7abe1d458b98be2aac70473476, "wire");
            id_ad5bde7abe1d458b98be2aac70473476.WireTo(id_8e86d62f84b34863a6a62d53a032e3c5, "uiInstanceOutput");
            id_8e86d62f84b34863a6a62d53a032e3c5.WireTo(id_af953ee65d6a40fd8daaea897010da1e, "objectOutput");
            id_e7ec539dc8f14104a73e529078265da6.WireTo(outputPortsVert, "children");
            id_e7ec539dc8f14104a73e529078265da6.WireTo(id_779111549ca04d2aa3c61295f0ecc363, "children");
            outputPortsVert.WireTo(id_8d6fd5b0d45d4687b85c49a0eed97df1, "children");
            id_8d6fd5b0d45d4687b85c49a0eed97df1.WireTo(id_5a2bd3d4434f42d89f11a7779d75c579, "uiLayout");
            id_8d6fd5b0d45d4687b85c49a0eed97df1.WireTo(portHighlightingEventHandlers, "eventHandlers");
            id_779111549ca04d2aa3c61295f0ecc363.WireTo(addNewOutputPortButton, "children");
            addNewOutputPortButton.WireTo(id_c19ba6d398f743558d477625dc927ebb, "eventButtonClicked");
            id_c19ba6d398f743558d477625dc927ebb.WireTo(id_8fdaabf0f7be47d3827202826085f315, "dataOutput");
            id_8fdaabf0f7be47d3827202826085f315.WireTo(id_df1aed92066b447cadb10911cd5fbcfc, "objectOutput");
            id_13dd2ae8ed794e3b9f6e20cf572029a1.WireTo(id_b90796dadf5845f48dc61f76eddae066, "uiInstanceOutput");
            id_b90796dadf5845f48dc61f76eddae066.WireTo(id_66a273fb6d074dd8a18978ed71d5d768, "objectOutput");
            id_44958262f161495b95793211ad858ecc.WireTo(id_06f38856907347a780d9d6577b8b63d1, "senderOutput");
            id_324a69051a7e47708d1c438f78304895.WireTo(id_b180ea6bc3144a79a1ac4ae4d03360b5, "senderOutput");
            id_74c1b0b39c9c401aa1e94511ef66098e.WireTo(id_ed929b67420349048209c70d8855e705, "senderOutput");
            id_ed203e3b832b47f1ad6ac0c50ca1e019.WireTo(id_74822d51f2d44806bca5502182141685, "senderOutput");
            id_bd3532856dbc4836824dcef169f007ae.WireTo(id_d204e174801146b4888b24eb0f0795e3, "senderOutput");
            id_ff90b17e6f384ae69ffbb3e5736c6957.WireTo(id_bf054f397bd34b91a91c633e2afa180f, "senderOutput");
            id_9c0b406fbdeb4614931b796fb4daf065.WireTo(id_f6052d7c78414c2d910aeacf425c0807, "senderOutput");
            id_8c83281fd8904a55a65c0f259fd73d99.WireTo(id_b0f88e4a77b44df78077d37d7f8828c9, "senderOutput");
            portHighlightingEventHandlers.WireTo(id_44958262f161495b95793211ad858ecc, "propagatedHandlerFanoutList");
            portHighlightingEventHandlers.WireTo(id_324a69051a7e47708d1c438f78304895, "propagatedHandlerFanoutList");
            portHighlightingEventHandlers.WireTo(id_ff90b17e6f384ae69ffbb3e5736c6957, "propagatedHandlerFanoutList");
            portHighlightingEventHandlers.WireTo(id_bd3532856dbc4836824dcef169f007ae, "propagatedHandlerFanoutList");
            portHighlightingEventHandlers.WireTo(id_9c0b406fbdeb4614931b796fb4daf065, "propagatedHandlerFanoutList");
            // END AUTO-GENERATED WIRING

            
            return (rootUI as IUI).GetWPFElement();
        }

        // Methods

        public PortGraphNodeUI()
        {
            
        }
    }
}










