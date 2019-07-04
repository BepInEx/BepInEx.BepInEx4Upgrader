using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace Harmony
{
    public static class PatchTools
    {
        public static MethodInfo GetPatchMethod<T>(Type patchType, string name, Type[] parameters = null)
        {
            MethodInfo methodInfo = Enumerable.FirstOrDefault<MethodInfo>(patchType.GetMethods(AccessTools.all), (MethodInfo m) => Enumerable.Count<object>(m.GetCustomAttributes(typeof(T), true)) > 0);
            if (methodInfo == null)
            {
                methodInfo = AccessTools.Method(patchType, name, parameters, null);
            }
            return methodInfo;
        }

        public static void GetPatches(Type patchType, out MethodInfo prefix, out MethodInfo postfix, out MethodInfo transpiler)
        {
            prefix = PatchTools.GetPatchMethod<HarmonyPrefix>(patchType, "Prefix", null);
            postfix = PatchTools.GetPatchMethod<HarmonyPostfix>(patchType, "Postfix", null);
            transpiler = PatchTools.GetPatchMethod<HarmonyTranspiler>(patchType, "Transpiler", null);
        }
    }
}