using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Harmony
{
    public class DelegateTypeFactory
    {
        private static int counter;

        private readonly ModuleBuilder module;

        public DelegateTypeFactory()
        {
            counter++;
            var name = new AssemblyName("HarmonyDTFAssembly" + counter);
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
            module = assemblyBuilder.DefineDynamicModule("HarmonyDTFModule" + counter);
        }

        public Type CreateDelegateType(MethodInfo method)
        {
            var attr = TypeAttributes.Public | TypeAttributes.Sealed;
            var typeBuilder = module.DefineType("HarmonyDTFType" + counter, attr, typeof(MulticastDelegate));
            typeBuilder.DefineConstructor(
                MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.HideBySig |
                MethodAttributes.RTSpecialName, CallingConventions.Standard, new[]
                {
                    typeof(object),
                    typeof(IntPtr)
                }).SetImplementationFlags(MethodImplAttributes.CodeTypeMask);
            var parameters = method.GetParameters();
            var methodBuilder = typeBuilder.DefineMethod("Invoke",
                MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Virtual |
                MethodAttributes.HideBySig, method.ReturnType, parameters.Types());
            methodBuilder.SetImplementationFlags(MethodImplAttributes.CodeTypeMask);
            for (var i = 0; i < parameters.Length; i++)
                methodBuilder.DefineParameter(i + 1, ParameterAttributes.None, parameters[i].Name);
            return typeBuilder.CreateType();
        }
    }
}