using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Invio.CodeAnalysis {
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class CaseSensitivityInLinqExpressionsAnalyzer : DiagnosticAnalyzer {

        public override void Initialize(AnalysisContext context) {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterOperationAction(AnalyzeOperator, OperationKind.BinaryOperator);
            context.RegisterOperationAction(AnalyzeInvoke, OperationKind.Invocation);
        }

        private static MethodInfo ObjectStaticEqualsMethod { get; } =
            ReflectionHelper.GetFuncMethod<Object, Object, Boolean>(Object.Equals);
        private static MethodInfo StringStaticEqualsMethod { get; } =
            ReflectionHelper.GetFuncMethod<String, String, Boolean>(String.Equals);
        private static MethodInfo StringInstanceEqualsMethod { get; } =
            ReflectionHelper.GetMethodFromExpression<String>(s => s.Equals(String.Empty));

        private static MethodInfo EnumerableOfStringContainsMethod { get; } =
            ReflectionHelper.GetFuncMethod<IEnumerable<String>, String, Boolean>(
                Enumerable.Contains);

        private void AnalyzeInvoke(OperationAnalysisContext context) {
            try {
                if (context.Operation is IInvocationOperation invocation &&
                    IsInLinqExpression(invocation)) {

                    var violation = false;
                    if (invocation.TargetMethod.IsMethod(StringStaticEqualsMethod) ||
                        invocation.TargetMethod.IsMethod(StringInstanceEqualsMethod) ||
                        invocation.TargetMethod.IsMethod(EnumerableOfStringContainsMethod)) {

                        violation = true;
                    } else if (invocation.TargetMethod.IsMethod(ObjectStaticEqualsMethod) &&
                        (invocation.Arguments[0].Value.IsOfType<String>() ||
                            invocation.Arguments[1].Value.IsOfType<String>())) {

                        violation = true;
                    } else switch (invocation.TargetMethod.Name) {
                        // This is a .Equals call on some non-string instance
                        case "Equals" when
                            !invocation.TargetMethod.IsStatic &&
                            invocation.TargetMethod.Parameters.Length == 1 &&
                            invocation.Arguments[0].Value.IsOfType<String>():
                        // This is one of the many .Contains functions on a collection type or
                        // interface.
                        case "Contains" when
                            !invocation.TargetMethod.IsStatic &&
                            // It's possible we won't be able to load the type because it's in an
                            // unreferenced assembly
                            invocation.TargetMethod.ContainingType.TryLoadType(out var type) &&
                            type.IsDerivativeOf(typeof(IEnumerable<String>)):

                            violation = true;
                            break;
                    }

                    if (violation) {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Rules.CaseSensitivityInLinqExpressionsRule,
                            invocation.Syntax.GetLocation()
                        ));
                    }
                }
            } catch (Exception ex) {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rules.CodeAnalysisErrorRule,
                    context.Operation.Syntax.GetLocation(),
                    nameof(CaseSensitivityInLinqExpressionsAnalyzer),
                    ex,
                    ex.StackTrace.Replace("\n", " <-- ").Replace("\r", String.Empty)
                ));
            }
        }

        private void AnalyzeOperator(OperationAnalysisContext context) {
            try {
                if (context.Operation is IBinaryOperation op &&
                    (op.OperatorKind == BinaryOperatorKind.Equals ||
                        op.OperatorKind == BinaryOperatorKind.NotEquals) &&
                    (op.LeftOperand.IsOfType<String>() || op.RightOperand.IsOfType<String>()) &&
                    IsInLinqExpression(op)) {

                    context.ReportDiagnostic(Diagnostic.Create(
                        Rules.CaseSensitivityInLinqExpressionsRule,
                        op.Syntax.GetLocation()
                    ));
                }
            } catch (Exception ex) {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rules.CodeAnalysisErrorRule,
                    context.Operation.Syntax.GetLocation(),
                    nameof(CaseSensitivityInLinqExpressionsAnalyzer),
                    ex,
                    ex.StackTrace.Replace("\n", " <-- ").Replace("\r", String.Empty)
                ));
            }
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(
                Rules.CaseSensitivityInLinqExpressionsRule,
                Rules.CodeAnalysisErrorRule
            );

        private static Boolean IsInLinqExpression(IOperation operation) {
            if (operation == null) {
                throw new ArgumentNullException(nameof(operation));
            }

            // Find IAnonymousFunction parent, Find Queryable.ExtensionMethod invocation parent
            return operation.FindAncestorOfType<IAnonymousFunctionOperation>(out var function) &&
                function.FindAncestorOfType<IInvocationOperation>(out var invocation) &&
                invocation.TargetMethod.ContainingType.Is(typeof(Queryable));
        }
    }
}