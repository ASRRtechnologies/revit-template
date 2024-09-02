using System.Net.Http;
using ASRR.Core.Persistence;
using ASRR.Revit.Core.Http;
using Microsoft.Extensions.DependencyInjection;
using RevitTemplate.Config;
using RevitTemplate.Service;
using RevitTemplate.Settings;

namespace RevitTemplate;

/// <summary>
///     Provides a host for the application's services and manages their lifetimes
/// </summary>
public static class Host
{
    private static IServiceProvider _serviceProvider;

    private static readonly IPersistentStorageProvider PersistentStorageProvider =
        new JsonBasedPersistenceProvider(typeof(Host).Namespace);

    private static HttpClient _httpClient;

    /// <summary>
    ///     Starts the host and configures the application's services
    /// </summary>
    public static void Start()
    {
        var services = new ServiceCollection();

        services.AddSerilogConfiguration();

        // Setup storage and http services
        SetupHttpClient();
        services.AddTransient(_ => PersistentStorageProvider);
        services.AddTransient(_ => new HttpService(_httpClient));

        // Add configurator services
        services.AddFacadeConfigurator();
        services.AddTransient(_ => new WallPlacer());

        _serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    ///     Get service of type <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The type of service object to get</typeparam>
    /// <exception cref="System.InvalidOperationException">There is no service of type <typeparamref name="T"/></exception>
    public static T GetService<T>() where T : class
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    private static void SetupHttpClient()
    {
        var databaseSettings = PersistentStorageProvider.Fetch<DatabaseSettings>();
        _httpClient = new HttpClient {BaseAddress = new Uri(databaseSettings.BaseUrl)};
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", databaseSettings.ApiKey);
    }

    private static void AddFacadeConfigurator(this IServiceCollection services)
    {
        services.AddTransient(_ => new FacadeConfiguratorService(
            GetService<HttpService>(),
            @"C:\asrr\resources\RevitTemplate",
            @"C:\asrr\output\RevitTemplate"));
    }
}