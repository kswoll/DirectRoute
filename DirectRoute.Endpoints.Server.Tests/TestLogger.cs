using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectRoute.Endpoints.Server.Tests
{
    public class TestLogger : ILogger, IDisposable
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
        }

        public void Dispose()
        {
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
        }
    }
}
