using ASRR.Core.Persistence;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using RevitTemplate.Settings;

namespace RevitTemplate.Commands.Settings;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class OpenExportSettingsCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var persistentStorageProvider = Host.GetService<IPersistentStorageProvider>();
        persistentStorageProvider.Open<ExportSettings>();
        return Result.Succeeded;
    }
}