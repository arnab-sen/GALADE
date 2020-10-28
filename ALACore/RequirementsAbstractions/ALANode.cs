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
        public Box _selectedPort;

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
            rootUI = new Box() {Background = Brushes.LightSkyBlue};
            Model = CreateDummyAbstractionModel();

            // BEGIN AUTO-GENERATED INSTANTIATIONS
            Horizontal id_c6d7bc99b67946119489ba743d4ed05a = new Horizontal() {  };
            Vertical nodeMiddle = new Vertical() { InstanceName = "nodeMiddle", Margin = new Thickness(1, 0, 1, 0) };
            Horizontal nodeIdRow = new Horizontal() { InstanceName = "nodeIdRow" };
            DropDownMenu nodeTypeDropDownMenu = new DropDownMenu() { InstanceName = "nodeTypeDropDownMenu", Text = Model.GetValue("Type"), Width = 100 };
            TextBox nodeNameTextBox = new TextBox() { InstanceName = "nodeNameTextBox", Text = Model.GetValue("Name"), Width = 50 };
            UIFactory id_04ddd06f25e44929a11c4ad490490854 = new UIFactory(getUIContainer: () =>{var inputPortsVert = new Vertical();inputPortsVert.Margin = new Thickness(0);return inputPortsVert;}) {  };
            DataFlowConnector<object> inputPortsVertConnector = new DataFlowConnector<object>() { InstanceName = "inputPortsVertConnector" };
            ConvertToEvent<object> id_08488a212e2e4634b526d34430eba3b1 = new ConvertToEvent<object>() {  };
            EventConnector id_8dbd3d66e76e4bf09c324ba942a3732b = new EventConnector() {  };
            Data<object> id_6b1dfab85fa541f7b763c42b3c976b62 = new Data<object>() { Lambda = Model.GetImplementedPorts };
            Cast<object, IEnumerable<KeyValuePair<string, string>>> id_3bc852b4f8f247f2b4128253d59f784e = new Cast<object, IEnumerable<KeyValuePair<string, string>>>() {  };
            ForEach<KeyValuePair<string, string>> id_fa419c3d5cb24389baa5fae8a551212b = new ForEach<KeyValuePair<string, string>>() {  };
            Apply<KeyValuePair<string, string>, object> id_9de2da1fd0104628b7aafcf49ef7d14c = new Apply<KeyValuePair<string, string>, object>() { Lambda = input =>{var port = new Port();port.Type = input.Value;port.Name = input.Key;port.IsInputPort = true;return port;} };
            Apply<object, object> setUpInputPortBox = new Apply<object, object>() { InstanceName = "setUpInputPortBox", Lambda = input =>{var box = new Box();box.Payload = input;box.Width = 50;box.Height = 15;box.Background = Brushes.White;box.BorderThickness = new Thickness(2);var toolTipLabel = new System.Windows.Controls.Label() { Content = (input as Port).FullName };box.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = (input as Port).ToString();var text = new Text(text: (input as Port).Name);text.HorizAlignment = HorizontalAlignment.Center;box.WireTo(text, "uiLayout");inputPortsVertConnector.Data.WireTo(box, "children");PortBoxes.Add(box);return box;} };
            UIFactory id_ef3f94de376a4fdfb22ed5239e3c52ab = new UIFactory(getUIContainer: () =>{var outputPortsVert = new Vertical();outputPortsVert.Margin = new Thickness(0);return outputPortsVert;}) {  };
            DataFlowConnector<object> outputPortsVertConnector = new DataFlowConnector<object>() { InstanceName = "outputPortsVertConnector" };
            ConvertToEvent<object> id_706fb2c950f546b0854110a77743e96c = new ConvertToEvent<object>() {  };
            EventConnector id_0a71e21ff29542c1972d4a3b8854c0cc = new EventConnector() {  };
            Data<object> id_fb691dc5653a46c4b7e99228c03d1392 = new Data<object>() { Lambda = Model.GetAcceptedPorts };
            Cast<object, IEnumerable<KeyValuePair<string, string>>> id_00a83490dcd446c09c9664592e1a9a8b = new Cast<object, IEnumerable<KeyValuePair<string, string>>>() {  };
            ForEach<KeyValuePair<string, string>> id_02f65966ccc2493e8c5eec96f04423e6 = new ForEach<KeyValuePair<string, string>>() {  };
            Apply<KeyValuePair<string, string>, object> id_c445630649fb44e5a3062dc584bdc413 = new Apply<KeyValuePair<string, string>, object>() { Lambda = input =>{var port = new Port();port.Type = input.Value;port.Name = input.Key;port.IsInputPort = false;return port;} };
            Apply<object, object> setUpOutputPortBox = new Apply<object, object>() { InstanceName = "setUpOutputPortBox", Lambda = input =>{var box = new Box();box.Payload = input;box.Width = 50;box.Height = 15;box.Background = Brushes.White;box.BorderThickness = new Thickness(2);var toolTipLabel = new System.Windows.Controls.Label() { Content = (input as Port).FullName };box.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = (input as Port).ToString();var text = new Text(text: (input as Port).Name);text.HorizAlignment = HorizontalAlignment.Center;box.WireTo(text);outputPortsVertConnector.Data.WireTo(box, "children");box.InitialiseUI();return box;} };
            ApplyAction<object> id_ea575d3eb22a4509927f9c4fcc4f7afe = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = Brushes.Red;} };
            DataFlowConnector<object> id_ffa73bfc60944c37af7fe0af8283b723 = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_d50b852d6da44d759a1398a318c2503e = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = Brushes.Black;} };
            ApplyAction<object> id_87b0b5c2e4b74d58a40ccb85a99dc8d5 = new ApplyAction<object>() { Lambda = input =>{(input as Box).InitialiseUI();} };
            Apply<object, object> id_ed3387f56f084528aba36ee9b68cd3be = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseEnter");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_3ff612b9138d4e07b02902e674b5c7ca = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            Apply<object, object> id_7276aef02fc849209deeaae68feeec4c = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseLeave");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_4a50a8626644471294391a5883bcbd58 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            DataFlowConnector<object> id_5a76a8c97965401d8cda6ab631b62bab = new DataFlowConnector<object>() {  };
            DataFlowConnector<object> id_513f15527555405eb69c2533a0752a05 = new DataFlowConnector<object>() {  };
            Apply<object, object> id_d2d6815df32640c084eba1d9922d9ad2 = new Apply<object, object>() { Lambda = input =>{var mouseButtonEvent = new MouseButtonEvent("MouseDown");mouseButtonEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseButtonEvent, "eventHandlers");return mouseButtonEvent;} };
            DynamicWiring<IDataFlow<object>> id_d7268693aeac4986893c4b82bc8e1ec7 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_f513c2cb2b504e96b468db6510cb1b17 = new ApplyAction<object>() { Lambda = input =>{_selectedPort = input as Box;_selectedPort.Render.Focus();} };
            Apply<object, object> id_28576fd62a1c440895e21babf1b6d197 = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("GotFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_7f8cedfb03334c08bd7b8ecf1e212507 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_5b42b9268926427b86d80e25ecb4051f = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = Brushes.SandyBrown;} };
            Apply<object, object> id_8566ea315aab45479b96c771e9ac7ea3 = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("LostFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_60fe1b7e282f464ca5b41872303014f1 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_fb5b880f26c64386a839df3f6936fa58 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = Brushes.White;} };
            MouseEvent id_5fcddc3452124e5db3810e53c3b5d02a = new MouseEvent(eventName: "MouseEnter") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_4b2f645c40dd42ae9d2a85df5b7f3e82 = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.Aquamarine;} };
            MouseEvent id_ec3daa1c4f404fd38fc6d7a3c3981490 = new MouseEvent(eventName: "MouseLeave") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_784ba6702c3a4957a18a20af7cb5f15f = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) (input as Box).Background = Brushes.LightSkyBlue;} };
            RoutedEventSubscriber id_9b80d54da16e431ba858bd18bd4d24bc = new RoutedEventSubscriber(eventName: "GotFocus") { ExtractSender = source => (source as Box).Render };
            RoutedEventSubscriber id_1fd3679c0ec642309cc8203a38289546 = new RoutedEventSubscriber(eventName: "LostFocus") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_04edd718078649ce83d0fc4d7517dc9a = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.LightSkyBlue;} };
            MouseButtonEvent id_a023c030388c45748e020d1f6416c3ae = new MouseButtonEvent(eventName: "MouseLeftButtonDown") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_12345fa0d4d84efa988d910e6deefa47 = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) ui.Focus();} };
            ApplyAction<object> id_b7538f3fa4694ac5a117cddba56250e0 = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.Aquamarine;} };
            DataFlowConnector<object> id_8609e74b85b34449a65ae100c24bfdf9 = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_1362639f68f6426f9ee6ec3302490e06 = new ApplyAction<object>() { Lambda = input =>{StateTransition.Update(Enums.DiagramMode.IdleSelected);} };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            rootUI.WireTo(id_c6d7bc99b67946119489ba743d4ed05a, "uiLayout");
            rootUI.WireTo(id_5fcddc3452124e5db3810e53c3b5d02a, "eventHandlers");
            rootUI.WireTo(id_ec3daa1c4f404fd38fc6d7a3c3981490, "eventHandlers");
            rootUI.WireTo(id_9b80d54da16e431ba858bd18bd4d24bc, "eventHandlers");
            rootUI.WireTo(id_1fd3679c0ec642309cc8203a38289546, "eventHandlers");
            rootUI.WireTo(id_a023c030388c45748e020d1f6416c3ae, "eventHandlers");
            id_c6d7bc99b67946119489ba743d4ed05a.WireTo(id_04ddd06f25e44929a11c4ad490490854, "children");
            id_c6d7bc99b67946119489ba743d4ed05a.WireTo(nodeMiddle, "children");
            id_c6d7bc99b67946119489ba743d4ed05a.WireTo(id_ef3f94de376a4fdfb22ed5239e3c52ab, "children");
            nodeMiddle.WireTo(nodeIdRow, "children");
            nodeIdRow.WireTo(nodeTypeDropDownMenu, "children");
            nodeIdRow.WireTo(nodeNameTextBox, "children");
            id_04ddd06f25e44929a11c4ad490490854.WireTo(inputPortsVertConnector, "uiInstanceOutput");
            inputPortsVertConnector.WireTo(id_08488a212e2e4634b526d34430eba3b1, "fanoutList");
            id_08488a212e2e4634b526d34430eba3b1.WireTo(id_8dbd3d66e76e4bf09c324ba942a3732b, "eventOutput");
            id_8dbd3d66e76e4bf09c324ba942a3732b.WireTo(id_6b1dfab85fa541f7b763c42b3c976b62, "fanoutList");
            id_6b1dfab85fa541f7b763c42b3c976b62.WireTo(id_3bc852b4f8f247f2b4128253d59f784e, "dataOutput");
            id_3bc852b4f8f247f2b4128253d59f784e.WireTo(id_fa419c3d5cb24389baa5fae8a551212b, "output");
            id_fa419c3d5cb24389baa5fae8a551212b.WireTo(id_9de2da1fd0104628b7aafcf49ef7d14c, "elementOutput");
            id_9de2da1fd0104628b7aafcf49ef7d14c.WireTo(setUpInputPortBox, "output");
            setUpInputPortBox.WireTo(id_ffa73bfc60944c37af7fe0af8283b723, "output");
            id_ef3f94de376a4fdfb22ed5239e3c52ab.WireTo(outputPortsVertConnector, "uiInstanceOutput");
            outputPortsVertConnector.WireTo(id_706fb2c950f546b0854110a77743e96c, "fanoutList");
            id_706fb2c950f546b0854110a77743e96c.WireTo(id_0a71e21ff29542c1972d4a3b8854c0cc, "eventOutput");
            id_0a71e21ff29542c1972d4a3b8854c0cc.WireTo(id_fb691dc5653a46c4b7e99228c03d1392, "fanoutList");
            id_fb691dc5653a46c4b7e99228c03d1392.WireTo(id_00a83490dcd446c09c9664592e1a9a8b, "dataOutput");
            id_00a83490dcd446c09c9664592e1a9a8b.WireTo(id_02f65966ccc2493e8c5eec96f04423e6, "output");
            id_02f65966ccc2493e8c5eec96f04423e6.WireTo(id_c445630649fb44e5a3062dc584bdc413, "elementOutput");
            id_c445630649fb44e5a3062dc584bdc413.WireTo(setUpOutputPortBox, "output");
            setUpOutputPortBox.WireTo(id_ffa73bfc60944c37af7fe0af8283b723, "output");
            id_ffa73bfc60944c37af7fe0af8283b723.WireTo(id_5a76a8c97965401d8cda6ab631b62bab, "fanoutList");
            id_ffa73bfc60944c37af7fe0af8283b723.WireTo(id_513f15527555405eb69c2533a0752a05, "fanoutList");
            id_ed3387f56f084528aba36ee9b68cd3be.WireTo(id_3ff612b9138d4e07b02902e674b5c7ca, "output");
            id_3ff612b9138d4e07b02902e674b5c7ca.WireTo(id_ea575d3eb22a4509927f9c4fcc4f7afe, "wire");
            id_7276aef02fc849209deeaae68feeec4c.WireTo(id_4a50a8626644471294391a5883bcbd58, "output");
            id_4a50a8626644471294391a5883bcbd58.WireTo(id_d50b852d6da44d759a1398a318c2503e, "wire");
            id_5a76a8c97965401d8cda6ab631b62bab.WireTo(id_ed3387f56f084528aba36ee9b68cd3be, "fanoutList");
            id_5a76a8c97965401d8cda6ab631b62bab.WireTo(id_7276aef02fc849209deeaae68feeec4c, "fanoutList");
            id_5a76a8c97965401d8cda6ab631b62bab.WireTo(id_d2d6815df32640c084eba1d9922d9ad2, "fanoutList");
            id_5a76a8c97965401d8cda6ab631b62bab.WireTo(id_28576fd62a1c440895e21babf1b6d197, "fanoutList");
            id_5a76a8c97965401d8cda6ab631b62bab.WireTo(id_8566ea315aab45479b96c771e9ac7ea3, "fanoutList");
            id_513f15527555405eb69c2533a0752a05.WireTo(id_87b0b5c2e4b74d58a40ccb85a99dc8d5, "fanoutList");
            id_d2d6815df32640c084eba1d9922d9ad2.WireTo(id_d7268693aeac4986893c4b82bc8e1ec7, "output");
            id_d7268693aeac4986893c4b82bc8e1ec7.WireTo(id_f513c2cb2b504e96b468db6510cb1b17, "wire");
            id_28576fd62a1c440895e21babf1b6d197.WireTo(id_7f8cedfb03334c08bd7b8ecf1e212507, "output");
            id_7f8cedfb03334c08bd7b8ecf1e212507.WireTo(id_5b42b9268926427b86d80e25ecb4051f, "wire");
            id_8566ea315aab45479b96c771e9ac7ea3.WireTo(id_60fe1b7e282f464ca5b41872303014f1, "output");
            id_60fe1b7e282f464ca5b41872303014f1.WireTo(id_fb5b880f26c64386a839df3f6936fa58, "wire");
            id_5fcddc3452124e5db3810e53c3b5d02a.WireTo(id_4b2f645c40dd42ae9d2a85df5b7f3e82, "sourceOutput");
            id_ec3daa1c4f404fd38fc6d7a3c3981490.WireTo(id_784ba6702c3a4957a18a20af7cb5f15f, "sourceOutput");
            id_9b80d54da16e431ba858bd18bd4d24bc.WireTo(id_b7538f3fa4694ac5a117cddba56250e0, "sourceOutput");
            id_1fd3679c0ec642309cc8203a38289546.WireTo(id_04edd718078649ce83d0fc4d7517dc9a, "sourceOutput");
            id_a023c030388c45748e020d1f6416c3ae.WireTo(id_8609e74b85b34449a65ae100c24bfdf9, "sourceOutput");
            id_8609e74b85b34449a65ae100c24bfdf9.WireTo(id_12345fa0d4d84efa988d910e6deefa47, "fanoutList");
            id_8609e74b85b34449a65ae100c24bfdf9.WireTo(id_1362639f68f6426f9ee6ec3302490e06, "fanoutList");
            // END AUTO-GENERATED WIRING

            Render = (rootUI as IUI).GetWPFElement();
        }

        private AbstractionModel CreateDummyAbstractionModel()
        {
            var model = new AbstractionModel();
            model.AddImplementedPort("IEvent", "input1");
            model.AddImplementedPort("IEvent", "input2");
            model.AddImplementedPort("IEvent", "input3");
            model.AddAcceptedPort("IEvent", "complete");
            model.AddProperty("Type", "Box");
            model.AddProperty("Name", "test");

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








































































































































































