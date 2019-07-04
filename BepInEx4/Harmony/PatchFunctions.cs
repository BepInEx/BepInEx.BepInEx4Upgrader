using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Memory = Harmony.ILCopying.Memory;

namespace Harmony
{
    public static class PatchFunctions
    {
        public static void AddPrefix(PatchInfo patchInfo, string owner, HarmonyMethod info)
        {
            if (info == null || info.method == null) return;
            var priority = info.prioritiy == -1 ? 400 : info.prioritiy;
            var before = info.before ?? new string[0];
            var after = info.after ?? new string[0];
            patchInfo.AddPrefix(info.method, owner, priority, before, after);
        }

        public static void RemovePrefix(PatchInfo patchInfo, string owner)
        {
            patchInfo.RemovePrefix(owner);
        }

        public static void AddPostfix(PatchInfo patchInfo, string owner, HarmonyMethod info)
        {
            if (info == null || info.method == null) return;
            var priority = info.prioritiy == -1 ? 400 : info.prioritiy;
            var before = info.before ?? new string[0];
            var after = info.after ?? new string[0];
            patchInfo.AddPostfix(info.method, owner, priority, before, after);
        }

        public static void RemovePostfix(PatchInfo patchInfo, string owner)
        {
            patchInfo.RemovePostfix(owner);
        }

        public static void AddTranspiler(PatchInfo patchInfo, string owner, HarmonyMethod info)
        {
            if (info == null || info.method == null) return;
            var priority = info.prioritiy == -1 ? 400 : info.prioritiy;
            var before = info.before ?? new string[0];
            var after = info.after ?? new string[0];
            patchInfo.AddTranspiler(info.method, owner, priority, before, after);
        }

        public static void RemoveTranspiler(PatchInfo patchInfo, string owner)
        {
            patchInfo.RemoveTranspiler(owner);
        }

        public static void RemovePatch(PatchInfo patchInfo, MethodInfo patch)
        {
            patchInfo.RemovePatch(patch);
        }

        public static List<MethodInfo> GetSortedPatchMethods(MethodBase original, Patch[] patches)
        {
            return (from p in patches
                where p.patch != null
                orderby p
                select p.GetMethod(original)).ToList();
        }

        public static void UpdateWrapper(MethodBase original, PatchInfo patchInfo, string instanceID)
        {
            var sortedPatchMethods = GetSortedPatchMethods(original, patchInfo.prefixes);
            var sortedPatchMethods2 = GetSortedPatchMethods(original, patchInfo.postfixes);
            var sortedPatchMethods3 = GetSortedPatchMethods(original, patchInfo.transpilers);
            var dynamicMethod = MethodPatcher.CreatePatchedMethod(original, instanceID, sortedPatchMethods,
                sortedPatchMethods2, sortedPatchMethods3);
            if (dynamicMethod == null)
                throw new MissingMethodException("Cannot create dynamic replacement for " + original.FullDescription());
            var methodStart = Memory.GetMethodStart(original);
            var methodStart2 = Memory.GetMethodStart(dynamicMethod);
            Memory.WriteJump(methodStart, methodStart2);
            PatchTools.RememberObject(original, dynamicMethod);
        }
    }
}