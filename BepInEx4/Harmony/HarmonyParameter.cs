using System;

namespace Harmony
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple =
        true)]
    public class HarmonyParameter : Attribute
    {
        public HarmonyParameter(string originalName) : this(originalName, null)
        {
        }

        public HarmonyParameter(string originalName, string newName)
        {
            OriginalName = originalName;
            NewName = newName;
        }

        public string OriginalName { get; }

        public string NewName { get; }
    }
}