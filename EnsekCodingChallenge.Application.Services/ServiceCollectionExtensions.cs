using System;
using Microsoft.Extensions.DependencyInjection;

namespace EnsekCodingChallenge.Application.Services
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddScoped<IMeterReadingsService, MeterReadingsService>();
        }
    }
}
