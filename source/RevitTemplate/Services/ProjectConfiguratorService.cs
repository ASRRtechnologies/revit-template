using System.IO;
using System.Net;
using ASRR.Revit.Core.Http;
using ASRR.Revit.Core.Model;
using ASRR.Revit.Core.RevitModel;
using Autodesk.Revit.UI;
using RevitTemplate.Dto;
using RevitTemplate.Exceptions;
using RevitTemplate.Settings;
using RevitTemplate.Utilities;

namespace RevitTemplate.Services;

public class ProjectConfiguratorService
{
    private readonly HttpService _httpService;
    private readonly ModelFetcher _modelFetcher;
    private readonly FileUploader _fileUploader;
    private readonly ModelPlacer _modelPlacer;
    private readonly WallService _wallService;
    private readonly FacadeConfiguratorService _facadeConfiguratorService;
    private readonly string _dynamicModelDestinationFolder;

    public ProjectConfiguratorService(HttpService httpService, ModelFetcher modelFetcher, FileUploader fileUploader,
        FacadeConfiguratorService facadeConfiguratorService, string facadeDestinationFolder)
    {
        _httpService = httpService ?? new HttpService();
        _modelFetcher = modelFetcher ?? new ModelFetcher(_httpService);
        _fileUploader = fileUploader ?? new FileUploader(_httpService);
        _modelPlacer = new ModelPlacer();
        _wallService = new WallService();
        _facadeConfiguratorService = facadeConfiguratorService
                                     ?? throw new ArgumentNullException(nameof(facadeConfiguratorService));
        _dynamicModelDestinationFolder = facadeDestinationFolder
                                         ?? throw new ArgumentNullException(nameof(facadeDestinationFolder));
        Directory.CreateDirectory(_dynamicModelDestinationFolder);
    }

    public void Configure(UIApplication uiApp, string queueId, ExportSettings exportSettings)
    {
        if (queueId == null)
        {
            throw new ConfigurationFailedException("Configuration failed. Queue item id is null");
        }

        var authResponse = _httpService.Get($"/auth/user-info");
        if (!authResponse.IsSuccessStatusCode)
            throw new ConfigurationFailedException("Failed to authenticate to API. Make sure API key is valid");


        var queueItem = _httpService.GetForObject<QueueItemDto>($"/queues/find/{queueId}")
                        ?? throw new ConfigurationFailedException(
                            $"Failed to fetch queue item with id '{queueId}' from db");

        var status = new ProjectConfigurationStatus()
        {
            WorkerId = Dns.GetHostEntry("LocalHost").HostName,
            Message = "Starting configuration"
        };

        var started =
            _httpService.PostForObject<QueueItemDto, ProjectConfigurationStatus>($"/queues/job/{queueId}", status);

        if (started == null)
        {
            throw new ConfigurationFailedException(
                $"Failed to start job for queue item '{queueId}'. Make sure job is not locked");
        }

        try
        {
            Configure(uiApp, queueItem, exportSettings, status);
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
            PostStatus(queueId, status);
            throw;
        }
    }

    private void Configure(UIApplication uiApp, QueueItemDto queueItem, ExportSettings exportSettings,
        ProjectConfigurationStatus status)
    {
        var queueId = queueItem.Id;
        var exportFolder = Path.Combine(exportSettings.ExportDirectory, "project-configurations", queueId);
        var progress = 5;
        UpdateStatus(queueId, status, "Fetching project geometry", progress);
        var projectGeometry = _httpService.GetForObject<ProjectGeometryDto>($"/queues/job/{queueId}/geometry")
                              ?? throw new ConfigurationFailedException(
                                  $"Failed to fetch project geometry for queue item '{queueId}'");

        if (exportSettings.TemplateFilePath == null)
        {
            throw new ConfigurationFailedException("Configuration failed. Template file not found.");
        }

        progress = 10;
        var totalBlocks = projectGeometry.Blocks.Count;
        var progressPerBlock = 65 / totalBlocks;
        var i = 1;
        foreach (var block in projectGeometry.Blocks)
        {
            UpdateStatus(queueId, status, $"Generating block {i}/{totalBlocks}", progress);
            block.filePath = ConfigureBlock(uiApp, block, exportFolder, exportSettings, status, queueId,
                progressPerBlock);
            progress += progressPerBlock;
            i++;
        }

        progress = 75;
        var j = 1;
        using var newDoc = uiApp.Application.NewProjectDocument(exportSettings.TemplateFilePath);
        foreach (var block in projectGeometry.Blocks)
        {
            UpdateStatus(queueId, status, $"Placing block {j}/{totalBlocks}", progress);
            if (block.filePath != null)
            {
                var blockConfig = block.BlockConfiguration;
                _modelPlacer.Place(newDoc, block.filePath, blockConfig.Position.ToXyz(), null, 0);

                GroupUtilities.RotateGroup(newDoc, blockConfig.BlockId, new DegreeRotation(blockConfig.Rotation.Y));
            }

            progress += 10 / totalBlocks;
            j++;
        }

        UpdateStatus(queueId, status, "Saving files", 85);
        Exporter.SaveFiles(newDoc, projectGeometry.Id, exportFolder, exportSettings);

        if (exportSettings.UploadToDb)
        {
            UpdateStatus(queueId, status, "Uploading files", 95);
            var uploadPath = $"/queues/job/{queueId}/upload";
            _fileUploader.Upload(exportFolder, uploadPath, exportSettings);
        }

        UpdateStatus(queueId, status, "Configuration complete", 100, true);
    }

