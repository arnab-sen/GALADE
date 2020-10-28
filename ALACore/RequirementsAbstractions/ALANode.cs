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

        // Private fields
        private Box rootUI;

        // Ports

        // Methods
        private void SetWiring()
        {
            rootUI = new Box();
            Model = CreateDummyAbstractionModel();

            // BEGIN AUTO-GENERATED INSTANTIATIONS
            Horizontal id_0abfe29a31534c828f51d02f6bf032bf = new Horizontal() {  };
            Vertical nodeMiddle = new Vertical() { InstanceName = "nodeMiddle" };
            Horizontal nodeIdRow = new Horizontal() { InstanceName = "nodeIdRow" };
            DropDownMenu nodeTypeDropDownMenu = new DropDownMenu() { InstanceName = "nodeTypeDropDownMenu", Text = Model.GetValue("Type"), Width = 100 };
            TextBox nodeNameTextBox = new TextBox() { InstanceName = "nodeNameTextBox", Text = Model.GetValue("Name"), Width = 50 };
            UIFactory id_206ccb0f56044018b03dced8c32e032b = new UIFactory(getUIContainer: () =>{var inputPortsVert = new Vertical();return inputPortsVert;}) {  };
            DataFlowConnector<object> inputPortsVertConnector = new DataFlowConnector<object>() { InstanceName = "inputPortsVertConnector" };
            ConvertToEvent<object> id_b6b869d57173407cb8f490d5f4af0462 = new ConvertToEvent<object>() {  };
            EventConnector id_8c394b8ee2374ef792c4b67c1d7682ca = new EventConnector() {  };
            Data<object> id_60c0df077f89406a94a4ec3b93d51eba = new Data<object>() { Lambda = Model.GetImplementedPorts };
            Cast<object, IEnumerable<KeyValuePair<string, string>>> id_2a71d0d6a10d4d4687611420855d4758 = new Cast<object, IEnumerable<KeyValuePair<string, string>>>() {  };
            ForEach<KeyValuePair<string, string>> id_7e5444e0e88b4f81a9fbac7737aab726 = new ForEach<KeyValuePair<string, string>>() {  };
            Apply<KeyValuePair<string, string>, object> id_0c14ecde0c5d4859a5b82cf5b85015a8 = new Apply<KeyValuePair<string, string>, object>() { Lambda = input =>{var port = new Port();port.Type = input.Value;port.Name = input.Key;port.IsInputPort = true;return port;} };
            Apply<object, object> setUpInputPortBox = new Apply<object, object>() { InstanceName = "setUpInputPortBox", Lambda = input =>{var box = new Box();box.Payload = input;box.Width = 50;box.Height = 15;box.Background = Brushes.White;var toolTipLabel = new System.Windows.Controls.Label() { Content = (input as Port).FullName };box.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = (input as Port).ToString();var text = new Text(text: (input as Port).Name);text.HorizAlignment = HorizontalAlignment.Center;box.WireTo(text);inputPortsVertConnector.Data.WireTo(box, "children");box.InitialiseUI();return box;} };
            UIFactory id_7f77c6f60abc4c94a81f6cf6de057cb6 = new UIFactory(getUIContainer: () =>{var outputPortsVert = new Vertical();return outputPortsVert ;}) {  };
            DataFlowConnector<object> outputPortsVertConnector = new DataFlowConnector<object>() { InstanceName = "outputPortsVertConnector" };
            ConvertToEvent<object> id_72dee9d6e8b545cfab4700a524858ce7 = new ConvertToEvent<object>() {  };
            EventConnector id_e719b96106574e48b476460380a90eab = new EventConnector() {  };
            Data<object> id_9dfdceccb4554c9b8735b242cdb00cfa = new Data<object>() { Lambda = Model.GetAcceptedPorts };
            Cast<object, IEnumerable<KeyValuePair<string, string>>> id_ec180359ae6043bc83b17afb71c775bd = new Cast<object, IEnumerable<KeyValuePair<string, string>>>() {  };
            ForEach<KeyValuePair<string, string>> id_3157b52038624b8a8e8ab9c81e5795f5 = new ForEach<KeyValuePair<string, string>>() {  };
            Apply<KeyValuePair<string, string>, object> id_5560a099ebc24d30aeca570aa4ff431f = new Apply<KeyValuePair<string, string>, object>() { Lambda = input =>{var port = new Port();port.Type = input.Value;port.Name = input.Key;port.IsInputPort = false;return port;} };
            Apply<object, object> setUpOutputPortBox = new Apply<object, object>() { InstanceName = "setUpOutputPortBox", Lambda = input =>{var box = new Box();box.Payload = input;box.Width = 50;box.Height = 15;box.Background = Brushes.White;var toolTipLabel = new System.Windows.Controls.Label() { Content = (input as Port).FullName };box.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = (input as Port).ToString();var text = new Text(text: (input as Port).Name);text.HorizAlignment = HorizontalAlignment.Center;box.WireTo(text);outputPortsVertConnector.Data.WireTo(box, "children");box.InitialiseUI();return box;} };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            rootUI.WireTo(id_0abfe29a31534c828f51d02f6bf032bf, "uiLayout");
            id_0abfe29a31534c828f51d02f6bf032bf.WireTo(id_206ccb0f56044018b03dced8c32e032b, "children");
            id_0abfe29a31534c828f51d02f6bf032bf.WireTo(nodeMiddle, "children");
            id_0abfe29a31534c828f51d02f6bf032bf.WireTo(id_7f77c6f60abc4c94a81f6cf6de057cb6, "children");
            nodeMiddle.WireTo(nodeIdRow, "children");
            nodeIdRow.WireTo(nodeTypeDropDownMenu, "children");
            nodeIdRow.WireTo(nodeNameTextBox, "children");
            id_206ccb0f56044018b03dced8c32e032b.WireTo(inputPortsVertConnector, "uiInstanceOutput");
            inputPortsVertConnector.WireTo(id_b6b869d57173407cb8f490d5f4af0462, "fanoutList");
            id_b6b869d57173407cb8f490d5f4af0462.WireTo(id_8c394b8ee2374ef792c4b67c1d7682ca, "eventOutput");
            id_8c394b8ee2374ef792c4b67c1d7682ca.WireTo(id_60c0df077f89406a94a4ec3b93d51eba, "fanoutList");
            id_60c0df077f89406a94a4ec3b93d51eba.WireTo(id_2a71d0d6a10d4d4687611420855d4758, "dataOutput");
            id_2a71d0d6a10d4d4687611420855d4758.WireTo(id_7e5444e0e88b4f81a9fbac7737aab726, "output");
            id_7e5444e0e88b4f81a9fbac7737aab726.WireTo(id_0c14ecde0c5d4859a5b82cf5b85015a8, "elementOutput");
            id_0c14ecde0c5d4859a5b82cf5b85015a8.WireTo(setUpInputPortBox, "output");
            id_7f77c6f60abc4c94a81f6cf6de057cb6.WireTo(outputPortsVertConnector, "uiInstanceOutput");
            outputPortsVertConnector.WireTo(id_72dee9d6e8b545cfab4700a524858ce7, "fanoutList");
            id_72dee9d6e8b545cfab4700a524858ce7.WireTo(id_e719b96106574e48b476460380a90eab, "eventOutput");
            id_e719b96106574e48b476460380a90eab.WireTo(id_9dfdceccb4554c9b8735b242cdb00cfa, "fanoutList");
            id_9dfdceccb4554c9b8735b242cdb00cfa.WireTo(id_ec180359ae6043bc83b17afb71c775bd, "dataOutput");
            id_ec180359ae6043bc83b17afb71c775bd.WireTo(id_3157b52038624b8a8e8ab9c81e5795f5, "output");
            id_3157b52038624b8a8e8ab9c81e5795f5.WireTo(id_5560a099ebc24d30aeca570aa4ff431f, "elementOutput");
            id_5560a099ebc24d30aeca570aa4ff431f.WireTo(setUpOutputPortBox, "output");
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






























































