using System.Reflection;
using BepInEx4.Common;

namespace BepInEx4
{
    /// <summary>
    ///     Paths used by BepInEx
    /// </summary>
    public static class Paths
    {
        private static string executablePath;

        /// <summary>
        ///     The directory that the core BepInEx DLLs reside in.
        /// </summary>
        public static string BepInExAssemblyDirectory => BepInEx.Paths.BepInExAssemblyDirectory;

        /// <summary>
        ///     The path to the core BepInEx DLL.
        /// </summary>
        public static string BepInExAssemblyPath { get; } = Utility.CombinePaths(BepInExAssemblyDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.dll");

        /// <summary>
        ///     The path of the currently executing program BepInEx is encapsulated in.
        /// </summary>
        public static string ExecutablePath => BepInEx.Paths.ExecutablePath;

        /// <summary>
        ///     The directory that the currently executing process resides in.
        /// </summary>
        public static string GameRootPath => BepInEx.Paths.GameRootPath;

        /// <summary>
        ///     The path to the Managed folder of the currently running Unity game.
        /// </summary>
        public static string ManagedPath => BepInEx.Paths.ManagedPath;

        /// <summary>
        ///     The path to the patcher plugin folder which resides in the BepInEx folder.
        /// </summary>
        public static string PatcherPluginPath => BepInEx.Paths.PatcherPluginPath;

        /// <summary>
        ///     The path to the main BepInEx folder.
        /// </summary>
        public static string PluginPath { get; set; }

        /// <summary>
        ///     The name of the currently executing process.
        /// </summary>
        public static string ProcessName => BepInEx.Paths.ProcessName;
    }
}