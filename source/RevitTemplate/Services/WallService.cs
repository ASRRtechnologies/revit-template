using System.IO;
using ASRR.Revit.Core;
using ASRR.Revit.Core.Model;
using ASRR.Revit.Core.RevitModel;
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
            throw new WallCreationFailedException("No level found in document");
        }

        var wallTypes = new FilteredElementCollector(doc)
            .WhereElementIsElementType()
            .OfCategory(BuiltInCategory.OST_Walls)
            .OfClass(typeof(WallType));

        if (wallTypes.FirstElement() is not WallType wallType)
        {
            throw new WallCreationFailedException("No wall type found in document");
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

            transaction.Commit();

            // var vectorRotation = new VectorRotation(rotation);
            // if (vectorRotation.RotationInDegrees != 0.0)
            // {
            //     RotateWall(doc, created, vectorRotation);
            // }

            if (rotation != null && rotation.Y != 0.0)
            {
                var degreeRotation = new DegreeRotation(rotation.Y);
                RotateWallDegrees(doc, created, degreeRotation);
            }

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
        transaction.Start("Rotate wall");

        try
        {
            if (element.Location is LocationCurve curve)
            {
                var line = curve.Curve;
                var aa = line.GetEndPoint(0);
                var cc = new XYZ(aa.X, aa.Y, aa.Z + 10);
                var axis = Line.CreateBound(aa, cc);
                Console.WriteLine(rotation.RotationInDegrees);
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

    public bool RotateWallDegrees(Document doc, Element element, DegreeRotation rotation)
    {
        var rotated = false;

        using var transaction = WarningDiscardFailuresPreprocessor.GetTransaction(doc);
        transaction.Start("Rotate wall");

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

    public bool RotateElementDegrees(Document doc, ElementId elementId, DegreeRotation rotation)
    {
        var rotated = false;

        var elements = new FilteredElementCollector(doc)
            .WhereElementIsNotElementType().ToList();

        var element = elements.FirstOrDefault(e => e.Id == elementId);

        if (element == null)
        {
            return false;
        }

        using var transaction = WarningDiscardFailuresPreprocessor.GetTransaction(doc);
        transaction.Start("Rotate element");

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
            else if (element.Location is LocationPoint)
            {
                TransformUtilities.Rotate(element, rotation);
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

    public static ElementId CopyPasteWall(Document doc, string sourcePath, ElementId wallId)
    {
        if (!File.Exists(sourcePath))
        {
            throw new WallCreationFailedException($"No file found at '{sourcePath}'");
        }

        using var sourceDoc = doc.Application.OpenDocumentFile(sourcePath);
        var copyPasteOptions = DocumentUtilities.CopyPasteOptions();

        using var transaction = WarningDiscardFailuresPreprocessor.GetTransaction(doc);
        transaction.Start("Copy-pasting wall");

        var copied = ElementTransformUtils.CopyElements(sourceDoc, new List<ElementId> {wallId},
            doc, null, copyPasteOptions);

        transaction.Commit();
        sourceDoc.Close();

        return copied.FirstOrDefault();
    }

    public List<ElementId> CopyPasteElements(Document doc, string sourcePath)
    {
        if (!File.Exists(sourcePath))
        {
            throw new WallCreationFailedException($"No file found at '{sourcePath}'");
        }

        using var sourceDoc = doc.Application.OpenDocumentFile(sourcePath);
        var copyPasteOptions = DocumentUtilities.CopyPasteOptions();

        using var transaction = WarningDiscardFailuresPreprocessor.GetTransaction(doc);
        transaction.Start("Copy-pasting elements");

        var elements = ModelElementCollector.GetParentModelElements(sourceDoc).ToList();
        var elementIds = elements.Select(e => e.Id).ToList();
        var copied = ElementTransformUtils.CopyElements(sourceDoc, elementIds, doc, 
            null, copyPasteOptions);

        transaction.Commit();
        sourceDoc.Close();

        return copied.ToList();
    }
}