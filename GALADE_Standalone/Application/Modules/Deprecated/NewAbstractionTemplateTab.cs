using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;
using System.Windows;
using System.Windows.Media;

namespace Application
{
    /// <summary>
    /// <para>Contains the abstractions that have their UI represented in the "Create abstraction template" tab, and has virtual port mappings from several ports from the contained abstractions.</para>
    /// <para>Ports:</para>
    /// <para>1. IUI child: The port that connects to the UI parent of this abstraction's tab.</para>
    /// <para>2. IEvent createDomainAbstraction: The mapped button click IEvent from the "Create Domain Abstraction" button.</para>
    /// <para>3. IEvent createStoryAbstraction: The mapped button click IEvent from the "Create Story Abstraction" button.</para>
    /// <para>4. IDataFlowB&lt;List&lt;string&gt;&gt; programmingParadigmsDropDownListInput: Mapped input port to the DropDownMenus showing the available programming paradigms.</para>
    /// <para>5. IDataFlow&lt;Tuple&lt;string, Dictionary&lt;string, string&gt;&gt;&gt; newTemplateOutput: A dictionary entry representing the newly created template.</para>
    /// </summary>
    public class NewAbstractionTemplateTab : IUI
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        // Private fields
        private Tab mainTab = new Tab("Create abstraction template" ) { InstanceName = "mainTab" };

        // Input instances
        private DataFlowConnector<List<string>> programmingParadigmsFromTemplate = new DataFlowConnector<List<string>>() { InstanceName = "programmingParadigmsFromTemplate" };

        // Output instances
        private Cast<object, Tuple<string, Dictionary<string, string>>> abstractionTemplateEntry = 
            new Cast<object, Tuple<string, Dictionary<string, string>>>() { InstanceName = "abstractionTemplateEntry" };
        private ConvertToEvent<object> domainAbstractionButtonClicked = new ConvertToEvent<object>() { InstanceName = "domainAbstractionButtonClicked" };
        private ConvertToEvent<object> storyAbstractionButtonClicked = new ConvertToEvent<object>() { InstanceName = "storyAbstractionButtonClicked" };

        // Ports
        private IEvent createDomainAbstraction;
        private IEvent createStoryAbstraction;
        private IDataFlowB<List<string>> programmingParadigmsDropDownListInput;
        private IDataFlow<Tuple<string, Dictionary<string, string>>> newTemplateOutput;

