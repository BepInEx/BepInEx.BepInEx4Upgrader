using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Contract;
using BepInEx.Logging;
using HarmonyLib;
using Mono.Cecil;

namespace BepInEx.BepIn4Patcher
{
    public class BepIn4Patcher
    {
        private static readonly HarmonyLib.Harmony
            PatchInstance = new HarmonyLib.Harmony("com.bepinex.legacy.typeloaderpatcher");

        private static readonly ManualLogSource Logger = Logging.Logger.CreateLogSource("BepInEx4Loader");

        private static readonly ConfigFile Config =
            new ConfigFile(Path.Combine(Paths.ConfigPath, "bepinex4loader.cfg"), true);

        private static readonly ConfigWrapper<string> BepInEx4PluginsPath = Config.Wrap(
            "Paths",
            "BepInEx4Plugins",
            "Location of BepInEx 4 plugins relative to BepInEx root folder",
            "");

        private static readonly ConfigWrapper<bool> BackUpAssemblies = Config.Wrap(
            "Patching",
            "BackupAssemblies",
            "Whether to back up original assemblies in bepinex4_backup folder",
            true);

        private static readonly ConfigWrapper<bool> PatchInMemory = Config.Wrap(
            "Patching",
            "PatchInMemory",
            "If true, will perform patching in memory",
            false);

        private static string PluginsPath;
        private static DefaultAssemblyResolver resolver;
        private static ReaderParameters readerParameters;

        private static readonly Dictionary<string, MemoryAssembly> shimmedAssemblies =
            new Dictionary<string, MemoryAssembly>();

        private static readonly Dictionary<string, Assembly>
            loadedMemoryAssemblies = new Dictionary<string, Assembly>();

        public static IEnumerable<string> TargetDLLs
        {
            get
            {
                RunPatcher();
                yield return string.Empty;
            }
        }

        public static void Patch(AssemblyDefinition ass)
        {
        }

