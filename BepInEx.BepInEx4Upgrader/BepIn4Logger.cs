using BepInEx.Logging;
using BepInEx4.Logging;
using Logger = BepInEx.Logging.Logger;
using LogLevel4 = BepInEx4.Logging.LogLevel;
using LogLevel = BepInEx.Logging.LogLevel;

namespace BepInEx.Bepin4Loader
{
    public class BepIn4Logger : BaseLogger
    {
        private ManualLogSource logger = Logger.CreateLogSource("BepInEx4");

        public override void Log(LogLevel4 level, object entry)
        {
            logger.Log((LogLevel)(int)level, entry);
        }
    }
}
