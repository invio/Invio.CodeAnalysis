using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Invio.CodeAnalysis {
    public static class TypeExtensions {
        private static ConcurrentDictionary<Tuple<Type, Type>, bool>
            isDerivativeOfCache { get; }

        static TypeExtensions() {
            isDerivativeOfCache = new ConcurrentDictionary<Tuple<Type, Type>, bool>();
        }

        public static bool IsDerivativeOf(this Type type, Type parentType) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            } else if (parentType == null) {
                throw new ArgumentNullException(nameof(parentType));
            }

            return isDerivativeOfCache.GetOrAdd(
                Tuple.Create(type, parentType),
                tuple => IsDerivativeOfImpl(tuple.Item1, tuple.Item2)
            );
        }

        private static bool IsDerivativeOfImpl(Type type, Type parentType) {
            if (type == parentType || parentType == typeof(object)) {
                return true;
            }

            var parentTypeInfo = parentType.GetTypeInfo();
            if (parentType.IsConstructedGenericType) {
                return parentTypeInfo.IsAssignableFrom(type.GetTypeInfo());
            }
            if (!parentTypeInfo.IsGenericType) {
                return parentTypeInfo.IsAssignableFrom(type.GetTypeInfo());
            }

            IEnumerable<Type> potentialMatches;

            if (parentTypeInfo.IsInterface) {
                potentialMatches = new [] { type }.Concat(type.GetTypeInfo().ImplementedInterfaces);
            } else {
                potentialMatches = GetBaseTypes(type);
            }

            return
                potentialMatches
                    .Select(potentialMatch => potentialMatch.GetTypeInfo())
                    .Where(potentialMatch => potentialMatch.IsGenericType)
                    .Select(potentialMatch => potentialMatch.GetGenericTypeDefinition())
                    .Any(potentialMatch => potentialMatch == parentType);
        }

        private static IEnumerable<Type> GetBaseTypes(Type type) {
            while (type != null) {
                yield return type;

                type = type.GetTypeInfo().BaseType;
            }
        }
    }
}