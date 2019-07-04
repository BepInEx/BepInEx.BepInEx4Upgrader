using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace Harmony
{
    public class HarmonyMethod : HarmonyLib.HarmonyMethod
    {
        internal static HarmonyMethod Copy(HarmonyLib.HarmonyMethod other)
        {
            return new HarmonyMethod()
            {
                after = other.after,
                before = other.before,
                argumentTypes = other.argumentTypes,
                declaringType = other.declaringType,
                method = other.method,
                methodName = other.methodName,
                priority = other.priority,
                methodType = other.methodType
            };
        }

        public static List<string> HarmonyFields()
        {
            return Enumerable.ToList<string>(Enumerable.Where<string>(AccessTools.GetFieldNames(typeof(HarmonyMethod)), (string s) => s != "method"));
        }

        public static HarmonyMethod Merge(List<HarmonyMethod> attributes)
        {
            HarmonyMethod harmonyMethod = new HarmonyMethod();
            if (attributes == null)
            {
                return harmonyMethod;
            }
            Traverse resultTrv = Traverse.Create(harmonyMethod);
            attributes.ForEach(delegate (HarmonyMethod attribute)
            {
                Traverse trv = Traverse.Create(attribute);
                HarmonyMethod.HarmonyFields().ForEach(delegate (string f)
                {
                    object value = trv.Field(f).GetValue();
                    if (value != null)
                    {
                        resultTrv.Field(f).SetValue(value);
                    }
                });
            });
            return harmonyMethod;
        }


        public HarmonyMethod()
        {
        }

        public HarmonyMethod(MethodInfo mi) : base(mi)
        {
        }

        public HarmonyMethod(Type t, string s, Type[] tt = null) : base(t, s, tt)
        {
        }
    }
}