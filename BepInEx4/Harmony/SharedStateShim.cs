using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using ShimHelpers;

namespace Harmony
{
    internal static class SharedStateShim
    {
        private static Type harmonyLibSharedState = typeof(HarmonyLib.Harmony).Assembly.GetType("HarmonyLib.HarmonySharedState");
        internal static Func<MethodBase, PatchInfo> GetPatchInfo = ShimUtil.MakeDelegate<Func<MethodBase, PatchInfo>>(harmonyLibSharedState, nameof(GetPatchInfo));
        internal static Func<IEnumerable<MethodBase>> GetPatchedMethods = ShimUtil.MakeDelegate<Func<IEnumerable<MethodBase>>>(harmonyLibSharedState, nameof(GetPatchedMethods));
        internal static Action<MethodBase, PatchInfo> UpdatePatchInfo = ShimUtil.MakeDelegate<Action<MethodBase, PatchInfo>>(harmonyLibSharedState, nameof(UpdatePatchInfo));
    }
}
