using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;
using Button = DomainAbstractions.Button;
using TextBox = DomainAbstractions.TextBox;

namespace RequirementsAbstractions
{
    public class ALANode
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string Type { get; set; } = "?";
        public string Name { get; set; } = "";
        public List<string> AvailableProgrammingParadigms { get; } = new List<string>();
        public List<string> AvailableDomainAbstractions { get; } = new List<string>();
        public List<string> AvailableRequirementsAbstractions { get; } = new List<string>();
        public Graph Graph { get; set; }
        public Canvas Canvas { get; set; }
        public StateTransition<Enums.DiagramMode> StateTransition { get; set; }
        public UIElement Render { get; set; }
        public AbstractionModel Model { get; set; }
        public List<Box> PortBoxes { get; } = new List<Box>();

        // Private fields
        private Box rootUI;
        private Box _selectedPort;
        private Point _mousePosInBox = new Point(0, 0);

        // Ports

        // Methods

        /// <summary>
        /// Get the currently selected port, and if none are selected, then return a default port.
        /// </summary>
        /// <param name="inputPort"></param>
        /// <returns></returns>
        public Box GetSelectedPort(bool inputPort = false) => _selectedPort ?? PortBoxes.FirstOrDefault(box => box.Payload is Port port && port.IsInputPort == inputPort);

