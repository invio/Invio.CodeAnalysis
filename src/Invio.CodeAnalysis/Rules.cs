using System;
using Microsoft.CodeAnalysis;

namespace Invio.CodeAnalysis {
    /// <summary>
    /// Static class containing declarations of Invio Code Analysis Rules. Grouped in categories by
    /// 100s.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Category</term>
    ///     <description>Description</description>
    ///   </listheader>
    ///   <item>
    ///     <term>0000..0099</term>
    ///     <description>General Syntax Rules</description>
    ///   </item>
    ///   <item>
    ///     <term>0100..0199</term>
    ///     <description>Coding Style Rules</description>
    ///   </item>
    ///   <item>
    ///     <term>1000..1099</term>
    ///     <description>Linq Expression and Usage Rules</description>
    ///   </item>
    /// </list>
    /// </remarks>
    public static class Rules {
        #region Category 1000 Linq Expressions

        public const String CategoryName1000 = "Linq";

        public const String CaseSensitivityInLinqExpressionsRuleId = "INV1000";

        public static DiagnosticDescriptor CaseSensitivityInLinqExpressionsRule { get; } =
            new DiagnosticDescriptor(
                CaseSensitivityInLinqExpressionsRuleId,
                "Use of implicit SQL collation behavior in Linq statement.",
                "A string comparison in a linq statement that does not explicitly specify case " +
                "handling may result in unexpected behavior. However it may be necessary for " +
                "performance optimization.",
                CategoryName1000,
                DiagnosticSeverity.Info,
                true
            );

        #endregion

        // Put Normal Rules Above

        #region Error Reporting

        public const String CodeAnalysisErrorRuleId = "INV9999";

        public static DiagnosticDescriptor CodeAnalysisErrorRule { get; } =
            new DiagnosticDescriptor(
                CodeAnalysisErrorRuleId,
                "An Error occurred during code analysis.",
                "An error occurred in analyzer {0}: {1} Stack Trace: {2}",
                "CodeAnalysis",
                DiagnosticSeverity.Error,
                true
            );

        #endregion
    }
}
