using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony.ILCopying;

namespace Harmony
{
    public class CodeTranspiler
    {
        private static readonly Dictionary<OpCode, OpCode> allJumpCodes = new Dictionary<OpCode, OpCode>
        {
            {
                OpCodes.Beq_S,
                OpCodes.Beq
            },
            {
                OpCodes.Bge_S,
                OpCodes.Bge
            },
            {
                OpCodes.Bge_Un_S,
                OpCodes.Bge_Un
            },
            {
                OpCodes.Bgt_S,
                OpCodes.Bgt
            },
            {
                OpCodes.Bgt_Un_S,
                OpCodes.Bgt_Un
            },
            {
                OpCodes.Ble_S,
                OpCodes.Ble
            },
            {
                OpCodes.Ble_Un_S,
                OpCodes.Ble_Un
            },
            {
                OpCodes.Blt_S,
                OpCodes.Blt
            },
            {
                OpCodes.Blt_Un_S,
                OpCodes.Blt_Un
            },
            {
                OpCodes.Bne_Un_S,
                OpCodes.Bne_Un
            },
            {
                OpCodes.Brfalse_S,
                OpCodes.Brfalse
            },
            {
                OpCodes.Brtrue_S,
                OpCodes.Brtrue
            },
            {
                OpCodes.Br_S,
                OpCodes.Br
            },
            {
                OpCodes.Leave_S,
                OpCodes.Leave
            }
        };

        private readonly IEnumerable<CodeInstruction> codeInstructions;

        private readonly List<MethodInfo> transpilers = new List<MethodInfo>();

        public CodeTranspiler(List<ILInstruction> ilInstructions)
        {
            codeInstructions = (from ilInstruction in ilInstructions
                select ilInstruction.GetCodeInstruction()).ToList().AsEnumerable();
        }

        public void Add(MethodInfo transpiler)
        {
            transpilers.Add(transpiler);
        }

        public static object ConvertInstruction(Type type, object op, out List<KeyValuePair<string, object>> unassigned)
        {
            var array = new object[2];
            array[0] = OpCodes.Nop;
            var obj = Activator.CreateInstance(type, array);
            var nonExisting = new List<KeyValuePair<string, object>>();
            Traverse.IterateFields(op, obj, delegate(string name, Traverse trvFrom, Traverse trvDest)
            {
                var obj2 = trvFrom.GetValue();
                if (!trvDest.FieldExists())
                {
                    nonExisting.Add(new KeyValuePair<string, object>(name, obj2));
                    return;
                }

                if (name == "opcode") obj2 = ReplaceShortJumps((OpCode) obj2);
                trvDest.SetValue(obj2);
            });
            unassigned = nonExisting;
            return obj;
        }

        public static IEnumerable ConvertInstructions(Type type, IEnumerable enumerable,
            out Dictionary<object, List<KeyValuePair<string, object>>> unassignedValues)
        {
            var assembly = type.GetGenericTypeDefinition().Assembly;
            var type2 = assembly.GetType(typeof(List<>).FullName);
            var type3 = type.GetGenericArguments()[0];
            var obj = Activator.CreateInstance(assembly.GetType(type2.MakeGenericType(type3).FullName));
            var method = obj.GetType().GetMethod("Add");
            unassignedValues = new Dictionary<object, List<KeyValuePair<string, object>>>();
            foreach (var op in enumerable)
            {
                List<KeyValuePair<string, object>> value;
                var obj2 = ConvertInstruction(type3, op, out value);
                unassignedValues.Add(obj2, value);
                method.Invoke(obj, new[]
                {
                    obj2
                });
            }

            return obj as IEnumerable;
        }

        public static IEnumerable<CodeInstruction> ConvertInstructions(IEnumerable instructions,
            Dictionary<object, List<KeyValuePair<string, object>>> unassignedValues)
        {
            var list = new List<CodeInstruction>();
            foreach (var obj in instructions)
            {
                var codeInstruction = new CodeInstruction(OpCodes.Nop);
                Traverse.IterateFields(obj, codeInstruction,
                    delegate(Traverse trvFrom, Traverse trvDest) { trvDest.SetValue(trvFrom.GetValue()); });
                List<KeyValuePair<string, object>> list2;
                if (unassignedValues.TryGetValue(obj, out list2))
                {
                    var traverse = Traverse.Create(codeInstruction);
                    foreach (var keyValuePair in list2) traverse.Field(keyValuePair.Key).SetValue(keyValuePair.Value);
                }

                list.Add(codeInstruction);
            }

            return list;
        }

        public static IEnumerable ConvertInstructions(MethodInfo transpiler, IEnumerable enumerable,
            out Dictionary<object, List<KeyValuePair<string, object>>> unassignedValues)
        {
            return ConvertInstructions((from p in transpiler.GetParameters()
                    select p.ParameterType).FirstOrDefault(t =>
                    t.IsGenericType && t.GetGenericTypeDefinition().Name.StartsWith("IEnumerable")), enumerable,
                out unassignedValues);
        }

        public static List<object> GetTranspilerCallParameters(ILGenerator generator, MethodInfo transpiler,
            MethodBase method, IEnumerable instructions)
        {
            var parameter = new List<object>();
            (from param in transpiler.GetParameters()
                select param.ParameterType).Do(delegate(Type type)
            {
                if (type.IsAssignableFrom(typeof(ILGenerator)))
                {
                    parameter.Add(generator);
                    return;
                }

                if (type.IsAssignableFrom(typeof(MethodBase)))
                {
                    parameter.Add(method);
                    return;
                }

                parameter.Add(instructions);
            });
            return parameter;
        }

        public IEnumerable<CodeInstruction> GetResult(ILGenerator generator, MethodBase method)
        {
            IEnumerable instructions = codeInstructions;
            transpilers.ForEach(delegate(MethodInfo transpiler)
            {
                Dictionary<object, List<KeyValuePair<string, object>>> unassignedValues;
                instructions = ConvertInstructions(transpiler, instructions, out unassignedValues);
                var transpilerCallParameters = GetTranspilerCallParameters(generator, transpiler, method, instructions);
                instructions = transpiler.Invoke(null, transpilerCallParameters.ToArray()) as IEnumerable;
                instructions = ConvertInstructions(instructions, unassignedValues);
            });
            return instructions as IEnumerable<CodeInstruction>;
        }

        private static OpCode ReplaceShortJumps(OpCode opcode)
        {
            foreach (var keyValuePair in allJumpCodes)
                if (opcode == keyValuePair.Key)
                    return keyValuePair.Value;
            return opcode;
        }
    }
}