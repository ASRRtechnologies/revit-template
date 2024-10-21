using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using RevitTemplate.Services;

namespace RevitTemplate.Commands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class PlaceWallCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiApp = commandData.Application;
        var uiDoc = uiApp.ActiveUIDocument;
        var doc = uiDoc.Document;
        
        try
        {
            var wallPlacer = Host.GetService<WallPlacer>();
            wallPlacer.Place(doc, new XYZ(0, 0, 0), 5400, 2650); // voorgevel
            wallPlacer.Place(doc, new XYZ(5400, 12000, 0), 5400, 2650, new XYZ(-180, 0, 0)); // achtergevel
            wallPlacer.Place(doc, new XYZ(5400, 0, 0), 12000, 2650, new XYZ(0, 90, 0)); // wand rechts
        }
        catch (Exception e)
        {
            MessageBox.Show($"Failed to place wall. Exception: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return Result.Failed;
        }

        return Result.Succeeded;
    }
}