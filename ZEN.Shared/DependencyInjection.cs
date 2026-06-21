using Microsoft.Extensions.DependencyInjection;

namespace ZEN.Shared;

public static class DependencyInjection
{
    public static IServiceCollection AddSharedComponents(this IServiceCollection services)
    {
        // Register any shared services here if needed
        return services;
    }
}
