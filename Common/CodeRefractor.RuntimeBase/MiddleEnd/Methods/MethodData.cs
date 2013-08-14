#region Usings

using System;
using System.Collections.Generic;
using System.Reflection;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations.Identifiers;

#endregion

namespace CodeRefractor.RuntimeBase.MiddleEnd.Methods
{
    public class MethodData : IdentifierValue
    {
        public bool IsStatic;
        public bool IsVoid;
        public LocalVariable Result;
        public bool IsPure;
        public List<IdentifierValue> Parameters { get; set; }

        public MethodData(MethodBase info)
        {
            Info = info;
            IsStatic = info.IsStatic;
            Parameters = new List<IdentifierValue>();
            IsVoid = info.GetReturnType() == typeof (void);
            IsPure = PureMethodTable.ComputeMethodPurity(info.ToString());
        }

        public MethodBase Info { get; private set; }

        public void ExtractNeededValuesFromStack(EvaluatorStack evaluatorStack)
        {
            var stack = evaluatorStack.Stack;
            var methodParams = Info.GetParameters();
            foreach (var t in methodParams)
            {
                Parameters.Insert(0, stack.Pop());
            }
            if (!IsStatic)
                Parameters.Insert(0, stack.Pop());
        }

        public override string ToString()
        {
            return String.Format(Result!=null 
                ? "Call {0} = {1};" 
                : "Call {0};", Result.Name, Info);
        }
    }
}