using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
        
        /// <summary>
        /// The object type without generics.
        /// </summary>
        public string Type { get; set; } = "Object";

        /// <summary>
        /// The object type, including any generics; this should always at least start with the contents of Type.
        /// </summary>
        public string FullType { get; set; } = "Object";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string SourceCode { get; set; }
        public string CodeFilePath { get; set; }

        // Private fields
        private Dictionary<string, string> _constructorArgs = new Dictionary<string, string>(); // name : value
        private Dictionary<string, string> _fields = new Dictionary<string, string>(); // name : value
        private Dictionary<string, string> _properties = new Dictionary<string, string>(); // name : value
        private Dictionary<string, Port> _implementedPorts = new Dictionary<string, Port>(); // name : type (e.g. IDataFlow<string>)
        private Dictionary<string, Port> _acceptedPorts = new Dictionary<string, Port>(); // name : type
        private Dictionary<string, string> _types = new Dictionary<string, string>(); // typeName : type. This contains the types of fields, properties, and constructor args
        private string _documentation = "";
        private HashSet<string> _initialised = new HashSet<string>();
        private List<string> _generics = new List<string>();
        private Dictionary<string, List<int>> _portGenericIndices = new Dictionary<string, List<int>>(); // name : list of used abstraction's generic indices
        private Dictionary<string, string> _portBaseTypes = new Dictionary<string, string>(); // name : original port type (e.g. IDataFlow<T>)
        private Dictionary<string, Port> _portsById = new Dictionary<string, Port>(); // Port.Id : Port
        private Dictionary<string, string> _memberDocumentation = new Dictionary<string, string>(); // name : documentation

        // Ports

        // Methods
        public List<KeyValuePair<string, string>> GetConstructorArgs() => _constructorArgs.ToList();
        public List<KeyValuePair<string, string>> GetFields() => _fields.ToList();
        public List<KeyValuePair<string, string>> GetProperties() => _properties.ToList();

        public Port GetPort(string portName)
        {
            if (_implementedPorts.ContainsKey(portName))
            {
                return _implementedPorts[portName];
            }
            else if (_acceptedPorts.ContainsKey(portName))
            {
                return _acceptedPorts[portName];
            }
            else
            {
                return null;
            }
        }

        public List<Port> GetImplementedPorts() => _implementedPorts.Values.ToList();
        public List<Port> GetAcceptedPorts() => _acceptedPorts.Values.ToList();
        public List<string> GetGenerics() => _generics.ToList();
        public List<int> GetPortGenericIndices(string portName) => _portGenericIndices.ContainsKey(portName) ? _portGenericIndices[portName].ToList() : new List<int>();
        public string GetType(string type) => _types.ContainsKey(type) ? _types[type] : "undefined";
        public string GetDocumentation() => _documentation;

        public string GetMemberDocumentation(string memberName) => _memberDocumentation.ContainsKey(memberName) ? _memberDocumentation[memberName] : "";
        public string GetCodeFilePath() => CodeFilePath;
        public HashSet<string> GetInitialisedVariables() => _initialised.Select(s => s).ToHashSet();
        public string GetPortBaseType(string portName) => _portBaseTypes.ContainsKey(portName) ? _portBaseTypes[portName] : "undefined";

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

        public void AddImplementedPort(string type, string name, bool isReversePort = false, string description = "")
        {
            var port = new Port()
            {
                Type = type,
                Name = name,
                Description = description,
                IsInputPort = true,
                IsReversePort = isReversePort
            };

            _portsById[port.Id] = port;

            _implementedPorts[name] = port;

            _portBaseTypes[port.Id] = type;
        }

        public void AddAcceptedPort(string type, string name, bool isReversePort = false, string description = "")
        {
            var port = new Port()
            {
                Type = type,
                Name = name,
                Description = description,
                IsInputPort = false,
                IsReversePort = isReversePort
            };

            _portsById[port.Id] = port;

            _acceptedPorts[name] = port;

            _portBaseTypes[port.Id] = type;
        }

        public void AddDocumentation(string documentation)
        {
            _documentation = documentation;
        }

        public void AddDocumentation(string memberName, string documentation)
        {
            _memberDocumentation[memberName] = documentation;
        }

        public void AddGeneric(string generic) => _generics.Add(generic);

        public void SetGenerics(IEnumerable<string> newGenerics)
        {
            _generics.Clear();
            _generics.AddRange(newGenerics);
        }

        public void AddPortGenericIndices(string portName, List<int> indices) => _portGenericIndices[portName] = indices;

        /// <summary>
        /// Finds and sets a value in the instance. This will fail if no field, property, or arg has the given name.
        /// </summary>
        /// <param name="name">The variable name, e.g. "Source"</param>
        /// <param name="value">The code literal, e.g. "new MyClass()"</param>
        public void SetValue(string name, string value, bool initialise = true)
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

            if (initialise) Initialise(name);

            if (name == "InstanceName") Description = value; 
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

        public void Clear()
        {
            Type = "";
            FullType = "";

            _documentation = "";
            SourceCode = "";
            CodeFilePath = "";

            _portsById.Clear();

            _implementedPorts.Clear();
            _acceptedPorts.Clear();
            _fields.Clear();
            _constructorArgs.Clear();
            _fields.Clear();
            _properties.Clear();
            _types.Clear();

            _initialised.Clear();
        }

        /// <summary>
        /// Replaces the contents of this node with that of another source. Note: This is destructive, and will set the destination with the default values found in the source.
        /// </summary>
        /// <param name="source"></param>
        public void CloneFrom(AbstractionModel source)
        {
            if (source == null) return;

            Type = source.Type;
            FullType = source.FullType;

            _documentation = source._documentation;
            SourceCode = source.SourceCode;
            CodeFilePath = source.GetCodeFilePath();

            _portsById.Clear();

            _implementedPorts.Clear();
            foreach (var pair in source._implementedPorts)
            {
                var port = pair.Value;
                AddImplementedPort(port.Type, port.Name,port.IsReversePort, port.Description);
                AddPortGenericIndices(port.Name, source.GetPortGenericIndices(port.Name));
            }

            _acceptedPorts.Clear();
            foreach (var pair in source._acceptedPorts)
            {
                var port = pair.Value;
                AddAcceptedPort(port.Type, port.Name, port.IsReversePort, port.Description);
                AddPortGenericIndices(port.Name, source.GetPortGenericIndices(port.Name));
            }

            ReplaceDict(source._fields, _fields);
            ReplaceDict(source._constructorArgs, _constructorArgs);
            ReplaceDict(source._properties, _properties);
            ReplaceDict(source._types, _types);
            ReplaceDict(source._memberDocumentation, _memberDocumentation);

            _initialised = source._initialised.ToHashSet();

            SetGenerics(source.GetGenerics());
        }

        private void ReplaceDict(Dictionary<string, string> source, Dictionary<string, string> current)
        {
            current.Clear();
            foreach (var pair in source)
            {
                current[pair.Key] = pair.Value;
            }
        }

        /// <summary>
        /// Returns the value of the first occurrence of a given member name.
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

            return null;
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

        }

        public void Initialise(string varName)
        {
            if (!_initialised.Contains(varName)) _initialised.Add(varName);
        }

        public void Deinitialise(string varName)
        {
            if (_initialised.Contains(varName)) _initialised.Remove(varName);
        }

        public void RefreshFullTypeWithGenerics()
        {
            if (!_generics.Any()) return;

            var sb = new StringBuilder();

            // Update instance type
            sb.Clear();

            sb.Append(Type);
            sb.Append("<" + _generics.First());
            sb.Append(string.Concat(_generics.Skip(1).Select(s => $", {s}")));
            sb.Append(">");

            FullType = sb.ToString();
        }

        public void UpdateGeneric(int index, string newType)
        {
            if (_generics.Count <= index) return;

            _generics[index] = newType;

            RefreshGenerics();
        }

        public void RefreshGenerics()
        {
            RefreshFullTypeWithGenerics();
            RefreshGenericsInPorts();
        }

        public void RefreshGenericsInPorts()
        {
            var sb = new StringBuilder();
            foreach (var port in _portsById.Values)
            {
                sb.Clear();

                var indexList = _portGenericIndices.ContainsKey(port.Name) ? _portGenericIndices[port.Name] : null;
                if (indexList == null || indexList.Count == 0) continue; // Only update ports with generics

                var fullType = port.Type;
                var typeWithoutGenerics = fullType.Split('<').First();
                
                sb.Append(typeWithoutGenerics);
                sb.Append("<" + _generics[indexList.First()]);
                sb.Append(string.Concat(indexList.Skip(1).Select(i => $", {_generics[i]}")));
                sb.Append(">");

                port.Type = sb.ToString();
            }
        }

        private List<string> GetNameMatches(Func<string, bool> matchFunc)
        {
            var mappings = new List<Dictionary<string, string>>()
            {
                _constructorArgs, _fields, _properties
            };

            var result = new List<string>();

            foreach (var dict in mappings)
            {
                result.AddRange(dict.Keys.Where(matchFunc));
            }


            return result.ToHashSet().ToList();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(FullType);
            if (!string.IsNullOrWhiteSpace(Name)) sb.Append(" " + Name);

            return sb.ToString();
        }

        public AbstractionModel(AbstractionModel source = null)
        {
            if (source != null) CloneFrom(source);
        }
    }
}
