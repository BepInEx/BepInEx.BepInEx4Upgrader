using System;

namespace Harmony
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HarmonyCleanup : Attribute
    {
    }
}