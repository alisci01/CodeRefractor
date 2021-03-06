#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CodeRefractor.CodeWriter.Linker;
using CodeRefractor.MiddleEnd;
using CodeRefractor.MiddleEnd.SimpleOperations;
using CodeRefractor.MiddleEnd.SimpleOperations.Identifiers;
using CodeRefractor.MiddleEnd.SimpleOperations.Methods;
using CodeRefractor.Runtime;
using CodeRefractor.RuntimeBase.Analyze;
using CodeRefractor.RuntimeBase.MiddleEnd;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations;
using CodeRefractor.Util;

#endregion

namespace CodeRefractor.RuntimeBase.Backend.ComputeClosure
{
    public static class MetaLinkerClosureComputing
    {
        public static List<MethodInterpreter> GetMethodClosure(this MethodInterpreter entryPoints,
            CrRuntimeLibrary crRuntime)
        {
            return GetClosureDictionary(entryPoints, crRuntime).Values.ToList();
        }

        private static Dictionary<MethodInterpreterKey, MethodInterpreter> GetClosureDictionary(
            this MethodInterpreter entryPoints, CrRuntimeLibrary crRuntime)
        {
            var result = new Dictionary<MethodInterpreterKey, MethodInterpreter> {{entryPoints.ToKey(), entryPoints}};
            UpdateMethodEntryClosure(entryPoints, result, crRuntime);


            return result;
        }


        public static List<Type> ImplementorsOfT(this Type t, IEnumerable<Type> usedTypes)
        {
            var result = usedTypes.Where(usedType => usedType.IsSubclassOf(t)).ToList();
            return result;
        }

        public static List<MethodInterpreter> GetMultiMethodsClosure(this List<MethodInterpreter> entryPoints,
            CrRuntimeLibrary crRuntime)
        {
            var results = new List<Dictionary<MethodInterpreterKey, MethodInterpreter>>();
            foreach (var interpreter in entryPoints)
            {
                var result = GetClosureDictionary(interpreter, crRuntime);
                GlobalMethodPool.Resolve(interpreter);
                results.Add(result);
            }
            var finalResult = new Dictionary<MethodInterpreterKey, MethodInterpreter>();
            foreach (var result in results)
            {
                foreach (var entry in result)
                {
                    finalResult[entry.Key] = entry.Value;
                }
            }
            return finalResult.Values.ToList();
        }

        public static void UpdateMethodEntryClosure(MethodInterpreter entryPoint,
            Dictionary<MethodInterpreterKey, MethodInterpreter> result, CrRuntimeLibrary crRuntime)
        {
            var useDef = entryPoint.MidRepresentation.UseDef;
            entryPoint.MidRepresentation.UpdateUseDef();
            var ops = useDef.GetLocalOperations();
            var callList = useDef.GetOperationsOfKind(OperationKind.Call).ToList();
            callList.AddRange(useDef.GetOperationsOfKind(OperationKind.CallVirtual));
            callList.AddRange(useDef.GetOperationsOfKind(OperationKind.CallInterface));
          var localOperations = callList.Select(i => ops[i]).ToArray();
            var toAdd = HandleCallInstructions(result, localOperations, crRuntime);
            var funcList = useDef.GetOperationsOfKind(OperationKind.LoadFunction).ToList();
            localOperations = funcList.Select(i => ops[i]).ToArray();
            HandleLoadFunctionInstructions(result, localOperations, toAdd, crRuntime);

            HandleTypeInitializers(result, toAdd, crRuntime);

            HandleGenerics(result, toAdd, crRuntime);
        }

        private static void HandleGenerics(Dictionary<MethodInterpreterKey, MethodInterpreter> result,
            List<MethodInterpreter> toAdd, CrRuntimeLibrary crRuntime)
        {
            foreach (var interpreter in toAdd)
            {
                if (interpreter.IsGenericDeclaringType())
                {
                    //var genericSpecialization = interpreter.Clone();
                    //genericSpecialization.Specialize();
                    UpdateMethodEntryClosure(interpreter, result, crRuntime);
                }
                else
                    UpdateMethodEntryClosure(interpreter, result, crRuntime);
            }
        }

        private static void HandleTypeInitializers(Dictionary<MethodInterpreterKey, MethodInterpreter> result,
            List<MethodInterpreter> toAdd, CrRuntimeLibrary crRuntime)
        {
            foreach (var it in toAdd)
            {
                var declaringType = it.Method.DeclaringType;
                if (declaringType == null)
                    continue;
                declaringType = crRuntime.GetMappedType(declaringType);
                if (declaringType == null)
                    continue;
                declaringType = declaringType.GetReversedType(crRuntime);
                if (declaringType.TypeInitializer == null) continue;
                var info = declaringType.TypeInitializer;

                var interpreter = info.GetInterpreter(crRuntime);
                if (interpreter != null)
                {
                    result[interpreter.ToKey()] = interpreter;
                    UpdateMethodEntryClosure(interpreter, result, crRuntime);
                }
            }
        }

