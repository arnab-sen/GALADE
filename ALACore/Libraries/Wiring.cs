using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;

namespace Libraries
{
    public static class Wiring
    {

        private delegate void InitializeDelegate();
        public delegate void OutputDelegate(string output);

        private static string firstPortName;
        private static event InitializeDelegate PostWiring;
        private static event InitializeDelegate OnWiring;
        public static event OutputDelegate Output;

        /// <summary>
        /// Important method that wires and connects all the classes and interfaces together to run the application.
        /// If object A (this) has a private field of an interface, and object B implements the interface, then wire them together. Returns this for fluent style programming.
        /// ------------------------------------------------------------------------------------------------------------------
        /// WireTo methods five important keys:
        /// 1. wires match interfaces, A (calls interface) and B (implements)
        /// 2. interfaces must be all private for matching to happen
        /// 3. can wire multiple matching interfaces
        /// 4. wires in order form top to bottom of not yet wired
        /// 5. can ovveride order by specifying port names as second parameter
        /// 6. looks for list as well (be careful of blocking other interfaces from wiring)
        /// ------------------------------------------------------------------------------------------------------------------
        /// </summary>
        /// <param name="A">
        /// The object on which the method is called is the object being wired from
        /// </param> 
        /// <param name="B">The object being wired to (must implement the interface)</param> 
        /// <returns></returns>
        /// <remarks>
        /// If A has two private fields of the same interface, the first compatible B object wired goes to the first one and the second compatible B object wired goes to the second.
        /// If A has multiple private interfaces of different types, only the first matching interface that B implements will be wired.
        /// In other words, by default, only one interface is wired between A and B
        /// To override this behaviour you can get give multiple interfaces in A a prefix "Pn_" where n is 0..9:
        /// Then a single wiring operation will wire all fieldnames with a consistent port prefix to the same B.
        /// These remarks apply only to single fields, not Lists.
        /// e.g.
        /// private IOneable client1Onabale;
        /// private ITwoable client1Twoable;
        /// private IThreeable client2;
        /// Clearly we want to wire two different clients. But if the first client wired implements all three interfaces, it will be wired to all three fields.
        /// So name the field like this:
        /// private IOneable P1_clientOnabale;
        /// private ITwoable P1_clientTwoable;
        /// private IThreeable P2_client;
        /// </remarks>
        public static T WireTo<T>(this T A, object B, string APortName = null, bool reverse = false)
        {
            string multiportExceptionMessage = $"Error: Wiring failed because the two instances are already wired together by another port.";

            if (A == null)
            {
                throw new ArgumentException("A cannot be null");
            }
            if (B == null)
            {
                throw new ArgumentException("B cannot be null");
            }

            // achieve the following via reflection
            // A.field = (<type of interface>)B;
            // A.list.Add( (<type of interface>)B );

            // Get the two instance name first for the Debug Output WriteLines
            var AinstanceName = A.GetType().GetProperties().FirstOrDefault(f => f.Name == "InstanceName")?.GetValue(A);
            if (AinstanceName == null) AinstanceName = A.GetType().GetFields().FirstOrDefault(f => f.Name == "InstanceName")?.GetValue(A);
            var BinstanceName = B.GetType().GetProperties().FirstOrDefault(f => f.Name == "InstanceName")?.GetValue(B);
            if (BinstanceName == null) BinstanceName = B.GetType().GetFields().FirstOrDefault(f => f.Name == "InstanceName")?.GetValue(B);


            var BType = B.GetType();
            var AfieldInfos = A.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => (APortName == null || f.Name == APortName) && (!reverse ^ EndsIn_B(f.Name))).ToList(); // find the fields that the name meets all criteria
                                                                                                                   // TODO: when not reverse ports ending in _B should be excluded

