using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;

namespace Application
{
    /// <summary>
    /// <para>Inserts a sequence of lines into a code file between a line matching a StartLandmark and another matching an EndLandmark.</para>
    /// <para>Ports:</para>
    /// <para>1. IEvent start:</para>
    /// <para>2. IDataFlow&lt;List&lt;string&gt;&gt; linesInput:</para>
    /// <para>3. IDataFlow&lt;string&gt; fileContentsInput:</para>
    /// <para>4. IDataFlow&lt;string&gt; newFileContentsOutput:</para>
    /// </summary>
    public class InsertFileCodeLines : IEvent, IDataFlow<List<string>>, IDataFlow<string>
    {
        // Public fields and properties
        public string InstanceName = "Default";

        public string StartLandmark
        {
            set => (startLandmarkConnector as IDataFlow<string>).Data = value;
        }
        public string EndLandmark 
        {
            set => (endLandmarkConnector as IDataFlow<string>).Data = value;
        }
        public string Indent { get; set; } = "";

        // Private fields
        private DataFlowConnector<string> startLandmarkConnector = new DataFlowConnector<string>() { InstanceName = "startLandmarkConnector" };
        private DataFlowConnector<string> endLandmarkConnector = new DataFlowConnector<string>() { InstanceName = "endLandmarkConnector" };
        
        // Ports
        private IDataFlow<string> newFileContentsOutput;
        
        // Input instances
        private DataFlowConnector<List<string>> linesInputConnector = new DataFlowConnector<List<string>>() { InstanceName = "linesInputConnector" };
        private DataFlowConnector<string> fileContentsInputConnector = new DataFlowConnector<string>() { InstanceName = "filePathInput" };
        private EventConnector startConnector = new EventConnector() { InstanceName = "startConnector" };
        
        // Output instances
        private Apply<string, string> newFileContentsOutputConnector = new Apply<string, string>() { Lambda = input => input };
        
        // IDataFlow<List<string>> implementation
        List<string> IDataFlow<List<string>>.Data { get { return default; } set { (linesInputConnector as IDataFlow<List<string>>).Data = value; } }

        // IDataFlow<string>
        string IDataFlow<string>.Data
        {
            get => fileContentsInputConnector.Data;
            set => (fileContentsInputConnector as IDataFlow<string>).Data = value;
        }

        // IEvent implementation
        void IEvent.Execute()
        {
            (startConnector as IEvent).Execute();
        }
        
        // PostWiringInitialize
        private void PostWiringInitialize()
        {
            // Mapping to virtual ports
            Utilities.ConnectToVirtualPort(newFileContentsOutputConnector, "output", newFileContentsOutput);
            
            // IDataFlowB and IEventB event handlers
            // Send out initial values
            // (instanceNeedingInitialValue as IDataFlow<T>).Data = defaultValue;
        }
        
