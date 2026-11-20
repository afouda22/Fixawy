namespace Fixawyy.Services;

public static class BookingServiceCollectionExtensions
{
    /// <summary>Registers everything the Teammate 2 Booking &amp; Review Engine needs (one line in Program.cs).</summary>
    public static IServiceCollection AddBookingEngine(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DevTestingOptions>(configuration.GetSection("DevTesting"));

        services.AddSingleton<IBookingClock, BookingClock>();
        services.AddScoped<ICurrentActorProvider, CurrentActorProvider>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<ITechnicianCatalogService, TechnicianCatalogService>();

        return services;
    }
}
