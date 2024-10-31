using System.IO;
using System.Net;
using ASRR.Revit.Core.Http;
using ASRR.Revit.Core.Model;
using ASRR.Revit.Core.RevitModel;
using ASRR.Revit.Core.Warnings;
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

    public void Configure(UIApplication uiApp, string queueItemId, ExportSettings exportSettings)
    {
        if (queueItemId == null)
        {
            throw new ConfigurationFailedException("Project configuration failed. Queue item id is null");
        }

        // TODO: replace this get with start job when status flow is in place .. or do it after this call so u can check if exists like in facade?
        var queueItem = _httpService.GetForObject<QueueItemDto>($"/queues/find/{queueItemId}")
                        ?? throw new ConfigurationFailedException(
                            $"Configuration failed. Failed to fetch queue item with id '{queueItemId}' from db");

        var status = new ProjectConfigurationStatus()
        {
            WorkerId = Dns.GetHostEntry("LocalHost").HostName,
            Message = "Starting configuration"
        };

        // todo: start job w/ status

        try
        {
            Configure(uiApp, queueItem, exportSettings, status);
        }
        catch (Exception)
        {
            // TODO: Update status w/ exception and post
            throw;
        }
    }

    private void Configure(UIApplication uiApp, QueueItemDto queueItem, ExportSettings exportSettings,
        ProjectConfigurationStatus status)
    {
        var queueId = queueItem.Id;
        var exportFolder = Path.Combine(exportSettings.ExportDirectory, "project-configurations", queueId);
        // var progress = 5;
        var projectGeometry = _httpService.GetForObject<ProjectGeometryDto>($"/queues/job/{queueId}/geometry")
                       ?? throw new ConfigurationFailedException(
                           $"Configuration failed. Failed to fetch geometry for queue item with id '{queueId}' from db");

        if (exportSettings.TemplateFilePath == null)
        {
            throw new ConfigurationFailedException("Configuration failed. Template file path not found.");
        }

        foreach (var block in projectGeometry.Blocks)
        {
            block.filePath = ConfigureBlock(uiApp, block, exportFolder, exportSettings, status);
        }
        
        using var newDoc = uiApp.Application.NewProjectDocument(exportSettings.TemplateFilePath);
        foreach (var block in projectGeometry.Blocks)
        {
            if (block.filePath == null) continue;
            var blockConfig = block.BlockConfiguration;
            _modelPlacer.Place(newDoc, block.filePath, blockConfig.Position.ToXyz(), null, 0);
            
            GroupUtilities.RotateGroup(newDoc, blockConfig.BlockId, new DegreeRotation(blockConfig.Rotation.Y));
        }
        
        Exporter.SaveRevitFileAndClose(newDoc, projectGeometry.Id, exportFolder);
    }

    private string ConfigureBlock(UIApplication uiApp, BlockGeometry block, string projectExportFolder,
        ExportSettings exportSettings, ProjectConfigurationStatus status)
    {
        var blockConfiguration = block.BlockConfiguration;
        var exportFolder = Path.Combine(projectExportFolder, "block-configurations", blockConfiguration.BlockId);
        foreach (var house in block.Houses)
        {
            house.filePath = ConfigureHouse(uiApp, house, exportFolder, exportSettings, status);
        }
        
        using var newDoc = uiApp.Application.NewProjectDocument(exportSettings.TemplateFilePath);
        foreach (var house in block.Houses)
        {
            if (house.filePath == null) continue;
            var houseConfig = house.HouseConfiguration;
            _modelPlacer.Place(newDoc, house.filePath, houseConfig.Position.ToXyz(), null, 0);
            
            var groupName = $"bnr_{houseConfig.Bnr}";
            GroupUtilities.RotateGroup(newDoc, groupName, new DegreeRotation(houseConfig.Rotation.Y));
        }
        
        GroupUtilities.CreateGroup(newDoc, blockConfiguration.BlockId);
        return Exporter.SaveRevitFileAndClose(newDoc, blockConfiguration.BlockId, exportFolder);
    }

    private string ConfigureHouse(UIApplication uiApp, HouseGeometry house, string blockExportFolder,
        ExportSettings exportSettings, ProjectConfigurationStatus status)
    {
        var houseConfiguration = house.HouseConfiguration;
        var exportFolder = Path.Combine(blockExportFolder, "house-configurations", houseConfiguration.Bnr);
        foreach (var dynamicModel in house.DynamicModels)
        {
            dynamicModel.filePath = GenerateDynamicModel(uiApp, dynamicModel, exportSettings, status);
        }

        using var newDoc = uiApp.Application.NewProjectDocument(exportSettings.TemplateFilePath);
        foreach (var dynamicModel in house.DynamicModels)
        {
            if (dynamicModel.filePath == null) continue;

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

        var name = $"bnr_{houseConfiguration.Bnr}";
        GroupUtilities.CreateGroup(newDoc, name);
        return Exporter.SaveRevitFileAndClose(newDoc, name, exportFolder, true);
    }

    private string GenerateDynamicModel(UIApplication uiApp, DynamicModelGeometry dynamicModel,
        ExportSettings exportSettings, ProjectConfigurationStatus status)
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
}