using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Harmony.ILCopying;

namespace Harmony.Tools
{
    internal class SelfPatching
    {
        private static readonly HashSet<MethodInfo> patchedMethods = new HashSet<MethodInfo>();

        private static int GetVersion(MethodInfo method)
        {
            var upgradeToLatestVersion =
                method.GetCustomAttributes(false).OfType<UpgradeToLatestVersion>().FirstOrDefault();
            if (upgradeToLatestVersion == null) return -1;
            return upgradeToLatestVersion.version;
        }

        private static string MethodKey(MethodInfo method)
        {
            return method.DeclaringType + " " + method;
        }

        private static bool IsHarmonyAssembly(Assembly assembly)
        {
            var customAttributes = assembly.GetCustomAttributes(typeof(GuidAttribute), false);
            return customAttributes.Length >= 1 && (customAttributes.GetValue(0) as GuidAttribute).Value ==
                   "69aee16a-b6e7-4642-8081-3928b32455df";
        }

        public static void PatchOldHarmonyMethods()
        {
            var potentialMethodsToUpgrade = new Dictionary<string, MethodInfo>();
            (from method in typeof(SelfPatching).Assembly.GetTypes()
                    .SelectMany(type => type.GetMethods(AccessTools.all))
                where method.GetCustomAttributes(false).Any(attr => attr is UpgradeToLatestVersion)
                select method).Do(delegate(MethodInfo method)
            {
                potentialMethodsToUpgrade.Add(MethodKey(method), method);
            });
            (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                    where IsHarmonyAssembly(assembly)
                    select assembly).SelectMany(assembly => assembly.GetTypes())
                .SelectMany(type => type.GetMethods(AccessTools.all)).Select(delegate(MethodInfo method)
                {
                    MethodInfo value;
                    potentialMethodsToUpgrade.TryGetValue(MethodKey(method), out value);
                    return new KeyValuePair<MethodInfo, MethodInfo>(method, value);
                }).Do(delegate(KeyValuePair<MethodInfo, MethodInfo> pair)
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    if (value != null && GetVersion(key) < GetVersion(value) && !patchedMethods.Contains(key))
                    {
                        patchedMethods.Add(key);
                        var methodStart = Memory.GetMethodStart(key);
                        var methodStart2 = Memory.GetMethodStart(value);
                        Memory.WriteJump(methodStart, methodStart2);
                    }
                });
        }
    }
}