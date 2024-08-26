﻿namespace RevitTemplate.Service;

public class WallPlacer
{
    public void Place(Document doc, XYZ position, double width, double height, double rotation)
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

        using var transaction = new Transaction(doc);
        transaction.Start("Create Wall");
        
        var startPosition = ConvertMmToFeet(position);
        var endPosition = ConvertMmToFeet(new XYZ(position.X + width, position.Y, position.Z));
        var line = Line.CreateBound(startPosition, endPosition);
        
        var created = Wall.Create(doc, line, wallType.Id, firstLevel.Id, ConvertMmToFeet(height), 0, false, true);

        if (rotation != 0.0)
        {
            RotateWall(created, rotation);
        }
        
        transaction.Commit();
    }

    public bool RotateWall(Wall wall, double degrees)
    {
        var rotated = false;

        if (wall.Location is LocationCurve curve)
        {
            var line = curve.Curve;
            var aa = line.GetEndPoint(0);
            var cc = new XYZ(aa.X, aa.Y, aa.Z + 10);
            var axis = Line.CreateBound(aa, cc);
            rotated = curve.Rotate(axis, ConvertToRadians(degrees));
        }

        return rotated;
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