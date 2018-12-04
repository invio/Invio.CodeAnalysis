using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Xunit;

namespace Invio.CodeAnalysis.Test {
    /// <summary>
    /// Superclass of all Unit Tests for DiagnosticAnalyzers
    /// </summary>
    public abstract class DiagnosticVerifier {
        #region To be implemented by Test classes

        /// <summary>
        /// Get the CSharp analyzer being tested - to be implemented in non-abstract class
        /// </summary>
        protected abstract IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers();

        protected virtual IEnumerable<MetadataReference> GetAdditionalReferences() {
            return null;
        }

        #endregion

        #region Verifier wrappers

        /// <summary>
        /// Called to test a C# DiagnosticAnalyzer when applied on the single inputted string as a source
        /// Note: input a DiagnosticResult for each Diagnostic expected
        /// </summary>
        /// <param name="source">A class in the form of a string to run the analyzer on</param>
        /// <param name="expected">DiagnosticResults that should appear after the analyzer is run on the source</param>
        /// <param name="verifyIfCompiles">Verify if the source compiles</param>
        protected async Task VerifyCSharpDiagnostic(
            String source,
            DiagnosticResult[] expected = null,
            Boolean verifyIfCompiles = true) {

            var analyzers = GetDiagnosticAnalyzers().ToList();
            await VerifyDiagnostics(
                new[] { source },
                LanguageNames.CSharp,
                analyzers,
                expected ?? new DiagnosticResult[0],
                verifyIfCompiles
            );
        }

        /// <summary>
        /// Called to test a VB.NET DiagnosticAnalyzer when applied on the single inputted string as a source
        /// Note: input a DiagnosticResult for each Diagnostic expected
        /// </summary>
        /// <param name="source">A class in the form of a string to run the analyzer on</param>
        /// <param name="expected">DiagnosticResults that should appear after the analyzer is run on the source</param>
        /// <param name="verifyIfCompiles">Verify if the source compiles</param>
        protected async Task VerifyVisualBasicDiagnostic(
            String source,
            DiagnosticResult[] expected = null,
            Boolean verifyIfCompiles = true) {

            var analyzers = GetDiagnosticAnalyzers().ToList();
            await VerifyDiagnostics(
                new[] { source },
                LanguageNames.VisualBasic,
                analyzers,
                expected ?? new DiagnosticResult[0],
                verifyIfCompiles
            );
        }

        /// <summary>
        /// Called to test a C# DiagnosticAnalyzer when applied on the single inputted string as a source
        /// Note: input a DiagnosticResult for each Diagnostic expected
        /// </summary>
        /// <param name="source">A class in the form of a string to run the analyzer on</param>
        /// <param name="expected">DiagnosticResults that should appear after the analyzer is run on the source</param>
        /// <param name="verifyIfCompiles">Verify if the source compiles</param>
        protected async Task VerifyCSharpDiagnostic(
            String source,
            DiagnosticResult expected,
            Boolean verifyIfCompiles = true) {

            await VerifyCSharpDiagnostic(source, new[] { expected }, verifyIfCompiles);
        }

        /// <summary>
        /// Called to test a VB.NET DiagnosticAnalyzer when applied on the single inputted string as a source
        /// Note: input a DiagnosticResult for each Diagnostic expected
        /// </summary>
        /// <param name="source">A class in the form of a string to run the analyzer on</param>
        /// <param name="expected">DiagnosticResults that should appear after the analyzer is run on the source</param>
        /// <param name="verifyIfCompiles">Verify if the source compiles</param>
        protected async Task VerifyVisualBasicDiagnostic(
            String source,
            DiagnosticResult expected,
            Boolean verifyIfCompiles = true) {

            await VerifyVisualBasicDiagnostic(source, new[] { expected }, verifyIfCompiles);
        }

        /// <summary>
        /// General method that gets a collection of actual diagnostics found in the source after the analyzer is run,
        /// then verifies each of them.
        /// </summary>
        /// <param name="sources">An array of strings to create source documents from to run the analyzers on</param>
        /// <param name="language">The language of the classes represented by the source strings</param>
        /// <param name="analyzers">The analyzers to be run on the source code</param>
        /// <param name="expected">DiagnosticResults that should appear after the analyzer is run on the sources</param>
        /// <param name="includeCompilerDiagnostics">Verify built-in compile diagnostics</param>
        private async Task VerifyDiagnostics(
            String[] sources,
            String language,
            List<DiagnosticAnalyzer> analyzers,
            DiagnosticResult[] expected,
            Boolean includeCompilerDiagnostics = true) {

            var diagnostics = await GetSortedDiagnostics(
                sources,
                language,
                analyzers,
                GetAdditionalReferences(),
                includeCompilerDiagnostics
            );

            VerifyDiagnosticResults(diagnostics, analyzers, language, expected);
        }

