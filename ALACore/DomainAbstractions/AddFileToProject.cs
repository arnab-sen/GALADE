using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Libraries;
using ProgrammingParadigms;
using System.Xml;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>[Add documentation here]</para>
    /// <para>Ports:</para>
    /// <para></para>
    /// </summary>
    public class AddFileToProject : IDataFlow<string> // filePathInput
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        // Private fields
        private IDataFlowB<string> csprojPathInput;
        private IEvent complete;

        // Ports

        // Methods
        string IDataFlow<string>.Data
        {
            get => default;
            set
            {
                var sourcePath = value;

                var csprojPath = csprojPathInput.Data;
                var csprojSource = Utilities.ReadFileSafely(csprojPath);

                var csproj = new XmlDocument();
                csproj.LoadXml(csprojSource);
                var compileIncludes = csproj.GetElementsByTagName("Compile");

                var nodeBase = compileIncludes[0];
                var newInclusion = nodeBase.Clone();
                newInclusion.Attributes.GetNamedItem("Include").Value = sourcePath;
                nodeBase.ParentNode.AppendChild(newInclusion);

                try
                {
                    using (var writer = new XmlTextWriter(csprojPath, Encoding.UTF8)
                    {
                        Formatting = Formatting.Indented
                    })
                    {
                        csproj.WriteContentTo(writer);
                        writer.Flush();
                    }

                    complete?.Execute();
                }
                catch (Exception e)
                {
                    Logging.Log($"Could not write to file {sourcePath}");
                }
            }
        }

        public AddFileToProject()
        {

        }
    }
}