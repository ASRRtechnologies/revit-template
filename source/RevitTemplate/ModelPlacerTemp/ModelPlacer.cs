using System.IO;
using ASRR.Revit.Core;
using ASRR.Revit.Core.Exporter.Groups.Model;
using ASRR.Revit.Core.Exporter.Groups.Service;
using ASRR.Revit.Core.Model;
using ASRR.Revit.Core.Warnings;
using NLog;

namespace RevitTemplate.ModelPlacerTemp;

public class ModelPlacer
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly ModelElementCopyPaster _copyPaster;
    private readonly GroupTypeCopyPaster _groupTypeCopyPaster;
    private readonly GroupTypeSetCreator _groupTypeSetCreator;

    public ModelPlacer()
    {
        _copyPaster = new ModelElementCopyPaster();
        _groupTypeSetCreator = new GroupTypeSetCreator();
        _groupTypeCopyPaster = new GroupTypeCopyPaster();
    }

    public void Place(Document doc, string filePath, XYZ position, XYZ rotation, int levelId) // TODO: use levelId
    {
        if (!File.Exists(filePath))
        {
            _logger.Error($"Could not find file at: '{filePath}'");
            return;
        }

        _logger.Info($"Collecting contents of '{filePath}' in file");
        using (var sourceDoc = doc.Application.OpenDocumentFile(filePath))
        {
            // var existingGroupTypes = Collector.GetAllOfType<GroupType>(doc).ToList();

            var modelElementsInSourceDoc = GetModelElementsInSourceDoc(sourceDoc);
            if (modelElementsInSourceDoc == null) return;

            // var existingModelGroups = GetExistingModelGroups(modelElementsInSourceDoc, existingGroupTypes);

            foreach (var sourceModelGroup in modelElementsInSourceDoc)
            {
                var group = sourceModelGroup as Group;
                var groupTypeSet = _groupTypeSetCreator.Create(sourceDoc, group, false);
                var groupTypeSetAsList = new List<GroupTypeSet> {groupTypeSet};

                var copiedGroupTypeSet =
                    _groupTypeCopyPaster.CopyGroupTypeSets(sourceDoc, doc, groupTypeSetAsList).First();

                _logger.Info(
                    $"Copied grouptypeset has {copiedGroupTypeSet.AttachedDetailGroupTypes.Count} atached deetail groups");
                _groupTypeCopyPaster.PlaceModelGroup(doc, copiedGroupTypeSet, group, new MillimeterPosition(position));
                sourceDoc.Close(false);
            }

            //Copy all models that don't conflict with existing grouptypes in the destination document
            // var copyPastableElementIds = modelElementsInSourceDoc.Except(existingModelGroups).Select(e => e.Id).Distinct().ToList();
            // if (copyPastableElementIds.Any())
            //     pastedIds.AddRange(_copyPaster.CopyPasteModelElements(sourceDoc, doc, copyPastableElementIds));
            //
            // //Separately copy-paste the modelgroups that have an existing grouptype in the destination document
            // pastedIds.AddRange(CopyPasteModelGroupsWithExistingGroupType(sourceDoc, doc, existingModelGroups.Select(e => e as Group).ToList(), existingGroupTypes));
            //
            // TransformElements(doc, pastedIds, new MillimeterPosition(position), new VectorRotation(rotation));
        }
    }

    /// <summary>
    /// Just copy-pastes the modelgroups, knowing that it'll create duplicate grouptypes, and deletes those duplicates afterwards
    /// </summary>
    private List<ElementId> CopyPasteModelGroupsWithExistingGroupType(Document sourceDoc, Document doc,
        List<Group> modelGroups, List<GroupType> existingGroupTypes)
    {
        var pastedIds = new List<ElementId>();
        if (!modelGroups.Any())
            return pastedIds;

        foreach (var modelGroup in modelGroups)
        {
            //Get the grouptype this modelgroup should have when it's pasted into the destination document
            var groupType = existingGroupTypes.First(gt => gt.Name == modelGroup.GroupType.Name);
            var idAsList = new List<ElementId> {modelGroup.Id};
            var pastedIdAsList = _copyPaster.CopyPasteModelElements(sourceDoc, doc, idAsList).ToList();
            if (!pastedIdAsList.Any())
                continue; //Failed to paste

            var pastedGroup = doc.GetElement(pastedIdAsList.First()) as Group;

            using (var transaction = WarningDiscardFailuresPreprocessor.GetTransaction(doc))
            {
                transaction.Start("Delete duplicate grouptype");

                var duplicateGroupType = pastedGroup.GroupType;
                pastedGroup.GroupType = groupType;

                _logger.Trace($"Deleting duplicate grouptype '{duplicateGroupType.Name}'");
                doc.Delete(duplicateGroupType.Id);

                transaction.Commit();
            }

            pastedIds.Add(pastedGroup.Id);
        }

        return pastedIds;
    }

    /// <summary>
    /// Transforms the new elements that were a result of placing the contents of the source document into the destination document
    /// </summary>
    private static void TransformElements(Document doc, IEnumerable<ElementId> elementIds, IPosition position,
        IRotation rotation)
    {
        using (var transaction = WarningDiscardFailuresPreprocessor.GetTransaction(doc))
        {
            transaction.Start("Transform pasted ids");

            foreach (var id in elementIds)
            {
                if (id == null) continue;
                var pastedElement = doc.GetElement(id);

                //Rotate around the origin axis first so we don't mess up with the wrong origin point the new group has
                TransformUtilities.RotateGlobal(pastedElement, rotation);
                TransformUtilities.Move(pastedElement, position);
            }

            transaction.Commit();
        }
    }

    /// <summary>
    /// Returns the modelgroups in the source document that are already present in the destination document
    /// </summary>
    private List<Element> GetExistingModelGroups(IEnumerable<Element> modelElementsInSourceDoc,
        IEnumerable<GroupType> existingGroupTypes)
    {
        var existingGroupTypeNames = existingGroupTypes.Select(gt => gt.Name).ToList();
        var existingModelGroups = modelElementsInSourceDoc
            .Where(e => e is Group group && existingGroupTypeNames.Contains(group.GroupType.Name)).ToList();

        if (existingModelGroups.Any())
        {
            var names = string.Join(", ", existingModelGroups.Select(mg => $"'{mg.Name}'"));
            _logger.Trace(
                $"Found {existingModelGroups.Count} modelgroups that already exist in destination document: {names}");
        }

        return existingModelGroups;
    }

    /// <summary>
    /// Returns the parent model elements that should be copied from this file into the destination document
    /// </summary>
    private List<Element> GetModelElementsInSourceDoc(Document sourceDoc)
    {
        var modelElements = GetModelElements(sourceDoc).ToList();
        if (modelElements.Any()) return modelElements;
        _logger.Error("File doesn't contain model elements");
        return null;
    }

    private List<Element> GetModelElements(Document doc)
    {
        var modelElements = ModelElementCollector.GetParentModelElements(doc).ToList();

        _logger.Trace($"Found {modelElements.Count} modelgroups in source file");
        return modelElements;
    }
}