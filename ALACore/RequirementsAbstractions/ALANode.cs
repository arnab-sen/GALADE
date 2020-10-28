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

        public delegate void SomethingChangedDelegate();

        public SomethingChangedDelegate PositionChanged;

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
            Horizontal id_4b87177095dc4a199361eca9faef907a = new Horizontal() {  };
            Vertical nodeMiddle = new Vertical() { InstanceName = "nodeMiddle", Margin = new Thickness(1, 0, 1, 0) };
            Horizontal nodeIdRow = new Horizontal() { InstanceName = "nodeIdRow" };
            DropDownMenu nodeTypeDropDownMenu = new DropDownMenu() { InstanceName = "nodeTypeDropDownMenu", Text = Model.GetValue("Type"), Width = 100 };
            TextBox nodeNameTextBox = new TextBox() { InstanceName = "nodeNameTextBox", Text = Model.GetValue("Name"), Width = 50 };
            UIFactory id_af51aa961b9d4581ab15ba57e652e2e6 = new UIFactory(getUIContainer: () =>{var inputPortsVert = new Vertical();inputPortsVert.Margin = new Thickness(0);return inputPortsVert;}) {  };
            DataFlowConnector<object> inputPortsVertConnector = new DataFlowConnector<object>() { InstanceName = "inputPortsVertConnector" };
            ConvertToEvent<object> id_fdd4fddd589946cf8e688245dfce874e = new ConvertToEvent<object>() {  };
            EventConnector id_a481798a4f74412483e75d66521b551c = new EventConnector() {  };
            Data<object> id_0a8a6d45c42a40ef9b6bc979bf3ccd73 = new Data<object>() { Lambda = Model.GetImplementedPorts };
            Cast<object, IEnumerable<KeyValuePair<string, string>>> id_2d140f52595843c99f6533e320d544e1 = new Cast<object, IEnumerable<KeyValuePair<string, string>>>() {  };
            ForEach<KeyValuePair<string, string>> id_28adf69c642f4013b1d7b2f8bd44efee = new ForEach<KeyValuePair<string, string>>() {  };
            Apply<KeyValuePair<string, string>, object> id_7286bb27825743baa7409ac1f2a21515 = new Apply<KeyValuePair<string, string>, object>() { Lambda = input =>{var port = new Port();port.Type = input.Value;port.Name = input.Key;port.IsInputPort = true;return port;} };
            Apply<object, object> setUpInputPortBox = new Apply<object, object>() { InstanceName = "setUpInputPortBox", Lambda = input =>{var box = new Box();box.Payload = input;box.Width = 50;box.Height = 15;box.Background = Brushes.White;box.BorderThickness = new Thickness(2);var toolTipLabel = new System.Windows.Controls.Label() { Content = (input as Port).FullName };box.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = (input as Port).ToString();var text = new Text(text: (input as Port).Name);text.HorizAlignment = HorizontalAlignment.Center;box.WireTo(text, "uiLayout");inputPortsVertConnector.Data.WireTo(box, "children");PortBoxes.Add(box);return box;} };
            UIFactory id_999e97d2c5384d488c61a885cdfaadee = new UIFactory(getUIContainer: () =>{var outputPortsVert = new Vertical();outputPortsVert.Margin = new Thickness(0);return outputPortsVert;}) {  };
            DataFlowConnector<object> outputPortsVertConnector = new DataFlowConnector<object>() { InstanceName = "outputPortsVertConnector" };
            ConvertToEvent<object> id_4938dd82b23b485cbba1fb0a5caa1c54 = new ConvertToEvent<object>() {  };
            EventConnector id_b9102cf492644e8c8383401501aea863 = new EventConnector() {  };
            Data<object> id_b1289488a16a4883890f2e6097205ad2 = new Data<object>() { Lambda = Model.GetAcceptedPorts };
            Cast<object, IEnumerable<KeyValuePair<string, string>>> id_eb960db4d2cd4fbb9d111748b15dd009 = new Cast<object, IEnumerable<KeyValuePair<string, string>>>() {  };
            ForEach<KeyValuePair<string, string>> id_761ce6d9bde14e65ab650b06605d8ad9 = new ForEach<KeyValuePair<string, string>>() {  };
            Apply<KeyValuePair<string, string>, object> id_ed444d9c5e83485b9655d59024a6d3a6 = new Apply<KeyValuePair<string, string>, object>() { Lambda = input =>{var port = new Port();port.Type = input.Value;port.Name = input.Key;port.IsInputPort = false;return port;} };
            Apply<object, object> setUpOutputPortBox = new Apply<object, object>() { InstanceName = "setUpOutputPortBox", Lambda = input =>{var box = new Box();box.Payload = input;box.Width = 50;box.Height = 15;box.Background = Brushes.White;box.BorderThickness = new Thickness(2);var toolTipLabel = new System.Windows.Controls.Label() { Content = (input as Port).FullName };box.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = (input as Port).ToString();var text = new Text(text: (input as Port).Name);text.HorizAlignment = HorizontalAlignment.Center;box.WireTo(text);outputPortsVertConnector.Data.WireTo(box, "children");PortBoxes.Add(box);return box;} };
            ApplyAction<object> id_7e665c2aa25a48498d1fa7a861fb1c58 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = Brushes.Red;} };
            DataFlowConnector<object> id_02674bf7131d45b085ff4d2dddd0ddc8 = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_28a7345b342b4ef59b68704db73dfc0c = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = Brushes.Black;} };
            ApplyAction<object> id_61b2408d99d14d30ac60260e3a1fcc41 = new ApplyAction<object>() { Lambda = input =>{(input as Box).InitialiseUI();} };
            Apply<object, object> id_5c0d90381e9b4c92a077ef00eba15e5d = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseEnter");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_cceb01e4d82a498cb7e4f86aa1683a71 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            Apply<object, object> id_812d6657eacf459dadd8d509ed943324 = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseLeave");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_255fbbb7382a4882ab61222cc050011d = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            DataFlowConnector<object> id_d25e55afc6ef45c8950950cd120f7e6d = new DataFlowConnector<object>() {  };
            DataFlowConnector<object> id_fbbadb64c3804bdc82740097e045dbf6 = new DataFlowConnector<object>() {  };
            Apply<object, object> id_9b3533bb754044dbac80ca38210ddae2 = new Apply<object, object>() { Lambda = input =>{var mouseButtonEvent = new MouseButtonEvent("MouseDown");mouseButtonEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseButtonEvent, "eventHandlers");return mouseButtonEvent;} };
            DynamicWiring<IDataFlow<object>> id_555ae46967cb4620bfbf2075fce0240e = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_6197f4eabc3c4048811e5157ce285c9e = new ApplyAction<object>() { Lambda = input =>{_selectedPort = input as Box;_selectedPort.Render.Focus();} };
            Apply<object, object> id_86e2e3c1c7fb4f18a798bfba0212a2d7 = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("GotFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_c13f1532894e403ebc8c5de801f32498 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_00d529f77f7548f48d74e3942aa023f9 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = Brushes.SandyBrown;} };
            Apply<object, object> id_e5bdec55cd3a47e387834a41e7505aa1 = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("LostFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_993e12cb1c2a4b528e34c64fa63c73a0 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_c115ac64866045938be2f7a8e8333dc6 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = Brushes.White;} };
            MouseEvent id_919913c63d364db3bc3a6f61ea1cc5d3 = new MouseEvent(eventName: "MouseEnter") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_0aa9650f6639442aa7f6f12b4a37c075 = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.Aquamarine;} };
            MouseEvent id_ed2346efadfa44a98b1291aab2ab75f4 = new MouseEvent(eventName: "MouseLeave") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_695fb9968a0f420383c9151944a77eef = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) (input as Box).Background = Brushes.LightSkyBlue;} };
            RoutedEventSubscriber id_8c7847ebd5ee4e06a08bdacc723b6f04 = new RoutedEventSubscriber(eventName: "GotFocus") { ExtractSender = source => (source as Box).Render };
            RoutedEventSubscriber id_495afa36dbfd4baabdd06e59dc708431 = new RoutedEventSubscriber(eventName: "LostFocus") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_9285ac89257d4370b616ec9f54319466 = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.LightSkyBlue;} };
            MouseButtonEvent id_316882496ea9470f80c1cc1c19bdcca4 = new MouseButtonEvent(eventName: "MouseLeftButtonDown") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_a1eb9f4abefa4779a6ffea0b926a9170 = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) ui.Focus();} };
            ApplyAction<object> id_a4daa1d93e8f433da8914522e08269c5 = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.Aquamarine;} };
            DataFlowConnector<object> id_2631aa854e954974a9f4785522d60603 = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_b423747fde9d46d18017f7bd711919ab = new ApplyAction<object>() { Lambda = input =>{StateTransition.Update(Enums.DiagramMode.IdleSelected);} };
            MouseEvent id_8142a87f823c4e9690a4e575a5d4cf77 = new MouseEvent(eventName: "MouseMove") { ExtractSender = source => (source as Box).Render, Condition = args => Mouse.LeftButton == MouseButtonState.Pressed };
            ApplyAction<object> id_e79bcf5030234e0dbf75a6cc03887f29 = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;var mousePos = Mouse.GetPosition(Canvas);var oldPosition = new Point(Canvas.GetLeft(render), Canvas.GetTop(render));Canvas.SetLeft(render, mousePos.X - _mousePosInBox.X);Canvas.SetTop(render, mousePos.Y - _mousePosInBox.Y);PositionChanged?.Invoke();} };
            MouseButtonEvent id_13d0eb407e514626aaeef8280e1a11d0 = new MouseButtonEvent(eventName: "MouseLeftButtonUp") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_ca4cefb3ae134bf89f994d2b2fd1edea = new ApplyAction<object>() { Lambda = input =>{if (Mouse.Captured == (input as Box).Render) Mouse.Capture(null);} };
            ApplyAction<object> id_7186e8cd639e4dfa98c794752cb1472e = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;_mousePosInBox = Mouse.GetPosition(render);Mouse.Capture(render);} };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            rootUI.WireTo(id_4b87177095dc4a199361eca9faef907a, "uiLayout");
            rootUI.WireTo(id_919913c63d364db3bc3a6f61ea1cc5d3, "eventHandlers");
            rootUI.WireTo(id_ed2346efadfa44a98b1291aab2ab75f4, "eventHandlers");
            rootUI.WireTo(id_8c7847ebd5ee4e06a08bdacc723b6f04, "eventHandlers");
            rootUI.WireTo(id_495afa36dbfd4baabdd06e59dc708431, "eventHandlers");
            rootUI.WireTo(id_316882496ea9470f80c1cc1c19bdcca4, "eventHandlers");
            rootUI.WireTo(id_8142a87f823c4e9690a4e575a5d4cf77, "eventHandlers");
            rootUI.WireTo(id_13d0eb407e514626aaeef8280e1a11d0, "eventHandlers");
            id_4b87177095dc4a199361eca9faef907a.WireTo(id_af51aa961b9d4581ab15ba57e652e2e6, "children");
            id_4b87177095dc4a199361eca9faef907a.WireTo(nodeMiddle, "children");
            id_4b87177095dc4a199361eca9faef907a.WireTo(id_999e97d2c5384d488c61a885cdfaadee, "children");
            nodeMiddle.WireTo(nodeIdRow, "children");
            nodeIdRow.WireTo(nodeTypeDropDownMenu, "children");
            nodeIdRow.WireTo(nodeNameTextBox, "children");
            id_af51aa961b9d4581ab15ba57e652e2e6.WireTo(inputPortsVertConnector, "uiInstanceOutput");
            inputPortsVertConnector.WireTo(id_fdd4fddd589946cf8e688245dfce874e, "fanoutList");
            id_fdd4fddd589946cf8e688245dfce874e.WireTo(id_a481798a4f74412483e75d66521b551c, "eventOutput");
            id_a481798a4f74412483e75d66521b551c.WireTo(id_0a8a6d45c42a40ef9b6bc979bf3ccd73, "fanoutList");
            id_0a8a6d45c42a40ef9b6bc979bf3ccd73.WireTo(id_2d140f52595843c99f6533e320d544e1, "dataOutput");
            id_2d140f52595843c99f6533e320d544e1.WireTo(id_28adf69c642f4013b1d7b2f8bd44efee, "output");
            id_28adf69c642f4013b1d7b2f8bd44efee.WireTo(id_7286bb27825743baa7409ac1f2a21515, "elementOutput");
            id_7286bb27825743baa7409ac1f2a21515.WireTo(setUpInputPortBox, "output");
            setUpInputPortBox.WireTo(id_02674bf7131d45b085ff4d2dddd0ddc8, "output");
            id_999e97d2c5384d488c61a885cdfaadee.WireTo(outputPortsVertConnector, "uiInstanceOutput");
            outputPortsVertConnector.WireTo(id_4938dd82b23b485cbba1fb0a5caa1c54, "fanoutList");
            id_4938dd82b23b485cbba1fb0a5caa1c54.WireTo(id_b9102cf492644e8c8383401501aea863, "eventOutput");
            id_b9102cf492644e8c8383401501aea863.WireTo(id_b1289488a16a4883890f2e6097205ad2, "fanoutList");
            id_b1289488a16a4883890f2e6097205ad2.WireTo(id_eb960db4d2cd4fbb9d111748b15dd009, "dataOutput");
            id_eb960db4d2cd4fbb9d111748b15dd009.WireTo(id_761ce6d9bde14e65ab650b06605d8ad9, "output");
            id_761ce6d9bde14e65ab650b06605d8ad9.WireTo(id_ed444d9c5e83485b9655d59024a6d3a6, "elementOutput");
            id_ed444d9c5e83485b9655d59024a6d3a6.WireTo(setUpOutputPortBox, "output");
            setUpOutputPortBox.WireTo(id_02674bf7131d45b085ff4d2dddd0ddc8, "output");
            id_02674bf7131d45b085ff4d2dddd0ddc8.WireTo(id_d25e55afc6ef45c8950950cd120f7e6d, "fanoutList");
            id_02674bf7131d45b085ff4d2dddd0ddc8.WireTo(id_fbbadb64c3804bdc82740097e045dbf6, "fanoutList");
            id_5c0d90381e9b4c92a077ef00eba15e5d.WireTo(id_cceb01e4d82a498cb7e4f86aa1683a71, "output");
            id_cceb01e4d82a498cb7e4f86aa1683a71.WireTo(id_7e665c2aa25a48498d1fa7a861fb1c58, "wire");
            id_812d6657eacf459dadd8d509ed943324.WireTo(id_255fbbb7382a4882ab61222cc050011d, "output");
            id_255fbbb7382a4882ab61222cc050011d.WireTo(id_28a7345b342b4ef59b68704db73dfc0c, "wire");
            id_d25e55afc6ef45c8950950cd120f7e6d.WireTo(id_5c0d90381e9b4c92a077ef00eba15e5d, "fanoutList");
            id_d25e55afc6ef45c8950950cd120f7e6d.WireTo(id_812d6657eacf459dadd8d509ed943324, "fanoutList");
            id_d25e55afc6ef45c8950950cd120f7e6d.WireTo(id_9b3533bb754044dbac80ca38210ddae2, "fanoutList");
            id_d25e55afc6ef45c8950950cd120f7e6d.WireTo(id_86e2e3c1c7fb4f18a798bfba0212a2d7, "fanoutList");
            id_d25e55afc6ef45c8950950cd120f7e6d.WireTo(id_e5bdec55cd3a47e387834a41e7505aa1, "fanoutList");
            id_fbbadb64c3804bdc82740097e045dbf6.WireTo(id_61b2408d99d14d30ac60260e3a1fcc41, "fanoutList");
            id_9b3533bb754044dbac80ca38210ddae2.WireTo(id_555ae46967cb4620bfbf2075fce0240e, "output");
            id_555ae46967cb4620bfbf2075fce0240e.WireTo(id_6197f4eabc3c4048811e5157ce285c9e, "wire");
            id_86e2e3c1c7fb4f18a798bfba0212a2d7.WireTo(id_c13f1532894e403ebc8c5de801f32498, "output");
            id_c13f1532894e403ebc8c5de801f32498.WireTo(id_00d529f77f7548f48d74e3942aa023f9, "wire");
            id_e5bdec55cd3a47e387834a41e7505aa1.WireTo(id_993e12cb1c2a4b528e34c64fa63c73a0, "output");
            id_993e12cb1c2a4b528e34c64fa63c73a0.WireTo(id_c115ac64866045938be2f7a8e8333dc6, "wire");
            id_919913c63d364db3bc3a6f61ea1cc5d3.WireTo(id_0aa9650f6639442aa7f6f12b4a37c075, "sourceOutput");
            id_ed2346efadfa44a98b1291aab2ab75f4.WireTo(id_695fb9968a0f420383c9151944a77eef, "sourceOutput");
            id_8c7847ebd5ee4e06a08bdacc723b6f04.WireTo(id_a4daa1d93e8f433da8914522e08269c5, "sourceOutput");
            id_495afa36dbfd4baabdd06e59dc708431.WireTo(id_9285ac89257d4370b616ec9f54319466, "sourceOutput");
            id_316882496ea9470f80c1cc1c19bdcca4.WireTo(id_2631aa854e954974a9f4785522d60603, "sourceOutput");
            id_2631aa854e954974a9f4785522d60603.WireTo(id_a1eb9f4abefa4779a6ffea0b926a9170, "fanoutList");
            id_2631aa854e954974a9f4785522d60603.WireTo(id_b423747fde9d46d18017f7bd711919ab, "fanoutList");
            id_2631aa854e954974a9f4785522d60603.WireTo(id_7186e8cd639e4dfa98c794752cb1472e, "fanoutList");
            id_8142a87f823c4e9690a4e575a5d4cf77.WireTo(id_e79bcf5030234e0dbf75a6cc03887f29, "sourceOutput");
            id_13d0eb407e514626aaeef8280e1a11d0.WireTo(id_ca4cefb3ae134bf89f994d2b2fd1edea, "sourceOutput");
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














































































































































































































