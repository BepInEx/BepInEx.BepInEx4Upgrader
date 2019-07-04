using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Harmony
{
    public static class Transpilers
    {
        public static IEnumerable<CodeInstruction> MethodReplacer(this IEnumerable<CodeInstruction> instructions,
            MethodBase from, MethodBase to)
        {
            foreach (var codeInstruction in instructions)
            {
                if (codeInstruction.operand as MethodBase == from) codeInstruction.operand = to;
                yield return codeInstruction;
            }

            IEnumerator<CodeInstruction> enumerator = null;
            yield break;
            yield break;
        }

        public static IEnumerable<CodeInstruction> DebugLogger(this IEnumerable<CodeInstruction> instructions,
            string text)
        {
            yield return new CodeInstruction(OpCodes.Ldstr, text);
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FileLog), "Log"));
            foreach (var codeInstruction in instructions) yield return codeInstruction;
            IEnumerator<CodeInstruction> enumerator = null;
            yield break;
            yield break;
        }
    }
}