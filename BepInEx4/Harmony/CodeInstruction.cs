using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Harmony.ILCopying;

namespace Harmony
{
    public class CodeInstruction
    {
        public List<ExceptionBlock> blocks = new List<ExceptionBlock>();

        public List<Label> labels = new List<Label>();

        public OpCode opcode;

        public object operand;

        public CodeInstruction(OpCode opcode, object operand = null)
        {
            this.opcode = opcode;
            this.operand = operand;
        }

        public CodeInstruction(CodeInstruction instruction)
        {
            opcode = instruction.opcode;
            operand = instruction.operand;
            labels = instruction.labels.ToArray().ToList();
        }

        public override string ToString()
        {
            var list = new List<string>();
            foreach (var label in labels) list.Add("Label" + label.GetHashCode());
            foreach (var exceptionBlock in blocks)
                list.Add("EX_" + exceptionBlock.blockType.ToString().Replace("Block", ""));
            var arg = list.Count > 0 ? " [" + string.Join(", ", list.ToArray()) + "]" : "";
            var text = Emitter.FormatArgument(operand);
            if (text != "") text = " " + text;
            return string.Format(opcode + text + arg);
        }
    }
}