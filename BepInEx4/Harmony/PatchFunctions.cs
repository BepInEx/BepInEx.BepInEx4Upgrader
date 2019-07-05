using System;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using ShimHelpers;

namespace Harmony
{
    public static class PatchFunctions
    {
        private static readonly Type patchFunctionsNew =
            typeof(HarmonyLib.Harmony).Assembly.GetType("HarmonyLib.PatchFunctions");

        public static Func<MethodBase, PatchInfo, string, DynamicMethod> UpdateWrapper =
            ShimUtil.MakeDelegate<Func<MethodBase, PatchInfo, string, DynamicMethod>>(patchFunctionsNew, nameof(UpdateWrapper));


        public static void AddPrefix(PatchInfo patchInfo, string owner, HarmonyMethod info)
        {
            if (info?.method == null) return;
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
            if (info?.method == null) return;
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
            if (info?.method == null) return;
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
    }
}