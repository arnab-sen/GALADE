using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// A model of an abstraction instance, namely its constructor arguments and public fields and properties, as well as its ports.
    /// </summary>
    public class AbstractionModel
    {
        // Public field and properties
        public string InstanceName { get; set; } = "Default";
        public string Type { get; set; } = "Object";
        public string Name { get; set; } = "";

        // Private fields
        private Dictionary<string, string> _constructorArgs = new Dictionary<string, string>(); // name : value
        private Dictionary<string, string> _fields = new Dictionary<string, string>(); // name : value
        private Dictionary<string, string> _properties = new Dictionary<string, string>(); // name : value
        private Dictionary<string, Port> _implementedPorts = new Dictionary<string, Port>(); // name : type
        private Dictionary<string, Port> _acceptedPorts = new Dictionary<string, Port>(); // name : type
        private Dictionary<string, string> _generics = new Dictionary<string, string>(); // name : type
        private Dictionary<string, string> _types = new Dictionary<string, string>(); // typeName : type. This contains the types of fields, properties, and constructor args
        private string _documentation = "";

        // Ports

        // Methods
        public List<KeyValuePair<string, string>> GetConstructorArgs() => _constructorArgs.ToList();
        public List<KeyValuePair<string, string>> GetFields() => _fields.ToList();
        public List<KeyValuePair<string, string>> GetProperties() => _properties.ToList();
        public List<Port> GetImplementedPorts() => _implementedPorts.Values.ToList();
        public List<Port> GetAcceptedPorts() => _acceptedPorts.Values.ToList();
        public List<KeyValuePair<string, string>> GetGenerics() => _generics.ToList();
        public string GetType(string type) => _types.ContainsKey(type) ? _types[type] : "undefined";
        public string GetDocumentation() => _documentation;

        public void AddConstructorArg(string name, string initialValue = "", string type = "undefined")
        {
            _constructorArgs[name] = initialValue;
            _types[name] = type;
        }

        public void AddField(string name, string initialValue = "", string type = "undefined")
        {
            _fields[name] = initialValue;
            _types[name] = type;
        }

        public void AddProperty(string name, string initialValue = "", string type = "undefined")
        {
            _properties[name] = initialValue;
            _types[name] = type;
        }

        public void AddImplementedPort(string type, string name)
        {
            _implementedPorts[name] = new Port()
            {
                Type = type,
                Name = name,
                IsInputPort = true
            };
        }

        public void AddAcceptedPort(string type, string name)
        {
            _acceptedPorts[name] = new Port()
            {
                Type = type,
                Name = name,
                IsInputPort = false
            };;
        }

        public void AddGeneric(string generic, string initialValue = "")
        {
            _generics[generic] = initialValue;
        }

        public void AddDocumentation(string documentation)
        {
            _documentation = documentation;
        }

        /// <summary>
        /// Finds and sets a value in the instance. This will fail if no field, property, or arg has the given name.
        /// </summary>
        /// <param name="name">The variable name, e.g. "Source"</param>
        /// <param name="value">The code literal, e.g. "new MyClass()"</param>
        public void SetValue(string name, string value)
        {
            if (_constructorArgs.ContainsKey(name))
            {
                _constructorArgs[name] = value;
            }
            
            if (_fields.ContainsKey(name))
            {
                _fields[name] = value;
            }
            
            if (_properties.ContainsKey(name))
            {
                _properties[name] = value;
            }
        }

        public void SetImplementedPort(string type, string name)
        {
            if (_implementedPorts.ContainsKey(name))
            {
                _implementedPorts[name].Type = type;
                _implementedPorts[name].Name = name;
            }
            else
            {
                AddImplementedPort(type, name);
            }
        }

        public void SetAcceptedPort(string type, string name)
        {
            if (_acceptedPorts.ContainsKey(name))
            {
                _acceptedPorts[name].Type = type;
                _acceptedPorts[name].Name = name;
            }
            else
            {
                AddAcceptedPort(type, name);
            }
        }

        public void SetGeneric(string identifier, string type)
        {
            if (_generics.ContainsKey(identifier))
            {
                _generics[identifier] = type;
                UpdateGeneric(identifier, type);
            }
        }

        public void CloneFrom(AbstractionModel source)
        {
            Type = source.Type;
            Name = source.Name;
            _documentation = source._documentation;

            _implementedPorts.Clear();
            foreach (var pair in source._implementedPorts)
            {
                _implementedPorts[pair.Key] = pair.Value;
            }

            _acceptedPorts.Clear();
            foreach (var pair in source._acceptedPorts)
            {
                _acceptedPorts[pair.Key] = pair.Value;
            }

            _fields.Clear();
            foreach (var pair in source._fields)
            {
                _fields[pair.Key] = pair.Value;
            }

            _constructorArgs.Clear();
            foreach (var pair in source._constructorArgs)
            {
                _constructorArgs[pair.Key] = pair.Value;
            }

            _fields.Clear();
            foreach (var pair in source._fields)
            {
                _fields[pair.Key] = pair.Value;
            }

            _properties.Clear();
            foreach (var pair in source._properties)
            {
                _properties[pair.Key] = pair.Value;
            }

            _types.Clear();
            foreach (var pair in source._types)
            {
                _types[pair.Key] = pair.Value;
            }
        }

        /// <summary>
        /// Returns the value of the first occurrence of a given variable name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetValue(string name)
        {
            if (_constructorArgs.ContainsKey(name))
            {

                return _constructorArgs[name];
            }
            
            if (_fields.ContainsKey(name))
            {
                return _fields[name];
            }
            
            if (_properties.ContainsKey(name))
            {
                return _properties[name];
            }

            if (_generics.ContainsKey(name))
            {
                return _generics[name];
            }

            return "";
        }

        private string GetValueAndDict(string name, out Dictionary<string, string> foundAt)
        {
            if (_constructorArgs.ContainsKey(name))
            {
                foundAt = _constructorArgs;
                return _constructorArgs[name];
            }
            
            if (_fields.ContainsKey(name))
            {
                foundAt = _fields;
                return _fields[name];
            }
            
            if (_properties.ContainsKey(name))
            {
                foundAt = _properties;
                return _properties[name];
            }

            if (_generics.ContainsKey(name))
            {
                foundAt = _generics;
                return _generics[name];
            }

            foundAt = null;
            return "";
        }

        public void RemoveValue(string key)
        {
            if (_constructorArgs.ContainsKey(key))
            {
                _constructorArgs.Remove(key);
            }
            
            if (_fields.ContainsKey(key))
            {
                _fields.Remove(key);
            }
            
            if (_properties.ContainsKey(key))
            {
                _properties.Remove(key);
            }

            if (_generics.ContainsKey(key))
            {
                _generics.Remove(key);
            }
        }

        public void UpdateGeneric(string name, string newType)
        {
            var startRegex = $@"(?<=(<\s*)){name}";
            var midRegex = $@"(?<=(,\s*)){name}(?=\s*,)";
            var endRegex = $@"(?<=(,\s*)){name}(?=\s*>)";

            Func<string, bool> genericMatch = s =>
            {
                return Regex.IsMatch(s, startRegex) |
                       Regex.IsMatch(s, midRegex) |
                       Regex.IsMatch(s, endRegex);
            };

            // Apply to non-port variables
            var withGeneric = GetNameMatches(genericMatch);

            foreach (var match in withGeneric)
            {
                Dictionary<string, string> dict;
                var value = GetValueAndDict(match, out dict);
                if (dict == null) continue;


                string regex = "";
                if (Regex.IsMatch(match, startRegex))
                {
                    regex = startRegex;
                }
                else if (Regex.IsMatch(match, midRegex))
                {
                    regex = midRegex;
                }
                else if (Regex.IsMatch(match, endRegex))
                {
                    regex = endRegex;
                }

                if (!string.IsNullOrEmpty(regex))
                {
                    RemoveValue(match);
                    dict[Regex.Replace(match, regex, newType)] = value;
                }
            }

            // Apply to port variables
            var portsWithGeneric = new List<string>();

            foreach (var port in GetImplementedPorts())
            {
                if (genericMatch(port.Type)) portsWithGeneric.Add(port.Name);
            }

            foreach (var port in GetAcceptedPorts())
            {
                if (genericMatch(port.Type)) portsWithGeneric.Add(port.Name);
            }

            foreach (var portName in portsWithGeneric)
            {
                string regex = "";
                if (Regex.IsMatch(portName, startRegex))
                {
                    regex = startRegex;
                }
                else if (Regex.IsMatch(portName, midRegex))
                {
                    regex = midRegex;
                }
                else if (Regex.IsMatch(portName, endRegex))
                {
                    regex = endRegex;
                }

                if (_implementedPorts.ContainsKey(portName))
                {
                    var type = Regex.Replace(_implementedPorts[portName].Type, regex, newType);
                    SetImplementedPort(type, portName);
                }
                else
                {
                    var type = Regex.Replace(_acceptedPorts[portName].Type, regex, newType);
                    SetAcceptedPort(type, portName);
                }
            }
        }

        private List<string> GetNameMatches(Func<string, bool> matchFunc)
        {
            var mappings = new List<Dictionary<string, string>>()
            {
                _constructorArgs, _fields, _properties, _generics
            };

            var result = new List<string>();

            foreach (var dict in mappings)
            {
                result.AddRange(dict.Keys.Where(matchFunc));
            }


            return result.ToHashSet().ToList();
        }

        public AbstractionModel()
        {

        }
    }
}
