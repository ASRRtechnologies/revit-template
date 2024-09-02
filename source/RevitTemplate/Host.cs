using ASRR.Core.Persistence;
using Microsoft.Extensions.DependencyInjection;
using RevitTemplate.Config;
using RevitTemplate.Service;
using RevitTemplate.Settings;
using System.Net.Http;
using ASRR.Revit.Core.Http;
using Serilog;

namespace RevitTemplate;

/// <summary>
///     Provides a host for the application's services and manages their lifetimes
/// </summary>
public static class Host
{
    private static IServiceProvider _serviceProvider;

    private static readonly IPersistentStorageProvider persistentStorageProvider =
        new JsonBasedPersistenceProvider(typeof(Host).Namespace);

    private static HttpClient _httpClient;
    
    /// <summary>
    ///     Starts the host and configures the application's services
    /// </summary>
    public static void Start()
    {
        var services = new ServiceCollection();
        
        services.AddSerilogConfiguration();
        
        // Setup http
        SetupHttpClient();
        services.AddTransient(_ => persistentStorageProvider);
        services.AddTransient(_ => new HttpService(_httpClient));
        
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
        var databaseSettings = persistentStorageProvider.Fetch<DatabaseSettings>();
        _httpClient = new HttpClient {BaseAddress = new Uri(databaseSettings.BaseUrl) };
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", databaseSettings.ApiKey);
    }
}