        /// <summary>
        /// <para></para>
        /// </summary>
        public InsertFileCodeLines()
        {
            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR InsertFileCodeLines.xmind
            Apply<List<string>,string> id_f307f8006532496184c67f270e1ddb03 = new Apply<List<string>,string>() { InstanceName = "Default", Lambda = list => { var sb = new StringBuilder(); foreach (string line in list) { var newStr = line.Replace(Environment.NewLine, Environment.NewLine + Indent); sb.AppendLine(Indent + newStr); } return sb.ToString(); } };
            ConvertToEvent<string> id_3211377b14454d869f561996fa3c1a1d = new ConvertToEvent<string>() { InstanceName = "Default" };
            Data<string> id_40ab4f737292414e8585b145ba849f3d = new Data<string>() { InstanceName = "Default" };
            DataFlowConnector<string> id_89da89b45693446e9ed8ce1a5b10cb25 = new DataFlowConnector<string>() { InstanceName = "Default" };
            DataFlowConnector<string> id_a3730dc22a3e4e7e83ac8993f5a34db4 = new DataFlowConnector<string>() { InstanceName = "Default" };
            DataFlowConnector<string> id_d76b7961c34f4f3ab58073f7f9f044b9 = new DataFlowConnector<string>() { InstanceName = "Default" };
            DataFlowConnector<string> id_ef3b4163b2fe4603afcb5110ca17dfdd = new DataFlowConnector<string>() { InstanceName = "Default" };
            FileSplit id_6f631eb613fe476ea3e23aecd9c7bb6b = new FileSplit() { InstanceName = "Default", SplitAfterMatch = false };
            FileSplit id_cd9ca6174c044816b141ab0c641d06d1 = new FileSplit() { InstanceName = "Default", SplitAfterMatch = true };
            Operation<string> combineCodeSegments = new Operation<string>() { InstanceName = "combineCodeSegments", Lambda = ops => ops[0] + ops[1] + ops[2] };
            // END AUTO-GENERATED INSTANTIATIONS FOR InsertFileCodeLines.xmind
            
            // BEGIN AUTO-GENERATED WIRING FOR InsertFileCodeLines.xmind
            linesInputConnector.WireTo(id_f307f8006532496184c67f270e1ddb03, "fanoutList"); // (@DataFlowConnector<List<string>> (linesInputConnector).fanoutList) -- [IDataFlow<List<string>>] --> (Apply<List<string>,string> (id_f307f8006532496184c67f270e1ddb03).input)
            id_f307f8006532496184c67f270e1ddb03.WireTo(id_ef3b4163b2fe4603afcb5110ca17dfdd, "output"); // (Apply<List<string>,string> (id_f307f8006532496184c67f270e1ddb03).output) -- [IDataFlow<string>] --> (DataFlowConnector<string> (id_ef3b4163b2fe4603afcb5110ca17dfdd).dataInput)
            startConnector.WireTo(id_40ab4f737292414e8585b145ba849f3d, "fanoutList"); // (@EventConnector (startConnector).fanoutList) -- [IEvent] --> (Data<string> (id_40ab4f737292414e8585b145ba849f3d).start)
            id_40ab4f737292414e8585b145ba849f3d.WireTo(fileContentsInputConnector, "inputDataB"); // (Data<string> (id_40ab4f737292414e8585b145ba849f3d).inputDataB) -- [IDataFlowB<string>] --> (@DataFlowConnector<string> (fileContentsInputConnector).returnDataB)
            id_40ab4f737292414e8585b145ba849f3d.WireTo(id_89da89b45693446e9ed8ce1a5b10cb25, "dataOutput"); // (Data<string> (id_40ab4f737292414e8585b145ba849f3d).dataOutput) -- [IDataFlow<string>] --> (DataFlowConnector<string> (id_89da89b45693446e9ed8ce1a5b10cb25).dataInput)
            id_89da89b45693446e9ed8ce1a5b10cb25.WireTo(id_cd9ca6174c044816b141ab0c641d06d1, "fanoutList"); // (DataFlowConnector<string> (id_89da89b45693446e9ed8ce1a5b10cb25).fanoutList) -- [IDataFlow<string>] --> (FileSplit (id_cd9ca6174c044816b141ab0c641d06d1).fileContentsInput)
            id_89da89b45693446e9ed8ce1a5b10cb25.WireTo(id_6f631eb613fe476ea3e23aecd9c7bb6b, "fanoutList"); // (DataFlowConnector<string> (id_89da89b45693446e9ed8ce1a5b10cb25).fanoutList) -- [IDataFlow<string>] --> (FileSplit (id_6f631eb613fe476ea3e23aecd9c7bb6b).fileContentsInput)
            id_89da89b45693446e9ed8ce1a5b10cb25.WireTo(id_3211377b14454d869f561996fa3c1a1d, "fanoutList"); // (DataFlowConnector<string> (id_89da89b45693446e9ed8ce1a5b10cb25).fanoutList) -- [IDataFlow<string>] --> (ConvertToEvent<string> (id_3211377b14454d869f561996fa3c1a1d).start)
            id_cd9ca6174c044816b141ab0c641d06d1.WireTo(startLandmarkConnector, "matchInput"); // (FileSplit (id_cd9ca6174c044816b141ab0c641d06d1).matchInput) -- [IDataFlowB<string>] --> (@DataFlowConnector<string> (startLandmarkConnector).returnDataB)
            id_cd9ca6174c044816b141ab0c641d06d1.WireTo(id_d76b7961c34f4f3ab58073f7f9f044b9, "upperHalfOutput"); // (FileSplit (id_cd9ca6174c044816b141ab0c641d06d1).upperHalfOutput) -- [IDataFlow<string>] --> (DataFlowConnector<string> (id_d76b7961c34f4f3ab58073f7f9f044b9).dataInput)
            id_6f631eb613fe476ea3e23aecd9c7bb6b.WireTo(endLandmarkConnector, "matchInput"); // (FileSplit (id_6f631eb613fe476ea3e23aecd9c7bb6b).matchInput) -- [IDataFlowB<string>] --> (@DataFlowConnector<string> (endLandmarkConnector).returnDataB)
            id_6f631eb613fe476ea3e23aecd9c7bb6b.WireTo(id_a3730dc22a3e4e7e83ac8993f5a34db4, "lowerHalfOutput"); // (FileSplit (id_6f631eb613fe476ea3e23aecd9c7bb6b).lowerHalfOutput) -- [IDataFlow<string>] --> (DataFlowConnector<string> (id_a3730dc22a3e4e7e83ac8993f5a34db4).dataInput)
            id_3211377b14454d869f561996fa3c1a1d.WireTo(combineCodeSegments, "eventOutput"); // (ConvertToEvent<string> (id_3211377b14454d869f561996fa3c1a1d).eventOutput) -- [IEvent] --> (Operation<string> (combineCodeSegments).startOperation)
            combineCodeSegments.WireTo(id_d76b7961c34f4f3ab58073f7f9f044b9, "operands"); // (Operation<string> (combineCodeSegments).operands) -- [IDataFlowB<string>] --> (DataFlowConnector<string> (id_d76b7961c34f4f3ab58073f7f9f044b9).returnDataB)
            combineCodeSegments.WireTo(id_ef3b4163b2fe4603afcb5110ca17dfdd, "operands"); // (Operation<string> (combineCodeSegments).operands) -- [IDataFlowB<string>] --> (DataFlowConnector<string> (id_ef3b4163b2fe4603afcb5110ca17dfdd).returnDataB)
            combineCodeSegments.WireTo(id_a3730dc22a3e4e7e83ac8993f5a34db4, "operands"); // (Operation<string> (combineCodeSegments).operands) -- [IDataFlowB<string>] --> (DataFlowConnector<string> (id_a3730dc22a3e4e7e83ac8993f5a34db4).returnDataB)
            combineCodeSegments.WireTo(newFileContentsOutputConnector, "operationResultOutput"); // (Operation<string> (combineCodeSegments).operationResultOutput) -- [IDataFlow<string>] --> (@Apply<string,string> (newFileContentsOutputConnector).input)
            // END AUTO-GENERATED WIRING FOR InsertFileCodeLines.xmind
            
            // BEGIN MANUAL INSTANTIATIONS FOR InsertFileCodeLines.xmind
            // END MANUAL INSTANTIATIONS FOR InsertFileCodeLines.xmind
            
            // BEGIN MANUAL WIRING FOR InsertFileCodeLines.xmind
            // END MANUAL WIRING FOR InsertFileCodeLines.xmind
        }
    }
}