        private static void ShimPlugins()
        {
            if (!Directory.Exists(PluginsPath))
            {
                Directory.CreateDirectory(PluginsPath);
                return;
            }

            var bepinTypes = new HashSet<string>();
            var harmonyTypes = new HashSet<string>();
            var bepinFullTypes = new HashSet<string>();
            var harmonyFullTypes = new Dictionary<string, string>();

            using (var bepinAss = AssemblyDefinition.ReadAssembly(Assembly.GetExecutingAssembly().Location))
            {
                foreach (var type in bepinAss.MainModule.Types)
                    if (type.Namespace.StartsWith("BepInEx4"))
                        bepinTypes.Add(type.Name);
                    else if (type.Namespace.StartsWith("Harmony"))
                        harmonyTypes.Add(type.Name);
            }

            using (var origBepInAss =
                AssemblyDefinition.ReadAssembly(Path.Combine(Paths.BepInExAssemblyDirectory, "BepInEx.dll")))
            {
                foreach (var type in origBepInAss.MainModule.Types)
                    bepinFullTypes.Add(type.FullName);
            }

            using (var origHarmonyAss =
                AssemblyDefinition.ReadAssembly(Path.Combine(Paths.BepInExAssemblyDirectory, "0Harmony.dll")))
            {
                foreach (var type in origHarmonyAss.MainModule.Types)
                    harmonyFullTypes[type.Name] = type.Namespace;
            }

            var resolver = new DefaultAssemblyResolver();
            resolver.ResolveFailure += ResolveBepInEx4CecilAssembly;

            var assembliesToConvert = new Dictionary<string, AssemblyDefinition>();

            foreach (var file in Directory.GetFiles(PluginsPath, "*.dll"))
            {
                var name = Path.GetFileNameWithoutExtension(file);

                try
                {
                    var ass = AssemblyDefinition.ReadAssembly(new MemoryStream(File.ReadAllBytes(file)),
                        new ReaderParameters
                        {
                            AssemblyResolver = resolver
                        });

                    if (!ass.MainModule.AssemblyReferences.Any(r => r.Name == "BepInEx" && r.Version.Major <= 4) &&
                        !ass.MainModule.AssemblyReferences.Any(r => r.Name == "0Harmony" && r.Version.Major <= 1))
                    {
                        ass.Dispose();
                        continue;
                    }

                    assembliesToConvert[file] = ass;
                }
                catch (Exception e)
                {
                    Logger.LogWarning($"Skipping loading {name} because: {e.Message}");
                }
            }

            if (BackUpAssemblies.Value)
            {
                var backupDir = Path.Combine(Paths.BepInExRootPath, "bepinex4_backup");
                if (!Directory.Exists(backupDir))
                    Directory.CreateDirectory(backupDir);

                foreach (var kv in assembliesToConvert)
                {
                    var filePath = kv.Key;
                    var fileName = Path.GetFileName(filePath);

                    File.Copy(filePath, Path.Combine(backupDir, fileName), true);
                }
            }

            Logger.LogInfo($"Found {assembliesToConvert.Count} assemblies to shim");

            foreach (var kv in assembliesToConvert)
            {
                var ass = kv.Value;
                var path = kv.Key;

                Logger.LogInfo($"Shimming {ass.Name.Name}");

                var bepin4Ref =
                    ass.MainModule.AssemblyReferences.FirstOrDefault(r => r.Name == "BepInEx" && r.Version.Major <= 4);

                if (bepin4Ref != null)
                    bepin4Ref.Name = "BepInEx4";

                var harmonyRef =
                    ass.MainModule.AssemblyReferences.FirstOrDefault(r => r.Name == "0Harmony" && r.Version.Major <= 1);

                AssemblyNameReference harmony2Ref = null;
                if (harmonyRef != null)
                {
                    harmonyRef.Name = "0Harmony_BepInEx4";
                    harmony2Ref = new AssemblyNameReference("0Harmony", new Version(2, 0, 0, 0));
                    ass.MainModule.AssemblyReferences.Add(harmony2Ref);
                }

                // TODO: Maybe check against current BepInEx 5 DLL version?
                var bepin5Ref = new AssemblyNameReference("BepInEx", new Version(5, 0, 0, 0));
                ass.MainModule.AssemblyReferences.Add(bepin5Ref);

                foreach (var tr in ass.MainModule.GetTypeReferences())
                    if (tr.Namespace.StartsWith("BepInEx"))
                    {
                        if (bepinTypes.Contains(tr.Name)) // If it's a shimmed type, fix up name
                            tr.Namespace = $"BepInEx4{tr.Namespace.Substring("BepInEx".Length)}";
                        else if (bepinFullTypes.Contains(tr.FullName)) // If it's inside bepin 5, update scope
                            tr.Scope = bepin5Ref;
                    }
                    else if (tr.Namespace.StartsWith("Harmony"))
                    {
                        if (!harmonyTypes.Contains(tr.Name) && harmonyFullTypes.TryGetValue(tr.Name, out var @namespace)
                        ) // If it's inside harmony 2, change ref
                        {
                            tr.Namespace = @namespace;
                            tr.Scope = harmony2Ref;
                        }
                    }

                if (PatchInMemory.Value)
                    using (var ms = new MemoryStream())
                    {
                        ass.Write(ms);
                        shimmedAssemblies[ass.FullName] = new MemoryAssembly
                        {
                            data = ms.ToArray(),
                            path = path
                        };
                    }
                else
                    ass.Write(path);

                ass.Dispose();
            }
        }

