using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// Splits an input string into lines, then segments it using defined lambdas to identify start and stop lines. Segments are then bundled with their start and stop lines and sent out.
    /// </summary>
    public class MultilineSegmenter : IDataFlow<string> // input
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        /// <summary>
        /// Required - the condition to check whether a given line is a start line.
        /// </summary>
        public Func<string, bool> IsStartLine { get; set; }
        /// <summary>
        /// Required - the condition to check whether a given line is a stop line.
        /// </summary>
        public Func<string, bool> IsStopLine { get; set; }

        /// <summary>
        /// Optional - allows a transformation to the line before adding it to the segment.l
        /// </summary>
        public Func<string, string> ProcessLine { get; set; }

        // Private fields
        private string _inputString = "";

        private enum SegmentationMode
        {
            LookingForStartLine,
            LookingForStopLine
        }

        // Ports
        /// <summary>
        /// Contains a collection of (startLine, segmentLines, stopLine) tuples.
        /// </summary>
        private IDataFlow<List<Tuple<string, List<string>, string>>> segments; 

        // IDataFlow<string> implementation
        string IDataFlow<string>.Data
        {
            get => _inputString;
            set
            {

            }
        }

        // Methods

        private List<Tuple<string, List<string>, string>> CreateSegments(string input, Func<string, bool> isStartLine, Func<string, bool> isStopLine)
        {
            if (string.IsNullOrEmpty(input) || isStartLine == null || IsStopLine == null) return default;

            var segments = new List<Tuple<string, List<string>, string>>();

            var currentSegment = new List<string>();
            var allLines = input.Split(new [] { Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries).ToList();

            var mode = SegmentationMode.LookingForStartLine;
            var currentStartLine = "";

            foreach (var line in allLines)
            {
                if (mode == SegmentationMode.LookingForStartLine)
                {
                    if (isStartLine(line))
                    {
                        currentStartLine = line;
                    }
                }
                else if (mode == SegmentationMode.LookingForStopLine)
                {
                    if (isStopLine(line))
                    {
                        var currentStopLine = line;
                        var segmentBundle = Tuple.Create(currentStartLine, currentSegment, currentStopLine);
                        segments.Add(segmentBundle);

                        mode = SegmentationMode.LookingForStartLine;
                    }
                    else
                    {
                        currentSegment.Add(line);
                    }
                }
            }

            return segments;

        }
    }
}
