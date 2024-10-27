using ASRR.Revit.Core.Elements.Rotation;
using ASRR.Revit.Core.Model;
using ASRR.Revit.Core.Utilities;
using ASRR.Revit.Core.Warnings;
using RevitTemplate.Exceptions;

namespace RevitTemplate.Services;

public class WallService
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
        var endVector = new XYZ(position.X + width, position.Y, position.Z);
        var endPosition = CoordinateUtilities.ConvertMmToFeet(endVector);
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
                RotateWall(doc, created, vectorRotation);
            }

            transaction.Commit();
            return created;
        }
        catch (Exception)
        {
            transaction.RollBack();
            throw;
        }
    }

    public bool RotateWall(Document doc, Element element, VectorRotation rotation)
    {
        var rotated = false;

        using var transaction = WarningDiscardFailuresPreprocessor.GetTransaction(doc);
        transaction.Start("Create opening");

        try
        {
            if (element.Location is LocationCurve curve)
            {
                var line = curve.Curve;
                var aa = line.GetEndPoint(0);
                var cc = new XYZ(aa.X, aa.Y, aa.Z + 10);
                var axis = Line.CreateBound(aa, cc);
                rotated = curve.Rotate(axis, rotation.RotationInRadians);
            }
        }
        catch (Exception)
        {
            transaction.RollBack();
            throw;
        }

        transaction.Commit();
        return rotated;
    }

    public void CreateOpening(Document doc, Wall wall, XYZ position, double width, double height)
    {
        using var transaction = WarningDiscardFailuresPreprocessor.GetTransaction(doc);
        transaction.Start("Create opening");
        
        var startPosition = CoordinateUtilities.ConvertMmToFeet(position);
        var endVector = new XYZ(position.X + width, position.Y, position.Z + height);
        var endPosition = CoordinateUtilities.ConvertMmToFeet(endVector);

        try
        {
            doc.Create.NewOpening(wall, startPosition, endPosition);
            transaction.Commit();
        }
        catch (Exception)
        {
            transaction.Commit();
            throw;
        }
    }

    public void PaintExteriorWallFace(Document doc, Wall wall, string materialName)
    {
        var materials = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_Materials)
            .OfClass(typeof(Material));

        var material = materials.FirstOrDefault(m => m.Name == materialName);
        if (material == null)
        {
            throw new ConfigurationFailedException(
                $"Could not paint wall. No material with name '{materialName}' found in document");
        }

        var wallOrientation = wall.Orientation.Negate();

        using var transaction = WarningDiscardFailuresPreprocessor.GetTransaction(doc);
        transaction.Start("Painting wall");

        try
        {
            var geometryElement = wall.get_Geometry(new Options());
            foreach (var geometryObject in geometryElement)
            {
                if (geometryObject is not Solid solid) continue;
                foreach (Face face in solid.Faces)
                {
                    if (face is not PlanarFace planarFace) continue;
                    if (planarFace.FaceNormal.IsAlmostEqualTo(wallOrientation) && !doc.IsPainted(wall.Id, face))
                    {
                        doc.Paint(wall.Id, face, material.Id);
                    }
                }
            }
        }
        catch (Exception)
        {
            transaction.RollBack();
            throw;
        }

        transaction.Commit();
    }
}