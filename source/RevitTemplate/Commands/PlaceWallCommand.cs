using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using RevitTemplate.Dto;
using RevitTemplate.Model;
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
            var wallService = new WallService();
            // var materialService = new MaterialService();
            // var material = new MaterialDetails()
            // {
            //     Id = "123",
            //     Name = "Blackpepper_Brick",
            //     Textures = new Dictionary<TextureType, string>
            //     {
            //         {TextureType.RvtPng, @"C:\asrr\resources\RevitTemplate\materials\Blackpepper_Brick.jpg"},
            //         {TextureType.Bump, @"C:\asrr\resources\RevitTemplate\materials\Blackpepper_Brick_bump.jpg"}
            //     }
            // };
            // materialService.CreateMaterial(doc, material);
            var wall = wallService.Place(doc, new XYZ(0, 0, 0), 5400, 2650); // voorgevel
            // wallService.PaintExteriorWallFace(doc, wall, "Blackpepper_Brick");
            wallService.Place(doc, new XYZ(5400, 12000, 0), 5400, 2650, new XYZ(-180, 0, 0)); // achtergevel
            wallService.Place(doc, new XYZ(5400, 0, 0), 12000, 2650, new XYZ(0, 90, 0)); // wand rechts
        }
        catch (Exception e)
        {
            MessageBox.Show($"Failed to place wall. Exception: {e.Message}", "Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
            return Result.Failed;
        }

        return Result.Succeeded;
    }
}