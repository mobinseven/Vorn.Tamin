using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Vorn.Tamin.Extensions;

/// <summary>
/// Configuration options for the EP.Tamin SDK, suitable for use with
/// <c>IConfiguration</c> binding (e.g. <c>appsettings.json</c> section <c>"Tamin"</c>).
/// </summary>
public sealed class TaminOptions
{
    /// <summary>
    /// Base URL for the EP.Tamin API.
    /// Defaults to the sandbox endpoint <c>https://ep-test.tamin.ir/api/</c>.
    /// </summary>
    public string BaseUrl { get; set; } = "https://ep-test.tamin.ir/api/";

    /// <summary>Client-Id header value issued during API onboarding.</summary>
    public string? ClientId { get; set; }

    /// <summary>Pre-obtained OAuth bearer token (optional — use <see cref="Username"/> / <see cref="Password"/> to obtain one at runtime instead).</summary>
    public string? OAuthToken { get; set; }

    /// <summary>Username for the login flow, when no static token is configured.</summary>
    public string? Username { get; set; }

    /// <summary>Password for the login flow, when no static token is configured.</summary>
    public string? Password { get; set; }
}

/// <summary>
/// Extension methods for registering EP.Tamin SDK services into an <see cref="IServiceCollection"/>.
/// </summary>
public static class TaminServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="TaminSession"/> as a scoped service backed by
    /// <see cref="IHttpClientFactory"/>.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configureOptions">Delegate to configure <see cref="TaminOptions"/>.</param>
    /// <returns>The original <paramref name="services"/> for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddTaminClient(o =>
    /// {
    ///     o.BaseUrl   = "https://ep-test.tamin.ir/api/";
    ///     o.ClientId  = "your-client-id";
    ///     o.OAuthToken = "your-token";
    /// });
    /// </code>
    /// Then inject <c>TaminSession</c> into your services normally.
    /// </example>
    public static IServiceCollection AddTaminClient(
        this IServiceCollection services,
        Action<TaminOptions> configureOptions)
    {
        services.Configure(configureOptions);
        services.AddHttpClient("TaminClient");
        services.AddScoped<TaminSession>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<TaminOptions>>().Value;
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = factory.CreateClient("TaminClient");

            var baseUri = new Uri(options.BaseUrl);

            if (!string.IsNullOrWhiteSpace(options.OAuthToken))
                return new TaminSession(httpClient, options.OAuthToken, baseUri, clientId: options.ClientId);

            // If no static token is configured we create the session without one.
            // Callers must invoke TaminSession.CreateAsync(...) to obtain a token at runtime.
            return new TaminSession(httpClient, null, baseUri, needToken: false, clientId: options.ClientId);
        });

        return services;
    }
}
