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
    /// <para>Reads in and outputs a list of programming paradigms from a text file.</para>
    /// <para>Ports:</para>
    /// <para>1. IDataFlow&lt;string&gt; filePathInput:</para>
    /// <para>2. IDataFlow&lt;List&lt;string&gt;&gt; templatesOutput:</para>
    /// </summary>
    public class ParseProgrammingParadigmTemplates : IDataFlow<string>
    {
        // Public fields and properties
        public string InstanceName = "Default";

        // Private fields
        private string alaProgrammingParadigmFilePath = "";

        // Input instances
        private FileReader readLocalALAProgrammingParadigmTemplateFile = new FileReader() { InstanceName = "readLocalALAProgrammingParadigmTemplateFile" };

        // Output instances
        private Apply<List<string>, List<string>> sendOutput = new Apply<List<string>, List<string>>() { InstanceName = "sendOutput", Lambda = s => s };

        // Ports
        private IDataFlow<List<string>> templatesOutput;

        public ParseProgrammingParadigmTemplates()
        {
            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR ParseProgrammingParadigmTemplates.xmind
            Apply<List<string>,List<string>> id_8ad808c3ab394c0a8976bc6ec999b721 = new Apply<List<string>,List<string>>() { InstanceName = "Default", Lambda = list => list.Select(s => s.Replace(" ","")).ToList() };
            Apply<List<string>,List<string>> id_ff1f4c6f01014474bbf43216cba732f5 = new Apply<List<string>,List<string>>() { InstanceName = "Default", Lambda = list => list.ToHashSet().ToList() };
            Apply<string,List<string>> id_bdbc349b4d474acca92d4100a5c577d1 = new Apply<string,List<string>>() { InstanceName = "Default", Lambda = s => s.Split(new[] { Environment.NewLine },StringSplitOptions.RemoveEmptyEntries).ToList() };
            // END AUTO-GENERATED INSTANTIATIONS FOR ParseProgrammingParadigmTemplates.xmind

            // BEGIN AUTO-GENERATED WIRING FOR ParseProgrammingParadigmTemplates.xmind
            readLocalALAProgrammingParadigmTemplateFile.WireTo(id_bdbc349b4d474acca92d4100a5c577d1, "fileContentOutput"); // (@FileReader (readLocalALAProgrammingParadigmTemplateFile).fileContentOutput) -- [IDataFlow<string>] --> (Apply<string,List<string>> (id_bdbc349b4d474acca92d4100a5c577d1).input)
            id_bdbc349b4d474acca92d4100a5c577d1.WireTo(id_ff1f4c6f01014474bbf43216cba732f5, "output"); // (Apply<string,List<string>> (id_bdbc349b4d474acca92d4100a5c577d1).output) -- [IDataFlow<List<string>>] --> (Apply<List<string>,List<string>> (id_ff1f4c6f01014474bbf43216cba732f5).input)
            id_ff1f4c6f01014474bbf43216cba732f5.WireTo(id_8ad808c3ab394c0a8976bc6ec999b721, "output"); // (Apply<List<string>,List<string>> (id_ff1f4c6f01014474bbf43216cba732f5).output) -- [IDataFlow<List<string>>] --> (Apply<List<string>,List<string>> (id_8ad808c3ab394c0a8976bc6ec999b721).input)
            id_8ad808c3ab394c0a8976bc6ec999b721.WireTo(sendOutput, "output"); // (Apply<List<string>,List<string>> (id_8ad808c3ab394c0a8976bc6ec999b721).output) -- [IDataFlow<List<string>>] --> (@Apply<List<string>,List<string>> (sendOutput).input)
            // END AUTO-GENERATED WIRING FOR ParseProgrammingParadigmTemplates.xmind
        }

        private void PostWiringInitialize()
        {
            // Mapping to virtual ports
            Utilities.ConnectToVirtualPort(sendOutput, "output", templatesOutput);
        }

        // IDataFlow<string> implementation
        string IDataFlow<string>.Data
        {
            get => alaProgrammingParadigmFilePath;
            set
            {
                alaProgrammingParadigmFilePath = value;
                (readLocalALAProgrammingParadigmTemplateFile as IDataFlow<string>).Data = alaProgrammingParadigmFilePath;
            }
        }
    }
}
