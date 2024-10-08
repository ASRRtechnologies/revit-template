using ASRR.Revit.Core.Warnings;

namespace RevitTemplate.Services;

public class WallPlacer
{
    public Wall Place(Document doc, XYZ position, double width, double height, double rotation)
    {
        var levels = new FilteredElementCollector(doc)
            .WhereElementIsNotElementType()
            .OfCategory(BuiltInCategory.INVALID)
            .OfClass(typeof(Level));

        var firstLevel = levels.FirstElement() as Level;

        var wallTypes = new FilteredElementCollector(doc)
            .WhereElementIsElementType()
            .OfCategory(BuiltInCategory.OST_Walls)
            .OfClass(typeof(WallType));

        var wallType = wallTypes.FirstElement() as WallType;

        using var transaction = WarningDiscardFailuresPreprocessor.GetTransaction(doc);
        transaction.Start("Create Wall");

        var startPosition = ConvertMmToFeet(position);
        var endPosition = ConvertMmToFeet(new XYZ(position.X + width, position.Y, position.Z));
        var line = Line.CreateBound(startPosition, endPosition);

        var created = Wall.Create(doc, line, wallType.Id, firstLevel.Id, ConvertMmToFeet(height), 0, false, true);
        // CreateOpening(doc, created, ConvertMmToFeet(new XYZ(startPosition.X + 1200, startPosition.Y, startPosition.Z + 1200)), ConvertMmToFeet(200), ConvertMmToFeet(200), rotation);

        if (rotation != 0.0)
        {
            RotateWall(created, rotation);
        }

        transaction.Commit();
        return created;
    }

    public bool RotateWall(Element element, double degrees)
    {
        var rotated = false;

        if (element.Location is LocationCurve curve)
        {
            var line = curve.Curve;
            var aa = line.GetEndPoint(0);
            var cc = new XYZ(aa.X, aa.Y, aa.Z + 10);
            var axis = Line.CreateBound(aa, cc);
            rotated = curve.Rotate(axis, ConvertToRadians(degrees));
        }

        return rotated;
    }

    public bool CreateOpening(Document doc, Wall wall, XYZ position, double width, double height, double rotation)
    {
        using var transaction = WarningDiscardFailuresPreprocessor.GetTransaction(doc);
        transaction.Start("Place opening");
        var startPosition = ConvertMmToFeet(position);
        var endPoint = new XYZ(startPosition.X + ConvertMmToFeet(width), startPosition.Y, startPosition.Z + ConvertMmToFeet(height));
        // todo: validate points are in wall
        try
        {
            var opening = doc.Create.NewOpening(wall, startPosition, endPoint);
            
            if (rotation != 0.0)
            {
                RotateWall(opening, rotation);
            }

            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            transaction.Commit();
            return false;
        }
    }

    // temp utils
    public static XYZ ConvertMmToFeet(XYZ vector)
    {
        return new XYZ
        (
            ConvertMmToFeet(vector.X),
            ConvertMmToFeet(vector.Y),
            ConvertMmToFeet(vector.Z)
        );
    }

    public static double ConvertMmToFeet(double millimeterValue)
    {
        return UnitUtils.Convert(millimeterValue, UnitTypeId.Millimeters,
            UnitTypeId.Feet);
    }

    private static double ConvertToRadians(double angle)
    {
        return Math.PI / 180 * angle;
    }
}