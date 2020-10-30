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
        public string VariableName { get; set; } = "";

        // Private fields
        private Dictionary<string, string> _constructorArgs = new Dictionary<string, string>(); // name : value
        private Dictionary<string, string> _fields = new Dictionary<string, string>(); // name : value
        private Dictionary<string, string> _properties = new Dictionary<string, string>(); // name : value
        private Dictionary<string, string> _implementedPorts = new Dictionary<string, string>(); // name : type
        private Dictionary<string, string> _acceptedPorts = new Dictionary<string, string>(); // name : type
        private Dictionary<string, string> _generics = new Dictionary<string, string>(); // name : type

        // Ports

        // Methods
        public List<KeyValuePair<string, string>> GetConstructorArgs() => _constructorArgs.ToList();
        public List<KeyValuePair<string, string>> GetFields() => _fields.ToList();
        public List<KeyValuePair<string, string>> GetProperties() => _properties.ToList();
        public List<KeyValuePair<string, string>> GetImplementedPorts() => _implementedPorts.ToList();
        public List<KeyValuePair<string, string>> GetAcceptedPorts() => _acceptedPorts.ToList();
        public List<KeyValuePair<string, string>> GetGenerics() => _generics.ToList();

        public void AddConstructorArg(string name, string initialValue = "")
        {
            _constructorArgs[name] = initialValue;
        }

        public void AddField(string name, string initialValue = "")
        {
            _fields[name] = initialValue;
        }

        public void AddProperty(string name, string initialValue = "")
        {
            _properties[name] = initialValue;
        }

        public void AddImplementedPort(string type, string name)
        {
            _implementedPorts[name] = type;
        }

        public void AddAcceptedPort(string type, string name)
        {
            _acceptedPorts[name] = type;
        }

        public void AddGeneric(string generic, string initialValue = "")
        {
            _generics[generic] = initialValue;
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

        public void SetImplementedPort(string type, string name) => _implementedPorts[name] = type;
        public void SetAcceptedPort(string type, string name) => _acceptedPorts[name] = type;

        public void SetGeneric(string identifier, string type)
        {
            if (_generics.ContainsKey(identifier))
            {
                _generics[identifier] = type;
                UpdateGeneric(identifier, type);
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

            if (_implementedPorts.ContainsKey(name))
            {
                return _implementedPorts[name];
            }

            if (_acceptedPorts.ContainsKey(name))
            {
                return _acceptedPorts[name];
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

            if (_implementedPorts.ContainsKey(name))
            {
                foundAt = _implementedPorts;
                return _implementedPorts[name];
            }

            if (_acceptedPorts.ContainsKey(name))
            {
                foundAt = _acceptedPorts;
                return _acceptedPorts[name];
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

            foreach (var portPair in _implementedPorts)
            {
                if (genericMatch(portPair.Value)) portsWithGeneric.Add(portPair.Key);
            }

            foreach (var portPair in _acceptedPorts)
            {
                if (genericMatch(portPair.Value)) portsWithGeneric.Add(portPair.Key);
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
                    _implementedPorts[portName] = Regex.Replace(_implementedPorts[portName], regex, newType);
                }
                else
                {
                    _acceptedPorts[portName] = Regex.Replace(_acceptedPorts[portName], regex, newType);
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
