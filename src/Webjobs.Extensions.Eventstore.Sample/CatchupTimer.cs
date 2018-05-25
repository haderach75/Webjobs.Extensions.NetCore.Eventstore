using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Webjobs.Extensions.Eventstore.Sample
{
    public class CatchupTimer
    {
        private readonly Stopwatch _sw = new Stopwatch();
        private readonly ILogger<CatchupTimer> _logger;

        public CatchupTimer(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CatchupTimer>();
        }

        public void Start()
        {
            if (!_sw.IsRunning)
            {
                _sw.Start();
                _logger.LogInformation("Stopwatch started.");
            }
        }

        public void Stop()
        {
            _sw.Stop();
            _logger.LogInformation($"Stopwatch stopped, Catchup complete in {_sw.ElapsedMilliseconds}ms");
        }
    }
}