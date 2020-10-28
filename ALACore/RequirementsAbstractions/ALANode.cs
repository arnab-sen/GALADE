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
        public UIElement Render { get; set; }
        public AbstractionModel Model { get; set; }
        public Box SelectedPort { get; set; }

        // Private fields
        private Box rootUI;

        // Ports

        // Methods
        private void SetWiring()
        {
            rootUI = new Box() {Background = Brushes.LightSkyBlue};
            Model = CreateDummyAbstractionModel();

            // BEGIN AUTO-GENERATED INSTANTIATIONS
            Horizontal id_77b90b7cad774252b01eadefa98a4922 = new Horizontal() {  };
            Vertical nodeMiddle = new Vertical() { InstanceName = "nodeMiddle" };
            Horizontal nodeIdRow = new Horizontal() { InstanceName = "nodeIdRow" };
            DropDownMenu nodeTypeDropDownMenu = new DropDownMenu() { InstanceName = "nodeTypeDropDownMenu", Text = Model.GetValue("Type"), Width = 100 };
            TextBox nodeNameTextBox = new TextBox() { InstanceName = "nodeNameTextBox", Text = Model.GetValue("Name"), Width = 50 };
            UIFactory id_c01926056a7e4b98a56c7a80fde7108a = new UIFactory(getUIContainer: () =>{var inputPortsVert = new Vertical();return inputPortsVert;}) {  };
            DataFlowConnector<object> inputPortsVertConnector = new DataFlowConnector<object>() { InstanceName = "inputPortsVertConnector" };
            ConvertToEvent<object> id_2b5d0e56657c4caeb5b5f1f1d1d3fb37 = new ConvertToEvent<object>() {  };
            EventConnector id_a0ab42f7428041b6a31d667fc71ebf02 = new EventConnector() {  };
            Data<object> id_e523c57b58d24ad0b8bf491ef88b4143 = new Data<object>() { Lambda = Model.GetImplementedPorts };
            Cast<object, IEnumerable<KeyValuePair<string, string>>> id_99f21c7edd134748af2407a5c0cc18eb = new Cast<object, IEnumerable<KeyValuePair<string, string>>>() {  };
            ForEach<KeyValuePair<string, string>> id_8cb1064973a3435aa8368c13d0871ac5 = new ForEach<KeyValuePair<string, string>>() {  };
            Apply<KeyValuePair<string, string>, object> id_5407b84bcfc84116a85c67bd12a03186 = new Apply<KeyValuePair<string, string>, object>() { Lambda = input =>{var port = new Port();port.Type = input.Value;port.Name = input.Key;port.IsInputPort = true;return port;} };
            Apply<object, object> setUpInputPortBox = new Apply<object, object>() { InstanceName = "setUpInputPortBox", Lambda = input =>{var box = new Box();box.Payload = input;box.Width = 50;box.Height = 15;box.Background = Brushes.White;box.BorderThickness = new Thickness(2);var toolTipLabel = new System.Windows.Controls.Label() { Content = (input as Port).FullName };box.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = (input as Port).ToString();var text = new Text(text: (input as Port).Name);text.HorizAlignment = HorizontalAlignment.Center;box.WireTo(text, "uiLayout");inputPortsVertConnector.Data.WireTo(box, "children");return box;} };
            UIFactory id_faff93cd6e9046dab10386a9050b7e4d = new UIFactory(getUIContainer: () =>{var outputPortsVert = new Vertical();return outputPortsVert ;}) {  };
            DataFlowConnector<object> outputPortsVertConnector = new DataFlowConnector<object>() { InstanceName = "outputPortsVertConnector" };
            ConvertToEvent<object> id_03117c75b8d24cbeb0d9f5cee015c20f = new ConvertToEvent<object>() {  };
            EventConnector id_575504c3bbd14d3682bdd6a0ef4f8014 = new EventConnector() {  };
            Data<object> id_f7f9f37e407c4a9885d4f8785741ad6d = new Data<object>() { Lambda = Model.GetAcceptedPorts };
            Cast<object, IEnumerable<KeyValuePair<string, string>>> id_7d1643299c3a46cbbd844f709a961e75 = new Cast<object, IEnumerable<KeyValuePair<string, string>>>() {  };
            ForEach<KeyValuePair<string, string>> id_4ea16b63759f41a3963eba8758917de6 = new ForEach<KeyValuePair<string, string>>() {  };
            Apply<KeyValuePair<string, string>, object> id_d6aba556d1ce445d9b6254df65f2bfac = new Apply<KeyValuePair<string, string>, object>() { Lambda = input =>{var port = new Port();port.Type = input.Value;port.Name = input.Key;port.IsInputPort = false;return port;} };
            Apply<object, object> setUpOutputPortBox = new Apply<object, object>() { InstanceName = "setUpOutputPortBox", Lambda = input =>{var box = new Box();box.Payload = input;box.Width = 50;box.Height = 15;box.Background = Brushes.White;box.BorderThickness = new Thickness(2);var toolTipLabel = new System.Windows.Controls.Label() { Content = (input as Port).FullName };box.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = (input as Port).ToString();var text = new Text(text: (input as Port).Name);text.HorizAlignment = HorizontalAlignment.Center;box.WireTo(text);outputPortsVertConnector.Data.WireTo(box, "children");box.InitialiseUI();return box;} };
            ApplyAction<object> id_4790f35aa9ce49dab7d991e4503f1584 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = Brushes.Red;} };
            DataFlowConnector<object> id_ec0fc9e3ce6c40108fc8bc470604dc09 = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_7697c0d9567b4a6a8e9451885589284a = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = Brushes.Black;} };
            ApplyAction<object> id_69b5f9705af944de97f7cd8650fc4494 = new ApplyAction<object>() { Lambda = input =>{(input as Box).InitialiseUI();} };
            Apply<object, object> id_52226c6488824153b742bdd763e4fdc0 = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseEnter");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_70e8b5bc8aea4429904204ea7bc7a872 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            Apply<object, object> id_fc14286d718c4dcd8ead7ce292640703 = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseLeave");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_d8fb230576a34042a09ca36bf13b17b4 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            DataFlowConnector<object> id_240f317a64f24bdabc438bb955a6f73f = new DataFlowConnector<object>() {  };
            DataFlowConnector<object> id_c1bb80b9d9e240159e9cb77a239c0b63 = new DataFlowConnector<object>() {  };
            Apply<object, object> id_11f8bfb899964b93ac4a4594e536776e = new Apply<object, object>() { Lambda = input =>{var mouseButtonEvent = new MouseButtonEvent("MouseDown");mouseButtonEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseButtonEvent, "eventHandlers");return mouseButtonEvent;} };
            DynamicWiring<IDataFlow<object>> id_3ac816fd82c24e80ad28b8e3e1337a8d = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_6eec8aac166741f186de200ec1007c82 = new ApplyAction<object>() { Lambda = input =>{SelectedPort = input as Box;SelectedPort.Render.Focus();} };
            Apply<object, object> id_42fff64d717f4042912dd649c0c0af96 = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("GotFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_26ccc206b074453a96aac10ddbc8c00a = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_b03ced06a02a41fe8929f2b67dcd2e49 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = Brushes.SandyBrown;} };
            Apply<object, object> id_af1fb1c267824535adb0795a19be14b9 = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("LostFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_a89031326eeb499392e98468e2c25e38 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_cf84c29926e9426a9a2a017cd5131f1a = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = Brushes.White;} };
            MouseEvent id_1e03f2f9f9af4a779ddbef9d19357f12 = new MouseEvent(eventName: "MouseEnter") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_df7606a13d3340bb90e8a841a49e010d = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.Aquamarine;} };
            MouseEvent id_cbf4fb073e3c4869b388674c8dc58349 = new MouseEvent(eventName: "MouseLeave") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_b9c04ab5e22f47c79e9bd2edc646d7a5 = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) (input as Box).Background = Brushes.LightSkyBlue;} };
            RoutedEventSubscriber id_1d285169ef0a412191a2a10c0ebb7412 = new RoutedEventSubscriber(eventName: "GotFocus") { ExtractSender = source => (source as Box).Render };
            RoutedEventSubscriber id_56fdcf9ac3c14678b65659e586a8876e = new RoutedEventSubscriber(eventName: "LostFocus") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_0e9f270c534341d28023835b556c7a70 = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.LightSkyBlue;} };
            MouseButtonEvent id_d3eee346c18f4e6bb2e79b9a773cd72f = new MouseButtonEvent(eventName: "MouseLeftButtonDown") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_72f9ffe4e5b34f0e92161f4fd855645e = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) ui.Focus();} };
            ApplyAction<object> id_773ccc72656e4b44a93a315deaf72569 = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.Aquamarine;} };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            rootUI.WireTo(id_77b90b7cad774252b01eadefa98a4922, "uiLayout");
            rootUI.WireTo(id_1e03f2f9f9af4a779ddbef9d19357f12, "eventHandlers");
            rootUI.WireTo(id_cbf4fb073e3c4869b388674c8dc58349, "eventHandlers");
            rootUI.WireTo(id_1d285169ef0a412191a2a10c0ebb7412, "eventHandlers");
            rootUI.WireTo(id_56fdcf9ac3c14678b65659e586a8876e, "eventHandlers");
            rootUI.WireTo(id_d3eee346c18f4e6bb2e79b9a773cd72f, "eventHandlers");
            id_77b90b7cad774252b01eadefa98a4922.WireTo(id_c01926056a7e4b98a56c7a80fde7108a, "children");
            id_77b90b7cad774252b01eadefa98a4922.WireTo(nodeMiddle, "children");
            id_77b90b7cad774252b01eadefa98a4922.WireTo(id_faff93cd6e9046dab10386a9050b7e4d, "children");
            nodeMiddle.WireTo(nodeIdRow, "children");
            nodeIdRow.WireTo(nodeTypeDropDownMenu, "children");
            nodeIdRow.WireTo(nodeNameTextBox, "children");
            id_c01926056a7e4b98a56c7a80fde7108a.WireTo(inputPortsVertConnector, "uiInstanceOutput");
            inputPortsVertConnector.WireTo(id_2b5d0e56657c4caeb5b5f1f1d1d3fb37, "fanoutList");
            id_2b5d0e56657c4caeb5b5f1f1d1d3fb37.WireTo(id_a0ab42f7428041b6a31d667fc71ebf02, "eventOutput");
            id_a0ab42f7428041b6a31d667fc71ebf02.WireTo(id_e523c57b58d24ad0b8bf491ef88b4143, "fanoutList");
            id_e523c57b58d24ad0b8bf491ef88b4143.WireTo(id_99f21c7edd134748af2407a5c0cc18eb, "dataOutput");
            id_99f21c7edd134748af2407a5c0cc18eb.WireTo(id_8cb1064973a3435aa8368c13d0871ac5, "output");
            id_8cb1064973a3435aa8368c13d0871ac5.WireTo(id_5407b84bcfc84116a85c67bd12a03186, "elementOutput");
            id_5407b84bcfc84116a85c67bd12a03186.WireTo(setUpInputPortBox, "output");
            setUpInputPortBox.WireTo(id_ec0fc9e3ce6c40108fc8bc470604dc09, "output");
            id_faff93cd6e9046dab10386a9050b7e4d.WireTo(outputPortsVertConnector, "uiInstanceOutput");
            outputPortsVertConnector.WireTo(id_03117c75b8d24cbeb0d9f5cee015c20f, "fanoutList");
            id_03117c75b8d24cbeb0d9f5cee015c20f.WireTo(id_575504c3bbd14d3682bdd6a0ef4f8014, "eventOutput");
            id_575504c3bbd14d3682bdd6a0ef4f8014.WireTo(id_f7f9f37e407c4a9885d4f8785741ad6d, "fanoutList");
            id_f7f9f37e407c4a9885d4f8785741ad6d.WireTo(id_7d1643299c3a46cbbd844f709a961e75, "dataOutput");
            id_7d1643299c3a46cbbd844f709a961e75.WireTo(id_4ea16b63759f41a3963eba8758917de6, "output");
            id_4ea16b63759f41a3963eba8758917de6.WireTo(id_d6aba556d1ce445d9b6254df65f2bfac, "elementOutput");
            id_d6aba556d1ce445d9b6254df65f2bfac.WireTo(setUpOutputPortBox, "output");
            setUpOutputPortBox.WireTo(id_ec0fc9e3ce6c40108fc8bc470604dc09, "output");
            id_ec0fc9e3ce6c40108fc8bc470604dc09.WireTo(id_240f317a64f24bdabc438bb955a6f73f, "fanoutList");
            id_ec0fc9e3ce6c40108fc8bc470604dc09.WireTo(id_c1bb80b9d9e240159e9cb77a239c0b63, "fanoutList");
            id_52226c6488824153b742bdd763e4fdc0.WireTo(id_70e8b5bc8aea4429904204ea7bc7a872, "output");
            id_70e8b5bc8aea4429904204ea7bc7a872.WireTo(id_4790f35aa9ce49dab7d991e4503f1584, "wire");
            id_fc14286d718c4dcd8ead7ce292640703.WireTo(id_d8fb230576a34042a09ca36bf13b17b4, "output");
            id_d8fb230576a34042a09ca36bf13b17b4.WireTo(id_7697c0d9567b4a6a8e9451885589284a, "wire");
            id_240f317a64f24bdabc438bb955a6f73f.WireTo(id_52226c6488824153b742bdd763e4fdc0, "fanoutList");
            id_240f317a64f24bdabc438bb955a6f73f.WireTo(id_fc14286d718c4dcd8ead7ce292640703, "fanoutList");
            id_240f317a64f24bdabc438bb955a6f73f.WireTo(id_11f8bfb899964b93ac4a4594e536776e, "fanoutList");
            id_240f317a64f24bdabc438bb955a6f73f.WireTo(id_42fff64d717f4042912dd649c0c0af96, "fanoutList");
            id_240f317a64f24bdabc438bb955a6f73f.WireTo(id_af1fb1c267824535adb0795a19be14b9, "fanoutList");
            id_c1bb80b9d9e240159e9cb77a239c0b63.WireTo(id_69b5f9705af944de97f7cd8650fc4494, "fanoutList");
            id_11f8bfb899964b93ac4a4594e536776e.WireTo(id_3ac816fd82c24e80ad28b8e3e1337a8d, "output");
            id_3ac816fd82c24e80ad28b8e3e1337a8d.WireTo(id_6eec8aac166741f186de200ec1007c82, "wire");
            id_42fff64d717f4042912dd649c0c0af96.WireTo(id_26ccc206b074453a96aac10ddbc8c00a, "output");
            id_26ccc206b074453a96aac10ddbc8c00a.WireTo(id_b03ced06a02a41fe8929f2b67dcd2e49, "wire");
            id_af1fb1c267824535adb0795a19be14b9.WireTo(id_a89031326eeb499392e98468e2c25e38, "output");
            id_a89031326eeb499392e98468e2c25e38.WireTo(id_cf84c29926e9426a9a2a017cd5131f1a, "wire");
            id_1e03f2f9f9af4a779ddbef9d19357f12.WireTo(id_df7606a13d3340bb90e8a841a49e010d, "sourceOutput");
            id_cbf4fb073e3c4869b388674c8dc58349.WireTo(id_b9c04ab5e22f47c79e9bd2edc646d7a5, "sourceOutput");
            id_1d285169ef0a412191a2a10c0ebb7412.WireTo(id_773ccc72656e4b44a93a315deaf72569, "sourceOutput");
            id_56fdcf9ac3c14678b65659e586a8876e.WireTo(id_0e9f270c534341d28023835b556c7a70, "sourceOutput");
            id_d3eee346c18f4e6bb2e79b9a773cd72f.WireTo(id_72f9ffe4e5b34f0e92161f4fd855645e, "sourceOutput");
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






















































































































