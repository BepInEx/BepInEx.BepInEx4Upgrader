using System.Collections.Generic;
using System.Reflection.Emit;

namespace Harmony.ILCopying
{
    public class ILInstruction
    {
        public object argument;

        public List<ExceptionBlock> blocks = new List<ExceptionBlock>();

        public List<Label> labels = new List<Label>();

        public int offset;

        public OpCode opcode;

        public object operand;

        public ILInstruction(OpCode opcode, object operand = null)
        {
            this.opcode = opcode;
            this.operand = operand;
            argument = operand;
        }

        public CodeInstruction GetCodeInstruction()
        {
            var codeInstruction = new CodeInstruction(opcode, argument);
            if (opcode.OperandType == OperandType.InlineNone) codeInstruction.operand = null;
            codeInstruction.labels = labels;
            codeInstruction.blocks = blocks;
            return codeInstruction;
        }

        public int GetSize()
        {
            var num = opcode.Size;
            switch (opcode.OperandType)
            {
                case OperandType.InlineBrTarget:
                case OperandType.InlineField:
                case OperandType.InlineI:
                case OperandType.InlineMethod:
                case OperandType.InlineString:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                case OperandType.ShortInlineR:
                    num += 4;
                    break;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    num += 8;
                    break;
                case OperandType.InlineSwitch:
                    num += (1 + ((int[]) operand).Length) * 4;
                    break;
                case OperandType.InlineVar:
                    num += 2;
                    break;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    num++;
                    break;
            }

            return num;
        }

        public override string ToString()
        {
            var text = "";
            AppendLabel(ref text, this);
            text = text + ": " + opcode.Name;
            if (operand == null) return text;
            text += " ";
            var operandType = opcode.OperandType;
            if (operandType <= OperandType.InlineString)
            {
                if (operandType != OperandType.InlineBrTarget)
                {
                    if (operandType != OperandType.InlineString) goto IL_D0;
                    return string.Concat(text, "\"", operand, "\"");
                }
            }
            else
            {
                if (operandType == OperandType.InlineSwitch)
                {
                    var array = (ILInstruction[]) operand;
                    for (var i = 0; i < array.Length; i++)
                    {
                        if (i > 0) text += ",";
                        AppendLabel(ref text, array[i]);
                    }

                    return text;
                }

                if (operandType != OperandType.ShortInlineBrTarget) goto IL_D0;
            }

            AppendLabel(ref text, operand);
            return text;
            IL_D0:
            text += operand;
            return text;
        }

        private static void AppendLabel(ref string str, object argument)
        {
            var ilinstruction = argument as ILInstruction;
            if (ilinstruction != null)
            {
                str = str + "IL_" + ilinstruction.offset.ToString("X4");
                return;
            }

            str = str + "IL_" + argument;
        }
    }
}