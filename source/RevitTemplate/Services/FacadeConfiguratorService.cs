﻿using System.IO;
using ASRR.Revit.Core.Http;
using ASRR.Revit.Core.RevitModel;
using ASRR.Revit.Core.Utilities;
using Autodesk.Revit.UI;
using RevitTemplate.Dto;
using RevitTemplate.Exceptions;
using RevitTemplate.Settings;

namespace RevitTemplate.Services;

public class FacadeConfiguratorService
{
    private readonly HttpService _httpService;
    private readonly ModelFetcher _modelFetcher;
    private readonly WallPlacer _wallPlacer;
    private readonly ModelPlacer _modelPlacer;
    private readonly FileUploader _fileUploader;
    private readonly string _modelDestinationFolder;

    public FacadeConfiguratorService(HttpService httpService, string modelDestinationFolder)
    {
        _httpService = httpService ?? new HttpService();
        _modelFetcher = new ModelFetcher(_httpService);
        _wallPlacer = new WallPlacer();
        _modelPlacer = new ModelPlacer();
        _fileUploader = new FileUploader(_httpService);
        _modelDestinationFolder =
            modelDestinationFolder ?? throw new ArgumentNullException(nameof(modelDestinationFolder));
        Directory.CreateDirectory(_modelDestinationFolder);
    }

    public void Configure(UIApplication uiApp, string configId, ExportSettings exportSettings)
    {
        if (configId == null)
        {
            throw new ConfigurationFailedException("Facade configuration failed. ConfigurationId is null");
        }

        var configuration = _httpService.GetForObject<FacadeConfigurationDto>($"/facade-configurations/find/{configId}")
                            ?? throw new ConfigurationFailedException(
                                $"Facade configuration failed. Failed to fetch configuration with id '{configId}' from db");

        var storedElements = FetchElementModels(configuration.Openings);
        var storedMaterials = FetchMaterialModels(configuration.Planes);

        if (exportSettings.TemplateFilePath == null)
        {
            // TODO: Update db status
            throw new ConfigurationFailedException("Facade configuration failed. Template file path not found.");
        }

        using (var newDoc = uiApp.Application.NewProjectDocument(exportSettings.TemplateFilePath))
        {
            var wall = _wallPlacer.Place(newDoc, new XYZ(0, 0, 0), configuration.Dimensions.X,
                configuration.Dimensions.Y);
            
            foreach (var opening in configuration.Openings)
            {
                var position = new XYZ(opening.Position.X, opening.Position.Z, opening.Position.Y);
                _wallPlacer.CreateOpening(newDoc, wall, position, opening.Width, opening.Height);
                var filePath = storedElements[opening.ElementId];
                _modelPlacer.Place(newDoc, filePath, position, new XYZ(0, 0, 0), 0);
            }

            var exportFolder = SaveRevitFile(newDoc, configuration, exportSettings.ExportDirectory);
        }

        if (exportSettings.UploadToDb)
        {
            _fileUploader.Upload(configId, exportSettings);
        }
    }

    private Dictionary<string, string> FetchElementModels(List<OpeningDto> openings)
    {
        var elements =
            _httpService.PostForObject<List<ElementDto>, IEnumerable<string>>("/elements/find",
                openings.Select(o => o.ElementId));

        var storedElements = new Dictionary<string, string>();

        foreach (var element in elements)
        {
            if (element.Files == null || !element.Files.Any())
            {
                // _logger.Error($"No files found for element '{element.Name}'. Skipping download..");
                continue;
            }

            var rvtFile = element.Files.FirstOrDefault(f => f.Extension == "rvt");

            if (rvtFile == null)
            {
                // _logger.Error($"No rvt file found for element '{element.Name}'. Skipping download..");
                continue;
            }

            var fileName = $"{element.Name}.rvt";
            var fetchPath = $"/blob-storage/download/{rvtFile.BlobId}/{fileName}";
            var destinationPath = Path.Combine(_modelDestinationFolder, fileName);

            if (File.Exists(destinationPath) && (File.GetLastWriteTime(destinationPath) > rvtFile.Created))
            {
                storedElements[element.Id] = destinationPath;
                continue;
            }

            if (_modelFetcher.Fetch(fetchPath, destinationPath)) storedElements[element.Id] = destinationPath;
        }

        return storedElements;
    }

