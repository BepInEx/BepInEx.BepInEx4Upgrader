using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony.ILCopying;

namespace Harmony
{
    public static class MethodPatcher
    {
        public static string INSTANCE_PARAM = "__instance";

        public static string ORIGINAL_METHOD_PARAM = "__originalMethod";

        public static string RESULT_VAR = "__result";

        public static string STATE_VAR = "__state";

        private static readonly bool DEBUG_METHOD_GENERATION_BY_DLL_CREATION = false;

        private static readonly MethodInfo getMethodMethod = typeof(MethodBase).GetMethod("GetMethodFromHandle", new[]
        {
            typeof(RuntimeMethodHandle)
        });

        [UpgradeToLatestVersion(1)]
        public static DynamicMethod CreatePatchedMethod(MethodBase original, List<MethodInfo> prefixes,
            List<MethodInfo> postfixes, List<MethodInfo> transpilers)
        {
            return CreatePatchedMethod(original, "HARMONY_PATCH_1.1.0", prefixes, postfixes, transpilers);
        }

        public static DynamicMethod CreatePatchedMethod(MethodBase original, string harmonyInstanceID,
            List<MethodInfo> prefixes, List<MethodInfo> postfixes, List<MethodInfo> transpilers)
        {
            DynamicMethod result;
            try
            {
                if (HarmonyInstance.DEBUG)
                    FileLog.LogBuffered(string.Concat("### Patch ", original.DeclaringType, ", ", original));
                var num = prefixes.Count() + postfixes.Count();
                var dynamicMethod = DynamicTools.CreateDynamicMethod(original, "_Patch" + num);
                var il = dynamicMethod.GetILGenerator();
                AssemblyBuilder assemblyBuilder = null;
                TypeBuilder typeBuilder = null;
                if (DEBUG_METHOD_GENERATION_BY_DLL_CREATION)
                    il = DynamicTools.CreateSaveableMethod(original, "_Patch" + num, out assemblyBuilder,
                        out typeBuilder);
                var existingVariables = DynamicTools.DeclareLocalVariables(original, il);
                var privateVars = new Dictionary<string, LocalBuilder>();
                LocalBuilder localBuilder = null;
                if (num > 0)
                {
                    localBuilder = DynamicTools.DeclareLocalVariable(il, AccessTools.GetReturnedType(original));
                    privateVars[RESULT_VAR] = localBuilder;
                }

                prefixes.ForEach(delegate(MethodInfo prefix)
                {
                    (from patchParam in prefix.GetParameters()
                        where patchParam.Name == STATE_VAR
                        select patchParam).Do(delegate(ParameterInfo patchParam)
                    {
                        var value = DynamicTools.DeclareLocalVariable(il, patchParam.ParameterType);
                        privateVars[prefix.DeclaringType.FullName] = value;
                    });
                });
                var label = il.DefineLabel();
                var flag = AddPrefixes(il, original, prefixes, privateVars, label);
                var methodCopier = new MethodCopier(original, il, existingVariables);
                foreach (var transpiler in transpilers) methodCopier.AddTranspiler(transpiler);
                var list = new List<Label>();
                var list2 = new List<ExceptionBlock>();
                methodCopier.Finalize(list, list2);
                foreach (var label2 in list) Emitter.MarkLabel(il, label2);
                foreach (var block in list2) Emitter.MarkBlockAfter(il, block);
                if (localBuilder != null) Emitter.Emit(il, OpCodes.Stloc, localBuilder);
                if (flag) Emitter.MarkLabel(il, label);
                AddPostfixes(il, original, postfixes, privateVars);
                if (localBuilder != null) Emitter.Emit(il, OpCodes.Ldloc, localBuilder);
                Emitter.Emit(il, OpCodes.Ret);
                if (HarmonyInstance.DEBUG)
                {
                    FileLog.LogBuffered("DONE");
                    FileLog.LogBuffered("");
                }

                if (DEBUG_METHOD_GENERATION_BY_DLL_CREATION)
                {
                    DynamicTools.SaveMethod(assemblyBuilder, typeBuilder);
                    result = null;
                }
                else
                {
                    DynamicTools.PrepareDynamicMethod(dynamicMethod);
                    result = dynamicMethod;
                }
            }
            catch (Exception innerException)
            {
                throw new Exception("Exception from HarmonyInstance \"" + harmonyInstanceID + "\"", innerException);
            }
            finally
            {
                if (HarmonyInstance.DEBUG) FileLog.FlushBuffer();
            }

            return result;
        }

