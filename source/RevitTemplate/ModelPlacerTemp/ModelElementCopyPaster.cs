using ASRR.Revit.Core;
using ASRR.Revit.Core.Utilities;
using ASRR.Revit.Core.Warnings;
using NLog;

namespace RevitTemplate.ModelPlacerTemp;

public class ModelElementCopyPaster
{
    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    protected readonly CopyPasteOptions copyOptions =
        UseDestinationHandler.UseDestinationOnDuplicateNameCopyPasteOptions();

    protected static CopyPasteConfiguration DefaultConfiguration =>
        new CopyPasteConfiguration
        {
            SeparateTransactions = false,
            CopyLevels = true
        };

    public IEnumerable<ElementId> CopyPasteModelElements(Document sourceDoc, Document destinationDoc,
        ICollection<ElementId> elementIds, CopyPasteConfiguration configuration = null)
    {
        if (configuration == null)
            configuration = DefaultConfiguration;

        if (configuration.CopyLevels)
            CopyLevels(sourceDoc, destinationDoc);

        var pastedIds = configuration.SeparateTransactions
            ? CopyPasteInSeparateTransactions(sourceDoc, destinationDoc, elementIds)
            : CopyPasteInSingleTransaction(sourceDoc, destinationDoc, elementIds);

        return pastedIds;
    }

    protected virtual IEnumerable<ElementId> CopyPasteInSingleTransaction(Document sourceDoc, Document destinationDoc,
        ICollection<ElementId> elementIds)
    {
        var pastedIds = new List<ElementId>();
        using (var transaction = WarningDiscardFailuresPreprocessor.GetTransaction(destinationDoc))
        {
            transaction.Start("Copy paste modelelements");
            pastedIds.AddRange(elementIds.Select(id => CopyModelElement(sourceDoc, destinationDoc, id)));
            transaction.Commit();
            return pastedIds;
        }
    }

    protected virtual IEnumerable<ElementId> CopyPasteInSeparateTransactions(Document sourceDoc,
        Document destinationDoc,
        IEnumerable<ElementId> elementIds)
    {
        var pastedIds = new List<ElementId>();
        foreach (var id in elementIds)
        {
            using (var transaction = WarningDiscardFailuresPreprocessor.GetTransaction(destinationDoc))
            {
                transaction.Start($"Copy paste modelelement with id {id.IntegerValue}");
                var copiedId = CopyModelElement(sourceDoc, destinationDoc, id);
                if (copiedId != null) pastedIds.Add(copiedId);
                transaction.Commit();
            }
        }

        return pastedIds;
    }

    protected ElementId CopyModelElement(Document sourceDoc, Document destinationDoc, ElementId modelElementId)
    {
        var idAsList = new List<ElementId> {modelElementId};

        _logger.Trace($"Copying modelElement '{sourceDoc.GetElement(modelElementId).Name}'");

        try
        {
            var copiedId = ElementTransformUtils.CopyElements(sourceDoc, idAsList,
                destinationDoc, Transform.Identity, copyOptions).First();

            return copiedId;
        }
        catch (Exception e)
        {
            return null;
        }
    }

    protected ICollection<ElementId> CopyModelElements(Document sourceDoc, Document destinationDoc,
        ICollection<ElementId> modelElementIds)
    {
        _logger.Trace(
            $"Copying modelElements '{string.Join(", ", modelElementIds.Select(id => sourceDoc.GetElement(id).Name))}'");

        var copiedIds = ElementTransformUtils.CopyElements(sourceDoc, modelElementIds,
            destinationDoc, Transform.Identity, copyOptions);

        return copiedIds;
    }

    protected void CopyLevels(Document sourceDoc, Document destinationDoc)
    {
        //Copy all the levels that are not already in the destination document
        using (var transaction = WarningDiscardFailuresPreprocessor.GetTransaction(destinationDoc))
        {
            transaction.Start("Copy levels");

            var sourceLevels = Collector.GetAllOfType<Level>(sourceDoc);
            var destinationLevels = Collector.GetAllOfType<Level>(destinationDoc);

            foreach (var level in sourceLevels.Where(level =>
                         destinationLevels.Find(l => l.Name == level.Name) == null))
            {
                _logger.Trace($"Copying level '{level.Name}' to new file");
                var idAsList = new List<ElementId> {level.Id};
                ElementTransformUtils.CopyElements(sourceDoc, idAsList, destinationDoc, Transform.Identity,
                    copyOptions);
            }

            transaction.Commit();
        }
    }
}