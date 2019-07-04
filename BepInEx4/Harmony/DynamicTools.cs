using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Harmony.ILCopying;

namespace Harmony
{
    public static class DynamicTools
    {
        public static DynamicMethod CreateDynamicMethod(MethodBase original, string suffix)
        {
            if (original == null) throw new ArgumentNullException("original cannot be null");
            var name = (original.Name + suffix).Replace("<>", "");
            var parameters = original.GetParameters();
            var list = parameters.Types().ToList();
            if (!original.IsStatic) list.Insert(0, typeof(object));
            var parameterTypes = list.ToArray();
            var dynamicMethod = new DynamicMethod(name,
                MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Static,
                CallingConventions.Standard, AccessTools.GetReturnedType(original), parameterTypes,
                original.DeclaringType, true);
            for (var i = 0; i < parameters.Length; i++)
                dynamicMethod.DefineParameter(i + 1, parameters[i].Attributes, parameters[i].Name);
            return dynamicMethod;
        }

        public static ILGenerator CreateSaveableMethod(MethodBase original, string suffix,
            out AssemblyBuilder assemblyBuilder, out TypeBuilder typeBuilder)
        {
            var assemblyName = new AssemblyName("DebugAssembly");
            var folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            assemblyBuilder =
                AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave,
                    folderPath);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name, assemblyName.Name + ".dll");
            typeBuilder = moduleBuilder.DefineType("Debug" + original.DeclaringType.Name, TypeAttributes.Public);
            if (original == null) throw new ArgumentNullException("original cannot be null");
            var text = original.Name + suffix;
            text = text.Replace("<>", "");
            var list = original.GetParameters().Types().ToList();
            if (!original.IsStatic) list.Insert(0, typeof(object));
            var parameterTypes = list.ToArray();
            return typeBuilder.DefineMethod(text,
                MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Static,
                CallingConventions.Standard, AccessTools.GetReturnedType(original), parameterTypes).GetILGenerator();
        }

        public static void SaveMethod(AssemblyBuilder assemblyBuilder, TypeBuilder typeBuilder)
        {
            typeBuilder.CreateType();
            assemblyBuilder.Save("HarmonyDebugAssembly.dll");
        }

        public static LocalBuilder[] DeclareLocalVariables(MethodBase original, ILGenerator il, bool logOutput = true)
        {
            var methodBody = original.GetMethodBody();
            var list = methodBody != null ? methodBody.LocalVariables : null;
            if (list == null) return new LocalBuilder[0];
            return list.Select(delegate(LocalVariableInfo lvi)
            {
                var localBuilder = il.DeclareLocal(lvi.LocalType, lvi.IsPinned);
                if (logOutput) Emitter.LogLocalVariable(il, localBuilder);
                return localBuilder;
            }).ToArray();
        }

        public static LocalBuilder DeclareLocalVariable(ILGenerator il, Type type)
        {
            if (type.IsByRef) type = type.GetElementType();
            if (AccessTools.IsClass(type))
            {
                var localBuilder = il.DeclareLocal(type);
                Emitter.LogLocalVariable(il, localBuilder);
                Emitter.Emit(il, OpCodes.Ldnull);
                Emitter.Emit(il, OpCodes.Stloc, localBuilder);
                return localBuilder;
            }

            if (AccessTools.IsStruct(type))
            {
                var localBuilder2 = il.DeclareLocal(type);
                Emitter.LogLocalVariable(il, localBuilder2);
                Emitter.Emit(il, OpCodes.Ldloca, localBuilder2);
                Emitter.Emit(il, OpCodes.Initobj, type);
                return localBuilder2;
            }

            if (AccessTools.IsValue(type))
            {
                var localBuilder3 = il.DeclareLocal(type);
                Emitter.LogLocalVariable(il, localBuilder3);
                if (type == typeof(float))
                    Emitter.Emit(il, OpCodes.Ldc_R4, 0f);
                else if (type == typeof(double))
                    Emitter.Emit(il, OpCodes.Ldc_R8, 0.0);
                else if (type == typeof(long))
                    Emitter.Emit(il, OpCodes.Ldc_I8, 0L);
                else
                    Emitter.Emit(il, OpCodes.Ldc_I4, 0);
                Emitter.Emit(il, OpCodes.Stloc, localBuilder3);
                return localBuilder3;
            }

            return null;
        }

        public static void PrepareDynamicMethod(DynamicMethod method)
        {
            var bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic;
            var bindingAttr2 = BindingFlags.Static | BindingFlags.NonPublic;
            var method2 = typeof(DynamicMethod).GetMethod("CreateDynMethod", bindingAttr);
            if (method2 != null)
            {
                method2.Invoke(method, new object[0]);
                return;
            }

            var method3 = typeof(RuntimeHelpers).GetMethod("_CompileMethod", bindingAttr2);
            var runtimeMethodHandle = (RuntimeMethodHandle) typeof(DynamicMethod)
                .GetMethod("GetMethodDescriptor", bindingAttr).Invoke(method, new object[0]);
            var method4 = typeof(RuntimeMethodHandle).GetMethod("GetMethodInfo", bindingAttr);
            if (method4 != null)
            {
                var obj = method4.Invoke(runtimeMethodHandle, new object[0]);
                method3.Invoke(null, new[]
                {
                    obj
                });
                return;
            }

            if (method3.GetParameters()[0].ParameterType == typeof(IntPtr))
            {
                method3.Invoke(null, new object[]
                {
                    runtimeMethodHandle.Value
                });
                return;
            }

            method3.Invoke(null, new object[]
            {
                runtimeMethodHandle
            });
        }
    }
}