    private string ConfigureBlock(UIApplication uiApp, BlockGeometry block, string projectExportFolder,
        ExportSettings exportSettings, ProjectConfigurationStatus status, string queueId, double allottedProgress)
    {
        var blockConfiguration = block.BlockConfiguration;
        var exportFolder = Path.Combine(projectExportFolder, "block-configurations", blockConfiguration.BlockId);

        var progress = status.Progress;
        var totalHouses = block.Houses.Count;
        var progressPerHouse = allottedProgress / totalHouses / 2;

        var i = 1;
        foreach (var house in block.Houses)
        {
            UpdateStatus(queueId, status, $"Generating house {i}/{totalHouses} of block {blockConfiguration.BlockId}",
                progress);
            house.filePath = ConfigureHouse(uiApp, house, exportFolder, exportSettings, status, queueId,
                progressPerHouse);
            progress += progressPerHouse;
            i++;
        }

        using var newDoc = uiApp.Application.NewProjectDocument(exportSettings.TemplateFilePath);
        foreach (var house in block.Houses)
        {
            if (house.filePath != null)
            {
                var houseConfig = house.HouseConfiguration;
                _modelPlacer.Place(newDoc, house.filePath, houseConfig.Position.ToXyz(), null, 0);

                var groupName = $"bnr_{houseConfig.Bnr}";
                GroupUtilities.RotateGroup(newDoc, groupName, new DegreeRotation(houseConfig.Rotation.Y));
            }

            UpdateStatus(queueId, status, progress += progressPerHouse);
        }

        GroupUtilities.CreateGroup(newDoc, blockConfiguration.BlockId);
        return Exporter.SaveRevitFileAndClose(newDoc, blockConfiguration.BlockId, exportFolder);
    }

