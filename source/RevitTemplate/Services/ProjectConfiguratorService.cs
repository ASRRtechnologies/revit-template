using System.IO;
using ASRR.Revit.Core.Http;
using ASRR.Revit.Core.RevitModel;
using Autodesk.Revit.UI;
using RevitTemplate.Exceptions;
using RevitTemplate.Settings;

namespace RevitTemplate.Services;

public class ProjectConfiguratorService
{
    private readonly HttpService _httpService;
    private readonly ModelFetcher _modelFetcher;
    private readonly FileUploader _fileUploader;
    private readonly FacadeConfiguratorService _facadeConfiguratorService;
    private readonly string _facadeDestinationFolder;
    
    public ProjectConfiguratorService(HttpService httpService, ModelFetcher modelFetcher, FileUploader fileUploader, string facadeDestinationFolder)
    {
        _httpService = httpService ?? new HttpService();
        _modelFetcher = modelFetcher ?? new ModelFetcher(_httpService);
        _fileUploader = fileUploader ?? new FileUploader(_httpService);
        _facadeDestinationFolder =
            facadeDestinationFolder ?? throw new ArgumentNullException(nameof(facadeDestinationFolder));
        Directory.CreateDirectory(_facadeDestinationFolder);
    }

    public void Configure(UIApplication uiApp, string queueItemId, ExportSettings exportSettings)
    {
        if (queueItemId == null)
        {
            throw new ConfigurationFailedException("Project configuration failed. Queue item id is null");
        }
    }
}