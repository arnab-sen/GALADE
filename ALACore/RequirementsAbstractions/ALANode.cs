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
        public UIElement Render { get; set; }
        public AbstractionModel Model { get; set; }
        public Box SelectedPort { get; set; }
        public List<Box> PortBoxes { get; } = new List<Box>();

        // Private fields
        private Box rootUI;

        // Ports

        // Methods
        private void SetWiring()
        {
            rootUI = new Box() {Background = Brushes.LightSkyBlue};
            Model = CreateDummyAbstractionModel();

            // BEGIN AUTO-GENERATED INSTANTIATIONS
            Horizontal id_ead4030125a44d14ba81cb8e9cc8f3ca = new Horizontal() {  };
            Vertical nodeMiddle = new Vertical() { InstanceName = "nodeMiddle", Margin = new Thickness(1, 0, 1, 0) };
            Horizontal nodeIdRow = new Horizontal() { InstanceName = "nodeIdRow" };
            DropDownMenu nodeTypeDropDownMenu = new DropDownMenu() { InstanceName = "nodeTypeDropDownMenu", Text = Model.GetValue("Type"), Width = 100 };
            TextBox nodeNameTextBox = new TextBox() { InstanceName = "nodeNameTextBox", Text = Model.GetValue("Name"), Width = 50 };
            UIFactory id_b7a0f4a1784246528f3e14f471b31f13 = new UIFactory(getUIContainer: () =>{var inputPortsVert = new Vertical();inputPortsVert.Margin = new Thickness(0);return inputPortsVert;}) {  };
            DataFlowConnector<object> inputPortsVertConnector = new DataFlowConnector<object>() { InstanceName = "inputPortsVertConnector" };
            ConvertToEvent<object> id_cf529a1540f74718878da86dcc29a20e = new ConvertToEvent<object>() {  };
            EventConnector id_d6cc827ca82242c29aa506f02b0bdf1d = new EventConnector() {  };
            Data<object> id_10a76725249b4506b3534121d99a4c29 = new Data<object>() { Lambda = Model.GetImplementedPorts };
            Cast<object, IEnumerable<KeyValuePair<string, string>>> id_4f30bc9012fa4a838aeccb93fcdfc7be = new Cast<object, IEnumerable<KeyValuePair<string, string>>>() {  };
            ForEach<KeyValuePair<string, string>> id_656c2ff7e6964a76a13dd8e72511ae11 = new ForEach<KeyValuePair<string, string>>() {  };
            Apply<KeyValuePair<string, string>, object> id_2528c18eafdb4a7091c49e1c7c915ab5 = new Apply<KeyValuePair<string, string>, object>() { Lambda = input =>{var port = new Port();port.Type = input.Value;port.Name = input.Key;port.IsInputPort = true;return port;} };
            Apply<object, object> setUpInputPortBox = new Apply<object, object>() { InstanceName = "setUpInputPortBox", Lambda = input =>{var box = new Box();box.Payload = input;box.Width = 50;box.Height = 15;box.Background = Brushes.White;box.BorderThickness = new Thickness(2);var toolTipLabel = new System.Windows.Controls.Label() { Content = (input as Port).FullName };box.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = (input as Port).ToString();var text = new Text(text: (input as Port).Name);text.HorizAlignment = HorizontalAlignment.Center;box.WireTo(text, "uiLayout");inputPortsVertConnector.Data.WireTo(box, "children");PortBoxes.Add(box);return box;} };
            UIFactory id_18ad764ccce149ebb19412242785ece4 = new UIFactory(getUIContainer: () =>{var outputPortsVert = new Vertical();outputPortsVert.Margin = new Thickness(0);return outputPortsVert;}) {  };
            DataFlowConnector<object> outputPortsVertConnector = new DataFlowConnector<object>() { InstanceName = "outputPortsVertConnector" };
            ConvertToEvent<object> id_258f2fd1c5be425784c4af9e740cf298 = new ConvertToEvent<object>() {  };
            EventConnector id_9d7e20b7cec14d5abe74779a3f8d2d36 = new EventConnector() {  };
            Data<object> id_24c77ece03ce423b96a166d0b8686047 = new Data<object>() { Lambda = Model.GetAcceptedPorts };
            Cast<object, IEnumerable<KeyValuePair<string, string>>> id_7de351f2a4a44fde9199812089e76aff = new Cast<object, IEnumerable<KeyValuePair<string, string>>>() {  };
            ForEach<KeyValuePair<string, string>> id_20e36c01069c4ac6886ae9de4c7cceeb = new ForEach<KeyValuePair<string, string>>() {  };
            Apply<KeyValuePair<string, string>, object> id_591e588a640844799c51fc26aba8e031 = new Apply<KeyValuePair<string, string>, object>() { Lambda = input =>{var port = new Port();port.Type = input.Value;port.Name = input.Key;port.IsInputPort = false;return port;} };
            Apply<object, object> setUpOutputPortBox = new Apply<object, object>() { InstanceName = "setUpOutputPortBox", Lambda = input =>{var box = new Box();box.Payload = input;box.Width = 50;box.Height = 15;box.Background = Brushes.White;box.BorderThickness = new Thickness(2);var toolTipLabel = new System.Windows.Controls.Label() { Content = (input as Port).FullName };box.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = (input as Port).ToString();var text = new Text(text: (input as Port).Name);text.HorizAlignment = HorizontalAlignment.Center;box.WireTo(text);outputPortsVertConnector.Data.WireTo(box, "children");box.InitialiseUI();return box;} };
            ApplyAction<object> id_c33c354366d84296b9a78612c88c0da7 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = Brushes.Red;} };
            DataFlowConnector<object> id_b7d2a3486598487d85d43d1ff2c7fdd5 = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_f5bb77d634e94877b895f4f6105b59df = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = Brushes.Black;} };
            ApplyAction<object> id_0ff69001138a4612a0144dc5e1e89115 = new ApplyAction<object>() { Lambda = input =>{(input as Box).InitialiseUI();} };
            Apply<object, object> id_4758ab8969014e3a97685b0cd56c23ce = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseEnter");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_48d6103806944f5aabc53279fa53c636 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            Apply<object, object> id_8124e2bbdbe741f892e160891f5cd838 = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseLeave");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_0aeedfca52df44eba965a03418789f2a = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            DataFlowConnector<object> id_6ce6fbb585664d94892e6638a85574b3 = new DataFlowConnector<object>() {  };
            DataFlowConnector<object> id_111a9c9f2a8f49c9acf51889fa0f18bf = new DataFlowConnector<object>() {  };
            Apply<object, object> id_0a7f43379aac42f099391c660f34f081 = new Apply<object, object>() { Lambda = input =>{var mouseButtonEvent = new MouseButtonEvent("MouseDown");mouseButtonEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseButtonEvent, "eventHandlers");return mouseButtonEvent;} };
            DynamicWiring<IDataFlow<object>> id_1a469a659f6041fd83571774fb8b3a11 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_1373b7d5a4f74f6a9a82e32bf8482c6f = new ApplyAction<object>() { Lambda = input =>{SelectedPort = input as Box;SelectedPort.Render.Focus();} };
            Apply<object, object> id_27bbb54bd45a4a379e7185492dd92048 = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("GotFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_cd2e25f6923f4c0c8c6cf53b23647033 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_a6e91880418e43d2aa33f05eb40a9bda = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = Brushes.SandyBrown;} };
            Apply<object, object> id_b3b69b585bfe48afac863263746e2713 = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("LostFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_206037f24172437dab37bb84d9489a9f = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_f67570c1cd8141a5bf2c21e11dc1d7f5 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = Brushes.White;} };
            MouseEvent id_5d0e52304aa747019a4c6834facea2b8 = new MouseEvent(eventName: "MouseEnter") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_3913e826edb347ae89400111c1329eb4 = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.Aquamarine;} };
            MouseEvent id_e295cbdd13164873a1b1d33022bc6b94 = new MouseEvent(eventName: "MouseLeave") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_9851ea64c12644379e3edb71e2a5c6cc = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) (input as Box).Background = Brushes.LightSkyBlue;} };
            RoutedEventSubscriber id_236b79a3cf0c473bac9df034bca0a871 = new RoutedEventSubscriber(eventName: "GotFocus") { ExtractSender = source => (source as Box).Render };
            RoutedEventSubscriber id_c3e4e581d8e24996a1a5942cae4ef560 = new RoutedEventSubscriber(eventName: "LostFocus") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_5e5f3bc3cbe148e7ac95c917ed445bb3 = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.LightSkyBlue;} };
            MouseButtonEvent id_7efd16688b3b446e9e11426e12556908 = new MouseButtonEvent(eventName: "MouseLeftButtonDown") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_86e187b81d054846adbe402f6cfdfbe9 = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) ui.Focus();} };
            ApplyAction<object> id_7667d95a33214bf19fbc83c63430de31 = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.Aquamarine;} };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            rootUI.WireTo(id_ead4030125a44d14ba81cb8e9cc8f3ca, "uiLayout");
            rootUI.WireTo(id_5d0e52304aa747019a4c6834facea2b8, "eventHandlers");
            rootUI.WireTo(id_e295cbdd13164873a1b1d33022bc6b94, "eventHandlers");
            rootUI.WireTo(id_236b79a3cf0c473bac9df034bca0a871, "eventHandlers");
            rootUI.WireTo(id_c3e4e581d8e24996a1a5942cae4ef560, "eventHandlers");
            rootUI.WireTo(id_7efd16688b3b446e9e11426e12556908, "eventHandlers");
            id_ead4030125a44d14ba81cb8e9cc8f3ca.WireTo(id_b7a0f4a1784246528f3e14f471b31f13, "children");
            id_ead4030125a44d14ba81cb8e9cc8f3ca.WireTo(nodeMiddle, "children");
            id_ead4030125a44d14ba81cb8e9cc8f3ca.WireTo(id_18ad764ccce149ebb19412242785ece4, "children");
            nodeMiddle.WireTo(nodeIdRow, "children");
            nodeIdRow.WireTo(nodeTypeDropDownMenu, "children");
            nodeIdRow.WireTo(nodeNameTextBox, "children");
            id_b7a0f4a1784246528f3e14f471b31f13.WireTo(inputPortsVertConnector, "uiInstanceOutput");
            inputPortsVertConnector.WireTo(id_cf529a1540f74718878da86dcc29a20e, "fanoutList");
            id_cf529a1540f74718878da86dcc29a20e.WireTo(id_d6cc827ca82242c29aa506f02b0bdf1d, "eventOutput");
            id_d6cc827ca82242c29aa506f02b0bdf1d.WireTo(id_10a76725249b4506b3534121d99a4c29, "fanoutList");
            id_10a76725249b4506b3534121d99a4c29.WireTo(id_4f30bc9012fa4a838aeccb93fcdfc7be, "dataOutput");
            id_4f30bc9012fa4a838aeccb93fcdfc7be.WireTo(id_656c2ff7e6964a76a13dd8e72511ae11, "output");
            id_656c2ff7e6964a76a13dd8e72511ae11.WireTo(id_2528c18eafdb4a7091c49e1c7c915ab5, "elementOutput");
            id_2528c18eafdb4a7091c49e1c7c915ab5.WireTo(setUpInputPortBox, "output");
            setUpInputPortBox.WireTo(id_b7d2a3486598487d85d43d1ff2c7fdd5, "output");
            id_18ad764ccce149ebb19412242785ece4.WireTo(outputPortsVertConnector, "uiInstanceOutput");
            outputPortsVertConnector.WireTo(id_258f2fd1c5be425784c4af9e740cf298, "fanoutList");
            id_258f2fd1c5be425784c4af9e740cf298.WireTo(id_9d7e20b7cec14d5abe74779a3f8d2d36, "eventOutput");
            id_9d7e20b7cec14d5abe74779a3f8d2d36.WireTo(id_24c77ece03ce423b96a166d0b8686047, "fanoutList");
            id_24c77ece03ce423b96a166d0b8686047.WireTo(id_7de351f2a4a44fde9199812089e76aff, "dataOutput");
            id_7de351f2a4a44fde9199812089e76aff.WireTo(id_20e36c01069c4ac6886ae9de4c7cceeb, "output");
            id_20e36c01069c4ac6886ae9de4c7cceeb.WireTo(id_591e588a640844799c51fc26aba8e031, "elementOutput");
            id_591e588a640844799c51fc26aba8e031.WireTo(setUpOutputPortBox, "output");
            setUpOutputPortBox.WireTo(id_b7d2a3486598487d85d43d1ff2c7fdd5, "output");
            id_b7d2a3486598487d85d43d1ff2c7fdd5.WireTo(id_6ce6fbb585664d94892e6638a85574b3, "fanoutList");
            id_b7d2a3486598487d85d43d1ff2c7fdd5.WireTo(id_111a9c9f2a8f49c9acf51889fa0f18bf, "fanoutList");
            id_4758ab8969014e3a97685b0cd56c23ce.WireTo(id_48d6103806944f5aabc53279fa53c636, "output");
            id_48d6103806944f5aabc53279fa53c636.WireTo(id_c33c354366d84296b9a78612c88c0da7, "wire");
            id_8124e2bbdbe741f892e160891f5cd838.WireTo(id_0aeedfca52df44eba965a03418789f2a, "output");
            id_0aeedfca52df44eba965a03418789f2a.WireTo(id_f5bb77d634e94877b895f4f6105b59df, "wire");
            id_6ce6fbb585664d94892e6638a85574b3.WireTo(id_4758ab8969014e3a97685b0cd56c23ce, "fanoutList");
            id_6ce6fbb585664d94892e6638a85574b3.WireTo(id_8124e2bbdbe741f892e160891f5cd838, "fanoutList");
            id_6ce6fbb585664d94892e6638a85574b3.WireTo(id_0a7f43379aac42f099391c660f34f081, "fanoutList");
            id_6ce6fbb585664d94892e6638a85574b3.WireTo(id_27bbb54bd45a4a379e7185492dd92048, "fanoutList");
            id_6ce6fbb585664d94892e6638a85574b3.WireTo(id_b3b69b585bfe48afac863263746e2713, "fanoutList");
            id_111a9c9f2a8f49c9acf51889fa0f18bf.WireTo(id_0ff69001138a4612a0144dc5e1e89115, "fanoutList");
            id_0a7f43379aac42f099391c660f34f081.WireTo(id_1a469a659f6041fd83571774fb8b3a11, "output");
            id_1a469a659f6041fd83571774fb8b3a11.WireTo(id_1373b7d5a4f74f6a9a82e32bf8482c6f, "wire");
            id_27bbb54bd45a4a379e7185492dd92048.WireTo(id_cd2e25f6923f4c0c8c6cf53b23647033, "output");
            id_cd2e25f6923f4c0c8c6cf53b23647033.WireTo(id_a6e91880418e43d2aa33f05eb40a9bda, "wire");
            id_b3b69b585bfe48afac863263746e2713.WireTo(id_206037f24172437dab37bb84d9489a9f, "output");
            id_206037f24172437dab37bb84d9489a9f.WireTo(id_f67570c1cd8141a5bf2c21e11dc1d7f5, "wire");
            id_5d0e52304aa747019a4c6834facea2b8.WireTo(id_3913e826edb347ae89400111c1329eb4, "sourceOutput");
            id_e295cbdd13164873a1b1d33022bc6b94.WireTo(id_9851ea64c12644379e3edb71e2a5c6cc, "sourceOutput");
            id_236b79a3cf0c473bac9df034bca0a871.WireTo(id_7667d95a33214bf19fbc83c63430de31, "sourceOutput");
            id_c3e4e581d8e24996a1a5942cae4ef560.WireTo(id_5e5f3bc3cbe148e7ac95c917ed445bb3, "sourceOutput");
            id_7efd16688b3b446e9e11426e12556908.WireTo(id_86e187b81d054846adbe402f6cfdfbe9, "sourceOutput");
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
































































































































































