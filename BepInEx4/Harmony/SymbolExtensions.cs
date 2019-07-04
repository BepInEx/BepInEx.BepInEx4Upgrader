using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Harmony
{
    public static class SymbolExtensions
    {
        public static MethodInfo GetMethodInfo(Expression<Action> expression)
        {
            return GetMethodInfo(expression);
        }

        public static MethodInfo GetMethodInfo<T>(Expression<Action<T>> expression)
        {
            return GetMethodInfo(expression);
        }

        public static MethodInfo GetMethodInfo<T, TResult>(Expression<Func<T, TResult>> expression)
        {
            return GetMethodInfo(expression);
        }

        public static MethodInfo GetMethodInfo(LambdaExpression expression)
        {
            var methodCallExpression = expression.Body as MethodCallExpression;
            if (methodCallExpression == null)
                throw new ArgumentException("Invalid Expression. Expression should consist of a Method call only.");
            var method = methodCallExpression.Method;
            if (method == null) throw new Exception("Cannot find method for expression " + expression);
            return method;
        }
    }
}