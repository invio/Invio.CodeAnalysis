using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Invio.CodeAnalysis.Test {
    public class CaseSensitivityInLinqExpressionsTest : DiagnosticVerifier {
        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers() {
            return new[] { new CaseSensitivityInLinqExpressionsAnalyzer() };
        }

        protected override IEnumerable<MetadataReference> GetAdditionalReferences() {
            return new[] {
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                MetadataReference.CreateFromFile(typeof(IQueryable<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Queryable).Assembly.Location),
            };
        }

        [Fact]
        public async Task WarnOnUseOfStringEqualsOperator() {
            const string testCode = @"
                using System;
                using System.Linq;

                namespace TestCase {
                    public class Example {
                        public IQueryable<TestType> Go(IQueryable<TestType> queryable) {
                            return queryable.Where(t => t.Value == ""Foo"");
                        }
                    }

                    public class TestType {
                        public String Value { get; set; }
                    }
                }";

            var expected = new DiagnosticResult {
                Id = Rules.CaseSensitivityInLinqExpressionsRuleId,
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 8, 57)
                }
            };

            await VerifyCSharpDiagnostic(testCode, expected);
        }

        [Fact]
        public async Task WarnOnUseOfStringNotEqualsOperator() {
            const string testCode = @"
                using System;
                using System.Linq;

                namespace TestCase {
                    public class Example {
                        public IQueryable<TestType> Go(IQueryable<TestType> queryable) {
                            return queryable.Where(t => t.Value != ""Foo"");
                        }
                    }

                    public class TestType {
                        public String Value { get; set; }
                    }
                }";

            var expected = new DiagnosticResult {
                Id = Rules.CaseSensitivityInLinqExpressionsRuleId,
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 8, 57)
                }
            };

            await VerifyCSharpDiagnostic(testCode, expected);
        }

        [Fact]
        public async Task WarnOnUseOfStringEqualsMethod() {
            const string testCode = @"
                using System;
                using System.Linq;

                namespace TestCase {
                    public class Example {
                        public IQueryable<TestType> Go(IQueryable<TestType> queryable) {
                            return queryable.Where(t => t.Value.Equals(""Foo""));
                        }
                    }

                    public class TestType {
                        public String Value { get; set; }
                    }
                }";

            var expected = new DiagnosticResult {
                Id = Rules.CaseSensitivityInLinqExpressionsRuleId,
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 8, 57)
                }
            };

            await VerifyCSharpDiagnostic(testCode, expected);
        }

        [Fact]
        public async Task WarnOnUseOfObjectEqualsMethod() {
            const string testCode = @"
                using System;
                using System.Linq;

                namespace TestCase {
                    public class Example {
                        public IQueryable<TestType> Go(IQueryable<TestType> queryable) {
                            return queryable.Where(t => 123.Equals(t.Value));
                        }
                    }

                    public class TestType {
                        public String Value { get; set; }
                    }
                }";

            var expected = new DiagnosticResult {
                Id = Rules.CaseSensitivityInLinqExpressionsRuleId,
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 8, 57)
                }
            };

            await VerifyCSharpDiagnostic(testCode, expected);
        }

        [Fact]
        public async Task WarnOnUseOfStaticStringEqualsMethod() {
            const string testCode = @"
                using System;
                using System.Linq;

                namespace TestCase {
                    public class Example {
                        public IQueryable<TestType> Go(IQueryable<TestType> queryable) {
                            return queryable.Where(t => String.Equals(t.Value, ""Foo""));
                        }
                    }

                    public class TestType {
                        public String Value { get; set; }
                    }
                }";

            var expected = new DiagnosticResult {
                Id = Rules.CaseSensitivityInLinqExpressionsRuleId,
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 8, 57)
                }
            };

            await VerifyCSharpDiagnostic(testCode, expected);
        }

        [Fact]
        public async Task WarnOnUseOfStaticObjectEqualsMethod() {
            const string testCode = @"
                using System;
                using System.Linq;

                namespace TestCase {
                    public class Example {
                        public IQueryable<TestType> Go(IQueryable<TestType> queryable) {
                            return queryable.Where(t => Object.Equals(t.Value, ""Foo""));
                        }
                    }

                    public class TestType {
                        public String Value { get; set; }
                    }
                }";

            var expected = new DiagnosticResult {
                Id = Rules.CaseSensitivityInLinqExpressionsRuleId,
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 8, 57)
                }
            };

            await VerifyCSharpDiagnostic(testCode, expected);
        }

        [Fact]
        public async Task WarnOnUseOfEnumerableOfStringContainsMethod() {
            const string testCode = @"
                using System;
                using System.Collections.Generic;
                using System.Linq;

                namespace TestCase {
                    public class Example {
                        public IQueryable<TestType> Go(IQueryable<TestType> queryable, IEnumerable<String> list) {
                            return queryable.Where(t => list.Contains(t.Value));
                        }
                    }

                    public class TestType {
                        public String Value { get; set; }
                    }
                }";

            var expected = new DiagnosticResult {
                Id = Rules.CaseSensitivityInLinqExpressionsRuleId,
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 9, 57)
                }
            };

            await VerifyCSharpDiagnostic(testCode, expected);
        }

        [Fact]
        public async Task WarnOnUseOfStringArrayContainsMethod() {
            const string testCode = @"
                using System;
                using System.Linq;

                namespace TestCase {
                    public class Example {
                        public IQueryable<TestType> Go(IQueryable<TestType> queryable, String[] ary) {
                            return queryable.Where(t => ary.Contains(t.Value));
                        }
                    }

                    public class TestType {
                        public String Value { get; set; }
                    }
                }";

            var expected = new DiagnosticResult {
                Id = Rules.CaseSensitivityInLinqExpressionsRuleId,
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 8, 57)
                }
            };

            await VerifyCSharpDiagnostic(testCode, expected);
        }

        [Fact]
        public async Task WarnOnUseOfStringSetContainsMethod() {
            const string testCode = @"
                using System;
                using System.Collections.Generic;
                using System.Linq;

                namespace TestCase {
                    public class Example {
                        public IQueryable<TestType> Go(IQueryable<TestType> queryable, ISet<String> set) {
                            return queryable.Where(t => set.Contains(t.Value));
                        }
                    }

                    public class TestType {
                        public String Value { get; set; }
                    }
                }";

            var expected = new DiagnosticResult {
                Id = Rules.CaseSensitivityInLinqExpressionsRuleId,
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 9, 57)
                }
            };

            await VerifyCSharpDiagnostic(testCode, expected);
        }

        [Fact]
        public async Task WarnOnUseOfStringListContainsMethod() {
            const string testCode = @"
                using System;
                using System.Collections.Generic;
                using System.Linq;

                namespace TestCase {
                    public class Example {
                        public IQueryable<TestType> Go(IQueryable<TestType> queryable, List<String> list) {
                            return queryable.Where(t => list.Contains(t.Value));
                        }
                    }

                    public class TestType {
                        public String Value { get; set; }
                    }
                }";

            var expected = new DiagnosticResult {
                Id = Rules.CaseSensitivityInLinqExpressionsRuleId,
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 9, 57)
                }
            };

            await VerifyCSharpDiagnostic(testCode, expected);
        }

        [Fact]
        public async Task StringEqualWithComparisionOK() {
            const string testCode = @"
                using System;
                using System.Linq;

                namespace TestCase {
                    public class Example {
                        public IQueryable<TestType> Go(IQueryable<TestType> queryable) {
                            return queryable
                                .Where(t => t.Value.Equals(""Foo"", StringComparison.Ordinal));
                        }
                    }

                    public class TestType {
                        public String Value { get; set; }
                    }
                }";

            await VerifyCSharpDiagnostic(testCode);
        }

        [Fact]
        public async Task StaticStringEqualsWithComparisionOK() {
            const string testCode = @"
                using System;
                using System.Linq;

                namespace TestCase {
                    public class Example {
                        public IQueryable<TestType> Go(IQueryable<TestType> queryable) {
                            return queryable
                                .Where(t =>
                                    String.Equals(t.Value, ""Foo"", StringComparison.Ordinal)
                                );
                        }
                    }

                    public class TestType {
                        public String Value { get; set; }
                    }
                }";

            await VerifyCSharpDiagnostic(testCode);
        }

        [Fact]
        public async Task EnumerableOfStringContainsWithComparerOK() {
            const string testCode = @"
                using System;
                using System.Collections.Generic;
                using System.Linq;

                namespace TestCase {
                    public class Example {
                        public IQueryable<TestType> Go(IQueryable<TestType> queryable, IEnumerable<String> list) {
                            return queryable.Where(t => list.Contains(t.Value, StringComparer.Ordinal));
                        }
                    }

                    public class TestType {
                        public String Value { get; set; }
                    }
                }";

            await VerifyCSharpDiagnostic(testCode);
        }

        [Fact]
        public async Task ListOfStringContainsWithComparerOK() {
            const string testCode = @"
                using System;
                using System.Collections.Generic;
                using System.Linq;

                namespace TestCase {
                    public class Example {
                        public IQueryable<TestType> Go(IQueryable<TestType> queryable, List<String> list) {
                            return queryable.Where(t => list.Contains(t.Value, StringComparer.Ordinal));
                        }
                    }

                    public class TestType {
                        public String Value { get; set; }
                    }
                }";

            await VerifyCSharpDiagnostic(testCode);
        }

        [Theory]
        [InlineData("==")]
        [InlineData("!=")]
        public async Task NullCheckOK(String op) {
            var testCode = $@"
                using System;
                using System.Linq;

                namespace TestCase {{
                    public class Example {{
                        public IQueryable<TestType> Go(IQueryable<TestType> queryable) {{
                            return queryable.Where(t => t.Value {op} null);
                        }}
                    }}

                    public class TestType {{
                        public String Value {{ get; set; }}
                    }}
                }}";

            await VerifyCSharpDiagnostic(testCode);
        }
    }
}
