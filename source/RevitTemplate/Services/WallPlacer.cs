using ASRR.Revit.Core.Model;
using ASRR.Revit.Core.Utilities;
using ASRR.Revit.Core.Warnings;
using RevitTemplate.Exceptions;

namespace RevitTemplate.Services;

public class WallPlacer
{
    public Wall Place(Document doc, XYZ position, double width, double height, XYZ rotation = null)
    {
        var levels = new FilteredElementCollector(doc)
            .WhereElementIsNotElementType()
            .OfCategory(BuiltInCategory.INVALID)
            .OfClass(typeof(Level));

        if (levels.FirstElement() is not Level firstLevel)
        {
            throw new ConfigurationFailedException("No level found in document");
        }

        var wallTypes = new FilteredElementCollector(doc)
            .WhereElementIsElementType()
            .OfCategory(BuiltInCategory.OST_Walls)
            .OfClass(typeof(WallType));

        if (wallTypes.FirstElement() is not WallType wallType)
        {
            throw new ConfigurationFailedException("No wall type found in document");
        }

        using var transaction = WarningDiscardFailuresPreprocessor.GetTransaction(doc);
        transaction.Start("Create Wall");

        var startPosition = CoordinateUtilities.ConvertMmToFeet(position);
        var endPosition = CoordinateUtilities.ConvertMmToFeet(new XYZ(position.X + width, position.Y, position.Z));
        var line = Line.CreateBound(startPosition, endPosition);

        try
        {
            var created = Wall.Create(
                doc,
                line,
                wallType.Id,
                firstLevel.Id,
                CoordinateUtilities.ConvertMmToFeet(height),
                0,
                false,
                true);


            var vectorRotation = new VectorRotation(rotation);
            if (vectorRotation.RotationInDegrees != 0.0)
            {
                RotateWall(created, vectorRotation);
            }

            transaction.Commit();
            return created;
        }
        catch (Exception)
        {
            transaction.Commit();
            throw;
        }
    }

    public bool RotateWall(Element element, VectorRotation rotation)
    {
        var rotated = false;

        if (element.Location is LocationCurve curve)
        {
            var line = curve.Curve;
            var aa = line.GetEndPoint(0);
            var cc = new XYZ(aa.X, aa.Y, aa.Z + 10);
            var axis = Line.CreateBound(aa, cc);
            rotated = curve.Rotate(axis, rotation.RotationInRadians);
        }

        return rotated;
    }

    public void CreateOpening(Document doc, Wall wall, XYZ position, double width, double height, XYZ rotation = null)
    {
        using var transaction = WarningDiscardFailuresPreprocessor.GetTransaction(doc);
        transaction.Start("Create opening");
        
        var vectorRotation = new VectorRotation(rotation);
        var startPosition = CoordinateUtilities.ConvertMmToFeet(position);
        var endPosition =
            CoordinateUtilities.ConvertMmToFeet(new XYZ(position.X + width, position.Y, position.Z + height));
        
        try
        {
            var opening = doc.Create.NewOpening(wall, startPosition, endPosition);

            if (vectorRotation.RotationInDegrees != 0.0)
            {
                // TODO
            }

            transaction.Commit();
        }
        catch (Exception)
        {
            transaction.Commit();
            throw;
        }
    }
}