        private static OpCode LoadIndOpCodeFor(Type type)
        {
            if (type.IsEnum) return OpCodes.Ldind_I4;
            if (type == typeof(float)) return OpCodes.Ldind_R4;
            if (type == typeof(double)) return OpCodes.Ldind_R8;
            if (type == typeof(byte)) return OpCodes.Ldind_U1;
            if (type == typeof(ushort)) return OpCodes.Ldind_U2;
            if (type == typeof(uint)) return OpCodes.Ldind_U4;
            if (type == typeof(ulong)) return OpCodes.Ldind_I8;
            if (type == typeof(sbyte)) return OpCodes.Ldind_I1;
            if (type == typeof(short)) return OpCodes.Ldind_I2;
            if (type == typeof(int)) return OpCodes.Ldind_I4;
            if (type == typeof(long)) return OpCodes.Ldind_I8;
            return OpCodes.Ldind_Ref;
        }

        private static HarmonyParameter GetParameterAttribute(this ParameterInfo parameter)
        {
            return parameter.GetCustomAttributes(false).FirstOrDefault(attr => attr is HarmonyParameter) as
                HarmonyParameter;
        }

        private static HarmonyParameter[] GetParameterAttributes(this MethodInfo method)
        {
            return (from attr in method.GetCustomAttributes(false)
                where attr is HarmonyParameter
                select attr).Cast<HarmonyParameter>().ToArray();
        }

        private static HarmonyParameter[] GetParameterAttributes(this Type type)
        {
            return (from attr in type.GetCustomAttributes(false)
                where attr is HarmonyParameter
                select attr).Cast<HarmonyParameter>().ToArray();
        }

        private static string GetParameterOverride(this ParameterInfo parameter)
        {
            var parameterAttribute = parameter.GetParameterAttribute();
            if (parameterAttribute != null && !string.IsNullOrEmpty(parameterAttribute.OriginalName))
                return parameterAttribute.OriginalName;
            return null;
        }

        private static string GetParameterOverride(HarmonyParameter[] patchAttributes, string name)
        {
            if (patchAttributes.Length != 0)
            {
                var harmonyParameter = patchAttributes.SingleOrDefault(p => p.NewName == name);
                if (harmonyParameter != null && !string.IsNullOrEmpty(harmonyParameter.OriginalName))
                    return harmonyParameter.OriginalName;
            }

            return null;
        }

        private static string GetParameterOverride(this MethodInfo method, string name, bool checkClass)
        {
            var parameterOverride = GetParameterOverride(method.GetParameterAttributes(), name);
            if (parameterOverride == null && checkClass)
                return GetParameterOverride(method.DeclaringType.GetParameterAttributes(), name);
            return parameterOverride;
        }

