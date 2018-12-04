using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Invio.CodeAnalysis {
    internal static class ReflectionHelper {
        public static MethodInfo GetFuncMethod<T1, T2, TResult>(Func<T1, T2, TResult> method) {
            if (method == null) {
                throw new ArgumentNullException(nameof(method));
            }

            return method.GetMethodInfo();
        }

        public static MethodInfo GetMethodFromExpression<TDefiningType>(
            Expression<Action<TDefiningType>> invokeMethod) {

            if (invokeMethod == null) {
                throw new ArgumentNullException(nameof(invokeMethod));
            }
            if (!(invokeMethod.Body is MethodCallExpression methodCall)) {
                throw new ArgumentException(
                    $"The body of the expression must be a {nameof(MethodCallExpression)}.",
                    nameof(invokeMethod)
                );
            }

            return methodCall.Method;
        }
    }
}