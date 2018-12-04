# Invio.CodeAnalysis

This library is a collection of general purpose code analysis rules for the [Roslyn Compiler](https://github.com/dotnet/roslyn) utilized by other Invio libraries, as well as helper functions for developing rules.

_Note: This library is currently a proof of concept, and contains only a single code analysis rule._

## Usage

In order to apply Invio.CodeAnalysis rules to your library, simply reference the Invio.CodeAnalysis nuget package. MSBuild will automatically detect the analyzer rules defined in the library and run them against your library during compilation.

## Building, Testing, Packaging

Invio.CodeAnalysis can be built simply by invoking `dotnet run` from the root or from the `src/Invio.CodeAnalysis` folder. Unit tests implemented with `XUnit` are included in `test/Invio.CodeAnalysis.Test` and can be run with `dotnet test`.

The Invio.CodeAnalysis.Example project is included as an example consumer of the analyzers, and for MSBuild testing.

The Invio.CodeAnalysis package is configured via Invio.CodeAnalysis.nuspec. A nuspec file is used as opposed to MSBuild project properties because analyzer dlls must be placed in a different folder than normal libraries.

## Debugging

When adding code analysis rules it is sometimes necessary to debug behavior against a specific codebase. In order to do this it is neccessary to attach to the compiler process that is building the codebase.

1. Run `dotnet build --no-incremental --no-restore --force -v d` for the project you would like to debug.
1. In the build output, find the command used for `Task "Csc"`
    * This should look something like:  
    `/path_to_dotnet/dotnet /path_to_dotnet_sdl/Roslyn/bincore/csc.dll /noconfig ... /define:TRACE;DEBUG;NETSTANDARD;NETSTANDARD2_0 /reference:... code files ...`
1. Configure your debugger to launch `csc.dll` with the specified arguments.  
    * _Note: It is sometimes necessary to drop some generated code files from the commandline arguments._
1. At this point you should be able to debug your analyzer rules as normal.

## TODO

* So far I've been unable to get MSBuild to load an analyzer dll that has non-framework dependencies. This is a blocker for having a shared library that is useful for implementing library specific code analysis rules.
    * Get `Invio.CodeAnalysis` working with a reference to the [Invio.Extensions.Reflection](https://github.com/invio/Invio.Extensions.Reflection) package and eliminate copy-pasted code in TypeExtensions.cs and ReflectionHelper.cs.
    * Create a `.Analyzer` library specific to [Invio.QueryProvider](https://github.com/invio/Invio.QueryProvider) that depends on this library.
* Add additional boilerplate support for creating syntax and operation specific code analysis rules.
* Add more rules.