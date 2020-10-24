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
            Vertical id_85122e00757a4f998479737018f29c83 = new Vertical() {  };
            Vertical id_0b538d4dc0834a8ea0f2a342d3beb071 = new Vertical() {  };
            Box id_7d2439234d2f465e8af6805f74e9ce01 = new Box() { Width = 50, Height = 20, Background = Brushes.White };
            TextBox id_7262ab9707f34185927901b7281adefc = new TextBox() { Width = 100 };
            Box nodeContainer = new Box() { InstanceName = "nodeContainer", Background = Brushes.LightSkyBlue, CornerRadius = new CornerRadius(3), BorderThickness = new Thickness(2) };
            Horizontal id_9ee3f78a9f5a4533a65f1e9a49e2eb31 = new Horizontal() {  };
            Text id_effeeb8bb70c4e3083601f63a0ae830f = new Text(text: "input") { HorizAlignment = HorizontalAlignment.Center };
            TextBox id_031bfa7681574f48adbf8494b9148e66 = new TextBox() { Width = 50 };
            Horizontal id_c46c654c8a1f48d8aaf945371cec7e07 = new Horizontal() {  };
            Vertical inputPortsVert = new Vertical() { InstanceName = "inputPortsVert" };
            Vertical id_90c1a73332b34846bfd583fce7148f07 = new Vertical() {  };
            Button addNewInputPortButton = new Button(title: "+") { InstanceName = "addNewInputPortButton" };
            Data<object> id_edbf30d1617b451fb98820eb851333a5 = new Data<object>() { Lambda = () => inputPortsVert };
            ApplyAction<object> id_7413e1377c6940059dce785fa75396c9 = new ApplyAction<object>() { Lambda = input =>{(input as IUI).GetWPFElement();} };
            UIFactory id_0434585e578c431eb28d8cfd58b06556 = new UIFactory(getUIContainer: () =>{return new Box() {Width = 50,Height = 20,Background = Brushes.White};}) {  };
            DynamicWiring<IUI> id_fddf0cac82ce4325a8d72d7b02a5caea = new DynamicWiring<IUI>() { SourcePort = "uiLayout" };
            Vertical id_a06ac9f154124b5b8b102e3a7d80ec94 = new Vertical() {  };
            Vertical outputPortsVert = new Vertical() { InstanceName = "outputPortsVert" };
            Box id_4daf95d474d640e3b1bc1e405dc3caa4 = new Box() { Width = 50, Height = 20, Background = Brushes.White };
            Text id_8cd0a39b90f34145819a2b7053961316 = new Text(text: "output") { HorizAlignment = HorizontalAlignment.Center };
            Vertical id_bd61a7d561874147956ace9e882f3496 = new Vertical() {  };
            Button addNewOutputPortButton = new Button(title: "+") { InstanceName = "addNewOutputPortButton" };
            Data<object> id_57d00540867d45068276fee80801deb7 = new Data<object>() { Lambda = () => outputPortsVert };
            DynamicWiring<IUI> id_55f877f64bfd402ebcd48349b4152770 = new DynamicWiring<IUI>() { SourcePort = "children" };
            ApplyAction<object> id_2bb70c4f9d5944919dbe496b0b27348c = new ApplyAction<object>() { Lambda = input =>{(input as IUI).GetWPFElement();} };
            MouseEvent id_58d14a1a773648b0b765af74a0a45069 = new MouseEvent(eventName: "MouseEnter") {  };
            ApplyAction<object> id_54fc5ddc095c4b49b76a61590ef2bb03 = new ApplyAction<object>() { Lambda = input =>{(input as Border).BorderBrush = Brushes.Red;(input as Border).BorderThickness = new Thickness(3);} };
            MouseEvent id_3467ce1809fa42f18677db7192dd68e6 = new MouseEvent(eventName: "MouseLeave") {  };
            ApplyAction<object> id_0fe7d1810090435a9c91f9863d1cf588 = new ApplyAction<object>() { Lambda = input =>{(input as Border).BorderBrush = Brushes.Black;(input as Border).BorderThickness = new Thickness(1);} };
            ApplyAction<object> id_9d268820f98045bf83aa4d4a46ced56e = new ApplyAction<object>() { Lambda = input =>{(input as Border).Focus();} };
            RoutedEventSubscriber id_3cd406c4bb28481495a1b956c8331bf2 = new RoutedEventSubscriber(eventName: "GotFocus") {  };
            ApplyAction<object> id_7e01f7f292484e4b836697f18366e0d1 = new ApplyAction<object>() { Lambda = input =>{(input as Border).Background = Brushes.Aquamarine;} };
            RoutedEventSubscriber id_6e9e12d6e0b840d0a7c61fb22fbe2803 = new RoutedEventSubscriber(eventName: "LostFocus") {  };
            ApplyAction<object> id_8f1a87fae38e43498530b7726c450e4c = new ApplyAction<object>() { Lambda = input =>{(input as Border).Background = Brushes.LightSkyBlue;} };
            RoutedEventSubscriber id_1100e23abece4f3390f33404af113350 = new RoutedEventSubscriber(eventName: "GotFocus") {  };
            ApplyAction<object> id_13482d7dc9ea44ffabf66fad5e8ee8a9 = new ApplyAction<object>() { Lambda = input =>{(input as Border).Background = Brushes.LightSalmon;} };
            MouseButtonEvent id_612282012f4144b1ad40c364f0bbd174 = new MouseButtonEvent(eventName: "PreviewMouseLeftButtonDown") {  };
            RoutedEventSubscriber id_affb130c0a5e4c07ba0b64cb8602e140 = new RoutedEventSubscriber(eventName: "LostFocus") {  };
            ApplyAction<object> id_bdd8e9f62d804e0ebac8b0aa766e0319 = new ApplyAction<object>() { Lambda = input =>{(input as Border).Background = Brushes.White;} };
            MouseButtonEvent id_ce8eeadc058b4658863da3bea28953dd = new MouseButtonEvent(eventName: "MouseLeftButtonDown") {  };
            ApplyAction<object> id_8f04baf034f14543a26bddcec9438968 = new ApplyAction<object>() { Lambda = input =>{var ui = input as Border;if (!ui.IsKeyboardFocusWithin) (input as Border).Focus();} };
            EventHandlerConnector portHighlightingEventHandlers = new EventHandlerConnector() { InstanceName = "portHighlightingEventHandlers" };
            DynamicWiring<IEventHandler> id_9421b997281a4ed797e266b6ad276985 = new DynamicWiring<IEventHandler>() { SourcePort = "eventHandlers" };
            DynamicWiring<IUI> id_26779c3bd3c94643b0546a06f2585814 = new DynamicWiring<IUI>() { SourcePort = "children" };
            UIFactory id_0c608207acc542de82f1e1310c75e973 = new UIFactory(getUIContainer: () =>{return new Box() {Width = 50,Height = 20,Background = Brushes.White};}) {  };
            DynamicWiring<IUI> id_8d2ab8ac6bb74e6791220180e8f3d613 = new DynamicWiring<IUI>() { SourcePort = "uiLayout" };
            DynamicWiring<IEventHandler> id_40307a4f7b10484bb67bbf7895672c9c = new DynamicWiring<IEventHandler>() { SourcePort = "eventHandlers" };
            UIFactory id_9550a9301dbc480ea5c913a5b60dd206 = new UIFactory(getUIContainer: () =>{return new Text(text: "output") {HorizAlignment = HorizontalAlignment.Center};}) {  };
            UIFactory id_a93f21b66d2d46d68dcf4e54834818dc = new UIFactory(getUIContainer: () =>{return new Text(text: "input") {HorizAlignment = HorizontalAlignment.Center};}) {  };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            rootUI.WireTo(nodeContainer, "children");
            id_85122e00757a4f998479737018f29c83.WireTo(inputPortsVert, "children");
            id_85122e00757a4f998479737018f29c83.WireTo(id_90c1a73332b34846bfd583fce7148f07, "children");
            id_0b538d4dc0834a8ea0f2a342d3beb071.WireTo(id_c46c654c8a1f48d8aaf945371cec7e07, "children");
            id_7d2439234d2f465e8af6805f74e9ce01.WireTo(id_effeeb8bb70c4e3083601f63a0ae830f, "uiLayout");
            id_7d2439234d2f465e8af6805f74e9ce01.WireTo(portHighlightingEventHandlers, "eventHandlers");
            nodeContainer.WireTo(id_9ee3f78a9f5a4533a65f1e9a49e2eb31, "uiLayout");
            nodeContainer.WireTo(id_3cd406c4bb28481495a1b956c8331bf2, "eventHandlers");
            nodeContainer.WireTo(id_6e9e12d6e0b840d0a7c61fb22fbe2803, "eventHandlers");
            nodeContainer.WireTo(id_ce8eeadc058b4658863da3bea28953dd, "eventHandlers");
            id_9ee3f78a9f5a4533a65f1e9a49e2eb31.WireTo(id_85122e00757a4f998479737018f29c83, "children");
            id_9ee3f78a9f5a4533a65f1e9a49e2eb31.WireTo(id_0b538d4dc0834a8ea0f2a342d3beb071, "children");
            id_9ee3f78a9f5a4533a65f1e9a49e2eb31.WireTo(id_a06ac9f154124b5b8b102e3a7d80ec94, "children");
            id_c46c654c8a1f48d8aaf945371cec7e07.WireTo(id_7262ab9707f34185927901b7281adefc, "children");
            id_c46c654c8a1f48d8aaf945371cec7e07.WireTo(id_031bfa7681574f48adbf8494b9148e66, "children");
            inputPortsVert.WireTo(id_7d2439234d2f465e8af6805f74e9ce01, "children");
            id_90c1a73332b34846bfd583fce7148f07.WireTo(addNewInputPortButton, "children");
            addNewInputPortButton.WireTo(id_edbf30d1617b451fb98820eb851333a5, "eventButtonClicked");
            id_edbf30d1617b451fb98820eb851333a5.WireTo(id_26779c3bd3c94643b0546a06f2585814, "dataOutput");
            id_0434585e578c431eb28d8cfd58b06556.WireTo(id_fddf0cac82ce4325a8d72d7b02a5caea, "uiInstanceOutput");
            id_fddf0cac82ce4325a8d72d7b02a5caea.WireTo(id_9421b997281a4ed797e266b6ad276985, "objectOutput");
            id_fddf0cac82ce4325a8d72d7b02a5caea.WireTo(id_a93f21b66d2d46d68dcf4e54834818dc, "wire");
            id_a06ac9f154124b5b8b102e3a7d80ec94.WireTo(outputPortsVert, "children");
            id_a06ac9f154124b5b8b102e3a7d80ec94.WireTo(id_bd61a7d561874147956ace9e882f3496, "children");
            outputPortsVert.WireTo(id_4daf95d474d640e3b1bc1e405dc3caa4, "children");
            id_4daf95d474d640e3b1bc1e405dc3caa4.WireTo(id_8cd0a39b90f34145819a2b7053961316, "uiLayout");
            id_4daf95d474d640e3b1bc1e405dc3caa4.WireTo(portHighlightingEventHandlers, "eventHandlers");
            id_bd61a7d561874147956ace9e882f3496.WireTo(addNewOutputPortButton, "children");
            addNewOutputPortButton.WireTo(id_57d00540867d45068276fee80801deb7, "eventButtonClicked");
            id_57d00540867d45068276fee80801deb7.WireTo(id_55f877f64bfd402ebcd48349b4152770, "dataOutput");
            id_55f877f64bfd402ebcd48349b4152770.WireTo(id_2bb70c4f9d5944919dbe496b0b27348c, "objectOutput");
            id_55f877f64bfd402ebcd48349b4152770.WireTo(id_0c608207acc542de82f1e1310c75e973, "wire");
            id_58d14a1a773648b0b765af74a0a45069.WireTo(id_54fc5ddc095c4b49b76a61590ef2bb03, "senderOutput");
            id_3467ce1809fa42f18677db7192dd68e6.WireTo(id_0fe7d1810090435a9c91f9863d1cf588, "senderOutput");
            id_3cd406c4bb28481495a1b956c8331bf2.WireTo(id_7e01f7f292484e4b836697f18366e0d1, "senderOutput");
            id_6e9e12d6e0b840d0a7c61fb22fbe2803.WireTo(id_8f1a87fae38e43498530b7726c450e4c, "senderOutput");
            id_1100e23abece4f3390f33404af113350.WireTo(id_13482d7dc9ea44ffabf66fad5e8ee8a9, "senderOutput");
            id_612282012f4144b1ad40c364f0bbd174.WireTo(id_9d268820f98045bf83aa4d4a46ced56e, "senderOutput");
            id_affb130c0a5e4c07ba0b64cb8602e140.WireTo(id_bdd8e9f62d804e0ebac8b0aa766e0319, "senderOutput");
            id_ce8eeadc058b4658863da3bea28953dd.WireTo(id_8f04baf034f14543a26bddcec9438968, "senderOutput");
            id_9421b997281a4ed797e266b6ad276985.WireTo(portHighlightingEventHandlers, "wire");
            portHighlightingEventHandlers.WireTo(id_58d14a1a773648b0b765af74a0a45069, "propagatedHandlerFanoutList");
            portHighlightingEventHandlers.WireTo(id_3467ce1809fa42f18677db7192dd68e6, "propagatedHandlerFanoutList");
            portHighlightingEventHandlers.WireTo(id_612282012f4144b1ad40c364f0bbd174, "propagatedHandlerFanoutList");
            portHighlightingEventHandlers.WireTo(id_1100e23abece4f3390f33404af113350, "propagatedHandlerFanoutList");
            portHighlightingEventHandlers.WireTo(id_affb130c0a5e4c07ba0b64cb8602e140, "propagatedHandlerFanoutList");
            id_26779c3bd3c94643b0546a06f2585814.WireTo(id_7413e1377c6940059dce785fa75396c9, "objectOutput");
            id_26779c3bd3c94643b0546a06f2585814.WireTo(id_0434585e578c431eb28d8cfd58b06556, "wire");
            id_0c608207acc542de82f1e1310c75e973.WireTo(id_8d2ab8ac6bb74e6791220180e8f3d613, "uiInstanceOutput");
            id_8d2ab8ac6bb74e6791220180e8f3d613.WireTo(id_40307a4f7b10484bb67bbf7895672c9c, "objectOutput");
            id_8d2ab8ac6bb74e6791220180e8f3d613.WireTo(id_9550a9301dbc480ea5c913a5b60dd206, "wire");
            id_40307a4f7b10484bb67bbf7895672c9c.WireTo(portHighlightingEventHandlers, "wire");
            // END AUTO-GENERATED WIRING

            
            return (rootUI as IUI).GetWPFElement();
        }

        // Methods

        public PortGraphNodeUI()
        {
            
        }
    }
}




















