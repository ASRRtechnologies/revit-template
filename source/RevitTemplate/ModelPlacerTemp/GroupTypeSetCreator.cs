using ASRR.Revit.Core;
using ASRR.Revit.Core.Exporter.Groups.Model;
using ASRR.Revit.Core.Model;
using ASRR.Revit.Core.Utilities;

namespace RevitTemplate.ModelPlacerTemp;

public class GroupTypeSetCreator
{
    /// <param name="doc"></param>
    /// <param name="modelGroup"></param>
    /// <param name="includeOffset">
    ///     Indicates whether the offset of the group relative to the origin of the document should be
    ///     accounted for
    /// </param>
    /// <returns></returns>
    public GroupTypeSet Create(Document doc, Group modelGroup, bool includeOffset)
    {
        var detailGroupTypes = GetAttachedDetailGroupTypes(doc, modelGroup);

        var groupTypeSet = new GroupTypeSet
        {
            ModelGroupType = modelGroup.GroupType,
            PositionOffset = CreatePosition(modelGroup, includeOffset),
            AttachedDetailGroupTypes = detailGroupTypes
        };

        return groupTypeSet;
    }

    private static IPosition CreatePosition(Group modelGroup, bool includeOffset)
    {
        var position = TransformUtilities.GetPosition(modelGroup);

        if (!includeOffset)
            return new MillimeterPosition(new XYZ(0, 0, position.PositionInMillimeters.Z));

        return position;
    }

    private List<AttachedDetailGroupType> GetAttachedDetailGroupTypes(Document doc, Group modelGroup)
    {
        var detailGroupTypes = new List<AttachedDetailGroupType>();
        var detailGroups = FindAttachedDetailGroups(doc, modelGroup);
        var allFloorPlans = Collector.GetAllOfType<ViewPlan>(doc)
            .Where(v => v.ViewType == ViewType.FloorPlan).ToList();

        foreach (var detailGroup in detailGroups)
        {
            var ownerViewId = detailGroup.OwnerViewId;
            var ownerFloorPlan = allFloorPlans.FirstOrDefault(floorplan => floorplan.Id == ownerViewId);

            if (ownerFloorPlan != null)
                detailGroupTypes.Add(new AttachedDetailGroupType
                {
                    GroupType = detailGroup.GroupType,
                    FloorPlanName = ownerFloorPlan.Name
                });
        }

        return detailGroupTypes;
    }

    private List<Group> FindAttachedDetailGroups(Document doc, Group modelGroup)
    {
        ElementFilter familyCategoryFilter = new ElementCategoryFilter(BuiltInCategory.OST_IOSAttachedDetailGroups);
        ICollection<Element> detailGroups =
            new FilteredElementCollector(doc).WherePasses(familyCategoryFilter).ToElements();

        return detailGroups.Where(dg => HasGroupAsParent(dg, modelGroup)).Select(dg => dg as Group).ToList();
    }

    private bool HasGroupAsParent(Element detailGroup, Element modelGroup)
    {
        var groupNameParameter = detailGroup.get_Parameter(BuiltInParameter.GROUP_ATTACHED_PARENT_NAME);
        if (groupNameParameter == null)
            return false;

        return modelGroup.Name == groupNameParameter.AsString();
    }
}