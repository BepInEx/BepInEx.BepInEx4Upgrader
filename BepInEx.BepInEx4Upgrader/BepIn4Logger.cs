using BepInEx.Logging;
using BepInEx4.Logging;
using Logger = BepInEx.Logging.Logger;

namespace BepInEx.BepIn4Patcher
{
    public class BepIn4Logger : BaseLogger
    {
        private ManualLogSource logger = Logger.CreateLogSource("BepInEx4");

        public override void Log(LogLevel level, object entry)
        {
            logger.Log(level, entry);
        }
    }
}
