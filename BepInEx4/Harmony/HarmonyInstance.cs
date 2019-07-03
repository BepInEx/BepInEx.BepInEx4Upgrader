using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Harmony;
using HarmonyLib;

namespace Harmony
{
    public class HarmonyInstance : HarmonyLib.Harmony
    {
        public static bool DEBUG;

        private HarmonyInstance(string id) : base(id)
        {
        }

        public new string Id => base.Id;

        public static HarmonyInstance Create(string id)
        {
            if (id == null)
                throw new Exception("id cannot be null");
            return new HarmonyInstance(id);
        }

        public new void PatchAll()
        {
            base.PatchAll();
        }

        public new void PatchAll(Assembly assembly)
        {
            base.PatchAll(assembly);
        }

        public void PatchAll(Type type)
        {
            HarmonyWrapper.PatchAll(type, this);
        }

        public void Patch(MethodBase original, HarmonyMethod prefix, HarmonyMethod postfix,
            HarmonyMethod transpiler = null)
        {
            base.Patch(original, prefix, postfix, transpiler);
        }

        public void RemovePatch(MethodBase original, HarmonyPatchType type, string harmonyID = null)
        {
            Unpatch(original, type, harmonyID);
        }

        public void RemovePatch(MethodBase original, MethodInfo patch)
        {
            Unpatch(original, patch);
        }

        public new Patches GetPatchInfo(MethodBase method)
        {
            return new Patches(HarmonyLib.Harmony.GetPatchInfo(method));
        }

        public IEnumerable<MethodBase> GetPatchedMethods()
        {
            return base.GetPatchedMethods();
        }

        public Dictionary<string, Version> VersionInfo(out Version currentVersion)
        {
            return HarmonyLib.Harmony.VersionInfo(out currentVersion);
        }
    }
}