            //if (A.GetType().Name=="DragDrop" && AinstanceName=="Initial Tag")
            //{ // for breakpoint
            //}
            var wiredSomething = false;
            firstPortName = null;
            foreach (var BimplementedInterface in BType.GetInterfaces()) // consider every interface implemented by B 
            {
                // find the first field in A that matches the interface type of B
                var AfieldInfo = AfieldInfos.FirstOrDefault(f => f.FieldType == BimplementedInterface 
                                                                 || f.FieldType.IsEquivalentTo(BimplementedInterface) 
                                                                 && f.GetValue(A) == null); 

                // look for normal private fields first
                if (AfieldInfo != null)  // there is a match
                {

                    if (SamePort(AfieldInfo.Name))
                    {
                        AfieldInfo.SetValue(A, B);  // do the wiring
                        wiredSomething = true;
                        OutputWiring(A, B, AfieldInfo);
                    }
                    continue;  // could be more than one interface to wire
                }

                // do the same as above for private fields that are a list of the interface of the matching type
                foreach (var AlistFieldInfo in AfieldInfos)
                {
                    if (!AlistFieldInfo.FieldType.IsGenericType) //not matching interface
                    {
                        continue;
                    }
                    var AListFieldValue = AlistFieldInfo.GetValue(A);

                    var AListGenericArguments = AlistFieldInfo.FieldType.GetGenericArguments();
                    if (AListGenericArguments.Length != 1) continue;    // A list should only have one type anyway 
                    if (AListGenericArguments[0].IsAssignableFrom(BimplementedInterface)) // JRS: There was some case where == didn't work, maybe in the gamescoring application
                    {
                        if (AListGenericArguments[0] != BimplementedInterface)
                        {
                            var g = AListGenericArguments[0];
                            if (g != typeof(object)) throw new Exception("Different types");
                            continue;
                        }
                        if (AListFieldValue == null)
                        {
                            var listType = typeof(List<>);
                            Type[] listParam = { BimplementedInterface };
                            AListFieldValue = Activator.CreateInstance(listType.MakeGenericType(listParam));
                            if (wiredSomething)
                            {
                                throw new Exception(multiportExceptionMessage);
                            }

                            AlistFieldInfo.SetValue(A, AListFieldValue);
                        }

                        AListFieldValue.GetType().GetMethod("Add").Invoke(AListFieldValue, new[] { B });
                        wiredSomething = true;
                        OutputWiring(A, B, AlistFieldInfo);
                        break;
                    }

                }
            }

            if (!reverse && !wiredSomething)
            {
                if (APortName != null)
                {
                    // a specific port was specified so see if the port was already wired
                    var value = AfieldInfos.FirstOrDefault()?.GetValue(A);
                    if (value != null && !(value is IList))
                    {
                        throw new Exception($"Port already wired {A.GetType().Name}[{AinstanceName}].{APortName} to {BType.Name}[{BinstanceName}]");
                    }
                }
                //throw new Exception($"Failed to wire {A.GetType().Name}[{AinstanceName}].{APortName} to {BType.Name}[{BinstanceName}]");
            }

            // PostWiringInitialize for A
            var postWiringMethod = A.GetType().GetMethod("PostWiringInitialize", BindingFlags.NonPublic | BindingFlags.Instance);
            if (postWiringMethod != null)
            {
                InitializeDelegate handler = (InitializeDelegate)Delegate.CreateDelegate(typeof(InitializeDelegate), A, postWiringMethod);
                PostWiring -= handler;  // instances can be wired to/from more than once, so only register their PostWiringInitialize once
                PostWiring += handler;
            }
            
            // PostWiringInitialize for B
            postWiringMethod = B.GetType().GetMethod("PostWiringInitialize", BindingFlags.NonPublic | BindingFlags.Instance);
            if (postWiringMethod != null)
            {
                InitializeDelegate handler = (InitializeDelegate)Delegate.CreateDelegate(typeof(InitializeDelegate), B, postWiringMethod);
                PostWiring -= handler;  // instances can be wired to/from more than once, so only register their PostWiringInitialize once
                PostWiring += handler;
            }
            

            var onWiringMethod = A.GetType().GetMethod("OnWiringInitialize", BindingFlags.NonPublic | BindingFlags.Instance);
            if (onWiringMethod != null)
            {
                InitializeDelegate handler = (InitializeDelegate)Delegate.CreateDelegate(typeof(InitializeDelegate), A, onWiringMethod);
                OnWiring += handler;
                OnWiringInitialize();
                OnWiring -= handler; // OnWiringInitialize() is always called after wiring, so we remove the handler to avoid it being invoked again on the next wiring
            }

            return A;
        }

        /// <summary>
        /// Wire B to A and returns A. Used to wire objects to input ports of A.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="A">The object being wire to</param>
        /// <param name="B">The object being wired from</param>
        /// <returns>A</returns>
        public static object WireFrom<T>(this object A, T B, string APortName = null)
        {
            B.WireTo(A, APortName);
            return A;
        }


