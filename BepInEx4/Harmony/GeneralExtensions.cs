using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Harmony
{
    public static class GeneralExtensions
    {
        public static string Description(this Type[] parameters)
        {
            var source = parameters.Select(delegate(Type p)
            {
                if (p != null) return p.FullName;
                return "null";
            });
            return "(" + source.Aggregate("", delegate(string s, string x)
            {
                if (s.Length != 0) return s + ", " + x;
                return x;
            }) + ")";
        }

        public static string FullDescription(this MethodBase method)
        {
            return method.DeclaringType.FullName + "." + method.Name + (from p in method.GetParameters()
                       select p.ParameterType).ToArray().Description();
        }

        public static Type[] Types(this ParameterInfo[] pinfo)
        {
            return (from pi in pinfo
                select pi.ParameterType).ToArray();
        }

        public static T GetValueSafe<S, T>(this Dictionary<S, T> dictionary, S key)
        {
            T result;
            if (dictionary.TryGetValue(key, out result)) return result;
            return default;
        }

        public static T GetTypedValue<T>(this Dictionary<string, object> dictionary, string key)
        {
            object obj;
            if (dictionary.TryGetValue(key, out obj) && obj is T) return (T) obj;
            return default;
        }
    }
}