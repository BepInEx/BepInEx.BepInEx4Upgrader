using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Harmony
{
    public static class PatchTools
    {
        private static readonly Dictionary<object, object> objectReferences = new Dictionary<object, object>();

        public static void RememberObject(object key, object value)
        {
            objectReferences[key] = value;
        }

        public static MethodInfo GetPatchMethod<T>(Type patchType, string name, Type[] parameters = null)
        {
            var methodInfo = patchType.GetMethods(AccessTools.all)
                .FirstOrDefault(m => m.GetCustomAttributes(typeof(T), true).Count() > 0);
            if (methodInfo == null) methodInfo = AccessTools.Method(patchType, name, parameters);
            return methodInfo;
        }

        public static void GetPatches(Type patchType, out MethodInfo prefix, out MethodInfo postfix,
            out MethodInfo transpiler)
        {
            prefix = GetPatchMethod<HarmonyPrefix>(patchType, "Prefix");
            postfix = GetPatchMethod<HarmonyPostfix>(patchType, "Postfix");
            transpiler = GetPatchMethod<HarmonyTranspiler>(patchType, "Transpiler");
        }
    }
}