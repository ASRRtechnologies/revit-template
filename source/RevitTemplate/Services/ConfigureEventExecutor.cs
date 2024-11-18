using ASRR.Core.Persistence;
using Autodesk.Revit.UI;
using RevitTemplate.Http.Service;
using RevitTemplate.Settings;

namespace RevitTemplate.Services;

public class ConfigureEventExecutor : IExternalEventExecutor
{
    public void Execute(UIApplication uiApp, string queueId)
    {
        // _logger.Info($"--------------STARTING CONFIGURATION SEQUENCE FOR QUEUE ID '{queueId}'-----------------------------------");
        var projectConfiguratorService = Host.GetService<ProjectConfiguratorService>();
        var persistentStorageProvider = Host.GetService<IPersistentStorageProvider>();
        var exportSettings = persistentStorageProvider.Fetch<ExportSettings>();

        projectConfiguratorService.Configure(uiApp, queueId, exportSettings);
    }
}