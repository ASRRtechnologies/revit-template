using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using RevitTemplate.Service;

namespace RevitTemplate.Commands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class PlaceWallCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiApp = commandData.Application;
        var uidoc = uiApp.ActiveUIDocument;
        var doc = uidoc.Document;

        try
        {
            var wallPlacer = Host.GetService<WallPlacer>();
            wallPlacer.Place(doc, new XYZ(0, 0, 0), 5400, 2650, 0);
            wallPlacer.Place(doc, new XYZ(5400, -12000, 0), 5400, 2650, 180);
            wallPlacer.Place(doc, new XYZ(5400, 0, 0), 12000, 2650, -90);
        }
        catch (Exception e)
        {
            MessageBox.Show($"Failed to place wall. Exception: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return Result.Failed;
        }

        return Result.Succeeded;
    }
}