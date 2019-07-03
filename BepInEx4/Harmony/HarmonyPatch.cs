using System;
using HarmonyLib;

namespace Harmony
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class HarmonyPatch : HarmonyAttribute
    {
        public HarmonyPatch()
        {
        }

        public HarmonyPatch(Type type)
        {
            info.declaringType = type;
        }

        public HarmonyPatch(string methodName)
        {
            info.methodName = methodName;
        }

        public HarmonyPatch(string propertyName, PropertyMethod type)
        {
            var text = type == PropertyMethod.Getter ? "get_" : "set_";
            info.methodName = text + propertyName;
        }

        public HarmonyPatch(Type[] parameter)
        {
            info.argumentTypes = parameter;
        }

        public HarmonyPatch(Type type, string methodName, Type[] parameter = null, int[] byRef = null)
        {
            info.declaringType = type;
            info.methodName = methodName;
            if (byRef != null && byRef.Length <= parameter.Length)
                for (var i = 0; i < byRef.Length; i++)
                    parameter[byRef[i]] = parameter[byRef[i]].MakeByRefType();
            info.argumentTypes = parameter;
        }

        public HarmonyPatch(Type type, Type[] parameter = null)
        {
            info.declaringType = type;
            info.argumentTypes = parameter;
        }
    }
}