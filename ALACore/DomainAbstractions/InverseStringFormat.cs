using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;
using ProgrammingParadigms;
using System.Text.RegularExpressions;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>Parses a string and outputs its parameters in a dictionary, given a format.</para>
    /// </summary>
    public class InverseStringFormat : IDataFlow<string>
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string Format { get; set; } = "";

        // Private fields
        private string _lastUnparsedInput = "";
        private Dictionary<string, string> extractedParameters = new Dictionary<string, string>();

        // Ports
        private IDataFlow<Dictionary<string, string>> extractedParametersOutput;

        /// <summary>
        /// <para>Parses a string and outputs its parameters in a dictionary, given a format.</para>
        /// </summary>
        public InverseStringFormat()
        {

        }

        public static Dictionary<string, string> GetInverseStringFormat(string input, string format)
        {
            List<string> names = (from Match match in Regex.Matches(format, @"(?<={)[^{}]+(?=})") select match.Value).ToList();
            List<string> values = new List<string>();

            foreach (var name in names)
            {
                string currentParam = $"{{{name}}}";
                string before = format.Substring(0, format.IndexOf(currentParam));
                string after = format.Substring(format.IndexOf(currentParam) + currentParam.Length);
                string escapeRegex = @"(?=[\\\*\+\?\|\{\}\[\]\(\)\^\$\.\#])";
                string regexFormat = $"(?<=({Regex.Replace(before, escapeRegex, "\\")})).*?(?=({Regex.Replace(after, escapeRegex, "\\")}))";
                string patternWithOtherParamsReplaced = Regex.Replace(regexFormat, @"\\{[\w\d_]+?\\}", ".*?");
                var match = Regex.Match(input, patternWithOtherParamsReplaced);
                values.Add(match.Value);
            }

            Dictionary<string, string> nameValueDictionary = new Dictionary<string, string>();
            for (int i = 0; i < names.Count; i++)
            {
                nameValueDictionary[names[i]] = values[i].Trim();
            }

            return nameValueDictionary;
        }

        // IDataFlow<string> implementation
        string IDataFlow<string>.Data
        {
            get => _lastUnparsedInput;
            set
            {
                _lastUnparsedInput = value;

                if (!string.IsNullOrEmpty(Format))
                {
                    extractedParameters = GetInverseStringFormat(_lastUnparsedInput, Format);

                    if (extractedParametersOutput != null) extractedParametersOutput.Data = extractedParameters; 
                }
            }
        }


    }
}
