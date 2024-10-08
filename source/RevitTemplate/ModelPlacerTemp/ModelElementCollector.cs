namespace RevitTemplate.ModelPlacerTemp;

public static class ModelElementCollector
{
    //These categories are to be made sure that they are included when looking for modelelements
    private static readonly List<BuiltInCategory> VitalCategories = new List<BuiltInCategory>
    {
        BuiltInCategory.OST_IOSModelGroups,
        BuiltInCategory.OST_MEPSpaces,
        BuiltInCategory.OST_RoomSeparationLines,
        BuiltInCategory.OST_FloorOpening
    };

    public static IEnumerable<Element> GetParentModelElements(Document doc, IEnumerable<Element> elements)
    {
        return GetParentModelElements(doc, elements.Select(e => e.Id).ToList());
    }

    public static IEnumerable<Element> GetParentModelElements(Document doc,
        ICollection<ElementId> elementIds = null)
    {
        var collector = elementIds == null
            ? new FilteredElementCollector(doc)
            : new FilteredElementCollector(doc, elementIds);

        //Find all the toplevel modelElements in the given elements and return only those
        using (collector)
        {
            IEnumerable<Element> modelElements = collector.WherePasses(GetModelCategoryFilter(doc)).ToElements();

            //Filter out everything that isn't top level (and filter out duplicates)
            var parentElements = modelElements.Select(element => GetParentModelElement(doc, element)).ToList();
            parentElements = parentElements.GroupBy(e => e.Id).Select(g => g.First()).ToList();

            return parentElements.Where(IsModelElement);
        }
    }

    //Returns the highest element in the hierarchy of a given element
    public static Element GetParentModelElement(Document doc, Element element)
    {
        //See if it is a member of a group
        if (element.GroupId != ElementId.InvalidElementId)
            return GetParentModelElement(doc, doc.GetElement(element.GroupId));

        //If the element is a family instance, we check for its supercomponent
        if (element is FamilyInstance familyInstance && familyInstance.SuperComponent != null)
            return GetParentModelElement(doc, familyInstance.SuperComponent);

        //None of the above, so the element is a parent itself
        return element;
    }

    public static bool IsModelElement(Element element)
    {
        if (element.Category == null || element.ViewSpecific || element.Location == null)
            return false;

        return element.Category.CategoryType == CategoryType.Model ||
               element.Category.CategoryType == CategoryType.Internal;
    }

    private static ElementFilter GetModelCategoryFilter(Document doc)
    {
        //Get all the categories that are associated with geometry in the document
        var modelCategories = doc.Settings.Categories.Cast<Category>()
            .Where(c => c.CategoryType == CategoryType.Model && c.CanAddSubcategory)
            .Select(c => (BuiltInCategory) c.Id.IntegerValue);

        modelCategories = modelCategories.Union(VitalCategories);

        //Create a filter based on those categories
        var categoryFilters = modelCategories
            .Select(category => new ElementCategoryFilter(category) as ElementFilter).ToList();
        var modelFilter = new LogicalOrFilter(categoryFilters);

        return modelFilter;
    }
}