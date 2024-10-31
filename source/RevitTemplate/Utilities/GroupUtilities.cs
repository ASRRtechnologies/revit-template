using System.IO;
using ASRR.Revit.Core;
using ASRR.Revit.Core.Model;
using ASRR.Revit.Core.RevitModel;
using ASRR.Revit.Core.Warnings;
using RevitTemplate.Exceptions;

namespace RevitTemplate.Utilities;

public static class GroupUtilities
{
    public static void CreateGroup(Document doc, string groupName)
    {
        var elements = ModelElementCollector.GetParentModelElements(doc).ToList();
        var ids = elements.Select(e => e.Id).ToList();
        
        using var transaction = WarningDiscardFailuresPreprocessor.GetTransaction(doc);
        transaction.Start("Create Group");
        try
        {
            if (ids.Count > 0)
            {
                var group = doc.Create.NewGroup(ids);
                group.GroupType.Name = groupName;

            }

            transaction.Commit();
        }
        catch (Exception)
        {
            transaction.RollBack();
            throw;
        }
    }
    
    public static void CreateGroup(Document doc, List<ElementId> elementIds, string groupName)
    {
        var elements = ModelElementCollector.GetParentModelElements(doc).ToList();
        var ids = elements.Where(e => elementIds.Contains(e.Id)).Select(e => e.Id).ToList();
        
        using var transaction = WarningDiscardFailuresPreprocessor.GetTransaction(doc);
        transaction.Start("Create Group");
        try
        {
            if (ids.Count > 0)
            {
                var group = doc.Create.NewGroup(ids);
                group.GroupType.Name = groupName;
            }

            transaction.Commit();
        }
        catch (Exception)
        {
            transaction.RollBack();
            throw;
        }
    }
    
    public static void RotateGroup(Document doc, string groupName, IRotation rotation)
    {
        var groups = new FilteredElementCollector(doc)
            .WhereElementIsNotElementType()
            .OfCategory(BuiltInCategory.OST_IOSModelGroups)
            .OfClass(typeof(Group))
            .Cast<Group>()
            .ToList();

        var matchingGroups = groups.Where(g => g.Name == groupName).ToList();
        if (matchingGroups.Count() > 1)
        {
            throw new TransformationFailedException($"More than 1 group with name $'{groupName}' found");
        }
        var group = matchingGroups.FirstOrDefault();

        if (group == null)
        {
            throw new TransformationFailedException($"Could not find group '{groupName}' in doc");
        }
        
        using var transaction = WarningDiscardFailuresPreprocessor.GetTransaction(doc);
        transaction.Start("Rotate group");

        try
        {
            if (rotation != null) TransformUtilities.Rotate(group, rotation);
            transaction.Commit();
        }
        catch (Exception)
        {
            transaction.RollBack();
            throw;
        }
    }
}