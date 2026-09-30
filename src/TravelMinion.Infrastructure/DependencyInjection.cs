using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using TravelMinion.Application;
using TravelMinion.Infrastructure.Persistence;

namespace TravelMinion.Infrastructure;

/// <summary>Registers the infrastructure adapters with the DI container.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the research sources, URL fetcher and the Semantic Kernel enricher.
    /// The Tavily source is only used when an API key is configured.
    /// </summary>
    public static IServiceCollection AddTravelMinionResearch(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<TavilyOptions>(configuration.GetSection(TavilyOptions.SectionName));
        services.Configure<JinaOptions>(configuration.GetSection(JinaOptions.SectionName));

        services.AddHttpClient<TavilyResearchSource>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<TavilyOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
            }

            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddHttpClient<JinaUrlFetcher>(client => client.Timeout = TimeSpan.FromSeconds(15));
        services.AddHttpClient<DuckDuckGoResearchSource>(client => client.Timeout = TimeSpan.FromSeconds(10));

        services.AddTransient(sp => new ResearchEngine(
            sp.GetRequiredService<IResearchEnricher>(),
            sp.GetRequiredService<JinaUrlFetcher>(),
            sp.GetRequiredService<DuckDuckGoResearchSource>(),
            string.IsNullOrWhiteSpace(sp.GetRequiredService<IOptions<TavilyOptions>>().Value.ApiKey)
                ? null
                : sp.GetRequiredService<TavilyResearchSource>()));

        return services;
    }

    /// <summary>
    /// Registers the Semantic Kernel chat completion service for a profile, and
    /// the LLM-backed research enricher that depends on it.
    /// Only OpenAI-compatible <c>/chat/completions</c> providers are supported at present.
    /// </summary>
    public static IServiceCollection AddTravelMinionLlm(this IServiceCollection services, LlmProfile profile)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(profile);

        services.AddKernel().AddOpenAIChatCompletion(profile.ModelId, new Uri(profile.BaseUrl), profile.ApiKey);
        services.AddTransient<IResearchEnricher, SemanticKernelResearchEnricher>();
        return services;
    }

    /// <summary>
    /// Registers the EF Core context and the Trip repository. The provider is chosen
    /// from <c>Database:Provider</c> (SqlServer in production, Sqlite for local dev).
    /// </summary>
    public static IServiceCollection AddTravelMinionPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = configuration["Database:Provider"] ?? "Sqlite";
        var connectionString = configuration.GetConnectionString("TravelMinion");

        services.AddDbContext<TravelMinionDbContext>(options =>
        {
            if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlServer(connectionString
                    ?? throw new InvalidOperationException(
                        "ConnectionStrings:TravelMinion is required for the SqlServer provider."));
            }
            else
            {
                options.UseSqlite(connectionString ?? "Data Source=travelminion.db");
            }
        });

        services.AddScoped<ITripRepository, TripRepository>();
        return services;
    }
}
