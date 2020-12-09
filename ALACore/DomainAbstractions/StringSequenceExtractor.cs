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
    /// <para>Parses a string to extract a sequence of strings, and has the ability to take into account bracket balancing.
    /// If bracket balancing is considered, then this assumes that all brackets are balanced and in the correct order.</para>
    /// <para>Ports:</para>
    /// <para>1. IDataFlow&lt;string&gt; unparsedInput:</para>
    /// <para>2. IDataFlow&lt;List&lt;string&gt;&gt; sequenceOutput:</para>
    /// </summary>
    public class StringSequenceExtractor : IDataFlow<string> // unparsedInput
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public char Separator { get; set; } = ' ';
        public bool ConsiderBracketBalance { get; set; } = false;
        public string StartingBrackets { get; set; } = ""; // E.g. "{{(" to start with '{' balance at 2, and '(' balance at 1
        public bool TrimEntries { get; set; } = true;


        // Private fields
        private string _unparsed;
        private List<string> _sequence;

        private Dictionary<char, char> _closedToOpened = new Dictionary<char, char>()
        {
            { ')', '(' },
            { '}', '{' },
            { ']', '[' },
            { '>', '<' }
        };

        // Ports
        private IDataFlow<List<string>> sequenceOutput;
        
        // IDataFlow<string> implementation
        string IDataFlow<string>.Data
        {
            get => _unparsed;
            set
            {
                _unparsed = value;
                _sequence = Parse(_unparsed);
                if (sequenceOutput != null) sequenceOutput.Data = _sequence;
            }

        }

        // Methods
        private Dictionary<char, int> GetStartingBalances()
        {
            var bracketBalances = new Dictionary<char, int>()
            {
                { '(', 0 },
                { '{', 0 },
                { '[', 0 },
                { '<', 0 }
            };

            foreach (var startingBracket in StartingBrackets)
            {
                if (bracketBalances.ContainsKey(startingBracket)) bracketBalances[startingBracket]++;
            }

            return bracketBalances;
        }

        private bool AtStartingBalance(Dictionary<char, int> currentBalances)
        {
            var startingBalances = GetStartingBalances();

            foreach (var bracket in startingBalances.Keys)
            {
                if (currentBalances.ContainsKey(bracket) && currentBalances[bracket] != startingBalances[bracket]) return false;
            }

            return true;
        }

        private List<string> Parse(string unparsedString)
        {
            var seq = new List<string>();
            var sb = new StringBuilder();

            var startingBalances = GetStartingBalances();
            var bracketBalances = new Dictionary<char, int>()
            {
                { '(', 0 },
                { '{', 0 },
                { '[', 0 },
                { '<', 0 }
            };

            var latest = sb.ToString();

            foreach (var c in unparsedString)
            {
                if (bracketBalances.ContainsKey(c))
                {
                    bracketBalances[c]++;
                    if (AtStartingBalance(bracketBalances)) continue;
                }
                else if (_closedToOpened.ContainsKey(c))
                {
                    var openBracket = _closedToOpened[c];
                    if (bracketBalances.ContainsKey(openBracket) && bracketBalances[openBracket] > startingBalances[openBracket])
                    {
                        bracketBalances[openBracket]--;
                        if (bracketBalances[openBracket] < startingBalances[openBracket]) break;
                    }
                }
                else if (c == Separator && AtStartingBalance(bracketBalances))
                {
                    latest = sb.ToString();

                    if (TrimEntries) latest = latest.Trim();
                    seq.Add(latest);

                    sb.Clear();

                    continue; // Don't want the separator in the sequence
                }
                 
                sb.Append(c);
            }

            latest = sb.ToString();

            if (TrimEntries) latest = latest.Trim();
            seq.Add(latest);

            sb.Clear();

            return seq;
        }
        
        /// <summary>
        /// <para></para>
        /// </summary>
        public StringSequenceExtractor()
        {
            
        }
    }
}