    private Dictionary<string, string> FetchMaterialModels(List<PlaneDto> planes)
    {
        // TODO: dto wordt anders
        var materials =
            _httpService.PostForObject<List<MaterialDto>, IEnumerable<string>>("/materials/find",
                planes.Select(o => o.MaterialId));

        var storedMaterials = new Dictionary<string, string>();

        foreach (var material in materials)
        {
            if (material.TextureFile == null)
            {
                // _logger.Error($"No files found for material '{material.Name}'. Skipping download..");
                continue;
            }

            var rvtFile = (material.TextureFile.Extension == "rvt") ? material.TextureFile : null;

            if (rvtFile == null)
            {
                // _logger.Error($"No rvt file found for material '{material.Name}'. Skipping download..");
                continue;
            }

            var fileName = $"{material.Name}.rvt";
            var fetchPath = $"/blob-storage/download/{rvtFile.BlobId}/{fileName}";
            var destinationPath = Path.Combine(_modelDestinationFolder, fileName);

            if (File.Exists(destinationPath) && (File.GetLastWriteTime(destinationPath) > rvtFile.Created))
            {
                storedMaterials[material.Id] = destinationPath;
                continue;
            }

            if (_modelFetcher.Fetch(fetchPath, destinationPath)) storedMaterials[material.Id] = destinationPath;
        }

        return storedMaterials;
    }

    private string SaveRevitFile(Document doc, FacadeConfigurationDto configuration, string exportDirectory)
    {
        if (exportDirectory == null)
        {
            // TODO: Update db status
            throw new ConfigurationFailedException("Cannot save revit file. No export directory is given.");
        }

        Directory.CreateDirectory(exportDirectory);
        var exportFolder = Path.Combine(exportDirectory, configuration.Id);

        if (Directory.Exists(exportFolder))
            Directory.Delete(exportFolder, true);

        Directory.CreateDirectory(exportFolder);

        var rvtFilePath = Path.Combine(exportFolder, $"{configuration.Id}.rvt");
        // status.UpdateProgress("Saving .rvt", 70);

        var options = new SaveAsOptions();
        options.OverwriteExistingFile = true;

        doc.SaveAs(rvtFilePath, options);
        // _logger.Info($"Saved Revit file at '{rvtFilePath}'");

        // status.UpdateProgress("Exporting views to pdf", 80);
        SavePdf(doc, exportFolder, configuration);

        // status.UpdateProgress("Exporting ifc", 90);
        // _ifcExporter.Export(doc, exportFolder, configuration);

        doc.Close();
        FileUtilities.RemoveBackUpFilesFromDirectory(exportFolder);
        return exportFolder;
    }

    private static void SavePdf(Document doc, string exportFolder, FacadeConfigurationDto configuration)
    {
        var pdfExportOptions = new PDFExportOptions();
        pdfExportOptions.FileName = $"{configuration.Name}_{DateTime.Now:yyyy-MM-dd}";

        try
        {
            var collector = new FilteredElementCollector(doc).OfClass(typeof(ViewSheet));
            var viewPlans = collector.Cast<ViewSheet>().Where(view => view.Name.StartsWith("")).ToList();
            
            doc.Export(
                exportFolder,
                viewPlans.Select(v => v.Id).ToList(),
                pdfExportOptions
            );
        }
        catch (Exception)
        {
            doc.Close();
            FileUtilities.RemoveBackUpFilesFromDirectory(exportFolder);
            throw;
        }
    }
}