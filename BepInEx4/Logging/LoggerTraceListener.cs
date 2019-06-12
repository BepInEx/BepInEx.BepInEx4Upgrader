using System.Diagnostics;

namespace BepInEx4.Logging
{
	/// <summary>
	/// A trace listener that writes to an underlying <see cref="BaseLogger"/> instance.
	/// </summary>
	/// <inheritdoc cref="TraceListener"/>
    public class LoggerTraceListener : TraceListener
    {
		/// <summary>
		/// The logger instance that is being written to.
		/// </summary>
        public BaseLogger Logger { get; }

		/// <param name="logger">The logger instance to write to.</param>
        public LoggerTraceListener(BaseLogger logger)
        {
        }
		
		/// <summary>
		/// Writes a message to the underlying <see cref="BaseLogger"/> instance.
		/// </summary>
		/// <param name="message">The message to write.</param>
        public override void Write(string message)
        {
        }
		
	    /// <summary>
	    /// Writes a message and a newline to the underlying <see cref="BaseLogger"/> instance.
	    /// </summary>
	    /// <param name="message">The message to write.</param>
        public override void WriteLine(string message)
        {
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        { }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
        }
    }
}
