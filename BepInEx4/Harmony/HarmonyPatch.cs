using System;

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
            info.originalType = type;
        }

        public HarmonyPatch(string methodName)
        {
            info.methodName = methodName;
        }

        public HarmonyPatch(string propertyName, PropertyMethod type)
        {
            var str = type == PropertyMethod.Getter ? "get_" : "set_";
            info.methodName = str + propertyName;
        }

        public HarmonyPatch(Type[] parameter)
        {
            info.parameter = parameter;
        }

        public HarmonyPatch(Type type, string methodName, Type[] parameter = null, int[] byRef = null)
        {
            info.originalType = type;
            info.methodName = methodName;
            if (byRef != null && byRef.Length <= parameter.Length)
                for (var i = 0; i < byRef.Length; i++)
                    parameter[byRef[i]] = parameter[byRef[i]].MakeByRefType();
            info.parameter = parameter;
        }

        public HarmonyPatch(Type type, Type[] parameter = null)
        {
            info.originalType = type;
            info.parameter = parameter;
        }
    }
}