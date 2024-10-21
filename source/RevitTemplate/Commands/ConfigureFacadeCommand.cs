using System.Configuration;
using System.Windows;
using ASRR.Core.Persistence;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using RevitTemplate.Dto;
using RevitTemplate.Services;
using RevitTemplate.Settings;
using RevitTemplate.UI;

namespace RevitTemplate.Commands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class ConfigureFacadeCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiApp = commandData.Application;
        var facadeConfigurationService = Host.GetService<FacadeConfiguratorService>();
        var persistentStorageProvider = Host.GetService<IPersistentStorageProvider>();
        var exportSettings = persistentStorageProvider.Fetch<ExportSettings>();
        
        var facadeConfigurationInput = new ManualFacadeConfigurationInput();
        var result = facadeConfigurationInput.ShowDialog();
        
        if (result != true)
        {
            return Result.Cancelled;
        }
        
        try
        {
            facadeConfigurationService.Configure(uiApp, facadeConfigurationInput.FacadeConfigurationId, exportSettings);
        }
        catch (ConfigurationException e)
        {
            MessageBox.Show($"Failed to configure. Exception: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return Result.Failed;
        }

        return Result.Succeeded;
    }
}