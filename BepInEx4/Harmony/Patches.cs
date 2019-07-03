using System.Collections.ObjectModel;
using HarmonyLib;

namespace Harmony
{
    public class Patches
    {
        public readonly ReadOnlyCollection<Patch> Postfixes;
        public readonly ReadOnlyCollection<Patch> Prefixes;
        public readonly ReadOnlyCollection<Patch> Transpilers;

        private readonly HarmonyLib.Patches realPatches;

        public Patches(HarmonyLib.Patches real)
        {
            realPatches = real;

            Prefixes = realPatches.Prefixes;
            Postfixes = realPatches.Postfixes;
            Transpilers = realPatches.Transpilers;
        }

        public Patches(Patch[] prefixes, Patch[] postfixes, Patch[] transpilers) : this(
            new HarmonyLib.Patches(prefixes, postfixes, transpilers, null))
        {
        }

        public ReadOnlyCollection<string> Owners => realPatches.Owners;
    }
}