        #endregion

        private static readonly MetadataReference CorlibReference =
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location);

        private static readonly MetadataReference SystemCoreReference =
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);

        private static readonly MetadataReference CSharpSymbolsReference =
            MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location);

        private static readonly MetadataReference CodeAnalysisReference =
            MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location);

        private static readonly MetadataReference SystemDiagReference =
            MetadataReference.CreateFromFile(typeof(Process).Assembly.Location);

        private static readonly CompilationOptions CSharpDefaultOptions =
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        private static readonly CompilationOptions VisualBasicDefaultOptions =
            new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        internal static string DefaultFilePathPrefix = "Test";
        internal static string CSharpDefaultFileExt = "cs";
        internal static string VisualBasicDefaultExt = "vb";
        internal static string TestProjectName = "TestProject";

        #region  Get Diagnostics

        /// <summary>
        /// Given classes in the form of strings, their language, and an IDiagnosticAnlayzer to apply to it, return the diagnostics found in the string after converting it to a document.
        /// </summary>
        /// <param name="sources">Classes in the form of strings</param>
        /// <param name="language">The language the source classes are in</param>
        /// <param name="analyzers">The analyzers to be run on the sources</param>
        /// <param name="references">Additional referenced modules</param>
        /// <param name="includeCompilerDiagnostics">Get compiler diagnostics too</param>
        /// <returns>An IEnumerable of Diagnostics that surfaced in the source code, sorted by Location</returns>
        private static async Task<Diagnostic[]> GetSortedDiagnostics(
            String[] sources,
            String language,
            List<DiagnosticAnalyzer> analyzers,
            IEnumerable<MetadataReference> references = null,
            Boolean includeCompilerDiagnostics = false) {

            return await GetSortedDiagnosticsFromDocuments(
                analyzers,
                GetDocuments(sources, language, references),
                includeCompilerDiagnostics
            );
        }

        /// <summary>
        /// Given an analyzer and a document to apply it to, run the analyzer and gather an array of diagnostics found in it.
        /// The returned diagnostics are then ordered by location in the source document.
        /// </summary>
        /// <param name="analyzers">The analyzer to run on the documents</param>
        /// <param name="documents">The Documents that the analyzer will be run on</param>
        /// <param name="includeCompilerDiagnostics">Get compiler diagnostics too</param>
        /// <returns>An IEnumerable of Diagnostics that surfaced in the source code, sorted by Location</returns>
        protected static async Task<Diagnostic[]> GetSortedDiagnosticsFromDocuments(
            List<DiagnosticAnalyzer> analyzers,
            Document[] documents,
            Boolean includeCompilerDiagnostics = false) {

            var projects = new HashSet<Project>();
            foreach (var document in documents) {
                projects.Add(document.Project);
            }

            var diagnostics = new List<Diagnostic>();
            foreach (var project in projects) {
                var compilation = await project.GetCompilationAsync();
                var compilationWithAnalyzers =
                    compilation.WithAnalyzers(ImmutableArray.Create(analyzers.ToArray()));
                var diags = includeCompilerDiagnostics ?
                    await compilationWithAnalyzers.GetAllDiagnosticsAsync() :
                    await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

                foreach (var diag in diags) {
                    if (diag.Location == Location.None || diag.Location.IsInMetadata) {
                        diagnostics.Add(diag);
                    } else {
                        foreach (var document in documents) {
                            var tree = await document.GetSyntaxTreeAsync();
                            if (tree == diag.Location.SourceTree) {
                                diagnostics.Add(diag);
                            }
                        }
                    }
                }
            }

            var results = SortDiagnostics(diagnostics);
            return results;
        }

        /// <summary>
        /// Sort diagnostics by location in source document
        /// </summary>
        /// <param name="diagnostics">The list of Diagnostics to be sorted</param>
        /// <returns>An IEnumerable containing the Diagnostics in order of Location</returns>
        private static Diagnostic[] SortDiagnostics(IEnumerable<Diagnostic> diagnostics) {
            return diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
        }

        #endregion

        #region Set up compilation and documents

        /// <summary>
        /// Given an array of strings as sources and a language, turn them into a project and return the documents and spans of it.
        /// </summary>
        /// <param name="sources">Classes in the form of strings</param>
        /// <param name="language">The language the source code is in</param>
        /// <returns>A Tuple containing the Documents produced from the sources and their TextSpans if relevant</returns>
        private static Document[] GetDocuments(
            String[] sources,
            String language,
            IEnumerable<MetadataReference> references = null) {

            if (language != LanguageNames.CSharp && language != LanguageNames.VisualBasic) {
                throw new ArgumentException("Unsupported Language");
            }

            var project = CreateProject(sources, language, references);
            var documents = project.Documents.ToArray();

            if (sources.Length != documents.Length) {
                throw new SystemException(
                    "Amount of sources did not match amount of Documents created");
            }

            return documents;
        }

        /// <summary>
        /// Create a Document from a string through creating a project that contains it.
        /// </summary>
        /// <param name="source">Classes in the form of a string</param>
        /// <param name="language">The language the source code is in</param>
        /// <returns>A Document created from the source string</returns>
        protected static Document CreateDocument(
            String source,
            String language = LanguageNames.CSharp,
            IEnumerable<MetadataReference> references = null) {

            return CreateProject(new[] { source }, language, references).Documents.First();
        }

        /// <summary>
        /// Create a project using the inputted strings as sources.
        /// </summary>
        /// <param name="sources">Classes in the form of strings</param>
        /// <param name="language">The language the source code is in</param>
        /// <returns>A Project created out of the Documents created from the source strings</returns>
        private static Project CreateProject(
            String[] sources,
            String language = LanguageNames.CSharp,
            IEnumerable<MetadataReference> references = null) {

            var fileNamePrefix = DefaultFilePathPrefix;
            var fileExt =
                language == LanguageNames.CSharp ? CSharpDefaultFileExt : VisualBasicDefaultExt;

            var options =
                language == LanguageNames.CSharp ? CSharpDefaultOptions : VisualBasicDefaultOptions;

            var projectId = ProjectId.CreateNewId(debugName: TestProjectName);

            var solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, TestProjectName, TestProjectName, language)
                .AddMetadataReference(projectId, CorlibReference)
                .AddMetadataReference(projectId, SystemCoreReference)
                .AddMetadataReference(projectId, CSharpSymbolsReference)
                .AddMetadataReference(projectId, CodeAnalysisReference)
                .AddMetadataReference(projectId, SystemDiagReference)
                .WithProjectCompilationOptions(projectId, options);

            if (references != null) {
                solution = solution.AddMetadataReferences(projectId, references);
            }

            var count = 0;
            foreach (var source in sources) {
                var newFileName = fileNamePrefix + count + "." + fileExt;
                var documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
                solution = solution.AddDocument(documentId, newFileName, SourceText.From(source));
                count++;
            }

            return solution.GetProject(projectId);
        }

        #endregion

        #region Actual comparisons and verifications

        /// <summary>
        /// Checks each of the actual Diagnostics found and compares them with the corresponding DiagnosticResult in the array of expected results.
        /// Diagnostics are considered equal only if the DiagnosticResultLocation, Id, Severity, and Message of the DiagnosticResult match the actual diagnostic.
        /// </summary>
        /// <param name="actualResults">The Diagnostics found by the compiler after running the analyzer on the source code</param>
        /// <param name="analyzers">The analyzers that was being run on the sources</param>
        /// <param name="expectedResults">Diagnostic Results that should have appeared in the code</param>
        private static void VerifyDiagnosticResults(
            ICollection<Diagnostic> actualResults,
            List<DiagnosticAnalyzer> analyzers,
            String language,
            params DiagnosticResult[] expectedResults) {

            var expectedCount = expectedResults.Length;
            var actualCount = actualResults.Count;

            Assert.True(
                expectedCount == actualCount,
                String.Format(
                    "Mismatch between number of diagnostics returned, expected \"{0}\" actual \"{1}\" (Language:{3})\n\nDiagnostics:\n{2}\n",
                    expectedCount,
                    actualCount,
                    actualResults.Any() ?
                        FormatDiagnostics(analyzers[0], actualResults.ToArray()) :
                        "    NONE.",
                    language
                )
            );

            //For debug purpose
            foreach (var actual in actualResults) {
                Console.WriteLine("Bug : {0} ({1}) {2}", actual.Id, actual.Severity,
                    actual.Location.GetLineSpan().StartLinePosition);
            }

            for (var i = 0; i < expectedResults.Length; i++) {
                var actual = actualResults.ElementAt(i);

                var expected = expectedResults[i];

                Assert.True(
                    actual.Id == expected.Id,
                    String.Format(
                        "Expected diagnostic id to be \"{0}\" was \"{1}\"\n\nDiagnostic:\n    {2}\n(Language: {3})",
                        expected.Id,
                        actual.Id,
                        FormatDiagnostics(analyzers[0], actual),
                        language
                    )
                );

                Assert.True(
                    actual.Severity == expected.Severity,
                    String.Format(
                        "Expected diagnostic severity to be \"{0}\" was \"{1}\"\n\nDiagnostic:\n    {2}\n(Language: {3})",
                        expected.Severity,
                        actual.Severity,
                        FormatDiagnostics(analyzers[0], actual),
                        language
                    )
                );

                if (expected.Message != null) {
                    Assert.True(
                        actual.GetMessage() == expected.Message,
                        String.Format(
                            "Expected diagnostic message to be \"{0}\" was \"{1}\"\n\nDiagnostic:\n    {2}\n(Language: {3})",
                            expected.Message,
                            actual.GetMessage(),
                            FormatDiagnostics(analyzers[0], actual),
                            language
                        )
                    );
                }

                if (expected.Line >= 0) {
                    VerifyDiagnosticLocation(
                        analyzers[0],
                        actual,
                        actual.Location,
                        expected.Locations.First(),
                        language
                    );

                    var additionalLocations = actual.AdditionalLocations.ToArray();

                    Assert.True(additionalLocations.Length == expected.Locations.Length - 1,
                        String.Format(
                            "Expected {0} additional locations but got {1} for Diagnostic:\n" +
                            "    {2}\n(Language: {3})",
                            expected.Locations.Length - 1,
                            additionalLocations.Length,
                            FormatDiagnostics(analyzers[0], actual),
                            language
                        )
                    );

                    for (var j = 0; j < additionalLocations.Length; ++j) {
                        VerifyDiagnosticLocation(
                            analyzers[0],
                            actual,
                            additionalLocations[j],
                            expected.Locations[j + 1],
                            language
                        );
                    }
                }
            }
        }

        /// <summary>
        /// Helper method to VerifyDiagnosticResult that checks the location of a diagnostic and compares it with the location in the expected DiagnosticResult.
        /// </summary>
        /// <param name="analyzer">The analyzer that was being run on the sources</param>
        /// <param name="diagnostic">The diagnostic that was found in the code</param>
        /// <param name="actual">The Location of the Diagnostic found in the code</param>
        /// <param name="expected">The DiagnosticResultLocation that should have been found</param>
        private static void VerifyDiagnosticLocation(
            DiagnosticAnalyzer analyzer,
            Diagnostic diagnostic,
            Location actual,
            DiagnosticResultLocation expected,
            String language) {
            var actualSpan = actual.GetLineSpan();

            Assert.True(
                actualSpan.Path == expected.Path ||
                (actualSpan.Path != null &&
                    actualSpan.Path.Contains("Test0.") && expected.Path.Contains("Test.")),
                String.Format(
                    "Expected diagnostic to be in file \"{0}\" was actually in file \"{1}\"\n\nDiagnostic:\n    {2}\n(Language: {3})",
                    expected.Path,
                    actualSpan.Path,
                    FormatDiagnostics(analyzer, diagnostic),
                    language
                )
            );

            var actualLinePosition = actualSpan.StartLinePosition;

            // Only check line position if there is an actual line in the real diagnostic
            if (actualLinePosition.Line > 0) {
                Assert.True(
                    actualLinePosition.Line + 1 == expected.Line,
                    String.Format(
                        "Expected diagnostic to be on line \"{0}\" was actually on line \"{1}\"\n\nDiagnostic:\n    {2}\n(Language: {3})",
                        expected.Line,
                        actualLinePosition.Line + 1,
                        FormatDiagnostics(analyzer, diagnostic),
                        language
                    )
                );
            }

            // Only check column position if there is an actual column position in the real diagnostic
            if (expected.Column != -1 && actualLinePosition.Character > 0) {
                Assert.True(
                    actualLinePosition.Character + 1 == expected.Column,
                    String.Format(
                        "Expected diagnostic to start at column \"{0}\" was actually at column \"{1}\"\n\nDiagnostic:\n    {2}\n(Language: {3})",
                        expected.Column,
                        actualLinePosition.Character + 1,
                        FormatDiagnostics(analyzer, diagnostic),
                        language
                    )
                );
            }
        }

        #endregion

        #region Formatting Diagnostics

        /// <summary>
        /// Helper method to format a Diagnostic into an easily readable string
        /// </summary>
        /// <param name="analyzer">The analyzer that this verifier tests</param>
        /// <param name="diagnostics">The Diagnostics to be formatted</param>
        /// <returns>The Diagnostics formatted as a string</returns>
        private static string FormatDiagnostics(
            DiagnosticAnalyzer analyzer,
            params Diagnostic[] diagnostics) {
            var builder = new StringBuilder();
            for (var i = 0; i < diagnostics.Length; ++i) {
                builder.AppendLine("// " + diagnostics[i].ToString());

                var analyzerType = analyzer.GetType();
                var rules = analyzer.SupportedDiagnostics;

                foreach (var rule in rules) {
                    if (rule != null && rule.Id == diagnostics[i].Id) {
                        var location = diagnostics[i].Location;
                        if (location == Location.None) {
                            builder.AppendFormat("GetGlobalResult({0}.{1})", analyzerType.Name,
                                rule.Id);
                        } else {
                            Assert.True(
                                location.IsInSource,
                                $"Test base does not currently handle diagnostics in metadata " +
                                $"locations. Diagnostic in metadata: {diagnostics[i]}"
                            );

                            string resultMethodName =
                                diagnostics[i].Location.SourceTree.FilePath.EndsWith(".cs") ?
                                    "GetCSharpResultAt" :
                                    "GetBasicResultAt";
                            var linePosition =
                                diagnostics[i].Location.GetLineSpan().StartLinePosition;

                            builder.AppendFormat("{0}({1}, {2}, {3}.{4})",
                                resultMethodName,
                                linePosition.Line + 1,
                                linePosition.Character + 1,
                                analyzerType.Name,
                                rule.Id);
                        }

                        if (i != diagnostics.Length - 1) {
                            builder.Append(',');
                        }

                        builder.AppendLine();
                        break;
                    }
                }
            }

            return builder.ToString();
        }

        #endregion
    }

    /// <summary>
    /// Location where the diagnostic appears, as determined by path, line number, and column number.
    /// </summary>
    public struct DiagnosticResultLocation {
        public DiagnosticResultLocation(String path, Int32 line, Int32 column) {
            if (line < -1) {
                throw new ArgumentOutOfRangeException(nameof(line), "line must be >= -1");
            }

            if (column < -1) {
                throw new ArgumentOutOfRangeException(nameof(column), "column must be >= -1");
            }

            this.Path = path;
            this.Line = line;
            this.Column = column;
        }

        public String Path { get; }
        public Int32 Line { get; }
        public Int32 Column { get; }
    }

    /// <summary>
    /// Struct that stores information about a Diagnostic appearing in a source
    /// </summary>
    public struct DiagnosticResult {
        private DiagnosticResultLocation[] locations;

        public DiagnosticResultLocation[] Locations {
            get => this.locations ?? (this.locations = new DiagnosticResultLocation[] { });

            set => this.locations = value;
        }

        public DiagnosticSeverity Severity { get; set; }

        public String Id { get; set; }

        public String Message { get; set; }

        public String Path => this.Locations.Length > 0 ? this.Locations[0].Path : "";

        public Int32 Line => this.Locations.Length > 0 ? this.Locations[0].Line : -1;

        public Int32 Column => this.Locations.Length > 0 ? this.Locations[0].Column : -1;

        //TODO: Find a better way to specify .vb

        public DiagnosticResult WithLocation(Int32 line) {
            return this.WithLocation("Test0.cs", line, -1);
        }

        public DiagnosticResult WithLocation(Int32 line, Int32 column) {
            return this.WithLocation("Test0.cs", line, column);
        }

        public DiagnosticResult WithLocation(String path, Int32 line) {
            return this.WithLocation(path, line, -1);
        }

        public DiagnosticResult WithLocation(String path, Int32 line, Int32 column) {
            var result = this;
            Array.Resize(ref result.locations, (result.locations?.Length ?? 0) + 1);
            result.locations[result.locations.Length - 1] =
                new DiagnosticResultLocation(path, line, column);
            return result;
        }
    }
}