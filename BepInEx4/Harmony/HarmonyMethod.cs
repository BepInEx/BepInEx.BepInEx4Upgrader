using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Harmony
{
    public class HarmonyMethod
    {
        public string[] after;

        public string[] before;

        public MethodInfo method;

        public string methodName;

        public Type originalType;

        public Type[] parameter;

        public int prioritiy = -1;

        public HarmonyMethod()
        {
        }

        public HarmonyMethod(MethodInfo method)
        {
            ImportMethod(method);
        }

        public HarmonyMethod(Type type, string name, Type[] parameters = null)
        {
            var theMethod = AccessTools.Method(type, name, parameters);
            ImportMethod(theMethod);
        }

        private void ImportMethod(MethodInfo theMethod)
        {
            method = theMethod;
            if (method != null)
            {
                var harmonyMethods = method.GetHarmonyMethods();
                if (harmonyMethods != null) Merge(harmonyMethods).CopyTo(this);
            }
        }

        public static List<string> HarmonyFields()
        {
            return (from s in AccessTools.GetFieldNames(typeof(HarmonyMethod))
                where s != "method"
                select s).ToList();
        }

        public static HarmonyMethod Merge(List<HarmonyMethod> attributes)
        {
            var harmonyMethod = new HarmonyMethod();
            if (attributes == null) return harmonyMethod;
            var resultTrv = Traverse.Create(harmonyMethod);
            attributes.ForEach(delegate(HarmonyMethod attribute)
            {
                var trv = Traverse.Create(attribute);
                HarmonyFields().ForEach(delegate(string f)
                {
                    var value = trv.Field(f).GetValue();
                    if (value != null) resultTrv.Field(f).SetValue(value);
                });
            });
            return harmonyMethod;
        }
    }
}