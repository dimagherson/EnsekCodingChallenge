using System;
using Microsoft.Extensions.DependencyInjection;

namespace EnsekCodingChallenge.Application.DataAccess
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDataAccess(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddScoped<IMeterReadingsDataAccess, MeterReadingsDataAccess>();
        }
    }
}
