using System;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using ShimHelpers;

namespace Harmony
{
    internal static class PatchFunctionsShim
    {
        private static Type harmonyLibPatchFunctions = typeof(HarmonyLib.Harmony).Assembly.GetType("HarmonyLib.PatchFunctions");

        public static Action<PatchInfo, string, HarmonyMethod> AddPrefix =
            ShimUtil.MakeDelegate<Action<PatchInfo, string, HarmonyMethod>>(harmonyLibPatchFunctions,
                nameof(AddPrefix));

        public static Action<PatchInfo, string, HarmonyMethod> AddPostfix =
            ShimUtil.MakeDelegate<Action<PatchInfo, string, HarmonyMethod>>(harmonyLibPatchFunctions,
                nameof(AddPostfix));

        public static Action<PatchInfo, string, HarmonyMethod> AddTranspiler =
            ShimUtil.MakeDelegate<Action<PatchInfo, string, HarmonyMethod>>(harmonyLibPatchFunctions,
                nameof(AddTranspiler));

        public static Action<PatchInfo, string> RemovePrefix =
            ShimUtil.MakeDelegate<Action<PatchInfo, string>>(harmonyLibPatchFunctions,
                nameof(RemovePrefix));

        public static Action<PatchInfo, string> RemovePostfix =
            ShimUtil.MakeDelegate<Action<PatchInfo, string>>(harmonyLibPatchFunctions,
                nameof(RemovePostfix));

        public static Action<PatchInfo, string> RemoveTranspiler =
            ShimUtil.MakeDelegate<Action<PatchInfo, string>>(harmonyLibPatchFunctions,
                nameof(RemoveTranspiler));

        public static Action<PatchInfo, MethodInfo> RemovePatch =
            ShimUtil.MakeDelegate<Action<PatchInfo, MethodInfo>>(harmonyLibPatchFunctions,
                nameof(RemovePatch));

        public static Func<MethodBase, PatchInfo, string, DynamicMethod> UpdateWrapper =
            ShimUtil.MakeDelegate<Func<MethodBase, PatchInfo, string, DynamicMethod>>(harmonyLibPatchFunctions,
                nameof(UpdateWrapper));

        //internal static List<ILInstruction> GetInstructions(ILGenerator generator, MethodBase method)
        //{
        //    return MethodBodyReader.GetInstructions(generator, method);
        //}

        //internal static List<MethodInfo> GetSortedPatchMethods(MethodBase original, Patch[] patches)
        //{
        //    return new PatchSorter(patches).Sort(original);
        //}

        //internal static DynamicMethod UpdateWrapper(MethodBase original, PatchInfo patchInfo, string instanceID)
        //{
        //    List<MethodInfo> sortedPatchMethods = PatchFunctions.GetSortedPatchMethods(original, patchInfo.prefixes);
        //    List<MethodInfo> sortedPatchMethods2 = PatchFunctions.GetSortedPatchMethods(original, patchInfo.postfixes);
        //    List<MethodInfo> sortedPatchMethods3 = PatchFunctions.GetSortedPatchMethods(original, patchInfo.transpilers);
        //    List<MethodInfo> sortedPatchMethods4 = PatchFunctions.GetSortedPatchMethods(original, patchInfo.finalizers);
        //    DynamicMethod dynamicMethod = MethodPatcher.CreatePatchedMethod(original, instanceID, sortedPatchMethods, sortedPatchMethods2, sortedPatchMethods3, sortedPatchMethods4);
        //    if (dynamicMethod == null)
        //    {
        //        throw new MissingMethodException("Cannot create dynamic replacement for " + original.FullDescription());
        //    }
        //    string text = Memory.DetourMethod(original, dynamicMethod);
        //    if (text != null)
        //    {
        //        throw new FormatException("Method " + original.FullDescription() + " cannot be patched. Reason: " + text);
        //    }
        //    PatchTools.RememberObject(original, dynamicMethod);
        //    return dynamicMethod;
        //}
    }
}
