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
    /// <para>Applies an operation (described by a lambda) on inputs of type T and returns an output of type T.</para>
    /// <para>Ports:</para>
    /// <para>1. IEvent start: The event that starts the operation.</para>
    /// <para>2. List&lt;IDataFlowB&lt;T&gt;&gt; operands: The input operands for the operation.</para>
    /// <para>3. IDataFlow&lt;T&gt; operationResultOutput: The output from the operation.</para>
    /// </summary>
    public class Operation<T> : IEvent // start
    {
        // Properties
        public string InstanceName { get; set; } = "Default";
        public delegate T OperationDelegate(List<T> operands);
        public OperationDelegate Lambda;

        // Private fields
        private T operationResult = default;

        // Ports
        private List<IDataFlowB<T>> operands;
        private IDataFlow<T> operationResultOutput;

        /// <summary>
        /// <para>Applies an operation (described by a lambda) on inputs of type T and returns an output of type T.</para>
        /// </summary>
        public Operation()
        {

        }

        private void Push()
        {
            if (operationResultOutput != null) operationResultOutput.Data = operationResult;
        }

        private void TestLambda(List<T> operandList)
        {
            // Test a lambda here before minifying it
            Lambda = op =>
            {
                return default;
            };

            var testResult = Lambda(operandList);
        }

        void IEvent.Execute()
        {
            try
            {
                List<T> operandList = new List<T>();
                foreach (var dataFlowB in operands)
                {
                    operandList.Add(dataFlowB.Data);
                }

                // TestLambda(operandList);

                operationResult = Lambda(operandList);

                Push();
            }
            catch (Exception e)
            {
                Logging.Log(e);
            }
        }
    }
}
