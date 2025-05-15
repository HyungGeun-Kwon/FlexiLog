using System;
using System.Threading;

namespace FlexiLog.Writer
{
    public sealed class LogEntry
    {
        public ELogLevel LogLevel { get; }
        public string Message { get; }
        public Exception Exception { get; }
        public string ThreadName { get; }
        public DateTime Timestamp { get; }

        public LogEntry(ELogLevel level, string message, Exception ex = null)
        {
            LogLevel = level;
            Message = message;
            Exception = ex;
            ThreadName = Thread.CurrentThread.Name ?? $"Thread-{Thread.CurrentThread.ManagedThreadId}";
            Timestamp = DateTime.Now;
        }
    }
}
