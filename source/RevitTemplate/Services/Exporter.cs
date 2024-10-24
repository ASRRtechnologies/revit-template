using System.IO;
using ASRR.Revit.Core.Exporter.GLTF.Core;
using ASRR.Revit.Core.Exporter.GLTF.Model;
using ASRR.Revit.Core.Utilities;
using RevitTemplate.Settings;

namespace RevitTemplate.Services;

public static class Exporter
{
    public static bool SaveFiles(Document doc, string entityId, string exportFolder, ExportSettings exportSettings)
    {
        if (entityId == null) throw new ArgumentNullException(nameof(entityId));
        if (exportFolder == null) throw new ArgumentNullException(nameof(exportFolder));

        if (Directory.Exists(exportFolder)) Directory.Delete(exportFolder, true);
        Directory.CreateDirectory(exportFolder);

        var savedAll = true;
        try
        {
            if (exportSettings.Rvt) savedAll &= SaveRevitFile(doc, entityId, exportFolder);
            if (exportSettings.Pdf) savedAll &= SavePdf(doc, entityId, exportFolder);
            if (exportSettings.Glb) savedAll &= SaveGlb(doc, entityId, exportFolder, exportSettings.GlbExportViewName);
        }
        catch (Exception)
        {
            doc.Close();
            FileUtilities.RemoveBackUpFilesFromDirectory(exportFolder);
            throw;
        }

        doc.Close();
        FileUtilities.RemoveBackUpFilesFromDirectory(exportFolder);
        return savedAll;
    }

    private static bool SaveRevitFile(Document doc, string entityId, string exportFolder)
    {
        var rvtFilePath = Path.Combine(exportFolder, $"{entityId}.rvt");

        var options = new SaveAsOptions();
        options.OverwriteExistingFile = true;

        doc.SaveAs(rvtFilePath, options);
        return true;
    }

    private static bool SavePdf(Document doc, string entityId, string exportFolder)
    {
        var pdfExportOptions = new PDFExportOptions();
        pdfExportOptions.FileName = $"{entityId}_{DateTime.Now:yyyy-MM-dd}";

        var collector = new FilteredElementCollector(doc).OfClass(typeof(ViewSheet));
        var viewPlans = collector.Cast<ViewSheet>().Where(view => view.Name.StartsWith("")).ToList();

        return doc.Export(exportFolder, viewPlans.Select(v => v.Id).ToList(), pdfExportOptions);
    }

    private static bool SaveGlb(Document doc, string entityId, string exportFolder, string viewName = null)
    {
        var context = CreateExportContext(doc, entityId, exportFolder);
        var exporter = new CustomExporter(doc, context)
        {
            IncludeGeometricObjects = true,
            ShouldStopOnError = false
        };
                
        var view3D = Collector.GetFirstOfType<View3D>(doc);
        var allViews = Collector.GetAllOfType<View3D>(doc);

        var glbView = allViews.FirstOrDefault(v => v.Name == viewName) ?? view3D;
        return TryToExport(exporter, glbView);
    }
    
    private static GLTFExportContext CreateExportContext(Document doc, string entityId, string exportFolder)
    {
        var filePath = Path.Combine(exportFolder, entityId);
        var preferences = new Preferences()
        {
            path = filePath,
            fileName = entityId
        };
        
        var ctx = new GLTFExportContext(doc, preferences);
        return ctx;
    }
    
    private static bool TryToExport(CustomExporter exporter, View view)
    {
        try
        {
            exporter.Export(view);
            return true;
        }
        catch (Exception)
        {
            // Logger.Error($"Failed to export because '{e.Message}'");
            return false;
        }
    }
}