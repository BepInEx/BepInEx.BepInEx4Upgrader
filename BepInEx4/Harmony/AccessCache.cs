using System;
using System.Collections.Generic;
using System.Reflection;

namespace Harmony
{
    public class AccessCache
    {
        private readonly Dictionary<Type, Dictionary<string, Dictionary<int, MethodBase>>> methods =
            new Dictionary<Type, Dictionary<string, Dictionary<int, MethodBase>>>();

        private readonly Dictionary<Type, Dictionary<string, FieldInfo>> fields =
            new Dictionary<Type, Dictionary<string, FieldInfo>>();

        private readonly Dictionary<Type, Dictionary<string, PropertyInfo>> properties =
            new Dictionary<Type, Dictionary<string, PropertyInfo>>();

        [UpgradeToLatestVersion(1)]
        public FieldInfo GetFieldInfo(Type type, string name)
        {
            Dictionary<string, FieldInfo> dictionary = null;
            if (!fields.TryGetValue(type, out dictionary))
            {
                dictionary = new Dictionary<string, FieldInfo>();
                fields.Add(type, dictionary);
            }

            FieldInfo fieldInfo = null;
            if (!dictionary.TryGetValue(name, out fieldInfo))
            {
                fieldInfo = AccessTools.Field(type, name);
                dictionary.Add(name, fieldInfo);
            }

            return fieldInfo;
        }

        public PropertyInfo GetPropertyInfo(Type type, string name)
        {
            Dictionary<string, PropertyInfo> dictionary = null;
            if (!properties.TryGetValue(type, out dictionary))
            {
                dictionary = new Dictionary<string, PropertyInfo>();
                properties.Add(type, dictionary);
            }

            PropertyInfo propertyInfo = null;
            if (!dictionary.TryGetValue(name, out propertyInfo))
            {
                propertyInfo = AccessTools.Property(type, name);
                dictionary.Add(name, propertyInfo);
            }

            return propertyInfo;
        }

        private static int CombinedHashCode(IEnumerable<object> objects)
        {
            var num = 352654597;
            var num2 = num;
            var num3 = 0;
            foreach (var obj in objects)
            {
                if (num3 % 2 == 0)
                    num = ((num << 5) + num + (num >> 27)) ^ obj.GetHashCode();
                else
                    num2 = ((num2 << 5) + num2 + (num2 >> 27)) ^ obj.GetHashCode();
                num3++;
            }

            return num + num2 * 1566083941;
        }

        public MethodBase GetMethodInfo(Type type, string name, Type[] arguments)
        {
            Dictionary<string, Dictionary<int, MethodBase>> dictionary = null;
            methods.TryGetValue(type, out dictionary);
            if (dictionary == null)
            {
                dictionary = new Dictionary<string, Dictionary<int, MethodBase>>();
                methods.Add(type, dictionary);
            }

            Dictionary<int, MethodBase> dictionary2 = null;
            dictionary.TryGetValue(name, out dictionary2);
            if (dictionary2 == null)
            {
                dictionary2 = new Dictionary<int, MethodBase>();
                dictionary.Add(name, dictionary2);
            }

            MethodBase methodBase = null;
            var key = CombinedHashCode(arguments);
            dictionary2.TryGetValue(key, out methodBase);
            if (methodBase == null)
            {
                methodBase = AccessTools.Method(type, name, arguments);
                dictionary2.Add(key, methodBase);
            }

            return methodBase;
        }
    }
}