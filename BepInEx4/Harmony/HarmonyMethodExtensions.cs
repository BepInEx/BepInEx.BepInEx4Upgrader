using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Harmony
{
    public static class HarmonyMethodExtensions
    {
        public static void CopyTo(this HarmonyMethod from, HarmonyMethod to)
        {
            HarmonyLib.HarmonyMethodExtensions.CopyTo(from, to);
        }

        public static HarmonyMethod Clone(this HarmonyMethod original)
        {
            return HarmonyLib.HarmonyMethodExtensions.Clone(original);
        }

        public static HarmonyMethod Merge(this HarmonyMethod master, HarmonyMethod detail)
        {
            if (detail == null) return master;
            var harmonyMethod = new HarmonyMethod();
            var resultTrv = Traverse.Create(harmonyMethod);
            var masterTrv = Traverse.Create(master);
            var detailTrv = Traverse.Create(detail);
            HarmonyMethod.HarmonyFields().ForEach(delegate(string f)
            {
                var value = masterTrv.Field(f).GetValue();
                var value2 = detailTrv.Field(f).GetValue();
                resultTrv.Field(f).SetValue(value2 ?? value);
            });
            return harmonyMethod;
        }

        public static List<HarmonyMethod> GetHarmonyMethods(this Type type)
        {
            return type.GetCustomAttributes(true).Where(attr => attr is HarmonyAttribute).Cast<HarmonyAttribute>()
                .Select(attr => attr.info).ToList();
        }

        public static List<HarmonyMethod> GetHarmonyMethods(this MethodBase method)
        {
            if (method is DynamicMethod) return new List<HarmonyMethod>();
            return method.GetCustomAttributes(true).Where(attr => attr is HarmonyAttribute).Cast<HarmonyAttribute>()
                .Select(attr => attr.info).ToList();
        }
    }
}