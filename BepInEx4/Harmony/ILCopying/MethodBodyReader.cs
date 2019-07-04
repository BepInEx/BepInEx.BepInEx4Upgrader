using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Harmony.ILCopying
{
    public class MethodBodyReader
    {
        private static readonly OpCode[] one_byte_opcodes = new OpCode[225];

        private static readonly OpCode[] two_bytes_opcodes = new OpCode[31];

        private static readonly Dictionary<Type, MethodInfo> emitMethods;

        private readonly IList<ExceptionHandlingClause> exceptions;

        private readonly ILGenerator generator;

        private readonly ByteBuffer ilBytes;

        private readonly IList<LocalVariableInfo> locals;

        private readonly MethodBase method;

        private readonly Type[] methodArguments;

        private readonly Module module;

        private readonly ParameterInfo[] parameters;

        private readonly ParameterInfo this_parameter;

        private readonly Type[] typeArguments;

        private readonly List<ILInstruction> ilInstructions;

        private LocalBuilder[] variables;

        [MethodImpl(MethodImplOptions.Synchronized)]
        static MethodBodyReader()
        {
            var fields = typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public);
            for (var i = 0; i < fields.Length; i++)
            {
                var opCode = (OpCode) fields[i].GetValue(null);
                if (opCode.OpCodeType != OpCodeType.Nternal)
                {
                    if (opCode.Size == 1)
                        one_byte_opcodes[opCode.Value] = opCode;
                    else
                        two_bytes_opcodes[opCode.Value & 255] = opCode;
                }
            }

            emitMethods = new Dictionary<Type, MethodInfo>();
            typeof(ILGenerator).GetMethods().ToList().Do(delegate(MethodInfo method)
            {
                if (method.Name != "Emit") return;
                var array = method.GetParameters();
                if (array.Length != 2) return;
                var array2 = (from p in array
                    select p.ParameterType).ToArray();
                if (array2[0] != typeof(OpCode)) return;
                emitMethods[array2[1]] = method;
            });
        }

        public MethodBodyReader(MethodBase method, ILGenerator generator)
        {
            this.generator = generator;
            this.method = method;
            module = method.Module;
            var methodBody = method.GetMethodBody();
            if (methodBody == null) throw new ArgumentException("Method " + method.FullDescription() + " has no body");
            var ilasByteArray = methodBody.GetILAsByteArray();
            if (ilasByteArray == null)
                throw new ArgumentException("Can not get IL bytes of method " + method.FullDescription());
            ilBytes = new ByteBuffer(ilasByteArray);
            ilInstructions = new List<ILInstruction>((ilasByteArray.Length + 1) / 2);
            var declaringType = method.DeclaringType;
            if (declaringType.IsGenericType)
                try
                {
                    typeArguments = declaringType.GetGenericArguments();
                }
                catch
                {
                    typeArguments = null;
                }

            if (method.IsGenericMethod)
                try
                {
                    methodArguments = method.GetGenericArguments();
                }
                catch
                {
                    methodArguments = null;
                }

            if (!method.IsStatic) this_parameter = new ThisParameter(method);
            parameters = method.GetParameters();
            locals = methodBody.LocalVariables;
            exceptions = methodBody.ExceptionHandlingClauses;
        }

        public static List<ILInstruction> GetInstructions(ILGenerator generator, MethodBase method)
        {
            if (method == null) throw new ArgumentNullException("Method cannot be null");
            var methodBodyReader = new MethodBodyReader(method, generator);
            methodBodyReader.DeclareVariables(null);
            methodBodyReader.ReadInstructions();
            return methodBodyReader.ilInstructions;
        }

        public void ReadInstructions()
        {
            while (ilBytes.position < ilBytes.buffer.Length)
            {
                var position = ilBytes.position;
                var ilinstruction = new ILInstruction(ReadOpCode())
                {
                    offset = position
                };
                ReadOperand(ilinstruction);
                ilInstructions.Add(ilinstruction);
            }

            ResolveBranches();
            ParseExceptions();
        }

        public void DeclareVariables(LocalBuilder[] existingVariables)
        {
            if (generator == null) return;
            if (existingVariables != null)
            {
                variables = existingVariables;
                return;
            }

            variables = (from lvi in locals
                select generator.DeclareLocal(lvi.LocalType, lvi.IsPinned)).ToArray();
        }

        private void ResolveBranches()
        {
            foreach (var ilinstruction in ilInstructions)
            {
                var operandType = ilinstruction.opcode.OperandType;
                if (operandType != OperandType.InlineBrTarget)
                {
                    if (operandType == OperandType.InlineSwitch)
                    {
                        var array = (int[]) ilinstruction.operand;
                        var array2 = new ILInstruction[array.Length];
                        for (var i = 0; i < array.Length; i++) array2[i] = GetInstruction(array[i], false);
                        ilinstruction.operand = array2;
                        continue;
                    }

                    if (operandType != OperandType.ShortInlineBrTarget) continue;
                }

                ilinstruction.operand = GetInstruction((int) ilinstruction.operand, false);
            }
        }

        private void ParseExceptions()
        {
            foreach (var exceptionHandlingClause in exceptions)
            {
                var tryOffset = exceptionHandlingClause.TryOffset;
                var tryOffset2 = exceptionHandlingClause.TryOffset;
                var tryLength = exceptionHandlingClause.TryLength;
                var handlerOffset = exceptionHandlingClause.HandlerOffset;
                var offset = exceptionHandlingClause.HandlerOffset + exceptionHandlingClause.HandlerLength - 1;
                if (exceptionHandlingClause.Flags != ExceptionHandlingClauseOptions.Clause)
                    exceptionHandlingClause.Flags.ToString().ToLower();
                GetInstruction(tryOffset, false).blocks
                    .Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock, null));
                GetInstruction(offset, true).blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock, null));
                switch (exceptionHandlingClause.Flags)
                {
                    case ExceptionHandlingClauseOptions.Clause:
                        GetInstruction(handlerOffset, false).blocks.Add(
                            new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, exceptionHandlingClause.CatchType));
                        break;
                    case ExceptionHandlingClauseOptions.Filter:
                        GetInstruction(exceptionHandlingClause.FilterOffset, false).blocks
                            .Add(new ExceptionBlock(ExceptionBlockType.BeginExceptFilterBlock, null));
                        break;
                    case ExceptionHandlingClauseOptions.Finally:
                        GetInstruction(handlerOffset, false).blocks
                            .Add(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock, null));
                        break;
                    case ExceptionHandlingClauseOptions.Fault:
                        GetInstruction(handlerOffset, false).blocks
                            .Add(new ExceptionBlock(ExceptionBlockType.BeginFaultBlock, null));
                        break;
                }
            }
        }

        public void FinalizeILCodes(List<MethodInfo> transpilers, List<Label> endLabels, List<ExceptionBlock> endBlocks)
        {
            if (generator == null) return;
            Label label;
            foreach (var ilinstruction in ilInstructions)
            {
                var operandType = ilinstruction.opcode.OperandType;
                if (operandType != OperandType.InlineBrTarget)
                {
                    if (operandType != OperandType.InlineSwitch)
                    {
                        if (operandType != OperandType.ShortInlineBrTarget) continue;
                    }
                    else
                    {
                        var array = ilinstruction.operand as ILInstruction[];
                        if (array != null)
                        {
                            var list = new List<Label>();
                            foreach (var ilinstruction2 in array)
                            {
                                var item = generator.DefineLabel();
                                ilinstruction2.labels.Add(item);
                                list.Add(item);
                            }

                            ilinstruction.argument = list.ToArray();
                        }

                        continue;
                    }
                }

                var ilinstruction3 = ilinstruction.operand as ILInstruction;
                if (ilinstruction3 != null)
                {
                    label = generator.DefineLabel();
                    ilinstruction3.labels.Add(label);
                    ilinstruction.argument = label;
                }
            }

            var codeTranspiler = new CodeTranspiler(ilInstructions);
            transpilers.Do(delegate(MethodInfo transpiler) { codeTranspiler.Add(transpiler); });
            var source = codeTranspiler.GetResult(generator, method);
            for (;;)
            {
                var codeInstruction2 = source.LastOrDefault();
                if (codeInstruction2 == null || codeInstruction2.opcode != OpCodes.Ret) break;
                endLabels.AddRange(codeInstruction2.labels);
                var list2 = source.ToList();
                list2.RemoveAt(list2.Count - 1);
                source = list2;
            }

            IEnumerable<CodeInstruction> sequence = source.ToArray();
            var idx = 0;
            Action<Label> a1 = null;
            Action<ExceptionBlock> a2 = null;
            Action<ExceptionBlock> a3 = null;
            sequence.Do(delegate(CodeInstruction codeInstruction)
            {
                IEnumerable<Label> labels = codeInstruction.labels;
                Action<Label> action;
                if ((action =  a1) == null)
                {
                    action = (a1 = delegate(Label l) { Emitter.MarkLabel(generator, l); });
                }
                labels.Do(action);
                IEnumerable<ExceptionBlock> blocks = codeInstruction.blocks;
                Action<ExceptionBlock> action2;
                if ((action2 =  a2) == null)
                {
                    action2 = (a2 = delegate(ExceptionBlock block)
                    {
                        Label? label4;
                        Emitter.MarkBlockBefore(generator, block, out label4);
                    });
                }
                blocks.Do(action2);
                var opCode = codeInstruction.opcode;
                var obj = codeInstruction.operand;
                if (opCode == OpCodes.Ret)
                {
                    var label2 = generator.DefineLabel();
                    opCode = OpCodes.Br;
                    obj = label2;
                    endLabels.Add(label2);
                }

                if (true)
                {
                    if (opCode.OperandType == OperandType.InlineNone)
                    {
                        Emitter.Emit(generator, opCode);
                    }
                    else
                    {
                        if (obj == null) throw new Exception("Wrong null argument: " + codeInstruction);
                        var methodInfo = EmitMethodForType(obj.GetType());
                        if (methodInfo == null)
                            throw new Exception(string.Concat("Unknown Emit argument type ", obj.GetType(), " in ",
                                codeInstruction));
                        if (HarmonyInstance.DEBUG)
                            FileLog.LogBuffered(string.Concat(Emitter.CodePos(generator), opCode, " ",
                                Emitter.FormatArgument(obj)));
                        methodInfo.Invoke(generator, new[]
                        {
                            opCode,
                            obj
                        });
                    }
                }

                IEnumerable<ExceptionBlock> blocks2 = codeInstruction.blocks;
                Action<ExceptionBlock> action3;
                if ((action3 =  a3) == null)
                {
                    action3 = (a3 = delegate(ExceptionBlock block) { Emitter.MarkBlockAfter(generator, block); });
                }
                blocks2.Do(action3);
                idx++;
            });
        }

        private static void GetMemberInfoValue(MemberInfo info, out object result)
        {
            result = null;
            var memberType = info.MemberType;
            if (memberType <= MemberTypes.Method)
            {
                switch (memberType)
                {
                    case MemberTypes.Constructor:
                        result = (ConstructorInfo) info;
                        return;
                    case MemberTypes.Event:
                        result = (EventInfo) info;
                        return;
                    case MemberTypes.Constructor | MemberTypes.Event:
                        break;
                    case MemberTypes.Field:
                        result = (FieldInfo) info;
                        return;
                    default:
                        if (memberType != MemberTypes.Method) return;
                        result = (MethodInfo) info;
                        return;
                }
            }
            else if (memberType != MemberTypes.Property)
            {
                if (memberType != MemberTypes.TypeInfo && memberType != MemberTypes.NestedType) return;
                result = (Type) info;
            }
            else
            {
                result = (PropertyInfo) info;
            }
        }

        private void ReadOperand(ILInstruction instruction)
        {
            switch (instruction.opcode.OperandType)
            {
                case OperandType.InlineBrTarget:
                {
                    var num = ilBytes.ReadInt32();
                    instruction.operand = num + ilBytes.position;
                    return;
                }

                case OperandType.InlineField:
                {
                    var metadataToken = ilBytes.ReadInt32();
                    instruction.operand = module.ResolveField(metadataToken, typeArguments, methodArguments);
                    instruction.argument = (FieldInfo) instruction.operand;
                    return;
                }

                case OperandType.InlineI:
                {
                    var num2 = ilBytes.ReadInt32();
                    instruction.operand = num2;
                    instruction.argument = (int) instruction.operand;
                    return;
                }

                case OperandType.InlineI8:
                {
                    var num3 = ilBytes.ReadInt64();
                    instruction.operand = num3;
                    instruction.argument = (long) instruction.operand;
                    return;
                }

                case OperandType.InlineMethod:
                {
                    var metadataToken2 = ilBytes.ReadInt32();
                    instruction.operand = module.ResolveMethod(metadataToken2, typeArguments, methodArguments);
                    if (instruction.operand is ConstructorInfo)
                    {
                        instruction.argument = (ConstructorInfo) instruction.operand;
                        return;
                    }

                    instruction.argument = (MethodInfo) instruction.operand;
                    return;
                }

                case OperandType.InlineNone:
                    instruction.argument = null;
                    return;
                case OperandType.InlineR:
                {
                    var num4 = ilBytes.ReadDouble();
                    instruction.operand = num4;
                    instruction.argument = (double) instruction.operand;
                    return;
                }

                case OperandType.InlineSig:
                {
                    var metadataToken3 = ilBytes.ReadInt32();
                    instruction.operand = module.ResolveSignature(metadataToken3);
                    instruction.argument = (SignatureHelper) instruction.operand;
                    return;
                }

                case OperandType.InlineString:
                {
                    var metadataToken4 = ilBytes.ReadInt32();
                    instruction.operand = module.ResolveString(metadataToken4);
                    instruction.argument = (string) instruction.operand;
                    return;
                }

                case OperandType.InlineSwitch:
                {
                    var num5 = ilBytes.ReadInt32();
                    var num6 = ilBytes.position + 4 * num5;
                    var array = new int[num5];
                    for (var i = 0; i < num5; i++) array[i] = ilBytes.ReadInt32() + num6;
                    instruction.operand = array;
                    return;
                }

                case OperandType.InlineTok:
                {
                    var metadataToken5 = ilBytes.ReadInt32();
                    instruction.operand = module.ResolveMember(metadataToken5, typeArguments, methodArguments);
                    GetMemberInfoValue((MemberInfo) instruction.operand, out instruction.argument);
                    return;
                }

                case OperandType.InlineType:
                {
                    var metadataToken6 = ilBytes.ReadInt32();
                    instruction.operand = module.ResolveType(metadataToken6, typeArguments, methodArguments);
                    instruction.argument = (Type) instruction.operand;
                    return;
                }

                case OperandType.InlineVar:
                {
                    var num7 = ilBytes.ReadInt16();
                    if (!TargetsLocalVariable(instruction.opcode))
                    {
                        instruction.operand = GetParameter(num7);
                        instruction.argument = num7;
                        return;
                    }

                    var localVariable = GetLocalVariable(num7);
                    if (localVariable == null)
                    {
                        instruction.argument = num7;
                        return;
                    }

                    instruction.operand = localVariable;
                    instruction.argument = variables[localVariable.LocalIndex];
                    return;
                }

                case OperandType.ShortInlineBrTarget:
                {
                    var b = (sbyte) ilBytes.ReadByte();
                    instruction.operand = b + ilBytes.position;
                    return;
                }

                case OperandType.ShortInlineI:
                {
                    if (instruction.opcode == OpCodes.Ldc_I4_S)
                    {
                        var b2 = (sbyte) ilBytes.ReadByte();
                        instruction.operand = b2;
                        instruction.argument = (sbyte) instruction.operand;
                        return;
                    }

                    var b3 = ilBytes.ReadByte();
                    instruction.operand = b3;
                    instruction.argument = (byte) instruction.operand;
                    return;
                }

                case OperandType.ShortInlineR:
                {
                    var num8 = ilBytes.ReadSingle();
                    instruction.operand = num8;
                    instruction.argument = (float) instruction.operand;
                    return;
                }

                case OperandType.ShortInlineVar:
                {
                    var b4 = ilBytes.ReadByte();
                    if (!TargetsLocalVariable(instruction.opcode))
                    {
                        instruction.operand = GetParameter(b4);
                        instruction.argument = b4;
                        return;
                    }

                    var localVariable2 = GetLocalVariable(b4);
                    if (localVariable2 == null)
                    {
                        instruction.argument = b4;
                        return;
                    }

                    instruction.operand = localVariable2;
                    instruction.argument = variables[localVariable2.LocalIndex];
                    return;
                }
            }

            throw new NotSupportedException();
        }

        private ILInstruction GetInstruction(int offset, bool isEndOfInstruction)
        {
            var num = ilInstructions.Count - 1;
            if (offset < 0 || offset > ilInstructions[num].offset)
                throw new Exception(string.Concat("Instruction offset ", offset, " is outside valid range 0 - ",
                    ilInstructions[num].offset));
            var i = 0;
            var num2 = num;
            while (i <= num2)
            {
                var num3 = i + (num2 - i) / 2;
                var ilinstruction = ilInstructions[num3];
                if (isEndOfInstruction)
                {
                    if (offset == ilinstruction.offset + ilinstruction.GetSize() - 1) return ilinstruction;
                }
                else if (offset == ilinstruction.offset)
                {
                    return ilinstruction;
                }

                if (offset < ilinstruction.offset)
                    num2 = num3 - 1;
                else
                    i = num3 + 1;
            }

            throw new Exception("Cannot find instruction for " + offset.ToString("X4"));
        }

        private static bool TargetsLocalVariable(OpCode opcode)
        {
            return opcode.Name.Contains("loc");
        }

        private LocalVariableInfo GetLocalVariable(int index)
        {
            var list = locals;
            if (list == null) return null;
            return list[index];
        }

        private ParameterInfo GetParameter(int index)
        {
            if (index == 0) return this_parameter;
            return parameters[index - 1];
        }

        private OpCode ReadOpCode()
        {
            var b = ilBytes.ReadByte();
            if (b == 254) return two_bytes_opcodes[ilBytes.ReadByte()];
            return one_byte_opcodes[b];
        }

        private MethodInfo EmitMethodForType(Type type)
        {
            foreach (var keyValuePair in emitMethods)
                if (keyValuePair.Key == type)
                    return keyValuePair.Value;
            foreach (var keyValuePair2 in emitMethods)
                if (keyValuePair2.Key.IsAssignableFrom(type))
                    return keyValuePair2.Value;
            return null;
        }

        private class ThisParameter : ParameterInfo
        {
            public ThisParameter(MethodBase method)
            {
                MemberImpl = method;
                ClassImpl = method.DeclaringType;
                NameImpl = "this";
                PositionImpl = -1;
            }
        }
    }
}