using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace Harmony
{
    public class PatchProcessor
    {
        private static readonly object locker = typeof(HarmonyLib.Harmony).Assembly.GetType("HarmonyLib.PatchProcessor")
            .GetField("locker", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

        private readonly Type container;

        private readonly HarmonyMethod containerAttributes;

        private readonly HarmonyInstance instance;

        private List<MethodBase> originals = new List<MethodBase>();

        private readonly HarmonyMethod postfix;

        private readonly HarmonyMethod prefix;

        private readonly HarmonyMethod transpiler;

        public PatchProcessor(HarmonyInstance instance, Type type, HarmonyMethod attributes)
        {
            this.instance = instance;
            container = type;
            containerAttributes = attributes ?? new HarmonyMethod(null);
            prefix = containerAttributes.Clone();
            postfix = containerAttributes.Clone();
            transpiler = containerAttributes.Clone();
            PrepareType();
        }

        public PatchProcessor(HarmonyInstance instance, List<MethodBase> originals, HarmonyMethod prefix = null,
            HarmonyMethod postfix = null, HarmonyMethod transpiler = null)
        {
            this.instance = instance;
            this.originals = originals;
            this.prefix = prefix ?? new HarmonyMethod(null);
            this.postfix = postfix ?? new HarmonyMethod(null);
            this.transpiler = transpiler ?? new HarmonyMethod(null);
        }

        public PatchProcessor(HarmonyInstance instance, HarmonyMethod original, HarmonyMethod prefix = null,
            HarmonyMethod postfix = null, HarmonyMethod transpiler = null)
        {
            this.instance = instance;
            containerAttributes = original;
            originals = new List<MethodBase>(new[]
            {
                GetOriginalMethod()
            });
            this.prefix = prefix ?? new HarmonyMethod(null);
            this.postfix = postfix ?? new HarmonyMethod(null);
            this.transpiler = transpiler ?? new HarmonyMethod(null);
        }

        public static Patches GetPatchInfo(MethodBase method)
        {
            var obj = locker;
            Patches result;
            lock (obj)
            {
                var patchInfo = SharedStateShim.GetPatchInfo(method);
                if (patchInfo == null)
                    result = null;
                else
                    result = new Patches(patchInfo.prefixes, patchInfo.postfixes, patchInfo.transpilers);
            }

            return result;
        }

        public static IEnumerable<MethodBase> AllPatchedMethods()
        {
            var obj = locker;
            IEnumerable<MethodBase> patchedMethods;
            lock (obj)
            {
                patchedMethods = SharedStateShim.GetPatchedMethods();
            }

            return patchedMethods;
        }

        public void Patch()
        {
            var obj = locker;
            lock (obj)
            {
                foreach (var methodBase in originals)
                {
                    if (methodBase == null) throw new NullReferenceException("original");
                    if (RunMethod<HarmonyPrepare, bool>(true, methodBase))
                    {
                        var patchInfo = SharedStateShim.GetPatchInfo(methodBase) ?? new PatchInfo();
                        PatchFunctionsShim.AddPrefix(patchInfo, instance.Id, prefix);
                        PatchFunctionsShim.AddPostfix(patchInfo, instance.Id, postfix);
                        PatchFunctionsShim.AddTranspiler(patchInfo, instance.Id, transpiler);
                        PatchFunctionsShim.UpdateWrapper(methodBase, patchInfo, instance.Id);
                        SharedStateShim.UpdatePatchInfo(methodBase, patchInfo);
                        RunMethod<HarmonyCleanup>(methodBase);
                    }
                }
            }
        }

        public void Unpatch(HarmonyPatchType type, string harmonyID)
        {
            var obj = locker;
            lock (obj)
            {
                foreach (var methodBase in originals)
                {
                    var patchInfo = SharedStateShim.GetPatchInfo(methodBase) ?? new PatchInfo();
                    if (type == HarmonyPatchType.All || type == HarmonyPatchType.Prefix)
                        PatchFunctionsShim.RemovePrefix(patchInfo, harmonyID);
                    if (type == HarmonyPatchType.All || type == HarmonyPatchType.Postfix)
                        PatchFunctionsShim.RemovePostfix(patchInfo, harmonyID);
                    if (type == HarmonyPatchType.All || type == HarmonyPatchType.Transpiler)
                        PatchFunctionsShim.RemoveTranspiler(patchInfo, harmonyID);
                    PatchFunctionsShim.UpdateWrapper(methodBase, patchInfo, instance.Id);
                    SharedStateShim.UpdatePatchInfo(methodBase, patchInfo);
                }
            }
        }

        public void Unpatch(MethodInfo patch)
        {
            var obj = locker;
            lock (obj)
            {
                foreach (var methodBase in originals)
                {
                    var patchInfo = SharedStateShim.GetPatchInfo(methodBase) ?? new PatchInfo();
                    PatchFunctionsShim.RemovePatch(patchInfo, patch);
                    PatchFunctionsShim.UpdateWrapper(methodBase, patchInfo, instance.Id);
                    SharedStateShim.UpdatePatchInfo(methodBase, patchInfo);
                }
            }
        }

        private void PrepareType()
        {
            if (!RunMethod<HarmonyPrepare, bool>(true)) return;
            var enumerable = RunMethod<HarmonyTargetMethods, IEnumerable<MethodBase>>(null);
            if (enumerable != null)
            {
                originals = enumerable.ToList();
            }
            else if (Attribute.GetCustomAttribute(container, typeof(HarmonyPatchAll)) != null)
            {
                var originalType = containerAttributes.declaringType;
                originals.AddRange(AccessTools.GetDeclaredConstructors(originalType).Cast<MethodBase>());
                originals.AddRange(AccessTools.GetDeclaredMethods(originalType).Cast<MethodBase>());
            }
            else
            {
                var methodBase = GetOriginalMethod() ?? RunMethod<HarmonyTargetMethod, MethodBase>(null);
                if (methodBase == null)
                    throw new ArgumentException("No target method specified for class " + container.FullName);
                originals.Add(methodBase);
            }

            PatchTools.GetPatches(container, out prefix.method, out postfix.method, out transpiler.method);
            if (prefix.method != null)
            {
                if (!prefix.method.IsStatic)
                    throw new ArgumentException("Patch method " + prefix.method.FullDescription() + " must be static");
                var harmonyMethods = prefix.method.GetHarmonyMethods().Cast<HarmonyLib.HarmonyMethod>().ToList();
                containerAttributes.Merge(HarmonyMethod.Merge(harmonyMethods)).CopyTo(prefix);
            }

            if (postfix.method != null)
            {
                if (!postfix.method.IsStatic)
                    throw new ArgumentException("Patch method " + postfix.method.FullDescription() + " must be static");
                var harmonyMethods2 = postfix.method.GetHarmonyMethods().Cast<HarmonyLib.HarmonyMethod>().ToList();
                containerAttributes.Merge(HarmonyMethod.Merge(harmonyMethods2)).CopyTo(postfix);
            }

            if (transpiler.method != null)
            {
                if (!transpiler.method.IsStatic)
                    throw new ArgumentException("Patch method " + transpiler.method.FullDescription() +
                                                " must be static");
                var harmonyMethods3 = transpiler.method.GetHarmonyMethods().Cast<HarmonyLib.HarmonyMethod>().ToList();
                containerAttributes.Merge(HarmonyMethod.Merge(harmonyMethods3)).CopyTo(transpiler);
            }
        }

        private MethodBase GetOriginalMethod()
        {
            var harmonyMethod = containerAttributes;
            if (harmonyMethod.declaringType == null) return null;
            if (harmonyMethod.methodName == null)
                return AccessTools.Constructor(harmonyMethod.declaringType, harmonyMethod.argumentTypes);
            return AccessTools.Method(harmonyMethod.declaringType, harmonyMethod.methodName,
                harmonyMethod.argumentTypes);
        }

        private T RunMethod<S, T>(T defaultIfNotExisting, params object[] parameters)
        {
            if (container == null) return defaultIfNotExisting;
            var name = typeof(S).Name.Replace("Harmony", "");
            var list = new List<object>();
            list.Add(instance);
            var list2 = list;
            list2.AddRange(parameters);
            var types = AccessTools.GetTypes(list2.ToArray());
            var patchMethod = PatchTools.GetPatchMethod<S>(container, name, types);
            if (patchMethod != null && typeof(T).IsAssignableFrom(patchMethod.ReturnType))
                return (T) patchMethod.Invoke(null, list2.ToArray());
            patchMethod = PatchTools.GetPatchMethod<S>(container, name, new[]
            {
                typeof(HarmonyInstance)
            });
            if (patchMethod != null && typeof(T).IsAssignableFrom(patchMethod.ReturnType))
                return (T) patchMethod.Invoke(null, new object[]
                {
                    instance
                });
            patchMethod = PatchTools.GetPatchMethod<S>(container, name, Type.EmptyTypes);
            if (patchMethod == null) return defaultIfNotExisting;
            object[] emptyTypes;
            if (typeof(T).IsAssignableFrom(patchMethod.ReturnType))
            {
                MethodBase methodBase = patchMethod;
                object obj = null;
                emptyTypes = Type.EmptyTypes;
                return (T) methodBase.Invoke(obj, emptyTypes);
            }

            MethodBase methodBase2 = patchMethod;
            object obj2 = null;
            emptyTypes = Type.EmptyTypes;
            methodBase2.Invoke(obj2, emptyTypes);
            return defaultIfNotExisting;
        }

        private void RunMethod<S>(params object[] parameters)
        {
            if (container == null) return;
            var name = typeof(S).Name.Replace("Harmony", "");
            var list = new List<object>();
            list.Add(instance);
            var list2 = list;
            list2.AddRange(parameters);
            var types = AccessTools.GetTypes(list2.ToArray());
            var patchMethod = PatchTools.GetPatchMethod<S>(container, name, types);
            if (patchMethod != null)
            {
                patchMethod.Invoke(null, list2.ToArray());
                return;
            }

            patchMethod = PatchTools.GetPatchMethod<S>(container, name, new[]
            {
                typeof(HarmonyInstance)
            });
            if (patchMethod != null)
            {
                patchMethod.Invoke(null, new object[]
                {
                    instance
                });
                return;
            }

            patchMethod = PatchTools.GetPatchMethod<S>(container, name, Type.EmptyTypes);
            if (patchMethod != null)
            {
                MethodBase methodBase = patchMethod;
                object obj = null;
                object[] emptyTypes = Type.EmptyTypes;
                methodBase.Invoke(obj, emptyTypes);
            }
        }
    }
}