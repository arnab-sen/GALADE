using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    public class ClassFileGenerator
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public List<string> Usings { get; set; } = new List<string>();
        public string Namespace { get; set; } = "Application";
        public string ClassName { get; set; } = "DefaultClass";
        public HashSet<string> Regions { get; set; } = new HashSet<string>();
        public string ClassAccessLevel { get; set; } = "public";
        public List<string> ImplementedInterfaces { get; set; } = new List<string>();
        public string BaseListInlineComment { get; set; } = "";
        public bool IsInterface { get; set; } = false;
        public List<string> Description { get; set; } = new List<string>();
        public List<string> ConstructorBody { get; set; } = new List<string>();

        // Private fields
        private int currentIndent = 0;
        private Dictionary<string, List<string>> regionVariables = new Dictionary<string, List<string>>() { { "Public fields and properties", new List<string>() } };
        private Dictionary<string, string> methodStubs = new Dictionary<string, string>();
        private Dictionary<string, string> methodPreStubComments = new Dictionary<string, string>();
        private Dictionary<string, List<string>> methodBodies = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> getterBodies = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> setterBodies = new Dictionary<string, List<string>>();

        // Ports

        public ClassFileGenerator()
        {
            Usings.AddRange(GetDefaultUsings());
        }

        public List<string> GetDefaultUsings()
        {
            var defaultUsings = new List<string>();
            defaultUsings.Add("System");
            defaultUsings.Add("System.Text");
            defaultUsings.Add("System.Linq");
            defaultUsings.Add("System.Collections.Generic");
            defaultUsings.Add("System.Threading.Tasks");

            return defaultUsings;
        }

        public void AddMethod(
            string methodName, 
            string returnType = "void", 
            string accessLevel = "",
            List<Tuple<string, string>> parameters = null,
            string preStubComment = "",
            List<string> methodBody = null,
            string region = "")
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(preStubComment)) methodPreStubComments[methodName] = preStubComment;

            if (!IsInterface && !string.IsNullOrEmpty(accessLevel)) sb.Append($"{accessLevel} ");
            sb.Append($"{returnType} {methodName}(");

            if (parameters != null && parameters.Count > 0)
            {
                sb.Append($"{parameters[0].Item1} {parameters[0].Item2}");

                foreach (var parameter in parameters.Skip(1))
                {
                    sb.Append($", {parameter.Item1} {parameter.Item2}");
                } 
            }

            sb.Append(")");

            methodStubs[methodName] = sb.ToString();
            if (methodBody != null) methodBodies[methodName] = methodBody;
            if (!string.IsNullOrEmpty(region))
            {
                if (!regionVariables.ContainsKey(region)) regionVariables[region] = new List<string>();
                regionVariables[region].Add($"method:{methodName}");
            }
        }

        public void AddField(
            string variableType,
            string variableName,
            string region,
            string accessLevel = "",
            string defaultValue = null)
        {
            var sb = new StringBuilder();
            if (!IsInterface && !string.IsNullOrEmpty(accessLevel)) sb.Append($"{accessLevel} ");
            
            sb.Append($"{variableType} {variableName}"); 

            if (defaultValue != null && !IsInterface) sb.Append($" = {defaultValue}");
            sb.Append(";");

            if (Regions.Contains(region))
            {
                if (!regionVariables.ContainsKey(region)) regionVariables[region] = new List<string>() { };

                regionVariables[region].Add(sb.ToString());
            }
        }

        public void AddProperty(
            string propertyType,
            string propertyName,
            string accessLevel = "",
            string defaultValue = "",
            string region = "none",
            string getterBody = "",
            string setterBody = "",
            bool isAutoProperty = false)
        {
            var sb = new StringBuilder();

            if (!IsInterface && !string.IsNullOrEmpty(accessLevel)) sb.Append($"{accessLevel} ");
            sb.Append($"{propertyType} {propertyName} {{ ");

            if (!string.IsNullOrEmpty(getterBody))
            {
                sb.Append($"get {getterBody}");
            }
            else if (isAutoProperty)
            {
                sb.Append($"get; ");
            }
            else
            {
                sb.Append($"get {{ return default; }} ");
            }

            if (!string.IsNullOrEmpty(getterBody))
            {
                sb.Append($"set {setterBody} }}");
            }
            else if (isAutoProperty)
            {
                sb.Append($"set; }}");
            }
            else
            {
                sb.Append($"set {{ }} }}");
            }

            if (!string.IsNullOrEmpty(defaultValue) && !IsInterface)
            {
                sb.Append($" = {defaultValue};");
            }

            if (Regions.Contains(region))
            {
                if (!regionVariables.ContainsKey(region)) regionVariables[region] = new List<string>() { };

                regionVariables[region].Add(sb.ToString());
            }

        }

        public void AddLine(StringBuilder sb, string content)
        {
            sb.AppendLine($"{new string(' ', 4 * currentIndent)}{content}");
        }

        public string BuildFile()
        {
            var fileBuilder = new StringBuilder();
            var lineBuffer = new StringBuilder();
            currentIndent = 0;

            // Usings
            foreach (var _using in Usings)
            {
                AddLine(fileBuilder, $"using {_using};");
            }
            AddLine(fileBuilder, "");

            // Namespace
            AddLine(fileBuilder, $"namespace {Namespace}");
            AddLine(fileBuilder, "{");
            currentIndent++;

            // Class Documentation
            AddLine(fileBuilder, "/// <summary>");
            foreach (var line in Description)
            {
                AddLine(fileBuilder, $"/// <para>{line}</para>");
            }
            AddLine(fileBuilder, "/// </summary>");

            // Class name and implemented interfaces
            lineBuffer.Clear();
            lineBuffer.Append($"{ClassAccessLevel} {(IsInterface ? "interface" : "class")} {ClassName}"); 

            if (ImplementedInterfaces.Count > 0)
            {
                lineBuffer.Append($" : {ImplementedInterfaces[0]}");

                foreach (var implementedInterface in ImplementedInterfaces.Skip(1))
                {
                    lineBuffer.Append($", {implementedInterface}");
                }
            }

            if (!string.IsNullOrEmpty(BaseListInlineComment)) lineBuffer.Append(" " + BaseListInlineComment);

            AddLine(fileBuilder, lineBuffer.ToString());
            lineBuffer.Clear();

            AddLine(fileBuilder, "{");
            currentIndent++;

            // Global fields and properties
            foreach (var region in Regions)
            {
                AddLine(fileBuilder, $"// {region}");

                if (regionVariables.ContainsKey(region))
                {
                    foreach (var regionVariable in regionVariables[region])
                    {
                        if (regionVariable.StartsWith("method:")) // Is a method
                        {
                            var methodName = regionVariable.Replace("method:", "");

                            if (methodPreStubComments.ContainsKey(methodName)) AddLine(fileBuilder, $"// {methodPreStubComments[methodName]}");

                            // Different types of region variables should be separated by spaces
                            if (regionVariables[region].Count > 1) AddLine(fileBuilder, "");

                            AddLine(fileBuilder, methodStubs[methodName]);
                            if (!IsInterface)
                            {
                                AddLine(fileBuilder, "{");
                                currentIndent++;
                                if (methodBodies.ContainsKey(methodName) && methodBodies[methodName].Count > 0)
                                {
                                    foreach (var line in methodBodies[methodName])
                                    {
                                        AddLine(fileBuilder, line);
                                    }
                                }
                                else
                                {
                                    AddLine(fileBuilder, ""); 
                                }
                        
                                currentIndent--;
                                AddLine(fileBuilder, "}"); 
                            }
                        }
                        else // Is a field or property
                        {
                            AddLine(fileBuilder, regionVariable); 
                        }
                    } 
                }

                AddLine(fileBuilder, "");
            }

            if (!IsInterface)
            {
                // Constructor Documentation
                AddLine(fileBuilder, "/// <summary>");
                AddLine(fileBuilder, $"/// <para>{Description.FirstOrDefault()}</para>");
                AddLine(fileBuilder, "/// </summary>");

                // Constructor
                AddLine(fileBuilder, $"public {ClassName}()");
                AddLine(fileBuilder, "{");
                currentIndent++;

                if (ConstructorBody.Count > 0)
                {
                    foreach (var line in ConstructorBody)
                    {
                        AddLine(fileBuilder, line);
                    }
                }
                else
                {
                    AddLine(fileBuilder, ""); 
                }

                currentIndent--;
                AddLine(fileBuilder, "}"); 
            }

            // // Method stubs
            // foreach (var kvp in methodStubs)
            // {
            //     AddLine(fileBuilder, "");
            //     if (methodPreStubComments.ContainsKey(kvp.Key)) AddLine(fileBuilder, $"// {methodPreStubComments[kvp.Key]}");
            //
            //     AddLine(fileBuilder, kvp.Value);
            //     if (!IsInterface)
            //     {
            //         AddLine(fileBuilder, "{");
            //         currentIndent++;
            //         if (methodBodies.ContainsKey(kvp.Key) && methodBodies[kvp.Key].Count > 0)
            //         {
            //             foreach (var line in methodBodies[kvp.Key])
            //             {
            //                 AddLine(fileBuilder, line);
            //             }
            //         }
            //         else
            //         {
            //             AddLine(fileBuilder, ""); 
            //         }
            //
            //         currentIndent--;
            //         AddLine(fileBuilder, "}"); 
            //     }
            // }

            currentIndent--;
            AddLine(fileBuilder, "}");

            currentIndent--;
            AddLine(fileBuilder, "}");

            var fileContents = fileBuilder.ToString();
            return fileContents;
        }
    }
}
