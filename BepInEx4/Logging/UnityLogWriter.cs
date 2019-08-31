using BepInEx.Logging;
using UnityEngine;

namespace BepInEx4.Logging
{
    /// <summary>
    ///     Logs entries using Unity specific outputs.
    /// </summary>
    public class UnityLogWriter : BaseLogger
    {
        /// <summary>
        ///     Writes a string specifically to the game output log.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteToLog(string value)
        {
        }

        protected void InternalWrite(string value)
        {
        }

        /// <summary>
        ///     Logs an entry to the Logger instance.
        /// </summary>
        /// <param name="level">The level of the entry.</param>
        /// <param name="entry">The textual value of the entry.</param>
        public override void Log(LogLevel level, object entry)
        {
        }

        public override void WriteLine(string value)
        {
        }

        public override void Write(char value)
        {
        }

        public override void Write(string value)
        {
        }

        /// <summary>
        ///     Start listening to Unity's log message events and sending the messages to BepInEx logger.
        /// </summary>
        public static void ListenUnityLogs()
        {
        }

        private static void OnUnityLogMessageReceived(string message, string stackTrace, LogType type)
        {
        }
    }
}