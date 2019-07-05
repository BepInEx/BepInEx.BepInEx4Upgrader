using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace Harmony
{
    public class HarmonyInstance
    {
        public static bool DEBUG;

        private HarmonyInstance(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public static HarmonyInstance Create(string id)
        {
            if (id == null) throw new Exception("id cannot be null");
            return new HarmonyInstance(id);
        }

        public void PatchAll()
        {
            var assembly = new StackTrace().GetFrame(1).GetMethod().ReflectedType.Assembly;
            PatchAll(assembly);
        }

        public void PatchAll(Assembly assembly)
        {
            assembly.GetTypes().Do(delegate(Type type)
            {
                var harmonyMethods = type.GetHarmonyMethods();
                if (harmonyMethods != null && harmonyMethods.Any())
                {
                    var attributes = HarmonyMethod.Merge(harmonyMethods);
                    new PatchProcessor(this, type, attributes).Patch();
                }
            });
        }

        public void PatchAll(Type type)
        {
            type.GetMethods(BindingFlags.Static | BindingFlags.Public).Do(delegate(MethodInfo method)
            {
                var harmonyMethods = method.GetHarmonyMethods();
                if (harmonyMethods != null && harmonyMethods.Any())
                {
                    var original = HarmonyMethod.Merge(harmonyMethods);
                    HarmonyMethod prefix = null;
                    HarmonyMethod transpiler = null;
                    HarmonyMethod postfix = null;
                    if (method.GetCustomAttributes(true).Any(x => x is HarmonyPrefix))
                        prefix = new HarmonyMethod(method);
                    if (method.GetCustomAttributes(true).Any(x => x is HarmonyTranspiler))
                        transpiler = new HarmonyMethod(method);
                    if (method.GetCustomAttributes(true).Any(x => x is HarmonyPostfix))
                        postfix = new HarmonyMethod(method);
                    new PatchProcessor(this, original, prefix, postfix, transpiler).Patch();
                }
            });
        }

        public void Patch(MethodBase original, HarmonyMethod prefix, HarmonyMethod postfix,
            HarmonyMethod transpiler = null)
        {
            new PatchProcessor(this, new List<MethodBase>
            {
                original
            }, prefix, postfix, transpiler).Patch();
        }

        public void RemovePatch(MethodBase original, HarmonyPatchType type, string harmonyID = null)
        {
            new PatchProcessor(this, new List<MethodBase>
            {
                original
            }).Unpatch(type, harmonyID);
        }

        public void RemovePatch(MethodBase original, MethodInfo patch)
        {
            new PatchProcessor(this, new List<MethodBase>
            {
                original
            }).Unpatch(patch);
        }

        public Patches GetPatchInfo(MethodBase method)
        {
            return PatchProcessor.GetPatchInfo(method);
        }

        public IEnumerable<MethodBase> GetPatchedMethods()
        {
            return HarmonySharedState.GetPatchedMethods();
        }

        public Dictionary<string, Version> VersionInfo(out Version currentVersion)
        {
            currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            var assemblies = new Dictionary<string, Assembly>();
            Action<Patch> a1 = null;
            Action<Patch> a2 = null;
            Action<Patch> a3 = null;
            GetPatchedMethods().Do(delegate(MethodBase method)
            {
                var patchInfo = HarmonySharedState.GetPatchInfo(method);
                IEnumerable<Patch> prefixes = patchInfo.prefixes;
                Action<Patch> action;
                if ((action =  a1) == null)
                {
                    action = (a1 = delegate(Patch fix)
                    {
                        assemblies[fix.owner] = fix.patch.DeclaringType.Assembly;
                    });
                }
                prefixes.Do(action);
                IEnumerable<Patch> postfixes = patchInfo.postfixes;
                Action<Patch> action2;
                if ((action2 =  a2) == null)
                {
                    action2 = (a2 = delegate(Patch fix)
                    {
                        assemblies[fix.owner] = fix.patch.DeclaringType.Assembly;
                    });
                }
                postfixes.Do(action2);
                IEnumerable<Patch> transpilers = patchInfo.transpilers;
                Action<Patch> action3;
                if ((action3 =  a3) == null)
                {
                    action3 = (a3 = delegate(Patch fix)
                    {
                        assemblies[fix.owner] = fix.patch.DeclaringType.Assembly;
                    });
                }
                transpilers.Do(action3);
            });
            var result = new Dictionary<string, Version>();
            assemblies.Do(delegate(KeyValuePair<string, Assembly> info)
            {
                var assemblyName = info.Value.GetReferencedAssemblies()
                    .FirstOrDefault(a => a.FullName.StartsWith("0Harmony, Version"));
                if (assemblyName != null) result[info.Key] = assemblyName.Version;
            });
            return result;
        }
    }
}