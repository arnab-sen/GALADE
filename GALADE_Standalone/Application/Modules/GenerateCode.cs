using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace Application
{
    /// <summary>
    /// <para>Converts an entire VisualPortGraph into its equivalent ALA code.</para>
    /// <para>Ports:</para>
    /// <para>1. IEvent start:</para>
    /// <para>2. IDataFlow&lt;List&lt;string&gt;&gt; instantiationLinesOutput:</para>
    /// <para>3. IDataFlow&lt;List&lt;string&gt;&gt; wiringLinesOutput:</para>
    /// </summary>
    public class GenerateCode : IEvent
    {
        // Public fields and properties
        public string InstanceName = "Default";
        public VisualPortGraph Graph { get; set; }

        // Private fields
        
        // Ports
        private IDataFlow<List<string>> instantiationLinesOutput;
        private IDataFlow<List<string>> wiringLinesOutput;
        
        // Input instances
        private EventConnector startConnector = new EventConnector() { InstanceName = "startConnector" };
        
        // Output instances
        private Apply<List<string>, List<string>> instantiationLinesOutputConnector = new Apply<List<string>, List<string>>() { InstanceName = "instantiationLinesOutputConnector", Lambda = input => input };
        private Apply<List<string>, List<string>> wiringLinesOutputConnector = new Apply<List<string>, List<string>>() { InstanceName = "wiringLinesOutputConnector", Lambda = input => input };
        
        // IEvent implementation
        void IEvent.Execute()
        {
            (startConnector as IEvent).Execute();
        }
        
        // PostWiringInitialize
        private void PostWiringInitialize()
        {
            // Mapping to virtual ports
            Utilities.ConnectToVirtualPort(instantiationLinesOutputConnector, "output", instantiationLinesOutput);
            Utilities.ConnectToVirtualPort(wiringLinesOutputConnector, "output", wiringLinesOutput);
            
            // IDataFlowB and IEventB event handlers
            // Send out initial values
            // (instanceNeedingInitialValue as IDataFlow<T>).Data = defaultValue;
        }

        // Methods
        public VisualPortGraph GetGraph() => Graph;

        public string GetInstantiationCode(VisualPortGraphNode node)
        {
            if (node.Render.Visibility != Visibility.Visible || node.Name.StartsWith("@")) return "";

            var sb = new StringBuilder();
            var obj = JObject.Parse(node.Serialise());

            string type = obj.GetValue("Type").ToString();
            string name = obj.GetValue("Name").ToString();
            if (string.IsNullOrWhiteSpace(name)) name = $"id_{obj.GetValue("Id")}";

            var constructorArgs = new List<string>();
            var properties = new List<string>();

            if (!name.StartsWith("id_")) properties.Add($"InstanceName = \"{name}\"");

            foreach (JObject parameter in obj.GetValue("NodeParameters"))
            {
                if (parameter["ParameterType"].ToString() == "Constructor")
                {
                    constructorArgs.Add($"{parameter["Name"]}: {parameter["Value"]}");
                }
                else if (parameter["ParameterType"].ToString() == "Property")
                {
                    properties.Add($"{parameter["Name"]} = {parameter["Value"]}");
                }
            }

            var constructorArgsSB = new StringBuilder();
            if (constructorArgs.Count > 0)
            {
                constructorArgsSB.Append(constructorArgs[0]);

                if (constructorArgs.Count > 1)
                {
                    foreach (var arg in constructorArgs.Skip(1))
                    {
                        constructorArgsSB.Append($", {arg}");
                    }
                }
            }

            var propertiesSB = new StringBuilder();
            if (properties.Count > 0)
            {
                propertiesSB.Append(properties[0]);

                if (properties.Count > 1)
                {
                    foreach (var prop in properties.Skip(1))
                    {
                        propertiesSB.Append($", {prop}");
                    }
                }
            }

            var unflattenedString = $"{type} {name} = new {type}({constructorArgsSB}) {{ {propertiesSB} }};";
            // var flattenedString = unflattenedString.Replace(Environment.NewLine, ""); // Note: this does not handle when new lines are escaped
            var flattenedString = Regex.Replace(unflattenedString, @"[\t\n\r]", ""); // Note: this does not handle when new lines are escaped

            sb.Append(flattenedString);

            return sb.ToString();
        }

        public string GetWiringCode(IPortConnection wire)
        {
            if (wire.Render.Visibility != Visibility.Visible) return "";
            if ((Graph.GetNode(wire.SourceId) as VisualPortGraphNode)?.Type.StartsWith("@") ?? false) return "";

            var sb = new StringBuilder();

            var A = Graph.GetNode(wire.SourceId) as VisualPortGraphNode;
            var AName = !string.IsNullOrWhiteSpace(A.Name) ? A.Name : $"id_{A.Id}";
            AName = AName.Replace("@", "");

            var B = Graph.GetNode(wire.DestinationId) as VisualPortGraphNode;
            var BName = !string.IsNullOrWhiteSpace(B.Name) ? B.Name : $"id_{B.Id}";
            BName = BName.Replace("@", "");

            sb.Append($"{AName}.WireTo({BName}, \"{wire.SourcePort.Name}\");");

            return sb.ToString();
        }

        /// <summary>
        /// <para></para>
        /// </summary>
        public GenerateCode()
        {
            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR GenerateCode.xmind
            Apply<IPortConnection,string> getWiringCode = new Apply<IPortConnection,string>() { InstanceName = "getWiringCode", Lambda = wire => GetWiringCode(wire) };
            Apply<VisualPortGraph,IEnumerable<IPortConnection>> id_93847d6aaed2417982f07e3c5ceb7d6c = new Apply<VisualPortGraph,IEnumerable<IPortConnection>>() { InstanceName = "Default", Lambda = graph => graph.GetConnections() };
            Apply<VisualPortGraph,IEnumerable<VisualPortGraphNode>> id_58c0814d58704d2ba3914cb9086e1d4c = new Apply<VisualPortGraph,IEnumerable<VisualPortGraphNode>>() { InstanceName = "Default", Lambda = graph => graph.GetNodes().Select(n => n as VisualPortGraphNode) };
            Apply<VisualPortGraphNode,string> getInstantiationCode = new Apply<VisualPortGraphNode,string>() { InstanceName = "getInstantiationCode", Lambda = node => GetInstantiationCode(node) };
            Collection<string> id_edb65ab7de9643389d7ad3b04e552a23 = new Collection<string>() { InstanceName = "Default", OutputLength = -2, OutputOnEvent = true };
            Collection<string> id_f0164989439c44c69ad7d21790e8b6fe = new Collection<string>() { InstanceName = "Default", OutputLength = -2, OutputOnEvent = true };
            ConditionalData<string> id_4bffda1df55b4072b1d4c5fae8296108 = new ConditionalData<string>() { InstanceName = "Default", Condition = s => !string.IsNullOrWhiteSpace(s) };
            ConditionalData<string> id_cdddd549ddf24caa98e599e12ae9bd4a = new ConditionalData<string>() { InstanceName = "Default", Condition = s => !string.IsNullOrWhiteSpace(s) };
            Data<VisualPortGraph> id_02492ace575b4661a828cda6c3ae4513 = new Data<VisualPortGraph>() { InstanceName = "Default", Lambda = GetGraph };
            Data<VisualPortGraph> id_6da14dcf73384827ba2a82ae293841d5 = new Data<VisualPortGraph>() { InstanceName = "Default", Lambda = GetGraph };
            ForEach<IPortConnection> id_2964f2d203b64e86b40148d73196645d = new ForEach<IPortConnection>() { InstanceName = "Default" };
            ForEach<VisualPortGraphNode> id_153c3759698e4ce889f4782c9d76d418 = new ForEach<VisualPortGraphNode>() { InstanceName = "Default" };
            // END AUTO-GENERATED INSTANTIATIONS FOR GenerateCode.xmind
            
            // BEGIN AUTO-GENERATED WIRING FOR GenerateCode.xmind
            startConnector.WireTo(id_6da14dcf73384827ba2a82ae293841d5, "fanoutList"); // (@EventConnector (startConnector).fanoutList) -- [IEvent] --> (Data<VisualPortGraph> (id_6da14dcf73384827ba2a82ae293841d5).start)
            startConnector.WireTo(id_02492ace575b4661a828cda6c3ae4513, "fanoutList"); // (@EventConnector (startConnector).fanoutList) -- [IEvent] --> (Data<VisualPortGraph> (id_02492ace575b4661a828cda6c3ae4513).start)
            id_6da14dcf73384827ba2a82ae293841d5.WireTo(id_58c0814d58704d2ba3914cb9086e1d4c, "dataOutput"); // (Data<VisualPortGraph> (id_6da14dcf73384827ba2a82ae293841d5).dataOutput) -- [IDataFlow<VisualPortGraph>] --> (Apply<VisualPortGraph,IEnumerable<VisualPortGraphNode>> (id_58c0814d58704d2ba3914cb9086e1d4c).input)
            id_58c0814d58704d2ba3914cb9086e1d4c.WireTo(id_153c3759698e4ce889f4782c9d76d418, "output"); // (Apply<VisualPortGraph,IEnumerable<VisualPortGraphNode>> (id_58c0814d58704d2ba3914cb9086e1d4c).output) -- [IDataFlow<IEnumerable<VisualPortGraphNode>>] --> (ForEach<VisualPortGraphNode> (id_153c3759698e4ce889f4782c9d76d418).collectionInput)
            id_153c3759698e4ce889f4782c9d76d418.WireTo(getInstantiationCode, "elementOutput"); // (ForEach<VisualPortGraphNode> (id_153c3759698e4ce889f4782c9d76d418).elementOutput) -- [IDataFlow<VisualPortGraphNode>] --> (Apply<VisualPortGraphNode,string> (getInstantiationCode).input)
            id_153c3759698e4ce889f4782c9d76d418.WireTo(id_edb65ab7de9643389d7ad3b04e552a23, "complete"); // (ForEach<VisualPortGraphNode> (id_153c3759698e4ce889f4782c9d76d418).complete) -- [IEvent] --> (Collection<string> (id_edb65ab7de9643389d7ad3b04e552a23).clearList)
            getInstantiationCode.WireTo(id_4bffda1df55b4072b1d4c5fae8296108, "output"); // (Apply<VisualPortGraphNode,string> (getInstantiationCode).output) -- [IDataFlow<string>] --> (ConditionalData<string> (id_4bffda1df55b4072b1d4c5fae8296108).input)
            id_4bffda1df55b4072b1d4c5fae8296108.WireTo(id_edb65ab7de9643389d7ad3b04e552a23, "conditionMetOutput"); // (ConditionalData<string> (id_4bffda1df55b4072b1d4c5fae8296108).conditionMetOutput) -- [IDataFlow<string>] --> (Collection<string> (id_edb65ab7de9643389d7ad3b04e552a23).element)
            id_edb65ab7de9643389d7ad3b04e552a23.WireTo(instantiationLinesOutputConnector, "listOutput"); // (Collection<string> (id_edb65ab7de9643389d7ad3b04e552a23).listOutput) -- [IDataFlow<List<string>>] --> (@DataFlowConnector<List<string>> (instantiationLinesOutputConnector).dataInput)
            id_02492ace575b4661a828cda6c3ae4513.WireTo(id_93847d6aaed2417982f07e3c5ceb7d6c, "dataOutput"); // (Data<VisualPortGraph> (id_02492ace575b4661a828cda6c3ae4513).dataOutput) -- [IDataFlow<VisualPortGraph>] --> (Apply<VisualPortGraph,IEnumerable<IPortConnection>> (id_93847d6aaed2417982f07e3c5ceb7d6c).input)
            id_93847d6aaed2417982f07e3c5ceb7d6c.WireTo(id_2964f2d203b64e86b40148d73196645d, "output"); // (Apply<VisualPortGraph,IEnumerable<IPortConnection>> (id_93847d6aaed2417982f07e3c5ceb7d6c).output) -- [IDataFlow<IEnumerable<IPortConnection>>] --> (ForEach<IPortConnection> (id_2964f2d203b64e86b40148d73196645d).collectionInput)
            id_2964f2d203b64e86b40148d73196645d.WireTo(getWiringCode, "elementOutput"); // (ForEach<IPortConnection> (id_2964f2d203b64e86b40148d73196645d).elementOutput) -- [IDataFlow<IPortConnection>] --> (Apply<IPortConnection,string> (getWiringCode).input)
            id_2964f2d203b64e86b40148d73196645d.WireTo(id_f0164989439c44c69ad7d21790e8b6fe, "complete"); // (ForEach<IPortConnection> (id_2964f2d203b64e86b40148d73196645d).complete) -- [IEvent] --> (Collection<string> (id_f0164989439c44c69ad7d21790e8b6fe).clearList)
            getWiringCode.WireTo(id_cdddd549ddf24caa98e599e12ae9bd4a, "output"); // (Apply<IPortConnection,string> (getWiringCode).output) -- [IDataFlow<string>] --> (ConditionalData<string> (id_cdddd549ddf24caa98e599e12ae9bd4a).input)
            id_cdddd549ddf24caa98e599e12ae9bd4a.WireTo(id_f0164989439c44c69ad7d21790e8b6fe, "conditionMetOutput"); // (ConditionalData<string> (id_cdddd549ddf24caa98e599e12ae9bd4a).conditionMetOutput) -- [IDataFlow<string>] --> (Collection<string> (id_f0164989439c44c69ad7d21790e8b6fe).element)
            id_f0164989439c44c69ad7d21790e8b6fe.WireTo(wiringLinesOutputConnector, "listOutput"); // (Collection<string> (id_f0164989439c44c69ad7d21790e8b6fe).listOutput) -- [IDataFlow<List<string>>] --> (@DataFlowConnector<List<string>> (wiringLinesOutputConnector).dataInput)
            // END AUTO-GENERATED WIRING FOR GenerateCode.xmind
            
            // BEGIN MANUAL INSTANTIATIONS FOR GenerateCode.xmind
            // END MANUAL INSTANTIATIONS FOR GenerateCode.xmind
            
            // BEGIN MANUAL WIRING FOR GenerateCode.xmind
            // END MANUAL WIRING FOR GenerateCode.xmind
        }
    }
}
