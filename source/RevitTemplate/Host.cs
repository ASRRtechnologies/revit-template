using System.Net.Http;
using ASRR.Core.Persistence;
using ASRR.Revit.Core.Http;
using ASRR.Revit.Core.RevitModel;
using Microsoft.Extensions.DependencyInjection;
using RevitTemplate.Config;
using RevitTemplate.Http.Service;
using RevitTemplate.Services;
using RevitTemplate.Settings;

namespace RevitTemplate;

/// <summary>
///     Provides a host for the application's services and manages their lifetimes
/// </summary>
public static class Host
{
    private const string ServerBaseAddress = "http://localhost:4000/";

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

        // Setup storage
        services.AddTransient(_ => PersistentStorageProvider);

        // Setup http server for incoming requests
        services.AddSingleton(_ =>
            new HttpServer(ServerBaseAddress, new ExecuteRequestProcessor(), new ConfigureEventExecutor()));

        // Setup http services for outgoing requests
        SetupHttpClient();
        services.AddTransient(_ => new HttpService(_httpClient));

        // Add configurator services
        services.AddTransient(_ => new ModelFetcher(GetService<HttpService>()));
        services.AddTransient(_ => new FileUploader(GetService<HttpService>()));
        services.AddFacadeConfigurator();
        services.AddProjectConfigurator();

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

    public static void LaunchServer()
    {
        var httpServer = GetService<HttpServer>();
        httpServer.Start();
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
            GetService<ModelFetcher>(),
            GetService<FileUploader>(),
            @"C:\asrr\resources\RevitTemplate\models",
            @"C:\asrr\resources\RevitTemplate\materials"));
    }

    private static void AddProjectConfigurator(this IServiceCollection services)
    {
        services.AddTransient(_ => new ProjectConfiguratorService(
            GetService<HttpService>(),
            GetService<ModelFetcher>(),
            GetService<FileUploader>(),
            GetService<FacadeConfiguratorService>(),
            @"C:\asrr\resources\RevitTemplate\dynamic-models"));
    }
}