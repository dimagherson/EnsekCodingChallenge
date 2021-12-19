using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnsekCodingChallenge.Application.DataAccess
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDataAccess(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var connectionString = configuration.GetConnectionString("EnsekDB");

            return services
                .AddScoped<IMeterReadingsDataAccess, MeterReadingsDataAccess>()
                .AddScoped(x => new ConnectionString(connectionString));
        }
    }
}