    private string ConfigureHouse(UIApplication uiApp, HouseGeometry house, string blockExportFolder,
        ExportSettings exportSettings, ProjectConfigurationStatus status, string queueId, double allottedProgress)
    {
        var houseConfiguration = house.HouseConfiguration;
        var exportFolder = Path.Combine(blockExportFolder, "house-configurations", houseConfiguration.Bnr);

        var progress = status.Progress;
        var totalDynamicModels = house.DynamicModels.Count;
        var progressPerDynamicModel = allottedProgress / totalDynamicModels / 2;

        var i = 1;
        foreach (var dynamicModel in house.DynamicModels)
        {
            dynamicModel.filePath = GenerateDynamicModel(uiApp, dynamicModel, exportSettings);
            UpdateStatus(queueId, status, progress += progressPerDynamicModel);
            i++;
        }

        using var newDoc = uiApp.Application.NewProjectDocument(exportSettings.TemplateFilePath);
        foreach (var dynamicModel in house.DynamicModels)
        {
            if (dynamicModel.filePath != null)
            {
                var x = dynamicModel.Rotation.Y switch
                {
                    0 => dynamicModel.Position.X + (dynamicModel.Dimensions.X / 2),
                    180 => dynamicModel.Position.X - (dynamicModel.Dimensions.X / 2),
                    90 => dynamicModel.Position.X + (dynamicModel.Dimensions.Z / 2),
                    -90 => dynamicModel.Position.X - (dynamicModel.Dimensions.Z / 2),
                    _ => dynamicModel.Position.X
                };

                var y = dynamicModel.Rotation.Y switch
                {
                    0 => (-dynamicModel.Position.Z) + (dynamicModel.Dimensions.Z / 2),
                    180 => (-dynamicModel.Position.Z) - (dynamicModel.Dimensions.Z / 2),
                    90 => (-dynamicModel.Position.Z) + (dynamicModel.Dimensions.X / 2),
                    -90 => (-dynamicModel.Position.Z) - (dynamicModel.Dimensions.X / 2),
                    _ => -dynamicModel.Position.Z
                };

                var position = new XYZ(x, y, dynamicModel.Position.Y);
                _modelPlacer.Place(newDoc, dynamicModel.filePath, position, null, 0);

                var groupName = dynamicModel.FacadeConfiguration != null
                    ? dynamicModel.FacadeConfiguration.Id
                    : dynamicModel.Id;

                GroupUtilities.RotateGroup(newDoc, groupName, new DegreeRotation(dynamicModel.Rotation.Y));
            }

            UpdateStatus(queueId, status, progress += progressPerDynamicModel);
        }

        var name = $"bnr_{houseConfiguration.Bnr}";
        GroupUtilities.CreateGroup(newDoc, name);
        return Exporter.SaveRevitFileAndClose(newDoc, name, exportFolder, true);
    }

    private string GenerateDynamicModel(UIApplication uiApp, DynamicModelGeometry dynamicModel,
        ExportSettings exportSettings)
    {
        var facadeConfiguration = dynamicModel.FacadeConfiguration;

        if (facadeConfiguration == null)
        {
            using var newDoc = uiApp.Application.NewProjectDocument(exportSettings.TemplateFilePath);
            // _wallService.Place(newDoc, new XYZ(0, 0, 0), dynamicModel.Dimensions.X,
            //     dynamicModel.Dimensions.Y, dynamicModel.Rotation.ToXyz());
            _wallService.Place(newDoc, new XYZ(0, 0, 0), dynamicModel.Dimensions.X,
                dynamicModel.Dimensions.Y);
            GroupUtilities.CreateGroup(newDoc, dynamicModel.Id);
            return Exporter.SaveRevitFileAndClose(newDoc, dynamicModel.Id, _dynamicModelDestinationFolder);
        }

        var fileName = $"{facadeConfiguration.Id}.rvt";
        var destinationPath = Path.Combine(_dynamicModelDestinationFolder, "facade-configurations",
            facadeConfiguration.Id, fileName);

        var rvtFile = facadeConfiguration.Files.FirstOrDefault(f => f.Extension == "rvt");

        var toGenerate =
            rvtFile == null ||
            rvtFile.Created < facadeConfiguration.ConfigurationLastChanged ||
            (File.Exists(destinationPath) &&
             File.GetLastWriteTime(destinationPath) < facadeConfiguration.ConfigurationLastChanged);

        if (toGenerate)
        {
            var facadeExportSettings = new ExportSettings
            {
                TemplateFilePath = exportSettings.TemplateFilePath,
                ExportDirectory = _dynamicModelDestinationFolder,
                GlbExportViewName = exportSettings.GlbExportViewName
            };
            _facadeConfiguratorService.Configure(uiApp, facadeConfiguration.Id, facadeExportSettings);
            return destinationPath;
        }

        var fetchPath = $"/blob-storage/download/{rvtFile.BlobId}/{fileName}";
        if (File.Exists(destinationPath) || _modelFetcher.Fetch(fetchPath, destinationPath)) return destinationPath;
        return null;
    }

    private void UpdateStatus(string queueItemId, ProjectConfigurationStatus status, double progress = 0.0)
    {
        status.Progress = progress;
        PostStatus(queueItemId, status);
    }

    private void UpdateStatus(string queueItemId, ProjectConfigurationStatus status, string message = null,
        double progress = 0.0, bool finished = false)
    {
        status.Message = message;
        status.Progress = progress;
        status.Finished = finished;
        PostStatus(queueItemId, status);
    }

    private void PostStatus(string queueItemId, ProjectConfigurationStatus status)
    {
        _httpService.PostForObject<QueueItemDto, ProjectConfigurationStatus>($"/queues/job/status/{queueItemId}",
            status);
    }
}