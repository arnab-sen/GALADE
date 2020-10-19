using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;

namespace ProgrammingParadigms
{
    /// <summary>
    /// A data table that stores objects by their given ids. This should be instantiated once per application, and stored as a global variable.
    /// </summary>
    public class Registry
    {
        // Public properties
        public string InstanceName { get; set; } = "DefaultRegistry";

        // Private fields
        private Dictionary<string, object> _registry = new Dictionary<string, object>();

        // Methods
        /// <summary>
        /// Check if the registry contains a value mapped to a given id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool Contains(string id) => _registry.ContainsKey(id);

        /// <summary>
        /// Safely retrieve a value mapped to a given id. If no such value exists, null is returned.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public object Get(string id) => Contains(id) ? _registry[id] : null;

        /// <summary>
        /// Add a new value and id pair to the registry. If the id already exists, an exception is thrown.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="val"></param>
        public void Add(string id, object val)
        {
            if (Contains(id))
            {
                throw new Exception($"Could not add to Registry {InstanceName}: Id {id} is already mapped to a value.");
            }
            else
            {
                _registry[id] = val;
            }
        }

        /// <summary>
        /// Replace an existing value, mapped to a given id, by the given value. If no such value exists, an exception is thrown.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="val"></param>
        public void Replace(string id, object val)
        {
            if (!Contains(id))
            {
                throw new Exception($"Could not replace value at Id {id} in Registry {InstanceName}: Id is not mapped to a value.");
            }
            else
            {
                _registry[id] = val;
            }
        }

        /// <summary>
        /// Safely pass an id-value pair to the registry. If the id exists, its value is replaced, else a new entry is added.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="val"></param>
        public void Update(string id, object val)
        {
            if (Contains(id))
            {
                Replace(id, val);
            }
            else
            {
                Add(id, val);
            }
        }

        public void Delete(string id)
        {
            if (Contains(id)) _registry.Remove(id);
        }
    }
}