        public static void TypeLoadHook(ref Dictionary<AssemblyDefinition, List<PluginInfo>> __result, string directory,
            Func<TypeDefinition, PluginInfo> typeSelector)
        {
            if (directory != Paths.PluginPath)
                return;

            foreach (var dll in Directory.GetFiles(Path.GetFullPath(PluginsPath), "*.dll",
                SearchOption.TopDirectoryOnly))
                try
                {
                    var ass = AssemblyDefinition.ReadAssembly(dll, readerParameters);

                    if (shimmedAssemblies.TryGetValue(ass.FullName, out var memAsm))
                    {
                        ass.Dispose();
                        ass = AssemblyDefinition.ReadAssembly(new MemoryStream(memAsm.data), readerParameters);
                    }

                    var matches = ass.MainModule.Types.Select(typeSelector).Where(t => t != null).ToList();

                    if (matches.Count == 0)
                    {
                        ass.Dispose();
                        continue;
                    }

                    if (memAsm != null)
                        foreach (var pluginInfo in matches)
                        {
                            typeof(PluginInfo).GetProperty(nameof(PluginInfo.Location))
                                .SetValue(pluginInfo, memAsm.path, null);
                            loadedMemoryAssemblies[ass.FullName] = Assembly.Load(memAsm.data);
                        }

                    __result[ass] = matches;
                }
                catch (Exception e)
                {
                    Logger.LogError(e.ToString());
                }
        }

        private static void Finish()
        {
            PatchInstance.Patch(
                typeof(TypeLoader).GetMethod(nameof(TypeLoader.FindPluginTypes)).MakeGenericMethod(typeof(PluginInfo)),
                postfix: new HarmonyMethod(typeof(BepIn4Patcher).GetMethod(nameof(TypeLoadHook))));
        }

        private static void RunPatcher()
        {
            resolver = new DefaultAssemblyResolver();
            readerParameters = new ReaderParameters {AssemblyResolver = resolver};

            resolver.ResolveFailure += (sender, reference) =>
            {
                var name = new AssemblyName(reference.FullName);

                if (Utility.TryResolveDllAssembly(name, Paths.BepInExAssemblyDirectory, readerParameters,
                        out var assembly) ||
                    Utility.TryResolveDllAssembly(name, Paths.PluginPath, readerParameters, out assembly) ||
                    Utility.TryResolveDllAssembly(name, Paths.ManagedPath, readerParameters, out assembly) ||
                    Utility.TryResolveDllAssembly(name, PluginsPath, readerParameters, out assembly))
                    return assembly;

                if (shimmedAssemblies.TryGetValue(reference.FullName, out var memAsm))
                    return AssemblyDefinition.ReadAssembly(new MemoryStream(memAsm.data), readerParameters);

                return ResolveBepInEx4CecilAssembly(sender, reference);
            };

            PluginsPath = Path.Combine(Paths.BepInExRootPath, BepInEx4PluginsPath.Value);

            AppDomain.CurrentDomain.AssemblyResolve += ResolveBepInEx4Assemblies;

            Logger.LogInfo("Starting BepInEx4 Migrator!");

            ShimPlugins();

            Logger.LogInfo("Initializing BepInEx 4");

            BepInEx4.Paths.PluginPath = PluginsPath;
            BepInEx4.Logger.SetLogger(new BepIn4Logger());
        }

        private static AssemblyDefinition ResolveBepInEx4CecilAssembly(object sender, AssemblyNameReference reference)
        {
            try
            {
                if (reference.Name == "BepInEx4" || reference.Name == "0Harmony_BepInEx4")
                    return AssemblyDefinition.ReadAssembly(Assembly.GetExecutingAssembly().Location);
                return AssemblyDefinition.ReadAssembly(Path.Combine(PluginsPath, $"{reference.Name}.dll"));
            }
            catch (Exception)
            {
            }

            return null;
        }

        private static Assembly ResolveBepInEx4Assemblies(object sender, ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name).Name;
            if (name == "BepInEx4" || name == "0Harmony_BepInEx4")
                return Assembly.GetExecutingAssembly();

            if (loadedMemoryAssemblies.TryGetValue(args.Name, out var res))
                return res;

            try
            {
                return Assembly.LoadFile(Path.Combine(PluginsPath, $"{name}.dll"));
            }
            catch (Exception)
            {
            }

            return null;
        }

        private class MemoryAssembly
        {
            public byte[] data;
            public string path;
        }
    }
}