using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ProgrammingParadigms
{
    public static class Enums
    {
        // Enums
        public enum ALALayer
        {
            Libraries = 0,
            ProgrammingParadigms = 1,
            DomainAbstractions = 2,
            Application = 3
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum ParameterType
        {
            Constructor = 0,
            Property = 1
        }

        [Flags]
        public enum DiagramMode
        {
            None = 0,
            Idle = 1,
            TextEditing = 1 << 1,
            SingleNodeSelect = 1 << 2,
            MultiNodeSelect = 1 << 3,
            DragSelect = 1 << 4,
            IdleSelected = 1 << 5,
            Any = 1 << 6,
            Paused = 1 << 7,
            SingleConnectionSelect = 1 << 8,
            MovingConnection = 1 << 9,
            AwaitingPortSelection = 1 << 10,
            AddingCrossConnection = 1 << 11,
            Panning = 1 << 12
        }
    }
}
