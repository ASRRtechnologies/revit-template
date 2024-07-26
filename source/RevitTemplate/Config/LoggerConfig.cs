using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace RevitTemplate.Config;

/// <summary>
///     Application logging configuration
/// </summary>
/// <example>
/// <code lang="csharp">
/// public class Class(ILogger logger)
/// {
///     private void Execute()
///     {
///         logger.Information("Message");
///     }
/// }
/// </code>
/// </example>
public static class LoggerConfig
{
    private const string LogTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}]: {Message:lj}{NewLine}{Exception}";

    public static void AddSerilogConfiguration(this IServiceCollection services)
    {
        var logger = CreateDefaultLogger();
        services.AddSingleton<ILogger>(logger);

        AppDomain.CurrentDomain.UnhandledException += OnOnUnhandledException;
    }

    private static Logger CreateDefaultLogger()
    {
        return new LoggerConfiguration()
            .WriteTo.Debug(LogEventLevel.Debug, LogTemplate)
            .MinimumLevel.Debug()
            .CreateLogger();
    }

    private static void OnOnUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        var exception = (Exception)args.ExceptionObject;
        var logger = Host.GetService<ILogger>();
        logger.Fatal(exception, "Domain unhandled exception");
    }
}