        private static void EmitCallParameter(ILGenerator il, MethodBase original, MethodInfo patch,
            Dictionary<string, LocalBuilder> variables)
        {
            var flag = !original.IsStatic;
            var parameters = original.GetParameters();
            var array = (from p in parameters
                select p.Name).ToArray();
            foreach (var parameterInfo in patch.GetParameters())
                if (parameterInfo.Name == ORIGINAL_METHOD_PARAM)
                {
                    var constructorInfo = original as ConstructorInfo;
                    if (constructorInfo != null)
                    {
                        Emitter.Emit(il, OpCodes.Ldtoken, constructorInfo);
                        Emitter.Emit(il, OpCodes.Call, getMethodMethod);
                    }
                    else
                    {
                        var methodInfo = original as MethodInfo;
                        if (methodInfo != null)
                        {
                            Emitter.Emit(il, OpCodes.Ldtoken, methodInfo);
                            Emitter.Emit(il, OpCodes.Call, getMethodMethod);
                        }
                        else
                        {
                            Emitter.Emit(il, OpCodes.Ldnull);
                        }
                    }
                }
                else if (parameterInfo.Name == INSTANCE_PARAM)
                {
                    if (original.IsStatic)
                        Emitter.Emit(il, OpCodes.Ldnull);
                    else if (parameterInfo.ParameterType.IsByRef)
                        Emitter.Emit(il, OpCodes.Ldarga, 0);
                    else
                        Emitter.Emit(il, OpCodes.Ldarg_0);
                }
                else if (parameterInfo.Name == STATE_VAR)
                {
                    var opcode = parameterInfo.ParameterType.IsByRef ? OpCodes.Ldloca : OpCodes.Ldloc;
                    Emitter.Emit(il, opcode, variables[patch.DeclaringType.FullName]);
                }
                else if (parameterInfo.Name == RESULT_VAR)
                {
                    if (AccessTools.GetReturnedType(original) == typeof(void))
                        throw new Exception("Cannot get result from void method " + original.FullDescription());
                    var opcode2 = parameterInfo.ParameterType.IsByRef ? OpCodes.Ldloca : OpCodes.Ldloc;
                    Emitter.Emit(il, opcode2, variables[RESULT_VAR]);
                }
                else
                {
                    var text = parameterInfo.Name;
                    var parameterOverride = parameterInfo.GetParameterOverride();
                    if (parameterOverride != null)
                    {
                        text = parameterOverride;
                    }
                    else
                    {
                        parameterOverride = patch.GetParameterOverride(text, true);
                        if (parameterOverride != null) text = parameterOverride;
                    }

                    var num = Array.IndexOf(array, text);
                    if (num == -1)
                        throw new Exception("Parameter \"" + parameterInfo.Name + "\" not found in method " +
                                            original.FullDescription());
                    var flag2 = !parameters[num].IsOut && !parameters[num].ParameterType.IsByRef;
                    var flag3 = !parameterInfo.IsOut && !parameterInfo.ParameterType.IsByRef;
                    var arg = num + (flag ? 1 : 0);
                    if (flag2 == flag3)
                    {
                        Emitter.Emit(il, OpCodes.Ldarg, arg);
                    }
                    else if (flag2 && !flag3)
                    {
                        Emitter.Emit(il, OpCodes.Ldarga, arg);
                    }
                    else
                    {
                        Emitter.Emit(il, OpCodes.Ldarg, arg);
                        Emitter.Emit(il, LoadIndOpCodeFor(parameters[num].ParameterType));
                    }
                }
        }

        private static bool AddPrefixes(ILGenerator il, MethodBase original, List<MethodInfo> prefixes,
            Dictionary<string, LocalBuilder> variables, Label label)
        {
            var canHaveJump = false;
            prefixes.ForEach(delegate(MethodInfo fix)
            {
                EmitCallParameter(il, original, fix, variables);
                Emitter.Emit(il, OpCodes.Call, fix);
                if (fix.ReturnType != typeof(void))
                {
                    if (fix.ReturnType != typeof(bool))
                        throw new Exception(string.Concat("Prefix patch ", fix,
                            " has not \"bool\" or \"void\" return type: ", fix.ReturnType));
                    Emitter.Emit(il, OpCodes.Brfalse, label);
                    canHaveJump = true;
                }
            });
            return canHaveJump;
        }

        private static void AddPostfixes(ILGenerator il, MethodBase original, List<MethodInfo> postfixes,
            Dictionary<string, LocalBuilder> variables)
        {
            postfixes.ForEach(delegate(MethodInfo fix)
            {
                EmitCallParameter(il, original, fix, variables);
                Emitter.Emit(il, OpCodes.Call, fix);
                if (fix.ReturnType != typeof(void))
                    throw new Exception(string.Concat("Postfix patch ", fix, " has not \"void\" return type: ",
                        fix.ReturnType));
            });
        }
    }
}