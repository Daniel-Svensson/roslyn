using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class MethodContractRewriter
    {
        internal static BoundBlock Rewrite(SourceMethodSymbol sourceMethodSymbol, MethodContractSyntax contract, BoundBlock body, TypeCompilationState compilationState, DiagnosticBag diagsForCurrentMethod)
        {
            var binder = compilationState.Compilation.GetBinderFactory(sourceMethodSymbol.SyntaxTree)
                                     .GetBinder(body.Syntax);
            SyntheticBoundNodeFactory factory = new SyntheticBoundNodeFactory(sourceMethodSymbol, sourceMethodSymbol.SyntaxNode, compilationState, diagsForCurrentMethod);
            var contractType = compilationState.Compilation.GetTypeByReflectionType(typeof(System.Diagnostics.Contracts.Contract), diagsForCurrentMethod);

            var contractStatements = ArrayBuilder<BoundStatement>.GetInstance(contract.Requires.Count);
            foreach (var requires in contract.Requires)
            {
                var condition = binder.BindExpression(requires.Condition, diagsForCurrentMethod);
                
                var methodCall = factory.StaticCall(contractType, "Requires", condition);
                var statement = factory.ExpressionStatement(methodCall);

                contractStatements.Add(statement);
            }
            foreach (var requires in contract.Ensures)
            {
                var condition = binder.BindExpression(requires.Condition, diagsForCurrentMethod);
                var methodCall = factory.StaticCall(contractType, "Ensures", condition);
                var statement = factory.ExpressionStatement(methodCall);

                contractStatements.Add(statement);
            }

            return body.Update(body.Locals, body.Statements.InsertRange(0, contractStatements.ToImmutableAndFree()));
        }
    }
}
