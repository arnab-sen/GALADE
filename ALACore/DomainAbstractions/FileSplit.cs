using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>Splits a file into two string halves at the first line that starts with a given match string.</para>
    /// <para>This ignores leading whitespace in the line of a potential match, e.g. if Match = "MATCH", then "MATCH..." and " MATCH..." will both be matches.</para>
    /// <para>Ports:</para>
    /// <para>1. IDataFlow&lt;string&gt; fileContentsInput:</para>
    /// <para>2. IDataFlowB&lt;string&gt; matchInput:</para>
    /// <para>3. IDataFlow&lt;string&gt; upperHalfOutput:</para>
    /// <para>4. IDataFlow&lt;string&gt; lowerHalfOutput:</para>
    /// </summary>
    public class FileSplit : IDataFlow<string>
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        /// <summary>
        /// The string to match.
        /// </summary>
        public string Match { get; set; }
        /// <summary>
        /// Whether the second half should start before or after the match. In other words, whether to exclude the match from the halves.
        /// </summary>
        public bool SplitAfterMatch { get; set; } = true;

        // Private fields
        private string fileContents;
        
        // Ports
        private IDataFlowB<string> matchInput;
        private IDataFlow<string> upperHalfOutput;
        private IDataFlow<string> lowerHalfOutput;

        // Methods
        Tuple<string, string> SplitFile(string contents, string match)
        {
            var upperSB = new StringBuilder();
            var lowerSB = new StringBuilder();

            var lines = contents.Split(new[] {Environment.NewLine}, StringSplitOptions.None);

            bool matchFound = false;

            foreach (string line in lines)
            {
                if (!matchFound)
                {
                    if (!line.TrimStart().StartsWith(match))
                    {
                        upperSB.AppendLine(line);
                    }
                    else
                    {
                        matchFound = true;

                        if (SplitAfterMatch)
                        {
                            upperSB.AppendLine(line);
                        }
                        else
                        {
                            lowerSB.AppendLine(line);
                        }
                    }
                }
                else
                {
                    lowerSB.AppendLine(line);
                }
            }

            return Tuple.Create(upperSB.ToString(), lowerSB.ToString());
        }
        
    // IDataFlow<string> implementation
        string IDataFlow<string>.Data
        {
            get => fileContents;
            set
            {
                if (matchInput != null) Match = matchInput.Data;

                try
                {
                    if (!string.IsNullOrWhiteSpace(Match))
                    {
                        fileContents = value;

                        var split = SplitFile(fileContents, Match);

                        if (upperHalfOutput != null) upperHalfOutput.Data = split.Item1;
                        if (lowerHalfOutput != null) lowerHalfOutput.Data = split.Item2;
                    }
                }
                catch (Exception e)
                {
                    Logging.Log($"Failed to split file in FileSplit for match \"{Match}\".\nException: {e}");
                }
            }
        }
        
        /// <summary>
        /// <para>Splits a file into two string halves at the first occurrence of a given Match string.</para>
        /// </summary>
        public FileSplit()
        {
            
        }
    }
}
