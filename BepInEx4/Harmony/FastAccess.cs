using System;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Harmony
{
    public class FastAccess
    {
        public static InstantiationHandler CreateInstantiationHandler(Type type)
        {
            var constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                null, new Type[0], null);
            if (constructor == null)
                throw new ApplicationException(
                    $"The type {type} must declare an empty constructor (the constructor may be private, internal, protected, protected internal, or public).");
            var dynamicMethod = new DynamicMethod("InstantiateObject_" + type.Name,
                MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Static,
                CallingConventions.VarArgs, typeof(object), null, type, true);
            var ilgenerator = dynamicMethod.GetILGenerator();
            ilgenerator.Emit(OpCodes.Newobj, constructor);
            ilgenerator.Emit(OpCodes.Ret);
            return (InstantiationHandler) dynamicMethod.CreateDelegate(typeof(InstantiationHandler));
        }

        public static GetterHandler CreateGetterHandler(PropertyInfo propertyInfo)
        {
            var getMethod = propertyInfo.GetGetMethod(true);
            var dynamicMethod = CreateGetDynamicMethod(propertyInfo.DeclaringType);
            var ilgenerator = dynamicMethod.GetILGenerator();
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Call, getMethod);
            BoxIfNeeded(getMethod.ReturnType, ilgenerator);
            ilgenerator.Emit(OpCodes.Ret);
            return (GetterHandler) dynamicMethod.CreateDelegate(typeof(GetterHandler));
        }

        public static GetterHandler CreateGetterHandler(FieldInfo fieldInfo)
        {
            var dynamicMethod = CreateGetDynamicMethod(fieldInfo.DeclaringType);
            var ilgenerator = dynamicMethod.GetILGenerator();
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldfld, fieldInfo);
            BoxIfNeeded(fieldInfo.FieldType, ilgenerator);
            ilgenerator.Emit(OpCodes.Ret);
            return (GetterHandler) dynamicMethod.CreateDelegate(typeof(GetterHandler));
        }

        public static GetterHandler CreateFieldGetter(Type type, params string[] names)
        {
            foreach (var name in names)
            {
                if (AccessTools.Field(typeof(ILGenerator), name) != null)
                    return CreateGetterHandler(AccessTools.Field(type, name));
                if (AccessTools.Property(typeof(ILGenerator), name) != null)
                    return CreateGetterHandler(AccessTools.Property(type, name));
            }

            return null;
        }

        public static SetterHandler CreateSetterHandler(PropertyInfo propertyInfo)
        {
            var setMethod = propertyInfo.GetSetMethod(true);
            var dynamicMethod = CreateSetDynamicMethod(propertyInfo.DeclaringType);
            var ilgenerator = dynamicMethod.GetILGenerator();
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldarg_1);
            UnboxIfNeeded(setMethod.GetParameters()[0].ParameterType, ilgenerator);
            ilgenerator.Emit(OpCodes.Call, setMethod);
            ilgenerator.Emit(OpCodes.Ret);
            return (SetterHandler) dynamicMethod.CreateDelegate(typeof(SetterHandler));
        }

        public static SetterHandler CreateSetterHandler(FieldInfo fieldInfo)
        {
            var dynamicMethod = CreateSetDynamicMethod(fieldInfo.DeclaringType);
            var ilgenerator = dynamicMethod.GetILGenerator();
            ilgenerator.Emit(OpCodes.Ldarg_0);
            ilgenerator.Emit(OpCodes.Ldarg_1);
            UnboxIfNeeded(fieldInfo.FieldType, ilgenerator);
            ilgenerator.Emit(OpCodes.Stfld, fieldInfo);
            ilgenerator.Emit(OpCodes.Ret);
            return (SetterHandler) dynamicMethod.CreateDelegate(typeof(SetterHandler));
        }

        private static DynamicMethod CreateGetDynamicMethod(Type type)
        {
            return new DynamicMethod("DynamicGet_" + type.Name, typeof(object), new[]
            {
                typeof(object)
            }, type, true);
        }

        private static DynamicMethod CreateSetDynamicMethod(Type type)
        {
            return new DynamicMethod("DynamicSet_" + type.Name, typeof(void), new[]
            {
                typeof(object),
                typeof(object)
            }, type, true);
        }

        private static void BoxIfNeeded(Type type, ILGenerator generator)
        {
            if (type.IsValueType) generator.Emit(OpCodes.Box, type);
        }

        private static void UnboxIfNeeded(Type type, ILGenerator generator)
        {
            if (type.IsValueType) generator.Emit(OpCodes.Unbox_Any, type);
        }
    }
}