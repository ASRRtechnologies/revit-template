using System.Configuration;
using System.Windows;
using ASRR.Core.Persistence;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using RevitTemplate.Services;
using RevitTemplate.Settings;
using RevitTemplate.UI;

namespace RevitTemplate.Commands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class ConfigureProjectCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiApp = commandData.Application;
        var projectConfiguratorService = Host.GetService<ProjectConfiguratorService>();
        var persistentStorageProvider = Host.GetService<IPersistentStorageProvider>();
        var exportSettings = persistentStorageProvider.Fetch<ExportSettings>();

        var input = new ManualConfigurationInput();
        var result = input.ShowDialog();

        if (result != true)
        {
            return Result.Cancelled;
        }

        try
        {
            projectConfiguratorService.Configure(uiApp, input.Id, exportSettings);
        }
        catch (ConfigurationException e)
        {
            MessageBox.Show($"Failed to configure. Exception: {e.Message}", "Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
            return Result.Failed;
        }

        return Result.Succeeded;
    }
}