        private static void HandleLoadFunctionInstructions(Dictionary<MethodInterpreterKey, MethodInterpreter> result,
            LocalOperation[] localOperations, List<MethodInterpreter> toAdd, CrRuntimeLibrary crRuntime)
        {
            if (localOperations.Length == 0)
                return;
            foreach (var localOperation in localOperations)
            {
                var functionPointer = (FunctionPointerStore) localOperation;
                var info = functionPointer.FunctionPointer;
                var interpreter = info.GetInterpreter(crRuntime);
                if (interpreter == null)
                    continue;
                result[interpreter.ToKey()] = interpreter;
                toAdd.Add(interpreter);
            }
        }

        class MethodClass
        {
            public Type Class;
            public MethodBase Method;
        }
        private static List<MethodInterpreter> HandleCallInstructions(
            Dictionary<MethodInterpreterKey, MethodInterpreter> result, LocalOperation[] localOperations,
            CrRuntimeLibrary crRuntime)
        {
            var toAdd = new List<MethodInterpreter>();
            if (localOperations.Length == 0)
                return toAdd;
            foreach (var localOperation in localOperations)
            {
                var methodData = (MethodData) localOperation;
                var info = methodData.Info;
                if (info.DeclaringType == typeof (object)
                    || info.DeclaringType == typeof (IntPtr))
                    continue;
                var descInfo = methodData.Interpreter.ToKey();
                if (result.ContainsKey(descInfo))
                    continue;
                var interpreter = info.Register(crRuntime);
                if (interpreter == null)
                    continue;
                var isGenericDeclaringType = interpreter.IsGenericDeclaringType();
                if (isGenericDeclaringType)
                {
                    interpreter.Specialize();
                }
                var interpreterExists = result.ContainsKey(descInfo);
                if (!interpreterExists)
                {
                    result[descInfo] = interpreter;
                    UpdateMethodEntryClosure(interpreter, result, crRuntime);
               

                //Seems we might need to manually inject method interpreters for explicit interface methods ...
                    if (methodData.Info.DeclaringType != null &&
                        methodData.Info.DeclaringType.IsInterface)
                    {
                        var objects =
                            methodData.Info.DeclaringType.Assembly.GetObjectsByInterface(methodData.Info.DeclaringType)
                                .ToArray();
                        var @params = methodData.Info.GetParameters().Select(u => u.ParameterType).ToArray();

                       
                        var methods =
                            objects.Select(
                                j => new MethodClass
                                {
                                    Method = 
                                    j.GetMethod(methodData.Info.GetMethodName(), //Explicit 
                                        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null,
                                        @params, null),
                                Class= j}).Union(objects.Select(
                                j => new MethodClass
                                {
                                    Method =
                                    j.GetMethod(methodData.Info.Name, //Implicit
                                        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null,
                                        @params, null),
                                    Class = j
                                })
                                        )
                                      .Union(objects.SelectMany(y => 
                                          y.GetProperties().SelectMany(p => p.GetAccessors()).Where(p => p.Name == methodData.Info.Name || p.Name == methodData.Info.Name).Select(k=>new MethodClass//Properties
                                      {
                                          Method =k,
                                          Class = y}
                                       ))).Where(k => k.Method != null).ToArray();

                      
                        foreach (var methodInfo in methods)
                        {
                            var interpreter2 = methodInfo.Method.Register(crRuntime);

                            var descInfo2 = interpreter2.ToKey(methodInfo.Method.DeclaringType);
                            if(methodInfo.Method.IsFinal)
                                descInfo2 = interpreter2.ToKey(methodInfo.Class);

                            if (result.ContainsKey(descInfo2))
                                continue;

                            if (interpreter2 == null)
                                continue;
                            var isGenericDeclaringType2 = interpreter2.IsGenericDeclaringType();
                            if (isGenericDeclaringType2)
                            {
                                interpreter2.Specialize();
                            }
                            var interpreterExists2 = result.ContainsKey(descInfo2);
                            if (!interpreterExists2)
                            {
                                result[descInfo2] = interpreter2;
                                UpdateMethodEntryClosure(interpreter2, result, crRuntime);
                            }

                            toAdd.Add(interpreter2);
                        }
                    }
                }
                toAdd.Add(interpreter);
               
            }
            return toAdd;
        }
    }
}