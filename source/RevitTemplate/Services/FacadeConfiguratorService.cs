using System.IO;
using System.Net.Http;
using System.Text;
using ASRR.Revit.Core.Http;
using ASRR.Revit.Core.RevitModel;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
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
    private readonly Exporter _exporter;
    private readonly string _modelDestinationFolder;
    private readonly string _materialDestinationFolder;

    public FacadeConfiguratorService(HttpService httpService, string modelDestinationFolder,
        string materialDestinationFolder)
    {
        _httpService = httpService ?? new HttpService();
        _modelFetcher = new ModelFetcher(_httpService);
        _wallPlacer = new WallPlacer();
        _modelPlacer = new ModelPlacer();
        _fileUploader = new FileUploader(_httpService);
        _exporter = new Exporter();
        _modelDestinationFolder =
            modelDestinationFolder ?? throw new ArgumentNullException(nameof(modelDestinationFolder));
        _materialDestinationFolder =
            materialDestinationFolder ?? throw new ArgumentNullException(nameof(materialDestinationFolder));
        Directory.CreateDirectory(_modelDestinationFolder);
        Directory.CreateDirectory(_materialDestinationFolder);
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

        var startResponse = _httpService.Post($"/facade-configurations/generation/start/{configId}", null);
        if (!startResponse.IsSuccessStatusCode)
        {
            throw new ConfigurationFailedException(
                "Failed to start configuration. Make sure configuration is not already in progress");
        }

        var status = new FacadeConfigurationStatus();
        try
        {
            Configure(uiApp, configuration, exportSettings, status);
        }
        catch (Exception e)
        {
            var exception = new ConfigurationExceptionDto
            {
                Message = e.Message,
                StackTrace = e.StackTrace,
                Type = e.GetType().Name
            };
            status.Exception = exception;
            PostStatus(configId, status);
            throw;
        }
    }

    private void Configure(UIApplication uiApp, FacadeConfigurationDto configuration, ExportSettings exportSettings,
        FacadeConfigurationStatus status)
    {
        var configId = configuration.Id;
        var exportFolder = Path.Combine(exportSettings.ExportDirectory, "facade-configurations", configId);
        var progress = 10;
        UpdateStatus(configId, status, "Fetching models", progress);
        var storedElements = FetchElementModels(configuration.Openings);

        UpdateStatus(configId, status, "Fetching materials", progress += 10); // == 20
        var storedMaterials = FetchMaterialModels(configuration.Planes);

        if (exportSettings.TemplateFilePath == null)
        {
            throw new ConfigurationFailedException("Facade configuration failed. Template file path not found.");
        }

        using (var newDoc = uiApp.Application.NewProjectDocument(exportSettings.TemplateFilePath))
        {
            UpdateStatus(configId, status, "Placing wall", progress += 10); // == 30
            var wall = _wallPlacer.Place(newDoc, new XYZ(0, 0, 0), configuration.Dimensions.X,
                configuration.Dimensions.Y);

            UpdateStatus(configId, status, "Placing openings", progress += 5); // == 35
            foreach (var opening in configuration.Openings)
            {
                storedElements.TryGetValue(opening.ElementId, out var filePath);
                PlaceOpening(newDoc, wall, opening, filePath);
                UpdateStatus(configId, status, progress += 25 / configuration.Openings.Count);
            }

            UpdateStatus(configId, status, "Placing materials", progress); // == 60
            foreach (var plane in configuration.Planes)
            {
                storedMaterials.TryGetValue(plane.MaterialId, out var textureFiles);
                PlacePlane(newDoc, wall, plane, textureFiles);
                UpdateStatus(configId, status, progress += 25 / configuration.Planes.Count);
            }

            UpdateStatus(configId, status, "Saving Revit file", 85);
            Exporter.SaveFiles(newDoc, configId, exportFolder, exportSettings);
        }

        if (exportSettings.UploadToDb)
        {
            UpdateStatus(configId, status, "Uploading files", 95);
            var uploadPath = $"/facade-configurations/{configId}";
            _fileUploader.Upload(exportFolder, uploadPath, exportSettings, true);
        }

        UpdateStatus(configId, status, "Configuration complete", 100, true);
    }

    private Dictionary<string, string> FetchElementModels(IEnumerable<OpeningDto> openings)
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

    private Dictionary<string, (TextureType, string)> FetchMaterialModels(IEnumerable<PlaneDto> planes)
    {
        var materials =
            _httpService.PostForObject<List<MaterialDto>, IEnumerable<string>>("/materials/find",
                planes.Select(o => o.MaterialId));

        var storedMaterials = new Dictionary<string, (TextureType, string)>(); // maybe make this prettier if needed

        foreach (var material in materials)
        {
            foreach (var texture in material.Textures)
            {
                if (texture.File == null) continue;

                var fileName = $"{material.Name}.{texture.File.Extension}";
                var fetchPath = $"/blob-storage/download/{texture.File.BlobId}/{fileName}";
                var destinationPath = Path.Combine(_materialDestinationFolder, fileName);

                if (File.Exists(destinationPath) && (File.GetLastWriteTime(destinationPath) > texture.File.Created))
                {
                    storedMaterials[material.Id] = (texture.GetTextureType(), destinationPath);
                    continue;
                }

                if (_modelFetcher.Fetch(fetchPath, destinationPath))
                    storedMaterials[material.Id] = (texture.GetTextureType(), destinationPath);
            }
        }

        return storedMaterials;
    }

    private void PlaceOpening(Document doc, Wall wall, OpeningDto opening, string filePath)
    {
        var position = new XYZ(opening.Position.X, opening.Position.Z, opening.Position.Y);
        _wallPlacer.CreateOpening(doc, wall, position, opening.Width, opening.Height);
        _modelPlacer.Place(doc, filePath, position, new XYZ(0, 0, 0), 0);
    }

    private void PlacePlane(Document doc, Wall wall, PlaneDto plane, (TextureType, string) textureFiles)
    {
        // TODO
    }

    private void UpdateStatus(string configId, FacadeConfigurationStatus status, double progress = 0.0)
    {
        status.Progress = progress;
        PostStatus(configId, status);
    }

    private void UpdateStatus(string configId, FacadeConfigurationStatus status, string message = null,
        double progress = 0.0, bool finished = false)
    {
        status.Message = message;
        status.Progress = progress;
        status.Finished = finished;
        PostStatus(configId, status);
    }

    private void PostStatus(string configId, FacadeConfigurationStatus status)
    {
        var payload = JsonConvert.SerializeObject(status);
        var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");
        _httpService.Post($"/facade-configurations/generation/status/{configId}", httpContent);
    }
}