        private void SetWiring()
        {
            rootUI = new Box()
            {
                Background = Brushes.LightSkyBlue
            };

            Model = CreateDummyAbstractionModel();

            // BEGIN AUTO-GENERATED INSTANTIATIONS
            Horizontal id_cfd009025d3c4a5ba5502555888737ca = new Horizontal() {  };
            Vertical nodeMiddle = new Vertical() { InstanceName = "nodeMiddle", Margin = new Thickness(1, 0, 1, 0) };
            Horizontal nodeIdRow = new Horizontal() { InstanceName = "nodeIdRow" };
            DropDownMenu nodeTypeDropDownMenu = new DropDownMenu() { InstanceName = "nodeTypeDropDownMenu", Text = Model.GetValue("Type"), Width = 100 };
            TextBox nodeNameTextBox = new TextBox() { InstanceName = "nodeNameTextBox", Text = Model.GetValue("Name"), Width = 50 };
            UIFactory id_e0554d50c14d461fbda09660bb0db692 = new UIFactory(getUIContainer: () =>{var inputPortsVert = new Vertical();inputPortsVert.Margin = new Thickness(0);return inputPortsVert;}) {  };
            DataFlowConnector<object> inputPortsVertConnector = new DataFlowConnector<object>() { InstanceName = "inputPortsVertConnector" };
            ConvertToEvent<object> id_507e6f01533144aa933aa90ea7554564 = new ConvertToEvent<object>() {  };
            EventConnector id_c548ed7d25784d71898c8b7b560e7a4e = new EventConnector() {  };
            Data<object> id_2e98ef4f97c742189b487d56e68fe6be = new Data<object>() { Lambda = Model.GetImplementedPorts };
            Cast<object, IEnumerable<KeyValuePair<string, string>>> id_95179f1a83e74747bfa6eb03759ff51f = new Cast<object, IEnumerable<KeyValuePair<string, string>>>() {  };
            ForEach<KeyValuePair<string, string>> id_e6eb08636e354ba8b3ac34bfd93d3d03 = new ForEach<KeyValuePair<string, string>>() {  };
            Apply<KeyValuePair<string, string>, object> id_123d8dde7f23467d9a04c218fda47d9d = new Apply<KeyValuePair<string, string>, object>() { Lambda = input =>{var port = new Port();port.Type = input.Value;port.Name = input.Key;port.IsInputPort = true;return port;} };
            Apply<object, object> setUpInputPortBox = new Apply<object, object>() { InstanceName = "setUpInputPortBox", Lambda = input =>{var box = new Box();box.Payload = input;box.Width = 50;box.Height = 15;box.Background = Brushes.White;box.BorderThickness = new Thickness(2);var toolTipLabel = new System.Windows.Controls.Label() { Content = (input as Port).FullName };box.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = (input as Port).ToString();var text = new Text(text: (input as Port).Name);text.HorizAlignment = HorizontalAlignment.Center;box.WireTo(text, "uiLayout");inputPortsVertConnector.Data.WireTo(box, "children");PortBoxes.Add(box);return box;} };
            UIFactory id_5e708ba270764edc895762863bd98c11 = new UIFactory(getUIContainer: () =>{var outputPortsVert = new Vertical();outputPortsVert.Margin = new Thickness(0);return outputPortsVert;}) {  };
            DataFlowConnector<object> outputPortsVertConnector = new DataFlowConnector<object>() { InstanceName = "outputPortsVertConnector" };
            ConvertToEvent<object> id_445c991b9f5b434ea6783c7c0b0bdb56 = new ConvertToEvent<object>() {  };
            EventConnector id_9c9fe997b9224bcb9c4c2a4c5222a075 = new EventConnector() {  };
            Data<object> id_6cadcd995c3e412a8a4c49d8ca4f126b = new Data<object>() { Lambda = Model.GetAcceptedPorts };
            Cast<object, IEnumerable<KeyValuePair<string, string>>> id_031bd8cb7ecb40a4884c1e689004825c = new Cast<object, IEnumerable<KeyValuePair<string, string>>>() {  };
            ForEach<KeyValuePair<string, string>> id_e1ad74a0bde740f58fc3d5728625ec19 = new ForEach<KeyValuePair<string, string>>() {  };
            Apply<KeyValuePair<string, string>, object> id_b95232c9615041b98fe80900c05f891f = new Apply<KeyValuePair<string, string>, object>() { Lambda = input =>{var port = new Port();port.Type = input.Value;port.Name = input.Key;port.IsInputPort = false;return port;} };
            Apply<object, object> setUpOutputPortBox = new Apply<object, object>() { InstanceName = "setUpOutputPortBox", Lambda = input =>{var box = new Box();box.Payload = input;box.Width = 50;box.Height = 15;box.Background = Brushes.White;box.BorderThickness = new Thickness(2);var toolTipLabel = new System.Windows.Controls.Label() { Content = (input as Port).FullName };box.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = (input as Port).ToString();var text = new Text(text: (input as Port).Name);text.HorizAlignment = HorizontalAlignment.Center;box.WireTo(text);outputPortsVertConnector.Data.WireTo(box, "children");PortBoxes.Add(box);return box;} };
            ApplyAction<object> id_f8fcf2484d6d40e698004d46d605e7f3 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = Brushes.Red;} };
            DataFlowConnector<object> id_e0c5b3cd267547489dbdd7a7b622c4bf = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_a13cec3fe7c646cd9e73578d65bc35ae = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = Brushes.Black;} };
            ApplyAction<object> id_9d04c86792be4467a565fbf2a4df464c = new ApplyAction<object>() { Lambda = input =>{(input as Box).InitialiseUI();} };
            Apply<object, object> id_6149fb183ef9489d9d7ec4708fbba3c7 = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseEnter");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_532035b652f540e99f63fb74dc63ff4b = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            Apply<object, object> id_3aa8c72f4580438c8d20a325fda0884b = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseLeave");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_91ba189cef3a41289fc1dc3b8fd3ef76 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            DataFlowConnector<object> id_effed114dd484301845f5386cdf552d9 = new DataFlowConnector<object>() {  };
            DataFlowConnector<object> id_de2733ae28c74dad803c80cc86e6096b = new DataFlowConnector<object>() {  };
            Apply<object, object> id_e7a47a827d6e49049fc945b46fd5270b = new Apply<object, object>() { Lambda = input =>{var mouseButtonEvent = new MouseButtonEvent("MouseDown");mouseButtonEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseButtonEvent, "eventHandlers");return mouseButtonEvent;} };
            DynamicWiring<IDataFlow<object>> id_846b237418f247ce8efd2899c376a4b0 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_ed2bbd920cb2435988d94a94b3fd6771 = new ApplyAction<object>() { Lambda = input =>{_selectedPort = input as Box;_selectedPort.Render.Focus();} };
            Apply<object, object> id_1857cbed1daf4db0a93f8fedab46c7e5 = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("GotFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_4d12d68a00164a1da635c430c0e8dd2e = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_1f5c5c0f7d574ca6be0c9e170769945a = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = Brushes.SandyBrown;} };
            Apply<object, object> id_ee0b3cc384d941e699c217331845e171 = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("LostFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_e3280408c5174f09acf6ae4fbf113a99 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_1cb36043a99d4c05b92b8cb122bef0e5 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = Brushes.White;} };
            MouseEvent id_b6c6b65bd111472f91e1d8d740241cc2 = new MouseEvent(eventName: "MouseEnter") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_f09f4adf0a764012a34d94b937be7a1d = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.Aquamarine;} };
            MouseEvent id_d4e06d4737ac41ab9b53cb6ba0f66c17 = new MouseEvent(eventName: "MouseLeave") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_3fb3e0e4866c4dd6a5a423ffd7222a83 = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) (input as Box).Background = Brushes.LightSkyBlue;} };
            RoutedEventSubscriber id_83fb3200fa4141598b3ff648cb868050 = new RoutedEventSubscriber(eventName: "GotFocus") { ExtractSender = source => (source as Box).Render };
            RoutedEventSubscriber id_478430779e3f473f8cf57e93ddc3ac85 = new RoutedEventSubscriber(eventName: "LostFocus") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_7c91b8780a924c8ebb0ffe651b61aaae = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.LightSkyBlue;} };
            MouseButtonEvent id_b6ea1b9e969d4e9e95fb6d1004a58605 = new MouseButtonEvent(eventName: "MouseLeftButtonDown") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_418c264545584dad9fa6ab8ea82fba83 = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) ui.Focus();} };
            ApplyAction<object> id_43d0e7bc471b4049a19b3c2c0fc8446d = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.Aquamarine;} };
            DataFlowConnector<object> id_94e342a0e43349b3883c929f1ea62b6e = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_91797579e3f04697bbee50e683886079 = new ApplyAction<object>() { Lambda = input =>{StateTransition.Update(Enums.DiagramMode.IdleSelected);} };
            MouseEvent id_f607d7345dca4e79b0bf7e90df377a1d = new MouseEvent(eventName: "MouseMove") { ExtractSender = source => (source as Box).Render, Condition = args => Mouse.LeftButton == MouseButtonState.Pressed };
            ApplyAction<object> id_5461e4310ca7476a88a37b24609cd87b = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;var mousePos = Mouse.GetPosition(Canvas);Canvas.SetLeft(render, mousePos.X - _mousePosInBox.X);Canvas.SetTop(render, mousePos.Y - _mousePosInBox.Y);} };
            MouseButtonEvent id_4e494055c31d4b7b8f47912b28a66120 = new MouseButtonEvent(eventName: "MouseLeftButtonUp") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_6d2c9620dfa54db68824a36c44341ff7 = new ApplyAction<object>() { Lambda = input =>{if (Mouse.Captured == (input as Box).Render) Mouse.Capture(null);} };
            ApplyAction<object> id_f4a370a862d24e8386687d13fea9a9a4 = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;_mousePosInBox = Mouse.GetPosition(render);Mouse.Capture(render);} };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            rootUI.WireTo(id_cfd009025d3c4a5ba5502555888737ca, "uiLayout");
            rootUI.WireTo(id_b6c6b65bd111472f91e1d8d740241cc2, "eventHandlers");
            rootUI.WireTo(id_d4e06d4737ac41ab9b53cb6ba0f66c17, "eventHandlers");
            rootUI.WireTo(id_83fb3200fa4141598b3ff648cb868050, "eventHandlers");
            rootUI.WireTo(id_478430779e3f473f8cf57e93ddc3ac85, "eventHandlers");
            rootUI.WireTo(id_b6ea1b9e969d4e9e95fb6d1004a58605, "eventHandlers");
            rootUI.WireTo(id_f607d7345dca4e79b0bf7e90df377a1d, "eventHandlers");
            rootUI.WireTo(id_4e494055c31d4b7b8f47912b28a66120, "eventHandlers");
            id_cfd009025d3c4a5ba5502555888737ca.WireTo(id_e0554d50c14d461fbda09660bb0db692, "children");
            id_cfd009025d3c4a5ba5502555888737ca.WireTo(nodeMiddle, "children");
            id_cfd009025d3c4a5ba5502555888737ca.WireTo(id_5e708ba270764edc895762863bd98c11, "children");
            nodeMiddle.WireTo(nodeIdRow, "children");
            nodeIdRow.WireTo(nodeTypeDropDownMenu, "children");
            nodeIdRow.WireTo(nodeNameTextBox, "children");
            id_e0554d50c14d461fbda09660bb0db692.WireTo(inputPortsVertConnector, "uiInstanceOutput");
            inputPortsVertConnector.WireTo(id_507e6f01533144aa933aa90ea7554564, "fanoutList");
            id_507e6f01533144aa933aa90ea7554564.WireTo(id_c548ed7d25784d71898c8b7b560e7a4e, "eventOutput");
            id_c548ed7d25784d71898c8b7b560e7a4e.WireTo(id_2e98ef4f97c742189b487d56e68fe6be, "fanoutList");
            id_2e98ef4f97c742189b487d56e68fe6be.WireTo(id_95179f1a83e74747bfa6eb03759ff51f, "dataOutput");
            id_95179f1a83e74747bfa6eb03759ff51f.WireTo(id_e6eb08636e354ba8b3ac34bfd93d3d03, "output");
            id_e6eb08636e354ba8b3ac34bfd93d3d03.WireTo(id_123d8dde7f23467d9a04c218fda47d9d, "elementOutput");
            id_123d8dde7f23467d9a04c218fda47d9d.WireTo(setUpInputPortBox, "output");
            setUpInputPortBox.WireTo(id_e0c5b3cd267547489dbdd7a7b622c4bf, "output");
            id_5e708ba270764edc895762863bd98c11.WireTo(outputPortsVertConnector, "uiInstanceOutput");
            outputPortsVertConnector.WireTo(id_445c991b9f5b434ea6783c7c0b0bdb56, "fanoutList");
            id_445c991b9f5b434ea6783c7c0b0bdb56.WireTo(id_9c9fe997b9224bcb9c4c2a4c5222a075, "eventOutput");
            id_9c9fe997b9224bcb9c4c2a4c5222a075.WireTo(id_6cadcd995c3e412a8a4c49d8ca4f126b, "fanoutList");
            id_6cadcd995c3e412a8a4c49d8ca4f126b.WireTo(id_031bd8cb7ecb40a4884c1e689004825c, "dataOutput");
            id_031bd8cb7ecb40a4884c1e689004825c.WireTo(id_e1ad74a0bde740f58fc3d5728625ec19, "output");
            id_e1ad74a0bde740f58fc3d5728625ec19.WireTo(id_b95232c9615041b98fe80900c05f891f, "elementOutput");
            id_b95232c9615041b98fe80900c05f891f.WireTo(setUpOutputPortBox, "output");
            setUpOutputPortBox.WireTo(id_e0c5b3cd267547489dbdd7a7b622c4bf, "output");
            id_e0c5b3cd267547489dbdd7a7b622c4bf.WireTo(id_effed114dd484301845f5386cdf552d9, "fanoutList");
            id_e0c5b3cd267547489dbdd7a7b622c4bf.WireTo(id_de2733ae28c74dad803c80cc86e6096b, "fanoutList");
            id_6149fb183ef9489d9d7ec4708fbba3c7.WireTo(id_532035b652f540e99f63fb74dc63ff4b, "output");
            id_532035b652f540e99f63fb74dc63ff4b.WireTo(id_f8fcf2484d6d40e698004d46d605e7f3, "wire");
            id_3aa8c72f4580438c8d20a325fda0884b.WireTo(id_91ba189cef3a41289fc1dc3b8fd3ef76, "output");
            id_91ba189cef3a41289fc1dc3b8fd3ef76.WireTo(id_a13cec3fe7c646cd9e73578d65bc35ae, "wire");
            id_effed114dd484301845f5386cdf552d9.WireTo(id_6149fb183ef9489d9d7ec4708fbba3c7, "fanoutList");
            id_effed114dd484301845f5386cdf552d9.WireTo(id_3aa8c72f4580438c8d20a325fda0884b, "fanoutList");
            id_effed114dd484301845f5386cdf552d9.WireTo(id_e7a47a827d6e49049fc945b46fd5270b, "fanoutList");
            id_effed114dd484301845f5386cdf552d9.WireTo(id_1857cbed1daf4db0a93f8fedab46c7e5, "fanoutList");
            id_effed114dd484301845f5386cdf552d9.WireTo(id_ee0b3cc384d941e699c217331845e171, "fanoutList");
            id_de2733ae28c74dad803c80cc86e6096b.WireTo(id_9d04c86792be4467a565fbf2a4df464c, "fanoutList");
            id_e7a47a827d6e49049fc945b46fd5270b.WireTo(id_846b237418f247ce8efd2899c376a4b0, "output");
            id_846b237418f247ce8efd2899c376a4b0.WireTo(id_ed2bbd920cb2435988d94a94b3fd6771, "wire");
            id_1857cbed1daf4db0a93f8fedab46c7e5.WireTo(id_4d12d68a00164a1da635c430c0e8dd2e, "output");
            id_4d12d68a00164a1da635c430c0e8dd2e.WireTo(id_1f5c5c0f7d574ca6be0c9e170769945a, "wire");
            id_ee0b3cc384d941e699c217331845e171.WireTo(id_e3280408c5174f09acf6ae4fbf113a99, "output");
            id_e3280408c5174f09acf6ae4fbf113a99.WireTo(id_1cb36043a99d4c05b92b8cb122bef0e5, "wire");
            id_b6c6b65bd111472f91e1d8d740241cc2.WireTo(id_f09f4adf0a764012a34d94b937be7a1d, "sourceOutput");
            id_d4e06d4737ac41ab9b53cb6ba0f66c17.WireTo(id_3fb3e0e4866c4dd6a5a423ffd7222a83, "sourceOutput");
            id_83fb3200fa4141598b3ff648cb868050.WireTo(id_43d0e7bc471b4049a19b3c2c0fc8446d, "sourceOutput");
            id_478430779e3f473f8cf57e93ddc3ac85.WireTo(id_7c91b8780a924c8ebb0ffe651b61aaae, "sourceOutput");
            id_b6ea1b9e969d4e9e95fb6d1004a58605.WireTo(id_94e342a0e43349b3883c929f1ea62b6e, "sourceOutput");
            id_94e342a0e43349b3883c929f1ea62b6e.WireTo(id_418c264545584dad9fa6ab8ea82fba83, "fanoutList");
            id_94e342a0e43349b3883c929f1ea62b6e.WireTo(id_91797579e3f04697bbee50e683886079, "fanoutList");
            id_94e342a0e43349b3883c929f1ea62b6e.WireTo(id_f4a370a862d24e8386687d13fea9a9a4, "fanoutList");
            id_f607d7345dca4e79b0bf7e90df377a1d.WireTo(id_5461e4310ca7476a88a37b24609cd87b, "sourceOutput");
            id_4e494055c31d4b7b8f47912b28a66120.WireTo(id_6d2c9620dfa54db68824a36c44341ff7, "sourceOutput");
            // END AUTO-GENERATED WIRING

            Render = (rootUI as IUI).GetWPFElement();
        }

        private AbstractionModel CreateDummyAbstractionModel()
        {
            var model = new AbstractionModel();
            model.AddImplementedPort("Port", "input");
            model.AddAcceptedPort("Port", "output");
            model.AddProperty("Type", "NewNode");
            model.AddProperty("Name", "");

            return model;
        }

        public void CreateInternals()
        {
            SetWiring();
        }

        public ALANode()
        {

        }
    }
}






































































































































































































