using Microsoft.Extensions.Logging;

namespace Vidyano.Service.EntityFrameworkCore.Logging
{
    /// <summary>
    /// Passes EntityFrameworkCore Command logging to Profiler.
    /// </summary>
    [ProviderAlias("vidyano-efcore")]
    public class VidyanoEntityFrameworkCoreLoggerProvider : ILoggerProvider
    {
        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
        {
            return new VidyanoEntityFrameworkCoreLogger();
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}