        public NewAbstractionTemplateTab()
        {
            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR NewAbstractionTemplateTab.xmind
            Apply<object,Dictionary<string,string>> id_07422c223a1b49a1888f46a4d02ac46b = new Apply<object,Dictionary<string,string>>() { InstanceName = "Default", Lambda = t => (t as Tuple<string,Dictionary<string,string>>).Item2 };
            Button id_208a467b028d482c81197b128b7022a9 = new Button("Preview template" ) { InstanceName = "Default" };
            Button id_2f9440f0c68a4b308d44568c8ef3b38f = new Button("Clear fields" ) { InstanceName = "Default", Margin = new Thickness(5,0,0,0) };
            Button id_3090376c0d6a469daa41bcee0ec7d4e1 = new Button("Create Story Abstraction" ) { InstanceName = "Default", Margin = new Thickness(5,0,0,0) };
            Button id_428f0ba667114e3182465ca8a66d2b53 = new Button("Add" ) { InstanceName = "Default", Margin = new Thickness(5,0,0,0) };
            Button id_47adba8726544b0c89a6813891af8898 = new Button("Add" ) { InstanceName = "Default", Margin = new Thickness(5,0,0,0) };
            Button id_7f1eb6fb40c241fbabab8b1905bac7ad = new Button("Create Domain Abstraction" ) { InstanceName = "Default", Margin = new Thickness(5,0,0,0) };
            Cast<string,object> id_31a2c30a19cf4f16b2be2d708543b374 = new Cast<string,object>() { InstanceName = "Default" };
            Cast<string,object> id_621c2cf749d64980bbb0b6e2d11180d5 = new Cast<string,object>() { InstanceName = "Default" };
            Cast<string,object> id_6a02d97e013846a7ac73170b934c84a3 = new Cast<string,object>() { InstanceName = "Default" };
            Cast<string,object> id_9a2293c11d764a44b694651d1dcd7fc5 = new Cast<string,object>() { InstanceName = "Default" };
            Cast<string,object> id_acc12278de0348eabf94be1432df7afc = new Cast<string,object>() { InstanceName = "Default" };
            Data<object> clearAllObjects = new Data<object>() { InstanceName = "clearAllObjects", storedData = null };
            Data<object> id_df3bcd486adc49768032a6b241c4c404 = new Data<object>() { InstanceName = "Default" };
            Data<object> id_f0fe1e2db08a44c7942351cf25e107e9 = new Data<object>() { InstanceName = "Default" };
            Data<string> clearAllFields = new Data<string>() { InstanceName = "clearAllFields", storedData = "" };
            Data<string> id_5d3449d2c2c441aabaff3476a4b591d9 = new Data<string>() { InstanceName = "Default", storedData = "<Type>" };
            DataFlowConnector<object> abstractionType = new DataFlowConnector<object>() { InstanceName = "abstractionType" };
            DataFlowConnector<object> acceptedPortName = new DataFlowConnector<object>() { InstanceName = "acceptedPortName" };
            DataFlowConnector<object> acceptedPorts = new DataFlowConnector<object>() { InstanceName = "acceptedPorts" };
            DataFlowConnector<object> acceptedPortType = new DataFlowConnector<object>() { InstanceName = "acceptedPortType" };
            DataFlowConnector<object> currentTemplate = new DataFlowConnector<object>() { InstanceName = "currentTemplate" };
            DataFlowConnector<object> id_27530a5332274375ab4d3e97fef55f2e = new DataFlowConnector<object>() { InstanceName = "Default" };
            DataFlowConnector<object> id_27c730697fb545bab6656b32332bf1ae = new DataFlowConnector<object>() { InstanceName = "Default" };
            DataFlowConnector<object> id_56afc3ab53ff41ad916ff5ce94ac94b5 = new DataFlowConnector<object>() { InstanceName = "Default", Data = "" };
            DataFlowConnector<object> id_7c3917f34bbd4750b0afe96c9769b4b3 = new DataFlowConnector<object>() { InstanceName = "Default" };
            DataFlowConnector<object> id_b5c1d8aac9c249d995765672eee104ff = new DataFlowConnector<object>() { InstanceName = "Default", Data = "" };
            DataFlowConnector<object> id_bd93beb7e12f4414aed422969b2eb680 = new DataFlowConnector<object>() { InstanceName = "Default" };
            DataFlowConnector<object> id_d416180698124388bc6969c6c1ff5083 = new DataFlowConnector<object>() { InstanceName = "Default" };
            DataFlowConnector<object> id_e6be42dd421542a58834688de01798e3 = new DataFlowConnector<object>() { InstanceName = "Default" };
            DataFlowConnector<object> id_fcb5a684821e4c2d901d9fe692f9b9d5 = new DataFlowConnector<object>() { InstanceName = "Default" };
            DataFlowConnector<object> implementedPortName = new DataFlowConnector<object>() { InstanceName = "implementedPortName" };
            DataFlowConnector<object> implementedPorts = new DataFlowConnector<object>() { InstanceName = "implementedPorts" };
            DataFlowConnector<object> implementedPortType = new DataFlowConnector<object>() { InstanceName = "implementedPortType" };
            DataFlowConnector<string> id_30f6472d900741b5adcbc7038d02e104 = new DataFlowConnector<string>() { InstanceName = "Default" };
            DataFlowConnector<string> id_5f88698018334b8196b37bbc6f1d3813 = new DataFlowConnector<string>() { InstanceName = "Default" };
            DropDownMenu id_41070129381f46c99cc1bdb95314b10e = new DropDownMenu() { InstanceName = "Default", Text = "<Type>", Margin = new Thickness(5,0,0,0) };
            DropDownMenu id_7b663597f0834037b2d2371b98a11c57 = new DropDownMenu() { InstanceName = "Default", Text = "<Type>", Margin = new Thickness(5,0,0,0) };
            EventConnector id_16eac96a1c0d40d38c24817aaa86f7ef = new EventConnector() { InstanceName = "Default" };
            EventConnector id_55f7680ee50f40d3a536b1b108fadc67 = new EventConnector() { InstanceName = "Default" };
            EventConnector id_95ccf6ab3b634c08867144ab150c61f2 = new EventConnector() { InstanceName = "Default" };
            EventConnector id_b7ec761d38b0456e89c3092f5a32fcd9 = new EventConnector() { InstanceName = "Default" };
            EventConnector id_c5af5b7a03654b4c91d71f513f9781a0 = new EventConnector() { InstanceName = "Default" };
            EventConnector id_ea97ba36a3a747d2b1ccecd727729bf2 = new EventConnector() { InstanceName = "Default" };
            Horizontal id_28b1e7620ecf4fb29fcfd6030079ccdd = new Horizontal() { InstanceName = "Default", Margin = new Thickness(0,10,0,0), HorizAlignment = HorizontalAlignment.Center };
            Horizontal id_8207e49b394e4fe7bdc4827b192d59a2 = new Horizontal() { InstanceName = "Default", Margin = new Thickness(10), Ratios = new[] { 50,50 } };
            Horizontal id_8d2f6ce98ded4cccbb69743dc52448e4 = new Horizontal() { InstanceName = "Default", Ratios = new int[] {20,30,30,10}, Margin = new Thickness(0,5,0,0) };
            Horizontal id_90eb8909776c419482f6a4a934b1cba8 = new Horizontal() { InstanceName = "Default", Ratios = new int[] {20,30,30,10}, Margin = new Thickness(0,5,0,0) };
            Horizontal id_9c645b52589e48ec929db7de79f9786e = new Horizontal() { InstanceName = "Default" };
            Horizontal id_ab59660112554fdea2710ad8f05d273a = new Horizontal() { InstanceName = "Default", Ratios = new int[] {20,30,30,10}, Margin = new Thickness(0,5,0,0) };
            Horizontal id_e8a6d65090a34eed8f54fcfb7255df52 = new Horizontal() { InstanceName = "Default" };
            JSONWriter<Dictionary<string,string>> id_bf05dd3dae0b4a12b7af06cb4d440a16 = new JSONWriter<Dictionary<string,string>>() { InstanceName = "Default" };
            Operation<object> id_4570bb2ca7214f3d9ae8d6b44926221f = new Operation<object>() { InstanceName = "Default", Lambda = ops => $"{ops[0]}{ops[1]};" };
            Operation<object> id_701aa74cc996426b9ac1420bfd12796f = new Operation<object>() { InstanceName = "Default", Lambda = ops => {var abstr = ops[0] as string; var impl = ops[1] as string; var prov = ops[2] as string; var template = new Dictionary<string,string>() {{"AbstractionType",abstr},{"ImplementedPorts",impl},{"AcceptedPorts",prov}}; return Tuple.Create(abstr,template);} };
            Operation<object> id_c33c98f980a44503b5ef60ef3abc9432 = new Operation<object>() { InstanceName = "Default", Lambda = ops => $"{ops[0]} {ops[1]}" };
            Operation<object> id_ef98f4365a5949d0b80d4af78b3c95a4 = new Operation<object>() { InstanceName = "Default", Lambda = ops => $"{ops[0]} {ops[1]}" };
            Operation<object> id_f291bd8140f541f3be3b70f083beefb2 = new Operation<object>() { InstanceName = "Default", Lambda = ops => $"{ops[0]}{ops[1]};" };
            Text id_0271a74e9e1f4158bd65e77d8ac7bdf4 = new Text("Implemented ports:" ) { InstanceName = "Default", FontSize = 12 };
            Text id_11f78143b1204a789e1af593eb4ebf08 = new Text("Accepted ports:" ) { InstanceName = "Default", FontSize = 12 };
            Text id_6b4447a304454b40a6c9f6316b39b070 = new Text("Abstraction type:" ) { InstanceName = "Default", FontSize = 12 };
            Text id_8fd52526ee254981ade41557b69e3d87 = new Text("Template Preview:" ) { InstanceName = "Default", FontSize = 16, HorizAlignment = HorizontalAlignment.Left };
            Text id_b13a37f5399d4c69a931056fe67b59cb = new Text("Template Configuration:" ) { InstanceName = "Default", FontSize = 16, HorizAlignment = HorizontalAlignment.Left };
            TextBox id_1871cad854c744f385b15dd3332436d0 = new TextBox() { InstanceName = "Default", Width = 500, Height = 100, Margin = new Thickness(0,5,0,0) };
            TextBox id_1919b1cb93d14cb4b52d3b4ed1cdc574 = new TextBox() { InstanceName = "Default", Margin = new Thickness(5,0,0,0) };
            TextBox id_8ee0114af5bc403a935b4c5d249aff93 = new TextBox() { InstanceName = "Default", Margin = new Thickness(5,0,0,0) };
            TextBox id_ff27a98906344620a9507982ceb7f70b = new TextBox() { InstanceName = "Default", Margin = new Thickness(5,0,0,0) };
            Vertical id_bf369ea59a804b90aff271407794fa25 = new Vertical() { InstanceName = "Default", Margin = new Thickness(10,0,0,0) };
            Vertical id_ff333db37192438fbb21e68332fc7589 = new Vertical() { InstanceName = "Default" };
            // END AUTO-GENERATED INSTANTIATIONS FOR NewAbstractionTemplateTab.xmind

            // BEGIN AUTO-GENERATED WIRING FOR NewAbstractionTemplateTab.xmind
            programmingParadigmsFromTemplate.WireTo(id_7b663597f0834037b2d2371b98a11c57, "fanoutList"); // (@DataFlowConnector<List<string>> (programmingParadigmsFromTemplate).fanoutList) -- [IDataFlow<List<string>>] --> (DropDownMenu (id_7b663597f0834037b2d2371b98a11c57).itemsInput)
            programmingParadigmsFromTemplate.WireTo(id_41070129381f46c99cc1bdb95314b10e, "fanoutList"); // (@DataFlowConnector<List<string>> (programmingParadigmsFromTemplate).fanoutList) -- [IDataFlow<List<string>>] --> (DropDownMenu (id_41070129381f46c99cc1bdb95314b10e).itemsInput)
            mainTab.WireTo(id_8207e49b394e4fe7bdc4827b192d59a2, "tabItemList"); // (@Tab (mainTab).tabItemList) -- [List<IUI>] --> (Horizontal (id_8207e49b394e4fe7bdc4827b192d59a2).child)
            mainTab.WireTo(id_28b1e7620ecf4fb29fcfd6030079ccdd, "tabItemList"); // (@Tab (mainTab).tabItemList) -- [List<IUI>] --> (Horizontal (id_28b1e7620ecf4fb29fcfd6030079ccdd).child)
            id_8207e49b394e4fe7bdc4827b192d59a2.WireTo(id_ff333db37192438fbb21e68332fc7589, "children"); // (Horizontal (id_8207e49b394e4fe7bdc4827b192d59a2).children) -- [IUI] --> (Vertical (id_ff333db37192438fbb21e68332fc7589).child)
            id_8207e49b394e4fe7bdc4827b192d59a2.WireTo(id_bf369ea59a804b90aff271407794fa25, "children"); // (Horizontal (id_8207e49b394e4fe7bdc4827b192d59a2).children) -- [IUI] --> (Vertical (id_bf369ea59a804b90aff271407794fa25).child)
            id_ff333db37192438fbb21e68332fc7589.WireTo(id_b13a37f5399d4c69a931056fe67b59cb, "children"); // (Vertical (id_ff333db37192438fbb21e68332fc7589).children) -- [List<IUI>] --> (Text (id_b13a37f5399d4c69a931056fe67b59cb).ui)
            id_ff333db37192438fbb21e68332fc7589.WireTo(id_90eb8909776c419482f6a4a934b1cba8, "children"); // (Vertical (id_ff333db37192438fbb21e68332fc7589).children) -- [List<IUI>] --> (Horizontal (id_90eb8909776c419482f6a4a934b1cba8).child)
            id_ff333db37192438fbb21e68332fc7589.WireTo(id_ab59660112554fdea2710ad8f05d273a, "children"); // (Vertical (id_ff333db37192438fbb21e68332fc7589).children) -- [List<IUI>] --> (Horizontal (id_ab59660112554fdea2710ad8f05d273a).child)
            id_ff333db37192438fbb21e68332fc7589.WireTo(id_8d2f6ce98ded4cccbb69743dc52448e4, "children"); // (Vertical (id_ff333db37192438fbb21e68332fc7589).children) -- [List<IUI>] --> (Horizontal (id_8d2f6ce98ded4cccbb69743dc52448e4).child)
            id_90eb8909776c419482f6a4a934b1cba8.WireTo(id_6b4447a304454b40a6c9f6316b39b070, "children"); // (Horizontal (id_90eb8909776c419482f6a4a934b1cba8).children) -- [IUI] --> (Text (id_6b4447a304454b40a6c9f6316b39b070).ui)
            id_90eb8909776c419482f6a4a934b1cba8.WireTo(id_ff27a98906344620a9507982ceb7f70b, "children"); // (Horizontal (id_90eb8909776c419482f6a4a934b1cba8).children) -- [IUI] --> (TextBox (id_ff27a98906344620a9507982ceb7f70b).child)
            id_90eb8909776c419482f6a4a934b1cba8.WireTo(id_e8a6d65090a34eed8f54fcfb7255df52, "children"); // (Horizontal (id_90eb8909776c419482f6a4a934b1cba8).children) -- [IUI] --> (Horizontal (id_e8a6d65090a34eed8f54fcfb7255df52).child)
            id_90eb8909776c419482f6a4a934b1cba8.WireTo(id_9c645b52589e48ec929db7de79f9786e, "children"); // (Horizontal (id_90eb8909776c419482f6a4a934b1cba8).children) -- [IUI] --> (Horizontal (id_9c645b52589e48ec929db7de79f9786e).child)
            id_ff27a98906344620a9507982ceb7f70b.WireTo(id_31a2c30a19cf4f16b2be2d708543b374, "textOutput"); // (TextBox (id_ff27a98906344620a9507982ceb7f70b).textOutput) -- [IDataFlow<string>] --> (Cast<string,object> (id_31a2c30a19cf4f16b2be2d708543b374).input)
            id_ff27a98906344620a9507982ceb7f70b.WireTo(id_701aa74cc996426b9ac1420bfd12796f, "eventEnterPressed"); // (TextBox (id_ff27a98906344620a9507982ceb7f70b).eventEnterPressed) -- [IEvent] --> (Operation<object> (id_701aa74cc996426b9ac1420bfd12796f).startOperation)
            id_31a2c30a19cf4f16b2be2d708543b374.WireTo(abstractionType, "output"); // (Cast<string,object> (id_31a2c30a19cf4f16b2be2d708543b374).output) -- [IDataFlow<object>] --> (DataFlowConnector<object> (abstractionType).dataInput)
            id_ab59660112554fdea2710ad8f05d273a.WireTo(id_0271a74e9e1f4158bd65e77d8ac7bdf4, "children"); // (Horizontal (id_ab59660112554fdea2710ad8f05d273a).children) -- [IUI] --> (Text (id_0271a74e9e1f4158bd65e77d8ac7bdf4).ui)
            id_ab59660112554fdea2710ad8f05d273a.WireTo(id_7b663597f0834037b2d2371b98a11c57, "children"); // (Horizontal (id_ab59660112554fdea2710ad8f05d273a).children) -- [IUI] --> (DropDownMenu (id_7b663597f0834037b2d2371b98a11c57).child)
            id_ab59660112554fdea2710ad8f05d273a.WireTo(id_1919b1cb93d14cb4b52d3b4ed1cdc574, "children"); // (Horizontal (id_ab59660112554fdea2710ad8f05d273a).children) -- [IUI] --> (TextBox (id_1919b1cb93d14cb4b52d3b4ed1cdc574).child)
            id_ab59660112554fdea2710ad8f05d273a.WireTo(id_47adba8726544b0c89a6813891af8898, "children"); // (Horizontal (id_ab59660112554fdea2710ad8f05d273a).children) -- [IUI] --> (Button (id_47adba8726544b0c89a6813891af8898).child)
            id_7b663597f0834037b2d2371b98a11c57.WireTo(id_acc12278de0348eabf94be1432df7afc, "selectedItem"); // (DropDownMenu (id_7b663597f0834037b2d2371b98a11c57).selectedItem) -- [IDataFlow<string>] --> (Cast<string,object> (id_acc12278de0348eabf94be1432df7afc).input)
            id_acc12278de0348eabf94be1432df7afc.WireTo(implementedPortType, "output"); // (Cast<string,object> (id_acc12278de0348eabf94be1432df7afc).output) -- [IDataFlow<object>] --> (DataFlowConnector<object> (implementedPortType).dataInput)
            id_1919b1cb93d14cb4b52d3b4ed1cdc574.WireTo(id_9a2293c11d764a44b694651d1dcd7fc5, "textOutput"); // (TextBox (id_1919b1cb93d14cb4b52d3b4ed1cdc574).textOutput) -- [IDataFlow<string>] --> (Cast<string,object> (id_9a2293c11d764a44b694651d1dcd7fc5).input)
            id_1919b1cb93d14cb4b52d3b4ed1cdc574.WireTo(id_95ccf6ab3b634c08867144ab150c61f2, "eventEnterPressed"); // (TextBox (id_1919b1cb93d14cb4b52d3b4ed1cdc574).eventEnterPressed) -- [IEvent] --> (EventConnector (id_95ccf6ab3b634c08867144ab150c61f2).NEEDNAME)
            id_9a2293c11d764a44b694651d1dcd7fc5.WireTo(implementedPortName, "output"); // (Cast<string,object> (id_9a2293c11d764a44b694651d1dcd7fc5).output) -- [IDataFlow<object>] --> (DataFlowConnector<object> (implementedPortName).dataInput)
            id_47adba8726544b0c89a6813891af8898.WireTo(id_95ccf6ab3b634c08867144ab150c61f2, "eventButtonClicked"); // (Button (id_47adba8726544b0c89a6813891af8898).eventButtonClicked) -- [IEvent] --> (EventConnector (id_95ccf6ab3b634c08867144ab150c61f2).NEEDNAME)
            id_95ccf6ab3b634c08867144ab150c61f2.WireTo(id_ef98f4365a5949d0b80d4af78b3c95a4, "fanoutList"); // (EventConnector (id_95ccf6ab3b634c08867144ab150c61f2).fanoutList) -- [IEvent] --> (Operation<object> (id_ef98f4365a5949d0b80d4af78b3c95a4).startOperation)
            id_95ccf6ab3b634c08867144ab150c61f2.WireTo(id_f291bd8140f541f3be3b70f083beefb2, "fanoutList"); // (EventConnector (id_95ccf6ab3b634c08867144ab150c61f2).fanoutList) -- [IEvent] --> (Operation<object> (id_f291bd8140f541f3be3b70f083beefb2).startOperation)
            id_95ccf6ab3b634c08867144ab150c61f2.WireTo(id_b7ec761d38b0456e89c3092f5a32fcd9, "fanoutList"); // (EventConnector (id_95ccf6ab3b634c08867144ab150c61f2).fanoutList) -- [IEvent] --> (EventConnector (id_b7ec761d38b0456e89c3092f5a32fcd9).NEEDNAME)
            id_ef98f4365a5949d0b80d4af78b3c95a4.WireTo(implementedPortType, "operands"); // (Operation<object> (id_ef98f4365a5949d0b80d4af78b3c95a4).operands) -- [IDataFlowB<object>] --> (DataFlowConnector<object> (implementedPortType).returnDataB)
            id_ef98f4365a5949d0b80d4af78b3c95a4.WireTo(implementedPortName, "operands"); // (Operation<object> (id_ef98f4365a5949d0b80d4af78b3c95a4).operands) -- [IDataFlowB<object>] --> (DataFlowConnector<object> (implementedPortName).returnDataB)
            id_ef98f4365a5949d0b80d4af78b3c95a4.WireTo(id_e6be42dd421542a58834688de01798e3, "operationResultOutput"); // (Operation<object> (id_ef98f4365a5949d0b80d4af78b3c95a4).operationResultOutput) -- [IDataFlow<object>] --> (DataFlowConnector<object> (id_e6be42dd421542a58834688de01798e3).dataInput)
            id_f291bd8140f541f3be3b70f083beefb2.WireTo(id_56afc3ab53ff41ad916ff5ce94ac94b5, "operands"); // (Operation<object> (id_f291bd8140f541f3be3b70f083beefb2).operands) -- [IDataFlowB<object>] --> (DataFlowConnector<object> (id_56afc3ab53ff41ad916ff5ce94ac94b5).returnDataB)
            id_f291bd8140f541f3be3b70f083beefb2.WireTo(id_e6be42dd421542a58834688de01798e3, "operands"); // (Operation<object> (id_f291bd8140f541f3be3b70f083beefb2).operands) -- [IDataFlowB<object>] --> (DataFlowConnector<object> (id_e6be42dd421542a58834688de01798e3).returnDataB)
            id_f291bd8140f541f3be3b70f083beefb2.WireTo(id_27c730697fb545bab6656b32332bf1ae, "operationResultOutput"); // (Operation<object> (id_f291bd8140f541f3be3b70f083beefb2).operationResultOutput) -- [IDataFlow<object>] --> (DataFlowConnector<object> (id_27c730697fb545bab6656b32332bf1ae).dataInput)
            id_27c730697fb545bab6656b32332bf1ae.WireTo(id_56afc3ab53ff41ad916ff5ce94ac94b5, "fanoutList"); // (DataFlowConnector<object> (id_27c730697fb545bab6656b32332bf1ae).fanoutList) -- [IDataFlow<object>] --> (DataFlowConnector<object> (id_56afc3ab53ff41ad916ff5ce94ac94b5).dataInput)
            id_27c730697fb545bab6656b32332bf1ae.WireTo(implementedPorts, "fanoutList"); // (DataFlowConnector<object> (id_27c730697fb545bab6656b32332bf1ae).fanoutList) -- [IDataFlow<object>] --> (DataFlowConnector<object> (implementedPorts).dataInput)
            id_b7ec761d38b0456e89c3092f5a32fcd9.WireTo(id_701aa74cc996426b9ac1420bfd12796f, "fanoutList"); // (EventConnector (id_b7ec761d38b0456e89c3092f5a32fcd9).fanoutList) -- [IEvent] --> (Operation<object> (id_701aa74cc996426b9ac1420bfd12796f).startOperation)
            id_8d2f6ce98ded4cccbb69743dc52448e4.WireTo(id_11f78143b1204a789e1af593eb4ebf08, "children"); // (Horizontal (id_8d2f6ce98ded4cccbb69743dc52448e4).children) -- [IUI] --> (Text (id_11f78143b1204a789e1af593eb4ebf08).ui)
            id_8d2f6ce98ded4cccbb69743dc52448e4.WireTo(id_41070129381f46c99cc1bdb95314b10e, "children"); // (Horizontal (id_8d2f6ce98ded4cccbb69743dc52448e4).children) -- [IUI] --> (DropDownMenu (id_41070129381f46c99cc1bdb95314b10e).child)
            id_8d2f6ce98ded4cccbb69743dc52448e4.WireTo(id_8ee0114af5bc403a935b4c5d249aff93, "children"); // (Horizontal (id_8d2f6ce98ded4cccbb69743dc52448e4).children) -- [IUI] --> (TextBox (id_8ee0114af5bc403a935b4c5d249aff93).child)
            id_8d2f6ce98ded4cccbb69743dc52448e4.WireTo(id_428f0ba667114e3182465ca8a66d2b53, "children"); // (Horizontal (id_8d2f6ce98ded4cccbb69743dc52448e4).children) -- [IUI] --> (Button (id_428f0ba667114e3182465ca8a66d2b53).child)
            id_41070129381f46c99cc1bdb95314b10e.WireTo(id_621c2cf749d64980bbb0b6e2d11180d5, "selectedItem"); // (DropDownMenu (id_41070129381f46c99cc1bdb95314b10e).selectedItem) -- [IDataFlow<string>] --> (Cast<string,object> (id_621c2cf749d64980bbb0b6e2d11180d5).input)
            id_621c2cf749d64980bbb0b6e2d11180d5.WireTo(acceptedPortType, "output"); // (Cast<string,object> (id_621c2cf749d64980bbb0b6e2d11180d5).output) -- [IDataFlow<object>] --> (DataFlowConnector<object> (acceptedPortType).dataInput)
            id_8ee0114af5bc403a935b4c5d249aff93.WireTo(id_6a02d97e013846a7ac73170b934c84a3, "textOutput"); // (TextBox (id_8ee0114af5bc403a935b4c5d249aff93).textOutput) -- [IDataFlow<string>] --> (Cast<string,object> (id_6a02d97e013846a7ac73170b934c84a3).input)
            id_8ee0114af5bc403a935b4c5d249aff93.WireTo(id_55f7680ee50f40d3a536b1b108fadc67, "eventEnterPressed"); // (TextBox (id_8ee0114af5bc403a935b4c5d249aff93).eventEnterPressed) -- [IEvent] --> (EventConnector (id_55f7680ee50f40d3a536b1b108fadc67).NEEDNAME)
            id_6a02d97e013846a7ac73170b934c84a3.WireTo(acceptedPortName, "output"); // (Cast<string,object> (id_6a02d97e013846a7ac73170b934c84a3).output) -- [IDataFlow<object>] --> (DataFlowConnector<object> (acceptedPortName).dataInput)
            id_428f0ba667114e3182465ca8a66d2b53.WireTo(id_55f7680ee50f40d3a536b1b108fadc67, "eventButtonClicked"); // (Button (id_428f0ba667114e3182465ca8a66d2b53).eventButtonClicked) -- [IEvent] --> (EventConnector (id_55f7680ee50f40d3a536b1b108fadc67).NEEDNAME)
            id_55f7680ee50f40d3a536b1b108fadc67.WireTo(id_c33c98f980a44503b5ef60ef3abc9432, "fanoutList"); // (EventConnector (id_55f7680ee50f40d3a536b1b108fadc67).fanoutList) -- [IEvent] --> (Operation<object> (id_c33c98f980a44503b5ef60ef3abc9432).startOperation)
            id_55f7680ee50f40d3a536b1b108fadc67.WireTo(id_4570bb2ca7214f3d9ae8d6b44926221f, "fanoutList"); // (EventConnector (id_55f7680ee50f40d3a536b1b108fadc67).fanoutList) -- [IEvent] --> (Operation<object> (id_4570bb2ca7214f3d9ae8d6b44926221f).startOperation)
            id_55f7680ee50f40d3a536b1b108fadc67.WireTo(id_c5af5b7a03654b4c91d71f513f9781a0, "fanoutList"); // (EventConnector (id_55f7680ee50f40d3a536b1b108fadc67).fanoutList) -- [IEvent] --> (EventConnector (id_c5af5b7a03654b4c91d71f513f9781a0).NEEDNAME)
            id_c33c98f980a44503b5ef60ef3abc9432.WireTo(acceptedPortType, "operands"); // (Operation<object> (id_c33c98f980a44503b5ef60ef3abc9432).operands) -- [IDataFlowB<object>] --> (DataFlowConnector<object> (acceptedPortType).returnDataB)
            id_c33c98f980a44503b5ef60ef3abc9432.WireTo(acceptedPortName, "operands"); // (Operation<object> (id_c33c98f980a44503b5ef60ef3abc9432).operands) -- [IDataFlowB<object>] --> (DataFlowConnector<object> (acceptedPortName).returnDataB)
            id_c33c98f980a44503b5ef60ef3abc9432.WireTo(id_fcb5a684821e4c2d901d9fe692f9b9d5, "operationResultOutput"); // (Operation<object> (id_c33c98f980a44503b5ef60ef3abc9432).operationResultOutput) -- [IDataFlow<object>] --> (DataFlowConnector<object> (id_fcb5a684821e4c2d901d9fe692f9b9d5).dataInput)
            id_4570bb2ca7214f3d9ae8d6b44926221f.WireTo(id_b5c1d8aac9c249d995765672eee104ff, "operands"); // (Operation<object> (id_4570bb2ca7214f3d9ae8d6b44926221f).operands) -- [IDataFlowB<object>] --> (DataFlowConnector<object> (id_b5c1d8aac9c249d995765672eee104ff).returnDataB)
            id_4570bb2ca7214f3d9ae8d6b44926221f.WireTo(id_fcb5a684821e4c2d901d9fe692f9b9d5, "operands"); // (Operation<object> (id_4570bb2ca7214f3d9ae8d6b44926221f).operands) -- [IDataFlowB<object>] --> (DataFlowConnector<object> (id_fcb5a684821e4c2d901d9fe692f9b9d5).returnDataB)
            id_4570bb2ca7214f3d9ae8d6b44926221f.WireTo(id_27530a5332274375ab4d3e97fef55f2e, "operationResultOutput"); // (Operation<object> (id_4570bb2ca7214f3d9ae8d6b44926221f).operationResultOutput) -- [IDataFlow<object>] --> (DataFlowConnector<object> (id_27530a5332274375ab4d3e97fef55f2e).dataInput)
            id_27530a5332274375ab4d3e97fef55f2e.WireTo(id_b5c1d8aac9c249d995765672eee104ff, "fanoutList"); // (DataFlowConnector<object> (id_27530a5332274375ab4d3e97fef55f2e).fanoutList) -- [IDataFlow<object>] --> (DataFlowConnector<object> (id_b5c1d8aac9c249d995765672eee104ff).dataInput)
            id_27530a5332274375ab4d3e97fef55f2e.WireTo(acceptedPorts, "fanoutList"); // (DataFlowConnector<object> (id_27530a5332274375ab4d3e97fef55f2e).fanoutList) -- [IDataFlow<object>] --> (DataFlowConnector<object> (acceptedPorts).dataInput)
            id_c5af5b7a03654b4c91d71f513f9781a0.WireTo(id_701aa74cc996426b9ac1420bfd12796f, "fanoutList"); // (EventConnector (id_c5af5b7a03654b4c91d71f513f9781a0).fanoutList) -- [IEvent] --> (Operation<object> (id_701aa74cc996426b9ac1420bfd12796f).startOperation)
            id_bf369ea59a804b90aff271407794fa25.WireTo(id_8fd52526ee254981ade41557b69e3d87, "children"); // (Vertical (id_bf369ea59a804b90aff271407794fa25).children) -- [List<IUI>] --> (Text (id_8fd52526ee254981ade41557b69e3d87).ui)
            id_bf369ea59a804b90aff271407794fa25.WireTo(id_1871cad854c744f385b15dd3332436d0, "children"); // (Vertical (id_bf369ea59a804b90aff271407794fa25).children) -- [List<IUI>] --> (TextBox (id_1871cad854c744f385b15dd3332436d0).child)
            id_28b1e7620ecf4fb29fcfd6030079ccdd.WireTo(id_208a467b028d482c81197b128b7022a9, "children"); // (Horizontal (id_28b1e7620ecf4fb29fcfd6030079ccdd).children) -- [IUI] --> (Button (id_208a467b028d482c81197b128b7022a9).child)
            id_28b1e7620ecf4fb29fcfd6030079ccdd.WireTo(id_2f9440f0c68a4b308d44568c8ef3b38f, "children"); // (Horizontal (id_28b1e7620ecf4fb29fcfd6030079ccdd).children) -- [IUI] --> (Button (id_2f9440f0c68a4b308d44568c8ef3b38f).child)
            id_28b1e7620ecf4fb29fcfd6030079ccdd.WireTo(id_7f1eb6fb40c241fbabab8b1905bac7ad, "children"); // (Horizontal (id_28b1e7620ecf4fb29fcfd6030079ccdd).children) -- [IUI] --> (Button (id_7f1eb6fb40c241fbabab8b1905bac7ad).child)
            id_28b1e7620ecf4fb29fcfd6030079ccdd.WireTo(id_3090376c0d6a469daa41bcee0ec7d4e1, "children"); // (Horizontal (id_28b1e7620ecf4fb29fcfd6030079ccdd).children) -- [IUI] --> (Button (id_3090376c0d6a469daa41bcee0ec7d4e1).child)
            id_208a467b028d482c81197b128b7022a9.WireTo(id_701aa74cc996426b9ac1420bfd12796f, "eventButtonClicked"); // (Button (id_208a467b028d482c81197b128b7022a9).eventButtonClicked) -- [IEvent] --> (Operation<object> (id_701aa74cc996426b9ac1420bfd12796f).startOperation)
            id_701aa74cc996426b9ac1420bfd12796f.WireTo(abstractionType, "operands"); // (Operation<object> (id_701aa74cc996426b9ac1420bfd12796f).operands) -- [IDataFlowB<object>] --> (DataFlowConnector<object> (abstractionType).returnDataB)
            id_701aa74cc996426b9ac1420bfd12796f.WireTo(implementedPorts, "operands"); // (Operation<object> (id_701aa74cc996426b9ac1420bfd12796f).operands) -- [IDataFlowB<object>] --> (DataFlowConnector<object> (implementedPorts).returnDataB)
            id_701aa74cc996426b9ac1420bfd12796f.WireTo(acceptedPorts, "operands"); // (Operation<object> (id_701aa74cc996426b9ac1420bfd12796f).operands) -- [IDataFlowB<object>] --> (DataFlowConnector<object> (acceptedPorts).returnDataB)
            id_701aa74cc996426b9ac1420bfd12796f.WireTo(currentTemplate, "operationResultOutput"); // (Operation<object> (id_701aa74cc996426b9ac1420bfd12796f).operationResultOutput) -- [IDataFlow<object>] --> (DataFlowConnector<object> (currentTemplate).dataInput)
            currentTemplate.WireTo(id_07422c223a1b49a1888f46a4d02ac46b, "fanoutList"); // (DataFlowConnector<object> (currentTemplate).fanoutList) -- [IDataFlow<object>] --> (Apply<object,Dictionary<string,string>> (id_07422c223a1b49a1888f46a4d02ac46b).input)
            id_07422c223a1b49a1888f46a4d02ac46b.WireTo(id_bf05dd3dae0b4a12b7af06cb4d440a16, "output"); // (Apply<object,Dictionary<string,string>> (id_07422c223a1b49a1888f46a4d02ac46b).output) -- [IDataFlow<Dictionary<string,string>>>] --> (JSONWriter<Dictionary<string,string>> (id_bf05dd3dae0b4a12b7af06cb4d440a16).valueInput)
            id_bf05dd3dae0b4a12b7af06cb4d440a16.WireTo(id_1871cad854c744f385b15dd3332436d0, "stringOutput"); // (JSONWriter<Dictionary<string,string>> (id_bf05dd3dae0b4a12b7af06cb4d440a16).stringOutput) -- [IDataFlow<string>] --> (TextBox (id_1871cad854c744f385b15dd3332436d0).textInput)
            id_2f9440f0c68a4b308d44568c8ef3b38f.WireTo(id_ea97ba36a3a747d2b1ccecd727729bf2, "eventButtonClicked"); // (Button (id_2f9440f0c68a4b308d44568c8ef3b38f).eventButtonClicked) -- [IEvent] --> (EventConnector (id_ea97ba36a3a747d2b1ccecd727729bf2).NEEDNAME)
            id_ea97ba36a3a747d2b1ccecd727729bf2.WireTo(clearAllObjects, "fanoutList"); // (EventConnector (id_ea97ba36a3a747d2b1ccecd727729bf2).fanoutList) -- [IEvent] --> (Data<object> (clearAllObjects).start)
            id_ea97ba36a3a747d2b1ccecd727729bf2.WireTo(clearAllFields, "fanoutList"); // (EventConnector (id_ea97ba36a3a747d2b1ccecd727729bf2).fanoutList) -- [IEvent] --> (Data<string> (clearAllFields).start)
            id_ea97ba36a3a747d2b1ccecd727729bf2.WireTo(id_5d3449d2c2c441aabaff3476a4b591d9, "fanoutList"); // (EventConnector (id_ea97ba36a3a747d2b1ccecd727729bf2).fanoutList) -- [IEvent] --> (Data<string> (id_5d3449d2c2c441aabaff3476a4b591d9).start)
            id_ea97ba36a3a747d2b1ccecd727729bf2.WireTo(id_16eac96a1c0d40d38c24817aaa86f7ef, "fanoutList"); // (EventConnector (id_ea97ba36a3a747d2b1ccecd727729bf2).fanoutList) -- [IEvent] --> (EventConnector (id_16eac96a1c0d40d38c24817aaa86f7ef).NEEDNAME)
            clearAllObjects.WireTo(id_bd93beb7e12f4414aed422969b2eb680, "dataOutput"); // (Data<object> (clearAllObjects).dataOutput) -- [IDataFlow<object>] --> (DataFlowConnector<object> (id_bd93beb7e12f4414aed422969b2eb680).dataInput)
            id_bd93beb7e12f4414aed422969b2eb680.WireTo(acceptedPorts, "fanoutList"); // (DataFlowConnector<object> (id_bd93beb7e12f4414aed422969b2eb680).fanoutList) -- [IDataFlow<object>] --> (DataFlowConnector<object> (acceptedPorts).dataInput)
            id_bd93beb7e12f4414aed422969b2eb680.WireTo(implementedPorts, "fanoutList"); // (DataFlowConnector<object> (id_bd93beb7e12f4414aed422969b2eb680).fanoutList) -- [IDataFlow<object>] --> (DataFlowConnector<object> (implementedPorts).dataInput)
            id_bd93beb7e12f4414aed422969b2eb680.WireTo(abstractionType, "fanoutList"); // (DataFlowConnector<object> (id_bd93beb7e12f4414aed422969b2eb680).fanoutList) -- [IDataFlow<object>] --> (DataFlowConnector<object> (abstractionType).dataInput)
            id_bd93beb7e12f4414aed422969b2eb680.WireTo(id_b5c1d8aac9c249d995765672eee104ff, "fanoutList"); // (DataFlowConnector<object> (id_bd93beb7e12f4414aed422969b2eb680).fanoutList) -- [IDataFlow<object>] --> (DataFlowConnector<object> (id_b5c1d8aac9c249d995765672eee104ff).dataInput)
            id_bd93beb7e12f4414aed422969b2eb680.WireTo(id_56afc3ab53ff41ad916ff5ce94ac94b5, "fanoutList"); // (DataFlowConnector<object> (id_bd93beb7e12f4414aed422969b2eb680).fanoutList) -- [IDataFlow<object>] --> (DataFlowConnector<object> (id_56afc3ab53ff41ad916ff5ce94ac94b5).dataInput)
            clearAllFields.WireTo(id_5f88698018334b8196b37bbc6f1d3813, "dataOutput"); // (Data<string> (clearAllFields).dataOutput) -- [IDataFlow<string>] --> (DataFlowConnector<string> (id_5f88698018334b8196b37bbc6f1d3813).dataInput)
            id_5f88698018334b8196b37bbc6f1d3813.WireTo(id_ff27a98906344620a9507982ceb7f70b, "fanoutList"); // (DataFlowConnector<string> (id_5f88698018334b8196b37bbc6f1d3813).fanoutList) -- [IDataFlow<string>] --> (TextBox (id_ff27a98906344620a9507982ceb7f70b).textInput)
            id_5f88698018334b8196b37bbc6f1d3813.WireTo(id_1919b1cb93d14cb4b52d3b4ed1cdc574, "fanoutList"); // (DataFlowConnector<string> (id_5f88698018334b8196b37bbc6f1d3813).fanoutList) -- [IDataFlow<string>] --> (TextBox (id_1919b1cb93d14cb4b52d3b4ed1cdc574).textInput)
            id_5f88698018334b8196b37bbc6f1d3813.WireTo(id_8ee0114af5bc403a935b4c5d249aff93, "fanoutList"); // (DataFlowConnector<string> (id_5f88698018334b8196b37bbc6f1d3813).fanoutList) -- [IDataFlow<string>] --> (TextBox (id_8ee0114af5bc403a935b4c5d249aff93).textInput)
            id_5d3449d2c2c441aabaff3476a4b591d9.WireTo(id_30f6472d900741b5adcbc7038d02e104, "dataOutput"); // (Data<string> (id_5d3449d2c2c441aabaff3476a4b591d9).dataOutput) -- [IDataFlow<string>] --> (DataFlowConnector<string> (id_30f6472d900741b5adcbc7038d02e104).dataInput)
            id_30f6472d900741b5adcbc7038d02e104.WireTo(id_7b663597f0834037b2d2371b98a11c57, "fanoutList"); // (DataFlowConnector<string> (id_30f6472d900741b5adcbc7038d02e104).fanoutList) -- [IDataFlow<string>] --> (DropDownMenu (id_7b663597f0834037b2d2371b98a11c57).defaultSelectionInput)
            id_30f6472d900741b5adcbc7038d02e104.WireTo(id_41070129381f46c99cc1bdb95314b10e, "fanoutList"); // (DataFlowConnector<string> (id_30f6472d900741b5adcbc7038d02e104).fanoutList) -- [IDataFlow<string>] --> (DropDownMenu (id_41070129381f46c99cc1bdb95314b10e).defaultSelectionInput)
            id_16eac96a1c0d40d38c24817aaa86f7ef.WireTo(id_1871cad854c744f385b15dd3332436d0, "fanoutList"); // (EventConnector (id_16eac96a1c0d40d38c24817aaa86f7ef).fanoutList) -- [IEvent] --> (TextBox (id_1871cad854c744f385b15dd3332436d0).NEEDNAME)
            id_7f1eb6fb40c241fbabab8b1905bac7ad.WireTo(id_df3bcd486adc49768032a6b241c4c404, "eventButtonClicked"); // (Button (id_7f1eb6fb40c241fbabab8b1905bac7ad).eventButtonClicked) -- [IEvent] --> (Data<object> (id_df3bcd486adc49768032a6b241c4c404).start)
            id_df3bcd486adc49768032a6b241c4c404.WireTo(currentTemplate, "inputDataB"); // (Data<object> (id_df3bcd486adc49768032a6b241c4c404).inputDataB) -- [IDataFlowB<object>] --> (DataFlowConnector<object> (currentTemplate).returnDataB)
            id_df3bcd486adc49768032a6b241c4c404.WireTo(id_7c3917f34bbd4750b0afe96c9769b4b3, "dataOutput"); // (Data<object> (id_df3bcd486adc49768032a6b241c4c404).dataOutput) -- [IDataFlow<object>] --> (DataFlowConnector<object> (id_7c3917f34bbd4750b0afe96c9769b4b3).dataInput)
            id_7c3917f34bbd4750b0afe96c9769b4b3.WireTo(abstractionTemplateEntry, "fanoutList"); // (DataFlowConnector<object> (id_7c3917f34bbd4750b0afe96c9769b4b3).fanoutList) -- [IDataFlow<object>] --> (@Cast<object,Tuple<string,Dictionary<string,string>>> (abstractionTemplateEntry).input)
            id_7c3917f34bbd4750b0afe96c9769b4b3.WireTo(domainAbstractionButtonClicked, "fanoutList"); // (DataFlowConnector<object> (id_7c3917f34bbd4750b0afe96c9769b4b3).fanoutList) -- [IDataFlow<object>] --> (@ConvertToEvent<object> (domainAbstractionButtonClicked).start)
            id_3090376c0d6a469daa41bcee0ec7d4e1.WireTo(id_f0fe1e2db08a44c7942351cf25e107e9, "eventButtonClicked"); // (Button (id_3090376c0d6a469daa41bcee0ec7d4e1).eventButtonClicked) -- [IEvent] --> (Data<object> (id_f0fe1e2db08a44c7942351cf25e107e9).start)
            id_f0fe1e2db08a44c7942351cf25e107e9.WireTo(currentTemplate, "inputDataB"); // (Data<object> (id_f0fe1e2db08a44c7942351cf25e107e9).inputDataB) -- [IDataFlowB<object>] --> (DataFlowConnector<object> (currentTemplate).returnDataB)
            id_f0fe1e2db08a44c7942351cf25e107e9.WireTo(id_d416180698124388bc6969c6c1ff5083, "dataOutput"); // (Data<object> (id_f0fe1e2db08a44c7942351cf25e107e9).dataOutput) -- [IDataFlow<object>] --> (DataFlowConnector<object> (id_d416180698124388bc6969c6c1ff5083).dataInput)
            id_d416180698124388bc6969c6c1ff5083.WireTo(abstractionTemplateEntry, "fanoutList"); // (DataFlowConnector<object> (id_d416180698124388bc6969c6c1ff5083).fanoutList) -- [IDataFlow<object>] --> (@Cast<object,Tuple<string,Dictionary<string,string>>> (abstractionTemplateEntry).input)
            id_d416180698124388bc6969c6c1ff5083.WireTo(storyAbstractionButtonClicked, "fanoutList"); // (DataFlowConnector<object> (id_d416180698124388bc6969c6c1ff5083).fanoutList) -- [IDataFlow<object>] --> (@ConvertToEvent<object> (storyAbstractionButtonClicked).start)
            // END AUTO-GENERATED WIRING FOR NewAbstractionTemplateTab.xmind
        }

        
        private void PostWiringInitialize()
        {
            // Mapping to virtual ports
            Utilities.ConnectToVirtualPort(abstractionTemplateEntry, "output", newTemplateOutput);
            Utilities.ConnectToVirtualPort(domainAbstractionButtonClicked, "eventOutput", createDomainAbstraction);
            Utilities.ConnectToVirtualPort(storyAbstractionButtonClicked, "eventOutput", createStoryAbstraction);


            // DataChanged lambdas
            if (programmingParadigmsDropDownListInput != null)
            {
                programmingParadigmsDropDownListInput.DataChanged += () => (programmingParadigmsFromTemplate as IDataFlow<List<string>>).Data = programmingParadigmsDropDownListInput.Data;
            }

            // Send out initial values
            
        }

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            return (mainTab as IUI)?.GetWPFElement();
        }
    }
}
