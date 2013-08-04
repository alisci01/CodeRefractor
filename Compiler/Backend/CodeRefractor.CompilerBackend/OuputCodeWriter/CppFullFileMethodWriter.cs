﻿#region Usings

using System;
using System.Reflection;
using System.Text;
using CodeRefractor.RuntimeBase;
using CodeRefractor.RuntimeBase.MiddleEnd;

#endregion

namespace CodeRefractor.CompilerBackend.OuputCodeWriter
{
    public static class CppFullFileMethodWriter
    {
        public static MetaLinker CreateLinkerFromEntryPoint(this MethodInfo definition)
        {
            var linker = new MetaLinker();
            linker.SetEntryPoint(definition);
            linker.ComputeDependencies(definition);
            linker.ComputeLabels(definition);
            linker.EvaluateMethods();
            MetaLinkerOptimizer.OptimizeMethods();
            return linker;
        }

        public static string WriteHeaderMethod(this MethodBase methodBase, bool writeEndColon = true,
                                               Type mappedType = null)
        {
            var retType = methodBase.GetReturnType().ToCppMangling();
            var sb = new StringBuilder();
            var arguments = methodBase.GetArgumentsAsText();

            sb.AppendFormat("{0} {1}({2})",
                            retType, methodBase.ClangMethodSignature(mappedType), arguments);
            if (writeEndColon)
                sb.Append(";");

            sb.AppendLine();
            return sb.ToString();
        }

        public static string WritePInvokeDefinition(this MethodInterpreter methodBase, string methodDll)
        {
            var retType = methodBase.Method.GetReturnType().ToCppMangling();
            var sb = new StringBuilder();
            var arguments = methodBase.Method.GetArgumentsAsText();

            sb.AppendFormat("typedef {0} (*{1}_type)({2})",
                            retType, methodDll, arguments);

            sb.AppendLine(";");
            sb.AppendFormat("{0}_type {0};", methodDll);
            sb.AppendLine();
            return sb.ToString();
        }
    }
}