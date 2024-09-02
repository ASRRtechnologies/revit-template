using System.Configuration;
using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using RevitTemplate.UI;

namespace RevitTemplate.Commands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class ConfigureFacadeCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiApp = commandData.Application;
        var uidoc = uiApp.ActiveUIDocument;
        var doc = uidoc.Document;
        
        var manualConfigurationInput = new ManualFacadeConfigurationInput();
        var result = manualConfigurationInput.ShowDialog();
        
        if (result != true)
        {
            return Result.Cancelled;
        }
        
        try
        {
            MessageBox.Show($"FacadeConfigurationId Given: {manualConfigurationInput.FacadeConfigurationId}");
        }
        catch (ConfigurationException e)
        {
            MessageBox.Show($"Failed to configure. Exception: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return Result.Failed;
        }

        return Result.Succeeded;
    }
}