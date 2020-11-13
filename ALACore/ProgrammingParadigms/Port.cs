using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;
using Newtonsoft.Json.Linq;

namespace ProgrammingParadigms
{
    public class Port
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Name { get; set; } = "default";
        public string FullName
        {
            get => !string.IsNullOrEmpty(Type) ? $"{Type} {Name}" : Name;
        }
        public bool IsInputPort { get; set; }
        public int Index { get; set; } = 0;
        public List<string> ConnectionIds = new List<string>();

        public override string ToString()
        {
            return FullName;
        }

        public string Serialise()
        {
            JObject obj = new JObject();
            obj["Type"] = Type;
            obj["Name"] = Name;
            obj["IsInputPort"] = IsInputPort;
            obj["Index"] = Index;
            obj["ConnectionIds"] = new JArray(ConnectionIds);

            return obj.ToString();
        }

        public void Deserialise(string memento)
        {
            var obj = JObject.Parse(memento);
            Deserialise(obj);
        }

        public void Deserialise(JObject obj)
        {
            try
            {
                Type = obj.GetValue("Type")?.ToString() ?? Type;
                Name = obj.GetValue("Name")?.ToString() ?? Name;
                IsInputPort = obj.ContainsKey("IsInputPort") ? bool.Parse(obj.GetValue("IsInputPort").ToString()) : IsInputPort;
                Index = obj.ContainsKey("IsInputPort") ? int.Parse(obj["Index"].ToString()) : Index;
                ConnectionIds = obj.ContainsKey("ConnectionIds") ? obj.GetValue("ConnectionIds").Select(jt => jt.ToString()).ToList() : ConnectionIds;
            }
            catch (Exception e)
            {
                Logging.Log($"Port deserialisation failed from {obj.ToString()}.{Environment.NewLine}Reason: {e}");
            }
        }

        public Port(string memento = "", Port source = null)
        {
            if (!string.IsNullOrEmpty(memento))
            {
                try
                {
                    Deserialise(memento);
                }
                catch (Exception e)
                {

                }
            }

            if (source != null)
            {
                Type = source.Type;
                Name = source.Name;
                IsInputPort = source.IsInputPort;
            }

            Id = Utilities.GetUniqueId();
        }
    }
}
