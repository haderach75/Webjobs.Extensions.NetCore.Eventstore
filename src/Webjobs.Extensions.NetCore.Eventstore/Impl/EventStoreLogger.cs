using System;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using ILogger = EventStore.ClientAPI.ILogger;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    public class EventStoreLogger : ILogger
    {
        private readonly Microsoft.Extensions.Logging.ILogger _logger;

        public EventStoreLogger(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger("EventStoreConnection");
        }
        
        public void Error(string format, params object[] args)
        {
            _logger.LogError(Log(format, args));
        }

        public void Error(Exception ex, string format, params object[] args)
        {
            _logger.LogError(Log(ex, format, args));
        }

        public void Debug(string format, params object[] args)
        {
            _logger.LogDebug(Log(format, args));
        }

        public void Debug(Exception ex, string format, params object[] args)
        {
            _logger.LogError(Log(ex, format, args));
        }

        public void Info(string format, params object[] args)
        {
            _logger.LogInformation(Log(format, args));
        }

        public void Info(Exception ex, string format, params object[] args)
        {
            _logger.LogInformation(Log(ex, format, args));
        }
        
        private string Log(string format, params object[] args)
        {
            return string.Format("[{0:00},{1:HH:mm:ss.fff}] {2}", Thread.CurrentThread.ManagedThreadId,DateTime.UtcNow,args.Length == 0 ? format : string.Format(format, args));
        }

        private string Log(Exception exc, string format, params object[] args)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (; exc != null; exc = exc.InnerException)
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine(exc.ToString());
            }
            return $"[{Thread.CurrentThread.ManagedThreadId:00},{DateTime.UtcNow:HH:mm:ss.fff}] {(args.Length == 0 ? format : string.Format(format, args))}\nEXCEPTION(S) OCCURRED:{stringBuilder}";
        }
    }
}