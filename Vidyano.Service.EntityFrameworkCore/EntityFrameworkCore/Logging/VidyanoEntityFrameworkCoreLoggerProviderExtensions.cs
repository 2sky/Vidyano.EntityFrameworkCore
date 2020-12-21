using Microsoft.Extensions.Logging;

namespace Vidyano.Service.EntityFrameworkCore.Logging
{
    public static class VidyanoEntityFrameworkCoreLoggerProviderExtensions
    {
        public static ILoggingBuilder AddVidyanoEntityFrameworkCore(this ILoggingBuilder builder)
        {
            builder
                .AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information)
                .AddProvider(new VidyanoEntityFrameworkCoreLoggerProvider());
            return builder;
        }
    }
}