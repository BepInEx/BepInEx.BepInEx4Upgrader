using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Harmony
{
    public class MethodInvoker
    {
        public static FastInvokeHandler GetHandler(DynamicMethod methodInfo, Module module)
        {
            return Handler(methodInfo, module);
        }

        public static FastInvokeHandler GetHandler(MethodInfo methodInfo)
        {
            return Handler(methodInfo, methodInfo.DeclaringType.Module);
        }

        private static FastInvokeHandler Handler(MethodInfo methodInfo, Module module,
            bool directBoxValueAccess = false)
        {
            var dynamicMethod = new DynamicMethod(
                "FastInvoke_" + methodInfo.Name + "_" + (directBoxValueAccess ? "direct" : "indirect"), typeof(object),
                new[]
                {
                    typeof(object),
                    typeof(object[])
                }, module);
            var ilgenerator = dynamicMethod.GetILGenerator();
            if (!methodInfo.IsStatic)
            {
                ilgenerator.Emit(OpCodes.Ldarg_0);
                EmitUnboxIfNeeded(ilgenerator, methodInfo.DeclaringType);
            }

            var flag = true;
            var parameters = methodInfo.GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                var type = parameters[i].ParameterType;
                var isByRef = type.IsByRef;
                if (isByRef) type = type.GetElementType();
                var isValueType = type.IsValueType;
                if (isByRef && isValueType && !directBoxValueAccess)
                {
                    ilgenerator.Emit(OpCodes.Ldarg_1);
                    EmitFastInt(ilgenerator, i);
                }

                ilgenerator.Emit(OpCodes.Ldarg_1);
                EmitFastInt(ilgenerator, i);
                if (isByRef && !isValueType)
                {
                    ilgenerator.Emit(OpCodes.Ldelema, typeof(object));
                }
                else
                {
                    ilgenerator.Emit(OpCodes.Ldelem_Ref);
                    if (isValueType)
                    {
                        if (!isByRef || !directBoxValueAccess)
                        {
                            ilgenerator.Emit(OpCodes.Unbox_Any, type);
                            if (isByRef)
                            {
                                ilgenerator.Emit(OpCodes.Box, type);
                                ilgenerator.Emit(OpCodes.Dup);
                                ilgenerator.Emit(OpCodes.Unbox, type);
                                if (flag)
                                {
                                    flag = false;
                                    ilgenerator.DeclareLocal(typeof(void*), true);
                                }

                                ilgenerator.Emit(OpCodes.Stloc_0);
                                ilgenerator.Emit(OpCodes.Stelem_Ref);
                                ilgenerator.Emit(OpCodes.Ldloc_0);
                            }
                        }
                        else
                        {
                            ilgenerator.Emit(OpCodes.Unbox, type);
                        }
                    }
                }
            }

            if (methodInfo.IsStatic)
                ilgenerator.EmitCall(OpCodes.Call, methodInfo, null);
            else
                ilgenerator.EmitCall(OpCodes.Callvirt, methodInfo, null);
            if (methodInfo.ReturnType == typeof(void))
                ilgenerator.Emit(OpCodes.Ldnull);
            else
                EmitBoxIfNeeded(ilgenerator, methodInfo.ReturnType);
            ilgenerator.Emit(OpCodes.Ret);
            return (FastInvokeHandler) dynamicMethod.CreateDelegate(typeof(FastInvokeHandler));
        }

        private static void EmitCastToReference(ILGenerator il, Type type)
        {
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, type);
                return;
            }

            il.Emit(OpCodes.Castclass, type);
        }

        private static void EmitUnboxIfNeeded(ILGenerator il, Type type)
        {
            if (type.IsValueType) il.Emit(OpCodes.Unbox_Any, type);
        }

        private static void EmitBoxIfNeeded(ILGenerator il, Type type)
        {
            if (type.IsValueType) il.Emit(OpCodes.Box, type);
        }

        private static void EmitFastInt(ILGenerator il, int value)
        {
            switch (value)
            {
                case -1:
                    il.Emit(OpCodes.Ldc_I4_M1);
                    return;
                case 0:
                    il.Emit(OpCodes.Ldc_I4_0);
                    return;
                case 1:
                    il.Emit(OpCodes.Ldc_I4_1);
                    return;
                case 2:
                    il.Emit(OpCodes.Ldc_I4_2);
                    return;
                case 3:
                    il.Emit(OpCodes.Ldc_I4_3);
                    return;
                case 4:
                    il.Emit(OpCodes.Ldc_I4_4);
                    return;
                case 5:
                    il.Emit(OpCodes.Ldc_I4_5);
                    return;
                case 6:
                    il.Emit(OpCodes.Ldc_I4_6);
                    return;
                case 7:
                    il.Emit(OpCodes.Ldc_I4_7);
                    return;
                case 8:
                    il.Emit(OpCodes.Ldc_I4_8);
                    return;
                default:
                    if (value > -129 && value < 128)
                    {
                        il.Emit(OpCodes.Ldc_I4_S, (sbyte) value);
                        return;
                    }

                    il.Emit(OpCodes.Ldc_I4, value);
                    return;
            }
        }
    }
}