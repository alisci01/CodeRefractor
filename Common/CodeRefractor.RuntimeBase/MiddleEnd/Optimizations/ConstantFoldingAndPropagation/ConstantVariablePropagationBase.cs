#region Usings

using CodeRefractor.MiddleEnd.Optimizations.Common;
using CodeRefractor.MiddleEnd.SimpleOperations;
using CodeRefractor.MiddleEnd.SimpleOperations.Identifiers;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations;

#endregion

namespace CodeRefractor.RuntimeBase.Backend.Optimizations.ConstantFoldingAndPropagation
{
    public abstract class ConstantVariablePropagationBase : ResultingInFunctionOptimizationPass
    {
        protected static ConstValue GetConstantFromOperation(LocalOperation srcOperation,
            out Assignment srcVariableDefinition)
        {
            srcVariableDefinition = null;
            return srcOperation.Kind != OperationKind.Assignment
                ? null
                : ConstantFromOperation(out srcVariableDefinition, srcOperation);
        }

        public static ConstValue ConstantFromOperation(out Assignment srcVariableDefinition, LocalOperation srcOperation)
        {
            srcVariableDefinition = (Assignment) srcOperation;
            var constValue = srcVariableDefinition.Right as ConstValue;
            return constValue;
        }

        public static bool SameVariable(LocalVariable src, LocalVariable dest)
        {
            return
                src != null
                && dest != null
                && (src.Kind == dest.Kind
                    && src.Id == dest.Id);
        }
    }
}