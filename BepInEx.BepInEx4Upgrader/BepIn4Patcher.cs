using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Bepin4Loader;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using Mono.Cecil;

namespace BepInEx.BepIn4Patcher
{
    public class BepIn4Patcher
    {
        private static readonly ManualLogSource Logger = Logging.Logger.CreateLogSource("BepInEx4Loader");

        private static readonly ConfigFile Config =
            new ConfigFile(Path.Combine(Paths.ConfigPath, "bepinex4loader.cfg"), true);

        private static readonly ConfigWrapper<string> BepInEx4PluginsPath = Config.Wrap(
            "Paths",
            "BepInEx4Plugins",
            "Location of BepInEx 4 plugins inside BepInEx 5 plugins folder",
            "bepinex4_plugins");

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
            var pluginsPath = Path.Combine(Paths.PluginPath, BepInEx4PluginsPath.Value);

            if (!Directory.Exists(pluginsPath))
            {
                Directory.CreateDirectory(pluginsPath);
                return;
            }

            var bepinTypes = new HashSet<string>();
            var bepinFullTypes = new HashSet<string>();

            using (var bepinAss = AssemblyDefinition.ReadAssembly(Assembly.GetExecutingAssembly().Location))
            {
                foreach (var type in bepinAss.MainModule.Types)
                    if (type.Namespace.StartsWith("BepInEx4"))
                        bepinTypes.Add(type.Name);
            }

            using (var origBepInAss =
                AssemblyDefinition.ReadAssembly(Path.Combine(Paths.BepInExAssemblyDirectory, "BepInEx.dll")))
            {
                foreach (var type in origBepInAss.MainModule.Types)
                    bepinFullTypes.Add(type.FullName);
            }

            DefaultAssemblyResolver resolver = new DefaultAssemblyResolver();
            resolver.ResolveFailure += ResolveBepInEx4CecilAssembly;

            var assembliesToConvert = new Dictionary<string, AssemblyDefinition>();

            foreach (var file in Directory.GetFiles(pluginsPath, "*.dll"))
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

                if (harmonyRef != null)
                    harmonyRef.Name = "0Harmony_BepInEx4";

                // TODO: Maybe check against current BepInEx 5 DLL version?
                var bepin5Ref = new AssemblyNameReference("BepInEx", new Version(5, 0, 0, 0));
                ass.MainModule.AssemblyReferences.Add(bepin5Ref);

                foreach (var tr in ass.MainModule.GetTypeReferences())
                {
                    if (tr.Namespace.StartsWith("BepInEx"))
                    {
                        if (bepinTypes.Contains(tr.Name)) // If it's a shimmed type, fix up name
                            tr.Namespace = $"BepInEx4{tr.Namespace.Substring("BepInEx".Length)}";
                        else if (bepinFullTypes.Contains(tr.FullName)) // If it's inside bepin 5, update scope
                            tr.Scope = bepin5Ref;
                    }
                }

                ass.Write(path);
                ass.Dispose();
            }
        }

        private static void RunPatcher()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveBepInEx4Assemblies;
            TypeLoader.AssemblyResolve += ResolveBepInEx4CecilAssembly;

            Logger.LogInfo("Starting BepInEx4 Migrator!");

            ShimPlugins();

            Logger.LogInfo("Initializing BepInEx 4");

            BepInEx4.Paths.PluginPath = Path.Combine(Paths.PluginPath, BepInEx4PluginsPath.Value);
            BepInEx4.Logger.SetLogger(new BepIn4Logger());
        }

        private static AssemblyDefinition ResolveBepInEx4CecilAssembly(object sender, AssemblyNameReference reference)
        {
            try
            {
                if (reference.Name == "BepInEx4" || reference.Name == "0Harmony_BepInEx4")
                    return AssemblyDefinition.ReadAssembly(Assembly.GetExecutingAssembly().Location);
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
            return null;
        }
    }
}