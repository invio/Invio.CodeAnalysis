using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Invio.CodeAnalysis {
    public static class CodeAnalysisExtension {
        public static Boolean IsOfType<T>(this IOperation operation) {
            if (operation == null) {
                throw new ArgumentNullException(nameof(operation));
            }

            return operation.Type != null && operation.Type.Is<T>() ||
                operation is IConversionOperation conversion &&
                conversion.Operand.Type != null && conversion.Operand.Type.Is<T>();
        }

        public static Boolean Is<T>(this ITypeSymbol typeSymbol) {
            return typeSymbol.Is(typeof(T));
        }

        public static Boolean Is(this ITypeSymbol typeSymbol, Type type) {
            if (typeSymbol == null) {
                throw new ArgumentNullException(nameof(typeSymbol));
            }
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }

            if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType) {
                return $"{namedType.Name}`{namedType.TypeArguments.Length}" == type.Name &&
                    typeSymbol.ContainingNamespace.ToString() == type.Namespace &&
                    namedType.TypeArguments
                        .ZipFill(
                            type.GenericTypeArguments,
                            (symbol, typeArg) =>
                                symbol != null && typeArg != null && symbol.Is(typeArg))
                        .All(x => x);
            } else {
                return typeSymbol.Name == type.Name &&
                    typeSymbol.ContainingNamespace.ToString() == type.Namespace;
            }
        }

        public static Boolean IsNullValue(this IOperation operation) {
            if (operation == null) {
                throw new ArgumentNullException(nameof(operation));
            }

            while (operation != null) {
                switch (operation) {
                    case ILiteralOperation literal:
                        return !literal.ConstantValue.HasValue || literal.ConstantValue.Value == null;
                    case IConversionOperation conversion:
                        operation = conversion.Operand;
                        continue;
                    default:
                        return false;
                }
            }

            return false;
        }

        public static Type LoadType(this INamedTypeSymbol typeSymbol) {
            return typeSymbol.TryLoadType(out var type) ? type : null;
        }

        public static Boolean TryLoadType(this INamedTypeSymbol typeSymbol, out Type type) {
            if (typeSymbol == null) {
                throw new ArgumentNullException(nameof(typeSymbol));
            }

            try {
                var assembly = Assembly.Load(new AssemblyName(typeSymbol.ContainingAssembly.Name));
                if (typeSymbol.IsGenericType) {
                    var genericType = assembly.GetType(
                        $"{typeSymbol.ContainingNamespace}.{typeSymbol.Name}`{typeSymbol.TypeArguments.Length}"
                    );

                    if (genericType != null) {
                        var typeArguments =
                            typeSymbol.TypeArguments
                                .Select(t =>
                                    t is INamedTypeSymbol namedTypeArg ?
                                        namedTypeArg.LoadType() :
                                        null)
                                .ToArray();

                        if (typeArguments.All(t => t != null)) {
                            type = genericType.MakeGenericType(typeArguments);
                            return true;
                        }
                    }

                    type = null;
                    return false;
                } else {
                    type = assembly.GetType($"{typeSymbol.ContainingNamespace}.{typeSymbol.Name}");
                    return type != null;
                }
            } catch (FileNotFoundException) {
                type = null;
                return false;
            } catch (FileLoadException) {
                type = null;
                return false;
            }
        }

        public static Boolean FindAncestorOfType<TOperation>(
            this IOperation operation,
            out TOperation ancestor)
            where TOperation : IOperation {

            while (true) {
                if (operation.Parent is TOperation match) {
                    ancestor = match;
                    return true;
                }

                if (operation.Parent != null) {
                    operation = operation.Parent;
                } else {
                    ancestor = default(TOperation);
                    return false;
                }
            }
        }

        public static Boolean IsMethod(this IMethodSymbol symbol, MethodInfo method) {
            return symbol.Name == method.Name &&
                symbol.ContainingType.Is(method.DeclaringType) &&
                symbol.IsStatic == method.IsStatic &&
                (method.IsGenericMethod ?
                    symbol.IsGenericMethod :
                    !symbol.IsGenericMethod) &&
                // Allow for checks against either a concrete generic method, or a generic method
                // definition without type parameters
                (!method.IsGenericMethod || method.IsGenericMethodDefinition ||
                    TypeParametersEq(method.GetGenericArguments(), symbol.TypeArguments)) &&
                ParametersEq(method.GetParameters(), symbol.Parameters);
        }

        private static Boolean ParametersEq(
            IEnumerable<ParameterInfo> expectedParameters,
            IEnumerable<IParameterSymbol> parameterSymbols) {

            return expectedParameters
                .ZipFill(
                    parameterSymbols,
                    (expected, param) =>
                        expected != null &&
                        param != null &&
                        (expected.ParameterType.IsGenericParameter ||
                            param.Type.Is(expected.ParameterType)))
                .All(x => x);
        }

        private static Boolean TypeParametersEq(
            IEnumerable<Type> expectedTypes,
            IEnumerable<ITypeSymbol> typeParameters) {

            return expectedTypes
                .ZipFill(
                    typeParameters,
                    (expected, param) => expected != null && param != null && param.Is(expected))
                .All(x => x);
        }

        private static IEnumerable<TResult> ZipFill<T1, T2, TResult>(
            this IEnumerable<T1> source,
            IEnumerable<T2> other,
            Func<T1, T2, TResult> func) {

            using (var enum1 = source.GetEnumerator())
            using (var enum2 = other.GetEnumerator()) {

                var hasNext1 = enum1.MoveNext();
                var hasNext2 = enum2.MoveNext();

                while (hasNext1 || hasNext2) {
                    yield return func(
                        hasNext1 ? enum1.Current : default(T1),
                        hasNext2 ? enum2.Current : default(T2)
                    );

                    hasNext1 = hasNext1 && enum1.MoveNext();
                    hasNext2 = hasNext2 && enum2.MoveNext();
                }
            }
        }
    }
}
