using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Logging;
using Vidyano.Service.Profiling;

namespace Vidyano.Service.EntityFrameworkCore.Logging
{
    public class VidyanoEntityFrameworkCoreLogger : ILogger
    {
        /// <inheritdoc />
        public IDisposable? BeginScope<TState>(TState state)
        {
            return null;
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel) => true;

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            IProfiler? profiler;
            try
            {
                profiler = IProfiler.Current;
                if (profiler == null)
                    return;
            }
            catch
            {
                // NOTE: EF core migrations will also try to Log the commands
                return;
            }

            if (eventId.Id == 20101 // Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted
                                    //&& state is Microsoft.Extensions.Logging.LoggerMessage.LogValues<string, string, System.Data.CommandType, int, string, string>
                && state is IReadOnlyList<KeyValuePair<string, object>> data)
            {
                T GetData<T>(string key)
                {
                    return (T)data.First(kvp => kvp.Key == key).Value;
                }

                var commandId = Guid.NewGuid().ToString("n");
                var commandText = GetData<string>("commandText");
                var type = GetData<CommandType>("commandType").ToString();
                var elapsedMilliseconds = decimal.Parse(GetData<string>("elapsed"), CultureInfo.InvariantCulture);
                var commandTimeout = GetData<int?>("commandTimeout");

                var parametersData = GetData<string?>("parameters");
                if (!string.IsNullOrEmpty(parametersData))
                {
                    var newLine = GetData<string?>("newLine") ?? "\n";
                    // @__userId_0='?' (DbType = Guid)
                    commandText = $"/*{newLine}{parametersData}{newLine}*/{newLine}{newLine}{commandText}";
                }

                profiler.AddDB(commandId, commandText, elapsedMilliseconds, type, commandTimeout);
            }
        }
    }
}