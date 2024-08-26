namespace RevitTemplate.Service;

public class WallPlacer
{
    public void Place(Document doc, XYZ position, double width, double height)
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

        var endPosition = new XYZ(position.X + width, position.Y, position.Z);
        var line = Line.CreateBound(position, endPosition);
        Wall.Create(doc, line, wallType.Id, firstLevel.Id, height, 0, false, true);

        transaction.Commit();
    }
}