        /// <summary>
        /// The SamePort function always returns true the first time it is called (for a given A and B) but on the second and subsequent calls
        /// it only returns true if the name has the same Px_ prefix as the first.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static bool SamePort(string name)
        {
            if (name.Length >= 3 && name[0] == 'P' && name[2] == '_' && name[1] >= '0' && name[1] <= '9')
            {
                string portName = name.Substring(0, 3);
                if (firstPortName == null)
                {
                    firstPortName = portName;
                }
                return portName.Equals(firstPortName);
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private static bool EndsIn_B(string s)
        {
            if (s == null) return false;
            var rv = s.Length > 2 && s.EndsWith("_B");
            if (rv)
            {
            }
            return s.Length > 2 && s.EndsWith("_B");
        }

        public static T IfWireTo<T>(this T A, bool condition, object B, string portName = null)
        {
            if (condition) A.WireTo(B, APortName: portName);
            return A;
        }

        public static T IfWireFrom<T>(this T A, bool condition, object B, string portName = null)
        {
            if (condition) A.WireFrom(B, APortName: portName);
            return A;
        }

        /// <summary>
        /// A method called after application wiring and before runtime execution. Any object used for wiring can implement this method by defining a "void PostWiringInitialize()".
        /// </summary>
        public static void PostWiringInitialize()
        {
            PostWiring?.Invoke();
        }

        public static void OnWiringInitialize()
        {
            OnWiring?.Invoke();
        }

        public static void OutputWiring(dynamic A, dynamic B, dynamic matchedInterface, bool save = true)
        {
            if (Output == null) return;

            string AInstanceName = "(No InstanceName)";
            string BInstanceName = "(No InstanceName)";
            try { if (A.InstanceName != null) AInstanceName = $"(\"{A.InstanceName}\")"; } catch { };
            try { if (B.InstanceName != null) BInstanceName = $"(\"{B.InstanceName}\")"; } catch { };

            var AClassName = A.GetType().Name;
            var BClassName = B.GetType().Name;
            string matchedInterfaceType = $"{matchedInterface.FieldType.Name}";
            if (matchedInterface.FieldType.GenericTypeArguments.Length > 0)
            {
                matchedInterfaceType += $"<{matchedInterface.FieldType.GenericTypeArguments[0]}>";
            }

            string output = $"({AClassName} {AInstanceName}.{matchedInterface.Name}) -- [{matchedInterfaceType}] --> ({BClassName} {BInstanceName})";
            
            Output?.Invoke(output);
        }


        public static T DeleteWireTo<T>(this T A, object B, string APortName = null, bool reverse = false)
        {
            if (A == null)
            {
                throw new ArgumentException("A cannot be null");
            }
            if (B == null)
            {
                throw new ArgumentException("B cannot be null");
            }

            // achieve the following via reflection
            // A.field = null;
            // A.list.Remove( (<type of interface>)B );

            // Get the two instance name first for the Debug Output WriteLines
            var AinstanceName = A.GetType().GetProperties().FirstOrDefault(f => f.Name == "instanceName")?.GetValue(A);
            if (AinstanceName == null) AinstanceName = A.GetType().GetFields().FirstOrDefault(f => f.Name == "instanceName")?.GetValue(A);
            var BinstanceName = B.GetType().GetProperties().FirstOrDefault(f => f.Name == "instanceName")?.GetValue(B);
            if (BinstanceName == null) BinstanceName = B.GetType().GetFields().FirstOrDefault(f => f.Name == "instanceName")?.GetValue(B);


            var BType = B.GetType();
            var AfieldInfos = A.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => (f.Name == APortName) && (!reverse ^ EndsIn_B(f.Name))).ToList(); // find the fields that the name meets all criteria

            foreach (var BimplementedInterface in BType.GetInterfaces()) // consider every interface implemented by B 
            {
                // find the first field in A that matches the interface type of B
                var AfieldInfo = AfieldInfos.FirstOrDefault(f => f.FieldType == BimplementedInterface
                                                                 || f.FieldType.IsEquivalentTo(BimplementedInterface)
                                                                 && f.GetValue(A) == null);

                // look for normal private fields first
                if (AfieldInfo != null)  // there is a match
                {

                    if (SamePort(AfieldInfo.Name))
                    {
                        AfieldInfo.SetValue(A, null);  // do the wiring
                    }
                    continue;  // could be more than one interface to wire
                }

                // do the same as above for private fields that are a list of the interface of the matching type
                foreach (var AlistFieldInfo in AfieldInfos)
                {
                    if (!AlistFieldInfo.FieldType.IsGenericType) //not matching interface
                    {
                        continue;
                    }
                    var AListFieldValue = AlistFieldInfo.GetValue(A);

                    var AListGenericArguments = AlistFieldInfo.FieldType.GetGenericArguments();
                    if (AListGenericArguments.Length != 1) continue;    // A list should only have one type anyway 
                    if (AListGenericArguments[0].IsAssignableFrom(BimplementedInterface)) // JRS: There was some case where == didn't work, maybe in the gamescoring application
                    {
                        if (AListGenericArguments[0] != BimplementedInterface)
                        {
                            var g = AListGenericArguments[0];
                            if (g != typeof(object)) throw new Exception("Different types");
                            continue;
                        }
                        if (AListFieldValue == null)
                        {
                            var listType = typeof(List<>);
                            Type[] listParam = { BimplementedInterface };
                            AListFieldValue = Activator.CreateInstance(listType.MakeGenericType(listParam));

                            AlistFieldInfo.SetValue(A, AListFieldValue);
                        }

                        AListFieldValue.GetType().GetMethod("Remove").Invoke(AListFieldValue, new[] { B });
                        break;
                    }

                }
            }

            return A;
        }
    }
}
