using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vidyano.Service.Charts;
using Vidyano.Service.EntityFrameworkCore;
using Vidyano.Service.EntityFrameworkCore.Dto;

namespace Vidyano.Service.SqlServer
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class VidyanoSqlServerServiceCollectionExtensions
    {
        public static IServiceCollection AddVidyanoSqlServer(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddVidyanoDefaults(configuration);

            services.AddDbContext<DefaultRepositoryProvider>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("Vidyano"));
            });

            services.AddSingleton<ProviderSpecificService, EntityFrameworkCoreSpecificService>();
            services.AddSingleton<ProviderSpecificChartService>();
            services.AddSingleton<Repository.DataLayer.IRepositoryCacheUpdateStore, CacheSynchronizer>();
            services.AddScoped<Repository.DataLayer.IRepositoryLogStore>(provider => provider.GetRequiredService<DefaultRepositoryProvider>());
            services.AddScoped<Repository.DataLayer.IRepositoryProvider>(provider => provider.GetRequiredService<DefaultRepositoryProvider>());
            services.AddScoped<Repository.DataLayer.IRepositoryRegisteredStreamStore>(provider => provider.GetRequiredService<DefaultRepositoryProvider>());
            services.AddScoped<Repository.DataLayer.IRepositoryFeedbackStore>(provider => provider.GetRequiredService<DefaultRepositoryProvider>());
            services.AddScoped<Repository.DataLayer.IRepositorySettingStore>(provider => provider.GetRequiredService<DefaultRepositoryProvider>());
            services.AddScoped<Repository.DataLayer.IRepositoryUserStore, DefaultRepositoryUserStore>();
            services.AddScoped<Repository.DataLayer.IRepositoryUserNotificationStore, DefaultRepositoryUserNotificationStore>();

            return services;
        }
    }
}